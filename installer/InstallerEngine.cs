using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace HavegetDynamicIsland.Setup
{
    public sealed class InstallOptions
    {
        public string TargetDir;
        public bool DesktopShortcut = true;
        public bool StartWithWindows = false;
        public bool LaunchAfterInstall = false;

        public static string DefaultTargetDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\HAVEGET DynamicIsland");
            }
        }
    }

    public sealed class RuntimeResult
    {
        public bool Success;
        public bool RebootRequired;
        public string Message;
    }

    public sealed class InstallResult
    {
        public bool Success;
        public bool RebootRequired;
        public string Message;
        public string Warning;
        public string AppPath;
    }

    public static class SetupLog
    {
        private static readonly object Gate = new object();
        public static readonly string LogPath = Path.Combine(Path.GetTempPath(), "HAVEGET-DynamicIsland-setup.log");

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine, new UTF8Encoding(false));
                }
            }
            catch { }
        }

        public static void Error(string where, Exception ex)
        {
            if (ex == null) return;
            Write("ERROR [" + where + "] " + ex.GetType().FullName + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    public static class InstallerEngine
    {
        public const string ProductName = "HAVEGET DynamicIsland";
        public const string ProductNameEn = "HAVEGET DynamicIsland";
        public const string Version = "1.2.6";
        public const string AppExeName = "HAVEGET DynamicIsland.exe";
        public const string UninstallExeName = "Uninstall.exe";
        public const string UninstallKeyName = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\HAVEGETDynamicIsland";
        public const string RunKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string RunValueName = "HAVEGETDynamicIsland";
        public const int MinNetRelease = 528040; // .NET Framework 4.8
        public const int Net48Release = 528040;
        public const string DotNet48Url = "https://go.microsoft.com/fwlink/?LinkId=2085155";

        public static string CurrentExePath
        {
            get
            {
                try { return Assembly.GetEntryAssembly().Location; }
                catch { return Process.GetCurrentProcess().MainModule.FileName; }
            }
        }

        public static bool IsSupportedWindows()
        {
            Version v = Environment.OSVersion.Version;
            return v.Major >= 10;
        }

        public static string WindowsName()
        {
            return "Windows " + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor;
        }

        public static int GetNetRelease()
        {
            int found = 0;
            try
            {
                RegistryView[] views = new RegistryView[] { RegistryView.Registry64, RegistryView.Registry32 };
                foreach (RegistryView view in views)
                {
                    try
                    {
                        using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                        using (RegistryKey key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", false))
                        {
                            if (key == null) continue;
                            object value = key.GetValue("Release", null);
                            if (value == null) continue;
                            int release = Convert.ToInt32(value);
                            if (release > found) found = release;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return found;
        }

        public static string RuntimeLabel(int release)
        {
            if (release >= 533320) return ".NET Framework 4.8.1";
            if (release >= Net48Release) return ".NET Framework 4.8";
            if (release >= 461808) return ".NET Framework 4.7.2";
            if (release >= 461308) return ".NET Framework 4.7.1";
            if (release >= 460798) return ".NET Framework 4.7";
            if (release >= 394802) return ".NET Framework 4.6.2";
            return "未检测到 .NET Framework 4.6.2+";
        }

        public static bool IsRuntimeReady
        {
            get { return GetNetRelease() >= MinNetRelease; }
        }

        public static RuntimeResult EnsureDotNet(Action<double, string> progress)
        {
            RuntimeResult result = new RuntimeResult();
            int release = GetNetRelease();
            if (release >= MinNetRelease)
            {
                result.Success = true;
                result.Message = RuntimeLabel(release);
                if (progress != null) progress(1.0, result.Message + " 已就绪");
                return result;
            }

            string downloadDir = Path.Combine(Path.GetTempPath(), "HAVEGETDynamicIslandSetup");
            string installerPath = Path.Combine(downloadDir, "ndp48-web.exe");
            try
            {
                Directory.CreateDirectory(downloadDir);
                if (File.Exists(installerPath) && new FileInfo(installerPath).Length < 1024 * 1024)
                    File.Delete(installerPath);

                if (!File.Exists(installerPath))
                {
                    if (progress != null) progress(0.02, "正在连接 Microsoft 下载 .NET Framework 4.8…");
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                    using (TimedWebClient client = new TimedWebClient())
                    {
                        client.Headers.Add("User-Agent", "HAVEGETDynamicIslandSetup/1.0");
                        client.DownloadProgressChanged += delegate(object sender, System.Net.DownloadProgressChangedEventArgs e)
                        {
                            if (progress != null)
                                progress(0.03 + 0.42 * (e.ProgressPercentage / 100.0),
                                    "正在下载 .NET Framework 4.8 · " + e.ProgressPercentage + "%");
                        };
                        client.DownloadFile(DotNet48Url, installerPath);
                    }
                }

                if (progress != null) progress(0.48, "正在验证 Microsoft 数字签名…");
                VerifyMicrosoftSignature(installerPath);

                if (progress != null) progress(0.52, "正在安装 .NET Framework 4.8，如弹出 UAC 请选择“是”…");
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = installerPath;
                psi.Arguments = "/quiet /norestart";
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                psi.WindowStyle = ProcessWindowStyle.Normal;
                int exitCode;
                using (Process p = Process.Start(psi))
                {
                    if (p == null) throw new InvalidOperationException("无法启动 .NET Framework 安装程序。");
                    p.WaitForExit(30 * 60 * 1000);
                    exitCode = p.HasExited ? p.ExitCode : -1;
                }
                SetupLog.Write("dotnet installer exit=" + exitCode);
                if (exitCode == 1223) // ERROR_CANCELLED from UAC
                {
                    result.Success = false;
                    result.Message = "已取消管理员授权，运行环境尚未安装。可以重试，或手动安装 .NET Framework 4.8。";
                    return result;
                }
                if (exitCode == 1602)
                {
                    result.Success = false;
                    result.Message = "Microsoft 安装程序已取消。请重试。";
                    return result;
                }
                bool reboot = exitCode == 3010 || exitCode == 1641;
                if (exitCode != 0 && !reboot && exitCode != 1)
                {
                    result.Success = false;
                    result.Message = "运行环境安装返回代码 " + exitCode + "。请手动安装 .NET Framework 4.8。";
                    return result;
                }
                if (progress != null) progress(0.96, "正在确认运行环境…");
                int now = GetNetRelease();
                if (now < MinNetRelease && !reboot)
                {
                    result.Success = false;
                    result.Message = "安装程序已结束，但仍未检测到可用的 .NET Framework。请重启电脑后重试，或从 Microsoft 官网下载安装。";
                    return result;
                }
                result.Success = true;
                result.RebootRequired = reboot || now < MinNetRelease;
                result.Message = reboot ? "运行环境已安装，需要重启电脑后生效。" : "运行环境已就绪。";
                if (progress != null) progress(1.0, result.Message);
                return result;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                SetupLog.Error("EnsureDotNet.UAC", ex);
                result.Success = false;
                result.Message = ex.NativeErrorCode == 1223
                    ? "已取消管理员授权，无法自动安装 .NET Framework 4.8。"
                    : "启动 Microsoft 安装程序失败：" + ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                SetupLog.Error("EnsureDotNet", ex);
                result.Success = false;
                result.Message = "运行环境自动补全失败：" + ex.Message;
                return result;
            }
        }

        private static void VerifyMicrosoftSignature(string path)
        {
            X509Certificate cert = X509Certificate.CreateFromSignedFile(path);
            if (cert == null) throw new InvalidOperationException("下载文件没有数字签名。");
            string subject = cert.Subject ?? "";
            if (subject.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("下载文件的数字签名不是 Microsoft，已停止安装。");
            SetupLog.Write("dotnet signature: " + subject);
        }

        private sealed class TimedWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                request.Timeout = 30000;
                HttpWebRequest http = request as HttpWebRequest;
                if (http != null) http.ReadWriteTimeout = 60000;
                return request;
            }
        }

        private static void Retry(Action action, int attempts)
        {
            if (action == null) return;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch
                {
                    if (i == attempts - 1) throw;
                    Thread.Sleep(220);
                }
            }
        }

        public static InstallResult Install(InstallOptions options, Action<double, string> progress)
        {
            InstallResult result = new InstallResult();
            try
            {
                if (options == null) options = new InstallOptions();
                string target = string.IsNullOrWhiteSpace(options.TargetDir) ? InstallOptions.DefaultTargetDir : Path.GetFullPath(options.TargetDir);
                if (!IsSafeTarget(target)) throw new InvalidOperationException("安装路径不安全：" + target);

                if (progress != null) progress(0.02, "正在关闭旧版本…");
                StopRunningApp();

                if (progress != null) progress(0.10, "正在创建安装目录…");
                Directory.CreateDirectory(target);
                string appPath = Path.Combine(target, AppExeName);
                string uninstallPath = Path.Combine(target, UninstallExeName);

                byte[] appBytes = ReadAppPayload();
                if (appBytes == null || appBytes.Length < 10000)
                    throw new InvalidOperationException("安装包内的主程序数据损坏，请重新下载安装包。");

                if (progress != null) progress(0.24, "正在写入 HAVEGET DynamicIsland 主程序…");
                string tempApp = appPath + ".new";
                Retry(delegate { File.WriteAllBytes(tempApp, appBytes); }, 6);
                Retry(delegate
                {
                    if (File.Exists(appPath)) File.Delete(appPath);
                    File.Move(tempApp, appPath);
                }, 6);

                if (progress != null) progress(0.42, "正在准备卸载程序…");
                string self = CurrentExePath;
                if (!string.Equals(self, uninstallPath, StringComparison.OrdinalIgnoreCase))
                {
                    Retry(delegate
                    {
                        if (File.Exists(uninstallPath)) File.Delete(uninstallPath);
                        File.Copy(self, uninstallPath, true);
                    }, 6);
                }

                WriteReadme(target);

                if (progress != null) progress(0.60, "正在创建快捷方式…");
                bool shortcutsOk = CreateShortcuts(target, options);

                if (progress != null) progress(0.78, "正在配置开机启动…");
                bool startupOk = ConfigureStartup(appPath, options.StartWithWindows);

                if (progress != null) progress(0.88, "正在注册卸载信息…");
                bool uninstallOk = RegisterUninstall(target, appPath, uninstallPath);

                List<string> warnings = new List<string>();
                if (!shortcutsOk) warnings.Add("未能创建快捷方式，可以从安装目录直接启动。");
                if (options.StartWithWindows && !startupOk) warnings.Add("开机启动未能写入，可稍后在应用设置中重试。");
                if (!uninstallOk) warnings.Add("未能注册卸载入口，可运行安装目录中的 Uninstall.exe 卸载。");
                if (warnings.Count > 0) result.Warning = string.Join(" ", warnings.ToArray());

                if (options.LaunchAfterInstall)
                {
                    try { Process.Start(new ProcessStartInfo(appPath, "--expanded") { UseShellExecute = true, WorkingDirectory = target }); }
                    catch (Exception ex) { SetupLog.Error("LaunchAfterInstall", ex); }
                }

                if (progress != null) progress(1.0, "安装完成");
                result.Success = true;
                result.AppPath = appPath;
                result.Message = "HAVEGET DynamicIsland 已安装到 " + target;
                SetupLog.Write("install success: " + target + " desktop=" + options.DesktopShortcut + " startup=" + options.StartWithWindows + " warning=" + result.Warning);
                return result;
            }
            catch (Exception ex)
            {
                SetupLog.Error("Install", ex);
                result.Success = false;
                if (ex is UnauthorizedAccessException)
                    result.Message = "无法写入安装目录。请确认当前用户对目标目录有写入权限，或换一个安装位置。";
                else
                    result.Message = ex.Message;
                return result;
            }
        }

        private static bool IsSafeTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return false;
            string full = Path.GetFullPath(target).TrimEnd('\\');
            if (full.Length < 12) return false;
            if (full == Path.GetPathRoot(full).TrimEnd('\\')) return false;
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');
            if (full.StartsWith(windows + "\\", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static byte[] ReadAppPayload()
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("HAVEGETDynamicIsland.exe"))
            {
                if (s == null) return null;
                using (MemoryStream ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void WriteReadme(string target)
        {
            try
            {
                string text =
                    "HAVEGET DynamicIsland for Windows " + Version + "\r\n" +
                    "========================================\r\n\r\n" +
                    "启动：双击桌面上的 HAVEGET DynamicIsland，时间胶囊会停靠在屏幕顶部中央。\r\n" +
                    "展开：点击时间胶囊；收起：点击面板外部、按 Esc，或右键点击展开面板。\r\n" +
                    "翻页：在展开的岛内上下滚动鼠标滚轮，或点击左侧圆点切换页面。\r\n" +
                    "栏目：音乐、通知、系统状态、剪贴板、通知中心、控制中心、日历日程、中转站，另有设置页。\r\n" +
                    "音乐：读取 Windows 当前媒体会话，显示真实曲名、歌手和封面；播放/暂停、上一首/下一首、\r\n" +
                    "      点击进度条都会控制当前播放器，进度条下方显示当前时间和总时长。\r\n" +
                    "通知：微信风格通知卡片，可标记已读或把回复草稿复制到剪贴板；通知中心保留最近记录。\r\n" +
                    "状态：查看本机 CPU、内存、磁盘、网络和电量，每两秒刷新一次。\r\n" +
                    "剪贴板：最近复制的文字会出现在这里，点击卡片可重新复制。\r\n" +
                    "控制中心：网络、蓝牙可在岛内直接切换（可能需要 UAC），音量和亮度直接调节，不会跳转系统设置。\r\n" +
                    "日历日程：选择日期，填写 HH:mm 时间和内容后添加提醒；到点会在岛内弹出提醒并响铃。\r\n" +
                    "中转站：把文件拖入岛内或点击“添加文件”；点击文件可打开，点击 × 可移出列表。\r\n" +
                    "设置：版本 v" + Version + "。可切换 6 套主题、打开开机自启动和显示秒数、开关上面 8 个栏目。\r\n" +
                    "      连续点击版本号 8 次，输入开发者密码 HAVEGETTJZYY 后，可调整 X/Y 偏移、\r\n" +
                    "      胶囊宽度、胶囊高度和胶囊时间字号。\r\n" +
                    "托盘：右键托盘图标可以打开任意栏目、通知速览、切换置顶、开机启动、隐藏或退出。\r\n\r\n" +
                    "卸载：双击安装目录中的 Uninstall.exe，或在 Windows 设置的“已安装的应用”中卸载。\r\n\r\n" +
                    "示例通知为应用内演示；音乐控制的是当前 Windows 媒体会话。系统状态、剪贴板和提醒只在本机处理，不上传网络。\r\n" +
                    "网络/蓝牙属于硬件切换，会请求管理员权限；没有对应硬件时会显示“未检测到”。\r\n";                File.WriteAllText(Path.Combine(target, "使用说明.txt"), text, new UTF8Encoding(true));
            }
            catch (Exception ex) { SetupLog.Error("WriteReadme", ex); }
        }

        private static void StopRunningApp()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("HAVEGET DynamicIsland");
                foreach (Process p in processes)
                {
                    try
                    {
                        p.CloseMainWindow();
                        if (!p.WaitForExit(900)) p.Kill();
                        p.WaitForExit(1500);
                    }
                    catch { }
                    finally { try { p.Dispose(); } catch { } }
                }
            }
            catch (Exception ex) { SetupLog.Error("StopRunningApp", ex); }
        }

        private static bool CreateShortcuts(string target, InstallOptions options)
        {
            string appPath = Path.Combine(target, AppExeName);
            string uninstallPath = Path.Combine(target, UninstallExeName);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string group = Path.Combine(programs, ProductName);
            try { Directory.CreateDirectory(group); } catch { }
            string desktopLink = Path.Combine(desktop, ProductName + ".lnk");
            string startLink = Path.Combine(group, ProductName + ".lnk");
            string uninstallLink = Path.Combine(group, "卸载" + ProductName + ".lnk");
            bool ok = true;

            if (options.DesktopShortcut)
                ok &= CreateShortcut(desktopLink, appPath, "", target, appPath);
            else
                TryDelete(desktopLink);

            ok &= CreateShortcut(startLink, appPath, "", target, appPath);
            ok &= CreateShortcut(uninstallLink, uninstallPath, "/uninstall", target, uninstallPath);
            return ok;
        }

        private static bool CreateShortcut(string linkPath, string targetPath, string arguments, string workingDir, string iconPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) throw new InvalidOperationException("WScript.Shell 不可用");
                object shell = Activator.CreateInstance(shellType);
                object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });
                Type linkType = link.GetType();
                linkType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { targetPath });
                linkType.InvokeMember("Arguments", BindingFlags.SetProperty, null, link, new object[] { arguments ?? "" });
                linkType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { workingDir ?? "" });
                linkType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { iconPath + ",0" });
                linkType.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { ProductName + " for Windows" });
                linkType.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
                if (Marshal.IsComObject(link)) Marshal.ReleaseComObject(link);
                if (Marshal.IsComObject(shell)) Marshal.ReleaseComObject(shell);
                return true;
            }
            catch (Exception ex)
            {
                SetupLog.Error("CreateShortcut " + linkPath, ex);
                return false;
            }
        }

        private static bool ConfigureStartup(string appPath, bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyName))
                {
                    if (key == null) return false;
                    if (enabled) key.SetValue(RunValueName, "\"" + appPath + "\" --startup", RegistryValueKind.String);
                    else key.DeleteValue(RunValueName, false);
                }
                return true;
            }
            catch (Exception ex) { SetupLog.Error("ConfigureStartup", ex); return false; }
        }

        private static bool RegisterUninstall(string target, string appPath, string uninstallPath)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKeyName))
                {
                    if (key == null) return false;
                    long size = 0;
                    try { foreach (string f in Directory.GetFiles(target)) size += new FileInfo(f).Length; } catch { }
                    key.SetValue("DisplayName", ProductName + " for Windows", RegistryValueKind.String);
                    key.SetValue("DisplayVersion", Version, RegistryValueKind.String);
                    key.SetValue("Publisher", "HAVEGET", RegistryValueKind.String);
                    key.SetValue("InstallLocation", target, RegistryValueKind.String);
                    key.SetValue("DisplayIcon", appPath + ",0", RegistryValueKind.String);
                    key.SetValue("UninstallString", "\"" + uninstallPath + "\" /uninstall", RegistryValueKind.String);
                    key.SetValue("QuietUninstallString", "\"" + uninstallPath + "\" /uninstall /S", RegistryValueKind.String);
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    key.SetValue("EstimatedSize", (int)(size / 1024), RegistryValueKind.DWord);
                    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"), RegistryValueKind.String);
                    key.SetValue("URLInfoAbout", "https://go.microsoft.com/", RegistryValueKind.String);
                }
                return true;
            }
            catch (Exception ex)
            {
                SetupLog.Error("RegisterUninstall", ex);
                return false;
            }
        }

        public static bool IsInstalled
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKeyName, false))
                    {
                        return key != null && key.GetValue("InstallLocation") != null;
                    }
                }
                catch { return false; }
            }
        }

        public static string InstalledVersion
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKeyName, false))
                    {
                        return key == null ? "" : (key.GetValue("DisplayVersion") as string ?? "");
                    }
                }
                catch { return ""; }
            }
        }

        public static InstallResult Uninstall(bool removeData, Action<double, string> progress)
        {
            InstallResult result = new InstallResult();
            try
            {
                string target = Path.GetDirectoryName(CurrentExePath);
                if (string.IsNullOrWhiteSpace(target)) throw new InvalidOperationException("无法确定安装目录。");
                if (progress != null) progress(0.05, "正在关闭 HAVEGET DynamicIsland…");
                StopRunningApp();

                if (progress != null) progress(0.25, "正在删除快捷方式…");
                RemoveShortcuts();
                ConfigureStartup("", false);

                if (progress != null) progress(0.42, "正在清理注册表…");
                try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyName, false); } catch { }

                if (removeData)
                {
                    if (progress != null) progress(0.55, "正在删除本地数据…");
                    try
                    {
                        string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"HAVEGET\DynamicIsland");
                        if (Directory.Exists(data)) Directory.Delete(data, true);
                    }
                    catch (Exception ex) { SetupLog.Error("Uninstall.RemoveData", ex); }
                }

                if (progress != null) progress(0.68, "正在删除程序文件…");
                string self = CurrentExePath;
                try
                {
                    foreach (string file in Directory.GetFiles(target))
                    {
                        if (string.Equals(file, self, StringComparison.OrdinalIgnoreCase)) continue;
                        try { File.Delete(file); } catch { }
                    }
                    foreach (string dir in Directory.GetDirectories(target))
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                }
                catch (Exception ex) { SetupLog.Error("Uninstall.DeleteFiles", ex); }

                if (progress != null) progress(1.0, "卸载完成");
                result.Success = true;
                result.Message = "HAVEGET DynamicIsland 已从本机移除。";
                SetupLog.Write("uninstall success target=" + target + " removeData=" + removeData);
                return result;
            }
            catch (Exception ex)
            {
                SetupLog.Error("Uninstall", ex);
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

        private static void RemoveShortcuts()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                TryDelete(Path.Combine(desktop, ProductName + ".lnk"));
                string group = Path.Combine(programs, ProductName);
                TryDelete(Path.Combine(group, ProductName + ".lnk"));
                TryDelete(Path.Combine(group, "卸载" + ProductName + ".lnk"));
                try { if (Directory.Exists(group) && Directory.GetFileSystemEntries(group).Length == 0) Directory.Delete(group); } catch { }
            }
            catch (Exception ex) { SetupLog.Error("RemoveShortcuts", ex); }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetShortPathName(string longPath, StringBuilder shortPath, uint bufferLength);

        private static string ShortPath(string path)
        {
            try
            {
                StringBuilder buffer = new StringBuilder(1024);
                uint length = GetShortPathName(path, buffer, (uint)buffer.Capacity);
                if (length > 0 && length < buffer.Capacity) return buffer.ToString();
            }
            catch { }
            return path;
        }

        public static void ScheduleUninstallerCleanup()
        {
            string exe = CurrentExePath;
            string dir = Path.GetDirectoryName(exe);
            ScheduleSelfDelete(exe, dir);
        }

        private static void ScheduleSelfDelete(string exePath, string dir)
        {
            try
            {
                string batchPath = Path.Combine(Path.GetTempPath(), "haveget-dynamic-island-cleanup-" + Guid.NewGuid().ToString("N") + ".cmd");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("ping 127.0.0.1 -n 3 >nul");
                sb.AppendLine("del /f /q \"" + ShortPath(exePath) + "\" >nul 2>&1");
                sb.AppendLine("rmdir /s /q \"" + ShortPath(dir) + "\" >nul 2>&1");
                sb.AppendLine("del /f /q \"%~f0\" >nul 2>&1");
                File.WriteAllText(batchPath, sb.ToString(), Encoding.Default);
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c \"" + ShortPath(batchPath) + "\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
            }
            catch (Exception ex) { SetupLog.Error("ScheduleSelfDelete", ex); }
        }

        public static void OpenDownloadPage()
        {
            try { Process.Start(new ProcessStartInfo(DotNet48Url) { UseShellExecute = true }); }
            catch (Exception ex) { SetupLog.Error("OpenDownloadPage", ex); }
        }
    }
}
