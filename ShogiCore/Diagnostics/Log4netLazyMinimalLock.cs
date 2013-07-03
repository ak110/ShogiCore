using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace ShogiCore.Diagnostics {
    /// <summary>
    /// log4net.Appender.FileAppender+MinimalLockを改造してみたもの
    /// </summary>
    public class Log4netLazyMinimalLock : log4net.Appender.FileAppender.LockingModelBase {
        LazyWriteFileStream stream = null;

        public override void OpenFile(string filename, bool append, Encoding encoding) {
            try {
                using (CurrentAppender.SecurityContext.Impersonate(this)) {
                    stream = new LazyWriteFileStream(filename, append ? FileMode.Append : FileMode.Create);
                }
            } catch (Exception e) {
                CurrentAppender.ErrorHandler.Error("Unable to acquire lock on file " + filename, e);
            }
        }

        public override void CloseFile() {
            using (CurrentAppender.SecurityContext.Impersonate(this)) {
                stream.Dispose();
                stream = null;
            }
        }

        public override Stream AcquireLock() {
            return stream;
        }

        public override void ReleaseLock() {
        }
    }

    /// <summary>
    /// 遅延書き込みを行うFileStream。
    /// </summary>
    public class LazyWriteFileStream : Stream {
        readonly string path;
        FileMode fileMode;

        readonly string mutexName;
        readonly string dirPath;

        readonly ByteSequenceQueue byteSequenceQueue = new ByteSequenceQueue();

        volatile bool threadValid = true;
        readonly Thread thread;

        public LazyWriteFileStream(string path, FileMode fileMode) {
            this.path = path;
            this.fileMode = fileMode;
            mutexName = path.Replace('\\', '/');
            dirPath = Path.GetDirectoryName(path);

            thread = new Thread(WriteThread);
            thread.Name = "LazyWriteFileStream";
            thread.IsBackground = true; // 一応残らないように…。
            thread.Start();
        }

        protected override void Dispose(bool disposing) {
            lock (byteSequenceQueue) {
                threadValid = false;
                Monitor.Pulse(byteSequenceQueue);
            }

            base.Dispose(disposing);

            thread.Join();

            // 残ったデータがある場合は書き込みを行う
            if (0 < byteSequenceQueue.Count) {
                byte[] writeBuffer = new byte[byteSequenceQueue.Count];
                byteSequenceQueue.Dequeue(writeBuffer, 0, writeBuffer.Length);

                try {
                    WriteData(writeBuffer, writeBuffer.Length);
                } catch {
                    // 書き込み失敗したなら諦める。
                }
            }
        }

        public override bool CanRead {
            get { return false; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        public override void Flush() {
            // 無視
        }

        public override long Length {
            get {
                FileInfo fi = new FileInfo(path);
                return fi.Exists ? fi.Length : 0;
            }
        }

        public override long Position {
            get { return Length; }
            set { throw new InvalidOperationException(); }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value) {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            // UTF-8のBOMだけが来たら無視する。
            // log4netは<encoding value="UTF-8" />すると
            // Encoding = Encoding.GetEncoding("UTF-8")的な感じになる仕組みになっていて、
            // それで得られるのは new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) 的なインスタンスなので、
            // BOMが付く。
            // BOMに罪は無いのだけれど、BOMのせいで何も出力してないログファイルがエクスプローラから見て1kbになってしまって、
            // 出力されたかどうかが分からなかったり、そもそも何も出力してないのにファイルが出来てしまうのが
            // 割と邪魔なので、何とか回避したい。
            // でもこれを回避するのは設定ファイルからは無理そうな気がするので、
            // ここで対症療法的にてきとーに回避してみる。
            if (count == 3 &&
                buffer[offset + 0] == 0xef &&
                buffer[offset + 1] == 0xbb &&
                buffer[offset + 2] == 0xbf) {
                return;
            }

            lock (byteSequenceQueue) {
                byteSequenceQueue.Enqueue(buffer, offset, count);
                Monitor.Pulse(byteSequenceQueue);
            }
        }

        /// <summary>
        /// 書き込みスレッド
        /// </summary>
        private void WriteThread() {
            byte[] writeBuffer = new byte[65536];
            int writeLength = 0;
            int retrySleep = 1;

            while (threadValid) {
                lock (byteSequenceQueue) {
                    if (0 < writeLength) {
                        // 前回書き込み時エラーにより書き込むべきモノが残っている場合、
                        // 適当に待ってから書き込み。
                        Monitor.Wait(byteSequenceQueue, retrySleep);
                        retrySleep = Math.Min(retrySleep * 2, 4096); // 連続でエラる場合は徐々にsleep時間を長くしてみる。
                        // バッファにまだ空きがあって、書き込むデータが来てたらバッファに追記
                        int writable = writeBuffer.Length - writeLength;
                        if (0 < writable && 0 < byteSequenceQueue.Count) {
                            int len = Math.Min(writable, byteSequenceQueue.Count);
                            byteSequenceQueue.Dequeue(writeBuffer, writeLength, len);
                            writeLength += len;
                        }
                    } else if (byteSequenceQueue.Count <= 0) {
                        // キューとwriteBufferが空なら待つ
                        Monitor.Wait(byteSequenceQueue);
                        continue;
                    } else {
                        // 何かしら書き込むべきデータがあるなら500ミリ秒(適当)待ってからキューのデータをwriteBufferへ。
                        Monitor.Wait(byteSequenceQueue, 500);

                        writeLength = Math.Min(byteSequenceQueue.Count, writeBuffer.Length);
                        byteSequenceQueue.Dequeue(writeBuffer, 0, writeLength);
                    }
                }

                // 書き込み
                try {
                    WriteData(writeBuffer, writeLength);
                    writeLength = 0; // 書き込み成功
                    retrySleep = 1; // 戻しておく
                } catch {
                    // エラったら次回また。
                }
            }
        }

        /// <summary>
        /// ファイルへ書き込み
        /// </summary>
        private void WriteData(byte[] writeBuffer, int writeLength) {
            using (Mutex mutex = new Mutex(false, mutexName)) // 一応排他してみる
            {
                mutex.WaitOne();
                try {
                    Directory.CreateDirectory(dirPath);
                    using (FileStream stream = new FileStream(path, fileMode)) {
                        stream.Write(writeBuffer, 0, writeLength);
                    }
                    fileMode = FileMode.Append; // 1回でも書き込みが成功したら次回からは追記モードとする。
                } finally {
                    mutex.ReleaseMutex();
                }
            }
        }
    }

    /// <summary>
    /// バッファ。Queue&lt;byte&gt;に1バイトずつEnqueueするのは遅そうな気がするので自前実装。でも速度は未測定。
    /// </summary>
    public class ByteSequenceQueue {
        byte[] queue = new byte[1024];
        int writePos = 0;
        int readPos = 0;

        /// <summary>
        /// 中身の数を返す。
        /// </summary>
        public int Count {
            // queue.Lengthは2の乗数の前提
            get { return (writePos + queue.Length - readPos) & (queue.Length - 1); }
        }

        /// <summary>
        /// 書き込み
        /// </summary>
        public void Enqueue(byte[] buffer, int offset, int count) {
            int currentCount = Count;
            int cap = queue.Length;
            // サイズが足りなければ増やす。
            // 一時的に大量のログが出力されてサイズがでかくなってしまうと
            // 二度と小さくならないのがしょぼいけど小さくする機能は割と面倒なので手抜き。
            if (cap - currentCount < count) {
                do {
                    cap <<= 1;
                } while (cap - currentCount < count);

                byte[] newQueue = new byte[cap];
                if (readPos <= writePos) {
                    Array.Copy(queue, readPos, newQueue, readPos, writePos - readPos);
                } else {
                    Array.Copy(queue, readPos, newQueue, 0, queue.Length - readPos);
                    Array.Copy(queue, 0, newQueue, queue.Length - readPos, writePos);
                    writePos = queue.Length - readPos + writePos;
                    readPos = 0;
                }
                queue = newQueue;
            }
            // 書き込み
            if (count <= queue.Length - writePos) {
                Array.Copy(buffer, offset, queue, writePos, count);
                writePos = (writePos + count) & (queue.Length - 1);
            } else {
                int end = queue.Length - writePos;
                Array.Copy(buffer, offset, queue, writePos, end);
                Array.Copy(buffer, offset + end, queue, 0, count - end);
                writePos = count - end;
            }
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public void Dequeue(byte[] buffer, int offset, int count) {
            if (Count < count) {
                throw new ArgumentOutOfRangeException("count");
            }

            if (readPos <= writePos) {
                Array.Copy(queue, readPos, buffer, offset, count);
                readPos = (readPos + count) & (queue.Length - 1);
            } else {
                if (readPos + count <= queue.Length) {
                    Array.Copy(queue, readPos, buffer, offset, count);
                    readPos = (readPos + count) & (queue.Length - 1);
                } else {
                    int end = queue.Length - readPos;
                    Array.Copy(queue, readPos, buffer, offset, end);
                    Array.Copy(queue, 0, buffer, offset + end, count - end);
                    readPos = count - end;
                }
            }
        }
    }
}
