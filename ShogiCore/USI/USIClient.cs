using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ShogiCore.Notation;
using ShogiCore.Threading;

namespace ShogiCore.USI {
    /// <summary>
    /// USIプロトコルのクライアント。
    /// </summary>
    /// <remarks>
    /// http://www.geocities.jp/shogidokoro/usi.html
    /// http://gps.tanaka.ecc.u-tokyo.ac.jp/gpsshogi/index.php?%BB%C8%A4%A4%CA%FD%2F%CA%AC%BB%B6%C3%B5%BA%F7%28usi.pl%29
    /// </remarks>
    public class USIClient : IDisposable {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly log4net.ILog usiLogger = log4net.LogManager.GetLogger("USILogger");

        TextReader input;
        TextWriter output;

        StringBuilder infoString = null;

        Thread thread = null;
        volatile bool threadValid = true;

        Queue<USICommand> commandQueue = new Queue<USICommand>();

        /// <summary>
        /// goを受信してからbestmoveを送信するまでの間trueなフラグ
        /// </summary>
        volatile bool going = false;
        object goingLock = new object();

#if !DANGEROUS
        int stopReceivedTick;
#endif

        /// <summary>
        /// gameoverコマンドを受信したならtrue。usinewgameコマンドでfalse。
        /// </summary>
        public bool GameOverReceived { get; private set; }
        /// <summary>
        /// gameover, stopコマンドを受信したならtrue。
        /// </summary>
        public bool ToBeStop { get; private set; }
        /// <summary>
        /// setoptionコマンドを1つ以上受信したならtrue。
        /// </summary>
        public bool ToBeSaveOption { get; private set; }

        /// <summary>
        /// オプションの取得
        /// </summary>
        public USIOptions Options { get; private set; }
        /// <summary>
        /// オプションのデフォルト値
        /// </summary>
        public USIOptions OptionDefaults { get; private set; }

        #region 各種イベント

        /// <summary>
        /// 例外発生
        /// </summary>
        public event ThreadExceptionEventHandler UnhandledException;

        /// <summary>
        /// stopコマンドを受信。別スレッドから呼ばれる事に注意。
        /// </summary>
        public event EventHandler StopReceived;
        /// <summary>
        /// quitコマンドを受信。別スレッドから呼ばれる事に注意。
        /// </summary>
        public event EventHandler QuitReceived;

        #endregion

        /// <summary>
        /// Quietモード（info stringやDebugWriteLineを出力しない）にするならtrue
        /// </summary>
        public bool QuietMode { get; set; }

        /// <summary>
        /// 初期化
        /// </summary>
        public USIClient()
            : this(Console.In, Console.Out) { }

        /// <summary>
        /// 初期化
        /// </summary>
        public USIClient(TextReader input, TextWriter output) {
            Options = new USIOptions();
            OptionDefaults = new USIOptions();
            this.input = input;
            this.output = output;

            // C/C++では↓だが、.NETでは…？
            //setvbuf(stdin, NULL, _IONBF, 0);
            //setvbuf(stdout, NULL, _IONBF, 0); // これが必要（標準出力のバッファリングを無効にする）
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            threadValid = false;
            if (output != Console.Out) { // ←適当(´ω`)
                output.Dispose();
            }
            if (input != Console.In) { // ←適当(´ω`)
                input.Dispose();
            }
            ThreadUtility.SafeJoin(thread, 3000);
        }

        /// <summary>
        /// オプションのデータの読み込み。
        /// コンストラクタの直後にでも呼ぶべし。ファイルが存在しなければ無視される。
        /// </summary>
        public void LoadOptions(string fileName) {
            Options = USIOptions.Deserialize(fileName);
        }

        /// <summary>
        /// オプションのデータの保存。
        /// 対局開始前とかに、client.ToBeSaveOptionがtrueなら呼ぶとか。
        /// </summary>
        public void SaveOptions(string fileName) {
            Options.Serialize(fileName);
        }

        /// <summary>
        /// 受信処理の開始
        /// </summary>
        public void Start() {
            thread = new Thread(ThreadProc);
            thread.Name = "USIClient";
            thread.IsBackground = true; // 念のため。
            thread.Priority = ThreadPriority.AboveNormal; // ちょびっと頑張り目に。
            thread.Start();
        }

        /// <summary>
        /// メッセージ受信スレッド
        /// </summary>
        private void ThreadProc() {
            try {
                InnerThreadProc();
            } catch (Exception e) {
                var UnhandledException = this.UnhandledException;
                if (UnhandledException != null) {
                    UnhandledException(this, new ThreadExceptionEventArgs(e));
                }
            }
        }

        /// <summary>
        /// メッセージ受信スレッド
        /// </summary>
        private void InnerThreadProc() {
            while (threadValid) {
                string line;
                try {
                    line = ReadLine();
                    if (line == null) {
                        if (input.Peek() < 0) { // EOF
                            line = "quit"; // 念のため擬似的にquitコマンド扱い
                        } else {
                            continue; // ？
                        }
                    }
                } catch (Exception e) {
                    var UnhandledException = this.UnhandledException;
                    if (UnhandledException != null) {
                        UnhandledException(this, new ThreadExceptionEventArgs(e));
                    }

                    line = "quit"; // エラー時は擬似的にquitコマンド扱いしてみる (手抜き)
                }

                USICommand command = USICommand.Parse(line);
                switch (command.Name) {
                    case "usi":
                        // 初期化
                        ToBeStop = false;
                        lock (goingLock) going = false;
                        break;

                    case "setoption": {
                            // オプションの受信
                            Match m = Regex.Match(line, "setoption name (.+?) value (.*)");
                            if (m.Success) {
                                string name = m.Groups[1].Value;
                                Options[name] = m.Groups[2].Value;
                                if (!name.StartsWith("USI_", StringComparison.Ordinal)) { // "USI_"で始まるのは無視。
                                    ToBeSaveOption = true;
                                }
                            }
                        } break;

                    case "usinewgame":
                        GameOverReceived = false;
                        // 念のため
                        ToBeStop = false;
                        lock (goingLock) going = false;
                        break;

                    case "go":
                        lock (goingLock) {
                            // 止まってないのにgoが来た？
                            if (going) {
                                logger.Info("goコマンド受信によるstop");
                                // 終了
                                ToBeStop = true;
                                // イベントの発行
                                EventHandler StopReceived = this.StopReceived;
                                if (StopReceived != null) {
                                    StopReceived(this, EventArgs.Empty);
                                }
                            }
                            going = true;
                        }
                        break;

                    case "stop":
                        lock (goingLock) {
                            if (going) {
                                // 終了
#if !DANGEROUS
                                stopReceivedTick = Environment.TickCount;
#endif
                                ToBeStop = true;
                                // イベントの発行
                                EventHandler StopReceived = this.StopReceived;
                                if (StopReceived != null) {
                                    StopReceived(this, EventArgs.Empty);
                                }
                            } else {
                                logger.Info("go未受信によりstopコマンドを無視");
                            }
                        }
                        break;

                    case "gameover": // ゲーム終了
                        GameOverReceived = true;
                        ToBeStop = true;
                        break;

                    case "quit": {
                            // イベントの発行
                            EventHandler QuitReceived = this.QuitReceived;
                            if (QuitReceived != null) {
                                QuitReceived(this, EventArgs.Empty);
                            }
                            // 強制終了
                            threadValid = false;
                        } break;

                    case "echo":
                        WriteLine(command.Parameters);
                        Flush();
                        break;
                }

                // キューへ追加
                lock (commandQueue) {
                    commandQueue.Enqueue(command);
                    Monitor.Pulse(commandQueue);
                }
            }
        }

        /// <summary>
        /// コマンドの受信
        /// </summary>
        public USICommand ReceiveCommand() {
            Debug.Assert(thread != null, "USIClient.Start()呼んでない");
            if (thread == null) {
                throw new InvalidOperationException("通信スレッドが開始されていません");
            }

            lock (commandQueue) {
                while (commandQueue.Count <= 0) {
                    if (!threadValid) { // 念のため。
                        return USICommand.Create("quit");
                    }
                    Monitor.Wait(commandQueue);
                }
                return commandQueue.Dequeue();
            }
        }

        /// <summary>
        /// id nameとid authorを送信
        /// </summary>
        /// <param name="name">プログラム名</param>
        /// <param name="author">プログラム作者</param>
        public void SendIdNameAuthor(string name, string author) {
            WriteLine("id name " + name);
            WriteLine("id author " + author);
            Flush();
        }

        /// <summary>
        /// オプション。
        /// Option("BookFile", "string", "public.bin");
        /// Option("UseBook", "check", "true");
        /// とか。
        /// </summary>
        /// <remarks>
        /// check
        ///     チェックボックスを表示します。
        /// button
        ///     ボタンを表示します。このボタンを押すと、オプション名で指定された
        ///     コマンドがエンジンに送られます。（オプション名がoptionnameなら、
        ///     このボタンを押すと、setoption name optionnameというコマンドが送られます。）
        /// string
        ///     文字列を入力できるコントロールを表示します。
        /// filename
        ///     ファイル名を入力できるコントロールを表示します。ファイル名を
        ///     指定しやすいよう、ファイル選択ボタンを表示し、それを押すと、
        ///     ファイル選択のダイアログを表示するようになっています。ここで
        ///     選択したファイルの絶対パスが設定値となります。
        /// </remarks>
        /// <param name="name">名前</param>
        /// <param name="type">タイプ</param>
        /// <param name="defaultValue">デフォルト値</param>
        public void SendOption(string name, string type, string defaultValue) {
            OptionDefaults[name] = defaultValue;
            string def;
            if (!Options.TryGetValue(name, out def)) {
                Options.Add(name, def = defaultValue);
            }
            WriteLine("option name {0} type {1} default {2}", name, type, def);
            Flush();
        }
        /// <summary>
        /// チェックボックス
        /// </summary>
        public void SendOptionCheck(string name, bool defaultValue) {
            SendOption(name, "check", defaultValue ? "true" : "false");
        }
        /// <summary>
        /// ボタン
        /// </summary>
        public void SendOptionButton(string name) {
            WriteLine("option name {0} type button", name);
            Flush();
        }
        /// <summary>
        /// ファイル名
        /// </summary>
        public void SendOptionFileName(string name, string defaultValue) {
            SendOption(name, "filename", defaultValue);
        }
        /// <summary>
        /// スピンコントロール。
        /// </summary>
        public void SendOptionSpin(string name, string defaultValue, int min, int max) {
            OptionDefaults[name] = defaultValue;
            string def;
            if (!Options.TryGetValue(name, out def)) {
                Options.Add(name, def = defaultValue);
            }
            WriteLine("option name {0} type spin default {1} min {2} max {3}",
                name, Math.Min(Math.Max(int.Parse(def), min), max).ToString(), min, max);
            Flush();
        }
        /// <summary>
        /// ドロップダウンリスト。
        /// </summary>
        public void SendOptionCombo(string name, string defaultValue, params string[] vars) {
            OptionDefaults[name] = defaultValue;
            string def;
            if (!Options.TryGetValue(name, out def)) {
                Options.Add(name, def = defaultValue);
            }
            if (Array.IndexOf(vars, def) < 0) def = defaultValue; // 念のため
            WriteLine("option name {0} type combo default {1} var {2}",
                name, def, string.Join(" var ", vars));
            Flush();
        }

        /// <summary>
        /// オプションの終了
        /// </summary>
        public void SendUSIOK() {
            Send("usiok");
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        public void SendReadyOK() {
            Send("readyok");
        }

        /// <summary>
        /// genmove_probability 指し手を実現確率とともに生成する。現在は確率としては使われず、指し手の探索すべき順序の予想のみに用いられるので、評価値順等適当で良い。 
        /// </summary>
        public void SendGenMoveProbability(IEnumerable<KeyValuePair<MoveData, int>> moves) {
            StringBuilder str = new StringBuilder();
            str.Append("genmove_probability");
            foreach (KeyValuePair<MoveData, int> move in moves) {
                str.Append(' ');
                str.Append(SFENNotationWriter.ToString(move.Key));
                str.Append(' ');
                str.Append(move.Value);
            }
            Send(str.ToString());
        }

        #region bestmoveコマンド(指し手の送信)

        /// <summary>
        /// 手の送信
        /// </summary>
        public void SendBestMove(MoveData move) {
            InternalSendBestMove(SFENNotationWriter.ToString(move));
        }

        /// <summary>
        /// 手の送信
        /// </summary>
        public void SendBestMove(MoveData move, MoveData ponder) {
            if (Options["USI_Ponder"] == "true") {
                InternalSendBestMove(SFENNotationWriter.ToString(move) +
                    " ponder " + SFENNotationWriter.ToString(ponder));
            } else {
                SendBestMove(move);
            }
        }

        /// <summary>
        /// 勝ち宣言
        /// </summary>
        public void SendBestMoveWin() { InternalSendBestMove("win"); }

        /// <summary>
        /// 投了
        /// </summary>
        public void SendBestMoveResign() { InternalSendBestMove("resign"); }

        /// <summary>
        /// パス
        /// </summary>
        public void SendBestMovePass() { InternalSendBestMove("pass"); }

        /// <summary>
        /// 手の送信処理
        /// </summary>
        /// <param name="move"></param>
        private void InternalSendBestMove(string move) {
            lock (goingLock) {
                Send("bestmove " + move);
                going = false;
                if (ToBeStop) {
                    ToBeStop = false;
#if !DANGEROUS
                    int time = Environment.TickCount - stopReceivedTick;
                    if (20 < time) {
                        logger.Info("stopコマンドによる停止時間: " + time.ToString("#,##0") + "ms");
                    }
#endif
                }
            }
        }

        #endregion

        #region infoコマンド(探索中の情報表示)

        /// <summary>
        /// infoコマンド開始
        /// </summary>
        /// <remarks>
        /// エンジンは、goコマンドで思考を開始してからbestmoveコマンドで
        /// 指し手を返すまでの間、infoコマンドによって思考中の情報を返す
        /// ことができます。
        /// </remarks>
        public void BeginInfo() {
            infoString = new StringBuilder();
            infoString.Append("info");
        }

        /// <summary>
        /// infoコマンド終了
        /// </summary>
        public void EndInfo() {
            if (4 < infoString.Length) { // "info".Length
                WriteLine(infoString.ToString());
                Flush();
            }
            infoString = null;
        }

        /// <summary>
        /// 現在思考中の手の（基本の）探索深さを返します。
        /// </summary>
        /// <param name="depth">探索深さ</param>
        public void InfoDepth(int depth) {
            infoString.Append(" depth ");
            infoString.Append(depth);
        }

        /// <summary>
        /// 現在、選択的に読んでいる手の探索深さを返します。
        /// </summary>
        /// <remarks>
        /// seldepthを使うときは、必ずその前でdepthを使って
        /// 基本深さを示す必要があります。
        /// </remarks>
        /// <param name="depth">探索深さ</param>
        public void InfoSelDepth(int depth) {
            infoString.Append(" seldepth ");
            infoString.Append(depth);
        }

        /// <summary>
        /// 思考を開始してから経過した時間を返します（単位はミリ秒）。これはpvと一緒に返す必要があります。
        /// </summary>
        /// <param name="time">時間[ms]</param>
        public void InfoTime(int time) {
            infoString.Append(" time ");
            infoString.Append(time);
        }

        /// <summary>
        /// 思考開始から探索したノード数を返します。これは定期的に返す必要があります。
        /// </summary>
        /// <param name="nodes">ノード数</param>
        public void InfoNodes(long nodes) {
            infoString.Append(" nodes ");
            infoString.Append(nodes);
        }

        /// <summary>
        /// １秒あたりの探索局面数を返します。これは定期的に返す必要があります。
        /// </summary>
        /// <param name="nps">探索局面数</param>
        public void InfoNPS(int nps) {
            infoString.Append(" nps ");
            infoString.Append(nps);
        }

        /// <summary>
        /// InfoTime(), InfoNodes(), InfoNPS()をまとめて送信。
        /// </summary>
        /// <param name="time">時間[ms]</param>
        /// <param name="nodes">ノード数</param>
        public void InfoTimeNodes(int time, long nodes) {
            infoString.Append(" time ");
            infoString.Append(time);
            infoString.Append(" nodes ");
            infoString.Append(nodes);
            if (0 < time) {
                infoString.Append(" nps ");
                infoString.Append(nodes * 1000 / time);
            }
        }

        /// <summary>
        /// 現在の読み筋を返します。
        /// なお、pvを使う場合、infoのあとに書くサブコマンドの中で最後に書くようにして下さい。
        /// stringとの同時使用はできません。
        /// </summary>
        /// <param name="moves">読み筋</param>
        public void InfoPV(params MoveData[] moves) {
            if (moves.Length <= 0) return;
            infoString.Append(" pv ");
            infoString.Append(string.Join(" ",
                Array.ConvertAll(moves, SFENNotationWriter.ToString)));
        }

        /// <summary>
        /// エンジンによる現在の評価値を返します
        /// </summary>
        /// <param name="score">歩１枚の価値を100とした値</param>
        public void InfoScoreCP(int score) {
            infoString.Append(" score cp ");
            infoString.Append(score);
        }

        /// <summary>
        /// 詰みを発見した
        /// </summary>
        /// <param name="win">勝ちならtrue、負けならfalse</param>
        public void InfoScoreMate(bool win) {
            infoString.Append(" score mate ");
            infoString.Append(win ? "+" : "-");
        }

        /// <summary>
        /// 詰みを発見した
        /// </summary>
        /// <param name="mate">詰み手数。エンジンの勝ちならプラス、エンジンの負けならマイナス</param>
        public void InfoScoreMate(int mate) {
            infoString.Append(" score mate ");
            infoString.Append(mate);
        }

        /// <summary>
        /// 現在思考中の手を返します。（思考開始局面から最初に指す手です。）
        /// </summary>
        /// <remarks>
        /// 空または特殊手は出力しない
        /// </remarks>
        /// <param name="move">指し手</param>
        public void InfoCurrentMove(MoveData move) {
            if (!move.IsEmpty && !move.IsSpecialMove) {
                infoString.Append(" currmove ");
                infoString.Append(SFENNotationWriter.ToString(move));
            }
        }

        /// <summary>
        /// エンジンが現在使用しているハッシュの使用率を返します。
        /// このコマンドは定期的に返す必要があります。
        /// </summary>
        /// <param name="fullPerMill">ハッシュの使用率(単位はパーミル、全体を１０００とした値）</param>
        public void InfoHashFull(int fullPerMill) {
            infoString.Append(" hashfull ");
            infoString.Append(Math.Min(Math.Max(fullPerMill, 0), 1000));
        }

        /// <summary>
        /// GUIに表示させたい任意の文字列を返します。
        /// pvと同時使用はできません。
        /// </summary>
        /// <param name="str">GUIに表示させたい任意の文字列</param>
        public void InfoString(string str) {
            if (QuietMode) {
                logger.Debug("info string: " + str);
            } else {
                infoString.Append(" string ");
                infoString.Append(str);
            }
        }

        /// <summary>
        /// 未対応のサブコマンド。
        /// </summary>
        /// <param name="name">項目名</param>
        /// <param name="value">値</param>
        public void Info(string name, string value) {
            infoString.Append(" " + name + " ");
            infoString.Append(value);
        }

        #endregion

        /// <summary>
        /// BeginInfo / EndInfoを用いずにinfo string
        /// </summary>
        /// <param name="str">GUIに表示させたい任意の文字列</param>
        public void InfoStringIndirect(string str) {
            if (QuietMode) {
                logger.Debug("info string: " + str);
            } else {
                Send("info string " + str);
            }
        }

        /// <summary>
        /// GPS将棋風、詰まされ時のinfo string (usi.pl用)
        /// </summary>
        public void LossByCheckmate() {
            Send("info string loss by checkmate");
        }

        #region checkmateコマンド(詰将棋解答機能)

        /// <summary>
        /// 詰将棋解答未実装
        /// </summary>
        public void SendCheckmateNotImplemented() {
            Send("checkmate notimplemented");
        }

        /// <summary>
        /// 詰将棋解答(時間切れ)
        /// </summary>
        public void SendCheckmateTimeout() {
            Send("checkmate timeout");
        }

        /// <summary>
        /// 詰将棋解答(不詰み)
        /// </summary>
        public void SendCheckmateNomate() {
            Send("checkmate nomate");
        }

        /// <summary>
        /// 詰将棋解答(詰み)
        /// </summary>
        public void SendCheckmate(params MoveData[] moves) {
            if (moves == null) {
                Send("checkmate");
            } else {
                Send("checkmate " + string.Join(" ",
                    Array.ConvertAll(moves, SFENNotationWriter.ToString)));
            }
        }

        #endregion

        /// <summary>
        /// デバッグ出力
        /// </summary>
        /// <param name="line">出力</param>
        public void DebugWriteLine(string line) {
            if (QuietMode) {
                logger.Debug("  # " + line);
            } else {
                WriteLine("  # " + line);
            }
        }

        /// <summary>
        /// その他のコマンドの送信
        /// </summary>
        /// <param name="raw">コマンド</param>
        public void Send(params string[] raw) {
            // 繋げて出力するだけ。
            Send(string.Join(" ", raw));
        }

        /// <summary>
        /// その他のコマンドの送信
        /// </summary>
        /// <param name="line">コマンド</param>
        private void Send(string line) {
            WriteLine(line);
            Flush();
        }

        #region IO

        /// <summary>
        /// 受信
        /// </summary>
        private string ReadLine() {
            // 入力
            string line = InnerReadLine();
            if (string.IsNullOrEmpty(line)) return null;

            // ログ
            Debug.WriteLine("USIClient> " + line);
            if (usiLogger.IsDebugEnabled) {
                usiLogger.Debug("> " + line);
            }

            return line;
        }

        /// <summary>
        /// 受信
        /// </summary>
        private string InnerReadLine() {
            return input.ReadLine();
        }

        /// <summary>
        /// 送信
        /// </summary>
        private void WriteLine(string line) {
            // ログ
            Debug.WriteLine("USIClient< " + line);
            if (usiLogger.IsDebugEnabled) {
                usiLogger.Debug("< " + line);
            }

            // 出力
            lock (output) {
                output.Write(line + "\n"); // USIの改行コードはLF。
            }
        }

        /// <summary>
        /// 送信
        /// </summary>
        private void WriteLine(string str, params object[] args) {
            WriteLine(string.Format(str, args));
        }

        /// <summary>
        /// 送信
        /// </summary>
        private void Flush() {
            output.Flush();
        }

        #endregion

        #region コマンドの解析処理など

        /// <summary>
        /// goコマンドの残り時間のparse
        /// </summary>
        /// <param name="infinite">制限時間が無制限ならtrue</param>
        /// <param name="firstTurnTime">先手の残り持ち時間 [ms]</param>
        /// <param name="secondTurnTime">後手の残り持ち時間 [ms]</param>
        /// <param name="byoyomiTime">秒読み [ms]</param>
        public static void ParseAllTimes(string goStr,
            out bool infinite, out int? firstTurnTime, out int? secondTurnTime, out int? byoyomiTime) {
            firstTurnTime = null;
            secondTurnTime = null;
            byoyomiTime = null;
            if (goStr.Contains("infinite")) {
                infinite = true;
            } else {
                infinite = false;
                string[] list = goStr.Split(' ');
                for (int i = 0; i < list.Length - 1; i++) {
                    if (list[i] == "btime") {
                        firstTurnTime = int.Parse(list[++i]);
                    } else if (list[i] == "wtime") {
                        secondTurnTime = int.Parse(list[++i]);
                    } else if (list[i] == "byoyomi") {
                        byoyomiTime = int.Parse(list[++i]);
                    }
                }
            }
        }

        /// <summary>
        /// go mateコマンドの残り時間のparse
        /// </summary>
        /// <param name="infinite">制限時間が無制限ならtrue</param>
        /// <param name="tsumeLimitTime">制限時間 [ms]</param>
        public static void ParseLimitTimes(string goStr, out bool infinite, out int tsumeLimitTime) {
            if (goStr.Contains("infinite")) {
                infinite = true;
                tsumeLimitTime = 0;
            } else {
                infinite = false;
                Match m = Regex.Match(goStr, @"mate\s+(\d+)");
                tsumeLimitTime = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            }
        }

        #endregion
    }
}
