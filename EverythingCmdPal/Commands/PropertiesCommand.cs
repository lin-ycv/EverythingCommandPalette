using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class PropertiesCommand : InvokableCommand
    {
        private readonly string _fullPath;

        private static unsafe bool ShowFileProperties(string filename)
        {
            var propertiesPtr = Marshal.StringToHGlobalUni("properties");
            var filenamePtr = Marshal.StringToHGlobalUni(filename);

            try
            {
                var filenamePCWSTR = new PCWSTR((char*)filenamePtr);
                var propertiesPCWSTR = new PCWSTR((char*)propertiesPtr);

                var info = new SHELLEXECUTEINFOW
                {
                    cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
                    lpVerb = propertiesPCWSTR,
                    lpFile = filenamePCWSTR,
                    nShow = (int)SHOW_WINDOW_CMD.SW_SHOW,
                    fMask = 0x0000000C,
                };

                return PInvoke.ShellExecuteEx(ref info);
            }
            finally
            {
                Marshal.FreeHGlobal(filenamePtr);
                Marshal.FreeHGlobal(propertiesPtr);
            }
        }

        internal PropertiesCommand(string fullname)
        {
            _fullPath = fullname;
            Name = Resources.open_properties;
            Icon = new IconInfo("\uE90F");
        }

        public override CommandResult Invoke()
        {
            try
            {
                ShowFileProperties(_fullPath);
            }
            catch (Exception e)
            {
                //ExtensionHost.LogMessage($"EPT-CP: {Resources.unknown_error}\n{e.Message}");
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{e.Message}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
            return CommandResult.Dismiss();
        }
    }
}
