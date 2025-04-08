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

        internal OpenConsoleCommand(string fullPath)
        {
            _fullPath = fullPath;
            Name = Resources.open_console;
            Icon = new IconInfo("\uE756");
        }

        public override CommandResult Invoke()
        {
            var path = Path.GetDirectoryName(_fullPath);
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
