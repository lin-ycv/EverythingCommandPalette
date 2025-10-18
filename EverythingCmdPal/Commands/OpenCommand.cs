using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class OpenCommand : InvokableCommand
    {
        private readonly string _fullPath;
        private readonly bool _isFolder;

        internal OpenCommand(string fullname, bool folder)
        {
            _fullPath = fullname;
            _isFolder= folder;
            Name = Resources.open_file;
            Icon = new("\uE8E5");
        }

        public override CommandResult Invoke()
        {
            var path = _isFolder ? _fullPath : Path.GetDirectoryName(_fullPath);
            string msg = string.Empty;
            if (ShellHelper.OpenInShell(_fullPath, ref msg, null, path))
            {
                _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.open_exception}\n{_fullPath}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
