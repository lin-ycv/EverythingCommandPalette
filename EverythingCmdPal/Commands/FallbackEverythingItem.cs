using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingCmdPal.Commands;

internal sealed partial class FallbackSearchController : IDisposable
{
    private sealed record Request(string Query, SearchSettings Settings);
    private sealed record Probe(string Query, bool HasResults, string? Diagnostic = null);
    private readonly FallbackShowMoreItem _showMoreItem;
    private readonly LatestQueryWorker<Request, Probe> _worker;
    private readonly object _sync = new();
    private Probe _probe = new(string.Empty, false);
    private string _query = string.Empty;
    private SearchSettings _lastSettings = Query.Settings.Current;
    private bool _disposed;

    internal FallbackSearchController()
    {
        _showMoreItem = new(this);
        _worker = new(ProbeAsync, probe => Volatile.Write(ref _probe, probe), _ => { },
            Publish, ex => ExtensionHost.LogMessage($"ECP: Fallback search failed: {ex}"));
        Query.Settings.Changed += OnSettingsChanged;
    }

    internal IFallbackCommandItem[] FallbackItems => [_showMoreItem];

    internal void OnUpdateQuery(string query)
    {
        lock (_sync)
        {
            if (_disposed) return;
            _query = query;
            // Empty queries also invalidate in-flight work. Clearing the item
            // directly allows an older search to make it visible again.
            _worker.Submit(new(query, Query.Settings.Current));
        }
    }

    private static async Task<Probe> ProbeAsync(Request request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return new(request.Query, false);
        var response = await Query.Search(request.Query, request.Settings, token,
            maxResults: 1, loadThumbnails: false);
        // Keep an unavailable search discoverable: opening it shows the real
        // indexing/IPC error instead of making the extension silently vanish.
        try { return new(request.Query, response.Results is null or { Count: > 0 },
            response.Results is null ? response.ErrorMessage ?? "Everything is unavailable. Open to view the error." : null); }
        finally { Query.DisposeResults(response.Results); }
    }

    private void Publish()
    {
        var probe = Volatile.Read(ref _probe);
        if (probe.HasResults) _showMoreItem.SetQuery(probe.Query, probe.Diagnostic);
        else _showMoreItem.ClearResult();
    }

    private void OnSettingsChanged(object? sender, EventArgs args)
    {
        lock (_sync)
        {
            var current = Query.Settings.Current;
            bool requiresSearch = current.RequiresNewSearchComparedTo(_lastSettings);
            _lastSettings = current;
            if (!_disposed && requiresSearch) _worker.Submit(new(_query, current));
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            Query.Settings.Changed -= OnSettingsChanged;
            _worker.Dispose();
        }
        _ = _worker.Completion.ContinueWith(_ => _showMoreItem.Dispose(), TaskScheduler.Default);
        GC.SuppressFinalize(this);
    }
}

internal sealed partial class FallbackShowMoreItem : FallbackCommandItem, IDisposable
{
    private static readonly NoOpCommand _noOp = new();
    private readonly FallbackSearchController _controller;
    private readonly ResultsPage _resultsPage = new();

    internal FallbackShowMoreItem(FallbackSearchController controller)
        : base(_noOp, Resources.top_level_subtitle, "com.everything.fallback.more")
    {
        _controller = controller;
        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = Query.DefaultIcon;
    }

    public override void UpdateQuery(string query) => _controller.OnUpdateQuery(query);

    internal void SetQuery(string query, string? diagnostic = null)
    {
        _resultsPage.StageQuery(query);
        Title = $"Search Everything for \"{query}\"";
        Subtitle = diagnostic ?? Resources.top_level_subtitle;
        Command = _resultsPage;
    }

    internal void ClearResult()
    {
        Title = string.Empty;
        Subtitle = string.Empty;
        Command = _noOp;
    }

    public void Dispose()
    {
        _resultsPage.Dispose();
        GC.SuppressFinalize(this);
    }
}
