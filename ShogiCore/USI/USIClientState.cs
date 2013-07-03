using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.USI {
    /// <summary>
    /// USIクライアントの状態
    /// </summary>
    public enum USIClientState {
        /// <summary>
        /// ゲーム開始待ち
        /// </summary>
        WaitGame,
        /// <summary>
        /// ゲーム中
        /// </summary>
        Game,
        /// <summary>
        /// 終了中
        /// </summary>
        Quit,
    }
}
