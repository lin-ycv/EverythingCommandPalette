using EverythingCmdPal.Commands;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingCmdPal;

internal sealed partial class ResultsPage : DynamicListPage, IDisposable, IFallbackHandler
{
    private sealed record SearchRequest(string Query, string FilterPrefix, SearchSettings Settings);
    private sealed record ResultBatch(IListItem[] Items, List<Result>? Results, SearchSettings Settings, string Query,
        string? WarningMessage = null);
    private readonly SettingsManager _settings;
    private readonly Func<string, SearchSettings, CancellationToken, Task<Query.SearchResponse>> _search;
    internal Task Completion => _worker.Completion;
    private readonly object _stateLock = new();
    private readonly CommandHandler _commandHandler = new();
    private readonly ResultsFilters _filters;
    private readonly LatestQueryWorker<SearchRequest, ResultBatch> _worker;
    private ResultBatch _batch;
    private string _searchText = string.Empty;
    private bool _staged;
    private bool _hasQuery;
    private volatile bool _disposed;

    public ResultsPage() : this(Query.Settings, (query, settings, token) => Query.Search(query, settings, token), Query.DefaultIcon)
    {
    }

    internal ResultsPage(SettingsManager settings,
        Func<string, SearchSettings, CancellationToken, Task<Query.SearchResponse>> search, IconInfo icon)
    {
        _settings = settings;
        _search = search;
        Icon = icon;
        Name = "Everything";
        PlaceholderText = Resources.noquery;
        ShowDetails = _settings.Current.DetailPane;
        _batch = new([], null, _settings.Current, string.Empty);
        _filters = new ResultsFilters(_settings.Current.Filters);
        _filters.PropChanged += OnFiltersChanged;
        Filters = _filters;
        _worker = new(FetchAsync, batch => Volatile.Write(ref _batch, batch),
            batch => Query.DisposeResults(batch.Results), OnBatchReady, OnSearchError);
        _settings.Changed += OnSettingsChanged;
    }

    public override IListItem[] GetItems()
    {
        lock (_stateLock)
        {
            if (_staged && !_disposed)
            {
                _staged = false;
                QueueSearch();
            }
        }
        // Publish complete arrays; never enumerate a list modified by a worker.
        return [.. Volatile.Read(ref _batch).Items];
    }

    // Preparing a fallback entry must not start another full search on the home
    // page. GetItems starts the search only when the results are requested.
    internal void StageQuery(string query)
    {
        lock (_stateLock)
        {
            // A settings-triggered fallback re-probe can return the same text.
            // Keep in-flight work valid, but still refresh on the next GetItems
            // so returning to the same query does not reuse old file results.
            if (query == _searchText)
            {
                _staged = true;
                return;
            }
            _searchText = query;
            _staged = true;
            ClearResults();
        }
        SetSearchNoUpdate(query);
    }

    private async Task<ResultBatch> FetchAsync(SearchRequest request, CancellationToken token)
    {
        IsLoading = true;
        try
        {
            string query = request.FilterPrefix + request.Query;
            var response = await _search(query, request.Settings, token);
            try
            {
                return BuildBatch(response.Results, request.Settings, query, response.ErrorCode,
                    response.ErrorMessage, response.WarningMessage);
            }
            catch
            {
                Query.DisposeResults(response.Results);
                throw;
            }
        }
        finally { IsLoading = false; }
    }

    private ResultBatch BuildBatch(List<Result>? results, SearchSettings settings, string query, uint errorCode = 0,
        string? errorMessage = null, string? warningMessage = null)
    {
        var items = results is null
            ? new List<IListItem>(Query.EverythingFailed(errorCode, errorMessage))
            : results.Select(result => (IListItem)CreateItem(result, settings)).ToList();
        if (results is not null && warningMessage is not null)
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Slow sort enabled",
                Subtitle = warningMessage,
                Icon = new IconInfo("\uE7BA"),
            });
        if (results is { Count: > 0 } && settings.ShowMore)
            items.Add(new ListItem(new ShowMoreCommand(query, settings.Exe))
            {
                Title = Resources.show_more,
                Subtitle = Resources.show_more_subtitle,
                Icon = Icon,
            });
        return new([.. items], results, settings, query, warningMessage);
    }

    private void OnBatchReady()
    {
        RefreshPresentation();
        NotifyResultsChanged();
    }

    private void RefreshPresentation()
    {
        // Build off-lock: constructing rows/raising host notifications while
        // holding the query-worker lock risks COM re-entrancy deadlocks.
        while (!_disposed)
        {
            var old = Volatile.Read(ref _batch);
            var current = _settings.Current;
            if (ReferenceEquals(old.Settings, current) || current.RequiresNewSearchComparedTo(old.Settings)) return;
            var next = old.Results is null ? old with { Settings = current }
                : BuildBatch(old.Results, current, old.Query, warningMessage: old.WarningMessage);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _batch, next, old), old)) return;
        }
    }

    private ListItem CreateItem(Result r, SearchSettings settings)
    {
        var resultsItem = new ListItem(new OpenCommand(r.FullName, r.IsFolder))
        {
            Title = r.FileName,
            Subtitle = r.FilePath,
            Icon = r.Icon,
            MoreCommands = _commandHandler.LoadCommands(r.FullName, r.IsFolder, this, settings),
            DataPackage = DataPackageHelper.CreateDataPackageForPath(r),
        };
        if (r.Preview)
        {
            resultsItem.Details = new Details()
            {
                //// Title and Body text is large and unselectable, but displayed right under HeroImage
                //Title = fileName,
                //Body = fileName,
                HeroImage = r.Icon,
                Metadata = [
                    new DetailsElement() {
                            Key = r.FileName,
                            Data = new DetailsLink() {
                                Text = r.FilePath,
                                Link = new Uri(r.FilePath.StartsWith(@"\\", StringComparison.Ordinal) ? r.FilePath : "file:///" + r.FilePath.Replace("\\", "/"))
                            }
                        },
                        new DetailsElement() {
                            Data = new DetailsSeparator()
                        },
                        new DetailsElement(){
                            Key = "Size",
                            Data = new DetailsLink(){
                                Text = r.SizeKB.ToString("N0", CultureInfo.InvariantCulture)+" KB"
                            }
                        },
                        new DetailsElement() {
                            Key = "Type",
                            Data = new DetailsTags(){
                                Tags=[
                                    new Tag(string.IsNullOrEmpty(r.FileType) ? r.Extension : r.FileType){
                                        Foreground = ColorHelpers.FromRgb(51,51,51),
                                        Background=ColorHelpers.FromRgb(224,204,179)},
                                ]
                            }
                        },
                        new DetailsElement(){
                            Key = "Modified Date",
                            Data = new DetailsLink(){
                                Text = r.ModifiedDate
                            }
                        }
                 ],
            };
        }
        return resultsItem;
    }

    private void NotifyResultsChanged()
    {
        var batch = Volatile.Read(ref _batch);
        ShowDetails = batch.Settings.DetailPane;
        RaiseItemsChanged(batch.Items.Length);
        // Retired published icon streams follow their WinRT reference lifetime.
        // Explicitly closing them here can break a host still fetching an icon.
    }

    private void OnSearchError(Exception exception)
    {
        ExtensionHost.LogMessage($"ECP: Search failed: {exception}");
        ExtensionHost.ShowStatus(new StatusMessage
        {
            Message = $"{Resources.unknown_error}: {exception.Message}",
            State = MessageState.Error,
        }, StatusContext.Page);
    }

    public void NotifySearchTextChanged() => OnPropertyChanged(nameof(SearchText));

    public void UpdateQuery(string query) => UpdateSearchText(_searchText, query);

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        lock (_stateLock)
        {
            _searchText = newSearch;
            _staged = false;
            QueueSearch();
        }
    }

    private void QueueSearch()
    {
        if (_disposed) return;
        _hasQuery = true;
        _worker.Submit(new(_searchText, _filters.GetPrefix(), _settings.Current));
    }

    private void OnSettingsChanged(object? sender, EventArgs args)
    {
        var current = _settings.Current;
        if (!current.RequiresNewSearchComparedTo(Volatile.Read(ref _batch).Settings))
        {
            RefreshPresentation();
            NotifyResultsChanged();
            return;
        }
        _filters.Refresh(current.Filters);
        lock (_stateLock)
        {
            if (_disposed) return;
            ClearResults();
            if (_hasQuery && !_staged) QueueSearch();
        }
        OnPropertyChanged(nameof(Filters));
        NotifyResultsChanged();
    }

    private void ClearResults() => _worker.Invalidate(() =>
        Volatile.Write(ref _batch, new ResultBatch([], null, _settings.Current, string.Empty)));

    private void OnFiltersChanged(object? sender, IPropChangedEventArgs args)
    {
        if (args.PropertyName != nameof(Filters.CurrentFilterId)) return;
        lock (_stateLock) QueueSearch();
    }

    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed) return;
            _disposed = true;
            _settings.Changed -= OnSettingsChanged;
            _filters.PropChanged -= OnFiltersChanged;
            _worker.Dispose();
        }
        _ = _worker.Completion.ContinueWith(_ => Query.DisposeResults(_batch.Results), TaskScheduler.Default);
        GC.SuppressFinalize(this);
    }

    private sealed partial class ResultsFilters : Filters
    {
        private const string AllFilterId = "all";
        private const string FolderFilterId = "folder";
        private const string FileFilterId = "file";
        private volatile (string Id, string Name, string Prefix)[] _customFilters = [];

        internal ResultsFilters(IReadOnlyDictionary<string, string> filters)
        {
            CurrentFilterId = AllFilterId;
            Refresh(filters);
        }

        internal void Refresh(IReadOnlyDictionary<string, string> filters)
        {
            _customFilters = filters.Keys.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key =>
            {
                string name = key.Trim().TrimEnd(':');
                return ($"custom:{name}", string.Format(CultureInfo.CurrentCulture, Resources.filter_custom, name), name + ":");
            }).ToArray();
            if (CurrentFilterId is not (AllFilterId or FolderFilterId or FileFilterId)
                && !_customFilters.Any(filter => filter.Id == CurrentFilterId))
                CurrentFilterId = AllFilterId;
        }

        internal string GetPrefix() => CurrentFilterId switch
        {
            FolderFilterId => "folder:",
            FileFilterId => "file:",
            _ => _customFilters.FirstOrDefault(filter => filter.Id == CurrentFilterId).Prefix ?? string.Empty,
        };

        public override IFilterItem[] GetFilters()
        {
            var filters = new List<IFilterItem>
            {
                new Filter { Id = AllFilterId, Name = Resources.filter_all, Icon = new IconInfo("\uE71C") },
                new Filter { Id = FolderFilterId, Name = Resources.filter_folders, Icon = new IconInfo("\uE8B7") },
                new Filter { Id = FileFilterId, Name = Resources.filter_files, Icon = new IconInfo("\uE8A5") },
            };
            if (_customFilters.Length > 0)
            {
                filters.Add(new Separator());
                foreach (var filter in _customFilters)
                    filters.Add(new Filter { Id = filter.Id, Name = filter.Name, Icon = new IconInfo("\uE71C") });
            }
            return [.. filters];
        }
    }
}
