using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingCmdPal.Commands
{
    /// <summary>
    /// Works, but neither method can appear topmost
    /// Difference: CsWin method have set always and just once options
    /// none CsWin method only have just once option
    /// </summary>
    internal sealed partial class OpenWithCommand : InvokableCommand
    {
        private readonly string _fullPath;

        private static unsafe bool OpenWith(string filename)
        {
            var filenamePtr = Marshal.StringToHGlobalUni(filename);
            var verbPtr = Marshal.StringToHGlobalUni("openas");

            try
            {
                var filenamePCWSTR = new PCWSTR((char*)filenamePtr);
                var verbPCWSTR = new PCWSTR((char*)verbPtr);

                var info = new SHELLEXECUTEINFOW
                {
                    cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
                    lpVerb = verbPCWSTR,
                    lpFile = filenamePCWSTR,
                    nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL,
                    fMask = 0x0000000C | 0x00000100, //SEE_MASK_INVOKEIDLIST+SEE_MASK_NOASYNC
                    //hwnd = PInvoke.GetActiveWindow(), // need to get cmdpal window handle
                };

                return PInvoke.ShellExecuteEx(ref info);
            }
            finally
            {
                Marshal.FreeHGlobal(filenamePtr);
                Marshal.FreeHGlobal(verbPtr);
            }
        }

        //private async void OpenWith(string filename)
        //{
        //    try
        //    {
        //        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(_fullPath);
        //        if (file != null)
        //        {
        //            // Set the option to show the "Open With" dialog
        //            var options = new Windows.System.LauncherOptions
        //            {
        //                DisplayApplicationPicker = true
        //            };

        //            // Launch the file with the "Open With" dialog
        //            Windows.System.Launcher.LaunchFileAsync(file, options);
        //        }
        //        else
        //            ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.file_not_found}\n{_fullPath}", State = MessageState.Error }, StatusContext.Page);
        //    }
        //    catch (Exception e)
        //    {
        //        ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.unknown_error}\n{_fullPath}\n{e.Message}", State = MessageState.Error }, StatusContext.Page);
        //    }
        //}

        internal OpenWithCommand(string fullPath)
        {
            _fullPath = fullPath;
            Name = Resources.open_with;
            Icon = new IconInfo("\uE7AC");
        }

        public override CommandResult Invoke()
        {
            if (OpenWith(_fullPath))
                return CommandResult.Dismiss();
            else
            {
                //ExtensionHost.LogMessage($"EPT-CP: {Resources.unknown_error}\n{_fullPath}");
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.unknown_error}\n{_fullPath}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
