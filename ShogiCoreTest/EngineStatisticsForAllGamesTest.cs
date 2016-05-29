using System;
using Xunit;

namespace ShogiCore {
    public class EngineStatisticsForAllGamesTest {
        [Fact]
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

            Assert.Equal(
                "通算平均時間(実測)：序盤～終盤=10/11/12/13/14 平均=12" + Environment.NewLine +
                "通算平均時間(USI)： 序盤～終盤=10/11/12/13/14 平均=12" + Environment.NewLine +
                "通算平均深さ：      序盤～終盤=4.0/4.0/4.0/4.0/4.0 平均=4.0" + Environment.NewLine +
                "通算平均ノード数：  序盤～終盤=1,000/1,100/1,200/1,300/1,400 平均=1,200" + Environment.NewLine +
                "通算平均NPS：       序盤～終盤=1,000/1,000/1,000/1,000/1,000 平均=1,000",
                a.ToString());
        }
    }
}
