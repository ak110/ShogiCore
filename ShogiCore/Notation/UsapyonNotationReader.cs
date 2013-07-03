using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// CSA将棋・うさぴょんの定跡データの読み込みを行う
    /// </summary>
    public class UsapyonNotationReader : IBinaryNotationReader {
        #region IBinaryNotationReader メンバ

        public bool CanRead(byte[] data) {
            return data != null && data.Length % 512 == 0;
        }

        public IEnumerable<Notation> Read(byte[] data) {
            if (!CanRead(data)) {
                throw new NotationFormatException("CSA将棋定跡データの読み込みに失敗しました");
            }

            for (int i = 0; i < data.Length; i += 512) {
                yield return LoadGame(data, i);
            }
        }

        #endregion

        /// <summary>
        /// 1ゲーム分の読み込み
        /// </summary>
        private Notation LoadGame(byte[] data, int i) {
            List<MoveDataEx> moves = new List<MoveDataEx>();

            for (int j = 0; j < 512; j += 2) {
                int from = data[i + j + 1];
                if (from == 0x00 || from == 0xff) break;
                int to = data[i + j];

                moves.Add(new MoveDataEx(new MoveData(from, to)));
            }

            Notation notation = new Notation();
            notation.Moves = moves.ToArray();
            //notation.Winner = (moves.Count % 2) ^ 1; // 最後の手を指した方が勝者とする。(反則とかもあるのだが…)
            notation.Winner = -1;
            return notation;
        }
    }
}
