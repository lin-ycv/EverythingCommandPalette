using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EverythingCmdPal.Helpers;

// Shared by the results page and fallback. Values are never re-enqueued: doing
// that with DropOldest can overwrite a newer request with an older one.
internal sealed partial class LatestQueryWorker<TRequest, TResult> : IDisposable
{
    private readonly record struct Request(long Version, TRequest Value);
    private readonly object _sync = new();
    private readonly Channel<Request> _requests = Channel.CreateBounded<Request>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false,
    });
    private readonly CancellationTokenSource _stop = new();
    private readonly Func<TRequest, CancellationToken, Task<TResult>> _fetch;
    private readonly Action<TResult> _commit;
    private readonly Action<TResult> _discard;
    private readonly Action _notify;
    private readonly Action<Exception> _error;
    private long _version;
    private bool _disposed;
    internal Task Completion { get; }

    internal LatestQueryWorker(Func<TRequest, CancellationToken, Task<TResult>> fetch,
        Action<TResult> commit, Action<TResult> discard, Action notify, Action<Exception> error)
    {
        (_fetch, _commit, _discard, _notify, _error) = (fetch, commit, discard, notify, error);
        Completion = Task.Run(ProcessAsync);
    }

    internal void Submit(TRequest value)
    {
        lock (_sync)
        {
            if (!_disposed) _requests.Writer.TryWrite(new Request(++_version, value));
        }
    }

    internal void Invalidate(Action? reset = null)
    {
        lock (_sync)
        {
            if (_disposed) return;
            ++_version;
            reset?.Invoke();
        }
    }

    private async Task ProcessAsync()
    {
        try
        {
            await foreach (var pending in _requests.Reader.ReadAllAsync(_stop.Token))
            {
                var request = pending;
                while (_requests.Reader.TryRead(out var newer)) request = newer;
                lock (_sync)
                    if (_disposed || request.Version != _version) continue;
                try
                {
                    var result = await _fetch(request.Value, _stop.Token);
                    bool committed = false;
                    try
                    {
                        lock (_sync)
                        {
                            if (!_disposed && request.Version == _version)
                            {
                                // Commit managed state only; never call COM/host
                                // code while holding the worker lock.
                                _commit(result);
                                committed = true;
                            }
                        }
                        if (committed) _notify();
                    }
                    finally { if (!committed) _discard(result); }
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    bool current;
                    lock (_sync) current = !_disposed && request.Version == _version;
                    if (current)
                    {
                        try { _error(ex); }
                        catch { /* A disconnected host must not terminate the consumer. */ }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        finally
        {
            lock (_sync)
            {
                _disposed = true;
                _requests.Writer.TryComplete();
                _stop.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            ++_version;
            _requests.Writer.TryComplete();
            _stop.Cancel();
        }
        // Do not dispose the CTS/results until the consumer has left the native
        // query. Dispose is non-blocking; Completion is available to tests/cleanup.
    }
}
