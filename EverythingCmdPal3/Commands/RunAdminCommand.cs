using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class RunAdminCommand(string p, string instance) : InvokableCommand
    {
            public override string Name => Resources.run_admin;
            public override IconInfo Icon => new("\uE7EF");

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (Shell.OpenInShell(p, ref msg, null, Path.GetDirectoryName(p), "runAs"))
            {
                EverythingSearchService.RecordRun(p, instance);
                return CommandResult.Dismiss();
            }
            else
            {
                StatusNotifications.Show(new StatusMessage() { Message = $"{Resources.run_admin_exception}\n{p}\n{msg}", State = MessageState.Error });
                return CommandResult.KeepOpen();
            }
        }
    }
}
