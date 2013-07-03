using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
  /// <summary>
  /// Pieceの配列に対する処理とか。
  /// </summary>
  public static unsafe partial class BoardUtility {
        /// <summary>
        /// スタート位置からまっすぐ検索
        /// </summary>
        public static int SearchNotEmpty(Piece* board, int start, int direct) {
            for (int pos = start + direct; ; pos += direct) {
                if (board[pos] != Piece.EMPTY) return pos;
            }
        }

        /// <summary>
        /// 二歩ならtrue
        /// </summary>
        public static bool ExistsFU(Piece* board, int turn, int filex10) {
            if (turn == 0) {
                for (int rank = 2 + Board.Padding; rank <= 9 + Board.Padding; rank++) {
                    if (board[filex10 + rank] == Piece.FU) {
                        return true;
                    }
                }
            } else {
                for (int rank = 1 + Board.Padding; rank <= 8 + Board.Padding; rank++) {
                    if (board[filex10 + rank] == Piece.EFU) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// その地点への利きの有無だけを調べる
        /// </summary>
        /// <param name="turn">0なら先手、1なら後手</param>
        public static bool ControlExists(Piece* board, int turn, int pos) {
            if (board[pos] == Piece.WALL) {
                return false;
            }
            for (int dir = 0; dir < 8; dir++) {
                int direct = -Board.Direct[turn * 16 + dir];
                Piece p = board[pos + direct];
                if (p == Piece.EMPTY) {
                    //飛び道具の利きをチェック
                    p = board[SearchNotEmpty(board, pos, direct)];
                    if (PieceUtility.IsSelf(p, turn) && PieceUtility.CanJump(p, dir)) {
                        return true;
                    }
                } else if (PieceUtility.IsSecondTurn(p) == (turn != 0)) {
                    if (PieceUtility.CanJump(p, dir)) {
                        return true;
                    } else if (PieceUtility.CanMove(p, dir)) {
                        return true;
                    }
                }
            }
            for (int dir = 8; dir < 10; dir++) {
                int direct = -Board.Direct[turn * 16 + dir];
                Piece p = board[pos + direct];
                if (PieceUtility.IsSelf(p, turn) && PieceUtility.CanMove(p, dir)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// その地点への利きを調べる
        /// </summary>
        /// <param name="turn">0なら先手、1なら後手</param>
        public static int GetControl(Piece* board, int turn, int pos) {
            if (board[pos] == Piece.WALL) {
                return 0;
            }
            int result = 0;
            for (int dir = 0; dir < 8; dir++) {
                int direct = -Board.Direct[turn * 16 + dir];
                Piece p = board[pos + direct];
                if (p == Piece.EMPTY) {
                    //飛び道具の利きをチェック
                    p = board[SearchNotEmpty(board, pos, direct)];
                    if (PieceUtility.IsSelf(p, turn) && PieceUtility.CanJump(p, dir)) {
                        result |= (1 << (dir + 10));
                    }
                } else if (PieceUtility.IsSecondTurn(p) == (turn != 0)) {
                    if (PieceUtility.CanJump(p, dir)) {
                        result |= (1 << (dir + 10));
                    } else if (PieceUtility.CanMove(p, dir)) {
                        result |= (1 << dir);
                    }
                }
            }
            for (int dir = 8; dir < 10; dir++) {
                int direct = -Board.Direct[turn * 16 + dir];
                Piece p = board[pos + direct];
                if (PieceUtility.IsSelf(p, turn) && PieceUtility.CanMove(p, dir)) {
                    result |= (1 << dir);
                }
            }
            return result;
        }

        /// <summary>
        /// 手の適用
        /// </summary>
        /// <param name="move">手</param>
        public static void Do(Piece* board, Move move, int turn) {
            if (move.IsSpecialState) return;
            if (move.IsPut) {
                //駒を打つ
                Debug.Assert(board[move.To] == Piece.EMPTY);
                board[move.To] = PieceUtility.SelfOrEnemy[turn * 32 + (byte)move.PutPiece];
            } else {
                //盤上の駒を移動
                Debug.Assert(board[move.To] == move.Capture); // 移動先合ってる？
                Debug.Assert(PieceUtility.IsSelf(board[move.From], turn), "移動元が自駒以外"); // 移動元あってる？
                Debug.Assert(PieceUtility.CanPromote(board[move.From]) || !move.IsPromote, "成れない駒を成る手？");
                //move.Capture = board[move.To]; //移動先の駒を記憶
                board[move.To] = board[move.From] | move.Promote;
                board[move.From] = Piece.EMPTY;
            }
        }

        /// <summary>
        /// 手の適用解除
        /// </summary>
        /// <param name="move">手</param>
        public static void Undo(Piece* board, Move move, int turn) {
            if (move.IsSpecialState) return;
            if (move.IsPut) {
                //駒を打つ
                Debug.Assert(board[move.To] == PieceUtility.SelfOrEnemy[turn * 32 + (byte)move.PutPiece]);
                board[move.To] = Piece.EMPTY;
            } else {
                //盤上の駒を移動
                Debug.Assert(board[move.From] == Piece.EMPTY); // 移動元合ってる？
                board[move.From] = board[move.To] ^ move.Promote;
                board[move.To] = move.Capture;
            }
        }

        /// <summary>
        /// moveが王手なのかどうか調べる。
        /// </summary>
        public static bool IsCheckMove(Piece* board, Move move, int turn, int kingE) {
            Debug.Assert(!ControlExists(board, turn, kingE), "既に王手かかってる？");
            if (move.IsSpecialState) return false;
            Do(board, move, turn);
            bool check = ControlExists(board, turn, kingE);
            Undo(board, move, turn);
            return check;
        }

        /// <summary>
        /// デバッグ用文字列化
        /// </summary>
        public static string GetDumpString(Piece* board) {
            StringBuilder str = new StringBuilder();
            for (int y = 0; y < 9 + 2; y++) {
                for (int x = 0; x < 16; x++) {
                    if (board[x + y * 16] == Piece.WALL) {
                        str.Append("壁");
                    } else {
                        str.Append(PieceUtility.ToName(board[x + y * 16]));
                    }
                }
                str.AppendLine();
            }
            return str.ToString();
        }
  }
}
