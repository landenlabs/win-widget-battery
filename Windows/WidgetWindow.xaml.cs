// Copyright (c) 2026
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

    public string WidgetId => _settings.Id;

    public WidgetWindow(WidgetSettings settings, BatteryService batteryService)
    {
        InitializeComponent();

        _settings = settings;
        _batteryService = batteryService;

        // Get current display configuration
        _currentDisplayConfiguration = DisplayService.GetCurrentDisplayConfiguration();

        // Get position for current display configuration
        var (x, y) = DisplayService.GetDisplayPosition(settings, _currentDisplayConfiguration);
        Left = x;
        Top = y;

        InitializeUpdateTimer();
        InitializeDisplayCheckTimer();
        UpdateBatteryDisplay();
    }

    private void InitializeUpdateTimer()
    {
        _updateTimer = new System.Windows.Threading.DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(_settings.UpdateInterval);
        _updateTimer.Tick += (s, e) => UpdateBatteryDisplay();
        _updateTimer.Start();
    }

    /// <summary>
    /// Monitors display configuration changes (monitor plugged/unplugged)
    /// </summary>
    private void InitializeDisplayCheckTimer()
    {
        _displayCheckTimer = new System.Windows.Threading.DispatcherTimer();
        _displayCheckTimer.Interval = TimeSpan.FromSeconds(2); // Check every 2 seconds
        _displayCheckTimer.Tick += (s, e) => CheckDisplayConfigurationChanged();
        _displayCheckTimer.Start();
    }

    /// <summary>
    /// Checks if display configuration has changed (new monitor plugged in, etc.)
    /// </summary>
    private void CheckDisplayConfigurationChanged()
    {
        var newConfig = DisplayService.GetCurrentDisplayConfiguration();

        if (newConfig.ConfigurationHash != _currentDisplayConfiguration.ConfigurationHash)
        {
            // Display configuration changed - apply saved position for new configuration
            _currentDisplayConfiguration = newConfig;

            var (x, y) = DisplayService.GetDisplayPosition(_settings, _currentDisplayConfiguration);

            // Move to the saved position for this display configuration
            Left = x;
            Top = y;
        }
    }

    private void UpdateBatteryDisplay()
    {
        var batteryInfo = _batteryService.GetBatteryStatus();

        Dispatcher.Invoke(() =>
        {
            // Update battery percentage and emoji
            BatteryDisplay.Text = $"{batteryInfo.GetStatusEmoji()} {batteryInfo.BatteryPercentage}%";

            // Update status text
            StatusText.Text = $"Status: {batteryInfo.Status}";

            // Update time remaining
            if (batteryInfo.TimeRemaining.HasValue && batteryInfo.TimeRemaining.Value.TotalSeconds > 0 && _settings.ShowTimeRemaining)
            {
                var time = batteryInfo.TimeRemaining.Value;
                if (time.TotalHours >= 1)
                    TimeRemainingText.Text = $"Remaining: {time.Hours}h {time.Minutes}m";
                else if (time.TotalMinutes >= 1)
                    TimeRemainingText.Text = $"Remaining: {time.Minutes}m {time.Seconds}s";
                else
                    TimeRemainingText.Text = $"Remaining: {time.Seconds}s";
            }
            else
            {
                TimeRemainingText.Text = "";
            }

            // Update battery bar color and width
            var percentage = batteryInfo.BatteryPercentage / 100.0;
            BatteryBar.Width = 78 * percentage;
            BatteryBar.Background = batteryInfo.GetStatusColor();
        });
    }

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
        var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(_bgColorHex);
        WidgetBorder.Background = new SolidColorBrush(color) { Opacity = opacity };
    }

    private void Widget_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Check if click is on an interactive element (button)
        if (IsClickOnInteractiveElement(e.OriginalSource))
        {
            LogEvent("MouseLeftButtonDown on interactive element - skipping drag");
            return;
        }

        LogEvent("MouseLeftButtonDown - Starting DragMove");

        // Use WPF's built-in DragMove method - this is what the time widget uses
        try
        {
            DragMove();
            LogEvent("DragMove completed");
            SaveCurrentPosition();
        }
        catch (Exception ex)
        {
            LogEvent($"DragMove exception: {ex.Message}");
        }

        e.Handled = true;
    }

    private void Widget_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // DragMove handles all the movement, we don't need to do anything here
    }

    private void Widget_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // DragMove handles release, we just save position
        LogEvent("MouseLeftButtonUp");
        e.Handled = true;
    }

    private void SaveCurrentPosition()
    {
        LogEvent($"Saving position - Config: {_currentDisplayConfiguration.ConfigurationHash}, X: {(int)Left}, Y: {(int)Top}");

        // Save the current position for the current display configuration
        DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfiguration, (int)Left, (int)Top);
        SettingsService.Save(App.Settings);
    }

    /// <summary>
    /// Checks if the click source is an interactive element (button, etc)
    /// </summary>
    private bool IsClickOnInteractiveElement(object? source)
    {
        // Check if click is on a button or other interactive control
        if (source is System.Windows.Controls.Button)
        {
            LogEvent("IsClickOnInteractiveElement: Button detected");
            return true;
        }

        // Check if the source element is inside the IconPanel (buttons area)
        if (source is System.Windows.FrameworkElement fe)
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(fe);
            while (parent != null)
            {
                if (parent == IconPanel)
                {
                    LogEvent("IsClickOnInteractiveElement: Inside IconPanel");
                    return true;
                }
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
        }

        return false;
    }

    /// <summary>
    /// Logs drag-related events for debugging
    /// </summary>
    private void LogEvent(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        System.Diagnostics.Debug.WriteLine($"[{timestamp}] [DragWidget] {message}");
    }

    public void OpenSettings()
    {
        var dlg = new SettingsWindow(_settings, livePreviewTarget: this);
        if (dlg.ShowDialog() == true)
        {
            // Settings were saved, refresh display if needed
            UpdateBatteryDisplay();
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow() { Owner = this }.ShowDialog();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Remove this widget?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ((App)System.Windows.Application.Current).RemoveWidget(_settings.Id);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer = null;
        base.OnClosed(e);
    }
}
