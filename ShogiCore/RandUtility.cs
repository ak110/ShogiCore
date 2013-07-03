using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 乱数
    /// </summary>
    public static class RandUtility {
        //*
        static Toolkit.MersenneTwister rand = Toolkit.MersenneTwister.CreateRandom();
        /*/
        static Random rand = new Random();
        //*/

        /// <summary>
        /// インスタンスの取得
        /// </summary>
        //*
        public static Toolkit.MersenneTwister Instance {
        /*/
        public static Random Instance {
        //*/
            get { return rand; }
        }

        /// <summary>
        /// NextUInt
        /// </summary>
        public static uint NextUInt() { lock (rand) return rand.NextUInt(); }

        /// <summary>
        /// [0, 1024)な乱数
        /// </summary>
        public static uint Next1024() { lock (rand) return rand.NextUInt() % 1024; }

        /// <summary>
        /// [0, maxValue)な乱数
        /// </summary>
        public static int Next(int maxValue) { lock (rand) return rand.Next(maxValue); }

        /// <summary>
        /// シャッフル
        /// </summary>
        public static void Shuffle<T>(IList<T> list, int startIndex = 0) {
            Shuffle(list, startIndex, list.Count - startIndex);
        }
        /// <summary>
        /// シャッフル
        /// </summary>
        public static void Shuffle<T>(IList<T> list, int startIndex, int count) {
            lock (rand) {
                Shuffle(rand, list, startIndex, count);
            }
        }

        /// <summary>
        /// シャッフル
        /// </summary>
        public static void Shuffle<T>(Random rand, IList<T> list, int startIndex = 0) {
            Shuffle(rand, list, startIndex, list.Count - startIndex);
        }
        /// <summary>
        /// シャッフル
        /// </summary>
        public static void Shuffle<T>(Random rand, IList<T> list, int startIndex, int count) {
            int last = startIndex + count;
            for (int i = startIndex; i < last - 1; i++) {
                int r = i + rand.Next(last - i);
                T t = list[i];
                list[i] = list[r];
                list[r] = t;
            }
        }

        /// <summary>
        /// 0が一番多くてmaxが一番少ない三角形な分布の乱数
        /// </summary>
        public static int GetLeftTriangle(int max) {
            lock (rand) {
                return GetLeftTriangle(rand, max);
            }
        }
        /// <summary>
        /// 0が一番多くてmaxが一番少ない三角形な分布の乱数
        /// </summary>
        public static int GetLeftTriangle(Random rand, int max) {
            return Math.Abs(rand.Next(max) + rand.Next(max) - (max - 1));
        }
        /// <summary>
        /// 0が一番少なくてmaxが一番多い三角形な分布の乱数
        /// </summary>
        public static int GetRightTriangle(int max) {
            lock (rand) {
                return GetRightTriangle(rand, max);
            }
        }
        /// <summary>
        /// 0が一番少なくてmaxが一番多い三角形な分布の乱数
        /// </summary>
        public static int GetRightTriangle(Random rand, int max) {
            return max - GetLeftTriangle(rand, max);
        }

        /// <summary>
        /// 正規分布な乱数
        /// </summary>
        /// <remarks>
        /// ボックスミューラー法
        /// </remarks>
        /// <param name="u">平均</param>
        /// <param name="sigma">標準偏差</param>
        /// <returns>正規分布な乱数</returns>
        public static double GetGaussian(double u, double sigma) {
            lock (rand) {
                double x = rand.NextDouble(), y = rand.NextDouble(); // 0～1
                double resultA = Math.Sqrt((-2 * Math.Log(x))) * (Math.Cos(2 * Math.PI * y));
                //double resultB = Math.Sqrt((-2 * Math.Log(y))) * (Math.Cos(2 * Math.PI * x));
                return (resultA * sigma) + u;
            }
        }
        /// <summary>
        /// 標準正規分布な乱数
        /// </summary>
        /// <returns>標準正規分布な乱数</returns>
        public static double GetStandardGaussian() {
            return GetGaussian(0.0, 1.0);
        }

        /// <summary>
        /// 配列を乱数埋めする
        /// </summary>
        public static void FillRandom(sbyte[][][] x) {
            foreach (var xx in x) FillRandom(xx);
        }
        /// <summary>
        /// 配列を乱数埋めする
        /// </summary>
        public static void FillRandom(sbyte[][] x) {
            foreach (var xx in x) FillRandom(xx);
        }
        /// <summary>
        /// 配列を乱数埋めする
        /// </summary>
        public static void FillRandom(sbyte[] x) {
            lock (rand) {
                for (int i = 0; i < x.Length; i++) x[i] = unchecked((sbyte)rand.NextUInt());
                // ↑手抜き気味。
            }
        }
        /// <summary>
        /// 配列を乱数埋めする
        /// </summary>
        public static void FillRandom(byte[] x) {
            lock (rand) {
                for (int i = 0; i < x.Length; i++) x[i] = unchecked((byte)rand.NextUInt());
                // ↑手抜き気味。
            }
        }
    }
}
