using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// ハッシュ計算
    /// </summary>
    public static unsafe partial class HashSeed {
        /// <summary>
        /// ハッシュ値の算出
        /// </summary>
        public static ulong CalculateHash(Piece[] board, int turn, int fromFile, int toFile, int fromRank, int toRank) {
            Debug.Assert(1 <= fromRank && fromRank <= toRank && toRank <= 9);
            Debug.Assert(1 <= fromFile && fromFile <= toFile && toFile <= 9);
            ulong hash = HashSeed.TurnSeed[turn];
            for (int file = fromFile * 0x10; file <= toFile * 0x10; file += 0x10) {
                for (int rank = fromRank + Board.Padding; rank <= toRank + Board.Padding; rank++) {
                    hash ^= HashSeed.Seed[(byte)board[file + rank]][file + rank];
                }
            }
            return hash;
        }

        /// <summary>
        /// ハッシュ値の算出
        /// </summary>
        public static ulong CalculateHash(Piece* board, int turn, int fromFile, int toFile, int fromRank, int toRank) {
            Debug.Assert(1 <= fromRank && fromRank <= toRank && toRank <= 9);
            Debug.Assert(1 <= fromFile && fromFile <= toFile && toFile <= 9);
            ulong hash = HashSeed.TurnSeed[turn];
            for (int file = fromFile * 0x10; file <= toFile * 0x10; file += 0x10) {
                for (int rank = fromRank + Board.Padding; rank <= toRank + Board.Padding; rank++) {
                    hash ^= HashSeed.Seed[(byte)board[file + rank]][file + rank];
                }
            }
            return hash;
        }
    }
}
