using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
#if true
using DirectType = System.Int32;
#else
using DirectType = System.SByte;
#endif

namespace ShogiCore {
    public unsafe partial class Board {
        /// <summary>
        /// 方向テーブル。
        /// board上のオフセットで表す。
        /// </summary>
        /// <remarks>
        ///   14        -18
        ///   15    -1  -17
        ///   16    0   -16
        ///   17    1   -15
        /// 書き換えれてしまうけど書き換えないように。
        /// </remarks>
#if USE_UNSAFE
        public static readonly DirectType* Direct = UnsafeMemoryStaticBlock.ToUnsafe(new DirectType[16 * 2] {
#else
        public static readonly DirectType[] Direct = (new DirectType[16 * 2] {
#endif
        //   0    1    2    3    4    5   6    7    8    9
        //  左下 下  右下  左   右  左上 上 右上 左上2 右上2
            +17, +1, -15, +16, -16, +15, -1, -17, +14, -18, -999, -999, -999, -999, -999, -999,
            -17, -1, +15, -16, +16, -15, +1, +17, -14, +18, -999, -999, -999, -999, -999, -999,
            // 8,    9,
            // 5, 6, 7,
            // 3,    4,
            // 0, 1, 2,
        });

        /// <summary>
        /// 方向テーブル
        /// </summary>
#if USE_UNSAFE
        public static readonly DirectType* Direct24 = UnsafeMemoryStaticBlock.ToUnsafe(new DirectType[32 * 2] {
#else
        public static readonly DirectType[] Direct24 = (new DirectType[32 * 2] {
#endif
            +17, +1, -15,
            +16,     -16,
            +15, -1, -17,
            +30, +14, -2, -18, -34,
            +31,               -33,
            +32,               -32,
            +33,               -31,
            +34, +18, +2, -14, -30,
            -999, -999, -999, -999, -999, -999, -999, -999,
            // 符号反転
            -17, -1, +15,
            -16,     +16,
            -15, +1, +17,
            -30, -14, +2, +18, +34,
            -31,               +33,
            -32,               +32,
            -33,               +31,
            -34, -18, -2, +14, +30,
            -999, -999, -999, -999, -999, -999, -999, -999,
        });

        /// <summary>
        /// 方向テーブル
        /// </summary>
#if USE_UNSAFE
        public static readonly DirectType* DirectIndexLRSym = UnsafeMemoryStaticBlock.ToUnsafe(new DirectType[24] {
#else
        public static readonly DirectType[] DirectIndexLRSym = (new DirectType[] {
#endif
            0, 1, 0,
            2,    2,
            3, 4, 3,
            5, 6, 7, 6, 5,
            8,          8,
            9,          9,
            10,         10,
            11, 12, 13, 12, 11,
        });

        /// <summary>
        /// Board上の座標値から0～80の値に変換。９一が0、1九が80。
        /// 持ち駒も-1 ～ -7で。+7 すると 0～87の88個になる。
        /// </summary>
#if USE_UNSAFE
        public static readonly sbyte* PositionIndex = UnsafeMemoryStaticBlock.ToUnsafe(new sbyte[BoardLength]);
#else
        public static readonly sbyte[] PositionIndex = (new sbyte[BoardLength]);
#endif
        /// <summary>
        /// PositionIndexの逆変換。+7した値なので注意
        /// </summary>
#if USE_UNSAFE
        public static readonly byte* PositionIndexR = UnsafeMemoryStaticBlock.ToUnsafe(new byte[81 + 7]);
#else
        public static readonly byte[] PositionIndexR = (new byte[81 + 7]);
#endif

        /// <summary>
        /// 位置の差の中央 (同じ位置の場合の値)
        /// </summary>
        public const int PositionDiffCenter = 0x99 - 0x11; // == 0x88
        /// <summary>
        /// 位置の差のテーブルの要素数
        /// </summary>
        public const int PositionDiffRangeCount = PositionDiffCenter * 2 + 1;

        /// <summary>
        /// 相対位置を適当に量子化するテーブル。9*17 → 32
        /// </summary>
#if USE_UNSAFE
        public static readonly sbyte* Relative32Table = UnsafeMemoryStaticBlock.ToUnsafe(new sbyte[9 * 17] {
#else
        public static readonly sbyte[] Relative32Table = (new sbyte[9 * 17] {
#endif
            31, 31, 31, 28, 15,  6,  1,  0,  5, 14, 27, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 29, 16,  7,  2,  3,  4, 13, 26, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 30, 17,  8,  9, 10, 11, 12, 25, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 31, 18, 19, 20, 21, 22, 23, 24, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31,
            31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31,
        });

        /// <summary>
        /// テーブルの初期化
        /// </summary>
        static Board() {
            // 位置indexテーブル
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    int x = 9 - file / 0x10;
                    int y = rank - Padding - 1;
                    int p = x + y * 9;
                    PositionIndex[file + rank] = (sbyte)p;
                    PositionIndexR[p + 7] = (byte)(file + rank);
                }
            }
            PositionIndex[(byte)Piece.FU] = -7;
            PositionIndex[(byte)Piece.KY] = -6;
            PositionIndex[(byte)Piece.KE] = -5;
            PositionIndex[(byte)Piece.GI] = -4;
            PositionIndex[(byte)Piece.KI] = -3;
            PositionIndex[(byte)Piece.KA] = -2;
            PositionIndex[(byte)Piece.HI] = -1;
            PositionIndexR[0] = (byte)Piece.FU;
            PositionIndexR[1] = (byte)Piece.KY;
            PositionIndexR[2] = (byte)Piece.KE;
            PositionIndexR[3] = (byte)Piece.GI;
            PositionIndexR[4] = (byte)Piece.KI;
            PositionIndexR[5] = (byte)Piece.KA;
            PositionIndexR[6] = (byte)Piece.HI;
        }

        /// <summary>
        /// テーブルの初期化
        /// </summary>
        public static int ReadyStatic() {
            return Direct[0];
        }

        /// <summary>
        /// 座標値から筋の取得
        /// </summary>
        public static int GetFile(int pos) {
            return (pos - Board.Padding) >> 4;
        }
        /// <summary>
        /// 座標値から段の取得
        /// </summary>
        public static int GetRank(int pos) {
            return (pos - Board.Padding) & 0x0f;
        }
        /// <summary>
        /// 座標値から段の取得
        /// </summary>
        public static int GetRank(int pos, int turn) {
            int rank = (pos - Board.Padding) & 0x0f;
            int rankT = (10 & (0 - turn)) + ((0 - turn) ^ rank) + turn;
            Debug.Assert(rankT == (turn == 0 ? rank : 10 - rank));
            return rankT;
        }

        /// <summary>
        /// 左右の距離の絶対値を返す。
        /// </summary>
        public static int GetFileDistance(int from, int to) {
            int ff = (from - Board.Padding) >> 4;
            int tf = (to - Board.Padding) >> 4;
            /*
            return Utility.Abs9[tf - ff + 9];
            /*/
            int x = tf - ff;
            int y = (int)x >> 31;
            return (x ^ y) - y;
            //*/
        }
        /// <summary>
        /// 上下の距離を返す。(0 ～ 16、8が同じ段)
        /// 例えば2+8ならfromから2段上にtoが居る。
        /// </summary>
        /// <remarks>
        /// 先手用。後手ならfromとtoを逆に。
        /// 例えば、
        /// GetRankDistance(0x55 + Padding, 0x33 + Padding) == 2 + 8
        /// </remarks>
        public static int GetRankDistance(int from, int to) {
            int fr = (from - Board.Padding) & 0x0f;
            int tr = (to - Board.Padding) & 0x0f;
            return tr - fr + 8;
        }
        /// <summary>
        /// 距離のインデックスを返す。
        /// </summary>
        public static int GetDistanceIndex(int from, int to) {
            //return GetRankDistance(from, to) * 9 + GetFileDistance(from, to);
            // ↓手動で展開してみた

            int fromFile = (from - Board.Padding) >> 4;
            int fromRank = (from - Board.Padding) & 0x0f;
            int toFile = (to - Board.Padding) >> 4;
            int toRank = (to - Board.Padding) & 0x0f;
            /*
            int file = Utility.Abs9[toFile - fromFile + 9];
            /*/
            int x = toFile - fromFile;
            int y = (int)x >> 31;
            int file = (x ^ y) - y;
            //*/
            int rank = toRank - fromRank + 8;
            return rank * 9 + file;
        }
        /// <summary>
        /// 距離のインデックスを返す。
        /// </summary>
        public static int GetDistanceIndex(int fromFile, int fromRank, int toFile, int toRank) {
            /*
            int file = Utility.Abs9[toFile - fromFile + 9];
            /*/
            int x = toFile - fromFile;
            int y = (int)x >> 31;
            int file = (x ^ y) - y;
            //*/
            int rank = toRank - fromRank + 8;
            return rank * 9 + file;
        }

        /// <summary>
        /// 2つの位置の、横か縦の差の大きい方を返す。
        /// </summary>
        /// <param name="a">位置</param>
        /// <param name="b">位置</param>
        /// <returns></returns>
        public static int GetSlippage(int a, int b) {
            a -= Board.Padding;
            b -= Board.Padding;
            return Math.Max(
                Math.Abs(a / 0x10 - b / 0x10),
                Math.Abs((a & 0x0f) - (b & 0x0f)));
        }

        /// <summary>
        /// fromからtoへの方向の値を返す。
        /// </summary>
        public static int GetDirectTo(int from, int to) {
            int d = GetSlippage(from, to);
            if (d == 0) return 0;
            int dir = (to - from) / d;
            return dir;
        }

        /// <summary>
        /// Utility.IndexOf(Board.Direct, direct, 0, 10)
        /// </summary>
        public static int GetDirectIndex(int direct) {
            return Utility.IndexOf(Board.Direct, direct, 0, 10);
        }
        /// <summary>
        /// Utility.IndexOf(Board.Direct, direct, 0, 8)
        /// </summary>
        public static int GetDirectIndex8(int direct) {
            return Utility.IndexOf(Board.Direct, direct, 0, 8);
        }
        /// <summary>
        /// Utility.IndexOf(Board.Direct, direct, 8, 2)
        /// </summary>
        public static int GetDirectIndexKE(int direct) {
            return Utility.IndexOf(Board.Direct, direct, 8, 2);
        }
    }
}
