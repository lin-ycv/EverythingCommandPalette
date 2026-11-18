using EverythingCmdPal.Helpers;
using EverythingCmdPal.Interop;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.IO;
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
            try
            {
                // Resolve the storage item before copying, so the clipboard data survives CmdPal closing.
                Task.Run(async () =>
                {
                    IStorageItem item = _isFolder
                        ? await StorageFolder.GetFolderFromPathAsync(_fullPath)
                        : await StorageFile.GetFileFromPathAsync(_fullPath);

                    DataPackage package = new() { RequestedOperation = DataPackageOperation.Copy };
                    package.SetStorageItems(new IStorageItem[] { item });

                    // The packaged CmdPal SDK cannot set arbitrary clipboard content.
                    // Use an STA thread for the WinRT clipboard APIs instead.
                    Exception? clipboardError = null;
                    var thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            Clipboard.SetContent(package);
                            Clipboard.Flush();
                        }
                        catch (Exception ex)
                        {
                            clipboardError = ex;
                        }
                    });
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.Start();
                    thread.Join();

                    if (clipboardError is not null)
                        throw clipboardError;
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{_fullPath}\n{ex}");
                return CommandResult.KeepOpen();
            }

            //// apparently Clipboard.SetContentWithOptions only works on UI thread?
            //// Clipboardhelper currently only deals with texts
            //Clipboard.Clear();
            //DataPackage package = new() { RequestedOperation = DataPackageOperation.Copy };
            //Task.Run(async () =>
            //{
            //    try
            //{
            //    package.SetStorageItems([
            //        _isFolder ?
            //            await StorageFolder.GetFolderFromPathAsync(_fullPath)/*.GetResults()*/:
            //            await StorageFile.GetFileFromPathAsync(_fullPath)/*.GetResults()*/,
            //            ]);
            //    if (!Clipboard.SetContentWithOptions(package, new ClipboardContentOptions()))
            //    {
            //        throw new InvalidOperationException("SetContentWithOptions returned false");
            //    }
            //    Query.IncrementRunCount(_fullPath);
            //    Clipboard.Flush();
            //}
            //catch (Exception e)
            //{
            //    ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{_fullPath}\n{e.Message}");
            //    //ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.clipboard_failed}\n{_fullPath}", State = MessageState.Error }, StatusContext.Page);
            //}
            //});

            Query.IncrementRunCount(_fullPath);
            return CommandResult.Dismiss();
        }
    }
}
