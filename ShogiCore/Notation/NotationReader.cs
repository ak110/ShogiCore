using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋譜の読み込みを行うクラスのインターフェース
    /// </summary>
    /// <exception cref="NotationException">エラーなど</exception>
    public interface IBinaryNotationReader {
        /// <summary>
        /// 読み込めるかどうか判定
        /// </summary>
        bool CanRead(byte[] data);
        /// <summary>
        /// 読み込み。
        /// </summary>
        IEnumerable<Notation> Read(byte[] data);
    }

    /// <summary>
    /// 棋譜の読み込みを行うクラスのインターフェース
    /// </summary>
    /// <exception cref="NotationException">エラーなど</exception>
    public interface IStringNotationReader {
        /// <summary>
        /// 読み込めるかどうか判定
        /// </summary>
        bool CanRead(string data);
        /// <summary>
        /// 読み込み。
        /// </summary>
        IEnumerable<Notation> Read(string data);
    }

    /// <summary>
    /// IStringNotationReaderの実装補助
    /// </summary>
    public abstract class StringNotationReader : IBinaryNotationReader, IStringNotationReader {
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

        #region IBinaryNotationReader メンバ

        public bool CanRead(byte[] data) {
            return CanRead(data == null ? null : Encoding.GetString(data));
        }

        public IEnumerable<Notation> Read(byte[] data) {
            return Read(data == null ? null : Encoding.GetString(data));
        }

        #endregion

        #region IStringNotationReader メンバ

        public abstract bool CanRead(string data);

        public abstract IEnumerable<Notation> Read(string data);

        #endregion
    }
}
