using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// こまごました関数とか。
    /// </summary>
    public static class Utility {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 配列の要素が全て一致したらtrue
        /// </summary>
        public static bool IsMatchAll<T>(T[] a, T[] b) where T : struct {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }
        /// <summary>
        /// 配列の要素が全て一致したらtrue
        /// </summary>
        public static bool IsMatchAll<T>(T[][] a, T[][] b) where T : struct {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                if (!IsMatchAll(a[i], b[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 配列の要素が全て一致したらtrue
        /// </summary>
        public static bool IsMatchAll(Array a, Array b) {
            if (a.Rank != b.Rank) return false;
            for (int i = 0; i < a.Rank; i++) {
                if (a.GetLength(i) != b.GetLength(i)) return false;
            }
            System.Collections.IEnumerator aa = a.GetEnumerator();
            System.Collections.IEnumerator bb = b.GetEnumerator();
            while (true) {
                if (!aa.MoveNext()) {
                    return !bb.MoveNext();
                } else if (!bb.MoveNext()) {
                    return false;
                }
                if (!aa.Current.Equals(bb.Current)) return false;
            }
        }

        /// <summary>
        /// n番目のアイテムを先頭に持ってくる。
        /// var item = list[n]; list.RemoveAt(n); list.Insert(0, item); と等価。
        /// </summary>
        public static void MoveToFirst<T>(List<T> list, int n) {
            Debug.Assert(0 <= n);
            if (n <= 0) return;

            var item = list[n];
            for (int i = n - 1; 0 <= i; i--) {
                list[i + 1] = list[i];
            }
            list[0] = item;
        }

        /// <summary>
        /// n番目のアイテムを先頭に持ってくる。
        /// var item = list[n]; list.RemoveAt(n); list.Insert(0, item); と等価。
        /// </summary>
        public static void MoveToFirst<T>(T[] list, int n) {
            Debug.Assert(0 <= n);
            if (n <= 0) return;

            var item = list[n];
            for (int i = n - 1; 0 <= i; i--) {
                list[i + 1] = list[i];
            }
            list[0] = item;
        }

        #region 配列の処理とか

        /// <summary>
        /// 平均偏差(値の絶対値の平均)を返す。
        /// </summary>
        public static float GetDeviation(short[] array) {
            return array.Average(x => (float)Math.Abs((int)x));
        }
        /// <summary>
        /// 平均偏差(値の絶対値の平均)を返す。
        /// </summary>
        public static float GetDeviation(short[][] array) {
            int count = 0;
            double sum = 0;
            foreach (var x in array) {
                if (x == null) continue;
                foreach (var y in x) {
                    sum += Math.Abs((int)y);
                    count++;
                }
            }
            return (float)(sum / count);
        }
        /// <summary>
        /// 平均偏差(値の絶対値の平均)を返す。
        /// </summary>
        public static float GetDeviation(short[][][] array) {
            int count = 0;
            double sum = 0;
            foreach (var x in array) {
                if (x == null) continue;
                foreach (var y in x) {
                    if (y == null) continue;
                    foreach (var z in y) {
                        sum += Math.Abs((int)z);
                        count++;
                    }
                }
            }
            return (float)(sum / count);
        }

        /// <summary>
        /// 平均偏差(値の絶対値の平均)を返す。
        /// </summary>
        public static float GetDeviation(int[] array) {
            return array.Average(x => (float)Math.Abs((int)x));
        }
        /// <summary>
        /// 平均偏差(値の絶対値の平均)を返す。
        /// </summary>
        public static float GetDeviation(int[][] array) {
            int count = 0;
            double sum = 0;
            foreach (var x in array) {
                if (x == null) continue;
                foreach (var y in x) {
                    sum += Math.Abs((int)y);
                    count++;
                }
            }
            return (float)(sum / count);
        }
        /// <summary>
        /// 平均偏差(値の絶対値の平均)を返す。
        /// </summary>
        public static float GetDeviation(int[][][] array) {
            int count = 0;
            double sum = 0;
            foreach (var x in array) {
                if (x == null) continue;
                foreach (var y in x) {
                    if (y == null) continue;
                    foreach (var z in y) {
                        sum += Math.Abs((int)z);
                        count++;
                    }
                }
            }
            return (float)(sum / count);
        }

        /// <summary>
        /// 最大値と最小値の差を返す。
        /// </summary>
        public static int GetRangeWidth(short[] array) {
            return array.Max() - array.Min(); // 速度無視気味の手抜き実装
        }
        /// <summary>
        /// 最大値と最小値の差を返す。
        /// </summary>
        public static int GetRangeWidth(short[][] array) {
            return array.Max(x => x == null ? short.MinValue : x.Max())
                - array.Min(x => x == null ? short.MaxValue : x.Min());
        }
        /// <summary>
        /// 最大値と最小値の差を返す。
        /// </summary>
        public static int GetRangeWidth(short[][][] array) {
            return array.Max(x => x == null ? short.MinValue : x.Max(y => y == null ? short.MinValue : y.Max()))
                - array.Min(x => x == null ? short.MaxValue : x.Min(y => y == null ? short.MaxValue : y.Min()));
        }

        /// <summary>
        /// 最大値と最小値の差を返す。
        /// </summary>
        public static int GetRangeWidth(int[] array) {
            return array.Max() - array.Min(); // 速度無視気味の手抜き実装
        }
        /// <summary>
        /// 最大値と最小値の差を返す。
        /// </summary>
        public static int GetRangeWidth(int[][] array) {
            return array.Max(x => x == null ? int.MinValue : x.Max())
                - array.Min(x => x == null ? int.MaxValue : x.Min());
        }
        /// <summary>
        /// 最大値と最小値の差を返す。
        /// </summary>
        public static int GetRangeWidth(int[][][] array) {
            return array.Max(x => x == null ? int.MinValue : x.Max(y => y == null ? int.MinValue : y.Max()))
                - array.Min(x => x == null ? int.MaxValue : x.Min(y => y == null ? int.MaxValue : y.Min()));
        }

        /// <summary>
        /// 差の配列を返す。
        /// </summary>
        public static int[] GetDiff(int[] a, int[] b) {
            if (a.Length != b.Length) throw new ArgumentException();
            var result = new int[a.Length];
            for (int i = 0; i < a.Length; i++) {
                result[i] = a[i] - b[i];
            }
            return result;
        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone()。
        /// </summary>
        public static T Clone<T>(T t) where T : ICloneable {
            return (T)t.Clone();
        }

#if false
        /// <summary>
        /// Clone()。
        /// </summary>
        public static T[] Clone<T>(T[] t) where T : struct {
            if (t == null) return null;
            return (T[])t.Clone();
        }
#else
        /// <summary>
        /// Clone()。
        /// </summary>
        public static T[] Clone<T>(T[] t) where T : struct {
            if (t == null) return null;
            T[] copy = new T[t.Length];
            for (int i = 0; i < t.Length; i++) {
                copy[i] = t[i];
            }
            return copy;
        }
#endif

        /// <summary>
        /// Clone()。
        /// </summary>
        public static T[] CloneRef<T>(T[] t) where T : ICloneable {
            if (t == null) return null;
            T[] copy = new T[t.Length];
            for (int i = 0; i < t.Length; i++) {
                copy[i] = Clone(t[i]);
            }
            return copy;
        }

        /// <summary>
        /// Clone()。
        /// </summary>
        public static T[][] Clone<T>(T[][] t) where T : struct {
            if (t == null) return null;
            var copy = new T[t.Length][];
            for (int i = 0; i < t.Length; i++) {
                copy[i] = Clone(t[i]);
            }
            return copy;
        }

        /// <summary>
        /// Clone()。
        /// </summary>
        public static T[][][] Clone<T>(T[][][] t) where T : struct {
            if (t == null) return null;
            var copy = new T[t.Length][][];
            for (int i = 0; i < t.Length; i++) {
                copy[i] = Clone(t[i]);
            }
            return copy;
        }

        /// <summary>
        /// Clone()。
        /// </summary>
        public static List<T> Clone<T>(List<T> t) where T : struct {
            if (t == null) return null;
            return new List<T>(t);
        }
        /// <summary>
        /// Clone()。
        /// </summary>
        public static List<List<T>> Clone<T>(List<List<T>> t) where T : struct {
            if (t == null) return null;
            return t.ConvertAll(Clone);
        }
        /// <summary>
        /// Clone()。
        /// </summary>
        public static List<T[]> Clone<T>(List<T[]> t) where T : struct {
            if (t == null) return null;
            return t.ConvertAll(Clone);
        }

        /// <summary>
        /// Clone()。
        /// </summary>
        public static Stack<T> Clone<T>(Stack<T> t) where T : struct {
            if (t == null) return null;
            return new Stack<T>(t);
        }

        /// <summary>
        /// Clone()。
        /// </summary>
        public static Stack<T[]> Clone<T>(Stack<T[]> t) where T : struct {
            if (t == null) return null;
            var list = new List<T[]>(t.Count);
            foreach (var item in t) list.Add(Clone(item));
            return new Stack<T[]>(list);
        }

        /// <summary>
        /// Clone()。
        /// </summary>
        public static Stack<T[][]> Clone<T>(Stack<T[][]> t) where T : struct {
            if (t == null) return null;
            var list = new List<T[][]>(t.Count);
            foreach (var item in t) list.Add(Clone(item));
            return new Stack<T[][]>(list);
        }

        #endregion

        #region CreateEmptyClone

        /// <summary>
        /// 同じ構造で要素が空の配列を作成
        /// </summary>
        public static T[] CreateEmptyClone<T>(T[] array) {
            return new T[array.Length];
        }

        /// <summary>
        /// 同じ構造で要素が空の配列を作成
        /// </summary>
        public static T[][] CreateEmptyClone<T>(T[][] array) {
            T[][] result = new T[array.Length][];
            for (int i = 0; i < result.Length; i++) {
                if (array[i] == null) continue;
                result[i] = new T[array[i].Length];
            }
            return result;
        }

        /// <summary>
        /// 同じ構造で要素が空の配列を作成
        /// </summary>
        public static T[][][] CreateEmptyClone<T>(T[][][] array) {
            T[][][] result = new T[array.Length][][];
            for (int i = 0; i < result.Length; i++) {
                if (array[i] == null) continue;
                result[i] = new T[array[i].Length][];
                for (int j = 0; j < result[i].Length; j++) {
                    if (array[i][j] == null) continue;
                    result[i][j] = new T[array[i][j].Length];
                }
            }
            return result;
        }

        #endregion

        #region ClearArray

        public static void ClearArray<T>(T[][][] array) where T : struct { // 念のため値型限定
            foreach (var a in array) {
                if (a == null) continue;
                ClearArray(a);
            }
        }

        public static void ClearArray<T>(T[][] array) where T : struct { // 念のため値型限定
            foreach (var a in array) {
                if (a == null) continue;
                ClearArray(a);
            }
        }

        public static void ClearArray<T>(T[] array) where T : struct { // 念のため値型限定
            Array.Clear(array, 0, array.Length);
        }

        #endregion
            
        /// <summary>
        /// swap。
        /// </summary>
        public static void Swap<T>(ref T x, ref T y) {
            var t = x;
            x = y;
            y = t;
        }

        /// <summary>
        /// Array.Clear()
        /// </summary>
        public static void Clear(int[] p, int startIndex, int count) {
            Array.Clear(p, startIndex, count);
        }
        /// <summary>
        /// Array.Clear()
        /// </summary>
        public static unsafe void Clear(int* p, int startIndex, int count) {
            ZeroMemory(p + startIndex, sizeof(int) * count);
        }
        /// <summary>
        /// Array.Clear()
        /// </summary>
        public static void Clear(byte[] p, int startIndex, int count) {
            Array.Clear(p, startIndex, count);
        }
        /// <summary>
        /// Array.Clear()
        /// </summary>
        public static unsafe void Clear(byte* p, int startIndex, int count) {
            ZeroMemory(p + startIndex, sizeof(byte) * count);
        }
        /// <summary>
        /// Array.Clear()
        /// </summary>
        public static void Clear(bool[] p, int startIndex, int count) {
            Array.Clear(p, startIndex, count);
        }
        /// <summary>
        /// Array.Clear()
        /// </summary>
        public static unsafe void Clear(bool* p, int startIndex, int count) {
            ZeroMemory(p + startIndex, sizeof(bool) * count);
        }

        /// <summary>
        /// Array.IndexOf
        /// </summary>
        public static int IndexOf<T>(T[] array, T value, int startIndex, int count) {
            return Array.IndexOf(array, value, startIndex, count);
        }
        /// <summary>
        /// Array.IndexOf
        /// </summary>
        public static unsafe int IndexOf(sbyte* array, sbyte value, int startIndex, int count) {
            for (int i = 0; i < count; i++) {
                if (array[startIndex + i] == value) return startIndex + i;
            }
            return -1;
        }
        /// <summary>
        /// Array.IndexOf
        /// </summary>
        public static unsafe int IndexOf(int* array, int value, int startIndex, int count) {
            for (int i = 0; i < count; i++) {
                if (array[startIndex + i] == value) return startIndex + i;
            }
            return -1;
        }

        /// <summary>
        /// Array.Copy
        /// </summary>
        public static unsafe void Copy(bool* sourceArray, bool* destinationArray, int length) {
            CopyMemory(destinationArray, sourceArray, sizeof(bool) * length);
        }
        /// <summary>
        /// Array.Copy
        /// </summary>
        public static unsafe void Copy(bool[] sourceArray, bool[] destinationArray, int length) {
            Array.Copy(sourceArray, destinationArray, length);
        }

        /// <summary>
        /// ZeroMemory()
        /// </summary>
        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void ZeroMemory(void* outDest, int inNumOfBytes);
        /// <summary>
        /// ZeroMemory()
        /// </summary>
        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void ZeroMemory(float[] outDest, int inNumOfBytes);

        /// <summary>
        /// CopyMemory()
        /// </summary>
        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, void* inSrc, int inNumOfBytes);

        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, byte[] inSrc, int inNumOfBytes);

        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, sbyte[] inSrc, int inNumOfBytes);

        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, short[] inSrc, int inNumOfBytes);

        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, int[] inSrc, int inNumOfBytes);

        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, float[] inSrc, int inNumOfBytes);
        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(float[] outDest, void* inSrc, int inNumOfBytes);

        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(void* outDest, Piece[] inSrc, int inNumOfBytes);
        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void CopyMemory(Piece[] outDest, void* inSrc, int inNumOfBytes);

        /// <summary>
        /// MoveMemory()
        /// </summary>
        [DllImport("kernel32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern unsafe void MoveMemory(void* outDest, void* inSrc, int inNumOfBytes);

        /// <summary>
        /// 王が1度でもrank段目以上に進入するならtrue
        /// </summary>
        public static bool IsNyuugyokuKifu(Notation.Notation notation, int rank) {
            Board board = new Board(notation.InitialBoard);
            foreach (Move move in board.ReadNotation(notation.Moves)) {
                if ((board[move.From] & ~Piece.ENEMY) == Piece.OU &&
                    Board.GetRank(move.To, board.Turn) <= rank) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 起動時の引数でProcessorAffinityを設定するための適当ユーティリティ。
        /// </summary>
        public static long SetProcessorAffinity(string[] args) {
            long processorAffinity = 0;
            if (args.Contains("CPU0")) processorAffinity |= 1;
            if (args.Contains("CPU1")) processorAffinity |= 1 << 1;
            if (args.Contains("CPU2")) processorAffinity |= 1 << 2;
            if (args.Contains("CPU3")) processorAffinity |= 1 << 3;
            if (args.Contains("CPU4")) processorAffinity |= 1 << 4;
            if (args.Contains("CPU5")) processorAffinity |= 1 << 5;
            if (args.Contains("CPU6")) processorAffinity |= 1 << 6;
            if (args.Contains("CPU7")) processorAffinity |= 1 << 7; // ここまで。(手抜き)
            if (processorAffinity != 0) {
                Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)processorAffinity;
            }
            return processorAffinity;
        }

        /// <summary>
        /// Process.Start()の例外出さないバージョン
        /// </summary>
        /// <param name="path">パス</param>
        public static void ProcessStartSafe(string path) {
            try {
                Process.Start(path);
            } catch (Exception e) {
                logger.Warn("Process.Start()失敗: " + path, e);
            }
        }

        /// <summary>
        /// プロセッサ数が1以上の場合に限ってプライオリティを変更
        /// </summary>
        public static void SetPriorityIfMP(ProcessPriorityClass pc) {
            if (Environment.ProcessorCount <= 1) return;
            try {
                Process.GetCurrentProcess().PriorityClass = pc;
            } catch (System.ComponentModel.Win32Exception e) { // mono対策
                logger.Warn("プロセス優先度変更に失敗", e);
            }
        }
        /// <summary>
        /// !DEBUGな時のみSetPriorityIfMP()。
        /// </summary>
        public static void SetPriorityIfMPRelease(ProcessPriorityClass pc) {
#if !DEBUG
            SetPriorityIfMP(pc);
#endif
        }
    }
}
