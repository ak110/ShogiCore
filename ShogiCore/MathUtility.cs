using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// 平均を算出
    /// </summary>
    public struct AverageCounter {
        /// <summary>
        /// 合計値
        /// </summary>
        public long SumValue { get; private set; }
        /// <summary>
        /// 加算回数
        /// </summary>
        public int Count { get; private set; }
        /// <summary>
        /// 平均値
        /// </summary>
        public double Average { get { return (double)SumValue / Count; } }
        /// <summary>
        /// 加算
        /// </summary>
        /// <param name="value"></param>
        public void Add(int value) {
            SumValue += value;
            Count++;
        }
        /// <summary>
        /// 加算
        /// </summary>
        /// <param name="other"></param>
        public void Add(AverageCounter other) {
            SumValue += other.SumValue;
            Count += other.Count;
        }
        /// <summary>
        /// 加算
        /// </summary>
        public static AverageCounter Sum(IEnumerable<AverageCounter> counters) {
            AverageCounter sum = new AverageCounter();
            foreach (AverageCounter c in counters) sum.Add(c);
            return sum;
        }
    }

    /// <summary>
    /// ビット演算とか数学・統計関係の処理とか。
    /// </summary>
    /// <remarks>
    /// 統計関数ソースコード for C++(Windows95/98/Me / プログラミング)
    /// http://www.vector.co.jp/soft/win95/prog/se153377.html
    /// とかをかなりコピペ。
    ///
    /// あとあちこち適当なので要注意。
    /// </remarks>
    public static class MathUtility {
        /// <summary>
        /// オーバーフローしないよう注意しつつ足し算
        /// </summary>
        public static void SafeAdd(ref short vl, int x) {
            vl = (short)Math.Min(Math.Max(vl + x, -short.MaxValue), short.MaxValue);
        }

        /// <summary>
        /// ビットの立ってる数を数える。
        /// </summary>
        /// <remarks>
        /// http://d.hatena.ne.jp/mclh46/20100408/1270737141
        /// </remarks>
        public static int PopCnt(uint b) {
            unchecked {
                /*
                b = (b & 0x55555555) + (b >> 1 & 0x55555555);
                b = (b & 0x33333333) + (b >> 2 & 0x33333333);
                b = (b & 0x0f0f0f0f) + (b >> 4 & 0x0f0f0f0f);
                b = (b & 0x00ff00ff) + (b >> 8 & 0x00ff00ff);
                b = (b & 0x0000ffff) + (b >> 16 & 0x0000ffff);
                return (int)b;
                /*/
                b -= (b >> 1) & 0x55555555u;
                b = (b & 0x33333333u) + ((b >> 2) & 0x33333333u);
                return (int)((((b + (b >> 4)) & 0x0f0f0f0fu) * 0x01010101u) >> 24);
                //*/
            }
        }

        /// <summary>
        /// ビットの立ってる数を数える。
        /// </summary>
        public static int PopCnt64(ulong b) {
            unchecked {
                /*
                b = (b & 0x5555555555555555ul) + ((b >>  1) & 0x5555555555555555ul);
                b = (b & 0x3333333333333333ul) + ((b >>  2) & 0x3333333333333333ul);
                b = (b & 0x0f0f0f0f0f0f0f0ful) + ((b >>  4) & 0x0f0f0f0f0f0f0f0ful);
                b = (b & 0x00ff00ff00ff00fful) + ((b >>  8) & 0x00ff00ff00ff00fful);
                b = (b & 0x0000ffff0000fffful) + ((b >> 16) & 0x0000ffff0000fffful);
                b = (b & 0x00000000fffffffful) + ((b >> 32) & 0x00000000fffffffful);
                return (int)b;
                /*/
                b -= (b >> 1) & 0x5555555555555555ul;
                b = (b & 0x3333333333333333u) + ((b >> 2) & 0x3333333333333333u);
                return (int)((((b + (b >> 4)) & 0x0f0f0f0f0f0f0f0ful) * 0x0101010101010101ul) >> 56);
                //*/
            }
        }

        /// <summary>
        /// bsr(ntz)。下位ビットからスキャンして最初に1なbitを返す
        /// </summary>
        public static int BitScanReverse(uint x) {
            unchecked {
                /*
                // ntz = 32 - nlz(~x & (x - 1))
                x = ~x & (x - 1);
                
                int y, m, n;
                y = (int)(-(x >> 16));
                m = (y >> 16) & 16;
                n = 16 - m;
                x >>= m;

                y = (int)(x - 0x100u);
                m = (y >> 16) & 8;
                n += m;
                x <<= m;

                y = (int)(x - 0x1000);
                m = (y >> 16) & 4;
                n += m;
                x <<= m;

                y = (int)(x - 0x4000);
                m = (y >> 16) & 2;
                n += m;
                x <<= m;

                y = (int)(x >> 14);
                m = y & ~(y >> 1);

                return 32 - (n + 2 - m);
                /*/
                x = ~x & (x - 1);
                int n = 0;
                while (x != 0) {
                    n++;
                    x >>= 1;
                }
                return n;
                //*/
            }
        }

        /// <summary>
        /// シグモイド関数
        /// 値域[0, 1]、x=0のとき0.5、x=∞で1、x=-∞で0。
        /// </summary>
        public static double Sigmoid(double x) {
            double nexp = Math.Exp(-x);
            if (double.IsInfinity(nexp)) return 0.0;
            double y = 1 / (1 + nexp);
            Debug.Assert(0.0 <= y && y <= 1.0, "値域が変？");
            return y;
        }

        /// <summary>
        /// シグモイド関数を微分したもの。
        /// 値域[0, 0.25]の偶関数で、x=0の時最大値。
        /// </summary>
        public static double SigmoidDiff(double x) {
            // シグモイド関数 T(x) := 1 / (1 + exp(-x));
            // ∂T(x)/∂x := T(x) * (1 - T(x))
            //             = exp(x)/(exp(x) + 1)^2
            double exp = Math.Exp(x);
            if (double.IsInfinity(exp)) return 0.0;
            double exp1 = exp + 1;
            double y = exp / (exp1 * exp1);
            Debug.Assert(0.0 <= y && y <= 0.25, "値域が変？");
            return y;
        }

        /// <summary>
        /// ロジスティック関数
        /// </summary>
        public static double Logistic(double x) {
            return Math.Log(1.0 + Math.Exp(-x));
        }

        /// <summary>
        /// ロジスティック関数を微分したもの
        /// </summary>
        public static double LogisticDiff(double x) {
            double nexp = Math.Exp(-x);
            return -nexp / (1.0 + nexp);
        }

        /// <summary>
        /// シグモイド関数をT(x)としたとき、T(abs(x))を微分したもの。
        /// 結果はT(x)を微分して符号を掛けたものになる。
        /// </summary>
        public static double SigmoidDiffSigned(double x) {
            return Math.Sign(x) * SigmoidDiff(x);
        }

        /// <summary>
        /// 値が正の要素数と負の要素数をカウントする
        /// </summary>
        /// <param name="diff">差の配列</param>
        /// <param name="np">値が正の要素数</param>
        /// <param name="nm">値が負の要素数</param>
        public static void GetSignCount(int[] diff, out int np, out int nm) {
            np = 0;
            nm = 0;
            foreach (int t in diff) {
                if (t < 0) nm++;
                else if (0 < t) np++;
            }
        }

        /// <summary>
        /// 符号検定。
        /// </summary>
        public static double SignTest(int[] diff) {
            int np, nm;
            return SignTest(diff, out np, out nm);
        }
        /// <summary>
        /// 符号検定。
        /// </summary>
        public static double SignTest(int[] diff, out int np, out int nm) {
            GetSignCount(diff, out np, out nm);
            return SignTest(np, nm);
        }
        /// <summary>
        /// 符号検定。np側に偏ってると言えるかどうかを調べる。
        /// 戻り値は、「偏っていない」といえる確率。
        /// </summary>
        /// <remarks>
        /// np=勝った数、nm=負けた数とすれば有意に勝ち越してるかどうかの検定にも使える。(ただし、引き分けは無視される)
        /// http://aoki2.si.gunma-u.ac.jp/lecture/Average/sign-test.html
        /// http://seib-dgvm.com/hsato/Tool/sign-test.html
        /// </remarks>
        /// <param name="np">値が正の要素数</param>
        /// <param name="nm">値が負の要素数</param>
        /// <returns>片側有意確率。両側なら倍にする。</returns>
        public static double SignTest(int np, int nm) {
            int N = np + nm;
            if (N < 1000) { // オーバーフローしそうなところから近似計算に切り替える
                double p = 0.0; // 片側有意確率
                for (int i = 0, m = Math.Min(np, nm); i < m; i++) {
                    p += Combin(N, i);
                }
                p /= Math.Pow(2, N);
                // 正な側を基準とする
                return nm < np ? p : 1 - p;
            } else {
                double x = (np - N * 0.5) / Math.Sqrt(0.5 * (1.0 - 0.5) * N);
                return 1.0 - NormSDist(x);
            }
        }

        /// <summary>
        /// 引き分け込みの勝ち負けの有意確率を返す (t検定)
        /// </summary>
        /// <param name="win">勝った数</param>
        /// <param name="lose">負けた数</param>
        /// <param name="even">引き分けの数</param>
        /// <returns>片側有意確率。両側なら倍にする。</returns>
        public static double WinLoseEvenTest(int win, int lose, int even) {
            int n = win + lose + even;
            if (n <= 1) return 0.0;
            // 勝ちを1、負けを-1、引き分けを0として、標本標準偏差を求める
            double a = (double)(win - lose) / n; // 標本平均
            double wa = win * (1 - a) * (1 - a);
            double la = lose * (-1 - a) * (-1 - a);
            double ea = even * (0 - a) * (0 - a);
            double s = Math.Sqrt((wa + ea + la) / n); // σ = 1/n Σ(x_i - avg(x))^2
            double t = (a - 0) * Math.Sqrt(n - 1) / s;
            // 自由度n-1のt分布片側確率
            return TDist(t, n - 1);
        }

        /// <summary>
        /// 勝率の信頼区間を算出
        /// </summary>
        /// <remarks>
        /// http://homepage3.nifty.com/nneo/winningrate.html
        /// Clopper and Pearsonの方法（C. J. Clopper and E. S. Pearson, Biometrika 26, pp. 404-413 (1934)）
        /// </remarks>
        /// <param name="win">勝った数</param>
        /// <param name="lose">負けた数</param>
        /// <param name="a">1 - 信頼水準。普通は0.05とかを指定すべし。</param>
        /// <param name="wL">勝率の下限</param>
        /// <param name="wH">勝率の上限</param>
        public static void GetWinConfidence(int win, int lose, double a, out double wL, out double wH) {
            int N = win + lose;
            wL = win <= 0 ? 0.0 : win / (win + (N - win + 1) * FInv(a / 2, 2 * (N - win + 1), 2 * win));
            wH = (win + 1) / (win + 1 + (N - win) * FInv(1 - a / 2, 2 * (N - win), 2 * (win + 1)));
        }

        /// <summary>
        /// 勝率をレーティング差に変換。
        /// </summary>
        /// <remarks>
        /// http://ameblo.jp/professionalhearts/entry-10003265717.html
        /// </remarks>
        /// <param name="winRate">勝率。0.5で勝ち負け同数、0.75で4回に3回勝ち。</param>
        /// <returns>レーティングの差</returns>
        public static double WinRateToRatingDiff(double winRate) {
            if (winRate < 0.0 || 1.0 < winRate) {
                throw new ArgumentOutOfRangeException("winRate");
            } else if (winRate <= 0.0) {
                return 0.0;
            }
            return -400 * Math.Log(1.0 / winRate - 1.0, 10.0);
        }
        /// <summary>
        /// レーティング差を勝率に変換
        /// </summary>
        /// <remarks>
        /// WinRateToRatingDiff()の逆関数。
        /// Bradley-Terry Modelの期待勝率そのまま。
        /// </remarks>
        /// <param name="dr">レーティングの差</param>
        /// <returns>期待勝率</returns>
        public static double RatingDiffToWinRate(double dr) {
            return 1.0 / (Math.Pow(10.0, -dr / 400.0) + 1.0);
        }

        /// <summary>
        /// ウィルコクソンの符号化順位検定の為の諸々を算出
        /// </summary>
        /// <param name="diff">差の配列</param>
        /// <param name="N">有効ペア数</param>
        /// <param name="rankSumN">負の側の順位合計</param>
        /// <param name="rankSumP">正の側の順位合計</param>
        public static void GetWilcoxonValues(int[] diff, out int N, out double rankSumN, out double rankSumP) {
            // 0を除いてコピる
            List<int> list = new List<int>(diff.Length);
            foreach (int t in diff) {
                if (t == 0) {
                    // 差が無いのは無視
                } else {
                    list.Add(t);
                }
            }
            // 絶対値
            N = list.Count;
            int[] abs = new int[N];
            for (int i = 0; i < abs.Length; i++) {
                abs[i] = Math.Abs(list[i]);
            }
            // 順位
            double[] rank = new double[N];
            for (int i = 0; i < abs.Length; i++) {
                int r = 1; // より小さいのの数
                int eq = 0; // 同一順位の個数 (重複が無ければ1)
                foreach (int t in abs) {
                    if (abs[i] == t) {
                        eq++;
                    } else if (t < abs[i]) {
                        r++;
                    }
                }
                // ↓なんかややこしい。。(´ω`)
                rank[i] = eq == 1 ? r : (double)(r + (r + eq - 1)) / 2;
            }
            // 順位合計
            rankSumN = 0;
            rankSumP = 0;
            for (int i = 0; i < rank.Length; i++) {
                if (list[i] < 0) {
                    rankSumN += rank[i];
                } else {
                    rankSumP += rank[i];
                }
            }
        }

        /// <summary>
        /// ウィルコクソンの符号化順位検定
        /// </summary>
        /// <returns>有意確率</returns>
        public static double WilcoxonTest(int[] diff) {
            int N;
            double rankSumP, rankSumN;
            return WilcoxonTest(diff, out N, out rankSumP, out rankSumN);
        }
        /// <summary>
        /// ウィルコクソンの符号化順位検定
        /// </summary>
        /// <returns>有意確率</returns>
        public static double WilcoxonTest(int[] diff,
            out int N, out double rankSumP, out double rankSumN) {
            GetWilcoxonValues(diff, out N, out rankSumP, out rankSumN);
            return WilcoxonTest(N, rankSumP, rankSumN);
        }
        /// <summary>
        /// ウィルコクソンの符号化順位検定
        /// </summary>
        /// <remarks>
        /// http://aoki2.si.gunma-u.ac.jp/lecture/Average/mpsr-test.html
        /// http://seib-dgvm.com/hsato/Tool/Wilcoxon.html
        /// </remarks>
        /// <returns>有意確率</returns>
        public static double WilcoxonTest(int N, double rankSumP, double rankSumN) {
            double T = Math.Min(rankSumN, rankSumP);

            const bool useExact = false;

            double p;
            if (N < 30 && useExact) { // ←適当
                int limit = (N + 1) * N / 2 + 1;
                double[] table = new double[limit];

                for (int i = 0; i < limit; i++) {
                    table[i] = 0;
                }
                table[0] = 1;

                for (int n = 1, sum = 0; n <= N; sum += n++) {
                    for (int i = sum; i >= 0; i--) {
                        table[i + n] += table[i];
                    }
                }

                double denom = Math.Pow(2.0, -N);

                if (T < 0) {
                    T = 0;
                } else if (T >= limit) {
                    T = limit - 1;
                }

                double total = 0;
                p = 0;
                for (int i = 0; i < limit; i++) {
                    total += (double)table[i] * denom;
                    if (T == i) {
                        p = total * 2;
                    }
                }
            } else {
                // 正規分布による近似
                double Z0 = Math.Abs(T - N * (N + 1) / 4.0) /
                    Math.Sqrt(N * (N + 1) * (2 * N + 1) / 24.0);
                // 有意確率 P = Pr{| Z | ≧ Z0}
                p = NormSDist(-Z0) * 2;
            }

            // 正な側を基準とする
            return rankSumP < rankSumN ? p : 1 - p;
        }

        /// <summary>
        /// 2標本の検定
        /// </summary>
        /// <param name="np1">勝った回数（a vs b）</param>
        /// <param name="nm1">負けた回数（a vs b）</param>
        /// <param name="np2">勝った回数（a' vs b）</param>
        /// <param name="nm2">負けた回数（a' vs b）</param>
        /// <returns>aよりa'が強いか否かの検定の有意確率。（適当表現）</returns>
        public static double DoubleSignTest(int np1, int nm1, int np2, int nm2) {
            double N1 = np1 + nm1;
            double N2 = np2 + nm2;
            double N = N1 + N2;
            double p1 = (double)np1 / N1;
            double p2 = (double)np2 / N2;
            double p = (double)(np1 + np2) / N;
            double x = Math.Sqrt((double)(N1 * N2) / N) * (p2 - p1) / Math.Sqrt(p * (1.0 - p));
            return 1.0 - NormSDist(x);
        }

#if 未実装
        /// <summary>
        /// t分布のパーセント点
        /// </summary>
        /// <remarks>
        /// 山内の近似式。
        /// http://satellite.sourceforge.jp/manuals/reference-ja/function_tinv.html
        /// </remarks>
        /// <param name="p">両側確率</param>
        /// <param name="n">自由度</param>
        /// <returns>パーセント点</returns>
        public static double TInv(double p, int n) {
            if (n <= 0) {
                throw new ArgumentOutOfRangeException("n");
            }

            p = 1.0 - p / 2.0;

            double u = NormSInv(p);
            double u2 = u * u;
            double Y1 = (u2 + 1) / 4;
            double Y2 = ((5 * u2 + 16) * u2 + 3) / 96;
            double Y3 = (((3 * u2 + 19) * u2 + 17) * u2 - 15) / 384;
            double Y4 = ((((79 * u2 + 776) * u2 + 1482) * u2 - 1920) * u2 - 945) / 92160;
            double Y5 = (((((27 * u2 + 339) * u2 + 930) * u2 - 1782) * u2 - 765) * u2 + 17955) / 368640;
            double x = u * (1 + (Y1 + (Y2 + (Y3 + (Y4 + Y5 / n) / n) / n) / n) / n);

            /*
            if (n <= Math.Pow(Math.Log10(p), 2) + 3) {
                do {
                    double w = Math.Atan2(x / Math.Sqrt(n), 1.0);
                    double z = Math.Pow(Math.Cos(w), 2.0);
                    double y = 1.0;
                    for (int i = n - 2; 2 <= i; i -= 2) {
                        y = 1.0 + (i - 1.0) / i * z * y;
                    }
                    double a, b;
                    if (n % 2 == 0) {
                        a = Math.Sin(w) / 2.0;
                        b = 0.5;
                    } else {
                        a = (n == 1) ? 0.0 : Math.Sin(w) * Math.Cos(w) / Math.PI;
                        b = 0.5 + w / Math.PI;
                    }
                    double p1 = Math.Max(0.0, 1 - b - a * y);

                    double n1 = n + 1;
                    double delta = (p1 - p) / Math.Exp((n1 * Math.Log(n1 / (n + x * x))
                            + Math.Log(n / n1 / 2.0 / Math.PI) - 1
                            + (1 / n1 - 1 / n) / 6.0) / 2.0);
                    x += delta;
                    if (Math.Abs(delta) <= Math.Pow(0.1, Math.Abs((int)(Math.Log10(Math.Abs(x)) - 4)))) break;
                } while (x != 0);
            }
            //*/
            return x;
        }
#endif

        /// <summary>
        /// t分布の片側確率を返す
        /// </summary>
        /// <param name="t">tの値</param>
        /// <param name="f">自由度</param>
        /// <returns>片側確率</returns>
        public static double TDist(double t, int f) {
            if (t > 0) {
                return 0.5 * FDist(t * t, 1, f);
            } else {
                return 1 - 0.5 * FDist(t * t, 1, f);
            }
        }

        /// <summary>
        /// F分布のパーセント点。
        /// </summary>
        /// <param name="p">確率</param>
        /// <param name="v1">自由度の分子</param>
        /// <param name="v2">自由度の分母</param>
        public static double FInv(double p, int v1, int v2) {
            if (p < 0 || 1.0 < p) {
                throw new ArgumentOutOfRangeException("p");
            }

            if (30 < v1 && 30 < v2) {
                double u = -NormSInv(p);
                double a = 2.0 / (9 * v1);
                double b = 2.0 / (9 * v2);
                double temp = ((1 - a) * (1 - b) + u * Math.Sqrt(
                    (1 - a) * (1 - a) * b + (1 - b) * (1 - b) * a - a * b * u * u)) /
                    ((1 - b) * (1 - b) - b * u * u);
                return temp * temp * temp;
            } else {
                double xL = 0;
                double x0 = 0.5;
                double xU = 1;
                double alpha1 = p;
                double temp = (1 / x0 - 1) * v1 / v2;
                double alpha2 = FDist(temp, v1, v2);
                for (int i = 0; i < 500; i++) {
                    if (alpha1 > alpha2) {
                        xL = x0;
                    } else {
                        xU = x0;
                    }
                    x0 = (xL + xU) / 2;
                    temp = (1 / x0 - 1) * v1 / v2;
                    alpha2 = FDist(temp, v1, v2);
                }
                return (1 / x0 - 1) * v1 / v2;
            }
        }

        /// <summary>
        /// F分布の上限確率
        /// </summary>
        /// <param name="F">F値</param>
        /// <param name="v1">自由度の分子</param>
        /// <param name="v2">自由度の分母</param>
        /// <returns>上限確率</returns>
        public static double FDist(double F, int v1, int v2) {
            if (F < 0) F = 0;

            double x = v2 / (v2 + v1 * F);
            int a1 = v1 % 2;
            int a2 = v2 % 2;

            double Ix, U;
            if ((a2 == 1) && (a1 == 1)) {
                Ix = 1 - 2 / 3.141592 * Math.Atan(Math.Sqrt(1 / x - 1));
                U = Math.Sqrt(x * (1 - x)) / 3.141592;
            } else if ((a2 == 1) && (a1 == 0)) {
                Ix = Math.Sqrt(x);
                U = Math.Sqrt(x) * (1 - x) / 2;
            } else if ((a2 == 0) && (a1 == 1)) {
                Ix = 1 - Math.Sqrt(1 - x);
                U = x * Math.Sqrt(1 - x) / 2;
            } else { //if ((na == 0) && (ma == 0))
                Ix = x;
                U = x * (1 - x);
            }

            if (a1 == 0) a1 = 2;
            if (a2 == 0) a2 = 2;

            double f2;
            for (f2 = a2; f2 < v2; f2 += 2) {
                double f1 = a1;
                Ix = Ix - 2 * U / f2;
                U = (f1 + f2) * x * U / f2;
            }
            for (double f1 = a1; f1 < v1; f1 += 2) {
                Ix = Ix + 2 * U / f1;
                U = (f1 + f2) * (1 - x) * U / f1;
            }

            return Ix;
        }

        /// <summary>
        /// 標準正規分布の分布関数 P[ Z ≦ x ] の値
        /// </summary>
        /// <example>
        /// normsdist(-1.644853) == 0.05
        /// normsdist(1.644853) == 0.95
        /// </example>
        public static double NormSDist(double x) {
            const double d1 = 0.0498673470;
            const double d2 = 0.0211410061;
            const double d3 = 0.0032776263;
            const double d4 = 0.0000380036;
            const double d5 = 0.0000488906;
            const double d6 = 0.0000053830;

            if (0 < x) {
                double temp = 1 + d1 * x + d2 * Math.Pow(x, 2) + d3 * Math.Pow(x, 3) + d4 * Math.Pow(x, 4) + d5 * Math.Pow(x, 5) + d6 * Math.Pow(x, 6);
                return 1 - Math.Pow(temp, -16) / 2;
            } else {
                x = -x;
                double temp = 1 + d1 * x + d2 * Math.Pow(x, 2) + d3 * Math.Pow(x, 3) + d4 * Math.Pow(x, 4) + d5 * Math.Pow(x, 5) + d6 * Math.Pow(x, 6);
                return Math.Pow(temp, -16) / 2;
            }
        }

        /// <summary>
        /// 標準正規分布のパーセント点
        /// </summary>
        /// <remarks>
        /// Hastingsの近似式を使用。
        /// </remarks>
        /// <example>
        /// normsinv(0.05) == -1.64
        /// normsinv(0.95) == 1.64
        /// </example>
        /// <param name="p">確率</param>
        public static double NormSInv(double p) {
            const double a0 = 2.30753;
            const double a1 = 0.27061;
            const double b1 = 0.99229;
            const double b2 = 0.04481;

            if (p < 0 || 1.0 < p) {
                throw new ArgumentOutOfRangeException("p");
            }
            if (p <= 0.5) {
                double z = Math.Sqrt(Math.Log(1 / p / p));
                return -(z - (a0 + a1 * z) / (1 + b1 * z + b2 * z * z));
            } else {
                p = 1 - p;
                double z = Math.Sqrt(Math.Log(1 / p / p));
                return z - (a0 + a1 * z) / (1 + b1 * z + b2 * z * z);
            }
        }

        /// <summary>
        /// combination。x個からy個を取り出す組合せの数。
        /// </summary>
        public static double Combin(int n, int r) {
            if (n < 0) throw new ArgumentOutOfRangeException("n");
            if (r < 0 || n < r) throw new ArgumentOutOfRangeException("r");

            if (r > n - r) {
                r = n - r;
            }

            double c = 1.0;
            for (int i = 0; i < r; i++) {
                c *= (double)(n - i) / (i + 1);
            }

            return c;
        }

        /// <summary>
        /// χ^2 (カイ二乗値)を求める
        /// </summary>
        public static double Chi2(double[] x, double F) {
            double chi2 = 0.0;
            foreach (var xx in x) {
                chi2 += (xx - F) * (xx - F) / F;
            }
            return chi2;
        }

        /// <summary>
        /// 将棋倶楽部24方式のレーティング計算
        /// </summary>
        /// <remarks>
        /// http://www10.plala.or.jp/greenstone/content1_3.html
        ///      Rn = Ro + K(W-We)
        ///          Rn：新しい点数 
        ///          Ro：はじめの点数 
        ///          K：定数(＝３２） 
        ///          W：得点（勝った場合１、負けた場合０） 
        ///          We：期待勝率 
        ///
        ///　ここで We は以下のようになります。
        ///
        ///      We = 0.5 + 0.00125dr
        ///          dr：レーティングの差（＝Ｒ点差） 
        ///
        /// 注１)インターネット将棋道場では、点数のやりとりを、
        /// 1点から31点の範囲で行っています。つまり勝ったら少なく
        /// とも1点はもらえます。負けても31点を超して取られること
        /// はありません。
        /// </remarks>
        /// <param name="R1">1人目のレーティング</param>
        /// <param name="R2">2人目のレーティング</param>
        /// <param name="sens1">レーティング変動量の割合。1.0で普通。2.0で倍、0.5で半分。</param>
        /// <param name="sens2">レーティング変動量の割合。1.0で普通。2.0で倍、0.5で半分。</param>
        /// <param name="winner">1人目が勝ったら0、負けたら1、引き分けで-1</param>
        public static void Rate24(ref double R1, ref double R2, double sens1, double sens2, int winner) {
            if ((winner & ~1) != 0) throw new ArgumentOutOfRangeException("winner");

            const double K = 32.0;
            const double minWe = 1.0 / K, maxWe = 1.0 - minWe; // 最低1点とする為の値
            double We1 = Math.Min(Math.Max(0.5 + 0.00125 * (R1 - R2), minWe), maxWe);
            double We2 = Math.Min(Math.Max(0.5 + 0.00125 * (R2 - R1), minWe), maxWe);
            double W1 = winner < 0 ? 0.5 : winner ^ 1;
            double W2 = winner < 0 ? 0.5 : winner ^ 0;
            R1 += Math.Min(Math.Max(K * (W1 - We1), -31), 31) * sens1;
            R2 += Math.Min(Math.Max(K * (W2 - We2), -31), 31) * sens2;
        }

        /// <summary>
        /// Bradley-Terry Modelのレーティング計算
        /// </summary>
        /// <remarks>
        /// http://www10.plala.or.jp/greenstone/content1_3.html
        /// We = 1 / (10^{ - (dr/400)} + 1)
        /// </remarks>
        /// <param name="R1">1人目のレーティング</param>
        /// <param name="R2">2人目のレーティング</param>
        /// <param name="sens1">レーティング変動量の割合。1.0で普通。2.0で倍、0.5で半分。</param>
        /// <param name="sens2">レーティング変動量の割合。1.0で普通。2.0で倍、0.5で半分。</param>
        /// <param name="winner">1人目が勝ったら0、負けたら1、引き分けで-1</param>
        public static void RateBradleyTerry(ref double R1, ref double R2, double sens1, double sens2, int winner) {
            if ((winner & ~1) != 0) throw new ArgumentOutOfRangeException("winner");

            const double K = 32.0;
            double We1 = RatingDiffToWinRate(R1 - R2);
            double We2 = RatingDiffToWinRate(R2 - R1);
            double W1 = winner < 0 ? 0.5 : winner ^ 1;
            double W2 = winner < 0 ? 0.5 : winner ^ 0;
            R1 += Math.Min(Math.Max(K * (W1 - We1), -64), 64) * sens1;
            R2 += Math.Min(Math.Max(K * (W2 - We2), -64), 64) * sens2;
            // 変動量は、極端過ぎないように念のため64を上限とする
        }

        /// <summary>
        /// x = 0 ～ y.Count-1 に対する最小二乗法。(変則的だけど速度とLINQとの相性優先で)
        /// </summary>
        /// <param name="y">y</param>
        /// <param name="a">傾き</param>
        /// <param name="b">切片</param>
        public static void LinEst(IEnumerable<double> y, out double a, out double b) {
            double sx = 0, sx2 = 0, sxy = 0, sy = 0;
            int xx = 0;
            foreach (double yy in y) {
                sx += xx;
                sx2 += (double)xx * xx;
                sxy += xx * yy;
                sy += yy;
                xx++;
            }
            int n = xx;
            // a = (nΣ xy - ΣxΣy) / (nΣx^2 - (Σx)^2)
            // b = (Σx^2Σy - ΣxyΣx) / (nΣx^2 - (Σx)^2)
            double r = (n * sx2 - sx * sx);
            a = (n * sxy - sx * sy) / r;
            b = (sx2 * sy - sxy * sx) / r;
        }

        /// <summary>
        /// x, y に対する最小二乗法
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="a">傾き</param>
        /// <param name="b">切片</param>
        public static void LinEst(IEnumerable<double> x, IEnumerable<double> y, out double a, out double b) {
            double sx = 0, sx2 = 0, sxy = 0, sy = 0;
            int n = 0;
            for (IEnumerator<double> xe = x.GetEnumerator(), ye = y.GetEnumerator();
                xe.MoveNext() && ye.MoveNext(); n++) {
                double xx = xe.Current, yy = ye.Current;
                sx += xx;
                sx2 += xx * xx;
                sxy += xx * yy;
                sy += yy;
            }
            // a = (nΣ xy - ΣxΣy) / (nΣx^2 - (Σx)^2)
            // b = (Σx^2Σy - ΣxyΣx) / (nΣx^2 - (Σx)^2)
            double r = (n * sx2 - sx * sx);
            a = (n * sxy - sx * sy) / r;
            b = (sx2 * sy - sxy * sx) / r;
        }
    }
}
