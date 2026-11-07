using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace HavegetDynamicIsland
{
    public static class AppPaths
    {
        public static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"HAVEGET\DynamicIsland");
        public static readonly string SettingsFile = Path.Combine(DataDir, "settings.ini");
        public static readonly string LogFile = Path.Combine(DataDir, "dynamic-island.log");

        public static void Ensure()
        {
            try { Directory.CreateDirectory(DataDir); } catch { }
        }

        public static string ExecutablePath
        {
            get
            {
                try { return Assembly.GetEntryAssembly().Location; }
                catch { return Process.GetCurrentProcess().MainModule.FileName; }
            }
        }
    }

    public static class Log
    {
        private static readonly object Gate = new object();

        public static void Write(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine;
            try
            {
                AppPaths.Ensure();
                lock (Gate) { File.AppendAllText(AppPaths.LogFile, line, Encoding.UTF8); }
                return;
            }
            catch { }
            try
            {
                lock (Gate) { File.AppendAllText(Path.Combine(Path.GetTempPath(), "HAVEGET-DynamicIsland.log"), line, Encoding.UTF8); }
            }
            catch { }
        }

        public static void Error(string where, Exception ex)
        {
            if (ex == null) return;
            Write("ERROR [" + where + "] " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    public static class Startup
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "HAVEGETDynamicIsland";

        public static bool IsEnabled
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    {
                        if (key == null) return false;
                        return key.GetValue(ValueName) != null;
                    }
                }
                catch { return false; }
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (key == null) return;
                    if (enabled)
                        key.SetValue(ValueName, "\"" + AppPaths.ExecutablePath + "\" --startup", RegistryValueKind.String);
                    else
                        key.DeleteValue(ValueName, false);
                }
            }
            catch (Exception ex) { Log.Error("Startup.SetEnabled", ex); }
        }
    }

    public sealed class BatteryInfo
    {
        public int Percent;
        public bool HasBattery;
        public bool Charging;

        public static BatteryInfo Read()
        {
            BatteryInfo info = new BatteryInfo();
            try
            {
                System.Windows.Forms.PowerStatus p = System.Windows.Forms.SystemInformation.PowerStatus;
                float percent = p.BatteryLifePercent;
                info.HasBattery = percent >= 0 && percent <= 1 &&
                    p.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.NoSystemBattery;
                info.Percent = info.HasBattery ? (int)Math.Round(percent * 100) : 100;
                info.Charging = p.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online ||
                                (p.BatteryChargeStatus & System.Windows.Forms.BatteryChargeStatus.Charging) != 0;
            }
            catch
            {
                info.HasBattery = false;
                info.Percent = 100;
                info.Charging = true;
            }
            return info;
        }
    }

    public static class TextUtil
    {
        public static string FormatBytes(ulong bytes)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int i = 0;
            while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
            if (i == 0) return ((long)value) + " " + units[i];
            return value.ToString(value >= 10 ? "0" : "0.0") + " " + units[i];
        }

        public static string FormatUptime(ulong milliseconds)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);
            if (t.TotalDays >= 1) return ((int)t.TotalDays) + " 天 " + t.Hours + " 小时";
            if (t.TotalHours >= 1) return ((int)t.TotalHours) + " 小时 " + t.Minutes + " 分";
            return Math.Max(1, t.Minutes) + " 分钟";
        }
    }
}
