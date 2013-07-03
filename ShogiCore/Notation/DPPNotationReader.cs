using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// http://aleag.cocolog-nifty.com/blog/2008/04/post_a76d.html
    /// aleag1_v1_0_dist.zipのsamplesに入ってるdppファイルの読み込み。適当実装。
    /// </summary>
    public class DPPNotationReader : StringNotationReader {
        /// <summary>
        /// 文字列化用テーブル
        /// </summary>
        public static readonly char[] NameTable = new char[] {
            '.','F','Y','E','G','I','A','H','O','T','S','M','Z','I','U','R',
            ' ','f','y','e','g','i','a','h','o','t','s','m','z','i','u','r',
            '-', // ブログに貼られてるのは-のもあるので一応対応
        };
        /// <summary>
        /// 文字のPieceData化
        /// </summary>
        public static Piece ParseChar(char c) {
            int n = Array.IndexOf(NameTable, c);
            if (n < 0) throw new ArgumentException("c");
            return (Piece)(n & ~0x20); // '-' 対策
        }

        /// <summary>
        /// 読み込み。
        /// </summary>
        public override IEnumerable<Notation> Read(string data) {
            if (data == null) {
                throw new NotationException("DPPデータの読み込みに失敗しました");
            }
            Notation notation = InnerLoad(data);
            yield return notation;
        }

        /// <summary>
        /// 読み込めるか判定
        /// </summary>
        public override bool CanRead(string data) {
            return data != null &&
                0 <= data.IndexOf('.') &&
                0 <= data.IndexOfAny(new[] { 'F', 'f', 'Y', 'y' });
            // ↑適当 (´ω`)
        }

        /// <summary>
        /// 読み込み。
        /// </summary>
        private static Notation InnerLoad(string data) {
            BoardData board = new BoardData();

            int state = 0;
            foreach (string line in data.Split(
                new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)) {
                if (string.IsNullOrEmpty(line)) continue;
                string line2 = line.Trim();
                if (line2.StartsWith("#", StringComparison.Ordinal) ||
                    line2 == "e" ||
                    line2.Length <= 0) continue;

                switch (state) {
                case 0: // 後手持ち駒
                    ParseHand(board, line2, 1);
                    state++;
                    break;

                case 1: // 盤面
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    if (line2.Length == 9) {
                        try {
                            for (int i = 0; i < 9; i++) {
                                board[9 - i, state] = ParseChar(line2[i]);
                            }
                            state++;
                        } catch (Exception e) {
                            throw new NotationException("DPPデータの読み込みに失敗: " + line2, e);
                        }
                    } else {
                        throw new NotationException("DPPデータの読み込みに失敗: " + line2);
                    }
                    break;

                case 10: // 先手持ち駒
                    ParseHand(board, line2, 0);
                    state++;
                    break;

                case 11: // 手番
                    board.Turn = line2.StartsWith("s", StringComparison.Ordinal) ? 0 : 1; // s または senteban
                    state++;
                    break;
                }
            }

            Notation notation = new Notation();
            notation.InitialBoard = board;
            notation.Moves = new MoveDataEx[0];
            return notation;
        }

        /// <summary>
        /// 持ち駒のParse
        /// </summary>
        private static void ParseHand(BoardData board, string line, int turn) {
            int[] hand = board.GetHand(turn);
            line = line.ToUpperInvariant();
            for (int i = 0; i < line.Length; ) {
                if (line[i] == ' ') { i++; continue; }
                Piece p = (Piece)Array.IndexOf<char>(NameTable, line[i++]);
                if (p == unchecked((Piece)(-1))) continue;
                p &= unchecked((Piece)(~0x20));
                if (p == Piece.EMPTY) continue;

                int n = 1;
                if (i < line.Length && char.IsDigit(line[i])) {
                    n = 0;
                    do {
                        n *= 10;
                        n += line[i++] - '0';
                    } while (i < line.Length && char.IsDigit(line[i]));
                }
                hand[(byte)p] = n;
            }
        }
    }
}
