using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO;

namespace ShogiCore.IO {
    /// <summary>
    /// WinAPIを用いたファイル操作クラス (適当実装)
    /// </summary>
    public class WinFile : IDisposable {
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// SafeFileHandle
        /// </summary>
        public SafeFileHandle SafeFileHandle { get; private set; }

        /// <summary>
        /// ファイルを開く
        /// </summary>
        /// <param name="lpFileName">ファイル名</param>
        /// <param name="dwDesiredAccess">アクセスモード(0、GENERIC_*)</param>
        /// <param name="dwShareMode">共有モード(FILE_SHARE_*)</param>
        /// <param name="dwCreationDisposition">作成方法(CREATE_NEWなど)</param>
        /// <param name="dwFlagsAndAttributes">ファイル属性(FILE_ATTRIBUTE_*、FILE_FLAG_*)</param>
        public WinFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, uint dwCreationDisposition, uint dwFlagsAndAttributes) {
            FileName = lpFileName;
            IntPtr lpSecurityAttributes = IntPtr.Zero; // セキュリティ記述子
            IntPtr hTemplateFile = IntPtr.Zero; // テンプレートファイルのハンドル

            SafeFileHandle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode,
                lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
            if (SafeFileHandle.IsInvalid) {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            SafeFileHandle.Dispose();
        }

        /// <summary>
        /// ファイルサイズの取得
        /// </summary>
        /// <returns>ファイルサイズ</returns>
        public ulong GetSize() {
            uint lFileSizeHigh;
            uint lFileSizeLow = GetFileSize(SafeFileHandle, out lFileSizeHigh);
            if (lFileSizeLow == unchecked((uint)-1)) { // ホントに-1かもしれないのでチェックしてエラーなら例外
                int error = Marshal.GetLastWin32Error();
                if (error != NO_ERROR) {
                    throw new Win32Exception(error);
                }
            }
            return lFileSizeLow | ((ulong)lFileSizeHigh << 32);
        }

        /// <summary>
        /// ファイルポインタの移動
        /// </summary>
        /// <param name="distanceToMove">移動量</param>
        /// <param name="dwMoveMethod">開始点(FILE_*)</param>
        public ulong Seek(uint distanceToMove, uint dwMoveMethod) {
            uint lFileSizeHigh;
            uint lFileSizeLow = SetFilePointer(SafeFileHandle, distanceToMove, out lFileSizeHigh, dwMoveMethod);
            ulong fileSize = lFileSizeLow | ((ulong)lFileSizeHigh << 32);
            if (fileSize == 0) { // ホントに0かもしれないのでチェックしてエラーなら例外
                int error = Marshal.GetLastWin32Error();
                if (error != NO_ERROR) {
                    throw new Win32Exception(error);
                }
            }
            return fileSize;
        }

        /// <summary>
        /// ファイルの終わり（EOF）を、現在のファイルポインタの位置へ移動
        /// </summary>
        public void SetEndOfFile() {
            if (!SetEndOfFile(SafeFileHandle)) {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// ファイルを読み込む
        /// </summary>
        /// <param name="lpBuffer">バッファ</param>
        /// <param name="nNumberOfBytesToRead">読み込み対象のバイト数</param>
        /// <returns>読み込んだバイト数</returns>
        public uint Read(IntPtr lpBuffer, uint nNumberOfBytesToRead) {
            IntPtr lpOverlapped = IntPtr.Zero;
            uint nNumberOfBytesRead;
            if (!ReadFile(SafeFileHandle, lpBuffer, nNumberOfBytesToRead, out nNumberOfBytesRead, lpOverlapped)) {
                throw new Win32Exception();
            }
            return nNumberOfBytesRead;
        }

        /// <summary>
        /// ファイルへ書き込む
        /// </summary>
        /// <param name="lpBuffer">バッファ</param>
        /// <param name="nNumberOfBytesToWrite">書き込み対象のバイト数</param>
        /// <returns>書き込んだバイト数</returns>
        public uint Write(IntPtr lpBuffer, uint nNumberOfBytesToWrite) {
            IntPtr lpOverlapped = IntPtr.Zero;
            uint nNumberOfBytesWritten;
            if (!WriteFile(SafeFileHandle, lpBuffer, nNumberOfBytesToWrite, out nNumberOfBytesWritten, lpOverlapped)) {
                throw new Win32Exception();
            }
            return nNumberOfBytesWritten;
        }

        /// <summary>
        /// メモリマップドファイルの作成
        /// </summary>
        /// <param name="writable">書き込み可能にするならtrue</param>
        /// <param name="mapName">名前。null可</param>
        /// <param name="handle">ハンドル</param>
        /// <param name="memory">メモリ</param>
        public void CreateMemoryMappedFile(bool writable, string mapName, out SafeFileHandle handle, out IntPtr memory) {
            handle = CreateFileMapping(SafeFileHandle, IntPtr.Zero,
                writable ? PAGE_READWRITE : PAGE_READONLY, 0, 0, mapName);
            memory = MapViewOfFile(handle, writable ? FILE_MAP_WRITE : FILE_MAP_READ, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// メモリマップドファイルの削除
        /// </summary>
        /// <param name="handle">ハンドル</param>
        /// <param name="memory">メモリ</param>
        public void DeleteMemoryMappedFile(SafeFileHandle handle, IntPtr memory) {
            try {
                if (!UnmapViewOfFile(memory)) {
                    throw new Win32Exception();
                }
            } finally {
                handle.Dispose();
            }
        }

        /// <summary>
        /// セクタサイズを取得
        /// </summary>
        /// <param name="lpRootPathName">ルートパス名(@"C:" とか @"\\server\path\" とか)</param>
        /// <returns>セクタサイズ</returns>
        public static uint GetBytesPerSector(string lpRootPathName) {
            uint nSectorsPerCluster, nBytesPerSector, nNumberOfFreeClusters, nTotalNumberOfClusters;
            if (!GetDiskFreeSpace(lpRootPathName, out nSectorsPerCluster,
                out nBytesPerSector, out nNumberOfFreeClusters, out nTotalNumberOfClusters)) {
                throw new Win32Exception();
            }
            return nBytesPerSector;
        }

        /// <summary>
        /// UnsafeMemoryReadStreamへの読み込み
        /// </summary>
        /// <param name="path">パス</param>
        /// <returns>UnsafeMemoryReadStream</returns>
        public static unsafe UnsafeMemoryReadStream ReadToStream(string path) {
            byte* buffer, data;
            ulong size;
            ReadToBuffer(path, out buffer, out data, out size);
            return new UnsafeMemoryReadStream(data, (long)size, x => FreeReadBuffer(buffer));
        }

        /// <summary>
        /// UnsafeMemoryReadStreamへの読み込み(メモリマップドファイルバージョン)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static unsafe UnsafeMemoryReadStream ReadToStreamMMF(string path, bool sequential) {
            SafeFileHandle handle;
            IntPtr memory;

            WinFile file = new WinFile(path, WinFile.GENERIC_READ, WinFile.FILE_SHARE_DELETE,
                WinFile.OPEN_EXISTING, sequential ? WinFile.FILE_FLAG_SEQUENTIAL_SCAN : 0);
            file.CreateMemoryMappedFile(false, null, out handle, out memory);

            return new UnsafeMemoryReadStream((byte*)memory, (long)file.GetSize(), (ptr) => {
                file.DeleteMemoryMappedFile(handle, (IntPtr)ptr);
                file.Dispose();
            });
        }

        /// <summary>
        /// ファイルを一気に読み込み
        /// </summary>
        /// <param name="path">ファイル名</param>
        /// <param name="buffer">読み込んだバッファ</param>
        /// <param name="data">データの先頭を示すポインタ</param>
        /// <param name="size">ファイルサイズ</param>
        public static unsafe void ReadToBuffer(string path, out byte* buffer, out byte* data, out ulong size) {
            const uint ReadSize = 8192 * 1024; // 適当
            buffer = null;
            try {
                // セクタサイズ取得
                uint sectorSize = GetBytesPerSector(Path.GetPathRoot(path));
                // ファイル開く
                using (WinFile file = new WinFile(path, GENERIC_READ, FILE_SHARE_DELETE,
                    OPEN_EXISTING, FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN)) {
                    // サイズ取得
                    size = file.GetSize();
                    // メモリ割り当て
                    buffer = (byte*)Marshal.AllocHGlobal((int)(size + sectorSize + ReadSize));
                    data = buffer + (sectorSize - (ulong)buffer % sectorSize); // アライン
                    // 読み込み
                    for (ulong offset = 0; offset < size; ) {
                        offset += file.Read((IntPtr)(data + offset), ReadSize);
                    }
                }
            } catch (Exception e) {
                if (buffer != null) {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                    buffer = null;
                }
                size = 0;
                throw new IOException("ファイルの読み込みに失敗", e);
            }
        }

        /// <summary>
        /// ReadToBuffer()のバッファを開放
        /// </summary>
        /// <param name="buffer">読み込んだバッファ</param>
        public static unsafe void FreeReadBuffer(byte* buffer) {
            Marshal.FreeHGlobal((IntPtr)buffer);
        }

        #region WinAPI

        public const int ERROR_NO_SYSTEM_RESOURCES = 1450;

        public const int NO_ERROR = 0;

        public const uint SECTION_QUERY = 0x0001;
        public const uint SECTION_MAP_WRITE = 0x0002;
        public const uint SECTION_MAP_READ = 0x0004;
        public const uint SECTION_MAP_EXECUTE = 0x0008;
        public const uint SECTION_EXTEND_SIZE = 0x0010;
        public const uint SECTION_MAP_EXECUTE_EXPLICIT = 0x0020;// not included in SECTION_ALL_ACCESS
        public const uint FILE_MAP_COPY = SECTION_QUERY;
        public const uint FILE_MAP_WRITE = SECTION_MAP_WRITE;
        public const uint FILE_MAP_READ = SECTION_MAP_READ;
        //public const uint FILE_MAP_ALL_ACCESS = SECTION_ALL_ACCESS;
        public const uint FILE_MAP_EXECUTE = SECTION_MAP_EXECUTE_EXPLICIT;   // not included in FILE_MAP_ALL_ACCESS

        public const uint PAGE_NOACCESS = 0x01;
        public const uint PAGE_READONLY = 0x02;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_WRITECOPY = 0x08;
        public const uint PAGE_EXECUTE = 0x10;
        public const uint PAGE_EXECUTE_READ = 0x20;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        public const uint PAGE_GUARD = 0x100;
        public const uint PAGE_NOCACHE = 0x200;
        public const uint PAGE_WRITECOMBINE = 0x400;

        public const uint GENERIC_READ = (0x80000000);
        public const uint GENERIC_WRITE = (0x40000000);
        public const uint GENERIC_EXECUTE = (0x20000000);
        public const uint GENERIC_ALL = (0x10000000);

        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_SHARE_DELETE = 0x00000004;
        public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        public const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
        public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
        public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        public const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
        public const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
        public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
        public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        public const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;

        public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
        public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
        public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
        public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
        public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
        public const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
        public const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;

        public const uint CREATE_NEW = 1;
        public const uint CREATE_ALWAYS = 2;
        public const uint OPEN_EXISTING = 3;
        public const uint OPEN_ALWAYS = 4;
        public const uint TRUNCATE_EXISTING = 5;

        public const uint FILE_BEGIN = 0;
        public const uint FILE_CURRENT = 1;
        public const uint FILE_END = 2;

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern uint GetFileSize(SafeFileHandle hFile, out uint lpFileSizeHigh);

        [DllImport("kernel32")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern uint SetFilePointer(SafeFileHandle hFile, uint lDistanceToMove, out uint lpFileSizeHigh, uint dwMoveMethod);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern bool SetEndOfFile(SafeFileHandle hFile);

        [DllImport("kernel32", SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern bool ReadFile(SafeFileHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32", SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern bool WriteFile(SafeFileHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern bool GetDiskFreeSpace(string lpRootPathName, out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters, out uint lpTotalNumberOfClusters);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32", SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern IntPtr MapViewOfFile(SafeFileHandle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);
        // dwNumberOfBytesToMapはSIZE_T

        [DllImport("kernel32", SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        #endregion
    }
}
