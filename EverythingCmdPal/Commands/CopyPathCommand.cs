using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class CopyPathCommand : InvokableCommand
    {
        private readonly string _fullPath;

        internal CopyPathCommand(string fullname)
        {
            _fullPath = fullname;
            Name = Resources.copy_path;
            Icon = new IconInfo("\uE71B");
        }

        public override CommandResult Invoke()
        {
            // Use fool-proof win32helper
            Win32Helper.Execute(_fullPath, "--as-text");

            //try
            //{
            //    ClipboardHelper.SetText(_fullPath);
            //    ExtensionHost.ShowStatus(new StatusMessage() { Message = Resources.copy_path_ok, State = MessageState.Success }, StatusContext.Page);
            //}
            //catch (Exception e)
            //{
            //    //ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{_fullPath}\n{e.Message}");
            //    ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.clipboard_failed}\n{_fullPath}\n{e.Message}", State = MessageState.Error }, StatusContext.Page);
            //}
            _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
            return CommandResult.KeepOpen();
        }
    }
}
