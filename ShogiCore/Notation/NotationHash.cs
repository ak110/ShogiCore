using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋譜のハッシュ値を作る。
    /// </summary>
    public static partial class NotationHash {
        /// <summary>
        /// 棋譜のハッシュ値を算出して返す。
        /// </summary>
        /// <remarks>
        /// 初期盤面とかは無視するので注意。
        /// </remarks>
        public static long GetHash(INotation notation) {
            unchecked {
                ulong seed = 0x3621407d39891259ul;
                for (int i = 0; i < notation.Moves.Length; i++) {
                    MoveData m = notation.Moves[i].MoveData;
                    seed += Seed[(m.From ^ seed) & 0xff] + Seed[(m.To ^ seed) & 0xff];
                }
                return (long)seed;
            }
        }

        /// <summary>
        /// 棋譜の重複の削除を行う
        /// </summary>
        public static void RemoveDuplications(IList<Notation> notations) {
            // コピる
            Notation[] array = new Notation[notations.Count];
            notations.CopyTo(array, 0);
            notations.Clear();
            // 重複してないのだけを追加
            HashSet<long> hashTable = new HashSet<long>();
            foreach (Notation item in array) {
                long hash = GetHash(item);
                if (hashTable.Add(hash)) {
                    notations.Add(item);
                }
            }
        }
        /// <summary>
        /// notationsのうち、originalに含まれるものを全て削除
        /// </summary>
        public static void RemoveAllUnion(List<Notation> notations, List<Notation> original) {
            HashSet<long> hashTable = new HashSet<long>();
            for (int i = 0, n = original.Count; i < n; i++) hashTable.Add(GetHash(original[i]));

            notations.RemoveAll(x => hashTable.Contains(GetHash(x)));
        }

        /// <summary>
        /// ハッシュ値の比較。
        /// </summary>
        public static int Compare(Notation a, Notation b) {
            return GetHash(a).CompareTo(GetHash(b));
        }
    }
}
