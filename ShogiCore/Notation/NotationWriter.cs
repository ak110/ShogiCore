using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 文字列な棋譜書き込みを行うクラス
    /// </summary>
    /// <exception cref="NotationException">エラーなど</exception>
    public interface IStringNotationWriter {
        string WriteToString(IEnumerable<Notation> notations);
    }

    /// <summary>
    /// バイナリな棋譜書き込みを行うクラス
    /// </summary>
    /// <exception cref="NotationException">エラーなど</exception>
    public interface IBinaryNotationWriter {
        byte[] WriteToBinary(IEnumerable<Notation> notations);
    }

    public abstract class StringNotationWriter : IBinaryNotationWriter, IStringNotationWriter {
        /// <summary>
        /// Encoding
        /// </summary>
        Encoding encoding;

        /// <summary>
        /// Encoding
        /// </summary>
        public virtual Encoding Encoding {
            get {
                if (encoding == null) {
                    encoding = Encoding.GetEncoding(932);
                }
                return encoding;
            }
            protected set { encoding = value; }
        }

        /// <summary>
        /// WriteToBinary
        /// </summary>
        public byte[] WriteToBinary(Notation notation) {
            return WriteToBinary(new[] { notation });
        }

        /// <summary>
        /// WriteToString
        /// </summary>
        public string WriteToString(Notation notation) {
            return WriteToString(new[] { notation });
        }

        #region IBinaryNotationWriter メンバ

        public byte[] WriteToBinary(IEnumerable<Notation> notations) {
            return Encoding.GetBytes(WriteToString(notations));
        }

        #endregion

        #region IStringNotationWriter メンバ

        public abstract string WriteToString(IEnumerable<Notation> notations);

        #endregion
    }
}
