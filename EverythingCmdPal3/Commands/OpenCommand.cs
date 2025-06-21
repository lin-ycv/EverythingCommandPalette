using static EverythingCmdPal3.Interop.NativeMethods;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class OpenCommand : InvokableCommand
    {
        private readonly string _fullPath;

        internal OpenCommand(string fullname)
        {
            _fullPath = fullname;
            Name = Resources.open_file;
            Icon = new("\uE8E5");
        }

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (Shell.OpenInShell(_fullPath, ref msg))
            {
                _ = Everything3_IncRunCountFromFilenameW(ResultsPage.Client, _fullPath);
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
