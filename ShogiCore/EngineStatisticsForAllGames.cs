using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 対局してるエンジンの統計情報（複数対局分の平均値）
    /// </summary>
    public class EngineStatisticsForAllGames {
        /// <summary>
        /// 序盤～終盤の平均値を算出するためのクラス
        /// </summary>
        public class MeanValueSet {
            public readonly double[] Totals = new double[5];
            public int Count;
            /// <summary>
            /// 値を追加
            /// </summary>
            public void Add(double[] means) {
                Debug.Assert(Totals.Length == means.Length);
                for (int i = 0; i < Totals.Length; i++)
                    Totals[i] += means[i];
                Count++;
            }
            /// <summary>
            /// 文字列化
            /// </summary>
            public string ToString(string format) {
                return "序盤～終盤"
                    + "=" + (Totals[0] / Count).ToString(format)
                    + "/" + (Totals[1] / Count).ToString(format)
                    + "/" + (Totals[2] / Count).ToString(format)
                    + "/" + (Totals[3] / Count).ToString(format)
                    + "/" + (Totals[4] / Count).ToString(format)
                    + " 平均=" + (Totals.Average() / Count).ToString(format)
                    ;
            }
        }

        public readonly MeanValueSet TimeReal = new MeanValueSet();
        public readonly MeanValueSet TimeUSI = new MeanValueSet();
        public readonly MeanValueSet Depth = new MeanValueSet();
        public readonly MeanValueSet Nodes = new MeanValueSet();
        public readonly MeanValueSet NPS = new MeanValueSet();

        /// <summary>
        /// 1局分のデータを追加
        /// </summary>
        public void Add(EngineStatisticsForGame stat) {
            TimeReal.Add(GetMean(stat, 0));
            TimeUSI.Add(GetMean(stat, 1));
            Depth.Add(GetMean(stat, 2));
            Nodes.Add(GetMean(stat, 3));
            NPS.Add(GetMean(stat, 4));
        }

        /// <summary>
        /// 序盤～終盤を5個に分けてそれぞれの平均を算出する
        /// </summary>
        private double[] GetMean(EngineStatisticsForGame stat, int index) {
            int n = stat.States.Count / 5;
            if (n <= 0)
                return new double[5] { 0.0, 0.0, 0.0, 0.0, 0.0 };
            return new double[5] {
                stat.States.Take(n).Average(s => s.Values[index]),
                stat.States.Skip(n * 1).Take(n).Average(s => s.Values[index]),
                stat.States.Skip(n * 2).Take(n).Average(s => s.Values[index]),
                stat.States.Skip(n * 3).Take(n).Average(s => s.Values[index]),
                stat.States.Skip(n * 4).Average(s => s.Values[index]),
            };
        }

        public override string ToString() {
            return
                "通算平均時間(実測)：" + TimeReal.ToString("#,##0") + Environment.NewLine +
                "通算平均時間(USI)： " + TimeUSI.ToString("#,##0") + Environment.NewLine +
                "通算平均深さ：      " + Depth.ToString("0.0") + Environment.NewLine +
                "通算平均ノード数：  " + Nodes.ToString("#,##0") + Environment.NewLine +
                "通算平均NPS：       " + NPS.ToString("#,##0");
        }
    }
}
