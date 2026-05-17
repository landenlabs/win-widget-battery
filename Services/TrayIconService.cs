// Copyright (c) 2026
using System.Drawing;
using System.Windows.Forms;

namespace WinWidgetBattery.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;

    // Static items reused on each Opening rebuild
    private readonly ToolStripMenuItem  _addWidgetItem;
    private readonly ToolStripSeparator _topSep = new();
    private readonly ToolStripSeparator _botSep = new();
    private readonly ToolStripMenuItem  _aboutItem;
    private readonly ToolStripMenuItem  _exitItem;

    private readonly Func<List<Models.WidgetSettings>> _getWidgets;
    private readonly Action<string> _onWidgetSettings;
    private readonly Action<string> _onWidgetRemove;

    public TrayIconService(
        Action onAddWidget,
        Func<List<Models.WidgetSettings>> getWidgets,
        Action<string> onWidgetSettings,
        Action<string> onWidgetRemove,
        Action onAbout,
        Action onExit)
    {
        _getWidgets       = getWidgets;
        _onWidgetSettings = onWidgetSettings;
        _onWidgetRemove   = onWidgetRemove;

        _addWidgetItem = new ToolStripMenuItem("+ Add Widget", null, (_, _) => UIInvoke(onAddWidget));
        _aboutItem     = new ToolStripMenuItem("About",        null, (_, _) => UIInvoke(onAbout));
        _exitItem      = new ToolStripMenuItem("Exit",         null, (_, _) => UIInvoke(onExit));

        _menu = new ContextMenuStrip();
        _menu.Opening += OnMenuOpening;

        _notifyIcon = new NotifyIcon
        {
            Icon             = LoadTrayIcon(),
            Visible          = true,
            Text             = "Battery Widget",
            ContextMenuStrip = _menu
        };
        _notifyIcon.DoubleClick += (_, _) =>
            UIInvoke(() => _onWidgetSettings(_getWidgets().FirstOrDefault()?.Id ?? ""));
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _menu.Items.Clear();
        _menu.Items.Add(_addWidgetItem);
        _menu.Items.Add(_topSep);

        var widgets = _getWidgets();
        for (int i = 0; i < widgets.Count; i++)
        {
            var id    = widgets[i].Id;
            var label = widgets.Count == 1 ? "Battery Widget" : $"Battery Widget {i + 1}";

            var widgetMenu = new ToolStripMenuItem(label);
            widgetMenu.DropDownItems.Add("Settings", null, (_, _) => UIInvoke(() => _onWidgetSettings(id)));
            widgetMenu.DropDownItems.Add("Remove",   null, (_, _) => UIInvoke(() => _onWidgetRemove(id)));

            // Disable Remove when it's the only widget
            ((ToolStripMenuItem)widgetMenu.DropDownItems[1]).Enabled = widgets.Count > 1;

            _menu.Items.Add(widgetMenu);
        }

        _menu.Items.Add(_botSep);
        _menu.Items.Add(_aboutItem);
        _menu.Items.Add(_exitItem);
    }

    // Dispatch to WPF UI thread so callers don't have to think about it.
    private static void UIInvoke(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private static Icon LoadTrayIcon()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WinWidgetBattery.battery.ico");
        return stream != null ? new Icon(stream, 16, 16) : SystemIcons.Application;
    }
}
