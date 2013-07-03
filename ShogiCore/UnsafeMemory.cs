using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;

namespace ShogiCore {
    /// <summary>
    /// GlobalAllocのラッパー。
    /// </summary>
    public unsafe class UnsafeMemory : CriticalFinalizerObject, IDisposable {
        IntPtr handle;

        /// <summary>
        /// アクセス用のポインタ。(32 byteアライン済み)
        /// </summary>
        public void* Pointer { get; private set; }
        /// <summary>
        /// サイズ
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// 配列用ヘルパ
        /// </summary>
        /// <param name="size">sizeof(要素の型)</param>
        /// <param name="count">要素数</param>
        /// <returns>UnsafeMemory</returns>
        public static UnsafeMemory CreateArray(int size, int count) {
            return new UnsafeMemory(size * count, size % 32 == 0);
            // サイズをalignしてたらメモリ確保時もalign。
        }
        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="cb">サイズ[byte]</param>
        /// <param name="align32">32byteアラインするのかどうか。</param>
        public UnsafeMemory(int cb, bool align32) {
            Debug.Assert(IntPtr.Size <= 8, "IntPtr.ToInt64()");
            Size = cb;
            if (align32) {
                handle = Marshal.AllocHGlobal(cb + 31);
                Pointer = (void*)((handle.ToInt64() + 31) & ~31);
            } else {
                handle = Marshal.AllocHGlobal(cb);
                Pointer = (void*)handle;
            }
            ZeroMemory();
            // 一応登録しといてみる？
            GC.AddMemoryPressure(Size); // align分は面倒なので無視
        }

        /// <summary>
        /// インスタンスがGCに回収される時に呼び出されます。
        /// </summary>
        ~UnsafeMemory() {
            Dispose(false);
        }

        /// <summary>
        /// 使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }

        /// <summary>
        /// 使用されているアンマネージ リソースを解放し、オプションでマネージ リソースも解放します。
        /// </summary>
        /// <param name="disposing">マネージ リソースとアンマネージ リソースの両方を解放する場合は true。アンマネージ リソースだけを解放する場合は false。 </param>
        private void Dispose(bool disposing) {
            if (handle != IntPtr.Zero) {
                Marshal.FreeHGlobal(handle);
                handle = IntPtr.Zero;
                Pointer = null;
                // 登録解除
                GC.RemoveMemoryPressure(Size); // align分は面倒なので無視
                // Disposeしたならファイナライザは省略可能
                if (disposing) {
                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// ZeroMemory
        /// </summary>
        public void ZeroMemory() {
            Utility.ZeroMemory(Pointer, Size);
        }

        // ↓使う？ 意味無いか…。

        #region VirtualAlloc

        [DllImport("kernel32.dll")]
        public static extern unsafe void* VirtualAlloc(void* lpAddress, int dwSize, int flAllocationType, int flProtect);
        [DllImport("kernel32.dll")]
        public static extern unsafe bool VirtualFree(void* lpAddress, int dwSize, int dwFreeType);

        public const int PAGE_NOACCESS = 0x01;
        public const int PAGE_READONLY = 0x02;
        public const int PAGE_READWRITE = 0x04;
        public const int PAGE_WRITECOPY = 0x08;
        public const int PAGE_EXECUTE = 0x10;
        public const int PAGE_EXECUTE_READ = 0x20;
        public const int PAGE_EXECUTE_READWRITE = 0x40;
        public const int PAGE_EXECUTE_WRITECOPY = 0x80;
        public const int PAGE_GUARD = 0x100;
        public const int PAGE_NOCACHE = 0x200;
        public const int PAGE_WRITECOMBINE = 0x400;
        public const int MEM_COMMIT = 0x1000;
        public const int MEM_RESERVE = 0x2000;
        public const int MEM_DECOMMIT = 0x4000;
        public const int MEM_RELEASE = 0x8000;
        public const int MEM_FREE = 0x10000;
        public const int MEM_PRIVATE = 0x20000;
        public const int MEM_MAPPED = 0x40000;
        public const int MEM_RESET = 0x80000;
        public const int MEM_TOP_DOWN = 0x100000;
        public const int MEM_WRITE_WATCH = 0x200000;
        public const int MEM_PHYSICAL = 0x400000;
        public const int MEM_ROTATE = 0x800000;
        public const int MEM_LARGE_PAGES = 0x20000000;
        public const int MEM_4MB_PAGES = unchecked((int)0x80000000);

        #endregion
    }

    
}
