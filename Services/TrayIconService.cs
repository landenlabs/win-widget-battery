// Copyright (c) 2026
using System.Drawing;
using System.Drawing.Drawing2D;
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
        _onAddWidget      = onAddWidget;
        _getWidgets       = getWidgets;
        _onWidgetSettings = onWidgetSettings;
        _onWidgetRemove   = onWidgetRemove;
        _onAbout          = onAbout;
        _onExit           = onExit;

        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon    = LoadTrayIcon(),
            Visible = true,
            Text    = "Battery Widget"
        };
        _notifyIcon.DoubleClick += (_, _) => Invoke(() =>
            _onWidgetSettings(_getWidgets().FirstOrDefault()?.Id ?? ""));
        BuildMenu();
    }

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

    public void Dispose() => _notifyIcon?.Dispose();

    private static Icon LoadTrayIcon()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WinWidgetBattery.battery.ico");
        return stream != null ? new Icon(stream, 16, 16) : SystemIcons.Application;
    }
}
