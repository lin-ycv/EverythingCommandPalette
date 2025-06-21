// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3;

public partial class ECP3CommandsProvider : CommandProvider
{
    private readonly static SettingsManager settings = new();
    private readonly ListItem _listItem = new(new ResultsPage(settings)) 
    { 
        Subtitle = Resources.top_level_subtitle 
    };

    public ECP3CommandsProvider()
    {
        Id = "ECP3";
        DisplayName = "Everything3";
        Icon = IconHelpers.FromRelativePath("Assets\\EverythingPT.svg");
        Settings = settings.Settings;
    }
    public override ICommandItem[] TopLevelCommands() => [_listItem];

}
