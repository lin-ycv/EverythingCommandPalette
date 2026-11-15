using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using static EverythingCmdPal3.Interop.NativeMethods;

namespace EverythingCmdPal3.Helpers;

// SDK3 has per-client state. This service owns one client, serializes its use,
// and returns managed values so no row can outlive a native result list.
internal sealed partial class EverythingSearchService : IDisposable
{
    private readonly object _gate = new();
    private nint _client;
    private string? _instance;
    private bool _disposed;
    private const int MaxPath = 32768;
    internal const int TimeoutMilliseconds = 5000;

    internal SearchResponse Search(string text, SearchSettings settings, CancellationToken token,
        bool probe = false)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            token.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeoutMilliseconds);
            var operationToken = timeout.Token;
            try
            {
                nint client = Connect(settings.Instance);
                // Registration owns a borrow of this client. It is disposed
                // before ResetClient can destroy it, including on cancellation.
                using var registration = operationToken.Register(() => Everything3_ShutdownClient(client));
                operationToken.ThrowIfCancellationRequested();
                return Execute(client, text, settings, probe, operationToken);
            }
            catch (Exception) when (operationToken.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
                throw new TimeoutException("Everything took longer than five seconds. Check its indexing and sort settings.");
            }
            catch (EverythingException)
            {
                ResetClient(); // A restarted/disconnected server is retried on the next query.
                throw;
            }
            finally
            {
                if (operationToken.IsCancellationRequested) ResetClient();
            }
        }
    }

    private nint Connect(string instance)
    {
        if (_client != 0 && _instance != instance) ResetClient();
        if (_client != 0) return _client;
        // Respect the selected instance; silently falling back searches the wrong database.
        _client = Everything3_ConnectW(instance);
        if (_client == 0) throw Failure(string.IsNullOrEmpty(instance)
            ? "Connect to the unnamed Everything instance" : $"Connect to Everything instance '{instance}'");
        _instance = instance;
        return _client;
    }

    private static SearchResponse Execute(nint client, string text, SearchSettings settings,
        bool probe, CancellationToken token)
    {
        nint state = Everything3_CreateSearchState();
        if (state == 0) throw Failure("CreateSearchState");
        nint list = 0;
        var timer = Stopwatch.StartNew();
        try
        {
            Check(Everything3_SetSearchTextW(state, text));
            Check(Everything3_SetSearchMatchPath(state, settings.Match));
            Check(Everything3_SetSearchRegex(state, settings.Regex));
            ConfigureViewport(state, (nuint)(probe ? 1 : settings.Max));
            string? warning = null;
            if (!probe)
            {
                ValidateSort(client, (Property)settings.SortOption, ref warning);
                if (settings.SortOption2 >= 0 && settings.SortOption2 != settings.SortOption)
                    ValidateSort(client, (Property)settings.SortOption2, ref warning);
                Check(Everything3_AddSearchSort(state, (Property)settings.SortOption, settings.SortAscending));
                if (settings.SortOption2 >= 0 && settings.SortOption2 != settings.SortOption)
                    Check(Everything3_AddSearchSort(state, (Property)settings.SortOption2, settings.SortAscending2));
            }
            else Check(Everything3_AddSearchSort(state, Property.NAME, true));
            Check(Everything3_AddSearchPropertyRequest(state, Property.FULL_PATH));
            Check(Everything3_AddSearchPropertyRequest(state, Property.IS_FOLDER));
            if (settings.DetailPane && !probe)
            {
                Check(Everything3_AddSearchPropertyRequest(state, Property.DATE_MODIFIED));
                Check(Everything3_AddSearchPropertyRequest(state, Property.SIZE));
            }
            token.ThrowIfCancellationRequested();
            long start = timer.ElapsedMilliseconds;
            list = Everything3_Search(client, state);
            long nativeMs = timer.ElapsedMilliseconds - start;
            if (list == 0) throw Failure("Search");
            if (!probe && nativeMs >= 250 && settings.SortOption2 >= 0 && settings.SortOption2 != settings.SortOption)
                warning ??= $"Search took {nativeMs} ms with two sort keys. Set Secondary sort to None to reduce work on broad queries.";
            token.ThrowIfCancellationRequested();
            nuint total = Everything3_GetResultListCount(list);
            nuint count = Everything3_GetResultListViewportCount(list);
            count = nuint.Min(count, (nuint)(probe ? 1 : settings.Max));
            var results = new List<SearchResult>((int)count);
            // Reuse one UTF-16 buffer per query, rather than 64 KB per row.
            var buffer = new char[MaxPath];
            long metadataStart = timer.ElapsedMilliseconds;
            for (nuint i = 0; i < count; ++i)
            {
                token.ThrowIfCancellationRequested();
                nuint length = Everything3_GetResultFullPathNameW(list, i, buffer, (nuint)buffer.Length);
                if (length >= (nuint)buffer.Length) throw new IOException("Everything returned a path exceeding the Windows path limit.");
                if (length == 0)
                {
                    uint error = Everything3_GetLastError();
                    if (error != 0) throw new EverythingException("Read path", error);
                    continue;
                }
                string path = new(buffer, 0, (int)length);
                bool folder = Everything3_IsFolderResult(list, i);
                results.Add(new(path, folder,
                    settings.DetailPane && !probe ? ECP_GetSize(list, i) : string.Empty,
                    settings.DetailPane && !probe ? ECP_GetModifiedDateTime(list, i) : string.Empty));
            }
            return new([.. results], total > count, nativeMs, timer.ElapsedMilliseconds - metadataStart, warning);
        }
        finally
        {
            if (list != 0) Everything3_DestroyResultList(list);
            Everything3_DestroySearchState(state);
        }
    }

    private static void ValidateSort(nint client, Property property, ref string? warning)
    {
        bool indexed = Everything3_IsPropertyIndexed(client, property);
        uint error = Everything3_GetLastError();
        if (!indexed && error != 0) throw new EverythingException("Check indexed property", error);
        if (!indexed)
            throw new InvalidOperationException($"Enable {property} indexing in Everything before sorting by it.");
        bool fast = Everything3_IsPropertyFastSort(client, property);
        error = Everything3_GetLastError();
        if (!fast && error != 0) throw new EverythingException("Check fast sort", error);
        if (!fast) warning = $"Enable fast sorting for {property} in Everything to speed up broad searches.";
    }

    // Action invocations must not wait behind an in-flight query or reuse its
    // cancellable client. Run history uses a separate short-lived connection.
    internal static void RecordRun(string path, string instance)
    {
        _ = Task.Run(() =>
        {
            nint client = 0;
            try
            {
                client = Everything3_ConnectW(instance);
                if (client != 0) _ = Everything3_IncRunCountFromFilenameW(client, path);
            }
            catch (Exception ex) { ExtensionHost.LogMessage($"ECP3: Run history unavailable: {ex.Message}"); }
            finally { if (client != 0) Everything3_DestroyClient(client); }
        });
    }

    internal static void ConfigureViewport(nint state, nuint count)
    {
        // SDK3 3.0.1.12 applies both viewport setters but always returns FALSE.
        // Read the state back instead of treating that vendor bug as failure.
        _ = Everything3_SetSearchViewportOffset(state, 0);
        nuint actualOffset = Everything3_GetSearchViewportOffset(state);
        uint error = Everything3_GetLastError();
        if (error != 0) throw new EverythingException(nameof(Everything3_GetSearchViewportOffset), error);
        if (actualOffset != 0)
            throw new InvalidOperationException($"Everything did not apply the search offset (expected 0; received {actualOffset}).");

        _ = Everything3_SetSearchViewportCount(state, count);
        nuint actualCount = Everything3_GetSearchViewportCount(state);
        error = Everything3_GetLastError();
        if (error != 0) throw new EverythingException(nameof(Everything3_GetSearchViewportCount), error);
        if (actualCount != count)
            throw new InvalidOperationException($"Everything did not apply the result limit (expected {count}; received {actualCount}).");
    }

    private static void Check(bool success, [CallerArgumentExpression(nameof(success))] string operation = "Configure search")
    {
        if (!success) throw Failure(operation);
    }
    private static EverythingException Failure(string operation) => new(operation, Everything3_GetLastError());
    private void ResetClient()
    {
        if (_client != 0) Everything3_DestroyClient(_client);
        _client = 0;
        _instance = null;
    }
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            ResetClient();
        }
    }
}

internal sealed class EverythingException(string operation, uint code)
    : Exception(code == 0
        ? $"{operation}: Everything reported failure without an error code."
        : $"{operation}: {ECP_TranslateError(code)} (0x{code:X8})")
{
    internal uint ErrorCode { get; } = code;
}
