using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ShogiCore {
    /// <summary>
    /// コンソールアプリ用のユーティリティ
    /// </summary>
    public static class ConsoleUtility {
        /// <summary>
        /// 最大3回までしか開かない
        /// </summary>
        public const int MaxOpenCount = 3;
        /// <summary>
        /// 開いた回数
        /// </summary>
        public static int ErrorOpenCount { get; set; }

        /// <summary>
        /// pauseコマンド風一時停止。(手抜き実装)
        /// </summary>
        public static void Pause() {
            Console.WriteLine("続行するには何かキーを押してください . . . ");
            Console.ReadKey();
        }

        /// <summary>
        /// 標準エラーとError.logに書き込む
        /// </summary>
        /// <param name="e">例外</param>
        public static void WriteError(Exception e) {
            WriteError(e.ToString());
        }

        /// <summary>
        /// 標準エラーとError.logに書き込む
        /// </summary>
        /// <param name="msg">エラーメッセージ</param>
        public static void WriteError(string msg) {
            string error = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]") + Environment.NewLine + msg + Environment.NewLine;
            Console.Error.Write(error);
            lock (AppIOManager.ErrorLogFile) {
                File.AppendAllText(AppIOManager.ErrorLogFile, error);
            }
        }

        /// <summary>
        /// 標準エラーとError.logに書き込み、Error.logを開く。
        /// </summary>
        /// <param name="e">例外</param>
        public static void WriteErrorWithOpen(Exception e) {
            WriteErrorWithOpen(e.ToString());
        }

        /// <summary>
        /// 標準エラーとError.logに書き込み、Error.logを開く。
        /// </summary>
        /// <param name="msg">エラーメッセージ</param>
        public static void WriteErrorWithOpen(string msg) {
            WriteError(msg);
            if (ErrorOpenCount < MaxOpenCount) {
                ErrorOpenCount++;
                Utility.ProcessStartSafe(AppIOManager.ErrorLogFile);
            }
        }

        /// <summary>
        /// PadRightのEncoding.Default版
        /// </summary>
        public static string PadRightEncoding(string str, int totalWidth) {
            int len = Encoding.Default.GetByteCount(str);
            return str + new string(' ', Math.Max(0, totalWidth - len));
        }

        /// <summary>
        /// PadLeftのEncoding.Default版
        /// </summary>
        public static string PadLeftEncoding(string str, int totalWidth) {
            int len = Encoding.Default.GetByteCount(str);
            return new string(' ', Math.Max(0, totalWidth - len)) + str;
        }

        /// <summary>
        /// ファイルと標準出力へ書き込み
        /// </summary>
        public static void WriteLineWithFile(string file, string line) {
            File.AppendAllText(file, line + Environment.NewLine);
            Console.WriteLine(line);
        }
    }
}
