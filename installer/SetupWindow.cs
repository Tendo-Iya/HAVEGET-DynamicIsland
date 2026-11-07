using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using HavegetDynamicIsland;

namespace HavegetDynamicIsland.Setup
{
    public sealed class SetupWindow : Window
    {
        private readonly InstallOptions _options;
        private readonly bool _uninstallMode;
        private Border _panel;
        private Grid _clipHost;
        private Grid _contentHost;
        private Grid _readyView;
        private Grid _progressView;
        private Grid _doneView;
        private Grid _errorView;
        private Grid _uninstallView;

        private TextBlock _statusText;
        private TextBlock _detailText;
        private TextBlock _percentText;
        private Border _progressFill;
        private ScaleTransform _progressScale;

        private TextBlock _errorTitle;
        private TextBlock _errorBody;
        private Border _errorRetryButton;
        private Border _errorDownloadButton;
        private Action _errorRetryAction;

        private TextBlock _doneTitle;
        private TextBlock _doneBody;
        private Border _doneOpenButton;
        private string _installedAppPath;
        private bool _rebootRequired;

        private TextBlock _uninstallDetail;
        private bool _removeData;

        private bool _busy;
        private bool _completed;
        private bool _allowClose;

        public SetupWindow(InstallOptions options, bool uninstallMode)
        {
            _options = options ?? new InstallOptions();
            _uninstallMode = uninstallMode;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 640;
            Height = uninstallMode ? 350 : 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;
            Topmost = true;
            Title = uninstallMode ? "卸载 HAVEGET DynamicIsland" : "安装 HAVEGET DynamicIsland";
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            try { Icon = LoadWindowIcon(); } catch { }

            BuildShell();
            BuildViews();
            if (uninstallMode) ShowView(_uninstallView, false);
            else ShowView(_readyView, false);
            Loaded += delegate
            {
                ShowView(uninstallMode ? _uninstallView : _readyView, true);
                Activate();
            };
            Closing += OnClosing;
            Closed += delegate
            {
                if (_uninstallMode && _completed) InstallerEngine.ScheduleUninstallerCleanup();
            };
        }

        private static ImageSource LoadWindowIcon()
        {
            using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName))
            {
                if (icon == null) return null;
                BitmapSource src = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
        }

        private void BuildShell()
        {
            Grid outer = new Grid();
            _panel = new Border();
            _panel.Margin = new Thickness(26);
            LinearGradientBrush bg = new LinearGradientBrush();
            bg.StartPoint = new Point(0, 0);
            bg.EndPoint = new Point(0, 1);
            bg.GradientStops.Add(new GradientStop(Color.FromArgb(250, 16, 17, 20), 0));
            bg.GradientStops.Add(new GradientStop(Color.FromArgb(253, 8, 9, 11), 1));
            _panel.Background = bg;
            _panel.BorderBrush = Ui.Alpha(Colors.White, 12);
            _panel.BorderThickness = new Thickness(1);
            _panel.CornerRadius = new CornerRadius(24);
            _panel.Effect = Ui.Glow(Colors.Black, 30, 0.42, 10);

            _clipHost = new Grid();
            _clipHost.ClipToBounds = true;
            _panel.Child = _clipHost;

            _contentHost = new Grid();
            _contentHost.Margin = new Thickness(24, 20, 24, 18);
            _clipHost.Children.Add(_contentHost);

            Grid dragArea = new Grid();
            dragArea.Height = 74;
            dragArea.VerticalAlignment = VerticalAlignment.Top;
            dragArea.Background = Brushes.Transparent;
            dragArea.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };
            _clipHost.Children.Add(dragArea);

            Border close = Ui.IconButton(Ico.Close, "关闭", 28, delegate { TryClose(); });
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Top;
            close.Margin = new Thickness(0, 22, 22, 0);
            _clipHost.Children.Add(close);

            outer.Children.Add(_panel);
            Content = outer;
            _clipHost.SizeChanged += delegate
            {
                if (_clipHost.ActualWidth <= 0 || _clipHost.ActualHeight <= 0) return;
                _clipHost.Clip = new RectangleGeometry(new Rect(0, 0, _clipHost.ActualWidth, _clipHost.ActualHeight), 24, 24);
            };
        }

        private void BuildViews()
        {
            _readyView = BuildReadyView();
            _progressView = BuildProgressView();
            _doneView = BuildDoneView();
            _errorView = BuildErrorView();
            _uninstallView = BuildUninstallView();
            _contentHost.Children.Add(_readyView);
            _contentHost.Children.Add(_progressView);
            _contentHost.Children.Add(_doneView);
            _contentHost.Children.Add(_errorView);
            _contentHost.Children.Add(_uninstallView);
            _progressView.Visibility = Visibility.Collapsed;
            _doneView.Visibility = Visibility.Collapsed;
            _errorView.Visibility = Visibility.Collapsed;
            _uninstallView.Visibility = Visibility.Collapsed;
        }

        private Grid BuildReadyView()
        {
            Grid view = new Grid();
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid hero = new Grid();
            hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            view.Children.Add(hero);

            Border logo = Ui.BrandMark(52);
            logo.VerticalAlignment = VerticalAlignment.Center;
            hero.Children.Add(logo);

            StackPanel titles = new StackPanel();
            titles.Margin = new Thickness(14, 0, 0, 0);
            titles.VerticalAlignment = VerticalAlignment.Center;
            TextBlock title = Ui.Text("HAVEGET DynamicIsland", 22, FontWeights.SemiBold, Ui.TextBrush);
            titles.Children.Add(title);
            TextBlock subtitle = Ui.Text("DYNAMIC ISLAND · WINDOWS", 8.6, FontWeights.Normal, Ui.FaintBrush);
            subtitle.Margin = new Thickness(1, 2, 0, 0);
            titles.Children.Add(subtitle);
            Grid.SetColumn(titles, 1);
            hero.Children.Add(titles);

            TextBlock desc = Ui.Text("音乐、通知与系统状态，收进屏幕顶部的一颗胶囊。", 10.5, FontWeights.Normal, Ui.SubBrush);
            desc.Margin = new Thickness(1, 16, 0, 0);
            desc.TextWrapping = TextWrapping.Wrap;
            Grid.SetRow(desc, 1);
            view.Children.Add(desc);

            Border optionsCard = new Border();
            optionsCard.VerticalAlignment = VerticalAlignment.Top;
            optionsCard.Margin = new Thickness(0, 14, 0, 0);
            optionsCard.Background = Ui.Alpha(Colors.White, 4);
            optionsCard.CornerRadius = new CornerRadius(16);
            optionsCard.Padding = new Thickness(14, 12, 14, 12);
            Grid.SetRow(optionsCard, 2);
            view.Children.Add(optionsCard);

            Grid options = new Grid();
            options.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            options.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            options.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            options.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            optionsCard.Child = options;

            Grid pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            FrameworkElement folder = Ui.Icon(Ico.Folder, Ui.AccentBrush, 14, 1.4);
            folder.VerticalAlignment = VerticalAlignment.Center;
            pathRow.Children.Add(folder);
            StackPanel pathStack = new StackPanel();
            pathStack.VerticalAlignment = VerticalAlignment.Center;
            string shownPath = string.IsNullOrEmpty(_options.TargetDir) ? InstallOptions.DefaultTargetDir : _options.TargetDir;
            TextBlock path = Ui.Text(shownPath, 9.8, FontWeights.Normal, Ui.TextBrush);
            path.TextTrimming = TextTrimming.CharacterEllipsis;
            pathStack.Children.Add(path);
            TextBlock pathHint = Ui.Text("当前用户 · 无需管理员权限", 8.5, FontWeights.Normal, Ui.FaintBrush);
            pathHint.Margin = new Thickness(0, 1, 0, 0);
            pathStack.Children.Add(pathHint);
            Grid.SetColumn(pathStack, 1);
            pathRow.Children.Add(pathStack);
            options.Children.Add(pathRow);

            Border divider = new Border();
            divider.Height = 1;
            divider.Margin = new Thickness(24, 10, 0, 8);
            divider.Background = Ui.Alpha(Colors.White, 8);
            Grid.SetRow(divider, 1);
            options.Children.Add(divider);

            StackPanel checks = new StackPanel();
            Grid.SetRow(checks, 2);
            options.Children.Add(checks);
            checks.Children.Add(MakeCheckRow("创建桌面快捷方式", "在桌面放置一个入口", true, delegate(bool on) { _options.DesktopShortcut = on; }));
            checks.Children.Add(MakeCheckRow("开机自动启动", "可选，登录后自动待命", false, delegate(bool on) { _options.StartWithWindows = on; }));

            int release = InstallerEngine.GetNetRelease();
            bool ready = release >= InstallerEngine.MinNetRelease;
            StackPanel runtimeRow = new StackPanel();
            runtimeRow.Orientation = Orientation.Horizontal;
            runtimeRow.Margin = new Thickness(2, 10, 0, 0);
            Grid.SetRow(runtimeRow, 3);
            options.Children.Add(runtimeRow);
            Ellipse runtimeDot = Ui.Dot(5, ready ? Ui.AccentBrush : Ui.AccentPaleBrush);
            runtimeDot.VerticalAlignment = VerticalAlignment.Center;
            runtimeRow.Children.Add(runtimeDot);
            TextBlock runtime = Ui.Text(
                ready ? "运行环境就绪 · " + InstallerEngine.RuntimeLabel(release) : "安装时自动补全 .NET Framework 4.8",
                8.6, FontWeights.Normal, ready ? Ui.FaintBrush : Ui.SubBrush);
            runtime.Margin = new Thickness(6, 0, 0, 0);
            runtime.VerticalAlignment = VerticalAlignment.Center;
            runtimeRow.Children.Add(runtime);

            Grid footer = new Grid();
            footer.Margin = new Thickness(0, 14, 0, 0);
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(footer, 3);
            view.Children.Add(footer);
            TextBlock footHint = Ui.Text(InstallerEngine.IsInstalled ? "已安装 " + InstallerEngine.InstalledVersion + "，将保留数据" : "", 8.6, FontWeights.Normal, Ui.FaintBrush);
            footHint.VerticalAlignment = VerticalAlignment.Center;
            footer.Children.Add(footHint);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(buttons, 1);
            footer.Children.Add(buttons);
            Border install = MakePrimaryButton(InstallerEngine.IsInstalled ? "更新" : "安装", 108, delegate { StartInstall(); });
            buttons.Children.Add(install);
            Border cancel = MakeGhostButton("取消", 72, delegate { TryClose(); });
            cancel.Margin = new Thickness(8, 0, 0, 0);
            buttons.Children.Add(cancel);

            return view;
        }

        private Grid BuildProgressView()
        {
            Grid view = new Grid();
            view.VerticalAlignment = VerticalAlignment.Center;
            StackPanel stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.VerticalAlignment = VerticalAlignment.Center;
            view.Children.Add(stack);

            Grid spinnerWrap = new Grid();
            spinnerWrap.Width = 52;
            spinnerWrap.Height = 52;
            spinnerWrap.HorizontalAlignment = HorizontalAlignment.Center;
            Ellipse ring = new Ellipse();
            ring.Width = 40;
            ring.Height = 40;
            ring.Stroke = Ui.AccentBrush;
            ring.StrokeThickness = 2;
            ring.StrokeDashArray = new DoubleCollection(new double[] { 2, 3.2 });
            ring.StrokeStartLineCap = PenLineCap.Round;
            ring.StrokeEndLineCap = PenLineCap.Round;
            ring.HorizontalAlignment = HorizontalAlignment.Center;
            ring.VerticalAlignment = VerticalAlignment.Center;
            RotateTransform rot = new RotateTransform(0, 20, 20);
            ring.RenderTransform = rot;
            DoubleAnimation spin = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(1300)));
            spin.RepeatBehavior = RepeatBehavior.Forever;
            rot.BeginAnimation(RotateTransform.AngleProperty, spin);
            spinnerWrap.Children.Add(ring);
            stack.Children.Add(spinnerWrap);

            _statusText = Ui.Text("正在准备安装…", 17, FontWeights.SemiBold, Ui.TextBrush);
            _statusText.HorizontalAlignment = HorizontalAlignment.Center;
            _statusText.Margin = new Thickness(0, 15, 0, 0);
            stack.Children.Add(_statusText);
            _detailText = Ui.Text("请稍候", 9.8, FontWeights.Normal, Ui.SubBrush);
            _detailText.HorizontalAlignment = HorizontalAlignment.Center;
            _detailText.Margin = new Thickness(0, 4, 0, 0);
            _detailText.TextWrapping = TextWrapping.Wrap;
            _detailText.TextAlignment = TextAlignment.Center;
            _detailText.MaxWidth = 440;
            stack.Children.Add(_detailText);

            Grid barWrap = new Grid();
            barWrap.Width = 360;
            barWrap.Height = 6;
            barWrap.Margin = new Thickness(0, 22, 0, 0);
            barWrap.HorizontalAlignment = HorizontalAlignment.Center;
            Border track = new Border();
            track.Background = Ui.Alpha(Colors.White, 18);
            track.CornerRadius = new CornerRadius(3);
            barWrap.Children.Add(track);
            _progressFill = new Border();
            _progressFill.HorizontalAlignment = HorizontalAlignment.Left;
            _progressFill.CornerRadius = new CornerRadius(3);
            _progressFill.Background = Ui.AccentBrush;
            _progressFill.Width = 360;
            _progressFill.RenderTransformOrigin = new Point(0, 0.5);
            _progressScale = new ScaleTransform(0.02, 1);
            _progressFill.RenderTransform = _progressScale;
            barWrap.Children.Add(_progressFill);
            stack.Children.Add(barWrap);

            _percentText = Ui.Text("2%", 9, FontWeights.Medium, Ui.FaintBrush, Ui.DisplayFont);
            _percentText.HorizontalAlignment = HorizontalAlignment.Center;
            _percentText.Margin = new Thickness(0, 8, 0, 0);
            stack.Children.Add(_percentText);
            TextBlock note = Ui.Text("安装期间请勿关闭窗口 · 所有文件都在本机处理", 8.4, FontWeights.Normal, Ui.FaintBrush);
            note.HorizontalAlignment = HorizontalAlignment.Center;
            note.Margin = new Thickness(0, 18, 0, 0);
            stack.Children.Add(note);
            return view;
        }

        private Grid BuildDoneView()
        {
            Grid view = new Grid();
            StackPanel stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.VerticalAlignment = VerticalAlignment.Center;
            view.Children.Add(stack);

            Grid checkWrap = new Grid();
            checkWrap.Width = 56;
            checkWrap.Height = 56;
            checkWrap.HorizontalAlignment = HorizontalAlignment.Center;
            Border circle = new Border();
            circle.Width = 50;
            circle.Height = 50;
            circle.CornerRadius = new CornerRadius(25);
            circle.Background = Ui.AccentBrush;
            circle.HorizontalAlignment = HorizontalAlignment.Center;
            circle.VerticalAlignment = VerticalAlignment.Center;
            FrameworkElement check = Ui.Icon(Ico.Check, Ui.InkBrush, 24, 2.2);
            check.HorizontalAlignment = HorizontalAlignment.Center;
            check.VerticalAlignment = VerticalAlignment.Center;
            circle.Child = check;
            checkWrap.Children.Add(circle);
            stack.Children.Add(checkWrap);

            _doneTitle = Ui.Text("安装完成", 23, FontWeights.SemiBold, Ui.TextBrush);
            _doneTitle.HorizontalAlignment = HorizontalAlignment.Center;
            _doneTitle.Margin = new Thickness(0, 14, 0, 0);
            stack.Children.Add(_doneTitle);
            _doneBody = Ui.Text("HAVEGET DynamicIsland 已经准备好，去屏幕顶部试试它吧。", 10, FontWeights.Normal, Ui.SubBrush);
            _doneBody.HorizontalAlignment = HorizontalAlignment.Center;
            _doneBody.TextAlignment = TextAlignment.Center;
            _doneBody.Margin = new Thickness(0, 4, 0, 0);
            stack.Children.Add(_doneBody);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Center;
            buttons.Margin = new Thickness(0, 24, 0, 0);
            _doneOpenButton = MakePrimaryButton("打开 DynamicIsland", 148, delegate { OpenInstalledApp(); });
            buttons.Children.Add(_doneOpenButton);
            Border finish = MakeGhostButton("完成", 90, delegate { TryClose(); });
            finish.Margin = new Thickness(9, 0, 0, 0);
            buttons.Children.Add(finish);
            stack.Children.Add(buttons);
            return view;
        }

        private Grid BuildErrorView()
        {
            Grid view = new Grid();
            StackPanel stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.VerticalAlignment = VerticalAlignment.Center;
            view.Children.Add(stack);

            Border iconCircle = new Border();
            iconCircle.Width = 58;
            iconCircle.Height = 58;
            iconCircle.CornerRadius = new CornerRadius(29);
            iconCircle.Background = Ui.Alpha(Colors.White, 8);
            iconCircle.HorizontalAlignment = HorizontalAlignment.Center;
            FrameworkElement icon = Ui.Icon("M12 3 L22 21 H2 Z M12 9 V14 M12 17.4 V18", Ui.SubBrush, 27, 1.8);
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            iconCircle.Child = icon;
            stack.Children.Add(iconCircle);

            _errorTitle = Ui.Text("安装遇到问题", 18, FontWeights.SemiBold, Ui.TextBrush);
            _errorTitle.HorizontalAlignment = HorizontalAlignment.Center;
            _errorTitle.Margin = new Thickness(0, 12, 0, 0);
            stack.Children.Add(_errorTitle);
            _errorBody = Ui.Text("", 9.5, FontWeights.Normal, Ui.SubBrush);
            _errorBody.HorizontalAlignment = HorizontalAlignment.Center;
            _errorBody.TextAlignment = TextAlignment.Center;
            _errorBody.TextWrapping = TextWrapping.Wrap;
            _errorBody.MaxWidth = 460;
            _errorBody.Margin = new Thickness(0, 5, 0, 0);
            stack.Children.Add(_errorBody);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Center;
            buttons.Margin = new Thickness(0, 20, 0, 0);
            _errorRetryButton = MakePrimaryButton("重试", 100, delegate { if (_errorRetryAction != null) _errorRetryAction(); });
            buttons.Children.Add(_errorRetryButton);
            _errorDownloadButton = MakeGhostButton("打开下载页", 110, delegate { InstallerEngine.OpenDownloadPage(); });
            _errorDownloadButton.Margin = new Thickness(9, 0, 0, 0);
            buttons.Children.Add(_errorDownloadButton);
            Border cancel = MakeGhostButton("取消", 76, delegate { TryClose(); });
            cancel.Margin = new Thickness(9, 0, 0, 0);
            buttons.Children.Add(cancel);
            stack.Children.Add(buttons);
            return view;
        }

        private Grid BuildUninstallView()
        {
            Grid view = new Grid();
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid hero = new Grid();
            hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Border iconCircle = new Border();
            iconCircle.Width = 52;
            iconCircle.Height = 52;
            iconCircle.CornerRadius = new CornerRadius(16);
            iconCircle.Background = Ui.Alpha(Colors.White, 8);
            iconCircle.HorizontalAlignment = HorizontalAlignment.Left;
            iconCircle.VerticalAlignment = VerticalAlignment.Center;
            FrameworkElement icon = Ui.Icon(Ico.Trash, Ui.SubBrush, 23, 1.5);
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            iconCircle.Child = icon;
            hero.Children.Add(iconCircle);
            StackPanel titles = new StackPanel();
            titles.Margin = new Thickness(14, 0, 0, 0);
            titles.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(titles, 1);
            TextBlock title = Ui.Text("卸载 HAVEGET DynamicIsland", 22, FontWeights.SemiBold, Ui.TextBrush);
            titles.Children.Add(title);
            TextBlock sub = Ui.Text("确定要从这台电脑移除 HAVEGET DynamicIsland 吗？", 9.8, FontWeights.Normal, Ui.SubBrush);
            sub.Margin = new Thickness(0, 2, 0, 0);
            titles.Children.Add(sub);
            hero.Children.Add(titles);
            view.Children.Add(hero);

            Border info = new Border();
            info.Background = Ui.Alpha(Colors.White, 6);
            info.BorderBrush = Ui.Alpha(Colors.White, 9);
            info.BorderThickness = new Thickness(1);
            info.CornerRadius = new CornerRadius(16);
            info.Padding = new Thickness(14, 11, 14, 11);
            info.Margin = new Thickness(0, 24, 0, 0);
            info.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(info, 1);
            view.Children.Add(info);
            StackPanel infoStack = new StackPanel();
            info.Child = infoStack;
            _uninstallDetail = Ui.Text("将删除程序文件、桌面快捷方式、开始菜单项和开机启动项。", 9.5, FontWeights.Normal, Ui.SubBrush);
            _uninstallDetail.TextWrapping = TextWrapping.Wrap;
            infoStack.Children.Add(_uninstallDetail);
            TextBlock keep = Ui.Text("默认保留你的 HAVEGET DynamicIsland 设置，方便以后重新安装。", 8.7, FontWeights.Normal, Ui.FaintBrush);
            keep.Margin = new Thickness(0, 4, 0, 0);
            infoStack.Children.Add(keep);
            infoStack.Children.Add(MakeCheckRow("同时删除本地数据", "HAVEGET DynamicIsland 设置和本地日志", false, delegate(bool on) { _removeData = on; }));

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetRow(buttons, 3);
            view.Children.Add(buttons);
            Border uninstall = MakeDangerButton("卸载", 106, delegate { StartUninstall(); });
            buttons.Children.Add(uninstall);
            Border cancel = MakeGhostButton("取消", 78, delegate { TryClose(); });
            cancel.Margin = new Thickness(9, 0, 0, 0);
            buttons.Children.Add(cancel);
            return view;
        }

        private Border MakeLogo(double size)
        {
            Border outer = new Border();
            outer.Width = size;
            outer.Height = size;
            outer.CornerRadius = new CornerRadius(size * 0.26);
            outer.Background = Ui.Brush("#14161A");
            outer.BorderBrush = Ui.Alpha(Colors.White, 14);
            outer.BorderThickness = new Thickness(1);
            Grid g = new Grid();
            outer.Child = g;
            Border capsule = new Border();
            capsule.Width = size * 0.68;
            capsule.Height = size * 0.27;
            capsule.CornerRadius = new CornerRadius(size * 0.14);
            capsule.Background = Ui.Brush("#050608");
            capsule.BorderBrush = Ui.Alpha(Colors.White, 32);
            capsule.BorderThickness = new Thickness(1);
            capsule.HorizontalAlignment = HorizontalAlignment.Center;
            capsule.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(capsule);
            Ellipse dot = Ui.Dot(size * 0.13, Ui.AccentBrush);
            dot.HorizontalAlignment = HorizontalAlignment.Left;
            dot.VerticalAlignment = VerticalAlignment.Center;
            dot.Margin = new Thickness(size * 0.20, 0, 0, 0);
            g.Children.Add(dot);
            Border battery = new Border();
            battery.Width = size * 0.2;
            battery.Height = size * 0.1;
            battery.CornerRadius = new CornerRadius(2);
            battery.Background = Ui.AccentBrush;
            battery.HorizontalAlignment = HorizontalAlignment.Right;
            battery.VerticalAlignment = VerticalAlignment.Center;
            battery.Margin = new Thickness(0, 0, size * 0.20, 0);
            battery.Opacity = 0.9;
            g.Children.Add(battery);
            return outer;
        }

        private Border MakeCheckRow(string title, string description, bool initial, Action<bool> changed)
        {
            Border row = new Border();
            row.Height = 31;
            row.CornerRadius = new CornerRadius(8);
            row.Background = Brushes.Transparent;
            row.Cursor = Cursors.Hand;
            row.MouseEnter += delegate { row.Background = Ui.Alpha(Colors.White, 6); };
            row.MouseLeave += delegate { row.Background = Brushes.Transparent; };
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Child = g;
            Border box = new Border();
            box.Width = 16;
            box.Height = 16;
            box.CornerRadius = new CornerRadius(5);
            box.BorderBrush = Ui.Alpha(Colors.White, 34);
            box.BorderThickness = new Thickness(1);
            box.Background = Ui.Alpha(Colors.White, 10);
            box.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(box);
            FrameworkElement check = Ui.Icon(Ico.Check, Ui.Brush("#06101A"), 10, 2.0);
            check.HorizontalAlignment = HorizontalAlignment.Center;
            check.VerticalAlignment = VerticalAlignment.Center;
            check.Visibility = Visibility.Collapsed;
            box.Child = check;
            TextBlock label = Ui.Text(title, 9.4, FontWeights.Medium, Ui.TextBrush);
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Margin = new Thickness(7, 0, 0, 0);
            Grid.SetColumn(label, 1);
            g.Children.Add(label);
            TextBlock desc = Ui.Text(description, 8.1, FontWeights.Normal, Ui.FaintBrush);
            desc.VerticalAlignment = VerticalAlignment.Center;
            desc.TextTrimming = TextTrimming.CharacterEllipsis;
            desc.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(desc, 2);
            g.Children.Add(desc);

            bool state = initial;
            Action apply = delegate
            {
                box.Background = state ? Ui.AccentBrush : (Brush)Ui.Alpha(Colors.White, 10);
                box.BorderBrush = state ? Brushes.Transparent : (Brush)Ui.Alpha(Colors.White, 28);
                check.Visibility = state ? Visibility.Visible : Visibility.Collapsed;
            };
            apply();
            row.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                state = !state;
                apply();
                if (changed != null) changed(state);
            };
            return row;
        }

        private Border MakePrimaryButton(string text, double width, Action click)
        {
            Border b = new Border();
            b.Width = width;
            b.Height = 40;
            b.CornerRadius = new CornerRadius(11);
            b.Background = Ui.AccentBrush;
            b.Cursor = Cursors.Hand;
            TextBlock t = Ui.Text(text, 10.8, FontWeights.SemiBold, Ui.InkBrush);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Opacity = 0.88; };
            b.MouseLeave += delegate { b.Opacity = 1.0; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            return b;
        }

        private Border MakeGhostButton(string text, double width, Action click)
        {
            Border b = new Border();
            b.Width = width;
            b.Height = 40;
            b.CornerRadius = new CornerRadius(11);
            b.Background = Ui.Alpha(Colors.White, 8);
            b.Cursor = Cursors.Hand;
            TextBlock t = Ui.Text(text, 10.2, FontWeights.Normal, Ui.SubBrush);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.Alpha(Colors.White, 13); };
            b.MouseLeave += delegate { b.Background = Ui.Alpha(Colors.White, 8); };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            return b;
        }

        private Border MakeDangerButton(string text, double width, Action click)
        {
            Border b = new Border();
            b.Width = width;
            b.Height = 40;
            b.CornerRadius = new CornerRadius(11);
            b.Background = Ui.Alpha(Colors.White, 8);
            b.Cursor = Cursors.Hand;
            TextBlock t = Ui.Text(text, 10.6, FontWeights.Medium, Ui.TextBrush);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.Alpha(Colors.White, 13); };
            b.MouseLeave += delegate { b.Background = Ui.Alpha(Colors.White, 8); };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            return b;
        }

        private void ShowView(Grid view, bool animate)
        {
            Grid[] all = new Grid[] { _readyView, _progressView, _doneView, _errorView, _uninstallView };
            foreach (Grid v in all)
            {
                if (v == view) continue;
                v.Visibility = Visibility.Collapsed;
                v.BeginAnimation(UIElement.OpacityProperty, null);
            }
            view.Visibility = Visibility.Visible;
            if (animate)
            {
                view.Opacity = 0;
                Ui.PopIn(view, 0, 0.975, 10);
            }
            else
            {
                view.Opacity = 1;
            }
        }

        public void BeginInstall()
        {
            StartInstall();
        }

        public void BeginUninstall()
        {
            StartUninstall();
        }

        private void StartInstall()
        {
            if (_busy) return;
            _busy = true;
            _completed = false;
            ShowView(_progressView, true);
            SetProgress(0.02, "正在检查运行环境…", "正在确认 Windows 版本与 .NET Framework 组件。");
            Thread thread = new Thread(delegate()
            {
                DateTime started = DateTime.UtcNow;
                try
                {
                    RuntimeResult runtime = InstallerEngine.EnsureDotNet(delegate(double pct, string message)
                    {
                        Post(delegate
                        {
                            SetProgress(0.03 + pct * 0.42, "正在补全运行环境…", message);
                        });
                    });
                    if (!runtime.Success)
                    {
                        Post(delegate { ShowError("运行环境未就绪", runtime.Message, true, delegate { StartInstall(); }); });
                        return;
                    }
                    _rebootRequired = runtime.RebootRequired;
                    Post(delegate { SetProgress(0.50, "正在安装 HAVEGET DynamicIsland…", "正在写入程序文件与快捷方式。"); });

                    InstallResult result = InstallerEngine.Install(_options, delegate(double pct, string message)
                    {
                        Post(delegate
                        {
                            SetProgress(0.50 + pct * 0.48, "正在安装 HAVEGET DynamicIsland…", message);
                        });
                    });
                    if (!result.Success)
                    {
                        Post(delegate { ShowError("安装未完成", result.Message, false, delegate { StartInstall(); }); });
                        return;
                    }
                    int elapsed = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                    if (elapsed < 700) Thread.Sleep(700 - elapsed);
                    Post(delegate { ShowDone(result); });
                }
                catch (Exception ex)
                {
                    SetupLog.Error("StartInstall", ex);
                    Post(delegate { ShowError("安装未完成", ex.Message, false, delegate { StartInstall(); }); });
                }
            });
            thread.IsBackground = true;
            thread.Name = "HAVEGETDynamicIslandSetup";
            thread.Start();
        }

        private void StartUninstall()
        {
            if (_busy) return;
            _busy = true;
            _completed = false;
            ShowView(_progressView, true);
            SetProgress(0.05, "正在卸载 HAVEGET DynamicIsland…", "正在关闭程序并清理安装文件。");
            Thread thread = new Thread(delegate()
            {
                try
                {
                    InstallResult result = InstallerEngine.Uninstall(_removeData, delegate(double pct, string message)
                    {
                        Post(delegate { SetProgress(0.05 + pct * 0.92, "正在卸载 HAVEGET DynamicIsland…", message); });
                    });
                    if (!result.Success)
                    {
                        Post(delegate { ShowError("卸载未完成", result.Message, false, delegate { StartUninstall(); }); });
                        return;
                    }
                    Post(delegate
                    {
                        _busy = false;
                        _completed = true;
                        _doneTitle.Text = "卸载完成";
                        _doneBody.Text = _removeData ? "程序与本地数据已从这台电脑移除。" : "程序已移除，HAVEGET DynamicIsland 设置已保留。";
                        _doneOpenButton.Visibility = Visibility.Collapsed;
                        ShowView(_doneView, true);
                    });
                }
                catch (Exception ex)
                {
                    SetupLog.Error("StartUninstall", ex);
                    Post(delegate { ShowError("卸载未完成", ex.Message, false, delegate { StartUninstall(); }); });
                }
            });
            thread.IsBackground = true;
            thread.Name = "DynamicIslandUninstall";
            thread.Start();
        }

        private void ShowDone(InstallResult result)
        {
            _busy = false;
            _completed = true;
            _installedAppPath = result.AppPath;
            _doneTitle.Text = _rebootRequired ? "安装完成，建议重启" : "安装完成";
            _doneBody.Text = _rebootRequired
                ? "运行环境也已更新，重启电脑后即可打开 HAVEGET DynamicIsland。"
                : (string.IsNullOrEmpty(result.Warning)
                    ? "HAVEGET DynamicIsland 已经准备好，去屏幕顶部试试它吧。"
                    : result.Warning + "\n程序已可直接从安装目录启动。");
            _doneBody.TextWrapping = TextWrapping.Wrap;
            _doneBody.MaxWidth = 460;
            _doneOpenButton.Visibility = Visibility.Visible;
            ShowView(_doneView, true);
            Ui.Bounce(_doneOpenButton, 0.04);
        }

        private void ShowError(string title, string body, bool showDownload, Action retry)
        {
            _busy = false;
            _errorTitle.Text = title;
            _errorBody.Text = body ?? "";
            _errorRetryAction = retry;
            _errorDownloadButton.Visibility = showDownload ? Visibility.Visible : Visibility.Collapsed;
            _errorRetryButton.Visibility = retry != null ? Visibility.Visible : Visibility.Collapsed;
            ShowView(_errorView, true);
        }

        private void SetProgress(double fraction, string status, string detail)
        {
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            _statusText.Text = status ?? "";
            _detailText.Text = detail ?? "";
            _percentText.Text = ((int)Math.Round(fraction * 100)) + "%";
            DoubleAnimation anim = new DoubleAnimation(fraction, new Duration(TimeSpan.FromMilliseconds(260)));
            anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            _progressScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        }

        private void Post(Action action)
        {
            if (action == null) return;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private void OpenInstalledApp()
        {
            try
            {
                if (!string.IsNullOrEmpty(_installedAppPath) && File.Exists(_installedAppPath))
                {
                    Process.Start(new ProcessStartInfo(_installedAppPath, "--expanded")
                    {
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(_installedAppPath)
                    });
                }
            }
            catch (Exception ex) { SetupLog.Error("OpenInstalledApp", ex); }
            TryClose();
        }

        private void TryClose()
        {
            if (_busy)
            {
                if (_statusText != null) _statusText.Text = "正在完成安装，请稍候…";
                return;
            }
            _allowClose = true;
            Close();
            Application.Current.Shutdown();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_busy)
            {
                e.Cancel = true;
                if (_statusText != null) _statusText.Text = "正在完成安装，请稍候…";
                return;
            }
            if (!_allowClose && !_completed && !_uninstallMode)
            {
                // Closing the installer before doing anything is harmless.
            }
        }
    }
}
