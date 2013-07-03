using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.USI {
    /// <summary>
    /// イベント引数
    /// </summary>
    public class USIEventArgs : EventArgs {
        /// <summary>
        /// 送信する/受信したメッセージ
        /// </summary>
        public string Message { get; private set; }
        /// <summary>
        /// 初期化
        /// </summary>
        public USIEventArgs(string message) {
            Message = message;
        }
    }

    /// <summary>
    /// イベント引数
    /// </summary>
    public class USICommandEventArgs : EventArgs {
        /// <summary>
        /// 送信する/受信したコマンド
        /// </summary>
        public USICommand USICommand { get; private set; }
        /// <summary>
        /// 処理したならtrue。USIDriver.ReceiveCommand()のキューに入らなくなる。
        /// </summary>
        public bool Handled { get; set; }
        /// <summary>
        /// 初期化
        /// </summary>
        public USICommandEventArgs(USICommand command) {
            USICommand = command;
        }
    }

    /// <summary>
    /// イベント引数
    /// </summary>
    public class USIInfoEventArgs : EventArgs {
        /// <summary>
        /// 受信したサブコマンド
        /// </summary>
        public List<USIInfo> SubCommands { get; private set; }
        /// <summary>
        /// 初期化
        /// </summary>
        public USIInfoEventArgs(List<USIInfo> subCommands) {
            SubCommands = subCommands;
        }
    }
}
