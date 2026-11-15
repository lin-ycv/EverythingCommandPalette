using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Helpers;

internal static class StatusNotifications
{
    internal static void Show(StatusMessage message) => new ToastStatusMessage(message)
    {
        // Retains the original message object and hides that same object.
        Duration = message.State == MessageState.Success ? 2500 : 6000,
    }.Show();
}
