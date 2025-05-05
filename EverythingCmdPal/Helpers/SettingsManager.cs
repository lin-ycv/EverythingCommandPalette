using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.Storage;
using static EverythingCmdPal.Interop.NativeMethods;

namespace EverythingCmdPal.Helpers
{
    public class SettingsManager : JsonSettingsManager
    {
        private static readonly string _namespace = "everything";
        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        private static readonly List<ChoiceSetSetting.Choice> _sort = [.. Enum.GetValues<Sort>()
            .Cast<Sort>()
            .Select(static e => new ChoiceSetSetting.Choice(e.ToString(), ((int)e).ToString(CultureInfo.InvariantCulture)))];

        private readonly ChoiceSetSetting _sortOption = new(
            Namespaced(nameof(SortOption)),
            Resources.sort,
            Resources.sort_description,
            _sort)
        { Value = "14" }; // Set Default to Date_Modified_Descending

        private readonly TextSetting _max = new(
            Namespaced(nameof(Max)),
            Resources.max,
            Resources.max_description,
            "10");

        private readonly TextSetting _prefix = new(
            Namespaced(nameof(Prefix)),
            Resources.prefix,
            Resources.prefix_description,
            "");

        private readonly ToggleSetting _match = new(
            Namespaced(nameof(Match)),
            Resources.match_path,
            Resources.match_path_description,
            false);

        private readonly ToggleSetting _regex = new(
            Namespaced(nameof(Regex)),
            Resources.regex,
            Resources.regex_description,
            false);

        private readonly ToggleSetting _runas = new(
            Namespaced(nameof(RunAs)),
            Resources.run_as,
            Resources.run_as_description,
            true);

        private readonly ToggleSetting _showMore = new(
            Namespaced(nameof(ShowMore)),
            Resources.show_more,
            Resources.show_more_description,
            true);

        private readonly TextSetting _exe = new(
            Namespaced(nameof(Exe)),
            Resources.exe,
            Resources.exe_description,
            FindEverythingExecutablePath());

        private readonly ToggleSetting _bSendto = new(
            Namespaced(nameof(EnableSend)),
            Resources.sendto_enable,
            Resources.sendto_enable_description,
            true);

        private readonly TextSetting _sendto = new(
            Namespaced(nameof(Sendto)),
            Resources.sendto,
            Resources.sendto_description,
            "notepad.exe,$P$");

        public int SortOption => int.TryParse(_sortOption.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 14;
        public uint Max => uint.TryParse(_max.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint result) ? result : 10;
        public string Prefix => _prefix.Value ?? string.Empty;
        public bool Match => _match.Value;
        public bool Regex => _regex.Value;
        public bool RunAs => _runas.Value;
        public bool ShowMore => _showMore.Value;
        public string Exe => _exe.Value ?? string.Empty;
        public bool EnableSend => _bSendto.Value;
        public string Sendto => _sendto.Value ?? string.Empty;
        internal bool is1_4;

        internal static string SettingsJsonPath()
        {
            var dir = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "everything.json");
        }

        public SettingsManager()
        {
            is1_4 = Everything_GetMinorVersion() < 5;
            FilePath = SettingsJsonPath();

            Settings.Add(_sortOption);
            Settings.Add(_max);
            Settings.Add(_prefix);
            Settings.Add(_match);
            Settings.Add(_regex);
            Settings.Add(_runas);
            Settings.Add(_showMore);
            Settings.Add(_exe);
            Settings.Add(_bSendto);
            Settings.Add(_sendto);

            // Load settings from file upon initialization
            LoadSettings();

            Settings.SettingsChanged += (s, a) => SaveSettings();
            Everything_SetRequestFlags(Request.FILE_NAME | Request.PATH | Request.EXTENSION | Request.DATE_MODIFIED | Request.SIZE);
            Everything_SetSort((Sort)SortOption);
            Everything_SetMax(Max);
            Everything_SetMatchPath(Match);
            Everything_SetRegex(Regex);

            if (is1_4)
                CheckFilters();
        }

        public override void SaveSettings()
        {
            Everything_SetSort((Sort)SortOption);
            Everything_SetMax(Max);
            Everything_SetMatchPath(Match);
            Everything_SetRegex(Regex);
            if (is1_4)
                CheckFilters();
            base.SaveSettings();
        }

        private readonly Dictionary<string, string> _filters = [];
        public Dictionary<string, string> Filters => _filters;

        internal void CheckFilters()
        {
            string filename = "filters.toml";
            string fullpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, filename);
            if (!File.Exists(fullpath))
                fullpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), $"Assets\\{filename}");

            string[] strArr;
            try
            {
                strArr = File.ReadAllLines(fullpath);
            }
            catch (Exception e)
            {
                ExtensionHost.LogMessage($"Error reading filters: {e.Message}");
                return;
            }

            _filters.Clear();
            foreach (string str in strArr)
            {
                if (str.Length == 0 || str[0] == '#') continue;
                string[] kv = str.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (kv.Length != 2) continue;

                if (kv[0].Contains(':'))
                    _filters.TryAdd(kv[0].ToLowerInvariant(), kv[1] + (kv[1].EndsWith(';') ? ' ' : string.Empty));
            }

        }
        private static string FindEverythingExecutablePath()
        {
            try
            {
                // First try to find Everything through file association in registry
                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(@"Everything.FileList\shell\open\command"))
                {
                    string? command = key?.GetValue("") as string;
                    if (!string.IsNullOrEmpty(command))
                    {
                        string exe = command.StartsWith('\"') && command.IndexOf('"', 1) > 1
                            ? command[1..command.IndexOf('"', 1)]
                            : command.Split(' ')[0];
                        if (Path.Exists(exe) && exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            return exe;
                    }
                }

                // Create a list of common installation directories as previous defined
                List<string> everythingPaths =
                [
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything 1.5a")
                ];

                // Check uninstall information in registry for installation locations
                string[] uninstallKeys =
                [
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything-1.5a",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything-1.5a"
                ];

                foreach (string subKey in uninstallKeys)
                {
                    // Select appropriate registry view based on OS architecture
                    RegistryView baseView = Environment.Is64BitOperatingSystem
                        ? RegistryView.Registry64
                        : RegistryView.Registry32;
                    using RegistryKey? localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, baseView);
                    using RegistryKey? key = localMachine?.OpenSubKey(subKey);
                    if (key != null)
                    {
                        // Check install location from registry
                        if (key.GetValue("InstallLocation") is string installLocation && Path.Exists(installLocation))
                            everythingPaths.Add(installLocation);
                        // Try to extract location from uninstall string
                        if (key.GetValue("UninstallString") is string uninstallString)
                        {
                            string? dir = Path.GetDirectoryName(uninstallString.Trim().Trim('"').Split(' ')[0]);
                            if (dir is not null)
                                everythingPaths.Add(dir);
                        }
                    }
                }

                // Check all collected paths for executable files
                foreach (string path in everythingPaths.Distinct())
                {
                    // Check for both 32-bit and 64-bit versions
                    string exe = Path.Combine(path, "Everything.exe");
                    string exe64 = Path.Combine(path, "Everything64.exe");
                    if (Path.Exists(exe))
                        return exe;
                    if (Path.Exists(exe64))
                        return exe64;
                }
                // No executable found
                return string.Empty;
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage($"Error finding Everything executable: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
