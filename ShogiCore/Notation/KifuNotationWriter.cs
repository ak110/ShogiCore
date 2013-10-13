using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// KIF形式の書き出し
    /// </summary>
    public class KifuNotationWriter : StringNotationWriter {
        /// <summary>
        /// 文字列化用テーブル
        /// </summary>
        public static readonly string[] NameTable = new string[] {
            "・","歩","香","桂","銀","金","角","飛","玉","と","成香","成桂","成銀","成金","馬","龍",
        };

        /// <summary>
        /// 棋譜の種類
        /// </summary>
        public enum Mode {
            KIF,
            KI2,
        }

        readonly Mode mode;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="mode"></param>
        public KifuNotationWriter(Mode mode) {
            this.mode = mode;
        }

        public override string WriteToString(IEnumerable<Notation> notations) {
            StringBuilder str = new StringBuilder();
            foreach (var notation in notations) {
                if (0 < str.Length) { // セパレータ
                    str.AppendLine();
                    str.AppendLine();
                }
                BuildKIF(notation, str);
            }
            return str.ToString();
        }

        /// <summary>
        /// KIF形式の書き込み
        /// </summary>
        private void BuildKIF(Notation notation, StringBuilder str) {
            // 初期局面
            int firstTurn;
            if (notation.InitialBoard == null) {
                firstTurn = 0;
                str.AppendLine("手合割：平手");
            } else {
                firstTurn = notation.InitialBoard.Turn;
                str.Append("後手の持駒：");
                AppendHands(str, notation, 1);
                str.AppendLine();
                str.AppendLine("  ９ ８ ７ ６ ５ ４ ３ ２ １ ");
                str.AppendLine("+---------------------------+");
                for (int y = 0; y < 9; y++) {
                    str.Append("|");
                    int rank = y + 1;
                    for (int x = 0; x < 9; x++) {
                        int file = (9 - x);
                        str.Append(KifuNotationReader.NameTable[(byte)notation.InitialBoard[file, rank]]);
                    }
                    str.Append("|" + "一二三四五六七八九"[y].ToString());
                    str.AppendLine();
                }
                str.AppendLine("+---------------------------+");
                str.Append("先手の持駒：");
                AppendHands(str, notation, 0);
                str.AppendLine();
                str.AppendLine(firstTurn == 0 ? "先手番" : "後手番");
            }
            if (string.IsNullOrEmpty(notation.FirstPlayerName) &&
                string.IsNullOrEmpty(notation.SecondPlayerName)) {
                // 双方名無しなら省略（←適当）
            } else {
                str.AppendLine("先手：" + notation.FirstPlayerName);
                str.AppendLine("後手：" + notation.SecondPlayerName);
            }

            // 指し手が無ければここまで
            if (notation.Moves == null || notation.Moves.Length <= 0) return;

            // 未実装
            if (mode == Mode.KI2) {
                throw new NotImplementedException("KI2形式は未実装");
            }

            str.AppendLine("手数----指手---------消費時間--");
            // 各手の出力
            BoardData board = notation.InitialBoard == null ?
                BoardData.CreateEquality() : notation.InitialBoard.Clone();
            for (int i = 0; i < notation.Moves.Length; i++) {
                MoveData move = notation.Moves[i].MoveData;
                // 手数
                str.Append((i + 1).ToString().PadLeft(4));
                str.Append(' ');
                // 指し手
                int shift = 0;
                if (0 < i && notation.Moves[i - 1].MoveData.To == move.To) {
                    str.Append("同　");
                } else {
                    str.Append("０１２３４５６７８９"[move.ToFile]);
                    str.Append("零一二三四五六七八九"[move.ToRank]);
                }
                if (move.IsPut) {
                    shift += 4;
                    str.Append(NameTable[(byte)move.PutPiece]);
                    str.Append('打');
                } else {
                    string name = NameTable[(byte)(board[move.FromFile, move.FromRank] & ~Piece.ENEMY)];
                    shift += name.Length * 2;
                    str.Append(name);
                    if (move.IsPromote) {
                        shift += 2;
                        str.Append('成');
                    }
                    shift += 4;
                    str.Append('(').Append(move.FromFile).Append(move.FromRank).Append(')');
                }

                // 消費時間 (手抜きにより0秒固定)
                str.Append("".PadRight(10 - shift));
                str.Append("(00:00 / 00:00:00)");
                str.AppendLine();

                board.Do(move);
            }

            // 終局理由とかも必要なのだがとりあえず未実装気味

            if (notation.Winner == -1) {
                // TODO: 引き分けは千日手と持将棋があるのでどうにかする
            } else if ((firstTurn ^ (notation.Moves.Length % 2) ^ 1) == notation.Winner) {
                // 最後の手を指したのが勝者側なら投了扱いにしてみる (手抜き)
                str.Append((notation.Moves.Length + 1).ToString().PadLeft(4));
                str.Append(' ');
                str.Append("投了");
                str.Append("          ");
                str.Append("(00:00 / 00:00:00)");
                str.AppendLine();
            }
        }

        /// <summary>
        /// 指し手部分の文字列化
        /// </summary>
        private void AppendHands(StringBuilder str, Notation notation, int turn) {
            bool hasAny = false;
            for (Piece p = Piece.HI; Piece.FU <= p; p--) {
                int h = notation.InitialBoard.GetHand(turn)[(byte)p];
                if (0 < h) {
                    if (hasAny) str.Append("　"); // 2回目以降のみ
                    hasAny = true;
                    str.Append(NameTable[(byte)p]);
                    if (1 < h) {
                        str.Append(NotationUtility.KanjiNumerals[h]);
                    }
                }
            }
            if (!hasAny) {
                str.Append("なし");
            }
        }

        private void BuildKI2(Notation notation, StringBuilder str) {
            throw new NotImplementedException();
        }
    }
}
