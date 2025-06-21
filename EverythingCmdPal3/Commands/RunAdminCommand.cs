using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class RunAdminCommand(string p) : InvokableCommand
    {
            public override string Name => Resources.run_admin;
            public override IconInfo Icon => new("\uE7EF");

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (Shell.OpenInShell(p, ref msg, null, Path.GetDirectoryName(p), "runAs"))
            {
                _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client, p);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.run_admin_exception}\n{p}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
