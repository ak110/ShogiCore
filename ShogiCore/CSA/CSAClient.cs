using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ShogiCore.Notation;

namespace ShogiCore.CSA {
    /// <summary>
    /// CSA通信プロトコルのクライアントの実装
    /// </summary>
    /// <remarks>
    /// http://www.computer-shogi.org/protocol/tcp_ip_server_112.html
    /// キープアライブは行うが、切断時は何もしない。(再接続は呼び元側で行う。)
    /// </remarks>
    public class CSAClient : IDisposable {
        /// <summary>
        /// デフォルトのポート番号
        /// </summary>
        public const ushort DefaultPort = 4081;

        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly log4net.ILog csaLogger = log4net.LogManager.GetLogger("CSALogger");

        static readonly Regex regexMove = new Regex(@"([+-][0-9]{4}[A-Z]{2})(\s*,\s*T(\d+))?", RegexOptions.Compiled);

        object connectLock = new object();
        object writerLock = new object();
        TcpClient client;
        StreamReader reader;
        StreamWriter writer;

        readonly Thread receiveThread = null;
        readonly Thread keepAliveThread = null;
        volatile bool threadValid = true;
        int lastTCPTick = Environment.TickCount;

        readonly List<string> beginEndStack = new List<string>(); // BEGIN Game_Summaryとかの処理の為のスタック。
        StringBuilder positionData = null;

        readonly PCLNotationReader pclReader = new PCLNotationReader();

        readonly Queue<CSAInternalCommand> commandQueue = new Queue<CSAInternalCommand>();

        /// <summary>
        /// PVを送るのかどうか。既定値false。
        /// </summary>
        public bool SendPV { get; set; }
        /// <summary>
        /// 改行の送信によるKeepAliveをするのかどうか。Windowsでは通常はTCPのKeepAliveを行うため不要。既定値false。
        /// </summary>
        public bool KeepAlive { get; set; }
        /// <summary>
        /// 終局時にログアウトするのかどうか。既定値false。
        /// </summary>
        public bool LogoutOnGameEnd { get; set; }

        /// <summary>
        /// ホスト名
        /// </summary>
        public string HostName { get; private set; }
        /// <summary>
        /// ポート番号
        /// </summary>
        public int PortNumber { get; private set; }

        /// <summary>
        /// 現在の状態
        /// </summary>
        public CSAState State { get; private set; }

        /// <summary>
        /// 前回の終局理由
        /// </summary>
        public GameEndReason LastGameEndReason { get; private set; }
        /// <summary>
        /// 前回の対局の結果
        /// </summary>
        public GameResult LastGameResult { get; private set; }

        /// <summary>
        /// プロトコルモード
        /// </summary>
        public enum ProtocolModes {
            /// <summary>
            /// 通常モード
            /// </summary>
            CSA,
            /// <summary>
            /// テストモード。
            /// </summary>
            /// <remarks>
            /// CSAのテスト用サーバ(gserver.computer-shogi.org)用。
            /// REJECT時や対局終了時にCHALLENGEを送る。
            /// </remarks>
            CSATest,
            /// <summary>
            /// shogi-serverの拡張モード
            /// </summary>
            /// <remarks>
            /// http://shogi-server.sourceforge.jp/protocol.html
            /// </remarks>
            ExMode,
        }
        /// <summary>
        /// 拡張モードならtrue
        /// </summary>
        public ProtocolModes ProtocolMode { get; private set; }

        /// <summary>
        /// ゲーム情報
        /// </summary>
        public CSAGameSummary GameSummary { get; private set; }
        /// <summary>
        /// ゲームID。例えばfloodgateだと「wdoor+floodgate-900-0+ShogiCore+gps_normal+20090929193004」とか。
        /// </summary>
        public string GameID { get; private set; }
        /// <summary>
        /// 開始局面のCSA棋譜文字列
        /// </summary>
        public string InitialPositionString { get; private set; }
        /// <summary>
        /// 開始局面
        /// </summary>
        public Notation.Notation InitialPosition { get; private set; }

        /// <summary>
        /// 最新局面(書き換えるとバグるので注意)
        /// </summary>
        public BoardData CurrentBoard { get; private set; }
        /// <summary>
        /// 思考開始時間
        /// </summary>
        int lastStartTime;
        /// <summary>
        /// 開始局面からの指し手履歴。参照・更新時はCurrentBoardでlock()すること。
        /// </summary>
        public List<MoveData> Moves { get; private set; }
        /// <summary>
        /// 開始局面からの指し手の時間[ミリ秒]のリスト。参照・更新時はCurrentBoardでlock()すること。
        /// </summary>
        public List<int> MilliSecondsList { get; private set; }
        /// <summary>
        /// 現在の先手の残り持ち時間[ミリ秒]
        /// </summary>
        public int FirstTurnRemainMilliSeconds { get { return playerTimes[0].Remain; } }
        /// <summary>
        /// 現在の後手の残り持ち時間[ミリ秒]
        /// </summary>
        public int SecondTurnRemainMilliSeconds { get { return playerTimes[1].Remain; } }

        PlayerTime[] playerTimes = new PlayerTime[2] { new PlayerTime(), new PlayerTime() };

        /// <summary>
        /// 終局
        /// </summary>
        public event EventHandler<CSAClientEventArgs> GameEnd;
        /// <summary>
        /// 通信切断
        /// </summary>
        public event EventHandler<CSAClientEventArgs> Disconnected;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="hostName">接続先（address:port形式も可。その場合portNumberは無視される。）</param>
        /// <param name="portNumber">接続先ポート番号</param>
        public CSAClient(string hostName, int portNumber = DefaultPort) {
            Match m = Regex.Match(hostName, @"^(.+):(\d+)$");
            if (m.Success) {
                hostName = m.Groups[1].Value;
                portNumber = int.Parse(m.Groups[2].Value);
            }
            HostName = hostName;
            PortNumber = portNumber;
            State = CSAState.TCPConnect;
            GameSummary = new CSAGameSummary();
            LastGameEndReason = GameEndReason.Interruption; // 最後まで行かなかったら中断扱い
            OpenConnection();
            // 受信スレッドの開始
            receiveThread = new Thread(ReceiveThread);
            receiveThread.Name = "CSAClient-Receive";
            receiveThread.IsBackground = true; // 念のため。
            receiveThread.Priority = ThreadPriority.AboveNormal; // ちょびっと頑張り目に。
            receiveThread.Start();
            // keep-aliveスレッドの開始
            keepAliveThread = new Thread(KeepAliveThread);
            keepAliveThread.Name = "CSAClient-KeepAlive";
            keepAliveThread.IsBackground = true; // 念のため
            keepAliveThread.Priority = ThreadPriority.AboveNormal; // ちょびっと頑張り目に。
            keepAliveThread.Start();
        }

        /// <summary>
        /// 接続
        /// </summary>
        private void OpenConnection() {
            client = new TcpClient();
            client.NoDelay = true;

            try {
                // TCPのKeepAlive。Windows限定な気がする。http://d.hatena.ne.jp/ak11/20101204
                List<byte> tcpKeepalive = new List<byte>(sizeof(uint) * 3); // struct tcp_keepalive
                tcpKeepalive.AddRange(BitConverter.GetBytes(1u)); // u_long onoff
                tcpKeepalive.AddRange(BitConverter.GetBytes(45000u)); // u_long keepalivetime
                tcpKeepalive.AddRange(BitConverter.GetBytes(1000u)); // u_long keepaliveinterval
                client.Client.IOControl(IOControlCode.KeepAliveValues, tcpKeepalive.ToArray(), null);
            } catch (Exception e) {
                logger.Info("TCPのKeepAlive設定に失敗。", e);
            }

            client.Connect(HostName, PortNumber);
            logger.Debug("接続成功: " + HostName + ":" + PortNumber.ToString());

            NetworkStream stream = client.GetStream();
            // ↓CSA的にはASCIIっぽいが、shogi-server拡張モードの%%CHATはUTF-8らしい。
            Encoding utf8 = new UTF8Encoding(false); // BOM付けない
            reader = new StreamReader(stream, utf8, false);
            writer = new StreamWriter(stream, utf8);
            writer.NewLine = "\n";

            State = CSAState.Login; // 未ログイン
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            Disconnect("Dispose");
            if (keepAliveThread.IsAlive) {
                keepAliveThread.Join();
            }
            if (receiveThread.IsAlive) {
                receiveThread.Join();
            }
        }

        /// <summary>
        /// 切断処理
        /// </summary>
        private void Disconnect(string reason) {
            lock (connectLock) {
                bool doLog = threadValid;
                if (threadValid) {
                    threadValid = false;
                    logger.Debug("ネットワーク切断開始: " + reason);
                }
                if (keepAliveThread.IsAlive) {
                    lock (keepAliveThread) {
                        Monitor.Pulse(keepAliveThread);
                    }
                }

                try {
                    try {
                        lock (writerLock) {
                            if (writer != null) {
                                writer.Close();
                                writer = null;
                            }
                        }
                        var reader = this.reader;
                        if (reader != null) {
                            this.reader = null;
                            reader.Close();
                        }
                    } catch (ObjectDisposedException) {
                    } catch (Exception e) {
                        logger.Warn("通信切断時に例外発生1", e);
                    } finally {
                        var client = this.client;
                        if (client != null) {
                            this.client = null;
                            client.Close();
                        }
                    }
                } catch (ObjectDisposedException) {
                } catch (Exception e) {
                    logger.Warn("通信切断時に例外発生2", e);
                } finally {
                    State = CSAState.Finished;
                }

                EnqueueCommand(CSAInternalCommandTypes.Disconnected);
                var Disconnected = this.Disconnected;
                if (Disconnected != null) {
                    Disconnected(this, new CSAClientEventArgs(this));
                }
                if (doLog) {
                    logger.Debug("ネットワーク切断終了");
                }
            }
        }

        /// <summary>
        /// メッセージ受信スレッド
        /// </summary>
        private void ReceiveThread() {
            try {
                InternalReceiveThread();
            } catch (Exception e) {
                logger.Error("メッセージ受信スレッドで例外発生", e);
            }
        }

        /// <summary>
        /// メッセージ受信スレッド
        /// </summary>
        private void InternalReceiveThread() {
            while (threadValid) {
                // 行の受信
                string line;
                try {
                    line = ReadLine();
                    if (line == null) {
                        if (client == null) {
                            continue; // 切断された
                        } else if (reader.EndOfStream) { // EOF
                            line = "LOGOUT:completed"; // 念のため擬似的にログアウト完了扱い
                        } else {
                            continue; // ？
                        }
                    }
                } catch (Exception e) {
                    logger.Warn("メッセージ受信時に例外発生", e);
                    line = "LOGOUT:completed"; // エラー時は擬似的にログアウト完了扱い (手抜き)
                }
                // 行の処理
                try {
                    InternalReceiveLine(line);
                } catch (Exception e) {
                    logger.Warn("メッセージ受信処理時に例外発生", e);
                }
            }
        }

        /// <summary>
        /// 行の処理
        /// </summary>
        /// <param name="line"></param>
        private void InternalReceiveLine(string line) {
            // 空なら戻る
            if (string.IsNullOrEmpty(line)) return;

            // Position受信処理
            if (positionData != null) {
                if (line == "END Position") {
                    beginEndStack.Remove("Position");
                    InitialPositionString = positionData.ToString();
                    InitialPosition = pclReader.Read(InitialPositionString).FirstOrDefault();
                    positionData = null;
                    // TODO: 持ち時間の算出
                } else {
                    positionData.AppendLine(line);
                }
                return;
            }

            int cologne, space;
            if (line[0] == '+' || line[0] == '-') {
                // 指し手の受信
                Match m = regexMove.Match(line);
                if (m.Success) {
                    var moveTurn = line[0] == '+' ? 0 : 1;
                    var time = playerTimes[CurrentBoard.Turn];
                    var csa = m.Groups[1].Value;
                    int ms;
                    if (m.Groups[3].Success) {
                        // 受信した思考時間をparse
                        ms = int.Parse(m.Groups[3].Value) * time.Unit;
                    } else {
                        // 思考時間を自前計算
                        ms = time.GetFixedTime(unchecked(Environment.TickCount - lastStartTime));
                    }
                    // 指し手を進めて、指し手と時間を記録
                    MoveData move;
                    lock (CurrentBoard) {
                        if (moveTurn == CurrentBoard.Turn) {
                            // 受信した指し手でcurrentBoardを進める
                            move = PCLNotationReader.ParseMoveWithDo(CurrentBoard, csa);
                            // 時間の算出を行う
                            Moves.Add(move);
                            MilliSecondsList.Add(ms);
                            time.Consume(ms);
                        } else {
                            // 既にSendMove側で手を進めていた場合、時間の補正のみ行う。
                            move = Moves.LastOrDefault();
                            if (m.Groups[3].Success) { // 時間を受信出来ていた場合
                                time.Remain -= ms - MilliSecondsList.LastOrDefault();
                                MilliSecondsList[MilliSecondsList.Count - 1] = ms;
                            }
                        }
                    }
                    // 通知
                    if (GameSummary.Your_Turn == line[0]) {
                        EnqueueCommand(CSAInternalCommandTypes.SelfMove, new MoveDataEx { MoveData = move, Time = ms }, line);
                    } else {
                        EnqueueCommand(CSAInternalCommandTypes.EnemyMove, new MoveDataEx { MoveData = move, Time = ms }, line);
                    }
                    lastStartTime = Environment.TickCount;
                } else {
                    logger.Warn("受信データの解析に失敗: " + line);
                }
            } else if (line[0] == '#') {
                switch (line) {
                    case "#SENNICHITE": LastGameEndReason = GameEndReason.Endless; break;
                    case "#OUTE_SENNICHITE": LastGameEndReason = GameEndReason.Perpetual; break;
                    case "#ILLEGAL_MOVE": LastGameEndReason = GameEndReason.IllegalMove; break;
                    case "#TIME_UP": LastGameEndReason = GameEndReason.TimeUp; break;
                    case "#RESIGN": LastGameEndReason = GameEndReason.Resign; break;
                    case "#JISHOGI": LastGameEndReason = GameEndReason.Nyuugyoku; break;
                    case "#CHUDAN": LastGameEndReason = GameEndReason.Abort; OnGameEnd(GameResult.Draw); break;
                    case "#WIN": OnGameEnd(GameResult.Win); break;
                    case "#LOSE": OnGameEnd(GameResult.Lose); break;
                    case "#DRAW": OnGameEnd(GameResult.Draw); break;
                    case "##[LOGIN] +OK x1":
                        Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
                        SetGameWaiting();
                        break;
                    default:
                        logger.Warn("受信データの解析に失敗: " + line);
                        break;
                }
            } else if (line[0] == '%') {
                string specialMoveCSA = line.Split(',').FirstOrDefault(); // 「%KACHI,T1」とか対策
                switch (specialMoveCSA) {
                    case "%TORYO": LastGameEndReason = GameEndReason.Resign; break;
                    case "%MATTA": LastGameEndReason = GameEndReason.Matta; break;
                    case "%CHUDAN": LastGameEndReason = GameEndReason.Interruption; break;
                    case "%SENNICHITE": LastGameEndReason = GameEndReason.Endless; break;
                    case "%JISHOGI": LastGameEndReason = GameEndReason.Jishogi; break;
                    case "%TSUMI": LastGameEndReason = GameEndReason.Mate; break;
                    case "%FUZUMI": LastGameEndReason = GameEndReason.NoMate; break;
                    case "%ERROR": LastGameEndReason = GameEndReason.Error; break;
                    case "%KACHI": LastGameEndReason = GameEndReason.Nyuugyoku; break;
                    case "%HIKIWAKE": LastGameEndReason = GameEndReason.NyuugyokuDraw; break;
                    default:
                        logger.Warn("受信データの解析に失敗: " + line);
                        break;
                }
                EnqueueCommand(CSAInternalCommandTypes.SpecialMove, default(MoveDataEx), line);
            } else if (0 <= (cologne = line.IndexOf(':'))) {
                switch (line.Substring(0, cologne)) {
                    case "LOGIN": // LOGIN:<username> OK, LOGIN:incorrect
                        if (line.EndsWith(" OK", StringComparison.Ordinal)) {
                            SetGameWaiting();
                        } else {
                            Debug.Assert(line == "LOGIN:incorrect");
                            State = CSAState.Login;
                            EnqueueCommand(CSAInternalCommandTypes.LoginFailed);
                        }
                        break;

                    case "LOGOUT": // LOGOUT:completed
                        Disconnect("Logout");
                        break;

                    case "START": // START:<GameID>
                        Debug.Assert(State == CSAState.AgreeWaiting);
                        GameID = line.Substring(cologne + 1);
                        OnStartReceived();
                        lastStartTime = Environment.TickCount;
                        break;

                    case "REJECT": // REJECT:<GameID> by <rejector>
                        SetGameWaiting();
                        break;

                    case "Protocol_Version": GameSummary.Protocol_Version = line.Substring(cologne + 1); break;
                    case "Protocol_Mode": GameSummary.Protocol_Mode = line.Substring(cologne + 1); break;
                    case "Format": GameSummary.Format = line.Substring(cologne + 1); break;
                    case "Declaration": GameSummary.Declaration = line.Substring(cologne + 1); break;
                    case "Game_ID": GameSummary.Game_ID = line.Substring(cologne + 1); break;
                    case "Name+": GameSummary.NameP = line.Substring(cologne + 1); break;
                    case "Name-": GameSummary.NameN = line.Substring(cologne + 1); break;
                    case "Your_Turn": GameSummary.Your_Turn = line[cologne + 1]; break;
                    case "Rematch_On_Draw": GameSummary.Rematch_On_Draw = line.Substring(cologne + 1) == "YES"; break;
                    case "To_Move": GameSummary.To_Move = line[cologne + 1]; break;

                    case "Time_Unit":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+": GameSummary.Times[0].Time_Unit = line.Substring(cologne + 1); break;
                            case "Time-": GameSummary.Times[1].Time_Unit = line.Substring(cologne + 1); break;
                            case "Time":
                                GameSummary.Times[0].Time_Unit = line.Substring(cologne + 1);
                                GameSummary.Times[1].Time_Unit = GameSummary.Times[0].Time_Unit;
                                break;
                            default: goto case "Time";
                        }
                        break;
                    case "Least_Time_Per_Move":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+": GameSummary.Times[0].Least_Time_Per_Move = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time-": GameSummary.Times[1].Least_Time_Per_Move = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time":
                                GameSummary.Times[0].Least_Time_Per_Move = int.Parse(line.Substring(cologne + 1));
                                GameSummary.Times[1].Least_Time_Per_Move = GameSummary.Times[0].Least_Time_Per_Move;
                                break;
                            default: goto case "Time";
                        }
                        break;
                    case "Time_Roundup":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+": GameSummary.Times[0].Time_Roundup = line.Substring(cologne + 1) == "YES"; break;
                            case "Time-": GameSummary.Times[1].Time_Roundup = line.Substring(cologne + 1) == "YES"; break;
                            case "Time":
                                GameSummary.Times[0].Time_Roundup = line.Substring(cologne + 1) == "YES";
                                GameSummary.Times[1].Time_Roundup = GameSummary.Times[0].Time_Roundup;
                                break;
                            default: goto case "Time";
                        }
                        break;
                    case "Total_Time":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+":
                                GameSummary.Times[0].Total_Time = int.Parse(line.Substring(cologne + 1));
                                break;
                            case "Time-":
                                GameSummary.Times[1].Total_Time = int.Parse(line.Substring(cologne + 1));
                                break;
                            case "Time":
                                GameSummary.Times[0].Total_Time = int.Parse(line.Substring(cologne + 1));
                                GameSummary.Times[1].Total_Time = GameSummary.Times[0].Total_Time;
                                break;
                            default: goto case "Time";
                        }
                        break;
                    case "Byoyomi":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+": GameSummary.Times[0].Byoyomi = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time-": GameSummary.Times[1].Byoyomi = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time":
                                GameSummary.Times[0].Byoyomi = int.Parse(line.Substring(cologne + 1));
                                GameSummary.Times[1].Byoyomi = GameSummary.Times[0].Byoyomi;
                                break;
                            default: goto case "Time";
                        }
                        break;
                    case "Delay":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+": GameSummary.Times[0].Delay = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time-": GameSummary.Times[1].Delay = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time":
                                GameSummary.Times[0].Delay = int.Parse(line.Substring(cologne + 1));
                                GameSummary.Times[1].Delay = GameSummary.Times[0].Delay;
                                break;
                            default: goto case "Time";
                        }
                        break;
                    case "Increment":
                        switch (beginEndStack.LastOrDefault()) {
                            case "Time+": GameSummary.Times[0].Increment = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time-": GameSummary.Times[1].Increment = int.Parse(line.Substring(cologne + 1)); break;
                            case "Time":
                                GameSummary.Times[0].Increment = int.Parse(line.Substring(cologne + 1));
                                GameSummary.Times[1].Increment = GameSummary.Times[0].Increment;
                                break;
                            default: goto case "Time";
                        }
                        break;

                    default:
                        logger.Warn("受信データの解析に失敗: " + line);
                        break;
                }
            } else if (0 <= (space = line.IndexOf(' '))) {
                switch (line.Substring(0, space)) {
                    case "BEGIN": {
                            string name = line.Substring(space + 1);
                            beginEndStack.Add(name);

                            switch (name) {
                                case "Game_Summary":
                                    if (beginEndStack.Count == 1) { // 念のため
                                        GameSummary = new CSAGameSummary();
                                        // 初期化
                                        positionData = null;
                                        InitialPosition = null;
                                        State = CSAState.GameReceiving;
                                    }
                                    break;

                                case "Position":
                                    if (beginEndStack.Count == 2 &&
                                        beginEndStack[0] == "Game_Summary") { // 念のため
                                        // 初期化
                                        positionData = new StringBuilder();
                                    }
                                    break;
                            }
                        }
                        break;
                    case "END": {
                            string name = line.Substring(space + 1);
                            beginEndStack.Remove(name);
                            // ↑微妙かもしれないが一応慎重に。。

                            // GameSummary受信終了通知
                            if (name == "Game_Summary") {
                                OnGameSummaryReceived();
                                State = CSAState.AgreeWaiting;
                                EnqueueCommand(CSAInternalCommandTypes.GameSummaryReceived);
                            } else if (name.StartsWith("Time", StringComparison.Ordinal)) {
                                playerTimes[0] = new PlayerTime(GameSummary.Times[0]);
                                playerTimes[1] = new PlayerTime(GameSummary.Times[1]);
                            }
                        }
                        break;

                    case "CHALLENGE":
                        if (line == "CHALLENGE ACCEPTED") {
                            State = CSAState.GameWaiting;
                        } else {
                            goto default;
                        }
                        break;

                    default:
                        logger.Warn("受信データの解析に失敗: " + line);
                        break;
                }
            } else {
                switch (line) {
                    case "START":
                        Debug.Assert(State == CSAState.AgreeWaiting);
                        OnStartReceived();
                        break;

                    case "REJECT":
                        SetGameWaiting();
                        break;

                    default:
                        logger.Warn("受信データの解析に失敗: " + line);
                        break;
                }
            }
            return;
        }

        /// <summary>
        /// Game Summary受信時の処理
        /// </summary>
        private void OnGameSummaryReceived() {
            Moves = new List<MoveData>();
            MilliSecondsList = new List<int>();
            // 初期化
            if (InitialPosition != null) {
                // 初期局面
                if (InitialPosition.InitialBoard != null) {
                    CurrentBoard = InitialPosition.InitialBoard.Clone();
                } else {
                    CurrentBoard = new BoardData();
                }
                // 指し手
                lock (CurrentBoard) {
                    foreach (MoveDataEx move in InitialPosition.Moves) {
                        playerTimes[CurrentBoard.Turn].Consume(move.Time * playerTimes[CurrentBoard.Turn].Unit);
                        Moves.Add(move.MoveData);
                        MilliSecondsList.Add(move.Time);
                        CurrentBoard.Do(move.MoveData);
                    }
                }
            } else {
                CurrentBoard = new BoardData();
            }
        }

        /// <summary>
        /// START受信時の処理
        /// </summary>
        private void OnStartReceived() {
            State = CSAState.Game;
            EnqueueCommand(CSAInternalCommandTypes.Start);
            // KeepAliveスレッドを起こす
            lock (keepAliveThread) {
                Monitor.PulseAll(keepAliveThread);
            }
        }

        /// <summary>
        /// 終局時の処理
        /// </summary>
        private void OnGameEnd(GameResult gameResult) {
            LastGameResult = gameResult;
            var GameEnd = this.GameEnd;
            if (GameEnd != null) {
                GameEnd(this, new CSAClientEventArgs(this));
            }
            SetGameWaiting();
            // ログアウト
            if (LogoutOnGameEnd) {
                SendLogout();
            }
        }

        /// <summary>
        /// コマンドをキューへ投入
        /// </summary>
        private void EnqueueCommand(CSAInternalCommandTypes commandType, MoveDataEx moveDataEx = default(MoveDataEx), string receivedString = null) {
            lock (commandQueue) {
                commandQueue.Enqueue(new CSAInternalCommand { CommandType = commandType, MoveDataEx = moveDataEx, ReceivedString = receivedString });
                Monitor.PulseAll(commandQueue);
            }
        }

        /// <summary>
        /// コマンドを受信
        /// </summary>
        /// <returns>受信したコマンド</returns>
        public CSAInternalCommand ReceiveCommand() {
            lock (commandQueue) {
                while (true) {
                    if (commandQueue.Count <= 0) {
                        if (!threadValid) {
                            return new CSAInternalCommand { CommandType = CSAInternalCommandTypes.Disconnected };
                        }
                        Monitor.Wait(commandQueue);
                        continue;
                    }
                    return commandQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// 対局待ちっぽい状態に遷移する
        /// </summary>
        private void SetGameWaiting() {
            switch (ProtocolMode) {
                case ProtocolModes.CSA:
                    State = CSAState.GameWaiting;
                    break;

                case ProtocolModes.CSATest:
                    State = CSAState.Connected;
                    EnqueueCommand(CSAInternalCommandTypes.TestConnected);
                    break;

                case ProtocolModes.ExMode:
                    State = CSAState.Connected;
                    EnqueueCommand(CSAInternalCommandTypes.ExConnected);
                    break;

                default:
                    throw new InvalidOperationException("ProtocolModeが不正: " + ProtocolMode);
            }
        }

        /// <summary>
        /// Keep-Aliveスレッド
        /// </summary>
        private void KeepAliveThread() {
            try {
                InternalKeepAliveThread();
            } catch (Exception e) {
                logger.Error("Keep-Aliveスレッドで例外発生", e);
            }
        }

        /// <summary>
        /// Keep-Aliveスレッド
        /// </summary>
        private void InternalKeepAliveThread() {
            while (threadValid) {
                // http://www.computer-shogi.org/protocol/tcp_ip_server_113.html
                // 　またクライアントは対局中、手番にかかわらず、長さゼロの文字列、
                // もしくはLF1文字のみをサーバに送信することができる。 サーバは前
                // 者を受け取った場合、単純に無視する。後者を受け取った場合、短い
                // 待ち時間の後にLF1文字のみをそのクライアントに返信する。クライア
                // ントは、これらの送信を頻繁に行ってはならない。 具体的には、当該
                // クライアントからの何らかの文字列をサーバが受信してから30秒を経
                // ずして同一のクライアントからこれらの送信を行ってはならない。ク
                // ライアントがこの規定に反した場合、サーバは当該クライアントを反
                // 則負けとして扱うことができる。
                // http://wdoor.c.u-tokyo.ac.jp/shogi/floodgate.html
                // keep alive: 試合がない状態が続いても通信が切れない対策候補
                //     * 空白+改行をおくる (サーバは無視する) (注意: 拡張仕様, 選手権では使用不可)
                //     * 改行のみ送る (サーバは改行を返す) (対局中など条件付きでCSA仕様) 
                // http://cgi3.tky.3web.ne.jp/~kayaken/csabbs/mibbs.cgi?mo=p&fo=open&tn=0050&rn=50
                // 51: 名前：山田剛＠CSA対局サーバ担当投稿日：2009/04/05(日) 14:59
                // >>48
                // 実は改行のみの送信を対局サーバが無視しなければならないのですが、
                // CSAの対局サーバは対局中の改行に対応できていません。
                // すみませんがよろしくお願いいたします。
                if (KeepAlive) {
                    // keep-alive
                    int now = Environment.TickCount;
                    int wait = 45 * 1000 - (now - lastTCPTick);
                    if (wait <= 0) {
                        // keep-alive送信
                        WriteLineWithFlush("");
                    } else {
                        // 待つ
                        lock (keepAliveThread) {
                            Monitor.Wait(keepAliveThread, wait);
                        }
                    }
                } else {
                    // とりあえず待機。
                    lock (keepAliveThread) {
                        Monitor.Wait(keepAliveThread);
                    }
                }
            }
        }

        /// <summary>
        /// ログイン
        /// </summary>
        /// <param name="user">ユーザー名</param>
        /// <param name="pass">パスワード</param>
        /// <param name="mode">動作モード</param>
        public void SendLogin(string user, string pass, ProtocolModes mode) {
            ProtocolMode = mode;
            if (ProtocolMode == ProtocolModes.ExMode) {
                WriteLineWithFlush("LOGIN " + user + " " + pass + " x1");
            } else {
                WriteLineWithFlush("LOGIN " + user + " " + pass);
            }
            // 一応KeepAliveThreadを起こす
            lock (keepAliveThread) {
                Monitor.PulseAll(keepAliveThread);
            }
        }

        /// <summary>
        /// ログアウト
        /// </summary>
        public void SendLogout() {
            WriteLineWithFlush("LOGOUT");
        }

        /// <summary>
        /// GameSummary受信後、対局を受ける場合。
        /// </summary>
        public void SendAgree() {
            Debug.Assert(State == CSAState.AgreeWaiting);
            WriteLineWithFlush("AGREE"); // <GameID>を指定出来るが面倒なので非対応
        }

        /// <summary>
        /// GameSummary受信後、対局を蹴る場合。
        /// </summary>
        public void SendReject() {
            Debug.Assert(State == CSAState.AgreeWaiting);
            WriteLineWithFlush("REJECT"); // <GameID>を指定出来るが面倒なので非対応
        }

        /// <summary>
        /// 指し手の送信
        /// </summary>
        /// <param name="move">指し手</param>
        /// <param name="comment">コメント。"jouseki"など。</param>
        public void SendMove(MoveData move, string comment = "") {
            Debug.Assert(State == CSAState.Game);
            string moveDataString;
            lock (CurrentBoard) {
                moveDataString = PCLNotationWriter.ToString(CurrentBoard, move);
                // 思考時間をいったん自前算出
                var time = playerTimes[CurrentBoard.Turn];
                var ms = time.GetFixedTime(unchecked(Environment.TickCount - lastStartTime));
                time.Consume(ms);
                // 指し手と思考時間を記録し、currentBoardを進める
                Moves.Add(move);
                MilliSecondsList.Add(ms);
                CurrentBoard.Do(move);
            }
            // 指し手の送信
            if (SendPV && !string.IsNullOrEmpty(comment)) {
                WriteLineWithFlush(moveDataString + ",'" + comment);
            } else {
                WriteLineWithFlush(moveDataString);
            }
        }

        /// <summary>
        /// 投了
        /// </summary>
        /// <param name="comment">コメント</param>
        public void SendToryo(string comment = "") {
            Debug.Assert(State == CSAState.Game);
            if (SendPV && !string.IsNullOrEmpty(comment)) {
                WriteLineWithFlush("%TORYO,'" + comment);
            } else {
                WriteLineWithFlush("%TORYO");
            }
        }

        /// <summary>
        /// 入玉勝ち宣言
        /// </summary>
        public void SendKachi() {
            Debug.Assert(State == CSAState.Game);
            WriteLineWithFlush("%KACHI");
        }

        /// <summary>
        /// 中断
        /// </summary>
        public void SendChudan() {
            Debug.Assert(State == CSAState.Game);
            WriteLineWithFlush("%CHUDAN");
        }

        /// <summary>
        /// テスト用サーバなどで対局待ち状態へ遷移するためのコマンド
        /// </summary>
        public void SendChallenge() {
            Debug.Assert(ProtocolMode == ProtocolModes.CSATest);
            Debug.Assert(State == CSAState.Connected);
            State = CSAState.GameWaiting;
            WriteLineWithFlush("CHALLENGE");
        }

        /// <summary>
        /// クライアントの一覧 (拡張モード用)
        /// </summary>
        public void SendExWho() {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%WHO");
        }

        /// <summary>
        /// 拡張モードのクライアント全てに送信 (拡張モード用)
        /// </summary>
        public void SendExChat(string msg) {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%CHAT " + msg);
        }

        /// <summary>
        /// 拡張モードの「##[LOGIN] +OK x1」に対する応答
        /// </summary>
        /// <param name="gameName">ゲーム名</param>
        /// <param name="turn">手番。+で先手、-で後手、*で相手の希望に合わせる</param>
        public void SendExGame(string gameName = "floodgate-900-0", string turn = "*") {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            Debug.Assert(State == CSAState.Connected);
            State = CSAState.GameWaiting;
            WriteLineWithFlush("%%GAME " + gameName + " " + turn);
        }

        /// <summary>
        /// 拡張モードの「##[LOGIN] +OK x1」に対する応答 (エラー表示あり)
        /// </summary>
        /// <param name="gameName">ゲーム名</param>
        /// <param name="turn">手番。+で先手、-で後手、*で相手の希望に合わせる</param>
        public void SendExChallenge(string gameName = "floodgate-900-0", string turn = "*") {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            Debug.Assert(State == CSAState.Connected);
            State = CSAState.GameWaiting;
            WriteLineWithFlush("%%CHALLENGE " + gameName + " " + turn);
        }

        /// <summary>
        /// game_idの一覧 (拡張モード用)
        /// </summary>
        public void SendExList() {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%LIST");
        }

        /// <summary>
        /// 盤面表示 (拡張モード用)
        /// </summary>
        public void SendExShow(string gameId) {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%SHOW " + gameId);
        }

        /// <summary>
        /// 指定した対局の観察を継続的に行う (拡張モード用)
        /// </summary>
        public void SendExMonitorOn(string gameId) {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%MONITORON " + gameId);
        }

        /// <summary>
        /// 指定した対局の観察を解除する (拡張モード用)
        /// </summary>
        public void SendExMonitorOff(string gameId) {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%MONITOROFF " + gameId);
        }

        /// <summary>
        /// サーバが管理するプレイヤーのレーティング一覧を表示 (拡張モード用)
        /// </summary>
        public void SendExRating() {
            Debug.Assert(ProtocolMode == ProtocolModes.ExMode);
            WriteLineWithFlush("%%RATING");
        }

        /// <summary>
        /// 1行の送信
        /// </summary>
        private void WriteLineWithFlush(string line) {
            Debug.Assert(line == line.Trim());
            try {
                DoWriteLineRetry(line, 5);
                lastTCPTick = Environment.TickCount;
                if (csaLogger.IsDebugEnabled) {
                    csaLogger.Debug("> " + line);
                }
            } catch (Exception e) {
                // 何かしらエラったら切断
                logger.Warn("送信エラー: " + line, e);
                Disconnect("送信エラー");
            }
        }

        /// <summary>
        /// 1行の受信
        /// </summary>
        private string ReadLine() {
            try {
                string line = DoReadLineRetry(5);
                lastTCPTick = Environment.TickCount;
                if (csaLogger.IsDebugEnabled) {
                    csaLogger.Debug("< " + line);
                }
                return line;
            } catch (Exception e) {
                // 何かしらエラったら切断
                logger.Warn("受信エラー", e);
                Disconnect("受信エラー");
                return null;
            }
        }

        private void DoWriteLineRetry(string line, int retryCount) {
            try {
                lock (writerLock) { // 一応排他。
                    if (writer == null) {
                        logger.Info("切断済みのため未送信: " + line);
                        return;
                    }
                    writer.WriteLine(line);
                    writer.Flush();
                }
            } catch (Exception e) {
                // 切断対策
                if (IsDisconnected()) {
                    logger.Info("切断済みのため未送信: " + line);
                    return;
                }
                // リトライ回数が尽きていたら例外をそのまま呼び元へ
                if (retryCount <= 0) throw;
                // リトライ(再帰)
                logger.Debug("送信エラー発生のためリトライ: " + e.Message);
                Thread.Sleep(1);
                DoWriteLineRetry(line, retryCount - 1);
            }
        }

        private string DoReadLineRetry(int retryCount) {
            try {
                var reader = this.reader;
                if (reader == null) return null;
                return reader.ReadLine(); // ←一応CRLFだけじゃなくLFも対応してたはず。
            } catch (Exception e) {
                // 切断対策
                if (IsDisconnected()) return null;
                // リトライ回数が尽きていたら例外をそのまま呼び元へ
                if (retryCount <= 0) throw;
                // リトライ(再帰)
                logger.Debug("受信エラー発生のためリトライ: " + e.Message);
                Thread.Sleep(1);
                return DoReadLineRetry(retryCount - 1);
            }
        }

        /// <summary>
        /// 切断済みかどうか判定など
        /// </summary>
        private bool IsDisconnected() {
            try {
                var client = this.client;
                if (client == null) return true;
                if (!client.Connected) {
                    Disconnect("!Connected");
                    return true;
                }
                return false;
            } catch (ObjectDisposedException) {
                Disconnect("ObjectDisposedException");
                return true;
            } catch {
                return false;
            }
        }
    }
}
