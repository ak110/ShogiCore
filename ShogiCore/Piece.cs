using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ShogiCore {
    /// <summary>
    /// 駒の種類
    /// </summary>
    public enum Piece : byte {
        EMPTY = 0,
        FU = 1,
        KY = 2,
        KE = 3,
        GI = 4,
        KI = 5,
        KA = 6,
        HI = 7,
        OU = 8,
        TO = PROMOTED | FU, // 9
        NY = PROMOTED | KY, // 10
        NK = PROMOTED | KE, // 11
        NG = PROMOTED | GI, // 12
        // = PROMOTED | KI, // 13
        UM = PROMOTED | KA, // 14
        RY = PROMOTED | HI, // 15
        WALL = 16,
        EFU = ENEMY | FU, // 17
        EKY = ENEMY | KY, // 18
        EKE = ENEMY | KE, // 19
        EGI = ENEMY | GI, // 20
        EKI = ENEMY | KI, // 21
        EKA = ENEMY | KA, // 22
        EHI = ENEMY | HI, // 23
        EOU = ENEMY | OU, // 24
        ETO = ENEMY | TO, // 25
        ENY = ENEMY | NY, // 26
        ENK = ENEMY | NK, // 27
        ENG = ENEMY | NG, // 28
        //  = ENEMY |   , // 29
        EUM = ENEMY | UM, // 30
        ERY = ENEMY | RY, // 31
        /// <summary>
        /// 駒の種類の総数。
        /// </summary>
        AllCount = 32,

        /// <summary>
        /// 成り駒bit
        /// </summary>
        PROMOTED = 1 << PieceUtility.PromotedShift,
        /// <summary>
        /// 後手bit
        /// </summary>
        ENEMY = 1 << PieceUtility.EnemyShift,

        /// <summary>
        /// PROMOTED | ENEMY
        /// </summary>
        PE = PROMOTED | ENEMY,

        /// <summary>
        /// PROMOTED | KI
        /// </summary>
        NKi = PROMOTED | KI,
        /// <summary>
        /// ENEMY | NKI
        /// </summary>
        ENKi = ENEMY | NKi,
    }
}
