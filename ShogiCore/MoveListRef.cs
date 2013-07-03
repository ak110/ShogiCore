using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// List&lt;Move&gt; 的なもの。今のところデバッグ用。
    /// </summary>
    public class MoveListRef : IList<Move> {
        Move[] buffer;
        int offset;

        /// <summary>
        /// 初期化
        /// </summary>
        public MoveListRef(Move[] buffer, int offset, int count) {
            this.buffer = buffer;
            this.offset = offset;
            this.Count = count;
        }

        #region IList<Move> メンバ

        public int IndexOf(Move item) {
            int n = Array.IndexOf(buffer, item, offset, Count);
            if (n < 0) return n;
            return n - offset;
        }

        public void Insert(int index, Move item) {
            for (int i = index; i < Count; i++) {
                buffer[offset + i + 1] = buffer[offset + i];
                // ※ここは1個はみ出る
            }
            buffer[offset + index] = item;
            Count++;
        }

        public void RemoveAt(int index) {
            for (int i = index; i < Count - 1; i++) {
                buffer[offset + i] = buffer[offset + i + 1];
                // ※ ここははみ出す必要ない
            }
            Count--;
        }

        public Move this[int index] {
            get { return buffer[offset + index]; }
            set { buffer[offset + index] = value; }
        }

        #endregion

        #region ICollection<Move> メンバ

        public void Add(Move item) {
            buffer[offset + Count++] = item;
        }

        public void Clear() {
            Count = 0;
        }

        public bool Contains(Move item) {
            return 0 < IndexOf(item);
        }

        public void CopyTo(Move[] array, int arrayIndex) {
            Array.Copy(buffer, offset, array, arrayIndex, Count);
        }

        public int Count { get; private set; }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(Move item) {
            int n = IndexOf(item);
            if (0 < n) {
                RemoveAt(n);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<Move> メンバ

        public IEnumerator<Move> GetEnumerator() {
            for (int i = 0; i < Count; i++) {
                yield return buffer[offset + i];
            }
        }

        #endregion

        #region IEnumerable メンバ

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// TrueForAll
        /// </summary>
        public bool TrueForAll(Predicate<Move> pred) {
            for (int i = 0; i < Count; i++) {
                if (!pred(buffer[offset + i])) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// List&lt;Move&gt; 的なもの。今のところデバッグ用。
    /// </summary>
    public unsafe class MoveListRefPtr : IList<Move> {
        readonly Move* buffer;
        readonly int offset;

        /// <summary>
        /// 初期化
        /// </summary>
        public MoveListRefPtr(Move* buffer, int offset, int count) {
            this.buffer = buffer;
            this.offset = offset;
            this.Count = count;
        }

        #region IList<Move> メンバ

        public int IndexOf(Move item) {
            for (int i = 0; i < Count; i++) {
                if (buffer[i + offset].Equals(item)) {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, Move item) {
            for (int i = index; i < Count; i++) {
                buffer[offset + i + 1] = buffer[offset + i];
                // ※ここは1個はみ出る
            }
            buffer[offset + index] = item;
            Count++;
        }

        public void RemoveAt(int index) {
            for (int i = index; i < Count - 1; i++) {
                buffer[offset + i] = buffer[offset + i + 1];
                // ※ ここははみ出す必要ない
            }
            Count--;
        }

        public Move this[int index] {
            get { return buffer[offset + index]; }
            set { buffer[offset + index] = value; }
        }

        #endregion

        #region ICollection<Move> メンバ

        public void Add(Move item) {
            buffer[offset + Count++] = item;
        }

        public void Clear() {
            Count = 0;
        }

        public bool Contains(Move item) {
            return 0 < IndexOf(item);
        }

        public void CopyTo(Move[] array, int arrayIndex) {
            if (array.Length < arrayIndex + Count) {
                throw new ArgumentOutOfRangeException();
            }

            fixed (Move* p = array) {
                Utility.CopyMemory(p + arrayIndex, buffer + offset, sizeof(Move) * Count);
            }
        }

        public int Count { get; private set; }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(Move item) {
            int n = IndexOf(item);
            if (0 < n) {
                RemoveAt(n);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<Move> メンバ

        public IEnumerator<Move> GetEnumerator() {
            return new Enumerator(this);
            // ↓unsafeだと何故かこれ使えない
            /*
            for (int i = 0; i < count; i++) {
                yield return buffer[offset + i];
            }
            //*/
        }

        #endregion

        #region IEnumerable メンバ

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        class Enumerator : IEnumerator<Move> {
            int index = 0;
            MoveListRefPtr parent;

            public Enumerator(MoveListRefPtr parent) {
                this.parent = parent;
            }

            #region IEnumerator<Move> メンバ

            public Move Current {
                get { return parent[index]; }
            }

            #endregion

            #region IDisposable メンバ

            public void Dispose() {
            }

            #endregion

            #region IEnumerator メンバ

            object System.Collections.IEnumerator.Current {
                get { return Current; }
            }

            public bool MoveNext() {
                return ++index < parent.Count;
            }

            public void Reset() {
                index = 0;
            }

            #endregion
        }

        /// <summary>
        /// TrueForAll
        /// </summary>
        public bool TrueForAll(Predicate<Move> pred) {
            for (int i = 0; i < Count; i++) {
                if (!pred(buffer[offset + i])) return false;
            }
            return true;
        }
    }
}
