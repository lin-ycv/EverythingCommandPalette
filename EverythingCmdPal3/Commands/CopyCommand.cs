using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace EverythingCmdPal3.Commands;

internal sealed partial class CopyCommand(string path, string instance) : InvokableCommand
{
    public override string Name => Resources.copy;
    public override IconInfo Icon => new("\uE8C8");
    public override CommandResult Invoke()
    {
        try
        {
            Win32Helper.Execute(path, "--copy");
            EverythingSearchService.RecordRun(path, instance);
            return CommandResult.Dismiss();
        }
        catch (Exception ex)
        {
            StatusNotifications.Show(new StatusMessage { Message = ex.Message, State = MessageState.Error });
            return CommandResult.KeepOpen();
        }
    }
}
