
#if !DANGEROUS
#define USE_SFC
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// 成功・失敗の回数を数えて、統計情報として出力したりするための補助クラス。
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct SuccessFailureCounter {
#if USE_SFC
        /// <summary>
        /// 成功回数
        /// </summary>
        public uint Success { get; set; }
        /// <summary>
        /// 失敗回数
        /// </summary>
        public uint Failure { get; set; }
#else
        /// <summary>
        /// 成功回数
        /// </summary>
        public uint Success { get { return 0; } set { } }
        /// <summary>
        /// 失敗回数
        /// </summary>
        public uint Failure { get { return 0; } set { } }
#endif

        /// <summary>
        /// 合計回数
        /// </summary>
        public long Total {
#if USE_SFC
            get { return (long)Success + Failure; }
#else
            get { return 0; }
#endif
        }
        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate {
#if USE_SFC
            get {
                unchecked {
                    long total = Total;
                    if (total == 0) return 0;
                    return (double)Success / total;
                }
            }
#else
            get { return 0; }
#endif
        }

        /// <summary>
        /// リセット。
        /// </summary>
        public void Clear() {
#if USE_SFC
            Success = 0;
            Failure = 0;
#endif
        }

        /// <summary>
        /// 成功
        /// </summary>
        public void CountSuccess() {
#if USE_SFC
            unchecked { Success++; }
#endif
        }
        /// <summary>
        /// 失敗
        /// </summary>
        public void CountFailure() {
#if USE_SFC
            unchecked { Failure++; }
#endif
        }

        /// <summary>
        /// 成功or失敗
        /// </summary>
        public void Count(bool success) {
#if USE_SFC
            if (success) CountSuccess();
            else CountFailure();
#endif
        }

#if USE_SFC
        /// <summary>
        /// 適当文字列化
        /// </summary>
        public override string ToString() {
            return (SuccessRate * 100).ToString("##0.0").PadLeft(6) + "%, " +
                Success.ToString() + " / " +
                Total.ToString();
        }
#else
        public override string ToString() {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// 加算
        /// </summary>
        public void Add(SuccessFailureCounter operand) {
#if USE_SFC
            unchecked {
                Success += operand.Success;
                Failure += operand.Failure;
            }
#endif
        }
    }

    /// <summary>
    /// SuccessFailureCounterのSum()。
    /// </summary>
    public static class SuccessFailureCounterSum {
        /// <summary>
        /// SuccessFailureCounterのSum()。
        /// </summary>
        public static SuccessFailureCounter Sum(this IEnumerable<SuccessFailureCounter> source) {
            SuccessFailureCounter sum = new SuccessFailureCounter();
            foreach (SuccessFailureCounter sfc in source) sum.Add(sfc);
            return sum;
        }
        /// <summary>
        /// SuccessFailureCounterのSum()。
        /// </summary>
        public static SuccessFailureCounter Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, SuccessFailureCounter> selector) {
            SuccessFailureCounter sum = new SuccessFailureCounter();
            foreach (var s in source) sum.Add(selector(s));
            return sum;
        }
    }
}
