using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace HavegetDynamicIsland
{
    public sealed class UiTheme
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Color Accent;
        public readonly Color Deep;
        public readonly Color Pale;
        public readonly Color Muted;
        public readonly Color Ink;

        public UiTheme(string id, string name, string accent, string deep, string pale, string muted, string ink)
        {
            Id = id;
            Name = name;
            Accent = Ui.ColorFromHex(accent);
            Deep = Ui.ColorFromHex(deep);
            Pale = Ui.ColorFromHex(pale);
            Muted = Ui.ColorFromHex(muted);
            Ink = Ui.ColorFromHex(ink);
        }
    }

    public static class Ui
    {
        public static readonly Color BgTop = ColorFromHex("#0A0F16");
        public static readonly Color BgBottom = ColorFromHex("#04070B");
        public static readonly Color TextColor = ColorFromHex("#F5F9FF");
        public static readonly Color SubColor = ColorFromHex("#A7B3C4");
        public static readonly Color FaintColor = ColorFromHex("#707C8C");
        public static readonly Color DangerColor = ColorFromHex("#FF7676");
        public static readonly Color WarnColor = ColorFromHex("#FFC46B");

        public static readonly UiTheme[] Themes = new UiTheme[]
        {
            new UiTheme("azure", "蔚蓝", "#3B9EFF", "#1C6DC9", "#BCDDFF", "#7FA7C9", "#04111F"),
            new UiTheme("mint", "薄荷", "#25CFA2", "#0C8F70", "#B6F4E1", "#78B7A3", "#04140F"),
            new UiTheme("violet", "紫罗兰", "#A98BFF", "#6C4AD9", "#E4DAFF", "#9C8BC8", "#10081F"),
            new UiTheme("amber", "琥珀", "#FFB454", "#C27A16", "#FFE3B5", "#C7A574", "#1A1003"),
            new UiTheme("rose", "玫瑰", "#FF7FB8", "#C44380", "#FFD6E8", "#C78FA9", "#1C0712"),
            new UiTheme("graphite", "石墨", "#D8DCE4", "#848C99", "#F3F5F8", "#A2A9B3", "#0B0D11")
        };

        public static UiTheme CurrentTheme = Themes[0];
        public static Color AccentColor = Themes[0].Accent;
        public static Color AccentDeepColor = Themes[0].Deep;
        public static Color AccentPaleColor = Themes[0].Pale;
        public static Color AccentMutedColor = Themes[0].Muted;
        public static Color InkColor = Themes[0].Ink;

        public static readonly FontFamily UIFont = new FontFamily("Microsoft YaHei UI");
        public static readonly FontFamily DisplayFont = PickDisplayFont();
        public static readonly FontFamily MonoFont = PickMonoFont();

        public static readonly SolidColorBrush TextBrush = Frozen(new SolidColorBrush(TextColor));
        public static readonly SolidColorBrush SubBrush = Frozen(new SolidColorBrush(SubColor));
        public static readonly SolidColorBrush FaintBrush = Frozen(new SolidColorBrush(FaintColor));
        public static readonly SolidColorBrush AccentBrush = new SolidColorBrush(AccentColor);
        public static readonly SolidColorBrush AccentDeepBrush = new SolidColorBrush(AccentDeepColor);
        public static readonly SolidColorBrush AccentPaleBrush = new SolidColorBrush(AccentPaleColor);
        public static readonly SolidColorBrush AccentMutedBrush = new SolidColorBrush(AccentMutedColor);
        public static readonly SolidColorBrush DangerBrush = Frozen(new SolidColorBrush(DangerColor));
        public static readonly SolidColorBrush WarnBrush = Frozen(new SolidColorBrush(WarnColor));
        public static readonly SolidColorBrush InkBrush = new SolidColorBrush(InkColor);
        public static readonly SolidColorBrush WhiteBrush = Brushes.White;

        private static readonly Dictionary<byte, SolidColorBrush> AccentTints = new Dictionary<byte, SolidColorBrush>();

        public static SolidColorBrush AccentTint(byte alpha)
        {
            SolidColorBrush brush;
            if (!AccentTints.TryGetValue(alpha, out brush))
            {
                brush = new SolidColorBrush(Color.FromArgb(alpha, AccentColor.R, AccentColor.G, AccentColor.B));
                AccentTints[alpha] = brush;
            }
            return brush;
        }

        public static UiTheme ThemeById(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (UiTheme theme in Themes)
                {
                    if (string.Equals(theme.Id, id, StringComparison.OrdinalIgnoreCase)) return theme;
                }
            }
            return Themes[0];
        }

        public static void ApplyTheme(string id)
        {
            UiTheme theme = ThemeById(id);
            CurrentTheme = theme;
            AccentColor = theme.Accent;
            AccentDeepColor = theme.Deep;
            AccentPaleColor = theme.Pale;
            AccentMutedColor = theme.Muted;
            InkColor = theme.Ink;
            AccentBrush.Color = AccentColor;
            AccentDeepBrush.Color = AccentDeepColor;
            AccentPaleBrush.Color = AccentPaleColor;
            AccentMutedBrush.Color = AccentMutedColor;
            InkBrush.Color = InkColor;
            foreach (SolidColorBrush tinted in AccentTints.Values)
            {
                Color c = tinted.Color;
                tinted.Color = Color.FromArgb(c.A, AccentColor.R, AccentColor.G, AccentColor.B);
            }
        }

        public static string ToHex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
        private static FontFamily PickDisplayFont()
        {
            try
            {
                foreach (FontFamily f in Fonts.SystemFontFamilies)
                {
                    if (string.Equals(f.Source, "Segoe UI Variable Display", StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            catch { }
            return new FontFamily("Segoe UI");
        }

        private static FontFamily PickMonoFont()
        {
            try
            {
                foreach (FontFamily f in Fonts.SystemFontFamilies)
                {
                    if (string.Equals(f.Source, "Cascadia Mono", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f.Source, "Cascadia Code", StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            catch { }
            return new FontFamily("Consolas");
        }

        public static Color ColorFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Colors.White;
            hex = hex.TrimStart('#');
            if (hex.Length == 8)
            {
                return Color.FromArgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            }
            return Color.FromRgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        public static SolidColorBrush Brush(string hex)
        {
            return Frozen(new SolidColorBrush(ColorFromHex(hex)));
        }

        public static SolidColorBrush Alpha(Color color, byte alpha)
        {
            return Frozen(new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B)));
        }

        public static SolidColorBrush Alpha(string hex, byte alpha)
        {
            Color c = ColorFromHex(hex);
            return Frozen(new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B)));
        }

        public static T Frozen<T>(T freezable) where T : Freezable
        {
            if (freezable.CanFreeze) freezable.Freeze();
            return freezable;
        }

        public static LinearGradientBrush Gradient(string first, string second, double angle)
        {
            LinearGradientBrush b = new LinearGradientBrush();
            b.StartPoint = new Point(0, 0);
            b.EndPoint = new Point(1, 0);
            b.GradientStops.Add(new GradientStop(ColorFromHex(first), 0));
            b.GradientStops.Add(new GradientStop(ColorFromHex(second), 1));
            if (Math.Abs(angle - 90) < 0.1)
            {
                b.StartPoint = new Point(0, 0);
                b.EndPoint = new Point(0, 1);
            }
            else if (Math.Abs(angle - 135) < 0.1)
            {
                b.StartPoint = new Point(0, 0);
                b.EndPoint = new Point(1, 1);
            }
            return Frozen(b);
        }

        public static TextBlock Text(string text, double size, FontWeight weight, Brush brush)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontFamily = UIFont;
            t.FontSize = size;
            t.FontWeight = weight;
            t.Foreground = brush;
            TextOptions.SetTextFormattingMode(t, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(t, TextRenderingMode.ClearType);
            return t;
        }

        public static TextBlock Text(string text, double size, FontWeight weight, Brush brush, FontFamily family)
        {
            TextBlock t = Text(text, size, weight, brush);
            t.FontFamily = family;
            return t;
        }

        public static TextBlock Tabular(TextBlock text)
        {
            if (text != null) Typography.SetNumeralAlignment(text, FontNumeralAlignment.Tabular);
            return text;
        }

        public static Border Card(Brush background, double radius)
        {
            Border b = new Border();
            b.Background = background;
            b.CornerRadius = new CornerRadius(radius);
            return b;
        }

        public static Border Round(Brush background, double radius, UIElement child)
        {
            Border b = Card(background, radius);
            if (child != null) b.Child = child;
            return b;
        }

        public static Ellipse Dot(double size, Brush fill)
        {
            Ellipse e = new Ellipse();
            e.Width = size;
            e.Height = size;
            e.Fill = fill;
            return e;
        }

        public static DropShadowEffect Glow(Color color, double blur, double opacity, double depth)
        {
            DropShadowEffect d = new DropShadowEffect();
            d.Color = color;
            d.BlurRadius = blur;
            d.ShadowDepth = depth;
            d.Opacity = opacity;
            d.Direction = 270;
            return d;
        }

        public static FrameworkElement Icon(string data, Brush stroke, double size, double thickness)
        {
            return Icon(data, stroke, size, thickness, false);
        }

        public static FrameworkElement Icon(string data, Brush stroke, double size, double thickness, bool fill)
        {
            Path p = new Path();
            p.Data = Geometry.Parse(data);
            p.Stroke = stroke;
            p.StrokeThickness = thickness;
            p.StrokeStartLineCap = PenLineCap.Round;
            p.StrokeEndLineCap = PenLineCap.Round;
            p.StrokeLineJoin = PenLineJoin.Round;
            if (fill)
            {
                p.Fill = stroke;
                p.StrokeThickness = 0;
            }
            Viewbox v = new Viewbox();
            v.Width = size;
            v.Height = size;
            v.Stretch = Stretch.Uniform;
            v.Child = p;
            return v;
        }

        public static Border BrandMark(double size)
        {
            Border mark = new Border();
            mark.Width = size;
            mark.Height = size;
            mark.CornerRadius = new CornerRadius(size * 0.30);
            LinearGradientBrush bg = new LinearGradientBrush();
            bg.StartPoint = new Point(0, 0);
            bg.EndPoint = new Point(1, 1);
            bg.GradientStops.Add(new GradientStop(AccentPaleColor, 0));
            bg.GradientStops.Add(new GradientStop(AccentDeepColor, 1));
            bg.Freeze();
            mark.Background = bg;
            mark.BorderBrush = Alpha(Colors.White, 42);
            mark.BorderThickness = new Thickness(Math.Max(0.6, size * 0.012));
            FrameworkElement child = Icon(Ico.HMark, WhiteBrush, size * 0.55, Math.Max(1.5, size * 0.105));
            child.HorizontalAlignment = HorizontalAlignment.Center;
            child.VerticalAlignment = VerticalAlignment.Center;
            mark.Child = child;
            return mark;
        }

        public static Border IconButton(string iconData, string tooltip, double size, Action click)
        {
            Grid g = new Grid();
            g.Children.Add(Icon(iconData, SubBrush, size * 0.48, 1.6));
            Border b = new Border();
            b.Width = size;
            b.Height = size;
            b.CornerRadius = new CornerRadius(size / 2);
            b.Background = Brushes.Transparent;
            b.Child = g;
            b.Cursor = System.Windows.Input.Cursors.Hand;
            b.ToolTip = tooltip;
            b.MouseEnter += delegate { b.Background = Alpha(Colors.White, 16); };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; };
            b.MouseLeftButtonUp += delegate(object s, System.Windows.Input.MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            return b;
        }

        public static void PopIn(FrameworkElement element, int delayMs, double fromScale, double distance)
        {
            if (element == null) return;
            ScaleTransform scale = new ScaleTransform(fromScale, fromScale);
            TranslateTransform translate = new TranslateTransform(0, distance);
            TransformGroup group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(translate);
            element.RenderTransform = group;
            element.RenderTransformOrigin = new Point(0.5, 0.42);
            element.Opacity = 1;

            Duration duration = new Duration(TimeSpan.FromMilliseconds(210));
            DoubleAnimation sx = new DoubleAnimation(fromScale, 1, duration);
            sx.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            sx.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            DoubleAnimation sy = new DoubleAnimation(fromScale, 1, duration);
            sy.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            sy.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            DoubleAnimation tr = new DoubleAnimation(distance, 0, new Duration(TimeSpan.FromMilliseconds(230)));
            tr.BeginTime = TimeSpan.FromMilliseconds(delayMs + 20);
            tr.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            translate.BeginAnimation(TranslateTransform.YProperty, tr);
        }

        public static void FadeIn(FrameworkElement element, int delayMs, int ms)
        {
            if (element == null) return;
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 0;
            DoubleAnimation fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(ms)));
            fade.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        public static void Bounce(UIElement element, double amount)
        {
            // The island stays calm; feedback is conveyed with color, not bounce.
        }

        public static string ShortRelative(DateTime localTime)
        {
            TimeSpan d = DateTime.Now - localTime;
            if (d.TotalSeconds < 45) return "刚刚";
            if (d.TotalMinutes < 60) return ((int)d.TotalMinutes) + " 分钟前";
            if (d.TotalHours < 24) return ((int)d.TotalHours) + " 小时前";
            if (d.TotalDays < 7) return ((int)d.TotalDays) + " 天前";
            return localTime.ToString("MM/dd");
        }
    }

    public static class Ico
    {
        public const string Close = "M7 7 L17 17 M17 7 L7 17";
        public const string Plus = "M12 5 V19 M5 12 H19";
        public const string Folder = "M3 7.5 A2.5 2.5 0 0 1 5.5 5 H9.2 L11.4 7.2 H18.5 A2.5 2.5 0 0 1 21 9.7 V17.5 A2.5 2.5 0 0 1 18.5 20 H5.5 A2.5 2.5 0 0 1 3 17.5 Z";
        public const string Bell = "M6.2 16.8 H17.8 L16.3 14.2 V10.2 A4.3 4.3 0 0 0 7.7 10.2 V14.2 Z M10 20 A2.1 2.1 0 0 0 14 20";
        public const string Gear = "M4 7 H20 M4 12 H20 M4 17 H20 M9 9.3 A2.3 2.3 0 1 0 9 4.7 A2.3 2.3 0 1 0 9 9.3 M15 14.3 A2.3 2.3 0 1 0 15 9.7 A2.3 2.3 0 1 0 15 14.3 M11 19.3 A2.3 2.3 0 1 0 11 14.7 A2.3 2.3 0 1 0 11 19.3";
        public const string Battery = "M3 8 H17.5 A2 2 0 0 1 19.5 10 V14 A2 2 0 0 1 17.5 16 H3 A1.5 1.5 0 0 1 1.5 14.5 V9.5 A1.5 1.5 0 0 1 3 8 Z M21 10.5 V13.5 M4 10.2 H15.5 V13.8 H4 Z";
        public const string Bolt = "M13 2 L5 13 H11 L9.5 22 L19 10.5 H12.5 Z";
        public const string Trash = "M5 7 H19 M9.5 7 V5 H14.5 V7 M7 7 L8 21 H16 L17 7 M10.2 11 V17 M13.8 11 V17";
        public const string Check = "M5 12 L10 17 L19 7";
        public const string ChevronDown = "M5 9 L12 16 L19 9";
        public const string ChevronLeft = "M15 5 L8 12 L15 19";
        public const string ChevronRight = "M9 5 L16 12 L9 19";
        public const string Clock = "M12 21 A9 9 0 1 0 12 3 A9 9 0 1 0 12 21 M12 7 V12 L15.5 14";
        public const string Sparkle = "M12 2.5 L14.1 9.9 L21.5 12 L14.1 14.1 L12 21.5 L9.9 14.1 L2.5 12 L9.9 9.9 Z";
        public const string Refresh = "M20 12 A8 8 0 1 1 17.2 6 M20.2 3.5 V8.2 H15.5";
        public const string Music = "M9 18 V5 L19 3 V16 M9 18 A2.5 2.5 0 1 1 4 18 A2.5 2.5 0 1 1 9 18 M19 16 A2.5 2.5 0 1 1 14 16 A2.5 2.5 0 1 1 19 16";
        public const string Wifi = "M2.5 8.5 C6.5 4.5 17.5 4.5 21.5 8.5 M5.5 12 C8.5 9 15.5 9 18.5 12 M8.5 15.5 C10.5 13.5 13.5 13.5 15.5 15.5 M12 19.2 A1.2 1.2 0 1 0 12 16.8 A1.2 1.2 0 1 0 12 19.2";
        public const string Moon = "M20.5 15.5 A8.8 8.8 0 0 1 8.5 3.5 A8.8 8.8 0 1 0 20.5 15.5 Z";
        public const string Cpu = "M8 8 H16 V16 H8 Z M5 5 H19 V19 H5 Z M9 2 V5 M15 2 V5 M9 19 V22 M15 19 V22 M2 9 H5 M2 15 H5 M19 9 H22 M19 15 H22";
        public const string Memory = "M5 8 H19 V16 H5 Z M8 5 V8 M12 5 V8 M16 5 V8 M8 16 V19 M12 16 V19 M16 16 V19 M2 11 H5 M19 11 H22";
        public const string Disk = "M4 6 C4 4.2 20 4.2 20 6 V18 C20 19.8 4 19.8 4 18 Z M4 6 V18 M4 12 H20 C20 13.8 4 13.8 4 12";
        public const string Grid = "M4 4 H10 V10 H4 Z M14 4 H20 V10 H14 Z M4 14 H10 V20 H4 Z M14 14 H20 V20 H14 Z";
        public const string Play = "M8 5.5 L18.5 12 L8 18.5 Z";
        public const string Pause = "M8 5 H10.5 V19 H8 Z M13.5 5 H16 V19 H13.5 Z";
        public const string Prev = "M19 5 L9 12 L19 19 M8 5 V19";
        public const string Next = "M5 5 L15 12 L5 19 M16 5 V19";
        public const string Pulse = "M2.5 12 H6 L8.4 5.5 L12.5 19 L15.2 10 H21.5";
        public const string HMark = "M6 4.4 V19.6 M18 4.4 V19.6 M6 12 H18";
        public const string Alert = "M12 3 L22 21 H2 Z M12 9 V14 M12 17.3 V18";
        public const string ArrowUp = "M12 19 V5 M6 11 L12 5 L18 11";
        public const string Download = "M12 3 V15 M6.5 9.5 L12 15 L17.5 9.5 M4 20 H20";
        public const string Lock = "M6.5 11 H17.5 V20 H6.5 Z M9 11 V8.2 A3 3 0 0 1 15 8.2 V11";
        public const string Eye = "M2.5 12 C5.5 7.5 8.5 5.5 12 5.5 C15.5 5.5 18.5 7.5 21.5 12 C18.5 16.5 15.5 18.5 12 18.5 C8.5 18.5 5.5 16.5 2.5 12 Z M12 9.2 A2.8 2.8 0 1 0 12 14.8 A2.8 2.8 0 1 0 12 9.2";
        public const string Clipboard = "M8 5 H16 V20 H8 Z M9 5 V3.5 H15 V5 M10 9.5 H14 M10 12.5 H14 M10 15.5 H13";
        public const string Chat = "M4 5.5 A2.5 2.5 0 0 1 6.5 3 H17.5 A2.5 2.5 0 0 1 20 5.5 V14.5 A2.5 2.5 0 0 1 17.5 17 H11 L6.5 20.5 V17 H6.5 A2.5 2.5 0 0 1 4 14.5 Z M8 8 H16 M8 11.5 H14";
        public const string Calendar = "M4 6 H20 V20 H4 Z M4 10 H20 M8 3 V7 M16 3 V7 M7.5 13 H9.5 M11.25 13 H13.25 M15 13 H17 M7.5 16.5 H9.5 M11.25 16.5 H13.25 M15 16.5 H17";
        public const string Bluetooth = "M12 3 V21 L18 17 L7 8 M12 3 L18 7 L7 16";
        public const string Volume = "M4 9.5 H8 L12 5.5 V18.5 L8 14.5 H4 Z M16 9 A4 4 0 0 1 16 15 M18.5 6.5 A7.5 7.5 0 0 1 18.5 17.5";
        public const string Send = "M3 11.5 L21 3 L13.5 21 L10.5 13.5 Z M10.5 13.5 L21 3";
        public const string Image = "M3 5 H21 V19 H3 Z M3 16 L9 10 L14 15 L17 12 L21 16 M8 8.5 A1.5 1.5 0 1 0 8 9.5 A1.5 1.5 0 1 0 8 8.5";
        public const string List = "M4 6 H6 M9 6 H20 M4 12 H6 M9 12 H20 M4 18 H6 M9 18 H20";
    }
}
