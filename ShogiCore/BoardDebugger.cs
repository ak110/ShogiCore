using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// Boardのデバッグ処理
    /// </summary>
    public unsafe class BoardDebugger {
        /// <summary>
        /// 異常なし
        /// </summary>
        public const string RegularString = "異常なし";

        /// <summary>
        /// Boardの状態が正常ならRegularString、異常ならそれを表す文字列を返す
        /// </summary>
        public string GetString(Board board) {
            StringBuilder str = new StringBuilder();

            // ハッシュ
            ulong hash1 = board.HashValue;
            ulong hash2 = board.CalculateHash();
            if (hash1 != hash2) {
                str.AppendLine("ハッシュ値の不一致: HashValue=" + hash1.ToString() + " CalculateHash()=" + hash2.ToString());
            }
            // 玉の位置
            if (board.GetKing(0) != 0 && board[board.GetKing(0)] != Piece.OU) {
                str.AppendLine("先手玉の位置が不正: GetKing(0)=" + board.GetKing(0));
            }
            if (board.GetKing(1) != 0 && board[board.GetKing(1)] != Piece.EOU) {
                str.AppendLine("後手玉の位置が不正: GetKing(1)=" + board.GetKing(1));
            }
            // 二歩データ
            var bp = board.DangerousGetPtr();
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                bool existsFU1 = board.ExistsFU(0, file / 0x10);
                bool existsFU2 = BoardUtility.ExistsFU(bp, 0, file);
                bool existsFU3 = board.ExistsFU(1, file / 0x10);
                bool existsFU4 = BoardUtility.ExistsFU(bp, 1, file);
                if (existsFU1 != existsFU2) {
                    str.AppendLine("二歩データの差分計算ミス？: 先手" + (file / 0x10).ToString() + "筋");
                }
                if (existsFU3 != existsFU4) {
                    str.AppendLine("二歩データの差分計算ミス？: 後手" + (file / 0x10).ToString() + "筋");
                }
            }
            // 利き
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Board.Padding; rank <= 9 + Board.Padding; rank++) {
                    int control1 = board.GetControl(0, file + rank);
                    int control2 = BoardUtility.GetControl(bp, 0, file + rank);
                    int control3 = board.GetControl(1, file + rank);
                    int control4 = BoardUtility.GetControl(bp, 1, file + rank);
                    if (control1 != control2) {
                        str.AppendLine("利き差分計算ミス？(先手): " + (file + rank - Board.Padding).ToString("x"));
                    }
                    if (control3 != control4) {
                        str.AppendLine("利き差分計算ミス？(後手): " + (file + rank - Board.Padding).ToString("x"));
                    }
                }
            }

            return str.Length <= 0 ? RegularString : str.ToString();
        }
    }
}
