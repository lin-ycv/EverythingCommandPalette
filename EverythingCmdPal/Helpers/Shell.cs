using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EverythingCmdPal.Helpers
{
    internal static class ShellHelper
    {
        // Allow any process to set the foreground window.
        // Required because this extension runs as a background COM server
        // which Windows does not grant foreground activation rights to.
        private const int ASFW_ANY = -1;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        /// <summary>
        /// commonly used helper to launch things in shell differently
        /// </summary>
        /// <param name="path">the thing to launch, ie "explorer.exe"</param>
        /// <param name="msg">variable used to log any exception messages</param>
        /// <param name="args">optional arguments to pass</param>
        /// <param name="dir">working directory, ie for when opening in terminal</param>
        /// <param name="verb">runAs, runAsUser, open</param>
        /// <returns>bool to indicate execution success</returns>
        internal static bool OpenInShell(string path, ref string msg, string? args = null, string? dir = null, string verb = "")
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = string.IsNullOrEmpty(args) ? string.Empty : args,
                FileName = path,
                UseShellExecute = true,
                Verb = verb,
                WorkingDirectory = string.IsNullOrEmpty(dir) ? string.Empty : dir,
            };
            try
            {
                // Grant the launched process permission to steal foreground
                AllowSetForegroundWindow(ASFW_ANY);
                Process.Start(startInfo);
                return true;
            }
            catch (Exception e)
            {
                msg = e.Message;
                ExtensionHost.LogMessage($"ECP: Failed to launch: {e.Message}");
                return false;
            }
        }
    }
}
