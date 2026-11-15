using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace EverythingCmdPal3.Commands;

internal sealed partial class CopyPathCommand(string path, string instance) : InvokableCommand
{
    public override string Name => Resources.copy_path;
    public override IconInfo Icon => new("\uE71B");
    public override CommandResult Invoke()
    {
        try
        {
            Win32Helper.Execute(path, "--as-text");
            EverythingSearchService.RecordRun(path, instance);
            StatusNotifications.Show(new StatusMessage { Message = Resources.copy_path_ok, State = MessageState.Success });
        }
        catch (Exception ex)
        {
            StatusNotifications.Show(new StatusMessage { Message = ex.Message, State = MessageState.Error });
        }
        return CommandResult.KeepOpen();
    }
}
