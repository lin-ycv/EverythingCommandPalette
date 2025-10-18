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
        internal CommandContextItem[] LoadCommands(string fullPath, bool isFolder, ResultsPage page)
        {
            List<CommandContextItem> items = [];
            string p = $"\"{(isFolder ? fullPath : Path.GetDirectoryName(fullPath))}\\\" ";
            items.Add(new CommandContextItem(new BrowseCommand(p, page))
            {
                RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, false, false, (int)VirtualKey.Enter, 0),
                
            });

            // works but dialog not topmost (handler/UI Thread)
            if (!isFolder)
                items.Add(new CommandContextItem(new OpenWithCommand(fullPath))
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(false, false, true, false, (int)VirtualKey.Enter, 0),
                });

            if (Query.Settings.EnableSend)
                items.Add(new CommandContextItem(new SendCommand(fullPath, Query.Settings.Sendto))
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, false, false, (int)VirtualKey.N, 0),
                });

            if (CanFileBeRunAsAdmin(fullPath))
            {
                items.Add(new CommandContextItem(new RunAdminCommand(fullPath, isFolder))
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, true, false, (int)VirtualKey.Enter, 0)
                });
                if (Query.Settings.RunAs)
                    items.Add(new CommandContextItem(new RunAsCommand(fullPath, isFolder))
                    {
                        RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, true, false, (int)VirtualKey.U, 0)
                    });
            }

            items.AddRange([
                new CommandContextItem(new OpenFolderCommand(fullPath)){
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,true,false,(int)VirtualKey.E, 0)
                },
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
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true,false,false,false, (int)VirtualKey.Delete, 0)
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
