using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore.Search {
    /// <summary>
    /// Static Exchange Evaluation。
    /// 交換値というのもコレ？
    /// </summary>
    public unsafe class SEE {
        /// <summary>
        /// 交換値。Bonanzaの値を使ってみる？
        /// </summary>
        public static readonly short[] ExchangeValues = BonanzaConstants.Exchange4Per2;

        short[] table;
        short[] promoteBenefits = new short[2 * 16];
        short promoteMaxValue;

        /// <summary>
        /// 初期化。
        /// </summary>
        public SEE() : this(Utility.Clone(ExchangeValues)) { }
        /// <summary>
        /// 初期化。
        /// </summary>
        public SEE(short[] table) {
            this.table = table;
            promoteBenefits[1 * 16 + (byte)Piece.FU] = (short)(table[(byte)Piece.TO] - table[(byte)Piece.FU]);
            promoteBenefits[1 * 16 + (byte)Piece.KY] = (short)(table[(byte)Piece.NY] - table[(byte)Piece.KY]);
            promoteBenefits[1 * 16 + (byte)Piece.KE] = (short)(table[(byte)Piece.NK] - table[(byte)Piece.KE]);
            promoteBenefits[1 * 16 + (byte)Piece.GI] = (short)(table[(byte)Piece.NG] - table[(byte)Piece.GI]);
            promoteBenefits[1 * 16 + (byte)Piece.KA] = (short)(table[(byte)Piece.UM] - table[(byte)Piece.KA]);
            promoteBenefits[1 * 16 + (byte)Piece.HI] = (short)(table[(byte)Piece.RY] - table[(byte)Piece.HI]);
            promoteMaxValue = promoteBenefits.Skip(16).Max();
        }

        /// <summary>
        /// 交換値を返す。
        /// </summary>
        public int GetExchange(Piece piece) {
            return table[(byte)piece];
        }

        /// <summary>
        /// 指し手に対するSEE。
        /// </summary>
        public int Evaluate(Board board, Move move) {
            int turn = board.Turn;
            int see;
            // 自利き・敵利きが明らかに無い場合は簡易的に処理した方が早い気がするのでしてみる。
            if (!board.ControlExists(turn ^ 1, move.To) &&
                (move.IsPut || (board.GetControl(turn ^ 1, move.From) & ~0x03ff) == 0)) { // ←方向の加味は面倒なので手抜き
                // 敵利きが無くて、移動元に敵の飛び利きも無いなら、明らかに取り返せない手。
                Piece toPiece = move.IsPut ? move.PutPiece : board[move.From];
                see = table[(byte)(move.Capture & ~Piece.ENEMY)]
                    + promoteBenefits[((byte)move.Promote << (4 - PieceUtility.PromotedShift)) + (byte)(toPiece & ~Piece.PE)];
                Debug.Assert(see == InnerEvaluateStrict(board, move), "SEEの手抜き計算にバグ");
            } else {
                see = InnerEvaluateStrict(board, move);
            }

            Debug.Assert(board.FullHashValue == board.CalculateFullHash(), "盤面破壊チェック");

            // 最大値は、取って(成れて)取り返されなかった時。
            Debug.Assert(see <= table[(byte)(move.Capture & ~Piece.ENEMY)]
                + (move.IsPut ? 0 : promoteBenefits[((byte)move.Promote << (4 - PieceUtility.PromotedShift)) + (byte)(board[move.From] & ~Piece.ENEMY)]),
                "SEEの最大値がおかしい?: " + move.GetDebugString(board));
            // 最小値は、取って取り返されて成られた時。
            Debug.Assert(table[(byte)(move.Capture & ~Piece.ENEMY)]
                - table[(byte)(move.IsPut ? move.PutPiece : (board[move.From] & ~Piece.ENEMY))]
                - promoteMaxValue <= see, "SEEの最小値がおかしい?: " + move.GetDebugString(board));

            return see;
        }

        /// <summary>
        /// ちゃんとした実装。
        /// </summary>
        private int InnerEvaluateStrict(Board board, Move move) {
            int turn = board.Turn;
            int see;
            // ちゃんとした実装
            MoveData* memory1 = stackalloc MoveData[16], memory2 = stackalloc MoveData[16];
            MoveQueue* moveQueues = stackalloc MoveQueue[2];
            moveQueues[0] = new MoveQueue(memory1);
            moveQueues[1] = new MoveQueue(memory2);

            GetMoveQueues(moveQueues, board, move.To, move);
            Piece toPiece = Piece.EMPTY;
            Do(board, ref toPiece, moveQueues, turn, move);

            see = table[(byte)(move.Capture & ~Piece.ENEMY)]
                + promoteBenefits[((byte)move.Promote << (4 - PieceUtility.PromotedShift)) + (byte)(toPiece & ~Piece.PE)];
            /*
            see = -InnerEvaluate(board, toPiece, move.To, moveQueues, turn ^ 1, -see);
            /*/
            int alpha = table[(byte)(move.Capture & ~Piece.ENEMY)]
                - table[(byte)(move.IsPut ? move.PutPiece : (board[move.From] & ~Piece.ENEMY))]
                - promoteMaxValue; // 最小値は、取り返されて成られた時。
            int beta = see; // 最大値は、取り返されなかった時。
            see = -InnerEvaluateAlphaBeta(board, toPiece, move.To, moveQueues, turn ^ 1, -see, -beta, -alpha);
            //*/
            return see;
        }

        /// <summary>
        /// ある位置での現在のターンに対するSEE。
        /// </summary>
        private int InnerEvaluateAlphaBeta(Board board, Piece toPiece, int to,
            MoveQueue* moveQueues, int turn, int current, int alpha, int beta) {
            // βカット
            //if (beta <= current) return current;
            // 取る手が尽きた？
            if (moveQueues[turn].QueueEmpty) return current;
            // αの更新
            if (alpha < current) {
                alpha = current;
            }

            MoveData move = moveQueues[turn].Peek();
            int see = current + table[(byte)(toPiece & ~Piece.ENEMY)]
                + promoteBenefits[((byte)move.Promote << (4 - PieceUtility.PromotedShift)) + (byte)(board[move.From] & ~Piece.ENEMY)];
            // 子ノードでのβカット
            if (see <= alpha) return Math.Max(current, see);

            Do(board, ref toPiece, to, moveQueues, turn);
            see = -InnerEvaluateAlphaBeta(board, toPiece, to, moveQueues, turn ^ 1, -see, -beta, -alpha);

            return Math.Max(current, see);
        }

        struct MoveData {
            public byte From;
            public Piece Promote;
            public byte DirectIndex;
            public short Value;
            // デバッグ用適当文字列化
            public override string ToString() {
                return (From - Board.Padding).ToString("x") +
                    (Promote == Piece.EMPTY ? "" : "成") +
                    ":" + Value.ToString();
            }
        }

        unsafe struct MoveQueue {
            const int RoundMask = 0x0f;
            MoveData* list;
            int pos;
            int count;

            /// <summary>
            /// 初期化。
            /// </summary>
            public MoveQueue(MoveData* memory) {
                list = memory;
                pos = 0;
                count = 0;
            }
            /// <summary>
            /// 空ならtrue
            /// </summary>
            public bool QueueEmpty {
                get { return count <= pos; }
            }
            /// <summary>
            /// ソートりつつ挿入
            /// </summary>
            public void InsertWithSort(MoveData moveData) {
                int i = pos, n = count; // ←0ではなくposから開始。
                for (; i < n; i++) {
                    if (moveData.Value < list[i & RoundMask].Value) break;
                }
                if (i < n) {
                    for (int j = count; i < j; j--) {
                        list[j & RoundMask] = list[(j - 1) & RoundMask];
                    }
                    list[i & RoundMask] = moveData;
                    count++;
                } else {
                    list[(count++) & RoundMask] = moveData;
                }
            }
            /// <summary>
            /// キューの先頭のを参照
            /// </summary>
            public MoveData Peek() { return list[pos & RoundMask]; }
            /// <summary>
            /// キューから1つ取り出す
            /// </summary>
            public MoveData Dequeue() { return list[(pos++) & RoundMask]; }
        }

        /// <summary>
        /// toへ移動出来る駒のリストの構築
        /// </summary>
        /// <param name="moveExclude">この手だけは除外</param>
        private void GetMoveQueues(MoveQueue* moveQueues, Board board, int to, Move moveExclude) {
            for (int t = 0; t < 2; t++) {
                int control = board.GetControl(t, to);
                if (control == 0) continue;
                for (int dir = 0; dir < 8; dir++) {
                    if ((control & (1 << (dir + 10))) != 0) {
                        int direct = -Board.Direct[t * 16 + dir];
                        int m = board.SearchNotEmpty(to, direct);
                        Debug.Assert(PieceUtility.IsFirstTurn(board[m]) == (t == 0) && PieceUtility.CanJump(board[m], dir));
                        if (m == moveExclude.From) continue; // 追加
                        //if (pin[m] == 0 || pin[m] == Board.Direct[dir] || pin[m] == -Board.Direct[dir]) {
                        moveQueues[t].InsertWithSort(CreateMove(board, m, to, dir));
                    } else if ((control & (1 << dir)) != 0) {
                        int direct = -Board.Direct[t * 16 + dir];
                        int m = to + direct;
                        Debug.Assert(PieceUtility.IsFirstTurn(board[m]) == (t == 0) && PieceUtility.CanMove(board[m], dir));
                        if (m == moveExclude.From) continue; // 追加
                        //if (pin[m] == 0 || pin[m] == Board.Direct[dir] || pin[m] == -Board.Direct[dir]) {
                        moveQueues[t].InsertWithSort(CreateMove(board, m, to, dir));
                    }
                }
                for (int dir = 8; dir < 10; dir++) {
                    if ((control & (1 << dir)) != 0) {
                        int direct = -Board.Direct[t * 16 + dir];
                        int m = to + direct;
                        Debug.Assert(board[m] == PieceUtility.SelfOrEnemy[t * 32 + (byte)Piece.KE]);
                        if (m == moveExclude.From) continue; // 追加
                        //if (pin[m] == 0) {
                        moveQueues[t].InsertWithSort(CreateMove(board, m, to, dir));
                    }
                }
            }
        }

        /// <summary>
        /// 手の作成
        /// </summary>
        private MoveData CreateMove(Board board, int from, int to, int dir) {
            Piece p = (board[from] & ~Piece.ENEMY);
            int turn = ((byte)board[from] >> PieceUtility.EnemyShift) & 0x01;
            Piece promote = 0 < promoteBenefits[1 * 16 + (byte)p] && // 成るメリットありで
                (Board.GetRank(to, turn) <= 3 || Board.GetRank(from, turn) <= 3) ? // 成れるなら成る
                Piece.PROMOTED : Piece.EMPTY;
            return new MoveData {
                From = (byte)from,
                Promote = promote,
                DirectIndex = (byte)dir,
                Value = table[(byte)p],
                //Value = table[(byte)(p | promote)],
            };
        }

        /// <summary>
        /// 指定ターンの取る手を1つ進める
        /// </summary>
        private void Do(Board board, ref Piece toPiece, int to, MoveQueue* moveQueues, int turn) {
            MoveData move = moveQueues[turn].Dequeue();
            toPiece = board[move.From] | move.Promote;

            if (8 <= move.DirectIndex) return; // 桂馬なら終わり。

            // 桂馬以外なら飛び駒があるかもしれない
            int m = board.SearchNotEmpty(move.From, -Board.Direct[turn * 16 + move.DirectIndex]);
            Piece p = board[m];
            if (PieceUtility.IsFirstTurn(p)) { // 先手
                int dir = turn == 0 ? move.DirectIndex : 7 - move.DirectIndex;
                if (PieceUtility.CanJump(p, dir)) {
                    moveQueues[0].InsertWithSort(CreateMove(board, m, to, dir));
                }
            } else if (PieceUtility.IsSecondTurn(p)) { // 後手
                p = (p & ~Piece.ENEMY);
                int dir = turn == 0 ? 7 - move.DirectIndex : move.DirectIndex;
                if (PieceUtility.CanJump(p, dir)) {
                    moveQueues[1].InsertWithSort(CreateMove(board, m, to, dir));
                }
            }
        }

        /// <summary>
        /// moveを仮想的に適用する
        /// </summary>
        private void Do(Board board, ref Piece toPiece, MoveQueue* moveQueues, int turn, Move move) {
            if (move.IsPut) {
                toPiece = PieceUtility.SelfOrEnemy[turn * 32 + (byte)move.PutPiece];
            } else {
                toPiece = board[move.From] | move.Promote;
                if ((board[move.From] & ~Piece.ENEMY) == Piece.KE) return; // 桂馬なら終わり。
                // 桂馬以外なら飛び駒があるかもしれない
                int direct = Board.GetDirectTo(move.To, move.From);
                if (direct == 0) return;
                int dir = Board.GetDirectIndex8(direct); // fromからtoへの方向。先手にとって逆なので注意
                Debug.Assert(0 <= dir && dir < 8, "方向が不正");
                int m = board.SearchNotEmpty(move.From, direct);
                Piece p = board[m];
                if (PieceUtility.IsFirstTurn(p)) { // 先手
                    if (PieceUtility.CanJump(p, 7 - dir)) {
                        moveQueues[0].InsertWithSort(CreateMove(board, m, move.To, 7 - dir));
                    }
                } else if (PieceUtility.IsSecondTurn(p)) { // 後手
                    if (PieceUtility.CanJump(p, dir)) {
                        moveQueues[1].InsertWithSort(CreateMove(board, m, move.To, dir));
                    }
                }
            }
        }
    }
}
