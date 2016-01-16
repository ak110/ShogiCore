using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// プレイヤーのインターフェース
    /// </summary>
    public interface IPlayer : IDisposable {
        /// <summary>
        /// おなまえ。
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// CPUなら統計情報などの文字。特に無ければ空文字。
        /// </summary>
        string GetDisplayString(Board board);

        /// <summary>
        /// 対局開始
        /// </summary>
        void GameStart();

        /// <summary>
        /// 一手の処理
        /// </summary>
        /// <param name="btime">先手の持ち時間情報</param>
        /// <param name="wtime">後手の持ち時間情報</param>
        Move DoTurn(Board board, PlayerTime btime, PlayerTime wtime);

        /// <summary>
        /// DoTurn()などが実行中なら強制終了させる(別スレッドから呼ばれる)
        /// </summary>
        void Abort();

        /// <summary>
        /// 対局終了
        /// </summary>
        void GameEnd(GameResult result);
    }
}
