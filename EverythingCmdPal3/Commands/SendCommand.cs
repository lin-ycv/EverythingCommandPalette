using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class SendCommand(string p, ResultsPage page) : InvokableCommand
    {
        public override string Name => Resources.sendto;
        public override IconInfo Icon => new("\uE8A7");

        public override CommandResult Invoke()
        {
            string[] s = page.settings.Sendto.Split(',');
            s[1] = s[1].Replace("$P$", p);
            string msg = string.Empty;
            if (Shell.OpenInShell(s[0], ref msg, $"\"{s[1]}\""))
            {
                _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client, p);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.sendto_failed}\n\"{s[0]}\" \"{s[1]}\"\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
