using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HavegetDynamicIsland
{
    public sealed class AppSettings
    {
        private static readonly object Gate = new object();

        public bool StartWithWindows = false;
        public bool ShowSeconds = false;
        public bool Topmost = true;
        public bool AutoCollapse = true;
        public bool NotificationPreview = true;

        // Developer parameters. They stay persisted after the developer
        // password has been accepted once.
        public int IslandOffsetX = 0;
        public int IslandOffsetY = 0;
        public double CompactTimeFontSize = 15.5;
        public bool DeveloperMode = false;
        public string ThemeId = "azure";
        public double CapsuleWidth = 176.0;
        public double CapsuleHeight = 48.0;

        public bool ShowMusic = true;
        public bool ShowAlerts = true;
        public bool ShowStatus = true;
        public bool ShowClipboard = true;
        public bool ShowNotifyCenter = true;
        public bool ShowControl = true;
        public bool ShowCalendar = true;
        public bool ShowTransfer = true;

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                AppPaths.Ensure();
                if (File.Exists(AppPaths.SettingsFile))
                {
                    foreach (string raw in File.ReadAllLines(AppPaths.SettingsFile, Encoding.UTF8))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#")) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim();
                        string value = line.Substring(eq + 1).Trim();
                        if (key == "startWithWindows") s.StartWithWindows = value == "1";
                        else if (key == "showSeconds") s.ShowSeconds = value == "1";
                        else if (key == "topmost") s.Topmost = value != "0";
                        else if (key == "autoCollapse") s.AutoCollapse = value != "0";
                        else if (key == "notificationPreview") s.NotificationPreview = value != "0";
                        else if (key == "islandOffsetX")
                        {
                            int parsed;
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                                s.IslandOffsetX = Math.Max(-600, Math.Min(600, parsed));
                        }
                        else if (key == "islandOffsetY")
                        {
                            int parsed;
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                                s.IslandOffsetY = Math.Max(-300, Math.Min(600, parsed));
                        }
                        else if (key == "compactTimeFontSize")
                        {
                            double parsed;
                            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                                s.CompactTimeFontSize = Math.Max(10.0, Math.Min(26.0, parsed));
                        }
                        else if (key == "developerMode") s.DeveloperMode = value == "1";
                        else if (key == "theme") s.ThemeId = value;
                        else if (key == "capsuleWidth")
                        {
                            double parsed;
                            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                                s.CapsuleWidth = Math.Max(140.0, Math.Min(320.0, parsed));
                        }
                        else if (key == "capsuleHeight")
                        {
                            double parsed;
                            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                                s.CapsuleHeight = Math.Max(36.0, Math.Min(72.0, parsed));
                        }
                        else if (key == "showMusic") s.ShowMusic = value != "0";
                        else if (key == "showAlerts") s.ShowAlerts = value != "0";
                        else if (key == "showStatus") s.ShowStatus = value != "0";
                        else if (key == "showClipboard") s.ShowClipboard = value != "0";
                        else if (key == "showNotifyCenter") s.ShowNotifyCenter = value != "0";
                        else if (key == "showControl") s.ShowControl = value != "0";
                        else if (key == "showCalendar") s.ShowCalendar = value != "0";
                        else if (key == "showTransfer") s.ShowTransfer = value != "0";
                    }
                }
                s.StartWithWindows = Startup.IsEnabled;
            }
            catch (Exception ex) { Log.Error("Settings.Load", ex); }
            return s;
        }

        public void Save()
        {
            lock (Gate)
            {
                try
                {
                    AppPaths.Ensure();
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# HAVEGET DynamicIsland for Windows");
                    sb.AppendLine("startWithWindows=" + (StartWithWindows ? "1" : "0"));
                    sb.AppendLine("showSeconds=" + (ShowSeconds ? "1" : "0"));
                    sb.AppendLine("topmost=" + (Topmost ? "1" : "0"));
                    sb.AppendLine("autoCollapse=" + (AutoCollapse ? "1" : "0"));
                    sb.AppendLine("notificationPreview=" + (NotificationPreview ? "1" : "0"));
                    sb.AppendLine("islandOffsetX=" + IslandOffsetX.ToString(CultureInfo.InvariantCulture));
                    sb.AppendLine("islandOffsetY=" + IslandOffsetY.ToString(CultureInfo.InvariantCulture));
                    sb.AppendLine("compactTimeFontSize=" + CompactTimeFontSize.ToString("0.#", CultureInfo.InvariantCulture));
                    sb.AppendLine("developerMode=" + (DeveloperMode ? "1" : "0"));
                    sb.AppendLine("theme=" + ThemeId);
                    sb.AppendLine("capsuleWidth=" + CapsuleWidth.ToString("0.#", CultureInfo.InvariantCulture));
                    sb.AppendLine("capsuleHeight=" + CapsuleHeight.ToString("0.#", CultureInfo.InvariantCulture));
                    sb.AppendLine("showMusic=" + (ShowMusic ? "1" : "0"));
                    sb.AppendLine("showAlerts=" + (ShowAlerts ? "1" : "0"));
                    sb.AppendLine("showStatus=" + (ShowStatus ? "1" : "0"));
                    sb.AppendLine("showClipboard=" + (ShowClipboard ? "1" : "0"));
                    sb.AppendLine("showNotifyCenter=" + (ShowNotifyCenter ? "1" : "0"));
                    sb.AppendLine("showControl=" + (ShowControl ? "1" : "0"));
                    sb.AppendLine("showCalendar=" + (ShowCalendar ? "1" : "0"));
                    sb.AppendLine("showTransfer=" + (ShowTransfer ? "1" : "0"));
                    string temp = AppPaths.SettingsFile + ".tmp";
                    File.WriteAllText(temp, sb.ToString(), new UTF8Encoding(false));
                    if (File.Exists(AppPaths.SettingsFile)) File.Delete(AppPaths.SettingsFile);
                    File.Move(temp, AppPaths.SettingsFile);
                }
                catch (Exception ex) { Log.Error("Settings.Save", ex); }
            }
        }
    }
}
