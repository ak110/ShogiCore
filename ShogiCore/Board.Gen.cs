using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace ShogiCore {
    public unsafe partial class Board {
        /// <summary>
        /// 利きの差分計算その1
        /// </summary>
        private void UpdateControlPreMove_Gen(Move move) {
            var board = this.board;
            var control = this.control;
            var Turn = this.Turn;
            var moveFrom = move.From;
            // 元いた駒の効き
            switch (board[moveFrom] & unchecked(Piece.ENEMY - 1)) { // board[moveFrom] & ~(Piece.ENEMY) より早いかも？
			case Piece.EMPTY: break;
			case Piece.FU:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				break;
			case Piece.KY:
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.KE:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 8]] &= ~(1 << 8);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 9]] &= ~(1 << 9);
				break;
			case Piece.GI:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 0]] &= ~(1 << 0);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 2]] &= ~(1 << 2);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.KI:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.KA:
				{
                    int direct = Board.Direct[Turn * 16 + 0];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (0 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 2];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (2 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 5];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (5 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 7];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (7 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.HI:
				{
                    int direct = Board.Direct[Turn * 16 + 1];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (1 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 3];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (3 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 4];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (4 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.OU:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 0]] &= ~(1 << 0);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 2]] &= ~(1 << 2);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.TO:
			case Piece.NY:
			case Piece.NK:
			case Piece.NG:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.NKi: break;
			case Piece.UM:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				goto case Piece.KA;
			case Piece.RY:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 0]] &= ~(1 << 0);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 2]] &= ~(1 << 2);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				goto case Piece.HI;
			}
        }

		/// <summary>
        /// 利きの差分計算その2
        /// </summary>
        private void UpdateControlPostMove_Gen(Move move) {
            var board = this.board;
            var control = this.control;
            var Turn = this.Turn;
            var moveFrom = move.From;
            var moveTo = move.To;
            // 飛び利きの延長
            for (int t = 0; t < 2; t++) {
                if ((control[t][moveFrom] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                for (int dir = 0; dir < 8; dir++) {
                    if ((control[t][moveFrom] & (1 << (dir + 10))) != 0) {
                        int direct = Board.Direct[t * 16 + dir];
                        for (int j = moveFrom; ; ) {
                            j += direct;
                            control[t][j] |= (1 << (dir + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
                }
            }
            // 取った駒の効き
            if (move.IsCapture) {
                switch (move.Capture & unchecked(Piece.ENEMY - 1)) { // move.Capture & ~(Piece.ENEMY) より早いかも？
				case Piece.EMPTY: break;
				case Piece.FU:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] &= ~(1 << 6);
					break;
				case Piece.KY:
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 6];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (6 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					break;
				case Piece.KE:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 8]] &= ~(1 << 8);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 9]] &= ~(1 << 9);
					break;
				case Piece.GI:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 0]] &= ~(1 << 0);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 2]] &= ~(1 << 2);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] &= ~(1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] &= ~(1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] &= ~(1 << 7);
					break;
				case Piece.KI:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] &= ~(1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] &= ~(1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] &= ~(1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] &= ~(1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] &= ~(1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] &= ~(1 << 7);
					break;
				case Piece.KA:
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 0];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (0 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 2];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (2 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 5];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (5 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 7];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (7 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					break;
				case Piece.HI:
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 1];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (1 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 3];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (3 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 4];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (4 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 6];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] &= ~(1 << (6 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					break;
				case Piece.OU:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 0]] &= ~(1 << 0);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] &= ~(1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 2]] &= ~(1 << 2);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] &= ~(1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] &= ~(1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] &= ~(1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] &= ~(1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] &= ~(1 << 7);
					break;
				case Piece.TO:
				case Piece.NY:
				case Piece.NK:
				case Piece.NG:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] &= ~(1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] &= ~(1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] &= ~(1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] &= ~(1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] &= ~(1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] &= ~(1 << 7);
					break;
				case Piece.NKi: break;
				case Piece.UM:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] &= ~(1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] &= ~(1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] &= ~(1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] &= ~(1 << 6);
					goto case Piece.KA;
				case Piece.RY:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 0]] &= ~(1 << 0);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 2]] &= ~(1 << 2);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] &= ~(1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] &= ~(1 << 7);
					goto case Piece.HI;
				}
            }
        }

		/// <summary>
        /// 利きの差分計算その3
        /// </summary>
        private void UpdateControlPostDo_Gen(Move move) {
            var board = this.board;
            var control = this.control;
            var Turn = this.Turn;
            var moveTo = move.To;
            if (!move.IsCapture) {
                // 移動先で遮った飛び利き
                for (int t = 0; t < 2; t++) {
                    if ((control[t][moveTo] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                    for (int dir = 0; dir < 8; dir++) {
                        if ((control[t][moveTo] & (1 << (dir + 10))) != 0) {
                            int direct = Board.Direct[t * 16 + dir];
                            for (int j = moveTo; ; ) {
                                j += direct;
                                control[t][j] &= ~(1 << (dir + 10));
                                if (board[j] != Piece.EMPTY) break;
                            }
                        }
                    }
                }
            } else {
                Debug.Assert(!move.IsPut);
            }
            // 移動先の利き
            switch (board[moveTo] & unchecked(Piece.ENEMY - 1)) { // board[moveTo] & ~(Piece.ENEMY) より早いかも？
			case Piece.EMPTY: break;
			case Piece.FU:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				break;
			case Piece.KY:
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.KE:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 8]] |= (1 << 8);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 9]] |= (1 << 9);
				break;
			case Piece.GI:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 0]] |= (1 << 0);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 2]] |= (1 << 2);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.KI:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.KA:
				{
                    int direct = Board.Direct[Turn * 16 + 0];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (0 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 2];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (2 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 5];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (5 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 7];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (7 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.HI:
				{
                    int direct = Board.Direct[Turn * 16 + 1];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (1 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 3];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (3 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 4];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (4 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.OU:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 0]] |= (1 << 0);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 2]] |= (1 << 2);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.TO:
			case Piece.NY:
			case Piece.NK:
			case Piece.NG:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.NKi: break;
			case Piece.UM:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				goto case Piece.KA;
			case Piece.RY:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 0]] |= (1 << 0);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 2]] |= (1 << 2);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				goto case Piece.HI;
			}
        }

		/// <summary>
        /// 利きの差分計算(Undo)その1
        /// </summary>
        private void UpdateControlPreUndo_Gen(Move move) {
            var board = this.board;
            var control = this.control;
            var Turn = this.Turn;
            var moveTo = move.To;
            // 移動先の効き
            switch (board[moveTo] & unchecked(Piece.ENEMY - 1)) { // board[moveTo] & ~(Piece.ENEMY) より早いかも？
			case Piece.EMPTY: break;
			case Piece.FU:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				break;
			case Piece.KY:
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.KE:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 8]] &= ~(1 << 8);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 9]] &= ~(1 << 9);
				break;
			case Piece.GI:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 0]] &= ~(1 << 0);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 2]] &= ~(1 << 2);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.KI:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.KA:
				{
                    int direct = Board.Direct[Turn * 16 + 0];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (0 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 2];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (2 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 5];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (5 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 7];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (7 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.HI:
				{
                    int direct = Board.Direct[Turn * 16 + 1];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (1 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 3];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (3 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 4];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (4 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveTo; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] &= ~(1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.OU:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 0]] &= ~(1 << 0);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 2]] &= ~(1 << 2);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.TO:
			case Piece.NY:
			case Piece.NK:
			case Piece.NG:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				break;
			case Piece.NKi: break;
			case Piece.UM:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 1]] &= ~(1 << 1);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 3]] &= ~(1 << 3);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 4]] &= ~(1 << 4);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 6]] &= ~(1 << 6);
				goto case Piece.KA;
			case Piece.RY:
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 0]] &= ~(1 << 0);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 2]] &= ~(1 << 2);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 5]] &= ~(1 << 5);
				control[Turn ^ 0][moveTo + Board.Direct[Turn * 16 + 7]] &= ~(1 << 7);
				goto case Piece.HI;
			}
        }

		/// <summary>
        /// 利きの差分計算(Undo)その2
        /// </summary>
        private void UpdateControlPostUnmove_Gen(Move move) {
            var board = this.board;
            var control = this.control;
            var Turn = this.Turn;
            var moveFrom = move.From;
            var moveTo = move.To;
            // 取った駒の効き
            if (move.IsCapture) {
                switch (move.Capture & unchecked(Piece.ENEMY - 1)) { // move.Capture & ~(Piece.ENEMY) より早いかも？
				case Piece.EMPTY: break;
				case Piece.FU:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] |= (1 << 6);
					break;
				case Piece.KY:
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 6];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (6 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					break;
				case Piece.KE:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 8]] |= (1 << 8);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 9]] |= (1 << 9);
					break;
				case Piece.GI:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 0]] |= (1 << 0);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 2]] |= (1 << 2);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] |= (1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] |= (1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] |= (1 << 7);
					break;
				case Piece.KI:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] |= (1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] |= (1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] |= (1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] |= (1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] |= (1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] |= (1 << 7);
					break;
				case Piece.KA:
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 0];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (0 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 2];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (2 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 5];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (5 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 7];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (7 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					break;
				case Piece.HI:
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 1];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (1 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 3];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (3 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 4];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (4 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					{
                        int direct = Board.Direct[(Turn ^ 1) * 16 + 6];
                        for (int j = moveTo; ; ) {
                            j += direct;
                            control[Turn ^ 1][j] |= (1 << (6 + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
					break;
				case Piece.OU:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 0]] |= (1 << 0);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] |= (1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 2]] |= (1 << 2);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] |= (1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] |= (1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] |= (1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] |= (1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] |= (1 << 7);
					break;
				case Piece.TO:
				case Piece.NY:
				case Piece.NK:
				case Piece.NG:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] |= (1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] |= (1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] |= (1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] |= (1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] |= (1 << 6);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] |= (1 << 7);
					break;
				case Piece.NKi: break;
				case Piece.UM:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 1]] |= (1 << 1);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 3]] |= (1 << 3);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 4]] |= (1 << 4);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 6]] |= (1 << 6);
					goto case Piece.KA;
				case Piece.RY:
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 0]] |= (1 << 0);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 2]] |= (1 << 2);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 5]] |= (1 << 5);
					control[Turn ^ 1][moveTo + Board.Direct[(Turn ^ 1) * 16 + 7]] |= (1 << 7);
					goto case Piece.HI;
				}
            }
            // 飛び利きの延長
            for (int t = 0; t < 2; t++) {
                if ((control[t][moveFrom] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                for (int dir = 0; dir < 8; dir++) {
                    if ((control[t][moveFrom] & (1 << (dir + 10))) != 0) {
                        int direct = Board.Direct[t * 16 + dir];
                        for (int j = moveFrom; ; ) {
                            j += direct;
                            control[t][j] &= ~(1 << (dir + 10));
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
                }
            }
            // 元いた駒の効き
            switch (board[moveFrom] & unchecked(Piece.ENEMY - 1)) { // board[moveFrom] & ~(Piece.ENEMY) より早いかも？
			case Piece.EMPTY: break;
			case Piece.FU:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				break;
			case Piece.KY:
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.KE:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 8]] |= (1 << 8);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 9]] |= (1 << 9);
				break;
			case Piece.GI:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 0]] |= (1 << 0);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 2]] |= (1 << 2);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.KI:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.KA:
				{
                    int direct = Board.Direct[Turn * 16 + 0];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (0 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 2];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (2 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 5];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (5 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 7];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (7 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.HI:
				{
                    int direct = Board.Direct[Turn * 16 + 1];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (1 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 3];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (3 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 4];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (4 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				{
                    int direct = Board.Direct[Turn * 16 + 6];
                    for (int j = moveFrom; ; ) {
                        j += direct;
                        control[Turn ^ 0][j] |= (1 << (6 + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
				break;
			case Piece.OU:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 0]] |= (1 << 0);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 2]] |= (1 << 2);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.TO:
			case Piece.NY:
			case Piece.NK:
			case Piece.NG:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				break;
			case Piece.NKi: break;
			case Piece.UM:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 1]] |= (1 << 1);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 3]] |= (1 << 3);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 4]] |= (1 << 4);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 6]] |= (1 << 6);
				goto case Piece.KA;
			case Piece.RY:
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 0]] |= (1 << 0);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 2]] |= (1 << 2);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 5]] |= (1 << 5);
				control[Turn ^ 0][moveFrom + Board.Direct[Turn * 16 + 7]] |= (1 << 7);
				goto case Piece.HI;
			}
        }

		/// <summary>
        /// 利きの差分計算(Undo)その3
        /// </summary>
        private void UpdateControlPostUndo_Gen(Move move) {
            var board = this.board;
            var control = this.control;
            var moveTo = move.To;
            if (!move.IsCapture) {
                // 移動先で遮った飛び利き
                for (int t = 0; t < 2; t++) {
                    if ((control[t][moveTo] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                    for (int dir = 0; dir < 8; dir++) {
                        if ((control[t][moveTo] & (1 << (dir + 10))) != 0) {
                            int direct = Board.Direct[t * 16 + dir];
                            for (int j = moveTo; ; ) {
                                j += direct;
                                control[t][j] |= (1 << (dir + 10));
                                if (board[j] != Piece.EMPTY) break;
                            }
                        }
                    }
                }
            } else {
                Debug.Assert(!move.IsPut);
            }
        }

	}
}
