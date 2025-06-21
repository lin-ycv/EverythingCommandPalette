using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class OpenFolderCommand(string p) : InvokableCommand
    {
        public override string Name => Resources.open_folder;
        public override IconInfo Icon => new("\uE838");

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (Shell.OpenInShell("explorer.exe", ref msg, $"/select,\"{p}\""))
            {
                _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client, p);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.open_exception}\n{p}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
