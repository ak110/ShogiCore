using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// CSA棋譜の書き出し
    /// </summary>
    public class PCLNotationWriter : StringNotationWriter {
        public override string WriteToString(IEnumerable<Notation> notations) {
            StringBuilder str = new StringBuilder();
            foreach (var notation in notations) {
                if (0 < str.Length) str.Append("/\n"); // セパレータ
                BuildCSAStandard(notation, str);
            }
            return str.ToString();
        }

        /// <summary>
        /// 棋譜の文字列化
        /// </summary>
        private void BuildCSAStandard(Notation notation, StringBuilder str) {
            // ver、N+、N-
            str.Append("V2.2\n");
            str.Append("N+").Append(notation.FirstPlayerName).Append('\n');
            str.Append("N-").Append(notation.SecondPlayerName).Append('\n');
            // 盤面
            BoardData board = notation.InitialBoard;
            if (board == null) {
                str.Append("PI\n+\n");
            } else {
                str.Append(ToString(board));
            }
            // 着手
            if (notation.Moves != null) {
                board = board == null ? BoardData.CreateEquality() : board.Clone();
                foreach (var move in notation.Moves) {
                    str.Append(ToString(board, move.MoveData)).Append('\n');
                    board.Do(move.MoveData);
                }
            }

            // 終局理由とかも必要なのだがとりあえず未実装気味

            int firstTurn = board == null || board.Turn == 0 ? 0 : 1;
            if (notation.Winner == -1) {
                // TODO: 引き分けは千日手と持将棋があるのでどうにかする
            } else if ((firstTurn ^ (notation.Moves.Length % 2) ^ 1) == notation.Winner) {
                // 最後の手を指したのが勝者側なら投了扱いにしてみる (手抜き)
                str.Append("%TORYO").Append('\n');
            }
        }

        /// <summary>
        /// 局面の文字列化
        /// </summary>
        public static string ToString(BoardData board) {
            StringBuilder str = new StringBuilder();
            // PIとかで表現出来るかもしれなくても手抜き。
            for (int y = 0; y < 9; y++) {
                int rank = y + 1;
                str.Append("P").Append(rank);
                for (int x = 0; x < 9; x++) {
                    int file = 9 - x;
                    var p = board[file, rank];
                    if (p == Piece.EMPTY || p == Piece.ENEMY) {
                        str.Append(" * ");
                    } else if ((p & Piece.ENEMY) != 0) {
                        str.Append('-').Append(PCLNotationReader.ToCSAName(p));
                    } else {
                        str.Append('+').Append(PCLNotationReader.ToCSAName(p));
                    }
                }
                str.Append('\n');
            }
            // 持ち駒。ALが使えるかもしれなくても手抜き。
            for (int t = 0; t < 2; t++) {
                bool appended = false;
                int[] hand = board.GetHand(t);
                for (Piece p = Piece.FU; p < Piece.OU; p++) {
                    int n = hand[(byte)p];
                    if (n <= 0) continue;
                    if (!appended) {
                        appended = true;
                        str.Append(t == 0 ? "P+" : "P-");
                    }
                    string name = "00" + PCLNotationReader.ToCSAName(p);
                    for (int i = 0; i < n; i++) str.Append(name);
                }
                if (appended) {
                    str.Append('\n');
                }
            }
            // 手番
            str.Append(board.Turn == 0 ? "+\n" : "-\n");
            return str.ToString();
        }

        /// <summary>
        /// 指し手の文字列化
        /// </summary>
        /// <param name="board">局面</param>
        /// <param name="move">指し手</param>
        /// <returns>CSA棋譜の指し手表現</returns>
        public static string ToString(BoardData board, MoveData move) {
            if (move.IsSpecialMove) {
                if (move == MoveData.Resign) return "%TORYO";
                if (move == MoveData.Pass) return "%PASS"; // 適当
                if (move == MoveData.Win) return "%KACHI";
                if (move == MoveData.Endless) return "%SENNICHITE";
                if (move == MoveData.Perpetual) return "%ILLEGAL_MOVE"; // 適当
                return "%ILLEGAL_MOVE"; // 適当
            }
            if (move.IsPut) {
                return "+-"[board.Turn].ToString() + "00" +
                    move.ToFile.ToString() + move.ToRank.ToString() +
                    PCLNotationReader.CSANameTable[(byte)move.PutPiece];
            } else {
                Piece p = board[move.FromFile, move.FromRank] & ~Piece.ENEMY;
                if (move.IsPromote) {
                    p |= Piece.PROMOTED;
                }
                return "+-"[board.Turn].ToString() +
                    move.FromFile.ToString() + move.FromRank.ToString() +
                    move.ToFile.ToString() + move.ToRank.ToString() +
                    PCLNotationReader.CSANameTable[(byte)p];
            }
        }

        /// <summary>
        /// 指し手の文字列化
        /// </summary>
        /// <param name="board">局面。中身を変えてしまうので注意。</param>
        /// <param name="moves">指し手</param>
        /// <returns>CSA棋譜の指し手表現</returns>
        public static string ToString(BoardData board, IEnumerable<MoveData> moves) {
            StringBuilder str = new StringBuilder();
            foreach (MoveData move in moves) {
                str.Append(ToString(board, move));
                str.Append(' ');
                board.Do(move);
            }
            if (0 < str.Length) { // ダサいけど最後に末尾の空白を除去。
                str.Remove(str.Length - 1, 1);
            }
            return str.ToString();
        }
    }
}
