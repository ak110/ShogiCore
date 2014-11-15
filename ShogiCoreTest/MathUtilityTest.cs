using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiCore;

namespace ShogiCoreTest {
    [TestClass]
    public class MathUtilityTest {
        [TestMethod]
        public void TestSignTest() {
            Assert.AreEqual(0.588099, MathUtility.SignTest(10, 10), 0.000001);
            Assert.AreEqual(0.994091, MathUtility.SignTest(5, 15), 0.000001);
            Assert.AreEqual(0.005909, MathUtility.SignTest(15, 5), 0.000001);
            Assert.AreEqual(0.528174, MathUtility.SignTest(100, 100), 0.000001);
            Assert.AreEqual(0.000783, MathUtility.SignTest(150, 100), 0.001);
            Assert.AreEqual(0.999217, MathUtility.SignTest(100, 150), 0.001);
            Assert.AreEqual(0.500000, MathUtility.SignTest(1000, 1000), 0.000001);
            Assert.AreEqual(0.000000, MathUtility.SignTest(1500, 1000), 0.000001);
            Assert.AreEqual(1.000000, MathUtility.SignTest(1000, 1500), 0.000001);
            Assert.AreEqual(0.134728, MathUtility.SignTest(1050, 1000), 0.000001);
            Assert.AreEqual(0.865272, MathUtility.SignTest(1000, 1050), 0.000001);
        }
    }
}
