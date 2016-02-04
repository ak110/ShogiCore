using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiCore {
    [TestClass]
    public class EngineStatisticsForAllGamesTest {
        [TestMethod]
        public void TestToString() {
            EngineStatisticsForGame g = new EngineStatisticsForGame();
            g.States.Add(new EngineStatisticsForGame.State {
                Values = new double[] { 10, 10, 4, 1000, 1000 }
            });
            g.States.Add(new EngineStatisticsForGame.State {
                Values = new double[] { 11, 11, 4, 1100, 1000 }
            });
            g.States.Add(new EngineStatisticsForGame.State {
                Values = new double[] { 12, 12, 4, 1200, 1000 }
            });
            g.States.Add(new EngineStatisticsForGame.State {
                Values = new double[] { 13, 13, 4, 1300, 1000 }
            });
            g.States.Add(new EngineStatisticsForGame.State {
                Values = new double[] { 14, 14, 4, 1400, 1000 }
            });

            EngineStatisticsForAllGames a = new EngineStatisticsForAllGames();
            a.Add(g);

            Assert.AreEqual(
                "通算平均時間(実測)：平均=12 %(序盤～終盤)=17/18/20/22/23" + Environment.NewLine +
                "通算平均時間(USI)： 平均=12 %(序盤～終盤)=17/18/20/22/23" + Environment.NewLine +
                "通算平均深さ：      平均=4.0 %(序盤～終盤)=20/20/20/20/20" + Environment.NewLine +
                "通算平均ノード数：  平均=1,200 %(序盤～終盤)=17/18/20/22/23" + Environment.NewLine +
                "通算平均NPS：       平均=1,000 %(序盤～終盤)=20/20/20/20/20",
                a.ToString());
        }
    }
}
