using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Toolkit.IO {
	/// <summary>
	/// Streamの一部分なStream。
	/// 排他処理とか全く考えてないので注意。
	/// </summary>
	public class SubStream : Stream {
	    readonly Stream parent;
	    readonly long offset;
	    readonly long size;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="p">親Stream</param>
		/// <param name="o">オフセット</param>
		/// <param name="s">サイズ</param>
		public SubStream(Stream p, long o, long s) {
			parent = p;
			offset = o;
			size = s;
			// 初期位置へ移動
			Debug.Assert(parent.CanSeek);
			parent.Position = offset;
		}

		public override bool CanRead {
			get { return parent.CanRead; }
		}

		public override bool CanSeek {
			get { return parent.CanSeek; }
		}

		public override bool CanWrite {
			get { return false; }
		}

		public override void Flush() {
			//parent.Flush();
		}

		public override long Length {
			get { return size; }
		}

		public override long Position {
			get {
				return parent.Position - offset;
			}
			set {
				if (value < 0 || size <= value) {
					throw new ArgumentOutOfRangeException("Position", value, "ファイル位置の設定に失敗しました");
				}

				parent.Position = value + offset;
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			int toRead = Math.Min(count, (int)(Length - Position));
			return 0 < toRead ? parent.Read(buffer, offset, toRead) : toRead;
		}

		public override long Seek(long offset, SeekOrigin origin) {
			long pos;
			switch (origin) {
			case SeekOrigin.Begin: pos = offset; break;
			case SeekOrigin.Current: pos = Position + offset; break;
			case SeekOrigin.End: pos = Length - offset; break;
			default:
				throw new ArgumentOutOfRangeException("origin");
			}
			Position = pos;
			return pos;
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotSupportedException();
		}
	}
}
