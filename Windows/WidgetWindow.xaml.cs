// Copyright (c) 2026
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinWidgetBattery.Models;
using WinWidgetBattery.Services;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace WinWidgetBattery.Windows;

public partial class WidgetWindow : Window
{
    private readonly WidgetSettings _settings;
    private readonly BatteryService _batteryService;
    private readonly DeviceBatteryService _deviceBatteryService;
    private readonly PowerInfoService _powerInfoService;
    private System.Windows.Threading.DispatcherTimer? _updateTimer;
    private System.Windows.Threading.DispatcherTimer? _displayCheckTimer;
    private DisplayConfiguration _currentDisplayConfiguration;
    private string _bgColorHex = "#1E1E2E";
    private double _bgOpacity = 0.80;
    private string _barBgColorHex = "#454570";

    // Tracks whether this widget has called DeviceBatteryService.Start(),
    // so we always pair every Start() with exactly one Stop().
    private bool _deviceBatteryServiceStarted;

    private bool _isEmbedded;

    // Widget top-left in SCREEN PHYSICAL PIXELS — the single source of truth for position,
    // whether embedded (child of WorkerW) or a normal top-level window. See WidgetGeometry.
    private int _posX;
    private int _posY;

    // Drag (move) and corner-scale share the same start snapshot.
    private bool _isDragging;
    private bool _isResizing;
    private ResizeZone _resizeZone;
    private DesktopService.POINT _startCursor;
    private int _startX, _startY;
    private double _startW, _startH;      // DIPs
    private int _startPixelW;
    private int _startPercent;
    private bool _mouseDownOnBatteryIcon;

    // The widget sizes to its content, so a corner drag scales the fonts instead of setting a size.
    private const double MinScaleWidth = 60, MinScaleHeight = 30;
    private const int MinFontScale = 50, MaxFontScale = 200;   // matches the Settings slider
    private int _currentFontScale = 100;

    private SettingsWindow? _settingsDialog;

    public string WidgetId => _settings.Id;

    /// <summary>Current top-left in screen physical pixels.</summary>
    public (int X, int Y) ScreenPosition => (_posX, _posY);

    private double DpiX => VisualTreeHelper.GetDpi(this).DpiScaleX;
    private double DpiY => VisualTreeHelper.GetDpi(this).DpiScaleY;

    public WidgetWindow(WidgetSettings settings, BatteryService batteryService,
        DeviceBatteryService deviceBatteryService, PowerInfoService powerInfoService)
    {
        InitializeComponent();

        // Widgets can be added/removed from the tray at any time, so refresh this each time the menu opens.
        ContextMenuOpening += (_, _) => RemoveMenuItem.IsEnabled = App.Settings.Widgets.Count > 1;

        _settings = settings;
        _batteryService = batteryService;
        _deviceBatteryService = deviceBatteryService;
        _powerInfoService = powerInfoService;

        _bgColorHex = string.IsNullOrEmpty(settings.BackgroundColor) ? "#1E1E2E" : settings.BackgroundColor;
        _bgOpacity = settings.BackgroundOpacity > 0 ? settings.BackgroundOpacity : 0.80;
        _barBgColorHex = string.IsNullOrEmpty(settings.BarBackgroundColor) ? "#454570" : settings.BarBackgroundColor;

        _currentDisplayConfiguration = DisplayService.GetCurrentDisplayConfiguration();
        (_posX, _posY) = DisplayService.GetDisplayPosition(settings, _currentDisplayConfiguration);
        // Provisional (assumes 100 %); corrected for the real DPI in OnSourceInitialized before first show.
        Left = _posX;
        Top = _posY;

        InitializeUpdateTimer();
        InitializeDisplayCheckTimer();

        // Start the device scan only when this widget is configured to show it.
        _deviceBatteryServiceStarted = settings.ShowDeviceBatteries;
        if (_deviceBatteryServiceStarted)
            _deviceBatteryService.Start();

        UpdateBatteryDisplay();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        ApplyBackgroundInternal(_bgOpacity);
        ApplyBarBackgroundInternal();
        ApplyFontScale(_settings.FontScalePercent > 0 ? _settings.FontScalePercent : 100);
        ApplyVisibilitySettings();

        DesktopService.HideFromAltTab(this);
        _isEmbedded = _settings.EmbedInWallpaper && DesktopService.EmbedInWallpaper(this);
        if (!_isEmbedded)
            DesktopService.SetAlwaysOnBottom(this);

        MoveTo(_posX, _posY);
        EnsureOnScreen();
    }

    // ── Position (screen pixels) ─────────────────────────────────────────────

    /// <summary>Moves the widget so its top-left is at the given screen pixel position (does not save).</summary>
    public void MoveTo(int x, int y)
    {
        _posX = x;
        _posY = y;
        if (_isEmbedded)
        {
            // Pass the laid-out size so the child HWND also picks up content-size changes.
            bool measured = ActualWidth > 0 && ActualHeight > 0;
            DesktopService.MoveEmbeddedWindow(this, x, y,
                measured ? PixelWidth : null, measured ? PixelHeight : null);
        }
        else
        {
            Left = x / DpiX;
            Top  = y / DpiY;
        }
    }

    /// <summary>Persists the current position for the current monitor layout.</summary>
    public void SavePosition()
    {
        DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfiguration, _posX, _posY);
        SettingsService.Save(App.Settings);
    }

    // Content-sized window: use the laid-out size, or the real window rect before first layout.
    private int PixelWidth
    {
        get
        {
            if (ActualWidth > 0) return (int)Math.Round(ActualWidth * DpiX);
            var r = DesktopService.GetWindowBounds(this);
            return Math.Max(0, r.Right - r.Left);
        }
    }

    private int PixelHeight
    {
        get
        {
            if (ActualHeight > 0) return (int)Math.Round(ActualHeight * DpiY);
            var r = DesktopService.GetWindowBounds(this);
            return Math.Max(0, r.Bottom - r.Top);
        }
    }

    private PxRect PixelBounds => new(_posX, _posY, PixelWidth, PixelHeight);

    /// <summary>Embedded (child) windows need an explicit MoveWindow to pick up a new content size.</summary>
    private void RefreshEmbeddedSize()
    {
        if (!_isEmbedded) return;
        UpdateLayout();
        MoveTo(_posX, _posY);
    }

    /// <summary>Pulls the widget back onto a screen if it's off every monitor (e.g. after unplugging one).</summary>
    private void EnsureOnScreen()
    {
        var fix = WidgetGeometry.EnsureVisible(PixelBounds, DisplayService.GetScreens());
        if (fix is not { } p) return;
        MoveTo(p.X, p.Y);
        SavePosition();
    }

    // ── Timers ────────────────────────────────────────────────────────────────

    /// <summary>Minimum battery refresh period; battery status changes slowly and polling faster just burns power.</summary>
    public const int MinUpdateIntervalMs = 1000;

    private void InitializeUpdateTimer()
    {
        _updateTimer = new System.Windows.Threading.DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(MinUpdateIntervalMs, _settings.UpdateInterval));
        _updateTimer.Tick += (s, e) => UpdateBatteryDisplay();
        _updateTimer.Start();
    }

    /// <summary>Applies a new refresh period without a restart (clamped to <see cref="MinUpdateIntervalMs"/>).</summary>
    private void ApplyUpdateInterval(int ms)
    {
        if (_updateTimer != null)
            _updateTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(MinUpdateIntervalMs, ms));
    }

    private void InitializeDisplayCheckTimer()
    {
        _displayCheckTimer = new System.Windows.Threading.DispatcherTimer();
        _displayCheckTimer.Interval = TimeSpan.FromSeconds(2);
        _displayCheckTimer.Tick += (s, e) => CheckDisplayConfigurationChanged();
        _displayCheckTimer.Start();
    }

    private void CheckDisplayConfigurationChanged()
    {
        if (_isDragging || _isResizing) return;

        var newConfig = DisplayService.GetCurrentDisplayConfiguration();
        if (newConfig.ConfigurationHash == _currentDisplayConfiguration.ConfigurationHash) return;

        _currentDisplayConfiguration = newConfig;
        var (x, y) = DisplayService.GetDisplayPosition(_settings, _currentDisplayConfiguration);
        MoveTo(x, y);
        EnsureOnScreen();
    }

    private void UpdateBatteryDisplay()
    {
        var batteryInfo = _batteryService.GetBatteryStatus();

        Dispatcher.Invoke(() =>
        {
            BatteryIconText.Text = batteryInfo.GetStatusEmoji();
            BatteryPercentText.Text = batteryInfo.HasBattery ? $" {batteryInfo.BatteryPercentage}%" : " --";
            StatusText.Text = $"Status: {batteryInfo.Status}";

            if (_settings.ShowTimeRemaining)
            {
                if (batteryInfo.TimeRemaining is { TotalSeconds: > 0 } time)
                {
                    if (time.TotalHours >= 1)
                        TimeRemainingText.Text = $"Remaining: {time.Hours}h {time.Minutes}m";
                    else if (time.TotalMinutes >= 1)
                        TimeRemainingText.Text = $"Remaining: {time.Minutes}m {time.Seconds}s";
                    else
                        TimeRemainingText.Text = $"Remaining: {time.Seconds}s";
                }
                else
                {
                    // Windows only estimates discharge time. On AC (or no battery) there is
                    // no figure; on battery it can be briefly unknown while recalculating.
                    TimeRemainingText.Text = batteryInfo.IsCharging || !batteryInfo.HasBattery
                        ? "Remaining: on AC power"
                        : "Remaining: estimating…";
                }
                TimeRemainingText.Visibility = Visibility.Visible;
            }
            else
            {
                TimeRemainingText.Text = "";
                TimeRemainingText.Visibility = Visibility.Collapsed;
            }

            if (_settings.ShowIdleCountdown)
            {
                IdleCountdownText.Text = _powerInfoService.GetIdleCountdown(batteryInfo.IsCharging).Format();
                IdleCountdownText.Visibility = Visibility.Visible;
            }
            else
            {
                IdleCountdownText.Text = "";
                IdleCountdownText.Visibility = Visibility.Collapsed;
            }

            var percentage = batteryInfo.HasBattery ? batteryInfo.BatteryPercentage / 100.0 : 0.0;
            BatteryBar.Width = 78 * percentage;
            BatteryBar.Background = batteryInfo.GetStatusColor();

            var devices = _deviceBatteryService.GetDeviceBatteries();
            DeviceBatteriesList.ItemsSource = devices;
            DeviceBatteriesSection.Visibility =
                _settings.ShowDeviceBatteries && devices.Count > 0
                    ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    // ── Font scale ───────────────────────────────────────────────────────────

    public void ApplyFontScale(int percent)
    {
        SetFontSizes(percent);
        RefreshEmbeddedSize();
    }

    private void SetFontSizes(int percent)
    {
        _currentFontScale = percent;
        double factor = Math.Max(0.25, percent / 100.0);
        BatteryIconText.FontSize = Math.Max(8, 24 * factor);
        BatteryPercentText.FontSize = Math.Max(8, 24 * factor);
        // Message lines at ~80% of the main 24px font for readability.
        StatusText.FontSize = Math.Max(8, 19 * factor);
        TimeRemainingText.FontSize = Math.Max(8, 19 * factor);
        IdleCountdownText.FontSize = Math.Max(8, 19 * factor);
        TitleText.FontSize = Math.Max(6, 11 * factor);
        DeviceBatteriesList.FontSize = Math.Max(8, 11 * factor);
    }

    public void ApplyVisibilitySettings(bool showTitle, bool showBatteryIcon, bool showPercentage,
        bool showColorBar, bool showStatusText, bool showTimeRemaining, bool showIdleCountdown,
        bool showDeviceBatteries)
    {
        TitleSection.Visibility        = showTitle       ? Visibility.Visible : Visibility.Collapsed;
        BatteryIconText.Visibility     = showBatteryIcon ? Visibility.Visible : Visibility.Collapsed;
        BatteryPercentText.Visibility  = showPercentage  ? Visibility.Visible : Visibility.Collapsed;
        BatteryBarContainer.Visibility = showColorBar    ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Visibility          = showStatusText  ? Visibility.Visible : Visibility.Collapsed;
        if (!showTimeRemaining)
            TimeRemainingText.Visibility = Visibility.Collapsed;
        // showTimeRemaining=true: UpdateBatteryDisplay manages visibility based on data availability
        if (!showIdleCountdown)
            IdleCountdownText.Visibility = Visibility.Collapsed;
        // showIdleCountdown=true: UpdateBatteryDisplay manages visibility

        // Keep the device-service ref count in sync with the toggle.
        if (showDeviceBatteries && !_deviceBatteryServiceStarted) {
            _deviceBatteryService.Start();
            _deviceBatteryServiceStarted = true;
        } else if (!showDeviceBatteries && _deviceBatteryServiceStarted) {
            _deviceBatteryService.Stop();
            _deviceBatteryServiceStarted = false;
        }

        // Device batteries section: also gated on whether devices exist
        if (!showDeviceBatteries)
            DeviceBatteriesSection.Visibility = Visibility.Collapsed;
        // showDeviceBatteries=true: UpdateBatteryDisplay manages based on device count

        RefreshEmbeddedSize();
    }

    private void ApplyVisibilitySettings() =>
        ApplyVisibilitySettings(_settings.ShowTitle, _settings.ShowBatteryIcon, _settings.ShowPercentage,
            _settings.ShowColorBar, _settings.ShowStatusText, _settings.ShowTimeRemaining,
            _settings.ShowIdleCountdown, _settings.ShowDeviceBatteries);

    // ── Hover ────────────────────────────────────────────────────────────────

    private void Widget_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        IconPanel.Visibility = Visibility.Visible;
        ApplyBackgroundInternal(Math.Min(1.0, _bgOpacity + 0.07));
    }

    private void Widget_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        IconPanel.Visibility = Visibility.Collapsed;
        ApplyBackgroundInternal(_bgOpacity);
    }

    private void ApplyBackgroundInternal(double opacity)
    {
        try
        {
            var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(_bgColorHex);
            WidgetBorder.Background = new SolidColorBrush(color) { Opacity = opacity };
        }
        catch { }
    }

    // ── Dragging ─────────────────────────────────────────────────────────────

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsClickOnInteractiveElement(e.OriginalSource))
            return;

        var zone = GetResizeZone(e.GetPosition(WidgetBorder));
        if (zone != ResizeZone.None)
        {
            _mouseDownOnBatteryIcon = false;
            StartResize(zone);
            e.Handled = true;
            return;
        }

        _mouseDownOnBatteryIcon = IsOnBatteryIcon(e.OriginalSource);

        // Manual drag (DragMove() fails on Windows 10 with AllowsTransparency=True + WindowStyle=None,
        // and doesn't work at all for a window embedded in WorkerW). Everything is in screen pixels.
        SnapshotStart();
        _isDragging = true;
        SetCapturedCursor(System.Windows.Input.Cursors.SizeAll);
        WidgetBorder.CaptureMouse();
        e.Handled = true;
    }

    private void SnapshotStart()
    {
        _startCursor = DesktopService.GetCursorPosition();
        _startX = _posX;
        _startY = _posY;
        _startW = ActualWidth;
        _startH = ActualHeight;
        _startPixelW = PixelWidth;
        _startPercent = _currentFontScale;
    }

    private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging)
        {
            var c = DesktopService.GetCursorPosition();
            var (x, y) = WidgetGeometry.DragTo(_startX, _startY, _startCursor.X, _startCursor.Y, c.X, c.Y);
            MoveTo(x, y);
            return;
        }

        if (_isResizing)
        {
            UpdateResize();
            return;
        }

        UpdateHoverCursor(GetResizeZone(e.GetPosition(WidgetBorder)));
    }

    /// <summary>
    /// The border's XAML cursor is the move cursor (the battery icon overrides it with a hand).
    /// In a resize corner the border forces its resize cursor over any child cursor.
    /// </summary>
    private void UpdateHoverCursor(ResizeZone zone)
    {
        bool inCorner = zone != ResizeZone.None;
        WidgetBorder.Cursor = inCorner ? CursorFor(zone) : System.Windows.Input.Cursors.SizeAll;
        WidgetBorder.ForceCursor = inCorner;
    }

    /// <summary>
    /// While the border has mouse capture (drag/resize), it's the element queried for the
    /// cursor, so pin the appropriate cursor for the duration of the operation.
    /// </summary>
    private void SetCapturedCursor(System.Windows.Input.Cursor cursor)
    {
        WidgetBorder.Cursor = cursor;
        WidgetBorder.ForceCursor = true;
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging && !_isResizing) return;

        var c = DesktopService.GetCursorPosition();
        bool wasClick = _isDragging &&
                        Math.Abs(c.X - _startCursor.X) <= 5 &&
                        Math.Abs(c.Y - _startCursor.Y) <= 5;

        EndDragOrResize();
        UpdateHoverCursor(GetResizeZone(e.GetPosition(WidgetBorder)));
        e.Handled = true;

        if (wasClick)
            OpenPanelForClickedElement();
    }

    /// <summary>Capture can be lost mid-drag (Alt-Tab, Win key, UAC prompt); finish cleanly instead of sticking.</summary>
    private void Widget_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging || _isResizing)
            EndDragOrResize();
    }

    private void EndDragOrResize()
    {
        bool wasResizing = _isResizing;
        _isDragging = false;
        _isResizing = false;
        _resizeZone = ResizeZone.None;
        if (WidgetBorder.IsMouseCaptured)
            WidgetBorder.ReleaseMouseCapture();
        UpdateHoverCursor(ResizeZone.None);

        if (wasResizing)
            _settings.FontScalePercent = _currentFontScale;
        EnsureOnScreen();
        SavePosition();
    }

    // ── Corner scaling ───────────────────────────────────────────────────────

    /// <summary>
    /// Bottom corners scale the content; the top corners are always part of the drag zone
    /// (the widget is content-sized, so there is no top-edge resize to offer).
    /// </summary>
    private ResizeZone GetResizeZone(System.Windows.Point pos) =>
        WidgetGeometry.HitTest(pos.X, pos.Y, WidgetBorder.ActualWidth, WidgetBorder.ActualHeight,
                               allowTopCorners: false);

    private static System.Windows.Input.Cursor CursorFor(ResizeZone zone) => zone switch
    {
        ResizeZone.TopLeft or ResizeZone.BottomRight => System.Windows.Input.Cursors.SizeNWSE,
        ResizeZone.TopRight or ResizeZone.BottomLeft => System.Windows.Input.Cursors.SizeNESW,
        _ => System.Windows.Input.Cursors.SizeAll
    };

    private void StartResize(ResizeZone zone)
    {
        SnapshotStart();
        _isResizing = true;
        _resizeZone = zone;
        SetCapturedCursor(CursorFor(zone));
        WidgetBorder.CaptureMouse();
    }

    /// <summary>
    /// Translates the corner drag into a font-scale change: the size the drag *would* give
    /// (WidgetGeometry.Resize) is converted to a proportional scale percent. Bottom-left keeps
    /// the right edge fixed; bottom-right keeps the top-left fixed.
    /// </summary>
    private void UpdateResize()
    {
        var c = DesktopService.GetCursorPosition();
        var r = WidgetGeometry.Resize(_resizeZone, _startW, _startH, _startX, _startY,
                                      c.X - _startCursor.X, c.Y - _startCursor.Y,
                                      DpiX, DpiY, MinScaleWidth, MinScaleHeight);
        int percent = WidgetGeometry.ScalePercentFromResize(_startPercent, _startW, _startH, r.Width, r.Height,
                                                            MinFontScale, MaxFontScale);
        if (percent == _currentFontScale) return;

        SetFontSizes(percent);
        UpdateLayout();
        int x = WidgetGeometry.IsWest(_resizeZone) ? _startX + (_startPixelW - PixelWidth) : _startX;
        MoveTo(x, _startY);
    }

    private bool IsClickOnInteractiveElement(object? source)
    {
        if (source is System.Windows.Controls.Button)
            return true;
        if (source is System.Windows.FrameworkElement fe)
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(fe);
            while (parent != null)
            {
                if (parent == IconPanel) return true;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
        }
        return false;
    }

    private bool IsOnBatteryIcon(object? source)
    {
        if (source is not DependencyObject element) return false;
        while (element != null)
        {
            if (element == BatteryIconText) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void OpenPanelForClickedElement()
    {
        if (_mouseDownOnBatteryIcon)
            Process.Start(new ProcessStartInfo("ms-settings:batterysaver") { UseShellExecute = true });
    }

    // ── Settings / About ─────────────────────────────────────────────────────

    public void OpenSettings()
    {
        // Tray double-click / tray menu / widget menu can all request this while it's already open.
        if (_settingsDialog != null)
        {
            _settingsDialog.Activate();
            return;
        }

        bool ok;
        try
        {
            _settingsDialog = new SettingsWindow(_settings, livePreviewTarget: this);
            ok = _settingsDialog.ShowDialog() == true;
        }
        finally
        {
            _settingsDialog = null;
        }

        if (ok)
        {
            _bgColorHex = _settings.BackgroundColor;
            _bgOpacity = _settings.BackgroundOpacity;
            _barBgColorHex = _settings.BarBackgroundColor;
            ApplyBackgroundInternal(_bgOpacity);
            ApplyBarBackgroundInternal();
            ApplyFontScale(_settings.FontScalePercent);
            ApplyVisibilitySettings();
            ApplyUpdateInterval(_settings.UpdateInterval);
            UpdateBatteryDisplay();
        }
        else
        {
            ApplyBackground(_bgColorHex, _bgOpacity);
            ApplyBarBackground(_barBgColorHex);
            ApplyFontScale(_settings.FontScalePercent);
        }
    }

    public void ApplyBackground(string hexColor, double opacity)
    {
        try
        {
            _bgColorHex = hexColor;
            _bgOpacity = opacity;
            ApplyBackgroundInternal(opacity);
        }
        catch { }
    }

    private void ApplyBarBackgroundInternal()
    {
        try
        {
            var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(_barBgColorHex);
            BatteryBarTrack.Background = new SolidColorBrush(color);
        }
        catch { }
    }

    public void ApplyBarBackground(string hexColor)
    {
        _barBgColorHex = hexColor;
        ApplyBarBackgroundInternal();
    }

    private void BatteryUsage_Click(object sender, RoutedEventArgs e)
        => Process.Start(new ProcessStartInfo("ms-settings:batterysaver") { UseShellExecute = true });

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void About_Click(object sender, RoutedEventArgs e)
        => ((App)System.Windows.Application.Current).OpenAbout(this);

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (App.Settings.Widgets.Count <= 1)
        {
            System.Windows.MessageBox.Show(
                "This is the only widget. Use Exit to close the app instead.",
                "Remove Widget", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "Remove this widget?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            ((App)System.Windows.Application.Current).RemoveWidget(_settings.Id);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
        => System.Windows.Application.Current.Shutdown();

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer = null;
        _displayCheckTimer?.Stop();
        _displayCheckTimer = null;

        if (_deviceBatteryServiceStarted) {
            _deviceBatteryService.Stop();
            _deviceBatteryServiceStarted = false;
        }

        base.OnClosed(e);
    }
}
