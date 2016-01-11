using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiCore {
    [TestClass]
    public class EngineStatisticsForAllGamesTest {
        [TestMethod]
        public void TestToString() {
            EngineStatisticsForGame g = new EngineStatisticsForGame();
            g.TimeReal.Values.Add(10);
            g.TimeReal.Values.Add(11);
            g.TimeReal.Values.Add(12);
            g.TimeUSI.Values.Add(10);
            g.TimeUSI.Values.Add(11);
            g.TimeUSI.Values.Add(12);
            g.Depth.Values.Add(4);
            g.Depth.Values.Add(4);
            g.Depth.Values.Add(4);
            g.Nodes.Values.Add(1000);
            g.Nodes.Values.Add(1100);
            g.Nodes.Values.Add(1200);
            g.NPS.Values.Add(1000);
            g.NPS.Values.Add(1000);
            g.NPS.Values.Add(1000);
            g.Calculate();

            EngineStatisticsForAllGames a = new EngineStatisticsForAllGames();
            a.Add(g);

            Assert.AreEqual(
                "通算平均時間(実測)：全体=11 比率(序盤～終盤)=0.909/1/1.09" + Environment.NewLine +
                "通算平均時間(USI)： 全体=11 比率(序盤～終盤)=0.909/1/1.09" + Environment.NewLine +
                "通算平均深さ：      全体=4.0 比率(序盤～終盤)=1/1/1" + Environment.NewLine +
                "通算平均ノード数：  全体=1,100 比率(序盤～終盤)=0.909/1/1.09" + Environment.NewLine +
                "通算平均NPS：       全体=1,000 比率(序盤～終盤)=1/1/1",
                a.ToString());
        }
    }
}
