using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ShogiCore.Notation {
    /// <summary>
    /// SFEN(Shogi Forsyth-Edwards notation)表記法。
    /// </summary>
    public class SFENNotationReader : StringNotationReader {
        /// <summary>
        /// CSA用文字列化テーブル
        /// </summary>
        static readonly string[] NameTable = {
        //  空 歩  香  桂  銀  金  角  飛   王  と   杏   圭   全  金  馬   龍  
            "","P","L","N","S","G","B","R","K","+P","+L","+N","+S","","+B","+R",
            "","p","l","n","s","g","b","r","k","+p","+l","+n","+s","","+b","+r",
        };

        /// <summary>
        /// 名前の取得
        /// </summary>
        public static string ToName(Piece p) {
            return NameTable[(byte)p];
        }
        /// <summary>
        /// ToName()の逆変換
        /// </summary>
        /// <param name="name">駒名</param>
        /// <returns>PieceData.FU ～ PieceData.ERY</returns>
        public static Piece FromName(string name) {
            int n = Array.IndexOf(NameTable, name);
            Debug.Assert(0 < n);
            return (Piece)n;
        }

        /// <summary>
        /// 読み込めるか判定
        /// </summary>
        public override bool CanRead(string data) {
            return data != null && (data.Contains("startpos") || data.Contains("sfen") || HasMaybePos(data));
            // ↑適当 (´ω`)
        }

        /// <summary>
        /// SFENの局面の表現を含んでる気がするならtrue
        /// </summary>
        private bool HasMaybePos(string data) {
            // 最初の空白を飛ばす
            int i = 0;
            for (; i < data.Length; i++) {
                if (data[i] != ' ') break;
            }
            // 次の空白をsearch (無ければfalse)
            int sp = data.IndexOf(' ', i);
            if (sp < 0) return false;
            // spまでの間に/が含まれていたらtrue(適当)
            return 0 <= data.IndexOf('/', 0, sp);
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public override IEnumerable<Notation> Read(string data) {
            if (data == null) {
                throw new NotationException("SFEN棋譜データの読み込みに失敗しました");
            }
            foreach (string line in data.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                yield return InnerLoad(line);
            }
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        private Notation InnerLoad(string data) {
            // 例：
            // lnsgkgsn1/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL w - 1

            string[] str = data.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (str.Length <= 0) {
                throw new NotationFormatException("SFEN棋譜データの読み込みに失敗しました");
            }
            int strPos = 0;

            BoardData board;
            if (str[strPos] == "position") strPos++; // USIのログとかからコピペ出来るように無視する
            if (str[strPos] == "startpos") { // 平手初期盤面
                strPos++;
                // これはSFENというよりUSIプロトコルの方かもしれんが、面倒なのでここで処理してしまう。
                board = null;
            } else {
                if (str[strPos] == "sfen") strPos++;

                if (str.Length < 3) {
                    throw new NotationFormatException("SFEN棋譜の盤面データの読み込みに失敗しました (1)");
                }

                string[] strB = str[strPos++].Split('/');
                if (strB.Length != 9) {
                    throw new NotationFormatException("SFEN棋譜の盤面データの読み込みに失敗しました (2)");
                }

                // 盤面
                board = new BoardData();
                for (int y = 0; y < 9; y++) {
                    int rank = y + 1;
                    Piece promote = 0;
                    for (int file = 9, i = 0; 1 <= file; i++) {
                        if (strB[y].Length <= i) {
                            //throw new NotationFormatException("SFEN棋譜の盤面データの読み込みに失敗しました (3)");
                            break;
                        }
                        switch (strB[y][i]) {
                            case 'P': board[file--, rank] = Piece.FU | promote; promote = 0; break;
                            case 'L': board[file--, rank] = Piece.KY | promote; promote = 0; break;
                            case 'N': board[file--, rank] = Piece.KE | promote; promote = 0; break;
                            case 'S': board[file--, rank] = Piece.GI | promote; promote = 0; break;
                            case 'G': board[file--, rank] = Piece.KI | promote; promote = 0; break;
                            case 'B': board[file--, rank] = Piece.KA | promote; promote = 0; break;
                            case 'R': board[file--, rank] = Piece.HI | promote; promote = 0; break;
                            case 'K': board[file--, rank] = Piece.OU | promote; promote = 0; break;
                            case 'p': board[file--, rank] = Piece.EFU | promote; promote = 0; break;
                            case 'l': board[file--, rank] = Piece.EKY | promote; promote = 0; break;
                            case 'n': board[file--, rank] = Piece.EKE | promote; promote = 0; break;
                            case 's': board[file--, rank] = Piece.EGI | promote; promote = 0; break;
                            case 'g': board[file--, rank] = Piece.EKI | promote; promote = 0; break;
                            case 'b': board[file--, rank] = Piece.EKA | promote; promote = 0; break;
                            case 'r': board[file--, rank] = Piece.EHI | promote; promote = 0; break;
                            case 'k': board[file--, rank] = Piece.EOU | promote; promote = 0; break;
                            case '+': promote = Piece.PROMOTED; break;
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9': {
                                    int n = strB[y][i] - '0';
                                    for (int j = 0; j < n; j++) {
                                        board[file--, rank] = Piece.EMPTY;
                                    }
                                    promote = 0;
                                }
                                break;
                            default:
                                throw new NotationException("SFEN棋譜の盤面データの読み込みに失敗しました。不正な駒：" + strB[y][i]);
                        }
                    }
                }
                // 手番
                board.Turn = str[strPos++] == "w" ? 1 : 0; // b:先手、w:後手
                // 持ち駒
                int[][] hand = { board.GetHand(0), board.GetHand(1) };
                int lastNumber = 1;
                string handStr = str[strPos++];
                if (handStr != "-") {
                    for (int i = 0; i < handStr.Length; i++) {
                        switch (handStr[i]) {
                            case 'P': hand[0][(byte)Piece.FU] += lastNumber; lastNumber = 1; break;
                            case 'L': hand[0][(byte)Piece.KY] += lastNumber; lastNumber = 1; break;
                            case 'N': hand[0][(byte)Piece.KE] += lastNumber; lastNumber = 1; break;
                            case 'S': hand[0][(byte)Piece.GI] += lastNumber; lastNumber = 1; break;
                            case 'G': hand[0][(byte)Piece.KI] += lastNumber; lastNumber = 1; break;
                            case 'B': hand[0][(byte)Piece.KA] += lastNumber; lastNumber = 1; break;
                            case 'R': hand[0][(byte)Piece.HI] += lastNumber; lastNumber = 1; break;
                            case 'K': hand[0][(byte)Piece.OU] += lastNumber; lastNumber = 1; break;
                            case 'p': hand[1][(byte)Piece.FU] += lastNumber; lastNumber = 1; break;
                            case 'l': hand[1][(byte)Piece.KY] += lastNumber; lastNumber = 1; break;
                            case 'n': hand[1][(byte)Piece.KE] += lastNumber; lastNumber = 1; break;
                            case 's': hand[1][(byte)Piece.GI] += lastNumber; lastNumber = 1; break;
                            case 'g': hand[1][(byte)Piece.KI] += lastNumber; lastNumber = 1; break;
                            case 'b': hand[1][(byte)Piece.KA] += lastNumber; lastNumber = 1; break;
                            case 'r': hand[1][(byte)Piece.HI] += lastNumber; lastNumber = 1; break;
                            case 'k': hand[1][(byte)Piece.OU] += lastNumber; lastNumber = 1; break;
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9': {
                                    lastNumber = handStr[i] - '0';
                                    while (char.IsDigit(handStr[++i])) { // ←ユニコード絡みで[0-9]以外も引っかかるかもしれんけど手抜き
                                        lastNumber *= 10;
                                        lastNumber += handStr[i] - '0';
                                    }
                                    i--;
                                }
                                break;
                            default:
                                throw new NotationException("SFEN棋譜の持ち駒データの読み込みに失敗しました。不正な持ち駒: " + handStr[i]);
                        }
                    }
                }

                // 次の手が何手目かの情報は、あるなら（movesでないなら）スキップ
                if (strPos < str.Length) {
                    if (str[strPos] != "moves")
                        strPos++;
                }
            }

            List<MoveDataEx> moves = new List<MoveDataEx>();
            if (strPos < str.Length) {
                if (str[strPos++] != "moves") Debug.Fail("謎エラー？");
                // 指し手
                for (int i = strPos; i < str.Length; i++) {
                    moves.Add(new MoveDataEx(ToMoveData(str[i])));
                }
            }

            Notation notation = new Notation();
            notation.InitialBoard = board;
            notation.Moves = moves.ToArray();
            return notation;
        }

        /// <summary>
        /// SFENの指し手表現をMoveDataへ変換
        /// </summary>
        public static MoveData ToMoveData(string str) {
            switch (str) {
                case "resign": return MoveData.Resign;
                case "win": return MoveData.Win;
                case "pass": return MoveData.Pass;
                case "endless": return MoveData.Endless; // 適当
                case "perpetual": return MoveData.Perpetual; // 適当
            }
            if (str.Length < 4) {
                throw new NotationException("SFEN棋譜の指し手データの読み込みに失敗: " + str);
            }
            int from = 0;
            if (str[1] == '*') {
                // 駒を打つ手
                switch (char.ToUpperInvariant(str[0])) {
                    case 'P': from = (byte)Piece.FU + 100; break;
                    case 'L': from = (byte)Piece.KY + 100; break;
                    case 'N': from = (byte)Piece.KE + 100; break;
                    case 'S': from = (byte)Piece.GI + 100; break;
                    case 'G': from = (byte)Piece.KI + 100; break;
                    case 'B': from = (byte)Piece.KA + 100; break;
                    case 'R': from = (byte)Piece.HI + 100; break;
                    default: throw new NotationException("SFEN棋譜の指し手データの読み込みに失敗: " + str);
                }
            } else {
                if (str[0] < '1' || '9' < str[0])
                    throw new NotationException("SFEN棋譜の指し手データの読み込みに失敗: " + str);
                if (str[1] < 'a' || 'i' < str[1])
                    throw new NotationException("SFEN棋譜の指し手データの読み込みに失敗: " + str);
                // 動かす手
                int fromFile = str[0] - '1'; // [0-8]
                int fromRank = str[1] - 'a'; // [0-8]
                from = fromFile + 1 + fromRank * 9;
            }
            if (str[2] < '1' || '9' < str[2])
                throw new NotationException("SFEN棋譜の指し手データの読み込みに失敗: " + str);
            if (str[3] < 'a' || 'i' < str[3])
                throw new NotationException("SFEN棋譜の指し手データの読み込みに失敗: " + str);
            // 移動先
            int toFile = str[2] - '1'; // [0-8]
            int toRank = str[3] - 'a'; // [0-8]
            int to = toFile + 1 + toRank * 9;
            // 成り
            if (4 < str.Length && str[4] == '+') {
                to += 100;
            }
            var move = new MoveData(from, to);
            return move;
        }

        /// <summary>
        /// 指し手表現かどうかを判定
        /// </summary>
        public static bool IsMove(string str) {
            return Regex.IsMatch(str, @"^([PLNSGBR]\*|[1-9][a-i])[1-9][a-i]\+?$");
        }
    }
}
