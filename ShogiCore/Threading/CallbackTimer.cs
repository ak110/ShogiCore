using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShogiCore.Threading {
    /// <summary>
    /// 定期的にコールバックされるタイマー
    /// </summary>
    public class CallbackTimer : IDisposable {
        volatile bool threadValid = true;
        Thread thread;

        volatile WaitCallback callback = null;
        object state;
        int interval;

        /// <summary>
        /// 初期化
        /// </summary>
        public CallbackTimer() {
            thread = new Thread(ThreadProc);
            thread.Name = "CallbackTimer";
            thread.IsBackground = true;
            thread.Start();
        }
        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            Stop();
            lock (thread) {
                threadValid = false;
                Monitor.Pulse(thread);
            }
            ThreadUtility.SafeJoin(thread, 1000);
        }

        /// <summary>
        /// 実行中？
        /// </summary>
        public bool IsRunning {
            get { return callback != null; }
        }

        /// <summary>
        /// 開始
        /// </summary>
        public void Start(WaitCallback callback, object state, int interval) {
            Stop();
            lock (thread) {
                this.callback = callback;
                this.state = state;
                this.interval = interval;

                Monitor.Pulse(thread);
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop() {
            lock (thread) {
                if (callback == null) return;

                callback = null;
                Monitor.Pulse(thread);
            }
        }

        /// <summary>
        /// スレッド
        /// </summary>
        private void ThreadProc() {
            while (threadValid) {
                WaitCallback work;
                lock (thread) {
                    if (callback == null) {
                        Monitor.Wait(thread);
                        continue;
                    } else {
                        Monitor.Wait(thread, interval);
                        if (!threadValid) break;
                        if (callback == null) continue;

                        work = callback;
                    }
                }

                work(state);
            }
        }
    }
}
