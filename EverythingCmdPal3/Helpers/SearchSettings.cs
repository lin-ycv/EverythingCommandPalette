namespace EverythingCmdPal3.Helpers;

// One committed snapshot travels with the query and every action in its rows.
internal sealed record SearchSettings(
    string Instance, int SortOption, bool SortAscending,
    int SortOption2, bool SortAscending2, int Max, string Prefix,
    bool Match, bool Regex, bool RunAs, bool ShowMore, string Exe,
    bool EnableSend, string Sendto, bool DetailPane, bool CtrlEnterOpenFolder,
    bool EnableDelete)
{
    internal bool RequiresNewSearchComparedTo(SearchSettings previous) =>
        Instance != previous.Instance || SortOption != previous.SortOption ||
        SortAscending != previous.SortAscending || SortOption2 != previous.SortOption2 ||
        SortAscending2 != previous.SortAscending2 || Max != previous.Max || Prefix != previous.Prefix ||
        Match != previous.Match || Regex != previous.Regex || DetailPane != previous.DetailPane;
}
