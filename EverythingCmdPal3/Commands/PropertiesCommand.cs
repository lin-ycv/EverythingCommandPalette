using EverythingCmdPal3.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands;

internal sealed partial class PropertiesCommand(string path) : InvokableCommand
{
    public override string Name => "Properties";
    public override IconInfo Icon => new("\uE90F");
    public override CommandResult Invoke()
    {
        if (ShellFileActions.Invoke(path, "properties", out string error)) return CommandResult.Dismiss();
        StatusNotifications.Show(new StatusMessage { Message = error, State = MessageState.Error });
        return CommandResult.KeepOpen();
    }
}
