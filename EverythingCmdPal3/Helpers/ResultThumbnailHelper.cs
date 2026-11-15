using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace EverythingCmdPal3.Helpers;

// A small replacement for the 0.12 SDK's normal-size result thumbnail path.
// Keep image previews and Shell icons, while owning every temporary bitmap,
// HICON, PIDL and failed output stream. No jumbo-image-list COM objects needed.
internal static class ResultThumbnailHelper
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".ico" };

    internal static async Task<IRandomAccessStream?> GetThumbnail(string path, bool isFolder)
    {
        if (!isFolder && ImageExtensions.Contains(Path.GetExtension(path)))
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, 20);
                if (thumbnail is not null) return thumbnail;
            }
            catch (Exception)
            {
                // Files may disappear, or have no thumbnail provider. Use the
                // Shell icon just as the SDK helper does in this case.
            }
        }

        nint icon = GetIcon(path);
        if (icon == 0) return null;
        // Take ownership immediately, including if allocating the PNG buffer
        // or creating the managed Icon wrapper throws.
        byte[] png;
        try
        {
            using var image = Icon.FromHandle(icon); // does not own the HICON
            using var bitmap = image.ToBitmap();
            using var buffer = new MemoryStream();
            bitmap.Save(buffer, ImageFormat.Png);
            png = buffer.ToArray();
        }
        finally { _ = DestroyIcon(icon); }

        var stream = new InMemoryRandomAccessStream();
        try
        {
            using var output = stream.GetOutputStreamAt(0);
            using var writer = new DataWriter(output);
            writer.WriteBytes(png);
            await writer.StoreAsync();
            await writer.FlushAsync();
            stream.Seek(0);
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static nint GetIcon(string path)
    {
        nint pidl = 0;
        try
        {
            if (SHParseDisplayName(path, 0, out pidl, 0, out _) >= 0 && pidl != 0)
            {
                var info = new ShellFileInfo();
                nint result = SHGetFileInfoPidl(pidl, 0, ref info,
                    (uint)Marshal.SizeOf<ShellFileInfo>(), 0x100 | 0x8 | 0x4); // ICON | PIDL | SHELLICONSIZE
                if (result != 0 && info.Icon != 0) return info.Icon;
                if (info.Icon != 0) _ = DestroyIcon(info.Icon);
            }
        }
        finally { if (pidl != 0) Marshal.FreeCoTaskMem(pidl); }

        var fallback = new ShellFileInfo();
        nint fallbackResult = SHGetFileInfoPath(path, 0, ref fallback,
            (uint)Marshal.SizeOf<ShellFileInfo>(), 0x100 | 0x4);
        if (fallbackResult != 0) return fallback.Icon;
        if (fallback.Icon != 0) _ = DestroyIcon(fallback.Icon);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        internal nint Icon;
        internal int IconIndex;
        internal uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] internal string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHParseDisplayName(string name, nint bindContext, out nint pidl,
        uint attributes, out uint foundAttributes);

    [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", ExactSpelling = true)]
    private static extern nint SHGetFileInfoPidl(nint pidl, uint attributes,
        ref ShellFileInfo info, uint size, uint flags);

    [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern nint SHGetFileInfoPath(string path, uint attributes,
        ref ShellFileInfo info, uint size, uint flags);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint icon);
}

