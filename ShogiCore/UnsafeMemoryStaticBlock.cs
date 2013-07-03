
#if DANGEROUS
    //#define DEBUG_MODE
#elif !DEBUG // RELEASE
    #define DEBUG_MODE
#else // DEBUG
    #define DEBUG_MODE
#endif

#if DEBUG_MODE
#warning DEBUG_MODE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// staticなreadonlyテーブルのunsafeポインタ化用クラス。(必要量だけ確保する)
    /// </summary>
    /// <remarks>
    /// 64bit環境ではポインタの分少し増えるので注意。
    /// </remarks>
    public static unsafe class UnsafeMemoryStaticBlock {
        /// <summary>
        /// 確保するメモリサイズ。ToUnsafe()使う時に増やすべし。
        /// </summary>
        public const int Capacity = 32 * 1024 * 1024; // 13052533

#if !DEBUG_MODE
        /// <summary>
        /// メモリ。
        /// </summary>
        static readonly UnsafeMemory memory = new UnsafeMemory(Capacity, false);
#endif

        /// <summary>
        /// 割り当て済みメモリサイズ
        /// </summary>
        public static int AllocatedSize { get; private set; }

        /// <summary>
        /// メモリの割り当て
        /// </summary>
        /// <param name="cb">バイト数</param>
        /// <returns>ポインタ</returns>
        private static void* Allocate(int cb) {
#if DEBUG_MODE
            if (cb <= 0) {
                throw new ArgumentOutOfRangeException("cb", cb, "cbは0より大きい値を指定してください。");
            }
            AllocatedSize += cb;
            if (Capacity < AllocatedSize) {
                throw new ApplicationException("UnsafeMemoryStaticBlock.Capacityが不足しています。: cb = " + cb.ToString() + ", Capacity=" + Capacity.ToString());
            }
            return (void*)System.Runtime.InteropServices.Marshal.AllocHGlobal(cb);
#else
            int oldSize = AllocatedSize;
            AllocatedSize += cb;
            if (memory.Size < AllocatedSize) {
                throw new ApplicationException("UnsafeMemoryStaticBlock.Capacityが不足しています。: cb = " + cb.ToString() + ", Capacity=" + Capacity.ToString());
            }
            return (byte*)memory.Pointer + oldSize;
#endif
        }

#if USE_UNSAFE
        #region Update

        /// <summary>
        /// ToUnsafe()またはCopy
        /// </summary>
        public static void Update(short[][][] src, ref short*** dst) {
            if (dst == null) {
                dst = ToUnsafe(src);
            } else {
                Copy(src, dst);
            }
        }

        /// <summary>
        /// ToUnsafe()またはCopy
        /// </summary>
        public static void Update(short[][] src, ref short** dst) {
            if (dst == null) {
                dst = ToUnsafe(src);
            } else {
                Copy(src, dst);
            }
        }

        /// <summary>
        /// ToUnsafe()またはCopy
        /// </summary>
        public static void Update(short[] src, ref short* dst) {
            if (dst == null) {
                dst = ToUnsafe(src);
            } else {
                Copy(src, dst);
            }
        }

        #endregion

        #region ToUnsafe

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static byte* ToUnsafe(byte[] array) {
            byte* ptr = (byte*)Allocate(sizeof(byte) * array.Length);
            Utility.CopyMemory(ptr, array, sizeof(byte) * array.Length);
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static byte** ToUnsafe(byte[][] array) {
            byte** ptr = (byte**)Allocate(sizeof(byte*) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static byte*** ToUnsafe(byte[][][] array) {
            byte*** ptr = (byte***)Allocate(sizeof(byte**) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static sbyte* ToUnsafe(sbyte[] array) {
            sbyte* ptr = (sbyte*)Allocate(sizeof(sbyte) * array.Length);
            Utility.CopyMemory(ptr, array, sizeof(sbyte) * array.Length);
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static sbyte** ToUnsafe(sbyte[][] array) {
            sbyte** ptr = (sbyte**)Allocate(sizeof(sbyte*) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static sbyte*** ToUnsafe(sbyte[][][] array) {
            sbyte*** ptr = (sbyte***)Allocate(sizeof(sbyte**) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static short* ToUnsafe(short[] array) {
            short* ptr = (short*)Allocate(sizeof(short) * array.Length);
            Utility.CopyMemory(ptr, array, sizeof(short) * array.Length);
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static short** ToUnsafe(short[][] array) {
            short** ptr = (short**)Allocate(sizeof(short*) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static short*** ToUnsafe(short[][][] array) {
            short*** ptr = (short***)Allocate(sizeof(short**) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static int* ToUnsafe(int[] array) {
            int* ptr = (int*)Allocate(sizeof(int) * array.Length);
            Utility.CopyMemory(ptr, array, sizeof(int) * array.Length);
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static int** ToUnsafe(int[][] array) {
            int** ptr = (int**)Allocate(sizeof(int*) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static int*** ToUnsafe(int[][][] array) {
            int*** ptr = (int***)Allocate(sizeof(int**) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static Piece* ToUnsafe(Piece[] array) {
            Piece* ptr = (Piece*)Allocate(sizeof(Piece) * array.Length);
            Utility.CopyMemory(ptr, array, sizeof(Piece) * array.Length);
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static Piece** ToUnsafe(Piece[][] array) {
            Piece** ptr = (Piece**)Allocate(sizeof(Piece*) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }
        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static Piece*** ToUnsafe(Piece[][][] array) {
            Piece*** ptr = (Piece***)Allocate(sizeof(Piece**) * array.Length);
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        #endregion

        #region Copy

        /// <summary>
        /// コピー
        /// </summary>
        public static void Copy(short[][][] src, short*** dst) {
            for (int i = 0; i < src.Length; i++) {
                if (src[i] == null) continue;
                Copy(src[i], dst[i]);
            }
        }
        /// <summary>
        /// コピー
        /// </summary>
        public static void Copy(short[][] src, short** dst) {
            for (int i = 0; i < src.Length; i++) {
                if (src[i] == null) continue;
                Copy(src[i], dst[i]);
            }
        }
        /// <summary>
        /// コピー
        /// </summary>
        public static void Copy(short[] src, short* dst) {
            Utility.CopyMemory(dst, src, sizeof(short) * src.Length);
        }

        #endregion
#endif

        #region Update<T>

        /// <summary>
        /// ToUnsafe()またはCopy
        /// </summary>
        public static void Update<T>(T[][][] src, ref T[][][] dst) {
            if (dst == null) {
                dst = ToUnsafe(src);
            } else {
                Copy(src, dst);
            }
        }

        /// <summary>
        /// ToUnsafe()またはCopy
        /// </summary>
        public static void Update<T>(T[][] src, ref T[][] dst) {
            if (dst == null) {
                dst = ToUnsafe(src);
            } else {
                Copy(src, dst);
            }
        }

        /// <summary>
        /// ToUnsafe()またはCopy
        /// </summary>
        public static void Update<T>(T[] src, ref T[] dst) {
            if (dst == null) {
                dst = ToUnsafe(src);
            } else {
                Copy(src, dst);
            }
        }

        #endregion

        #region ToUnsafe<T>

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static T[][][] ToUnsafe<T>(T[][][] array) {
            T[][][] ptr = new T[array.Length][][];
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static T[][] ToUnsafe<T>(T[][] array) {
            T[][] ptr = new T[array.Length][];
            for (int i = 0; i < array.Length; i++) {
                ptr[i] = array[i] == null ? null : ToUnsafe(array[i]);
            }
            return ptr;
        }

        /// <summary>
        /// 配列をunsafe化。
        /// </summary>
        public static T[] ToUnsafe<T>(T[] array) {
            T[] ptr = new T[array.Length];
            Array.Copy(array, ptr, array.Length);
            return ptr;
        }

        #endregion

        #region Copy<T>

        /// <summary>
        /// コピー
        /// </summary>
        public static void Copy<T>(T[][][] src, T[][][] dst) {
            for (int i = 0; i < src.Length; i++) {
                if (src[i] == null) continue;
                Copy(src[i], dst[i]);
            }
        }

        /// <summary>
        /// コピー
        /// </summary>
        public static void Copy<T>(T[][] src, T[][] dst) {
            for (int i = 0; i < src.Length; i++) {
                if (src[i] == null) continue;
                Copy(src[i], dst[i]);
            }
        }

        /// <summary>
        /// コピー
        /// </summary>
        public static void Copy<T>(T[] src, T[] dst) {
            Array.Copy(src, dst, src.Length);
        }

        #endregion
    }
}
