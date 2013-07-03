using ShogiCore.CSA;
using ShogiCore.Notation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ShogiCore {
    public class CSAGameEventArgs : EventArgs {
        public CSAGame CSAGame { get; private set; }
        public CSAGameEventArgs(CSAGame game) { CSAGame = game; }
    }
    /// <summary>
    /// CSAプロトコルでの通信対局を行うクラス
    /// </summary>
    public class CSAGame {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// CSAClient
        /// </summary>
        public CSAClient Client { get; private set; }
        /// <summary>
        /// player
        /// </summary>
        public IPlayer Player { get; private set; }
        /// <summary>
        /// 拡張モードのゲーム名
        /// </summary>
        public string ExGameName { get; private set; }
        /// <summary>
        /// 拡張モードの手番
        /// </summary>
        public string ExTurn { get; private set; }

        /// <summary>
        /// 現在の局面
        /// </summary>
        public Board Board { get; private set; }

        /// <summary>
        /// 対局開始イベント
        /// </summary>
        public event EventHandler<CSAGameEventArgs> GameStart;
        /// <summary>
        /// 対局終了イベント
        /// </summary>
        public event EventHandler<CSAGameEventArgs> GameEnd;
        /// <summary>
        /// 指し手受信イベント
        /// </summary>
        public event EventHandler<CSAGameEventArgs> MoveReceived;

        /// <summary>
        /// 前回の指し手のコメント（* {評価値} {PV}）
        /// </summary>
        string lastMoveComment;
        /// <summary>
        /// 現在書き込み中のCSA棋譜ファイルパス（./Logs/配下に書き込む。）
        /// </summary>
        string currentCSAFilePath;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="client">ログイン済みのクライアント</param>
        /// <param name="player">プレイヤー</param>
        /// <param name="gameName">拡張モード時のゲーム名。拡張モードでない場合は無視される。</param>
        /// <param name="turn">拡張モード時の手番指定。拡張モードでない場合は無視される。</param>
        public CSAGame(CSAClient client, IPlayer player, string gameName = "floodgate-900-0", string turn = "*") {
            Client = client;
            Player = player;
            ExGameName = gameName;
            ExTurn = turn;

            Client.GameEnd += new EventHandler<CSAClientEventArgs>(csa_GameEnd);
            Client.Disconnected += new EventHandler<CSAClientEventArgs>(csa_Disconnected);
        }

        /// <summary>
        /// 1回の対局を行う
        /// </summary>
        public IEnumerable<Move> DoGame() {
            while (Client.State != CSAState.Finished) {
                CSAInternalCommand command = Client.ReceiveCommand();
                switch (command.CommandType) {
                    case CSAInternalCommandTypes.ExConnected:
                        Client.SendExGame(ExGameName, ExTurn);
                        break;

                    case CSAInternalCommandTypes.TestConnected:
                        Client.SendChallenge();
                        break;

                    case CSAInternalCommandTypes.GameSummaryReceived:
                        try {
                            Board = Board.FromNotation(Client.InitialPosition);
                            Client.SendAgree();
                        } catch (Exception e) {
                            logger.Warn(e);
                            // なんかエラったらとりあえずREJECT。。
                            Client.SendReject();
                        }
                        break;

                    case CSAInternalCommandTypes.Start:
                        // CSA棋譜の作成
                        try {
                            using (StreamWriter writer = OpenCSANotation()) {
                                writer.WriteLine("V2.2");
                                writer.Write("N+");
                                writer.WriteLine(Client.GameSummary.NameP);
                                writer.Write("N-");
                                writer.WriteLine(Client.GameSummary.NameN);
                                if (!string.IsNullOrEmpty(Client.GameSummary.Game_ID)) {
                                    writer.Write("$EVENT:");
                                    writer.WriteLine(Client.GameSummary.Game_ID);
                                }
                                writer.Write("$START_TIME:");
                                writer.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                                if (!string.IsNullOrEmpty(Client.InitialPositionString)) { // 無いと困るけど一応。。
                                    writer.WriteLine(Client.InitialPositionString.TrimEnd());
                                }
                            }
                        } catch (Exception e) {
                            logger.Warn("CSA棋譜の書き込みに失敗(1)", e);
                        }
                        // Player.GameStartは既に呼んである前提とする。
                        var GameStart = this.GameStart;
                        if (GameStart != null) {
                            GameStart(this, new CSAGameEventArgs(this));
                        }
                        if (Client.GameSummary.To_Move == Client.GameSummary.Your_Turn) {
                            // 先手なら指し手を返す
                            yield return DoTurn();
                        }
                        break;

                    case CSAInternalCommandTypes.SelfMove: // 自分の指し手がサーバから応答された場合
                        // 棋譜へ記録
                        try {
                            if (!string.IsNullOrEmpty(currentCSAFilePath)) {
                                if (string.IsNullOrEmpty(lastMoveComment)) {
                                    File.AppendAllText(currentCSAFilePath, command.ReceivedString + Environment.NewLine);
                                } else {
                                    File.AppendAllText(currentCSAFilePath, command.ReceivedString + Environment.NewLine + "'*" + lastMoveComment + Environment.NewLine);
                                }
                            }
                        } catch (Exception e) {
                            logger.Warn("CSA棋譜の書き込みに失敗(2)", e);
                        }
                        // イベント
                        {
                            var MoveReceived = this.MoveReceived;
                            if (MoveReceived != null) {
                                MoveReceived(this, new CSAGameEventArgs(this));
                            }
                        }
                        break;

                    case CSAInternalCommandTypes.EnemyMove: // 相手の指し手がサーバから来た場合
                        // 棋譜へ記録
                        try {
                            if (!string.IsNullOrEmpty(currentCSAFilePath)) {
                                File.AppendAllText(currentCSAFilePath, command.ReceivedString + Environment.NewLine);
                            }
                        } catch (Exception e) {
                            logger.Warn("CSA棋譜の書き込みに失敗(3)", e);
                        }
                        // イベント
                        {
                            var MoveReceived = this.MoveReceived;
                            if (MoveReceived != null) {
                                MoveReceived(this, new CSAGameEventArgs(this));
                            }
                        }
                        // 敵の指し手で局面を進めて一旦yield
                        Move enemyMove = Move.FromNotation(Board, command.MoveDataEx.MoveData);
                        Board.Do(enemyMove);
                        yield return enemyMove;
                        // 自分の番なので思考して局面を進めてもう一度yield
                        yield return DoTurn();
                        break;

                    case CSAInternalCommandTypes.SpecialMove: // %TORYOなどがサーバから来た場合
                        // 棋譜へ記録
                        try {
                            if (!string.IsNullOrEmpty(currentCSAFilePath)) {
                                File.AppendAllText(currentCSAFilePath, command.ReceivedString + Environment.NewLine);
                            }
                        } catch (Exception e) {
                            logger.Warn("CSA棋譜の書き込みに失敗(3)", e);
                        }
                        break;

                    case CSAInternalCommandTypes.Disconnected:
                        break; // yield break?

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 手番時の処理。player.DoTurn()して、結果の指し手で盤面を進めて、結果の指し手を返す。
        /// </summary>
        /// <returns>指し手</returns>
        private Move DoTurn() {
            // 思考
            Move move = Player.DoTurn(Board,
                Client.FirstTurnRemainSeconds * 1000,
                Client.SecondTurnRemainSeconds * 1000,
                Client.GameSummary.Times[Board.Turn].Byoyomi * 1000);
            // 評価値・読み筋
            lastMoveComment = "";
            USIPlayer usiPlayer = Player as USIPlayer; // TODO: 整理
            if (usiPlayer != null && !string.IsNullOrEmpty(usiPlayer.LastPV)) {
                BoardData b = Client.CurrentBoard.Clone();
                int score = usiPlayer.LastScore;
                if (b.Turn != 0) score = -score; // 後手番なら符号を反転
                string pvString = "";
                var moves = usiPlayer.LastPV.Split(' ').Select(x => SFENNotationReader.ToMoveData(x));
                if (0 < moves.Count()) {
                    // PVの先頭がこれから指す手なら(通常はそのはず)、その手をskip。
                    MoveData curMoveData = move.ToNotation();
                    if (moves.First() == curMoveData) {
                        moves = moves.Skip(1);
                        b.Do(curMoveData); // 局面はPV生成用に進めておく。
                    }
                    pvString = PCLNotationWriter.ToString(b, moves);
                }
                lastMoveComment = "* " + score.ToString() + " " + pvString;
            }
            // 指し手を送信
            if (move.IsSpecialState) {
                if (move == Move.Win) { // 入玉勝ち宣言
                    Client.SendKachi();
                } else if (move == Move.Resign) { // 投了
                    Client.SendToryo(lastMoveComment);
                } else {
                    logger.Warn("不正な指し手: " + move.ToString(Board));
                }
            } else { // 指し手
                Client.SendMove(move.ToNotation(), lastMoveComment);
                Board.Do(move);
            }
            return move;
        }

        /// <summary>
        /// ゲーム終了
        /// </summary>
        void csa_GameEnd(object sender, CSAClientEventArgs e) {
            try {
                CloseCSANotation();
            } catch (Exception ex) {
                logger.Warn("CSA棋譜の書き込みに失敗(4)", ex);
            }
            var player = this.Player;
            if (player != null) {
                player.GameEnd(Board, e.CSAClient.LastGameResult);
            }
            var GameEnd = this.GameEnd;
            if (GameEnd != null) {
                GameEnd(this, new CSAGameEventArgs(this));
            }
        }

        /// <summary>
        /// 通信切断
        /// </summary>
        void csa_Disconnected(object sender, CSAClientEventArgs e) {
            var player = this.Player;
            if (player != null) {
                player.Abort();
            }
        }

        /// <summary>
        /// CSA棋譜の書き込み開始
        /// </summary>
        private StreamWriter OpenCSANotation() {
            currentCSAFilePath = null;
            using (Mutex m = new Mutex(false, "{6CF923FD-7A26-42A0-8614-67AE6004CF7C}")) {
                try {
                    m.WaitOne();
                } catch { // 微妙だけど無視
                }
                try {
                    // 将棋所風ファイル名：「20130104_152323 BlunderXX vs BlunderXX-Test.csa」
                    string name = DateTime.Now.ToString("yyyyMMdd_HHmmss") + " " + Client.GameSummary.NameP + " vs " + Client.GameSummary.NameN;
                    name = SanitizeFileName(name);
                    string pathWithoutExtension = Path.Combine(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Logs"), name);
                    currentCSAFilePath = pathWithoutExtension + ".csa";
                    if (File.Exists(currentCSAFilePath)) {
                        for (int i = 1; ; i++) {
                            string t = pathWithoutExtension + " - (" + i + ")" + ".csa";
                            if (!File.Exists(t)) {
                                currentCSAFilePath = t;
                                break;
                            }
                        }
                    }
                    return new StreamWriter(currentCSAFilePath, false, Encoding.GetEncoding(932));
                } finally {
                    m.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// ファイル名を適当サニタイジング。こんなもんでいいんだろうか…。
        /// </summary>
        private string SanitizeFileName(string name) {
            if (Regex.IsMatch(name, @"^(AUX|CON|NUL|PRN|COM[1-9]|LPT[1-9])(\..*)?$")) {
                return DateTime.Now.ToString("yyyyMMdd_HHmmssfff"); // どうしようもないので適当
            }
            StringBuilder str = new StringBuilder(name);
            foreach (char c in Path.GetInvalidFileNameChars()) {
                str.Replace(c, '_');
            }
            return str.ToString();
        }

        /// <summary>
        /// CSA棋譜の書き込み終了
        /// </summary>
        private void CloseCSANotation() {
            if (string.IsNullOrEmpty(currentCSAFilePath)) return;
            // TODO: floodgateを真似て下記のような感じにしたい
            //'P1 *  *  *  *  *  *  * -HI-KY
            //'P2 * +NY+KA * -KI *  *  *  * 
            //'P3 *  *  *  * -OU-FU-FU *  * 
            //'P4 * +FU-FU-FU-GI-KI-KI * -FU
            //'P5+KY+KE+FU * -KE * -GI+KE * 
            //'P6+FU+OU * +FU * +FU * +FU+FU
            //'P7 * +KA+KE *  *  *  *  *  * 
            //'P8 *  *  *  *  * +KI *  *  * 
            //'P9 * +KY * +GI *  *  *  *  * 
            //'P+00FU00FU00FU00FU00FU00GI00HI
            //'P-00FU
            //'+
            //'summary:toryo:Gekisashi lose:Apery_2700K_4c win
            //'$END_TIME:2013/01/04 05:21:25

            // とりあえずEND_TIMEだけ。
            StringBuilder str = new StringBuilder();
            str.Append("'$END_TIME:").AppendLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            File.AppendAllText(currentCSAFilePath, str.ToString());
        }
    }
}
