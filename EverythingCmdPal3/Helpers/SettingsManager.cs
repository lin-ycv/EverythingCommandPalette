using EverythingCmdPal3.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using static EverythingCmdPal3.Interop.NativeMethods;

namespace EverythingCmdPal3.Helpers
{
    public class SettingsManager: JsonSettingsManager
    {
        private static readonly string _namespace = "everything3";
        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        private readonly object _settingsLock = new();
        private SearchSettings _current = null!;
        private string _committedJson = "{}";
        internal SearchSettings Current => Volatile.Read(ref _current);
        internal event EventHandler? Changed;
        private static readonly List<ChoiceSetSetting.Choice> _sort = [.. Enum.GetValues<Property>().Take(12)
            .Select(static e => new ChoiceSetSetting.Choice(e.ToString(), ((int)e).ToString(CultureInfo.InvariantCulture)))];
        private static readonly List<ChoiceSetSetting.Choice> _secondarySort = [new("None", "-1"), .. _sort];

        public string Instance => _instance.Value ?? string.Empty; // have default value,
        private readonly TextSetting _instance = new(
            Namespaced(nameof(Instance)),
            Resources.instance,
            Resources.instance_description,
            string.Empty);

        public int SortOption => ParseSort(_sortOption.Value, 5); // have default value
        private readonly ChoiceSetSetting _sortOption = new(
            Namespaced(nameof(SortOption)),
            Resources.sort,
            Resources.sort_description,
            _sort)
        { Value = "5" }; // Set Default to Date_Modified

        public bool SortAscending => _sortAscending.Value;
        private readonly BooleanSetting _sortAscending = new(
            Namespaced(nameof(SortAscending)),
            Resources.sort_ascending,
            Resources.sort_ascending_description,
            false);

        public int SortOption2 => ParseSort(_sortOption2.Value, -1, allowNone: true);
        private readonly ChoiceSetSetting _sortOption2 = new(
            Namespaced(nameof(SortOption2)),
            "Secondary sort",
            "None is fastest. Adding a second sort key can make broad searches much slower.",
            _secondarySort)
        { Value = "-1" };

        public bool SortAscending2 => _sortAscending2.Value;
        private readonly BooleanSetting _sortAscending2 = new(
            Namespaced(nameof(SortAscending2)),
            Resources.sort_ascending,
            Resources.sort_ascending_description,
            false);

        public int Max => int.TryParse(_max.Value, CultureInfo.InvariantCulture, out int max) && max > 0 ? Math.Min(max, 1000) : 10; // have default value, int value checked during save
        private readonly TextSetting _max = new(
            Namespaced(nameof(Max)),
            Resources.max,
            Resources.max_description,
            "10");

        public string Prefix => _prefix.Value ?? string.Empty; // have default value
        private readonly TextSetting _prefix = new(
            Namespaced(nameof(Prefix)),
            Resources.prefix,
            Resources.prefix_description,
            string.Empty);

        public bool Match => _match.Value;
        private readonly BooleanSetting _match = new(
            Namespaced(nameof(Match)),
            Resources.match_path,
            Resources.match_path_description,
            false);

        public bool Regex => _regex.Value;
        private readonly BooleanSetting _regex = new(
            Namespaced(nameof(Regex)),
            Resources.regex,
            Resources.regex_description,
            false);

        public bool RunAs => _runas.Value;
        private readonly BooleanSetting _runas = new(
            Namespaced(nameof(RunAs)),
            Resources.run_as,
            Resources.run_as_description,
            true);

        public bool ShowMore => _showMore.Value;
        private readonly BooleanSetting _showMore = new(
            Namespaced(nameof(ShowMore)),
            Resources.show_more,
            Resources.show_more_description,
            true);

        public string Exe => _exe.Value ?? string.Empty; // have default value
        private readonly TextSetting _exe = new(
            Namespaced(nameof(Exe)),
            Resources.exe,
            Resources.exe_description,
            FindEverythingExecutablePath());

        public bool EnableSend => _bSendto.Value;
        private readonly BooleanSetting _bSendto = new(
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

        private readonly BooleanSetting _detailPane = new(
            Namespaced(nameof(DetailPane)), "Details pane", "Show size and modified date for search results.", true);
        private readonly BooleanSetting _ctrlEnterOpenFolder = new(
            Namespaced(nameof(CtrlEnterOpenFolder)), "Ctrl+Enter opens the containing folder",
            "Move Open containing folder to the first secondary action. Browse uses Ctrl+Shift+Enter.", false);
        private readonly BooleanSetting _enableDelete = new(
            Namespaced(nameof(EnableDelete)), "Enable delete shortcut", "Allow Ctrl+Delete to move a result to the Recycle Bin.", true);
        public bool DetailPane => _detailPane.Value;
        public bool CtrlEnterOpenFolder => _ctrlEnterOpenFolder.Value;
        public bool EnableDelete => _enableDelete.Value;

        private static int ParseSort(string? value, int fallback, bool allowNone = false) =>
            int.TryParse(value, CultureInfo.InvariantCulture, out int sort) &&
            ((sort is >= 0 and < 12) || (allowNone && sort == -1)) ? sort : fallback;

        internal static string SettingsJsonPath()
        {
            string dir = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "everything3.json");
        }
        public SettingsManager() : this(SettingsJsonPath())
        {
        }

        internal SettingsManager(string filePath)
        {
            FilePath = filePath;

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
            Settings.Add(_detailPane);
            Settings.Add(_ctrlEnterOpenFolder);
            Settings.Add(_enableDelete);

            // Load settings from file upon initialization
            LoadSettings();

            Settings.SettingsChanged += (s, a) => SaveSettings();
        }
        
        public override void LoadSettings()
        {
            lock (_settingsLock)
            {
                if (File.Exists(FilePath))
                {
                    try { Settings.Update(SettingsStorage.Normalize(File.ReadAllText(FilePath))); }
                    catch (Exception ex) { ExtensionHost.LogMessage($"ECP3: Cannot load settings: {ex.Message}"); }
                }
                _committedJson = Settings.ToJson();
                Volatile.Write(ref _current, Capture());
            }
        }

        public override void SaveSettings()
        {
            lock (_settingsLock)
            {
                try
                {
                    _max.Value = Max.ToString(CultureInfo.InvariantCulture);
                    _sortOption.Value = SortOption.ToString(CultureInfo.InvariantCulture);
                    _sortOption2.Value = SortOption2.ToString(CultureInfo.InvariantCulture);
                    if (EnableSend && !SendToTemplate.TryParse(Sendto, out _, out _))
                        throw new FormatException("Send to requires an executable,arguments template.");
                    var next = Capture();
                    string json = Settings.ToJson();
                    SettingsStorage.Save(FilePath, json);
                    _committedJson = json;
                    Volatile.Write(ref _current, next);
                }
                catch
                {
                    Settings.Update(_committedJson);
                    throw;
                }
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private SearchSettings Capture() => new(Instance, SortOption, SortAscending,
            SortOption2, SortAscending2, Max, Prefix, Match, Regex, RunAs, ShowMore,
            Exe, EnableSend, Sendto, DetailPane, CtrlEnterOpenFolder, EnableDelete);

        private static string FindEverythingExecutablePath()
        {
            foreach (string root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
                foreach (string directory in new[] { "Everything", "Everything 1.5", "Everything 1.5a" })
                    foreach (string name in new[] { "Everything.exe", "Everything64.exe" })
                    {
                        string executable = Path.Combine(root, directory, name);
                        if (File.Exists(executable)) return executable;
                    }

            try
            {
                // Check uninstall information in registry for installation locations
                string[] uninstallKeys =
                [
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
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
