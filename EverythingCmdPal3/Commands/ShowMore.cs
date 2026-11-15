using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands;

internal sealed partial class ShowMore(string query, string executable, string instance) : InvokableCommand
{
    public override IconInfo Icon => new("\uF78B");
    public override string Name => Resources.show_more;
    public override ICommandResult Invoke()
    {
        string message = string.Empty;
        string args = CommandLine.EverythingSearch(query, instance);
        if (Shell.OpenInShell(executable, ref message, args)) return CommandResult.Dismiss();
        StatusNotifications.Show(new StatusMessage { Message = $"{Resources.ERROR_UNKNOWN}\n{message}", State = MessageState.Error });
        return CommandResult.KeepOpen();
    }
}
