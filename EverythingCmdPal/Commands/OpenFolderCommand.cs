using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class OpenFolderCommand : InvokableCommand
    {
        private readonly string _fullPath;

        internal OpenFolderCommand(string fullname)
        {
            _fullPath = fullname;
            Name = Resources.open_folder;
            Icon = new("\uE838");
        }

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (ShellHelper.OpenInShell("explorer.exe", ref msg, $"/select,\"{_fullPath}\""))
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
