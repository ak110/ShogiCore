using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ShogiCore {
    /// <summary>
    /// 履歴データ。
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct BoardHistoryEntry {
        /// <summary>
        /// (Moveを適用する前の)ハッシュ値
        /// </summary>
        public readonly ulong HashValue;
        /// <summary>
        /// (Moveを適用する前の)持ち駒値
        /// </summary>
        public readonly uint HandValue;
        /// <summary>
        /// 指し手
        /// </summary>
        public readonly Move Move;
        /// <summary>
        /// Moveが王手だったらtrue。
        /// </summary>
        /// <remarks>
        /// board.History[History.Count - 1].Check == board.Checked
        /// </remarks>
        public readonly bool Check;

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
            get { return HashValue ^ Board.GetHandHashValue(HandValue); }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public BoardHistoryEntry(ulong hash, uint hand, Move move, bool check) {
            HashValue = hash;
            HandValue = hand;
            Move = move;
            Check = check;
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public BoardHistoryEntry(BinaryReader reader) {
            HashValue = reader.ReadUInt64();
            HandValue = reader.ReadUInt32();
            Move = Move.FromBinary(null, reader.ReadUInt16());
            Move.Capture = (Piece)reader.ReadByte();
            Check = reader.ReadBoolean();
        }

        /// <summary>
        /// 書き込み
        /// </summary>
        public void Write(BinaryWriter writer) {
            writer.Write(HashValue);
            writer.Write(HandValue);
            writer.Write(Move.ToBinary());
            writer.Write((byte)Move.Capture);
            writer.Write(Check);
        }

        /// <summary>
        /// デバッグ表示用適当文字列化
        /// </summary>
        public override string ToString() {
            return string.Format("Hash {0} 持駒 {1} {2}{3}",
                HashValue.ToString(), HandValue.ToString(),
                Move.GetDebugString(), Check ? " (王手)" : "");
        }
    }
}
