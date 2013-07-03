using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// monoでLinqの一部が上手く動かないっぽいので自前実装
    /// </summary>
    public static class LinqEmu {
        /// <summary>
        /// Sum()
        /// </summary>
        public static int Sum(IEnumerable<int> source) {
            int sum = 0;
            foreach (var s in source) sum += s;
            return sum;
        }
        /// <summary>
        /// Sum()
        /// </summary>
        public static long Sum(IEnumerable<long> source) {
            long sum = 0;
            foreach (var s in source) sum += s;
            return sum;
        }
        /// <summary>
        /// Sum()
        /// </summary>
        public static int Sum<TSource>(IEnumerable<TSource> source, Func<TSource, int> selector) {
            int sum = 0;
            foreach (var s in source) sum += selector(s);
            return sum;
        }
        /// <summary>
        /// Sum()
        /// </summary>
        public static long Sum<TSource>(IEnumerable<TSource> source, Func<TSource, long> selector) {
            long sum = 0;
            foreach (var s in source) sum += selector(s);
            return sum;
        }
        /// <summary>
        /// Average()
        /// </summary>
        public static double Average(IEnumerable<int> source) {
            return (double)Sum(source) / Count(source); // 手抜き実装
        }
        /// <summary>
        /// Average()
        /// </summary>
        public static double Average(IEnumerable<long> source) {
            return (double)Sum(source) / Count(source); // 手抜き実装
        }

        /// <summary>
        /// Max()
        /// </summary>
        public static TSource Max<TSource>(IEnumerable<TSource> source) {
            Comparer<TSource> comparer = Comparer<TSource>.Default;
            TSource item = default(TSource);
            if (item == null) {
                foreach (TSource t in source) {
                    if (t != null &&
                        (item == null || comparer.Compare(t, item) > 0)) {
                        item = t;
                    }
                }
                return item;
            } else {
                bool exists = false;
                foreach (TSource t in source) {
                    if (exists) {
                        if (comparer.Compare(t, item) > 0) {
                            item = t;
                        }
                    } else {
                        item = t;
                        exists = true;
                    }
                }
                return item;
            }
        }
        /// <summary>
        /// Max()
        /// </summary>
        public static TResult Max<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector) {
            return Max(Select(source, selector));
        }

        /// <summary>
        /// Select()
        /// </summary>
        public static IEnumerable<TResult> Select<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector) {
            foreach (TSource item in source) {
                yield return selector(item);
            }
        }

        /// <summary>
        /// Count()
        /// </summary>
        public static int Count<TSource>(IEnumerable<TSource> source) {
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null) {
                return collection.Count;
            }
            int count = 0;
            using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
                while (enumerator.MoveNext()) count++;
            }
            return count;
        }

        /// <summary>
        /// Count()
        /// </summary>
        public static int Count<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate) {
            int c = 0;
            foreach (TSource local in source) {
                if (predicate(local)) c++;
            }
            return c;
        }
    }
}
