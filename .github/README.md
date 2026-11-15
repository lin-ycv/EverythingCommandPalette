# Everything SDK3 for Command Palette

This is the SDK3 variant of [EverythingCommandPalette](https://github.com/lin-ycv/EverythingCommandPalette). It searches Everything **1.5** through named pipes. Everything 1.4 requires the original SDK2 extension.

The existing design is retained, with a background search service, bounded result requests, cancellation of obsolete queries, and lazy icons. 

## Requirements

- Windows 10 build 19041 or newer; x64 or ARM64.
- A recent Everything 1.5 installation with its SDK3 IPC server available. The default instance is blank, selecting the unnamed beta/release database. Use `1.5a` for older alpha installations, or your custom name. The selected instance is respected exactly.
- PowerToys Command Palette compatible with extension SDK `0.12.260812002`. Use PowerToys `0.102.2933.0` or newer for reliable foreground activation of file actions.
- For development: .NET 10 SDK and the Windows SDK/MSIX build tools. The optional native integration checks also need Visual Studio C++ x64 build tools.

## Build and package

From the repository root:

```powershell
dotnet build EverythingCmdPal3/EverythingCmdPal3.csproj -p:Platform=x64
dotnet build EverythingCmdPal3/EverythingCmdPal3.csproj -p:Platform=ARM64

# Unsigned local test package; sign before installing or distributing.
dotnet build EverythingCmdPal3/EverythingCmdPal3.csproj -c Release -p:Platform=x64 -p:AppxPackageSigningEnabled=false
```

Packages appear in `EverythingCmdPal3/AppPackages`. Release builds trim the extension. The STA file-action helper is built from `Helper/` automatically and included under `Win32/`, including its runtime; prebuilt files in the source `EverythingCmdPal3/Win32/` folder are not used. Each architecture includes its matching vendor SDK3 DLL.
