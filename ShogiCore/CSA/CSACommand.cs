using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.CSA {
    /// <summary>
    /// CSAコマンド
    /// </summary>
    public struct CSACommand {
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
        public static readonly CSACommand Empty = new CSACommand();

        /// <summary>
        /// 空ならtrue
        /// </summary>
        public bool IsEmpty { get { return string.IsNullOrEmpty(Name); } }

        /// <summary>
        /// コマンドの作成
        /// </summary>
        public static CSACommand Create(string name) {
            return new CSACommand() { Name = name };
        }

        /// <summary>
        /// USIコマンドの解析
        /// </summary>
        public static CSACommand Parse(string line) {
            if (line.Contains(Environment.NewLine)) {
                throw new ArgumentException("改行が含まれた文字列は解析出来ません", "line");
            }

            int col = line.IndexOf(':');
            if (col < 0) {
                int sp = line.IndexOf(' ');
                if (sp < 0) {
                    return new CSACommand() { Name = line, Parameters = "" };
                } else {
                    return new CSACommand() {
                        Name = line.Substring(0, sp),
                        Parameters = line.Substring(sp + 1),
                    };
                }
            } else {
                return new CSACommand() {
                    Name = line.Substring(0, col),
                    Parameters = line.Substring(col + 1),
                };
            }
        }

        /// <summary>
        /// 適当文字列化
        /// </summary>
        public override string ToString() {
            return Name + " " + Parameters;
        }
    }
}
