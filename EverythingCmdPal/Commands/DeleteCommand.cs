using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.VisualBasic.FileIO;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

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
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    if (_isFolder)
                        FileSystem.DeleteDirectory(_fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    else
                        FileSystem.DeleteFile(_fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure is not null)
            {
                ExtensionHost.LogMessage($"EPT-CP: Failed to recycle {_fullPath}\n{failure}");
                return CommandResult.KeepOpen();
            }

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
                Description = $"{_fullPath}",
                PrimaryCommand = new Delete(_fullPath, _isFolder),
                IsPrimaryCommandCritical = true,
            };
            return CommandResult.Confirm(args);
        }
    }
}
