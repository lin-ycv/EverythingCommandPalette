using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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

        private readonly BooleanSetting _detailPane = new(
            Namespaced(nameof(DetailPane)),
            Resources.detail_pane,
            Resources.detail_pane_description,
            true);

        private readonly BooleanSetting _match = new(
            Namespaced(nameof(Match)),
            Resources.match_path,
            Resources.match_path_description,
            false);

        private readonly BooleanSetting _regex = new(
            Namespaced(nameof(Regex)),
            Resources.regex,
            Resources.regex_description,
            false);

        private readonly BooleanSetting _runas = new(
            Namespaced(nameof(RunAs)),
            Resources.run_as,
            Resources.run_as_description,
            true);

        private readonly BooleanSetting _showMore = new(
            Namespaced(nameof(ShowMore)),
            Resources.show_more,
            Resources.show_more_description,
            true);

        private readonly BooleanSetting _showResultsWhenEmpty = new(
            Namespaced(nameof(ShowResultsWhenEmpty)),
            Resources.show_empty_results,
            Resources.show_empty_results_description,
            false);

        private readonly BooleanSetting _enableDelete = new(
            Namespaced(nameof(EnableDelete)),
            Resources.enable_delete,
            Resources.enable_delete_description,
            true);

        private readonly BooleanSetting _ctrlEnterOpenFolder = new(
            Namespaced(nameof(CtrlEnterOpenFolder)),
            Resources.ctrl_enter_open_folder,
            Resources.ctrl_enter_open_folder_description,
            false);

        private readonly TextSetting _exe = new(
            Namespaced(nameof(Exe)),
            Resources.exe,
            Resources.exe_description,
            "");

        private readonly BooleanSetting _bSendto = new(
            Namespaced(nameof(EnableSend)),
            Resources.sendto_enable,
            Resources.sendto_enable_description,
            true);

        private readonly TextSetting _sendto = new(
            Namespaced(nameof(Sendto)),
            Resources.sendto,
            Resources.sendto_description,
            "notepad.exe,$P$");

        private readonly TextSetting _filterPath = new(
            Namespaced(nameof(FilterPath)),
            Resources.filter_path,
            Resources.filter_path_description,
            "");

        public int SortOption => int.TryParse(_sortOption.Value, CultureInfo.InvariantCulture, out int sort) && Enum.IsDefined((Sort)sort) ? sort : 14;
        public uint Max => uint.TryParse(_max.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint result) && result > 0 ? result : 10;
        public string Prefix => _prefix.Value ?? string.Empty;
        public bool DetailPane => _detailPane.Value;
        public bool Match => _match.Value;
        public bool Regex => _regex.Value;
        public bool RunAs => _runas.Value;
        public bool ShowMore => _showMore.Value;
        public bool ShowResultsWhenEmpty => _showResultsWhenEmpty.Value;
        public bool EnableDelete => _enableDelete.Value;
        public bool CtrlEnterOpenFolder => _ctrlEnterOpenFolder.Value;
        public string Exe => _exe.Value ?? string.Empty;
        public bool EnableSend => _bSendto.Value;
        public string Sendto => _sendto.Value ?? string.Empty;
        public string FilterPath => _filterPath.Value ?? string.Empty;
        internal readonly bool is1_4;
        private readonly object _settingsLock = new();
        private SearchSettings _current = null!;
        private string _committedJson = "{}";
        internal SearchSettings Current => Volatile.Read(ref _current);
        internal event EventHandler? Changed;

        internal static string SettingsJsonPath()
        {
            var dir = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "everything.json");
        }

        public SettingsManager() : this(SettingsJsonPath(), ApplicationData.Current.LocalFolder.Path,
            FindEverythingExecutablePath(), Everything_GetMinorVersion() < 5)
        {
        }

        // Keep file/form tests independent of packaged Windows and the native DLL.
        internal SettingsManager(string filePath, string filterPath, string executablePath, bool everything14)
        {
            is1_4 = everything14;
            FilePath = filePath;
            _filterPath.Value = filterPath;
            _exe.Value = executablePath;

            Settings.Add(_sortOption);
            Settings.Add(_max);
            Settings.Add(_prefix);
            Settings.Add(_detailPane);
            Settings.Add(_match);
            Settings.Add(_regex);
            Settings.Add(_runas);
            Settings.Add(_showMore);
            Settings.Add(_showResultsWhenEmpty);
            Settings.Add(_enableDelete);
            Settings.Add(_ctrlEnterOpenFolder);
            Settings.Add(_exe);
            Settings.Add(_bSendto);
            Settings.Add(_sendto);
            if (is1_4)
                Settings.Add(_filterPath);

            // Load settings from file upon initialization
            LoadSettings();

            Settings.SettingsChanged += (_, _) => SaveSettings();
        }

        public override void LoadSettings()
        {
            lock (_settingsLock)
            {
                if (File.Exists(FilePath))
                {
                    try { Settings.Update(SettingsStorage.Normalize(File.ReadAllText(FilePath))); }
                    catch (Exception ex) { ExtensionHost.LogMessage($"ECP: Cannot load settings: {ex.Message}"); }
                }
                PublishSettings();
            }
        }

        public override void SaveSettings()
        {
            lock (_settingsLock)
            {
                try
                {
                    // Persist before announcing a new snapshot. No Everything SDK
                    // mutations occur in a settings callback or on the host UI thread.
                    _sortOption.Value = SortOption.ToString(CultureInfo.InvariantCulture);
                    _max.Value = Max.ToString(CultureInfo.InvariantCulture);
                    if (EnableSend && !SendToTemplate.TryParse(Sendto, out _, out _))
                        throw new FormatException("Send to requires an executable,arguments template (for example notepad.exe,$P$).");
                    var next = CaptureSettings();
                    string json = Settings.ToJson();
                    SettingsStorage.Save(FilePath, json);
                    _committedJson = json;
                    Volatile.Write(ref _current, next);
                }
                catch
                {
                    Settings.Update(_committedJson);
                    throw; // Let CmdPal display a save failure instead of pretending it worked.
                }
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void PublishSettings()
        {
            var next = CaptureSettings();
            _committedJson = Settings.ToJson();
            Volatile.Write(ref _current, next);
        }

        private SearchSettings CaptureSettings()
        {
            CheckFilters();
            return new SearchSettings(
                SortOption, Max, Prefix, DetailPane, Match, Regex, RunAs, ShowMore,
                EnableDelete, CtrlEnterOpenFolder, Exe, EnableSend, Sendto, is1_4,
                new Dictionary<string, string>(_filters), ShowResultsWhenEmpty);
        }

        private readonly Dictionary<string, string> _filters = [];
        public IReadOnlyDictionary<string, string> Filters => Current.Filters;

        internal void CheckFilters()
        {
            if (!is1_4)
            {
                _filters.Clear();
                return;
            }
            string filename = "filters.toml";
            string fullpath = Path.Combine(FilterPath, filename);
            if (!File.Exists(fullpath))
                fullpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), $"Assets\\{filename}");

            _filters.Clear();
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
            string a64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a","Everything64.exe"),  
                   s64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything","Everything.exe"),  
                   a32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything 1.5","Everything.exe"),  
                   s32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything","Everything.exe");  

            if (Path.Exists(a64)) return a64;
            if (Path.Exists(s64)) return s64;
            if (Path.Exists(a32)) return a32;
            if (Path.Exists(s32)) return s32;

            try
            {
                // Check uninstall information in registry for installation locations
                string[] uninstallKeys =
                [
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything 1.5a",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything 1.5a"
                ];

                foreach (string subKey in uninstallKeys)
                {
                    using RegistryKey? key = Registry.LocalMachine.OpenSubKey(subKey);
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
                            string? dir = Path.GetDirectoryName(uninstallString.Contains('"') ? uninstallString.Split('"')[1] : uninstallString);
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
