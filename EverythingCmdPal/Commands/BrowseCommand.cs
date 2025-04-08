using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class BrowseCommand : InvokableCommand
    {
        private readonly string _fullPath;
        private readonly bool _isfolder;
        private readonly DynamicListPage _page;

        internal BrowseCommand(string fullname, bool isFolder, DynamicListPage page)
        {
            _fullPath = fullname;
            _isfolder = isFolder;
            _page = page;
            Name = Resources.browse;
            Icon = new IconInfo("\uF89A");
        }

        public override CommandResult Invoke()
        {
            _page.SearchText = $"\"{(_isfolder ? _fullPath : Path.GetDirectoryName(_fullPath))}\\\" ";
            return CommandResult.KeepOpen();
        }
    }
}
