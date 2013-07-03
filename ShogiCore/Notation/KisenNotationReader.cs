using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋泉棋譜形式
    /// </summary>
    public class KisenNotationReader : IBinaryNotationReader {
        #region IBinaryNotationReader メンバ

        public bool CanRead(byte[] data) {
            return data != null && data.Length % 512 == 0;
        }

        public IEnumerable<Notation> Read(byte[] data) {
            if (!CanRead(data)) {
                throw new NotationFormatException("棋泉棋譜データの読み込みに失敗しました");
            }

            for (int i = 0; i < data.Length; i += 512) {
                Notation notation = LoadGame(data, i);
                if (notation == null) continue;
                yield return notation;
            }
        }

        #endregion

        /// <summary>
        /// 1ゲーム分の読み込み
        /// </summary>
        private unsafe Notation LoadGame(byte[] data, int i) {
            List<MoveDataEx> moves = new List<MoveDataEx>();

            Piece* board = stackalloc Piece[82];
            for (int j = 55; j <= 63; j++) board[j] = Piece.FU;
            for (int j = 19; j <= 27; j++) board[j] = Piece.EFU;
            board[73] = board[81] = Piece.KY;
            board[01] = board[9] = Piece.EKY;
            board[74] = board[80] = Piece.KE;
            board[02] = board[8] = Piece.EKE;
            board[75] = board[79] = Piece.GI;
            board[03] = board[07] = Piece.EGI;
            board[76] = board[78] = Piece.KI;
            board[04] = board[06] = Piece.EKI;
            board[71] = Piece.KA;
            board[11] = Piece.EKA;
            board[65] = Piece.HI;
            board[17] = Piece.EHI;
            board[77] = Piece.OU;
            board[05] = Piece.EOU;
            int** hand = stackalloc int*[2];
            int* hand0 = stackalloc int[10];
            int* hand1 = stackalloc int[10];
            for (int j = 0; j < 10; j++) { hand0[j] = 0; hand1[j] = 0; }
            hand[0] = hand0;
            hand[1] = hand1;

            for (int j = 0; j < 512; j += 2) {
                int to = data[i + j];
                if (to == 0x00 || to == 0xff) break;
                int from = data[i + j + 1];
                if (from == 0x00 || from == 0xff) break; // 念のため。
                // ※駒落ちなら1手目が0xffffらしい。IDXファイルも見ないとイカンので無視る。

                int turn = moves.Count % 2;

                if (100 < from) {
                    if (100 < to) break; // 裏返しに打っちゃった反則負け？
                    // 持ち駒を使う手
                    // 飛車、角、金、銀、桂、香、歩の枚数？
                    int k = 1;
                    for (int sum = hand[turn][k]; ; sum += hand[turn][++k]) {
                        if (from - 100 <= sum) break;
                    }
                    // kが1:飛車 2:角 3:金 4:銀 5:桂 6:香 7:歩
                    from = 100 + (8 - k);
                    board[to] = (Piece)(8 - k);
                    hand[turn][k]--;
                } else {
                    int tt = 100 < to ? to - 100 : to;
                    hand[turn][8 - (byte)(board[tt] & ~Piece.ENEMY)]++;
                    board[tt] = board[from]; // 成るかどうかはここでは無視っちゃう。持ち駒の為の処理なので。
                    board[from] = Piece.EMPTY;
                }

                moves.Add(new MoveDataEx(new MoveData(from, to)));
            }

            if (moves.Count <= 0) {
                return null;
            }
            Notation notation = new Notation();
            notation.Moves = moves.ToArray();
            //notation.Winner = (moves.Count % 2) ^ 1; // 最後の手を指した方が勝者とする。(反則とかもあるのだが…)
            notation.Winner = -1;
            return notation;
        }

#if false

564 名無し名人 : 02/11/12 14:55 ID:t7xodq8d
    -NUM- -R.DATE- -R.TIME- -SENDER- -CONTENTS-
    00337 95-12-23 22:09:31 SHOP0008 棋泉6.0データ形式

    棋泉（Version6.0）データ形式を公開いたします。5.6x形式からは上位互換性が
    あります。

    ----------------------------
    棋泉（Version6.0）データ形式
    ----------------------------

    棋泉データは拡張子"kif"と"idx"の２つのデータファイルからなっている：

    １．ＫＩＦファイル
    　　◎機能：棋譜データ（手順）を格納する。
    　　◎１レコード（１局分のデータ）＝512バイトのランダムアクセスファイル
    　　◎１手のデータは２バイトから成っており、１手目から指し手の順に格納
    　　　されている。最大２５６手までの棋譜が登録できる。
    　　　１手のデータは「元位置」、「移動先」からなる。
    　　　　　 F E D C B A 9 8 7 6 5 4 3 2 1 0 (bit)
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    　　　　　　<------ 元位置 -------> <------ 移動先 ------->


565 名無し名人 : 02/11/12 14:56 ID:t7xodq8d

    　（１）元位置データ
    　　　①　１～　８１の場合：盤上の駒の位置を意味する。
    　　　　　　　　　　　　　　盤の位置の対応は以下のとおり。
    　　　　　　 　９ 　８　 ７　 ６　 ５　 ４　 ３　 ２　 １
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 9 ・ 8 ・ 7 ・ 6 ・ 5 ・ 4 ・ 3 ・ 2 ・ 1 ・ 一
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 18 ・ 17 ・ 16 ・ 15 ・ 14 ・ 13 ・ 12 ・ 11 ・ 10 ・ 二
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 27 ・ 26 ・ 25 ・ 24 ・ 23 ・ 22 ・ 21 ・ 20 ・ 19 ・ 三
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 36 ・ 35 ・ 34 ・ 33 ・ 32 ・ 31 ・ 30 ・ 29 ・ 28 ・ 四
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 45 ・ 44 ・ 43 ・ 42 ・ 41 ・ 40 ・ 39 ・ 38 ・ 37 ・ 五
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・


566 名無し名人 : 02/11/12 14:57 ID:t7xodq8d
    ・ 54 ・ 53 ・ 52 ・ 51 ・ 50 ・ 49 ・ 48 ・ 47 ・ 46 ・ 六
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 63 ・ 62 ・ 61 ・ 60 ・ 59 ・ 58 ・ 57 ・ 56 ・ 55 ・ 七
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 72 ・ 71 ・ 70 ・ 69 ・ 68 ・ 67 ・ 66 ・ 65 ・ 64 ・ 八
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ 81 ・ 80 ・ 79 ・ 78 ・ 77 ・ 76 ・ 75 ・ 74 ・ 73 ・ 九
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・


    　　　②１０１～１３４の場合：持ち駒を使用したことを意味する
    　　　　１から３４までの持ち駒リストの位置を示す。持ち駒リストの中
    　　　　では駒は飛、角、金、銀、桂、香、歩の順でソートされている。
    　　　　例えば、持ち駒リストに
    　　　　　　角金金桂香歩歩歩
    　　　　とあった場合１０４は４番目の持ち駒：つまり桂を意味する。


567 名無し名人 : 02/11/12 14:57 ID:t7xodq8d
    　（２）移動先データ
    　　　①　　１～　８１の場合：移動先の盤上の位置を意味する。
    　　　　　　　　　　　　　　　盤の位置の対応は前述のとおり。

    　　　②１０１～１８１の場合：移動先で駒が「成った」ことを意味する。
    　　　　　　　　　　　　　　　位置は-100した１～８１を意味する。


568 名無し名人 : 02/11/12 14:58 ID:t7xodq8d
    ２．ＩＤＸファイル
    　　◎機能：棋士名・対局日・棋戦名などの管理情報を格納する。
    　　 １レコード＝32バイト（16ワード）のランダムアクセスファイル

    　　　第１レコードの先頭４バイト（Intel形式:Little Endian）には登録されて
    　　　いる棋譜数が格納されている。

    　　　　　　0 1　2　3　4　5　6 7　8　9 10 11 12 13 14 15 (Word)
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    　　　　　 <---> <--------------- 未使用 ---------------->
    |
    +-----棋譜数(4byte)


569 名無し名人 : 02/11/12 14:58 ID:t7xodq8d
    　　　第２レコード以降のレコードは下図のようにワード単位で使用されて
    　　　いる：
    　　　　　　0 1　2　3　4　5　6 7　8　9 10 11 12 13 14 15 (Word)
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・
    ・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・
    　　　　　　 |　| |　| |　| |　| |　| | <---未使用--->
    | | | | | | | | | | |
    | | | | | | | | | | |
    | | | | | | | | | | +----手合い割りコード
    | | | | | | | | | +---上位バイト：先手戦型番号
    | | | | | | | | | 下位バイト：後手戦型番号
    | | | | | | | | +--- 終局コード
    | | | | | | | +--- 戦型番号
    | | | | | | +------ 日
    | | | | | +--------- 月
    | | | | +------------ 年
    | | | +--------------- 局
    | | +------------------ 棋戦名番号
    | +--------------------- 後手棋士番号
    +------------------------ 先手棋士番号


    　　　第２レコードから各棋譜の情報が入っている。
    　　　棋譜番号nの情報は第(n+1)レコードに格納されている。
    　　　従って、KIFﾌｧｲﾙの第nレコードには、IDXﾌｧｲﾙの第(n+1)レコード
    　　　が対応している。


570 名無し名人 : 02/11/12 14:58 ID:t7xodq8d
    　　◎終局コード
    　　　0:手数の偶奇（パリティ）で勝負を決める（Ver4.1形式）
    　　　1:まで先手勝ち
    　　　2:まで後手勝ち
    　　　3:まで千日手
    　　　4:まで持将棋
    　　　5:以下先手勝ち（256手を越える棋譜に使用）
    　　　6:以下後手勝ち（ 　〃　　　　　〃　　　）
    　　　7:以下千日手　（ 　〃　　　　　〃　　　）
    　　　8:以下持将棋　（ 　〃　　　　　〃　　　）

    　　◎手合い割りコード
    　　　0:平手
    　　　1:香落ち
    　　　2:角落ち
    　　　3:飛車落ち
    　　　4:飛香落ち
    　　　5:２枚落ち
    　　　6:４枚落ち
    　　　7:６枚落ち

    　　◎駒落ち棋譜の１手目のデータは0xffffとし使用しない。
    　　　（上手の第１手は２手目として記録する。）

    -･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-･-


571 名無し名人 : 02/11/12 14:59 ID:t7xodq8d
    　　棋泉5.6x形式との相違点
    　　　(1) 棋譜数：2byte（65535局）→ 4byte（2^31-1局）に拡張
    　　　　　昔の棋譜データでは第２ワード（3,4byte目）にゴミが入っている
    　　　　　可能性があるので当面（60000局を越えるまで）それは強制的にゼ
    　　　　　ロにすること。

    (2) 駒落ち棋譜対応

    Dec.23,1995SHOP0008 かかし

    -NUM- -R.DATE- -R.TIME- -SENDER- -CONTENTS-


572 名無し名人 : 02/11/12 15:00 ID:t7xodq8d
    と、以上です。
    7年前ですがネット駒音で作者が公開されていました。

#endif
        /// <summary>
        /// 王が4段目以上に進入した棋譜なのかどうかを調べる。
        /// </summary>
        public static unsafe bool IsNyuugyokuNotation(byte[] data, int i) {
            Piece* board = stackalloc Piece[82];
            for (int j = 55; j <= 63; j++) board[j] = Piece.FU;
            for (int j = 19; j <= 27; j++) board[j] = Piece.EFU;
            board[73] = board[81] = Piece.KY;
            board[01] = board[9] = Piece.EKY;
            board[74] = board[80] = Piece.KE;
            board[02] = board[8] = Piece.EKE;
            board[75] = board[79] = Piece.GI;
            board[03] = board[07] = Piece.EGI;
            board[76] = board[78] = Piece.KI;
            board[04] = board[06] = Piece.EKI;
            board[71] = Piece.KA;
            board[11] = Piece.EKA;
            board[65] = Piece.HI;
            board[17] = Piece.EHI;
            board[77] = Piece.OU;
            board[05] = Piece.EOU;
            int** hand = stackalloc int*[2];
            int* hand0 = stackalloc int[10];
            int* hand1 = stackalloc int[10];
            for (int j = 0; j < 10; j++) { hand0[j] = 0; hand1[j] = 0; }
            hand[0] = hand0;
            hand[1] = hand1;

            for (int j = 0, turn = 0; j < 512; j += 2, turn ^= 1) {
                int to = data[i + j];
                if (to == 0x00 || to == 0xff) break;
                int from = data[i + j + 1];
                if (from == 0x00 || from == 0xff) break; // 念のため。
                // ※駒落ちなら1手目が0xffffらしい。IDXファイルも見ないとイカンので無視る。

                if (100 < from) {
                    if (100 < to) break; // 裏返しに打っちゃった反則負け？
                    // 持ち駒を使う手
                    // 飛車、角、金、銀、桂、香、歩の枚数？
                    int k = 1;
                    for (int sum = hand[turn][k]; ; sum += hand[turn][++k]) {
                        if (from - 100 <= sum) break;
                    }
                    // kが1:飛車 2:角 3:金 4:銀 5:桂 6:香 7:歩
                    board[to] = (Piece)(8 - k);
                    hand[turn][k]--;
                } else {
                    int tt = 100 < to ? to - 100 : to;
                    hand[turn][8 - (byte)(board[tt] & ~Piece.ENEMY)]++;
                    board[tt] = board[from]; // 成るかどうかはここでは無視っちゃう。持ち駒の為の処理なので。
                    board[from] = Piece.EMPTY;

                    // 進入した？
                    if ((board[tt] == Piece.OU && tt <= 36) ||
                        (board[tt] == Piece.EOU && 46 <= tt)) {
                        return true;
                    }
                }
            }

            // 進入しなかった
            return false;
        }
    }
}
