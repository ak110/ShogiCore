using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace ShogiCore.IO {
    /// <summary>
    /// byte*なMemoryStream。読み込み専用。
    /// </summary>
    public unsafe class UnsafeMemoryReadStream : Stream {
        byte* buffer;
        long length;
        Action<IntPtr> deleter; // Action<byte*>は「エラー CS0306: 型 'byte*' は、型引数に使用されない可能性があります。」になるので仕方なくIntPtr。

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="buffer">バッファ</param>
        /// <param name="length">長さ</param>
        /// <param name="deleter">削除子</param>
        public UnsafeMemoryReadStream(byte* buffer, long length, Action<IntPtr> deleter) {
            this.buffer = buffer;
            this.length = length;
            this.deleter = deleter;
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public override void Close() {
            base.Close();
            if (buffer != null) {
                deleter((IntPtr)buffer);
                buffer = null;
            }
        }

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanSeek {
            get { return true; }
        }

        public override bool CanWrite {
            get { return false; }
        }

        public override void Flush() {
        }

        public override long Length { get { return length; } }

        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count) {
            long newPos = Position + count;
            if (newPos <= Length) {
                Marshal.Copy((IntPtr)(this.buffer + Position), buffer, offset, count);
                Position = newPos;
                return count;
            } else {
                throw new IOException("バッファを超えてアクセス: 位置=" + Position.ToString() +
                    " サイズ=" + count.ToString() + " 最大サイズ=" + Length.ToString());
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch (origin) {
            case SeekOrigin.Begin: Position = offset; break;
            case SeekOrigin.Current: Position += offset; break;
            case SeekOrigin.End: Position = Length - offset; break;
            default:
                throw new ArgumentOutOfRangeException("無効なSeekOrigin");
            }
            return Position;
        }

        public override void SetLength(long value) {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new InvalidOperationException();
        }
    }
}
