using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;
using System.Globalization;
using System;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Commands;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EverythingCmdPal;

internal partial class ResultsPage : DynamicListPage, IDisposable, IFallbackHandler
{
    private readonly List<IListItem> _results = [];
    private List<Result> _currentResults = [];
    private readonly Channel<string> _queryChannel;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly ResultsFilters _filters;
    private string _searchText = string.Empty;
    internal string pre = string.Empty;

    public ResultsPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Name = "Everything";
        PlaceholderText = Resources.noquery;
        ShowDetails = true;
        _filters = new ResultsFilters();
        _filters.PropChanged += OnFiltersChanged;
        Filters = _filters;

        // Bounded channel with capacity 1: new queries overwrite any pending query.
        // This naturally debounces rapid keystrokes — only the latest query matters.
        _queryChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        // Start the single consumer that serializes all Everything SDK access
        _ = Task.Run(ProcessQueriesAsync);
    }

    public override IListItem[] GetItems() => [.. _results];

    /// <summary>
    /// Single consumer loop: reads queries from the channel and runs searches serially.
    /// Because there is only one reader, the Everything SDK's global state is never
    /// accessed concurrently. After the blocking Everything_QueryW returns, we check
    /// whether a newer query arrived while we were blocked — if so, we discard the
    /// stale results and loop to process the newer query immediately.
    /// </summary>
    private async Task ProcessQueriesAsync()
    {
        var reader = _queryChannel.Reader;
        try
        {
            await foreach (string query in reader.ReadAllAsync(_disposeCts.Token))
            {
                IsLoading = true;
                try
                {
                    DisposeResults(_currentResults);
                    _results.Clear();
                    string q = $"{pre}{query}";
                    List<Result> results = await Query.Search(q, _disposeCts.Token);

                    // After the search completes, check if a newer query arrived
                    // while we were blocked in Everything_QueryW. If so, discard
                    // these stale results and let the loop pick up the new query.
                    if (reader.TryRead(out string newerQuery))
                    {
                        // Dispose the stale results we just fetched
                        DisposeResults(results);
                        // Re-enqueue the newer query so ReadAllAsync picks it up
                        _queryChannel.Writer.TryWrite(newerQuery);
                        IsLoading = false;
                        continue;
                    }

                    _currentResults = results ?? [];

                    if (results == null)
                        _results.AddRange(Query.EverythingFailed());
                    else
                    {
                        foreach (Result r in results)
                        {
                            ListItem resultsItem = new(new OpenCommand(r.FullName, r.IsFolder))
                            {
                                Title = r.FileName,
                                Subtitle = r.FilePath,
                                Icon = r.Icon,
                                MoreCommands = new CommandHandler().LoadCommands(r.FullName, r.IsFolder, this),
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
                                                    Text = r.FilePath
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
                                                        new Tag(r.FileType ?? r.Extension ?? string.Empty){
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

                            _results.Add(resultsItem);
                            
                        }
                        if (_results.Count != 0 && Query.Settings.ShowMore)
                        {
                            _results.Add(new ListItem(new ShowMoreCommand(q, Query.Settings.Exe))
                            {
                                Title = Resources.show_more,
                                Subtitle = Resources.show_more_subtitle,
                                Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg"), //new IconInfo("\uF78B"),
                            });
                        }
                    }
                    IsLoading = false;
                    RaiseItemsChanged(_results.Count);
                }
                catch (OperationCanceledException) { break; }
                catch { IsLoading = false; }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void UpdateQuery(string query)
    {
        _queryChannel.Writer.TryWrite(query);
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _searchText = newSearch;
        // Write the latest query into the channel. With DropOldest, this
        // replaces any pending (not-yet-consumed) query, so only the most
        // recent keystroke's query will be processed.
        _queryChannel.Writer.TryWrite($"{newSearch}");
    }

    private void OnFiltersChanged(object? sender, IPropChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Filters.CurrentFilterId))
            return;

        pre = _filters.GetPrefix();
        _queryChannel.Writer.TryWrite($"{_searchText}");
    }

    public void Dispose()
    {
        _filters.PropChanged -= OnFiltersChanged;
        _queryChannel.Writer.Complete();
        _disposeCts.Cancel();
        _disposeCts.Dispose();
        DisposeResults(_currentResults);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes all Result objects in the list to free their thumbnail streams.
    /// </summary>
    private static void DisposeResults(List<Result> results)
    {
        if (results == null) return;
        foreach (var r in results)
            r.Dispose();
        results.Clear();
    }

    private class ResultsFilters : Filters
    {
        private const string AllFilterId = "all";
        private const string FolderFilterId = "folder";
        private const string FileFilterId = "file";

        private readonly List<(string Id, string Name, string Prefix)> _customFilters = [];

        internal ResultsFilters()
        {
            CurrentFilterId = AllFilterId;

            foreach (string key in Query.Settings.Filters.Keys)
            {
                string normalized = key.Trim();
                if (normalized.Length == 0)
                    continue;

                string keyWithoutSuffix = normalized.EndsWith(':') ? normalized[..^1] : normalized;
                string prefix = normalized.EndsWith(':') ? normalized : $"{normalized}:";
                _customFilters.Add(($"custom:{keyWithoutSuffix}", string.Format(Resources.filter_custom, keyWithoutSuffix), prefix));
            }
        }

        internal string GetPrefix()
        {
            return CurrentFilterId switch
            {
                AllFilterId => string.Empty,
                FolderFilterId => "folder:",
                FileFilterId => "file:",
                _ => GetCustomPrefix(CurrentFilterId),
            };
        }

        public override IFilterItem[] GetFilters()
        {
            List<IFilterItem> filters =
            [
                new Filter() { Id = AllFilterId, Name = Resources.filter_all, Icon = new IconInfo("\uE71C") },
                new Filter() { Id = FolderFilterId, Name = Resources.filter_folders, Icon = new IconInfo("\uE8B7") },
                new Filter() { Id = FileFilterId, Name = Resources.filter_files, Icon = new IconInfo("\uE8A5") },
            ];

            if (_customFilters.Count > 0)
            {
                filters.Add(new Separator());
                foreach (var filter in _customFilters)
                {
                    filters.Add(new Filter() { Id = filter.Id, Name = filter.Name, Icon = new IconInfo("\uE71C") });
                }
            }

            return [.. filters];
        }

        private string GetCustomPrefix(string filterId)
        {
            foreach (var filter in _customFilters)
            {
                if (filter.Id == filterId)
                    return filter.Prefix;
            }

            return string.Empty;
        }
    }
}
