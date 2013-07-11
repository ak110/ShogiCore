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
        /// info pvコマンドの値。非null。
        /// </summary>
        public string[] LastPV { get; private set; }
        /// <summary>
        /// info depthコマンドの値。
        /// </summary>
        public double? LastDepth { get; private set; }
        /// <summary>
        /// info npsコマンドの値。
        /// </summary>
        public double? LastNPS { get; private set; }

        /// <summary>
        /// USIDriver
        /// </summary>
        public USIDriver Driver { get; private set; }
        /// <summary>
        /// オプション
        /// </summary>
        public USIOptions Options { get; private set; }

        /// <summary>
        /// CommandReceived
        /// </summary>
        public event EventHandler<USICommandEventArgs> CommandReceived;
        /// <summary>
        /// InfoReceived
        /// </summary>
        public event EventHandler<USIInfoEventArgs> InfoReceived;

        /// <summary>
        /// １対局分の深さのリスト
        /// </summary>
        public List<double?> GameDepthList { get; private set; }
        /// <summary>
        /// １対局分のNPSのリスト
        /// </summary>
        public List<double?> GameNPSList { get; private set; }

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
            Name = Driver.IdName;
            // オプション。とりあえず適当に入れておく。
            Options = new USIOptions();
            //Options["USI_Ponder"] = "false";
            //Options["USI_Hash"] = "32";
            // 念のため初期化
            LastPV = new string[0];
            GameDepthList = new List<double?>();
            GameNPSList = new List<double?>();
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
            foreach (KeyValuePair<string, string> p in Options) {
                Driver.SendSetOption(p.Key, p.Value);
            }
            if (!Driver.WaitForReadyOK(30000)) {
                throw new ApplicationException("USIエンジンからの応答がありませんでした。");
            }
        }

        public Move DoTurn(Board board, int firstTurnTime, int secondTurnTime, int byoyomi) {
            HasScore = false;
            LastScore = 0;
            LastScoreString = "";
            LastPV = new string[0];
            LastDepth = null;
            LastNPS = null;

            if (!Driver.GameStarted) {
                Driver.SendUSINewGame();
            }
            Driver.SendPosition(board.ToNotation());
            Driver.SendGo(firstTurnTime, secondTurnTime, byoyomi);

            while (true) {
                USICommand command;
                if (!Driver.TryReceiveCommand(Timeout.Infinite, out command)) {
                    return Move.Resign;
                }

                switch (command.Name) {
                    case "bestmove":
                        GameDepthList.Add(LastDepth);
                        GameNPSList.Add(LastNPS);

                        if (command.Parameters.StartsWith("resign", StringComparison.Ordinal)) {
                            return Move.Resign;
                        } else if (command.Parameters.StartsWith("win", StringComparison.Ordinal)) {
                            return Move.Win;
                        } else if (command.Parameters.StartsWith("pass", StringComparison.Ordinal)) {
                            return Move.Pass;
                        }
                        string sfenMove = command.Parameters.Split(new[] { ' ' }, 2)[0]; // ponderとかは無視！
                        // PVと一致した指し手かどうかチェック
                        if (0 < LastPV.Length && LastPV[0] != sfenMove) {
                            // 一致してなければ無効にしてしまう
                            HasScore = false;
                            LastPV = new string[0];
                            LastScore = 0;
                            LastScoreString = "";
                        }
                        // 指し手を解析
                        Move move = Move.FromNotation(board, SFENNotationReader.ToMoveData(sfenMove));
                        return move;
                    // bestmove以外のコマンドはスルー
                }
            }

            /* 例↓
> position startpos
> go btime 0 wtime 0 byoyomi 100
< info string EvaluationTable: log.08-12-28,182906,J=4.967: n=8000 w=0.00030 tx=100 !bad !op opea=10
< info string TimeControl=Nodes
< info depth 0 nodes 2445 score cp 0 pv 2g2f
< info depth 0 nodes 25206 score cp 0 pv 7g7f
< bestmove 2g2f
             */
        }

        public void Abort() {
            Driver.SendStop();
        }

        public void GameEnd(Board board, GameResult result) {
            GameDepthList.Clear();
            GameNPSList.Clear();
            switch (result) {
                case GameResult.Win: Driver.SendGameOverWin(); break;
                case GameResult.Lose: Driver.SendGameOverLose(); break;
                case GameResult.Draw: Driver.SendGameOverDraw(); break;
                default: goto case GameResult.Draw;
            }
        }

        #endregion

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

                    case "pv": LastPV = info.Parameters; break;

                    case "depth":
                        try {
                            string s = info.Parameters.FirstOrDefault().Replace(",", "");
                            LastDepth = double.Parse(s);
                        } catch (Exception ex) {
                            logger.Debug("depthの解析に失敗: " + info.Parameters.FirstOrDefault(), ex);

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

        /// <summary>
        /// 全体の深さの平均
        /// </summary>
        public double? MeanDepth {
            get { return GetMean(GameDepthList, 1.0); }
        }
        /// <summary>
        /// 序盤の深さの平均
        /// </summary>
        public double? MeanDepthOfOpening {
            get { return GetMean(GameDepthList.Take(GameDepthList.Count / 2), 1.0); }
        }
        /// <summary>
        /// 終盤の深さの平均
        /// </summary>
        public double? MeanDepthOfEndGame {
            get { return GetMean(GameDepthList.Skip(GameDepthList.Count / 2), 1.0); }
        }
        /// <summary>
        /// 全体のNPSの平均
        /// </summary>
        public double? MeanNPS {
            get { return GetMean(GameNPSList); }
        }
        /// <summary>
        /// 序盤のNPSの平均
        /// </summary>
        public double? MeanNPSOfOpening {
            get { return GetMean(GameNPSList.Take(GameNPSList.Count / 2)); }
        }
        /// <summary>
        /// 終盤のNPSの平均
        /// </summary>
        public double? MeanNPSOfEndGame {
            get { return GetMean(GameNPSList.Skip(GameNPSList.Count / 2)); }
        }

        /// <summary>
        /// 平均を算出。特異値の影響を避けるために中央値からmedianThreshold以上外れているものは除外して平均。
        /// </summary>
        private double? GetMean(IEnumerable<double?> list, double medianThreshold = 0.3) {
            try {
                // nullを除外して要素数チェック
                var nps = list.Where(x => x.HasValue).Select(x => x.Value);
                if (!nps.Any()) return null;
                // 中央値を求める
                var npsOrdered = nps.OrderBy(x => x);
                int c = nps.Count();
                double median = c % 2 == 0 ?
                    npsOrdered.Skip(c / 2 - 1).Take(2).Average() :
                    npsOrdered.Skip(c / 2).First();
                // 中央値から±3割以上離れている値は除外して平均
                double a = median * medianThreshold;
                return nps.Where(x => Math.Abs(median - x) <= a).Average();
            } catch (Exception e) {
                logger.Warn("平均値算出失敗", e);
                return null;
            }
        }
    }
}
