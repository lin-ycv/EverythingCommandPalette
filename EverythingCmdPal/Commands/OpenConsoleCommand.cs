using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class OpenConsoleCommand : InvokableCommand
    {
        private readonly string _fullPath;
        private readonly bool _isFolder;

        internal OpenConsoleCommand(string fullPath, bool isFolder)
        {
            _fullPath = fullPath;
            Name = Resources.open_console;
            Icon = new IconInfo("\uE756");
            _isFolder = isFolder;
        }

        public override CommandResult Invoke()
        {
            var path = _isFolder ? _fullPath : Path.GetDirectoryName(_fullPath);
            string msg = string.Empty;
            if (ShellHelper.OpenInShell("cmd.exe", ref msg, null, path))
            {
                _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.open_console_exception}\n{path}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
