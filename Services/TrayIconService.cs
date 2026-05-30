// Copyright (c) 2026
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinWidgetBattery.Models;

namespace WinWidgetBattery.Services;

public class TrayIconService : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    private NotifyIcon? _notifyIcon;
    private readonly Action _onAddWidget;
    private readonly Func<List<Models.WidgetSettings>> _getWidgets;
    private readonly Action<string> _onWidgetSettings;
    private readonly Action<string> _onWidgetRemove;
    private readonly Action _onAbout;
    private readonly Action _onExit;
    private readonly Func<BatteryInfo> _getBattery;

    // Level-based icons, keyed by state. Loaded once and reused.
    private readonly Dictionary<string, Icon> _levelIcons = new();
    private readonly Icon _fallbackIcon;
    private System.Windows.Threading.DispatcherTimer? _iconTimer;
    private string? _currentIconKey;

    public TrayIconService(
        Action onAddWidget,
        Func<List<Models.WidgetSettings>> getWidgets,
        Action<string> onWidgetSettings,
        Action<string> onWidgetRemove,
        Action onAbout,
        Action onExit,
        Func<BatteryInfo> getBattery)
    {
        _onAddWidget      = onAddWidget;
        _getWidgets       = getWidgets;
        _onWidgetSettings = onWidgetSettings;
        _onWidgetRemove   = onWidgetRemove;
        _onAbout          = onAbout;
        _onExit           = onExit;
        _getBattery       = getBattery;

        _fallbackIcon = LoadIcoIcon() ?? SystemIcons.Application;
        LoadLevelIcons();

        InitializeTrayIcon();
        InitializeIconTimer();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon    = _fallbackIcon,
            Visible = true,
            Text    = "Battery Widget"
        };
        _notifyIcon.DoubleClick += (_, _) => Invoke(() =>
            _onWidgetSettings(_getWidgets().FirstOrDefault()?.Id ?? ""));
        BuildMenu();
        UpdateBatteryIcon();
    }

    private void InitializeIconTimer()
    {
        _iconTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _iconTimer.Tick += (_, _) => UpdateBatteryIcon();
        _iconTimer.Start();
    }

    // ── Level-based tray icon ─────────────────────────────────────────────────

    private void UpdateBatteryIcon()
    {
        if (_notifyIcon == null) return;

        BatteryInfo info;
        try { info = _getBattery(); }
        catch { return; }

        string key = PickIconKey(info);
        if (key != _currentIconKey && _levelIcons.TryGetValue(key, out var icon))
        {
            _notifyIcon.Icon = icon;
            _currentIconKey  = key;
        }

        _notifyIcon.Text = info.HasBattery
            ? $"Battery Widget — {info.BatteryPercentage}%"
            : "Battery Widget";
    }

    // 100% (or no battery) → original battery.png; otherwise color by level.
    // Charging state is intentionally ignored for the color choice.
    private static string PickIconKey(BatteryInfo info)
    {
        if (!info.HasBattery || info.BatteryPercentage >= 100) return "full";
        if (info.BatteryPercentage >= 66) return "green";
        if (info.BatteryPercentage >= 33) return "yellow";
        return "red";
    }

    private void LoadLevelIcons()
    {
        TryAddLevelIcon("full",   "WinWidgetBattery.battery.png");
        TryAddLevelIcon("green",  "WinWidgetBattery.battery-green.png");
        TryAddLevelIcon("yellow", "WinWidgetBattery.battery-yellow.png");
        TryAddLevelIcon("red",    "WinWidgetBattery.battery-red.png");
    }

    private void TryAddLevelIcon(string key, string resourceName)
    {
        var icon = LoadPngIcon(resourceName);
        if (icon != null)
            _levelIcons[key] = icon;
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    public void RebuildMenu() => BuildMenu();

    private void BuildMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new ContextMenuStrip();

        var widgets = _getWidgets();
        bool canRemove = widgets.Count > 1;

        for (int i = 0; i < widgets.Count; i++)
        {
            string id    = widgets[i].Id;
            string label = widgets.Count == 1 ? "Battery Widget" : $"Battery Widget {i + 1}";

            var sub = new ToolStripMenuItem(label);
            sub.DropDownItems.Add("Settings", null, (_, _) => Invoke(() => _onWidgetSettings(id)));

            var removeItem = new ToolStripMenuItem("Remove Widget", null,
                (_, _) => Invoke(() => _onWidgetRemove(id)));
            removeItem.Enabled = canRemove;
            sub.DropDownItems.Add(removeItem);

            menu.Items.Add(sub);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("+ Add Widget", null, (_, _) => Invoke(_onAddWidget));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About", null, (_, _) => Invoke(_onAbout));
        menu.Items.Add("Exit",  null, (_, _) => Invoke(_onExit));

        _notifyIcon.ContextMenuStrip = menu;
    }

    private static void Invoke(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            action();
        else
            System.Windows.Application.Current?.Dispatcher.Invoke(action);
    }

    public void Dispose()
    {
        _iconTimer?.Stop();
        _notifyIcon?.Dispose();
        foreach (var icon in _levelIcons.Values)
            icon.Dispose();
        _fallbackIcon.Dispose();
    }

    // ── Icon loading ──────────────────────────────────────────────────────────

    private static Icon? LoadIcoIcon()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WinWidgetBattery.battery.ico");
        return stream != null ? new Icon(stream, 16, 16) : null;
    }

    private static Icon? LoadPngIcon(string resourceName)
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var src = new Bitmap(stream);
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, 32, 32);
        }

        // GetHicon allocates an unmanaged handle; clone into a managed Icon and
        // destroy the handle immediately so nothing leaks for the app lifetime.
        IntPtr hicon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hicon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hicon);
        }
    }
}
