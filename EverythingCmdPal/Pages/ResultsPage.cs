using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;
using System.Globalization;
using System;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Commands;
using System.Threading;

namespace EverythingCmdPal;

internal partial class ResultsPage : DynamicListPage, IDisposable, IFallbackHandler
{
    private readonly List<IListItem> _results = [];
    private readonly Lock _lock = new();
    private CancellationTokenSource _cts = new();
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
    }
    public override IListItem[] GetItems() => [.. _results];
    public void UpdateQuery(string query)
    {
        IsLoading = true;
        CancellationTokenSource cts;
        lock (_lock)
        {
            _cts.Cancel();
            _cts = new();
            cts = _cts;
        }
        try
        {
            _results.Clear();
            string q = $"{pre}{query}";
            List<Result> results = Query.Search(q, cts.Token);
            if (results == null)
                _results.AddRange(Query.EverythingFailed());
            else
            {
                foreach (Result r in results)
                {
                    _results.Add(new ListItem(new OpenCommand(r.FullName, r.IsFolder))
                    {
                        Title = r.FileName,
                        Subtitle = r.FilePath,
                        Icon = r.Icon,
                        MoreCommands = new CommandHandler().LoadCommands(r.FullName, r.IsFolder, this),
                        Details = new Details()
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
                                                new Tag(r.FileType ?? string.Empty){
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
                        },
                    });
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
        catch { IsLoading = false; }
    }
    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _searchText = newSearch;
        UpdateQuery(newSearch);
    }

    private void OnFiltersChanged(object? sender, IPropChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Filters.CurrentFilterId))
            return;

        pre = _filters.GetPrefix();
        UpdateQuery(_searchText);
    }

    public void Dispose()
    {
        _filters.PropChanged -= OnFiltersChanged;
        _cts.Cancel();
        GC.SuppressFinalize(this);
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
