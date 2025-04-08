// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using static EverythingCmdPal.Interop.NativeMethods;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System;
using Windows.Storage.Streams;
using EverythingCmdPal.Helpers;
using System.IO;
using EverythingCmdPal.Commands;

namespace EverythingCmdPal;

internal sealed partial class Results : DynamicListPage
{
    internal readonly SettingsManager _settings;
    public Results(SettingsManager settings)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Name = "Everything";
        PlaceholderText = Resources.noquery;
        _settings = settings;
        ShowDetails = true;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        // Need to call this to invoke GetItem
        // passing in a number doesn't seem to do anything, omitted
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        //ExtensionHost.LogMessage("Query:\n" + SearchText + "\nLength: " + SearchText.Length);
        // Don't waste resource searching if there's no query
        // On first launch GetItems is called (no query)
        if (SearchText.Length == 0)
            return [];

        IsLoading = true;

        string query = SearchText;
        if (_settings.Prefix.Length > 0)
            query = _settings.Prefix + SearchText;

        if (_settings.is1_4 && SearchText.Contains(':'))
            foreach (var kv in _settings.Filters)
                if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    query = query.Replace(kv.Key, string.Empty, StringComparison.OrdinalIgnoreCase).Trim() + $" {kv.Value}";

        Everything_SetSearchW(query);
        if (!Everything_QueryW(true))
        {
            // Throwing an exception would make sense, however,
            // WinRT & COM totally eat any exception info.
            // var e = new Win32Exception("Unable to Query");
            var lastError = Everything_GetLastError();
            var message = lastError switch
            {
                (uint)EverythingErrors.EVERYTHING_OK => "The operation completed successfully",
                (uint)EverythingErrors.EVERYTHING_ERROR_MEMORY => "Failed to allocate memory for the search query",
                (uint)EverythingErrors.EVERYTHING_ERROR_IPC => "IPC is not available",
                (uint)EverythingErrors.EVERYTHING_ERROR_REGISTERCLASSEX => "Failed to register the search query window class",
                (uint)EverythingErrors.EVERYTHING_ERROR_CREATEWINDOW => "Failed to create the search query window",
                (uint)EverythingErrors.EVERYTHING_ERROR_CREATETHREAD => "Failed to create the search query thread",
                (uint)EverythingErrors.EVERYTHING_ERROR_INVALIDINDEX => "Invalid index.The index must be greater or equal to 0 and less than the number of visible results",
                (uint)EverythingErrors.EVERYTHING_ERROR_INVALIDCALL => "Invalid call",
                _ => "Unexpected error",
            };
            List<ListItem> items =
            [
                new ListItem(new NoOpCommand()) { Title = "Failed to query. Error was:", },
            ];

            if (lastError == (uint)EverythingErrors.EVERYTHING_ERROR_IPC)
            {
                items.Add(new ListItem(new StartEverythingCommand(this))
                {
                    Title = message,
                    Subtitle = $"0x{lastError:X8}",
                });
                ExtensionHost.ShowStatus(new StatusMessage() { Message = Resources.everything_not_running, State = MessageState.Error }, StatusContext.Page);
            }
            else
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = message,
                    Subtitle = $"0x{lastError:X8}",
                });
            }
            IsLoading = false;
            return [.. items];
        }

        var resultCount = Everything_GetNumResults();

        // Create a List to store ListItems
        var itemList = new List<ListItem>();

        // Loop through the results and add them to the List
        for (uint i = 0; i < resultCount; i++)
        {
            // Get the result file name
            var fileName = Marshal.PtrToStringUni(Everything_GetResultFileNameW(i));

            // Get the result file path
            var filePath = Marshal.PtrToStringUni(Everything_GetResultPathW(i));

            // Get the result file extension
            var ext = Marshal.PtrToStringUni(Everything_GetResultExtensionW(i));

            long size = EPT_GetSizeKB(i);

            string md = EPT_GetModifiedDateTime(i);

            if (fileName == null || filePath == null)
                continue;

            // Check if it's a folder
            bool isFolder = Everything_IsFolderResult(i);

            // Concatenate the file path and file name
            var fullName = Path.Combine(filePath, fileName);

            // Get icon of the file
            IconInfo? icon = null;
            try
            {
                var stream = ThumbnailHelper.GetThumbnail(fullName).Result;

                if (stream != null)
                {
                    var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
                    icon = new IconInfo(data, data);
                }
            }
            catch (Exception e)
            {
                ExtensionHost.LogMessage($"Failed to get the icon: {fullName}\n{e.Message}");
            }
            icon ??= IconHelpers.FromRelativePath("Assets\\EverythingPt.svg");

            itemList.Add(new ListItem(new OpenCommand(fullName))
            {
                Title = fileName,
                Subtitle = filePath,
                Icon = icon,
                MoreCommands = new CommandHandler().LoadCommands(fullName, isFolder, this),
                Details = new Details()
                {
                    //// Title and Body text is large and unselectable, but displayed right under HeroImage
                    //Title = fileName,
                    //Body = fileName,                    
                    //HeroImage = icon,
                    Metadata = [
                        new DetailsElement() {
                            Key = fileName,
                            Data = new DetailsLink(){
                            Text = filePath}
                        },
                        new DetailsElement() {
                            Data = new DetailsSeparator()
                        },
                        new DetailsElement(){
                            Key = "Size",
                            Data = new DetailsLink(){
                                Text = size.ToString("N0", CultureInfo.InvariantCulture)+" KB",
                            }
                        },
                        new DetailsElement() {
                            Key = "Extension",
                            Data = new DetailsTags(){
                                Tags=[
                                    new Tag(ext ?? string.Empty){
                                        Foreground = ColorHelpers.FromRgb(51,51,51),
                                        Background=ColorHelpers.FromRgb(224,204,179)},
                                ]
                            }
                        },
                        new DetailsElement(){
                            Key = "Modified Date",
                            Data = new DetailsLink(){
                                Text = md
                            }
                        }

                        //new DetailsElement() { Key = "Title Display Text", Data = new DetailsLink("https://github.com/lin-ycv", "clickable text to open the url") },
                    ],
                },
            });
        }

        if (_settings.ShowMore)
        {
            itemList.Add(new ListItem(new ShowMoreCommand(query, _settings.Exe))
            {
                Title = Resources.show_more,
                Subtitle = Resources.show_more_subtitle,
                Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg"), //new IconInfo("\uF78B"),
            });
        }
        IsLoading = false;
        return [.. itemList];
    }
}
