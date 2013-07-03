using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ShogiCore.IO {
    /// <summary>
    /// 入出力関係の諸々。
    /// </summary>
    public static class IOUtility {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// zipファイル対応のファイル読み込み
        /// </summary>
        public static IEnumerable<byte[]> ReadAllBytes(string fileName) {
            if (Path.GetExtension(fileName).ToLowerInvariant() == ".zip") {
                using (var zip = new Toolkit.IO.ZipReader(fileName)) {
                    foreach (var entry in zip.Entries) {
                        if (!entry.IsFile) continue; // ファイル以外は無視
                        yield return zip.ReadAllBytes(entry);
                    }
                }
            } else {
                yield return File.ReadAllBytes(fileName);
            }
        }

        /// <summary>
        /// log4netの全appenderをflushする。
        /// </summary>
        /// <remarks>
        /// immediateFlush = trueの時にフォルダをrotateしたりするとバッファ分が切れてしまったりするので。
        /// </remarks>
        public static void FlushLog4net() {
            log4net.Repository.ILoggerRepository rep = log4net.LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in rep.GetAppenders()) {
                var buffered = appender as log4net.Appender.BufferingAppenderSkeleton;
                if (buffered != null) {
                    buffered.Flush();
                }
            }
        }

        /// <summary>
        /// ログのローテート。
        /// </summary>
        /// <param name="path">ログファイルのパス</param>
        /// <param name="count">残す個数。ゼロなら処理無しでreturn。</param>
        public static void LogRotate(string path, int count) {
            try {
                lock (path) { // ←とってもいい加減な排他処理
                    if (count <= 0) return; // 無効な設定。

                    string tempPath = path + "." + Path.GetRandomFileName();
                    try {
                        // 自分対自分などでの2重のローテート対策(適当気味)
                        try {
                            // 存在しないなら処理無し
                            if (!File.Exists(path)) return;
                            // 存在チェックの直後にファイルを移動しておくことで、
                            // 複数のプロセスが重複して処理することを避けてみる
                            File.Move(path, tempPath);
                        } catch (Exception e) { // たぶんFileNotFoundException
                            logger.Debug("ログローテートをキャンセル", e);
                            return;
                        }

                        // 最後を削除
                        string last = path + "." + count.ToString();
                        if (File.Exists(last)) File.Delete(last);
                        // 1個ずつずらす
                        for (int i = count - 1; 0 < i; i--) {
                            string file = path + "." + i.ToString();
                            if (File.Exists(file)) File.Move(file, last);
                            last = file;
                        }
                        // 最初のを1へ。
                        File.Move(tempPath, last);
                    } catch {
                        try {
                            if (File.Exists(tempPath)) {
                                File.Move(tempPath, path);
                            }
                        } catch (Exception ex) {
                            logger.Warn("ログローテート失敗時のリカバー失敗", ex);
                        }
                        throw;
                    }
                }
            } catch (Exception e) {
                logger.Warn("ログローテート失敗", e);
            }
        }

        /// <summary>
        /// 日付入りファイル名化
        /// </summary>
        /// <param name="path">ファイルのパス</param>
        public static void RenameDateTime(string path) {
            lock (path) { // ←とってもいい加減な排他処理
                if (File.Exists(path)) {
                    string prefix = AppIOManager.GetDateTimeString();
                    string ext = Path.GetExtension(path);
                    string to = Path.ChangeExtension(path, null) +
                        "." + prefix + ext;
                    try {
                        File.Move(path, to);
                    } catch (Exception e) {
                        if (File.Exists(to)) {
                            for (int i = 0; ; i++) {
                                string to2 = Path.ChangeExtension(path, null) +
                                    "." + prefix +
                                    "." + i.ToString() + "." + ext;
                                if (!File.Exists(to2)) {
                                    File.Move(path, to2);
                                    break;
                                }
                            }
                        } else {
                            logger.Warn("ログのリネームに失敗", e);
                        }
                    }
                } else if (Directory.Exists(path)) {
                    string suffix = AppIOManager.GetDateTimeString();
                    string to = path + "-" + suffix;
                    try {
                        Directory.Move(path, to);
                    } catch (Exception e) {
                        if (Directory.Exists(to)) {
                            for (int i = 0; ; i++) {
                                string to2 = to + "-" + i.ToString();
                                if (!Directory.Exists(to2)) {
                                    Directory.Move(path, to2);
                                    break;
                                }
                            }
                        } else {
                            logger.Warn("ログディレクトリのリネームに失敗", e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ディレクトリのロック
        /// </summary>
        public class DirectoryLocker : IDisposable {
            string dir;

            /// <summary>
            /// ディレクトリのロック
            /// </summary>
            /// <param name="dir">ディレクトリ</param>
            public DirectoryLocker(string dir) {
                this.dir = dir;
                LockDirectory(dir);
            }

            /// <summary>
            /// アンロック
            /// </summary>
            public void Dispose() {
                UnlockDirectory(dir);
            }
        }

        /// <summary>
        /// ディレクトリのロック
        /// </summary>
        /// <remarks>
        /// dir/.lock というファイルを作成する。
        /// samba経由とかbatでも使えるように。
        /// batでは、
        /// <code>
        /// goto :EOF
        /// goto :EndOfLockFunctions
        ///     :LockDirectory
        ///     :RetryLock
        ///         copy /Y NUL "%~dpnx1\lock.tmp"
        ///         rename "%~dpnx1\lock.tmp" .lock || goto :RetryLock
        ///     goto :EOF
        /// 
        ///     :UnlockDirectory
        ///         del "%~dpnx1\.lock"
        ///     goto :EOF
        /// :EndOfLockFunctions
        /// </code>
        /// とか書いておいて、
        /// <code>
        /// call :LockDirectory "D:\Data\ShogiCore\BlunderUtility\Data"
        ///     ...
        /// call :UnlockDirectory "D:\Data\ShogiCore\BlunderUtility\Data"
        /// </code>
        /// とかやって使うといいはず。
        /// </remarks>
        /// <param name="dir">ディレクトリ</param>
        public static void LockDirectory(string dir) {
            const int MaxWaitSeconds = 10;

            string path = Path.Combine(dir, ".lock");

            for (int i = 0; i < MaxWaitSeconds * 100; i++) {
                try {
                    using (FileStream s = File.Open(path, FileMode.CreateNew)) {
                        //s.Write(new byte[0], 0, 0);
                        //s.Flush();
                        return;
                    }
                } catch (IOException) {
                    System.Threading.Thread.Sleep(10);
                } catch (UnauthorizedAccessException) { // 時々コレも発生する
                    System.Threading.Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// アンロック。
        /// </summary>
        /// <param name="dir">ディレクトリ</param>
        public static void UnlockDirectory(string dir) {
            string path = Path.Combine(dir, ".lock");
            File.Delete(path);
        }
    }
}
