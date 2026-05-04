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
    private System.Windows.Threading.DispatcherTimer? _updateTimer;
    private System.Windows.Threading.DispatcherTimer? _displayCheckTimer;
    private DisplayConfiguration _currentDisplayConfiguration;
    private string _bgColorHex = "#1E1E2E";
    private double _bgOpacity = 0.80;
    private string _barBgColorHex = "#454570";

    private bool _isEmbedded;
    private bool _isDragging;
    private System.Windows.Point _dragOffset;

    public string WidgetId => _settings.Id;

    public WidgetWindow(WidgetSettings settings, BatteryService batteryService)
    {
        InitializeComponent();

        _settings = settings;
        _batteryService = batteryService;

        _bgColorHex = string.IsNullOrEmpty(settings.BackgroundColor) ? "#1E1E2E" : settings.BackgroundColor;
        _bgOpacity = settings.BackgroundOpacity > 0 ? settings.BackgroundOpacity : 0.80;
        _barBgColorHex = string.IsNullOrEmpty(settings.BarBackgroundColor) ? "#454570" : settings.BarBackgroundColor;

        _currentDisplayConfiguration = DisplayService.GetCurrentDisplayConfiguration();
        var (x, y) = DisplayService.GetDisplayPosition(settings, _currentDisplayConfiguration);
        Left = x;
        Top = y;

        InitializeUpdateTimer();
        InitializeDisplayCheckTimer();
        UpdateBatteryDisplay();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        ApplyBackgroundInternal(_bgOpacity);
        ApplyBarBackgroundInternal();
        ApplyFontScale(_settings.FontScalePercent > 0 ? _settings.FontScalePercent : 100);
        ApplyVisibilitySettings();

        if (_settings.EmbedInWallpaper)
        {
            _isEmbedded = DesktopService.EmbedInWallpaper(this);
            if (_isEmbedded)
                DesktopService.MoveEmbeddedWindow(this, (int)Left, (int)Top);
            else
                DesktopService.SetAlwaysOnBottom(this);
        }
        else
        {
            DesktopService.SetAlwaysOnBottom(this);
        }
    }

    private void InitializeUpdateTimer()
    {
        _updateTimer = new System.Windows.Threading.DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(_settings.UpdateInterval);
        _updateTimer.Tick += (s, e) => UpdateBatteryDisplay();
        _updateTimer.Start();
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
        var newConfig = DisplayService.GetCurrentDisplayConfiguration();
        if (newConfig.ConfigurationHash != _currentDisplayConfiguration.ConfigurationHash)
        {
            _currentDisplayConfiguration = newConfig;
            var (x, y) = DisplayService.GetDisplayPosition(_settings, _currentDisplayConfiguration);
            if (_isEmbedded)
                DesktopService.MoveEmbeddedWindow(this, x, y);
            else
            {
                Left = x;
                Top = y;
            }
        }
    }

    private void UpdateBatteryDisplay()
    {
        var batteryInfo = _batteryService.GetBatteryStatus();

        Dispatcher.Invoke(() =>
        {
            BatteryIconText.Text = batteryInfo.GetStatusEmoji();
            BatteryPercentText.Text = $" {batteryInfo.BatteryPercentage}%";
            StatusText.Text = $"Status: {batteryInfo.Status}";

            bool hasTime = batteryInfo.TimeRemaining.HasValue && batteryInfo.TimeRemaining.Value.TotalSeconds > 0;
            if (hasTime && _settings.ShowTimeRemaining)
            {
                var time = batteryInfo.TimeRemaining.Value;
                if (time.TotalHours >= 1)
                    TimeRemainingText.Text = $"Remaining: {time.Hours}h {time.Minutes}m";
                else if (time.TotalMinutes >= 1)
                    TimeRemainingText.Text = $"Remaining: {time.Minutes}m {time.Seconds}s";
                else
                    TimeRemainingText.Text = $"Remaining: {time.Seconds}s";
                TimeRemainingText.Visibility = Visibility.Visible;
            }
            else
            {
                TimeRemainingText.Text = "";
                TimeRemainingText.Visibility = Visibility.Collapsed;
            }

            var percentage = batteryInfo.BatteryPercentage / 100.0;
            BatteryBar.Width = 78 * percentage;
            BatteryBar.Background = batteryInfo.GetStatusColor();
        });
    }

    // ── Font scale ───────────────────────────────────────────────────────────

    public void ApplyFontScale(int percent)
    {
        double factor = Math.Max(0.25, percent / 100.0);
        BatteryIconText.FontSize = Math.Max(8, 24 * factor);
        BatteryPercentText.FontSize = Math.Max(8, 24 * factor);
        StatusText.FontSize = Math.Max(6, 10 * factor);
        TimeRemainingText.FontSize = Math.Max(6, 9 * factor);
        TitleText.FontSize = Math.Max(6, 11 * factor);
    }

    public void ApplyVisibilitySettings(bool showTitle, bool showBatteryIcon, bool showPercentage,
        bool showColorBar, bool showStatusText, bool showTimeRemaining)
    {
        TitleSection.Visibility       = showTitle       ? Visibility.Visible : Visibility.Collapsed;
        BatteryIconText.Visibility    = showBatteryIcon ? Visibility.Visible : Visibility.Collapsed;
        BatteryPercentText.Visibility = showPercentage  ? Visibility.Visible : Visibility.Collapsed;
        BatteryBarContainer.Visibility = showColorBar   ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Visibility         = showStatusText  ? Visibility.Visible : Visibility.Collapsed;
        if (!showTimeRemaining)
            TimeRemainingText.Visibility = Visibility.Collapsed;
        // showTimeRemaining=true: UpdateBatteryDisplay manages visibility based on data availability
    }

    private void ApplyVisibilitySettings() =>
        ApplyVisibilitySettings(_settings.ShowTitle, _settings.ShowBatteryIcon, _settings.ShowPercentage,
            _settings.ShowColorBar, _settings.ShowStatusText, _settings.ShowTimeRemaining);

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

        if (_isEmbedded)
        {
            var cursor = DesktopService.GetCursorPosition();
            var bounds = DesktopService.GetWindowBounds(this);
            _dragOffset = new System.Windows.Point(cursor.X - bounds.Left, cursor.Y - bounds.Top);
            _isDragging = true;
            WidgetBorder.CaptureMouse();
        }
        else
        {
            try { DragMove(); } catch { }
            SaveCurrentPosition();
        }
        e.Handled = true;
    }

    private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging || !_isEmbedded) return;
        var cursor = DesktopService.GetCursorPosition();
        int newX = cursor.X - (int)_dragOffset.X;
        int newY = cursor.Y - (int)_dragOffset.Y;
        DesktopService.MoveEmbeddedWindow(this, newX, newY);
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        WidgetBorder.ReleaseMouseCapture();
        if (_isEmbedded)
        {
            var bounds = DesktopService.GetWindowBounds(this);
            DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfiguration, bounds.Left, bounds.Top);
            SettingsService.Save(App.Settings);
        }
    }

    private void SaveCurrentPosition()
    {
        DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfiguration, (int)Left, (int)Top);
        SettingsService.Save(App.Settings);
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

    // ── Settings / About ─────────────────────────────────────────────────────

    public void OpenSettings()
    {
        var dlg = new SettingsWindow(_settings, livePreviewTarget: this);
        if (dlg.ShowDialog() == true)
        {
            _bgColorHex = _settings.BackgroundColor;
            _bgOpacity = _settings.BackgroundOpacity;
            _barBgColorHex = _settings.BarBackgroundColor;
            ApplyBackgroundInternal(_bgOpacity);
            ApplyBarBackgroundInternal();
            ApplyFontScale(_settings.FontScalePercent);
            ApplyVisibilitySettings();
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
        => new AboutWindow() { Owner = this }.ShowDialog();

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
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
        base.OnClosed(e);
    }
}
