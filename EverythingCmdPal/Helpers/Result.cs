using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal.Helpers
{
    internal class Result
    {
        internal string FileName { get; set; }
        internal string FilePath { get; set; }
        internal string FullName { get; set; }
        internal string Extension { get; set; }
        internal IconInfo Icon { get; set; }
        internal bool IsFolder { get; set; }
        internal long SizeKB { get; set; }
        internal string ModifiedDate { get; set; }
    }
}
