using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// SFEN棋譜の書き出し
    /// </summary>
    public class SFENNotationWriter : IStringNotationWriter {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region IStringNotationWriter メンバ

        public string WriteToString(IEnumerable<Notation> notations) {
            StringBuilder str = new StringBuilder();
            foreach (Notation notation in notations) {
                str.AppendLine(WriteToString(notation));
            }
            return str.ToString();
        }

        #endregion

        /// <summary>
        /// SFEN棋譜の書き出し(末尾に改行無しの文字列で返す)
        /// </summary>
        public string WriteToString(Notation notation) {
            StringBuilder str = new StringBuilder();
            // 初期局面
            if (notation.InitialBoard == null) {
                str.Append("startpos");
            } else {
                str.Append("sfen ");
                // 盤面
                for (int rank = 1; rank <= 9; rank++) {
                    for (int file = 9; 1 <= file; file--) { // １段目の左側（９筋側）から駒の種類を書いていきます
                        if (notation.InitialBoard[file, rank] == Piece.EMPTY) {
                            int count = 1;
                            for (file--; 1 <= file; file--) {
                                if (notation.InitialBoard[file, rank] != Piece.EMPTY) break;
                                count++;
                            }
                            file++; // 1個戻す。(´ω`)
                            str.Append(count);
                        } else {
                            str.Append(SFENNotationReader.ToName(
                                notation.InitialBoard[file, rank]));
                        }
                    }
                    if (rank < 9) str.Append('/');
                }
                str.Append(' ');
                // 手番
                str.Append("bw"[notation.InitialBoard.Turn]);
                str.Append(' ');
                // 持ち駒
                {
                    string handStr = "";
                    for (int t = 0; t < 2; t++) {
                        Piece turnMask = t == 0 ? Piece.EMPTY : Piece.ENEMY;
                        int[] hand = notation.InitialBoard.GetHand(t);
                        for (Piece p = Piece.FU; p < Piece.OU; p++) {
                            int n = hand[(byte)p];
                            if (0 < n) {
                                if (n == 1) {
                                    handStr += SFENNotationReader.ToName(p | turnMask);
                                } else {
                                    handStr += n.ToString();
                                    handStr += SFENNotationReader.ToName(p | turnMask);
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(handStr)) {
                        str.Append('-');
                    } else {
                        str.Append(handStr);
                    }
                }
                str.Append(' ');
                // 手数
                str.Append('1');
                // startpos化
                if (string.CompareOrdinal(str.ToString(),
                    "sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1") == 0) {
                    str.Length = 0;
                    str.Append("startpos");
                }
            }
            // 指し手
            if (notation.Moves != null && 0 < notation.Moves.Length) {
                str.Append(" moves");
                foreach (MoveDataEx t in notation.Moves) {
                    str.Append(' ');
                    str.Append(ToString(t.MoveData));
                }
            }
            return str.ToString();
        }

        /// <summary>
        /// MoveDataを棋譜内の指し手表現に変換。"3c3d+" とか。
        /// </summary>
        /// <param name="move">指し手</param>
        /// <returns>指し手表現</returns>
        public static string ToString(MoveData move) {
            if (move.From == 0 && move.To == 0) {
                return "resign";
            } else if (move.IsSpecialMove) {
                if (move == MoveData.Pass) return "pass";
                if (move == MoveData.Resign) return "resign";
                if (move == MoveData.Win) return "win";
                if (move == MoveData.Endless) return "endless"; // 適当
                if (move == MoveData.Perpetual) return "perpetual"; // 適当
                logger.Warn("不正な指し手: From=0x" + move.From.ToString("x2") + ", To=0x" + move.To.ToString("x2"));
                return "resign";
            } else if (move.IsPut) {
                return string.Format("{0}*{1}{2}",
                    SFENNotationReader.ToName(move.PutPiece & ~Piece.ENEMY),
                    move.ToFile, "0abcdefghi"[move.ToRank]);
            } else {
                return string.Format("{0}{1}{2}{3}{4}",
                    move.FromFile, "0abcdefghi"[move.FromRank],
                    move.ToFile, "0abcdefghi"[move.ToRank],
                    move.IsPromote ? "+" : "");
            }
        }
    }
}
