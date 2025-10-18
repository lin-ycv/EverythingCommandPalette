using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.IO;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class Delete : InvokableCommand
    {
        private string _fullPath;
        private bool _isFolder;
        internal Delete(string path, bool folder)
        {
            _fullPath = path;
            _isFolder = folder;
        }
        public override CommandResult Invoke()
        {
            // Use fool-proof win32helper
            Win32Helper.Execute(_fullPath, "--delete");

            // should use FileSystem.DeleteFile
            // but doesn't work on non-ui thread
            //if (_isFolder)
            //    Directory.Delete(_fullPath, true);
            //else
            //    File.Delete(_fullPath);

            return CommandResult.Dismiss();
        }
    }
    internal sealed partial class DeleteCommand : InvokableCommand
    {
        private readonly string _fullPath;
        private readonly bool _isFolder;
        internal DeleteCommand(string fullPath, bool isFolder)
        {
            _fullPath = fullPath;
            _isFolder = isFolder;
            Name = Resources.delete;
            Icon = new IconInfo("\uE74D");
        }
        public override CommandResult Invoke()
        {
            var args = new ConfirmationArgs()
            {
                Title = Resources.delete_confirm,
                Description = $"{_fullPath}\n\n{Resources.delete_warn}",
                PrimaryCommand = new Delete(_fullPath, _isFolder),
                IsPrimaryCommandCritical = true,
            };
            return CommandResult.Confirm(args);
        }
    }
}
