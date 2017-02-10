using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ShogiCore.Notation;

namespace ShogiCore {
    /// <summary>
    /// 手。
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{GetDebugString()}")]
    //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Move : IEquatable<Move> {
#if DEBUG
        /// <summary>
        /// Assertを行うならtrue
        /// </summary>
        public static bool AssertMembers = true;
#endif

        #region 特殊状態。…適当実装。

        /// <summary>
        /// パス
        /// </summary>
        public static Move Pass { get { return new Move(0, 0xff, Piece.EFU, Piece.EMPTY); } }
        /// <summary>
        /// 投了
        /// </summary>
        public static Move Resign { get { return new Move(0, 0xff, Piece.EKY, Piece.EMPTY); } }
        /// <summary>
        /// 勝ち宣言
        /// </summary>
        public static Move Win { get { return new Move(0, 0xff, Piece.EKE, Piece.EMPTY); } }
        /// <summary>
        /// 千日手の申告
        /// </summary>
        public static Move Endless { get { return new Move(0, 0xff, Piece.EGI, Piece.EMPTY); } }
        /// <summary>
        /// 連続王手の千日手
        /// </summary>
        public static Move Perpetual { get { return new Move(0, 0xff, Piece.EKI, Piece.EMPTY); } }

        /// <summary>
        /// 置換表1 (デバッグ用)
        /// </summary>
        public static Move Trans1 { get { return new Move(0, 0xff, Piece.EKA, Piece.EMPTY); } }
        /// <summary>
        /// 置換表2 (デバッグ用)
        /// </summary>
        public static Move Trans2 { get { return new Move(0, 0xff, Piece.EHI, Piece.EMPTY); } }

        #endregion

        #region 定数

        /// <summary>
        /// Fromのシフト量
        /// </summary>
        public const int ShiftFrom = 0;
        /// <summary>
        /// Toのシフト量
        /// </summary>
        public const int ShiftTo = 8;
        /// <summary>
        /// Promoteのシフト量
        /// </summary>
        public const int ShiftPromote = 16;
        /// <summary>
        /// Captureのシフト量
        /// </summary>
        public const int ShiftCapture = 24;
        /// <summary>
        /// Fromのマスク値
        /// </summary>
        public const uint MaskFrom = 0x000000ff;
        /// <summary>
        /// Toのマスク値
        /// </summary>
        public const uint MaskTo = 0x0000ff00;
        /// <summary>
        /// Promoteのマスク値
        /// </summary>
        public const uint MaskPromote = 0x00ff0000;
        /// <summary>
        /// Captureのマスク値
        /// </summary>
        public const uint MaskCapture = 0xff000000;

        /// <summary>
        /// MaskFrom | MaskTo
        /// </summary>
        public const uint MaskFromTo = MaskFrom | MaskTo;
        /// <summary>
        /// MaskFrom | MaskTo | MaskPromote
        /// </summary>
        public const uint MaskFromToPromote = MaskFrom | MaskTo | MaskPromote;

        #endregion

        /// <summary>
        /// 値。
        /// </summary>
        public uint Value;

        /// <summary>
        /// 移動元。
        /// KomaInf.OU以下なら打ち駒。それ以外はBoard.Paddingを含んだBoard.boardのindex。
        /// </summary>
        public byte From {
            get { unchecked { return (byte)((Value >> ShiftFrom) & 0xff); } }
            set { unchecked { Value = Value & ~MaskFrom | value; } }
        }
        /// <summary>
        /// 移動先。
        /// Board.Paddingを含んだBoard.boardのindex。
        /// </summary>
        public byte To {
            get { unchecked { return (byte)((Value >> ShiftTo) & 0xff); } }
            set { unchecked { Value = Value & ~MaskTo | ((uint)value << ShiftTo); } }
        }
        /// <summary>
        /// 成るときはPiece.PROMOTED、それ以外はPiece.EMPTY
        /// </summary>
        public Piece Promote {
            get { unchecked { return (Piece)((Value >> ShiftPromote) & 0xff); } }
            set { unchecked { Value = Value & ~MaskPromote | ((uint)value << ShiftPromote); } }
        }
        /// <summary>
        /// 取る駒。
        /// </summary>
        public Piece Capture {
            get { unchecked { return (Piece)((Value >> ShiftCapture) & 0xff); } }
            set { unchecked { Value = Value & ~MaskCapture | ((uint)value << ShiftCapture); } }
        }

        /// <summary>
        /// 成る手ならtrue
        /// </summary>
        public bool IsPromote {
            //get { return Promote != Piece.EMPTY; }
            get { return (Value & MaskPromote) != 0; }
        }

        /// <summary>
        /// 取る手ならtrue
        /// </summary>
        public bool IsCapture {
            //get { return Capture != Piece.EMPTY; }
            get { return (Value & MaskCapture) != 0; }
        }

        /// <summary>
        /// 取る手か成る手ならtrue
        /// </summary>
        public bool IsCaptureOrPromote {
            get { return (Value & (MaskPromote | MaskCapture)) != 0; }
        }

        /// <summary>
        /// 空ならtrue
        /// </summary>
        public bool IsEmpty {
            //get { return From == 0 && To == 0; }
            get { return (Value & MaskFromTo) == 0; }
        }

        /// <summary>
        /// 特殊状態のどれかならtrue(投了とか)
        /// </summary>
        public bool IsSpecialState {
            get {
                if ((Value & MaskFromTo) == (0xff << ShiftTo)) {
                    Debug.Assert(
                        this == Pass || this == Resign || this == Win ||
                        this == Endless || this == Perpetual);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 駒を打つ手ならtrue
        /// </summary>
        public bool IsPut {
            get { return (byte)((Value >> ShiftFrom) & 0xff) < (byte)Piece.OU; }
        }
        /// <summary>
        /// 打つ駒の種類
        /// </summary>
        public Piece PutPiece {
            get { return (Piece)(byte)((Value >> ShiftFrom) & 0xff); }
        }

        /// <summary>
        /// 歩を打つ手ならtrue
        /// </summary>
        public bool IsPutFU {
            get {
                Debug.Assert(PutPiece == Piece.FU ? IsPut : true, "PutPiece == FUならIsPutのはず。");
                return PutPiece == Piece.FU;
            }
        }

        /// <summary>
        /// 移動先。パスとかの場合は0。
        /// </summary>
        public int RealTo {
            get { return IsSpecialState ? 0 : (int)To; }
        }

        /// <summary>
        /// 成る手から成らない手を生成
        /// </summary>
        public Move ToNotPromote {
            get { return new Move(Value & ~MaskPromote); }
        }

        /// <summary>
        /// From,To(Promote以外)が一致してたらtrue
        /// </summary>
        public bool EqualsFromTo(Move other) {
            return ((Value ^ other.Value) & MaskFromTo) == 0;
        }

        /// <summary>
        /// otherが成らない手で、thisがそれと同じ移動元・移動先で成る手な場合true。(ただし、判定高速化のため、this == otherの場合にもtrueとする)
        /// </summary>
        /// <remarks>
        /// オーダリングとかで成らない手より成る手を優先する場合に使う。
        /// </remarks>
        public bool IsPromoteMoveOf(Move other) {
            return Value == (other.Value | ((byte)Piece.PROMOTED << ShiftPromote));
        }

        #region インスタンス生成ヘルパー

        /// <summary>
        /// 駒を打つ手を生成
        /// </summary>
        public static Move CreatePut(Piece piece, int to) {
            //Debug.Assert(Piece.EMPTY == 0);
            return new Move((uint)((byte)piece << ShiftFrom | (to << ShiftTo)));
        }
        /// <summary>
        /// 駒を移動する手を生成
        /// </summary>
        public static Move CreateMove(Board board, int from, int to, Piece promote) {
            return new Move((uint)((from << ShiftFrom) | (to << ShiftTo) | ((byte)promote << ShiftPromote) | ((byte)board[to] << ShiftCapture)));
        }
        /// <summary>
        /// 駒を移動する手を生成
        /// </summary>
        public static Move CreateMove(Piece[] board, int from, int to, Piece promote) {
            return new Move((uint)((from << ShiftFrom) | (to << ShiftTo) | ((byte)promote << ShiftPromote) | ((byte)board[to] << ShiftCapture)));
        }
        /// <summary>
        /// 駒を移動する手を生成
        /// </summary>
        public static Move CreateMove(Piece* board, int from, int to, Piece promote) {
            return new Move((uint)((from << ShiftFrom) | (to << ShiftTo) | ((byte)promote << ShiftPromote) | ((byte)board[to] << ShiftCapture)));
        }

        #endregion

        /// <summary>
        /// 初期化
        /// </summary>
        public Move(uint value) {
            Value = value;
            DebugAssertMembers();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public Move(int from, int to, Piece promote, Piece capture) {
            Value = (uint)((from << ShiftFrom) | (to << ShiftTo) | ((byte)promote << ShiftPromote) | ((byte)capture << ShiftCapture));
            DebugAssertMembers();
        }

        /// <summary>
        /// assert。
        /// </summary>
        [Conditional("DEBUG")]
        private void DebugAssertMembers() {
#if DEBUG
            if (!AssertMembers) return;
#endif
            //if (!IsSpecialState) {
            if (!(From == 0 && To == 0xff)) {
                if (IsPut) {
                    Debug.Assert(Promote == Piece.EMPTY);
                    Debug.Assert(Capture == Piece.EMPTY);
                }
                if (Promote != Piece.EMPTY) {
                    Debug.Assert(Promote == Piece.PROMOTED, "Move.Promoteが不正");
                }
            }
            Debug.Assert(Capture != Piece.ENEMY, "Move.Captureが不正");
            Debug.Assert(Capture != Piece.WALL, "Move.Captureが不正");
            Debug.Assert(Capture != Piece.OU, "王を取っちゃう手");
            Debug.Assert(Capture != Piece.EOU, "王を取っちゃう手");
        }

        /// <summary>
        /// override
        /// </summary>
        public override bool Equals(object obj) {
            return Equals((Move)obj);
        }
        /// <summary>
        /// override
        /// </summary>
        public override int GetHashCode() {
            return HashCode;
        }

        /// <summary>
        /// GetHashCode()。プロパティの方がインライン化されやすい気がするので作ってみた。(←いい加減)
        /// </summary>
        public int HashCode {
            get { return unchecked((int)(Value & MaskFromToPromote)); }
        }

        #region IEquatable<Move> メンバ

        public bool Equals(Move other) {
#if true
            return (this.Value & MaskFromToPromote) == (other.Value & MaskFromToPromote);
#elif true
            return HashCode == other.HashCode;
#else
            return From == other.From && To == other.To &&
                Promote == other.Promote &&
                (Piece & ~Piece.PEW) == (other.Piece & ~Piece.PEW);
#endif
        }

        #endregion

        /// <summary>
        /// デバッグ用の詳細な文字列化
        /// </summary>
        public string GetDebugString() {
            return GetDebugString(null);
        }
        /// <summary>
        /// デバッグ用の詳細な文字列化
        /// </summary>
        public string GetDebugString(Board board) {
            if (IsEmpty || IsSpecialState) {
                return ToString(board);
            }
            return ToString(board) + " : " + PieceUtility.ToDisplayString(Capture);
        }

        /// <summary>
        /// 適当文字列化
        /// </summary>
        public override string ToString() {
            return ToString(null);
        }
        /// <summary>
        /// 適当文字列化
        /// </summary>
        public string ToString(Board board) {
            if (board == null) {
                return ToString(2, Piece.EMPTY, new Move());
            } else {
                return ToString(board.Turn, board[From], board.GetLastMove());
            }
        }
        /// <summary>
        /// 適当文字列化(board.GetLastMove() == thisの場合用)
        /// </summary>
        public string ToStringForLater(Board board) {
            Debug.Assert(board != null);
            Debug.Assert(board.GetLastMove() == this);
            return ToString(board.Turn ^ 1, board[To], board.GetSecondLastMove());
        }
        /// <summary>
        /// 適当文字列化
        /// </summary>
        public string ToString(int turn, Piece fromPiece, Move lastMove) {
            if (IsEmpty) return "Empty";
            if (IsSpecialState) {
                return new[] { "▲", "△", "" }[turn] + GetSpecialStateName();
            }
            return new[] { "▲", "△", "" }[turn] +
                (To == lastMove.To ? "同" :  (To - Board.Padding).ToString("x2")) +
                PieceUtility.ToName(IsPut ? PutPiece : fromPiece) +
                (IsPromote ? "成" : "") +
                (IsPut ? "打" : "(" + (From - Board.Padding).ToString("x2") + ")");
        }
        /// <summary>
        /// 適当文字列化その2
        /// </summary>
        public string ToStringSimple(Board board) {
            return ToStringSimple(board,
                board == null ? new Move() : board.GetLastMove());
        }
        /// <summary>
        /// 適当文字列化その2
        /// </summary>
        /// <param name="lastMove">一つ前の手。</param>
        public string ToStringSimple(Board board, Move lastMove) {
            return ToStringSimple(board == null ? Piece.WALL : board[From], lastMove);
        }
        /// <summary>
        /// 適当文字列化その2
        /// </summary>
        public string ToStringSimple(Piece fromPiece, Move lastMove) {
            if (IsSpecialState) {
                return GetSpecialStateName();
            }
            if (IsPut) {
                return (To - Board.Padding).ToString("x2") + PieceUtility.ToName(PutPiece) + "打";
            }
            string pn = fromPiece == Piece.WALL ? "  " : PieceUtility.ToName(fromPiece | Promote);
            if (lastMove.To == To) {
                return "同" + pn;
            }
            return (To - Board.Padding).ToString("x2") + pn;
        }

        /// <summary>
        /// SpecialStateの文字列化
        /// </summary>
        private string GetSpecialStateName() {
            Debug.Assert(IsSpecialState);
            if (this == Pass) return "パス";
            if (this == Resign) return "投了";
            if (this == Win) return "勝ち";
            if (this == Endless) return "千日手";
            if (this == Perpetual) return "連続王手の千日手";
#if DANGEROUS
            return "エラー";
#else
            throw new InvalidOperationException("不正な指し手: " + Value.ToString("x8"));
#endif
        }

        /// <summary>
        /// 上下・敵味方の反転。(デバッグとか用)
        /// </summary>
        public Move Reverse() {
            Move move = new Move();
            move.From = IsPut ? From : (byte)(Board.BoardCenter - From);
            move.To = (byte)(Board.BoardCenter - To);
            move.Promote = Promote;
            move.Capture = PieceUtility.SelfOrEnemy[1 * 32 + (byte)Capture];
            return move;
        }

        /// <summary>
        /// operator
        /// </summary>
        public static bool operator ==(Move x, Move y) { return x.Equals(y); }
        /// <summary>
        /// operator
        /// </summary>
        public static bool operator !=(Move x, Move y) { return !(x == y); }

        #region 定跡データとかとの相互変換

        /// <summary>
        /// 棋譜データへの変換
        /// </summary>
        public MoveData ToNotation() {
            if (IsSpecialState) {
                if (this == Pass) return MoveData.Pass;
                if (this == Resign) return MoveData.Resign;
                if (this == Win) return MoveData.Win;
                if (this == Endless) return MoveData.Endless;
                if (this == Perpetual) return MoveData.Perpetual;
#if DANGEROUS
                return new MoveData();
#else
                throw new InvalidOperationException("不正な指し手: " + Value.ToString("x8"));
#endif
            } else if (IsEmpty) {
                return new MoveData();
            }
            int from = 0x11 <= From ? GetUsapyonIndex(From) : 100 + From;
            int n = GetUsapyonIndex(To);
            int to = IsPromote ? n + 100 : n;
            unchecked {
                return new MoveData((byte)from, (byte)to);
            }
        }
        /// <summary>
        /// 棋泉寄付形式とかでの座標表現に変換
        /// </summary>
        static int GetUsapyonIndex(/*int turn,*/ int pos) {
            //pos = turn == 0 ? pos : (Board.BoardCenter - pos);
            pos -= Board.Padding;
            return (pos / 0x10) + ((pos & 0x0f) - 1) * 9;
        }

        /// <summary>
        /// 棋譜データへの変換
        /// </summary>
        public static MoveData[] ToNotation(IEnumerable<Move> moves) {
            if (moves == null) return new MoveData[0];
            MoveData[] data = new MoveData[moves.Count()];
            int i = 0;
            foreach (Move move in moves) {
                data[i++] = move.ToNotation();
            }
            return data;
        }

        /// <summary>
        /// 棋譜データからの変換
        /// </summary>
        public static Move FromNotation(Board board, MoveData move) {
            if (move.IsSpecialMove) {
                if (move == MoveData.Resign) return Move.Resign;
                if (move == MoveData.Pass) return Move.Pass;
                if (move == MoveData.Win) return Move.Win;
                if (move == MoveData.Endless) return Move.Endless;
                if (move == MoveData.Perpetual) return Move.Perpetual;
            }
            Move result = FromUsapyon(board, move.From, move.To);
            Debug.Assert(result.IsPut == move.IsPut);
            Debug.Assert(result.IsPromote == move.IsPromote);
            return result;
        }

        /// <summary>
        /// うさぴょん定跡データからの変換
        /// </summary>
        static Move FromUsapyon(Board board, byte from, byte to) {
            if (to == 0xff || from == 0xff) {
                return new Move();
            }
            if (to == 0x00 && from == 0x00) {
                return new Move();
            }
            Move move = new Move();
            //move.Kind = TE_NORMAL;
            // from
            if (100 < from) {
                move.From = (byte)(from - 100); // 駒を打つ
            } else {
                int rank = (from + 8) / 9;
                int file = (from - 1) % 9 + 1;
                move.From = (byte)(file * 0x10 + rank + Board.Padding);
            }
            // to
            {
                if (100 < to) {
                    move.Promote = Piece.PROMOTED;
                    to -= 100;
                }
                int rank = (to + 8) / 9;
                int file = (to - 1) % 9 + 1;
                move.To = (byte)(file * 0x10 + rank + Board.Padding);
            }
            move.Capture = board[move.To];
            return move;
        }

        /// <summary>
        /// バイナリ化 (互換性無し)
        /// </summary>
        public ushort ToBinary() {
            if (IsEmpty) {
                return 0;
            } else if (this == Pass) {
                return ushort.MaxValue;
            } else {
                if (IsSpecialState) {
                    throw new InvalidOperationException("パス以外の特殊手は未対応: " + ToString());
                }
                int a = Board.PositionIndex[From] + 7; // 0～87
                int b = IsPromote ? Board.PositionIndex[To] + 100 : Board.PositionIndex[To]; // 100～180 : 0～80
                return (ushort)((a << 8) | b);
            }
        }
        /// <summary>
        /// バイナリから復元
        /// </summary>
        /// <param name="board">Board。指定しないとCaptureが設定されないので注意</param>
        public static Move FromBinary(Board board, ushort value) {
            if (value == 0) {
                return new Move();
            } else if (value == ushort.MaxValue) {
                return Pass;
            } else {
                int a = value >> 8, b = value & 0xff;

                Move move = new Move();
                move.From = Board.PositionIndexR[a];
                if (b < 100) {
                    move.To = Board.PositionIndexR[b + 7];
                } else {
                    move.To = Board.PositionIndexR[b - 100 + 7];
                    move.Promote = Piece.PROMOTED;
                }
                if (board != null) {
                    move.Capture = board[move.To];
                }
                return move;
            }
        }

        /// <summary>
        /// "+7776FU"とかの生成
        /// </summary>
        public string ToCSAString(Board board) {
            int f = IsPut ? 0x00 : From - Board.Padding;
            int ft = f * 0x100 + (To  - Board.Padding);
            return "+-"[board.Turn].ToString() +
                ft.ToString("x4") +
                PCLNotationReader.ToCSAName(IsPut ? PutPiece : (board[From] | Promote));
        }

        /// <summary>
        /// "+7776FU"とかのparse。
        /// </summary>
        public static Move FromCSAString(Board board, string str) {
            int turn;
            return FromCSAString(board, str, out turn);
        }
        /// <summary>
        /// "+7776FU"とかのparse。
        /// </summary>
        public static Move FromCSAString(Board board, string str, out int turn) {
            // 手番
            switch (str[0]) {
            case '+': turn = 0; break;
            case '-': turn = 1; break;
            default:
                throw new InvalidOperationException("CSA指し手表現で手番が不明: " + str);
            }
            if (board.Turn != turn) {
                throw new InvalidOperationException("CSA指し手表現で手番が盤面と不一致: " + str);
            }
            // from, to
            int from = int.Parse(str.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            int to = int.Parse(str.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            Piece p = PCLNotationReader.FromCSAName(str.Substring(5, 2));
            Move move = new Move();
            move.From = from == 0 ? (byte)p : (byte)(from + Board.Padding);
            move.To = (byte)(to + Board.Padding);
            move.Promote = (p ^ board[move.From]) & Piece.PROMOTED; // 高速性重視の適当実装
            move.Capture = board[move.To];
            return move;
        }

        #endregion
    }
}
