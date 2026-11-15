using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace EverythingCmdPal3.Helpers;

internal static class ShellFileActions
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellExecuteInfo
    {
        internal uint Size, Mask;
        internal nint Window;
        [MarshalAs(UnmanagedType.LPWStr)] internal string Verb;
        [MarshalAs(UnmanagedType.LPWStr)] internal string File;
        [MarshalAs(UnmanagedType.LPWStr)] internal string? Parameters;
        [MarshalAs(UnmanagedType.LPWStr)] internal string? Directory;
        internal int Show;
        internal nint Instance, IdList;
        [MarshalAs(UnmanagedType.LPWStr)] internal string? Class;
        internal nint ClassKey;
        internal uint HotKey;
        internal nint Icon, Process;
    }

    [DllImport("shell32.dll", EntryPoint = "ShellExecuteExW", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellExecuteEx(ref ShellExecuteInfo info);

    internal static bool Invoke(string path, string verb, out string error)
    {
        var info = new ShellExecuteInfo
        {
            Size = (uint)Marshal.SizeOf<ShellExecuteInfo>(),
            Mask = 0xC | 0x100, // INVOKEIDLIST | NOASYNC; launch under the host's foreground grant.
            Verb = verb,
            File = path,
            Show = 1,
        };
        bool success = ShellExecuteEx(ref info);
        error = success ? string.Empty : new Win32Exception(Marshal.GetLastWin32Error()).Message;
        return success;
    }
}
