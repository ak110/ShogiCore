using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.BoardProperty {
    /// <summary>
    /// Boardには将棋のルールに直接関係ないコードは埋め込みたくなかったので
    /// 別クラスにしてみる為のインターフェース。
    /// これはこれで変な設計の気はしつつも…。
    /// </summary>
    public interface IBoardProperty : ICloneable {
        /// <summary>
        /// Boardに接続。
        /// イベントリスナとかを登録したり差分計算の準備をしたり。
        /// </summary>
        void Attach(Board board);
        /// <summary>
        /// Boardから接続解除
        /// </summary>
        void Detach(Board board);
        /// <summary>
        /// 複製の作成。BoardにはAttach()してないものを返すべし。
        /// </summary>
        new IBoardProperty Clone();
    }
}
