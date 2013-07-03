using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.USI {
    /// <summary>
    /// USIコマンド
    /// </summary>
    public struct USIInfo {
        /// <summary>
        /// コマンド名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// パラメータ。空っぽでもnullにはせずに""とする。
        /// </summary>
        public string Parameters { get; set; }

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

            int i = 0;

            List<USIInfo> list = new List<USIInfo>();
            while (i < input.Length) {
                int sp = input.IndexOf(' ', i);
                if (sp < 0) {
                    string str = input.Substring(i).Trim();
                    if (0 < str.Length) list.Add(new USIInfo() { Name = str });
                    break;
                }

                string subcmd = input.Substring(i, sp - i);
                switch (subcmd) {
                case "depth":
                case "seldepth":
                case "time":
                case "nodes":
                case "nps":
                case "currmove":
                case "hashfull":
                    // オプション1個なもの。
                    {
                        int sp2 = input.IndexOf(' ', sp + 1);
                        if (sp2 < 0) sp2 = input.Length;
                        list.Add(new USIInfo() {
                            Name = subcmd,
                            Parameters = input.Substring(sp + 1, sp2 - sp - 1),
                        });
                        i = sp2 + 1;
                    }
                    break;

                case "score":
                    // オプション2個なもの。
                    {
                        int sp2 = input.IndexOf(' ', sp + 1);
                        int sp3 = sp2 < 0 ? input.Length : input.IndexOf(' ', sp2 + 1);
                        list.Add(new USIInfo() {
                            Name = subcmd,
                            Parameters = input.Substring(sp + 1, sp3 - sp - 1),
                        });
                        i = sp3 + 1;
                    }
                    break;
                    
                case "string":
                case "pv":
                default:
                    Debug.Assert(subcmd == "string" || subcmd == "pv", "不明なサブコマンド: " + subcmd);
                    // 行末まで。
                    list.Add(new USIInfo() {
                        Name = subcmd,
                        Parameters = input.Substring(sp + 1),
                    });
                    i = input.Length;
                    break;
                }

            }

            return list;
        }
    }
}
