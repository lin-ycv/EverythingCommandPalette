using EverythingCmdPal3.Interop;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class CopyPathCommand(string p) : InvokableCommand
    {
       public override string Name => Resources.copy_path;
       public override IconInfo Icon => new("\uE71B");
        

        public override CommandResult Invoke()
        {
            try
            {
                ClipboardHelper.SetText(p);
                ExtensionHost.ShowStatus(new StatusMessage() { Message = Resources.copy_path_ok, State = MessageState.Success }, StatusContext.Page);
            }
            catch (Exception e)
            {
                //ExtensionHost.LogMessage($"EPT-CP: {Resources.clipboard_failed}\n{_fullPath}\n{e.Message}");
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.clipboard_failed}\n{p}\n{e.Message}", State = MessageState.Error }, StatusContext.Page);
            }
            _ = NativeMethods.Everything3_IncRunCountFromFilenameW(ResultsPage.Client, p);
            return CommandResult.KeepOpen();
        }
    }
}
