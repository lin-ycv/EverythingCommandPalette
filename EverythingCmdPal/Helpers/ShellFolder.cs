using System;
using System.Runtime.InteropServices;

namespace EverythingCmdPal.Helpers;

internal static class ShellFolder
{
    internal static bool OpenAndSelect(string path, out string error)
    {
        const int ChangedApartment = unchecked((int)0x80010106);
        // Command callbacks normally run in the extension's MTA. Also accept
        // an existing STA, and balance only successful COM initializations.
        int initialized = CoInitializeEx(0, 0);
        nint pidl = 0;
        try
        {
            if (initialized < 0 && initialized != ChangedApartment)
                Marshal.ThrowExceptionForHR(initialized);
            Marshal.ThrowExceptionForHR(SHParseDisplayName(path, 0, out pidl, 0, out _));
            if (pidl == 0) throw new InvalidOperationException("The Shell could not resolve this item.");
            // cidl=0 means this is the full item PIDL: open its parent and
            // select it. Opus supports intercepting this standard Shell API.
            Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidl, 0, 0, 0));
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            if (pidl != 0) Marshal.FreeCoTaskMem(pidl);
            if (initialized >= 0) CoUninitialize();
        }
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoInitializeEx(nint reserved, uint model);
    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void CoUninitialize();
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHParseDisplayName(string name, nint bindContext,
        out nint pidl, uint attributes, out uint foundAttributes);
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHOpenFolderAndSelectItems(nint pidl, uint count, nint children, uint flags);
}
