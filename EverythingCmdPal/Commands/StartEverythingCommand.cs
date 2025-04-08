using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class StartEverythingCommand : InvokableCommand
    {
        private Results _page;
        internal StartEverythingCommand(Results page)
        {
            _page = page;
            Name = "";
            Icon = new IconInfo("\uE819");
        }

        public override CommandResult Invoke()
        {
            string _path = _page._settings.Exe;
            if (!File.Exists(_path))
            {
                _path = Path.Exists("C:\\Program Files\\Everything 1.5a\\Everything64.exe") ? "C:\\Program Files\\Everything 1.5a\\Everything64.exe" :
                    (Path.Exists("C:\\Program Files\\Everything\\Everything.exe") ? "C:\\Program Files\\Everything\\Everything.exe" :
                    (Path.Exists("C:\\Program Files (x86)\\Everything 1.5a\\Everything.exe") ? "C:\\Program Files (x86)\\Everything 1.5a\\Everything.exe" :
                    (Path.Exists("C:\\Program Files (x86)\\Everything\\Everything.exe") ? "C:\\Program Files (x86)\\Everything\\Everything.exe" : string.Empty)));
            }
            string msg = string.Empty, verb = string.Empty;
            if (string.IsNullOrEmpty(_path))
            {
                _path = "https://www.voidtools.com/";
                verb = "open";
            }
            if (!ShellHelper.OpenInShell(_path, ref msg, null, null, verb))
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.unknown_error}\n{msg}", State = MessageState.Error }, StatusContext.Page);

            _page.SearchText = string.Empty;
            return CommandResult.KeepOpen();
        }
    }
}
