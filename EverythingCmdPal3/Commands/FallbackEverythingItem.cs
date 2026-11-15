using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingCmdPal3.Commands;

internal sealed partial class FallbackSearchController : IDisposable
{
    private sealed record Request(string Query, SearchSettings Settings);
    private sealed record Probe(string Query, bool HasResults, string? Diagnostic = null);
    private readonly FallbackShowMoreItem _showMoreItem;
    private readonly LatestQueryWorker<Request, Probe> _worker;
    private readonly object _sync = new();
    private Probe _probe = new(string.Empty, false);
    private string _query = string.Empty;
    private SearchSettings _lastSettings;
    private readonly SettingsManager _settings;
    private readonly EverythingSearchService _search = new();
    private bool _disposed;

    internal FallbackSearchController(SettingsManager settings)
    {
        _settings = settings;
        _lastSettings = settings.Current;
        _showMoreItem = new(this, settings);
        _worker = new(ProbeAsync, probe => Volatile.Write(ref _probe, probe), _ => { },
            Publish, ex => ExtensionHost.LogMessage($"ECP: Fallback search failed: {ex}"), debounceMilliseconds: 100);
        _settings.Changed += OnSettingsChanged;
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
            _worker.Submit(new(query, _settings.Current));
        }
    }

    private Task<Probe> ProbeAsync(Request request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return Task.FromResult(new Probe(request.Query, false));
        try
        {
            var response = _search.Search(request.Settings.Prefix + request.Query,
                request.Settings, token, probe: true);
            return Task.FromResult(new Probe(request.Query, response.Results.Length > 0));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            return Task.FromResult(new Probe(request.Query, true, ex.Message));
        }
    }

    private void Publish()
    {
        var probe = Volatile.Read(ref _probe);
        lock (_sync) if (_disposed || probe.Query != _query) return;
        if (probe.HasResults) _showMoreItem.SetQuery(probe.Query, probe.Diagnostic);
        else _showMoreItem.ClearResult();
    }

    private void OnSettingsChanged(object? sender, EventArgs args)
    {
        lock (_sync)
        {
            var current = _settings.Current;
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
            _settings.Changed -= OnSettingsChanged;
            _worker.Dispose();
        }
        _ = _worker.Completion.ContinueWith(_ => { _showMoreItem.Dispose(); _search.Dispose(); }, TaskScheduler.Default);
        GC.SuppressFinalize(this);
    }
}

internal sealed partial class FallbackShowMoreItem : FallbackCommandItem, IDisposable
{
    private static readonly NoOpCommand _noOp = new();
    private readonly FallbackSearchController _controller;
    private readonly ResultsPage _resultsPage;

    internal FallbackShowMoreItem(FallbackSearchController controller, SettingsManager settings)
        : base(_noOp, Resources.top_level_subtitle, "com.everything.fallback.more")
    {
        _controller = controller;
        _resultsPage = new ResultsPage(settings);
        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = IconHelpers.FromRelativePaths("Assets\\EverythingPT_theme-light.svg", "Assets\\EverythingPT_theme-dark.svg");
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
