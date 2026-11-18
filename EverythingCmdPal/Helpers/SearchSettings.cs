using System.Collections.Generic;
using System.Linq;

namespace EverythingCmdPal.Helpers;

// One immutable view is used for both the native query and its result menus.
internal sealed record SearchSettings(
    int SortOption, uint Max, string Prefix, bool DetailPane, bool Match,
    bool Regex, bool RunAs, bool ShowMore, bool EnableDelete,
    bool CtrlEnterOpenFolder, string Exe, bool EnableSend, string Sendto,
    bool IsEverything14, IReadOnlyDictionary<string, string> Filters,
    bool ShowResultsWhenEmpty = false)
{
    // Context-menu changes must not clear results or restart native searches.
    internal bool RequiresNewSearchComparedTo(SearchSettings previous) =>
        SortOption != previous.SortOption || Max != previous.Max || Prefix != previous.Prefix ||
        DetailPane != previous.DetailPane || Match != previous.Match || Regex != previous.Regex ||
        ShowResultsWhenEmpty != previous.ShowResultsWhenEmpty ||
        IsEverything14 != previous.IsEverything14 || Filters.Count != previous.Filters.Count ||
        Filters.Any(pair => !previous.Filters.TryGetValue(pair.Key, out var value) || value != pair.Value);
}
