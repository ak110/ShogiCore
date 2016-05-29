using System;
using System.Linq;
using Xunit;

namespace ShogiCore.Notation {
    public class SFENNotationReaderTest {
        [Fact]
        public void test1() {
            var data = "1nsR4l/lk1l+R4/1p1G3pp/p1psp1P2/1N4bP1/P1PPP1s1P/1P3+p2+p/LSG6/KNG b 2Pngb";
            Assert.Equal(1, new SFENNotationReader().Read(data).Count());
        }
    }
}
