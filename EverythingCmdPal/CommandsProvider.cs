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
    public CommandsProvider()
    {
        Id = "Everything";
        DisplayName = "Everything";
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Settings = Query.Settings.Settings;
        _listItem = new(new ResultsPage()) { Subtitle = Resources.top_level_subtitle };
    }

    public override ICommandItem[] TopLevelCommands() => [_listItem];

    //// Works for 0.1.0 but not 0.2.0 (something changed)
    //// Does not show results on main page, just an option to open Everything page with the query from main page
    //public override IFallbackCommandItem[]? FallbackCommands()
    //{
    //    ResultsPage results = new();
    //    FallbackCommandItem listitem = new(results, Resources.top_level_subtitle);
    //    return [listitem];

    //    //// To add results to the main page, have to do something like this below
    //    //// Except how to get query text? 
    //    //// App extension seems to be able to do this, but can't figure out how
    //    //// https://github.com/microsoft/PowerToys/issues/39315
    //    //List<Result> results = Query.Search(query, cts.Token);
    //    //foreach (Result r in results)
    //    //{
    //    //    _results.Add(new ListItem(new OpenCommand(r.FullName));
    //    //}
    //}
}
