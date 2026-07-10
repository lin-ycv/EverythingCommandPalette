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
/// Controller that searches Everything for the fallback "show more" item
/// on the main Command Palette page.
/// </summary>
internal sealed class FallbackSearchController : IDisposable
{
    private const int FallbackResultCount = 3;
    private readonly FallbackShowMoreItem _showMoreItem;
    private readonly Channel<string> _queryChannel;
    private readonly CancellationTokenSource _disposeCts = new();
    private List<Result> _currentResults = [];

    internal FallbackSearchController()
    {
        _showMoreItem = new FallbackShowMoreItem(this);

        _queryChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        _ = Task.Run(ProcessQueriesAsync);
    }

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
                        maxResults: FallbackResultCount, loadThumbnails: false);

                    if (reader.TryRead(out string newerQuery))
                    {
                        DisposeResults(results);
                        _queryChannel.Writer.TryWrite(newerQuery);
                        continue;
                    }

                    UpdateFallback(results, query);
                }
                catch (OperationCanceledException) { break; }
                catch { ClearAll(); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void UpdateFallback(List<Result> results, string query)
    {
        DisposeResults(_currentResults);
        _currentResults = results ?? [];

        if (results != null && results.Count > 0)
            _showMoreItem.SetQuery(query);
        else
            _showMoreItem.ClearResult();
    }

    private void ClearAll()
    {
        DisposeResults(_currentResults);
        _currentResults = [];
        _showMoreItem.ClearResult();
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

    public override void UpdateQuery(string query)
    {
        _controller.OnUpdateQuery(query);
    }

    internal void SetQuery(string query)
    {
        Title = $"Search Everything for \"{query}\"";
        Subtitle = Resources.top_level_subtitle;
        Icon = Query.DefaultIcon;
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
