
//#define SETTHREADAFFINITY

#pragma warning disable 162 // 到達出来ないコード

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace ShogiCore.Threading {
    /// <summary>
    /// 探索の並列化のためのクラス…として作ったけど学習の並列化の為のクラス。
    /// Parallel.For()的な感じ。
    /// </summary>
    public class ParallelRunner : IDisposable {
        /// <summary>
        /// logger
        /// </summary>
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 早く終わったスレッドが待つ時にSpinLockするのかMonitor.Waitするのか。
        /// </summary>
        public const bool UseSpinLock = false;

        object syncObject = new object();
        object workRunningCountSync = new object();
        volatile bool threadValid = true;
        volatile int freeThreadCount = 0;
        List<Thread> threads = new List<Thread>();
        List<bool> workList = new List<bool>();

        int workCurrentIndex = 0;
        int workRunningCount = 0;
        volatile bool workValid = true; // funcが1つでもfalseを返したらfalseになる。

        ThreadWork workFunc;
        int workStartIndex;
        int workCount;
        object workArgs;

        /// <summary>
        /// スレッド数
        /// </summary>
        public int ThreadCount { get; /*private*/ set; }
        /// <summary>
        /// スレッド優先度
        /// </summary>
        public ThreadPriorityLevel ThreadPriority { get; set; }

        /// <summary>
        /// 並列実行中ならtrue
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 並列実行可能ならtrue
        /// </summary>
        public bool Runnable {
            get { return !IsRunning && 1 < ThreadCount; }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="threadCount">スレッド数。ゼロで環境の最大値。</param>
        public ParallelRunner(int threadCount = 0) {
            ThreadCount = threadCount == 0 ? ThreadUtility.GetThreadCount() : threadCount;
        }

        /// <summary>
        /// 後始末
        /// </summary>
        ~ParallelRunner() {
            Dispose(false);
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 後始末。
        /// </summary>
        private void Dispose(bool disposing) {
            lock (syncObject) {
                if (threads.Count <= 0) return;

                Debug.Assert(workFunc == null);
                Debug.Assert(workList.TrueForAll(x => !x));
                Debug.Assert(workRunningCount == 0);
                Debug.Assert(freeThreadCount == ThreadCount - 1);

                threadValid = false;
                for (int i = 0; i < workList.Count; i++) workList[i] = false; // 念のため
                Monitor.PulseAll(syncObject);
            }
            Debug.WriteLine("ParallelRunnerスレッド数: " + threads.Count);
            threads.ForEach(x => ThreadUtility.SafeJoin(x, 1000));
            threads.Clear();
            workList.Clear();
            Debug.Assert(freeThreadCount == 0);
            threads = new List<Thread>(); // 念のため？
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Clear() {
            Dispose(false);
            freeThreadCount = 0; // 念のため
            threadValid = true;
        }

        /// <summary>
        /// 並列化する処理
        /// </summary>
        /// <param name="threadID">スレッドID</param>
        /// <param name="index">ループ変数</param>
        /// <param name="args">オプション引数</param>
        /// <returns>中断するならfalse</returns>
        public delegate bool ThreadWork(int threadID, int index, object args);

        /// <summary>
        /// 並列実行
        /// </summary>
        public void For(ThreadWork func, int startIndex, int count, object args) {
            bool runnable;
            lock (syncObject) {
                runnable = Runnable && ThreadCount <= count;
                if (runnable) IsRunning = true;
            }
            if (!runnable) {
                for (int i = 0; i < count; i++) {
                    if (!func(0, startIndex + i, args)) break;
                }
            } else {
                ReadyThreads();
                // 全スレッド止まるまで待つ (念のため)
                while (freeThreadCount < ThreadCount - 1) ;

                Debug.Assert(workRunningCount == 0);
                Debug.Assert(freeThreadCount == ThreadCount - 1);

                workRunningCount = 0;
                workCurrentIndex = 0;
                workValid = true; // funcが1つでもfalseを返したらfalseになる。

                Debug.Assert(workFunc == null);
                workFunc = func;
                workStartIndex = startIndex;
                workCount = count;
                workArgs = args;

                // スレッドで実行
                lock (syncObject) {
                    Debug.Assert(workList.Count == ThreadCount - 1);
                    for (int threadID = 0; threadID < ThreadCount - 1; threadID++) {
                        Debug.Assert(!workList[threadID]);
                        workList[threadID] = true;
                    }
                    Monitor.PulseAll(syncObject);
                }
                // 現在のスレッドでも実行
                WorkCallback(ThreadCount - 1);

                // 終わるまで待つ
                if (UseSpinLock) {
                    while (0 < workRunningCount) {
                        if (Interlocked.CompareExchange(ref workRunningCount, 0, 0) == 0) break;
                        Thread.SpinWait(1);
                    }
                } else {
                    while (0 < workRunningCount) {
                        lock (workRunningCountSync) {
                            if (Interlocked.CompareExchange(ref workRunningCount, 0, 0) == 0) break;
                            Monitor.Wait(workRunningCountSync);
                        }
                    }
                }
                // 全スレッド止まるまで待つ (念のため)
                while (freeThreadCount < ThreadCount - 1) ;
                while (!workList.TrueForAll(x => !x)) { // ←全部falseになるまで待つ。
                    Thread.SpinWait(1);
                }

                // 念のため
                Debug.Assert(workRunningCount == 0);
                //Debug.Assert(freeThreadCount == ThreadCount - 1); //←なんでこれ引っかかるんだろ。。？
                Debug.Assert(workList.TrueForAll(x => !x));

                lock (syncObject) {
                    workFunc = null;
                    IsRunning = false;
                }
            }
        }

        /// <summary>
        /// スレッドの準備
        /// </summary>
        private void ReadyThreads() {
            Debug.Assert(threadValid);
            if (threads.Count == ThreadCount - 1) return;
            Debug.Assert(threads.TrueForAll(x => !x.IsAlive), "スレッド生きてる？");
            // 多ければ削除
            while (ThreadCount - 1 < threads.Count) {
                threads.RemoveAt(threads.Count - 1);
                workList.RemoveAt(workList.Count - 1);
            }
            // 少なければ追加
            while (threads.Count < ThreadCount - 1) {
                workList.Add(false);
                var thread = new Thread(ThreadProc);
                thread.Name = "ParallelRunner:" + threads.Count.ToString();
                thread.IsBackground = true; // 念のためプロセス終了時にはAbortされるようにしてみる
                thread.Start(threads.Count);
                threads.Add(thread);
            }
        }

        /// <summary>
        /// お仕事の実行
        /// </summary>
        private void WorkCallback(int threadID) {
            Interlocked.Increment(ref workRunningCount);

            Debug.Assert(IsRunning);

            int execCount = 0;
            for (; workValid && threadValid; execCount++) {
                int index = Interlocked.Increment(ref workCurrentIndex) - 1;
                // ↑Increment()は++後の値を返すので、後置++風にするために-1。
                if (workCount <= index) break; // おしまい
                if (!workFunc(threadID, workStartIndex + index, workArgs)) {
                    workValid = false;
                    break;
                }
            }

            //Debug.WriteLineIf(1 < ThreadCount, "スレッド" + Thread.CurrentThread.ManagedThreadId + ": 処理数 = " + execCount);

            Debug.Assert(IsRunning);
            Debug.Assert(workFunc != null);

            lock (workRunningCountSync) {
                Interlocked.Decrement(ref workRunningCount);
                Monitor.Pulse(workRunningCountSync);
            }
        }

        /// <summary>
        /// スレッドの処理
        /// </summary>
        private void ThreadProc(object arg) {
            int threadID = (int)arg;

            // thread affinity
#if SETTHREADAFFINITY
            int affinity = ThreadUtility.SetThreadAffinity(threadID);
#else
            const int affinity = -1;
#endif

            // お仕事来るまで待ってる。
            while (threadValid) {
                lock (syncObject) {
                    if (!threadValid) break;
                    if (threadID < workList.Count && workList[threadID]) {
                        workList[threadID] = false;
                    } else {
                        freeThreadCount++;
                        Monitor.Wait(syncObject);
                        freeThreadCount--;
                        continue;
                    }
                }
                try {
                    // お仕事実行
                    WorkCallback(threadID);
                } catch (ThreadAbortException) {
                    // 無視
                } catch (Exception e) {
                    logger.Warn("並列処理中に例外発生", e);
                }
            }

            // thread affinity
            if (0 <= affinity) ThreadUtility.ResetThreadAffinity();
        }
    }
}
