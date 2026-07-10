using EverythingCmdPal.Commands;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal;

public partial class CommandsProvider : CommandProvider
{
    private readonly ListItem _listItem;
    private readonly FallbackSearchController _fallbackController = new();

    public CommandsProvider()
    {
        Id = "Everything";
        DisplayName = "Everything";
        Icon = Query.DefaultIcon;
        Settings = Query.Settings.Settings;
        _listItem = new(new ResultsPage()) { Subtitle = Resources.top_level_subtitle };
    }

    public override ICommandItem[] TopLevelCommands() => [_listItem];

    public override IFallbackCommandItem[] FallbackCommands() =>
        _fallbackController.FallbackItems;
}
