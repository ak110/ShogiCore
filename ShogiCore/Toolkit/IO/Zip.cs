using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace Toolkit.IO {
	/// <summary>
	/// ZIPファイルの読み込み
	/// </summary>
	public class ZipReader : IDisposable {
	    readonly Stream stream;
	    readonly bool leaveOpen;
	    readonly List<ZipEntry> entries = new List<ZipEntry>();

		/// <summary>
		/// 初期化。
		/// </summary>
		/// <param name="fileName">読み込み元ファイル名</param>
		public ZipReader(string fileName) : this(File.Open(fileName, FileMode.Open), false) { }

		/// <summary>
		/// 初期化。
		/// </summary>
		/// <param name="data">読み込み元データ</param>
		public ZipReader(byte[] data) : this(new MemoryStream(data), false) { }

		/// <summary>
		/// 初期化。
		/// </summary>
		/// <param name="stream">読み込み元ファイル</param>
		public ZipReader(Stream stream) : this(stream, false) { }

		/// <summary>
		/// 初期化。
		/// </summary>
		/// <param name="stream">読み込み元ファイル</param>
		/// <param name="leaveOpen">streamを開いたままにするのかどうか。</param>
		/// <exception cref="IOException">ZIP書庫として上手く読み込めなかった場合の例外</exception>
		public ZipReader(Stream stream, bool leaveOpen) {
			this.stream = stream;
			this.leaveOpen = leaveOpen;

			StreamRandAccessor acc = new StreamRandAccessor(this.stream);
			long endrecOffset = FindEndRec(acc);
			// エンドレコードの読み取り
			int fileCount = acc.GetUshort(endrecOffset + 10);
			long pos = acc.GetUint(endrecOffset + 16);
			entries.Clear();
			for (int i = 0; i < fileCount; i++) {
				if (acc.GetUint(pos) != 0x02014b50) { // "PK\x1\x2"
					throw new IOException("ZIPファイルの展開に失敗しました");
				}
				//ushort generalFlags = acc.GetUshort(pos + 8);
				//ushort compressionMethod = acc.GetUshort(pos + 10);
				ulong dosTime = acc.GetUint(pos + 12);
				uint compressedSize = acc.GetUint(pos + 20);
				uint size = acc.GetUint(pos + 24);
				ushort filenameLength = acc.GetUshort(pos + 28);
				ushort extraFieldLength = acc.GetUshort(pos + 30);
				ushort fileCommentLength = acc.GetUshort(pos + 32);
				ulong localHeaderOffset = acc.GetUint(pos + 42);

				byte[] fileNameBytes = new byte[filenameLength];
				for (int j = 0; j < filenameLength; ++j) {
					fileNameBytes[j] = acc.GetByte(j + pos + 46);
				}

				ZipEntry e = new ZipEntry(Encoding.Default.GetString(fileNameBytes)) {
				    Size = size,
				    CompressedSize = compressedSize,
				    DosTime = (long) dosTime,
				    HeaderOffset = (long) localHeaderOffset
				};
			    entries.Add(e);

				// 次のレコードへ
				pos += 46 + filenameLength
					+ extraFieldLength + fileCommentLength;
			}
		}

		/// <summary>
		/// エンドレコードを検索
		/// </summary>
		private long FindEndRec(StreamRandAccessor acc) {
			long stoppos;
			//	ulongって引き算とか比較が面倒くさいですなぁ．．（´Д｀）
			if (acc.Length < 66000) {
				stoppos = 0;
			} else {
				stoppos = acc.Length - 66000;
			}
			for (long i = acc.Length - 22; i >= stoppos; --i) {
				if (acc.GetUint(i) == 0x06054b50) { // "PK\0x05\0x06"
					ushort endcommentlength = acc.GetUshort(i + 20);
					if (i + 22 + endcommentlength != acc.Length)
						continue;
					return i;
				}
			}
			throw new IOException("ZIPファイルの展開に失敗しました");
		}
			
		/// <summary>
		/// 後始末
		/// </summary>
		public void Close() {
			Dispose();
		}

		/// <summary>
		/// 後始末
		/// </summary>
		public void Dispose() {
			if (!leaveOpen) {
				stream.Close();
			}
		}

		/// <summary>
		/// ファイルの個数を取得
		/// </summary>
		public int Count {
			get { return entries.Count; }
		}
		/// <summary>
		/// ファイルのリストを取得
		/// </summary>
		public ZipEntry[] Entries {
			get { return entries.ToArray(); }
		}

		/// <summary>
		/// ファイルを読み込み
		/// </summary>
		public Stream Open(int i) {
			return Open(entries[i]);
		}

		/// <summary>
		/// ファイルを読み込み
		/// </summary>
		public Stream Open(ZipEntry e) {
			StreamRandAccessor acc = new StreamRandAccessor(this.stream);
			long lhPos = e.HeaderOffset;
			//ushort lhFlag = acc.GetUshort(lhPos + 6);
			ushort lhCompressionMethod = acc.GetUshort(lhPos + 8);
			ushort lhFilenameLength = acc.GetUshort(lhPos + 26);
			ushort lhExtraField = acc.GetUshort(lhPos + 28);
			long startpos = lhPos + 30
			+ lhFilenameLength + lhExtraField;
			switch (lhCompressionMethod) {
			case 0:
				Debug.Assert(e.Size == e.CompressedSize);
				return new SubStream(stream, startpos, e.Size);
			case 8:
				return new DeflateStream(
					new SubStream(stream, startpos, e.CompressedSize),
					CompressionMode.Decompress, false);
			default:
				throw new IOException("ZIPファイルにサポートしていない圧縮形式のデータが含まれていました");
			}
		}

		/// <summary>
		/// ファイルを読み込み
		/// </summary>
		public byte[] ReadAllBytes(int i) {
			return ReadAllBytes(entries[i]);
		}
		/// <summary>
		/// ファイルを読み込み
		/// </summary>
		public byte[] ReadAllBytes(ZipEntry e) {
			using (Stream s = Open(e)) {
				byte[] data = new byte[e.Size];
				s.Read(data, 0, data.Length);
				return data;
			}
		}

		/// <summary>
		/// ファイルを解凍
		/// </summary>
		public void Extract(string fileName, int i) {
			Extract(fileName, entries[i]);
		}

		/// <summary>
		/// ファイルを解凍
		/// </summary>
		public void Extract(string fileName, ZipEntry e) {
			Directory.CreateDirectory(Path.GetDirectoryName(fileName));
			using (Stream s = Open(e))
			using (FileStream file = File.Create(fileName)) {
				byte[] buffer = new byte[0x1000];
				while (true) {
					int len = s.Read(buffer, 0, buffer.Length);
					if (len == 0) break;
					file.Write(buffer, 0, len);
				}
			}
			// 一応最終更新日時もセットしてみる。
			new FileInfo(fileName).LastWriteTime = e.DateTime;
		}
	}

	#region ZipReader実装用クラス

	class StreamRandAccessor {
	    readonly Stream _stream;
	    readonly byte[] _buffer = new byte[256]; // 読み込みバッファ
		int _readsize;	 // バッファに読み込めたサイズ。
		long _pos;		 // 現在読み込んでいるバッファのストリーム上の位置

		public StreamRandAccessor(Stream f) {
			_stream = f;
			// 読み込んでいないので。
			_pos = 0;
			_readsize = 0;
		}

		public byte GetByte(long i) {
			Check(ref i, 1);
			return _buffer[i];
		}

		public ushort GetUshort(long i) {
			Check(ref i, 2);
			byte b0 = _buffer[i];
			byte b1 = _buffer[i + 1];
			return (ushort)((b1 << 8) | b0);
		}

		public uint GetUint(long i) {
			Check(ref i, 4);
			byte b0 = _buffer[i];
			byte b1 = _buffer[i + 1];
			byte b2 = _buffer[i + 2];
			byte b3 = _buffer[i + 3];
			return (uint)((b3 << 24) | (b2 << 16) | (b1 << 8) | b0);
		}

		public long Read(byte[] data, long pos, uint size) {
			_stream.Seek(pos, SeekOrigin.Begin);
			int s;
			try { s = _stream.Read(data, 0, (int)size); } catch { s = 0; }
			return s;
		}

		private void Check(ref long i, uint size) {
			if (i < _pos || _pos + _readsize < i + size) {
				int offset = (int)((_buffer.Length - size) / 2);
				if (i < offset) {
					_pos = 0;
				} else {
					_pos = i - offset;
				}
				_stream.Seek(_pos, SeekOrigin.Begin);
				_readsize = _stream.Read(_buffer, 0, _buffer.Length);
			}
			i -= _pos;
		}

		public long Length {
			get {
				return _stream.Length;
			}
		}
	}

	#endregion
			
	/// <summary>
	/// ZIP書庫内のファイルやディレクトリ１個を表すオブジェクト
	/// </summary>
	public class ZipEntry : ICloneable {
		string name;
		uint size, compressedSize;
		uint dosTime;

	    public ZipEntry(string name) {
			this.name = name.Replace('/', Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// 複製の作成
		/// </summary>
		public ZipEntry Clone() {
			ZipEntry copy = (ZipEntry)MemberwiseClone();
			// string以外の参照型なメンバがあればここでコピー
			return copy;
		}

		#region ICloneable メンバ

		object ICloneable.Clone() {
			return Clone();
		}

		#endregion

		/// <summary>
		/// ファイル名
		/// </summary>
		public string Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// ファイルサイズ
		/// </summary>
		public long Size {
			get { return (long)size; }
			set { size = (uint)value; }
		}

		/// <summary>
		/// 圧縮時のサイズ
		/// </summary>
		public long CompressedSize {
			get { return (long)compressedSize; }
			set { compressedSize = (uint)value; }
		}

		/// <summary>
		/// 最終更新日時
		/// </summary>
		public long DosTime {
			get { return (long)dosTime; }
			set { dosTime = (uint)value; }
		}

	    /// <summary>
	    /// ファイル先頭からローカルヘッダまでのオフセット
	    /// </summary>
	    public long HeaderOffset { get; set; }

	    /// <summary>
		/// 最終更新日時
		/// </summary>
		public DateTime DateTime {
			get {
				if (dosTime == 0) {
					return DateTime.Now;
				} else {
					int sec = 2 * ((int)dosTime & 0x1f);
					int min = ((int)dosTime >> 5) & 0x3f;
					int hrs = ((int)dosTime >> 11) & 0x1f;
					int day = ((int)dosTime >> 16) & 0x1f;
					int mon = ((int)(dosTime >> 21) & 0xf);
					int year = ((int)(dosTime >> 25) & 0x7f) + 1980;
					return new DateTime(year, mon, day, hrs, min, sec);
				}
			}
			set {
				dosTime = (uint)(
					(value.Year - 1980 & 0x7f) << 25 |
					(value.Month) << 21 |
					(value.Day) << 16 |
					(value.Hour) << 11 |
					(value.Minute) << 5 |
					(value.Second) >> 1);
			}
		}

		/// <summary>
		/// ディレクトリならtrue
		/// </summary>
		public bool IsDirectory {
			get {
				return name.Length > 0 &&
					(name[name.Length - 1] == '/' || name[name.Length - 1] == '\\');
			}
		}

		/// <summary>
		/// ファイルならtrue
		/// </summary>
		public bool IsFile {
			get { return !IsDirectory; }
		}

		/// <summary>
		/// 適当文字列化
		/// </summary>
		public override string ToString() {
			return Name + " (" + Size.ToString() + ")";
		}
	}
}
