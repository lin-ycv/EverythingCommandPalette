using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading.Tasks;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class OpenWithCommand(string p) : InvokableCommand
    {
        public override string Name => Resources.open_with;
        public override IconInfo Icon => new("\uE7AC");

        private async Task<bool> OpenWith()
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(p);
                if (file != null)
                {
                    // Set the option to show the "Open With" dialog
                    var options = new Windows.System.LauncherOptions
                    {
                        DisplayApplicationPicker = true
                    };

                    // Launch the file with the "Open With" dialog
                    return await Windows.System.Launcher.LaunchFileAsync(file, options);
                }
                else
                    ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.file_not_found}\n{p}", State = MessageState.Error }, StatusContext.Page);
            }
            catch (Exception e)
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.ERROR_UNKNOWN}\n{p}\n{e.Message}", State = MessageState.Error }, StatusContext.Page);
            }
            return false;
        }

        public override CommandResult Invoke()
        {
            if (OpenWith().Result)
            {
                _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client, p);
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.ERROR_UNKNOWN}\n{p}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
