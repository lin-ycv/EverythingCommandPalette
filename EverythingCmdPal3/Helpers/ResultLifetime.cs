using System;
using System.Threading;

namespace EverythingCmdPal3.Helpers;

internal sealed partial class ResultLifetime : IDisposable
{
    private readonly CancellationTokenSource _source = new();
    private readonly object _gate = new();
    private bool _cancelled;
    internal CancellationToken Token { get; }
    internal ResultLifetime() { Token = _source.Token; }
    internal void Cancel()
    {
        lock (_gate)
        {
            if (_cancelled) return;
            _cancelled = true;
            _source.Cancel();
            _source.Dispose();
        }
    }
    public void Dispose() => Cancel();
}
