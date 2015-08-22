using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShogiCore.Notation;
using System.Linq;

namespace ShogiCore {
    [TestClass]
    public class BoardTest {
        [TestMethod]
        public void TestIsMate() {
            Board board = Board.FromNotation(
                new SFENNotationReader().Read(
                    @"sfen 4K2r1/2g5g/4ps+r1p/2bsk2ps/l1p1n4/P7P/p1PPP1S2/1PG4b1/L7L b 3PL4p3ng 1")
                    .Single());
            Assert.AreEqual(true, board.IsMate());
        }
    }
}
