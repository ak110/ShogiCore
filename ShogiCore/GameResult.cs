using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 勝ち負け引き分け
    /// </summary>
    public enum GameResult {
        Win = 1,
        Draw = 0,
        Lose = -1,
    }

    /// <summary>
    /// 終局理由
    /// </summary>
    public enum GameEndReason {
        /// <summary>
        /// 詰み（CSA棋譜：%TSUMI）
        /// </summary>
        Mate,
        /// <summary>
        /// 不詰（CSA棋譜：%FUZUMI）
        /// </summary>
        NoMate,
        /// <summary>
        /// 投了（CSA棋譜：%TORYO）
        /// </summary>
        Resign,
        /// <summary>
        /// 時間切れ（CSA棋譜：%TIME_UP）
        /// </summary>
        TimeUp,
        /// <summary>
        /// 無効な指し手（CSA棋譜：%ILLEGAL_MOVE）
        /// </summary>
        IllegalMove,
        /// <summary>
        /// 間違った勝ち宣言
        /// </summary>
        IllegalWinDecl,
        /// <summary>
        /// 千日手（CSA棋譜：%SENNICHITE）
        /// </summary>
        Endless,
        /// <summary>
        /// 連続王手の千日手
        /// </summary>
        Perpetual,
        /// <summary>
        /// 入玉勝ち（CSA棋譜：%KACHI）
        /// </summary>
        Nyuugyoku,
        /// <summary>
        /// 引き分け（CSA棋譜：%HIKIWAKE）
        /// </summary>
        NyuugyokuDraw,
        /// <summary>
        /// 持将棋（CSA棋譜：%JISHOGI）
        /// </summary>
        Jishogi,
        /// <summary>
        /// 強制中断
        /// </summary>
        Abort,
        /// <summary>
        /// 同一棋譜の発生
        /// </summary>
        SameNotation,
        /// <summary>
        /// 中断（CSA棋譜：%CHUDAN）
        /// </summary>
        Interruption,
        /// <summary>
        /// 待った（CSA棋譜：%MATTA）
        /// </summary>
        Matta,
        /// <summary>
        /// エラー（CSA棋譜：%ERROR）
        /// </summary>
        Error,
        /// <summary>
        /// その他
        /// </summary>
        Unknown,
    }

    /// <summary>
    /// 終局理由の文字列化
    /// </summary>
    public static class GameEndReasonUtility {
        /// <summary>
        /// 終局理由の文字列化
        /// </summary>
        /// <param name="reason">終局理由</param>
        /// <returns>日本語表現</returns>
        public static string ToString(GameEndReason reason) {
            switch (reason) {
                case GameEndReason.Mate: return "詰み";
                case GameEndReason.NoMate: return "不詰";
                case GameEndReason.Resign: return "投了";
                case GameEndReason.TimeUp: return "時間切れ";
                case GameEndReason.IllegalMove: return "無効な指し手";
                case GameEndReason.IllegalWinDecl: return "間違った勝ち宣言";
                case GameEndReason.Endless: return "千日手";
                case GameEndReason.Perpetual: return "連続王手の千日手";
                case GameEndReason.Nyuugyoku: return "入玉勝ち";
                case GameEndReason.NyuugyokuDraw: return "引き分け";
                case GameEndReason.Jishogi: return "持将棋";
                case GameEndReason.Abort: return "強制中断";
                case GameEndReason.SameNotation: return "同一棋譜の発生";
                case GameEndReason.Interruption: return "中断";
                case GameEndReason.Matta: return "待った";
                case GameEndReason.Error: return "エラー";
                case GameEndReason.Unknown: return "不明";
                default: goto case GameEndReason.Unknown;
            }
        }
        /// <summary>
        /// 終局理由のCSA棋譜文字列化
        /// </summary>
        /// <param name="reason">終局理由</param>
        /// <returns>CSA棋譜形式の特殊な指し手</returns>
        public static string ToStringCSA(this GameEndReason reason) {
            switch (reason) {
                case GameEndReason.Mate: return "%TSUMI";
                case GameEndReason.NoMate: return "%FUZUMI";
                case GameEndReason.Resign: return "%TORYO";
                case GameEndReason.TimeUp: return "%TIME_UP";
                case GameEndReason.IllegalMove: return "%ILLEGAL_MOVE";
                case GameEndReason.IllegalWinDecl: return "%ILLEGAL_MOVE"; // 適当
                case GameEndReason.Endless: return "%SENNICHITE";
                case GameEndReason.Perpetual: return "%ILLEGAL_MOVE"; // 適当
                case GameEndReason.Nyuugyoku: return "%KACHI";
                case GameEndReason.NyuugyokuDraw: return "%HIKIWAKE";
                case GameEndReason.Jishogi: return "%JISHOGI";
                case GameEndReason.Abort: return "%CHUDAN"; // 適当
                case GameEndReason.SameNotation: return "%ERROR"; // 適当
                case GameEndReason.Interruption: return "%CHUDAN";
                case GameEndReason.Matta: return "%MATTA";
                case GameEndReason.Error: return "%ERROR";
                case GameEndReason.Unknown: return "%ERROR"; // 適当
                default: goto case GameEndReason.Error;
            }
        }
    }
}
