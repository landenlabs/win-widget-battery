// Copyright (c) 2026
using System.Drawing;
using System.Windows.Forms;

namespace WinWidgetBattery.Services;

public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly Action _onAddWidget;
    private readonly Func<List<Models.WidgetSettings>> _getWidgets;
    private readonly Action<string> _onWidgetSettings;
    private readonly Action<string> _onWidgetRemove;
    private readonly Action _onAbout;
    private readonly Action _onExit;

    public TrayIconService(
        Action onAddWidget,
        Func<List<Models.WidgetSettings>> getWidgets,
        Action<string> onWidgetSettings,
        Action<string> onWidgetRemove,
        Action onAbout,
        Action onExit)
    {
        _onAddWidget = onAddWidget;
        _getWidgets = getWidgets;
        _onWidgetSettings = onWidgetSettings;
        _onWidgetRemove = onWidgetRemove;
        _onAbout = onAbout;
        _onExit = onExit;

        InitializeTrayIcon();
    }

    private static Icon LoadTrayIcon()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WinWidgetBattery.battery.ico");
        return stream != null ? new Icon(stream, 16, 16) : SystemIcons.Application;
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Visible = true,
            Text = "Battery Widget"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Add Widget", null, (s, e) => _onAddWidget());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("About", null, (s, e) => _onAbout());
        contextMenu.Items.Add("Exit", null, (s, e) => _onExit());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => _onAddWidget();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
