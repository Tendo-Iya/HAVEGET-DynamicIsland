using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace HavegetDynamicIsland
{
    public static class Program
    {
        private const string MutexName = "Local\\HAVEGETDynamicIsland.Single";
        private const string ShowEventName = "Local\\HAVEGETDynamicIsland.Show";

        private static Mutex _mutex;
        private static EventWaitHandle _showEvent;
        private static volatile bool _shuttingDown;

        [STAThread]
        public static void Main(string[] args)
        {
            bool solo = HasArg(args, "--solo");
            string mutexName = solo ? MutexName + ".Solo" : MutexName;
            string eventName = solo ? ShowEventName + ".Solo" : ShowEventName;
            bool created;
            _mutex = new Mutex(true, mutexName, out created);
            if (!created)
            {
                if (SignalExisting(eventName)) return;
                // If the owner is stale or protected, don't make the app
                // impossible to launch; continue with a fresh instance.
            }

            bool eventCreated;
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName, out eventCreated);
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                Log.Error("AppDomain", e.ExceptionObject as Exception);
            };

            try
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
                {
                    Log.Error("Dispatcher", e.Exception);
                    e.Handled = true;
                };

                AppSettings settings = AppSettings.Load();
                Ui.ApplyTheme(settings.ThemeId);
                IslandWindow window = new IslandWindow(settings);
                TrayController tray = new TrayController(window, settings);
                window.AttachTray(tray);

                bool openExpanded = HasArg(args, "--expanded");
                bool showPreview = HasArg(args, "--preview") || HasArg(args, "--demo-preview");
                int startTab = ParseStartTab(args);
                string snapshotPath = GetArgValue(args, "--snapshot=");
                bool captureSnapshot = !string.IsNullOrEmpty(snapshotPath);
                if (openExpanded || startTab >= 0 || showPreview || captureSnapshot)
                {
                    window.Loaded += delegate
                    {
                        window.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (showPreview) window.ShowNotificationPreview();
                            else if (startTab >= 0) window.ShowIsland(startTab);
                            else if (openExpanded) window.ShowIsland(0);
                            if (captureSnapshot)
                            {
                                DispatcherTimer timer = new DispatcherTimer();
                                timer.Interval = TimeSpan.FromMilliseconds(950);
                                timer.Tick += delegate
                                {
                                    timer.Stop();
                                    try { window.SaveSnapshot(snapshotPath); } catch (Exception ex) { Log.Error("Snapshot", ex); }
                                    window.ExitApplication();
                                };
                                timer.Start();
                            }
                        }));
                    };
                }

                Thread listener = new Thread(delegate()
                {
                    while (!_shuttingDown)
                    {
                        try { _showEvent.WaitOne(); }
                        catch { break; }
                        if (_shuttingDown) break;
                        try
                        {
                            window.Dispatcher.BeginInvoke(new Action(delegate { window.ShowIsland(-1); }));
                        }
                        catch { break; }
                    }
                });
                listener.IsBackground = true;
                listener.Name = "HAVEGET-DynamicIsland.ShowListener";
                listener.Start();
                window.Show();
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Error("Program.Main", ex);
                try
                {
                    MessageBox.Show("HAVEGET DynamicIsland 启动失败：\n" + ex.Message + "\n\n日志：" + AppPaths.LogFile,
                        "HAVEGET DynamicIsland", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            }
            finally
            {
                _shuttingDown = true;
                try { if (_showEvent != null) _showEvent.Set(); } catch { }
                try { if (_showEvent != null) _showEvent.Dispose(); } catch { }
                try { _mutex.ReleaseMutex(); } catch { }
                try { _mutex.Dispose(); } catch { }
            }
        }

        private static int ParseStartTab(string[] args)
        {
            if (args == null) return -1;
            foreach (string arg in args)
            {
                if (arg.StartsWith("--tab=", StringComparison.OrdinalIgnoreCase))
                {
                    string value = arg.Substring(6).Trim().ToLowerInvariant();
                    if (value == "music" || value == "0") return 0;
                    if (value == "alerts" || value == "notifications" || value == "1") return 1;
                    if (value == "status" || value == "system" || value == "2") return 2;
                    if (value == "clipboard" || value == "3") return 3;
                    if (value == "notifycenter" || value == "notificationcenter" || value == "4") return 4;
                    if (value == "control" || value == "controlcenter" || value == "5") return 5;
                    if (value == "calendar" || value == "6") return 6;
                    if (value == "transfer" || value == "7") return 7;
                    if (value == "settings" || value == "8") return 8;
                }
            }
            return -1;
        }

        private static bool HasArg(string[] args, string name)
        {
            if (args == null) return false;
            foreach (string arg in args)
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string GetArgValue(string[] args, string prefix)
        {
            if (args == null) return null;
            foreach (string arg in args)
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return arg.Substring(prefix.Length);
            }
            return null;
        }

        private static bool SignalExisting(string eventName)
        {
            try
            {
                using (EventWaitHandle evt = EventWaitHandle.OpenExisting(eventName))
                {
                    evt.Set();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
