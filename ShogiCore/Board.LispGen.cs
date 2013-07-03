using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace ShogiCore {
	public unsafe partial class Board {

		/// <summary>
		/// 歩の動く手の生成
		/// </summary>
		private int MakePieceMoveFU0(Move* moves, int index, int from, MoveType moveType) {
			Debug.Assert(PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.FU);
			int to = from + -1;
			if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) {
				if ((((from) - Board.Padding) & 0x0f) <= 4) {
					if (moveType != MoveType.AllWithNoPromote || (((to) - Board.Padding) & 0x0f) <= 1) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			return index;
		}

		/// <summary>
		/// 香の動く手の生成
		/// </summary>
		private int MakePieceMoveKY0(Move* moves, int index, int from, MoveType moveType) {
			Debug.Assert(PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.KY);
			int toRankLimit = moveType == MoveType.AllWithNoPromote ? 1 : 2; // 不成生成時は1段目のみ成る手のみ。
			const int diff = -1;
			// 空白の間動く手を生成
			int to;
			for (to = from + diff; board[to] == Piece.EMPTY; to += diff) {
				int toRank = (((to) - Board.Padding) & 0x0f);
				if (toRank <= 3) {
					if (toRank <= toRankLimit) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			// 敵の駒があるならそこへ動く
			if ((Piece.EFU <= board[to] && board[to] <= Piece.ERY)) {
				int toRank = (((to) - Board.Padding) & 0x0f);
				if (toRank <= 3) {
					if (toRank <= toRankLimit) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			return index;
		}

		/// <summary>
		/// 桂の動く手の生成
		/// </summary>
		private int MakePieceMoveKE0(Move* moves, int index, int from, MoveType moveType) {
			Debug.Assert(PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.KE);
			{
				int to = from + +14;
				if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) {
					if ((((from) - Board.Padding) & 0x0f) <= 5) {
						if ((((to) - Board.Padding) & 0x0f) <= 2) {
							// 成る手を生成
							moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
						} else {
							// 成る手と成らない手を生成
							Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
							moves[index++] = move;
							moves[index++] = move.ToNotPromote;
						}
					} else {
						// 成らない手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
					}
				}
			}
			{
				int to = from + -18;
				if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) {
					if ((((from) - Board.Padding) & 0x0f) <= 5) {
						if ((((to) - Board.Padding) & 0x0f) <= 2) {
							// 成る手を生成
							moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
						} else {
							// 成る手と成らない手を生成
							Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
							moves[index++] = move;
							moves[index++] = move.ToNotPromote;
						}
					} else {
						// 成らない手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
					}
				}
			}
			return index;
		}

		/// <summary>
		/// 銀の動く手の生成
		/// </summary>
		private int MakePieceMoveGI0(Move* moves, int index, int from, MoveType moveType) {
			index = MakeMove(moves, index, from, from + +17, moveType);
			index = MakeMove(moves, index, from, from + -15, moveType);
			index = MakeMove(moves, index, from, from + +15, moveType);
			index = MakeMove(moves, index, from, from + -1, moveType);
			index = MakeMove(moves, index, from, from + -17, moveType);
			return index;
		}

		/// <summary>
		/// 金の動く手の生成
		/// </summary>
		private int MakePieceMoveKI0(Move* moves, int index, int from, MoveType moveType) {
	{int to = from + +1; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + +16; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -16; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + +15; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -1; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -17; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
			return index;
		}

		/// <summary>
		/// 角の動く手の生成
		/// </summary>
		private int MakePieceMoveKA0(Move* moves, int index, int from, MoveType moveType) {
			index = MakeStraightKAHI0(moves, index, from, +17, moveType);
			index = MakeStraightKAHI0(moves, index, from, -15, moveType);
			index = MakeStraightKAHI0(moves, index, from, +15, moveType);
			index = MakeStraightKAHI0(moves, index, from, -17, moveType);
			return index;
		}

		/// <summary>
		/// 飛の動く手の生成
		/// </summary>
		private int MakePieceMoveHI0(Move* moves, int index, int from, MoveType moveType) {
			index = MakeStraightKAHI0(moves, index, from, +1, moveType);
			index = MakeStraightKAHI0(moves, index, from, +16, moveType);
			index = MakeStraightKAHI0(moves, index, from, -16, moveType);
			index = MakeStraightKAHI0(moves, index, from, -1, moveType);
			return index;
		}

		/// <summary>
		/// 馬の動く手の生成
		/// </summary>
		private int MakePieceMoveUM0(Move* moves, int index, int from, MoveType moveType) {
	{int to = from + +1; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + +16; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -16; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -1; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
			index = MakeStraightUMRY0(moves, index, from, +17, moveType);
			index = MakeStraightUMRY0(moves, index, from, -15, moveType);
			index = MakeStraightUMRY0(moves, index, from, +15, moveType);
			index = MakeStraightUMRY0(moves, index, from, -17, moveType);
			return index;
		}

		/// <summary>
		/// 龍の動く手の生成
		/// </summary>
		private int MakePieceMoveRY0(Move* moves, int index, int from, MoveType moveType) {
	{int to = from + +17; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -15; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + +15; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -17; if ((board[to] == Piece.EMPTY || (Piece.EFU <= board[to] && board[to] <= Piece.ERY))) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
			index = MakeStraightUMRY0(moves, index, from, +1, moveType);
			index = MakeStraightUMRY0(moves, index, from, +16, moveType);
			index = MakeStraightUMRY0(moves, index, from, -16, moveType);
			index = MakeStraightUMRY0(moves, index, from, -1, moveType);
			return index;
		}

		/// <summary>
		/// 飛び駒の動きを生成
		/// </summary>
		private int MakeStraightKAHI0(Move* moves, int index, int from, int diff, MoveType moveType) {
			Debug.Assert(
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.KA ||
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.HI);

			int fromRank = (((from) - Board.Padding) & 0x0f);
			// 空白の間動く手を生成
			int to;
			for (to = from + diff; board[to] == Piece.EMPTY; to += diff) {
				if ((board[from] & Piece.PROMOTED) == 0 && // ← ここでは PieceUtility.CanPromote(board[from]) と同義
					(fromRank <= 3 || (((to) - Board.Padding) & 0x0f) <= 3)) {
					if (moveType != MoveType.AllWithNoPromote) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			// 敵の駒があるならそこへ動く
			if ((Piece.EFU <= board[to] && board[to] <= Piece.ERY)) {
				if ((board[from] & Piece.PROMOTED) == 0 && // ← ここでは PieceUtility.CanPromote(board[from]) と同義
					(fromRank <= 3 || (((to) - Board.Padding) & 0x0f) <= 3)) {
					if (moveType != MoveType.AllWithNoPromote) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			return index;
		}

		/// <summary>
		/// 飛び駒の動きを生成
		/// </summary>
		private int MakeStraightUMRY0(Move* moves, int index, int from, int diff, MoveType moveType) {
			Debug.Assert(
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.UM ||
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.RY);

			// 空白の間動く手を生成
			int to;
			for (to = from + diff; board[to] == Piece.EMPTY; to += diff) {
				moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
			}
			// 敵の駒があるならそこへ動く
			if ((Piece.EFU <= board[to] && board[to] <= Piece.ERY)) {
				moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
			}
			return index;
		}

		/// <summary>
		/// 歩の動く手の生成
		/// </summary>
		private int MakePieceMoveFU1(Move* moves, int index, int from, MoveType moveType) {
			Debug.Assert(PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.FU);
			int to = from + 1;
			if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) {
				if ((10 - (((from) - Board.Padding) & 0x0f)) <= 4) {
					if (moveType != MoveType.AllWithNoPromote || (10 - (((to) - Board.Padding) & 0x0f)) <= 1) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			return index;
		}

		/// <summary>
		/// 香の動く手の生成
		/// </summary>
		private int MakePieceMoveKY1(Move* moves, int index, int from, MoveType moveType) {
			Debug.Assert(PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.KY);
			int toRankLimit = moveType == MoveType.AllWithNoPromote ? 1 : 2; // 不成生成時は1段目のみ成る手のみ。
			const int diff = 1;
			// 空白の間動く手を生成
			int to;
			for (to = from + diff; board[to] == Piece.EMPTY; to += diff) {
				int toRank = (10 - (((to) - Board.Padding) & 0x0f));
				if (toRank <= 3) {
					if (toRank <= toRankLimit) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			// 敵の駒があるならそこへ動く
			if ((Piece.FU <= board[to] && board[to] <= Piece.RY)) {
				int toRank = (10 - (((to) - Board.Padding) & 0x0f));
				if (toRank <= 3) {
					if (toRank <= toRankLimit) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			return index;
		}

		/// <summary>
		/// 桂の動く手の生成
		/// </summary>
		private int MakePieceMoveKE1(Move* moves, int index, int from, MoveType moveType) {
			Debug.Assert(PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.KE);
			{
				int to = from + -14;
				if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) {
					if ((10 - (((from) - Board.Padding) & 0x0f)) <= 5) {
						if ((10 - (((to) - Board.Padding) & 0x0f)) <= 2) {
							// 成る手を生成
							moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
						} else {
							// 成る手と成らない手を生成
							Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
							moves[index++] = move;
							moves[index++] = move.ToNotPromote;
						}
					} else {
						// 成らない手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
					}
				}
			}
			{
				int to = from + 18;
				if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) {
					if ((10 - (((from) - Board.Padding) & 0x0f)) <= 5) {
						if ((10 - (((to) - Board.Padding) & 0x0f)) <= 2) {
							// 成る手を生成
							moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
						} else {
							// 成る手と成らない手を生成
							Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
							moves[index++] = move;
							moves[index++] = move.ToNotPromote;
						}
					} else {
						// 成らない手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
					}
				}
			}
			return index;
		}

		/// <summary>
		/// 銀の動く手の生成
		/// </summary>
		private int MakePieceMoveGI1(Move* moves, int index, int from, MoveType moveType) {
			index = MakeMove(moves, index, from, from + -17, moveType);
			index = MakeMove(moves, index, from, from + 15, moveType);
			index = MakeMove(moves, index, from, from + -15, moveType);
			index = MakeMove(moves, index, from, from + 1, moveType);
			index = MakeMove(moves, index, from, from + 17, moveType);
			return index;
		}

		/// <summary>
		/// 金の動く手の生成
		/// </summary>
		private int MakePieceMoveKI1(Move* moves, int index, int from, MoveType moveType) {
	{int to = from + -1; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -16; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 16; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -15; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 1; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 17; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
			return index;
		}

		/// <summary>
		/// 角の動く手の生成
		/// </summary>
		private int MakePieceMoveKA1(Move* moves, int index, int from, MoveType moveType) {
			index = MakeStraightKAHI1(moves, index, from, -17, moveType);
			index = MakeStraightKAHI1(moves, index, from, 15, moveType);
			index = MakeStraightKAHI1(moves, index, from, -15, moveType);
			index = MakeStraightKAHI1(moves, index, from, 17, moveType);
			return index;
		}

		/// <summary>
		/// 飛の動く手の生成
		/// </summary>
		private int MakePieceMoveHI1(Move* moves, int index, int from, MoveType moveType) {
			index = MakeStraightKAHI1(moves, index, from, -1, moveType);
			index = MakeStraightKAHI1(moves, index, from, -16, moveType);
			index = MakeStraightKAHI1(moves, index, from, 16, moveType);
			index = MakeStraightKAHI1(moves, index, from, 1, moveType);
			return index;
		}

		/// <summary>
		/// 馬の動く手の生成
		/// </summary>
		private int MakePieceMoveUM1(Move* moves, int index, int from, MoveType moveType) {
	{int to = from + -1; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -16; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 16; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 1; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
			index = MakeStraightUMRY1(moves, index, from, -17, moveType);
			index = MakeStraightUMRY1(moves, index, from, 15, moveType);
			index = MakeStraightUMRY1(moves, index, from, -15, moveType);
			index = MakeStraightUMRY1(moves, index, from, 17, moveType);
			return index;
		}

		/// <summary>
		/// 龍の動く手の生成
		/// </summary>
		private int MakePieceMoveRY1(Move* moves, int index, int from, MoveType moveType) {
	{int to = from + -17; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 15; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + -15; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
	{int to = from + 17; if ((Piece.EMPTY <= board[to] && board[to] <= Piece.RY)) { moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY); } }
			index = MakeStraightUMRY1(moves, index, from, -1, moveType);
			index = MakeStraightUMRY1(moves, index, from, -16, moveType);
			index = MakeStraightUMRY1(moves, index, from, 16, moveType);
			index = MakeStraightUMRY1(moves, index, from, 1, moveType);
			return index;
		}

		/// <summary>
		/// 飛び駒の動きを生成
		/// </summary>
		private int MakeStraightKAHI1(Move* moves, int index, int from, int diff, MoveType moveType) {
			Debug.Assert(
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.KA ||
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.HI);

			int fromRank = (10 - (((from) - Board.Padding) & 0x0f));
			// 空白の間動く手を生成
			int to;
			for (to = from + diff; board[to] == Piece.EMPTY; to += diff) {
				if ((board[from] & Piece.PROMOTED) == 0 && // ← ここでは PieceUtility.CanPromote(board[from]) と同義
					(fromRank <= 3 || (10 - (((to) - Board.Padding) & 0x0f)) <= 3)) {
					if (moveType != MoveType.AllWithNoPromote) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			// 敵の駒があるならそこへ動く
			if ((Piece.FU <= board[to] && board[to] <= Piece.RY)) {
				if ((board[from] & Piece.PROMOTED) == 0 && // ← ここでは PieceUtility.CanPromote(board[from]) と同義
					(fromRank <= 3 || (10 - (((to) - Board.Padding) & 0x0f)) <= 3)) {
					if (moveType != MoveType.AllWithNoPromote) {
						// 成る手を生成
						moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
					} else {
						// 成る手と成らない手を生成
						Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
						moves[index++] = move;
						moves[index++] = move.ToNotPromote;
					}
				} else {
					// 成らない手を生成
					moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
				}
			}
			return index;
		}

		/// <summary>
		/// 飛び駒の動きを生成
		/// </summary>
		private int MakeStraightUMRY1(Move* moves, int index, int from, int diff, MoveType moveType) {
			Debug.Assert(
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.UM ||
				PieceUtility.SelfOrEnemy[Turn * 32 + (byte)board[from]] == Piece.RY);

			// 空白の間動く手を生成
			int to;
			for (to = from + diff; board[to] == Piece.EMPTY; to += diff) {
				moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
			}
			// 敵の駒があるならそこへ動く
			if ((Piece.FU <= board[to] && board[to] <= Piece.RY)) {
				moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
			}
			return index;
		}

		private int MakePut0(Move* moves, int index) {
			return index;
		}
		private int MakePut1(Move* moves, int index) {
			var Turn = this.Turn;
			int rank1 = 1 + (Turn ^ 1) + Padding;
			int rank2 = 9 - (Turn ^ 0) + Padding;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				if (existsFU[Turn * 16 + file / 0x10]) continue; // 歩がある筋は飛ばす
				for (int rank = rank1; rank <= rank2; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut2(Move* moves, int index) {
			var Turn = this.Turn;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
				}
			}
			return index;
		}
		private int MakePut3(Move* moves, int index) {
			var Turn = this.Turn;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
				}
			}
			return index;
		}
		private int MakePut4(Move* moves, int index) {
			var Turn = this.Turn;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut5(Move* moves, int index) {
			var Turn = this.Turn;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
				}
			}
			return index;
		}
		private int MakePut6(Move* moves, int index) {
			var Turn = this.Turn;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
				}
			}
			return index;
		}
		private int MakePut7(Move* moves, int index) {
			var Turn = this.Turn;
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
				}
			}
			return index;
		}
		private int MakePut8(Move* moves, int index) {
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut9(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut10(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut11(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut12(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut13(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut14(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut15(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut16(Move* moves, int index) {
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut17(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut18(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut19(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut20(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut21(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut22(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut23(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut24(Move* moves, int index) {
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut25(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut26(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut27(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut28(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut29(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut30(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut31(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut32(Move* moves, int index) {
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut33(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut34(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut35(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut36(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut37(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut38(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}
		private int MakePut39(Move* moves, int index) {
			var Turn = this.Turn;
			Piece pp0, pp1, pp2, pp3;
			{
				Piece** pp = stackalloc Piece*[4];
				pp[0] = &pp0; pp[1] = &pp1; pp[2] = &pp2; pp[3] = &pp3;
				int ppIndex = 0;
				var HandValue = this.HandValue;
				if ((HandValue & HandValueMaskGI) != 0) *pp[ppIndex++] = Piece.GI;
				if ((HandValue & HandValueMaskKI) != 0) *pp[ppIndex++] = Piece.KI;
				if ((HandValue & HandValueMaskKA) != 0) *pp[ppIndex++] = Piece.KA;
				if ((HandValue & HandValueMaskHI) != 0) *pp[ppIndex++] = Piece.HI;
			}
			for (int file = 0x10; file <= 0x90; file += 0x10) {
				bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
				for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
					if (board[file + rank] != Piece.EMPTY) continue;
					uint toValue = (uint)(file + rank) << Move.ShiftTo;
					int r = Turn == 0 ? rank - Padding : 10 - rank + Padding;
					if (2 <= r) {
						if (!existsFU) moves[index++].Value = ((uint)(byte)(Piece.FU) << Move.ShiftFrom | toValue);
						moves[index++].Value = ((uint)(byte)(Piece.KY) << Move.ShiftFrom | toValue);
						if (3 <= r) moves[index++].Value = ((uint)(byte)(Piece.KE) << Move.ShiftFrom | toValue);
					}
					moves[index++].Value = ((uint)(byte)(pp0) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp1) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp2) << Move.ShiftFrom | toValue);
					moves[index++].Value = ((uint)(byte)(pp3) << Move.ShiftFrom | toValue);
				}
			}
			return index;
		}

	}
}

