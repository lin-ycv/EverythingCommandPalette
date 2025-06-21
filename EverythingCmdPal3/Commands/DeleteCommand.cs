using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace EverythingCmdPal3.Commands
{
    internal sealed partial class Delete(string p, bool f) : InvokableCommand
    {
        public override CommandResult Invoke()
        {
            // should use FileSystem.DeleteFile
            // but doesn't work on non-ui thread
            if (f)
                Directory.Delete(p, true);
            else
                File.Delete(p);
            return CommandResult.Dismiss();
        }
    }
    internal sealed partial class DeleteCommand(string p, bool f) : InvokableCommand
    {
        public override string Name => Resources.delete;
        public override IconInfo Icon => new("\uE74D");

        public override CommandResult Invoke()
        {
            var args = new ConfirmationArgs()
            {
                Title = Resources.delete_confirm,
                Description = $"{p}\n\n{Resources.delete_warn}",
                PrimaryCommand = new Delete(p, f),
                IsPrimaryCommandCritical = true,
            };
            return CommandResult.Confirm(args);
        }
    }
}
