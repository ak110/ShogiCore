using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ShogiCore.Notation {
    /// <summary>
    /// PCL形式(拡張CSA形式)の読み込みを行う。
    /// </summary>
    public class PCLNotationReader : StringNotationReader {
        /// <summary>
        /// logger
        /// </summary>
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// CSA用文字列化テーブル
        /// </summary>
        public static readonly string[] CSANameTable = {
        //  空  歩   香   桂   銀   金   角   飛   王   と   杏   圭   全  金  馬   龍  
            "","FU","KY","KE","GI","KI","KA","HI","OU","TO","NY","NK","NG","","UM","RY",
        };
        /// <summary>
        /// CSA用名前の取得
        /// </summary>
        public static string ToCSAName(Piece p) {
            return CSANameTable[(byte)(p & ~Piece.ENEMY)];
        }
        /// <summary>
        /// ToCSAName()の逆変換
        /// </summary>
        /// <param name="name">駒名</param>
        /// <returns>PieceData.FU ～ PieceData.RY</returns>
        public static Piece FromCSAName(string name) {
            int n = Array.IndexOf(CSANameTable, name);
            if (n <= 0) {
                throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 駒名=" + name);
            }
            return (Piece)n;
        }

        /// <summary>
        /// 読み込めるか判定
        /// </summary>
        public override bool CanRead(string data) {
            return data != null &&
                0 <= data.IndexOf('P') &&
                (0 <= data.IndexOf('+') || 0 <= data.IndexOf('-'));
            // ↑適当 (´ω`)
        }

        /// <summary>
        /// 読み込み。
        /// </summary>
        public override IEnumerable<Notation> Read(string data) {
            List<List<string>> list;
            try {
                list = SplitCSAStandard(data);
            } catch (DecoderFallbackException e) {
                throw new NotationFormatException("CSA/PCL棋譜データの読み込みに失敗しました", e);
            }
            foreach (List<string> t in list) {
                List<int> linePos = new List<int>(); // 手数毎の行位置
                int moveCount = 1;
                char lastTurn = '-';
                for (int line = 0; line < t.Count; line++) {
                    if (string.IsNullOrEmpty(t[line])) continue;

                    if (t[line].StartsWith("'手数目:", StringComparison.Ordinal)) {
                        int n = int.Parse(t[line].Substring(5));
                        if (n < linePos.Count) {
                            // 「'手数目:」の前までの分。
                            yield return ParseCSAStandard(t.GetRange(0, line));
                            // 指定手数から「'手数目:」の行までを削除。
                            t.RemoveRange(linePos[n], line - (linePos[n] + 1) + 1);
                            // 次はn手目から。
                            line = linePos[n];
                            moveCount = n;
                            lastTurn = moveCount % 2 == 0 ? '+' : '-';
                        } else if (n == 1 && linePos.Count == 0) {
                            // 何故か最初に「'手数目:1」とあるデータがあった
                            t.RemoveAt(line--); // ちょっとでも軽量化してみる
                        } else {
                            logger.Warn("不正な「'手数目:」指定？: (" + line.ToString() + "行目) " + t[line]);
                        }
                    } else if (t[line][0] == '+' || t[line][0] == '-') {
                        if (1 < t[line].Length) { // 手番指定で無さそうなら手数カウント
                            if (lastTurn == t[line][0]) {
                                logger.Warn("手番が不正？: (" + line.ToString() + "行目) " + t[line]);
                            }
                            lastTurn = t[line][0];

                            while (linePos.Count <= moveCount) linePos.Add(0);
                            linePos[moveCount++] = line;
                        }
                    }
                }
                yield return ParseCSAStandard(t);
            }
        }

        /// <summary>
        /// CSA標準棋譜形式を対局毎に分割。
        /// </summary>
        List<List<string>> SplitCSAStandard(string data) {
            List<List<string>> list = new List<List<string>>();
            List<string> lines = new List<string>();
            foreach (string line in data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                AddLine(list, ref lines, line);
            }
            list.Add(lines);
            return list;
        }
        /// <summary>
        /// 行の追加
        /// </summary>
        private void AddLine(List<List<string>> list, ref List<string> lines, string line) {
            if (line == "/") { // セパレータ
                list.Add(lines);
                lines = new List<string>();
            } else if (line[0] == '\'') {
                lines.Add(line);
            } else if (0 <= line.IndexOf(',')) {
                // マルチパートステートメント対応(適当)
                foreach (string s in line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    AddLine(list, ref lines, s);
                }
            } else {
                lines.Add(line);
            }
        }

        /// <summary>
        /// 解析処理
        /// </summary>
        private Notation ParseCSAStandard(List<string> lines) {
            try {
                return ParseCSAStandard2(lines);
            } catch (IndexOutOfRangeException e) {
                throw new NotationFormatException("CSA/PCL棋譜データの読み込みに失敗しました", e);
            } catch (ArgumentOutOfRangeException e) {
                throw new NotationFormatException("CSA/PCL棋譜データの読み込みに失敗しました", e);
            } catch (NotSupportedException e) {
                throw new NotationFormatException("CSA/PCL棋譜データの読み込みに失敗しました", e);
            }
        }

        /// <summary>
        /// 解析処理
        /// </summary>
        private Notation ParseCSAStandard2(List<string> lines) {
            Notation notation = new Notation();
            BoardData board = new BoardData();
            List<MoveDataEx> moves = new List<MoveDataEx>();
            bool hasAnyData = false;

            // CSA棋譜のデフォルトの持ち時間は25分切れ負けとしておく。(選手権ルール)
            notation.TimeA = 25 * 60 * 1000;

            foreach (string line in lines) {
                switch (line[0]) {
                    case 'N': // 対局者名
                        if (line[1] == '+') {
                            if (2 < line.Length) {
                                notation.FirstPlayerName = line.Substring(2).Trim();
                            }
                        } else if (line[1] == '-') {
                            if (2 < line.Length) {
                                notation.SecondPlayerName = line.Substring(2).Trim();
                            }
                        } else {
                            throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 行=" + line);
                        }
                        hasAnyData = true;
                        break;

                    case '$': // 各種棋譜情報
                        try {
                            string[] sp = line.Split(new[] { ':' }, 2);
                            if (sp.Length == 2) {
                                notation.AdditionalInfo[sp[0]] = sp[1];
                                if (sp[0] == "$EVENT") { // 棋戦名
                                    // 例：$EVENT:wdoor+floodgate-900-0+gps_l+BlunderXX_4c+20130411100005
                                    // floodgateのイベント命名規則に合致してる場合、そこから持ち時間情報を取得。
                                    Match m = Regex.Match(sp[1], @"^\s*[\w+-]+-(\d+)-(\d+)\+[\w+-]+\s*$");
                                    if (m.Success) {
                                        notation.TimeA = int.Parse(m.Groups[1].Value) * 1000;
                                        notation.TimeB = int.Parse(m.Groups[2].Value) * 1000;
                                    }
                                } else if (sp[0] == "$TIME_LIMIT") { // 持ち時間(持ち時間と秒読み)
                                    // $TIME_LIMIT:HH:MM+SS
                                    Match m = Regex.Match(sp[1], @"^\s*(\d\d):(\d\d)\+(\d\d)\s*$");
                                    if (m.Success) {
                                        notation.TimeA = int.Parse(m.Groups[1].Value) * 3600000 + int.Parse(m.Groups[2].Value) * 60000;
                                        notation.TimeB = int.Parse(m.Groups[3].Value) * 1000;
                                    }
                                }
                            }
                            hasAnyData = true;
                        } catch (Exception e) {
                            throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 行=" + line, e);
                        }
                        break;

                    case 'P': // 初期盤面
                        bool equality;
                        ParseInitialBoard(board, line, out equality);
                        notation.InitialBoard = equality ? null : board.Clone();
                        hasAnyData = true;
                        break;

                    case '+':
                    case '-':
                        if (line.Length == 1) {
                            board.Turn = line[0] == '+' ? 0 : 1;
                            notation.InitialBoard = board.Clone();
                        } else {
                            moves.Add(new MoveDataEx(ParseMoveWithDo(board, line)));
                        }
                        hasAnyData = true;
                        break;

                    case '%':
                        if (line.StartsWith("%TORYO", StringComparison.Ordinal)) { // 投了
                            notation.Winner = moves.Count % 2 == 0 ? 1 : 0;
                        } else if (line.StartsWith("%CHUDAN", StringComparison.Ordinal)) { // 中断
                            notation.Winner = -1;
                        } else if (line.StartsWith("%SENNICHITE", StringComparison.Ordinal)) { // 千日手
                            notation.Winner = -1;
                        } else if (line.StartsWith("%TIME_UP", StringComparison.Ordinal)) { // 時間切れ(手番側の時間切れ)
                            notation.Winner = moves.Count % 2 == 0 ? 1 : 0;
                        } else if (line.StartsWith("%ILLEGAL_MOVE", StringComparison.Ordinal)) { // 反則負け(手番側の反則負け、反則の内容はコメントで記録可能) 
                            notation.Winner = moves.Count % 2 == 0 ? 1 : 0;
                        } else if (line.StartsWith("%JISHOGI", StringComparison.Ordinal)) { // 持将棋
                            notation.Winner = -1;
                        } else if (line.StartsWith("%KACHI", StringComparison.Ordinal)) { // (入玉で)勝ちの宣言
                            notation.Winner = moves.Count % 2 == 0 ? 0 : 1;
                        } else if (line.StartsWith("%HIKIWAKE", StringComparison.Ordinal)) { // (入玉で)引き分けの宣言
                            notation.Winner = -1;
                        } else if (line.StartsWith("%MATTA", StringComparison.Ordinal)) { // 待った
                        } else if (line.StartsWith("%TSUMI", StringComparison.Ordinal)) { // 詰み
                            notation.Winner = moves.Count % 2 == 0 ? 1 : 0;
                        } else if (line.StartsWith("%FUZUMI", StringComparison.Ordinal)) { // 不詰
                        } else if (line.StartsWith("%ERROR", StringComparison.Ordinal)) { // エラー
                            notation.Winner = -1; // ?
                        } else {
                            logger.Warn("未知の特殊手？:" + line);
                        }
                        hasAnyData = true;
                        break;

                    case 'T': // 時間
                        if (0 < moves.Count) {
                            int time;
                            if (int.TryParse(line.Substring(1), out time)) {
                                var moveData = moves[moves.Count - 1];
                                moveData.Time = time * 1000; // ミリ秒
                                moves[moves.Count - 1] = moveData;
                                hasAnyData = true;
                            } else {
                                logger.Warn("不正な思考時間？: " + line);
                            }
                        }
                        break;

                    case '\'': // コメント
                        if (0 < moves.Count) {
                            var moveData = moves[moves.Count - 1];
                            if (string.IsNullOrEmpty(moveData.Comment)) {
                                moveData.Comment = line.Substring(1);
                                if (moveData.Comment.StartsWith("** ")) {
                                    // 評価値・PV
                                    // 例：「'** 30 -3122OU +6867GI -4344FU」
                                    int n = moveData.Comment.IndexOf(' ', 3);
                                    if (n < 0) n = moveData.Comment.Length;
                                    int value;
                                    if (int.TryParse(moveData.Comment.Substring(3, n - 3), out value)) {
                                        moveData.Value = value;
                                    }
                                }
                            } else {
                                moveData.Comment += Environment.NewLine + line.Substring(1);
                            }
                            moves[moves.Count - 1] = moveData;
                            hasAnyData = true;
                        }
                        break;
                }
            }

            if (!hasAnyData)
                throw new NotationFormatException("CSA/PCL棋譜データの読み込みに失敗");

            notation.Moves = moves.ToArray();
            return notation;
        }

        /// <summary>
        /// CSA棋譜の指し手表現からMoveDataの作成と、指し手を進める処理
        /// </summary>
        public static MoveData ParseMoveWithDo(BoardData board, string line) {
            int turn = line[0] == '+' ? 0 : 1;
            if (board.Turn % 2 != turn) {
                logger.Warn("盤面と指し手の手番が不一致: " + line);
            }
            int fromFile = line[1] - '0';
            int fromRank = line[2] - '0';
            int toFile = line[3] - '0';
            int toRank = line[4] - '0';
            int to = toFile + (toRank - 1) * 9;
            if (to < 0 && 81 < to) {
                throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 行=" + line);
            }
            Piece p = FromCSAName(line.Substring(5, 2));
            int from;
            if (fromFile == 0 && fromRank == 0) {
                from = (byte)p + 100;
                if (board.GetHand(board.Turn)[(byte)p] <= 0) {
                    logger.Warn("持っていない駒を打つ手: " + line + Environment.NewLine + board.DumpToString);
                }
                board.GetHand(board.Turn)[(byte)p]--;
                board[toFile, toRank] = p | (turn == 0 ? 0 : Piece.ENEMY);
            } else {
                from = fromFile + (fromRank - 1) * 9;
                Piece capture = board[toFile, toRank];
                if (capture != Piece.EMPTY) {
                    board.GetHand(board.Turn)[(byte)(capture & ~Piece.PE)]++;
                }
                if ((board[fromFile, fromRank] & ~Piece.ENEMY) != p) { // 成った場合
                    if ((board[fromFile, fromRank] & ~Piece.ENEMY | Piece.PROMOTED) != p) {
                        logger.Warn("指し手の駒が不正？: " + line);
                    }
                    to += 100;
                    board[toFile, toRank] = board[fromFile, fromRank] | Piece.PROMOTED;
                } else {
                    if ((board[fromFile, fromRank] & ~Piece.ENEMY) != p) {
                        logger.Warn("指し手の駒が不正？: " + line);
                    }
                    capture = board[toFile, toRank];
                    board[toFile, toRank] = board[fromFile, fromRank];
                }
                board[fromFile, fromRank] = Piece.EMPTY;
            }
            board.Turn ^= 1; // 手番を反転
            return new MoveData(from, to);
        }

        /// <summary>
        /// 初期盤面の解析処理
        /// </summary>
        private void ParseInitialBoard(BoardData board, string line, out bool equality) {
            equality = false;
            switch (line[1]) {
                case 'I': // 平手初期配置(＋駒落ち)
                    board.SetEquality();
                    equality = true;
                    for (int i = 2; i + 4 <= line.Length; i += 4) {
                        equality = false;
                        int file = line[i + 0] - '0';
                        int rank = line[i + 1] - '0';
                        string csaName = line.Substring(i + 2, 2);
                        if ((board[file, rank] & ~Piece.ENEMY) != FromCSAName(csaName)) {
                            logger.Warn("平手初期配置の指定が不正？: " + line);
                        }
                        board[file, rank] = Piece.EMPTY;
                    }
                    break;

                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9': {
                        int rank = line[1] - '0';
                        if (rank < 1 || 9 < rank) {
                            throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 行=" + line);
                        }
                        int i = 2;
                        for (int file = 9; 1 <= file; file--) {
                            switch (line[i]) {
                                case '+': // 先手駒
                                    board[file, rank] = FromCSAName(line.Substring(i + 1, 2));
                                    i += 3;
                                    break;
                                case '-': // 後手駒
                                    board[file, rank] = FromCSAName(line.Substring(i + 1, 2)) | Piece.ENEMY;
                                    i += 3;
                                    break;
                                case ' ':
                                    if (line[i + 1] == '*' &&
                                        (line.Length <= i + 2 || line[i + 2] == ' ')) {
                                        // 正しく「 * 」になってる
                                    } else {
                                        logger.Warn("不正な盤面表現？: " + line);
                                    }
                                    i += 3;
                                    break;
                            }
                        }
                    }
                    break;

                case '+': { // 先手駒
                        for (int i = 2; i + 4 <= line.Length; i += 4) {
                            int file = line[i + 0] - '0';
                            int rank = line[i + 1] - '0';
                            string csaName = line.Substring(i + 2, 2);
                            if (csaName == "AL") {
                                board.SetHandAll(0);
                            } else {
                                Piece p = FromCSAName(csaName);
                                if (file == 0 && rank == 0) {
                                    board.GetHand(0)[(byte)p]++;
                                } else {
                                    if (file < 1 || 9 < file || rank < 1 || 9 < rank) {
                                        throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 行=" + line);
                                    }
                                    board[file, rank] = p;
                                }
                            }
                        }
                    }
                    break;

                case '-': { // 後手駒
                        for (int i = 2; i + 4 <= line.Length; i += 4) {
                            int file = line[i + 0] - '0';
                            int rank = line[i + 1] - '0';
                            string csaName = line.Substring(i + 2, 2);
                            if (csaName == "AL") {
                                board.SetHandAll(1);
                            } else {
                                Piece p = FromCSAName(csaName);
                                if (file == 0 && rank == 0) {
                                    board.GetHand(1)[(byte)p]++;
                                } else {
                                    if (file < 1 || 9 < file || rank < 1 || 9 < rank) {
                                        throw new NotationException("CSA/PCL棋譜データの読み込みに失敗: 行=" + line);
                                    }
                                    board[file, rank] = p | Piece.ENEMY;
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
