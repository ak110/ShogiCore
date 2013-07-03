using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Threading {
    [Serializable]
    public class ThreadException : ApplicationException {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ThreadException() { }
        public ThreadException(string message) : base(message) { }
        public ThreadException(string message, Exception inner) : base(message, inner) { }
        protected ThreadException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
