using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.BoardProperty {
    /// <summary>
    /// そっぽ度判定とか、攻め駒なのか守り駒なのかとか。
    /// </summary>
    /// <remarks>
    /// YSSのテーブルを参考に判定する。
    /// http://www32.ocn.ne.jp/~yss/book.html
    /// </remarks>
    public unsafe class BoardTerritory : IBoardProperty {
        #region テーブル

#if USE_UNSAFE
        static readonly sbyte** AttackTable = UnsafeMemoryStaticBlock.ToUnsafe(new sbyte[17][] {
#else
        static readonly sbyte[][] AttackTable = (new sbyte[17][] {
#endif
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -38, -40, -42, -48, -50, -50, -50, -50, -50 },
            new sbyte[9] { -20, -22, -28, -33, -45, -49, -50, -50, -50 },
            new sbyte[9] {   0,  -1,  -5, -13, -22, -31, -50, -50, -50 },
            new sbyte[9] {  40,  30,  10,   0,  -5, -25, -46, -50, -50 },
            new sbyte[9] {  70,  60,  42,  14,  -2, -20, -38, -45, -50 }, // <--- ２段真上を最大とする。
            new sbyte[9] {  70,  65,  50,  21,  -6, -22, -42, -48, -50 },
            new sbyte[9] {  70,  45,  37,  15,  -9, -25, -43, -50, -50 }, // <--- 中心。左隅(-100, -100)に王がいるとする
            new sbyte[9] {  32,  32,  29,   2, -16, -29, -49, -50, -50 },
            new sbyte[9] {   0,  -3,  -5, -15, -30, -38, -50, -50, -50 },
            new sbyte[9] { -10, -15, -20, -32, -40, -47, -50, -50, -50 },
            new sbyte[9] { -30, -34, -38, -45, -48, -50, -50, -50, -50 },
            new sbyte[9] { -46, -47, -49, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
        });
        // 最小値: -50, 最大値: 70

#if USE_UNSAFE
        static readonly sbyte** DefenseTable = UnsafeMemoryStaticBlock.ToUnsafe(new sbyte[17][] {
#else
        static readonly sbyte[][] DefenseTable = (new sbyte[17][] {
#endif
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -44, -47, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -36, -39, -45, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -21, -23, -30, -35, -46, -49, -50, -50, -50 },
            new sbyte[9] {   0,  -1,  -5, -13, -26, -42, -50, -50, -50 },
            new sbyte[9] {  16,  17,   1,  -5, -12, -33, -46, -50, -50 },
            new sbyte[9] {  31,  29,  24,  14, -10, -29, -41, -49, -50 },
            new sbyte[9] {  37,  38,  32,  16,  -4, -24, -39, -47, -50 },
            new sbyte[9] {  42,  42,  36,  18,  -2, -21, -36, -48, -50 }, // <--- 中心
            new sbyte[9] {  32,  32,  29,   9,  -5, -25, -40, -49, -50 },
            new sbyte[9] {  21,  20,   5,  -3, -16, -34, -46, -50, -50 },
            new sbyte[9] {  -5,  -7, -11, -25, -32, -42, -49, -50, -50 },
            new sbyte[9] { -21, -24, -31, -40, -47, -50, -50, -50, -50 },
            new sbyte[9] { -36, -39, -45, -49, -50, -50, -50, -50, -50 },
            new sbyte[9] { -44, -48, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
            new sbyte[9] { -50, -50, -50, -50, -50, -50, -50, -50, -50 },
        });
        // 最小値: -50, 最大値: 42

        #endregion

        Board board;
        uint[] table = new uint[Board.BoardLength];

        /// <summary>
        /// 座標に対応した値を返す。下から4bitずつ[0-7]で、総合、攻め、守り。
        /// </summary>
        public int GetValue(int pos, int turn) {
            // 下位16bitが先手、上位16bitが後手。
            return (int)((table[pos] >> (turn * 16)) & 0xffff);
        }

        #region IBoardProperty メンバ

        public void Attach(Board board) {
            Debug.Assert(this.board == null);
            this.board = board;
            GenerateTable(board, table);
            board.PreDo += new EventHandler<BoardMoveEventArgs>(board_PreDo);
            board.PostUndo += new EventHandler<BoardMoveEventArgs>(board_PostUndo);
        }

        public void Detach(Board board) {
            board.PreDo -= new EventHandler<BoardMoveEventArgs>(board_PreDo);
            board.PostUndo -= new EventHandler<BoardMoveEventArgs>(board_PostUndo);
            this.board = null;
        }

        public IBoardProperty Clone() {
            var copy = (BoardTerritory)MemberwiseClone();
            copy.board = null;
            Array.Copy(table, copy.table, table.Length);
            return copy;
        }

        #endregion

        #region ICloneable メンバ

        object ICloneable.Clone() {
            return Clone();
        }

        #endregion

        void board_PreDo(object sender, BoardMoveEventArgs e) {
            Debug.Assert(board == e.Board);
            if (e.Move.IsSpecialState) return;
            if (!e.Move.IsPut && (e.Board[e.Move.From] & ~Piece.ENEMY) == Piece.OU) {
                GenerateTable(e.Board, table);
            }
        }

        void board_PostUndo(object sender, BoardMoveEventArgs e) {
            Debug.Assert(board == e.Board);
            if (e.Move.IsSpecialState) return;
            if (!e.Move.IsPut && (e.Board[e.Move.From] & ~Piece.ENEMY) == Piece.OU) {
                GenerateTable(e.Board, table);
            }
        }

        /// <summary>
        /// テーブルの作成
        /// </summary>
        private static void GenerateTable(Board board, uint[] table) {
            int king0 = board.GetKing(0);
            int king1 = board.GetKing(1);
            if (king0 == 0 || king1 == 0) return;
            int rank0 = Board.GetRank(king0);
            int rank1 = Board.GetRank(king1);
            int file0 = Board.GetFile(king0);
            int file1 = Board.GetFile(king1);
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                int x0 = Math.Abs(file / 0x10 - file0);
                int x1 = Math.Abs(file / 0x10 - file1);
                for (int rank = 1 + Board.Padding; rank <= 9 + Board.Padding; rank++) {
                    int y0 = (rank - Board.Padding - rank0) + 8;
                    int y1 = (rank1 - rank + Board.Padding) + 8;
                    int atk0 = AttackTable[y1][x1];
                    int atk1 = AttackTable[y0][x0];
                    int def0 = DefenseTable[y0][x0];
                    int def1 = DefenseTable[y1][x1];
                    // そっぽ度：def+atkの最小値は-100, 最大値は112。これを0-7に変換。
                    int away0 = (def0 + atk0 + 100) * 9 / 256;
                    int away1 = (def1 + atk1 + 100) * 9 / 256;
                    // 攻め駒度：atkの最小値は-50、最大値は70。
                    int ap0 = (atk0 + 50) * 8 / 128;
                    int ap1 = (atk1 + 50) * 8 / 128;
                    // 守り駒度：defの最小値は-50、最大値は42。
                    int dp0 = (def0 + 50) * 11 / 128;
                    int dp1 = (def1 + 50) * 11 / 128;
                    // テーブルへセット。
                    // 下位16bitが先手、上位16bitが後手。
                    table[file + rank] = (uint)(
                        (away0 | (ap0 << 4) | (dp0 << 8)) |
                        ((away1 << 16) | (ap1 << 20) | (dp1 << 24)));
                }
            }
        }
    }
}
