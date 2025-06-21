using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class OpenConsoleCommand(string p) : InvokableCommand
    {
        public override string Name => Resources.open_console;
        public override IconInfo Icon => new("\uE756");

        public override CommandResult Invoke()
        {
            var path = Path.GetDirectoryName(p);
            string msg = string.Empty;
            if (Shell.OpenInShell("cmd.exe", ref msg, null, path))
            {
                _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client, p);
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
