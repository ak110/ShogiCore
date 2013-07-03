using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// Koma関連のヘルパ
    /// </summary>
    public static unsafe class PieceUtility {
        #region constants

        public const int PromotedShift = 3;
        public const int EnemyShift = 4;

        /// <summary>
        /// 文字列化用テーブル
        /// </summary>
        public static readonly string[] NameTable = new string[] {
            "　","歩","香","桂","銀","金","角","飛","玉","と","杏","圭","全","金","馬","龍",
        };

        /// <summary>
        /// 色々セコくまとめてみたテーブル
        /// 0bit目 : 移動可能かどうか。
        /// 1bit目 : ジャンプ可能かどうか。
        /// 2bit目 : 成れる駒かどうか ([9]のみ)
        /// 2bit目 : 歩・角・飛かどうか ([8]のみ)
        /// 2bit目 : 歩・香・桂かどうか ([7]のみ)
        /// </summary>
#if USE_UNSAFE
        public static readonly byte* SpecTable = UnsafeMemoryStaticBlock.ToUnsafe(new byte[10 * 16] {
#else
        public static readonly byte[] SpecTable = (new byte[10 * 16] {
#endif
         // 空歩香桂銀金角飛王と杏圭全金馬龍
            0,0,0,0,1,0,3,0,1,0,0,0,0,0,3,1, // 0 左下
            0,0,0,0,0,1,0,3,1,1,1,1,1,1,1,3, // 1 下
            0,0,0,0,1,0,3,0,1,0,0,0,0,0,3,1, // 2 右下
            0,0,0,0,0,1,0,3,1,1,1,1,1,1,1,3, // 3 左
            0,0,0,0,0,1,0,3,1,1,1,1,1,1,1,3, // 4 右
            0,0,0,0,1,1,3,0,1,1,1,1,1,1,3,1, // 5 左上
            0,1,3,0,1,1,0,3,1,1,1,1,1,1,1,3, // 6 上
            0,4,4,4,1,1,3,0,1,1,1,1,1,1,3,1, // 7 右上
            0,4,0,1,0,0,4,4,0,0,0,0,0,0,0,0, // 8 左上2
            0,4,4,5,4,0,4,4,0,0,0,0,0,0,0,0, // 9 右上2
        });

        /// <summary>
        /// 移動可能な方向の列挙。CanMoveと異なり、ジャンプ可能なとこは含まれてないので注意。
        /// </summary>
        public static readonly sbyte[][] CanMoveDirects = new sbyte[16][] {
            new sbyte[] { }, // 空
            new sbyte[] { 6 }, // 歩
            new sbyte[] { }, // 香
            new sbyte[] { 8, 9 }, // 桂
            new sbyte[] { 0, 2, 5, 6, 7 }, // 銀
            new sbyte[] { 1, 3, 4, 5, 6, 7 }, // 金
            new sbyte[] { }, // 角
            new sbyte[] { }, // 飛
            new sbyte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, // 王
            new sbyte[] { 1, 3, 4, 5, 6, 7 }, // と
            new sbyte[] { 1, 3, 4, 5, 6, 7 }, // 杏
            new sbyte[] { 1, 3, 4, 5, 6, 7 }, // 圭
            new sbyte[] { 1, 3, 4, 5, 6, 7 }, // 全
            new sbyte[] { }, // 金
            new sbyte[] { 1, 3, 4, 6 }, // 馬
            new sbyte[] { 0, 2, 5, 7 }, // 龍
        };
        /// <summary>
        /// ジャンプ可能な方向の列挙
        /// </summary>
        public static readonly sbyte[][] CanJumpDirects = new sbyte[16][] {
            new sbyte[] { }, // 空
            new sbyte[] { }, // 歩
            new sbyte[] { 6 }, // 香
            new sbyte[] { }, // 桂
            new sbyte[] { }, // 銀
            new sbyte[] { }, // 金
            new sbyte[] { 0, 2, 5, 7 }, // 角
            new sbyte[] { 1, 3, 4, 6 }, // 飛
            new sbyte[] { }, // 王
            new sbyte[] { }, // と
            new sbyte[] { }, // 杏
            new sbyte[] { }, // 圭
            new sbyte[] { }, // 全
            new sbyte[] { }, // 金
            new sbyte[] { 0, 2, 5, 7 }, // 馬
            new sbyte[] { 1, 3, 4, 6 }, // 龍
        };

        /// <summary>
        /// 得点テーブル
        /// </summary>
#if USE_UNSAFE
        public static readonly sbyte* PointTable = UnsafeMemoryStaticBlock.ToUnsafe(new sbyte[(byte)Piece.AllCount] {
#else
        public static readonly sbyte[] PointTable = (new sbyte[(byte)Piece.AllCount] {
#endif
        //  空 歩 香 桂 銀 金 角 飛 王 と 杏 圭 全 金 馬 龍
            +0,+1,+1,+1,+1,+1,+5,+5,+0,+1,+1,+1,+1,+1,+5,+5,
            -0,-1,-1,-1,-1,-1,-5,-5,-0,-1,-1,-1,-1,-1,-5,-5,
        });

        /// <summary>
        /// 自駒⇔敵駒の反転テーブル。
        /// EMPTYとWALLだけ反転しないのでテーブルにしてしまうのが早いはず。。
        /// </summary>
#if USE_UNSAFE
        public static readonly Piece* SelfOrEnemy = UnsafeMemoryStaticBlock.ToUnsafe(new Piece[(byte)Piece.AllCount * 2] {
#else
        public static readonly Piece[] SelfOrEnemy = (new Piece[(byte)Piece.AllCount * 2] {
#endif
            // 変換無し
            Piece.EMPTY, Piece.FU, Piece.KY, Piece.KE, Piece.GI, Piece.KI, Piece.KA, Piece.HI,
            Piece.OU, Piece.TO, Piece.NY, Piece.NK, Piece.NG, Piece.EMPTY, Piece.UM, Piece.RY,
            Piece.WALL, Piece.EFU, Piece.EKY, Piece.EKE, Piece.EGI, Piece.EKI, Piece.EKA, Piece.EHI,
            Piece.EOU, Piece.ETO, Piece.ENY, Piece.ENK, Piece.ENG, Piece.EMPTY, Piece.EUM, Piece.ERY,
            // 敵味方反転
            Piece.EMPTY, Piece.EFU, Piece.EKY, Piece.EKE, Piece.EGI, Piece.EKI, Piece.EKA, Piece.EHI,
            Piece.EOU, Piece.ETO, Piece.ENY, Piece.ENK, Piece.ENG, Piece.EMPTY, Piece.EUM, Piece.ERY,
            Piece.WALL, Piece.FU, Piece.KY, Piece.KE, Piece.GI, Piece.KI, Piece.KA, Piece.HI,
            Piece.OU, Piece.TO, Piece.NY, Piece.NK, Piece.NG, Piece.EMPTY, Piece.UM, Piece.RY,
        });

        /// <summary>
        /// 評価関数の重ね合わせ処理用テーブル
        /// </summary>
        public static byte[][] FoldTable = new byte[6][] {
            new byte[2] { (byte)Piece.KI, (byte)Piece.TO },
            new byte[2] { (byte)Piece.KI, (byte)Piece.NY },
            new byte[2] { (byte)Piece.KI, (byte)Piece.NK },
            new byte[2] { (byte)Piece.KI, (byte)Piece.NG },
            new byte[2] { (byte)Piece.KA, (byte)Piece.UM },
            new byte[2] { (byte)Piece.HI, (byte)Piece.RY },
        };
        /// <summary>
        /// 評価関数の重ね合わせ処理用テーブル
        /// </summary>
        public static byte[][] FoldTableSE = new byte[6 * 2][] {
            new byte[2] { (byte)Piece.KI, (byte)Piece.TO },
            new byte[2] { (byte)Piece.KI, (byte)Piece.NY },
            new byte[2] { (byte)Piece.KI, (byte)Piece.NK },
            new byte[2] { (byte)Piece.KI, (byte)Piece.NG },
            new byte[2] { (byte)Piece.KA, (byte)Piece.UM },
            new byte[2] { (byte)Piece.HI, (byte)Piece.RY },
            new byte[2] { (byte)Piece.EKI, (byte)Piece.ETO },
            new byte[2] { (byte)Piece.EKI, (byte)Piece.ENY },
            new byte[2] { (byte)Piece.EKI, (byte)Piece.ENK },
            new byte[2] { (byte)Piece.EKI, (byte)Piece.ENG },
            new byte[2] { (byte)Piece.EKA, (byte)Piece.EUM },
            new byte[2] { (byte)Piece.EHI, (byte)Piece.ERY },
        };

        #endregion

        /// <summary>
        /// テーブルの初期化
        /// </summary>
        public static int ReadyStatic() {
            return SpecTable[0];
        }

        /// <summary>
        /// 文字列化
        /// </summary>
        public static string ToDisplayString(Piece p) {
            Debug.Assert(Piece.EMPTY <= p && p <= Piece.ERY);
            if (p == Piece.EMPTY) return " ・";
            return (PieceUtility.IsSecondTurn(p) ? "v" : " ") + ToName(p);
        }

        /// <summary>
        /// 名前の取得
        /// </summary>
        public static string ToName(Piece p) {
            return NameTable[(byte)(p & ~Piece.ENEMY)];
        }

        /// <summary>
        /// 自分ならtrue
        /// </summary>
        public static bool IsSelf(Piece p, int turn) {
            return (SelfOrEnemy[(turn ^ 1) * 32 + (byte)p] & Piece.ENEMY) != 0 && p != Piece.WALL;
        }
        /// <summary>
        /// 敵ならtrue
        /// </summary>
        public static bool IsEnemy(Piece p, int turn) {
            return (SelfOrEnemy[turn * 32 + (byte)p] & Piece.ENEMY) != 0 && p != Piece.WALL;
        }
        /// <summary>
        /// 敵か空白ならtrue。味方か壁ならfalse。
        /// </summary>
        public static bool IsMovable(Piece p, int turn) {
            Debug.Assert(((SelfOrEnemy[(turn ^ 1) * 32 + (byte)p] & Piece.ENEMY) == 0) == (p == Piece.EMPTY || PieceUtility.IsEnemy(p, turn)));
            return (SelfOrEnemy[(turn ^ 1) * 32 + (byte)p] & Piece.ENEMY) == 0;
            //return p == Piece.EMPTY || PieceUtility.IsEnemy(p, turn);
        }

        /// <summary>
        /// 先手の駒ならtrue
        /// </summary>
        public static bool IsFirstTurn(Piece p) {
            //*
            return Piece.FU <= p && p <= Piece.RY;
            /*/
            Debug.Assert((Piece.FU <= p && p <= Piece.RY)
                == ((SelfOrEnemy[1 * 32 + (byte)p] & Piece.ENEMY) != 0));
            return (SelfOrEnemy[1 * 32 + (byte)p] & Piece.ENEMY) != 0;
            //*/
        }
        /// <summary>
        /// 後手の駒ならtrue
        /// </summary>
        public static bool IsSecondTurn(Piece p) {
            //*
            return Piece.EFU <= p && p <= Piece.ERY;
            /*/
            return (p & Piece.ENEMY) != 0 && p != Piece.WALL;
            //*/
        }

        /// <summary>
        /// WALLかEMPTYならtrue
        /// </summary>
        public static bool IsWallOrEmpty(Piece p) {
            Debug.Assert(Piece.EMPTY == 0);
            Debug.Assert(((byte)Piece.WALL & ((byte)Piece.WALL << 1)) == 0);
            return (p & ~Piece.WALL) == 0;
        }

        /// <summary>
        /// CanMoveのdirectIndexが負ならfalseるバージョン
        /// </summary>
        public static bool CanMoveSafe(Piece p, int directIndex) {
            return 0 <= directIndex && CanMove(p, directIndex);
        }
        /// <summary>
        /// CanJumpのdirectIndexが負ならfalseるバージョン
        /// </summary>
        public static bool CanJumpSafe(Piece p, int directIndex) {
            return 0 <= directIndex && CanJump(p, directIndex);
        }

        /// <summary>
        /// 移動できるならtrue
        /// </summary>
        /// <param name="directIndex">方向(0～9)</param>
        /// <param name="p">駒の種類</param>
        public static bool CanMove(Piece p, int directIndex) {
            return (SpecTable[directIndex * 16 + (byte)(p & ~Piece.ENEMY)] & 1) != 0;
        }
        /// <summary>
        /// ジャンプ出来るならtrue
        /// </summary>
        /// <param name="directIndex">方向(0～9)</param>
        /// <param name="p">駒の種類</param>
        public static bool CanJump(Piece p, int directIndex) {
            return (SpecTable[directIndex * 16 + (byte)(p & ~Piece.ENEMY)] & 2) != 0;
        }
        /// <summary>
        /// 成れるならtrue
        /// </summary>
        /// <param name="p">駒の種類</param>
        public static bool CanPromote(Piece p) {
            return (SpecTable[9 * 16 + (byte)(p & ~Piece.ENEMY)] & 4) != 0;
        }
        /// <summary>
        /// 歩・角・飛ならtrue
        /// </summary>
        /// <param name="p">駒の種類</param>
        public static bool AlwaysShouldPromote(Piece p) {
            return (SpecTable[8 * 16 + (byte)(p & ~Piece.ENEMY)] & 4) != 0;
        }
        /// <summary>
        /// 歩・香・桂ならtrue
        /// </summary>
        /// <param name="p">駒の種類</param>
        public static bool IsSmallPiece(Piece p) {
            return (SpecTable[7 * 16 + (byte)(p & ~Piece.ENEMY)] & 4) != 0;
        }

        /// <summary>
        /// 飛び駒(香角飛馬龍)ならtrue
        /// </summary>
        /// <param name="p">駒の種類</param>
        public static bool CanJumpPiece(Piece p) {
            Piece q = p & ~Piece.ENEMY;
            return (SpecTable[5 * 16 + (byte)q] | SpecTable[6 * 16 + (byte)q] & 2) != 0;
            // 左上か上にJump可能なら飛び駒。
        }
    }
}
