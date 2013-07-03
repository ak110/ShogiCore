using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// メモリの割り当てを行う為のヘルパクラス
    /// </summary>
    public unsafe class UnsafeMemoryAssignor {
        /// <summary>
        /// ポインタ
        /// </summary>
        public void* CurrentPointer { get; private set; }
#if DEBUG
        /// <summary>
        /// サイズ
        /// </summary>
        public int Size { get; private set; }
#endif

        /// <summary>
        /// 初期化
        /// </summary>
        public UnsafeMemoryAssignor(UnsafeMemory memory)
            : this(memory.Pointer, memory.Size) { }

        /// <summary>
        /// 初期化
        /// </summary>
        public UnsafeMemoryAssignor(void* pointer, int size) {
            CurrentPointer = pointer;
#if DEBUG
            Size = size;
#endif
        }

        /// <summary>
        /// ポインタを返してcb分進める。
        /// </summary>
        /// <param name="cb">バイト数</param>
        public void* Assign(int cb) {
            void* p = CurrentPointer;
            CurrentPointer = (byte*)CurrentPointer + cb;
#if DEBUG
            Size -= cb;
            Debug.Assert(0 <= Size, "割り当てエラー(不足)");
#endif
            return p;
        }

        /// <summary>
        /// 全部割り当てた事をAssert()する。
        /// </summary>
        [Conditional("DEBUG")]
        public void AssertFinished() {
#if DEBUG
            Debug.Assert(Size == 0, "割り当てエラー(余分)");
#endif
        }
    }
}
