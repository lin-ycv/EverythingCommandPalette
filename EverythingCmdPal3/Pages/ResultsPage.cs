using EverythingCmdPal3.Commands;
using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static EverythingCmdPal3.Interop.NativeMethods;

namespace EverythingCmdPal3;

internal sealed partial class ResultsPage : DynamicListPage, IDisposable
{
    private readonly SettingsManager _settings;
    private readonly EverythingSearchService _search = new();
    private readonly ResultIconCache _icons = new();
    private readonly CommandsHandler _commands = new();
    private readonly LatestQueryWorker<Request, Batch> _worker;
    private readonly ResultsFilters _filters = new();
    private Batch _batch = new([], null, null, string.Empty, new());
    private string _query = string.Empty;
    private int _started;
    private int _disposed;

    private sealed record Request(string Query, SearchSettings Settings);
    private sealed record Batch(IListItem[] Items, SearchResponse? Response,
        SearchSettings? Settings, string Query, ResultLifetime Icons);

    internal ResultsPage(SettingsManager settings)
    {
        _settings = settings;
        Icon = IconHelpers.FromRelativePaths("Assets\\EverythingPT_theme-light.svg", "Assets\\EverythingPT_theme-dark.svg");
        Title = "ECP3";
        Name = "Everything3";
        Id = "com.microsoft.cmdpal.everything3";
        ShowDetails = settings.Current.DetailPane;
        Filters = _filters;
        _worker = new(FetchAsync, batch =>
        {
            var old = Interlocked.Exchange(ref _batch, batch);
            old.Icons.Cancel();
            old.Icons.Dispose();
        }, batch =>
        {
            batch.Icons.Cancel();
            batch.Icons.Dispose();
        }, NotifyResultsChanged, OnSearchError, debounceMilliseconds: 100);
        _settings.Changed += OnSettingsChanged;
        _filters.PropChanged += OnFiltersChanged;
    }

    public override IListItem[] GetItems()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0) QueueSearch();
        // Return a copy: the host must not be able to mutate the published array.
        return [.. Volatile.Read(ref _batch).Items];
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        SetSearchNoUpdate(newSearch);
        Volatile.Write(ref _query, newSearch);
        Interlocked.Exchange(ref _started, 1);
        QueueSearch();
    }

    internal void UpdateQuery(string query)
    {
        SearchText = query;
        OnPropertyChanged(nameof(SearchText));
    }

    internal void StageQuery(string query)
    {
        Batch? previous = null;
        _worker.Invalidate(() => previous = Interlocked.Exchange(ref _batch,
            new Batch([], null, null, string.Empty, new())));
        previous?.Icons.Dispose();
        SetSearchNoUpdate(query);
        Volatile.Write(ref _query, query);
        Interlocked.Exchange(ref _started, 0);
    }

    private void QueueSearch()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var settings = _settings.Current;
        string filter = _filters.Prefix;
        string query = settings.Prefix + (filter.Length == 0 ? "" : filter + " ") + Volatile.Read(ref _query);
        _worker.Submit(new(query, settings));
    }

    private Task<Batch> FetchAsync(Request request, CancellationToken token)
    {
        IsLoading = true;
        var timer = Stopwatch.StartNew();
        SearchResponse? response = null;
        try
        {
            response = _search.Search(request.Query, request.Settings, token);
            token.ThrowIfCancellationRequested();
            var settings = _settings.Current;
            if (settings.RequiresNewSearchComparedTo(request.Settings)) settings = request.Settings;
            var batch = BuildBatch(response, settings, request.Query, token);
            if (timer.ElapsedMilliseconds >= 250)
                ExtensionHost.LogMessage($"ECP3: Search total={timer.ElapsedMilliseconds}ms; native={response.NativeMilliseconds}ms; metadata={response.MetadataMilliseconds}ms; rows={batch.Items.Length}. Icons load separately.");
            return Task.FromResult(batch);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            ExtensionHost.LogMessage($"ECP3: Search failed: {ex.Message}");
            uint code = ex is EverythingException sdk ? sdk.ErrorCode : 0;
            ICommand action = code == 0xE0000002 && File.Exists(request.Settings.Exe)
                ? new OpenCommand(request.Settings.Exe)
                : new NoOpCommand();
            return Task.FromResult(new Batch(
                [new ListItem(action)
                {
                    Title = code == 0xE0000002 ? Resources.launch_e : Resources.ERROR,
                    Subtitle = ex.Message,
                    Icon = new IconInfo("\uE783"),
                }], null, request.Settings, request.Query, new()));
        }
    }

    private Batch BuildBatch(SearchResponse response, SearchSettings settings, string query,
        CancellationToken token = default)
    {
        var icons = new ResultLifetime();
        try
        {
            var items = new List<IListItem>(response.Results.Length + 2);
            foreach (var result in response.Results)
            {
                token.ThrowIfCancellationRequested();
                string path = result.FullPath;
                string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
                if (name.Length == 0) name = path;
                string directory = Path.GetDirectoryName(path) ?? path;
                var item = new SearchResultItem(new OpenCommand(path, settings.Instance, result.IsFolder), result, _icons, icons.Token)
                {
                    Title = name,
                    Subtitle = directory,
                    MoreCommands = _commands.LoadCommands(path, result.IsFolder, this, settings),
                    DataPackage = DataPackageHelper.CreateDataPackageForPath(result),
                };
                if (settings.DetailPane)
                    item.Details = new Details
                    {
                        HeroImage = new IconInfo(result.IsFolder ? "\uE8B7" : "\uE8A5"),
                        Metadata =
                        [
                            new DetailsElement { Key = name, Data = new DetailsLink { Text = directory } },
                            new DetailsElement { Data = new DetailsSeparator() },
                            new DetailsElement { Key = Resources.size, Data = new DetailsLink { Text = result.Size } },
                            new DetailsElement { Key = Resources.ext, Data = new DetailsTags { Tags =
                                [new Tag(result.IsFolder ? "Folder" : Path.GetExtension(path))] } },
                            new DetailsElement { Key = Resources.date_mod, Data = new DetailsLink { Text = result.ModifiedDate } },
                        ],
                    };
                items.Add(item);
            }
            if (response.Warning is not null)
                items.Add(new ListItem(new NoOpCommand()) { Title = "Slow sort enabled", Subtitle = response.Warning });
            if (settings.ShowMore && response.HasMore)
                items.Add(new ListItem(new ShowMore(query, settings.Exe, settings.Instance))
                {
                    Subtitle = Resources.show_more_subtitle,
                });
            return new([.. items], response, settings, query, icons);
        }
        catch { icons.Cancel(); icons.Dispose(); throw; }
    }

    private void NotifyResultsChanged()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        RefreshPresentation();
        var batch = Volatile.Read(ref _batch);
        ShowDetails = batch.Settings?.DetailPane ?? _settings.Current.DetailPane;
        IsLoading = false;
        RaiseItemsChanged(batch.Items.Length);
    }

    private void OnSearchError(Exception exception)
    {
        IsLoading = false;
        ExtensionHost.LogMessage($"ECP3: Result publication failed: {exception.Message}");
    }

    private void OnSettingsChanged(object? sender, EventArgs args)
    {
        var previous = Volatile.Read(ref _batch);
        var settings = _settings.Current;
        if (previous.Settings is not null &&
            !settings.RequiresNewSearchComparedTo(previous.Settings))
        {
            NotifyResultsChanged();
            return;
        }
        if (Volatile.Read(ref _started) != 0) QueueSearch();
    }

    private void RefreshPresentation()
    {
        // A settings save may race with a completed native query. Reconcile
        // menus off-lock, and keep host notifications outside the worker lock.
        while (Volatile.Read(ref _disposed) == 0)
        {
            var previous = Volatile.Read(ref _batch);
            var settings = _settings.Current;
            if (previous.Response is null || previous.Settings is null ||
                ReferenceEquals(previous.Settings, settings) ||
                settings.RequiresNewSearchComparedTo(previous.Settings)) return;
            var next = BuildBatch(previous.Response, settings, previous.Query);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _batch, next, previous), previous))
            {
                previous.Icons.Dispose();
                return;
            }
            next.Icons.Dispose();
        }
    }

    private void OnFiltersChanged(object sender, IPropChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ResultsFilters.CurrentFilterId)) QueueSearch();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _settings.Changed -= OnSettingsChanged;
        _filters.PropChanged -= OnFiltersChanged;
        _worker.Dispose();
        _ = _worker.Completion.ContinueWith(_ =>
        {
            var batch = Volatile.Read(ref _batch);
            batch.Icons.Cancel();
            batch.Icons.Dispose();
            _search.Dispose();
        }, TaskScheduler.Default);
        GC.SuppressFinalize(this);
    }

    private sealed partial class ResultsFilters : Filters
    {
        internal ResultsFilters() { CurrentFilterId = "all"; }
        internal string Prefix => CurrentFilterId switch { "folder" => "folder:", "file" => "file:", _ => "" };
        public override IFilterItem[] GetFilters() =>
        [
            new Filter { Id = "all", Name = "All", Icon = new IconInfo("\uE71C") },
            new Filter { Id = "folder", Name = "Folders", Icon = new IconInfo("\uE8B7") },
            new Filter { Id = "file", Name = "Files", Icon = new IconInfo("\uE8A5") },
        ];
    }
}
