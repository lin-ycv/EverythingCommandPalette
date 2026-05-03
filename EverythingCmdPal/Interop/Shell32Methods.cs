using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EverythingCmdPal.Interop
{
    internal class Shell32Methods
    {

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFileAttributes(string lpFileName);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        private const uint SHGFI_TYPENAME = 0x000000400;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static string GetFileTypeDescription(string path)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            IntPtr result = SHGetFileInfo(path, GetFileAttributes(path), ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);

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
