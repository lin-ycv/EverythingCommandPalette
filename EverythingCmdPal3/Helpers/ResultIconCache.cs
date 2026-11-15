using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Storage.Streams;

namespace EverythingCmdPal3.Helpers;

internal sealed class ResultIconCache
{
    // Shell providers may block. They never occupy the query worker or COM getter.
    private static readonly SemaphoreSlim Slots = new(4, 4);
    private readonly object _gate = new();
    private readonly Dictionary<string, IconInfo> _icons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> _order = new();

    internal async Task<IconInfo?> GetAsync(string path, bool folder, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        lock (_gate)
            if (_icons.TryGetValue(path, out var cached)) return cached;
        await Slots.WaitAsync(token).ConfigureAwait(false);
        try
        {
            token.ThrowIfCancellationRequested();
            var stream = await ResultThumbnailHelper.GetThumbnail(path, folder).ConfigureAwait(false);
            if (stream is null) return null;
            IconInfo icon;
            try
            {
                token.ThrowIfCancellationRequested();
                var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
                icon = new IconInfo(data, data);
            }
            catch { stream.Dispose(); throw; }
            lock (_gate)
            {
                if (!_icons.ContainsKey(path)) _order.Enqueue(path);
                _icons[path] = icon;
                while (_order.Count > 128) _icons.Remove(_order.Dequeue());
            }
            // A published stream follows the WinRT reference lifetime. Closing
            // it on eviction can break a host still materializing the icon.
            return icon;
        }
        finally { Slots.Release(); }
    }
}
