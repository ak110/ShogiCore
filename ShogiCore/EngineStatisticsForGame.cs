using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 対局してるエンジンの統計情報（1対局分の各手番の情報）
    /// </summary>
    public class EngineStatisticsForGame {
        /// <summary>
        /// 1手指した時の情報
        /// </summary>
        public struct State {
            /// <summary>
            /// 時間(実測)、時間(USI)、深さ、ノード数、NPS
            /// </summary>
            public double[] Values;
        }

        /// <summary>
        /// 1局分の情報
        /// </summary>
        public List<State> States { get; private set; }

        public EngineStatisticsForGame() {
            States = new List<State>();
        }

        /// <summary>
        /// 手番の終了時に呼び出す
        /// </summary>
        /// <param name="player">エンジン</param>
        /// <param name="timeReal">実測時間（ミリ秒）</param>
        public void Add(USIPlayer player, double timeReal) {
            States.Add(new State {
                Values = new double[] {
                    timeReal,
                    player.LastTime ?? timeReal,
                    player.LastDepth ?? 0,
                    player.LastNodes ?? 0,
                    player.LastNPS ?? 0,
                }
            });
        }

        public double? MeanTimeRel { get { return GetMean(0); } }
        public double? MeanTimeUSI { get { return GetMean(1); } }
        public double? MeanDepth { get { return GetMean(2); } }
        public double? MeanNodes { get { return GetMean(3); } }
        public double? MeanNPS { get { return GetMean(4); } }

        private double? GetMean(int index) {
            if (!States.Any()) return null;
            return States.Average(s => s.Values[index]);
        }
    }
}
