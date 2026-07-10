using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EverythingCmdPal.Helpers
{
    internal static class ShellHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        private const int ASFW_ANY = -1;

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
                // Capture foreground window info BEFORE launching
                IntPtr foregroundHwnd = GetForegroundWindow();
                uint foregroundThread = GetWindowThreadProcessId(foregroundHwnd, out _);
                uint currentThread = GetCurrentThreadId();

                // Attach our thread to the foreground thread's input queue
                // so Windows treats us as if we are the foreground process.
                bool attached = false;
                if (foregroundThread != currentThread)
                    attached = AttachThreadInput(currentThread, foregroundThread, true);

                // Grant any process permission to set foreground
                AllowSetForegroundWindow(ASFW_ANY);

                Process? proc = Process.Start(startInfo);

                // If we got a process handle, wait briefly for its window and bring to front
                if (proc != null)
                {
                    try
                    {
                        // Allow the launched process to set itself foreground
                        AllowSetForegroundWindow(proc.Id);

                        // Give the process a moment to create its window
                        proc.WaitForInputIdle(1000);

                        if (proc.MainWindowHandle != IntPtr.Zero)
                            SetForegroundWindow(proc.MainWindowHandle);
                    }
                    catch
                    {
                        // Ignore — process may have exited, be console-only, etc.
                    }
                }

                // Detach
                if (attached)
                    AttachThreadInput(currentThread, foregroundThread, false);

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
