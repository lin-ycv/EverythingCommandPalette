using System;
using System.Diagnostics;
using System.IO;

namespace EverythingCmdPal3.Helpers;

internal static class Win32Helper
{
    internal static void Execute(string path, string action)
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Win32", "Helper.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(path);
        start.ArgumentList.Add(action);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start the file-action helper.");
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new IOException(string.IsNullOrWhiteSpace(error) ? "The file action did not complete." : error);
    }
}
