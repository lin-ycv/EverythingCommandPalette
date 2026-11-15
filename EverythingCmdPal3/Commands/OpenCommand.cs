using EverythingCmdPal3.Helpers;
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
        private readonly string? _instance;
        private readonly bool _isFolder;

        internal OpenCommand(string fullname, string? instance = null, bool isFolder = false)
        {
            _fullPath = fullname;
            _instance = instance;
            _isFolder = isFolder;
            Name = Resources.open_file;
            Icon = new("\uE8E5");
        }

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (Shell.OpenInShell(_fullPath, ref msg, dir: _isFolder ? _fullPath : System.IO.Path.GetDirectoryName(_fullPath)))
            {
                if (_instance is not null) EverythingSearchService.RecordRun(_fullPath, _instance);
                return CommandResult.Dismiss();
            }
            else
            {
                StatusNotifications.Show(new StatusMessage() { Message = $"{Resources.open_exception}\n{_fullPath}\n{msg}", State = MessageState.Error });
                return CommandResult.KeepOpen();
            }
        }
    }
}
