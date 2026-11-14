using EverythingCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.System;

namespace EverythingCmdPal.Commands
{
    internal sealed class CommandHandler
    {
        // Extensions for adding run as admin context menu item for applications
        private readonly string[] _appExtensions = [".exe", ".bat", ".appref-ms", ".lnk"];
        internal CommandContextItem[] LoadCommands(string fullPath, bool isFolder, ResultsPage page, SearchSettings settings)
        {
            List<CommandContextItem> items = [];
            bool ctrlEnterOpenFolder = settings.CtrlEnterOpenFolder;

            string p = $"\"{(isFolder ? fullPath : Path.GetDirectoryName(fullPath))}\\\" ";
            var browse = new CommandContextItem(new BrowseCommand(p, page))
            {
                RequestedShortcut = ctrlEnterOpenFolder
                    ? KeyChordHelpers.FromModifiers(ctrl: true, shift: true, vkey: (int)VirtualKey.Enter)
                    : default,
            };
            var openFolder = new CommandContextItem(new OpenFolderCommand(fullPath))
            {
                RequestedShortcut = ctrlEnterOpenFolder
                    ? default
                    : KeyChordHelpers.FromModifiers(ctrl: true, shift: true, vkey: (int)VirtualKey.E),
            };

            // Ctrl+Enter belongs to CmdPal's first visible secondary action.
            // Requesting it explicitly is ignored by the host (PowerToys#50843).
            if (ctrlEnterOpenFolder)
            {
                items.Add(openFolder);
                items.Add(browse);
            }
            else
            {
                items.Add(browse);
                items.Add(openFolder);
            }

            // Shift+Enter remains available for Open with in both shortcut modes.
            if (!isFolder)
                items.Add(new CommandContextItem(new OpenWithCommand(fullPath))
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(false, false, true, false, (int)VirtualKey.Enter, 0),
                });

            if (settings.EnableSend && SendToTemplate.TryParse(settings.Sendto, out _, out _))
                items.Add(new CommandContextItem(new SendCommand(fullPath, settings.Sendto))
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, false, false, (int)VirtualKey.N, 0),
                });
            if (CanFileBeRunAsAdmin(fullPath))
            {
                items.Add(new CommandContextItem(new RunAdminCommand(fullPath, isFolder))
                {
                    // Ctrl+Shift+Enter belongs to Browse in the alternate mode.
                    RequestedShortcut = KeyChordHelpers.FromModifiers(
                        ctrl: true, shift: true,
                        vkey: (int)(ctrlEnterOpenFolder ? VirtualKey.R : VirtualKey.Enter))
                });
                if (settings.RunAs)
                    items.Add(new CommandContextItem(new RunAsCommand(fullPath, isFolder))
                    {
                        RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, true, false, (int)VirtualKey.U, 0)
                    });
            }

            items.AddRange((CommandContextItem[])[
                // Working using win32Helper
                new CommandContextItem(new CopyCommand(fullPath, isFolder)) {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,false,false, (int)VirtualKey.C, 0)
                },
                new CommandContextItem(new CopyPathCommand(fullPath)) {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,true,false, (int)VirtualKey.C, 0)
                },
                new CommandContextItem(new OpenConsoleCommand(fullPath, isFolder)) {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,true,false, (int)VirtualKey.T, 0)
                },
                // Not working in cmdPal
                //new CommandContextItem(new ContextMenuCommand(fullPath, isFolder)) {
                //    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,false,false, (int)VirtualKey.M, 0)
                //},
                new CommandContextItem(new DeleteCommand(fullPath, isFolder)) {
                    RequestedShortcut = settings.EnableDelete
                        ? KeyChordHelpers.FromModifiers(true,false,false,false, (int)VirtualKey.Delete, 0)
                        : default,
                },
                new CommandContextItem(new PropertiesCommand(fullPath)) {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,false,false, (int)VirtualKey.I, 0)
                },
                ]);

            return [.. items];
        }

        private bool CanFileBeRunAsAdmin(string path)
        {
            string fileExtension = Path.GetExtension(path);
            foreach (string extension in _appExtensions)
            {
                // Using OrdinalIgnoreCase since this is internal
                if (extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
