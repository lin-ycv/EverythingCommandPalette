using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class CopyCommand : InvokableCommand
    {
        private readonly string _fullPath;
        private readonly bool _isFolder;

        internal CopyCommand(string fullname, bool isFolder)
        {
            _fullPath = fullname;
            _isFolder = isFolder;
            Name = Resources.copy;
            Icon = new IconInfo("\uE8C8");
        }

        public override CommandResult Invoke()
        {
            //// apparently Clipboard.SetContentWithOptions only works on UI thread?
            //// Clipboardhelper currently only deals with texts
            Clipboard.Clear();
            DataPackage package = new() { RequestedOperation = DataPackageOperation.Copy };
            Task.Run(async () =>
            {
                try
            {
                package.SetStorageItems([
                    _isFolder ?
                        await StorageFolder.GetFolderFromPathAsync(_fullPath)/*.GetResults()*/:
                        await StorageFile.GetFileFromPathAsync(_fullPath)/*.GetResults()*/,
                        ]);
                if (!Clipboard.SetContentWithOptions(package, new ClipboardContentOptions()))
                {
                    throw new InvalidOperationException("SetContentWithOptions returned false");
                }
                _ = NativeMethods.Everything_IncRunCountFromFileNameW(_fullPath);
                Clipboard.Flush();
            }
            catch (Exception e)
            {
                ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{_fullPath}\n{e.Message}");
                //ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.clipboard_failed}\n{_fullPath}", State = MessageState.Error }, StatusContext.Page);
            }
            });

            return CommandResult.Dismiss();
        }
    }
}
