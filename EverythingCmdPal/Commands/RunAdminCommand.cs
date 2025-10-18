using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class RunAdminCommand : InvokableCommand
    {
        private readonly string _fullPath;
        private readonly bool _isFolder;

        internal RunAdminCommand(string fullname, bool isFolder)
        {
            _fullPath = fullname;
            Name = Resources.run_admin;
            Icon = new("\uE7EF");
            _isFolder = isFolder;
        }

        public override CommandResult Invoke()
        {
            var path = _isFolder ? _fullPath : Path.GetDirectoryName(_fullPath);
            string msg = string.Empty;
            if (ShellHelper.OpenInShell(_fullPath, ref msg, null, path, "runAs"))
            {
                _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.run_admin_exception}\n{_fullPath}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
