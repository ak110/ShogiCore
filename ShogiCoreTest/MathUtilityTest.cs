using System;
using Xunit;

namespace ShogiCore {
    public class MathUtilityTest {
        [Fact]
        public void TestNormSDist() {
            Assert.Equal(0.001350, MathUtility.NormSDist(-3), 6);
            Assert.Equal(0.022750, MathUtility.NormSDist(-2), 6);
            Assert.Equal(0.158655, MathUtility.NormSDist(-1), 6);
            Assert.Equal(0.500000, MathUtility.NormSDist(+0), 6);
            Assert.Equal(0.841345, MathUtility.NormSDist(+1), 6);
            Assert.Equal(0.977250, MathUtility.NormSDist(+2), 6);
            Assert.Equal(0.998650, MathUtility.NormSDist(+3), 6);
        }

        [Fact]
        public void TestNormSInv() {
            Assert.Equal(-3.0, MathUtility.NormSInv(0.001350), 2);
            Assert.Equal(-2.0, MathUtility.NormSInv(0.022750), 2);
            Assert.Equal(-1.0, MathUtility.NormSInv(0.158655), 2);
            Assert.Equal(+0.0, MathUtility.NormSInv(0.500000), 2);
            Assert.Equal(+1.0, MathUtility.NormSInv(0.841345), 2);
            Assert.Equal(+2.0, MathUtility.NormSInv(0.977250), 2);
            Assert.Equal(+3.0, MathUtility.NormSInv(0.998650), 2);
        }

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
