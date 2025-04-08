using EverythingCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
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
            Path.Exists("C:\\Program Files\\Everything 1.5a\\Everything64.exe") ? "C:\\Program Files\\Everything 1.5a\\Everything64.exe" :
                    (Path.Exists("C:\\Program Files\\Everything\\Everything.exe") ? "C:\\Program Files\\Everything\\Everything.exe" :
                    (Path.Exists("C:\\Program Files (x86)\\Everything 1.5a\\Everything.exe") ? "C:\\Program Files (x86)\\Everything 1.5a\\Everything.exe" :
                    (Path.Exists("C:\\Program Files (x86)\\Everything\\Everything.exe") ? "C:\\Program Files (x86)\\Everything\\Everything.exe" : string.Empty)))
            );

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

        public int SortOption => int.Parse(_sortOption.Value ?? "14", CultureInfo.InvariantCulture);
        public uint Max => uint.Parse(_max.Value ?? "10", CultureInfo.InvariantCulture);
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
    }
}
