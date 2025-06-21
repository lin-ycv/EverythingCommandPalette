using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class CopyCommand(string p, bool f) : InvokableCommand
    {
        public override string Name => Resources.copy;
        public override IconInfo Icon => new("\uE8C8");

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
                        f ?
                        await StorageFolder.GetFolderFromPathAsync(p)/*.GetResults()*/:
                        await StorageFile.GetFileFromPathAsync(p)/*.GetResults()*/,
                        ]);
                    if (!Clipboard.SetContentWithOptions(package, new ClipboardContentOptions()))
                    {
                        throw new InvalidOperationException("SetContentWithOptions returned false");
                    }
                    _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client,p);
                    Clipboard.Flush();
                }
                catch (Exception e)
                {
                    ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{p}\n{e.Message}");
                    //ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.clipboard_failed}\n{_fullPath}", State = MessageState.Error }, StatusContext.Page);
                }
            });

            return CommandResult.Dismiss();
        }
    }
}
