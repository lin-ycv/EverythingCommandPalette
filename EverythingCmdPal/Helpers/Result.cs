using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using Windows.Storage.Streams;

namespace EverythingCmdPal.Helpers
{
    internal class Result : IDisposable
    {
        internal string FileName { get; set; }
        internal string FilePath { get; set; }
        internal string FullName { get; set; }
        internal string Extension { get; set; }
        internal string FileType { get; set; }
        internal IconInfo Icon { get; set; }
        internal bool IsFolder { get; set; }
        internal long SizeKB { get; set; }
        internal string ModifiedDate { get; set; }
        internal bool Preview { get; set; }

        /// <summary>
        /// The thumbnail stream backing this result's icon.
        /// Must be disposed when the result is no longer needed.
        /// </summary>
        internal IRandomAccessStream ThumbnailStream { get; set; }

        public void Dispose()
        {
            ThumbnailStream?.Dispose();
            ThumbnailStream = null;
        }
    }
}
