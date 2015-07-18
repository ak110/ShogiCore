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
        /// <param name="firstTurnTime">先手の残り持ち時間[ms]</param>
        /// <param name="secondTurnTime">後手の残り持ち時間[ms]</param>
        /// <param name="byoyomi">秒読み</param>
        Move DoTurn(Board board, int firstTurnTime, int secondTurnTime, int byoyomi);

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
