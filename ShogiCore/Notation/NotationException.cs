using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋譜の読み込みエラーなど。
    /// </summary>
    [Serializable]
    public class NotationException : ApplicationException {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public NotationException() { }
        public NotationException(string message) : base(message) { }
        public NotationException(string message, Exception inner) : base(message, inner) { }
        protected NotationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// 棋譜の読み込み時のエラーのうち、明らかに別フォーマットと思われるエラーの場合。読み込むべきフォーマットだけど壊れていると思われる場合などはNoationExceptionとする。
    /// </summary>
    [SerializableAttribute]
    public class NotationFormatException : NotationException {
        public NotationFormatException() { }
        public NotationFormatException(string message) : base(message) { }
        public NotationFormatException(string message, Exception inner) : base(message, inner) { }
        protected NotationFormatException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
