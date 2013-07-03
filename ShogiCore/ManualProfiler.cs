using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// 手動プロファイラ
    /// </summary>
    /// <remarks>
    /// 測定する箇所に下記のようなコードを挿入して利用。
    /// <code>
    /// if (ManualProfiler.Enable) ManualProfiler.Enter(ManualProfiler.Process.XXX);
    /// 
    /// 処理
    /// 
    /// if (ManualProfiler.Enable) ManualProfiler.Leave(ManualProfiler.Process.XXX);
    /// </code>
    /// </remarks>
    public static class ManualProfiler {
        /// <summary>
        /// 処理種別
        /// </summary>
        /// <remarks>
        /// 説明は属性で手抜き
        /// </remarks>
        public enum Process {
            [System.ComponentModel.Description("探索全体")]
            Search,

            [System.ComponentModel.Description("静止探索全体")]
            Quies,

            [System.ComponentModel.Description("指し手生成（通常）")]
            GetMovesN,

            [System.ComponentModel.Description("指し手生成（静止）")]
            GetMovesQ,

            [System.ComponentModel.Description("オーダリング準備")]
            PreOrdering,

            [System.ComponentModel.Description("オーダリング（通常）")]
            OrderingN,

            [System.ComponentModel.Description("オーダリング（静止）")]
            OrderingQ,

            [System.ComponentModel.Description("オーダリング（静避）")]
            OrderingQA,

            [System.ComponentModel.Description("ソート処理（通常）")]
            OrderingSort,

            [System.ComponentModel.Description("ソート処理（王手時）")]
            OrderingSortC,

            [System.ComponentModel.Description("指し手適用（通常）")]
            DoN,

            [System.ComponentModel.Description("指し手適用（静止）")]
            DoQ,

            [System.ComponentModel.Description("評価関数")]
            Evaluate,

            [System.ComponentModel.Description("詰み探索")]
            Mate,

            [System.ComponentModel.Description("探索前チェック")]
            PreSearchCheck,

            [System.ComponentModel.Description("探索後チェック")]
            PostSearchCheck,

            [System.ComponentModel.Description("全数")]
            _Count,
        }

        /// <summary>
        /// 測定を行うならtrue
        /// </summary>
#if DANGEROUS
        public const bool Enable = false;
#else
        public static bool Enable = false;
#endif

#if DANGEROUS
        /// <summary>
        /// 処理開始
        /// </summary>
        /// <param name="process">処理種別</param>
        public static void Enter(Process process) {
        }

        /// <summary>
        /// 処理開始
        /// </summary>
        /// <param name="process">処理種別</param>
        public static void Leave(Process process) {
        }
#else
        /// <summary>
        /// enumとDescriptionのDictionary
        /// </summary>
        static readonly Dictionary<Process, string> descList =
            (from m in typeof(Process).GetMembers()
             let attr = m.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                         .Cast<System.ComponentModel.DescriptionAttribute>()
                         .FirstOrDefault()
             where attr != null
             select new KeyValuePair<Process, string>(
                 (Process)Enum.Parse(typeof(Process), m.Name), attr.Description))
                .ToDictionary(x => x.Key, x => x.Value);

        /// <summary>
        /// 呼び出され回数
        /// </summary>
        static long[] callCounts = new long[(int)Process._Count];
        /// <summary>
        /// 総処理時間
        /// </summary>
        static long[] totalTimes = new long[(int)Process._Count];

        /// <summary>
        /// 処理開始
        /// </summary>
        /// <param name="process">処理種別</param>
        public static void Enter(Process process) {
            callCounts[(int)process]++;
            totalTimes[(int)process] -= Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// 処理完了
        /// </summary>
        /// <param name="process">処理種別</param>
        public static void Leave(Process process) {
            totalTimes[(int)process] += Stopwatch.GetTimestamp(); // + end - start == elapsed
        }
#endif

        /// <summary>
        /// ゼロクリア
        /// </summary>
        public static void Clear() {
#if !DANGEROUS
            Array.Clear(callCounts, 0, callCounts.Length);
            Array.Clear(totalTimes, 0, totalTimes.Length);
#endif
        }

        /// <summary>
        /// 処理結果の取得
        /// </summary>
        public static string GetDisplayString() {
            StringBuilder str = new StringBuilder();
#if !DANGEROUS
            const int Pad1 = 13;
            const int Pad2 = 11;
            const int Pad3 = 11;
            const int Pad4 = 11;
            const int Pad5 = 7;
            long freq = Stopwatch.Frequency;
            str.Append("　項目 ".PadRight(Pad1, '　'));
            str.Append("時間".PadLeft(Pad2 - 2));
            str.Append("回数".PadLeft(Pad3 - 2));
            str.Append("μ秒/回".PadLeft(Pad4 - 4));
            str.Append("時間分布".PadLeft(Pad5 - 4));
            str.AppendLine();
            double[] percents = new double[(int)Process._Count];
            for (Process p = 0; p < Process._Count; p++) {
                percents[(int)p] = totalTimes[(int)p] * 100.0 / totalTimes[(int)Process.Search];
                long ms = totalTimes[(int)p] * 1000 / freq;
                long us = totalTimes[(int)p] * 1000000 / freq;
                str.Append((descList[p] + ':').PadRight(Pad1, '　'));
                str.Append(ms.ToString("#,##0").PadLeft(Pad2));
                str.Append(callCounts[(int)p].ToString("#,##0").PadLeft(Pad3));
                str.Append((callCounts[(int)p] == 0 ? 0 : us / callCounts[(int)p]).ToString().PadLeft(Pad4));
                str.Append(percents[(int)p].ToString("0.0").PadLeft(Pad5));
                str.AppendLine();
            }
            double percentSum = percents.Skip(2).Sum(); // 「全体」2つを省いて合計
            str.AppendLine("未測定:".PadRight(Pad1, '　') +
                "".PadLeft(Pad2 + Pad3) +
                (100.0 - percentSum).ToString("0.0").PadLeft(Pad4));
            str.Replace("　", "  "); // 一応置換
#endif
            return str.ToString();
        }
    }
}
