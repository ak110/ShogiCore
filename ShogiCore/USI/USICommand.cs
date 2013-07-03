using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.USI {
    /// <summary>
    /// USIコマンド
    /// </summary>
    public struct USICommand {
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
        public static readonly USICommand Empty = new USICommand();

        /// <summary>
        /// 空ならtrue
        /// </summary>
        public bool IsEmpty { get { return string.IsNullOrEmpty(Name); } }

        // ↓構造体ではコレ書けない…
        //private USICommand() { }

        /// <summary>
        /// コマンドの作成
        /// </summary>
        public static USICommand Create(string name) {
            return new USICommand() { Name = name };
        }

        /// <summary>
        /// USIコマンドの解析
        /// </summary>
        public static USICommand Parse(string line) {
            if (line.Contains(Environment.NewLine)) {
                throw new ArgumentException("改行が含まれた文字列は解析出来ません", "line");
            }

            int sp = line.IndexOf(' ');
            if (sp < 0) {
                return new USICommand() { Name = line, Parameters = "" };
            } else {
                return new USICommand() {
                    Name = line.Substring(0, sp),
                    Parameters = line.Substring(sp + 1),
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
