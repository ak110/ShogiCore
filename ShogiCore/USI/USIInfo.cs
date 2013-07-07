using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ShogiCore.Notation;

namespace ShogiCore.USI {
    /// <summary>
    /// USIコマンド
    /// </summary>
    public struct USIInfo {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// コマンド名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// パラメータ。空っぽでもnullにはせずに""とする。
        /// </summary>
        public string[] Parameters { get; set; }

        /// <summary>
        /// 空
        /// </summary>
        public static readonly USIInfo Empty = new USIInfo();

        /// <summary>
        /// 空ならtrue
        /// </summary>
        public bool IsEmpty { get { return string.IsNullOrEmpty(Name); } }

        /// <summary>
        /// USIコマンドの解析
        /// </summary>
        public static List<USIInfo> Parse(string input) {
            if (input.Contains(Environment.NewLine)) {
                throw new ArgumentException("改行が含まれた文字列は解析出来ません", "line");
            }

            List<USIInfo> list = new List<USIInfo>();

            var inputList = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // 2個以上の空白は無視しちゃう(例えinfo stringの中でも1個扱いにしちゃう)
            for (int i = 0; i < inputList.Length; i++) {
                switch (inputList[i]) {
                    case "depth":
                    case "seldepth":
                    case "time":
                    case "nodes":
                    case "nps":
                    case "currmove":
                    case "hashfull":
                        // オプション1個なもの。
                        list.Add(new USIInfo() {
                            Name = inputList[i],
                            Parameters = new[]{ inputList[i + 1] },
                        });
                        i++;
                        break;

                    case "score":
                        // オプション2個なもの。
                        list.Add(new USIInfo() {
                            Name = inputList[i],
                            Parameters = new[] { inputList[i + 1], inputList[i + 2] },
                        });
                        i += 2;
                        break;

                    case "pv":
                        // 行末まで。
                        list.Add(new USIInfo() {
                            Name = inputList[i],
                            Parameters = inputList.Skip(i + 1).ToArray(),
                        });
                        // SFENの指し手でないものが出てきたら普通のコマンドかもしれないのでそれ以降はもう一度処理する。
                        for (i++; i < inputList.Length; i++) {
                            if (!SFENNotationReader.IsMove(inputList[i])) {
                                --i; // forなので1個戻す
                                break;
                            }
                        }
                        break;

                    case "string":
                        // 行末まで。
                        list.Add(new USIInfo() {
                            Name = inputList[i],
                            Parameters = inputList.Skip(i + 1).ToArray(),
                        });
                        i = inputList.Length;
                        break;

                    default:
                        logger.Warn("不正なinfoコマンド: " + inputList[i]);
                        list.Add(new USIInfo() { Name = inputList[i] });
                        break;
                }
            }

            return list;
        }

        /// <summary>
        /// 適当文字列化
        /// </summary>
        public override string ToString() {
            return Name + " " + string.Join(" ", Parameters);
        }
    }
}
