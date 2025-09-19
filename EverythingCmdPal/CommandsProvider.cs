using System.Linq;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal;

public partial class CommandsProvider : CommandProvider
{
    private readonly ListItem _listItem;
    private static ResultsPage _resultsPage = null!;

    public CommandsProvider()
    {
        Id = "Everything";
        DisplayName = "Everything";
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Settings = Query.Settings.Settings;
        _resultsPage = new ResultsPage();
        _listItem = new(_resultsPage) { Subtitle = Resources.top_level_subtitle };
    }

    public override ICommandItem[] TopLevelCommands() => [_listItem];

    public override IFallbackCommandItem[]? FallbackCommands()
    {
        // Prefill list of fallbackitems to show in main list
        // They auto update themselves with matches
        // If they don't have a match in the query, they don't show
        var fallbackItems = Enumerable.Range(0, (int)Query.Settings.Max).Select(x => new EverythingFallbackItem(x));
        return [.. fallbackItems];
    }
}
