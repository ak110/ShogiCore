using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShogiCore.Threading {
    /// <summary>
    /// 多重起動用排他処理
    /// </summary>
    public class IOLock : IDisposable {
        static object syncObject = new object();
        //static Semaphore semaphore = new Semaphore(0, 1, "{A89FAD24-AAD9-4065-A3E0-285D183700FC}");
        //static Mutex mutex = new Mutex(false, "{A89FAD24-AAD9-4065-A3E0-285D183700FC}");

        /// <summary>
        /// lock
        /// </summary>
        public IOLock() {
            Monitor.Enter(syncObject);
            //semaphore.WaitOne();
            //mutex.WaitOne();
        }

        /// <summary>
        /// unlock
        /// </summary>
        public void Dispose() {
            //mutex.ReleaseMutex();
            //semaphore.Release();
            Monitor.Exit(syncObject);
        }
    }
    // どうも将棋所経由の時だけ↑デッドロックるような…？
    // Windowsのパイプ関係のバグだろか。
}
