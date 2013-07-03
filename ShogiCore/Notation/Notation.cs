using System;
using System.Collections.Generic;
using System.Text;

namespace ShogiCore.Notation {
    /// <summary>
    /// 棋譜データ
    /// </summary>
    public interface INotation : ICloneable {
        /// <summary>
        /// 開始盤面。平手初期配置ならnull。
        /// </summary>
        BoardData InitialBoard { get; set; }
        /// <summary>
        /// 指し手のリスト。
        /// </summary>
        MoveDataEx[] Moves { get; set; }
        /// <summary>
        /// 先手の勝ちなら0、後手の勝ちなら1。
        /// </summary>
        int Winner { get; set; }
    }

    /// <summary>
    /// 棋譜データ
    /// </summary>
    [Serializable]
    public class Notation : INotation {
        /// <summary>
        /// 開始盤面。平手初期配置ならnull。
        /// </summary>
        public BoardData InitialBoard { get; set; }
        /// <summary>
        /// 指し手のリスト。
        /// </summary>
        public MoveDataEx[] Moves { get; set; }
        /// <summary>
        /// 先手の勝ちなら0、後手の勝ちなら1。不明や引き分けや初期値は-1。
        /// </summary>
        public int Winner { get; set; }
        /// <summary>
        /// 棋譜のタイトル(作品名など)
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 先手・下手のプレーヤーの名前
        /// </summary>
        public string FirstPlayerName { get; set; }
        /// <summary>
        /// 後手・上手のプレーヤーの名前
        /// </summary>
        public string SecondPlayerName { get; set; }
        /// <summary>
        /// 持ち時間[ミリ秒]
        /// </summary>
        public int TimeA { get; set; }
        /// <summary>
        /// 秒読み[ミリ秒]
        /// </summary>
        public int TimeB { get; set; }
        /// <summary>
        /// 棋譜の「手数=93」とかの値。
        /// </summary>
        public int InitMoveCount { get; set; }
        /// <summary>
        /// 任意の追加情報。kif形式とかで「○○：△△」は任意のものが書けるので。
        /// </summary>
        public Dictionary<string, string> AdditionalInfo { get; private set; }

        /// <summary>
        /// 初期化
        /// </summary>
        public Notation() {
            Winner = -1;
            AdditionalInfo = new Dictionary<string, string>();
        }

        #region ICloneable メンバ

        object ICloneable.Clone() {
            return Clone();
        }

        #endregion

        public Notation Clone() {
            Notation copy = (Notation)MemberwiseClone();
            if (InitialBoard != null) {
                copy.InitialBoard = InitialBoard.Clone();
            }
            if (Moves != null) {
                copy.Moves = (MoveDataEx[])Moves.Clone();
            }
            copy.AdditionalInfo = new Dictionary<string, string>(AdditionalInfo);
            return copy;
        }
    }
}
