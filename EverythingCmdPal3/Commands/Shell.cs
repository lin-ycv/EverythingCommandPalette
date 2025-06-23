﻿using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;

namespace EverythingCmdPal3.Commands
{
    internal static class Shell
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
                Process.Start(startInfo);
                return true;
            }
            catch (Exception e)
            {
                msg = e.Message;
                ExtensionHost.LogMessage($"ECP3 shell failed : {e.Message}\n{path}");
                return false;
            }
        }
    }
}
