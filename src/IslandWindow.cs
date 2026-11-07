using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HavegetDynamicIsland
{
    public enum IslandMode
    {
        Compact,
        Expanded,
        Preview
    }

    public sealed class IslandWindow : Window
    {
        private const double ShadowMargin = 20.0;
        private const double TopOffset = 4.0;
        private const double PreviewWidth = 380.0;
        private const double PreviewHeight = 86.0;
        private const double ExpandedWidth = 400.0;
        private const double ExpandedHeight = 200.0;
        private const double FixedWindowWidth = ExpandedWidth + ShadowMargin * 2.0;
        private const double FixedWindowHeight = 540.0;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
        private const int HTCLIENT = 1;
        private const double ExpandedRadius = 30.0;
        private const double CompactRadius = 24.0;
        private const double PreviewRadius = 26.0;
        private const int PageCount = 9;
        private const int PageMusic = 0;
        private const int PageAlerts = 1;
        private const int PageStatus = 2;
        private const int PageClipboard = 3;
        private const int PageNotifyCenter = 4;
        private const int PageControl = 5;
        private const int PageCalendar = 6;
        private const int PageTransfer = 7;
        private const int PageSettings = 8;
        private const string ProductVersion = "1.2.6";

        private static readonly TrackInfo[] Tracks = CreateTracks();
        private static readonly AlertInfo[] Alerts = CreateAlerts();
        private static readonly Random Randomizer = new Random();

        private readonly AppSettings _settings;
        private readonly SystemMonitor _monitor = new SystemMonitor();
        private double _compactWidth = 176.0;
        private double _compactHeight = 48.0;
        private TrayController _tray;

        private Rect _workArea;
        private Border _panel;
        private Grid _clipHost;
        private Grid _compactView;
        private Grid _expandedView;
        private Grid _previewView;

        private TextBlock _compactClock;
        private TextBlock _compactDate;
        private Border _compactUnreadDot;
        private Rectangle[] _equalizerBars;

        private Grid _tabBar;
        private Border _tabIndicator;
        private TranslateTransform _tabTranslate;
        private TextBlock[] _tabLabels;
        private FrameworkElement[] _tabIcons;
        private Border[] _tabButtons;
        private Ellipse _alertTabDot;

        private Border _bellButton;
        private Grid _bellIconHost;
        private Border _headerUnreadDot;
        private Border[] _pageDots;
        private Ellipse[] _pageDotMarks;
        private StackPanel _pageDotRail;
        private readonly List<int> _dotPages = new List<int>();
        private int _dotCount;

        private Grid _clipboardView;
        private Grid _notifyCenterView;
        private Grid _controlView;
        private Grid _calendarView;
        private Grid _transferView;
        private StackPanel _clipboardList;
        private TextBlock _clipboardEmpty;
        private readonly List<string> _clipItems = new List<string>();
        private string _lastClipText;
        private StackPanel _notifyCenterList;
        private readonly List<AlertInfo> _notificationHistory = new List<AlertInfo>();
        private bool _focusMode;
        private int _volumePercent = -1;
        private Border _networkTile;
        private TextBlock _networkTileState;
        private Border _bluetoothTile;
        private TextBlock _bluetoothTileState;
        private Border _focusTile;
        private TextBlock _focusTileState;
        private TextBlock _volumeValueText;
        private Border _volumeFill;
        private Border _volumeTrack;
        private TextBlock _controlMessage;
        private int _brightnessPercent = -1;
        private Border _brightnessFill;
        private TextBlock _brightnessValueText;
        private TextBlock _controlCpuText;
        private TextBlock _controlMemText;
        private TextBlock _controlDiskText;
        private TextBlock _controlBatteryText;
        private DateTime _calendarMonth;
        private DateTime _selectedDate;
        private Grid _calendarDayGrid;
        private TextBlock _calendarMonthLabel;
        private TextBlock _selectedDateText;
        private StackPanel _reminderListPanel;
        private TextBox _reminderTimeBox;
        private TextBox _reminderTextBox;
        private TextBlock _calendarFeedback;
        private readonly List<ReminderItem> _reminders = new List<ReminderItem>();
        private StackPanel _transferList;
        private Grid _transferEmpty;
        private readonly List<string> _transferFiles = new List<string>();
        private readonly Dictionary<string, Border> _themeSwatches = new Dictionary<string, Border>();
        private Border _startupTrack;
        private Ellipse _startupKnob;
        private TextBlock _devWidthValue;
        private TextBlock _devHeightValue;

        private Grid _settingsView;
        private PasswordBox _devPasswordBox;
        private Border _devPasswordPanel;
        private StackPanel _developerPanel;
        private TextBlock _devErrorText;
        private TextBlock _devXValue;
        private TextBlock _devYValue;
        private TextBlock _devFontValue;
        private int _versionTapCount;

        private Grid _contentHost;
        private Grid _musicView;
        private Grid _alertsView;
        private Grid _statusView;

        private readonly MediaSessionService _mediaService = new MediaSessionService();
        private Grid _albumArt;
        private Grid _albumDisc;
        private Image _albumImage;
        private double _mediaDurationSeconds;
        private bool _mediaAvailable;
        private RotateTransform _discRotate;
        private TextBlock _albumStamp;
        private TextBlock _musicTitle;
        private TextBlock _musicArtist;
        private TextBlock _musicCounter;
        private TextBlock _timeNow;
        private TextBlock _timeTotal;
        private Border _progressTrack;
        private ScaleTransform _progressScale;
        private Grid _playIconHost;
        private Border _playButton;
        private Border _albumDiscLabel;

        private Border _alertCard;
        private Grid _alertIconHost;
        private Border _alertIconBox;
        private TextBlock _alertApp;
        private TextBlock _alertTime;
        private TextBlock _alertTitle;
        private TextBlock _alertBody;
        private Border _alertUnreadDot;
        private TextBlock _alertSummary;
        private Border _readButton;
        private TextBlock _readButtonText;
        private Border _simulateButton;
        private TextBlock _replyButtonText;

        private Border _statusStrip;
        private TextBlock _networkValue;
        private Ellipse _networkDot;
        private Border _statusBanner;
        private Grid _statusIconHost;
        private Ellipse _statusIconDot;
        private TextBlock _statusTitle;
        private TextBlock _statusDetail;
        private TextBlock _statusLive;
        private TextBlock _cpuValue;
        private TextBlock _cpuSub;
        private TextBlock _memValue;
        private TextBlock _memSub;
        private TextBlock _diskValue;
        private TextBlock _diskSub;
        private TextBlock _batteryValue;
        private TextBlock _batterySub;
        private ScaleTransform _cpuFill;
        private ScaleTransform _memFill;
        private ScaleTransform _diskFill;
        private ScaleTransform _batteryFill;

        private TextBlock _previewApp;
        private TextBlock _previewTime;
        private TextBlock _previewTitle;
        private TextBlock _previewBody;
        private Grid _previewIconHost;

        private SpringAnimation _widthSpring;
        private SpringAnimation _heightSpring;
        private SpringAnimation _radiusSpring;
        private SpringAnimation _tabSpring;

        private DispatcherTimer _clockTimer;
        private DispatcherTimer _musicTimer;
        private DispatcherTimer _equalizerTimer;
        private DispatcherTimer _statusTimer;
        private DispatcherTimer _previewTimer;
        private DispatcherTimer _autoCollapseTimer;

        private IslandMode _mode = IslandMode.Compact;
        private int _tabIndex = 0;
        private int _trackIndex = 0;
        private int _alertIndex = 0;
        private int _unreadCount = 2;
        private int _elapsed = 74;
        private bool _playing = true;
        private bool _scrubbing;
        private bool _previewMouseInside;
        private bool _allowClose;
        private bool _cleanupDone;
        private bool _loaded;
        private double _wavePhase;
        private int _lastSecond;
        private SystemSnapshot _lastStatus;

        public IslandWindow(AppSettings settings)
        {
            _settings = settings ?? new AppSettings();
            _compactWidth = Math.Max(140.0, Math.Min(320.0, _settings.CapsuleWidth));
            _compactHeight = Math.Max(36.0, Math.Min(72.0, _settings.CapsuleHeight));
            _calendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;
            LoadReminders();
            foreach (AlertInfo alert in Alerts) _notificationHistory.Add(alert);
            _workArea = SystemParameters.WorkArea;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = _settings.Topmost;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Title = "HAVEGET DynamicIsland";
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            FontFamily = Ui.UIFont;
            Width = FixedWindowWidth;
            Height = FixedWindowHeight;
            Left = _workArea.Left + (_workArea.Width - Width) / 2.0;
            Top = _workArea.Top + TopOffset;
            try { Icon = LoadWindowIcon(); } catch { }
            BuildShell();
            CreateSprings();
            CreateTimers();
            ShowActivity(PageEnabled(0) ? 0 : FirstEnabledPage(), false);
            RefreshTrackUi();
            RefreshAlertUi();
            RefreshClock();
            RefreshStatus();
            UpdateUnreadIndicators();
            ApplyDeveloperSettings();

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            SizeChanged += delegate { UpdatePanelClip(); };
            Closing += OnClosing;
            Closed += OnClosed;
            Deactivated += OnDeactivated;
            Activated += delegate { CancelAutoCollapse(); };
            KeyDown += OnKeyDown;
            PreviewKeyDown += OnKeyDown;
            DpiChanged += delegate { Dispatcher.BeginInvoke(new Action(PositionAtTop)); };
        }

        public AppSettings Settings { get { return _settings; } }

        public void AttachTray(TrayController tray)
        {
            _tray = tray;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            PositionAtTop();
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null) source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                long packed = lParam.ToInt64();
                int screenX = (short)(packed & 0xFFFF);
                int screenY = (short)((packed >> 16) & 0xFFFF);
                Point client = PointFromScreen(new Point(screenX, screenY));
                Point origin = _panel.TranslatePoint(new Point(0, 0), this);
                bool inside = client.X >= origin.X && client.Y >= origin.Y &&
                              client.X <= origin.X + _panel.ActualWidth &&
                              client.Y <= origin.Y + _panel.ActualHeight;
                handled = true;
                return new IntPtr(inside ? HTCLIENT : HTTRANSPARENT);
            }
            return IntPtr.Zero;
        }

        private static ImageSource LoadWindowIcon()
        {
            using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(AppPaths.ExecutablePath))
            {
                if (icon == null) return null;
                BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
        }

        private static TrackInfo[] CreateTracks()
        {
            TrackInfo[] tracks = new TrackInfo[3];
            tracks[0] = new TrackInfo();
            tracks[0].Title = "Green Light";
            tracks[0].Artist = "HAVEGET Studio";
            tracks[0].Duration = 198;
            tracks[0].ArtA = "#9BD5FF";
            tracks[0].ArtB = "#0B3C68";
            tracks[1] = new TrackInfo();
            tracks[1].Title = "Soft Focus";
            tracks[1].Artist = "HAVEGET Studio";
            tracks[1].Duration = 181;
            tracks[1].ArtA = "#5CB8FF";
            tracks[1].ArtB = "#10344F";
            tracks[2] = new TrackInfo();
            tracks[2].Title = "Night Current";
            tracks[2].Artist = "HAVEGET Studio";
            tracks[2].Duration = 213;
            tracks[2].ArtA = "#78C8FF";
            tracks[2].ArtB = "#0A2B46";
            return tracks;
        }

        private static AlertInfo[] CreateAlerts()
        {
            AlertInfo[] alerts = new AlertInfo[3];
            alerts[0] = new AlertInfo();
            alerts[0].App = "微信";
            alerts[0].Title = "项目讨论";
            alerts[0].Body = "Slack: John mentioned you in 项目讨论";
            alerts[0].Time = "刚刚";
            alerts[0].Icon = Ico.Chat;
            alerts[0].Read = false;
            alerts[1] = new AlertInfo();
            alerts[1].App = "Slack";
            alerts[1].Title = "#design";
            alerts[1].Body = "Mia: 新的设计稿已经放到中转站了。";
            alerts[1].Time = "2 分钟前";
            alerts[1].Icon = Ico.Chat;
            alerts[1].Read = false;
            alerts[2] = new AlertInfo();
            alerts[2].App = "HAVEGET";
            alerts[2].Title = "所有栏目已就绪";
            alerts[2].Body = "栏目可以在设置中自由开关。";
            alerts[2].Time = "今天";
            alerts[2].Icon = Ico.Sparkle;
            alerts[2].Read = true;
            return alerts;
        }

        private void BuildShell()
        {
            Grid outer = new Grid();
            outer.Background = Brushes.Transparent;

            _panel = new Border();
            _panel.Width = _compactWidth;
            _panel.Height = _compactHeight;
            _panel.HorizontalAlignment = HorizontalAlignment.Center;
            _panel.VerticalAlignment = VerticalAlignment.Top;
            _panel.Margin = new Thickness(0, ShadowMargin, 0, 0);
            _panel.CornerRadius = new CornerRadius(CompactRadius);
            _panel.Background = CreatePanelBackground();
            _panel.BorderBrush = Ui.Alpha(Colors.White, 20);
            _panel.BorderThickness = new Thickness(1);
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.Color = Colors.Black;
            shadow.BlurRadius = 24;
            shadow.ShadowDepth = 8;
            shadow.Direction = 270;
            shadow.Opacity = 0.38;
            _panel.Effect = shadow;
            outer.Children.Add(_panel);
            _panel.PreviewMouseWheel += OnIslandWheel;
            _panel.AllowDrop = true;
            _panel.DragEnter += delegate(object s, DragEventArgs e)
            {
                e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
                e.Handled = true;
            };
            _panel.Drop += OnFileDrop;

            _clipHost = new Grid();
            _clipHost.ClipToBounds = true;
            _panel.Child = _clipHost;

            Border topSheen = new Border();
            topSheen.Height = 1;
            topSheen.Margin = new Thickness(30, 1, 30, 0);
            topSheen.VerticalAlignment = VerticalAlignment.Top;
            LinearGradientBrush sheen = new LinearGradientBrush();
            sheen.StartPoint = new Point(0, 0);
            sheen.EndPoint = new Point(1, 0);
            sheen.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0));
            sheen.GradientStops.Add(new GradientStop(Color.FromArgb(46, 255, 255, 255), 0.5));
            sheen.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            sheen.Freeze();
            topSheen.Background = sheen;
            topSheen.IsHitTestVisible = false;
            _clipHost.Children.Add(topSheen);

            _compactView = BuildCompactView();
            _clipHost.Children.Add(_compactView);

            _expandedView = BuildExpandedView();
            _expandedView.Visibility = Visibility.Collapsed;
            _expandedView.Opacity = 0;
            _clipHost.Children.Add(_expandedView);

            _previewView = BuildPreviewView();
            _previewView.Visibility = Visibility.Collapsed;
            _previewView.Opacity = 0;
            _clipHost.Children.Add(_previewView);

            Content = outer;
            _clipHost.SizeChanged += delegate { UpdatePanelClip(); };
            UpdatePanelClip();
        }

        private static LinearGradientBrush CreatePanelBackground()
        {
            LinearGradientBrush bg = new LinearGradientBrush();
            bg.StartPoint = new Point(0, 0);
            bg.EndPoint = new Point(0.15, 1);
            bg.GradientStops.Add(new GradientStop(Color.FromArgb(252, Ui.BgTop.R, Ui.BgTop.G, Ui.BgTop.B), 0));
            bg.GradientStops.Add(new GradientStop(Color.FromArgb(253, Ui.BgBottom.R, Ui.BgBottom.G, Ui.BgBottom.B), 1));
            bg.Freeze();
            return bg;
        }

        private Grid BuildCompactView()
        {
            Grid view = new Grid();
            view.Width = _compactWidth;
            view.Height = _compactHeight;
            view.HorizontalAlignment = HorizontalAlignment.Center;
            view.VerticalAlignment = VerticalAlignment.Top;
            view.Background = Brushes.Transparent;
            view.Cursor = Cursors.Hand;
            view.ToolTip = "点击展开 HAVEGET DynamicIsland";
            view.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Expand(_tabIndex);
            };
            view.MouseEnter += delegate
            {
                if (_mode == IslandMode.Compact) _panel.BorderBrush = Ui.Alpha(Colors.White, 34);
            };
            view.MouseLeave += delegate
            {
                if (_mode == IslandMode.Compact) _panel.BorderBrush = Ui.Alpha(Colors.White, 20);
            };

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Horizontal;
            content.HorizontalAlignment = HorizontalAlignment.Center;
            content.VerticalAlignment = VerticalAlignment.Center;
            view.Children.Add(content);

            FrameworkElement clockIcon = Ui.Icon(Ico.Clock, Ui.SubBrush, 15, 1.35);
            clockIcon.VerticalAlignment = VerticalAlignment.Center;
            content.Children.Add(clockIcon);

            _compactClock = Ui.Text("21:04", _settings.CompactTimeFontSize, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
            _compactClock.Margin = new Thickness(8, 0, 0, 0);
            _compactClock.VerticalAlignment = VerticalAlignment.Center;
            Ui.Tabular(_compactClock);
            content.Children.Add(_compactClock);

            _equalizerBars = null;
            _compactDate = null;
            _compactUnreadDot = null;
            return view;
        }
        private Grid BuildExpandedView()
        {
            Grid view = new Grid();
            view.Width = ExpandedWidth;
            view.HorizontalAlignment = HorizontalAlignment.Center;
            view.VerticalAlignment = VerticalAlignment.Stretch;

            Grid inner = new Grid();
            inner.Margin = new Thickness(16);
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            view.Children.Add(inner);

            _pageDotRail = new StackPanel();
            _pageDotRail.HorizontalAlignment = HorizontalAlignment.Center;
            _pageDotRail.VerticalAlignment = VerticalAlignment.Center;
            inner.Children.Add(_pageDotRail);

            _contentHost = new Grid();
            _contentHost.ClipToBounds = true;
            Grid.SetColumn(_contentHost, 1);
            inner.Children.Add(_contentHost);

            _musicView = BuildMusicView();
            _alertsView = BuildAlertsView();
            _statusView = BuildStatusView();
            _clipboardView = BuildClipboardView();
            _notifyCenterView = BuildNotifyCenterView();
            _controlView = BuildControlCenterView();
            _calendarView = BuildCalendarView();
            _transferView = BuildTransferView();
            _settingsView = BuildSettingsView();
            _contentHost.Children.Add(_musicView);
            _contentHost.Children.Add(_alertsView);
            _contentHost.Children.Add(_statusView);
            _contentHost.Children.Add(_clipboardView);
            _contentHost.Children.Add(_notifyCenterView);
            _contentHost.Children.Add(_controlView);
            _contentHost.Children.Add(_calendarView);
            _contentHost.Children.Add(_transferView);
            _contentHost.Children.Add(_settingsView);
            foreach (Grid page in AllPageViews()) page.Visibility = Visibility.Collapsed;

            RebuildPageDots();
            UpdatePageDots();
            view.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Collapse();
            };

            UpdatePageDots();
            return view;
        }
        private Grid BuildClipboardView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid head = new Grid();
            TextBlock title = Ui.Text("剪贴板", 13, FontWeights.SemiBold, Ui.TextBrush);
            title.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(title);
            Border clear = MakeSettingsButton("清空", false, 42);
            clear.HorizontalAlignment = HorizontalAlignment.Right;
            clear.VerticalAlignment = VerticalAlignment.Center;
            clear.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _clipItems.Clear();
                RefreshClipboardList();
            };
            head.Children.Add(clear);
            view.Children.Add(head);

            Grid body = new Grid();
            Grid.SetRow(body, 1);
            view.Children.Add(body);

            _clipboardEmpty = Ui.Text("复制文字后会出现在这里", 8.6, FontWeights.Normal, Ui.SubBrush);
            _clipboardEmpty.HorizontalAlignment = HorizontalAlignment.Center;
            _clipboardEmpty.VerticalAlignment = VerticalAlignment.Center;
            body.Children.Add(_clipboardEmpty);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            body.Children.Add(scroll);
            _clipboardList = new StackPanel();
            _clipboardList.Margin = new Thickness(0, 2, 0, 0);
            scroll.Content = _clipboardList;

            RefreshClipboardList();
            return view;
        }

        private Grid BuildNotifyCenterView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid head = new Grid();
            TextBlock title = Ui.Text("通知中心", 13, FontWeights.SemiBold, Ui.TextBrush);
            title.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(title);
            Border allRead = MakeSettingsButton("全部已读", false, 62);
            allRead.HorizontalAlignment = HorizontalAlignment.Right;
            allRead.VerticalAlignment = VerticalAlignment.Center;
            allRead.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                MarkAllRead();
                RefreshNotificationCenter();
            };
            head.Children.Add(allRead);
            view.Children.Add(head);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Grid.SetRow(scroll, 1);
            _notifyCenterList = new StackPanel();
            scroll.Content = _notifyCenterList;
            view.Children.Add(scroll);
            RefreshNotificationCenter();
            return view;
        }

        private Grid BuildControlCenterView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid tiles = new Grid();
            tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            view.Children.Add(tiles);

            _networkTile = BuildControlTile("网络", Ico.Wifi, out _networkTileState);
            _networkTile.Margin = new Thickness(0, 0, 4, 0);
            _networkTile.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ToggleNetwork(); };
            tiles.Children.Add(_networkTile);

            _bluetoothTile = BuildControlTile("蓝牙", Ico.Bluetooth, out _bluetoothTileState);
            _bluetoothTile.Margin = new Thickness(4, 0, 4, 0);
            _bluetoothTile.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ToggleBluetooth(); };
            Grid.SetColumn(_bluetoothTile, 1);
            tiles.Children.Add(_bluetoothTile);

            _focusTile = BuildControlTile("专注", Ico.Moon, out _focusTileState);
            _focusTile.Margin = new Thickness(4, 0, 0, 0);
            _focusTile.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ToggleFocusMode(); };
            Grid.SetColumn(_focusTile, 2);
            tiles.Children.Add(_focusTile);

            Grid volume = new Grid();
            volume.Margin = new Thickness(0, 8, 0, 0);
            volume.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            volume.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            volume.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            volume.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            volume.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            volume.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            Grid.SetRow(volume, 1);
            view.Children.Add(volume);
            FrameworkElement volumeIcon = Ui.Icon(Ico.Volume, Ui.SubBrush, 15, 1.4);
            volumeIcon.VerticalAlignment = VerticalAlignment.Center;
            volume.Children.Add(volumeIcon);
            TextBlock volumeLabel = Ui.Text("音量", 8.0, FontWeights.Normal, Ui.SubBrush);
            volumeLabel.VerticalAlignment = VerticalAlignment.Center;
            volumeLabel.Margin = new Thickness(7, 0, 9, 0);
            Grid.SetColumn(volumeLabel, 1);
            volume.Children.Add(volumeLabel);

            _volumeTrack = new Border();
            _volumeTrack.Height = 4;
            _volumeTrack.CornerRadius = new CornerRadius(2);
            _volumeTrack.Background = Ui.Alpha(Colors.White, 10);
            _volumeTrack.VerticalAlignment = VerticalAlignment.Center;
            _volumeTrack.Width = 128;
            _volumeTrack.HorizontalAlignment = HorizontalAlignment.Left;
            Grid volumeHost = new Grid();
            volumeHost.Width = 128;
            volumeHost.HorizontalAlignment = HorizontalAlignment.Left;
            volumeHost.VerticalAlignment = VerticalAlignment.Center;
            _volumeTrack.Width = 128;
            _volumeFill = new Border();
            _volumeFill.Height = 4;
            _volumeFill.CornerRadius = new CornerRadius(2);
            _volumeFill.Background = Ui.AccentBrush;
            _volumeFill.HorizontalAlignment = HorizontalAlignment.Left;
            _volumeFill.Width = 64;
            volumeHost.Children.Add(_volumeTrack);
            volumeHost.Children.Add(_volumeFill);
            Grid.SetColumn(volumeHost, 2);
            volume.Children.Add(volumeHost);

            _volumeValueText = Ui.Text("50%", 8.0, FontWeights.Medium, Ui.TextBrush, Ui.DisplayFont);
            _volumeValueText.VerticalAlignment = VerticalAlignment.Center;
            _volumeValueText.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(_volumeValueText, 3);
            Ui.Tabular(_volumeValueText);
            volume.Children.Add(_volumeValueText);

            Border down = MakeSettingsButton("−", false, 34);
            down.HorizontalAlignment = HorizontalAlignment.Right;
            down.VerticalAlignment = VerticalAlignment.Center;
            down.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ChangeVolume(-5); };
            Grid.SetColumn(down, 4);
            volume.Children.Add(down);
            Border up = MakeSettingsButton("+", true, 34);
            up.HorizontalAlignment = HorizontalAlignment.Right;
            up.VerticalAlignment = VerticalAlignment.Center;
            up.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ChangeVolume(5); };
            Grid.SetColumn(up, 5);
            volume.Children.Add(up);

            Grid brightness = new Grid();
            brightness.Margin = new Thickness(0, 6, 0, 0);
            brightness.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            brightness.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            brightness.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            brightness.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            brightness.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            brightness.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            Grid.SetRow(brightness, 2);
            view.Children.Add(brightness);
            FrameworkElement brightnessIcon = Ui.Icon(Ico.Bolt, Ui.SubBrush, 15, 1.4);
            brightnessIcon.VerticalAlignment = VerticalAlignment.Center;
            brightness.Children.Add(brightnessIcon);
            TextBlock brightnessLabel = Ui.Text("亮度", 8.0, FontWeights.Normal, Ui.SubBrush);
            brightnessLabel.VerticalAlignment = VerticalAlignment.Center;
            brightnessLabel.Margin = new Thickness(7, 0, 9, 0);
            Grid.SetColumn(brightnessLabel, 1);
            brightness.Children.Add(brightnessLabel);
            Grid brightnessHost = new Grid();
            brightnessHost.Width = 128;
            brightnessHost.HorizontalAlignment = HorizontalAlignment.Left;
            brightnessHost.VerticalAlignment = VerticalAlignment.Center;
            Border brightnessTrack = new Border();
            brightnessTrack.Height = 4;
            brightnessTrack.CornerRadius = new CornerRadius(2);
            brightnessTrack.Background = Ui.Alpha(Colors.White, 10);
            brightnessTrack.Width = 128;
            brightnessHost.Children.Add(brightnessTrack);
            _brightnessFill = new Border();
            _brightnessFill.Height = 4;
            _brightnessFill.CornerRadius = new CornerRadius(2);
            _brightnessFill.Background = Ui.AccentBrush;
            _brightnessFill.HorizontalAlignment = HorizontalAlignment.Left;
            brightnessHost.Children.Add(_brightnessFill);
            Grid.SetColumn(brightnessHost, 2);
            brightness.Children.Add(brightnessHost);
            _brightnessValueText = Ui.Text("--", 8.0, FontWeights.Medium, Ui.TextBrush, Ui.DisplayFont);
            _brightnessValueText.HorizontalAlignment = HorizontalAlignment.Center;
            _brightnessValueText.VerticalAlignment = VerticalAlignment.Center;
            Ui.Tabular(_brightnessValueText);
            Grid.SetColumn(_brightnessValueText, 3);
            brightness.Children.Add(_brightnessValueText);
            Border brightnessDown = MakeSettingsButton("−", false, 34);
            brightnessDown.HorizontalAlignment = HorizontalAlignment.Right;
            brightnessDown.VerticalAlignment = VerticalAlignment.Center;
            brightnessDown.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ChangeBrightness(-5); };
            Grid.SetColumn(brightnessDown, 4);
            brightness.Children.Add(brightnessDown);
            Border brightnessUp = MakeSettingsButton("+", true, 34);
            brightnessUp.HorizontalAlignment = HorizontalAlignment.Right;
            brightnessUp.VerticalAlignment = VerticalAlignment.Center;
            brightnessUp.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; ChangeBrightness(5); };
            Grid.SetColumn(brightnessUp, 5);
            brightness.Children.Add(brightnessUp);

            Border stats = new Border();
            stats.CornerRadius = new CornerRadius(14);
            stats.Background = Ui.Alpha(Colors.White, 4);
            stats.BorderBrush = Ui.Alpha(Colors.White, 7);
            stats.BorderThickness = new Thickness(1);
            stats.Padding = new Thickness(10, 6, 10, 6);
            stats.Margin = new Thickness(0, 8, 0, 0);
            Grid.SetRow(stats, 3);
            view.Children.Add(stats);
            Grid statsGrid = new Grid();
            stats.Child = statsGrid;
            for (int sc = 0; sc < 4; sc++) statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _controlCpuText = BuildControlStat(statsGrid, 0, "CPU");
            _controlMemText = BuildControlStat(statsGrid, 1, "内存");
            _controlDiskText = BuildControlStat(statsGrid, 2, "磁盘");
            _controlBatteryText = BuildControlStat(statsGrid, 3, "电量");
            _controlMessage = Ui.Text("", 7.7, FontWeights.Normal, Ui.SubBrush);
            _controlMessage.Margin = new Thickness(0, 9, 0, 0);
            _controlMessage.TextWrapping = TextWrapping.Wrap;
            _controlMessage.LineHeight = 13;
            Grid.SetRow(_controlMessage, 4);
            view.Children.Add(_controlMessage);

            RefreshControlCenter();
            return view;
        }

        private Border BuildControlTile(string label, string iconData, out TextBlock state)
        {
            Border card = new Border();
            card.CornerRadius = new CornerRadius(14);
            card.Background = Ui.Alpha(Colors.White, 4);
            card.BorderBrush = Ui.Alpha(Colors.White, 8);
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(10, 8, 10, 8);
            card.Cursor = Cursors.Hand;
            StackPanel content = new StackPanel();
            card.Child = content;
            FrameworkElement icon = Ui.Icon(iconData, Ui.AccentBrush, 16, 1.5);
            icon.HorizontalAlignment = HorizontalAlignment.Left;
            content.Children.Add(icon);
            TextBlock name = Ui.Text(label, 8.6, FontWeights.Medium, Ui.TextBrush);
            name.Margin = new Thickness(0, 5, 0, 0);
            content.Children.Add(name);
            state = Ui.Text("", 7.2, FontWeights.Normal, Ui.SubBrush);
            state.Margin = new Thickness(0, 1, 0, 0);
            state.TextTrimming = TextTrimming.CharacterEllipsis;
            content.Children.Add(state);
            return card;
        }

        private Grid BuildCalendarView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(138) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid head = new Grid();
            Border prev = Ui.IconButton(Ico.ChevronLeft, "上个月", 20, delegate { _calendarMonth = _calendarMonth.AddMonths(-1); RefreshCalendar(); });
            prev.HorizontalAlignment = HorizontalAlignment.Left;
            head.Children.Add(prev);
            _calendarMonthLabel = Ui.Text("", 10, FontWeights.SemiBold, Ui.TextBrush);
            _calendarMonthLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _calendarMonthLabel.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(_calendarMonthLabel);
            Border next = Ui.IconButton(Ico.ChevronRight, "下个月", 20, delegate { _calendarMonth = _calendarMonth.AddMonths(1); RefreshCalendar(); });
            next.HorizontalAlignment = HorizontalAlignment.Right;
            head.Children.Add(next);
            view.Children.Add(head);

            Grid week = new Grid();
            week.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 1; i < 7; i++) week.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            string[] weekdays = new string[] { "一", "二", "三", "四", "五", "六", "日" };
            for (int i = 0; i < 7; i++)
            {
                TextBlock d = Ui.Text(weekdays[i], 7.2, FontWeights.Normal, Ui.SubBrush);
                d.HorizontalAlignment = HorizontalAlignment.Center;
                d.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(d, i);
                week.Children.Add(d);
            }
            Grid.SetRow(week, 1);
            view.Children.Add(week);

            _calendarDayGrid = new Grid();
            for (int c = 0; c < 7; c++) _calendarDayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int r = 0; r < 6; r++) _calendarDayGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(23) });
            Grid.SetRow(_calendarDayGrid, 2);
            view.Children.Add(_calendarDayGrid);

            Grid add = new Grid();
            add.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            add.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            add.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            add.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            Grid.SetRow(add, 3);
            view.Children.Add(add);
            _selectedDateText = Ui.Text("", 7.4, FontWeights.Medium, Ui.AccentPaleBrush);
            _selectedDateText.VerticalAlignment = VerticalAlignment.Center;
            add.Children.Add(_selectedDateText);
            _reminderTimeBox = MakeInputBox(DateTime.Now.AddMinutes(10).ToString("HH:mm"), 44);
            Grid.SetColumn(_reminderTimeBox, 1);
            add.Children.Add(_reminderTimeBox);
            _reminderTextBox = MakeInputBox("", 0);
            _reminderTextBox.Margin = new Thickness(4, 0, 4, 0);
            Grid.SetColumn(_reminderTextBox, 2);
            add.Children.Add(_reminderTextBox);
            Border plus = MakeSettingsButton("+", true, 26);
            plus.HorizontalAlignment = HorizontalAlignment.Right;
            plus.VerticalAlignment = VerticalAlignment.Center;
            plus.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; AddReminder(); };
            Grid.SetColumn(plus, 3);
            add.Children.Add(plus);

            _calendarFeedback = Ui.Text("", 7.2, FontWeights.Normal, Ui.DangerBrush);
            _calendarFeedback.Visibility = Visibility.Collapsed;
            Grid.SetRow(_calendarFeedback, 4);
            view.Children.Add(_calendarFeedback);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Grid.SetRow(scroll, 5);
            _reminderListPanel = new StackPanel();
            scroll.Content = _reminderListPanel;
            view.Children.Add(scroll);

            RefreshCalendar();
            return view;
        }

        private TextBox MakeInputBox(string text, double width)
        {
            TextBox box = new TextBox();
            box.Text = text;
            if (width > 0) box.Width = width;
            box.Height = 24;
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.FontFamily = Ui.UIFont;
            box.FontSize = 8.0;
            box.Foreground = Ui.TextBrush;
            box.Background = Ui.Alpha(Colors.White, 6);
            box.BorderBrush = Ui.Alpha(Colors.White, 10);
            box.BorderThickness = new Thickness(1);
            box.Padding = new Thickness(5, 1, 5, 1);
            box.CaretBrush = Ui.TextBrush;
            return box;
        }

        private Grid BuildTransferView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid head = new Grid();
            TextBlock title = Ui.Text("中转站", 13, FontWeights.SemiBold, Ui.TextBrush);
            title.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(title);
            Border add = MakeSettingsButton("添加文件", true, 72);
            add.HorizontalAlignment = HorizontalAlignment.Right;
            add.VerticalAlignment = VerticalAlignment.Center;
            add.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; PickTransferFiles(); };
            head.Children.Add(add);
            view.Children.Add(head);

            Grid body = new Grid();
            Grid.SetRow(body, 1);
            view.Children.Add(body);

            _transferEmpty = new Grid();
            StackPanel emptyStack = new StackPanel();
            emptyStack.HorizontalAlignment = HorizontalAlignment.Center;
            emptyStack.VerticalAlignment = VerticalAlignment.Center;
            FrameworkElement folder = Ui.Icon(Ico.Folder, Ui.AccentMutedBrush, 28, 1.4);
            folder.HorizontalAlignment = HorizontalAlignment.Center;
            emptyStack.Children.Add(folder);
            TextBlock emptyText = Ui.Text("拖入文件或点击添加", 8.4, FontWeights.Normal, Ui.SubBrush);
            emptyText.HorizontalAlignment = HorizontalAlignment.Center;
            emptyText.Margin = new Thickness(0, 7, 0, 0);
            emptyStack.Children.Add(emptyText);
            _transferEmpty.Children.Add(emptyStack);
            body.Children.Add(_transferEmpty);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _transferList = new StackPanel();
            scroll.Content = _transferList;
            body.Children.Add(scroll);

            RefreshTransferList();
            return view;
        }
        private void PollClipboard()
        {
            if (_clipboardList == null) return;
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;
                string text = System.Windows.Clipboard.GetText();
                if (string.IsNullOrEmpty(text)) return;
                if (string.Equals(text, _lastClipText, StringComparison.Ordinal)) return;
                _lastClipText = text;
                string compact = text.Replace("\r", " ").Replace("\n", " ").Trim();
                if (compact.Length == 0) return;
                _clipItems.Insert(0, compact);
                while (_clipItems.Count > 6) _clipItems.RemoveAt(_clipItems.Count - 1);
                RefreshClipboardList();
            }
            catch { }
        }

        private void RefreshClipboardList()
        {
            if (_clipboardList == null) return;
            _clipboardList.Children.Clear();
            bool empty = _clipItems.Count == 0;
            if (_clipboardEmpty != null) _clipboardEmpty.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            for (int i = 0; i < _clipItems.Count && i < 4; i++)
            {
                string text = _clipItems[i];
                Border card = new Border();
                card.Height = 40;
                card.CornerRadius = new CornerRadius(10);
                card.Background = Ui.Alpha(Colors.White, 4);
                card.BorderBrush = Ui.Alpha(Colors.White, 7);
                card.BorderThickness = new Thickness(1);
                card.Padding = new Thickness(10, 0, 10, 0);
                card.Margin = new Thickness(0, 0, 0, 6);
                card.Cursor = Cursors.Hand;
                Grid row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                card.Child = row;
                TextBlock value = Ui.Text(text, 8.3, FontWeights.Normal, Ui.TextBrush);
                value.TextTrimming = TextTrimming.CharacterEllipsis;
                value.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(value);
                FrameworkElement copyIcon = Ui.Icon(Ico.Clipboard, Ui.AccentMutedBrush, 12, 1.4);
                copyIcon.HorizontalAlignment = HorizontalAlignment.Right;
                copyIcon.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(copyIcon, 1);
                row.Children.Add(copyIcon);
                string captured = text;
                card.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    try { System.Windows.Clipboard.SetText(captured); _lastClipText = captured; } catch { }
                };
                _clipboardList.Children.Add(card);
            }
        }

        private void RefreshNotificationCenter()
        {
            if (_notifyCenterList == null) return;
            _notifyCenterList.Children.Clear();
            int shown = 0;
            for (int i = 0; i < _notificationHistory.Count && shown < 3; i++, shown++)
            {
                AlertInfo item = _notificationHistory[i];
                Border card = new Border();
                card.Height = 76;
                card.CornerRadius = new CornerRadius(13);
                card.Background = Ui.Alpha(Colors.White, 4);
                card.BorderBrush = Ui.Alpha(Colors.White, 7);
                card.BorderThickness = new Thickness(1);
                card.Padding = new Thickness(10, 8, 10, 8);
                card.Margin = new Thickness(0, 0, 0, 6);
                card.Cursor = Cursors.Hand;
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                card.Child = grid;
                Border iconBox = new Border();
                iconBox.Width = 30;
                iconBox.Height = 30;
                iconBox.CornerRadius = new CornerRadius(9);
                iconBox.Background = Ui.AccentTint(22);
                iconBox.VerticalAlignment = VerticalAlignment.Top;
                Grid iconHost = new Grid();
                iconBox.Child = iconHost;
                FrameworkElement icon = Ui.Icon(item.Icon, Ui.AccentPaleBrush, 15, 1.5);
                icon.HorizontalAlignment = HorizontalAlignment.Center;
                icon.VerticalAlignment = VerticalAlignment.Center;
                iconHost.Children.Add(icon);
                grid.Children.Add(iconBox);
                StackPanel text = new StackPanel();
                text.Margin = new Thickness(8, 0, 0, 0);
                Grid.SetColumn(text, 1);
                grid.Children.Add(text);
                Grid top = new Grid();
                TextBlock app = Ui.Text(item.App, 8.3, FontWeights.Medium, Ui.TextBrush);
                top.Children.Add(app);
                TextBlock time = Ui.Text(item.Time, 7.0, FontWeights.Normal, Ui.FaintBrush);
                time.HorizontalAlignment = HorizontalAlignment.Right;
                top.Children.Add(time);
                text.Children.Add(top);
                TextBlock title = Ui.Text(item.Title, 9.5, FontWeights.SemiBold, Ui.TextBrush);
                title.Margin = new Thickness(0, 2, 0, 0);
                title.TextTrimming = TextTrimming.CharacterEllipsis;
                text.Children.Add(title);
                TextBlock body = Ui.Text(item.Body, 7.7, FontWeights.Normal, Ui.SubBrush);
                body.Margin = new Thickness(0, 1, 0, 0);
                body.TextTrimming = TextTrimming.CharacterEllipsis;
                text.Children.Add(body);
                if (!item.Read)
                {
                    Ellipse dot = new Ellipse();
                    dot.Width = 5;
                    dot.Height = 5;
                    dot.Fill = Ui.AccentBrush;
                    dot.HorizontalAlignment = HorizontalAlignment.Right;
                    dot.VerticalAlignment = VerticalAlignment.Top;
                    dot.Margin = new Thickness(0, 1, 1, 0);
                    grid.Children.Add(dot);
                }
                AlertInfo captured = item;
                card.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    captured.Read = true;
                    RecalculateUnread();
                    RefreshNotificationCenter();
                    RefreshAlertUi();
                    UpdateUnreadIndicators();
                };
                _notifyCenterList.Children.Add(card);
            }
        }

        private void RecalculateUnread()
        {
            int count = 0;
            for (int i = 0; i < _notificationHistory.Count; i++)
            {
                if (!_notificationHistory[i].Read) count++;
            }
            _unreadCount = Math.Min(9, count);
        }

        private void RefreshControlCenter()
        {
            UpdateControlCenterStatus();
            UpdateVolumeUi();
            UpdateBrightnessUi();
        }

        private TextBlock BuildControlStat(Grid parent, int column, string label)
        {
            StackPanel cell = new StackPanel();
            cell.HorizontalAlignment = HorizontalAlignment.Center;
            cell.VerticalAlignment = VerticalAlignment.Center;
            TextBlock name = Ui.Text(label, 6.7, FontWeights.Normal, Ui.FaintBrush);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            cell.Children.Add(name);
            TextBlock value = Ui.Text("--", 10.5, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
            value.HorizontalAlignment = HorizontalAlignment.Center;
            value.Margin = new Thickness(0, 1, 0, 0);
            Ui.Tabular(value);
            cell.Children.Add(value);
            Grid.SetColumn(cell, column);
            parent.Children.Add(cell);
            return value;
        }

        private void UpdateBrightnessUi()
        {
            if (_brightnessPercent < 0) _brightnessPercent = BrightnessControl.GetBrightness();
            if (_brightnessValueText != null) _brightnessValueText.Text = _brightnessPercent < 0 ? "--" : _brightnessPercent + "%";
            if (_brightnessFill != null) _brightnessFill.Width = _brightnessPercent < 0 ? 0 : 128.0 * _brightnessPercent / 100.0;
        }

        private void ChangeBrightness(int delta)
        {
            if (_brightnessPercent < 0) _brightnessPercent = BrightnessControl.GetBrightness();
            if (_brightnessPercent < 0) return;
            _brightnessPercent = Math.Max(0, Math.Min(100, _brightnessPercent + delta));
            BrightnessControl.SetBrightness(_brightnessPercent);
            UpdateBrightnessUi();
            if (_controlMessage != null) _controlMessage.Text = "亮度 " + _brightnessPercent + "%";
        }

        private void UpdateControlCenterStatus()
        {
            if (_networkTileState != null)
            {
                if (_lastStatus == null) _networkTileState.Text = "读取中";
                else if (!_lastStatus.Online) _networkTileState.Text = "未连接";
                else _networkTileState.Text = _lastStatus.NetworkDetail;
                if (_networkTile != null)
                {
                    bool online = _lastStatus != null && _lastStatus.Online;
                    _networkTile.Background = online ? Ui.AccentTint(18) : Ui.Alpha(Colors.White, 4);
                    _networkTile.BorderBrush = online ? Ui.AccentTint(42) : Ui.Alpha(Colors.White, 8);
                }
            }
            if (_bluetoothTileState != null) _bluetoothTileState.Text = "点击切换";
            if (_focusTileState != null) _focusTileState.Text = _focusMode ? "已开启" : "已关闭";
            if (_focusTile != null)
            {
                _focusTile.Background = _focusMode ? Ui.AccentTint(18) : Ui.Alpha(Colors.White, 4);
                _focusTile.BorderBrush = _focusMode ? Ui.AccentTint(42) : Ui.Alpha(Colors.White, 8);
            }
            if (_controlCpuText != null)
            {
                if (_lastStatus == null)
                {
                    _controlCpuText.Text = "--";
                    _controlMemText.Text = "--";
                    _controlDiskText.Text = "--";
                    _controlBatteryText.Text = "--";
                }
                else
                {
                    _controlCpuText.Text = _lastStatus.HasCpuSample ? ((int)Math.Round(_lastStatus.CpuPercent)) + "%" : "--";
                    _controlMemText.Text = ((int)Math.Round(_lastStatus.MemoryPercent)) + "%";
                    _controlDiskText.Text = ((int)Math.Round(_lastStatus.DiskPercent)) + "%";
                    _controlBatteryText.Text = _lastStatus.BatteryPercent + "%";
                }
            }
        }

        private void UpdateVolumeUi()
        {
            _volumePercent = AudioControl.GetVolume();
            if (_volumeValueText != null) _volumeValueText.Text = _volumePercent + "%";
            if (_volumeFill != null) _volumeFill.Width = 128.0 * _volumePercent / 100.0;
        }

        private void ChangeVolume(int delta)
        {
            if (_volumePercent < 0) _volumePercent = AudioControl.GetVolume();
            _volumePercent = Math.Max(0, Math.Min(100, _volumePercent + delta));
            AudioControl.SetVolume(_volumePercent);
            UpdateVolumeUi();
            if (_controlMessage != null) _controlMessage.Text = "音量 " + _volumePercent + "%";
        }

        private void ToggleFocusMode()
        {
            _focusMode = !_focusMode;
            UpdateControlCenterStatus();
            if (_controlMessage != null) _controlMessage.Text = _focusMode ? "专注模式已开启" : "专注模式已关闭";
        }

        private void ToggleNetwork()
        {
            if (!DeviceControl.HasWifiAdapter())
            {
                if (_controlMessage != null) _controlMessage.Text = "未检测到无线网卡";
                return;
            }
            if (_controlMessage != null) _controlMessage.Text = "正在请求切换 Wi-Fi…";
            bool started = DeviceControl.ToggleWifi();
            if (_controlMessage != null) _controlMessage.Text = started ? "已请求切换 Wi-Fi" : "网络切换已取消或失败";
        }

        private void ToggleBluetooth()
        {
            if (!DeviceControl.HasBluetooth())
            {
                if (_controlMessage != null) _controlMessage.Text = "未检测到蓝牙";
                return;
            }
            if (_controlMessage != null) _controlMessage.Text = "正在请求切换蓝牙…";
            bool started = DeviceControl.ToggleBluetooth();
            if (_controlMessage != null) _controlMessage.Text = started ? "已请求切换蓝牙" : "蓝牙切换已取消或失败";
        }

        private void RefreshCalendar()
        {
            if (_calendarMonthLabel == null) return;
            _calendarMonthLabel.Text = _calendarMonth.ToString("yyyy年 M月");
            if (_selectedDateText != null) _selectedDateText.Text = _selectedDate.ToString("M/d");
            if (_calendarDayGrid != null)
            {
                _calendarDayGrid.Children.Clear();
                DateTime first = _calendarMonth;
                int offset = ((int)first.DayOfWeek + 6) % 7;
                DateTime start = first.AddDays(-offset);
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 7; col++)
                    {
                        DateTime date = start.AddDays(row * 7 + col);
                        bool currentMonth = date.Month == _calendarMonth.Month && date.Year == _calendarMonth.Year;
                        bool selected = date.Date == _selectedDate.Date;
                        bool today = date.Date == DateTime.Today;
                        Border cell = new Border();
                        cell.Margin = new Thickness(1);
                        cell.CornerRadius = new CornerRadius(6);
                        cell.Background = selected ? Ui.AccentTint(28) : Brushes.Transparent;
                        cell.BorderBrush = selected ? Ui.AccentTint(75) : (today ? Ui.AccentTint(35) : Brushes.Transparent);
                        cell.BorderThickness = new Thickness(selected || today ? 1 : 0);
                        cell.Cursor = Cursors.Hand;
                        TextBlock number = Ui.Text(date.Day.ToString(), 7.8, selected ? FontWeights.SemiBold : FontWeights.Normal,
                            currentMonth ? (selected ? Ui.AccentPaleBrush : Ui.TextBrush) : Ui.FaintBrush);
                        number.HorizontalAlignment = HorizontalAlignment.Center;
                        number.VerticalAlignment = VerticalAlignment.Center;
                        cell.Child = number;
                        DateTime captured = date;
                        cell.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                        {
                            e.Handled = true;
                            _selectedDate = captured;
                            _calendarMonth = new DateTime(captured.Year, captured.Month, 1);
                            RefreshCalendar();
                        };
                        Grid.SetRow(cell, row);
                        Grid.SetColumn(cell, col);
                        _calendarDayGrid.Children.Add(cell);
                    }
                }
            }
            RefreshReminderList();
        }

        private void RefreshReminderList()
        {
            if (_reminderListPanel == null) return;
            _reminderListPanel.Children.Clear();
            List<ReminderItem> items = new List<ReminderItem>();
            foreach (ReminderItem item in _reminders)
            {
                if (item.Due.Date == _selectedDate.Date) items.Add(item);
            }
            items.Sort(delegate(ReminderItem a, ReminderItem b) { return a.Due.CompareTo(b.Due); });
            if (items.Count == 0)
            {
                TextBlock empty = Ui.Text("这一天没有日程", 7.8, FontWeights.Normal, Ui.FaintBrush);
                empty.Margin = new Thickness(2, 5, 0, 0);
                _reminderListPanel.Children.Add(empty);
                return;
            }
            foreach (ReminderItem item in items)
            {
                Grid row = new Grid();
                row.Height = 29;
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                TextBlock time = Ui.Text(item.Due.ToString("HH:mm"), 7.7, FontWeights.SemiBold, Ui.AccentPaleBrush, Ui.DisplayFont);
                time.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(time);
                TextBlock text = Ui.Text(item.Text, 8.0, FontWeights.Normal, Ui.TextBrush);
                text.VerticalAlignment = VerticalAlignment.Center;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                Grid.SetColumn(text, 1);
                row.Children.Add(text);
                Border remove = Ui.IconButton(Ico.Close, "删除提醒", 18, null);
                remove.HorizontalAlignment = HorizontalAlignment.Right;
                remove.VerticalAlignment = VerticalAlignment.Center;
                ReminderItem captured = item;
                remove.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _reminders.Remove(captured);
                    SaveReminders();
                    RefreshReminderList();
                };
                Grid.SetColumn(remove, 2);
                row.Children.Add(remove);
                _reminderListPanel.Children.Add(row);
            }
        }

        private void AddReminder()
        {
            if (_calendarFeedback == null) return;
            _calendarFeedback.Visibility = Visibility.Collapsed;
            string text = _reminderTextBox.Text == null ? "" : _reminderTextBox.Text.Trim();
            if (text.Length == 0)
            {
                _calendarFeedback.Text = "请输入提醒内容";
                _calendarFeedback.Visibility = Visibility.Visible;
                return;
            }
            TimeSpan time;
            if (!TimeSpan.TryParse(_reminderTimeBox.Text, out time) || time.TotalHours >= 24 || time.TotalMinutes < 0)
            {
                _calendarFeedback.Text = "时间格式为 HH:mm";
                _calendarFeedback.Visibility = Visibility.Visible;
                return;
            }
            DateTime due = _selectedDate.Date + time;
            if (due <= DateTime.Now.AddSeconds(5))
            {
                _calendarFeedback.Text = "提醒时间需要晚于现在";
                _calendarFeedback.Visibility = Visibility.Visible;
                return;
            }
            ReminderItem item = new ReminderItem();
            item.Due = due;
            item.Text = text;
            item.Notified = false;
            _reminders.Add(item);
            SaveReminders();
            _reminderTextBox.Text = "";
            RefreshCalendar();
        }

        private void CheckReminders()
        {
            List<ReminderItem> due = new List<ReminderItem>();
            DateTime now = DateTime.Now;
            foreach (ReminderItem item in _reminders)
            {
                if (item.Notified || item.Due > now) continue;
                item.Notified = true;
                due.Add(item);
            }
            if (due.Count == 0) return;
            SaveReminders();
            foreach (ReminderItem item in due)
            {
                SystemSounds.Exclamation.Play();
                AlertInfo alert = new AlertInfo();
                alert.App = "日程提醒";
                alert.Title = item.Due.ToString("HH:mm") + " 的提醒";
                alert.Body = item.Text;
                alert.Time = "刚刚";
                alert.Icon = Ico.Clock;
                alert.Read = false;
                _notificationHistory.Insert(0, alert);
                _unreadCount = Math.Min(9, _unreadCount + 1);
                RefreshNotificationCenter();
                UpdateUnreadIndicators();
                ShowCustomPreview(alert);
            }
        }

        private static string RemindersFile
        {
            get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AppPaths.SettingsFile), "reminders.txt"); }
        }

        private void LoadReminders()
        {
            try
            {
                string path = RemindersFile;
                if (!File.Exists(path)) return;
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;
                    long ticks;
                    if (!long.TryParse(line.Substring(0, tab), out ticks)) continue;
                    string text = line.Substring(tab + 1).Trim();
                    if (text.Length == 0) continue;
                    ReminderItem item = new ReminderItem();
                    item.Due = new DateTime(ticks);
                    item.Text = text;
                    item.Notified = item.Due <= DateTime.Now;
                    _reminders.Add(item);
                }
            }
            catch (Exception ex) { Log.Error("LoadReminders", ex); }
        }

        private void SaveReminders()
        {
            try
            {
                string path = RemindersFile;
                string dir = System.IO.Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                List<string> lines = new List<string>();
                foreach (ReminderItem item in _reminders)
                {
                    lines.Add(item.Due.Ticks.ToString() + "\t" + item.Text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " "));
                }
                File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
            }
            catch (Exception ex) { Log.Error("SaveReminders", ex); }
        }

        private void RefreshTransferList()
        {
            if (_transferList == null) return;
            _transferList.Children.Clear();
            bool empty = _transferFiles.Count == 0;
            if (_transferEmpty != null) _transferEmpty.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            _transferList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            for (int i = 0; i < _transferFiles.Count; i++)
            {
                string path = _transferFiles[i];
                Border card = new Border();
                card.Height = 42;
                card.CornerRadius = new CornerRadius(10);
                card.Background = Ui.Alpha(Colors.White, 4);
                card.BorderBrush = Ui.Alpha(Colors.White, 7);
                card.BorderThickness = new Thickness(1);
                card.Padding = new Thickness(9, 0, 8, 0);
                card.Margin = new Thickness(0, 0, 0, 6);
                card.Cursor = Cursors.Hand;
                Grid row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                card.Child = row;
                string ext = System.IO.Path.GetExtension(path);
                string iconData = (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".webp") ? Ico.Image : Ico.Folder;
                FrameworkElement icon = Ui.Icon(iconData, Ui.AccentMutedBrush, 14, 1.4);
                icon.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(icon);
                StackPanel text = new StackPanel();
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Margin = new Thickness(4, 0, 4, 0);
                TextBlock name = Ui.Text(System.IO.Path.GetFileName(path), 8.1, FontWeights.Medium, Ui.TextBrush);
                name.TextTrimming = TextTrimming.CharacterEllipsis;
                text.Children.Add(name);
                FileInfo info = new FileInfo(path);
                TextBlock size = Ui.Text(info.Exists ? TextUtil.FormatBytes((ulong)info.Length) : "", 6.9, FontWeights.Normal, Ui.FaintBrush);
                size.Margin = new Thickness(0, 1, 0, 0);
                text.Children.Add(size);
                Grid.SetColumn(text, 1);
                row.Children.Add(text);
                Border remove = Ui.IconButton(Ico.Close, "移出中转站", 18, null);
                remove.HorizontalAlignment = HorizontalAlignment.Right;
                remove.VerticalAlignment = VerticalAlignment.Center;
                string captured = path;
                remove.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _transferFiles.Remove(captured);
                    RefreshTransferList();
                };
                Grid.SetColumn(remove, 2);
                row.Children.Add(remove);
                card.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    try { Process.Start(new ProcessStartInfo(captured) { UseShellExecute = true }); } catch { }
                };
                _transferList.Children.Add(card);
            }
        }

        private void PickTransferFiles()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Title = "添加到中转站";
            if (dialog.ShowDialog(this) == true) AddTransferFiles(dialog.FileNames);
        }

        private void AddTransferFiles(IEnumerable<string> paths)
        {
            if (paths == null) return;
            bool added = false;
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                if (_transferFiles.Contains(path)) continue;
                _transferFiles.Insert(0, path);
                added = true;
            }
            while (_transferFiles.Count > 8) _transferFiles.RemoveAt(_transferFiles.Count - 1);
            if (!added) return;
            RefreshTransferList();
            if (_mode == IslandMode.Expanded) ShowActivity(PageTransfer, true);
            else Expand(PageTransfer);
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            AddTransferFiles(files);
            e.Handled = true;
        }
        private Grid BuildMusicView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(162) });
            view.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid artWrap = new Grid();
            artWrap.VerticalAlignment = VerticalAlignment.Center;
            artWrap.Margin = new Thickness(0, 0, 10, 0);
            view.Children.Add(artWrap);

            _albumArt = new Grid();
            _albumArt.Width = 152;
            _albumArt.Height = 152;
            _albumArt.Clip = new RectangleGeometry(new Rect(0, 0, 152, 152), 28, 28);
            _albumArt.Background = CreateArtworkBrush(Tracks[0]);
            DropShadowEffect artShadow = new DropShadowEffect();
            artShadow.Color = Colors.Black;
            artShadow.BlurRadius = 24;
            artShadow.ShadowDepth = 8;
            artShadow.Direction = 270;
            artShadow.Opacity = 0.42;
            _albumArt.Effect = artShadow;
            artWrap.Children.Add(_albumArt);

            Ellipse artGlow = new Ellipse();
            artGlow.Width = 162;
            artGlow.Height = 162;
            artGlow.Fill = new RadialGradientBrush(Color.FromArgb(38, 255, 255, 255), Color.FromArgb(0, 255, 255, 255));
            artGlow.HorizontalAlignment = HorizontalAlignment.Left;
            artGlow.VerticalAlignment = VerticalAlignment.Top;
            artGlow.Margin = new Thickness(-50, -66, 0, 0);
            artGlow.IsHitTestVisible = false;
            _albumArt.Children.Add(artGlow);

            Grid disc = new Grid();
            _albumDisc = disc;
            disc.Width = 106;
            disc.Height = 106;
            disc.HorizontalAlignment = HorizontalAlignment.Center;
            disc.VerticalAlignment = VerticalAlignment.Center;
            _discRotate = new RotateTransform(0);
            disc.RenderTransform = _discRotate;
            disc.RenderTransformOrigin = new Point(0.5, 0.5);

            Ellipse vinyl = new Ellipse();
            RadialGradientBrush vinylBrush = new RadialGradientBrush();
            vinylBrush.GradientOrigin = new Point(0.34, 0.28);
            vinylBrush.Center = new Point(0.5, 0.5);
            vinylBrush.RadiusX = 0.65;
            vinylBrush.RadiusY = 0.65;
            vinylBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 38, 49, 43), 0));
            vinylBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 13, 18, 15), 0.52));
            vinylBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 3, 6, 5), 1));
            vinylBrush.Freeze();
            vinyl.Fill = vinylBrush;
            vinyl.Stroke = Ui.Alpha(Colors.White, 18);
            vinyl.StrokeThickness = 1;
            disc.Children.Add(vinyl);

            Ellipse groove1 = MakeGroove(91);
            Ellipse groove2 = MakeGroove(76);
            Ellipse groove3 = MakeGroove(61);
            disc.Children.Add(groove1);
            disc.Children.Add(groove2);
            disc.Children.Add(groove3);

            _albumDiscLabel = new Border();
            _albumDiscLabel.Width = 29;
            _albumDiscLabel.Height = 29;
            _albumDiscLabel.CornerRadius = new CornerRadius(14.5);
            _albumDiscLabel.Background = Ui.AccentBrush;
            _albumDiscLabel.BorderBrush = Ui.Alpha(Colors.White, 60);
            _albumDiscLabel.BorderThickness = new Thickness(1);
            _albumDiscLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _albumDiscLabel.VerticalAlignment = VerticalAlignment.Center;
            TextBlock labelH = Ui.Text("H", 11.5, FontWeights.Bold, Ui.InkBrush, Ui.DisplayFont);
            labelH.HorizontalAlignment = HorizontalAlignment.Center;
            labelH.VerticalAlignment = VerticalAlignment.Center;
            _albumDiscLabel.Child = labelH;
            disc.Children.Add(_albumDiscLabel);

            Ellipse shine = new Ellipse();
            shine.Width = 30;
            shine.Height = 15;
            shine.Fill = new LinearGradientBrush(Color.FromArgb(82, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 40);
            shine.HorizontalAlignment = HorizontalAlignment.Left;
            shine.VerticalAlignment = VerticalAlignment.Top;
            shine.Margin = new Thickness(17, 13, 0, 0);
            shine.IsHitTestVisible = false;
            disc.Children.Add(shine);

            _albumArt.Children.Add(disc);

            _albumStamp = Ui.Text("HAVEGET", 6.4, FontWeights.Medium, Ui.Alpha(Colors.White, 160));
            _albumStamp.HorizontalAlignment = HorizontalAlignment.Right;
            _albumStamp.VerticalAlignment = VerticalAlignment.Bottom;
            _albumStamp.Margin = new Thickness(0, 0, 10, 8);
            _albumArt.Children.Add(_albumStamp);

            _albumImage = new Image();
            _albumImage.Width = 152;
            _albumImage.Height = 152;
            _albumImage.Stretch = Stretch.Uniform;
            _albumImage.HorizontalAlignment = HorizontalAlignment.Center;
            _albumImage.VerticalAlignment = VerticalAlignment.Center;
            _albumImage.Visibility = Visibility.Collapsed;
            _albumImage.IsHitTestVisible = false;
            RenderOptions.SetBitmapScalingMode(_albumImage, BitmapScalingMode.HighQuality);
            _albumArt.Children.Add(_albumImage);

            // Track information ------------------------------------------
            Grid info = new Grid();
            info.VerticalAlignment = VerticalAlignment.Center;
            info.Height = 152;
            Grid.SetColumn(info, 1);
            view.Children.Add(info);
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            info.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _musicTitle = Ui.Text("未在播放", 23, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
            _musicTitle.TextTrimming = TextTrimming.CharacterEllipsis;
            info.Children.Add(_musicTitle);

            _musicArtist = Ui.Text("打开播放器后自动同步", 10.5, FontWeights.Normal, Ui.SubBrush);
            _musicArtist.Margin = new Thickness(0, 2, 0, 0);
            _musicArtist.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetRow(_musicArtist, 1);
            info.Children.Add(_musicArtist);

            Grid progressWrap = new Grid();
            progressWrap.VerticalAlignment = VerticalAlignment.Top;
            progressWrap.Margin = new Thickness(0, 8, 0, 0);
            Grid.SetRow(progressWrap, 2);
            info.Children.Add(progressWrap);

            _progressTrack = new Border();
            _progressTrack.Height = 18;
            _progressTrack.Background = Brushes.Transparent;
            _progressTrack.Cursor = Cursors.Hand;
            _progressTrack.ToolTip = "点击或拖动调整播放进度";
            _progressTrack.MouseLeftButtonDown += OnProgressDown;
            _progressTrack.MouseMove += OnProgressMove;
            _progressTrack.MouseLeftButtonUp += OnProgressUp;
            _progressTrack.LostMouseCapture += delegate { _scrubbing = false; };
            progressWrap.Children.Add(_progressTrack);

            Grid trackLine = new Grid();
            trackLine.Height = 5;
            trackLine.VerticalAlignment = VerticalAlignment.Center;
            Border bgTrack = new Border();
            bgTrack.Background = Ui.Alpha(Colors.White, 28);
            bgTrack.CornerRadius = new CornerRadius(2.5);
            trackLine.Children.Add(bgTrack);
            Border fill = new Border();
            fill.Background = Ui.TextBrush;
            fill.CornerRadius = new CornerRadius(2.5);
            fill.HorizontalAlignment = HorizontalAlignment.Stretch;
            _progressScale = new ScaleTransform(0.001, 1);
            fill.RenderTransform = _progressScale;
            fill.RenderTransformOrigin = new Point(0, 0.5);
            trackLine.Children.Add(fill);
            _progressTrack.Child = trackLine;

            Grid times = new Grid();
            times.Margin = new Thickness(0, 1, 0, 0);
            progressWrap.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            progressWrap.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(progressWrap, 2);

            _timeNow = Ui.Text("00:00", 8.0, FontWeights.Normal, Ui.FaintBrush, Ui.DisplayFont);
            Ui.Tabular(_timeNow);
            times.Children.Add(_timeNow);
            _timeTotal = Ui.Text("00:00", 8.0, FontWeights.Normal, Ui.FaintBrush, Ui.DisplayFont);
            _timeTotal.HorizontalAlignment = HorizontalAlignment.Right;
            Ui.Tabular(_timeTotal);
            times.Children.Add(_timeTotal);
            Grid.SetRow(times, 1);
            progressWrap.Children.Add(times);

            StackPanel controls = new StackPanel();
            controls.Orientation = Orientation.Horizontal;
            controls.HorizontalAlignment = HorizontalAlignment.Center;
            controls.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(controls, 4);
            info.Children.Add(controls);

            Border previous = MakeTransportButton(Ico.Prev, "上一首", false, delegate { ChangeTrack(-1); });
            previous.Margin = new Thickness(0, 0, 14, 0);
            controls.Children.Add(previous);

            _playButton = new Border();
            _playButton.Width = 42;
            _playButton.Height = 42;
            _playButton.CornerRadius = new CornerRadius(21);
            _playButton.Background = Ui.TextBrush;
            _playButton.Cursor = Cursors.Hand;
            _playButton.ToolTip = "暂停";
            _playButton.Margin = new Thickness(0, 0, 14, 0);
            _playIconHost = new Grid();
            _playButton.Child = _playIconHost;
            _playButton.MouseEnter += delegate
            {
                _playButton.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.86, new Duration(TimeSpan.FromMilliseconds(120))));
            };
            _playButton.MouseLeave += delegate
            {
                _playButton.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, new Duration(TimeSpan.FromMilliseconds(120))));
            };
            _playButton.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                TogglePlay();
            };
            controls.Children.Add(_playButton);

            Border next = MakeTransportButton(Ico.Next, "下一首", false, delegate { ChangeTrack(1); });
            controls.Children.Add(next);

            return view;
        }
        private static Ellipse MakeGroove(double size)
        {
            Ellipse e = new Ellipse();
            e.Width = size;
            e.Height = size;
            e.Stroke = Ui.Alpha(Colors.White, 13);
            e.StrokeThickness = 1;
            e.HorizontalAlignment = HorizontalAlignment.Center;
            e.VerticalAlignment = VerticalAlignment.Center;
            return e;
        }

        private Border MakeTransportButton(string icon, string tip, bool subtle, Action click)
        {
            Grid g = new Grid();
            FrameworkElement iv = Ui.Icon(icon, subtle ? Ui.FaintBrush : Ui.SubBrush, 14, 1.6);
            iv.HorizontalAlignment = HorizontalAlignment.Center;
            iv.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(iv);
            Border b = new Border();
            b.Width = 30;
            b.Height = 30;
            b.CornerRadius = new CornerRadius(15);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Hand;
            b.ToolTip = tip;
            b.Child = g;
            b.MouseEnter += delegate { b.Background = Ui.Alpha(Colors.White, 8); };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            return b;
        }

        private Grid BuildSettingsView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;

            StackPanel root = new StackPanel();
            root.Margin = new Thickness(0, 0, 2, 0);
            view.Children.Add(root);

            Grid header = new Grid();
            header.Height = 28;
            TextBlock title = Ui.Text("设置", 16, FontWeights.SemiBold, Ui.TextBrush);
            title.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(title);
            Border versionHit = new Border();
            versionHit.HorizontalAlignment = HorizontalAlignment.Right;
            versionHit.VerticalAlignment = VerticalAlignment.Center;
            versionHit.Padding = new Thickness(8, 4, 0, 4);
            versionHit.Cursor = Cursors.Hand;
            versionHit.Background = Brushes.Transparent;
            versionHit.ToolTip = "v" + ProductVersion;
            versionHit.MouseLeftButtonUp += OnVersionTapped;
            TextBlock versionValue = Ui.Text("v" + ProductVersion, 9.6, FontWeights.SemiBold, Ui.SubBrush, Ui.DisplayFont);
            versionHit.Child = versionValue;
            header.Children.Add(versionHit);
            root.Children.Add(header);

            Grid themeRow = new Grid();
            themeRow.Margin = new Thickness(0, 7, 0, 0);
            themeRow.Height = 24;
            TextBlock themeLabel = Ui.Text("主题", 8.2, FontWeights.Normal, Ui.SubBrush);
            themeLabel.VerticalAlignment = VerticalAlignment.Center;
            themeRow.Children.Add(themeLabel);
            StackPanel swatches = new StackPanel();
            swatches.Orientation = Orientation.Horizontal;
            swatches.HorizontalAlignment = HorizontalAlignment.Right;
            swatches.VerticalAlignment = VerticalAlignment.Center;
            foreach (UiTheme theme in Ui.Themes)
            {
                Border swatch = new Border();
                swatch.Width = 22;
                swatch.Height = 22;
                swatch.CornerRadius = new CornerRadius(11);
                swatch.BorderThickness = new Thickness(1);
                swatch.BorderBrush = Brushes.Transparent;
                swatch.Background = Brushes.Transparent;
                swatch.Cursor = Cursors.Hand;
                swatch.ToolTip = theme.Name;
                swatch.Margin = new Thickness(6, 0, 0, 0);
                Ellipse dot = new Ellipse();
                dot.Width = 14;
                dot.Height = 14;
                dot.Fill = new SolidColorBrush(theme.Accent);
                dot.HorizontalAlignment = HorizontalAlignment.Center;
                dot.VerticalAlignment = VerticalAlignment.Center;
                swatch.Child = dot;
                string themeId = theme.Id;
                swatch.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ApplyThemeChoice(themeId);
                };
                swatches.Children.Add(swatch);
                _themeSwatches[theme.Id] = swatch;
            }
            themeRow.Children.Add(swatches);
            root.Children.Add(themeRow);
            RefreshThemeSwatches();

            Grid preferenceRow = new Grid();
            preferenceRow.Margin = new Thickness(0, 7, 0, 0);
            preferenceRow.Height = 24;
            preferenceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            preferenceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.Children.Add(preferenceRow);

            Grid startupRow = new Grid();
            startupRow.Margin = new Thickness(0, 0, 6, 0);
            TextBlock startupLabel = Ui.Text("开机自启动", 8.2, FontWeights.Normal, Ui.TextBrush);
            startupLabel.VerticalAlignment = VerticalAlignment.Center;
            startupRow.Children.Add(startupLabel);
            Border startupToggle = MakeToggle(Startup.IsEnabled, delegate(bool on)
            {
                Startup.SetEnabled(on);
                _settings.StartWithWindows = on;
                _settings.Save();
            }, out _startupTrack, out _startupKnob);
            startupToggle.HorizontalAlignment = HorizontalAlignment.Right;
            startupToggle.VerticalAlignment = VerticalAlignment.Center;
            startupRow.Children.Add(startupToggle);
            preferenceRow.Children.Add(startupRow);

            Grid secondsRow = new Grid();
            secondsRow.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(secondsRow, 1);
            TextBlock secondsLabel = Ui.Text("显示秒数", 8.2, FontWeights.Normal, Ui.TextBrush);
            secondsLabel.VerticalAlignment = VerticalAlignment.Center;
            secondsRow.Children.Add(secondsLabel);
            Border secondsToggle = MakeToggle(_settings.ShowSeconds, delegate(bool on)
            {
                _settings.ShowSeconds = on;
                _settings.Save();
                RefreshClock();
            });
            secondsToggle.HorizontalAlignment = HorizontalAlignment.Right;
            secondsToggle.VerticalAlignment = VerticalAlignment.Center;
            secondsRow.Children.Add(secondsToggle);
            preferenceRow.Children.Add(secondsRow);

            TextBlock sectionLabel = Ui.Text("栏目", 8.2, FontWeights.Normal, Ui.SubBrush);
            sectionLabel.Margin = new Thickness(0, 8, 0, 2);
            root.Children.Add(sectionLabel);

            Grid sectionGrid = new Grid();
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int r = 0; r < 4; r++)
                sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            string[] labels = new string[] { "音乐", "通知", "系统状态", "剪贴板", "通知中心", "控制中心", "日历日程", "中转站" };
            int[] pages = new int[] { PageMusic, PageAlerts, PageStatus, PageClipboard, PageNotifyCenter, PageControl, PageCalendar, PageTransfer };
            bool[] states = new bool[] { _settings.ShowMusic, _settings.ShowAlerts, _settings.ShowStatus, _settings.ShowClipboard,
                                         _settings.ShowNotifyCenter, _settings.ShowControl, _settings.ShowCalendar, _settings.ShowTransfer };
            for (int i = 0; i < 8; i++)
            {
                Grid row = new Grid();
                row.Margin = new Thickness(i % 2 == 0 ? 0 : 5, 0, i % 2 == 0 ? 5 : 0, 0);
                TextBlock label = Ui.Text(labels[i], 8.0, FontWeights.Normal, Ui.SubBrush);
                label.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(label);
                int page = pages[i];
                Border toggle = MakeToggle(states[i], delegate(bool on) { SetPageEnabled(page, on); });
                toggle.HorizontalAlignment = HorizontalAlignment.Right;
                toggle.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(toggle);
                Grid.SetRow(row, i / 2);
                Grid.SetColumn(row, i % 2);
                sectionGrid.Children.Add(row);
            }
            root.Children.Add(sectionGrid);

            _devPasswordPanel = new Border();
            _devPasswordPanel.Visibility = Visibility.Collapsed;
            _devPasswordPanel.Margin = new Thickness(0, 9, 0, 0);
            _devPasswordPanel.Padding = new Thickness(11, 9, 11, 9);
            _devPasswordPanel.CornerRadius = new CornerRadius(11);
            _devPasswordPanel.Background = Ui.Alpha(Colors.White, 4);
            _devPasswordPanel.BorderBrush = Ui.AccentTint(32);
            _devPasswordPanel.BorderThickness = new Thickness(1);
            root.Children.Add(_devPasswordPanel);
            StackPanel passwordStack = new StackPanel();
            _devPasswordPanel.Child = passwordStack;
            TextBlock passwordLabel = Ui.Text("开发者密码", 8.0, FontWeights.Normal, Ui.SubBrush);
            passwordStack.Children.Add(passwordLabel);
            Grid passwordRow = new Grid();
            passwordRow.Margin = new Thickness(0, 5, 0, 0);
            passwordRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            passwordRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            passwordStack.Children.Add(passwordRow);
            _devPasswordBox = new PasswordBox();
            _devPasswordBox.Height = 27;
            _devPasswordBox.FontFamily = Ui.UIFont;
            _devPasswordBox.FontSize = 9.8;
            _devPasswordBox.Foreground = Ui.TextBrush;
            _devPasswordBox.Background = Ui.Alpha(Colors.White, 7);
            _devPasswordBox.BorderBrush = Ui.Alpha(Colors.White, 13);
            _devPasswordBox.BorderThickness = new Thickness(1);
            _devPasswordBox.Padding = new Thickness(7, 3, 7, 3);
            _devPasswordBox.CaretBrush = Ui.TextBrush;
            _devPasswordBox.PasswordChar = '●';
            _devPasswordBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    TryUnlockDeveloper();
                    e.Handled = true;
                }
            };
            passwordRow.Children.Add(_devPasswordBox);
            Border unlock = MakeSettingsButton("解锁", true, 46);
            unlock.Margin = new Thickness(7, 0, 0, 0);
            unlock.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                TryUnlockDeveloper();
            };
            Grid.SetColumn(unlock, 1);
            passwordRow.Children.Add(unlock);
            _devErrorText = Ui.Text("", 7.2, FontWeights.Normal, Ui.DangerBrush);
            _devErrorText.Margin = new Thickness(0, 4, 0, 0);
            _devErrorText.Visibility = Visibility.Collapsed;
            passwordStack.Children.Add(_devErrorText);

            _developerPanel = new StackPanel();
            _developerPanel.Visibility = _settings.DeveloperMode ? Visibility.Visible : Visibility.Collapsed;
            _developerPanel.Margin = new Thickness(0, 9, 0, 0);
            root.Children.Add(_developerPanel);

            TextBlock devTitle = Ui.Text("开发者参数", 9.2, FontWeights.SemiBold, Ui.AccentPaleBrush);
            _developerPanel.Children.Add(devTitle);

            _developerPanel.Children.Add(BuildDevStepper("X 偏移", " px", 4, -600, 600, "0",
                delegate { return _settings.IslandOffsetX; },
                delegate(double v) { _settings.IslandOffsetX = (int)Math.Round(v); }, out _devXValue));
            _developerPanel.Children.Add(BuildDevStepper("Y 偏移", " px", 4, -300, 600, "0",
                delegate { return _settings.IslandOffsetY; },
                delegate(double v) { _settings.IslandOffsetY = (int)Math.Round(v); }, out _devYValue));
            _developerPanel.Children.Add(BuildDevStepper("胶囊宽度", " px", 4, 140, 320, "0",
                delegate { return _settings.CapsuleWidth; },
                delegate(double v) { _settings.CapsuleWidth = Math.Round(v); }, out _devWidthValue));
            _developerPanel.Children.Add(BuildDevStepper("胶囊高度", " px", 2, 36, 72, "0",
                delegate { return _settings.CapsuleHeight; },
                delegate(double v) { _settings.CapsuleHeight = Math.Round(v); }, out _devHeightValue));
            _developerPanel.Children.Add(BuildDevStepper("胶囊时间字号", " px", 1, 10, 26, "0.#",
                delegate { return _settings.CompactTimeFontSize; },
                delegate(double v) { _settings.CompactTimeFontSize = Math.Round(v * 2.0) / 2.0; }, out _devFontValue));

            Grid devActions = new Grid();
            devActions.Margin = new Thickness(0, 6, 0, 0);
            devActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            devActions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _developerPanel.Children.Add(devActions);
            Border reset = MakeSettingsButton("重置全部参数", true, 128);
            reset.HorizontalAlignment = HorizontalAlignment.Left;
            reset.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _settings.IslandOffsetX = 0;
                _settings.IslandOffsetY = 0;
                _settings.CapsuleWidth = 176.0;
                _settings.CapsuleHeight = 48.0;
                _settings.CompactTimeFontSize = 15.5;
                _settings.Save();
                RefreshDevValues();
                ApplyDeveloperSettings();
            };
            devActions.Children.Add(reset);
            Border lockMode = MakeSettingsButton("关闭开发者模式", false, 112);
            lockMode.Margin = new Thickness(7, 0, 0, 0);
            lockMode.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _settings.DeveloperMode = false;
                _settings.Save();
                _developerPanel.Visibility = Visibility.Collapsed;
                _devPasswordPanel.Visibility = Visibility.Collapsed;
                _versionTapCount = 0;
                if (_mode == IslandMode.Expanded) ApplyExpandedHeight();
            };
            Grid.SetColumn(lockMode, 1);
            devActions.Children.Add(lockMode);

            return view;
        }

        private Border MakeToggle(bool initial, Action<bool> changed)
        {
            Border ignoredTrack;
            Ellipse ignoredKnob;
            return MakeToggle(initial, changed, out ignoredTrack, out ignoredKnob);
        }

        private Border MakeToggle(bool initial, Action<bool> changed, out Border track, out Ellipse knob)
        {
            bool state = initial;
            Border localTrack = new Border();
            localTrack.Width = 32;
            localTrack.Height = 18;
            localTrack.CornerRadius = new CornerRadius(9);
            localTrack.BorderThickness = new Thickness(1);
            localTrack.Cursor = Cursors.Hand;
            Grid host = new Grid();
            localTrack.Child = host;
            Ellipse localKnob = new Ellipse();
            localKnob.Width = 12;
            localKnob.Height = 12;
            localKnob.HorizontalAlignment = HorizontalAlignment.Left;
            localKnob.Margin = new Thickness(2, 0, 0, 0);
            localKnob.VerticalAlignment = VerticalAlignment.Center;
            host.Children.Add(localKnob);
            Action refresh = delegate
            {
                localTrack.Background = state ? Ui.AccentTint(95) : Ui.Alpha(Colors.White, 9);
                localTrack.BorderBrush = state ? Ui.AccentTint(145) : Ui.Alpha(Colors.White, 12);
                localKnob.Fill = state ? Ui.InkBrush : Ui.SubBrush;
                localKnob.HorizontalAlignment = state ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                localKnob.Margin = state ? new Thickness(0, 0, 2, 0) : new Thickness(2, 0, 0, 0);
            };
            localTrack.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                state = !state;
                refresh();
                if (changed != null) changed(state);
            };
            refresh();
            track = localTrack;
            knob = localKnob;
            return localTrack;
        }

        private void RefreshThemeSwatches()
        {
            foreach (KeyValuePair<string, Border> pair in _themeSwatches)
            {
                bool active = string.Equals(pair.Key, Ui.CurrentTheme.Id, StringComparison.OrdinalIgnoreCase);
                pair.Value.BorderBrush = active ? Ui.AccentPaleBrush : Brushes.Transparent;
                pair.Value.Background = active ? Ui.AccentTint(26) : Brushes.Transparent;
            }
        }

        private void ApplyThemeChoice(string themeId)
        {
            _settings.ThemeId = themeId;
            _settings.Save();
            Ui.ApplyTheme(themeId);
            RefreshThemeSwatches();
            RefreshThemeVisuals();
        }

        private void RefreshThemeVisuals()
        {
            UpdatePageDots();
            if (_statusBanner != null) _statusBanner.Background = Ui.AccentTint(15);
            if (_statusBanner != null) _statusBanner.BorderBrush = Ui.AccentTint(35);
        }
        private Border BuildSettingsTip(string label, string value)
        {
            Border row = new Border();
            row.Height = 22;
            Grid grid = new Grid();
            row.Child = grid;
            TextBlock left = Ui.Text(label, 8.0, FontWeights.Normal, Ui.FaintBrush);
            left.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(left);
            TextBlock right = Ui.Text(value, 8.0, FontWeights.Normal, Ui.SubBrush);
            right.HorizontalAlignment = HorizontalAlignment.Right;
            right.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(right);
            return row;
        }

        private Border MakeSettingsButton(string text, bool accent, double width)
        {
            Border b = new Border();
            b.Width = width;
            b.Height = 26;
            b.CornerRadius = new CornerRadius(8);
            b.Background = accent ? Ui.AccentTint(22) : Ui.Alpha(Colors.White, 6);
            b.BorderBrush = accent ? Ui.AccentTint(55) : Ui.Alpha(Colors.White, 8);
            b.BorderThickness = new Thickness(1);
            b.Cursor = Cursors.Hand;
            TextBlock label = Ui.Text(text, 8.0, FontWeights.Medium, accent ? Ui.AccentPaleBrush : Ui.SubBrush);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            b.Child = label;
            b.MouseEnter += delegate { b.Opacity = 0.84; };
            b.MouseLeave += delegate { b.Opacity = 1.0; };
            return b;
        }

        private Grid BuildDevStepper(string label, string unit, double step, double min, double max, string format,
            Func<double> read, Action<double> write, out TextBlock value)
        {
            Grid row = new Grid();
            row.Height = 30;
            row.Margin = new Thickness(0, 2, 0, 0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });

            TextBlock name = Ui.Text(label, 8.2, FontWeights.Normal, Ui.SubBrush);
            name.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(name);

            Border minus = MakeStepButton("−");
            minus.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(minus, 1);
            row.Children.Add(minus);

            TextBlock display = Ui.Text("", 8.8, FontWeights.Medium, Ui.TextBrush, Ui.DisplayFont);
            display.HorizontalAlignment = HorizontalAlignment.Center;
            display.VerticalAlignment = VerticalAlignment.Center;
            Ui.Tabular(display);
            Grid.SetColumn(display, 2);
            row.Children.Add(display);
            value = display;

            Border plus = MakeStepButton("+");
            plus.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(plus, 3);
            row.Children.Add(plus);

            Action refresh = delegate
            {
                display.Text = read().ToString(format) + unit;
            };
            minus.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                double next = Math.Max(min, Math.Min(max, read() - step));
                write(next);
                _settings.Save();
                refresh();
                ApplyDeveloperSettings();
            };
            plus.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                double next = Math.Max(min, Math.Min(max, read() + step));
                write(next);
                _settings.Save();
                refresh();
                ApplyDeveloperSettings();
            };
            refresh();
            return row;
        }

        private Border MakeStepButton(string glyph)
        {
            Border b = new Border();
            b.Width = 22;
            b.Height = 22;
            b.CornerRadius = new CornerRadius(7);
            b.Background = Ui.Alpha(Colors.White, 6);
            b.Cursor = Cursors.Hand;
            TextBlock label = Ui.Text(glyph, 11, FontWeights.Medium, Ui.SubBrush, Ui.DisplayFont);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            b.Child = label;
            b.MouseEnter += delegate { b.Background = Ui.Alpha(Colors.White, 11); };
            b.MouseLeave += delegate { b.Background = Ui.Alpha(Colors.White, 6); };
            return b;
        }

        private void OnVersionTapped(object sender, MouseButtonEventArgs e)
        {
            if (_settings.DeveloperMode) return;
            _versionTapCount++;
            if (_versionTapCount >= 8)
            {
                _devPasswordPanel.Visibility = Visibility.Visible;
                if (_devErrorText != null) _devErrorText.Visibility = Visibility.Collapsed;
                if (_devPasswordBox != null)
                {
                    _devPasswordBox.Password = "";
                    _devPasswordBox.Focus();
                }
                if (_mode == IslandMode.Expanded) ApplyExpandedHeight();
            }
            e.Handled = true;
        }

        private void TryUnlockDeveloper()
        {
            if (_devPasswordBox == null) return;
            if (_devPasswordBox.Password == "HG-KYAMP4P9")
            {
                _settings.DeveloperMode = true;
                _settings.Save();
                _devPasswordPanel.Visibility = Visibility.Collapsed;
                _developerPanel.Visibility = Visibility.Visible;
                _versionTapCount = 0;
                RefreshDevValues();
                if (_mode == IslandMode.Expanded) ApplyExpandedHeight();
            }
            else
            {
                _devErrorText.Text = "密码不正确";
                _devErrorText.Visibility = Visibility.Visible;
                _devPasswordBox.SelectAll();
                _devPasswordBox.Focus();
            }
        }

        private void RefreshDevValues()
        {
            if (_devXValue != null) _devXValue.Text = _settings.IslandOffsetX + " px";
            if (_devYValue != null) _devYValue.Text = _settings.IslandOffsetY + " px";
            if (_devFontValue != null) _devFontValue.Text = _settings.CompactTimeFontSize.ToString("0.#") + " px";
            if (_devWidthValue != null) _devWidthValue.Text = _settings.CapsuleWidth.ToString("0") + " px";
            if (_devHeightValue != null) _devHeightValue.Text = _settings.CapsuleHeight.ToString("0") + " px";
        }

        private void ApplyDeveloperSettings()
        {
            if (_compactClock != null)
            {
                _compactClock.FontSize = _settings.CompactTimeFontSize;
            }
            _compactWidth = Math.Max(140.0, Math.Min(320.0, _settings.CapsuleWidth));
            _compactHeight = Math.Max(36.0, Math.Min(72.0, _settings.CapsuleHeight));
            if (_compactView != null)
            {
                _compactView.Width = _compactWidth;
                _compactView.Height = _compactHeight;
            }
            if (_mode == IslandMode.Compact && _panel != null)
            {
                _panel.Width = _compactWidth;
                _panel.Height = _compactHeight;
                UpdatePanelClip();
            }
            PositionAtTop();
        }
        private Border BuildStatusStrip()
        {
            Border strip = new Border();
            strip.Height = 52;
            strip.CornerRadius = new CornerRadius(13);
            strip.Background = Ui.Alpha(Colors.White, 4);
            strip.BorderBrush = Ui.Alpha(Colors.White, 7);
            strip.BorderThickness = new Thickness(1);

            Grid grid = new Grid();
            for (int i = 0; i < 4; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            strip.Child = grid;

            Border network = BuildStatusCell("网络", Ico.Wifi, true, 0);
            network.Margin = new Thickness(0);
            grid.Children.Add(network);

            Border cpu = BuildStatusCell("CPU", Ico.Cpu, false, 1);
            cpu.Margin = new Thickness(0);
            Grid.SetColumn(cpu, 1);
            grid.Children.Add(cpu);

            Border memory = BuildStatusCell("内存", Ico.Memory, false, 2);
            memory.Margin = new Thickness(0);
            Grid.SetColumn(memory, 2);
            grid.Children.Add(memory);

            Border battery = BuildStatusCell("电量", Ico.Battery, false, 3);
            battery.Margin = new Thickness(0);
            Grid.SetColumn(battery, 3);
            grid.Children.Add(battery);

            return strip;
        }

        private Border BuildStatusCell(string label, string iconData, bool network, int column)
        {
            Border cell = new Border();
            cell.Padding = new Thickness(10, 7, 8, 7);
            cell.Background = Brushes.Transparent;

            Grid layer = new Grid();
            cell.Child = layer;
            if (column > 0)
            {
                Border divider = new Border();
                divider.Width = 1;
                divider.Background = Ui.Alpha(Colors.White, 8);
                divider.HorizontalAlignment = HorizontalAlignment.Left;
                divider.Margin = new Thickness(0, 8, 0, 8);
                layer.Children.Add(divider);
            }

            StackPanel content = new StackPanel();
            content.VerticalAlignment = VerticalAlignment.Center;
            layer.Children.Add(content);

            StackPanel head = new StackPanel();
            head.Orientation = Orientation.Horizontal;
            FrameworkElement icon = Ui.Icon(iconData, Ui.FaintBrush, 9, 1.35);
            icon.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(icon);
            TextBlock name = Ui.Text(label, 7.1, FontWeights.Normal, Ui.FaintBrush);
            name.Margin = new Thickness(4, 0, 0, 0);
            name.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(name);
            content.Children.Add(head);

            StackPanel valueLine = new StackPanel();
            valueLine.Orientation = Orientation.Horizontal;
            valueLine.Margin = new Thickness(0, 3, 0, 0);
            if (network)
            {
                _networkDot = Ui.Dot(4, Ui.AccentBrush);
                _networkDot.VerticalAlignment = VerticalAlignment.Center;
                _networkDot.Margin = new Thickness(0, 0, 5, 0);
                valueLine.Children.Add(_networkDot);
                _networkValue = Ui.Text("--", 11.4, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
                _networkValue.VerticalAlignment = VerticalAlignment.Center;
                valueLine.Children.Add(_networkValue);
            }
            else if (column == 1)
            {
                _cpuValue = Ui.Text("--", 11.4, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
                Ui.Tabular(_cpuValue);
                valueLine.Children.Add(_cpuValue);
            }
            else if (column == 2)
            {
                _memValue = Ui.Text("--", 11.4, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
                Ui.Tabular(_memValue);
                valueLine.Children.Add(_memValue);
            }
            else
            {
                _batteryValue = Ui.Text("--", 11.4, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
                Ui.Tabular(_batteryValue);
                valueLine.Children.Add(_batteryValue);
            }
            content.Children.Add(valueLine);
            return cell;
        }

        private Grid BuildAlertsView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;

            _alertCard = new Border();
            _alertCard.CornerRadius = new CornerRadius(18);
            _alertCard.Background = Ui.Alpha(Colors.White, 4);
            _alertCard.BorderBrush = Ui.Alpha(Colors.White, 8);
            _alertCard.BorderThickness = new Thickness(1);
            _alertCard.Padding = new Thickness(15, 14, 15, 13);
            view.Children.Add(_alertCard);

            Grid card = new Grid();
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _alertCard.Child = card;

            Grid top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Children.Add(top);

            _alertIconBox = new Border();
            _alertIconBox.Width = 36;
            _alertIconBox.Height = 36;
            _alertIconBox.CornerRadius = new CornerRadius(11);
            _alertIconBox.Background = Ui.AccentTint(22);
            _alertIconBox.BorderBrush = Ui.AccentTint(38);
            _alertIconBox.BorderThickness = new Thickness(1);
            _alertIconBox.VerticalAlignment = VerticalAlignment.Top;
            _alertIconHost = new Grid();
            _alertIconBox.Child = _alertIconHost;
            top.Children.Add(_alertIconBox);

            StackPanel appStack = new StackPanel();
            appStack.VerticalAlignment = VerticalAlignment.Center;
            appStack.Margin = new Thickness(9, 0, 0, 0);
            Grid appLine = new Grid();
            _alertApp = Ui.Text("HAVEGET", 9.0, FontWeights.Medium, Ui.TextBrush);
            appLine.Children.Add(_alertApp);
            _alertTime = Ui.Text("刚刚", 7.6, FontWeights.Normal, Ui.FaintBrush);
            _alertTime.HorizontalAlignment = HorizontalAlignment.Right;
            _alertTime.VerticalAlignment = VerticalAlignment.Center;
            appLine.Children.Add(_alertTime);
            appStack.Children.Add(appLine);
            Grid.SetColumn(appStack, 1);
            top.Children.Add(appStack);

            _alertUnreadDot = new Border();
            _alertUnreadDot.Width = 6;
            _alertUnreadDot.Height = 6;
            _alertUnreadDot.CornerRadius = new CornerRadius(3);
            _alertUnreadDot.Background = Ui.AccentBrush;
            _alertUnreadDot.VerticalAlignment = VerticalAlignment.Center;
            _alertUnreadDot.Margin = new Thickness(8, 0, 1, 0);
            Grid.SetColumn(_alertUnreadDot, 2);
            top.Children.Add(_alertUnreadDot);

            _alertTitle = Ui.Text("通知标题", 13.4, FontWeights.SemiBold, Ui.TextBrush);
            _alertTitle.Margin = new Thickness(0, 9, 0, 0);
            _alertTitle.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetRow(_alertTitle, 1);
            card.Children.Add(_alertTitle);

            _alertBody = Ui.Text("", 9.3, FontWeights.Normal, Ui.SubBrush);
            _alertBody.Margin = new Thickness(0, 4, 0, 0);
            _alertBody.TextWrapping = TextWrapping.Wrap;
            _alertBody.LineHeight = 15;
            _alertBody.MaxHeight = 34;
            _alertBody.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetRow(_alertBody, 2);
            card.Children.Add(_alertBody);

            Grid actions = new Grid();
            actions.Margin = new Thickness(0, 10, 0, 0);
            Grid.SetRow(actions, 3);
            card.Children.Add(actions);

            StackPanel leftActions = new StackPanel();
            leftActions.Orientation = Orientation.Horizontal;
            actions.Children.Add(leftActions);

            _readButton = new Border();
            _readButton.Height = 25;
            _readButton.CornerRadius = new CornerRadius(8);
            _readButton.Background = Ui.AccentTint(24);
            _readButton.BorderBrush = Ui.AccentTint(60);
            _readButton.BorderThickness = new Thickness(1);
            _readButton.Padding = new Thickness(9, 0, 9, 0);
            _readButton.Cursor = Cursors.Hand;
            _readButtonText = Ui.Text("标记已读", 8.1, FontWeights.Medium, Ui.AccentPaleBrush);
            _readButtonText.VerticalAlignment = VerticalAlignment.Center;
            _readButton.Child = _readButtonText;
            _readButton.MouseEnter += delegate { if (_unreadCount > 0) _readButton.Background = Ui.AccentTint(36); };
            _readButton.MouseLeave += delegate { if (_unreadCount > 0) _readButton.Background = Ui.AccentTint(24); };
            _readButton.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (_unreadCount > 0) MarkAllRead();
            };
            leftActions.Children.Add(_readButton);

            _simulateButton = new Border();
            _simulateButton.Height = 25;
            _simulateButton.CornerRadius = new CornerRadius(8);
            _simulateButton.Background = Ui.Alpha(Colors.White, 6);
            _simulateButton.Padding = new Thickness(9, 0, 9, 0);
            _simulateButton.Cursor = Cursors.Hand;
            _simulateButton.Margin = new Thickness(6, 0, 0, 0);
            StackPanel replyContent = new StackPanel();
            replyContent.Orientation = Orientation.Horizontal;
            FrameworkElement replyIcon = Ui.Icon(Ico.Chat, Ui.SubBrush, 9, 1.4);
            replyIcon.VerticalAlignment = VerticalAlignment.Center;
            replyContent.Children.Add(replyIcon);
            _replyButtonText = Ui.Text("回复", 8.1, FontWeights.Normal, Ui.SubBrush);
            _replyButtonText.VerticalAlignment = VerticalAlignment.Center;
            _replyButtonText.Margin = new Thickness(4, 0, 0, 0);
            replyContent.Children.Add(_replyButtonText);
            _simulateButton.Child = replyContent;
            _simulateButton.MouseEnter += delegate { _simulateButton.Background = Ui.Alpha(Colors.White, 10); };
            _simulateButton.MouseLeave += delegate { _simulateButton.Background = Ui.Alpha(Colors.White, 6); };
            _simulateButton.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ReplyToCurrentNotification();
            };
            leftActions.Children.Add(_simulateButton);

            return view;
        }

        private Grid BuildStatusView()
        {
            Grid view = new Grid();
            view.ClipToBounds = true;
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _statusBanner = new Border();
            _statusBanner.CornerRadius = new CornerRadius(15);
            _statusBanner.Background = Ui.AccentTint(15);
            _statusBanner.BorderBrush = Ui.AccentTint(35);
            _statusBanner.BorderThickness = new Thickness(1);
            _statusBanner.Padding = new Thickness(12, 0, 12, 0);
            view.Children.Add(_statusBanner);

            Grid banner = new Grid();
            banner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(31) });
            banner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            banner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _statusBanner.Child = banner;

            Border iconBox = new Border();
            iconBox.Width = 30;
            iconBox.Height = 30;
            iconBox.CornerRadius = new CornerRadius(9);
            iconBox.Background = Ui.AccentTint(20);
            iconBox.VerticalAlignment = VerticalAlignment.Center;
            _statusIconHost = new Grid();
            iconBox.Child = _statusIconHost;
            banner.Children.Add(iconBox);

            StackPanel bannerText = new StackPanel();
            bannerText.VerticalAlignment = VerticalAlignment.Center;
            bannerText.Margin = new Thickness(10, 0, 0, 0);
            _statusTitle = Ui.Text("系统运行正常", 11.2, FontWeights.SemiBold, Ui.TextBrush);
            bannerText.Children.Add(_statusTitle);
            _statusDetail = Ui.Text("正在读取本机状态…", 8.0, FontWeights.Normal, Ui.SubBrush);
            _statusDetail.Margin = new Thickness(0, 2, 0, 0);
            _statusDetail.TextTrimming = TextTrimming.CharacterEllipsis;
            bannerText.Children.Add(_statusDetail);
            Grid.SetColumn(bannerText, 1);
            banner.Children.Add(bannerText);

            StackPanel live = new StackPanel();
            live.Orientation = Orientation.Horizontal;
            live.VerticalAlignment = VerticalAlignment.Center;
            _statusIconDot = Ui.Dot(4.5, Ui.AccentBrush);
            _statusIconDot.VerticalAlignment = VerticalAlignment.Center;
            live.Children.Add(_statusIconDot);
            _statusLive = Ui.Text("实时", 7.3, FontWeights.Normal, Ui.FaintBrush);
            _statusLive.Margin = new Thickness(4, 0, 0, 0);
            _statusLive.VerticalAlignment = VerticalAlignment.Center;
            live.Children.Add(_statusLive);
            Grid.SetColumn(live, 2);
            banner.Children.Add(live);

            Grid metrics = new Grid();
            metrics.Margin = new Thickness(0, 8, 0, 0);
            metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metrics.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            metrics.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(metrics, 1);
            view.Children.Add(metrics);

            Border cpu = BuildMetricCard("CPU", Ico.Cpu, out _cpuValue, out _cpuSub, out _cpuFill);
            cpu.Margin = new Thickness(0, 0, 4, 4);
            metrics.Children.Add(cpu);
            Border mem = BuildMetricCard("内存", Ico.Memory, out _memValue, out _memSub, out _memFill);
            mem.Margin = new Thickness(4, 0, 0, 4);
            Grid.SetColumn(mem, 1);
            metrics.Children.Add(mem);
            Border disk = BuildMetricCard("磁盘 C:", Ico.Disk, out _diskValue, out _diskSub, out _diskFill);
            disk.Margin = new Thickness(0, 4, 4, 0);
            Grid.SetRow(disk, 1);
            metrics.Children.Add(disk);
            Border battery = BuildMetricCard("电量", Ico.Battery, out _batteryValue, out _batterySub, out _batteryFill);
            battery.Margin = new Thickness(4, 4, 0, 0);
            Grid.SetRow(battery, 1);
            Grid.SetColumn(battery, 1);
            metrics.Children.Add(battery);

            return view;
        }

        private Border BuildMetricCard(string label, string iconData, out TextBlock value, out TextBlock sub, out ScaleTransform fill)
        {
            Border card = new Border();
            card.CornerRadius = new CornerRadius(12);
            card.Background = Ui.Alpha(Colors.White, 4);
            card.BorderBrush = Ui.Alpha(Colors.White, 7);
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(10, 7, 10, 8);

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            card.Child = grid;

            Grid top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            FrameworkElement iv = Ui.Icon(iconData, Ui.AccentMutedBrush, 9, 1.4);
            iv.VerticalAlignment = VerticalAlignment.Center;
            top.Children.Add(iv);
            TextBlock name = Ui.Text(label, 8.0, FontWeights.Normal, Ui.FaintBrush);
            name.Margin = new Thickness(4, 0, 0, 0);
            name.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(name, 1);
            top.Children.Add(name);
            grid.Children.Add(top);

            Grid valueRow = new Grid();
            Grid.SetRow(valueRow, 1);
            grid.Children.Add(valueRow);
            value = Ui.Text("--", 16, FontWeights.SemiBold, Ui.TextBrush, Ui.DisplayFont);
            value.VerticalAlignment = VerticalAlignment.Center;
            Ui.Tabular(value);
            valueRow.Children.Add(value);
            sub = Ui.Text("", 7.0, FontWeights.Normal, Ui.FaintBrush);
            sub.HorizontalAlignment = HorizontalAlignment.Right;
            sub.VerticalAlignment = VerticalAlignment.Center;
            sub.TextTrimming = TextTrimming.CharacterEllipsis;
            sub.Margin = new Thickness(8, 1, 0, 0);
            valueRow.Children.Add(sub);

            Border track = new Border();
            track.Height = 3;
            track.CornerRadius = new CornerRadius(1.5);
            track.Background = Ui.Alpha(Colors.White, 10);
            track.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(track, 2);
            grid.Children.Add(track);
            Border bar = new Border();
            bar.CornerRadius = new CornerRadius(1.5);
            bar.Background = Ui.AccentBrush;
            fill = new ScaleTransform(0.001, 1);
            bar.RenderTransform = fill;
            bar.RenderTransformOrigin = new Point(0, 0.5);
            track.Child = bar;

            return card;
        }

        private Grid BuildPreviewView()
        {
            Grid view = new Grid();
            view.Width = PreviewWidth;
            view.Height = PreviewHeight;
            view.HorizontalAlignment = HorizontalAlignment.Center;
            view.VerticalAlignment = VerticalAlignment.Top;
            view.Cursor = Cursors.Hand;
            view.Background = Brushes.Transparent;
            view.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Expand(1);
            };
            view.MouseEnter += delegate
            {
                _previewMouseInside = true;
                if (_previewTimer != null) _previewTimer.Stop();
            };
            view.MouseLeave += delegate
            {
                _previewMouseInside = false;
                ArmPreviewTimer();
            };

            Grid inner = new Grid();
            inner.Margin = new Thickness(14, 12, 12, 12);
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            view.Children.Add(inner);

            Border iconBox = new Border();
            iconBox.Width = 38;
            iconBox.Height = 38;
            iconBox.CornerRadius = new CornerRadius(12);
            iconBox.Background = Ui.AccentTint(22);
            iconBox.BorderBrush = Ui.AccentTint(38);
            iconBox.BorderThickness = new Thickness(1);
            iconBox.VerticalAlignment = VerticalAlignment.Top;
            _previewIconHost = new Grid();
            iconBox.Child = _previewIconHost;
            inner.Children.Add(iconBox);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(10, 0, 4, 0);
            content.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(content, 1);
            inner.Children.Add(content);

            Grid top = new Grid();
            _previewApp = Ui.Text("HAVEGET", 9.2, FontWeights.Medium, Ui.TextBrush);
            top.Children.Add(_previewApp);
            _previewTime = Ui.Text("刚刚", 7.7, FontWeights.Normal, Ui.FaintBrush);
            _previewTime.HorizontalAlignment = HorizontalAlignment.Right;
            top.Children.Add(_previewTime);
            content.Children.Add(top);

            _previewTitle = Ui.Text("新通知", 12.0, FontWeights.SemiBold, Ui.TextBrush);
            _previewTitle.Margin = new Thickness(0, 3, 0, 0);
            _previewTitle.TextTrimming = TextTrimming.CharacterEllipsis;
            content.Children.Add(_previewTitle);

            _previewBody = Ui.Text("", 8.6, FontWeights.Normal, Ui.SubBrush);
            _previewBody.Margin = new Thickness(0, 2, 0, 0);
            _previewBody.TextTrimming = TextTrimming.CharacterEllipsis;
            content.Children.Add(_previewBody);

            Border close = Ui.IconButton(Ico.Close, "收起", 22, delegate { Collapse(); });
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(close, 2);
            inner.Children.Add(close);

            return view;
        }

        private void CreateSprings()
        {
            _widthSpring = new SpringAnimation(_compactWidth, 330, 34, delegate(double v)
            {
                _panel.Width = Math.Max(1, v);
                UpdatePanelClip();
            });
            _heightSpring = new SpringAnimation(_compactHeight, 330, 34, delegate(double v)
            {
                _panel.Height = Math.Max(1, v);
                UpdatePanelClip();
            });
            _heightSpring.Settled += OnPanelSizeSettled;
            _radiusSpring = new SpringAnimation(CompactRadius, 280, 38, delegate(double v)
            {
                _panel.CornerRadius = new CornerRadius(v);
                UpdatePanelClip();
            });
            _tabSpring = new SpringAnimation(0, 320, 38, delegate(double v)
            {
                if (_tabTranslate != null) _tabTranslate.X = v;
            });
        }

        private void CreateTimers()
        {
            _clockTimer = new DispatcherTimer(DispatcherPriority.Background);
            _clockTimer.Interval = TimeSpan.FromMilliseconds(500);
            _clockTimer.Tick += delegate { RefreshClock(); };

            _musicTimer = new DispatcherTimer(DispatcherPriority.Background);
            _musicTimer.Interval = TimeSpan.FromSeconds(1);
            _musicTimer.Tick += delegate { TickMusic(); };

            _equalizerTimer = new DispatcherTimer(DispatcherPriority.Render);
            _equalizerTimer.Interval = TimeSpan.FromMilliseconds(70);
            _equalizerTimer.Tick += delegate { TickEqualizer(); };

            _statusTimer = new DispatcherTimer(DispatcherPriority.Background);
            _statusTimer.Interval = TimeSpan.FromSeconds(2);
            _statusTimer.Tick += delegate { RefreshStatus(); };

            _previewTimer = new DispatcherTimer(DispatcherPriority.Background);
            _previewTimer.Interval = TimeSpan.FromMilliseconds(6500);
            _previewTimer.Tick += delegate
            {
                _previewTimer.Stop();
                if (_mode == IslandMode.Preview && !_previewMouseInside) Collapse();
            };

            _autoCollapseTimer = new DispatcherTimer(DispatcherPriority.Background);
            _autoCollapseTimer.Interval = TimeSpan.FromMilliseconds(280);
            _autoCollapseTimer.Tick += delegate
            {
                _autoCollapseTimer.Stop();
                if (!IsActive && _settings.AutoCollapse && _mode == IslandMode.Expanded && !_scrubbing)
                    Collapse();
            };
        }

        public void SaveSnapshot(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                UpdateLayout();
                if (Content == null) return;
                FrameworkElement root = Content as FrameworkElement;
                if (root == null) return;
                root.UpdateLayout();
                int width = (int)Math.Ceiling(root.ActualWidth);
                int height = (int)Math.Ceiling(root.ActualHeight);
                if (width <= 0 || height <= 0) return;
                RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(root);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    encoder.Save(stream);
                }
            }
            catch (Exception ex) { Log.Error("SaveSnapshot", ex); }
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;
            PositionAtTop();
            Opacity = 1;
            try { Topmost = false; Topmost = _settings.Topmost; } catch { }
            StartLiveTimers();
            RefreshClock();
            RefreshTrackUi();
            RefreshAlertUi();
            RefreshStatus();
        }

        private void StartLiveTimers()
        {
            if (_clockTimer != null) _clockTimer.Start();
            if (_musicTimer != null) _musicTimer.Start();
            if (_equalizerTimer != null) _equalizerTimer.Start();
            if (_statusTimer != null) _statusTimer.Start();
            RefreshStatus();
        }

        private void StopLiveTimers()
        {
            if (_clockTimer != null) _clockTimer.Stop();
            if (_musicTimer != null) _musicTimer.Stop();
            if (_equalizerTimer != null) _equalizerTimer.Stop();
            if (_statusTimer != null) _statusTimer.Stop();
        }

        private void PositionAtTop()
        {
            _workArea = SystemParameters.WorkArea;
            double left = _workArea.Left + (_workArea.Width - Width) / 2.0 + _settings.IslandOffsetX;
            double top = _workArea.Top + TopOffset + _settings.IslandOffsetY;
            left = Math.Max(_workArea.Left, Math.Min(_workArea.Right - Width, left));
            top = Math.Max(_workArea.Top, Math.Min(_workArea.Bottom - Height, top));
            Left = left;
            Top = top;
        }

        private void SetWindowSize(double panelWidth, double panelHeight)
        {
            // Keep the layered window surface constant. Resizing a transparent
            // topmost window on every page change makes DWM drop a frame and
            // shows the desktop through the island for a moment (visible as a
            // flash). Only the panel inside the fixed surface animates.
            if (Math.Abs(Width - FixedWindowWidth) > 0.1) Width = FixedWindowWidth;
            if (Math.Abs(Height - FixedWindowHeight) > 0.1) Height = FixedWindowHeight;
            PositionAtTop();
        }

        private void UpdatePanelClip()
        {
            // ClipToBounds on the host follows the panel size; a hand-built
            // rounded Clip here suppressed the expanded view on some drivers.
            if (_clipHost != null) _clipHost.Clip = null;
        }

        private void OnPanelSizeSettled()
        {
            if (_mode == IslandMode.Compact) SetWindowSize(_compactWidth, _compactHeight);
            else if (_mode == IslandMode.Preview) SetWindowSize(PreviewWidth, PreviewHeight);
            else SetWindowSize(ExpandedWidth, ExpandedHeightFor(_tabIndex));
        }

        public void ShowIsland(int tab)
        {
            if (!IsVisible)
            {
                Show();
                PositionAtTop();
            }
            Expand(tab >= 0 ? tab : _tabIndex);
        }

        public void Expand(int tab)
        {
            if (!IsVisible)
            {
                Show();
                PositionAtTop();
            }
            if (tab < 0 || tab >= PageCount || !PageEnabled(tab)) tab = FirstEnabledPage();
            if (tab >= 0 && tab < PageCount) ShowActivity(tab, _mode == IslandMode.Expanded);
            if (_mode == IslandMode.Expanded)
            {
                Activate();
                return;
            }

            if (_previewTimer != null) _previewTimer.Stop();
            CancelAutoCollapse();
            _mode = IslandMode.Expanded;
            double targetHeight = ExpandedHeightFor(_tabIndex);
            SetWindowSize(ExpandedWidth, targetHeight);
            _widthSpring.To(ExpandedWidth, false);
            _heightSpring.To(targetHeight, false);
            _radiusSpring.To(ExpandedRadius, false);
            ShowExpandedContent();
            Topmost = _settings.Topmost;
            Activate();
        }

        public void Collapse()
        {
            if (_mode == IslandMode.Compact) return;
            _mode = IslandMode.Compact;
            CancelAutoCollapse();
            if (_previewTimer != null) _previewTimer.Stop();
            _widthSpring.To(_compactWidth, false);
            _heightSpring.To(_compactHeight, false);
            _radiusSpring.To(CompactRadius, false);
            ShowCompactContent();
            HideExpandedContent();
            HidePreviewContent();
            if (IsActive) Keyboard.ClearFocus();
        }

        private void ShowExpandedContent()
        {
            _previewView.Visibility = Visibility.Collapsed;
            _previewView.IsHitTestVisible = false;
            _previewView.Opacity = 0;

            _expandedView.Visibility = Visibility.Visible;
            _expandedView.IsHitTestVisible = true;
            _expandedView.Opacity = 1;

            _compactView.Visibility = Visibility.Collapsed;
            _compactView.IsHitTestVisible = false;
            _compactView.Opacity = 0;
            _panel.BorderBrush = Ui.Alpha(Colors.White, 20);
        }

        private void HideExpandedContent()
        {
            _expandedView.BeginAnimation(UIElement.OpacityProperty, null);
            _expandedView.IsHitTestVisible = false;
            _expandedView.Opacity = 0;
            _expandedView.Visibility = Visibility.Collapsed;
        }

        private void ShowCompactContent()
        {
            _compactView.BeginAnimation(UIElement.OpacityProperty, null);
            _compactView.Visibility = Visibility.Visible;
            _compactView.IsHitTestVisible = true;
            _compactView.Opacity = 1;
            _panel.BorderBrush = Ui.Alpha(Colors.White, 20);
        }

        private void HidePreviewContent()
        {
            _previewView.BeginAnimation(UIElement.OpacityProperty, null);
            _previewView.IsHitTestVisible = false;
            _previewView.Opacity = 0;
            _previewView.Visibility = Visibility.Collapsed;
        }

        public void ShowNotificationPreview()
        {
            ShowCustomPreview(Alerts[_alertIndex]);
        }

        private void ShowCustomPreview(AlertInfo alert)
        {
            if (alert == null || _focusMode) return;
            _previewApp.Text = alert.App;
            _previewTime.Text = alert.Time;
            _previewTitle.Text = alert.Title;
            _previewBody.Text = alert.Body;
            _previewIconHost.Children.Clear();
            FrameworkElement previewIcon = Ui.Icon(alert.Icon, Ui.AccentPaleBrush, 18, 1.6);
            previewIcon.HorizontalAlignment = HorizontalAlignment.Center;
            previewIcon.VerticalAlignment = VerticalAlignment.Center;
            _previewIconHost.Children.Add(previewIcon);

            if (!IsVisible)
            {
                Show();
                PositionAtTop();
            }
            CancelAutoCollapse();
            if (_previewTimer != null) _previewTimer.Stop();

            _mode = IslandMode.Preview;
            SetWindowSize(PreviewWidth, PreviewHeight);
            _widthSpring.To(PreviewWidth, false);
            _heightSpring.To(PreviewHeight, false);
            _radiusSpring.To(PreviewRadius, false);

            _expandedView.BeginAnimation(UIElement.OpacityProperty, null);
            _expandedView.Visibility = Visibility.Collapsed;
            _expandedView.IsHitTestVisible = false;
            _compactView.BeginAnimation(UIElement.OpacityProperty, null);
            _compactView.Visibility = Visibility.Collapsed;
            _compactView.IsHitTestVisible = false;

            _previewView.BeginAnimation(UIElement.OpacityProperty, null);
            _previewView.Visibility = Visibility.Visible;
            _previewView.IsHitTestVisible = true;
            _previewView.Opacity = 1;
            _previewMouseInside = false;
            ArmPreviewTimer();
            Topmost = _settings.Topmost;
        }

        private void ArmPreviewTimer()
        {
            if (_previewTimer == null || _mode != IslandMode.Preview) return;
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            if (!_settings.AutoCollapse || _mode != IslandMode.Expanded) return;
            if (_autoCollapseTimer == null) return;
            _autoCollapseTimer.Stop();
            _autoCollapseTimer.Start();
        }

        public void CancelAutoCollapse()
        {
            if (_autoCollapseTimer != null) _autoCollapseTimer.Stop();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _mode != IslandMode.Compact)
            {
                Collapse();
                e.Handled = true;
            }
        }

        private void OnIslandWheel(object sender, MouseWheelEventArgs e)
        {
            if (_mode == IslandMode.Preview) return;
            int direction = e.Delta > 0 ? -1 : 1;
            List<int> pages = EnabledPages();
            if (pages.Count == 0) return;
            int position = pages.IndexOf(_tabIndex);
            if (position < 0) position = 0;
            int next = pages[(position + direction + pages.Count) % pages.Count];
            if (_mode == IslandMode.Expanded)
            {
                ShowActivity(next, true);
                e.Handled = true;
            }
            else if (_mode == IslandMode.Compact)
            {
                Expand(next);
                e.Handled = true;
            }
        }

        public void ToggleVisible()
        {
            if (IsVisible)
            {
                StopLiveTimers();
                Hide();
            }
            else
            {
                Show();
                PositionAtTop();
                if (_mode != IslandMode.Compact) Collapse();
                StartLiveTimers();
            }
        }

        public void ApplyTopmost(bool topmost)
        {
            Topmost = topmost;
            _settings.Topmost = topmost;
        }

        public void ExitApplication()
        {
            _allowClose = true;
            Cleanup();
            try { Application.Current.Shutdown(); } catch { }
            try { Close(); } catch { }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                StopLiveTimers();
                Hide();
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_cleanupDone) return;
            _cleanupDone = true;
            StopLiveTimers();
            try { if (_previewTimer != null) _previewTimer.Stop(); } catch { }
            try { if (_autoCollapseTimer != null) _autoCollapseTimer.Stop(); } catch { }
            if (_tray != null)
            {
                try { _tray.Dispose(); } catch { }
                _tray = null;
            }
        }

        private Grid ViewForPage(int page)
        {
            if (page == PageMusic) return _musicView;
            if (page == PageAlerts) return _alertsView;
            if (page == PageStatus) return _statusView;
            if (page == PageClipboard) return _clipboardView;
            if (page == PageNotifyCenter) return _notifyCenterView;
            if (page == PageControl) return _controlView;
            if (page == PageCalendar) return _calendarView;
            if (page == PageTransfer) return _transferView;
            return _settingsView;
        }

        private Grid[] AllPageViews()
        {
            return new Grid[]
            {
                _musicView, _alertsView, _statusView, _clipboardView, _notifyCenterView,
                _controlView, _calendarView, _transferView, _settingsView
            };
        }

        private static string PageName(int page)
        {
            if (page == PageMusic) return "音乐";
            if (page == PageAlerts) return "通知";
            if (page == PageStatus) return "系统状态";
            if (page == PageClipboard) return "剪贴板";
            if (page == PageNotifyCenter) return "通知中心";
            if (page == PageControl) return "控制中心";
            if (page == PageCalendar) return "日历日程";
            if (page == PageTransfer) return "中转站";
            return "设置";
        }

        private bool PageEnabled(int page)
        {
            if (page == PageMusic) return _settings.ShowMusic;
            if (page == PageAlerts) return _settings.ShowAlerts;
            if (page == PageStatus) return _settings.ShowStatus;
            if (page == PageClipboard) return _settings.ShowClipboard;
            if (page == PageNotifyCenter) return _settings.ShowNotifyCenter;
            if (page == PageControl) return _settings.ShowControl;
            if (page == PageCalendar) return _settings.ShowCalendar;
            if (page == PageTransfer) return _settings.ShowTransfer;
            return true;
        }

        private int FirstEnabledPage()
        {
            List<int> pages = EnabledPages();
            return pages.Count > 0 ? pages[0] : PageSettings;
        }

        private List<int> EnabledPages()
        {
            List<int> pages = new List<int>();
            for (int i = 0; i < PageCount - 1; i++)
            {
                if (PageEnabled(i)) pages.Add(i);
            }
            pages.Add(PageSettings);
            return pages;
        }

        private void ShowActivity(int activity, bool animate)
        {
            if (activity < 0 || activity >= PageCount) return;
            bool changed = _tabIndex != activity;
            _tabIndex = activity;
            Grid next = ViewForPage(activity);
            if (next != null) ShowContentView(next, animate && changed);
            UpdatePageDots();
            if (activity == PageClipboard) RefreshClipboardList();
            else if (activity == PageNotifyCenter) RefreshNotificationCenter();
            else if (activity == PageControl) RefreshControlCenter();
            else if (activity == PageCalendar) RefreshCalendar();
            else if (activity == PageTransfer) RefreshTransferList();
            if (_mode == IslandMode.Expanded) ApplyExpandedHeight();
        }

        private double ExpandedHeightFor(int activity)
        {
            if (activity == PageMusic) return 230.0;
            if (activity == PageAlerts) return 240.0;
            if (activity == PageStatus) return 260.0;
            if (activity == PageClipboard) return 280.0;
            if (activity == PageNotifyCenter) return 320.0;
            if (activity == PageControl) return 320.0;
            if (activity == PageCalendar) return 370.0;
            if (activity == PageTransfer) return 280.0;
            if (_developerPanel != null && _developerPanel.Visibility == Visibility.Visible) return 500.0;
            if (_devPasswordPanel != null && _devPasswordPanel.Visibility == Visibility.Visible) return 450.0;
            return 320.0;
        }

        private void RebuildPageDots()
        {
            if (_pageDotRail == null) return;
            _pageDotRail.Children.Clear();
            _dotPages.Clear();
            _pageDots = new Border[PageCount];
            _pageDotMarks = new Ellipse[PageCount];
            List<int> pages = EnabledPages();
            _dotCount = pages.Count;
            for (int i = 0; i < pages.Count; i++)
            {
                int page = pages[i];
                Border button = new Border();
                button.Width = 18;
                button.Height = 16;
                button.CornerRadius = new CornerRadius(8);
                button.Background = Brushes.Transparent;
                button.Cursor = Cursors.Hand;
                button.ToolTip = PageName(page);
                button.Margin = new Thickness(0, 2, 0, 2);
                Ellipse mark = new Ellipse();
                mark.Width = 4.2;
                mark.Height = 4.2;
                mark.Fill = Ui.Alpha(Colors.White, 30);
                mark.HorizontalAlignment = HorizontalAlignment.Center;
                mark.VerticalAlignment = VerticalAlignment.Center;
                button.Child = mark;
                button.MouseEnter += delegate { if (_tabIndex != page) button.Background = Ui.Alpha(Colors.White, 8); };
                button.MouseLeave += delegate { if (_tabIndex != page) button.Background = Brushes.Transparent; };
                button.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ShowActivity(page, true);
                };
                _pageDotRail.Children.Add(button);
                _pageDots[i] = button;
                _pageDotMarks[i] = mark;
                _dotPages.Add(page);
            }
            UpdatePageDots();
        }

        public void SetPageEnabled(int page, bool enabled)
        {
            if (page < 0 || page > PageTransfer) return;
            if (page == PageMusic) _settings.ShowMusic = enabled;
            else if (page == PageAlerts) _settings.ShowAlerts = enabled;
            else if (page == PageStatus) _settings.ShowStatus = enabled;
            else if (page == PageClipboard) _settings.ShowClipboard = enabled;
            else if (page == PageNotifyCenter) _settings.ShowNotifyCenter = enabled;
            else if (page == PageControl) _settings.ShowControl = enabled;
            else if (page == PageCalendar) _settings.ShowCalendar = enabled;
            else if (page == PageTransfer) _settings.ShowTransfer = enabled;
            _settings.Save();
            RebuildPageDots();
            if (!PageEnabled(_tabIndex))
            {
                ShowActivity(PageSettings, false);
                if (_mode == IslandMode.Expanded) Expand(PageSettings);
            }
        }

        private void ApplyExpandedHeight()
        {
            if (_mode != IslandMode.Expanded) return;
            double target = ExpandedHeightFor(_tabIndex);
            SetWindowSize(ExpandedWidth, target);
            _heightSpring.To(target, false);
        }

        private void UpdatePageDots()
        {
            if (_pageDots == null || _pageDotMarks == null) return;
            for (int i = 0; i < _dotCount; i++)
            {
                if (_pageDots[i] == null || _pageDotMarks[i] == null) continue;
                bool active = _dotPages[i] == _tabIndex;
                _pageDots[i].Background = active ? Ui.AccentTint(30) : Brushes.Transparent;
                _pageDots[i].BorderBrush = active ? Ui.AccentTint(105) : Brushes.Transparent;
                _pageDots[i].BorderThickness = new Thickness(active ? 1 : 0);
                _pageDotMarks[i].Fill = active ? Ui.AccentPaleBrush : (Brush)Ui.Alpha(Colors.White, 32);
                _pageDotMarks[i].Width = active ? 6.5 : 3.6;
                _pageDotMarks[i].Height = active ? 6.5 : 3.6;
                if (active)
                {
                    DropShadowEffect glow = new DropShadowEffect();
                    glow.Color = Ui.AccentColor;
                    glow.BlurRadius = 8;
                    glow.ShadowDepth = 0;
                    glow.Opacity = 0.85;
                    _pageDotMarks[i].Effect = glow;
                }
                else
                {
                    _pageDotMarks[i].Effect = null;
                }
            }
        }

        private void UpdateBellUi()
        {
            if (_bellButton == null) return;
            bool alerts = _tabIndex == 1;
            _bellButton.Background = alerts ? Ui.AccentTint(16) : Brushes.Transparent;
            _bellButton.ToolTip = alerts ? "返回音乐" : "查看通知";
            if (_bellIconHost != null)
            {
                _bellIconHost.Children.Clear();
                FrameworkElement icon = Ui.Icon(Ico.Bell, alerts ? Ui.AccentBrush : Ui.SubBrush, 13, 1.6);
                icon.HorizontalAlignment = HorizontalAlignment.Center;
                icon.VerticalAlignment = VerticalAlignment.Center;
                _bellIconHost.Children.Add(icon);
            }
            if (_headerUnreadDot != null)
                _headerUnreadDot.Visibility = _unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowContentView(Grid next, bool animate)
        {
            Grid[] all = AllPageViews();
            next.BeginAnimation(UIElement.OpacityProperty, null);
            next.Opacity = 1;
            next.RenderTransform = Transform.Identity;
            next.Visibility = Visibility.Visible;
            foreach (Grid v in all)
            {
                if (v == null || v == next) continue;
                v.BeginAnimation(UIElement.OpacityProperty, null);
                v.Opacity = 0;
                v.Visibility = Visibility.Collapsed;
            }
            next.UpdateLayout();
        }

        private void RefreshClock()
        {
            DateTime now = DateTime.Now;
            if (_compactClock != null) _compactClock.Text = now.ToString(_settings.ShowSeconds ? "HH:mm:ss" : "HH:mm");
            if (_compactDate != null) _compactDate.Text = now.ToString("M月d日") + " " + WeekdayShort(now.DayOfWeek);
            _lastSecond = now.Second;
            PollClipboard();
            CheckReminders();
        }

        private static string WeekdayShort(DayOfWeek day)
        {
            string[] names = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return names[(int)day];
        }

        private void TickEqualizer()
        {
            if (!IsVisible) return;
            _wavePhase += 0.55;
            double factor = _playing ? 1.0 : 0.34;
            if (_equalizerBars != null)
            {
                for (int i = 0; i < _equalizerBars.Length; i++)
                {
                    double n = 0.5 + 0.5 * Math.Sin(_wavePhase + i * 0.88);
                    _equalizerBars[i].Height = 4.0 + n * 8.0 * factor;
                    _equalizerBars[i].Opacity = 0.48 + 0.25 * n * (0.7 + factor * 0.3);
                }
            }
            if (_playing && _discRotate != null)
                _discRotate.Angle = (_discRotate.Angle + 1.05) % 360.0;
        }

        private void TickMusic()
        {
            RefreshMediaState();
        }

        private void TogglePlay()
        {
            bool changed = _mediaService.TogglePlayPause();
            if (changed)
            {
                _playing = !_playing;
                UpdatePlayButton();
            }
            RefreshMediaState();
        }

        private void ChangeTrack(int delta)
        {
            if (delta < 0) _mediaService.Previous();
            else _mediaService.Next();
            Dispatcher.BeginInvoke(new Action(RefreshMediaState), DispatcherPriority.Background);
        }

        private void ShuffleTrack()
        {
            _mediaService.Next();
            Dispatcher.BeginInvoke(new Action(RefreshMediaState), DispatcherPriority.Background);
        }

        private void RefreshTrackUi()
        {
            RefreshMediaState();
        }

        private void RefreshMediaState()
        {
            if (_musicTitle == null) return;
            MediaSnapshot snapshot;
            try { snapshot = _mediaService.Poll(); }
            catch { snapshot = null; }
            if (snapshot == null || !snapshot.Available)
            {
                _mediaAvailable = false;
                _playing = false;
                _mediaDurationSeconds = 0;
                _elapsed = 0;
                _musicTitle.Text = "未在播放";
                _musicArtist.Text = "打开播放器后自动同步";
                if (_albumImage != null)
                {
                    _albumImage.Visibility = Visibility.Collapsed;
                    _albumImage.Source = null;
                }
                if (_albumDisc != null) _albumDisc.Visibility = Visibility.Visible;
                if (_albumStamp != null)
                {
                    _albumStamp.Visibility = Visibility.Visible;
                    _albumStamp.Text = "HAVEGET";
                }
                UpdatePlayButton();
                UpdateProgressUi();
                return;
            }
            _mediaAvailable = true;
            _playing = snapshot.Playing;
            _mediaDurationSeconds = snapshot.Duration.TotalSeconds;
            _elapsed = (int)Math.Round(snapshot.Position.TotalSeconds);
            _musicTitle.Text = snapshot.Title;
            _musicArtist.Text = snapshot.Artist;
            if (_albumImage != null)
            {
                if (snapshot.Artwork != null)
                {
                    if (_albumImage.Source != snapshot.Artwork) _albumImage.Source = snapshot.Artwork;
                    _albumImage.Visibility = Visibility.Visible;
                    if (_albumDisc != null) _albumDisc.Visibility = Visibility.Collapsed;
                    if (_albumStamp != null) _albumStamp.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _albumImage.Visibility = Visibility.Collapsed;
                    _albumImage.Source = null;
                    if (_albumDisc != null) _albumDisc.Visibility = Visibility.Visible;
                    if (_albumStamp != null)
                    {
                        _albumStamp.Visibility = Visibility.Visible;
                        _albumStamp.Text = "HAVEGET";
                    }
                }
            }
            UpdatePlayButton();
            UpdateProgressUi();
        }

        private static LinearGradientBrush CreateArtworkBrush(TrackInfo track)
        {
            LinearGradientBrush b = new LinearGradientBrush();
            b.StartPoint = new Point(0, 0);
            b.EndPoint = new Point(1, 1);
            b.GradientStops.Add(new GradientStop(Ui.ColorFromHex(track.ArtA), 0));
            b.GradientStops.Add(new GradientStop(Ui.ColorFromHex(track.ArtB), 1));
            b.Freeze();
            return b;
        }

        private void UpdatePlayButton()
        {
            if (_playIconHost == null) return;
            _playIconHost.Children.Clear();
            if (_playing)
            {
                StackPanel bars = new StackPanel();
                bars.Orientation = Orientation.Horizontal;
                bars.HorizontalAlignment = HorizontalAlignment.Center;
                bars.VerticalAlignment = VerticalAlignment.Center;
                Border first = new Border();
                first.Width = 3;
                first.Height = 13;
                first.CornerRadius = new CornerRadius(1.5);
                first.Background = Ui.InkBrush;
                Border second = new Border();
                second.Width = 3;
                second.Height = 13;
                second.CornerRadius = new CornerRadius(1.5);
                second.Background = Ui.InkBrush;
                second.Margin = new Thickness(4, 0, 0, 0);
                bars.Children.Add(first);
                bars.Children.Add(second);
                _playIconHost.Children.Add(bars);
            }
            else
            {
                Polygon triangle = new Polygon();
                PointCollection points = new PointCollection();
                points.Add(new Point(0, 0));
                points.Add(new Point(11, 6.5));
                points.Add(new Point(0, 13));
                triangle.Points = points;
                triangle.Fill = Ui.InkBrush;
                triangle.Stretch = Stretch.None;
                triangle.HorizontalAlignment = HorizontalAlignment.Center;
                triangle.VerticalAlignment = VerticalAlignment.Center;
                triangle.Margin = new Thickness(2.5, 0, 0, 0);
                _playIconHost.Children.Add(triangle);
            }
            if (_playButton != null)
            {
                _playButton.Opacity = _mediaAvailable ? 1.0 : 0.55;
                _playButton.Cursor = _mediaAvailable ? Cursors.Hand : Cursors.Arrow;
                _playButton.ToolTip = _mediaAvailable ? (_playing ? "暂停" : "播放") : "没有正在播放的媒体";
            }
        }

        private void UpdateProgressUi()
        {
            if (_progressScale == null) return;
            int duration = (int)Math.Round(_mediaDurationSeconds);
            if (_elapsed < 0) _elapsed = 0;
            if (duration > 0 && _elapsed > duration) _elapsed = duration;
            if (_timeNow != null) _timeNow.Text = FormatTime(_elapsed);
            if (_timeTotal != null) _timeTotal.Text = FormatTime(duration);
            double ratio = duration <= 0 ? 0 : (double)_elapsed / duration;
            _progressScale.ScaleX = Math.Max(0.001, Math.Min(1.0, ratio));
        }

        private static string FormatTime(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        }

        private void OnProgressDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            _scrubbing = true;
            _progressTrack.CaptureMouse();
            SeekFromMouse(e);
            e.Handled = true;
        }

        private void OnProgressMove(object sender, MouseEventArgs e)
        {
            if (!_scrubbing) return;
            SeekFromMouse(e);
        }

        private void OnProgressUp(object sender, MouseButtonEventArgs e)
        {
            if (!_scrubbing) return;
            _scrubbing = false;
            if (_progressTrack.IsMouseCaptured) _progressTrack.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void SeekFromMouse(MouseEventArgs e)
        {
            if (_progressTrack == null || _progressTrack.ActualWidth <= 0) return;
            if (!_mediaAvailable || _mediaDurationSeconds <= 0) return;
            double x = e.GetPosition(_progressTrack).X;
            double ratio = Math.Max(0, Math.Min(1, x / _progressTrack.ActualWidth));
            double seconds = _mediaDurationSeconds * ratio;
            _elapsed = (int)Math.Round(seconds);
            UpdateProgressUi();
            _mediaService.Seek(seconds);
        }

        private void MarkAllRead()
        {
            for (int i = 0; i < Alerts.Length; i++) Alerts[i].Read = true;
            for (int i = 0; i < _notificationHistory.Count; i++) _notificationHistory[i].Read = true;
            _unreadCount = 0;
            UpdateUnreadIndicators();
            RefreshAlertUi();
            RefreshNotificationCenter();
        }

        private void ReplyToCurrentNotification()
        {
            string reply = "收到，我看一下。";
            try
            {
                System.Windows.Clipboard.SetText(reply);
                _lastClipText = reply;
            }
            catch { }
            Alerts[_alertIndex].Read = true;
            RecalculateUnread();
            RefreshAlertUi();
            RefreshNotificationCenter();
            UpdateUnreadIndicators();
            if (_replyButtonText != null)
            {
                _replyButtonText.Text = "已复制";
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1600);
                timer.Tick += delegate
                {
                    timer.Stop();
                    if (_replyButtonText != null) _replyButtonText.Text = "回复";
                };
                timer.Start();
            }
        }

        private void SimulateAlert()
        {
            _alertIndex = (_alertIndex + 1) % Alerts.Length;
            Alerts[_alertIndex].Read = false;
            _unreadCount = Math.Min(9, _unreadCount + 1);
            RefreshAlertUi();
            RefreshNotificationCenter();
            UpdateUnreadIndicators();
            if (_alertCard != null) Ui.FadeIn(_alertCard, 0, 180);
        }

        private void RefreshAlertUi()
        {
            AlertInfo alert = Alerts[_alertIndex];
            if (_alertApp == null) return;
            _alertApp.Text = alert.App;
            _alertTime.Text = alert.Time;
            _alertTitle.Text = alert.Title;
            _alertBody.Text = alert.Body;
            if (_alertSummary != null)
                _alertSummary.Text = _unreadCount > 0 ? string.Format("{0} 条未读", _unreadCount) : "已全部读过";
            _alertUnreadDot.Visibility = _unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            _alertIconHost.Children.Clear();
            FrameworkElement alertIcon = Ui.Icon(alert.Icon, Ui.AccentPaleBrush, 17, 1.6);
            alertIcon.HorizontalAlignment = HorizontalAlignment.Center;
            alertIcon.VerticalAlignment = VerticalAlignment.Center;
            _alertIconHost.Children.Add(alertIcon);
            _alertCard.Background = Ui.Alpha(Colors.White, 5);

            if (_unreadCount > 0)
            {
                _readButton.Background = Ui.AccentTint(24);
                _readButton.BorderBrush = Ui.AccentTint(60);
                _readButton.Cursor = Cursors.Hand;
                _readButtonText.Text = "标记已读";
                _readButtonText.Foreground = Ui.AccentPaleBrush;
            }
            else
            {
                _readButton.Background = Brushes.Transparent;
                _readButton.BorderBrush = Ui.Alpha(Colors.White, 8);
                _readButton.Cursor = Cursors.Arrow;
                _readButtonText.Text = "已读";
                _readButtonText.Foreground = Ui.FaintBrush;
            }
            UpdateUnreadIndicators();
        }

        private void UpdateUnreadIndicators()
        {
            Visibility visible = _unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (_compactUnreadDot != null) _compactUnreadDot.Visibility = visible;
            if (_headerUnreadDot != null) _headerUnreadDot.Visibility = visible;
            UpdatePageDots();
            if (_alertSummary != null)
                _alertSummary.Text = _unreadCount > 0 ? string.Format("{0} 条未读", _unreadCount) : "已全部读过";
            if (_alertUnreadDot != null) _alertUnreadDot.Visibility = visible;
        }

        private void RefreshStatus()
        {
            if (_statusTitle == null) return;
            SystemSnapshot s;
            try { s = _monitor.Sample(); }
            catch (Exception ex)
            {
                Log.Error("SystemMonitor.Sample", ex);
                return;
            }
            _lastStatus = s;

            bool online = s.Online && s.NetworkLabel == "在线";
            _statusTitle.Text = online ? "系统运行正常" : "当前处于离线状态";
            _statusDetail.Text = online
                ? (string.IsNullOrEmpty(s.NetworkDetail) ? "网络已连接 · 本机数据" : s.NetworkDetail + " · 本机数据")
                : "网络不可用 · 灵动岛仍在本地运行";
            _statusIconDot.Fill = online ? Ui.AccentBrush : Ui.WarnBrush;
            _statusLive.Text = online ? "实时" : "本地";
            _statusBanner.BorderBrush = online ? Ui.AccentTint(35) : Ui.Alpha(Ui.WarnColor, 35);
            _statusIconHost.Children.Clear();
            FrameworkElement networkIcon = Ui.Icon(Ico.Wifi, online ? Ui.AccentBrush : Ui.WarnBrush, 15, 1.5);
            networkIcon.HorizontalAlignment = HorizontalAlignment.Center;
            networkIcon.VerticalAlignment = VerticalAlignment.Center;
            _statusIconHost.Children.Add(networkIcon);

            SetMetric(_cpuValue, _cpuSub, _cpuFill, s.HasCpuSample ? s.CpuPercent : -1, s.HasCpuSample ? "实时负载" : "采样中");
            SetMetric(_memValue, _memSub, _memFill, s.MemoryPercent,
                s.MemoryTotal > 0 ? TextUtil.FormatBytes(s.MemoryUsed) + " / " + TextUtil.FormatBytes(s.MemoryTotal) : "不可用");
            SetMetric(_diskValue, _diskSub, _diskFill, s.DiskPercent,
                s.DiskTotal > 0 ? TextUtil.FormatBytes(s.DiskFree) + " 可用" : "不可用");

            if (s.BatteryPresent)
            {
                SetMetric(_batteryValue, _batterySub, _batteryFill, s.BatteryPercent,
                    s.Charging ? "充电中" : "使用电池");
            }
            else
            {
                SetMetric(_batteryValue, _batterySub, _batteryFill, 100, "交流电");
            }

            if (_cpuValue != null) _cpuValue.ToolTip = s.WindowsName + " · " + TextUtil.FormatUptime(s.UptimeMilliseconds) + " 已运行";
            if (_memValue != null) _memValue.ToolTip = "物理内存占用";
            if (_diskValue != null) _diskValue.ToolTip = "系统盘空间";
            if (_batteryValue != null) _batteryValue.ToolTip = s.BatteryPresent ? "本机电池状态" : "本机使用交流电";
            UpdateControlCenterStatus();
            if (_tabIndex == PageControl) UpdateVolumeUi();
        }

        private static void SetMetric(TextBlock value, TextBlock sub, ScaleTransform fill, double percent, string subText)
        {
            if (value == null) return;
            if (percent < 0)
            {
                value.Text = "--";
                if (sub != null) sub.Text = subText;
                if (fill != null) AnimateFill(fill, 0);
                return;
            }
            value.Text = ((int)Math.Round(percent)) + "%";
            if (sub != null) sub.Text = subText;
            if (fill != null) AnimateFill(fill, Math.Max(0.0, Math.Min(1.0, percent / 100.0)));
        }

        private static void AnimateFill(ScaleTransform fill, double target)
        {
            if (fill == null) return;
            DoubleAnimation anim = new DoubleAnimation(target, new Duration(TimeSpan.FromMilliseconds(320)));
            anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            fill.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        }
    }

    public sealed class TrackInfo
    {
        public string Title;
        public string Artist;
        public int Duration;
        public string ArtA;
        public string ArtB;
    }

    public sealed class AlertInfo
    {
        public string App;
        public string Title;
        public string Body;
        public string Time;
        public string Icon;
        public bool Read;
    }

    public sealed class ReminderItem
    {
        public DateTime Due;
        public string Text;
        public bool Notified;
    }
}
