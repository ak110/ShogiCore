using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ShogiCore {
    /// <summary>
    /// アプリケーションのディレクトリ構成などを扱うクラス
    /// </summary>
    public static class AppIOManager {
        /// <summary>
        /// %APPDATA%/ShogiCore/
        /// </summary>
        public static string AppDataDirectory { get; private set; }

        /// <summary>
        /// ./Data/
        /// </summary>
        public static string DataDirectory { get; private set; }

        /// <summary>
        /// ./Logs/
        /// </summary>
        public static string LogsDirectory { get; private set; }
        /// <summary>
        /// ./Logs-Position/
        /// </summary>
        public static string LogsPositionDirectory { get; private set; }
        /// <summary>
        /// ./Logs-CSV/
        /// </summary>
        public static string LogsCSVDirectory { get; private set; }

        /// <summary>
        /// ./Learnings/
        /// </summary>
        public static string LearningsDirectory { get; private set; }
        /// <summary>
        /// ./Temp-EvaluationTable/
        /// </summary>
        public static string TempEvaluationTableDirectory { get; private set; }
        /// <summary>
        /// ./Temp-OrderingTable/
        /// </summary>
        public static string TempOrderingTableDirectory { get; private set; }
        /// <summary>
        /// ./Temp-OrderingTable/
        /// </summary>
        public static string TempBookDirectory { get; private set; }

        /// <summary>
        /// ./ThinkTimeData/
        /// </summary>
        public static string ThinkTimeDataDirectory { get; private set; }
        /// <summary>
        /// ./ThinkTimeDataNotations/
        /// </summary>
        public static string ThinkTimeDataNotationsDirectory { get; private set; }

        /// <summary>
        /// ./ShogiCore.Options.dat
        /// </summary>
        public static string BlunderOptionsFile { get; set; }

        /// <summary>
        /// ./Error.log。書き込み時はこの変数でlockかけること。
        /// </summary>
        public static string ErrorLogFile { get; private set; }

        /// <summary>
        /// 初期化
        /// </summary>
        static AppIOManager() {
            string ds = Path.DirectorySeparatorChar.ToString();
            string baseDirectoryDS = AppDomain.CurrentDomain.BaseDirectory + ds;

            AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blunder");
            DataDirectory = baseDirectoryDS + "Data";
            LogsDirectory = baseDirectoryDS + "Logs";
            LogsPositionDirectory = baseDirectoryDS + "Logs-Position";
            LogsCSVDirectory = baseDirectoryDS + "Logs-CSV";
            LearningsDirectory = baseDirectoryDS + "Learnings";
            TempEvaluationTableDirectory = baseDirectoryDS + "Temp-EvaluationTable";
            TempOrderingTableDirectory = baseDirectoryDS + "Temp-OrderingTable";
            TempBookDirectory = baseDirectoryDS + "Temp-Book";
            ThinkTimeDataDirectory = baseDirectoryDS + "ThinkTimeData";
            ThinkTimeDataNotationsDirectory = baseDirectoryDS + "ThinkTimeDataNotations";
            BlunderOptionsFile = baseDirectoryDS + "Blunder.Options.dat";
            ErrorLogFile = baseDirectoryDS + "Error.log";
        }

        /// <summary>
        /// 日時な文字列を返す
        /// </summary>
        public static string GetDateTimeString() {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }
}
