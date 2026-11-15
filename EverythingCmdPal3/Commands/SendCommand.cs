using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands;

internal sealed partial class SendCommand(string path, string template, string instance) : InvokableCommand
{
    public override string Name => Resources.sendto;
    public override IconInfo Icon => new("\uE8A7");
    public override CommandResult Invoke()
    {
        if (!SendToTemplate.TryParse(template, out string executable, out string arguments))
            return CommandResult.KeepOpen();
        // Accept legacy unquoted $P$ and templates that already quote it.
        arguments = arguments.Replace("\"$P$\"", "$P$")
            .Replace("$P$", CommandLine.QuoteArgument(path));
        string message = string.Empty;
        if (Shell.OpenInShell(executable, ref message, arguments))
        {
            EverythingSearchService.RecordRun(path, instance);
            return CommandResult.Dismiss();
        }
        StatusNotifications.Show(new StatusMessage { Message = $"{Resources.sendto_failed}\n{message}", State = MessageState.Error });
        return CommandResult.KeepOpen();
    }
}
