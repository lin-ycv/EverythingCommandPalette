using EverythingCmdPal3.Commands;
using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Windows.Storage.Streams;
using static EverythingCmdPal3.Interop.NativeMethods;

namespace EverythingCmdPal3;

internal sealed partial class ResultsPage : DynamicListPage, IDisposable
{
    internal readonly SettingsManager settings;
    private static readonly Lock _lock = new();
    private static CancellationTokenSource _cts = new();
    internal static IntPtr Client { get; set; }
    internal IntPtr SearchState { get; set; }
    const uint MAX_PATH = 32767;
    readonly List<IListItem> results = [];
    bool firstrun = true;
    public ResultsPage(SettingsManager _settings)
    {
        settings = _settings;
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Title = "ECP3";
        Name = "Everything3";
        Id = "com.microsoft.cmdpal.everything3";
        Client = IntPtr.Zero;
        ShowDetails = true;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        // Cmdpal handles, only executs if there is a change
        //if (newSearch.Length == 0 || oldSearch == newSearch) return;

        IsLoading = true;
        IntPtr resultsPtr = IntPtr.Zero;

        CancellationTokenSource cts, old = _cts;
        lock (_lock)
        {
            _cts.Cancel();
            _cts = new();
            cts = _cts;
        }

        try
        {
            if (Client == IntPtr.Zero)
            {
                Client = Everything3_ConnectW(settings.Instance);
                if (Client == IntPtr.Zero)
                {
                    Client = Everything3_ConnectW(string.Empty);
                    if (Client == IntPtr.Zero)
                    {
                        Client = Everything3_ConnectW("1.5a");
                        if (Client == IntPtr.Zero)
                            throw new ArgumentException(string.Empty);
                    }
                }
            }

            cts.Token.ThrowIfCancellationRequested();
            if (SearchState == IntPtr.Zero)
            {
                SearchState = Everything3_CreateSearchState();
                if (SearchState == IntPtr.Zero)
                    throw new ArgumentException(string.Empty);
            }

            if (firstrun || settings.updatedneeded)
            {
                Everything3_ClearSearchSorts(SearchState);
                Everything3_ClearSearchPropertyRequests(SearchState);

                Everything3_AddSearchSort(SearchState, (Property)settings.SortOption, settings.SortAscending);
                Everything3_AddSearchSort(SearchState, (Property)settings.SortOption2, settings.SortAscending2);

                // each prop needs to be set separately
                Everything3_AddSearchPropertyRequest(SearchState, Property.FULL_PATH); 
                Everything3_AddSearchPropertyRequest(SearchState, Property.IS_FOLDER);
                Everything3_AddSearchPropertyRequest(SearchState, Property.DATE_MODIFIED);
                Everything3_AddSearchPropertyRequest(SearchState, Property.SIZE);

                Everything3_SetSearchMatchPath(SearchState, settings.Match);
                Everything3_SetSearchRegex(SearchState, settings.Regex);

                firstrun = false;
                settings.updatedneeded = false;
            }

            Everything3_SetSearchTextW(SearchState, $"{settings.Prefix}{SearchText}");
            resultsPtr = Everything3_Search(Client, SearchState);

            cts.Token.ThrowIfCancellationRequested();
            if (resultsPtr == IntPtr.Zero)
                throw new ArgumentException(string.Empty);

            uint count = Everything3_GetResultListCount(resultsPtr);

            // write to temp list to prevent rash conditions when query changes rapidly
            List<IListItem> qresults = []; 
            char[] namebuffer;
            int max = settings.Max;
            for (uint i = 0; i < Math.Min(max, count); i++)
            {
                cts.Token.ThrowIfCancellationRequested();
                namebuffer = new char[MAX_PATH];
                ulong length = Everything3_GetResultFullPathNameW(resultsPtr, i, namebuffer, MAX_PATH);
                bool isFolder = Everything3_IsFolderResult(resultsPtr, i);
                string fullPath = new(namebuffer, 0, unchecked((int)length));
                string filename = Path.GetFileName(fullPath);
                string filepath = Path.GetDirectoryName(fullPath) ?? filename;
                string ext = isFolder ? "Folder" : Path.GetExtension(fullPath);
                if (filename == null) continue;
                IconInfo icon = null;
                try
                {
                    var stream = ThumbnailHelper.GetThumbnail(fullPath).Result;
                    if (stream != null)
                    {
                        IconData data = new(RandomAccessStreamReference.CreateFromStream(stream));
                        icon = new(data, data);
                    }
                }
                catch (Exception e)
                {
                    ExtensionHost.LogMessage($"Failed to get the icon: {fullPath}\n{e.Message}");
                }
                icon ??= isFolder ? new("\ue8b7") : IconHelpers.FromRelativePath("Assets\\EverythingPt.svg");
                qresults.Add(new ListItem(new OpenCommand(fullPath))
                {
                    Title = filename,
                    Subtitle = filepath,
                    Icon = icon,
                    MoreCommands = new CommandsHandler().LoadCommands(fullPath, isFolder, this),
                    Details = new Details()
                    {
                        HeroImage = icon,
                        Metadata =
                        [
                            new DetailsElement()
                        {
                            Key = filename,
                            Data = new DetailsLink(){ Text = filepath }
                        },
                        new DetailsElement(){
                            Data = new DetailsSeparator()
                        },
                        new DetailsElement(){
                            Key = Resources.size,
                            Data = new DetailsLink(){ Text = ECP_GetSize(resultsPtr,i)}
                        },
                        new DetailsElement(){
                            Key = Resources.ext,
                            Data = new DetailsTags(){
                                Tags = [
                                    new Tag(ext){
                                        Foreground = ColorHelpers.FromRgb(51,51,51),
                                        Background=ColorHelpers.FromRgb(224,204,179)
                                    }
                                ]
                            }
                        },
                        new DetailsElement(){
                            Key = Resources.date_mod,
                            Data = new DetailsLink(){ Text = ECP_GetModifiedDateTime(resultsPtr, i) }
                        }
                        ]
                    }
                });
            }
            if(settings.ShowMore && count > max)
            {
                qresults.Add(new ListItem(new ShowMore(SearchText, settings.Exe))
                {
                    //Title = Resources.show_more,
                    Subtitle = Resources.show_more_subtitle,
                });
            }
            

            cts.Token.ThrowIfCancellationRequested();
            results.Clear();
            results.AddRange(qresults);
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(OperationCanceledException))
            {
                uint er = Everything3_GetLastError();
                if (er == 0)
                    results.Add(new ListItem(new NoOpCommand())
                    {
                        Icon = new IconInfo("\ue783"),
                        Title = Resources.ERROR_UNKNOWN,
                        Subtitle = e.Message
                    });
                else if(er == 0xE0000002 && Path.Exists(settings.Exe))
                {
                    IconInfo icon = new IconInfo("\uf78b");
                    results.Add(new ListItem(new OpenCommand(settings.Exe))
                    {
                        Icon = icon,
                        Title = Resources.launch_e,
                        Subtitle = $"0x{er:X8}",
                        Details = new Details()
                        {
                            HeroImage = icon,
                            Title = Resources.ERROR,
                            Body = ECP_TranslateError(er)
                        }
                    });
                }
                else
                    results.Add(new ListItem(new NoOpCommand())
                    {
                        Icon = new IconInfo("\ue783"),
                        Title = $"{Resources.ERROR}: {ECP_TranslateError(er)}",
                        Subtitle = $"0x{er:X8}",
                        Details = e.Message.Length > 0 ? new Details()
                        {
                            Body = $"{e.Message}"
                        } : null
                    });
            }
        }
        finally
        {
            IsLoading = false;
            old.Dispose();
            Everything3_DestroyResultList(resultsPtr);        
            RaiseItemsChanged(results.Count); // if not raised, cmdpal will crash if query changes rapidly
        }
    }

    public override IListItem[] GetItems() => [.. results];

    void IDisposable.Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        Everything3_DestroySearchState(SearchState);
        Everything3_DestroyClient(Client);
    }
}
