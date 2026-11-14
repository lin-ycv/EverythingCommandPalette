using System;
using EverythingCmdPal.Commands;
using EverythingCmdPal.Helpers;
using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal;

public partial class CommandsProvider : CommandProvider, IDisposable
{
    private readonly ListItem _listItem;
    private readonly ResultsPage _resultsPage = new();
    private readonly FallbackSearchController _fallbackController = new();

    public CommandsProvider()
    {
        Id = "Everything";
        DisplayName = "Everything";
        Icon = Query.DefaultIcon;
        Settings = Query.Settings.Settings;
        _listItem = new(_resultsPage) { Subtitle = Resources.top_level_subtitle };
    }

    public override void Dispose()
    {
        _resultsPage.Dispose();
        _fallbackController.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    public override ICommandItem[] TopLevelCommands() => [_listItem];

    public override IFallbackCommandItem[] FallbackCommands() =>
        _fallbackController.FallbackItems;
}
