using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// (Pre|Post)(Do|Undo)用のEventArgs。
    /// </summary>
    public class BoardMoveEventArgs : EventArgs {
        /// <summary>
        /// Board
        /// </summary>
        public Board Board { get; private set; }
        /// <summary>
        /// Move
        /// </summary>
        public Move Move { get; private set; }
        /// <summary>
        /// 初期化
        /// </summary>
        public BoardMoveEventArgs(Board board) {
            Board = board;
        }
        /// <summary>
        /// 初期化
        /// </summary>
        public BoardMoveEventArgs Reset(Move move) {
            Move = move;
            return this;
        }
    }

}
