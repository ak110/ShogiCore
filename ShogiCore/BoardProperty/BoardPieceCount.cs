using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.BoardProperty {
    /// <summary>
    /// 盤上の駒の個数の差分計算
    /// </summary>
    public unsafe class BoardPieceCount : IBoardProperty {
        Board board;
        sbyte[] values = new sbyte[2 * 16]; // 先手後手の盤上の駒の数

        /// <summary>
        /// 直接取得
        /// </summary>
        public sbyte[] DangerousGetPtr() { return values; }

        #region IBoardProperty メンバ

        public void Attach(Board board) {
            Debug.Assert(this.board == null);
            this.board = board;
            GetValues(board, values);
            board.PreDo += new EventHandler<BoardMoveEventArgs>(board_PreDo);
#if DEBUG
            board.PostDo += new EventHandler<BoardMoveEventArgs>(board_PostDo);
#endif
            board.PostUndo += new EventHandler<BoardMoveEventArgs>(board_PostUndo);
        }

        public void Detach(Board board) {
            board.PreDo -= new EventHandler<BoardMoveEventArgs>(board_PreDo);
#if DEBUG
            board.PostDo -= new EventHandler<BoardMoveEventArgs>(board_PostDo);
#endif
            board.PostUndo -= new EventHandler<BoardMoveEventArgs>(board_PostUndo);
            this.board = null;
        }

        public IBoardProperty Clone() {
            var copy = (BoardPieceCount)MemberwiseClone();
            copy.board = null;
            Array.Copy(values, copy.values, values.Length);
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
            CalcDiff(e.Board, e.Move, values, +1);
        }

        void board_PostDo(object sender, BoardMoveEventArgs e) {
            Debug.Assert(board == e.Board);
            Debug.Assert(Utility.IsMatchAll(values, GetValues(board)));
        }

        void board_PostUndo(object sender, BoardMoveEventArgs e) {
            Debug.Assert(board == e.Board);
            if (e.Move.IsSpecialState) return;
            CalcDiff(e.Board, e.Move, values, -1);
            Debug.Assert(Utility.IsMatchAll(values, GetValues(board)));
        }

        /// <summary>
        /// デバッグ用計算
        /// </summary>
        private static sbyte[] GetValues(Board board) {
            sbyte[] values = new sbyte[2 * 16]; // 先手後手の盤上の駒の数
            GetValues(board, values);
            return values;
        }
        /// <summary>
        /// 初期計算
        /// </summary>
        private static void GetValues(Board board, sbyte[] values) {
            Array.Clear(values, 0, values.Length);
            var bp = board.DangerousGetPtr();
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Board.Padding; rank <= 9 + Board.Padding; rank++) {
                    Piece p = bp[file + rank];
                    int turn = ((byte)p >> PieceUtility.EnemyShift) & 1;
                    values[turn * 16 + (byte)(p & ~Piece.ENEMY)]++;
                }
            }
        }

        /// <summary>
        /// 差分計算
        /// </summary>
        /// <returns>差分</returns>
        private static void CalcDiff(Board board, Move move, sbyte[] values, sbyte sign) {
            if (move.IsPut) {
                values[board.Turn * 16 + (byte)move.PutPiece] += sign;
                values[0 * 16 + (byte)Piece.EMPTY] -= sign;
            } else {
                if (move.IsPromote) {
                    Piece movePiece = board[move.From];
                    values[board.Turn * 16 + (byte)(movePiece & ~Piece.ENEMY)] -= sign;
                    values[board.Turn * 16 + (byte)(movePiece & ~Piece.ENEMY | Piece.PROMOTED)] += sign;
                }
                if (move.IsCapture) {
                    values[(board.Turn ^ 1) * 16 + (byte)(move.Capture & ~Piece.ENEMY)] -= sign;
                    values[0 * 16 + (byte)Piece.EMPTY] += sign;
                }
            }
        }
    }
}
