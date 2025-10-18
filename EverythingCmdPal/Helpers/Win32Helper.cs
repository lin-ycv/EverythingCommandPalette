using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EverythingCmdPal.Helpers
{
    internal static class Win32Helper
    {
        internal static void Execute(string fullPath, string arg)
        {
            // Use fool-proof win32helper
            string exe = Path.Combine(AppContext.BaseDirectory, "Win32", "Helper.exe");
            var psi = new ProcessStartInfo(exe)
            {
                Arguments = $"\"{fullPath}\" {arg}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{fullPath}\n{error}");
        }
    }
}
