
#define DIFF_EXISTSFU // 二歩情報を差分計算するかどうか。

#pragma warning disable 162 // 到達出来ないコード

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ShogiCore.BoardProperty;
using System.IO;

namespace ShogiCore {
    /// <summary>
    /// 将棋盤
    /// </summary>
    public interface IBoardStructure {
        /// <summary>
        /// 手番
        /// </summary>
        int Turn { get; }

        /// <summary>
        /// ハッシュ値
        /// </summary>
        ulong HashValue { get; }
        /// <summary>
        /// 手番側の持ち駒値
        /// </summary>
        uint HandValue { get; }
        /// <summary>
        /// 持ち駒値のハッシュ値
        /// </summary>
        ulong HandHashValue { get; }

        /// <summary>
        /// 盤面クラスからのコピー
        /// </summary>
        /// <param name="board">盤面クラス</param>
        void CopyFrom(Board board);

        /// <summary>
        /// 進める
        /// </summary>
        /// <param name="move">手</param>
        void Do(Move move);
        /// <summary>
        /// 戻す
        /// </summary>
        /// <param name="move">手</param>
        void Undo(Move move);
    }

    /// <summary>
    /// 将棋盤
    /// </summary>
    public unsafe interface IBoard : IBoardStructure {
        /// <summary>
        /// 合法な手(但し、打ち歩詰め・連続王手の千日手も含む)を生成
        /// </summary>
        /// <param name="moves">指し手を書き込むバッファ</param>
        /// <param name="index">書き込む開始index</param>
        /// <param name="moveType">生成する指し手の種類</param>
        /// <remarks>index + 書き込んだ個数</remarks>
        int GetMoves(Move* moves, int index, Board.MoveType moveType);
    }

    /// <summary>
    /// 将棋盤
    /// </summary>
    [DebuggerDisplay("MoveCount={MoveCount} DebugString={DebugString}")]
    public unsafe partial class Board : ICloneable, IBoard {
        /// <summary>
        /// logger
        /// </summary>
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region デバッグ用フラグ

        /// <summary>
        /// Board.Gen.csの利き更新を使う
        /// </summary>
        const bool UseGenUpdateControl = true;

        #endregion

        /// <summary>
        /// 盤面(パディング入り)
        /// </summary>
        /// <remarks>
        /// 0x[横(数字右から左)][縦(漢数字上から下)] + Paddingで表す。
        /// つまり、右上が0x11, 左上が0x91、右下が0x19, 左下が0x99。(全部 + Paddingなので注意)
        /// </remarks>
        Piece[] board = new Piece[BoardLength];
        /// <summary>
        /// 持ち駒値
        /// </summary>
        public uint[] HandValues { get; private set; }
        /// <summary>
        /// (手番側の)持ち駒値
        /// </summary>
        public uint HandValue { get { return HandValues[Turn]; } }
        /// <summary>
        /// 持ち駒値のハッシュ値
        /// </summary>
        public ulong HandHashValue { get { return GetHandHashValue(HandValues[Turn]); } }
        /// <summary>
        /// 王の位置
        /// </summary>
        byte[] king = { 0, 0 };
        /// <summary>
        /// 敵味方の各位置の利き。
        /// </summary>
        int[][] control = new int[2][] { new int[BoardLength], new int[BoardLength] };

        /// <summary>
        /// その時点の自王への敵利き。
        /// つまりcontrol[Turn ^ 1][king[Turn]]。
        /// </summary>
        int kingControl = 0;
#if DIFF_EXISTSFU
        /// <summary>
        /// その筋に歩が存在するのかどうか。
        /// </summary>
        bool[] existsFU = new bool[2 * 16];
#endif
        /// <summary>
        /// 手数
        /// </summary>
        public int MoveCount { get; private set; }
        /// <summary>
        /// 先手の番なら0、後手の番なら1。
        /// </summary>
        public int Turn { get; private set; }
        /// <summary>
        /// ハッシュ値 (手番情報を含む)
        /// </summary>
        public ulong HashValue { get; private set; }

        /// <summary>
        /// 持ち駒値を含めたハッシュ値
        /// </summary>
        public ulong FullHashValue {
            get { return HashValue ^ HandValue; }
        }
        /// <summary>
        /// 持ち駒値のハッシュ値を含めたハッシュ値 (FullHashValueと違い、値が偏ってないバージョン)
        /// </summary>
        public ulong RandomizedFullHash {
            get { return HashValue ^ GetHandHashValue(HandValues[Turn]); }
        }

        /// <summary>
        /// ハッシュ値とかの履歴。History[MoveCount - 1]で最新の手のデータ。
        /// </summary>
        public readonly List<BoardHistoryEntry> History = new List<BoardHistoryEntry>(256);
        /// <summary>
        /// 履歴の最後
        /// </summary>
        public BoardHistoryEntry Last {
            get { return History[History.Count - 1]; }
        }
        /// <summary>
        /// 最後の手の取得。無ければnew Move()
        /// </summary>
        public Move GetLastMove() {
            return 1 <= History.Count ? History[History.Count - 1].Move : new Move();
        }
        /// <summary>
        /// 最後の1つ前の手の取得。無ければnew Move()
        /// </summary>
        public Move GetSecondLastMove() {
            return 2 <= History.Count ? History[History.Count - 2].Move : new Move();
        }
        /// <summary>
        /// 履歴の最後が歩打ちならtrue。
        /// 打ち歩詰め処理用。
        /// </summary>
        public bool LastWasPutFU {
            get { return 1 <= History.Count ? History[History.Count - 1].Move.IsPutFU : false; }
        }

        #region イベント

        /// <summary>
        /// Do()の最初
        /// </summary>
        public event EventHandler<BoardMoveEventArgs> PreDo;
        /// <summary>
        /// Do()の最後
        /// </summary>
        public event EventHandler<BoardMoveEventArgs> PostDo;
        /// <summary>
        /// Undo()の最初
        /// </summary>
        public event EventHandler<BoardMoveEventArgs> PreUndo;
        /// <summary>
        /// Undo()の最後
        /// </summary>
        public event EventHandler<BoardMoveEventArgs> PostUndo;

        /// <summary>
        /// 探索中のnewを避ける為の変数。
        /// </summary>
        private BoardMoveEventArgs boardMoveEventArgs;

        #endregion

        /// <summary>
        /// 駒を動かす手の生成
        /// </summary>
        delegate int MakePieceMoveDelegate(Move* moves, int index, int from, MoveType moveType);
        /// <summary>
        /// 駒を打つ手の生成
        /// </summary>
        delegate int MakePutDelegate(Move* moves, int index);

        /// <summary>
        /// 指し手生成用テーブル
        /// </summary>
        readonly MakePieceMoveDelegate[] MakePieceMoveTable;
        /// <summary>
        /// 指し手生成用テーブル
        /// </summary>
        readonly MakePutDelegate[] MakePutTable;

        /// <summary>
        /// 内部実装用の空コンストラクタ用パラメータ。
        /// </summary>
        struct EmptyConstructor { }
        /// <summary>
        /// 内部実装用の空コンストラクタ。初期化はここに書く。
        /// </summary>
        private Board(EmptyConstructor dummy) {
            HandValues = new uint[2];
            boardMoveEventArgs = new BoardMoveEventArgs(this);

            MakePieceMoveTable = new MakePieceMoveDelegate[(byte)Piece.AllCount] {
                null,
                MakePieceMoveFU0,
                MakePieceMoveKY0,
                MakePieceMoveKE0,
                MakePieceMoveGI0,
                MakePieceMoveKI0,
                MakePieceMoveKA0,
                MakePieceMoveHI0,
                (moves, index, from, moveType) => MakeMoveKing(moves, index, 0), // 手抜き
                MakePieceMoveKI0,
                MakePieceMoveKI0,
                MakePieceMoveKI0,
                MakePieceMoveKI0,
                null,
                MakePieceMoveUM0,
                MakePieceMoveRY0,
                null,
                MakePieceMoveFU1,
                MakePieceMoveKY1,
                MakePieceMoveKE1,
                MakePieceMoveGI1,
                MakePieceMoveKI1,
                MakePieceMoveKA1,
                MakePieceMoveHI1,
                (moves, index, from, moveType) => MakeMoveKing(moves, index, 0), // 手抜き
                MakePieceMoveKI1,
                MakePieceMoveKI1,
                MakePieceMoveKI1,
                MakePieceMoveKI1,
                null,
                MakePieceMoveUM1,
                MakePieceMoveRY1,
            };
            MakePutTable = new MakePutDelegate[40] {
                MakePut0,
                MakePut1,
                MakePut2,
                MakePut3,
                MakePut4,
                MakePut5,
                MakePut6,
                MakePut7,
                MakePut8,
                MakePut9,
                MakePut10,
                MakePut11,
                MakePut12,
                MakePut13,
                MakePut14,
                MakePut15,
                MakePut16,
                MakePut17,
                MakePut18,
                MakePut19,
                MakePut20,
                MakePut21,
                MakePut22,
                MakePut23,
                MakePut24,
                MakePut25,
                MakePut26,
                MakePut27,
                MakePut28,
                MakePut29,
                MakePut30,
                MakePut31,
                MakePut32,
                MakePut33,
                MakePut34,
                MakePut35,
                MakePut36,
                MakePut37,
                MakePut38,
                MakePut39,
            };
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public Board() : this(null) { }

        /// <summary>
        /// 初期化。
        /// </summary>
        /// <param name="boardData">Notation.BoardData</param>
        public Board(Notation.BoardData boardData)
            : this(new EmptyConstructor()) {
            Reset(boardData);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="boardData">Notation.BoardData</param>
        public void Reset(Notation.BoardData boardData) {
            ClearProperties();
            History.Clear();

            for (int i = 0; i < BoardLength; i++) {
                board[i] = Piece.WALL;
            }
            HandValues[0] = 0;
            HandValues[1] = 0;

            if (boardData == null) {
                for (int file = 0x10; file <= 0x90; file += 0x10) {
                    for (int rank = 2 + Padding; rank <= 8 + Padding; rank++) { // 2～8段目のみ
                        board[file + rank] = Piece.EMPTY;
                    }
                }
                for (int file = 0x10; file <= 0x90; file += 0x10) {
                    board[file + 3 + Padding] = Piece.EFU;
                    board[file + 7 + Padding] = Piece.FU;
                }
                board[0x11 + Padding] = board[0x91 + Padding] = Piece.EKY;
                board[0x19 + Padding] = board[0x99 + Padding] = Piece.KY;
                board[0x21 + Padding] = board[0x81 + Padding] = Piece.EKE;
                board[0x29 + Padding] = board[0x89 + Padding] = Piece.KE;
                board[0x31 + Padding] = board[0x71 + Padding] = Piece.EGI;
                board[0x39 + Padding] = board[0x79 + Padding] = Piece.GI;
                board[0x41 + Padding] = board[0x61 + Padding] = Piece.EKI;
                board[0x49 + Padding] = board[0x69 + Padding] = Piece.KI;
                board[0x22 + Padding] = Piece.EKA;
                board[0x88 + Padding] = Piece.KA;
                board[0x82 + Padding] = Piece.EHI;
                board[0x28 + Padding] = Piece.HI;
                board[king[1] = 0x51 + Padding] = Piece.EOU;
                board[king[0] = 0x59 + Padding] = Piece.OU;
                MoveCount = Turn = 0;
            } else {
                for (int file = 0x10; file <= 0x90; file += 0x10) {
                    for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                        board[file + rank] = boardData[file / 0x10, rank - Padding];
                    }
                }
                for (Piece p = Piece.FU; p <= Piece.HI; p++) {
                    HandValues[0] |= (uint)boardData.GetHand(0)[(byte)p] << Board.HandValueShift[(byte)p];
                    HandValues[1] |= (uint)boardData.GetHand(1)[(byte)p] << Board.HandValueShift[(byte)p];
                }
                MoveCount = 0;
                Turn = boardData.Turn;
                InitializeKing();
            }

            InternalReset();
        }

        /// <summary>
        /// 盤上から玉を探して座標を保持する
        /// </summary>
        private void InitializeKing() {
            king[0] = (byte)Math.Max(BoardUtility.IndexOf(board, Piece.OU, 0x11 + Padding, 0x99 - 0x11 + 1), 0);
            king[1] = (byte)Math.Max(BoardUtility.IndexOf(board, Piece.EOU, 0x11 + Padding, 0x99 - 0x11 + 1), 0);
        }

        /// <summary>
        /// 内部状態のリセット
        /// </summary>
        private void InternalReset() {
            HashValue = CalculateHash();

            // 利きデータの初期化。
            Utility.Clear(control[0], 0x11 + Padding, 0x99 - 0x11 + 1);
            Utility.Clear(control[1], 0x11 + Padding, 0x99 - 0x11 + 1);
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    Piece p = board[file + rank];
                    if (p == Piece.EMPTY) continue;
                    int t = ((byte)p >> PieceUtility.EnemyShift) & 1; // PieceUtility.IsFirstTurn(p) ? 0 : 1;
                    foreach (int dir in PieceUtility.CanJumpDirects[(byte)(p & ~Piece.ENEMY)]) {
                        for (int j = file + rank; ; ) {
                            j += Board.Direct[t * 16 + dir];
                            control[t][j] |= 1 << (dir + 10);
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
                    foreach (int dir in PieceUtility.CanMoveDirects[(byte)(p & ~Piece.ENEMY)]) {
                        control[t][file + rank + Board.Direct[t * 16 + dir]] |= 1 << dir;
                    }
                }
            }
            kingControl = king[Turn] != 0 ? GetControl(Turn ^ 1, king[Turn]) : 0;

            // 二歩チェックキャッシュの初期化。
#if DIFF_EXISTSFU
            Utility.Clear(existsFU, 1, 16 + 10 - 1);
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    switch (board[file + rank]) {
                        case Piece.FU: existsFU[0 * 16 + file / 0x10] = true; break;
                        case Piece.EFU: existsFU[1 * 16 + file / 0x10] = true; break;
                    }
                }
            }
#endif
            // 進行度なども再計算が必要なのでクリア
            ClearProperties();
        }


        #region ICloneable メンバ

        object ICloneable.Clone() {
            return Clone();
        }

        #endregion

        /// <summary>
        /// 複製の作成
        /// </summary>
        public Board Clone() {
            var copy = new Board(new EmptyConstructor());
            copy.CopyFrom(this);
            return copy;
        }

        /// <summary>
        /// otherの中身を自分へコピる。
        /// </summary>
        /// <param name="other">コピー元</param>
        public void CopyFrom(Board other) {
            Array.Copy(other.board, board, BoardLength);
            HandValues[0] = other.HandValues[0];
            HandValues[1] = other.HandValues[1];
            Array.Copy(other.control[0], control[0], control[0].Length);
            Array.Copy(other.control[1], control[1], control[1].Length);
#if DIFF_EXISTSFU
            Array.Copy(other.existsFU, existsFU, existsFU.Length);
#endif
            kingControl = other.kingControl;
            MoveCount = other.MoveCount;
            Turn = other.Turn;
            HashValue = other.HashValue;
            king[0] = other.king[0];
            king[1] = other.king[1];
            History.Clear();
            History.AddRange(other.History);
            PreDo = null;
            PostDo = null;
            PreUndo = null;
            PostUndo = null;
            ClearProperties();

            Debug.Assert(HashValue == other.HashValue, "CopyFrom()失敗？");
            Debug.Assert(HandValue == other.HandValue, "CopyFrom()失敗？");
            Debug.Assert(CalculateHash() == other.CalculateHash(), "CopyFrom()失敗？");
        }

        /// <summary>
        /// 持ち駒のハッシュ値を作成
        /// </summary>
        /// <param name="handValue">持ち駒値</param>
        /// <returns>ハッシュ値</returns>
        public static ulong GetHandHashValue(uint handValue) {
            return
                HashSeed.Seed_32_256[0][(handValue) & 0xff] ^
                HashSeed.Seed_32_256[1][(handValue >> 8) & 0xff] ^
                HashSeed.Seed_32_256[2][(handValue >> 16) & 0xff] ^
                HashSeed.Seed_32_256[3][(handValue >> 24) & 0xff];
        }

        #region 盤面などへのアクセッサ

        /// <summary>
        /// 盤面へのアクセッサ。
        /// [0x11 + Board.Padding]が右上、[0x99 + Board.Padding]が左下。
        /// </summary>
        public Piece this[int index] {
            get {
                Debug.Assert(0 <= index && index < BoardLength, "boardの範囲外へのアクセス");
                return board[index];
            }
        }

        /// <summary>
        /// ポインタを直接取得。
        /// </summary>
        public Piece[] DangerousGetPtr() {
            return board;
        }
        /// <summary>
        /// ポインタを直接取得。
        /// </summary>
        public int[][] DangerousGetControlPtr() {
            return control;
        }

        /// <summary>
        /// 王の位置を返す。
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public int GetKing(int turn) {
            return king[turn];
        }

        /// <summary>
        /// 持ってる駒の数を返す。
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        /// <param name="piece">駒の種類。</param>
        /// <returns>駒の数</returns>
        public int GetHand(int turn, Piece piece) {
            Debug.Assert(Piece.FU <= piece && piece < Piece.OU);
            return (int)((HandValues[turn] & Board.HandValueMask[(byte)piece]) >> Board.HandValueShift[(byte)piece]);
        }

        /// <summary>
        /// 現在のターンが王手されてる状態ならtrue。
        /// </summary>
        public bool Checked {
            get {
                Debug.Assert(kingControl == (king[Turn] != 0 ? GetControl(Turn ^ 1, king[Turn]) : 0));
                return kingControl != 0;
            }
        }

        /// <summary>
        /// 相手の玉が取れちゃう状態ならtrue。(つまり直前の手が自殺や王手回避漏れ)
        /// </summary>
        public bool WasSuicided {
            get {
                int kingE = king[Turn ^ 1];
                return kingE != 0 && ControlExists(Turn, kingE);
            }
        }

        #endregion

        #region 文字列化とか

        /// <summary>
        /// 文字列化
        /// </summary>
        public override string ToString() {
            StringBuilder str = new StringBuilder();
            str.Append("Hash " + HashValue.ToString()); // "x16"
            str.Append(" 持駒 " + HandValues[0] + "," + HandValues[1]); // "x8"
            str.Append(" " + (MoveCount + 1).ToString() + "手目");
            str.AppendLine();
            str.Append("後手の持駒：");
            AppendHands(str, 1);
            str.AppendLine();
            str.AppendLine("  ９ ８ ７ ６ ５ ４ ３ ２ １ ");
            str.AppendLine("+---------------------------+");
            for (int y = 0; y < 9; y++) {
                str.Append("|");
                int rank = y + 1 + Padding; // (Turn == 0 ? y + 1 : 9 - y) + Padding;
                for (int x = 0; x < 9; x++) {
                    int file = (9 - x) * 0x10; // (Turn == 0 ? 9 - x : x + 1) * 0x10;
                    str.Append(PieceUtility.ToDisplayString(board[file + rank]));
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

        /// <summary>
        /// 指し手部分の文字列化
        /// </summary>
        private void AppendHands(StringBuilder str, int turn) {
            bool hasAny = false;
            for (Piece p = Piece.HI; Piece.FU <= p; p--) {
                int h = GetHand(turn, p);
                if (0 < h) {
                    if (hasAny) str.Append("　"); // 2回目以降のみ
                    hasAny = true;
                    str.Append(PieceUtility.ToName(p));
                    if (1 < h) {
                        str.Append(Notation.NotationUtility.KanjiNumerals[h]);
                    }
                }
            }
            if (!hasAny) {
                str.Append("なし");
            }
        }

        /// <summary>
        /// 持ち駒の文字列化
        /// </summary>
        public string GetHandString(int turn) {
            StringBuilder str = new StringBuilder();
            AppendHands(str, turn);
            return str.ToString();
        }

        #endregion

        /// <summary>
        /// Piece[]にコピる
        /// </summary>
        public Piece[] CopyToArray() {
            return BoardUtility.CopyToArray(board);
        }

        /// <summary>
        /// スタート位置からまっすぐ検索
        /// </summary>
        public int SearchNotEmpty(int start, int diff) {
            return BoardUtility.SearchNotEmpty(board, start, diff);
        }

        #region CSA標準棋譜形式との相互変換

        /// <summary>
        /// CSA標準棋譜からのインスタンス化。
        /// </summary>
        /// <param name="data">CSA標準棋譜</param>
        /// <returns></returns>
        public static Board FromCSAStandard(string data) {
            return FromNotation(new Notation.PCLNotationReader().Read(data).FirstOrDefault());
        }

        /// <summary>
        /// CSA標準棋譜形式での盤面表現を返す。
        /// </summary>
        public string ToCSAStandard() {
            return new Notation.PCLNotationWriter().WriteToString(ToNotation());
        }

        #endregion

        #region 棋譜データとの相互変換

        /// <summary>
        /// 棋譜データからのインスタンス化
        /// </summary>
        /// <param name="notation">棋譜データ</param>
        /// <returns>Board</returns>
        public static Board FromNotation(Notation.INotation notation) {
            var board = new Board(notation.InitialBoard);
            board.DoAll(notation.Moves);
            return board;
        }

        /// <summary>
        /// 棋譜データからのインスタンス化
        /// </summary>
        /// <param name="notation">棋譜データ</param>
        /// <param name="moveCount">利用する指し手の数</param>
        /// <returns>Board</returns>
        public static Board FromNotation(Notation.INotation notation, int moveCount) {
            var board = new Board(notation.InitialBoard);
            board.DoAll(notation.Moves.Take(moveCount));
            return board;
        }

        /// <summary>
        /// 全手の適用
        /// </summary>
        /// <param name="moves">手のリスト</param>
        /// <exception cref="ShogiCore.Notation.NotationException">非合法手</exception>
        public void DoAll(IEnumerable<Notation.MoveDataEx> moves) {
            if (moves == null) return;

            int i = 0;
            foreach (var moveData in moves) {
                var move = Move.FromNotation(this, moveData.MoveData);
                if (IsLegalMove(ref move)) {
                    Do(move);
                } else {
                    // 最後が反則の手の場合は反則負けだと思われるので無視してしまう。
                    if (i + 1 == moves.Count()) {
                        break;
                    }
                    // それ以外はエラー
                    throw new Notation.NotationException(
                        string.Format("{0}/{1}手目に非合法手:{2}({3})",
                        (MoveCount + 1).ToString(), moves.Count().ToString(),
                        move.GetDebugString(this), GetIllegalReason(move)));
                }
                i++;
            }
        }

        /// <summary>
        /// 棋譜データからのインスタンス化。
        /// </summary>
        /// <remarks>
        /// 指し手は全て合法と仮定してIsLegalMove()を省いたもの。大差無いかもしれないけど一応。
        /// </remarks>
        /// <param name="notation">棋譜データ</param>
        /// <returns>Board</returns>
        public static Board FromNotationFast(Notation.INotation notation) {
            if (notation == null) {
                throw new ArgumentNullException("notation");
            }
            // 初期盤面
            var board = new Board(notation.InitialBoard);
            // 手の適用
            if (notation.Moves != null) {
                foreach (var moveData in notation.Moves) {
                    board.Do(Move.FromNotation(board, moveData.MoveData));
                }
            }
            return board;
        }

        /// <summary>
        /// 棋譜データへの変換 (重め)
        /// </summary>
        public Notation.Notation ToNotation() {
            var history = new List<BoardHistoryEntry>(History);
            // 開始局面まで戻る
            for (int i = 0, n = history.Count; i < n; i++) {
                UndoFast(history[n - i - 1].Move);
            }
            // 開始局面
            var notation = new Notation.Notation();
            notation.InitialBoard = ToBoardData();
            // 手を進めつつ記録
            notation.Moves = new ShogiCore.Notation.MoveDataEx[history.Count];
            for (int i = 0, n = history.Count; i < n; i++) {
                notation.Moves[i] = new Notation.MoveDataEx(history[i].Move.ToNotation());
                DoFast(history[i].Move);
            }
            return notation;
        }

        /// <summary>
        /// BoardDataへの変換
        /// </summary>
        public Notation.BoardData ToBoardData() {
            var boardData = new Notation.BoardData();
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    boardData[file / 0x10, rank - Padding] = board[file + rank];
                }
            }
            for (int t = 0; t < 2; t++) {
                int[] h = boardData.GetHand(t);
                for (var p = Piece.FU; p < Piece.OU; p++) {
                    h[(byte)p] = GetHand(t, (Piece)p);
                }
            }
            boardData.Turn = Turn;
            return boardData;
        }

        #endregion

        /// <summary>
        /// 棋譜の再生
        /// </summary>
        /// <param name="notation">棋譜</param>
        /// <returns>指し手</returns>
        public IEnumerable<Move> ReadNotation(Notation.INotation notation) {
            Reset(notation.InitialBoard);
            return ReadNotation(notation.Moves);
        }

        /// <summary>
        /// 棋譜の再生
        /// </summary>
        /// <param name="moves">棋譜の指し手</param>
        /// <returns>指し手</returns>
        public IEnumerable<Move> ReadNotation(Notation.MoveDataEx[] moves) {
            // 指し手
            for (int i = 0; i < moves.Length; i++) {
                Move move = Move.FromNotation(this, moves[i].MoveData);
                if (!IsLegalMove(ref move)) {
                    if (i + 1 == moves.Length) {
                        // 最後が違法手なら反則負けのはずで、これは結構な数あるので無視。
                    } else {
                        // これも何故か結構ある。ほぼ全部王手放置とかの自爆手。棋譜読み込みのバグだったりして…？
                        logger.Debug("指し手エラー: 手数=" +
                            (MoveCount + 1).ToString() + "/" +
                            moves.Length.ToString() + ", " +
                            move.GetDebugString(this) + ", " +
                            GetIllegalReason(move));
                    }
                    break; // エラー？
                }

                yield return move;

                // 進める
                Do(move);
            }
        }

        #region 適当バイナリ表現との相互変換

        /// <summary>
        /// 書き込み
        /// </summary>
        public void Write(BinaryWriter writer) {
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    writer.Write((byte)board[file + rank]);
                }
            }
            writer.Write(HandValues[0]);
            writer.Write(HandValues[1]);
            writer.Write((byte)Turn);
            writer.Write((ushort)MoveCount);
            writer.Write((ushort)History.Count);
            for (int i = 0; i < History.Count; i++) {
                History[i].Write(writer);
            }
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public void Read(BinaryReader reader) {
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    board[file + rank] = (Piece)reader.ReadByte();
                }
            }
            HandValues[0] = reader.ReadUInt32();
            HandValues[1] = reader.ReadUInt32();
            Turn = reader.ReadByte();
            MoveCount = reader.ReadUInt16();
            int historyCount = reader.ReadUInt16();
            History.Clear();
            for (int i = 0; i < historyCount; i++) {
                History.Add(new BoardHistoryEntry(reader));
            }
            InitializeKing();
            InternalReset();
        }

        #endregion

        /// <summary>
        /// ハッシュ値の算出処理
        /// </summary>
        public ulong CalculateHash() {
            return CalculateHash(1, 9, 1, 9);
        }
        /// <summary>
        /// 部分的なハッシュの計算
        /// </summary>
        public ulong CalculateHash(int fromFile, int toFile, int fromRank, int toRank) {
            return HashSeed.CalculateHash(board, Turn, fromFile, toFile, fromRank, toRank);
        }
        /// <summary>
        /// FullHashValueの非キャッシュ版
        /// </summary>
        public ulong CalculateFullHash() {
            return CalculateHash() ^ HandValues[Turn];
        }

        /// <summary>
        /// 指定した筋に歩が存在するかどうか。
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public bool ExistsFU(int turn, int file) {
#if DIFF_EXISTSFU
            return existsFU[turn * 16 + file];
#else
            return BoardUtility.ExistsFU(board, turn, file * 0x10);
#endif
        }

        public const int HandValueShiftFU = 0;
        public const int HandValueShiftKY = HandValueShiftFU + 6;
        public const int HandValueShiftKE = HandValueShiftKY + 4;
        public const int HandValueShiftGI = HandValueShiftKE + 4;
        public const int HandValueShiftKI = HandValueShiftGI + 4;
        public const int HandValueShiftKA = HandValueShiftKI + 4;
        public const int HandValueShiftHI = HandValueShiftKA + 3;
        public const int HandValueShiftOU = HandValueShiftHI + 3;
        public const uint HandValueMaskFU = ((1u << HandValueShiftKY) - 1);
        public const uint HandValueMaskKY = ((1u << HandValueShiftKE) - 1) & ~((1u << HandValueShiftKY) - 1);
        public const uint HandValueMaskKE = ((1u << HandValueShiftGI) - 1) & ~((1u << HandValueShiftKE) - 1);
        public const uint HandValueMaskGI = ((1u << HandValueShiftKI) - 1) & ~((1u << HandValueShiftGI) - 1);
        public const uint HandValueMaskKI = ((1u << HandValueShiftKA) - 1) & ~((1u << HandValueShiftKI) - 1);
        public const uint HandValueMaskKA = ((1u << HandValueShiftHI) - 1) & ~((1u << HandValueShiftKA) - 1);
        public const uint HandValueMaskHI = ((1u << HandValueShiftOU) - 1) & ~((1u << HandValueShiftHI) - 1);
        public const uint HandValueShiftedMaskFU = HandValueMaskFU >> HandValueShiftFU;
        public const uint HandValueShiftedMaskKY = HandValueMaskKY >> HandValueShiftKY;
        public const uint HandValueShiftedMaskKE = HandValueMaskKE >> HandValueShiftKE;
        public const uint HandValueShiftedMaskGI = HandValueMaskGI >> HandValueShiftGI;
        public const uint HandValueShiftedMaskKI = HandValueMaskKI >> HandValueShiftKI;
        public const uint HandValueShiftedMaskKA = HandValueMaskKA >> HandValueShiftKA;
        public const uint HandValueShiftedMaskHI = HandValueMaskHI >> HandValueShiftHI;
        /// <summary>
        /// 持ち駒値算出用のシフト量
        /// </summary>
        public static readonly sbyte[] HandValueShift = new sbyte[(byte)Piece.OU + 1] {
        // 空 歩 香 桂  銀  金  角  飛
            0, HandValueShiftFU,
            HandValueShiftKY, HandValueShiftKE,
            HandValueShiftGI, HandValueShiftKI,
            HandValueShiftKA, HandValueShiftHI,
            HandValueShiftOU,
        };

        /// <summary>
        /// 持ち駒値のマスク
        /// </summary>
        public static readonly uint[] HandValueMask = new uint[(byte)Piece.OU] {
            0, HandValueMaskFU,
            HandValueMaskKY, HandValueMaskKE,
            HandValueMaskGI, HandValueMaskKI,
            HandValueMaskKA, HandValueMaskHI,
        };

        /// <summary>
        /// 持ち駒値のボローマスク。
        /// </summary>
        public const uint HandValueBorrowMask =
            (1u << (HandValueShiftKY - 1)) |
            (1u << (HandValueShiftKE - 1)) |
            (1u << (HandValueShiftGI - 1)) |
            (1u << (HandValueShiftKI - 1)) |
            (1u << (HandValueShiftKA - 1)) |
            (1u << (HandValueShiftHI - 1)) |
            (1u << (HandValueShiftOU - 1)); // == 0x09222220 (0x08222224)

        /// <summary>
        /// a &lt;= b ならtrue
        /// </summary>
        public static bool IsHandValueDominant(uint a, uint b) {
            return unchecked((b - a) & HandValueBorrowMask) == 0; // 1つもボローしなかった
            // return unchecked((a - b) & HandValueMask) != 0; // 1つでもボローしたらtrue
            /*
            http:// www.yss-aya.com/bbs_log/bk2005-5.html
            #define SIGN_BITS 0x8222224
            int dominate(unsigned long n1,unsigned long n2)
            {
                unsigned int diff = n2 - n1;
                if ( (diff & SIGN_BITS) == 0 ) { // <= の判定だけならこれだけでOK
                    if (diff == 0)
                        return EQUAL;
                    else
                        return LESS_THAN; // <, == の判定ならここまででOK
                } else if ( (-diff & SIGN_BITS) == 0 )
                    return MORE_THAN;
                else
                    return UNCOMPARABLE;
            }
            */
        }

        /// <summary>
        /// 持ち駒値の文字列化 (デバッグとか用)
        /// </summary>
        public static string HandValueToString(uint hand) {
            const uint mask = (1u << HandValueShiftOU) - 1;
            if ((hand & mask) == 0) return "なし";
            StringBuilder str = new StringBuilder();
            for (Piece p = Piece.HI; Piece.FU <= p; p--) {
                uint h = (hand & HandValueMask[(byte)p]) >> HandValueShift[(byte)p];
                if (0 < h) {
                    if (0 < str.Length) str.Append("　"); // 2回目以降のみ
                    str.Append(PieceUtility.ToName(p));
                    if (1 < h) {
                        str.Append(Notation.NotationUtility.KanjiNumerals[h]);
                    }
                }
            }
            if (str.Length <= 0) return "なし";
            return str.ToString();
        }

        /// <summary>
        /// 合法手なのかどうかチェック (打ち歩詰は未チェック)
        /// </summary>
        public bool IsLegalMove(ref Move move) {
            if (!IsCorrectMove(move)) return false;
            if (move.IsSpecialState) {
                return true;
            } else {
                move.Capture = board[move.To];
                return !IsSuicideMove(move);
            }
        }

        /// <summary>
        /// 王手回避漏れや自殺手なのかどうか。
        /// </summary>
        /// <param name="move">指し手</param>
        /// <returns>自殺手だったらtrue</returns>
        public bool IsSuicideMove(Move move) {
            if (!Checked) {
                // 打つ手と飛び利きを遮ってない手は無視
                if (move.IsPut) return false;
                if ((control[Turn ^ 1][move.From] & Board.ControlMaskJump) == 0) return false;
            }
            // 王手回避漏れや自殺手のチェック。
            int kingS = (this.board[move.From] & ~Piece.ENEMY) == Piece.OU ? move.To : king[Turn];
            if (kingS == 0) return false;
            BoardUtility.Do(board, move, Turn);
            bool suicided = BoardUtility.ControlExists(board, Turn ^ 1, kingS);
            BoardUtility.Undo(board, move, Turn);
            Debug.Assert(HashValue == CalculateHash(), "board破壊チェック");
            return suicided;
        }

        /// <summary>
        /// 合法手なのかどうかチェック (打ち歩詰は未チェック)
        /// </summary>
        public bool IsCorrectMove(Move move) {
            if (move.IsEmpty) return false;
            if (move == Move.Pass) return !Checked;
            if (move.IsSpecialState) return true;

            if (move.IsPut) {
                if (board[move.To] != Piece.EMPTY) return false;
                if (GetHand(Turn, move.PutPiece) <= 0) return false;
                Piece putPiece = move.PutPiece;
                if (putPiece <= Piece.KE) {
                    // 行き所の無い駒
                    int toRank = Board.GetRank(move.To, Turn);
                    if (toRank <= 2) {
                        if (putPiece == Piece.KE) return false;
                        if (toRank <= 1) return false;
                    }
                    // 二歩
                    if (putPiece == Piece.FU) {
                        if (ExistsFU(Turn, Board.GetFile(move.To))) return false;
                        // if (IsMateByPutFU(move.To)) return false;
                    }
                }
                return true;
            }

            if (!PieceUtility.IsSelf(board[move.From], Turn)) return false;
            if (!PieceUtility.IsMovable(board[move.To], Turn)) return false;

            // 成る手
            if (move.IsPromote) {
                if (!PieceUtility.CanPromote(board[move.From])) return false;
                if (GetRank(move.To, Turn) > 3 && GetRank(move.From, Turn) > 3) return false;
            }

            // 行き所の無い駒
            Piece pieceType = board[move.From] & ~Piece.ENEMY;
            if (pieceType <= Piece.KE && !move.IsPromote) {
                int toRank = Board.GetRank(move.To, Turn);
                if (toRank <= 2) {
                    if (pieceType == Piece.KE) return false;
                    if (toRank <= 1) return false;
                }
            }

            // 桂馬なら移動先が正しいかチェックしておしまい
            if (pieceType == Piece.KE) {
                if (!PieceUtility.CanMoveSafe(board[move.From], Board.GetDirectIndexKE(NegativeByTurn(move.To - move.From, Turn)))) {
                    return false; // 移動先が変
                }
                return true;
            }

            // 玉なら移動先の敵利きチェック
            if (pieceType == Piece.OU) {
                if (ControlExists(Turn ^ 1, move.To)) return false; // 玉の移動先に敵利き
            }

            // 移動距離
            int d = Board.GetSlippage(move.From, move.To);
            if (d == 0) return false;
            // 方向
            int dir = (move.To - move.From) / d;

            if (d != 1) {
                if (!PieceUtility.CanJumpSafe(board[move.From], Board.GetDirectIndex8(NegativeByTurn(dir, Turn)))) {
                    return false; // 移動先が変
                }
                // 途中に邪魔な駒がいないかどうかチェック
                for (int i = 1, pos = move.From + dir; i < d; i++, pos = pos + dir) {
                    if (board[pos] != Piece.EMPTY) {
                        return false;
                    }
                }
            } else {
                if (!PieceUtility.CanMoveSafe(board[move.From], Board.GetDirectIndex8(NegativeByTurn(dir, Turn)))) {
                    return false; // 移動先が変
                }
            }

            return true;
        }

        /// <summary>
        /// !IsLegalMove()な時に、その理由を文字列で返すデバッグ用関数。(二度手間で少し重め)
        /// 合法な手ならnull。
        /// </summary>
        public string GetIllegalReason(Move move) {
            if (move.IsEmpty) return "Empty";
            if (move == Move.Pass) return "王手時のパス";
            if (move.IsSpecialState) return "合法手:" + move.ToString(this);

            if (move.IsPut) {
                if (board[move.To] != Piece.EMPTY) return "非空きマスに打つ手";
                if (GetHand(Turn, move.PutPiece) <= 0) return "非所持の駒を打つ手";
                Piece putPiece = move.PutPiece;
                if (putPiece <= Piece.KE) {
                    // 行き所の無い駒
                    int toRank = Board.GetRank(move.To, Turn);
                    if (toRank <= 2) {
                        if (putPiece == Piece.KE) return "行き所の無い駒(1)";
                        if (toRank <= 1) return "行き所の無い駒(2)";
                    }
                    // 二歩
                    if (putPiece == Piece.FU) {
                        if (ExistsFU(Turn, Board.GetFile(move.To))) return "二歩";
                        // if (IsMateByPutFU(move.To)) return "打ち歩詰め";
                    }
                }
                return Checked ? "王手回避漏れ(1)" : "自殺手(1)";
            }

            if (!PieceUtility.IsSelf(board[move.From], Turn)) return "自駒以外を動かす手";
            if (!PieceUtility.IsMovable(board[move.To], Turn)) return "移動先が自駒か壁";

            // 成る手
            if (move.IsPromote) {
                if (!PieceUtility.CanPromote(board[move.From])) return "成れない駒の成る手";
                if (GetRank(move.To, Turn) > 3 && GetRank(move.From, Turn) > 3) return "成れない移動元・先の成る手";
            }

            // 行き所の無い駒
            Piece pieceType = board[move.From] & ~Piece.ENEMY;
            if (pieceType <= Piece.KE && !move.IsPromote) {
                int toRank = Board.GetRank(move.To, Turn);
                if (toRank <= 2) {
                    if (pieceType == Piece.KE) return "行き所の無い駒(3)";
                    if (toRank <= 1) return "行き所の無い駒(4)";
                }
            }

            // 桂馬なら移動先が正しいかチェックしておしまい
            if (pieceType == Piece.KE) {
                if (!PieceUtility.CanMoveSafe(board[move.From], Board.GetDirectIndexKE(NegativeByTurn(move.To - move.From, Turn)))) {
                    return "移動先が変(1)";
                }
                return Checked ? "王手回避漏れ(2)" : "自殺手(2)";
            }

            // 玉なら移動先の敵利きチェック
            if (pieceType == Piece.OU) {
                if (ControlExists(Turn ^ 1, move.To)) return "玉の移動先に敵利き";
            }

            // 移動距離
            int d = Board.GetSlippage(move.From, move.To);
            if (d == 0) return "移動先が変(2)";
            // 方向
            int dir = (move.To - move.From) / d;

            if (d != 1) {
                if (!PieceUtility.CanJumpSafe(board[move.From], Board.GetDirectIndex8(NegativeByTurn(dir, Turn)))) {
                    return "移動先が変(3)";
                }
                // 途中に邪魔な駒がいないかどうかチェック
                for (int i = 1, pos = move.From + dir; i < d; i++, pos = pos + dir) {
                    if (board[pos] != Piece.EMPTY) {
                        return "飛び駒の移動途中に邪魔な駒";
                    }
                }
            } else {
                if (!PieceUtility.CanMoveSafe(board[move.From], Board.GetDirectIndex8(NegativeByTurn(dir, Turn)))) {
                    return "移動先が変(4)";
                }
            }

            return Checked ? "王手回避漏れ(4)" : "自殺手(4)";
        }

        /// <summary>
        /// 王手な手なのかどうか。
        /// </summary>
        public bool IsCheckMove(Move move) {
            // if (!IsLegalMove(ref move)) return false;
            int jc;
            if (move.IsPut || (jc = GetControl(Turn, move.From) & Board.ControlMaskJump) == 0 ||
                ((board[move.From] & ~Piece.ENEMY) == Piece.FU && jc == 1 << (Board.Direct[Turn * 16 + 6] + 10))) {
                // 打つ手か、自分の飛び利きを遮ってなかった駒か、
                // 飛車・香車先の歩を突く手(手抜き)なら、直接王手だけ考慮すればOK。
                bool isCheck = IsDirectCheckMove(move);
                Debug.Assert(isCheck == BoardUtility.IsCheckMove(board, move, Turn, king[Turn ^ 1]), "王手判定のバグ？");
                return isCheck;
            } else {
                return BoardUtility.IsCheckMove(board, move, Turn, king[Turn ^ 1]);
            }
        }

        /// <summary>
        /// 直接王手ならtrue
        /// </summary>
        public bool IsDirectCheckMove(Move move) {
            int kingE = king[Turn ^ 1];
            int to = move.To;
            Piece p = move.IsPut ? move.PutPiece :
                (board[move.From] | move.Promote) & ~Piece.ENEMY;
            foreach (int dir in PieceUtility.CanMoveDirects[(byte)p]) {
                if (to + Board.Direct[Turn * 16 + dir] == kingE) {
                    return true;
                }
            }
            foreach (int dir in PieceUtility.CanJumpDirects[(byte)p]) {
                if (SearchNotEmpty(to, Board.Direct[Turn * 16 + dir]) == kingE) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// moveで詰みなのかどうか。
        /// </summary>
        public bool IsMate(Move move) {
            Debug.Assert(IsCheckMove(move), "IsMate()の指し手が非王手: " + move.GetDebugString(this));
            if (move.PutPiece == Piece.FU) return false; // 歩打ちは例え詰みでも打ち歩詰め。
            DoFast(move);
            bool suicide = !move.IsPut && king[Turn ^ 1] != 0 && ControlExists(Turn, king[Turn ^ 1]);
            bool mate = !suicide && IsMateFast(); // 自殺手でなく詰みなら詰み。
            UndoFast(move);
            Debug.Assert(HashValue == CalculateHash(), "board破壊チェック");
            Debug.Assert(suicide || IsLegalMove(ref move), "IsMate()で不正な手？: " + GetIllegalReason(move));
            return mate;
        }

        /// <summary>
        /// 詰んでたらtrue
        /// </summary>
        public bool IsMate() {
            return Checked && IsMateFast();
        }

        /// <summary>
        /// 詰んでたらtrue。王手がかかってる時に呼ばれる事前提なので注意。
        /// </summary>
        public bool IsMateFast() {
            Debug.Assert(Checked, "Board.IsMate()で非王手");
            /*
            bool mate = InternalIsMate();
            Debug.Assert(mate == (GetMovesSafe().Count <= 0), "Board.IsMateFast()の実装バグ？");
            return mate;
            /*/
            return (GetMovesSafe().Count <= 0);
            // */
        }

        /// <summary>
        /// 詰んでたらtrue。
        /// </summary>
        private bool InternalIsMate() {
            int control = kingControl;
            if ((control & (control - 1)) == 0) {  // 通常の王手
                int id; // 方向のインデックス
                for (id = 0; id <= 17; id++) {
                    if (control == (1 << id)) break;
                }
                int check;  // 王手してきた駒の位置
                int diff; // 方向
                if (id < 10) {
                    diff = Board.Direct[Turn * 16 + id];
                    check = king[Turn] + diff;
                    Debug.Assert(PieceUtility.CanMove(board[check], id), "利きから王手駒の取得に失敗: " + board[check]);
                } else {
                    diff = Board.Direct[Turn * 16 + id - 10];
                    check = BoardUtility.SearchNotEmpty(board, king[Turn], diff);
                    Debug.Assert(PieceUtility.CanJump(board[check], id - 10), "利きから王手駒の取得に失敗: " + board[check]);
                }
                // 王手駒を取る
                if (CanMoveNotKing(check)) return false;
                // 玉を動かす
                if (CanMoveKing(control)) return false;
                if (10 <= id) {
                    // 合駒をする手を生成
                    for (int pos = king[Turn] + diff; board[pos] == Piece.EMPTY; pos += diff) {
                        if (CanMoveNotKing(pos)) return false; // 移動合
                    }
                    for (int pos = king[Turn] + diff; board[pos] == Piece.EMPTY; pos += diff) {
                        if (CanPutTo(pos)) return false; // 駒を打つ合
                    }
                }
            } else {
                // 両王手は玉を動かすしかない
                if (CanMoveKing(control)) return false;
            }
            return true; // 詰み
        }

        /// <summary>
        /// 王以外の駒をtoへ動かせるならtrue
        /// </summary>
        private bool CanMoveNotKing(int to) {
            int control = GetControl(Turn, to);
            if (control == 0) return false;

            for (int dir = 0; dir < 10; dir++) {
                if ((control & (1 << (dir + 10))) != 0) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    int from = BoardUtility.SearchNotEmpty(board, to, -diff);
                    Debug.Assert(PieceUtility.IsSelf(board[from], Turn) && PieceUtility.CanJump(board[from], dir));
                    if (PieceUtility.IsMovable(board[to], Turn)) {
                        Move move = Move.CreateMove(board, from, to, Piece.EMPTY);
                        if (!IsSuicideMove(move)) {
                            return true; // 動かせる
                        }
                    }
                } else if ((control & (1 << dir)) != 0) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    int from = to - diff;
                    Piece p = board[from];
                    if ((p & ~Piece.ENEMY) != Piece.OU) {
                        Debug.Assert(PieceUtility.IsSelf(p, Turn) && PieceUtility.CanMove(p, dir), "利きのデータがおかしい？: " + p);
                        if (PieceUtility.IsMovable(board[to], Turn)) {
                            if (!IsSuicideMove(Move.CreateMove(board, from, to, Piece.EMPTY))) {
                                return true; // 動かせる
                            }
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 王をどこかに動かして逃げれるならtrue
        /// </summary>
        /// <param name="control">的の利き</param>
        private bool CanMoveKing(int control) {
            for (int dir = 0; dir < 8; dir++) {
                int diff = Board.Direct[Turn * 16 + dir];
                int to = king[Turn] + diff;
                if (PieceUtility.IsMovable(board[to], Turn) &&
                    !ControlExists(Turn ^ 1, to) && // 敵の駒が効いていない
                    (control & (1 << (17 - dir))) == 0) { // 敵の飛駒で貫かれていない
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 指定位置に駒を打てるならtrue
        /// </summary>
        private bool CanPutTo(int pos) {
            if (board[pos] != Piece.EMPTY) return false;
            // 歩以外を何かしら持ってれば打てる
            if ((HandValue & ~HandValueMaskFU) != 0) return true;
            // 歩は二歩じゃなければ打てる
            if ((HandValue & HandValueMaskFU) != 0) {
                if (!ExistsFU(Turn, Board.GetFile(pos))) return true;
            }
            return false;
        }

        /// <summary>
        /// 指し手が打ち歩詰めなのかどうか
        /// </summary>
        public bool IsMateByPutFU(Move move) {
            return move.IsPutFU && IsMateByPutFU(move.To);
        }

        /// <summary>
        /// toに歩を打つことが打ち歩詰めかどうか
        /// </summary>
        public bool IsMateByPutFU(int to) {
            int diff6 = -Board.Direct[Turn * 16 + 6];
            if (king[Turn ^ 1] + diff6 != to) return false; // 玉の上でなければ打ち歩詰めではない
            if (board[to] != Piece.EMPTY) return false; // 敵玉の上が空でなければ歩は打てない

            board[to] = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)Piece.FU]; // 仮に歩を打ってみる
            if (BoardUtility.ControlExists(board, Turn ^ 0, to) &&
                GetMoveE(to) == (1 << 6)) { // 歩にひもがついていて敵が取れない
                for (int dir = 0; dir < 8; dir++) {
                    int kingTo = king[Turn ^ 1] + Board.Direct[Turn * 16 + dir];
                    Piece q = board[kingTo];
                    if (PieceUtility.IsMovable(q, Turn ^ 1) && !BoardUtility.ControlExists(board, Turn, kingTo)) {
                        // 抜け道があるので打ち歩詰めではない
                        board[to] = Piece.EMPTY; // 歩を取り除く
                        return false;
                    }
                }
                board[to] = Piece.EMPTY; // 歩を取り除く
                return true;
            }
            board[to] = Piece.EMPTY; // 歩を取り除く
            return false;
        }

        /// <summary>
        /// 利きを調べる
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public int GetControl(int turn, int pos) {
            Debug.Assert(board[pos] != Piece.WALL, "壁の利きを取得しようとした");
            Debug.Assert(control[turn][pos] == GetControlIndirect(turn, pos), "利きの差分計算バグ");
            return control[turn][pos];
        }
        /// <summary>
        /// 利きの有無だけを調べる
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public bool ControlExists(int turn, int pos) {
            Debug.Assert(board[pos] != Piece.WALL, "壁の利きを取得しようとした");
            Debug.Assert(control[turn][pos] == GetControlIndirect(turn, pos), "利きの差分計算バグ");
            return control[turn][pos] != 0;
        }
        /// <summary>
        /// 利きの数を調べる
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public int GetControlCount(int turn, int pos) {
            Debug.Assert(board[pos] != Piece.WALL, "壁の利きを取得しようとした");
            Debug.Assert(control[turn][pos] == GetControlIndirect(turn, pos), "利きの差分計算バグ");
            return MathUtility.PopCnt((uint)control[turn][pos]);
        }

        /// <summary>
        /// 利きを調べる
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public int GetControlIndirect(int turn, int pos) {
            return BoardUtility.GetControl(board, turn, pos);
        }
        /// <summary>
        /// 利きの有無だけを調べる
        /// </summary>
        /// <param name="turn">先手のなら0、後手のなら1</param>
        public bool ControlExistsIndirect(int turn, int pos) {
            return BoardUtility.GetControl(board, turn, pos) != 0;
        }

        /// <summary>
        /// その地点への敵の利き(ピンされていないもの)を調べる
        /// </summary>
        private int GetMoveE(int o) {
            if (board[o] == Piece.WALL) {
                return 0;
            }
            int result = 0;
            for (int dir = 0; dir < 10; dir++) {
                int diff = Board.Direct[Turn * 16 + dir];
                Piece p = board[o + diff];
                if (PieceUtility.IsEnemy(p, Turn) && PieceUtility.CanMove(p, dir)) {
                    result |= (1 << dir);
                } else if (p == Piece.EMPTY) {
                    // 飛び道具の利きをチェック
                    int pos = BoardUtility.SearchNotEmpty(board, o, diff);
                    p = board[pos];
                    if (PieceUtility.IsEnemy(p, Turn) && PieceUtility.CanJump(p, dir)) {
                        result |= (1 << (dir + 10));
                    }
                }
            }
            return result;
        }

        #region Do()/Undo()

        /// <summary>
        /// 一手動かす
        /// </summary>
        /// <param name="move">手</param>
        public void Do(Move move) {
            var PreDo = this.PreDo;
            if (PreDo != null) {
                PreDo(this, boardMoveEventArgs.Reset(move));
            }

            DoFast(move);

            var PostDo = this.PostDo;
            if (PostDo != null) {
                PostDo(this, boardMoveEventArgs.Reset(move));
            }
        }

        /// <summary>
        /// 一手動かす (イベント発行ナシ。必ずUndoFast()とペアで使うべし。)
        /// </summary>
        /// <param name="move">手</param>
        public void DoFast(Move move) {
#if DEBUG
            if (!move.IsSpecialState) {
                if (board[move.To] != move.Capture || !IsCorrectMove(move)) {
                    Debug.Fail("非合法な指し手をBoard.Do(): " + GetIllegalReason(move));
                }
            }
#endif

            ulong oldHash = HashValue;
            uint oldHand = HandValue;

            // 手の適用
            if (move.IsSpecialState) {
                // 何もしない
            } else {
#if DANGEROUS
                InnerDo(move);
#else
                try {
                    InnerDo(move);
                } catch (Exception e) {
                    throw new ApplicationException("指し手適用時に例外発生:" + GetIllegalReason(move) +
                        Environment.NewLine + ToString() +
                        Environment.NewLine + move.GetDebugString(this), e);
                }
#endif
            }

            // ターンチェンジ
            HashValue ^= HashSeed.TurnSeed[1];
            Turn ^= 1;
            MoveCount++;

            kingControl = king[Turn] != 0 ? GetControl(Turn ^ 1, king[Turn]) : 0;

            // 履歴る。
            History.Add(new BoardHistoryEntry(oldHash, oldHand, move, Checked));
            Debug.Assert(History.Count == MoveCount);
        }

        /// <summary>
        /// Do()の実装
        /// </summary>
        /// <param name="move">手</param>
        private void InnerDo(Move move) {
            if (move.IsPut) {
#if DIFF_EXISTSFU
                if (move.PutPiece == Piece.FU) {
                    int file = Board.GetFile(move.To);
                    Debug.Assert(!existsFU[Turn * 16 + file], "二歩？");
                    existsFU[Turn * 16 + file] = true;
                }
#endif
                // 駒を打つ
                Debug.Assert(board[move.To] == Piece.EMPTY); // 打つとこ空？
                HashValue ^= HashSeed.Seed[(byte)Piece.EMPTY][move.To];
                board[move.To] = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)move.PutPiece];
                Debug.Assert((HandValues[Turn] & Board.HandValueMask[move.From]) != 0, "持ってない駒を打とうとした？");
                HandValues[Turn] -= 1u << HandValueShift[move.From];
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
            } else {
#if DIFF_EXISTSFU
                if ((board[move.From] & ~Piece.ENEMY) == Piece.FU && move.IsPromote) {
                    int file = Board.GetFile(move.To);
                    Debug.Assert(existsFU[Turn * 16 + file]);
                    existsFU[Turn * 16 + file] = false;
                }
#endif
                // 盤上の駒を移動
                Debug.Assert(board[move.To] == move.Capture); // 移動先合ってる？
                Debug.Assert(PieceUtility.IsSelf(board[move.From], Turn), "自分の駒以外を動かそうとしてる？");
                Debug.Assert(!move.IsPromote || (board[move.From] & Piece.PROMOTED) == 0);
                // move.Capture = board[move.To]; // 移動先の駒を記憶
                if (UseGenUpdateControl) {
                    UpdateControlPreMove_Gen(move);
                } else {
                    UpdateControlPreMove(move);
                }
                HashValue ^= HashSeed.Seed[(byte)board[move.From]][move.From];
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                board[move.To] = board[move.From] | move.Promote;
                board[move.From] = Piece.EMPTY;
                HashValue ^= HashSeed.Seed[(byte)board[move.From]][move.From];
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                if (move.IsCapture) {
                    // 持駒を増やす
                    Piece capturedPiece = move.Capture & ~Piece.PE;
                    HandValues[Turn] += 1u << HandValueShift[(byte)capturedPiece];
#if DIFF_EXISTSFU
                    if ((move.Capture & ~Piece.ENEMY) == Piece.FU) {
                        int file = Board.GetFile(move.To);
                        Debug.Assert(existsFU[(Turn ^ 1) * 16 + file]);
                        existsFU[(Turn ^ 1) * 16 + file] = false;
                    }
#endif
                }
                // 王の移動を記録
                if ((board[move.To] & ~Piece.ENEMY) == Piece.OU) {
                    king[Turn] = move.To;
                }
                if (UseGenUpdateControl) {
                    UpdateControlPostMove_Gen(move);
                } else {
                    UpdateControlPostMove(move);
                }
            }
            if (UseGenUpdateControl) {
                UpdateControlPostDo_Gen(move);
            } else {
                UpdateControlPostDo(move);
            }
        }

        /// <summary>
        /// 一手戻す
        /// </summary>
        public void Undo() {
            Undo(History[History.Count - 1].Move);
        }

        /// <summary>
        /// 一手戻す
        /// </summary>
        /// <param name="move">手</param>
        public void Undo(Move move) {
            var PreUndo = this.PreUndo;
            if (PreUndo != null) {
                PreUndo(this, boardMoveEventArgs.Reset(move));
            }

            UndoFast(move);

            var PostUndo = this.PostUndo;
            if (PostUndo != null) {
                PostUndo(this, boardMoveEventArgs.Reset(move));
            }
        }

        /// <summary>
        /// 一手戻す (イベント発行ナシ。必ずDoFast()とペアで使うべし。)
        /// </summary>
        /// <param name="move">手</param>
        public void UndoFast(Move move) {
            Debug.Assert(move == History[History.Count - 1].Move);

            // 履歴を削除
            Debug.Assert(History.Count == MoveCount);
            History.RemoveAt(History.Count - 1);

            // ターンチェンジ
            MoveCount--;
            Turn ^= 1;
            HashValue ^= HashSeed.TurnSeed[1];

            // 手の適用解除
            if (move.IsSpecialState) {
                // 何もしない
            } else {
                InnerUndo(move);
            }

            kingControl = king[Turn] != 0 ? GetControl(Turn ^ 1, king[Turn]) : 0;
        }

        /// <summary>
        /// Undo()の実装
        /// </summary>
        /// <param name="move">手</param>
        private void InnerUndo(Move move) {
            if (UseGenUpdateControl) {
                UpdateControlPreUndo_Gen(move);
            } else {
                UpdateControlPreUndo(move);
            }
            if (move.IsPut) {
#if DIFF_EXISTSFU
                if (move.PutPiece == Piece.FU) {
                    int file = Board.GetFile(move.To);
                    Debug.Assert(existsFU[Turn * 16 + file]);
                    existsFU[Turn * 16 + file] = false;
                }
#endif
                // 駒を打った
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                board[move.To] = Piece.EMPTY;
                HandValues[Turn] += 1u << HandValueShift[move.From];
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
            } else {
                // 盤上の駒を移動
                Debug.Assert(board[move.From] == Piece.EMPTY);
                HashValue ^= HashSeed.Seed[(byte)board[move.From]][move.From];
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                board[move.From] = board[move.To] ^ move.Promote;
                board[move.To] = move.Capture;
                HashValue ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                HashValue ^= HashSeed.Seed[(byte)board[move.From]][move.From];
                if (move.IsCapture) {
                    Piece capturedPiece = move.Capture & ~Piece.PE;
                    Debug.Assert((HandValues[Turn] & Board.HandValueMask[(byte)capturedPiece]) != 0, "変なundo");
                    HandValues[Turn] -= 1u << HandValueShift[(byte)capturedPiece];
#if DIFF_EXISTSFU
                    if ((move.Capture & ~Piece.ENEMY) == Piece.FU) {
                        int file = Board.GetFile(move.To);
                        Debug.Assert(!existsFU[(Turn ^ 1) * 16 + file]);
                        existsFU[(Turn ^ 1) * 16 + file] = true;
                    }
#endif
                }
#if DIFF_EXISTSFU
                if ((board[move.From] & ~Piece.ENEMY) == Piece.FU && move.IsPromote) {
                    int file = Board.GetFile(move.To);
                    Debug.Assert(!existsFU[Turn * 16 + file]);
                    existsFU[Turn * 16 + file] = true;
                }
#endif
                if (UseGenUpdateControl) {
                    UpdateControlPostUnmove_Gen(move);
                } else {
                    UpdateControlPostUnmove(move);
                }
                // 王の移動を記録
                if ((board[move.From] & ~Piece.ENEMY) == Piece.OU) {
                    king[Turn] = move.From;
                }
            }
            if (UseGenUpdateControl) {
                UpdateControlPostUndo_Gen(move);
            } else {
                UpdateControlPostUndo(move);
            }
        }

        #endregion

        #region 利きの差分計算

        /// <summary>
        /// 利きの差分計算その1
        /// </summary>
        private void UpdateControlPreMove(Move move) {
            // 元いた駒の利き
            Piece p = board[move.From];
            foreach (int dir in PieceUtility.CanMoveDirects[(byte)(p & ~Piece.ENEMY)]) {
                int j = move.From + Board.Direct[Turn * 16 + dir];
                Debug.Assert((control[Turn ^ 0][j] & ~(1 << dir)) != 0, "利きのデータが不正？");
                control[Turn ^ 0][j] ^= 1 << dir; // 立ってる事が分かってたら&= ~(1 << bit)より早い？
            }
            foreach (int dir in PieceUtility.CanJumpDirects[(byte)(p & ~Piece.ENEMY)]) {
                int diff = Board.Direct[Turn * 16 + dir];
                for (int j = move.From; ; ) {
                    j += diff;
                    Debug.Assert((control[Turn ^ 0][j] & ~(1 << (dir + 10))) != 0, "利きのデータが不正？");
                    control[Turn ^ 0][j] ^= 1 << (dir + 10);
                    if (board[j] != Piece.EMPTY) {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 利きの差分計算その2
        /// </summary>
        private void UpdateControlPostMove(Move move) {
            // 飛び利きの延長
            for (int t = 0; t < 2; t++) {
                if ((control[t][move.From] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                for (int dir = 0; dir < 8; dir++) {
                    if ((control[t][move.From] & (1 << (dir + 10))) != 0) {
                        int diff = Board.Direct[t * 16 + dir];
                        for (int j = move.From; ; ) {
                            j += diff;
                            control[t][j] |= 1 << (dir + 10);
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
                }
            }
            // 取った駒の利き
            if (move.IsCapture) {
                foreach (int dir in PieceUtility.CanMoveDirects[(byte)(move.Capture & ~Piece.ENEMY)]) {
                    int j = move.To + Board.Direct[(Turn ^ 1) * 16 + dir];
                    Debug.Assert((control[Turn ^ 1][j] & ~(1 << dir)) != 0, "利きのデータが不正？");
                    control[Turn ^ 1][j] ^= 1 << dir;
                }
                foreach (int dir in PieceUtility.CanJumpDirects[(byte)(move.Capture & ~Piece.ENEMY)]) {
                    int diff = Board.Direct[(Turn ^ 1) * 16 + dir];
                    for (int j = move.To; ; ) {
                        j += diff;
                        Debug.Assert((control[Turn ^ 1][j] & ~(1 << (dir + 10))) != 0, "利きのデータが不正？");
                        control[Turn ^ 1][j] ^= 1 << (dir + 10);
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
            }
        }

        /// <summary>
        /// 利きの差分計算その3
        /// </summary>
        private void UpdateControlPostDo(Move move) {
            if (!move.IsCapture) {
                // 移動先で遮った飛び利き
                for (int t = 0; t < 2; t++) {
                    if ((control[t][move.To] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                    for (int dir = 0; dir < 8; dir++) {
                        if ((control[t][move.To] & (1 << (dir + 10))) != 0) {
                            int diff = Board.Direct[t * 16 + dir];
                            for (int j = move.To; ; ) {
                                j += diff;
                                Debug.Assert((control[t][j] & ~(1 << (dir + 10))) != 0, "利きのデータが不正？");
                                control[t][j] ^= 1 << (dir + 10);
                                if (board[j] != Piece.EMPTY) break;
                            }
                        }
                    }
                }
            } else {
                Debug.Assert(!move.IsPut);
            }
            // 移動先の利き
            foreach (int dir in PieceUtility.CanMoveDirects[(byte)(board[move.To] & ~Piece.ENEMY)]) {
                int j = move.To + Board.Direct[Turn * 16 + dir];
                control[Turn ^ 0][j] |= (1 << dir);
            }
            foreach (int dir in PieceUtility.CanJumpDirects[(byte)(board[move.To] & ~Piece.ENEMY)]) {
                int diff = Board.Direct[Turn * 16 + dir];
                for (int j = move.To; ; ) {
                    j += diff;
                    control[Turn ^ 0][j] |= (1 << (dir + 10));
                    if (board[j] != Piece.EMPTY) break;
                }
            }
        }

        /// <summary>
        /// 利きの差分計算(Undo)その1
        /// </summary>
        private void UpdateControlPreUndo(Move move) {
            // 移動先の利き
            foreach (int dir in PieceUtility.CanMoveDirects[(byte)(board[move.To] & ~Piece.ENEMY)]) {
                int j = move.To + Board.Direct[Turn * 16 + dir];
                Debug.Assert((control[Turn ^ 0][j] & ~(1 << dir)) != 0, "利きのデータが不正？");
                control[Turn ^ 0][j] ^= 1 << dir;
            }
            foreach (int dir in PieceUtility.CanJumpDirects[(byte)(board[move.To] & ~Piece.ENEMY)]) {
                int diff = Board.Direct[Turn * 16 + dir];
                for (int j = move.To; ; ) {
                    j += diff;
                    Debug.Assert((control[Turn ^ 0][j] & ~(1 << (dir + 10))) != 0, "利きのデータが不正？");
                    control[Turn ^ 0][j] ^= 1 << (dir + 10);
                    if (board[j] != Piece.EMPTY) break;
                }
            }
        }

        /// <summary>
        /// 利きの差分計算(Undo)その2
        /// </summary>
        private void UpdateControlPostUnmove(Move move) {
            // 取った駒の利き
            if (move.IsCapture) {
                foreach (int dir in PieceUtility.CanMoveDirects[(byte)(move.Capture & ~Piece.ENEMY)]) {
                    int j = move.To + Board.Direct[(Turn ^ 1) * 16 + dir];
                    control[Turn ^ 1][j] |= (1 << dir);
                }
                foreach (int dir in PieceUtility.CanJumpDirects[(byte)(move.Capture & ~Piece.ENEMY)]) {
                    int diff = Board.Direct[(Turn ^ 1) * 16 + dir];
                    for (int j = move.To; ; ) {
                        j += diff;
                        control[Turn ^ 1][j] |= (1 << (dir + 10));
                        if (board[j] != Piece.EMPTY) break;
                    }
                }
            }
            // 飛び利きの延長
            for (int t = 0; t < 2; t++) {
                if ((control[t][move.From] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                for (int dir = 0; dir < 8; dir++) {
                    if ((control[t][move.From] & (1 << (dir + 10))) != 0) {
                        int diff = Board.Direct[t * 16 + dir];
                        for (int j = move.From; ; ) {
                            j += diff;
                            Debug.Assert((control[t][j] &= ~(1 << (dir + 10))) != 0, "利きのデータが不正？");
                            control[t][j] ^= 1 << (dir + 10);
                            if (board[j] != Piece.EMPTY) break;
                        }
                    }
                }
            }
            // 元いた駒の利き
            Piece p = board[move.From];
            foreach (int dir in PieceUtility.CanMoveDirects[(byte)(p & ~Piece.ENEMY)]) {
                int j = move.From + Board.Direct[Turn * 16 + dir];
                control[Turn ^ 0][j] |= (1 << dir);
            }
            foreach (int dir in PieceUtility.CanJumpDirects[(byte)(p & ~Piece.ENEMY)]) {
                int diff = Board.Direct[Turn * 16 + dir];
                for (int j = move.From; ; ) {
                    j += diff;
                    control[Turn ^ 0][j] |= (1 << (dir + 10));
                    if (board[j] != Piece.EMPTY) break;
                }
            }
        }

        /// <summary>
        /// 利きの差分計算(Undo)その3
        /// </summary>
        private void UpdateControlPostUndo(Move move) {
            if (!move.IsCapture) {
                // 移動先で遮った飛び利き
                for (int t = 0; t < 2; t++) {
                    if ((control[t][move.To] & Board.ControlMaskJump) == 0) continue; // 地味に高速化のつもり。
                    for (int dir = 0; dir < 8; dir++) {
                        if ((control[t][move.To] & (1 << (dir + 10))) != 0) {
                            int diff = Board.Direct[t * 16 + dir];
                            for (int j = move.To; ; ) {
                                j += diff;
                                control[t][j] |= (1 << (dir + 10));
                                if (board[j] != Piece.EMPTY) break;
                            }
                        }
                    }
                }
            } else {
                Debug.Assert(!move.IsPut);
            }
        }

        #endregion

        /// <summary>
        /// Clone()してReverse()して返す。
        /// </summary>
        public Board ReverseClone() {
            Board board = Clone();
            board.Reverse();
            return board;
        }

        /// <summary>
        /// 上下敵味方反転 (主にデバッグ用)
        /// </summary>
        public void Reverse() {
            Turn ^= 1; // ターンチェンジ
            // 位置変換
            for (int i = 0x11 + Padding, j = 0x99 + Padding; i < j; i++, j--) {
                Piece p = board[i];
                board[i] = PieceUtility.SelfOrEnemy[1 * 32 + (byte)board[j]];
                board[j] = PieceUtility.SelfOrEnemy[1 * 32 + (byte)p];
                // Utility.Swap(ref control[0][i], ref control[0][j]);
                // Utility.Swap(ref control[1][i], ref control[1][j]);
            }
            board[0x55 + Padding] = PieceUtility.SelfOrEnemy[1 * 32 + (byte)board[0x55 + Padding]];
            // 玉の位置(上下反転)
            if (king[0] != 0) king[0] = (byte)(BoardCenter - king[0]);
            if (king[1] != 0) king[1] = (byte)(BoardCenter - king[1]);
            // 先手後手反転
            Utility.Swap(ref king[0], ref king[1]);
            Utility.Swap(ref HandValues[0], ref HandValues[1]);
            // 色々再計算
            InternalReset();
        }

        /// <summary>
        /// 左右反転 (主にデバッグ用)
        /// </summary>
        public void ReverseLR() {
            for (int file = 0x10; file <= 0x40; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    Utility.Swap(ref board[file + rank], ref board[0xa0 - file + rank]);
                }
            }
            king[0] -= Padding;
            king[1] -= Padding;
            king[0] = (byte)(0xa0 - (king[0] & 0xf0) + (king[0] & 0x0f));
            king[1] = (byte)(0xa0 - (king[1] & 0xf0) + (king[1] & 0x0f));
            king[0] += Padding;
            king[1] += Padding;
            InternalReset();
        }

        #region 千日手関係

        /// <summary>
        /// 最後の手で千日手になったならtrue。
        /// 具体的には、現在と同一局面が過去に3回あって今が4回目ならtrue。
        /// </summary>
        public bool IsEndless() {
            return 3 <= CountSamePhase(3, HashValue, HandValue);
        }

        /// <summary>
        /// max &lt;= CountSamePhase(max)
        /// </summary>
        public bool IsEndless(int max) {
            return max <= CountSamePhase(max, HashValue, HandValue);
        }

        /// <summary>
        /// 同一局面の出現回数を数えて返す。(現在のは含めない。つまり同一局面が無ければ0)
        /// </summary>
        /// <param name="max">最大回数。この回数以上あるならそこで打ち切り。(戻り値がこの値となる)</param>
        /// <returns>同一局面の出現回数</returns>
        public int CountSamePhase(int max, ulong hash, uint hand) {
            int samePhase = 0; // 最新のは含めない
            ulong fullHash = hash ^ hand;
            for (int i = History.Count - 4; 0 <= i; i -= 2) { // 2手前が同一局面ということは無い気がするので4手前から開始
                if (History[i].FullHashValue == fullHash) {
                    if (max <= ++samePhase) break;
                }
            }
            return samePhase;
        }

        /// <summary>
        /// 連続王手の千日手ならtrueを返す。
        /// </summary>
        /// <param name="checkOffset">0か1で相手か自分か。</param>
        /// <returns>連続王手の千日手なのかどうか</returns>
        public bool IsPerpetualCheck(int checkOffset) {
            return IsPerpetualCheck(checkOffset, 3);
        }

        /// <summary>
        /// 連続王手の千日手ならtrueを返す。
        /// </summary>
        /// <param name="checkOffset">0か1で相手か自分か。</param>
        /// <param name="max">最大回数。3でルール通りの千日手</param>
        /// <returns>連続王手の千日手なのかどうか</returns>
        public bool IsPerpetualCheck(int checkOffset, int max) {
            int samePhase = 0; // 最新のは含めない
            ulong fullHash = FullHashValue;
            for (int i = History.Count - 4; checkOffset <= i; i -= 2) { // 2手前が同一局面ということは無い気がするので4手前から開始
                if (!History[i - checkOffset].Check) break;
                if (History[i].FullHashValue == fullHash) {
                    if (max <= ++samePhase) { // HashValueが4回目となる場合
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        /// <summary>
        /// 盤上と持ち駒全部合わせた各駒の個数を返す。
        /// </summary>
        public int[] GetPiecesCount() {
            int[] pc = new int[(byte)Piece.OU + 1];
            for (int file = 0x10; file <= 0x90; file += 0x10) {
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    if (board[file + rank] == Piece.EMPTY) continue;
                    pc[(byte)(board[file + rank] & ~Piece.ENEMY)]++;
                }
            }
            for (Piece p = Piece.FU; p <= Piece.HI; p++) {
                pc[(byte)p] += GetHand(0, p);
                pc[(byte)p] += GetHand(1, p);
            }
            return pc;
        }

        /// <summary>
        /// 入玉勝ちならtrue
        /// </summary>
        /// <remarks>
        ///  http:/// www.computer-shogi.org/protocol/tcp_ip_1on1_11.html
        /// 「入玉宣言勝ち」の条件(第13回選手権で使用のもの):
        ///
        /// 次の条件が成立する場合、勝ちを宣言できる(以下「入玉宣言勝ち」と云う)。
        /// 条件:
        /// (a) 宣言側の手番である。
        /// (b) 宣言側の玉が敵陣三段目以内に入っている。
        /// (c) 宣言側が(大駒5点小駒1点の計算で)
        /// ・先手の場合28点以上の持点がある。
        /// ・後手の場合27点以上の持点がある。
        /// ・点数の対象となるのは、宣言側の持駒と敵陣三段目
        /// 以内に存在する玉を除く宣言側の駒のみである。
        /// (d) 宣言側の敵陣三段目以内の駒は、玉を除いて10枚以上存在する。
        /// (e) 宣言側の玉に王手がかかっていない。
        /// (詰めろや必死であることは関係ない)
        /// (f) 宣言側の持ち時間が残っている。(切れ負けの場合)
        /// 以上1つでも条件を満たしていない場合、宣言した方が負けとなる。
        /// (注) このルールは、日本将棋連盟がアマチュアの公式戦で使用しているものである。
        /// </remarks>
        public bool IsNyuugyokuWin() {
            if (Checked) {
                return false; // 王手がかかっていたら勝ちではない。
            }
            if (3 < Board.GetRank(king[Turn], Turn)) {
                return false; // 玉が敵陣３段目以内になければ勝ちではない。
            }

            BoardPoint point = AddProperty<BoardPoint>();

            if (point.GetCount(Turn) < 10) {
                return false; // 敵陣３段目以内にある駒が１０枚未満なら勝ちではない。
            }
            if (point.GetPoint(Turn) < (28 - Turn)) { // (Turn == 0 ? 28 : 27)
                return false; // 先手の場合、得点が２８点未満なら勝ちではない。
            }
            return true; // 全ての条件を満たしていれば勝ち。
        }

        /// <summary>
        /// 手を適用したときのハッシュと持ち駒値を算出して返す。
        /// </summary>
        public void GetHash(Move move, out ulong hash, out uint hand) {
            hash = HashValue;
            if (move.IsPut) {
                hash ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                hash ^= HashSeed.Seed[(byte)PieceUtility.SelfOrEnemy[Turn * 32 + (byte)move.PutPiece]][move.To];
            } else {
                hash ^= HashSeed.Seed[(byte)board[move.From]][move.From];
                hash ^= HashSeed.Seed[(byte)board[move.To]][move.To];
                hash ^= HashSeed.Seed[(byte)Piece.EMPTY][move.From];
                hash ^= HashSeed.Seed[(byte)(board[move.From] | move.Promote)][move.To];
            }
            hash ^= HashSeed.TurnSeed[1]; // ターン反転
            hand = HandValues[Turn ^ 1]; // 相手の方の値なので変わらない。
        }

        /// <summary>
        /// 手を適用したときのハッシュ値を算出して返す。
        /// </summary>
        public ulong GetFullHash(Move move) {
            ulong hash;
            uint hand;
            GetHash(move, out hash, out hand);
            return hash ^ hand;
        }

        #region IBoardProperty

        List<IBoardProperty> properties = new List<IBoardProperty>();

        /// <summary>
        /// 全部削除
        /// </summary>
        public void ClearProperties() {
            foreach (var prop in properties) prop.Detach(this);
            properties.Clear();
        }

        /// <summary>
        /// 追加。
        /// </summary>
        /// <param name="prop">追加するアイテム</param>
        /// <returns>既にあればそれ、無ければprop</returns>
        public IBoardProperty AddProperty(IBoardProperty prop) {
            if (prop == null) {
                throw new ArgumentNullException("prop");
            }
            // Equalsなのがあればそれを返す
            for (int i = 0, n = properties.Count; i < n; i++) {
                if (properties[i].Equals(prop)) {
                    return properties[i];
                }
            }
            // 無ければ追加
            prop.Attach(this);
            properties.Add(prop);
            return prop;
        }

        /// <summary>
        /// 追加その2。
        /// </summary>
        /// <typeparam name="T">追加するアイテムの型</typeparam>
        /// <returns>追加した、あるいは既にあったアイテムへの参照</returns>
        public T AddProperty<T>() where T : class, IBoardProperty, new() {
            // 同じ型のがあればそれを返す
            for (int i = 0, n = properties.Count; i < n; i++) {
                T item = properties[i] as T;
                if (item != null) return item;
            }
            // 無ければ追加
            {
                T item = new T();
                item.Attach(this);
                properties.Add(item);
                return item;
            }
        }

        #endregion

        #region デバッグ用プロパティ

        /// <summary>
        /// 利きの文字列化
        /// </summary>
        public string DebugDumpControl {
            get {
                StringBuilder str = new StringBuilder();
                for (int y = 0; y < 9; y++) {
                    for (int x = 0; x < 9; x++) {
                        int file = (9 - x) * 0x10, rank = y + 1 + Padding;
                        str.Append(
                            control[0][file + rank] != 0 &&
                            control[1][file + rank] != 0 ? "×" :
                            control[0][file + rank] != 0 ? "○" :
                            control[1][file + rank] != 0 ? "●" : "　");
                    }
                    str.AppendLine();
                }
                return str.ToString();
            }
        }
        /// <summary>
        /// 利きの文字列化
        /// </summary>
        public string DebugDumpControlIndirect {
            get {
                StringBuilder str = new StringBuilder();
                for (int y = 0; y < 9; y++) {
                    for (int x = 0; x < 9; x++) {
                        int file = (9 - x) * 0x10, rank = y + 1 + Padding;
                        str.Append(
                            BoardUtility.GetControl(board, 0, file + rank) != 0 &&
                            BoardUtility.GetControl(board, 1, file + rank) != 0 ? "×" :
                            BoardUtility.GetControl(board, 0, file + rank) != 0 ? "○" :
                            BoardUtility.GetControl(board, 1, file + rank) != 0 ? "●" : "　");
                    }
                    str.AppendLine();
                }
                return str.ToString();
            }
        }

        /// <summary>
        /// デバッガのテキストビジュアライザで見たいとき用。
        /// 関数よりプロパティの方が楽なので(´ω`)
        /// </summary>
        public string DebugToString {
            get { return ToString(); }
        }
        /// <summary>
        /// DebugToString()のToCSAStandard()版。
        /// 改行コードをLFからCR+LFに変換するので注意。
        /// </summary>
        public string DebugToCSAStandard {
            get { return ToCSAStandard().Replace("\n", Environment.NewLine); }
        }

        #endregion

        #region 指し手の生成

        /// <summary>
        /// 手の種類。bit flag化した方がいい気もしつつも、
        /// 個別に高速化したくて、網羅するのも面倒なので…。
        /// </summary>
        public enum MoveType {
            All,                // 飛車とか不成を含めない通常探索用の全部。
            MoveOnly,           // Allの打つ手無し版。
            AllWithNoPromote,   // 飛車とかの不成も含めて全部。
            CaptureOnly,        // 取る手のみ。
            NotCapture,          // 取らない手のみ。(AllのうちCaptureOnly以外)
            CaptureOrPromote,   // 取る手と成る手。
            BonanzaTacticals,   // 取る手と成る手と王の移動。
            Tacticals,          // MoveOnly + 王手な打つ手
        }

        /// <summary>
        /// 合法な手を生成
        /// </summary>
        /// <param name="moves">出力先。中身が入ってたら空にする。</param>
        /// <param name="buffer">出力処理用バッファ。</param>
        public MoveList GetMovesSafe(MoveType moveType = MoveType.All, MoveList moves = null, Move[] buffer = null) {
            if (buffer == null) {
                buffer = new Move[MaxLegalMoves];
            } else {
                Debug.Assert(MaxLegalMoves <= buffer.Length, "バッファ長が足りない");
            }

            int count = GetMoves(buffer, 0, moveType);
            if (moves == null) {
                moves = new MoveList(count);
            } else {
                // 中身が入ってたら空にする
                moves.Clear();
                if (moves.Capacity < count) {
                    moves.Capacity = count;
                }
            }

            for (int i = 0; i < count; i++) {
                Move move = buffer[i];
                if (IsMateByPutFU(move) || IsSuicideMove(move)) { // 打ち歩詰 or pinの自殺手
                    // 不正な手
                } else {
                    Debug.Assert(IsLegalMove(ref move), "不正な手: " + GetIllegalReason(move));
                    moves.Add(move);
                }
            }
            return moves;
        }

        /// <summary>
        /// 合法な手(但し、打ち歩詰め・連続王手の千日手も含む)を生成
        /// </summary>
        /// <param name="moves">指し手を書き込むバッファ</param>
        /// <param name="index">書き込む開始index</param>
        /// <param name="moveType">生成する指し手の種類</param>
        /// <remarks>index + 書き込んだ個数</remarks>
        public int GetMoves(Move[] moves, int index, MoveType moveType) {
            fixed (Move* p = moves) {
                return GetMoves(p, index, moveType);
            }
        }

        /// <summary>
        /// 合法な手(但し、打ち歩詰め・連続王手の千日手も含む)を生成
        /// </summary>
        /// <param name="moves">指し手を書き込むバッファ</param>
        /// <param name="index">書き込む開始index</param>
        /// <param name="moveType">生成する指し手の種類</param>
        /// <remarks>index + 書き込んだ個数</remarks>
        public int GetMoves(Move* moves, int index, MoveType moveType) {
            int oldIndex = index;
#if false // デバッグ用
            if (Turn == 0) {
                using (Board temp = ReverseClone()) {
                    var tempMoves = temp.GetMoves(moveType);
                    var tempMoves2 = new MoveList(tempMoves.Count);
                    for (int i = 0, n = tempMoves.Count; i < n; i++) {
                        tempMoves2.Add(tempMoves[i].Reverse());
                    }
                    return tempMoves2;
                }
            }
#endif

            Debug.Assert(kingControl == (king[Turn] != 0 ? GetControl(Turn ^ 1, king[Turn]) : 0));
            if (kingControl != 0) { // 王手されている時
                // 王手を受ける手を生成
                index = MakeAntiCheck(moves, index, kingControl, moveType);
                Debug.Assert(new MoveListRefPtr(moves, oldIndex, index - oldIndex).TrueForAll(IsCorrectMove), "王手を受ける手の生成で非合法手を生成");
            } else if (moveType == MoveType.CaptureOnly) {
                // 取る手のみ生成
                for (int file = 0x10; file <= 0x90; file += 0x10) {
                    for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                        int pos = file + rank;
                        // 自分の利きのある敵の駒に
                        if (ControlExists(Turn, pos) && PieceUtility.IsEnemy(board[pos], Turn)) {
                            index = MakeMoveTo(moves, index, pos);
                        }
                    }
                }
                Debug.Assert(new MoveListRefPtr(moves, oldIndex, index - oldIndex).TrueForAll(x => x.IsCapture), "取る手の生成で取らない手を生成");
                Debug.Assert(new MoveListRefPtr(moves, oldIndex, index - oldIndex).TrueForAll(IsCorrectMove), "取る手の生成で非合法手を生成");
            } else if (moveType == MoveType.CaptureOrPromote ||
                moveType == MoveType.BonanzaTacticals) {
                // 盤上の駒を動かす手
                if (moveType == MoveType.BonanzaTacticals) {
                    for (int file = 0x10; file <= 0x90; file += 0x10) {
                        for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                            int pos = file + rank;
                            if (PieceUtility.IsSelf(board[pos], Turn)) { // 取る手と成る手
                                Piece p = board[pos] & ~Piece.ENEMY;
                                if (p == Piece.OU) { // 王の移動
                                    index = MakeMoveKing(moves, index, kingControl);
                                } else {
                                    index = MakePieceMoveCaptureOrPromote(moves, index, pos);
                                }
                            }
                        }
                    }
                    Debug.Assert(new MoveListRefPtr(moves, oldIndex, index - oldIndex).TrueForAll(x => (x.Capture | x.Promote) != 0 || board[x.From] == PieceUtility.SelfOrEnemy[Turn * 32 + (byte)Piece.OU]), "取る手成る手王の移動の生成で謎の手を生成");
                } else {
                    for (int file = 0x10; file <= 0x90; file += 0x10) {
                        for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                            int pos = file + rank;
                            if (PieceUtility.IsSelf(board[pos], Turn)) { // 取る手と成る手
                                index = MakePieceMoveCaptureOrPromote(moves, index, pos);
                            }
                        }
                    }
                    Debug.Assert(new MoveListRefPtr(moves, oldIndex, index - oldIndex).TrueForAll(x => (x.Capture | x.Promote) != 0), "取る手成る手の生成で謎の手を生成");
                }
                Debug.Assert(new MoveListRefPtr(moves, oldIndex, index - oldIndex).TrueForAll(IsCorrectMove), "取る手成る手の生成で非合法手を生成");
            } else {
                Debug.Assert(moveType == MoveType.All ||
                    moveType == MoveType.NotCapture ||
                    moveType == MoveType.MoveOnly ||
                    moveType == MoveType.AllWithNoPromote ||
                    moveType == MoveType.Tacticals);
                if (moveType == MoveType.NotCapture) {
                    // 取らない手
                    for (int file = 0x10; file <= 0x90; file += 0x10) {
                        for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                            int pos = file + rank;
                            if (PieceUtility.IsSelf(board[pos], Turn)) {
                                Piece p = board[pos] & ~Piece.ENEMY;
                                if (p == Piece.OU) { // 王の移動
                                    index = MakeMoveKingNotCapture(moves, index, kingControl);
                                } else {
                                    index = MakePieceMoveNotCapture(moves, index, pos);
                                }
                            }
                        }
                    }
                } else {
                    // 盤上の駒を動かす手
                    if (Turn == 0) { // 先手 (PieceUtility.IsSelf()代わりに展開してみる)
                        for (int file = 0x10; file <= 0x90; file += 0x10) {
                            for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                                int pos = file + rank;
                                Piece p = board[pos];
                                if (Piece.FU <= p && p <= Piece.RY) {
                                    /*
                                    index = MakePieceMove_Switch(moves, index, pos, moveType);
                                    /*/
                                    index = MakePieceMoveTable[(byte)p](moves, index, pos, moveType);
                                    //*/
                                }
                            }
                        }
                    } else { // 後手
                        for (int file = 0x10; file <= 0x90; file += 0x10) {
                            for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                                int pos = file + rank;
                                Piece p = board[pos];
                                if ((p & Piece.ENEMY) != 0) { // && p != Piece.WALL
                                    /*
                                    index = MakePieceMove_Switch(moves, index, pos, moveType);
                                    /*/
                                    index = MakePieceMoveTable[(byte)p](moves, index, pos, moveType);
                                    //*/
                                }
                            }
                        }
                    }
                }
                // 持ち駒を打つ手
                if (moveType == MoveType.Tacticals) {
                    index = MakePutCheck(moves, index, index);
                } else if (moveType != MoveType.MoveOnly) {
                    /*
                    index = MakeLegalPuts(moves, index);
                    /*/
                    var HandValue = this.HandValue;
                    int hasFU = (HandValue & HandValueMaskFU) == 0 ? 0 : 1;
                    int hasKY = (HandValue & HandValueMaskKY) == 0 ? 0 : 2;
                    int hasKE = (HandValue & HandValueMaskKE) == 0 ? 0 : 4;
                    int others =
                        ((HandValue & HandValueMaskGI) == 0 ? 0 : 8) +
                        ((HandValue & HandValueMaskKI) == 0 ? 0 : 8) +
                        ((HandValue & HandValueMaskKA) == 0 ? 0 : 8) +
                        ((HandValue & HandValueMaskHI) == 0 ? 0 : 8);
                    index = MakePutTable[hasFU | hasKY | hasKE | others](moves, index);
                    //*/
                }
#if DEBUG
                for (int i = oldIndex; i < index; i++) {
                    if (!IsCorrectMove(moves[i]) && moves[i].PutPiece != Piece.FU) {
                        Debug.Fail("合法手の生成で非合法手を生成: " + GetIllegalReason(moves[i]));
                    }
                }
#endif
            }

            return index;
        }

        #endregion

        /// <summary>
        /// 王手防ぎ手の生成
        /// </summary>
        /// <param name="moves">配列</param>
        /// <param name="index">開始index</param>
        /// <returns>終了index</returns>
        public int GetAntiChecks(Move[] moves, int index) {
            fixed (Move* p = moves) {
                return GetAntiChecks(moves, index);
            }
        }

        /// <summary>
        /// 王手防ぎ手の生成
        /// </summary>
        /// <param name="moves">配列</param>
        /// <param name="index">開始index</param>
        /// <returns>終了index</returns>
        public int GetAntiChecks(Move* moves, int index) {
            if (kingControl == 0) {
                throw new InvalidOperationException();
            }
            return MakeAntiCheck(moves, index, kingControl, MoveType.AllWithNoPromote);
        }

        #region 指し手の生成の実装

        /// <summary>
        /// 打つ手の生成
        /// </summary>
        [Obsolete]
        private int MakeLegalPuts(Move* moves, int index) {
            var HandValue = this.HandValue;
            if (HandValue == 0) return index;

            var Turn = this.Turn;

            // ↓一応ほんのり早くなるのでキャッシュしてみる
            bool* rankPutFUTable = stackalloc bool[10];
            bool* rankPutKYTable = stackalloc bool[10];
            bool* rankPutKETable = stackalloc bool[10];
            for (int r = 0 + 1; r < 10; r++) {
                int rankR = Board.RankReverse(r, Turn);
                rankPutFUTable[r] = 2 <= rankR && (HandValue & HandValueMaskFU) != 0;
                rankPutKYTable[r] = 2 <= rankR && (HandValue & HandValueMaskKY) != 0;
                rankPutKETable[r] = 3 <= rankR && (HandValue & HandValueMaskKE) != 0;
            }

            for (int file = 0x10; file <= 0x90; file += 0x10) {
                bool existsFU = this.existsFU[Turn * 16 + file / 0x10];
                if (existsFU && (HandValue & ~HandValueMaskFU) == 0) continue; // 歩しか無いなら飛ばす。
                for (int rank = 1 + Padding; rank <= 9 + Padding; rank++) {
                    if (board[file + rank] != Piece.EMPTY) continue;
                    // 歩
                    if (rankPutFUTable[rank - Padding] && !existsFU) {
                        // && !IsMateByPutFU(toFile + toRank)
                        moves[index++] = Move.CreatePut(Piece.FU, file + rank);
                    }
                    // 香
                    if (rankPutKYTable[rank - Padding]) {
                        moves[index++] = Move.CreatePut(Piece.KY, file + rank);
                    }
                    // 桂
                    if (rankPutKETable[rank - Padding]) {
                        moves[index++] = Move.CreatePut(Piece.KE, file + rank);
                    }
                    // 銀金角飛
                    if ((HandValue & HandValueMaskGI) != 0) moves[index++] = Move.CreatePut(Piece.GI, file + rank);
                    if ((HandValue & HandValueMaskKI) != 0) moves[index++] = Move.CreatePut(Piece.KI, file + rank);
                    if ((HandValue & HandValueMaskKA) != 0) moves[index++] = Move.CreatePut(Piece.KA, file + rank);
                    if ((HandValue & HandValueMaskHI) != 0) moves[index++] = Move.CreatePut(Piece.HI, file + rank);
                }
            }
            return index;
        }

        /// <summary>
        /// 各駒の取る手と成る手の生成
        /// </summary>
        private int MakePieceMoveCaptureOrPromote(Move* moves, int index, int from) {
            Piece p = board[from] & ~Piece.ENEMY;
            if (p == Piece.OU) {
                for (int dir = 0; dir < 8; dir++) {
                    int to = from + Board.Direct[Turn * 16 + dir];
                    if (PieceUtility.IsEnemy(board[to], Turn) &&
                        control[Turn ^ 1][to] == 0 && // 敵の駒が効いていない
                        (kingControl & (1 << (17 - dir))) == 0) { // 敵の飛駒で貫かれていない
                        // 王で取る手の生成
                        moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
                    }
                }
            } else {
                Debug.Assert((PieceUtility.IsSecondTurn(board[from]) ? 1 : 0) == Turn, "敵駒を動かす手の生成？");
                foreach (var dir in PieceUtility.CanJumpDirects[(byte)p]) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    // 空白の間動く手を生成
                    int i;
                    for (i = diff; board[from + i] == Piece.EMPTY; i += diff) {
                        index = InternalMakeMoveCaptureOrPromote(moves, index, from, i);
                    }
                    // 敵の駒があるならそこへ動く
                    if (PieceUtility.IsEnemy(board[from + i], Turn)) {
                        index = InternalMakeMoveCaptureOrPromote(moves, index, from, i);
                    }
                }
                foreach (var dir in PieceUtility.CanMoveDirects[(byte)p]) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    if (PieceUtility.IsMovable(board[from + diff], Turn)) {
                        index = InternalMakeMoveCaptureOrPromote(moves, index, from, diff);
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// 玉の取らない手の生成
        /// </summary>
        private int MakeMoveKingNotCapture(Move* moves, int index, int kingControl) {
            int from = king[Turn];
            for (int dir = 0; dir < 8; dir++) {
                int diff = Board.Direct[Turn * 16 + dir];
                int to = from + diff;
                if (board[to] == Piece.EMPTY && // 移動可能＆取る手ではない手
                    control[Turn ^ 1][to] == 0 && // 敵の駒が効いていない
                    (kingControl & (1 << (17 - dir))) == 0) { // 敵の飛駒で貫かれていない
                    // 成らない手を生成
                    moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
                }
            }
            return index;
        }

        /// <summary>
        /// 各駒の取らない手の生成
        /// </summary>
        private int MakePieceMoveNotCapture(Move* moves, int index, int from) {
            Piece p = board[from] & ~Piece.ENEMY;
            foreach (var dir in PieceUtility.CanJumpDirects[(byte)p]) {
                int diff = Board.Direct[Turn * 16 + dir];
                // 空白の間動く手を生成
                for (int to = from + diff; board[to] == Piece.EMPTY; to += diff) {
                    index = InternalMakeMove(moves, index, from, to);
                }
            }
            foreach (var dir in PieceUtility.CanMoveDirects[(byte)p]) {
                int to = from + Board.Direct[Turn * 16 + dir];
                if (board[to] == Piece.EMPTY) {
                    // 空白へ動く手を生成
                    index = InternalMakeMove(moves, index, from, to);
                }
            }
            return index;
        }

        /// <summary>
        /// 盤上の駒を動かす
        /// </summary>
        private int MakeMove(Move* moves, int index, int from, int to, MoveType moveType) {
            if (PieceUtility.IsMovable(board[to], Turn)) {
                int toRank;

                if (PieceUtility.CanPromote(board[from]) &&
                    ((toRank = Board.GetRank(to, Turn)) <= 3 ||
                    Board.GetRank(from, Turn) <= 3)) {
                    if (moveType == MoveType.AllWithNoPromote ?
                        ShouldGenOnlyPromote(toRank, board[from]) :
                        (PieceUtility.AlwaysShouldPromote(board[from]) ||
                            (toRank <= 2 && PieceUtility.IsSmallPiece(board[from])))) {
                        // 成る手を生成
                        moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
                    } else {
                        // 成る手と成らない手を生成
                        Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
                        moves[index++] = move;
                        moves[index++] = move.ToNotPromote;
                    }
                } else {
                    // 成らない手を生成
                    moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
                }
            }
            return index;
        }

        /// <summary>
        /// 盤上の駒を動かす (IsMovableチェック無しバージョン)
        /// </summary>
        private int InternalMakeMove(Move* moves, int index, int from, int to) {
            int toRank;

            if (PieceUtility.CanPromote(board[from]) &&
                ((toRank = Board.GetRank(to, Turn)) <= 3 ||
                Board.GetRank(from, Turn) <= 3)) {
                if (PieceUtility.AlwaysShouldPromote(board[from]) ||
                    (toRank <= 2 && PieceUtility.IsSmallPiece(board[from]))) {
                    // 成る手を生成
                    moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
                } else {
                    // 成る手と成らない手を生成
                    Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
                    moves[index++] = move;
                    moves[index++] = move.ToNotPromote;
                }
            } else {
                // 成らない手を生成
                moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
            }
            return index;
        }

        /// <summary>
        /// ルール上必ず必要があるならtrue
        /// </summary>
        private static bool ShouldGenOnlyPromote(int toRank, Piece piece) {
            // 2段目以下の桂馬 or 1段目の歩・香
            if (toRank <= 2) {
                Piece p = piece & ~Piece.ENEMY;
                if (p == Piece.KE) return true;
                if (p <= Piece.KY) {
                    return toRank <= 1;
                }
            }
            return false;
        }

        /// <summary>
        /// 盤上の駒を動かす
        /// </summary>
        private int InternalMakeMoveCaptureOrPromote(Move* moves, int index, int from, int diff) {
            Debug.Assert(diff != 0);
            Debug.Assert(diff <= (int)sbyte.MinValue || (int)sbyte.MaxValue <= diff ||
                Board.GetDirectIndex(NegativeByTurn(diff, Turn)) < 0 ||
                PieceUtility.CanMove(board[from], Board.GetDirectIndex(NegativeByTurn(diff, Turn))),
                "移動できないとこに移動させようとしてる？");

            int to = from + diff;

            int toRank;

            if (PieceUtility.CanPromote(board[from]) &&
                ((toRank = Board.GetRank(to, Turn)) <= 3 ||
                Board.GetRank(from, Turn) <= 3)) {
                if (PieceUtility.AlwaysShouldPromote(board[from]) ||
                    (toRank <= 2 && PieceUtility.IsSmallPiece(board[from]))) {
                    // 成る手を生成
                    moves[index++] = Move.CreateMove(board, from, to, Piece.PROMOTED);
                } else {
                    // 成る手と成らない手を生成
                    Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
                    moves[index++] = move;
                    if (board[to] != Piece.EMPTY) {
                        moves[index++] = move.ToNotPromote;
                    }
                }
            } else {
                // 成らない手を生成
                if (board[to] != Piece.EMPTY) {
                    moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
                }
            }
            return index;
        }

        /// <summary>
        /// 玉を動かす手を生成
        /// </summary>
        private int MakeMoveKing(Move* moves, int index, int kingControl) {
            int from = king[Turn];
            for (int dir = 0; dir < 8; dir++) {
                int diff = Board.Direct[Turn * 16 + dir];
                int to = from + diff;
                if (PieceUtility.IsMovable(board[to], Turn) && // 移動可能
                    control[Turn ^ 1][to] == 0 && // 敵の駒が効いていない
                    (kingControl & (1 << (17 - dir))) == 0) { // 敵の飛駒で貫かれていない
                    // 成らない手を生成
                    moves[index++] = Move.CreateMove(board, from, to, Piece.EMPTY);
                }
            }
            return index;
        }

        /// <summary>
        /// 王手を受ける手を生成
        /// </summary>
        private int MakeAntiCheck(Move* moves, int index, int kingControl, MoveType moveType) {
            if ((kingControl & (kingControl - 1)) == 0) {  // 通常の王手 (立ってるビットの数が1個)
                int id; // 方向のインデックス
                for (id = 0; id <= 17; id++) {
                    if (kingControl == (1 << id)) break;
                }
                int check;  // 王手してきた駒の位置
                int diff; // 方向
                if (id < 10) {
                    diff = Board.Direct[Turn * 16 + id];
                    check = king[Turn] + diff;
                    Debug.Assert(PieceUtility.CanMove(board[check], id), "利きから王手駒の取得に失敗: " + board[check]);
                } else {
                    diff = Board.Direct[Turn * 16 + id - 10];
                    check = BoardUtility.SearchNotEmpty(board, king[Turn], diff);
                    Debug.Assert(PieceUtility.CanJump(board[check], id - 10), "利きから王手駒の取得に失敗: " + board[check]);
                }
                // 王手駒を取る
                index = MakeMoveToNotKing(moves, index, check, moveType);
                // 玉を動かす
                index = MakeMoveKing(moves, index, kingControl);

                if (10 <= id) {
                    // 合駒をする手を生成
                    for (int pos = king[Turn] + diff; board[pos] == Piece.EMPTY; pos += diff) {
                        index = MakeMoveToNotKing(moves, index, pos, moveType); // 移動合
                    }
                    for (int pos = king[Turn] + diff; board[pos] == Piece.EMPTY; pos += diff) {
                        index = MakePutTo(moves, index, pos); // 駒を打つ合
                    }
                }
            } else {
                // 両王手は玉を動かすしかない
                index = MakeMoveKing(moves, index, kingControl);
            }
            return index;
        }

        /// <summary>
        /// ある位置へ移動する手を生成
        /// </summary>
        private int MakeMoveTo(Move* moves, int index, int to) {
            int control = GetControl(Turn, to);
            if (control == 0) return index;

            for (int dir = 0; dir < 8; dir++) {
                if ((control & (1 << (dir + 10))) != 0) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    int from = BoardUtility.SearchNotEmpty(board, to, -diff);
                    Debug.Assert(PieceUtility.IsSelf(board[from], Turn) && PieceUtility.CanJump(board[from], dir));
                    int oldIndex = index;
                    index = MakeMove(moves, index, from, to, MoveType.All);
                    Debug.Assert(oldIndex == index || moves[index - 1].To == to, "MakeMove()がおかしい?");
                } else if ((control & (1 << dir)) != 0) {
                    int from = to - Board.Direct[Turn * 16 + dir];
                    Piece p = board[from];
                    if ((p & ~Piece.ENEMY) != Piece.OU || !ControlExists(Turn ^ 1, to)) {
                        Debug.Assert(PieceUtility.IsSelf(p, Turn) && PieceUtility.CanMove(p, dir), "利きのデータがおかしい？: " + p);
                        int oldIndex = index;
                        index = MakeMove(moves, index, from, to, MoveType.All);
                        Debug.Assert(oldIndex == index || moves[index - 1].To == to, "MakeMove()がおかしい?");
                    }
                }
            }
            for (int dir = 8; dir < 10; dir++) {
                if ((control & (1 << dir)) != 0) {
                    int from = to - Board.Direct[Turn * 16 + dir];
                    Piece p = board[from];
                    // if ((p & ~Piece.ENEMY) != Piece.OU || !ControlExists(Turn ^ 1, to)) {
                    Debug.Assert(PieceUtility.IsSelf(p, Turn) && PieceUtility.CanMove(p, dir), "利きのデータがおかしい？: " + p);
                    int oldIndex = index;
                    index = MakeMove(moves, index, from, to, MoveType.All);
                    Debug.Assert(oldIndex == index || moves[index - 1].To == to, "MakeMove()がおかしい?");
                }
            }
            return index;
        }

        /// <summary>
        /// ある位置へ玉以外の駒が移動する手を生成
        /// </summary>
        private int MakeMoveToNotKing(Move* moves, int index, int to, MoveType moveType) {
            int control = GetControl(Turn, to);
            if (control == 0) return index;

            for (int dir = 0; dir < 10; dir++) {
                if ((control & (1 << (dir + 10))) != 0) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    int from = BoardUtility.SearchNotEmpty(board, to, -diff);
                    Debug.Assert(PieceUtility.IsSelf(board[from], Turn) && PieceUtility.CanJump(board[from], dir));
                    int oldIndex = index;
                    index = MakeMove(moves, index, from, to, moveType);
                    Debug.Assert(oldIndex == index || moves[index - 1].To == to, "MakeMove()がおかしい?");
                } else if ((control & (1 << dir)) != 0) {
                    int from = to - Board.Direct[Turn * 16 + dir];
                    Piece p = board[from];
                    if ((p & ~Piece.ENEMY) != Piece.OU) {
                        Debug.Assert(PieceUtility.IsSelf(p, Turn) && PieceUtility.CanMove(p, dir), "利きのデータがおかしい？: " + p);
                        int oldIndex = index;
                        index = MakeMove(moves, index, from, to, moveType);
                        Debug.Assert(oldIndex == index || moves[index - 1].To == to, "MakeMove()がおかしい?");
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// ある位置へ合駒を打つ手を生成
        /// </summary>
        private int MakePutTo(Move* moves, int index, int to) {
            Debug.Assert(board[to] == Piece.EMPTY, "空いてないとこへの合駒？");
            var HandValue = this.HandValue;
            int rank = Board.GetRank(to, Turn);
            // 歩を打つ
            if ((HandValue & HandValueMaskFU) != 0 && 2 <= rank) {
                if (!ExistsFU(Turn, Board.GetFile(to)) /*&& !IsMateByPutFU(to)*/) {
                    moves[index++] = Move.CreatePut(Piece.FU, to);
                }
            }
            // 香を打つ
            if ((HandValue & HandValueMaskKY) != 0 && 2 <= rank) {
                moves[index++] = Move.CreatePut(Piece.KY, to);
            }
            // 桂を打つ
            if ((HandValue & HandValueMaskKE) != 0 && 3 <= rank) {
                moves[index++] = Move.CreatePut(Piece.KE, to);
            }
            // 金銀飛角を打つ
            if ((HandValue & HandValueMaskGI) != 0) { moves[index++] = Move.CreatePut(Piece.GI, to); }
            if ((HandValue & HandValueMaskKI) != 0) { moves[index++] = Move.CreatePut(Piece.KI, to); }
            if ((HandValue & HandValueMaskKA) != 0) { moves[index++] = Move.CreatePut(Piece.KA, to); }
            if ((HandValue & HandValueMaskHI) != 0) { moves[index++] = Move.CreatePut(Piece.HI, to); }
            return index;
        }

        #endregion

        #region 王手の生成処理

        /// <summary>
        /// 王手な手の生成
        /// </summary>
        public MoveList GetChecks() {
            Move[] moves = new Move[256];
            int count = GetChecks(moves, 0);
            MoveList list = new MoveList(count);
            list.AddRange(moves.Take(count));
            return list;
        }

        /// <summary>
        /// 王手な手の生成
        /// </summary>
        public int GetChecks(Move[] moves, int index) {
            fixed (Move* p = moves) {
                return GetChecks(p, index);
            }
        }

        /// <summary>
        /// 王手な手の生成
        /// </summary>
        public int GetChecks(Move* moves, int index) {
            int kingE = king[Turn ^ 1];
            if (kingE == 0) return index;
            // 王手されてるときの王手生成
            if (Checked) {
                return GetChecksFromLegalMoves(moves, index);
            }
            // 王手生成
            int oldIndex = index;
            index = MakeCheck(moves, index);
            DebugGetChecks(moves, oldIndex, index);
            return index;
        }

        #endregion

        #region 王手の生成の実装

        /// <summary>
        /// 王手生成のデバッグ
        /// </summary>
        [Conditional("DEBUG")]
        private void DebugGetChecks(Move* moves, int oldIndex, int index) {
            for (int i = oldIndex; i < index; i++) {
                Move move = moves[i];
                if (move.PutPiece != Piece.FU) { // 歩打ちは面倒なので未チェック
                    Debug.Assert(IsCorrectMove(move), "王手生成でバグ？ (非合法): " + GetIllegalReason(move));
                }
                Debug.Assert(IsCheckMove(move), "王手生成でバグ？ (非王手)");
            }
            Debug.Assert(HashValue == CalculateHash(), "board破壊チェック");
        }

        /// <summary>
        /// GetLegalMoves()して、その中の王手じゃない手を除外したものを返す。
        /// </summary>
        private int GetChecksFromLegalMoves(Move* moves, int index) {
            int oldIndex = index;
            index = GetMoves(moves, index, MoveType.AllWithNoPromote);
            // 非王手の削除
            return MoveUtility.RemoveAll(moves, oldIndex, index, x => !IsCheckMove(x));
        }

        /// <summary>
        /// 王手の生成
        /// </summary>
        private int MakeCheck(Move* moves, int index) {
            Debug.Assert(!Checked);
            int startIndex = index;
            // 盤面の非飛び駒による王手
            index = MakeMoveCheck(moves, index, startIndex);
            // 持ち駒からの王手
            index = MakePutCheck(moves, index, startIndex);
            // 飛び駒の王手
            index = MakeProjectileCheck(moves, index, startIndex);
            return index;
        }

        /// <summary>
        /// 盤面の駒による王手
        /// </summary>
        private int MakeMoveCheck(Move* moves, int index, int startIndex) {
            int Turn = this.Turn;
            int kingE = king[Turn ^ 1];

            var bp = DangerousGetPtr();
            // 移動王手
            foreach (var fromDiff in GenerateCheckTable.MoveFrom) {
                int from = kingE + NegativeByTurn(fromDiff, Turn);
                if (PieceUtility.IsSelf(bp[from], Turn)) {
                    Piece pt = bp[from] & ~Piece.ENEMY;
                    var table = GenerateCheckTable.Move[fromDiff + GenerateCheckTable.MoveTableOffset][(byte)pt];
                    foreach (var entry in table) {
#if DEBUG
                        Debug.Assert(entry.From == fromDiff, "テーブルのバグ？");
#endif
                        int to = kingE + NegativeByTurn(entry.To, Turn);
                        if (entry.Promote == Piece.PROMOTED &&
                            3 < Board.GetRank(from, Turn) &&
                            3 < Board.GetRank(to, Turn)) continue; // 成らなければいけないのに成れない
                        if (PieceUtility.IsMovable(board[to], Turn)) {
                            // 空のマスか、敵の駒なら、駒を進められる。
                            index = AddCheck(moves, index, startIndex, Move.CreateMove(board, from, to, entry.Promote));
                            Debug.Assert(kingE + NegativeByTurn(entry.To, Turn) == to, "書き換え間違えた？");
                        }
                    }
                }
            }
            // 桂馬移動王手その1
            Piece selfKE = Piece.KE | (Piece)(Turn << PieceUtility.EnemyShift);
            for (int x = -0x20; x <= +0x20; x += 0x10) {
                int from = kingE + NegativeByTurn(x + 3, Turn);
                if (from < 0x11 + Padding || 0x99 + Padding < from) continue; // 盤外
                if (bp[from] == selfKE) {
                    foreach (var entry in GenerateCheckTable.MoveKE3[x / 0x10 + 2]) {
                        int to = kingE + NegativeByTurn(entry.To, Turn);
                        Debug.Assert(entry.Promote == Piece.PROMOTED, "テーブルが不正？");
                        if (3 < Board.GetRank(from, Turn) &&
                            3 < Board.GetRank(to, Turn)) continue; // 成らなければいけないのに成れない
                        if (PieceUtility.IsMovable(board[to], Turn)) {
                            // 空のマスか、敵の駒なら、駒を進められる。
                            index = AddCheck(moves, index, startIndex, Move.CreateMove(board, from, to, entry.Promote));
                            Debug.Assert(kingE + NegativeByTurn(entry.To, Turn) == to, "書き換え間違えた？");
                        }
                    }
                }
            }
            // 桂馬移動王手その2
            for (int x = -0x20; x <= +0x20; x += 0x20) {
                int from = kingE + NegativeByTurn(x + 4, Turn);
                if (from < 0x11 + Padding || 0x99 + Padding < from) continue; // 盤外
                if (bp[from] == selfKE) {
                    foreach (var entry in GenerateCheckTable.MoveKE4[x / 0x10 + 2]) {
                        int to = kingE + NegativeByTurn(entry.To, Turn);
                        Debug.Assert(entry.Promote == Piece.EMPTY, "テーブルが不正？");
                        if (PieceUtility.IsMovable(board[to], Turn)) {
                            // 空のマスか、敵の駒なら、駒を進められる。
                            index = AddCheck(moves, index, startIndex, Move.CreateMove(board, from, to, entry.Promote));
                            Debug.Assert(kingE + NegativeByTurn(entry.To, Turn) == to, "書き換え間違えた？");
                        }
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// 持ち駒からの王手
        /// </summary>
        private int MakePutCheck(Move* moves, int index, int startIndex) {
            var HandValue = this.HandValue;
            int kingE = king[Turn ^ 1];

            if ((HandValue & HandValueMaskKI) != 0) {
                foreach (var pcp in GenerateCheckTable.Put[(byte)Piece.KI]) {
                    int pos = NegativeByTurn(pcp, Turn);
                    int to = kingE + pos;
                    if (board[to] == Piece.EMPTY) {
                        index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.KI, to));
                    }
                }
            }
            if ((HandValue & HandValueMaskKE) != 0) {
                foreach (var pcp in GenerateCheckTable.Put[(byte)Piece.KE]) {
                    int pos = NegativeByTurn(pcp, Turn);
                    int to = kingE + pos;
                    if (board[to] == Piece.EMPTY) {
                        index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.KE, to));
                    }
                }
            }
            if ((HandValue & HandValueMaskGI) != 0) {
                foreach (var pcp in GenerateCheckTable.Put[(byte)Piece.GI]) {
                    int pos = NegativeByTurn(pcp, Turn);
                    int to = kingE + pos;
                    if (board[to] == Piece.EMPTY) {
                        index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.GI, to));
                    }
                }
            }
            if ((HandValue & HandValueMaskFU) != 0) {
                int to = kingE + NegativeByTurn(GenerateCheckTable.Put[(byte)Piece.FU][0], Turn);
                if (board[to] == Piece.EMPTY) {
                    if (!ExistsFU(Turn, Board.GetFile(to)) /*&& !IsMateByPutFU(to)*/) {
                        index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.FU, to));
                    }
                }
            }
            if ((HandValue & HandValueMaskKY) != 0) {
                int diff = NegativeByTurn(GenerateCheckTable.Put[(byte)Piece.KY][0], Turn);
                int to = kingE + diff;
                for (; board[to] == Piece.EMPTY; to += diff) {
                    index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.KY, to));
                }
            }
            if ((HandValue & HandValueMaskHI) != 0) {
                foreach (var pcp in GenerateCheckTable.Put[(byte)Piece.HI]) {
                    int diff = NegativeByTurn(pcp, Turn);
                    int to = kingE + diff;
                    for (; board[to] == Piece.EMPTY; to += diff) {
                        index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.HI, to));
                    }
                }
            }
            if ((HandValue & HandValueMaskKA) != 0) {
                foreach (var pcp in GenerateCheckTable.Put[(byte)Piece.KA]) {
                    int diff = NegativeByTurn(pcp, Turn);
                    int to = kingE + diff;
                    for (; board[to] == Piece.EMPTY; to += diff) {
                        index = AddCheck(moves, index, startIndex, Move.CreatePut(Piece.KA, to));
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// 飛び駒の道を空ける王手
        /// </summary>
        private int MakePieceMoveCheck(Move* moves, int index, int startIndex, int from, int Rpin) {
            Piece p = (board[from] & ~Piece.ENEMY);
            if (p == Piece.OU) {
                for (int dir = 0; dir < 8; dir++) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    if (diff == Rpin || diff == -Rpin) continue;
                    Piece piece = board[king[Turn] + diff];
                    if (PieceUtility.IsMovable(piece, Turn) &&
                        !ControlExists(Turn ^ 1, king[Turn] + diff)) { // 敵の駒が効いていない
                        index = MakeMoveCheck(moves, index, startIndex, king[Turn], diff);
                    }
                }
            } else {
                Debug.Assert((PieceUtility.IsSecondTurn(board[from]) ? 1 : 0) == Turn, "敵駒を動かす手の生成？");
                foreach (var dir in PieceUtility.CanJumpDirects[(byte)p]) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    if (diff == Rpin || diff == -Rpin) continue;
                    // 空白の間動く手を生成
                    int i;
                    for (i = diff; board[from + i] == Piece.EMPTY; i += diff) {
                        index = MakeMoveCheck(moves, index, startIndex, from, i);
                    }
                    // 敵の駒があるならそこへ動く
                    if (PieceUtility.IsEnemy(board[from + i], Turn)) {
                        index = MakeMoveCheck(moves, index, startIndex, from, i);
                    }
                }
                foreach (var dir in PieceUtility.CanMoveDirects[(byte)p]) {
                    int diff = Board.Direct[Turn * 16 + dir];
                    if (diff == Rpin || diff == -Rpin) continue;
                    index = MakeMoveCheck(moves, index, startIndex, from, diff);
                }
            }
            return index;
        }

        /// <summary>
        /// 盤上の駒を動かす王手
        /// </summary>
        private int MakeMoveCheck(Move* moves, int index, int startIndex, int from, int diff) {
            Debug.Assert(diff != 0);
            Debug.Assert(unchecked(diff <= (int)sbyte.MinValue || (int)sbyte.MaxValue <= diff ||
                Board.GetDirectIndex(NegativeByTurn(diff, Turn)) < 0 ||
                PieceUtility.CanMove(board[from], Board.GetDirectIndex(NegativeByTurn(diff, Turn)))),
                "移動できないとこに移動させようとしてる？");

            int to = from + diff;

            if (PieceUtility.IsMovable(board[to], Turn)) {
                int rank = Board.GetRank(to, Turn);
                int fromrank = Board.GetRank(from, Turn);

                if (PieceUtility.CanPromote(board[from]) && (fromrank <= 3 || rank <= 3)) {
                    if (rank <= 2 && PieceUtility.IsSmallPiece(board[from])) {
                        // 成る手を生成
                        index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.PROMOTED));
                    } else {
                        // 成る手と成らない手を生成
                        Move move = Move.CreateMove(board, from, to, Piece.PROMOTED);
                        index = AddCheckUnique(moves, index, startIndex, move);
                        index = AddCheckUnique(moves, index, startIndex, move.ToNotPromote);
                    }
                } else {
                    // 成らない手を生成
                    index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.EMPTY));
                }
            }
            return index;
        }

        /// <summary>
        /// 飛び道具による王手の生成
        /// </summary>
        private int MakeProjectileCheck(Move* moves, int index, int startIndex) {
            int startIndex2 = index;

            int Turn = this.Turn;
            int kingE = king[Turn ^ 1];
            int kingERank = Board.GetRank(kingE);
            int kingERankT = Board.RankReverse(kingERank, Turn);
            Piece selfUM = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)Piece.UM];
            Piece selfRY = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)Piece.RY];

            // 角の王手・角の道へ入る王手
            // 王から角の道を走査し、利きを見て角を見つける。
            foreach (var dir in PieceUtility.CanJumpDirects[(byte)Piece.KA]) {
                int diff = -Board.Direct[Turn * 16 + dir];
                int to = kingE + diff;
                for (; board[to] == Piece.EMPTY; to += diff) {
                    // 角・馬の利きがある(toへ移動出来る角・馬がある)場合
                    if ((control[Turn][to] & Board.ControlMaskJumpDiag) != 0) {
                        foreach (var dir2 in PieceUtility.CanJumpDirects[(byte)Piece.KA]) {
                            if ((control[Turn][to] & (1 << (dir2 + 10))) != 0) {
                                // 移動できる角・馬があった
                                int from = SearchNotEmpty(to, -Board.Direct[Turn * 16 + dir2]);
                                Debug.Assert((board[from] & ~Piece.PE) == Piece.KA, "利きが不正？");
                                Debug.Assert(PieceUtility.IsSelf(board[from], Turn), "利きが不正？");
                                // 手の生成
                                index = AddCheckPNP(moves, index, startIndex2, from, to);
                            }
                        }
                    }
                    // 馬の移動利きかもしれないものがある場合
                    if ((control[Turn][to] & Board.ControlMaskMoveOrtho) != 0) {
                        foreach (var dir2 in PieceUtility.CanMoveDirects[(byte)Piece.UM]) {
                            int from = to + -Board.Direct[Turn * 16 + dir2];
                            if (board[from] == selfUM) {
                                // 移動できる馬があった
                                index = AddCheck(moves, index, startIndex2, Move.CreateMove(board, from, to, Piece.EMPTY));
                            }
                        }
                    }
                }
                // 敵駒・味方駒に移動できる角がある場合
                if ((control[Turn][to] & Board.ControlMaskJumpDiag) != 0) {
                    if (PieceUtility.IsEnemy(board[to], Turn)) {
                        // 敵駒なら取る手を生成
                        foreach (var dir2 in PieceUtility.CanJumpDirects[(byte)Piece.KA]) {
                            if ((control[Turn][to] & (1 << (dir2 + 10))) != 0) {
                                // 移動できる角・馬があった
                                int from = SearchNotEmpty(to, -Board.Direct[Turn * 16 + dir2]);
                                Debug.Assert((board[from] & ~Piece.PE) == Piece.KA, "利きが不正？");
                                Debug.Assert(PieceUtility.IsSelf(board[from], Turn), "利きが不正？");
                                // 手の生成
                                index = AddCheckPNP(moves, index, startIndex2, from, to);
                            }
                        }
                    } else if (PieceUtility.IsSelf(board[to], Turn)) {
                        // 味方の駒ならその先に自分の角があればよせる手を生成
                        if ((control[Turn][to] & (1 << (dir + 10))) != 0) {
#if DEBUG
                            int from = SearchNotEmpty(to, diff);
                            Piece ps = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)(board[from] & ~Piece.PROMOTED)];
                            Debug.Assert(ps == Piece.KA, "利きが不正？");
#endif
                            index = MakePieceMoveCheck(moves, index, startIndex, to, diff);
                        }
                    }
                }
                // 馬の移動で敵駒を取る手
                if ((control[Turn][to] & Board.ControlMaskMoveOrtho) != 0 &&
                    PieceUtility.IsEnemy(board[to], Turn)) {
                    foreach (var dir2 in PieceUtility.CanMoveDirects[(byte)Piece.UM]) {
                        int from = to + -Board.Direct[Turn * 16 + dir2];
                        if (board[from] == selfUM) {
                            // 移動できる馬があった
                            index = AddCheck(moves, index, startIndex2, Move.CreateMove(board, from, to, Piece.EMPTY));
                        }
                    }
                }
            }

            // 飛車の王手・飛車の道へ入る王手
            foreach (var dir in PieceUtility.CanJumpDirects[(byte)Piece.HI]) {
                int diff = -Board.Direct[Turn * 16 + dir];
                int to = kingE + diff;
                for (; board[to] == Piece.EMPTY; to += diff) {
                    // 飛・龍・香の利きがある(toへ移動出来る飛・龍・香がある)場合
                    if ((control[Turn][to] & Board.ControlMaskJumpOrtho) != 0) {
                        // TODO: dirと垂直な2つだけでいいのだけど手抜き実装中
                        foreach (var dir2 in PieceUtility.CanJumpDirects[(byte)Piece.HI]) {
                            if ((control[Turn][to] & (1 << (dir2 + 10))) != 0) {
                                // 移動できる飛・龍・香があった
                                int from = SearchNotEmpty(to, -Board.Direct[Turn * 16 + dir2]);
                                Debug.Assert((board[from] & ~Piece.PE) == Piece.HI || (board[from] & ~Piece.PE) == Piece.KY, "利きが不正？");
                                Debug.Assert(PieceUtility.IsSelf(board[from], Turn), "利きが不正？");
                                // 手の生成
                                if ((board[from] & ~Piece.ENEMY) == Piece.KY && dir != 6) {
                                    // 香車は真下しか王手にならない
                                } else {
                                    index = AddCheckPNPKY(moves, index, startIndex2, from, to, diff);
                                }
                            }
                        }
                    }
                    // 龍の移動利きかもしれないものがある場合
                    if ((control[Turn][to] & Board.ControlMaskMoveDiag) != 0) {
                        foreach (var dir2 in PieceUtility.CanMoveDirects[(byte)Piece.RY]) {
                            int from = to + -Board.Direct[Turn * 16 + dir2];
                            if (board[from] == selfRY) {
                                // 移動できる龍があった
                                index = AddCheck(moves, index, startIndex2, Move.CreateMove(board, from, to, Piece.EMPTY));
                            }
                        }
                    }
                }
                // 敵駒・味方駒に移動できる飛・龍・香がある場合
                if ((control[Turn][to] & Board.ControlMaskJumpOrtho) != 0) {
                    if (PieceUtility.IsEnemy(board[to], Turn)) {
                        // 敵駒なら取る手を生成
                        // TODO: dirと垂直な2つだけでいいのだけど手抜き実装中
                        foreach (var dir2 in PieceUtility.CanJumpDirects[(byte)Piece.HI]) {
                            if ((control[Turn][to] & (1 << (dir2 + 10))) != 0) {
                                // 移動できる飛・龍・香があった
                                int from = SearchNotEmpty(to, -Board.Direct[Turn * 16 + dir2]);
                                Debug.Assert((board[from] & ~Piece.PE) == Piece.HI || (board[from] & ~Piece.PE) == Piece.KY, "利きが不正？");
                                Debug.Assert(PieceUtility.IsSelf(board[from], Turn), "利きが不正？");
                                // 手の生成
                                if ((board[from] & ~Piece.ENEMY) == Piece.KY && dir != 6) {
                                    // 香車は真下しか王手にならない
                                } else {
                                    index = AddCheckPNPKY(moves, index, startIndex2, from, to, diff);
                                }
                            }
                        }
                    } else if (PieceUtility.IsSelf(board[to], Turn)) {
                        // 味方の駒ならその先に自分の飛があればよせる手を生成
                        if ((control[Turn][to] & (1 << (dir + 10))) != 0) {
#if DEBUG
                            int from = SearchNotEmpty(to, diff);
                            Piece ps = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)(board[from] & ~Piece.PROMOTED)];
                            Debug.Assert(ps == Piece.HI || (ps == Piece.KY && dir == 6), "利きが不正？");
#endif
                            index = MakePieceMoveCheck(moves, index, startIndex, to, diff);
                        }
                    }
                }
                // 龍の移動で敵駒を取る手
                if ((control[Turn][to] & Board.ControlMaskMoveDiag) != 0 &&
                    PieceUtility.IsEnemy(board[to], Turn)) {
                    foreach (var dir2 in PieceUtility.CanMoveDirects[(byte)Piece.RY]) {
                        int from = to + -Board.Direct[Turn * 16 + dir2];
                        if (board[from] == selfRY) {
                            // 移動できる龍があった
                            index = AddCheck(moves, index, startIndex2, Move.CreateMove(board, from, to, Piece.EMPTY));
                        }
                    }
                }
            }

            // 角成り・馬王手
            foreach (var dir in PieceUtility.CanMoveDirects[(byte)Piece.UM]) {
                int to = kingE + Board.Direct[Turn * 16 + dir];
                if (!PieceUtility.IsMovable(board[to], Turn)) {
                    continue;
                }
                if ((control[Turn][to] & Board.ControlMaskJumpDiag) != 0) {
                    foreach (var dir2 in PieceUtility.CanJumpDirects[(byte)Piece.KA]) {
                        if ((control[Turn][to] & (1 << (dir2 + 10))) == 0) continue;
                        int diff = -Board.Direct[Turn * 16 + dir2];
                        int from = BoardUtility.SearchNotEmpty(board, to, diff);
                        Debug.Assert((board[from] & ~Piece.PE) == Piece.KA, "利きが不正？");
                        // 手の生成
                        index = AddCheckP(moves, index, startIndex2, from, to);
                    }
                }
            }
            // 飛車成り・龍王手
            foreach (var dir in PieceUtility.CanMoveDirects[(byte)Piece.RY]) {
                int to = kingE + Board.Direct[Turn * 16 + dir];
                if (!PieceUtility.IsMovable(board[to], Turn)) {
                    continue;
                }
                if ((control[Turn][to] & Board.ControlMaskJumpOrtho) != 0) {
                    foreach (var dir2 in PieceUtility.CanJumpDirects[(byte)Piece.HI]) {
                        if ((control[Turn][to] & (1 << (dir2 + 10))) == 0) continue;
                        int diff = -Board.Direct[Turn * 16 + dir2];
                        int from = BoardUtility.SearchNotEmpty(board, to, diff);
                        Debug.Assert((board[from] & ~Piece.PE) == Piece.HI || (board[from] & ~Piece.PE) == Piece.KY, "利きが不正？");
                        // 手の生成
                        if ((board[from] & ~Piece.PE) == Piece.KY) {
                            if (dir == 5 || dir == 7) {
                                // 香車は玉の斜め上は王手にならない。
                            } else {
                                index = AddCheckP(moves, index, startIndex2, from, to);
                                // 玉の真横もいけるなら生成
                                int diff6 = Board.Direct[Turn * 16 + 6];
                                if (board[to] == Piece.EMPTY &&
                                    PieceUtility.IsMovable(board[to + diff6], Turn)) {
                                    to += diff6;
                                    index = AddCheckP(moves, index, startIndex2, from, to);
                                }
                            }
                        } else {
                            index = AddCheckP(moves, index, startIndex2, from, to);
                        }
                    }
                }
            }
            // 香車の斜め下→真横の成り王手 (上ので漏れてるので… _no)
            if (kingERankT <= 3) {
                int* froms = stackalloc int[2];
                int* tos = stackalloc int[2];
                froms[0] = 0; // 左下
                froms[1] = 2; // 右下
                tos[0] = 3; // 左
                tos[1] = 4; // 右
                Piece selfKY = PieceUtility.SelfOrEnemy[Turn * 32 + (byte)Piece.KY];
                for (int i = 0; i < 2; i++) {
                    int from = kingE + Board.Direct[Turn * 16 + froms[i]];
                    if (board[from] == selfKY) {
                        int to = kingE + Board.Direct[Turn * 16 + tos[i]];
                        if (PieceUtility.IsMovable(board[to], Turn)) {
                            index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.PROMOTED));
                            // ↑角の道から避ける手とかで重複の可能性はある…。
                        }
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// 成れるなら成る手と成らない手と生成。(ただし、香車で成ると王手にならないのは除外)
        /// </summary>
        private int AddCheckPNPKY(Move* moves, int index, int startIndex, int from, int to, int diff) {
            // 成れるなら成る手を生成
            if ((board[from] & Piece.PROMOTED) == 0 &&
                (Board.GetRank(to, Turn) <= 3 ||
                Board.GetRank(from, Turn) <= 3)) {
                // 香車で玉の直下でない場合は成る手はダメ。
                if ((board[from] & ~Piece.ENEMY) == Piece.KY && to + -diff != king[Turn ^ 1]) {
                    // 生成しない
                    Debug.Assert(!IsCheckMove(Move.CreateMove(board, from, to, Piece.PROMOTED)), "ロジックミス？");
                } else {
                    index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.PROMOTED));
                }
            }
            // 成らない手を生成
            index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.EMPTY));
            return index;
        }

        /// <summary>
        /// 成る手・成り済みの駒の手の生成。
        /// </summary>
        private int AddCheckP(Move* moves, int index, int startIndex, int from, int to) {
            if ((board[from] & Piece.PROMOTED) == 0) {
                if (Board.GetRank(to, Turn) <= 3 ||
                    Board.GetRank(from, Turn) <= 3) {
                    // 成れるなら成る手を生成
                    index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.PROMOTED));
                }
            } else {
                // 既に成ってるなら動く手を生成
                index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.EMPTY));
            }
            return index;
        }

        /// <summary>
        /// 成れるなら成る手と成らない手と生成。
        /// </summary>
        private int AddCheckPNP(Move* moves, int index, int startIndex, int from, int to) {
            // 成れるなら成る手を生成
            if ((board[from] & Piece.PROMOTED) == 0 &&
                (Board.GetRank(to, Turn) <= 3 ||
                Board.GetRank(from, Turn) <= 3)) {
                index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.PROMOTED));
            }
            // 成らない手を生成
            index = AddCheckUnique(moves, index, startIndex, Move.CreateMove(board, from, to, Piece.EMPTY));
            return index;
        }

        /// <summary>
        /// 王手な手の追加 (重複チェック付き)
        /// </summary>
        private int AddCheckUnique(Move* moves, int index, int startIndex, Move move) {
            //if (0 <= Array.IndexOf(moves, move, startIndex, index - startIndex)) return index;
            for (int i = startIndex; i < index; i++) {
                if (moves[i] == move) return index;
            }
            return AddCheck(moves, index, startIndex, move);
        }

        /// <summary>
        /// 王手な手の追加
        /// </summary>
        private int AddCheck(Move* moves, int index, int startIndex, Move move) {
            Debug.Assert(move.PutPiece == Piece.FU || IsCorrectMove(move),
                "王手生成のバグ？: " + GetIllegalReason(move));
            Debug.Assert(IsCheckMove(move), "王手生成のバグ？ (非王手)");
            Debug.Assert((board[move.From] & Piece.PROMOTED) == 0 || !move.IsPromote, "王手生成のバグ？ (成り駒が成る手)");
            // *
#if DEBUG
            for (int i = startIndex; i < index; i++) {
                if (moves[i] == move) Debug.Fail("王手生成のバグ？ (重複)");
            }
#endif
            /*/
            if (0 <= Array.IndexOf(moves, move, startIndex, index - startIndex)) return;
            // */
            moves[index++] = move;
            return index;
        }

        #endregion

        /// <summary>
        /// デバッグ文字列
        /// </summary>
        public string DebugString {
            get { return new BoardDebugger().GetString(this); }
        }
    }
}
