using System;
using System.Drawing;
using System.Windows.Forms;

namespace HavegetDynamicIsland
{
    public sealed class TrayController : IDisposable
    {
        private readonly IslandWindow _window;
        private readonly AppSettings _settings;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _startupItem;
        private readonly ToolStripMenuItem _topmostItem;
        private readonly ToolStripMenuItem _autoCollapseItem;
        private readonly ToolStripMenuItem _visibilityItem;
        private readonly Icon _icon;
        private bool _disposed;

        public TrayController(IslandWindow window, AppSettings settings)
        {
            _window = window;
            _settings = settings;

            _icon = Icon.ExtractAssociatedIcon(AppPaths.ExecutablePath);
            if (_icon == null) _icon = SystemIcons.Application;

            _menu = new ContextMenuStrip();
            _menu.ShowImageMargin = false;
            _menu.ShowCheckMargin = true;
            _menu.Renderer = new DarkMenuRenderer();
            _menu.Font = new Font("Microsoft YaHei UI", 9f);

            ToolStripMenuItem header = new ToolStripMenuItem("HAVEGET DynamicIsland");
            header.Enabled = false;
            header.Font = new Font("Microsoft YaHei UI", 9f, System.Drawing.FontStyle.Bold);
            _menu.Items.Add(header);
            ToolStripMenuItem version = new ToolStripMenuItem("Windows · v1.2.6");
            version.Enabled = false;
            _menu.Items.Add(version);
            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem open = MakeItem("打开灵动岛");
            open.Font = new Font("Microsoft YaHei UI", 9f, System.Drawing.FontStyle.Bold);
            open.Click += delegate { OnUi(delegate { _window.ShowIsland(-1); }); };
            _menu.Items.Add(open);

            string[] pageNames = new string[] { "音乐", "通知", "系统状态", "剪贴板", "通知中心", "控制中心", "日历日程", "中转站", "设置" };
            for (int i = 0; i < pageNames.Length; i++)
            {
                int page = i;
                ToolStripMenuItem pageItem = MakeItem(pageNames[i]);
                pageItem.Click += delegate { OnUi(delegate { _window.ShowIsland(page); }); };
                _menu.Items.Add(pageItem);
            }

            ToolStripMenuItem preview = MakeItem("通知速览");
            preview.Click += delegate { OnUi(delegate { _window.ShowNotificationPreview(); }); };
            _menu.Items.Add(preview);
            _menu.Items.Add(new ToolStripSeparator());

            _topmostItem = MakeItem("始终置顶");
            _topmostItem.CheckOnClick = false;
            _topmostItem.Click += delegate
            {
                _settings.Topmost = !_settings.Topmost;
                _settings.Save();
                _window.ApplyTopmost(_settings.Topmost);
            };
            _menu.Items.Add(_topmostItem);

            _autoCollapseItem = MakeItem("点击外部自动收起");
            _autoCollapseItem.CheckOnClick = false;
            _autoCollapseItem.Click += delegate
            {
                _settings.AutoCollapse = !_settings.AutoCollapse;
                _settings.Save();
                if (!_settings.AutoCollapse) _window.CancelAutoCollapse();
            };
            _menu.Items.Add(_autoCollapseItem);

            _startupItem = MakeItem("开机自动启动");
            _startupItem.CheckOnClick = false;
            _startupItem.Click += delegate
            {
                bool next = !Startup.IsEnabled;
                Startup.SetEnabled(next);
                _settings.StartWithWindows = next;
                _settings.Save();
                _startupItem.Checked = next;
            };
            _menu.Items.Add(_startupItem);

            _visibilityItem = MakeItem("隐藏灵动岛");
            _visibilityItem.Click += delegate { OnUi(delegate { _window.ToggleVisible(); }); };
            _menu.Items.Add(_visibilityItem);
            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = MakeItem("退出 HAVEGET DynamicIsland");
            exit.ForeColor = Color.FromArgb(255, 142, 142);
            exit.Click += delegate { OnUi(delegate { _window.ExitApplication(); }); };
            _menu.Items.Add(exit);

            _menu.Opening += delegate
            {
                _startupItem.Checked = Startup.IsEnabled;
                _topmostItem.Checked = _settings.Topmost;
                _autoCollapseItem.Checked = _settings.AutoCollapse;
                _visibilityItem.Text = _window.IsVisible ? "隐藏灵动岛" : "显示灵动岛";
            };

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = _icon;
            _notifyIcon.Text = "HAVEGET DynamicIsland";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = _menu;
            _notifyIcon.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) OnUi(delegate { _window.ToggleVisible(); });
            };
            _notifyIcon.DoubleClick += delegate { OnUi(delegate { _window.ShowIsland(-1); }); };
        }

        private static ToolStripMenuItem MakeItem(string text)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Padding = new Padding(2, 0, 2, 0);
            return item;
        }

        private void OnUi(Action action)
        {
            if (action == null) return;
            _window.Dispatcher.BeginInvoke(action);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _notifyIcon.Visible = false; _notifyIcon.Dispose(); } catch { }
            try { _menu.Dispose(); } catch { }
            try { if (_icon != null) _icon.Dispose(); } catch { }
        }

        private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkColorTable())
            {
                RoundedEdges = true;
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Selected ? Color.FromArgb(250, 253, 255) : Color.FromArgb(206, 216, 228);
                if (!e.Item.Enabled) e.TextColor = Color.FromArgb(110, 122, 137);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected) return;
                Rectangle r = new Rectangle(5, 2, e.Item.Width - 10, e.Item.Height - 4);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(26, 38, 52)))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    FillRounded(e.Graphics, b, r, 6);
                }
            }

            private static void FillRounded(Graphics g, Brush brush, Rectangle r, int radius)
            {
                using (System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int d = radius * 2;
                    p.AddArc(r.X, r.Y, d, d, 180, 90);
                    p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                    p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                    p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                    p.CloseFigure();
                    g.FillPath(brush, p);
                }
            }
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(11, 15, 21); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(11, 15, 21); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(11, 15, 21); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(11, 15, 21); } }
            public override Color MenuBorder { get { return Color.FromArgb(43, 55, 70); } }
            public override Color MenuItemBorder { get { return Color.Transparent; } }
            public override Color MenuItemSelected { get { return Color.FromArgb(26, 38, 52); } }
            public override Color SeparatorDark { get { return Color.FromArgb(43, 55, 70); } }
            public override Color SeparatorLight { get { return Color.FromArgb(43, 55, 70); } }
            public override Color CheckBackground { get { return AccentDrawing(); } }
            public override Color CheckSelectedBackground { get { return AccentDrawing(); } }
            public override Color CheckPressedBackground { get { return AccentDrawing(); } }
            private static Color AccentDrawing()
            {
                return Color.FromArgb(Ui.AccentColor.R, Ui.AccentColor.G, Ui.AccentColor.B);
            }
        }
    }
}
