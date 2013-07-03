using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShogiCore.Notation;

namespace ShogiCore.CSA {
    /// <summary>
    /// CSAプロトコルでの応答すべきデータ
    /// </summary>
    public struct CSAInternalCommand {
        /// <summary>
        /// 種別
        /// </summary>
        public CSAInternalCommandTypes CommandType { get; set; }
        /// <summary>
        /// 指し手情報 (SelfMove、EnemyMoveの場合のみ)
        /// </summary>
        public MoveDataEx MoveDataEx { get; set; }
        /// <summary>
        /// 受信した情報
        /// </summary>
        public string ReceivedString { get; set; }
    }

    /// <summary>
    /// 種別
    /// </summary>
    public enum CSAInternalCommandTypes {
        /// <summary>
        /// ログイン失敗
        /// </summary>
        LoginFailed,
        /// <summary>
        /// 拡張モードの##Loginなど。%%Gameを応答する。
        /// </summary>
        ExConnected,
        /// <summary>
        /// CSAテストモードのログイン済み状態。CHALLENGEを応答する。
        /// </summary>
        TestConnected,
        /// <summary>
        /// Game_Summary受信終了。AGREEかREJECTを応答する。
        /// </summary>
        GameSummaryReceived,
        /// <summary>
        /// START。先手の場合は指し手を応答する。
        /// </summary>
        Start,
        /// <summary>
        /// 自分の指し手を受信。
        /// </summary>
        SelfMove,
        /// <summary>
        /// 相手の指し手を受信。指し手を応答する。
        /// </summary>
        EnemyMove,
        /// <summary>
        /// 特殊な指し手を受信。
        /// </summary>
        SpecialMove,
        /// <summary>
        /// ログアウト完了や通信切断など。
        /// </summary>
        Disconnected,
    }
}
