using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.CSA {
    /// <summary>
    /// ゲーム情報
    /// </summary>
    public class CSAGameSummary {
        /// <summary>
        /// Protocol_Version
        /// </summary>
        public string Protocol_Version { get; set; }
        /// <summary>
        /// Protocol_Mode
        /// </summary>
        public string Protocol_Mode { get; set; }
        /// <summary>
        /// Format
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        /// Declaration
        /// </summary>
        public string Declaration { get; set; }
        /// <summary>
        /// Game_ID
        /// </summary>
        public string Game_ID { get; set; }
        /// <summary>
        /// Name+
        /// </summary>
        public string NameP { get; set; }
        /// <summary>
        /// Name-
        /// </summary>
        public string NameN { get; set; }
        /// <summary>
        /// Your_Turn。先手なら+、後手なら-
        /// </summary>
        public char Your_Turn { get; set; }
        /// <summary>
        /// false:NO, true:YES
        /// </summary>
        public bool Rematch_On_Draw { get; set; }
        /// <summary>
        /// To_Move。先手なら+、後手なら-
        /// </summary>
        public char To_Move { get; set; }

        /// <summary>
        /// 持ち時間情報
        /// </summary>
        public class Time {
            /// <summary>
            /// Time_Unit
            /// </summary>
            public string Time_Unit { get; set; }
            /// <summary>
            /// 1手の着手に必ず記録される消費時間
            /// </summary>
            public int Least_Time_Per_Move { get; set; }
            /// <summary>
            /// false:切り捨て、true:切り上げ
            /// </summary>
            public bool Time_Roundup { get; set; }
            /// <summary>
            /// 通算の持時間
            /// </summary>
            public int Total_Time { get; set; }
            /// <summary>
            /// Byoyomi
            /// </summary>
            public int Byoyomi { get; set; }

            /// <summary>
            /// 既定値を設定
            /// </summary>
            public Time() {
                Time_Unit = "1sec";
                //Least_Time_Per_Move = 0;
                Time_Roundup = true;
            }
        }

        /// <summary>
        /// 持ち時間情報(先手と後手)
        /// </summary>
        public Time[] Times { get; private set; }

        /// <summary>
        /// 初期化
        /// </summary>
        public CSAGameSummary() {
            Protocol_Version = "1.1";
            Protocol_Mode = "Server";
            Format = "Shogi 1.0";
            Declaration = ""; // "Jishogi 1.1"
            Game_ID = "";
            Rematch_On_Draw = true;
            Times = new Time[] { new Time(), new Time() };
        }
    }
}
