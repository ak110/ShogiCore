using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace ShogiCore.Threading {
    /// <summary>
    /// スレッド関係の処理とか
    /// </summary>
    public static class ThreadUtility {
        /// <summary>
        /// スレッド数として丁度良さそうな値を取得
        /// </summary>
        public static int GetThreadCount() {
            var n = Process.GetCurrentProcess().ProcessorAffinity.ToInt64();
            return n == 0 ? Environment.ProcessorCount : MathUtility.PopCnt64(unchecked((ulong)n));
        }

        /// <summary>
        /// Join()して時間切れたらAbort()。
        /// </summary>
        /// <param name="timeout">待つ時間[ms]</param>
        /// <returns>Join()成功時true、Abort()時false</returns>
        public static bool SafeJoin(Thread thread, int timeout) {
            if (thread.Join(timeout)) return true;
            thread.Abort(); // 時間切れたらデッドロック防止に強制停止。
            thread.Join(500);
            return false;
        }

        /// <summary>
        /// Join()して時間切れたらAbort()。
        /// Join()中は100ms毎にApplication.DoEvents()。
        /// </summary>
        /// <param name="timeout">待つ時間[ms]</param>
        /// <returns>Join()成功時true、Abort()時false</returns>
        public static bool SafeJoinForForms(Thread thread, int timeout) {
            for (int startTick = Environment.TickCount; unchecked(Environment.TickCount - startTick) < timeout; ) {
                if (thread.Join(100)) return true;
                System.Windows.Forms.Application.DoEvents();
            }
            thread.Abort(); // 時間切れたらデッドロック防止に強制停止。
            thread.Join(500);
            return false;
        }

        /// <summary>
        /// int変数のlockBitMaskのビットを用いたロック。
        /// </summary>
        public static void SpinLock(ref int lockVar, int lockBitMask) {
            int t = lockVar & ~lockBitMask;
#if false
            while (true) {
                if (Interlocked.Exchange(ref lockVar, t | lockBitMask) == t) break;
                /*
                while ((lockVar & lockBitMask) != 0) ; // C#ではコレにはvolatile付けれないのでデッドロックしてしまう
                /*/
                while (Interlocked.CompareExchange(ref lockVar, t, t) != t)  {
                    Thread.SpinWait(1);
                }
                //*/
            }
#else
            while (true) {
                // CompareExchange()のループ回すよりはExchangeだけの方が軽いっぽい？
                int ex = Interlocked.Exchange(ref lockVar, t | lockBitMask);
                if ((ex & lockBitMask) == 0) {
                    lockVar = ex | lockBitMask;
                    break;
                }
                Thread.SpinWait(1);
            }
#endif
            Debug.Assert((lockVar & lockBitMask) == lockBitMask, "SpinLock()でロックされてない(バグ？)");
        }
        /// <summary>
        /// SpinLock()に対応したunlock
        /// </summary>
        public static void SpinUnlock(ref int lockVar, int lockBitMask) {
            Debug.Assert((lockVar & lockBitMask) == lockBitMask, "ロックされてないのにSpinUnlock()");
            //*
            lockVar &= ~lockBitMask; // 単にコレでいいらしい
            /*/
            int t = lockVar & ~lockBitMask;
            int ex = Interlocked.Exchange(ref lockVar, t);
            Debug.Assert(ex == (t | lockBitMask), "SpinUnlock()失敗？");
            //*/
        }

        /// <summary>
        /// SetThreadAffinityMask()ってみる。
        /// </summary>
        /// <returns>何ビット目だったのか - 1を取得(0以上)。失敗時は-1</returns>
        public static int SetThreadAffinity(int threadID) {
            // ProcessorAffinityのthreadID番目のビットが立ってたら設定
            var n = Process.GetCurrentProcess().ProcessorAffinity.ToInt64();
            if (n == 0) return -1;

            for (int i = 0, nbits = 0; n != 0; n >>= 1, nbits++) {
                if ((n & 1) != 0) {
                    if (threadID <= i) {
                        Thread.BeginThreadAffinity();
                        SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1L << i));
                        return nbits;
                    }
                    i++;
                }
            }
            // 設定無し。
            return -1;
        }

        /// <summary>
        /// SetThreadAffinity()の解除。
        /// </summary>
        public static void ResetThreadAffinity() {
            Thread.EndThreadAffinity();
        }

        #region WinAPI

        [DllImport("kernel32.dll")]
        extern static IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        extern static int SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

        #endregion
    }
}
