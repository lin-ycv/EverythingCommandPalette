using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class StartEverythingCommand : InvokableCommand
    {
        internal StartEverythingCommand()
        {
            Name = "";
            Icon = new IconInfo("\uE819");
        }

        public override CommandResult Invoke()
        {
            string _path = Query.Settings.Exe;
            string msg = string.Empty, verb = string.Empty;
            if (string.IsNullOrEmpty(_path))
            {
                _path = "https://www.voidtools.com/";
                verb = "open";
            }
            if (!ShellHelper.OpenInShell(_path, ref msg, null, null, verb))
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.unknown_error}\n{msg}", State = MessageState.Error }, StatusContext.Page);

            return CommandResult.GoHome();
        }
    }
}
