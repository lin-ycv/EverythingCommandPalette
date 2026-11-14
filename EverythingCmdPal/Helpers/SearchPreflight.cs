using System;
using static EverythingCmdPal.Interop.NativeMethods;

namespace EverythingCmdPal.Helpers;

// Keep configuration failures separate from native error codes. Everything can
// execute an unindexed sort without reporting an SDK error, but take minutes.
internal static class SearchPreflight
{
    internal readonly record struct Capability(bool Enabled, uint ErrorCode = 0);
    internal sealed record CheckResult(uint ErrorCode = 0, string? ErrorMessage = null, string? WarningMessage = null)
    {
        internal bool Blocked => ErrorCode != 0 || ErrorMessage is not null;
    }

    internal static CheckResult Check(Sort sort, Func<FileInfoType, Capability> indexed,
        Func<Sort, Capability> fastSort)
    {
        var requirement = sort switch
        {
            Sort.SIZE_ASCENDING or Sort.SIZE_DESCENDING => (FileInfoType.FileSize, "file size"),
            Sort.DATE_CREATED_ASCENDING or Sort.DATE_CREATED_DESCENDING => (FileInfoType.DateCreated, "date created"),
            Sort.DATE_MODIFIED_ASCENDING or Sort.DATE_MODIFIED_DESCENDING => (FileInfoType.DateModified, "date modified"),
            Sort.DATE_ACCESSED_ASCENDING or Sort.DATE_ACCESSED_DESCENDING => (FileInfoType.DateAccessed, "date accessed"),
            Sort.ATTRIBUTES_ASCENDING or Sort.ATTRIBUTES_DESCENDING => (FileInfoType.Attributes, "attributes"),
            _ => ((FileInfoType)0, string.Empty),
        };
        if (requirement.Item1 != 0)
        {
            var capability = indexed(requirement.Item1);
            if (capability.ErrorCode != 0) return new(capability.ErrorCode);
            if (!capability.Enabled)
                return new(ErrorMessage: $"Enable 'Index {requirement.Item2}' in Everything > Tools > Options > Indexes, or choose another sort in the extension settings.");
        }

        var fast = fastSort(sort);
        if (fast.ErrorCode != 0) return new(fast.ErrorCode);
        return fast.Enabled ? new() : new(WarningMessage:
            "This sort has no fast index. Enable a matching Fast sort option in Everything > Tools > Options > Indexes where available, or choose Name sorting for faster searches.");
    }

    internal static Request RequestFlags(bool details) => Request.FILE_NAME | Request.PATH | Request.EXTENSION
        | (details ? Request.DATE_MODIFIED | Request.SIZE : 0);
}
