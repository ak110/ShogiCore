using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋譜の読み込みを行う。形式の(手抜き)自動判別処理付き。
    /// </summary>
    public class NotationLoader {
        /// <summary>
        /// IStringNotationReader
        /// </summary>
        public List<IStringNotationReader> StringReaders { get; private set; }
        /// <summary>
        /// IBinaryNotationReader
        /// </summary>
        public List<IBinaryNotationReader> BinaryReaders { get; private set; }

        /// <summary>
        /// 初期化
        /// </summary>
        public NotationLoader() {
            StringReaders = new List<IStringNotationReader> {
                new KifuNotationReader(),
                new PCLNotationReader(),
                new SFENNotationReader(),
                new DPPNotationReader(),
            };
            BinaryReaders = new List<IBinaryNotationReader> {
                new KisenNotationReader(),
                //new UsapyonNotationReader(), ←棋泉と区別が付かんのでデフォルトでは使わない
            };
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public List<Notation> Load(byte[] data) {
            if (Array.IndexOf(data, (byte)0x00, 0, Math.Min(512, data.Length)) < 0) {
                // 先頭512バイトに0が無いならテキストかも？
                try {
                    // テキストの場合はCSAにしてもkifにしてもShift_JISやASCIIのはず。
                    string str = Encoding.GetEncoding(932).GetString(data);
                    return Load(str);
                } catch (DecoderFallbackException) {
                    // エラー？
                }
            }
            // ここに来たなら多分バイナリのはず。
            return InnerLoad(data, BinaryReaders);
        }

        /// <summary>
        /// 読み込み。失敗時は空っぽ。
        /// </summary>
        public List<Notation> Load(string data) {
            return InnerLoad(data, StringReaders);
        }

        /// <summary>
        /// 読み込み。
        /// </summary>
        private static List<Notation> InnerLoad(string data, List<IStringNotationReader> readers) {
            for (int i = 0, n = readers.Count; i < n; i++) {
                try {
                    lock (readers[i]) { // 念のため
                        if (!readers[i].CanRead(data)) continue; // 未対応なので次へ。
                        var list = readers[i].Read(data).ToList();
                        if (list.Count <= 0)
                            continue; // 未対応扱い

                        // 1個以上の有効な棋譜だったらそれを返しておしまい。
                        return list;
                    }
                } catch (NotationFormatException) {
                    continue; // エラーが起きたら未対応扱いとして次へ行くいい加減実装。
                }
            }
            // 空っぽ。
            return new List<Notation>();
        }

        /// <summary>
        /// 読み込み。(中身は上のをコピペ)
        /// </summary>
        private static List<Notation> InnerLoad(byte[] data, List<IBinaryNotationReader> readers) {
            for (int i = 0, n = readers.Count; i < n; i++) {
                try {
                    lock (readers[i]) { // 念のため
                        if (!readers[i].CanRead(data)) continue; // 未対応なので次へ。
                        var list = readers[i].Read(data).ToList();
                        if (list.Count <= 0) continue; // 一応未対応扱い
                        if (list.Count == 1 && list[0].InitialBoard == null &&
                            (list[0].Moves == null || list[0].Moves.Length == 0)) continue; // 一応未対応扱い
                        // 1個以上の有効な棋譜だったらそれを返しておしまい。
                        return list;
                    }
                } catch (NotationException) {
                    continue; // エラーが起きたら未対応扱いとして次へ行くいい加減実装。
                }
            }
            // 空っぽ。
            return new List<Notation>();
        }
    }
}
