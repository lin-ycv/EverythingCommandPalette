﻿using System;
using System.Runtime.InteropServices;

namespace EverythingCmdPal.Interop
{
    internal sealed class NativeMethods
    {
        [Flags]
        internal enum Request
        {
            FILE_NAME = 0x00000001,
            PATH = 0x00000002,
            FULL_PATH_AND_FILE_NAME = 0x00000004,
            EXTENSION = 0x00000008,
            SIZE = 0x00000010,
            DATE_CREATED = 0x00000020,
            DATE_MODIFIED = 0x00000040,
            DATE_ACCESSED = 0x00000080,
            ATTRIBUTES = 0x00000100,
            FILE_LIST_FILE_NAME = 0x00000200,
            RUN_COUNT = 0x00000400,
            DATE_RUN = 0x00000800,
            DATE_RECENTLY_CHANGED = 0x00001000,
            HIGHLIGHTED_FILE_NAME = 0x00002000,
            HIGHLIGHTED_PATH = 0x00004000,
            HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 0x00008000,
        }

        public enum Sort
        {
            NAME_ASCENDING = 1,
            NAME_DESCENDING,
            PATH_ASCENDING,
            PATH_DESCENDING,
            SIZE_ASCENDING,
            SIZE_DESCENDING,
            EXTENSION_ASCENDING,
            EXTENSION_DESCENDING,
            TYPE_NAME_ASCENDING,
            TYPE_NAME_DESCENDING,
            DATE_CREATED_ASCENDING,
            DATE_CREATED_DESCENDING,
            DATE_MODIFIED_ASCENDING,
            DATE_MODIFIED_DESCENDING,
            ATTRIBUTES_ASCENDING,
            ATTRIBUTES_DESCENDING,
            FILE_LIST_FILENAME_ASCENDING,
            FILE_LIST_FILENAME_DESCENDING,
            RUN_COUNT_ASCENDING,
            RUN_COUNT_DESCENDING,
            DATE_RECENTLY_CHANGED_ASCENDING,
            DATE_RECENTLY_CHANGED_DESCENDING,
            DATE_ACCESSED_ASCENDING,
            DATE_ACCESSED_DESCENDING,
            DATE_RUN_ASCENDING,
            DATE_RUN_DESCENDING,
        }

        internal enum EverythingErrors : uint
        {
            EVERYTHING_OK = 0u,
            EVERYTHING_ERROR_MEMORY,
            EVERYTHING_ERROR_IPC,
            EVERYTHING_ERROR_REGISTERCLASSEX,
            EVERYTHING_ERROR_CREATEWINDOW,
            EVERYTHING_ERROR_CREATETHREAD,
            EVERYTHING_ERROR_INVALIDINDEX,
            EVERYTHING_ERROR_INVALIDCALL,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

#if ARM64
        internal const string dllName = "Everything2_ARM64.dll";
#else
        internal const string dllName = "Everything2_x64.dll";
#endif

        [DllImport(dllName)]
        internal static extern uint Everything_GetNumResults();

        //[DllImport(dllName)]
        //[return: MarshalAs(UnmanagedType.Bool)]

        //internal static extern bool Everything_GetMatchPath();

        //[DllImport(dllName)]
        //internal static extern uint Everything_GetMax();
        [DllImport(dllName)]
        internal static extern uint Everything_GetMinorVersion();

        //[DllImport(dllName)]
        //internal static extern bool Everything_GetRegex();
        [DllImport(dllName)]
        internal static extern bool Everything_GetResultDateModified(uint dwIndex, ref FILETIME lpDateModified);
        internal static string EPT_GetModifiedDateTime(uint i)
        {
            FILETIME ft = new();
            Everything_GetResultDateModified(i, ref ft);
            var dt = DateTime.FromFileTime(((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime).ToLocalTime();
            return dt.ToString("yyyy-MM-dd hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
        }
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern IntPtr Everything_GetResultExtensionW(uint dwIndex);

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern IntPtr Everything_GetResultFileNameW(uint dwIndex);

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern IntPtr Everything_GetResultPathW(uint dwIndex);
        [DllImport(dllName)]
        internal static extern bool Everything_GetResultSize(uint dwIndex, ref long lpSize);
        internal static long EPT_GetSizeKB(uint i)
        {
            long size = default;
            Everything_GetResultSize(i, ref size);
            return size / 1024;
        }

        //[DllImport(dllName)]
        //internal static extern uint Everything_GetSort();

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern uint Everything_IncRunCountFromFileNameW(string lpFileName);

        [DllImport(dllName)]
        internal static extern bool Everything_IsFolderResult(uint dwIndex);

        [DllImport(dllName)]
        internal static extern bool Everything_QueryW([MarshalAs(UnmanagedType.Bool)] bool bWait);

        [DllImport(dllName)]
        internal static extern void Everything_SetMax(uint dwMax);

        [DllImport(dllName)]
        internal static extern void Everything_SetRegex([MarshalAs(UnmanagedType.Bool)] bool bEnable);

        [DllImport(dllName)]
        internal static extern void Everything_SetRequestFlags(Request RequestFlags);

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern void Everything_SetSearchW(string lpSearchString);

        [DllImport(dllName)]
        internal static extern bool Everything_SetMatchPath([MarshalAs(UnmanagedType.Bool)] bool bEnable);

        [DllImport(dllName)]
        internal static extern void Everything_SetSort(Sort SortType);

        [DllImport(dllName)]
        internal static extern uint Everything_GetLastError();
    }
}
