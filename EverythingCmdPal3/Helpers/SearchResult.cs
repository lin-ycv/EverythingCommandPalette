namespace EverythingCmdPal3.Helpers;

internal sealed record SearchResult(string FullPath, bool IsFolder, string Size, string ModifiedDate);
internal sealed record SearchResponse(SearchResult[] Results, bool HasMore, long NativeMilliseconds,
    long MetadataMilliseconds, string? Warning = null);
