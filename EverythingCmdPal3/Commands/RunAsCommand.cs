using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class RunAsCommand(string p, string instance) : InvokableCommand
    {
        public override string Name => Resources.run_as;
        public override IconInfo Icon => new("\uE7EE");

        public override CommandResult Invoke()
        {
            var path = Path.GetDirectoryName(p);
            string msg = string.Empty;
            if (Shell.OpenInShell(p, ref msg, null, path, "runAsUser"))
            {
                EverythingSearchService.RecordRun(p, instance);
                return CommandResult.Dismiss();
            }
            else
            {
                StatusNotifications.Show(new StatusMessage() { Message = $"{Resources.run_as_exception}\n{p}\n{msg}", State = MessageState.Error });
                return CommandResult.KeepOpen();
            }
        }
    }
}
