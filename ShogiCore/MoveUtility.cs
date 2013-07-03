using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using ShogiCore.Notation;

namespace ShogiCore {
    /// <summary>
    /// Move[]とか用ヘルパメソッド。
    /// </summary>
    public static unsafe class MoveUtility {
        /// <summary>
        /// クイックソート内でInsertionSortる最大個数
        /// </summary>
        const int InsertionSortMaxCount = 15;

        /// <summary>
        /// PV表示とかっぽい感じの文字列化。
        /// 静止探索部分は括弧書きしたものを返す。
        /// </summary>
        /// <param name="normalCount">通常探索部の長さ。-1ですべて通常探索</param>
        public static string ToPVString(this IEnumerable<Move> moves, int normalCount, Board board, int turn) {
            if (moves == null) return "<不明>";

            StringBuilder str = new StringBuilder();
            Move last = board.GetLastMove();
            var boardCopy = board.CopyToArray();
            int count = 0;
            bool quies = false;
            foreach (Move move in moves) {
                if (count == normalCount) {
                    quies = true;
                    str.Append('【');
                }
                str.Append(move.ToString(turn, boardCopy[move.From], last));
                last = move;

                BoardUtility.Do(boardCopy, move, turn);
                turn ^= 1;
                count++;
            }
            if (str.Length <= 0) return "<エラー>";
            if (quies) {
                str.Append('】');
            }
            return str.ToString();
        }

        /// <summary>
        /// PV表示とかっぽい感じの文字列化。
        /// 静止探索部分は括弧書きしたものを返す。
        /// </summary>
        /// <param name="normalCount">通常探索部の長さ</param>
        public static string ToPVString(this IEnumerable<MoveData> moves, int normalCount, Board board, int turn) {
            if (moves == null) return "<不明>";

            StringBuilder str = new StringBuilder();
            Move last = board.GetLastMove();
            Board boardCopy = board.Clone();
            int count = 0;
            bool quies = false;
            foreach (MoveData moveData in moves) {
                if (count == normalCount) {
                    quies = true;
                    str.Append('【');
                }
                Move move = Move.FromNotation(boardCopy, moveData);
                str.Append(move.ToString(turn, boardCopy[move.From], last));
                last = move;

                boardCopy.Do(move);
                turn ^= 1;
                count++;
            }
            if (str.Length <= 0) return "<エラー>";
            if (quies) {
                str.Append('】');
            }
            return str.ToString();
        }

        /// <summary>
        /// デバッグとか用簡易文字列化。厳密な表現じゃないので注意。
        /// </summary>
        public static string ToSimpleString(this IEnumerable<Move> moves, Board board) {
            if (moves == null) return "<不明>";
            if (board == null) return "<null>";

            StringBuilder str = new StringBuilder();
            Move last = board.GetLastMove();
            bool first = true;
            var boardCopy = board.CopyToArray();
            int turn = board.Turn;
            foreach (Move move in moves) {
                if (first) {
                    first = false;
                } else {
                    str.Append(' ');
                }

                str.Append(move.ToStringSimple(boardCopy[move.From], last));
                last = move;

                BoardUtility.Do(boardCopy, move, turn);
                turn ^= 1;
            }

            if (str.Length <= 0) return "<エラー>";
            return str.ToString();
        }

        /// <summary>
        /// indexの要素を削除
        /// </summary>
        public static int RemoveAt(Move[] moves, int index, int last) {
            last--;
            for (int i = index; i < last; i++) {
                moves[i] = moves[i + 1];
            }
            return last;
        }

        /// <summary>
        /// 該当するものを全て削除
        /// </summary>
        public static int RemoveAll(Move[] moves, int index, int last, Predicate<Move> pred) {
            int removeCount = 0;
            for (int i = index; i < last; ) {
                if (pred(moves[i])) {
                    removeCount++;
                    last--;
                } else {
                    i++;
                }
                if (0 < removeCount) moves[i] = moves[i + removeCount];
            }
            return last;
        }

        /// <summary>
        /// 該当するものを全て削除
        /// </summary>
        public static int RemoveAll(Move* moves, int index, int last, Predicate<Move> pred) {
            int removeCount = 0;
            for (int i = index; i < last; ) {
                if (pred(moves[i])) {
                    removeCount++;
                    last--;
                } else {
                    i++;
                }
                if (0 < removeCount) moves[i] = moves[i + removeCount];
            }
            return last;
        }

        public delegate bool IndexedPredicate<in T, in I>(T value, I index);
        /// <summary>
        /// 該当するものを全て削除
        /// </summary>
        public static int RemoveAll<T>(Move[] moves, int index, int last, T[] sort, int sortOffset, Func<Move, T, bool> pred) {
            int removeCount = 0;
            for (int i = index; i < last; ) {
                if (pred(moves[i], sort[i + sortOffset])) {
                    removeCount++;
                    last--;
                } else {
                    i++;
                }
                if (0 < removeCount) {
                    moves[i] = moves[i + removeCount];
                    sort[i + sortOffset] = sort[i + removeCount + sortOffset];
                }
            }
            return last;
        }

        #region ソート

        /// <summary>
        /// ソート
        /// </summary>
        /// <param name="moves">配列</param>
        /// <param name="index">開始index</param>
        /// <param name="last">終了index + 1</param>
        /// <param name="sort">ソートの基準の値</param>
        /// <param name="sortOffset">開始indexの差。moves[index]にsort[index + sortOffset]が対応</param>
        public static void SortByValue(Move[] moves, int index, int last, short[] sort, int sortOffset) {
            QuickSort(moves, index, last - 1, sort, sortOffset);
            Debug.Assert(last - index <= 1 || sort[last - 1 + sortOffset] <= sort[index + sortOffset]);
        }

        /// <summary>
        /// クイックソート
        /// </summary>
        private static void QuickSort(Move[] data, int left, int right, short[] sort, int sortOffset) {
            if (right - left <= InsertionSortMaxCount) {
                InsertionSort(data, left, right, sort, sortOffset);
                return;
            }
            do {
                int a = left;
                int b = right;
                int t = a + ((b - a) >> 1);
                SwapIfGT(data, a, t, sort, sortOffset);
                SwapIfGT(data, a, b, sort, sortOffset);
                SwapIfGT(data, t, b, sort, sortOffset);
                var y = sort[t + sortOffset];
                do {
                    while (y < sort[a + sortOffset]) a++;
                    while (sort[b + sortOffset] < y) b--;

                    if (b < a) break;
                    if (a < b) {
                        var tmp1 = data[a];
                        data[a] = data[b];
                        data[b] = tmp1;
                        var tmp2 = sort[a + sortOffset];
                        sort[a + sortOffset] = sort[b + sortOffset];
                        sort[b + sortOffset] = tmp2;
                    }
                    a++;
                    b--;
                }
                while (a <= b);

                if ((b - left) <= (right - a)) {
                    if (left < b) {
                        QuickSort(data, left, b, sort, sortOffset);
                    }
                    left = a;
                } else {
                    if (a < right) {
                        QuickSort(data, a, right, sort, sortOffset);
                    }
                    right = b;
                }
            } while (left < right);
        }

        /// <summary>
        /// 比較と置換
        /// </summary>
        private static void SwapIfGT(Move[] data, int a, int b, short[] sort, int sortOffset) {
            if (a != b && sort[a + sortOffset] < sort[b + sortOffset]) {
                var tmp1 = data[a];
                data[a] = data[b];
                data[b] = tmp1;
                var tmp2 = sort[a + sortOffset];
                sort[a + sortOffset] = sort[b + sortOffset];
                sort[b + sortOffset] = tmp2;
            }
        }

        /// <summary>
        /// 挿入ソート
        /// </summary>
        public static void InsertionSort(Move[] data, int left, int right, short[] sort, int sortOffset) {
            // yaneurao's insertion sort
            for (int i = left + 1; i < right + 1; i++) {
                var tmp1 = data[i];
                var tmp2 = sort[i + sortOffset];
                if (sort[i - 1 + sortOffset] < tmp2) {
                    int j = i;
                    do {
                        data[j] = data[j - 1];
                        sort[j + sortOffset] = sort[j - 1 + sortOffset];
                        --j;
                    } while (left < j && sort[j - 1 + sortOffset] < tmp2);
                    data[j] = tmp1;
                    sort[j + sortOffset] = tmp2;
                }
            }
        }

        #endregion
    }
}
