using System;
using Xunit;

namespace ShogiCore {
    public class MathUtilityTest {
        [Fact]
        public void TestSignTest() {
            Assert.Equal(0.588099, MathUtility.SignTest(10, 10), 6);
            Assert.Equal(0.994091, MathUtility.SignTest(5, 15), 6);
            Assert.Equal(0.005909, MathUtility.SignTest(15, 5), 6);
            Assert.Equal(0.528174, MathUtility.SignTest(100, 100), 6);
            Assert.Equal(0.000607, MathUtility.SignTest(150, 100), 6);
            Assert.Equal(0.999393, MathUtility.SignTest(100, 150), 6);
            Assert.Equal(0.500000, MathUtility.SignTest(1000, 1000), 6);
            Assert.Equal(0.000000, MathUtility.SignTest(1500, 1000), 6);
            Assert.Equal(1.000000, MathUtility.SignTest(1000, 1500), 6);
            Assert.Equal(0.134728, MathUtility.SignTest(1050, 1000), 6);
            Assert.Equal(0.865272, MathUtility.SignTest(1000, 1050), 6);
        }

        [Fact]
        public void TestDoubleSignTest() {
            Assert.Equal(0.500000, MathUtility.DoubleSignTest(100, 100, 100, 100), 6);
            Assert.Equal(0.314865, MathUtility.DoubleSignTest(100, 100, 110, 100), 6);
            Assert.Equal(0.685135, MathUtility.DoubleSignTest(100, 100, 100, 110), 6);
            Assert.Equal(0.314865, MathUtility.DoubleSignTest(100, 110, 100, 100), 6);
            Assert.Equal(0.685135, MathUtility.DoubleSignTest(110, 100, 100, 100), 6);
            Assert.Equal(0.063685, MathUtility.DoubleSignTest(1000, 1000, 1100, 1000), 6);
            Assert.Equal(0.936315, MathUtility.DoubleSignTest(1000, 1000, 1000, 1100), 6);
        }
    }
}
