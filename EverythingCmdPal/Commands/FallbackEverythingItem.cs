using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EverythingCmdPal.Commands;

/// <summary>
/// Controller that searches Everything and distributes results:
/// - Top 3 results → dynamic TopLevelCommands (results section, top of main page)
/// - "Show more" → FallbackCommands (fallback section, bottom of main page)
/// </summary>
internal sealed class FallbackSearchController : IDisposable
{
    private const int TopResultCount = 3;
    private static readonly IconInfo _defaultIcon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
    private readonly FallbackShowMoreItem _showMoreItem;
    private readonly List<ICommandItem> _topResults = [];
    private readonly Action _onResultsChanged;
    private readonly Channel<string> _queryChannel;
    private readonly CancellationTokenSource _disposeCts = new();
    private List<Result> _currentResults = [];

    internal FallbackSearchController(Action onResultsChanged)
    {
        _onResultsChanged = onResultsChanged;
        _showMoreItem = new FallbackShowMoreItem(this);

        _queryChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        _ = Task.Run(ProcessQueriesAsync);
    }

    /// <summary>Dynamic items to include in TopLevelCommands (results section).</summary>
    internal IReadOnlyList<ICommandItem> TopResults => _topResults;

    /// <summary>Fallback items (just the "show more" entry).</summary>
    internal IFallbackCommandItem[] FallbackItems => [_showMoreItem];

    internal void OnUpdateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            ClearAll();
            return;
        }
        _queryChannel.Writer.TryWrite(query);
    }

    private async Task ProcessQueriesAsync()
    {
        var reader = _queryChannel.Reader;
        try
        {
            await foreach (string query in reader.ReadAllAsync(_disposeCts.Token))
            {
                try
                {
                    List<Result> results = await Query.Search(query, _disposeCts.Token,
                        maxResults: TopResultCount, loadThumbnails: false);

                    if (reader.TryRead(out string newerQuery))
                    {
                        DisposeResults(results);
                        _queryChannel.Writer.TryWrite(newerQuery);
                        continue;
                    }

                    DistributeResults(results, query);
                }
                catch (OperationCanceledException) { break; }
                catch { ClearAll(); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void DistributeResults(List<Result> results, string query)
    {
        DisposeResults(_currentResults);
        _currentResults = results ?? [];

        // Build top result items for the results section
        _topResults.Clear();
        if (results != null)
        {
            int count = Math.Min(TopResultCount, results.Count);
            for (int i = 0; i < count; i++)
            {
                var r = results[i];
                _topResults.Add(new ListItem(new OpenCommand(r.FullName, r.IsFolder))
                {
                    Title = r.FileName,
                    Subtitle = r.FilePath,
                    Icon = r.Icon ?? _defaultIcon,
                });
            }
        }

        // Update "show more" fallback
        if (results != null && results.Count > 0)
            _showMoreItem.SetQuery(query);
        else
            _showMoreItem.ClearResult();

        // Notify CommandsProvider to re-fetch TopLevelCommands
        _onResultsChanged();
    }

    private void ClearAll()
    {
        DisposeResults(_currentResults);
        _currentResults = [];
        _topResults.Clear();
        _showMoreItem.ClearResult();
        _onResultsChanged();
    }

    private static void DisposeResults(List<Result> results)
    {
        if (results == null) return;
        foreach (var r in results)
            r.Dispose();
        results.Clear();
    }

    public void Dispose()
    {
        _queryChannel.Writer.Complete();
        _disposeCts.Cancel();
        _disposeCts.Dispose();
        DisposeResults(_currentResults);
        _showMoreItem.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// "Show more" fallback item that navigates into the full ResultsPage.
/// Also serves as the entry point for UpdateQuery from the framework.
/// </summary>
internal sealed partial class FallbackShowMoreItem : FallbackCommandItem, IDisposable
{
    private static readonly NoOpCommand _noOp = new();
    private static readonly IconInfo _defaultIcon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
    private readonly FallbackSearchController _controller;
    private readonly ResultsPage _resultsPage = new();

    internal FallbackShowMoreItem(FallbackSearchController controller)
        : base(_noOp, Resources.top_level_subtitle, "com.everything.fallback.more")
    {
        _controller = controller;
        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = _defaultIcon;
    }

    public override void UpdateQuery(string query)
    {
        _controller.OnUpdateQuery(query);
    }

    internal void SetQuery(string query)
    {
        Title = $"Search Everything for \"{query}\"";
        Subtitle = Resources.top_level_subtitle;
        Icon = _defaultIcon;
        Command = _resultsPage;
        _resultsPage.SearchText = query;
        _resultsPage.UpdateQuery(query);
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
