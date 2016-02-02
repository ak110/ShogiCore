using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using ShogiCore.USI;
using ShogiCore.Notation;

namespace ShogiCore {
    /// <summary>
    /// USIエンジンなPlayerクラス
    /// </summary>
    public class USIPlayer : IPlayer {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 詰みの値
        /// </summary>
        public const int MateValue = 10000000;

        /// <summary>
        /// 前回の消費時間(実測)
        /// </summary>
        public Stopwatch LastTurnStopwatch { get; private set; }

        /// <summary>
        /// info score cp、もしくはinfo score mateを受け取っていたならtrue
        /// </summary>
        public bool HasScore { get; private set; }
        /// <summary>
        /// info score cpコマンドの値
        /// </summary>
        public int LastScore { get; private set; }
        /// <summary>
        /// info score mateを受け取っていたらtrue。(LastScoreに[MateValue + 手数]が入ってる)
        /// </summary>
        public bool LastScoreWasMate { get; private set; }
        /// <summary>
        /// scoreの表示用の文字列
        /// </summary>
        public string LastScoreString { get; private set; }
        /// <summary>
        /// info lowerbound
        /// </summary>
        public bool LastLowerBound { get; private set; }
        /// <summary>
        /// info upperbound
        /// </summary>
        public bool LastUpperBound { get; private set; }
        /// <summary>
        /// info pvコマンドの値。非null。
        /// </summary>
        public string[] LastPV { get; private set; }
        /// <summary>
        /// info timeコマンドの値。
        /// </summary>
        public double? LastTime { get; private set; }
        /// <summary>
        /// info depthコマンドの値。
        /// </summary>
        public double? LastDepth { get; private set; }
        /// <summary>
        /// info nodesコマンドの値。
        /// </summary>
        public double? LastNodes { get; private set; }
        /// <summary>
        /// info npsコマンドの値。
        /// </summary>
        public double? LastNPS { get; private set; }
        /// <summary>
        /// 前回のbestmoveのponderの指し手（存在する場合）
        /// </summary>
        public string LastPonderMove { get; private set; }
        /// <summary>
        /// ponder中ならtrue
        /// </summary>
        public bool IsPondering { get; private set; }

        /// <summary>
        /// USIDriver
        /// </summary>
        public USIDriver Driver { get; private set; }
        /// <summary>
        /// オプション
        /// </summary>
        public USIOptions Options { get; private set; }
        /// <summary>
        /// 秒読みを1秒減らしてエンジンへ送信するならtrue
        /// </summary>
        public bool ByoyomiHack { get; set;  }
        /// <summary>
        /// go depthする場合は設定する
        /// </summary>
        public int? GoDepth { get; set; }
        /// <summary>
        /// go nodesする場合は設定する
        /// </summary>
        public long? GoNodes { get; set; }

        /// <summary>
        /// CommandReceived
        /// </summary>
        public event EventHandler<USICommandEventArgs> CommandReceived;
        /// <summary>
        /// InfoReceived
        /// </summary>
        public event EventHandler<USIInfoEventArgs> InfoReceived;

        volatile bool aborted = false;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="engineFileName">USIエンジンのパス(環境変数利用可能)</param>
        /// <param name="engineArguments">USIエンジンの引数。nullで無し。将棋所が未対応なのでnullが無難？</param>
        /// <param name="logID">ログ記録用のエンジンのID</param>
        public USIPlayer(string engineFileName, string engineArguments = null, int logID = 1)
            : this(new USIDriver(engineFileName, engineArguments, logID)) { }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="driver">USIエンジン</param>
        public USIPlayer(USIDriver driver) {
            Driver = driver;
            Driver.CommandReceived += new EventHandler<USICommandEventArgs>(Driver_CommandReceived);
            Driver.InfoReceived += new EventHandler<USIInfoEventArgs>(Driver_InfoReceived);
            // オプション。とりあえず適当に入れておく。
            Options = new USIOptions();
            //Options["USI_Ponder"] = "false";
            //Options["USI_Hash"] = "32";
            // 初期化
            LastPV = new string[0];
            LastTurnStopwatch = new Stopwatch();
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            Driver.Dispose();
        }

        /// <summary>
        /// オプションの設定
        /// </summary>
        public void SetOption(string name, string value) {
            Options[name] = value;
        }

        #region IPlayer メンバ

        public string Name { get; set; }

        public string GetDisplayString(Board board) {
            StringBuilder str = new StringBuilder();
            str.AppendLine(Name + " by " + Driver.IdAuthor);

            // TODO: infoのデータの表示とか？

            return str.ToString();
        }

        public void GameStart() {
            GameStart(ProcessPriorityClass.Normal);
        }

        public void GameStart(ProcessPriorityClass pricessPriorityClass) {
            if (Driver.Start(pricessPriorityClass)) {
                Name = Driver.IdName;
                foreach (KeyValuePair<string, string> p in Options)
                    Driver.SendSetOption(p.Key, p.Value);
            }
            if (!Driver.WaitForReadyOK(30000))
                throw new ApplicationException("USIエンジンからの応答がありませんでした。");
            Driver.SendUSINewGame();
        }

        public Move DoTurn(Board board, PlayerTime btime, PlayerTime wtime) {
            var sfen = new SFENNotationWriter().WriteToString(board.ToNotation()).TrimEnd();

            LastTurnStopwatch.Restart();
            try {
                // go ponder中なら相手の指し手が一致しているかチェックして適当に処理
                if (IsPondering) {
                    var lastMoveSfen = SFENNotationWriter.ToString(board.GetLastMove().ToNotation());
                    if (lastMoveSfen == LastPonderMove) {
                        logger.Debug("ponder成功");
                        IsPondering = false;
                        LastPonderMove = null;
                        Driver.SendPonderHit();
                        goto WaitForBestMove;
                    } else {
                        logger.Debug("ponder失敗");
                        Driver.SendStop();
                        if (!Driver.WaitFor(30000, x => x.Name == "bestmove"))
                            logger.Warn("ponderに対するstopでbestmoveが未着 (1)");
                    }
                }

                // 初期化
                InitializeBeforeGo();
                IsPondering = false;
                LastPonderMove = null;

                // 局面送って思考開始
                Driver.SendPosition(sfen);
                if (GoDepth.HasValue)
                    Driver.SendGoDepth(GoDepth.Value);
                else if (GoNodes.HasValue)
                    Driver.SendGoNodes(GoNodes.Value);
                else
                    Driver.SendGo(false, btime, wtime, board.Turn == 0, ByoyomiHack);

                // 指し手を取得
                WaitForBestMove:
                return WaitForBestMove(board, board.Turn == 0 ? btime : wtime);
            } catch (Exception e) {
                throw new ApplicationException("USIエンジンの思考時に例外発生。エンジン=" + Name + " SFEN=" + sfen, e);
            } finally {
                LastTurnStopwatch.Stop();
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void InitializeBeforeGo() {
            HasScore = false;
            LastScore = 0;
            LastScoreString = "";
            LastPV = new string[0];
            LastTime = null;
            LastDepth = null;
            LastNodes = null;
            LastNPS = null;
        }

        private Move WaitForBestMove(Board board, PlayerTime t) {
            while (true) {
                USICommand command;
                if (!Driver.TryReceiveCommand(t.Unit / 2, out command)) {
                    // 中断
                    if (aborted)
                        return Move.Resign;
                    // 異常終了
                    if (Driver.IsProcessExited)
                        throw new ApplicationException("USIエンジンの異常終了");
                    // 無応答。ぎりぎり時間切れではなく盛大にオーバーしてる場合。(閾値は適当。単位時間×16とした。)
                    int time = (int)LastTurnStopwatch.ElapsedMilliseconds;
                    if (t.GetLimitTime() + t.Unit * 16 <= time) {
                        throw new ApplicationException("USIエンジンが無応答" +
                            " 実測時間=" + (time / 1000.0) +
                            " USI時間=" + (LastTime ?? 0) / 1000.0 +
                            " " + t);
                    }
                    // 時間が残ってる(?)ので再度待つ
                    continue;
                }

                switch (command.Name) {
                    case "bestmove":
                        if (command.Parameters.StartsWith("resign", StringComparison.Ordinal))
                            return Move.Resign;
                        else if (command.Parameters.StartsWith("win", StringComparison.Ordinal))
                            return Move.Win;
                        else if (command.Parameters.StartsWith("pass", StringComparison.Ordinal))
                            return Move.Pass;
                        var @params = command.Parameters.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string sfenMove = @params[0];
                        // PVと一致した指し手かどうかチェック
                        if (0 < LastPV.Length && LastPV[0] != sfenMove) {
                            // 一致してなければ無効にしてしまう
                            HasScore = false;
                            LastPV = new string[0];
                            LastScore = 0;
                            LastScoreString = "";
                        }
                        // ponder
                        if (3 <= @params.Length && @params[1] == "ponder")
                            LastPonderMove = @params[2];
                        // 指し手を解析
                        return Move.FromNotation(board, SFENNotationReader.ToMoveData(sfenMove));
                }
            }
        }

        public void Abort() {
            aborted = true;
            try {
                Driver.SendStop();
            } catch (Exception e) {
                logger.Debug("Abort時に例外発生", e);
            }
            Driver.Kill();
        }

        public void GameEnd(GameResult result) {
            if (IsPondering) {
                Driver.SendStop();
                if (!Driver.WaitFor(30000, x => x.Name == "bestmove"))
                    logger.Warn("ponderに対するstopでbestmoveが未着  (2)");
                IsPondering = false;
            }
            switch (result) {
                case GameResult.Win: Driver.SendGameOverWin(); break;
                case GameResult.Lose: Driver.SendGameOverLose(); break;
                case GameResult.Draw: Driver.SendGameOverDraw(); break;
                default: goto case GameResult.Draw;
            }
        }

        #endregion

        /// <summary>
        /// ponderを開始する。
        /// </summary>
        /// <remarks>
        /// DoTurn内に完全に隠蔽することも可能だが、よりシビアなタイミングで呼び出したり、
        /// ponderしなかったりする場合を考慮していちいち呼ばないとやらないように作っておく。
        /// </remarks>
        public void StartPonder(Board board, PlayerTime btime, PlayerTime wtime) {
            if (string.IsNullOrEmpty(LastPonderMove))
                return;
            logger.Debug("ponder開始");
            var notation = board.ToNotation();
            notation.Moves = Enumerable.Concat(notation.Moves,
                new[] { new MoveDataEx(SFENNotationReader.ToMoveData(LastPonderMove)) })
                .ToArray();
            var colorIsBlack = board.Turn != 0; // LastPonderMoveの分があるので手番反転
            InitializeBeforeGo();
            IsPondering = true;
            Driver.SendPosition(notation);
            Driver.SendGo(true, btime, wtime, colorIsBlack, ByoyomiHack);
        }

        /// <summary>
        /// コマンド受信時の処理
        /// </summary>
        void Driver_CommandReceived(object sender, USICommandEventArgs e) {
            switch (e.USICommand.Name) {
                case "id":
                    Name = Driver.IdName; // 手抜きで毎回セットしとく
                    e.Handled = true;
                    break;

                // TODO: 未実装

            }

            var CommandReceived = this.CommandReceived;
            if (CommandReceived != null) {
                CommandReceived(sender, e);
            }
        }

        /// <summary>
        /// infoコマンド受信時の処理
        /// </summary>
        void Driver_InfoReceived(object sender, USIInfoEventArgs e) {
            LastLowerBound = false;
            LastUpperBound = false;

            foreach (USIInfo info in e.SubCommands) {
                switch (info.Name) {
                    case "score": {
                            HasScore = true;
                            if (info.Parameters.FirstOrDefault() == "cp") {
                                // 評価値
                                string s = info.Parameters.Skip(1).FirstOrDefault();
                                int n;
                                if (int.TryParse(s, out n)) {
                                    LastScore = n;
                                    LastScoreString = s;
                                } else {
                                    LastScoreString = s;
                                }
                                LastScoreWasMate = false;
                            } else if (info.Parameters.FirstOrDefault() == "mate") {
                                // 詰み (GameTreeでの値に合わせる)
                                string mateStr = info.Parameters.Skip(1).FirstOrDefault();
                                int n;
                                if (string.IsNullOrEmpty(mateStr)) {
                                    LastScore = +MateValue;
                                    logger.Warn("不正なinfo score mate");
                                } else if (mateStr[0] == '+') {
                                    LastScore = +MateValue;
                                } else if (mateStr[0] == '-') {
                                    LastScore = -MateValue;
                                } else if (int.TryParse(mateStr, out n)) {
                                    LastScore = MateValue + Math.Abs(n);
                                } else {
                                    LastScore = +MateValue;
                                    logger.Warn("不正なinfo score mate: " + info.ToString());
                                }
                                LastScoreWasMate = true;
                            } else {
                                logger.Warn("不正なinfo score: " + info.ToString());
                            }
                        }
                        break;

                    case "lowerbound": LastLowerBound = true; break;
                    case "upperbound": LastUpperBound = true; break;

                    case "pv": LastPV = info.Parameters; break;

                    case "time":
                        try {
                            LastTime = double.Parse(info.Parameters.FirstOrDefault().Replace(",", ""));
                        } catch (Exception ex) {
                            logger.Debug("timeの解析に失敗: " + info.Parameters.FirstOrDefault(), ex);
                        }
                        break;

                    case "depth":
                        try {
                            LastDepth = double.Parse(info.Parameters.FirstOrDefault().Replace(",", ""));
                        } catch (Exception ex) {
                            logger.Debug("depthの解析に失敗: " + info.Parameters.FirstOrDefault(), ex);
                        }
                        break;

                    case "nodes":
                        try {
                            LastNodes = double.Parse(info.Parameters.FirstOrDefault().Replace(",", ""));
                        } catch (Exception ex) {
                            logger.Debug("nodesの解析に失敗: " + info.Parameters.FirstOrDefault(), ex);
                        }
                        break;

                    case "nps":
                        try {
                            string s = info.Parameters.FirstOrDefault().Replace(",", "").ToLowerInvariant();
                            if (s.EndsWith("m") || s.EndsWith("ｍ")) {
                                LastNPS = double.Parse(s.Substring(0, s.Length - 1)) * 1024 * 1024;
                            } else if (s.EndsWith("k") || s.EndsWith("ｋ")) {
                                LastNPS = double.Parse(s.Substring(0, s.Length - 1)) * 1024;
                            } else {
                                LastNPS = double.Parse(s);
                            }
                        } catch (Exception ex) {
                            logger.Debug("npsの解析に失敗: " + info.Parameters.FirstOrDefault(), ex);

                        }
                        break;
                }
            }

            var InfoReceived = this.InfoReceived;
            if (InfoReceived != null) {
                InfoReceived(sender, e);
            }
        }
    }
}
