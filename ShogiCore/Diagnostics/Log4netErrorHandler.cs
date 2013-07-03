using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Core;
using System.IO;

namespace ShogiCore.Diagnostics {
    /// <summary>
    /// log4netのエラーハンドラ
    /// </summary>
    public class Log4netErrorHandler : IErrorHandler {
        #region IErrorHandler メンバ

        public void Error(string message) {
            ConsoleUtility.WriteErrorWithOpen(message);
        }

        public void Error(string message, Exception e) {
            ConsoleUtility.WriteErrorWithOpen(message + Environment.NewLine + e.ToString());
        }

        public void Error(string message, Exception e, ErrorCode errorCode) {
            ConsoleUtility.WriteErrorWithOpen(errorCode.ToString() + " : " + message + Environment.NewLine + e.ToString());
        }

        #endregion
    }
}
