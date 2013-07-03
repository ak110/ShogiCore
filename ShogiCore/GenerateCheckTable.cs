using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// 王手生成の為のテーブル
    /// </summary>
    public static unsafe class GenerateCheckTable {
        /// <summary>
        /// 相対位置の値を生成して返す
        /// </summary>
        public static sbyte _POS(int fileDiff, int rankDiff) { return (sbyte)(fileDiff * 0x10 + rankDiff); }


        /// <summary>
        /// 駒打ちによる王手用テーブル
        /// </summary>
        /// <remarks>
        /// 飛角香でない持ち駒
        /// 歩銀金
        /// 歩は打ち歩詰/二歩のチェックが必要
        /// +------+------+------+------+------+
        /// |      |CanGo3|CanGo2|CanGo1|      |
        /// +------+------+------+------+------+
        /// |      |CanGo6|  王  |CanGo4|      |
        /// +------+------+------+------+------+
        /// |      |CanGo9|CanGo8|CanGo7|      |
        /// +------+------+------+------+------+
        /// CanGo13 　銀
        /// CanGo2  金
        /// CanGo46 金
        /// CanGo8  金銀歩
        /// CanGo79 金銀
        /// </remarks>
        public static readonly sbyte[][] Put = new sbyte[(byte)Piece.HI + 1][] { // 先手
            null, // 空
            new sbyte[] { _POS(0, 1), }, // 歩
            new sbyte[] { _POS(0, 1), }, // 香
            new sbyte[] { _POS(1, 2), _POS(-1, 2), }, // 桂
            new sbyte[] { _POS(0, 1), _POS(-1, 1), _POS(1, 1), _POS(-1, -1), _POS(1, -1), }, // 銀
            new sbyte[] { _POS(0, 1), _POS(-1, 1), _POS(1, 1), _POS(-1, 0), _POS(1, 0), _POS(0, -1), }, // 金
            new sbyte[] { _POS(1, 1), _POS(1, -1), _POS(-1, 1), _POS(-1, -1), }, // 角
            new sbyte[] { _POS(0, 1), _POS(0, -1), _POS(1, 0), _POS(-1, 0), }, // 飛
        };

        /// <summary>
        /// 移動元リスト
        /// </summary>
        public static readonly sbyte[] MoveFrom = new sbyte[5 * 5 - 2] {
            _POS(-2, -2), _POS(-1, -2), _POS(0, -2), _POS(1, -2), _POS(2, -2),
            _POS(-2, -1), _POS(-1, -1), _POS(0, -1), _POS(1, -1), _POS(2, -1),
            _POS(-2, +0), _POS(-1, +0), /*       ,*/ _POS(1, +0), _POS(2, +0),
            _POS(-2, +1), _POS(-1, +1), /*       ,*/ _POS(1, +1), _POS(2, +1),
            _POS(-2, +2), _POS(-1, +2), _POS(0, +2), _POS(1, +2), _POS(2, +2),
        };

        /// <summary>
        /// 移動王手用データ
        /// </summary>
        //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        public struct CheckMove {
#if DEBUG
            public readonly sbyte From;
#endif
            public readonly sbyte To;
            public readonly Piece Promote;
            public CheckMove(int from, int to, Piece promote) {
#if DEBUG
                From = (sbyte)from;
#endif
                To = (sbyte)to;
                Promote = promote;
            }
        }

        /// <summary>
        /// 移動王手用テーブル
        /// </summary>
        /// <remarks>
        /// 飛角香でない駒による王手
        /// 王の周りをまず見て、王手となる位置に進めることを考える
        ///
        /// +------+------+------+------+------+
        /// |      |CanGo3|CanGo2|CanGo1|      |
        /// +------+------+------+------+------+
        /// |      |CanGo6|  王  |CanGo4|      |
        /// +------+------+------+------+------+
        /// |      |CanGo9|CanGo8|CanGo7|      |
        /// +------+------+------+------+------+
        /// |      | 桂馬 |      | 桂馬 |      |
        /// +------+------+------+------+------+
        /// CanGo13 銀                     角 馬竜
        /// CanGo2  金 成                  飛 馬竜
        /// CanGo46 金 成                  飛 馬竜
        /// CanGo8  桂馬以外、成           飛 馬竜
        /// CanGo79 金銀 成                角 馬竜
        ///
        /// +------+------+------+------+------+
        /// |銀不成|銀成  |金銀不|銀成  |銀不成|
        /// +------+------+------+------+------+
        /// |銀成竜|金    |  銀  |金    |銀成竜|
        /// +------+------+------+------+------+
        /// |金銀不|銀    |  王  |銀    |金銀不(7→1)|
        /// +------+------+------+------+------+
        /// |金銀成|歩桂成|      |歩桂成|金銀成|
        /// +------+------+------+------+------+
        /// |金銀桂|全部89|zenbu |全部78|金銀　桂成
        /// +------+------+------+------+------+
        /// |桂馬成|桂馬成|桂馬成|桂馬成|桂馬成|
        /// +------+------+------+------+------+
        /// |桂馬不|      |桂馬不|      |桂馬不|
        /// +------+------+------+------+------+
        ///
        /// 龍と馬が飛び道具でない方向に寄っていくことによる王手もここで処理する。
        /// </remarks>
        public static readonly CheckMove[][][] Move = new CheckMove[16 * 5 - 3 - 8][][] {
            new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], null, null, null, null, null, null, null, null, null, null, null, 
            new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], null, null, null, null, null, null, null, null, null, null, null, 
            new CheckMove[16][], new CheckMove[16][], null, null, new CheckMove[16][], null, null, null, null, null, null, null, null, null, null, null, 
            new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], null, null, null, null, null, null, null, null, null, null, null, 
            new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], new CheckMove[16][], //null, null, null, null, null, null, null, null, null, null, null,
        };
        /// <summary>
        /// 移動王手テーブルの位置参照用オフセット
        /// </summary>
        public const int MoveTableOffset = 16 * 2 + 2;
        /// <summary>
        /// 桂馬王手用テーブル(王の3段下用)
        /// </summary>
        public static readonly CheckMove[][] MoveKE3 = new CheckMove[5][] {
            new CheckMove[1] { new CheckMove(_POS(-2, 3), _POS(-1, 1), Piece.PROMOTED) },
            new CheckMove[1] { new CheckMove(_POS(-1, 3), _POS( 0, 1), Piece.PROMOTED) },
            new CheckMove[2] { new CheckMove(_POS( 0, 3), _POS(-1, 1), Piece.PROMOTED),
                               new CheckMove(_POS( 0, 3), _POS( 1, 1), Piece.PROMOTED) },
            new CheckMove[1] { new CheckMove(_POS( 1, 3), _POS( 0, 1), Piece.PROMOTED) },
            new CheckMove[1] { new CheckMove(_POS( 2, 3), _POS( 1, 1), Piece.PROMOTED) },
        };
        /// <summary>
        /// 桂馬王手テーブル(王の4段下用)
        /// </summary>
        public static readonly CheckMove[][] MoveKE4 = new CheckMove[5][] {
            new CheckMove[1] { new CheckMove(_POS(-2, 4), _POS(-1, 2), Piece.EMPTY) },
            null,
            new CheckMove[2] { new CheckMove(_POS( 0, 4), _POS( 1, 2), Piece.EMPTY),
                               new CheckMove(_POS( 0, 4), _POS(-1, 2), Piece.EMPTY) },
            null,
            new CheckMove[1] { new CheckMove(_POS( 2, 4), _POS( 1, 2), Piece.EMPTY) },
        };

        /// <summary>
        /// 初期化用定数
        /// </summary>
        const int basePos = 0x55 + Board.Padding;

        /// <summary>
        /// テーブルの初期化
        /// </summary>
        static GenerateCheckTable() {
            // 成る移動王手
            foreach (Piece p in new[] { Piece.TO, Piece.NK, Piece.NG }) {
                foreach (var dir2 in PieceUtility.CanMoveDirects[(byte)p]) {
                    int to = basePos - Board.Direct[dir2];
                    Piece d = p & ~Piece.PROMOTED;
                    foreach (var dir1 in PieceUtility.CanMoveDirects[(byte)d]) {
                        int from = to - Board.Direct[dir1];
                        if (3 <= Board.GetRank(from) - 5) { // 桂馬の王手ははみ出る分は別処理
                            Debug.Assert(p == Piece.NK, "コーディングミス？");
                            continue;
                        }
                        AddMove(d, to, from, Piece.PROMOTED);
                    }
                }
            }
            // 成らない移動王手
            foreach (Piece p in new[] { Piece.FU, Piece.KY, Piece.GI, Piece.KI,
                Piece.TO, Piece.NY, Piece.NK, Piece.NG, Piece.UM, Piece.RY }) {
                foreach (var dir2 in PieceUtility.CanMoveDirects[(byte)p]) {
                    int to = basePos - Board.Direct[dir2];
                    foreach (var dir1 in PieceUtility.CanMoveDirects[(byte)p]) {
                        int from = to - Board.Direct[dir1];
                        AddMove(p, to, from, Piece.EMPTY);
                    }
                }
            }
            // 一応nullは長さ0で埋め。
            foreach (var table in Move) {
                if (table == null) continue;
                for (int i = 0; i < table.Length; i++) {
                    if (table[i] == null) {
                        table[i] = new CheckMove[0];
                    }
                }
            }
        }

        /// <summary>
        /// 追加。
        /// </summary>
        private static void AddMove(Piece p, int to, int from, Piece promote) {
            if (from == basePos) return; // 王の位置と同じfromは除外
            foreach (var dir in PieceUtility.CanMoveDirects[(byte)p]) { // 王手になっちゃってるfromも除外
                int controlTo = from + Board.Direct[dir];
                if (controlTo == basePos) return;
            }

            CheckMove[] table = Move[from - basePos + MoveTableOffset][(byte)p];
            Array.Resize(ref table, table == null ? 1 : table.Length + 1); // 手抜き
            table[table.Length - 1] = new CheckMove(from - basePos, to - basePos, promote);
            Move[from - basePos + MoveTableOffset][(byte)p] = table;
        }


        /// <summary>
        /// テーブルの初期化
        /// </summary>
        public static int ReadyStatic() {
            return Put[1][0];
        }
    }
}
