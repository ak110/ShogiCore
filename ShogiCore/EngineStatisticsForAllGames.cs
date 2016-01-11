using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 対局してるエンジンの統計情報（複数対局分の平均値）
    /// </summary>
    public class EngineStatisticsForAllGames {
        /// <summary>
        /// 平均値を算出するためのクラス
        /// </summary>
        public class MeanValue {
            double sum = 0.0;
            long count = 0;

            /// <summary>
            /// 値を追加する（nullなら無視）
            /// </summary>
            /// <param name="value">値</param>
            public void Add(double? value) {
                if (value.HasValue) {
                    sum += value.Value;
                    count++;
                }
            }

            /// <summary>
            /// 平均値を返す
            /// </summary>
            public double? Mean {
                get {
                    if (count == 0)
                        return null;
                    return sum / count;
                }
            }

            /// <summary>
            /// formatで文字列化
            /// </summary>
            public string ToString(string format) {
                var m = Mean;
                return m.HasValue ? m.Value.ToString(format) : "-";
            }

            /// <summary>
            /// 比率を有効数字3桁で返す
            /// </summary>
            public string RateToString(MeanValue org) {
                var m = Mean;
                var o = org.Mean;
                return m.HasValue && o.HasValue && o.Value != 0 ? (m.Value / o.Value).ToString("G3") : "-";
            }
        }
        /// <summary>
        /// 全体、序盤、終盤の平均値を算出するためのクラス
        /// </summary>
        public class MeanValueSet {
            public readonly MeanValue All = new MeanValue();
            public readonly MeanValue Opening = new MeanValue();
            public readonly MeanValue MidGame = new MeanValue();
            public readonly MeanValue EndGame = new MeanValue();
            /// <summary>
            /// 値を追加
            /// </summary>
            public void Add(GameMeanValue.ResultValues result) {
                All.Add(result.MeanOfAll);
                Opening.Add(result.MeanOfOpening);
                MidGame.Add(result.MeanOfMidGame);
                EndGame.Add(result.MeanOfEndGame);
            }
            /// <summary>
            /// 文字列化
            /// </summary>
            public string ToString(string format) {
                return "全体=" + All.ToString(format) +
                    " 比率(序盤～終盤)=" + Opening.RateToString(All) +
                    "/" + MidGame.RateToString(All) +
                    "/" + EndGame.RateToString(All);
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
            TimeReal.Add(stat.TimeReal.Result);
            TimeUSI.Add(stat.TimeUSI.Result);
            Depth.Add(stat.Depth.Result);
            Nodes.Add(stat.Nodes.Result);
            NPS.Add(stat.NPS.Result);
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
