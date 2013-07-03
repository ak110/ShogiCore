using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.CSA {
    /// <summary>
    /// CSA通信対局時の状態
    /// </summary>
    /// <remarks>
    /// http://shogi-server.sourceforge.jp/protocol.html
    /// ↑コレに合わせてみた。
    /// サーバー用なのでちょっとややこしいものの。。
    /// </remarks>
    public enum CSAState {
        /// <summary>
        /// TCP的に接続した状態
        /// </summary>
        TCPConnect,
        /// <summary>
        /// ログイン未完了
        /// </summary>
        Login,
        /// <summary>
        /// 拡張モードで%%GAME未完了
        /// </summary>
        Connected,
        /// <summary>
        /// 対局待ち
        /// </summary>
        GameWaiting,
        /// <summary>
        /// 対局条件受信中
        /// </summary>
        GameReceiving,
        /// <summary>
        /// AGREE未完了
        /// </summary>
        AgreeWaiting,
        /// <summary>
        /// ゲーム中
        /// </summary>
        Game,
        /// <summary>
        /// ログアウト済み
        /// </summary>
        Finished,
#if false
        /// <summary>
        /// TCP的に接続した状態
        /// </summary>
        TCPConnect,
#endif
    }
}
