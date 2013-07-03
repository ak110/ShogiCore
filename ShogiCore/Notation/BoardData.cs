using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 盤面を表すクラス
    /// </summary>
    [Serializable]
    public class BoardData : ICloneable {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 方向から筋の差分へ変換するテーブル
        /// </summary>
        public static readonly int[] DirectToFileOffset = new int[16 * 2] {
        //   0    1    2   3   4    5   6    7    8    9
        //  左下 下  右下 左  右  左上 上 右上 左上2 右上2
            +1,  +0,  -1, +1, -1, +1,  +0,  -1, +1, -1, -999, -999, -999, -999, -999, -999,
            -1,  -0,  +1, -1, +1, -1,  -0,  +1, -1, +1, +999, +999, +999, +999, +999, +999,
        };
        /// <summary>
        /// 方向から段の差分へ変換するテーブル
        /// </summary>
        public static readonly int[] DirectToRankOffset = new int[16 * 2] {
        //   0    1    2   3   4    5   6    7    8    9
        //  左下 下  右下 左  右  左上 上 右上 左上2 右上2
            +1, +1,  +1, +0, +0, -1, -1, -1, -2, -2, -999, -999, -999, -999, -999, -999,
            -1, -1,  -1, -0, -0, +1, +1, +1, +2, +2, +999, +999, +999, +999, +999, +999,
        };


        Piece[][] board = new Piece[9][] {
            new Piece[9], new Piece[9], new Piece[9],
            new Piece[9], new Piece[9], new Piece[9],
            new Piece[9], new Piece[9], new Piece[9],
        };
        int[][] hand = new int[2][] {
            new int[(byte)Piece.OU],
            new int[(byte)Piece.OU],
        };

        /// <summary>
        /// 先手番なら0、後手番なら1
        /// </summary>
        public int Turn { get; set; }

        /// <summary>
        /// インデクサ。[1, 1] ～ [9, 9]で指定。
        /// </summary>
        public Piece this[int file, int rank] {
            get { return board[file - 1][rank - 1]; }
            set { board[file - 1][rank - 1] = value; }
        }

        /// <summary>
        /// 指定位置の駒を取得
        /// </summary>
        public Piece this[SquareData sq] {
            get { return board[sq.File - 1][sq.Rank - 1]; }
            set { board[sq.File - 1][sq.Rank - 1] = value; }
        }

        /// <summary>
        /// 指定方向へ空じゃないマスを探す。見つからなければ0。
        /// </summary>
        public SquareData SearchNotEmpty(SquareData sq, int fileDiff, int rankDiff) {
            for (sq = sq.Add(fileDiff, rankDiff); sq.IsValid; sq = sq.Add(fileDiff, rankDiff)) {
                if (this[sq] != Piece.EMPTY) return sq;
            }
            return new SquareData();
        }

        /// <summary>
        /// 持駒の数
        /// </summary>
        /// <param name="turn">0なら先手の、1なら後手の。</param>
        /// <returns></returns>
        public int[] GetHand(int turn) {
            return hand[turn];
        }

        #region ICloneable メンバ

        object ICloneable.Clone() {
            return Clone();
        }

        #endregion

        public BoardData Clone() {
            BoardData copy = (BoardData)MemberwiseClone();
            copy.board = new Piece[9][] {
                (Piece[])board[0].Clone(), (Piece[])board[1].Clone(), (Piece[])board[2].Clone(),
                (Piece[])board[3].Clone(), (Piece[])board[4].Clone(), (Piece[])board[5].Clone(),
                (Piece[])board[6].Clone(), (Piece[])board[7].Clone(), (Piece[])board[8].Clone(),
            };
            copy.hand = new int[2][] {
                (int[])hand[0].Clone(),
                (int[])hand[1].Clone(),
            };
            return copy;
        }

        /// <summary>
        /// 空っぽにする
        /// </summary>
        public void Clear() {
            foreach (Piece[] t in board) {
                Array.Clear(t, 0, t.Length);
            }
            Array.Clear(hand[0], 0, hand[0].Length);
            Array.Clear(hand[1], 0, hand[1].Length);
        }

        /// <summary>
        /// 平手な状態にする。
        /// </summary>
        public void SetEquality() { // 英語変な気もする…
            Clear();
            for (int file = 1; file <= 9; file++) {
                this[file, 3] = Piece.EFU;
                this[file, 7] = Piece.FU;
            }
            this[1, 1] = this[9, 1] = Piece.EKY;
            this[1, 9] = this[9, 9] = Piece.KY;
            this[2, 1] = this[8, 1] = Piece.EKE;
            this[2, 9] = this[8, 9] = Piece.KE;
            this[3, 1] = this[7, 1] = Piece.EGI;
            this[3, 9] = this[7, 9] = Piece.GI;
            this[4, 1] = this[6, 1] = Piece.EKI;
            this[4, 9] = this[6, 9] = Piece.KI;
            this[2, 2] = Piece.EKA;
            this[8, 8] = Piece.KA;
            this[8, 2] = Piece.EHI;
            this[2, 8] = Piece.HI;
            this[5, 1] = Piece.EOU;
            this[5, 9] = Piece.OU;
            Turn = 0;
        }
        /// <summary>
        /// 平手なインスタンスを作成
        /// </summary>
        public static BoardData CreateEquality() { // 英語変な気もする…
            BoardData board = new BoardData();
            board.SetEquality();
            return board;
        }

        #region 駒落ち

        public void SetHandicapKY() {
            SetEquality();
            this[1, 1] = Piece.EMPTY; // 香
            Turn = 1;
        }

        public void SetHandicapKA() {
            SetEquality();
            this[2, 2] = Piece.EMPTY; // 角
            Turn = 1;
        }

        public void SetHandicapHI() {
            SetEquality();
            this[8, 2] = Piece.EMPTY; // 飛
            Turn = 1;
        }

        public void SetHandicapHIKY() {
            SetEquality();
            this[1, 1] = Piece.EMPTY; // 飛
            this[8, 2] = Piece.EMPTY; // 香
            Turn = 1;
        }

        public void SetHandicap2() {
            SetEquality();
            this[2, 2] = Piece.EMPTY; // 角
            this[8, 2] = Piece.EMPTY; // 飛
            Turn = 1;
        }

        public void SetHandicap4() {
            SetEquality();
            this[1, 1] = Piece.EMPTY; // 香
            this[9, 1] = Piece.EMPTY; // 香
            this[2, 2] = Piece.EMPTY; // 角
            this[8, 2] = Piece.EMPTY; // 飛
            Turn = 1;
        }

        public void SetHandicap6() {
            SetEquality();
            this[1, 1] = Piece.EMPTY; // 香
            this[2, 1] = Piece.EMPTY; // 桂
            this[8, 1] = Piece.EMPTY; // 桂
            this[9, 1] = Piece.EMPTY; // 香
            this[2, 2] = Piece.EMPTY; // 角
            this[8, 2] = Piece.EMPTY; // 飛
            Turn = 1;
        }

        public void SetHandicap8() {
            SetEquality();
            this[1, 1] = Piece.EMPTY; // 香
            this[2, 1] = Piece.EMPTY; // 桂
            this[3, 1] = Piece.EMPTY; // 銀
            this[7, 1] = Piece.EMPTY; // 銀
            this[8, 1] = Piece.EMPTY; // 桂
            this[9, 1] = Piece.EMPTY; // 香
            this[2, 2] = Piece.EMPTY; // 角
            this[8, 2] = Piece.EMPTY; // 飛
            Turn = 1;
        }

        public void SetHandicap10() {
            SetEquality();
            this[1, 1] = Piece.EMPTY; // 香
            this[2, 1] = Piece.EMPTY; // 桂
            this[3, 1] = Piece.EMPTY; // 銀
            this[4, 1] = Piece.EMPTY; // 金
            this[6, 1] = Piece.EMPTY; // 金
            this[7, 1] = Piece.EMPTY; // 銀
            this[8, 1] = Piece.EMPTY; // 桂
            this[9, 1] = Piece.EMPTY; // 香
            this[2, 2] = Piece.EMPTY; // 角
            this[8, 2] = Piece.EMPTY; // 飛
            Turn = 1;
        }

        public static BoardData CreateHandicapKY() {
            BoardData board = new BoardData();
            board.SetHandicapKY();
            return board;
        }

        public static BoardData CreateHandicapKA() {
            BoardData board = new BoardData();
            board.SetHandicapKA();
            return board;
        }

        public static BoardData CreateHandicapHI() {
            BoardData board = new BoardData();
            board.SetHandicapHI();
            return board;
        }

        public static BoardData CreateHandicapHIKY() {
            BoardData board = new BoardData();
            board.SetHandicapHIKY();
            return board;
        }

        public static BoardData CreateHandicap2() {
            BoardData board = new BoardData();
            board.SetHandicap2();
            return board;
        }

        public static BoardData CreateHandicap4() {
            BoardData board = new BoardData();
            board.SetHandicap4();
            return board;
        }

        public static BoardData CreateHandicap6() {
            BoardData board = new BoardData();
            board.SetHandicap6();
            return board;
        }

        public static BoardData CreateHandicap8() {
            BoardData board = new BoardData();
            board.SetHandicap8();
            return board;
        }

        public static BoardData CreateHandicap10() {
            BoardData board = new BoardData();
            board.SetHandicap10();
            return board;
        }

        #endregion

        /// <summary>
        /// 盤面、持ち駒にない駒を全て指定した方の持ち駒にする。
        /// </summary>
        public void SetHandAll(int turn) {
            int[] pieces = new int[(byte)Piece.OU + 1] { 0, 18, 4, 4, 4, 4, 2, 2, 0 }; // 全駒数
            // 手駒を除外
            for (Piece p = Piece.FU; p < Piece.OU; p++) {
                pieces[(byte)p] -= hand[turn ^ 1][(byte)p];
            }
            // 盤面の駒を除外
            for (int file = 1; file <= 9; file++) {
                for (int rank = 1; rank <= 9; rank++) {
                    pieces[(byte)(this[file, rank] & ~Piece.ENEMY & ~Piece.PROMOTED)]--;
                }
            }
            // 指定した方にセット
            for (Piece p = Piece.FU; p < Piece.OU; p++) {
                hand[turn][(byte)p] = pieces[(byte)p];
            }
        }

        /// <summary>
        /// MoveDataを適用
        /// </summary>
        /// <param name="move">指し手</param>
        public void Do(MoveData move) {
            if (move.IsEmpty || move.IsSpecialMove) {
                // 何もしない
            } else if (move.IsPut) {
                Piece p = move.PutPiece;
                if (hand[Turn][(byte)p] <= 0) {
                    logger.Warn("持っていない駒を打つ手: " + Environment.NewLine + DumpToString + move.ToString(this));
                }
                if (this[move.ToFile, move.ToRank] != Piece.EMPTY) {
                    logger.Warn("駒のあるマスへの駒打ち: " + Environment.NewLine + DumpToString + move.ToString(this));
                }
                hand[Turn][(byte)p]--;
                this[move.ToFile, move.ToRank] = Turn == 0 ? p : p | Piece.ENEMY;
            } else {
                Piece p = this[move.FromFile, move.FromRank];
                if (p == Piece.EMPTY) {
                    logger.Warn("動かす駒が存在しない: " + Environment.NewLine + DumpToString + move.ToString(this));
                }
                if (((p & Piece.ENEMY) == 0) != (Turn == 0)) {
                    logger.Warn("動かす駒の手番が不一致: " + Environment.NewLine + DumpToString + move.ToString(this));
                }

                Piece capture = this[move.ToFile, move.ToRank];
                if (capture != Piece.EMPTY) {
                    if (((capture & Piece.ENEMY) == 0) == (Turn == 0)) {
                        logger.Warn("取る駒の手番が不一致: " + Environment.NewLine + DumpToString + move.ToString(this));
                    }
                    hand[Turn][(byte)(capture & ~Piece.PE)]++;
                }

                this[move.FromFile, move.FromRank] = Piece.EMPTY;
                this[move.ToFile, move.ToRank] = move.IsPromote ? p | Piece.PROMOTED : p;
            }

            Turn ^= 1;
        }

        /// <summary>
        /// 指し手を戻す
        /// </summary>
        /// <param name="move">指し手</param>
        /// <param name="capture">取った駒。無いと戻せないので注意。</param>
        public void Undo(MoveData move, Piece capture) {
            Turn ^= 1;

            if (move.IsSpecialMove) {
                // 何もしない
            } else if (move.IsPut) {
                Piece p = move.PutPiece;
                hand[Turn][(byte)p]++;
                this[move.ToFile, move.ToRank] = Piece.EMPTY;
            } else {
                Piece p = this[move.ToFile, move.ToRank];
                this[move.FromFile, move.FromRank] = move.IsPromote ? p & ~Piece.PROMOTED : p;
                this[move.ToFile, move.ToRank] = capture;
            }
        }

        /// <summary>
        /// デバッグ用文字列化。デバッガで見やすいようにプロパティにしてみた。
        /// </summary>
        public string DumpToString {
            get {
                StringBuilder str = new StringBuilder();
                str.Append("後手の持駒：");
                AppendHands(str, 1);
                str.AppendLine();
                str.AppendLine("  ９ ８ ７ ６ ５ ４ ３ ２ １ ");
                str.AppendLine("+---------------------------+");
                for (int y = 0; y < 9; y++) {
                    str.Append("|");
                    //int toRank = (turn == 0 ? y + 1 : 9 - y) + Padding;
                    int rank = y + 1;
                    for (int x = 0; x < 9; x++) {
                        //int toFile = (turn == 0 ? 9 - x : x + 1) * 0x10;
                        int file = 9 - x;
                        str.Append(KifuNotationReader.NameTable[(byte)this[file, rank]]);
                    }
                    str.Append("|" + "一二三四五六七八九"[y].ToString());
                    str.AppendLine();
                }
                str.AppendLine("+---------------------------+");
                str.Append("先手の持駒：");
                AppendHands(str, 0);
                str.AppendLine();
                str.AppendLine(Turn == 0 ? "先手番" : "後手番");
                return str.ToString();
            }
        }

        /// <summary>
        /// 指し手部分の文字列化
        /// </summary>
        private void AppendHands(StringBuilder str, int turn) {
            bool hasAny = false;
            for (Piece p = Piece.HI; Piece.FU <= p; p--) {
                int h = hand[turn][(byte)p];
                if (0 < h) {
                    if (hasAny) str.Append("　"); // 2回目以降のみ
                    hasAny = true;
                    str.Append("　歩香桂銀金角飛"[(byte)p]);
                    if (1 < h) {
                        str.Append(NotationUtility.KanjiNumerals[h]);
                    }
                }
            }
            if (!hasAny) {
                str.Append("なし");
            }
        }

        /// <summary>
        /// 保存。(適当独自形式)
        /// </summary>
        public void Save(System.IO.BinaryWriter writer) {
            for (int file = 1; file <= 9; file++) {
                for (int rank = 1; rank <= 9; rank++) {
                    writer.Write((byte)this[file, rank]);
                }
            }
            for (int turn = 0; turn < 2; turn++) {
                for (Piece p = Piece.FU; p < Piece.OU; p++) {
                    writer.Write((byte)hand[turn][(byte)p]);
                }
            }
            writer.Write((byte)Turn);
        }

        /// <summary>
        /// 読み込み。(適当独自形式)
        /// </summary>
        public void Load(System.IO.BinaryReader reader) {
            for (int file = 1; file <= 9; file++) {
                for (int rank = 1; rank <= 9; rank++) {
                    this[file, rank] = (Piece)reader.ReadByte();
                }
            }
            for (int turn = 0; turn < 2; turn++) {
                for (Piece p = Piece.FU; p < Piece.OU; p++) {
                    hand[turn][(byte)p] = reader.ReadByte();
                }
            }
            Turn = reader.ReadByte();
        }
    }
}
