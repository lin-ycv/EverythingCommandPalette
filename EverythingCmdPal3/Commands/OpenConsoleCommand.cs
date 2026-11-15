using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class OpenConsoleCommand(string p, bool folder, string instance) : InvokableCommand
    {
        public override string Name => Resources.open_console;
        public override IconInfo Icon => new("\uE756");

        public override CommandResult Invoke()
        {
            var path = folder ? p : Path.GetDirectoryName(p);
            string msg = string.Empty;
            if (Shell.OpenInShell("cmd.exe", ref msg, null, path))
            {
                EverythingSearchService.RecordRun(p, instance);
                return CommandResult.Dismiss();
            }
            else
            {
                StatusNotifications.Show(new StatusMessage() { Message = $"{Resources.open_console_exception}\n{path}\n{msg}", State = MessageState.Error });
                return CommandResult.KeepOpen();
            }
        }
    }
}
