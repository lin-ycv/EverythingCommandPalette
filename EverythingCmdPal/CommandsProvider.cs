using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal;

public partial class CommandsProvider : CommandProvider
{
    private readonly SettingsManager _settingsManager = new();
    private readonly ListItem _listItem;

    public CommandsProvider()
    {
        Id = "Everything";
        DisplayName = "Everything";
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Settings = _settingsManager.Settings;
        _listItem = new(new Results(_settingsManager)) { Subtitle = Resources.top_level_subtitle };
    }

    public override ICommandItem[] TopLevelCommands() => [_listItem];
}
