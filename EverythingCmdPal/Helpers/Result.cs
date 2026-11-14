using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using Windows.Storage.Streams;

namespace EverythingCmdPal.Helpers
{
    internal sealed partial class Result : IDisposable
    {
        internal string FileName { get; set; } = string.Empty;
        internal string FilePath { get; set; } = string.Empty;
        internal string FullName { get; set; } = string.Empty;
        internal string Extension { get; set; } = string.Empty;
        internal string FileType { get; set; } = string.Empty;
        internal IconInfo Icon { get; set; } = null!;
        internal bool IsFolder { get; set; }
        internal long SizeKB { get; set; }
        internal string ModifiedDate { get; set; } = string.Empty;
        internal bool Preview { get; set; }

        /// <summary>
        /// The thumbnail stream backing this result's icon.
        /// Must be disposed when the result is no longer needed.
        /// </summary>
        internal IRandomAccessStream? ThumbnailStream { get; set; }

        public void Dispose()
        {
            ThumbnailStream?.Dispose();
            ThumbnailStream = null;
        }
    }
}
