using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore {
    /// <summary>
    /// 学習の初期値とかに使う為のBonanzaの定数など。
    /// </summary>
    public static class BonanzaConstants {
        /// <summary>
        /// 論文の交換値
        /// </summary>
        public static readonly short[] Exchange1 = new short[16] {
            //  歩   香   桂   銀   金   角   飛   王   と   杏   圭   全 金   馬   龍
            0, 106, 279, 304, 428, 527, 617, 700, 888, 272, 323, 363, 415, 0, 698, 854,
        };

        /// <summary>
        /// 論文の持ち駒評価
        /// </summary>
        public static readonly short[][] Hand1 = new short[8][] {
            null, // 空
            new short[19] { 0, 27, 33, 21, 6, -8, -17, -23, -29, -35, -41, -47, -53, -59, -65, -71, -77, -83, -89 }, // 歩
            new short[5] { 0, 28, 39, 51, 63 }, // 香
            new short[5] { 0, 22, 12, -15, -48 }, // 桂
            new short[5] { 0, 37, 28, -2, -51 }, // 銀
            new short[5] { 0, 31, 21, -4, -39 }, // 金
            new short[3] { 0, 28, 9 }, // 角
            new short[3] { 0, 59, 45 }, // 飛
        };

        /// <summary>
        /// 龍の交換値/2
        /// </summary>
        public const int Exchange4Per2OfRY = 831;
        /// <summary>
        /// ソースの交換値 / 2
        /// </summary>
        public static readonly short[] Exchange4Per2 = new short[16] {
            //  歩   香   桂   銀   金   角   飛   王   と   杏   圭   全 金   馬   龍
            0, 101, 254, 267, 385, 466, 567, 661, 555, 302, 339, 364, 431, 0, 699, 831,
        };
    }
}
