
//#define USE_DIFFSTACK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.BoardProperty {
    /// <summary>
    /// 入玉判定に使う駒の個数・ポイントの算出処理
    /// </summary>
    public unsafe class BoardPoint : IBoardProperty {
        Board board;
        sbyte[] values = new sbyte[4]; // values[turn * 2 + 0] : 駒個数、values[turn * 2 + 1] : ポイント
        sbyte[] diff = new sbyte[4];
#if USE_DIFFSTACK
        Stack<sbyte[]> diffStack = new Stack<sbyte[]>(Blunder.Search.GameTree.MaxPly);
#endif

        /// <summary>
        /// 駒の個数
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public int GetCount(int turn) { return values[turn * 2 + 0]; }

        /// <summary>
        /// 駒のポイント
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public int GetPoint(int turn) { return values[turn * 2 + 1]; }

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
            var copy = (BoardPoint)MemberwiseClone();
            copy.board = null;
            copy.values = Utility.Clone(values);
#if USE_DIFFSTACK
            copy.diffStack = Utility.Clone(diffStack);
#endif
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
#if USE_DIFFSTACK
            diffStack.Push(Utility.Clone(values));
#endif
            GetDiff(e.Board, e.Move, diff);
            values[0 * 2 + 0] += diff[0 * 2 + 0];
            values[0 * 2 + 1] += diff[0 * 2 + 1];
            values[1 * 2 + 0] += diff[1 * 2 + 0];
            values[1 * 2 + 1] += diff[1 * 2 + 1];
        }

        void board_PostDo(object sender, BoardMoveEventArgs e) {
            Debug.Assert(board == e.Board);
            Debug.Assert(Utility.IsMatchAll(values, GetValues(board)));
        }

        void board_PostUndo(object sender, BoardMoveEventArgs e) {
            Debug.Assert(board == e.Board);
            if (e.Move.IsSpecialState) return;
#if USE_DIFFSTACK
            values = diffStack.Pop();
#else
            GetDiff(e.Board, e.Move, diff);
            values[0 * 2 + 0] -= diff[0 * 2 + 0];
            values[0 * 2 + 1] -= diff[0 * 2 + 1];
            values[1 * 2 + 0] -= diff[1 * 2 + 0];
            values[1 * 2 + 1] -= diff[1 * 2 + 1];
#endif

            Debug.Assert(Utility.IsMatchAll(values, GetValues(board)));
        }

        /// <summary>
        /// デバッグ用計算
        /// </summary>
        private static sbyte[] GetValues(Board board) {
            sbyte[] values = new sbyte[4];
            GetValues(board, values);
            return values;
        }

        /// <summary>
        /// 終盤っぽさを1～16で返す。[turn ^ 0]が敵陣で[turn ^ 1]が自陣。
        /// </summary>
        private static void GetValues(Board board, sbyte[] values) {
            Array.Clear(values, 0, values.Length);
            // 攻め駒・守り駒
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Board.Padding; rank <= 3 + Board.Padding; rank++) {
                    Piece p = board[file + rank];
                    if (PieceUtility.IsFirstTurn(p) && p != Piece.OU) {
                        values[0 * 2 + 0]++;
                        values[0 * 2 + 1] += PieceUtility.PointTable[(byte)p];
                    }
                }
                for (int rank = 7 + Board.Padding; rank <= 9 + Board.Padding; rank++) {
                    Piece p = board[file + rank];
                    if (PieceUtility.IsSecondTurn(p) && p != Piece.EOU) {
                        values[1 * 2 + 0]++;
                        values[1 * 2 + 1] += PieceUtility.PointTable[(byte)(p & ~Piece.ENEMY)];
                    }
                }
            }
            // 持ち駒
            for (int t = 0; t < 2; t++) {
                uint handValue = board.HandValues[t];
                values[t * 2 + 1] += (sbyte)(
                    ((handValue >> Board.HandValueShiftFU) & Board.HandValueShiftedMaskFU) +
                    ((handValue >> Board.HandValueShiftKY) & Board.HandValueShiftedMaskKY) +
                    ((handValue >> Board.HandValueShiftKE) & Board.HandValueShiftedMaskKE) +
                    ((handValue >> Board.HandValueShiftGI) & Board.HandValueShiftedMaskGI) +
                    ((handValue >> Board.HandValueShiftKI) & Board.HandValueShiftedMaskKI) +
                    (((handValue >> Board.HandValueShiftKA) & Board.HandValueShiftedMaskKA) +
                    ((handValue >> Board.HandValueShiftHI) & Board.HandValueShiftedMaskHI)) * 5);
            }
        }

        /// <summary>
        /// 差分計算
        /// </summary>
        /// <returns>差分</returns>
        private static void GetDiff(Board board, Move move, sbyte[] values) {
            Array.Clear(values, 0, values.Length);

            int turn = board.Turn;
            if (move.IsPut) {
                sbyte pointDiff = PieceUtility.PointTable[(byte)move.PutPiece];
                // 持ち駒減少
                values[turn * 2 + 1] -= pointDiff;
                // 得点加算
                int toRank = Board.GetRank(move.To, turn);
                if (toRank <= 3) {
                    values[turn * 2 + 0]++;
                    values[turn * 2 + 1] += pointDiff;
                }
            } else {
                Piece p = (board[move.From] & ~Piece.ENEMY);
                bool isKing = p == Piece.OU;
                sbyte pointDiff = PieceUtility.PointTable[(byte)p];
                int fromRank = Board.GetRank(move.From, turn);
                if (fromRank <= 3 && !isKing) {
                    values[turn * 2 + 0]--;
                    values[turn * 2 + 1] -= pointDiff;
                }

                int toRank = Board.GetRank(move.To, turn);
                if (toRank <= 3 && !isKing) {
                    values[turn * 2 + 0]++;
                    values[turn * 2 + 1] += pointDiff;
                }

                if (move.IsCapture) {
                    // 持ち駒増加
                    Piece capturePlain = move.Capture & ~Piece.PE;
                    sbyte capturePoint = (sbyte)PieceUtility.PointTable[(byte)capturePlain];
                    values[turn * 2 + 1] += capturePoint;
                    // 得点減算
                    if (7 <= toRank) {
                        values[(turn ^ 1) * 2 + 0]--;
                        values[(turn ^ 1) * 2 + 1] -= capturePoint;
                    }
                }
            }
        }
    }
}
