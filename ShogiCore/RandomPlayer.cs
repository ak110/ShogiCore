using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// ランダム。
    /// </summary>
    public class RandomPlayer : IPlayer {
        /// <summary>
        /// 初期化。
        /// </summary>
        public RandomPlayer() {
            Name = "RandomPlayer";
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
        }

        #region IPlayer メンバ

        public string Name { get; set; }

        public string GetDisplayString(Board board) {
            return "";
        }

        public void GameStart() {
        }

        public Move DoTurn(Board board, int firstTurnTime, int secondTurnTime, int byoyomi) {
            MoveList moves = board.GetMovesSafe();
            return moves[RandUtility.Next(moves.Count)];
        }

        public void Abort() {
        }

        public void GameEnd(GameResult result) {
        }

        #endregion
    }
}
