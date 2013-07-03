using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// 手リスト
    /// </summary>
    public class MoveList : List<Move> {
        //*
        public MoveList() { }
        /*/
        public MoveList() : base(0x40) { }
        //*/

        public MoveList(int capacity) : base(capacity) { }

        private MoveList(MoveList other) : base(other) { }

        /// <summary>
        /// 複製の作成
        /// </summary>
        public MoveList Clone() {
            return new MoveList(this);
        }

        /*
        /// <summary>
        /// 暗黙の型変換
        /// </summary>
        public static implicit operator Move[](MoveList moves) {
            return moves.ToArray();
        }
        //*/
    }
}
