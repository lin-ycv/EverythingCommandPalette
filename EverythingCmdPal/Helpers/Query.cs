using EverythingCmdPal.Commands;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using static EverythingCmdPal.Interop.NativeMethods;
using Windows.Storage.Streams;
using System.IO;

namespace EverythingCmdPal.Helpers
{
    record TheResult(DateTimeOffset Timestamp, List<Result> Results);

    internal static class Query
    {
        internal static readonly SettingsManager Settings = new();
        static Dictionary<string, TheResult> _cache = [];
        private static readonly Lock _lock = new();

        // TODO: Lol obviously not actually 5h for merge, just for fiddling
        static TimeSpan _cacheExpiry = TimeSpan.FromHours(5);

        internal static List<Result> Search(string query, CancellationToken token)
        {
            lock (_lock)
            {
                List<Result> results = [];

                // Caching multiple results here as sometimes we can get multiple fallbackItems trying to call in here at once.
                // If we only cache the last item, we can end up tick-tocking back and forth between the last few searches.
                if (_cache.TryGetValue(query, out var previous) && DateTimeOffset.Now - previous.Timestamp < _cacheExpiry)
                {
                    return previous.Results;
                }

                if (token.IsCancellationRequested) return results;

                try
                {
                    
                    // The existing search method
                    results = SearchEverything(query, token);
                }
                catch
                {
                    // Do nothing
                }

                return results;
            }
        }

        static List<Result> SearchEverything(string query, CancellationToken token)
        {
            string orgQuery = query;
            token.ThrowIfCancellationRequested();
            if (Settings.Prefix.Length > 0)
                query = Settings.Prefix + query;

            if (Settings.is1_4 && query.Contains(':'))
                foreach (var kv in Settings.Filters)
                    if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                        query = query.Replace(kv.Key, string.Empty, StringComparison.OrdinalIgnoreCase).Trim() + $" {kv.Value}";

            Everything_SetSearchW(query);
            if (!Everything_QueryW(true) || token.IsCancellationRequested)
            {
                return [];
            }
            token.ThrowIfCancellationRequested();

            var resultCount = Everything_GetNumResults();

            // Create a List to store ListItems
            var resultsList = new List<Result>();

            // Loop through the results and add them to the List
            for (uint i = 0; i < resultCount; i++)
            {
                Result r = new();
                token.ThrowIfCancellationRequested();
                // Get the result file name
                r.FileName = Marshal.PtrToStringUni(Everything_GetResultFileNameW(i));

                // Get the result file path
                r.FilePath = Marshal.PtrToStringUni(Everything_GetResultPathW(i));

                // Get the result file extension
                r.Extension = Marshal.PtrToStringUni(Everything_GetResultExtensionW(i));

                r.SizeKB = EPT_GetSizeKB(i);

                r.ModifiedDate = EPT_GetModifiedDateTime(i);

                if (r.FileName == null || r.FilePath == null)
                    continue;

                // Check if it's a folder
                r.IsFolder = Everything_IsFolderResult(i);

                // Concatenate the file path and file name
                r.FullName = Path.Combine(r.FilePath, r.FileName);

                // Get icon of the file
                r.Icon = null;
                try
                {
                    var stream = ThumbnailHelper.GetThumbnail(r.FullName).Result;

                    if (stream != null)
                    {
                        var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
                        r.Icon = new IconInfo(data, data);
                    }
                }
                catch (Exception e)
                {
                    ExtensionHost.LogMessage($"Failed to get the icon: {r.FullName}\n{e.Message}");
                }
                r.Icon ??= IconHelpers.FromRelativePath("Assets\\EverythingPt.svg");

                resultsList.Add(r);
            }
            token.ThrowIfCancellationRequested();

            _cache[query] = new TheResult(DateTimeOffset.Now, resultsList);

            // TODO: cleanup cache?

            return resultsList;
        }

        internal static List<ListItem> EverythingFailed()
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
                items.Add(new ListItem(new StartEverythingCommand())
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
            return items;
        }
    }
}
