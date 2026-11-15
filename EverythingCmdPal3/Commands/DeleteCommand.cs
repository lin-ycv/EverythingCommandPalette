using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace EverythingCmdPal3.Commands;

internal sealed partial class DeleteCommand(string path) : InvokableCommand
{
    public override string Name => Resources.delete;
    public override IconInfo Icon => new("\uE74D");
    public override CommandResult Invoke() => CommandResult.Confirm(new ConfirmationArgs
    {
        Title = Resources.delete_confirm,
        Description = $"{path}\n\nMove this item to the Recycle Bin?",
        PrimaryCommand = new RecycleCommand(path),
        IsPrimaryCommandCritical = true,
    });

    private sealed partial class RecycleCommand(string path) : InvokableCommand
    {
        public override CommandResult Invoke()
        {
            try
            {
                Win32Helper.Execute(path, "--delete");
                return CommandResult.Dismiss();
            }
            catch (Exception ex)
            {
                StatusNotifications.Show(new StatusMessage { Message = ex.Message, State = MessageState.Error });
                return CommandResult.KeepOpen();
            }
        }
    }
}
