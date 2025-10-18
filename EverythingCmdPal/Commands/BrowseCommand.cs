using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class BrowseCommand(string q, DynamicListPage p) : InvokableCommand
    {
        public override string Name => Resources.browse;
        public override IconInfo Icon => new("\uF89A");

        public override CommandResult Invoke()
        {
            //p.SearchText = $"{q}";
            p.UpdateSearchText("", q);
            return CommandResult.KeepOpen();
        }
    }
}
