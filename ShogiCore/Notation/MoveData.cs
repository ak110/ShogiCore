using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.Notation {
    /// <summary>
    /// 指し手を表すクラス
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
    public struct MoveData : IEquatable<MoveData> {
        #region 特殊な指し手

        /// <summary>
        /// パス
        /// </summary>
        public static MoveData Pass { get { return new MoveData(0xff, (byte)Piece.EFU); } }
        /// <summary>
        /// 投了
        /// </summary>
        public static MoveData Resign { get { return new MoveData(0xff, (byte)Piece.EKY); } }
        /// <summary>
        /// 勝ち宣言
        /// </summary>
        public static MoveData Win { get { return new MoveData(0xff, (byte)Piece.EKE); } }
        /// <summary>
        /// 千日手の申告
        /// </summary>
        public static MoveData Endless { get { return new MoveData(0xff, (byte)Piece.EGI); } }
        /// <summary>
        /// 連続王手の千日手
        /// </summary>
        public static MoveData Perpetual { get { return new MoveData(0xff, (byte)Piece.EKI); } }

        #endregion

        /// <summary>
        /// 移動元、移動先
        /// </summary>
        public byte From, To;

        /// <summary>
        /// 初期化
        /// </summary>
        public MoveData(int from, int to) {
            Debug.Assert(byte.MinValue <= from && from <= byte.MaxValue);
            Debug.Assert(byte.MinValue <= to && to <= byte.MaxValue);
            From = (byte)from;
            To = (byte)to;
            Debug.Assert(IsPut || (1 <= FromFile && FromFile <= 9));
            Debug.Assert(IsPut || (1 <= FromRank && FromRank <= 9));
            Debug.Assert(1 <= ToFile && ToFile <= 9);
            Debug.Assert(1 <= ToRank && ToRank <= 9);
        }

        public bool IsEmpty { get { return From == 0 && To == 0; } }
        public bool IsSpecialMove { get { return From == 0xff; } }

        public int FromFile {
            get {
                Debug.Assert(!IsPut);
                return (From - 1) % 9 + 1;
            }
        }
        public int FromRank {
            get {
                Debug.Assert(!IsPut);
                return (From + 8) / 9;
            }
        }
        public int ToFile {
            get { return ((IsPromote ? To - 100 : To) - 1) % 9 + 1; }
        }
        public int ToRank {
            get { return ((IsPromote ? To - 100 : To) + 8) / 9; }
        }

        /// <summary>
        /// 成るならtrue
        /// </summary>
        public bool IsPromote {
            get { return 100 <= To; }
        }

        /// <summary>
        /// 成る手ならPiece.PROMOTED、それ以外ならPiece.EMPTYを返す。
        /// </summary>
        public Piece PromoteMask {
            get { return IsPromote ? Piece.PROMOTED : Piece.EMPTY; }
        }

        /// <summary>
        /// 駒を打つ手ならtrue
        /// </summary>
        public bool IsPut {
            get { return 100 <= From; }
        }
        /// <summary>
        /// 打つ駒の種類
        /// </summary>
        public Piece PutPiece {
            get { return (Piece)(From - 100); }
        }

        /// <summary>
        /// デバッグとか用適当文字列化
        /// </summary>
        public override string ToString() {
            if (IsPut) {
                return string.Format("{0}{1}{2}打", ToFile, ToRank,
                    KifuNotationWriter.NameTable[(byte)PutPiece]);
            } else {
                return string.Format("{0}{1} {2}({3}{4})",
                    ToFile, ToRank, IsPromote ? "成" : "",
                    FromFile, FromRank);
            }
        }

        /// <summary>
        /// 少しちゃんと文字列化
        /// </summary>
        public string ToString(BoardData boardData) {
            if (IsPut) {
                return ToString(boardData.Turn, Piece.EMPTY);
            } else {
                return ToString(boardData.Turn, boardData[FromFile, FromRank]);
            }
        }
        /// <summary>
        /// 少しちゃんと文字列化
        /// </summary>
        /// <param name="turn">手番</param>
        /// <param name="p">動かす駒。打つ手の時は使用しない。</param>
        /// <param name="lastMoveFile">１つ前の指し手の移動先の筋</param>
        /// <param name="lastMoveRank">１つ前の指し手の移動先の段</param>
        public string ToString(int turn, Piece p, int lastMoveFile = -1, int lastMoveRank = -1) {
            string turnPrefix = "▲△"[turn].ToString();
            if (this == MoveData.Resign) return turnPrefix + "投了";
            if (this == MoveData.Pass) return turnPrefix + "パス";
            if (this == MoveData.Win) return turnPrefix + "入玉勝ち";
            if (this == MoveData.Endless) return turnPrefix + "千日手";
            if (this == MoveData.Perpetual) return turnPrefix + "連続王手";
            if (IsPut) {
                return string.Format("{3}{0}{1}{2}打",
                    ToFile, ToRank, KifuNotationWriter.NameTable[(byte)PutPiece],
                    turnPrefix);
            } else {
                int ToFile = this.ToFile;
                int ToRank = this.ToRank;
                string toString = lastMoveFile == ToFile && lastMoveRank == ToRank ? "同" :
                    ToFile.ToString() + ToRank.ToString();
                return string.Format("{0}{1}{2}{3}({4}{5})",
                    turnPrefix, toString,
                    KifuNotationWriter.NameTable[(byte)(p & ~Piece.ENEMY)],
                    IsPromote ? "成" : "", FromFile, FromRank);
            }
        }

        public override bool Equals(object obj) {
            return Equals((MoveData)obj);
        }

        public override int GetHashCode() {
            return (From << 8) | To;
        }

        #region IEquatable<MoveData> メンバ

        public bool Equals(MoveData other) {
            return GetHashCode() == other.GetHashCode();
        }

        #endregion

        public static bool operator ==(MoveData x, MoveData y) { return x.Equals(y); }
        public static bool operator !=(MoveData x, MoveData y) { return !x.Equals(y); }
    }

    /// <summary>
    /// 棋譜の指し手
    /// </summary>
    public struct MoveDataEx {
        /// <summary>
        /// 指し手
        /// </summary>
        public MoveData MoveData;
        /// <summary>
        /// コメント
        /// </summary>
        public string Comment;
        /// <summary>
        /// 評価値
        /// </summary>
        public int? Value;
        /// <summary>
        /// 思考時間[ms]。負なら思考時間未記録とする。
        /// </summary>
        public int Time;

        /// <summary>
        /// 初期化
        /// </summary>
        public MoveDataEx(MoveData moveData, string comment = null, int? value = null, int? time = null) {
            MoveData = moveData;
            Comment = comment;
            Value = value;
            Time = -1;
        }
    }
}
