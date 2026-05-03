using EverythingCmdPal.Commands;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;

namespace EverythingCmdPal;

public partial class CommandsProvider : CommandProvider
{
    private readonly ListItem _listItem;
    private readonly FallbackSearchController _fallbackController;

    public CommandsProvider()
    {
        Id = "Everything";
        DisplayName = "Everything";
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Settings = Query.Settings.Settings;
        Frozen = false; // Signal that our items change dynamically
        _listItem = new(new ResultsPage()) { Subtitle = Resources.top_level_subtitle };
        _fallbackController = new FallbackSearchController(() => RaiseItemsChanged());
    }

    public override ICommandItem[] TopLevelCommands()
    {
        List<ICommandItem> items = [_listItem];
        items.AddRange(_fallbackController.TopResults);
        return [.. items];
    }

    public override IFallbackCommandItem[] FallbackCommands() =>
        _fallbackController.FallbackItems;
}
