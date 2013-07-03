using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.CSA {
    /// <summary>
    /// CSAClientからのイベント発生時の引数
    /// </summary>
    public class CSAClientEventArgs : EventArgs {
        /// <summary>
        /// CSAClient
        /// </summary>
        public CSAClient CSAClient { get; private set; }

        public CSAClientEventArgs(CSAClient client) {
            CSAClient = client;
        }
    }
}
