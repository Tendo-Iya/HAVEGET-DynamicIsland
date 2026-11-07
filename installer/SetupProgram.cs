using System;
using System.IO;
using System.Windows;

namespace HavegetDynamicIsland.Setup
{
    public static class SetupProgram
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                bool uninstall = HasArg(args, "/uninstall");
                bool silent = HasArg(args, "/s") || HasArg(args, "/silent");
                bool removeData = HasArg(args, "/removedata");
                string target = GetArgValue(args, "/target=");
                if (string.IsNullOrWhiteSpace(target)) target = InstallOptions.DefaultTargetDir;

                if (!InstallerEngine.IsSupportedWindows())
                {
                    if (!silent)
                    {
                        MessageBox.Show("HAVEGET DynamicIsland 仅支持 Windows 10 与 Windows 11。\n当前系统：" + Environment.OSVersion.VersionString,
                            "HAVEGET DynamicIsland 安装程序", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return 2;
                }

                if (silent)
                {
                    return RunSilent(uninstall, removeData, target, args);
                }

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.DispatcherUnhandledException += delegate(object s, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
                {
                    SetupLog.Error("Dispatcher", e.Exception);
                    e.Handled = true;
                };
                InstallOptions options = new InstallOptions();
                options.TargetDir = target;
                options.DesktopShortcut = !HasArg(args, "/nodesktop");
                options.StartWithWindows = HasArg(args, "/startup");
                options.LaunchAfterInstall = HasArg(args, "/launch");
                SetupWindow window = new SetupWindow(options, uninstall);
                if (HasArg(args, "/autoinstall") && !uninstall)
                {
                    window.Loaded += delegate
                    {
                        window.Dispatcher.BeginInvoke(new Action(delegate { window.BeginInstall(); }));
                    };
                }
                if (HasArg(args, "/autouninstall") && uninstall)
                {
                    window.Loaded += delegate
                    {
                        window.Dispatcher.BeginInvoke(new Action(delegate { window.BeginUninstall(); }));
                    };
                }
                app.Run(window);
                return 0;
            }
            catch (Exception ex)
            {
                SetupLog.Error("SetupProgram.Main", ex);
                try
                {
                    if (!HasArg(args, "/s") && !HasArg(args, "/silent"))
                        MessageBox.Show("安装程序遇到错误：\n" + ex.Message + "\n\n日志：" + SetupLog.LogPath,
                            "HAVEGET DynamicIsland 安装程序", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
                return 1;
            }
        }

        private static int RunSilent(bool uninstall, bool removeData, string target, string[] args)
        {
            SetupLog.Write("silent start uninstall=" + uninstall + " target=" + target);
            try
            {
                if (uninstall)
                {
                    InstallResult ur = InstallerEngine.Uninstall(removeData, delegate(double p, string m) { SetupLog.Write("uninstall " + (int)(p * 100) + "% " + m); });
                    SetupLog.Write("silent uninstall success=" + ur.Success + " " + ur.Message);
                    if (ur.Success) InstallerEngine.ScheduleUninstallerCleanup();
                    return ur.Success ? 0 : 1;
                }

                RuntimeResult runtime = InstallerEngine.EnsureDotNet(delegate(double p, string m)
                {
                    SetupLog.Write("runtime " + (int)(p * 100) + "% " + m);
                });
                if (!runtime.Success)
                {
                    SetupLog.Write("silent runtime failed: " + runtime.Message);
                    return 3;
                }
                InstallOptions options = new InstallOptions();
                options.TargetDir = target;
                options.DesktopShortcut = !HasArg(args, "/nodesktop");
                options.StartWithWindows = HasArg(args, "/startup");
                options.LaunchAfterInstall = HasArg(args, "/launch");
                InstallResult result = InstallerEngine.Install(options, delegate(double p, string m)
                {
                    SetupLog.Write("install " + (int)(p * 100) + "% " + m);
                });
                SetupLog.Write("silent install success=" + result.Success + " " + result.Message);
                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                SetupLog.Error("RunSilent", ex);
                return 1;
            }
        }

        private static bool HasArg(string[] args, string name)
        {
            if (args == null) return false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string GetArgValue(string[] args, string prefix)
        {
            if (args == null) return "";
            foreach (string arg in args)
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length).Trim('"', ' ');
            }
            return "";
        }
    }
}
