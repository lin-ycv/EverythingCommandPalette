// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EverythingCmdPal3.Helpers;
using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using EverythingCmdPal3.Commands;

namespace EverythingCmdPal3;

public partial class ECP3CommandsProvider : CommandProvider, IDisposable
{
    private readonly SettingsManager _settings = new();
    private readonly ResultsPage _page;
    private readonly FallbackSearchController _fallback;
    private readonly ListItem _listItem;

    public ECP3CommandsProvider()
    {
        _page = new ResultsPage(_settings);
        _fallback = new FallbackSearchController(_settings);
        _listItem = new ListItem(_page) { Subtitle = Resources.top_level_subtitle };
        Id = "ECP3";
        DisplayName = "Everything3";
        Icon = IconHelpers.FromRelativePaths("Assets\\EverythingPT_theme-light.svg", "Assets\\EverythingPT_theme-dark.svg");
        Settings = _settings.Settings;
    }
    public override ICommandItem[] TopLevelCommands() => [_listItem];
    public override IFallbackCommandItem[] FallbackCommands() => _fallback.FallbackItems;
    public override void Dispose()
    {
        _page.Dispose();
        _fallback.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

}
