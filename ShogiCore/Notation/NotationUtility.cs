using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋譜の読み込みでよく使う処理とかを適当に。
    /// </summary>
    public static class NotationUtility {
        /// <summary>
        /// 漢数字テーブル
        /// </summary>
        public static readonly string[] KanjiNumerals = {
            "零","一","二","三","四","五","六","七","八","九",
            "十","十一","十二","十三","十四","十五","十六","十七","十八","十九",
            "二十","二十一","二十二","二十三","二十四","二十五","二十六","二十七","二十八","二十九",
        };

        /// <summary>
        /// 漢数字・全角数字・半角数字を1文字読む。それら以外なら-1。
        /// </summary>
        public static int ReadNumber(char c) {
            return "0123456789０１２３４５６７８９零一二三四五六七八九".IndexOf(c) % 10;
            // -1 % 10 == -1
        }

        /// <summary>
        /// テスト用棋譜(序盤)
        /// </summary>
        public const string TestNotationO = @"
後手の持駒：なし
  ９ ８ ７ ６ ５ ４ ３ ２ １
+---------------------------+
|v香v桂 ・ ・ ・v金v銀v桂v香|一
| ・v飛 ・ ・v金 ・ ・v玉 ・|二
|v歩 ・v歩v歩v銀v歩v角v歩v歩|三
| ・v歩 ・ ・v歩 ・v歩 ・ ・|四
| ・ ・ ・ ・ ・ ・ ・ ・ ・|五
| ・ ・ 歩 歩 ・ ・ ・ ・ ・|六
| 歩 歩 ・ 銀 歩 歩 歩 歩 歩|七
| ・ 角 ・ 飛 ・ ・ ・ 銀 香|八
| 香 桂 ・ 金 ・ 金 ・ 桂 玉|九
+---------------------------+
先手の持駒：なし
後手番
";
        /// <summary>
        /// テスト用棋譜(終盤)
        /// </summary>
        public const string TestNotationE = @"
後手の持駒：飛　角　金　
  ９ ８ ７ ６ ５ ４ ３ ２ １
+---------------------------+
| ・v桂 ・ ・ ・ ・ ・v桂v玉|一
| ・ ・ ・ ・ ・ ・ ・v角v香|二
|v歩 ・ ・ と ・v歩 ・ ・ ・|三
| ・ ・v歩 ・v歩 ・v歩 ・v歩|四
| ・ ・ ・ ・ ・ ・ ・ 銀 ・|五
| ・ ・ 歩 ・ 歩 ・ 歩 ・ ・|六
| 歩 ・ 桂 ・ ・ ・ ・v歩 歩|七
| ・ ・ ・ ・ ・ ・v銀 銀 香|八
| ・ ・ ・ ・ ・ ・ ・ 桂 玉|九
+---------------------------+
先手の持駒：飛　金三　銀　香二　歩五　
";
        /// <summary>
        /// テスト用棋譜(詰め将棋)
        /// </summary>
        public const string TestNotationMate = @"
作品番号：長手数015
作品名：寿
作者：伊藤看寿
発表年月：宝暦5年
出典：将棋図巧
手数：611
完全性：完全扱い
手合割：平手　　
後手の持駒：桂三　
  ９ ８ ７ ６ ５ ４ ３ ２ １
+---------------------------+
|v金 ・ ・ ・v歩 ・ ・ ・v龍|一
| ・ ・ ・v香 歩 金 ・ ・ ・|二
| ・v銀 ・ ・ ・ ・v歩v歩v歩|三
| ・v香 ・ 角 ・ ・ ・ ・ 歩|四
| 香 ・ ・v角 ・v玉 歩v香 ・|五
| ・ ・ ・vと ・ ・ ・ 龍vと|六
| ・ ・ ・vと ・ ・v全 ・ 銀|七
| 圭 歩vと ・v金 銀 ・ ・ ・|八
| ・v金vと ・ ・ 歩 ・ ・ ・|九
+---------------------------+
先手の持駒：歩四　
先手：伊藤看寿 「寿」　６１１手
後手：将棋図巧　宝暦５年
";

        /// <summary>
        /// テスト用棋譜の読み込み(序盤)
        /// </summary>
        public static Notation LoadTestNotationO() {
            return new NotationLoader().Load(TestNotationO).First();
        }

        /// <summary>
        /// テスト用棋譜の読み込み(終盤)
        /// </summary>
        public static Notation LoadTestNotationE() {
            return new NotationLoader().Load(TestNotationE).First();
        }

        /// <summary>
        /// テスト用棋譜の読み込み(詰め将棋)
        /// </summary>
        public static Notation LoadTestNotationMate() {
            return new NotationLoader().Load(TestNotationMate).First();
        }
    }
}
