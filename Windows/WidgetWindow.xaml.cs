// Copyright (c) 2026
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinWidgetBattery.Models;
using WinWidgetBattery.Services;

namespace WinWidgetBattery.Windows;

public partial class WidgetWindow : Window
{
    private readonly WidgetSettings _settings;
    private readonly BatteryService _batteryService;
    private System.Windows.Threading.DispatcherTimer? _updateTimer;
    private bool _isDragging;
    private System.Windows.Point _dragStart;
    private double _windowStartLeft;
    private double _windowStartTop;

    public string WidgetId => _settings.Id;

    public WidgetWindow(WidgetSettings settings, BatteryService batteryService)
    {
        InitializeComponent();

        _settings = settings;
        _batteryService = batteryService;

        Left = _settings.X;
        Top = _settings.Y;

        InitializeUpdateTimer();
        UpdateBatteryDisplay();
    }

    private void InitializeUpdateTimer()
    {
        _updateTimer = new System.Windows.Threading.DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(_settings.UpdateInterval);
        _updateTimer.Tick += (s, e) => UpdateBatteryDisplay();
        _updateTimer.Start();
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
        WidgetBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xDD, 0x1E, 0x1E, 0x2E));
    }

    private void Widget_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        IconPanel.Visibility = Visibility.Collapsed;
        WidgetBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x1E, 0x1E, 0x2E));
    }

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStart = e.GetPosition(null);
        _windowStartLeft = Left;
        _windowStartTop = Top;
        CaptureMouse();
        e.Handled = true;
    }

    private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(null);
            Left = _windowStartLeft + (currentPos.X - _dragStart.X);
            Top = _windowStartTop + (currentPos.Y - _dragStart.Y);
        }
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();

            _settings.X = (int)Left;
            _settings.Y = (int)Top;
            SettingsService.Save(App.Settings);

            e.Handled = true;
        }
    }

    public void OpenSettings()
    {
        System.Windows.MessageBox.Show(
            "Settings for Battery Widget\n\nUpdate Interval: " + _settings.UpdateInterval + "ms\n\nMore options coming soon!",
            "Widget Settings",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Battery Widget v1.0.0\n\nA desktop widget that displays current battery status and charge level.",
            "About Battery Widget",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
