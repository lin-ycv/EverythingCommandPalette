<div align="center">

# ![](https://rawcdn.githack.com/lin-ycv/EverythingCommandPalette/85bd13109b06544445a1d6552f99261988cd2a2a/EverythingCmdPal/Assets/EverythingPT.svg) Everything for Command Palette

[![GitHub repo size](https://img.shields.io/github/repo-size/lin-ycv/EverythingCommandPalette?style=flat-square)](#)
[![GitHub release (release name instead of tag name)](https://img.shields.io/github/v/release/lin-ycv/EverythingCommandPalette?style=flat-square)](https://github.com/lin-ycv/EverythingCommandPalette/releases/latest)
[![GitHub all releases](https://img.shields.io/github/downloads/lin-ycv/EverythingCommandPalette/total?style=flat-square)](https://github.com/lin-ycv/EverythingCommandPalette/releases/)
[![GitHub release (latest by date)](https://img.shields.io/github/downloads/lin-ycv/EverythingCommandPalette/latest/total?style=flat-square)](https://github.com/lin-ycv/EverythingCommandPalette/releases/latest)<br>
[![Chocolatey version](https://img.shields.io/chocolatey/v/everythingcmdpal?style=flat-square)](https://community.chocolatey.org/packages/everythingcmdpal)
[![Winget](https://img.shields.io/winget/v/lin-ycv.EverythingCmdPal?style=flat-square)](https://github.com/microsoft/winget-pkgs/tree/master/manifests/l/lin-ycv/EverythingCmdPal)

</div>

EverythingCommandPalette (ECP) is an extension for the program [PowerToys Command Palette (CmdPal)](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview)<br>
ECP adds the ability to search the computer for files and folders using [Everything](https://www.voidtools.com/) inside CmdPal

#### Install Methods
Chose the one you prefer:
1) MS Store
2) Chocolatey
3) WinGet
4) App Package (MSIX)

Detailed explanations and instructions are on the [wiki page](https://github.com/lin-ycv/EverythingCommandPalette/wiki)

#### Runtime requirements and builds

The extension uses framework-dependent .NET 10 deployment.

Install the [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) for your package architecture (x64 or ARM64).

The .NET 10 Desktop Runtime, ASP.NET Core Runtime, and SDK also satisfy this requirement because they include the base .NET 10 Runtime. Installing only .NET 9 or .NET 11 does not satisfy the requirement under the default runtime roll-forward policy.

Installing the MSIX package directly does not automatically install the .NET runtime prerequisite. It must be installed separately, although package managers such as WinGet can handle this dependency.

#### Foreground activation and shortcuts
For reliable foreground activation, use PowerToys **0.102.2933.0 or newer**. Its CmdPal host renews foreground permission before each extension command ([PowerToys #50651](https://github.com/microsoft/PowerToys/pull/50651)). Updating the extension alone cannot provide this fix on older hosts.

The optional **Ctrl+Enter opens the containing folder** setting changes these shortcuts after you click Save. Existing search results refresh automatically:

| Action | Default | Setting enabled |
| --- | --- | --- |
| Open containing folder in Explorer | Ctrl+Shift+E | Ctrl+Enter |
| Browse in Everything | Ctrl+Enter | Ctrl+Shift+Enter |
| Run as administrator | Ctrl+Shift+Enter | Ctrl+Shift+R |
| Open with | Shift+Enter | Shift+Enter |

The setting is off by default. CmdPal assigns Ctrl+Enter to the first secondary action, so enabling this setting also moves Open containing folder ahead of Browse in the menu. All actions remain available in the context menu. New setting text uses the English resource fallback until translations are added.

#### Plugin Features
For an explanation of all the features and settings of this plugin, please visit the [features page](https://github.com/lin-ycv/EverythingCommandPalette/wiki/Features)

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/linycv)

## Support and Feedback

You can report bugs via [GitHub Issues](https://github.com/lin-ycv/EverythingCommandPalette/issues). Alternatively, if you're familiar with coding, you can address the bug yourself and submit a [Pull Request](https://github.com/lin-ycv/EverythingCommandPalette/pulls) with the fix.

For support or feedback, please utilize [GitHub Discussions](https://github.com/lin-ycv/EverythingCommandPalette/discussions).

## Developers and Contributors

If you want to contribute to this project, visit the [developer page](https://github.com/lin-ycv/CommandPalette/wiki/Developer) to get started.

[![Star History Chart](https://api.star-history.com/chart?repos=lin-ycv/everythingcommandpalette&type=date&legend=bottom-right)](https://www.star-history.com/?repos=lin-ycv%2Feverythingcommandpalette&type=date&legend=bottom-right)
