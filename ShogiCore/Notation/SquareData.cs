using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 座標
    /// </summary>
    public struct SquareData {
        /// <summary>
        /// 筋
        /// </summary>
        public int File;
        /// <summary>
        /// 段
        /// </summary>
        public int Rank;

        /// <summary>
        /// 筋が0(無効な値)ならtrue
        /// </summary>
        public bool IsEmpty {
            get { return File == 0; }
        }
        /// <summary>
        /// 有効な値なのか否か
        /// </summary>
        public bool IsValid {
            get { return 1 <= File && File <= 9 && 1 <= Rank && Rank <= 9; }
        }

        /// <summary>
        /// 座標値へ変換
        /// </summary>
        /// <remarks>
        /// 9筋       ←  →       1筋
        /// 09 08 07 06 05 04 03 02 01 1段目
        /// 18 17 16 15 14 13 12 11 10
        /// 27 26 25 24 23 22 21 20 19
        /// 36 35 34 33 32 31 30 29 28  ↑
        /// 45 44 43 42 41 40 39 38 37
        /// 54 53 52 51 50 49 48 47 46  ↓
        /// 63 62 61 60 59 58 57 56 55
        /// 72 71 70 69 68 67 66 65 64
        /// 81 80 79 78 77 76 75 74 73 9段目
        /// </remarks>
        public byte ToByte {
            get { return (byte)(File + (Rank - 1) * 9); }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="file">筋</param>
        /// <param name="rank">段</param>
        public SquareData(int file, int rank) {
            File = file;
            Rank = rank;
        }

        /// <summary>
        /// 適当文字列化
        /// </summary>
        public override string ToString() {
            return File.ToString() + Rank.ToString();
        }

        /// <summary>
        /// 距離を加算
        /// </summary>
        public SquareData Add(int fileOffset, int rankOffset) {
            SquareData other = this;
            other.File += fileOffset;
            other.Rank += rankOffset;
            return other;
        }
    }
}
