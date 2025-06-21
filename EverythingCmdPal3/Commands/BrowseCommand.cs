using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class BrowseCommand(string q, DynamicListPage p) : InvokableCommand
    {
        public override string Name => Resources.browse;
        public override IconInfo Icon => new("\uF89A");

        public override CommandResult Invoke()
        {
            p.SearchText = $"{q}";
            return CommandResult.KeepOpen();
        }
    }
}
