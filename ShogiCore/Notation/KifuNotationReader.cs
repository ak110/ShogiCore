using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ShogiCore.Notation {
    /// <summary>
    /// .kif形式と.ki2形式。
    /// </summary>
    /// <remarks>
    /// ↓この辺が参考になるかも。
    /// <![CDATA[
    /// http://wiki.optus.nu/shogi/index.php?cache=1&lan=jp&post=%25B4%25FD%25C9%25E8%25A4%25CE%25B7%25C1%25BC%25B0%25A4%25CB%25A4%25C4%25A4%25A4%25A4%25C6
    /// ]]>
    /// </remarks>
    public class KifuNotationReader : StringNotationReader {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static Regex moveCountRegex = new Regex(@"手数[＝=](\d+)\s*(([▲△▼▽]\S+)\s*まで)?", RegexOptions.Compiled);

        /// <summary>
        /// 文字列化用テーブル
        /// </summary>
        public static readonly string[] NameTable = new string[] {
            " ・"," 歩"," 香"," 桂"," 銀"," 金"," 角"," 飛"," 王"," と"," 杏"," 圭"," 全"," 金"," 馬"," 龍",
            " ・","v歩","v香","v桂","v銀","v金","v角","v飛","v王","vと","v杏","v圭","v全","v金","v馬","v龍",
        };

        #region ReadPiece

        /// <summary>
        /// 駒の表現をparse
        /// </summary>
        public static Piece ReadPieceForBoardPiece(string line, int i) {
            switch (line[i]) {
                case '　':
                case '・': return Piece.EMPTY;
                case '歩': return Piece.FU;
                case '香': return Piece.KY;
                case '桂': return Piece.KE;
                case '銀': return Piece.GI;
                case '金': return Piece.KI;
                case '角': return Piece.KA;
                case '飛': return Piece.HI;
                case '王':
                case '玉': return Piece.OU;
                case 'と': return Piece.TO;
                case '杏': return Piece.NY;
                case '圭': return Piece.NK;
                case '全': return Piece.NG;
                case '馬': return Piece.UM;
                case '龍':
                case '竜': return Piece.RY;
                default:
                    throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
            }
        }

        /// <summary>
        /// 駒の表現をparse
        /// </summary>
        public static Piece ReadPieceForHand(char c) {
            switch (c) {
                case '歩': return Piece.FU;
                case '香': return Piece.KY;
                case '桂': return Piece.KE;
                case '銀': return Piece.GI;
                case '金': return Piece.KI;
                case '角': return Piece.KA;
                case '飛': return Piece.HI;
            }
            return Piece.EMPTY;
        }

        /// <summary>
        /// 駒の表現をparse
        /// </summary>
        public static Piece ReadPieceForMove(string pieceString) {
            int i = 0;
            return ReadPieceForMove(pieceString, ref i);
        }
        /// <summary>
        /// 駒の表現をparse
        /// </summary>
        public static Piece ReadPieceForMove(string line, ref int i) {
            switch (line[i++]) {
                case '　':
                case '・': return Piece.EMPTY;
                case '歩': return Piece.FU;
                case '香': return Piece.KY;
                case '桂': return Piece.KE;
                case '銀': return Piece.GI;
                case '金': return Piece.KI;
                case '角': return Piece.KA;
                case '飛': return Piece.HI;
                case '王':
                case '玉': return Piece.OU;
                case 'と': return Piece.TO;
                case '杏': return Piece.NY;
                case '圭': return Piece.NK;
                case '全': return Piece.NG;
                case '馬': return Piece.UM;
                case '龍':
                case '竜': return Piece.RY;
                case '成':
                    switch (line[i++]) {
                        case '歩': return Piece.FU | Piece.PROMOTED;
                        case '香': return Piece.KY | Piece.PROMOTED;
                        case '桂': return Piece.KE | Piece.PROMOTED;
                        case '銀': return Piece.GI | Piece.PROMOTED;
                        case '金': return Piece.KI | Piece.PROMOTED;
                        case '角': return Piece.KA | Piece.PROMOTED;
                        case '飛': return Piece.HI | Piece.PROMOTED;
                        default:
                            throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
                    }

                default:
                    throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
            }
        }

        #endregion

        /// <summary>
        /// 読み込めるか判定
        /// </summary>
        public override bool CanRead(string data) {
            return data != null &&
                (data.Contains("先手") || data.Contains("後手") ||
                data.Contains("下手") || data.Contains("上手") ||
                0 <= data.IndexOfAny(new[] { '▲', '△', '▼', '▽' }));
            // ↑適当 (´ω`)
        }

        /// <summary>
        /// 読み込み。
        /// </summary>
        public override IEnumerable<Notation> Read(string data) {
            if (data == null) {
                throw new NotationException("KIF形式棋譜の読み込みに失敗しました");
            }

            Notation notation = new Notation();
            BoardData initialBoard = null, tempBoard = null;
            List<MoveDataEx> moves = new List<MoveDataEx>();

            string[] lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int emptyLineCount = 0;
            foreach (string t in lines) {
                string line = t.Trim();
                if (string.IsNullOrEmpty(line)) {
                    if (2 <= ++emptyLineCount) { // 空行が2個以上続いたなら一度yield
                        if (initialBoard != null || 0 < moves.Count) {
                            notation.InitialBoard = initialBoard;
                            notation.Moves = moves.ToArray();
                            yield return notation;
                            // 再度初期化
                            notation = new Notation();
                            moves = new List<MoveDataEx>();
                            initialBoard = tempBoard = null;
                        }
                    }
                    continue;
                } else {
                    emptyLineCount = 0;
                }

                Match m;
                if (line[0] == '*' || line[0] == '#') { // コメント？
                } else if ('0' <= line[0] && line[0] <= '9') {
                    if (2 <= line.Count(x => x == '/')) {
                        // 「/」を2個以上含むなら日付とかの情報と思われるので無視(適当)
                    } else {
                        // kifの指し手
                        ParseKIFMove(initialBoard, ref  tempBoard, moves, line);
                    }
                } else if (0 <= "▲△▼▽".IndexOf(line[0])) {
                    // ki2の指し手
                    // 面倒なのでkifと区別せず、例え行毎に混在してても読み込めちゃうようにしちゃう
                    ParseKI2Move(initialBoard, ref tempBoard, moves, line);
                } else if (line[0] == '+' || line[0] == '９') {
                    // 罫線と筋の番号
                    continue;
                } else if (line[0] == '|') {
                    // 盤面表現
                    ParseBoardLine(ref initialBoard, line);
                } else if (line.StartsWith("まで", StringComparison.Ordinal)) {
                    notation.Winner =
                        line.Contains("先手の勝") || line.Contains("下手の勝") ? 0 :
                        line.Contains("後手の勝") || line.Contains("上手の勝") ? 1 :
                        -1;
                    notation.InitialBoard = initialBoard;
                    notation.Moves = moves.ToArray();
                    yield return notation;
                    // 再度初期化
                    notation = new Notation();
                    moves = new List<MoveDataEx>();
                    initialBoard = tempBoard = null;
                } else if (line.StartsWith("手数----指手---------消費時間", StringComparison.Ordinal)) {
                    // 無視出来る行
                } else if ((m = moveCountRegex.Match(line)).Success) {
                    // 手数＝13  ▲４五角打  まで
                    notation.InitMoveCount = int.Parse(m.Groups[1].Value);
                    if (!string.IsNullOrEmpty(m.Groups[3].Value)) {
                        // とりあえず(?)手番のみ反映。_no
                        switch (m.Groups[3].Value[0]) {
                            case '▲':
                            case '▼':
                                if (initialBoard == null) initialBoard = new BoardData();
                                initialBoard.Turn = 1;
                                break;
                            case '△':
                            case '▽':
                                if (initialBoard == null) initialBoard = new BoardData();
                                initialBoard.Turn = 0;
                                break;
                        }
                    }
                } else if (line == "TEBAN=GOTE") {
                    if (initialBoard == null) initialBoard = new BoardData();
                    initialBoard.Turn = 1;
                } else {
                    // 指し手を保持した状態でまたヘッダ情報に遭遇した場合、yield
                    if (0 < moves.Count) {
                        notation.InitialBoard = initialBoard;
                        notation.Moves = moves.ToArray();
                        yield return notation;
                        // 再度初期化
                        notation = new Notation();
                        moves = new List<MoveDataEx>();
                        initialBoard = tempBoard = null;
                    }
                    // 棋譜情報など
                    int sep = line.IndexOfAny(new char[] { '：', ':', ' ' });
                    if (0 <= sep) {
                        string infoName = line.Substring(0, sep);
                        switch (infoName) {
                            case "手合割":
                                // 初期盤面
                                if (line.Contains("平手")) {
                                    //initialBoard = null;
                                    // 何もしない
                                } else if (line.Contains("香落")) {
                                    initialBoard = BoardData.CreateHandicapKY();
                                } else if (line.Contains("角落")) {
                                    initialBoard = BoardData.CreateHandicapKA();
                                } else if (line.Contains("飛車落")) {
                                    initialBoard = BoardData.CreateHandicapHI();
                                } else if (line.Contains("飛香落")) {
                                    initialBoard = BoardData.CreateHandicapHIKY();
                                } else if (line.Contains("二枚落")) {
                                    initialBoard = BoardData.CreateHandicap2();
                                } else if (line.Contains("四枚落")) {
                                    initialBoard = BoardData.CreateHandicap4();
                                } else if (line.Contains("六枚落")) {
                                    initialBoard = BoardData.CreateHandicap6();
                                } else if (line.Contains("八枚落")) {
                                    initialBoard = BoardData.CreateHandicap8();
                                } else if (line.Contains("十枚落")) {
                                    initialBoard = BoardData.CreateHandicap10();
                                } else if (line.Contains("詰将棋")) {
                                    // 何もしない
                                } else {
                                    logger.Warn("未知の手割合: " + line);
                                }
                                break;

                            case "先手の持駒":
                            case "下手の持駒":
                            case "先手持駒": // 一応対応
                            case "下手持駒": // 一応対応
                                if (initialBoard == null) initialBoard = new BoardData();
                                ParseHand(initialBoard.GetHand(0), line);
                                break;

                            case "後手の持駒":
                            case "上手の持駒":
                            case "後手持駒": // 一応対応
                            case "上手持駒": // 一応対応
                                if (initialBoard == null) initialBoard = new BoardData();
                                ParseHand(initialBoard.GetHand(1), line);
                                break;

                            case "作品名":
                                notation.Title = line.Substring(4).Trim();
                                break;

                            case "先手":
                            case "下手":
                                // おなまえ。
                                notation.FirstPlayerName = line.Substring(3).Trim();
                                // 空なら??演算子とかが使いやすいようにnullにしとく。
                                if (notation.FirstPlayerName.Length <= 0) {
                                    notation.FirstPlayerName = null;
                                }
                                break;

                            case "後手":
                            case "上手":
                                // おなまえ。
                                notation.SecondPlayerName = line.Substring(3).Trim();
                                // 空なら??演算子とかが使いやすいようにnullにしとく。
                                if (notation.SecondPlayerName.Length <= 0) {
                                    notation.SecondPlayerName = null;
                                }
                                break;

                            default:
                                // その他の棋譜情報
                                notation.AdditionalInfo[infoName] = line.Substring(sep + 1);
                                break;
                        }
                    } else {
                        switch (line) {
                            case "先手番":
                            case "下手番":
                                if (initialBoard == null) initialBoard = new BoardData();
                                initialBoard.Turn = 0;
                                break;

                            case "後手番":
                            case "上手番":
                                if (initialBoard == null) initialBoard = new BoardData();
                                initialBoard.Turn = 1;
                                break;

                            default:
                                // よく分からない行 (ログる)
                                logger.Debug("棋譜中に不正な行(1): " + line);
                                break;
                        }
                    }
                }
            }
            if (initialBoard != null || 0 < moves.Count) {
                notation.InitialBoard = initialBoard;
                notation.Moves = moves.ToArray();
                yield return notation;
            }
        }

        /// <summary>
        /// kif形式の指し手
        /// </summary>
        private void ParseKIFMove(BoardData initialBoard, ref BoardData tempBoard, List<MoveDataEx> moves, string line) {
            try {
                //手数----指手---------消費時間--
                //   1 ２四角打     ( 0:02/00:00:02)
                //   2 同　玉(13)   ( 0:14/00:00:14)

                if (tempBoard == null) {
                    tempBoard = initialBoard == null ?
                        BoardData.CreateEquality() :
                        initialBoard.Clone();
                }

                MoveData move;

                // 手数を飛ばす
                int i = 0;
                for (; i < line.Length && '0' <= line[i] && line[i] <= '9'; i++) ; // 数字を飛ばす
                for (; i < line.Length && char.IsWhiteSpace(line[i]); i++) ; // スペースを飛ばす

                // 移動先
                if (line[i] == '投' || line[i] == '千' || line[i] == '連' || line[i] == '入' || line[i] == '反') {
                    return;
                } else if (line[i] == '同') {
                    i++;
                    move.To = moves[moves.Count - 1].MoveData.To;
                    if (100 <= move.To) move.To -= 100; // 直前が成る手の場合を考慮しないといけないので注意。
                } else {
                    int toFile = NotationUtility.ReadNumber(line[i++]);
                    if (toFile <= 0) {
                        //throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
                        // 投了以外に何があるのか分からんので。。(´ω`)
                        return;
                    }

                    int toRank = NotationUtility.ReadNumber(line[i++]);
                    if (toRank <= 0) throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);

                    move.To = (byte)(toFile + (toRank - 1) * 9);
                }

                // 駒
                for (; char.IsWhiteSpace(line[i]); i++) ; // スペースを飛ばす
                Piece piece = ReadPieceForMove(line, ref i);

                if (line[i] == '成') {
                    i++;
                    move.To += 100;
                }

                // 移動元
                if (line[i] == '打') {
                    move.From = (byte)((byte)piece + 100);
                } else if (line[i] == '(') {
                    if (line[i + 3] != ')') {
                        throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
                    }
                    int from = int.Parse(line.Substring(i + 1, 2));
                    i += 4;
                    int fromRank = from % 10;
                    int fromFile = from / 10;
                    move.From = (byte)(fromFile + (fromRank - 1) * 9);
                    if ((tempBoard[fromFile, fromRank] & ~Piece.ENEMY) != piece) {
                        logger.Warn("移動元が不正？: 行=" + line);
                    }
                } else {
                    throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
                }

                moves.Add(new MoveDataEx(move));
                tempBoard.Do(move);
            } catch (ArgumentOutOfRangeException e) {
                throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line, e);
            }
        }

        static Regex ki2MoveRegex1 = new Regex(@"[▲△▼▽]([^▲△▼▽]+)\s*", RegexOptions.Compiled);
        static Regex ki2MoveRegex2 = new Regex(
            @"([1-9１２３４５６７８９][1-9一二三四五六七八九]|同\s*)" +
            @"(成?[歩香桂銀金角飛王玉と杏圭全金馬龍竜])" +
            @"(不成|成)?" +
            @"(\([1-9１２３４５６７８９][1-9一二三四五六七八九]\))?" +
            @"([打上下左右寄引直行入]*)" +
            @"(不成|成)?",
            RegexOptions.Compiled);

        /// <summary>
        /// ki2形式の指し手
        /// </summary>
        private void ParseKI2Move(BoardData initialBoard, ref BoardData tempBoard, List<MoveDataEx> moves, string line) {
            if (tempBoard == null) {
                tempBoard = initialBoard == null ?
                    BoardData.CreateEquality() :
                    initialBoard.Clone();
            }
            foreach (Match m1 in ki2MoveRegex1.Matches(line)) {
                Match m2 = ki2MoveRegex2.Match(m1.Groups[1].Value);
                if (m2.Success) {
                    string to = m2.Groups[1].Value;
                    string piece = m2.Groups[2].Value;
                    string promote = m2.Groups[3].Value + m2.Groups[6].Value;
                    string from = m2.Groups[4].Value;
                    string fromHint = m2.Groups[5].Value;
                    MoveData move = new MoveData();
                    // 移動先
                    if (to[0] == '同') {
                        move.To = moves[moves.Count - 1].MoveData.To;
                        if (100 <= move.To) move.To -= 100; // 直前が成る手の場合を考慮しないといけないので注意。
                    } else if (to.Length == 2) {
                        int toFile = NotationUtility.ReadNumber(to[0]);
                        if (toFile <= 0) {
                            throw new NotationException("KI2形式指し手の読み込みに失敗: 行=" + line + " 移動先=" + to);
                        }
                        int toRank = NotationUtility.ReadNumber(to[1]);
                        if (toRank <= 0) throw new NotationException("KI2形式指し手の読み込みに失敗: 行=" + line + " 移動先=" + to);
                        move.To = (byte)(toFile + (toRank - 1) * 9);
                    } else {
                        throw new NotationException("KI2形式指し手の読み込みに失敗: 行=" + line + " 移動先=" + to);
                    }
                    // 駒
                    Piece p = ReadPieceForMove(piece);
                    // 移動元
                    if (fromHint == "打") {
                        move.From = (byte)((byte)p + 100);
                    } else if (string.IsNullOrEmpty(from)) {
                        // 移動元を探す
                        SquareData[] fromList = new SquareData[10]; // 無効値で方向分だけ作成
                        int turn = tempBoard.Turn;
                        Piece fromPiece = turn == 0 ? p : p | Piece.ENEMY;
                        SquareData sqTo = new SquareData(move.ToFile, move.ToRank);
                        foreach (int dir in PieceUtility.CanMoveDirects[(byte)p]) {
                            SquareData sq = sqTo.Add(
                                -BoardData.DirectToFileOffset[turn * 16 + dir],
                                -BoardData.DirectToRankOffset[turn * 16 + dir]);
                            if (sq.IsValid && tempBoard[sq] == fromPiece) fromList[dir] = sq;
                        }
                        foreach (int dir in PieceUtility.CanJumpDirects[(byte)p]) {
                            SquareData sq = tempBoard.SearchNotEmpty(sqTo,
                                -BoardData.DirectToFileOffset[turn * 16 + dir],
                                -BoardData.DirectToRankOffset[turn * 16 + dir]);
                            if (!sq.IsEmpty && tempBoard[sq] == fromPiece) fromList[dir] = sq;
                        }
                        // 上下左右寄引直で移動元としてあり得ないマスを0にする。(このやり方でいいのかよく分からんが…)
                        foreach (char c in fromHint) {
                            // 8,    9,
                            // 5, 6, 7,
                            // 3,    4,
                            // 0, 1, 2,
                            switch (c) {
                                case '行':
                                case '入':
                                case '上': // 1段以上、上に動く
                                    fromList[0] = fromList[1] = fromList[2] = fromList[3] = fromList[4] = new SquareData();
                                    break;

                                case '引':
                                case '下': // 1段以上、下に動く
                                    fromList[3] = fromList[4] = fromList[5] = fromList[6] = fromList[7] = fromList[8] = fromList[9] = new SquareData();
                                    break;

                                case '寄': // 1マス以上、横に動く
                                    fromList[0] = fromList[1] = fromList[2] = fromList[5] = fromList[6] = fromList[7] = fromList[8] = fromList[9] = new SquareData();
                                    break;
                                case '直': // 1段上に上がる時？
                                    fromList[0] = fromList[1] = fromList[2] = fromList[3] = fromList[4] = fromList[5] = fromList[7] = fromList[8] = fromList[9] = new SquareData();
                                    break;

                                case '左': // 指す側から見て左側の駒を動かした場合
                                    if (!fromList[2].IsEmpty || !fromList[4].IsEmpty || !fromList[7].IsEmpty || !fromList[9].IsEmpty) {
                                        // 2, 4, 7, 9のいずれかがあれば上下や右側から左への移動は削除
                                        fromList[0] = fromList[1] = fromList[3] = fromList[5] = fromList[6] = fromList[8] = new SquareData();
                                    } else {
                                        // 右側から左への移動だけ削除
                                        fromList[0] = fromList[3] = fromList[5] = fromList[8] = new SquareData();
                                    }
                                    break;
                                case '右': // 指す側から見て右側の駒を動かした場合
                                    if (!fromList[0].IsEmpty || !fromList[3].IsEmpty || !fromList[5].IsEmpty || !fromList[8].IsEmpty) {
                                        // 0, 3, 5, 8のいずれかがあれば上下や右側から左への移動は削除
                                        fromList[1] = fromList[2] = fromList[4] = fromList[6] = fromList[7] = fromList[9] = new SquareData();
                                    } else {
                                        // 左側から右への移動だけ削除
                                        fromList[2] = fromList[4] = fromList[7] = fromList[9] = new SquareData();
                                    }
                                    break;

                                default: break;
                            }
                        }
                        var fromListNotZero = fromList.Where(x => !x.IsEmpty);
                        int fromCount = fromListNotZero.Count();
                        if (fromCount == 1) {
                            move.From = fromListNotZero.First().ToByte;
                        } else if (fromCount <= 0) {
                            if (string.IsNullOrEmpty(fromHint) && 0 < tempBoard.GetHand(turn)[(byte)p]) { // 打つ手
                                move.From = (byte)((byte)p + 100);
                            } else {
                                throw new NotationException("KI2形式指し手の移動元が不明: 行=" + line + " 指し手=" + m1.Value);
                            }
                        } else {
                            throw new NotationException("KI2形式指し手の移動元特定に失敗: 行=" + line + " 指し手=" + m1.Value);
                        }
                    } else {
                        // 括弧書きで移動元がある場合（滅多に無いけどなにかで見かけたので一応対応)
                        from = from.Trim('(', ')');
                        int fromFile = NotationUtility.ReadNumber(from[0]);
                        if (fromFile <= 0) {
                            throw new NotationException("KI2形式指し手の読み込みに失敗: 行=" + line + " 移動元=" + from);
                        }
                        int fromRank = NotationUtility.ReadNumber(from[1]);
                        if (fromRank <= 0) throw new NotationException("KI2形式指し手の読み込みに失敗: 行=" + line + " 移動元=" + from);
                        move.From = (byte)(fromFile + (fromRank - 1) * 9);
                    }
                    // 成り
                    if (promote == "成") move.To += 100;
                    moves.Add(new MoveDataEx(move));
                    tempBoard.Do(move);
                } else {
                    throw new NotationException("KI2形式指し手の読み込みに失敗: 行=" + line + " 指し手=" + m1.Groups[1].Value);
                }
            }
        }

        /// <summary>
        /// 盤面のparse
        /// </summary>
        private unsafe void ParseBoardLine(ref BoardData initialBoard, string line) {
            if (initialBoard == null) initialBoard = new BoardData();

            Piece* pieces = stackalloc Piece[9];
            int i = 0 + 1;
            for (int j = 0; j < 9; j++) {
                Piece enemyMask = line[i++] == 'v' ? Piece.ENEMY : Piece.EMPTY;
                if (line.Length <= i) {
                    throw new NotationException("KIF/KI2棋譜データの読み込みに失敗しました");
                }
                pieces[j] = ReadPieceForBoardPiece(line, i++) | enemyMask;
            }
            if (line[i++] != '|') {
                throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
            }

            int rank = NotationUtility.ReadNumber(line[i]);
            if (rank <= 0) {
                throw new NotationException("KIF形式棋譜の読み込みに失敗: 行=" + line);
            }

            for (int j = 0; j < 9; j++) {
                initialBoard[9 - j, rank] = pieces[j];
            }
        }

        /// <summary>
        /// 持ち駒のparse。
        /// </summary>
        private void ParseHand(int[] hand, string line) {
            // line = "後手の持駒：飛二　金四　銀三　桂三　香三　歩十二"; とか。
            for (int i = 0; i < line.Length; ) {
                Piece p = ReadPieceForHand(line[i++]);
                if (p != Piece.EMPTY) {
                    hand[(byte)p] = ParseHandCount(line, ref i);
                }
            }
        }

        /// <summary>
        /// 持ち駒の個数をparse。無ければ1。
        /// </summary>
        private int ParseHandCount(string line, ref int i) {
            if (line.Length <= i || char.IsWhiteSpace(line, i)) return 1;
            if (char.IsDigit(line, i)) {
                int t = 0;
                do {
                    if ('0' <= line[i] && line[i] <= '9') {
                        t *= 10;
                        t += line[i] - '0';
                    } else if ('０' <= line[i] && line[i] <= '９') {
                        t *= 10;
                        t += line[i] - '０';
                    } else {
                        throw new NotationException("KIF/KI2棋譜データの読み込みに失敗: " + line);
                    }
                    i++;
                } while (i < line.Length && char.IsDigit(line, i));
                return t;
            } else {
                int t = 0;
                if (line[i] == '十') { // 20以上がない事を利用したいい加減実装
                    t = 10;
                    i++;
                }
                int tt;
                if (i < line.Length) {
                    tt = NotationUtility.ReadNumber(line[i]);
                    if (tt <= 0) {
                        throw new NotationException("KIF/KI2棋譜データの読み込みに失敗: " + line);
                    }
                } else {
                    tt = 0;
                }
                return t + tt;
            }
        }
    }
}
