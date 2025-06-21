using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class ShowMore(string q, string e) : InvokableCommand
    {
        public override IconInfo Icon => new("\uf78b");
        public override string Name => Resources.show_more;
        public override ICommandResult Invoke()
        {
            string m = string.Empty;
            if(Shell.OpenInShell(e, ref m, $@"-s ""{q.Replace("\"", "\"\"\"")}"""))
                return CommandResult.Dismiss();
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.ERROR_UNKNOWN}\n{q}\n{m}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }

    }
}
