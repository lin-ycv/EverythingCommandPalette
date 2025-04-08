using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;

namespace EverythingCmdPal.Commands
{
    internal sealed partial class ShowMoreCommand : InvokableCommand
    {
        private readonly string _query;
        private readonly string _exe;

        internal ShowMoreCommand(string query, string exe)
        {
            _query = query;
            Name = Resources.show_more;
            Icon = new("\uF78B");
            _exe = exe;
        }

        public override CommandResult Invoke()
        {
            string msg = string.Empty;
            if (ShellHelper.OpenInShell(_exe, ref msg, $@"-s ""{_query.Replace("\"", "\"\"\"")}"""))
            {
                return CommandResult.Dismiss();
            }
            else
            {
                ExtensionHost.ShowStatus(new StatusMessage() { Message = $"{Resources.unknown_error}\n{_query}\n{msg}", State = MessageState.Error }, StatusContext.Page);
                return CommandResult.KeepOpen();
            }
        }
    }
}
