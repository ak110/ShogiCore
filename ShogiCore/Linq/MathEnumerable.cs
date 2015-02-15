using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Linq {
    /// <summary>
    /// Linq的なもの
    /// </summary>
    public static class MathEnumerable {
        /// <summary>
        /// 中央値
        /// </summary>
        /// <exception cref="System.InvalidOperationException">ソース シーケンスが空の場合</exception>
        public static double Median(this IEnumerable<int> source) {
            var count = source.Count();
            return count % 2 == 0 ?
                source.OrderBy(x => x).Skip(count / 2 - 1).Take(2).Average() :
                source.OrderBy(x => x).Skip(count / 2).First();
        }

        /// <summary>
        /// 中央値
        /// </summary>
        /// <exception cref="System.InvalidOperationException">ソース シーケンスが空の場合</exception>
        public static double Median(this IEnumerable<double> source) {
            var count = source.Count();
            return count % 2 == 0 ?
                source.OrderBy(x => x).Skip(count / 2 - 1).Take(2).Average() :
                source.OrderBy(x => x).Skip(count / 2).First();
        }
    }
}
