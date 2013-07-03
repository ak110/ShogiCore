using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Diagnostics {
    /// <summary>
    /// 軽量版Stopwatch
    /// </summary>
    public class LiteStopwatch {
        /// <summary>
        /// 開始したインスタンスを返す
        /// </summary>
        public static LiteStopwatch StartNew() {
            LiteStopwatch sw = new LiteStopwatch();
            sw.Start();
            return sw;
        }

        long elapsed;
        int startTime;

        /// <summary>
        /// 計測中ならtrue
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 経過時間 (ms)
        /// </summary>
        public long ElapsedMilliseconds {
            get {
                long result = elapsed;
                if (IsRunning) {
                    result += unchecked(Environment.TickCount - startTime);
                }
                return result;
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public LiteStopwatch() { Reset(); }

        /// <summary>
        /// リセット(停止して0秒化)
        /// </summary>
        public void Reset() {
            elapsed = 0;
            startTime = 0;
            IsRunning = false;
        }

        /// <summary>
        /// リセットして開始
        /// </summary>
        public void Restart() {
            elapsed = 0;
            startTime = Environment.TickCount;
            IsRunning = true;
        }

        /// <summary>
        /// 開始
        /// </summary>
        public void Start() {
            if (!IsRunning) {
                startTime = Environment.TickCount;
                IsRunning = true;
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop() {
            if (IsRunning) {
                elapsed += unchecked(Environment.TickCount - startTime);
                IsRunning = false;
                if (elapsed < 0) {
                    elapsed = 0;
                }
            }
        }
    }
}
