
//#define USE_DIFFSTACK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.BoardProperty {
    /// <summary>
    /// 進行度を算出するクラス。
    /// </summary>
    /// <remarks>
    /// http://chocobo.yasuda-u.ac.jp/~nisimura/mymove/index.cgi?no=749
    /// min(40, max(先手の平均段, 後手の平均段) * 10 + max(先手の持駒は何種類, 後手の持駒は何種類)) 
    /// </remarks>
    [DebuggerDisplay("Stage = {StageValue}")]
    public unsafe class BoardStage : IBoardProperty {
        /// <summary>
        /// 進行度の最大値
        /// </summary>
        public const int MaxStage = 16;

        Board board;
        int[] values = new int[7];
#if USE_DIFFSTACK
        Stack<int[]> diffStack = new Stack<int[]>(Blunder.Search.GameTree.MaxPly);
#endif

        /// <summary>
        /// 進行度。
        /// </summary>
        public int StageValue {
            get { return values[6]; }
        }

        /// <summary>
        /// 進行度によって値を線形に変化させる
        /// </summary>
        /// <param name="valueO">序盤の値</param>
        /// <param name="valueE">終盤の値</param>
        /// <returns>変化後の値</returns>
        public int InterpolateLiner(int valueO, int valueE) {
            return InterpolateLiner(valueO, valueE, StageValue);
        }
        /// <summary>
        /// 進行度によって値を線形に変化させる
        /// </summary>
        /// <param name="valueO">序盤の値</param>
        /// <param name="valueE">終盤の値</param>
        /// <param name="stage">進行度</param>
        /// <returns>変化後の値</returns>
        public static int InterpolateLiner(int valueO, int valueE, int stage) {
            // (valueO * (MaxStage - stage) + valueE * stage) / MaxStage
            // == valueO + (valueE - valueO) * stage / MaxStage;
            int value = valueO + (valueE - valueO) * stage / MaxStage;
            return value;
        }

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
            var copy = (BoardStage)MemberwiseClone();
            copy.board = null;
            Array.Copy(values, copy.values, values.Length);
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
            int* diff = stackalloc int[6];
            GetDiff(e.Board, e.Move, diff);
            values[0] += diff[0];
            values[1] += diff[1];
            values[2] += diff[2];
            values[3] += diff[3];
            values[4] += diff[4];
            values[5] += diff[5];
            values[6] = GetStage(values);
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
            int* diff = stackalloc int[6];
            GetDiff(e.Board, e.Move, diff);
            values[0] -= diff[0];
            values[1] -= diff[1];
            values[2] -= diff[2];
            values[3] -= diff[3];
            values[4] -= diff[4];
            values[5] -= diff[5];
            values[6] = GetStage(values);
#endif

            Debug.Assert(Utility.IsMatchAll(values, GetValues(board)));
        }

        /// <summary>
        /// デバッグ用計算
        /// </summary>
        private static int[] GetValues(Board board) {
            var values = new int[7];
            GetValues(board, values);
            return values;
        }
        /// <summary>
        /// 初期値の算出
        /// </summary>
        private static void GetValues(Board board, int[] values) {
            Array.Clear(values, 0, values.Length); // 段の合計、駒数、持ち駒種類数、stage用領域(適当)
            // 平均段の算出
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Board.Padding; rank <= 9 + Board.Padding; rank++) {
                    Piece p = board[file + rank];
                    if (p == Piece.EMPTY) continue; // ←書いた方が早い？
                    if (PieceUtility.IsSecondTurn(p)) { // 敵駒
                        values[1] += rank - Board.Padding;
                        values[3]++;
                    } else { // 自駒
                        values[0] += 10 - (rank - Board.Padding); 
                        values[2]++;
                    }
                }
            }
            // 持ち駒
            uint hand0 = board.HandValues[0];
            uint hand1 = board.HandValues[1];
            if ((hand0 & Board.HandValueMaskFU) != 0) values[4]++;
            if ((hand0 & Board.HandValueMaskKY) != 0) values[4]++;
            if ((hand0 & Board.HandValueMaskKE) != 0) values[4]++;
            if ((hand0 & Board.HandValueMaskGI) != 0) values[4]++;
            if ((hand0 & Board.HandValueMaskKI) != 0) values[4]++;
            if ((hand0 & Board.HandValueMaskKA) != 0) values[4]++;
            if ((hand0 & Board.HandValueMaskHI) != 0) values[4]++;
            if ((hand1 & Board.HandValueMaskFU) != 0) values[5]++;
            if ((hand1 & Board.HandValueMaskKY) != 0) values[5]++;
            if ((hand1 & Board.HandValueMaskKE) != 0) values[5]++;
            if ((hand1 & Board.HandValueMaskGI) != 0) values[5]++;
            if ((hand1 & Board.HandValueMaskKI) != 0) values[5]++;
            if ((hand1 & Board.HandValueMaskKA) != 0) values[5]++;
            if ((hand1 & Board.HandValueMaskHI) != 0) values[5]++;
            values[6] = GetStage(values);
        }

        /// <summary>
        /// 差分計算
        /// </summary>
        /// <returns>差分</returns>
        private static void GetDiff(Board board, Move move, int* values) {
            Utility.ZeroMemory(values, sizeof(int) * 6); // 段の合計、駒数、持ち駒種類数
            int turn = board.Turn;
            if (move.IsPut) {
                // 持ち駒減少
                if (board.GetHand(turn, move.PutPiece) == 1) {
                    values[4 + turn]--;
                }
                // 段の加算
                int toRank = Board.GetRank(move.To, turn ^ 1);
                values[0 + turn] += toRank;
                values[2 + turn]++;
            } else {
                // 段の加減
                int fromRank = Board.GetRank(move.From, turn ^ 1);
                values[0 + turn] -= fromRank;

                int toRank = Board.GetRank(move.To, turn ^ 1);
                values[0 + turn] += toRank;

                if (move.IsCapture) {
                    // 持ち駒増加
                    if (board.GetHand(turn, move.Capture & ~Piece.PE) == 0) {
                        values[4 + turn]++;
                    }
                    // 段の減算
                    values[0 + (turn ^ 1)] -= 10 - toRank;
                    values[2 + (turn ^ 1)]--;
                }
            }
        }

        /// <summary>
        /// 進行度の算出
        /// </summary>
        private static int GetStage(int[] values) {
            int rank = Math.Max(
                values[2] == 0 ? 0 : values[0] * 8 / values[2],
                values[3] == 0 ? 0 : values[1] * 8 / values[3]);
            int hand = Math.Max(values[4], values[5]);
            int stage = Math.Min(4 * 8, rank) + hand;
            return Math.Min(Math.Max(stage - 16, 0), 16);
        }
    }
}
