using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class SendCommand : InvokableCommand
    {
        private readonly string _fullPath, _exe, _arg;
        internal SendCommand(string fullPath, string exe)
        {
            _fullPath = fullPath;
            Name = Resources.sendto;
            Icon = new("\uE8A7");
            var split = exe.Split(',');
            _exe = split[0];
            _arg = split[1].Replace("$P$", _fullPath);
        }

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (ShellHelper.OpenInShell(_exe, ref msg, $"\"{_arg}\""))
            {
                _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.sendto_failed}\n\"{_exe}\" \"{_arg}\"\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
