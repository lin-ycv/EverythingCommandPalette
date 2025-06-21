using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static EverythingCmdPal3.Interop.NativeMethods;

namespace EverythingCmdPal3.Helpers
{
    public class SettingsManager: JsonSettingsManager
    {
        private static readonly string _namespace = "everything3";
        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        internal bool updatedneeded = true;
        private static readonly List<ChoiceSetSetting.Choice> _sort = [.. Enum.GetValues<Property>().Take(12)
            .Select(static e => new ChoiceSetSetting.Choice(e.ToString(), ((int)e).ToString(CultureInfo.InvariantCulture)))];

        public string Instance => _instance.Value; // have default value,
        private readonly TextSetting _instance = new(
            Namespaced(nameof(Instance)),
            Resources.instance,
            Resources.instance_description,
            "1.5a");

        public int SortOption => int.Parse(_sortOption.Value, CultureInfo.InvariantCulture); // have default value
        private readonly ChoiceSetSetting _sortOption = new(
            Namespaced(nameof(SortOption)),
            Resources.sort,
            Resources.sort_description,
            _sort)
        { Value = "5" }; // Set Default to Date_Modified

        public bool SortAscending => _sortAscending.Value;
        private readonly ToggleSetting _sortAscending = new(
            Namespaced(nameof(SortAscending)),
            Resources.sort_ascending,
            Resources.sort_ascending_description,
            false);

        public int SortOption2 => int.Parse(_sortOption2.Value, CultureInfo.InvariantCulture); // have default value
        private readonly ChoiceSetSetting _sortOption2 = new(
            Namespaced(nameof(SortOption2)),
            Resources.sort,
            Resources.sort_description,
            _sort)
        { Value = "10" }; // Set Default to Run_Count

        public bool SortAscending2 => _sortAscending2.Value;
        private readonly ToggleSetting _sortAscending2 = new(
            Namespaced(nameof(SortAscending2)),
            Resources.sort_ascending,
            Resources.sort_ascending_description,
            false);

        public int Max => int.Parse(_max.Value, CultureInfo.InvariantCulture); // have default value, int value checked during save
        private readonly TextSetting _max = new(
            Namespaced(nameof(Max)),
            Resources.max,
            Resources.max_description,
            "10");

        public string Prefix => _prefix.Value; // have default value
        private readonly TextSetting _prefix = new(
            Namespaced(nameof(Prefix)),
            Resources.prefix,
            Resources.prefix_description,
            string.Empty);

        public bool Match => _match.Value;
        private readonly ToggleSetting _match = new(
            Namespaced(nameof(Match)),
            Resources.match_path,
            Resources.match_path_description,
            false);

        public bool Regex => _regex.Value;
        private readonly ToggleSetting _regex = new(
            Namespaced(nameof(Regex)),
            Resources.regex,
            Resources.regex_description,
            false);

        public bool RunAs => _runas.Value;
        private readonly ToggleSetting _runas = new(
            Namespaced(nameof(RunAs)),
            Resources.run_as,
            Resources.run_as_description,
            true);

        public bool ShowMore => _showMore.Value;
        private readonly ToggleSetting _showMore = new(
            Namespaced(nameof(ShowMore)),
            Resources.show_more,
            Resources.show_more_description,
            true);

        public string Exe => _exe.Value; // have default value
        private readonly TextSetting _exe = new(
            Namespaced(nameof(Exe)),
            Resources.exe,
            Resources.exe_description,
            FindEverythingExecutablePath());

        public bool EnableSend => _bSendto.Value;
        private readonly ToggleSetting _bSendto = new(
            Namespaced(nameof(EnableSend)),
            Resources.sendto_enable,
            Resources.sendto_enable_description,
            true);

        public string Sendto => _sendto.Value ?? string.Empty;
        private readonly TextSetting _sendto = new(
            Namespaced(nameof(Sendto)),
            Resources.sendto,
            Resources.sendto_description,
            "notepad.exe,$P$");

        internal static string SettingsJsonPath()
        {
            string dir = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "everything3.json");
        }
        public SettingsManager()
        {
            FilePath = SettingsJsonPath();

            Settings.Add(_instance);
            Settings.Add(_sortOption);
            Settings.Add(_sortAscending);
            Settings.Add(_sortOption2);
            Settings.Add(_sortAscending2);
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
        }
        
        public override void SaveSettings()
        {
            if (!int.TryParse(_max.Value, out int _))
                _max.Value = "10";
            if (_showMore.Value && !Path.Exists(_exe.Value))
                _showMore.Value = false;
            if (_bSendto.Value)
            {
                bool valid =
                    _sendto.Value.Length > 0 &&
                    _sendto.Value.Contains(',') &&
                    _sendto.Value.Contains("$P$");
                if(!valid)
                    _bSendto.Value = false;
            }

            updatedneeded = true;
            base.SaveSettings();
        }

        private static string FindEverythingExecutablePath()
        {
            string a64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything64.exe"),
                   a32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything 1.5", "Everything.exe");

            if (Path.Exists(a64)) return a64;
            if (Path.Exists(a32)) return a32;

            try
            {
                // Check uninstall information in registry for installation locations
                string[] uninstallKeys =
                [
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything 1.5a",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything 1.5a"
                ];

                foreach (string subKey in uninstallKeys)
                {
                    using RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey);
                    if (key != null)
                    {
                        // Check install location from registry
                        if (key.GetValue("InstallLocation") is string installLocation && Path.Exists(installLocation))
                        {
                            string exe = Path.Combine(installLocation, "Everything.exe");
                            string exe64 = Path.Combine(installLocation, "Everything64.exe");

                            if (File.Exists(exe64)) return exe64;
                            if (File.Exists(exe)) return exe;
                        }
                        // Try to extract location from uninstall string
                        if (key.GetValue("UninstallString") is string uninstallString)
                        {
                            string dir = Path.GetDirectoryName(uninstallString.Contains('"') ? uninstallString.Split('"')[1] : uninstallString);
                            if (dir != null)
                            {
                                string exe = Path.Combine(dir, "Everything.exe");
                                string exe64 = Path.Combine(dir, "Everything64.exe");

                                if (File.Exists(exe64)) return exe64;
                                if (File.Exists(exe)) return exe;
                            }
                        }
                    }
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
