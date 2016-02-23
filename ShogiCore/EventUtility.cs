using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiCore {
    /// <summary>
    /// データを1つ持つEventArgs
    /// </summary>
    public class DataEventArgs<TData> : EventArgs {
        public TData Data { get; private set; }
        public DataEventArgs(TData data) { Data = data; }
    }

    /// <summary>
    /// event関連のヘルパ
    /// </summary>
    public static class EventUtility {
        /// <summary>
        /// イベントの呼び出し
        /// </summary>
        public static void InvokeSafe<TData>(this EventHandler<DataEventArgs<TData>> ev, object sender, TData data) {
            if (ev != null)
                ev(sender, new DataEventArgs<TData>(data));
        }

        /// <summary>
        /// イベントの呼び出し
        /// </summary>
        public static void InvokeSafe<TEventArgs>(this EventHandler<TEventArgs> ev, object sender, TEventArgs e) where TEventArgs : EventArgs {
            if (ev != null)
                ev(sender, e);
        }
    }
}
