using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// ソート済みのlong配列
    /// </summary>
    public class SortedListOfLong {
        long[] array;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="capacity">初期容量</param>
        public SortedListOfLong(int capacity = 2) {
            Count = 0;
            array = new long[capacity];
        }

        /// <summary>
        /// 現在の容量
        /// </summary>
        public int Capacity { get { return array.Length; } }

        /// <summary>
        /// 要素の個数
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 追加
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>既に存在したらfalse</returns>
        public bool Add(long value) {
            int left = 0;
            for (int right = Count - 1; left <= right; ) {
                int i = left + (right - left) / 2;
                if (array[i] == value) {
                    return false;
                }
                if (array[i] < value) {
                    left = i + 1;
                } else {
                    right = i - 1;
                }
            }
            InternalInsert(left, value);
            return true;
        }

        /// <summary>
        /// 空にする
        /// </summary>
        public void Clear() {
            Count = 0;
        }

        /// <summary>
        /// 削除
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>削除出来たらtrue</returns>
        public bool Remove(long value) {
            int index = IndexOf(value);
            if (index < 0) return false;
            return RemoveAt(index);
        }

        /// <summary>
        /// 削除
        /// </summary>
        /// <param name="index">序数</param>
        /// <returns>削除出来たらtrue</returns>
        public bool RemoveAt(int index) {
            // index以降を詰める
            Count--;
            for (int i = index, n = Count; i < n; i++) {
                array[i] = array[i + 1];
            }
            DebugAssertSorted();
            return true;
        }

        /// <summary>
        /// itemが含まれていたらtrue
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>含まれていたらtrue</returns>
        public bool Contains(long value) {
            return 0 <= IndexOf(value);
        }

        /// <summary>
        /// itemのindexを返す
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>序数</returns>
        public int IndexOf(long value) {
            for (int left = 0, right = Count - 1; left <= right; ) {
                int center = left + (right - left) / 2;
                if (array[center] == value) {
                    return center;
                }
                if (array[center] < value) {
                    left = center + 1;
                } else {
                    right = center - 1;
                }
            }
            return -1;
        }

        /// <summary>
        /// 挿入
        /// </summary>
        private void InternalInsert(int index, long item) {
            if (Capacity < Count + 1) { // ←1個ずつなのでここは1回で必ず足りる
                Array.Resize(ref array, array.Length * 2);
            }
            // 後ろに移動
            for (int i = Count - 1; index <= i; i--) {
                array[i + 1] = array[i];
            }
            // 追加
            array[index] = item;
            Count++;
            DebugAssertSorted();
        }

        /// <summary>
        /// ソート状態が保たれてる事を確認
        /// </summary>
        [Conditional("DEBUG")]
        private void DebugAssertSorted() {
            for (int i = 0 + 1; i < Count; i++)
                Debug.Assert(array[i - 1] < array[i]);
        }
    }
}
