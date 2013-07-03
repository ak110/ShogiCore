using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ShogiCore {
    public unsafe partial class Board {
        /// <summary>
        /// 合法手の最大数
        /// </summary>
        public const int MaxLegalMoves = 593;

        /// <summary>
        /// board.Length。
        /// </summary>
        public const int BoardLength = 16 * (9 + 4);
        /// <summary>
        /// 番兵の関係でずらす量。これ＋0x11が１一になる。
        /// </summary>
        public const int Padding = 0x13;
        /// <summary>
        /// index = BoardCenter - index で上下反転をするための値。
        /// </summary>
        public const int BoardCenter = 0xaa + Padding * 2;

        /// <summary>
        /// 移動利きのマスク
        /// </summary>
        public const int ControlMaskMove = (1 << 10) - 1; // 0x000003ff
        /// <summary>
        /// 飛び利きのマスク
        /// </summary>
        public const int ControlMaskJump = ControlMaskJumpOrtho | ControlMaskJumpDiag; // 0x0003fc00
        /// <summary>
        /// 縦横の飛び利きのマスク
        /// </summary>
        public const int ControlMaskMoveOrtho = 0x0000005a;
        /// <summary>
        /// 斜めの飛び利きのマスク
        /// </summary>
        public const int ControlMaskMoveDiag = 0x000000a5;
        /// <summary>
        /// 縦横の飛び利きのマスク
        /// </summary>
        public const int ControlMaskJumpOrtho = ControlMaskMoveOrtho << 10; // 0x00016800
        /// <summary>
        /// 斜めの飛び利きのマスク
        /// </summary>
        public const int ControlMaskJumpDiag = ControlMaskMoveDiag << 10; // 0x00029400
        /// <summary>
        /// 上以外の縦横の飛び利きのマスク
        /// </summary>
        public const int ControlMaskOrthoWithoutUp = ControlMaskJumpOrtho & ~(1 << 16); // 0x00006800;

        #region 番兵の少ないバージョン

        /// <summary>
        /// board.Length。
        /// </summary>
        public const int BoardLengthW1 = 16 * (9 + 2);
        /// <summary>
        /// 番兵の関係でずらす量
        /// </summary>
        public const int PaddingW1 = 1;
        /// <summary>
        /// PaddingW1とPaddingW2の変換
        /// </summary>
        public const int PaddingW2ToW1 = -Padding + PaddingW1;

        /// <summary>
        /// board.Length。
        /// </summary>
        public const int BoardLengthW0 = 0x99 - 0x11 + 1; // == 0x89 < 16 * 9 == 0x90
        /// <summary>
        /// 番兵の関係でずらす量
        /// </summary>
        public const int PaddingW0 = -0x11;
        /// <summary>
        /// PaddingW0とPaddingW1の変換
        /// </summary>
        public const int PaddingW1ToW0 = -PaddingW1 + PaddingW0;
        /// <summary>
        /// PaddingW0とPaddingW2の変換
        /// </summary>
        public const int PaddingW2ToW0 = -Padding + PaddingW0;

        #endregion

        /// <summary>
        /// unsafeメモリのサイズ。
        /// </summary>
        static int UnsafeMemorySize {
            get {
                return 0
                    + sizeof(Piece) * BoardLength // board
                    + sizeof(uint) * 2 // HandValues
                    + sizeof(int*) * 2 // control
                    + sizeof(int) * BoardLength * 2 // control[0], control[1]
                    + sizeof(bool) * 2 * 16 // existsFU
                    ;
                // 何故かconstに出来んのでプロパティ。(´ω`)
            }
        }

        /// <summary>
        /// 段の反転
        /// </summary>
        public static int RankReverse(int rank, int turn) {
            Debug.Assert(1 <= rank && rank <= 9);
            Debug.Assert(turn == 0 || turn == 1);
            int n = (10 & (0 - turn)) + ((0 - turn) ^ rank) + turn;
            Debug.Assert(n == (turn == 0 ? rank : 10 - rank));
            return n;
        }

        /// <summary>
        /// turnが0ならvalue、1なら-valueを返す。
        /// </summary>
        public static int NegativeByTurn(int value, int turn) {
            //return turn == 0 ? value : -value;

            Debug.Assert(turn == 0 || turn == 1);
            int n = ((0 - turn) ^ value) + turn;
            Debug.Assert(n == (turn == 0 ? value : -value));
            return n;
        }
    }
}
