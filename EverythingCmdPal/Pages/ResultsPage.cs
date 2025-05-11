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
    internal string pre = string.Empty;

    public ResultsPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Name = "Everything";
        PlaceholderText = Resources.noquery;
        ShowDetails = true;
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
                    _results.Add(new ListItem(new OpenCommand(r.FullName))
                    {
                        Title = r.FileName,
                        Subtitle = r.FilePath,
                        Icon = r.Icon,
                        MoreCommands = new CommandHandler().LoadCommands(r.FullName, r.IsFolder),
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
                                        Key = "Extension",
                                        Data = new DetailsTags(){
                                            Tags=[
                                                new Tag(r.Extension ?? string.Empty){
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
        UpdateQuery(newSearch);
    }

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
