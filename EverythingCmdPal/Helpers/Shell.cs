using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;

namespace EverythingCmdPal.Helpers
{
    internal static class ShellHelper
    {
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
                // PowerToys 0.102.2933.0+ renews foreground permission before each
                // command (PowerToys#50651). Launch synchronously while that grant
                // is valid; attaching input queues or granting ASFW_ANY here cannot
                // repair an older host and can interfere with activation.
                using Process? process = Process.Start(startInfo);

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
