using EverythingCmdPal.Commands;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CommandPalette.Extensions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using static EverythingCmdPal.Interop.NativeMethods;
using Windows.Storage.Streams;
using System.IO;
using System.Diagnostics;

namespace EverythingCmdPal.Helpers
{
    internal static class Query
    {
        internal static readonly IconInfo DefaultIcon = IconHelpers.FromRelativePaths("Assets\\EverythingPT_theme-light.svg", "Assets\\EverythingPT_theme-dark.svg");
        internal static readonly SettingsManager Settings = new();
        // Gate all Everything SDK access — the SDK uses global process state
        // and must be serialized across all callers (ResultsPage + fallback).
        private static readonly SemaphoreSlim _sdkGate = new(1, 1);

        internal sealed record SearchResponse(List<Result>? Results, uint ErrorCode = 0,
            string? ErrorMessage = null, string? WarningMessage = null);

        internal static async Task<SearchResponse> Search(string query, SearchSettings settings,
            CancellationToken token, int maxResults = -1, bool loadThumbnails = true)
        {
            var results = new List<Result>();
            var timer = Stopwatch.StartNew();
            long gateMs = 0, queryMs = 0, metadataMs = 0;
            bool acquired = false;
            string? warning = null;
            try
            {
                await _sdkGate.WaitAsync(token);
                acquired = true;
                try
                {
                    token.ThrowIfCancellationRequested();
                    gateMs = timer.ElapsedMilliseconds;
                    var preflight = SearchPreflight.Check((Sort)settings.SortOption,
                        type => ReadCapability(() => Everything_IsFileInfoIndexed(type)),
                        sort => ReadCapability(() => Everything_IsFastSort(sort)));
                    if (preflight.Blocked)
                        return new(null, preflight.ErrorCode, preflight.ErrorMessage);
                    warning = preflight.WarningMessage;
                    token.ThrowIfCancellationRequested();
                    uint limit = maxResults > 0 ? Math.Min((uint)maxResults, settings.Max) : settings.Max;
                    bool details = settings.DetailPane && loadThumbnails;
                    Everything_SetRequestFlags(SearchPreflight.RequestFlags(details));
                    // A fallback existence probe needs no ranked results. Avoid
                    // sorting all matches by metadata just to retrieve one item.
                    Everything_SetSort(loadThumbnails ? (Sort)settings.SortOption : Sort.NAME_ASCENDING);
                    Everything_SetMax(limit);
                    Everything_SetMatchPath(settings.Match);
                    Everything_SetRegex(settings.Regex);
                    Everything_SetSearchW(PrepareQuery(query, settings));
                    long queryStart = timer.ElapsedMilliseconds;
                    bool success = Everything_QueryW(true);
                    queryMs = timer.ElapsedMilliseconds - queryStart;
                    if (!success)
                        return new SearchResponse(null, Everything_GetLastError());
                    token.ThrowIfCancellationRequested();

                    uint count = Math.Min(Everything_GetNumResults(), limit);
                    for (uint i = 0; i < count; ++i)
                    {
                        token.ThrowIfCancellationRequested();
                        string? name = Marshal.PtrToStringUni(Everything_GetResultFileNameW(i));
                        string? path = Marshal.PtrToStringUni(Everything_GetResultPathW(i));
                        if (name is null || path is null) continue;
                        results.Add(new Result
                        {
                            FileName = name,
                            FilePath = path,
                            FullName = Path.Combine(path, name),
                            Extension = Marshal.PtrToStringUni(Everything_GetResultExtensionW(i)) ?? string.Empty,
                            IsFolder = Everything_IsFolderResult(i),
                            Preview = details,
                            SizeKB = details ? EPT_GetSizeKB(i) : 0,
                            ModifiedDate = details ? EPT_GetModifiedDateTime(i) : string.Empty,
                            Icon = DefaultIcon,
                        });
                    }
                    metadataMs = timer.ElapsedMilliseconds - queryStart - queryMs;
                }
                finally
                {
                    try { Everything_SetMax(settings.Max); }
                    finally { _sdkGate.Release(); }
                }

                // Shell and thumbnail work cannot mutate Everything's global query
                // state. Keep it outside the gate so fallback probes aren't delayed.
                if (loadThumbnails)
                {
                    var typeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var result in results)
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            var stream = await ResultThumbnailHelper.GetThumbnail(result.FullName, result.IsFolder);
                            if (stream is not null)
                            {
                                result.ThumbnailStream = stream;
                                token.ThrowIfCancellationRequested();
                                var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
                                result.Icon = new IconInfo(data, data);
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            // The row is not published yet; a failed IconInfo
                            // construction must not retain its unused stream.
                            result.Dispose();
                            result.Icon = DefaultIcon;
                            ExtensionHost.LogMessage($"ECP: Icon unavailable: {result.FullName}: {ex.Message}");
                        }
                        if (settings.DetailPane)
                        {
                            string key = result.IsFolder ? "\0folder" : result.Extension;
                            if (!typeNames.TryGetValue(key, out string? type))
                            {
                                type = Interop.Shell32Methods.GetFileTypeDescription(result.FullName, result.IsFolder) ?? string.Empty;
                                typeNames.Add(key, type);
                            }
                            result.FileType = type;
                        }
                    }
                }
                token.ThrowIfCancellationRequested();
                return new SearchResponse(results, WarningMessage: warning);
            }
            catch
            {
                DisposeResults(results);
                throw;
            }
            finally
            {
                if (!acquired) gateMs = timer.ElapsedMilliseconds;
                if (timer.ElapsedMilliseconds >= 1000)
                    ExtensionHost.LogMessage($"ECP: Slow search: total={timer.ElapsedMilliseconds}ms, SDK gate={gateMs}ms, native query={queryMs}ms, metadata={metadataMs}ms. Remaining time includes preflight and Shell/thumbnail work.");
            }
        }

        private static SearchPreflight.Capability ReadCapability(Func<bool> check)
        {
            bool enabled = check();
            // False also means IPC failure. Capture its error immediately, under
            // the same SDK gate, before another native call can overwrite it.
            return new(enabled, enabled ? 0 : Everything_GetLastError());
        }

        internal static string PrepareQuery(string query, SearchSettings settings)
        {
            query = settings.Prefix + query;
            if (settings.IsEverything14 && query.Contains(':'))
                foreach (var filter in settings.Filters)
                    if (query.Contains(filter.Key, StringComparison.OrdinalIgnoreCase))
                        query = query.Replace(filter.Key, string.Empty, StringComparison.OrdinalIgnoreCase).Trim() + $" {filter.Value}";
            return query;
        }

        internal static void IncrementRunCount(string path)
        {
            _sdkGate.Wait();
            try { _ = Everything_IncRunCountFromFileNameW(path); }
            finally { _sdkGate.Release(); }
        }

        internal static void DisposeResults(List<Result>? results)
        {
            if (results is null) return;
            foreach (var result in results) result.Dispose();
        }

        internal static List<ListItem> EverythingFailed(uint lastError, string? errorMessage = null)
        {
            if (errorMessage is not null)
                return [new ListItem(new NoOpCommand())
                {
                    Title = "Search blocked by Everything indexing settings",
                    Subtitle = errorMessage,
                }];
            // Throwing an exception would make sense, however,
            // WinRT & COM totally eat any exception info.
            // var e = new Win32Exception("Unable to Query");
            var message = lastError switch
            {
                (uint)EverythingErrors.EVERYTHING_OK => "Everything could not complete the query and reported no error code. Check the Everything client and query settings.",
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
                //ExtensionHost.ShowStatus(new StatusMessage() { Message = Resources.everything_not_running, State = MessageState.Error }, StatusContext.Page);
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
