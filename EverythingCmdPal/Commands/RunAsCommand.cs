using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class RunAsCommand : InvokableCommand
    {
        private readonly string _fullPath;

        internal RunAsCommand(string fullname)
        {
            _fullPath = fullname;
            Name = Resources.run_as;
            Icon = new("\uE7EE");
        }

        public override CommandResult Invoke()
        {
            var path = Path.GetDirectoryName(_fullPath);
            string msg = string.Empty;
            if (ShellHelper.OpenInShell(_fullPath, ref msg, null, path, "runAsUser"))
            {
                _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.run_as_exception}\n{_fullPath}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
