using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// ""の付け方
    /// </summary>
    public enum CSVDoubleQuotationMode {
        /// <summary>
        /// 付けない
        /// </summary>
        None,
        /// <summary>
        /// 全て付ける
        /// </summary>
        All,
        /// <summary>
        /// 最小限付ける
        /// </summary>
        Minimum,
    }

    /// <summary>
    /// CSVDoubleQuotationModeの拡張メソッド
    /// </summary>
    public static class CSVDoubleQuotationModeExtensions {
        /// <summary>
        /// エスケープ処理
        /// </summary>
        public static string Escape(this CSVDoubleQuotationMode doubleQuotation, string data) {
            switch (doubleQuotation) {
            case CSVDoubleQuotationMode.None:
                Debug.Assert(data.IndexOfAny(new char[] { '"', ',', '\r', '\n' }) < 0, "不正な文字: " + data);
                return data;

            case CSVDoubleQuotationMode.All:
                if (data == null) {
                    return "\"\"";
                }
                return "\"" + data.Replace("\"", "\"\"") + "\"";

            case CSVDoubleQuotationMode.Minimum:
                if (data == null) {
                    return "";
                }
                if (data.IndexOfAny(new char[] { '"', ',', '\r', '\n' }) < 0) {
                    return data;
                }
                return "\"" + data.Replace("\"", "\"\"") + "\"";

            default:
                throw new InvalidOperationException("CSVDoubleQuotationModeが不正: " + doubleQuotation.ToString());
            }
        }
    }

    /// <summary>
    /// CSV書き込み用クラス。
    /// </summary>
    /// <remarks>
    /// http://www.kasai.fm/wiki/rfc4180jp
    /// </remarks>
    public class CSVWriter : IDisposable {
        /// <summary>
        /// 出力先
        /// </summary>
        public Stream BaseStream { get; private set; }
        /// <summary>
        /// ""の付け方。既定値はAll。
        /// </summary>
        public CSVDoubleQuotationMode DoubleQuotation { get; set; }
        /// <summary>
        /// Encoding
        /// </summary>
        public Encoding Encoding { get; private set; }
        /// <summary>
        /// 列の区切り文字。","
        /// </summary>
        public string ColumnSeparator { get; private set; }
        /// <summary>
        /// 行の区切り文字。"\r\n"
        /// </summary>
        public string RecordSeparator { get; private set; }

        StreamWriter writer;
        bool lineFirst = true; // 行頭

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="output">出力先</param>
        /// <param name="doubleQuotation"> ""の付け方。既定値はAll。</param>
        public CSVWriter(Stream output, CSVDoubleQuotationMode doubleQuotation = CSVDoubleQuotationMode.All) {
            BaseStream = output;
            DoubleQuotation = doubleQuotation;
            Encoding = Encoding.Default; // ExcelやOOoはデフォルトでShift_JIS扱いするっぽいので…。
            ColumnSeparator = ",";
            RecordSeparator = "\r\n"; // 明示的にCRLFとする

            writer = new StreamWriter(BaseStream, Encoding);
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            writer.Close();
            // StreamWriterはBaseStreamを閉じないが、
            // BinaryWriterは閉じる。
            // どっちに合わせるべきかよく分からんけどとりあえず閉じておく。。
            BaseStream.Close();
        }

        /// <summary>
        /// １列追加
        /// </summary>
        /// <param name="col">列データ</param>
        public void Append(string col) {
            if (!lineFirst) {
                writer.Write(ColumnSeparator);
            }
            writer.Write(DoubleQuotation.Escape(col));
            lineFirst = false;
        }

        /// <summary>
        /// 改行を追加
        /// </summary>
        public void AppendLine() {
            writer.Write(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1) {
            if (!lineFirst) {
                writer.Write(ColumnSeparator);
            }
            writer.Write(DoubleQuotation.Escape(col1));
            writer.Write(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1, string col2) {
            if (!lineFirst) {
                writer.Write(ColumnSeparator);
            }
            writer.Write(DoubleQuotation.Escape(col1));
            writer.Write(ColumnSeparator);
            writer.Write(DoubleQuotation.Escape(col2));
            writer.Write(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1, string col2, string col3) {
            if (!lineFirst) {
                writer.Write(ColumnSeparator);
            }
            writer.Write(DoubleQuotation.Escape(col1));
            writer.Write(ColumnSeparator);
            writer.Write(DoubleQuotation.Escape(col2));
            writer.Write(ColumnSeparator);
            writer.Write(DoubleQuotation.Escape(col3));
            writer.Write(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1, string col2, string col3, string col4) {
            if (!lineFirst) {
                writer.Write(ColumnSeparator);
            }
            writer.Write(DoubleQuotation.Escape(col1));
            writer.Write(ColumnSeparator);
            writer.Write(DoubleQuotation.Escape(col2));
            writer.Write(ColumnSeparator);
            writer.Write(DoubleQuotation.Escape(col3));
            writer.Write(ColumnSeparator);
            writer.Write(DoubleQuotation.Escape(col4));
            writer.Write(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(params string[] cols) {
            if (!lineFirst) {
                writer.Write(ColumnSeparator);
            }
            if (0 < cols.Length) {
                writer.Write(DoubleQuotation.Escape(cols[0]));
                for (int i = 0 + 1; i < cols.Length; i++) {
                    writer.Write(ColumnSeparator);
                    writer.Write(DoubleQuotation.Escape(cols[i]));
                }
            }
            writer.Write(RecordSeparator);
            lineFirst = true;
        }
    }

    /// <summary>
    /// CSV書き込みクラスその2。
    /// </summary>
    /// <remarks>
    /// 通常はStringBuilderに書き込み、Flush()でファイルへ。
    /// CSVWriterだとDispose()が面倒な場合などに。
    /// </remarks>
    public class BufferedCSVWriter {
        /// <summary>
        /// 書き込むファイルのパス
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// ""の付け方。既定値はAll。
        /// </summary>
        public CSVDoubleQuotationMode DoubleQuotation { get; set; }
        /// <summary>
        /// Encoding
        /// </summary>
        public Encoding Encoding { get; private set; }
        /// <summary>
        /// 列の区切り文字。","
        /// </summary>
        public string ColumnSeparator { get; private set; }
        /// <summary>
        /// 行の区切り文字。"\r\n"
        /// </summary>
        public string RecordSeparator { get; private set; }

        StringBuilder str = new StringBuilder();
        bool lineFirst = true; // 行頭

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="path">書き込むファイルのパス</param>
        public BufferedCSVWriter(string path) : this(path, CSVDoubleQuotationMode.All) { }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="path">書き込むファイルのパス</param>
        /// <param name="doubleQuotation"> ""の付け方。既定値はAll。</param>
        public BufferedCSVWriter(string path, CSVDoubleQuotationMode doubleQuotation) {
            FilePath = path;
            DoubleQuotation = doubleQuotation;
            Encoding = Encoding.Default; // ExcelやOOoはデフォルトでShift_JIS扱いするっぽいので…。
            ColumnSeparator = ",";
            RecordSeparator = "\r\n"; // 明示的にCRLFとする
        }

        /// <summary>
        /// ファイルが存在しなかったりサイズ0だったりししつつ、かつAppendもしてないならtrue。
        /// </summary>
        public bool IsEmpty() {
            if (0 < str.Length) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); // ディレクトリは作ってしまう (手抜き)
            return !File.Exists(FilePath) || new FileInfo(FilePath).Length <= 0;
        }

        /// <summary>
        /// 書き込み (忘れずに呼ぶ事)
        /// </summary>
        public void Flush() {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            lock (str) {
                File.AppendAllText(FilePath, str.ToString(), Encoding);
                str.Length = 0;
            }
        }

        /// <summary>
        /// １列追加
        /// </summary>
        /// <param name="col">列データ</param>
        public void Append(string col) {
            if (!lineFirst) {
                str.Append(ColumnSeparator);
            }
            str.Append(DoubleQuotation.Escape(col));
            lineFirst = false;
        }

        /// <summary>
        /// 改行を追加
        /// </summary>
        public void AppendLine() {
            str.Append(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1) {
            if (!lineFirst) {
                str.Append(ColumnSeparator);
            }
            str.Append(DoubleQuotation.Escape(col1));
            str.Append(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1, string col2) {
            if (!lineFirst) {
                str.Append(ColumnSeparator);
            }
            str.Append(DoubleQuotation.Escape(col1));
            str.Append(ColumnSeparator);
            str.Append(DoubleQuotation.Escape(col2));
            str.Append(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1, string col2, string col3) {
            if (!lineFirst) {
                str.Append(ColumnSeparator);
            }
            str.Append(DoubleQuotation.Escape(col1));
            str.Append(ColumnSeparator);
            str.Append(DoubleQuotation.Escape(col2));
            str.Append(ColumnSeparator);
            str.Append(DoubleQuotation.Escape(col3));
            str.Append(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(string col1, string col2, string col3, string col4) {
            if (!lineFirst) {
                str.Append(ColumnSeparator);
            }
            str.Append(DoubleQuotation.Escape(col1));
            str.Append(ColumnSeparator);
            str.Append(DoubleQuotation.Escape(col2));
            str.Append(ColumnSeparator);
            str.Append(DoubleQuotation.Escape(col3));
            str.Append(ColumnSeparator);
            str.Append(DoubleQuotation.Escape(col4));
            str.Append(RecordSeparator);
            lineFirst = true;
        }

        /// <summary>
        /// 1行追加
        /// </summary>
        public void AppendLine(params string[] cols) {
            if (!lineFirst) {
                str.Append(ColumnSeparator);
            }
            if (0 < cols.Length) {
                str.Append(DoubleQuotation.Escape(cols[0]));
                for (int i = 0 + 1; i < cols.Length; i++) {
                    str.Append(ColumnSeparator);
                    str.Append(DoubleQuotation.Escape(cols[i]));
                }
            }
            str.Append(RecordSeparator);
            lineFirst = true;
        }
    }
}
