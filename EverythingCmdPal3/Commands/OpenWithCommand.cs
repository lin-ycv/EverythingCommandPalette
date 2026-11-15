using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands;

internal sealed partial class OpenWithCommand(string path, string instance) : InvokableCommand
{
    public override string Name => Resources.open_with;
    public override IconInfo Icon => new("\uE7AC");
    public override CommandResult Invoke()
    {
        if (ShellFileActions.Invoke(path, "openas", out string error))
        {
            EverythingSearchService.RecordRun(path, instance);
            return CommandResult.Dismiss();
        }
        StatusNotifications.Show(new StatusMessage { Message = error, State = MessageState.Error });
        return CommandResult.KeepOpen();
    }
}
