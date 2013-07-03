using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 連続対局の勝ち負けの数などを管理する
    /// </summary>
    public class GameStatistics {
        /// <summary>
        /// 全ゲーム数。(引き分けがなければWinCount[0] + WinCount[1])
        /// </summary>
        public int TotalGames { get; private set; }
        /// <summary>
        /// それぞれの勝った数
        /// </summary>
        public int[] WinCount { get; private set; }
        /// <summary>
        /// 引き分け回数
        /// </summary>
        public int EvenCount { get { return TotalGames - WinCount[0] - WinCount[1]; } }
        /// <summary>
        /// 同一棋譜発生回数
        /// </summary>
        public int SameGameCount { get; private set; }

        object syncRoot = new object();

        /// <summary>
        /// 初期化
        /// </summary>
        public GameStatistics() {
            WinCount = new int[2];
        }

        /// <summary>
        /// 合計
        /// </summary>
        public void Add(IEnumerable<GameStatistics> enumerable) {
            foreach (GameStatistics s in enumerable) {
                TotalGames += s.TotalGames;
                WinCount[0] += s.WinCount[0];
                WinCount[1] += s.WinCount[1];
                SameGameCount += s.SameGameCount;
            }
        }

        /// <summary>
        /// 結果を追加
        /// </summary>
        /// <param name="winner">-1:引き分け、0,1:勝った方</param>
        public void AddResult(int winner, GameEndReason reason) {
            lock (syncRoot) {
                TotalGames++;
                if (0 <= winner) {
                    WinCount[winner]++;
                } else {
                    if (reason == GameEndReason.SameNotation) SameGameCount++;
                }
            }
        }

        /// <summary>
        /// それぞれのの勝率(%)
        /// </summary>
        public float GetWinPercent(int index) {
            if (TotalGames == 0) return 0;
            return WinCount[index] * 100.0f / TotalGames;
        }

        /// <summary>
        /// 勝率とかの表示
        /// </summary>
        public string GetDisplayString(IPlayer[] players) {
            StringBuilder str = new StringBuilder();
            str.Append(players[0].Name);
            str.AppendFormat("(勝率{0}%", GetWinPercent(0).ToString("##0.0"));
            if (1 <= WinCount[0] + WinCount[1]) {
                str.Append(", 有意確率=");
                str.Append((MathUtility.SignTest(WinCount[0], WinCount[1]) * 100).ToString("##0.0"));
                str.Append("%");
            }
            str.Append(") ");
            str.AppendFormat("{0}-{1}-{2}/{3} ",
                WinCount[0].ToString(),
                EvenCount.ToString(),
                WinCount[1].ToString(),
                (WinCount[0] + WinCount[1]).ToString());
            str.Append(players[1].Name);
            str.AppendFormat("(勝率{0}%)", GetWinPercent(1).ToString("##0.0"));
            str.Append(", 重複=");
            str.Append(SameGameCount);
            str.Append("回");
            return str.ToString();
        }
    }
}
