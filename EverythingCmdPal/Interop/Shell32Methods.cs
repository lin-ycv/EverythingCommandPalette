using System;
using System.Runtime.InteropServices;

namespace EverythingCmdPal.Interop
{
    internal static class Shell32Methods
    {

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        private const uint SHGFI_TYPENAME = 0x000000400;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static string? GetFileTypeDescription(string path, bool isFolder)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            // The query already tells us whether this is a folder. With
            // USEFILEATTRIBUTES there is no need to hit the filesystem again.
            uint attributes = isFolder ? 0x10u : 0x80u;
            IntPtr result = SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);

            // Defensively free the icon handle if SHGetFileInfo populated it
            if (shinfo.hIcon != IntPtr.Zero)
                DestroyIcon(shinfo.hIcon);

            if (result != IntPtr.Zero)
            {
                return shinfo.szTypeName;
            }

            return null;
        }
    }
}
