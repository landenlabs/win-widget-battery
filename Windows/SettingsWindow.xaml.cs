// Copyright (c) 2026
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinWidgetBattery.Models;
using WinWidgetBattery.Services;

namespace WinWidgetBattery.Windows;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    private readonly WidgetSettings _widget;
    private readonly WidgetWindow? _livePreviewTarget;

    // ── Bindable properties ──────────────────────────────────────────────────

    private string _bgColorHex = "#1E1E2E";
    public string BgColorHex
    {
        get => _bgColorHex;
        set
        {
            _bgColorHex = value;
            _bgColorBrush = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BgColorBrush));
            LivePreviewBackground();
        }
    }

    private SolidColorBrush? _bgColorBrush;
    public SolidColorBrush BgColorBrush => _bgColorBrush ??= ParseBgBrush();

    private SolidColorBrush ParseBgBrush()
    {
        try { return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_bgColorHex)); }
        catch { return System.Windows.Media.Brushes.Black; }
    }

    private string _barBgColorHex = "#454570";
    public string BarBgColorHex
    {
        get => _barBgColorHex;
        set
        {
            _barBgColorHex = value;
            _barBgColorBrush = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BarBgColorBrush));
            LivePreviewBarBackground();
        }
    }

    private SolidColorBrush? _barBgColorBrush;
    public SolidColorBrush BarBgColorBrush => _barBgColorBrush ??= ParseBarBgBrush();

    private SolidColorBrush ParseBarBgBrush()
    {
        try { return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_barBgColorHex)); }
        catch { return System.Windows.Media.Brushes.Black; }
    }

    private int _bgOpacityPercent;
    public int BgOpacityPercent
    {
        get => _bgOpacityPercent;
        set { _bgOpacityPercent = value; OnPropertyChanged(); LivePreviewBackground(); }
    }

    private int _updateInterval;
    public int UpdateInterval
    {
        get => _updateInterval;
        set { _updateInterval = value; OnPropertyChanged(); }
    }

    private int _fontScalePercent;
    public int FontScalePercent
    {
        get => _fontScalePercent;
        set { _fontScalePercent = value; OnPropertyChanged(); _livePreviewTarget?.ApplyFontScale(value); }
    }

    private bool _embedInWallpaper;
    public bool EmbedInWallpaper
    {
        get => _embedInWallpaper;
        set { _embedInWallpaper = value; OnPropertyChanged(); }
    }

    // ── Component visibility ─────────────────────────────────────────────────

    private bool _showTitle;
    public bool ShowTitle
    {
        get => _showTitle;
        set { _showTitle = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    private bool _showBatteryIcon;
    public bool ShowBatteryIcon
    {
        get => _showBatteryIcon;
        set { _showBatteryIcon = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    private bool _showPercentage;
    public bool ShowPercentage
    {
        get => _showPercentage;
        set { _showPercentage = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    private bool _showColorBar;
    public bool ShowColorBar
    {
        get => _showColorBar;
        set { _showColorBar = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    private bool _showStatusText;
    public bool ShowStatusText
    {
        get => _showStatusText;
        set { _showStatusText = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    private bool _showTimeRemaining;
    public bool ShowTimeRemaining
    {
        get => _showTimeRemaining;
        set { _showTimeRemaining = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    private bool _showDeviceBatteries;
    public bool ShowDeviceBatteries
    {
        get => _showDeviceBatteries;
        set { _showDeviceBatteries = value; OnPropertyChanged(); LivePreviewVisibility(); }
    }

    // ── Originals for Cancel restore ────────────────────────────────────────

    private readonly string _origBgColor;
    private readonly string _origBarBgColor;
    private readonly int _origBgOpacityPercent;
    private readonly double _origPosX;
    private readonly double _origPosY;

    // ── Position picker ──────────────────────────────────────────────────────

    private double _mapScale;
    private double _mapLeft;
    private double _mapTop;
    private double _mapOffsetX;
    private double _mapOffsetY;
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private System.Windows.Controls.Border? _widgetMarker;
    private bool _markerDragging;
    private System.Windows.Point _markerDragStart;
    private double _markerDragOrigLeft;
    private double _markerDragOrigTop;
    private double _editPosX;
    private double _editPosY;

    public string WidgetPositionText => $"X: {(int)_editPosX}  Y: {(int)_editPosY}";
    private readonly int _origUpdateInterval;
    private readonly int _origFontScalePercent;
    private readonly bool _origShowTitle;
    private readonly bool _origShowBatteryIcon;
    private readonly bool _origShowPercentage;
    private readonly bool _origShowColorBar;
    private readonly bool _origShowStatusText;
    private readonly bool _origShowTimeRemaining;
    private readonly bool _origShowDeviceBatteries;

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsWindow(WidgetSettings widget, WidgetWindow? livePreviewTarget = null)
    {
        _widget = widget;
        _livePreviewTarget = livePreviewTarget;

        InitializeComponent();
        Topmost = true;

        // Snapshot originals so Cancel can restore the live preview
        _origBgColor          = string.IsNullOrEmpty(widget.BackgroundColor) ? "#1E1E2E" : widget.BackgroundColor;
        _origBarBgColor       = string.IsNullOrEmpty(widget.BarBackgroundColor) ? "#454570" : widget.BarBackgroundColor;
        _origBgOpacityPercent = (int)Math.Round(widget.BackgroundOpacity * 100);
        if (_origBgOpacityPercent == 0) _origBgOpacityPercent = 80;
        _origPosX = livePreviewTarget?.Left ?? widget.X;
        _origPosY = livePreviewTarget?.Top  ?? widget.Y;
        _editPosX = _origPosX;
        _editPosY = _origPosY;
        _origUpdateInterval   = widget.UpdateInterval;
        _origFontScalePercent = widget.FontScalePercent > 0 ? widget.FontScalePercent : 100;
        _origShowTitle        = widget.ShowTitle;
        _origShowBatteryIcon  = widget.ShowBatteryIcon;
        _origShowPercentage   = widget.ShowPercentage;
        _origShowColorBar     = widget.ShowColorBar;
        _origShowStatusText       = widget.ShowStatusText;
        _origShowTimeRemaining    = widget.ShowTimeRemaining;
        _origShowDeviceBatteries  = widget.ShowDeviceBatteries;

        // Load working copies
        _bgColorHex        = _origBgColor;
        _barBgColorHex     = _origBarBgColor;
        _bgOpacityPercent  = _origBgOpacityPercent;
        _updateInterval    = widget.UpdateInterval;
        _fontScalePercent  = _origFontScalePercent;
        _embedInWallpaper  = widget.EmbedInWallpaper;
        _showTitle         = widget.ShowTitle;
        _showBatteryIcon   = widget.ShowBatteryIcon;
        _showPercentage    = widget.ShowPercentage;
        _showColorBar      = widget.ShowColorBar;
        _showStatusText       = widget.ShowStatusText;
        _showTimeRemaining    = widget.ShowTimeRemaining;
        _showDeviceBatteries  = widget.ShowDeviceBatteries;

        OnPropertyChanged(nameof(BgColorHex));
        OnPropertyChanged(nameof(BgColorBrush));
        OnPropertyChanged(nameof(BarBgColorHex));
        OnPropertyChanged(nameof(BarBgColorBrush));
        OnPropertyChanged(nameof(BgOpacityPercent));
        OnPropertyChanged(nameof(UpdateInterval));
        OnPropertyChanged(nameof(FontScalePercent));
        OnPropertyChanged(nameof(EmbedInWallpaper));
        OnPropertyChanged(nameof(ShowTitle));
        OnPropertyChanged(nameof(ShowBatteryIcon));
        OnPropertyChanged(nameof(ShowPercentage));
        OnPropertyChanged(nameof(ShowColorBar));
        OnPropertyChanged(nameof(ShowStatusText));
        OnPropertyChanged(nameof(ShowTimeRemaining));
        OnPropertyChanged(nameof(ShowDeviceBatteries));

        UpdateColorHexLabel();
        UpdateBarBgColorHexLabel();
    }

    // ── Background color picker ──────────────────────────────────────────────

    private void BgColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        var picker = new ColorPickerWindow(_bgColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            BgColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void BgColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ColorPickerWindow(_bgColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            BgColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    // ── Bar background color picker ──────────────────────────────────────────

    private void BarBgColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        var picker = new ColorPickerWindow(_barBgColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            BarBgColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void BarBgColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ColorPickerWindow(_barBgColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            BarBgColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void LivePreviewBarBackground()
    {
        _livePreviewTarget?.ApplyBarBackground(_barBgColorHex);
        UpdateBarBgColorHexLabel();
    }

    private void UpdateBarBgColorHexLabel()
    {
        BarBgColorHexLabel.Text = _barBgColorHex.ToUpperInvariant();
    }

    private void LivePreviewBackground()
    {
        _livePreviewTarget?.ApplyBackground(_bgColorHex, _bgOpacityPercent / 100.0);
        UpdateColorHexLabel();
    }

    private void LivePreviewVisibility()
    {
        _livePreviewTarget?.ApplyVisibilitySettings(
            _showTitle, _showBatteryIcon, _showPercentage,
            _showColorBar, _showStatusText, _showTimeRemaining, _showDeviceBatteries);
    }

    private void UpdateColorHexLabel()
    {
        ColorHexLabel.Text = _bgColorHex.ToUpperInvariant();
    }

    // ── Screen-map position picker ───────────────────────────────────────────

    private void Window_Loaded(object sender, RoutedEventArgs e) => BuildScreenMap();

    private void BuildScreenMap()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        int minX = screens.Min(s => s.Bounds.Left);
        int minY = screens.Min(s => s.Bounds.Top);
        int maxX = screens.Max(s => s.Bounds.Right);
        int maxY = screens.Max(s => s.Bounds.Bottom);
        _mapOffsetX = minX;
        _mapOffsetY = minY;

        double cW = ScreenMapCanvas.ActualWidth;
        double cH = ScreenMapCanvas.ActualHeight;
        if (cW <= 0 || cH <= 0) return;

        double vdW = maxX - minX;
        double vdH = maxY - minY;
        _mapScale = Math.Min(cW / vdW, cH / vdH);

        _mapLeft = (cW - vdW * _mapScale) / 2.0;
        _mapTop  = (cH - vdH * _mapScale) / 2.0;

        var source = PresentationSource.FromVisual(this);
        _dpiScaleX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
        _dpiScaleY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

        ScreenMapCanvas.Children.Clear();

        foreach (var screen in screens)
        {
            double left = _mapLeft + (screen.Bounds.Left - minX) * _mapScale;
            double top  = _mapTop  + (screen.Bounds.Top  - minY) * _mapScale;
            double w    = screen.Bounds.Width  * _mapScale;
            double h    = screen.Bounds.Height * _mapScale;

            var monitorRect = new System.Windows.Controls.Border
            {
                Width = w, Height = h,
                Background       = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x30)),
                BorderBrush      = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x45, 0x45, 0x70)),
                BorderThickness  = new Thickness(1),
                CornerRadius     = new CornerRadius(2),
                IsHitTestVisible = false
            };
            System.Windows.Controls.Canvas.SetLeft(monitorRect, left);
            System.Windows.Controls.Canvas.SetTop(monitorRect, top);
            ScreenMapCanvas.Children.Add(monitorRect);

            var lbl = new System.Windows.Controls.TextBlock
            {
                Text       = screen.Primary ? "Primary" : $"{screen.Bounds.Width}×{screen.Bounds.Height}",
                FontSize   = 9,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0x5B, 0x70)),
                IsHitTestVisible = false
            };
            System.Windows.Controls.Canvas.SetLeft(lbl, left + 3);
            System.Windows.Controls.Canvas.SetTop(lbl,  top  + 2);
            ScreenMapCanvas.Children.Add(lbl);
        }

        double widgetWpx = (_livePreviewTarget?.ActualWidth  ?? 160) * _dpiScaleX;
        double widgetHpx = (_livePreviewTarget?.ActualHeight ?? 80)  * _dpiScaleY;
        double markerW   = Math.Max(widgetWpx * _mapScale, 14);
        double markerH   = Math.Max(widgetHpx * _mapScale, 8);

        double markerLeft = _mapLeft + (_editPosX * _dpiScaleX - minX) * _mapScale;
        double markerTop  = _mapTop  + (_editPosY * _dpiScaleY - minY) * _mapScale;

        _widgetMarker = new System.Windows.Controls.Border
        {
            Width = markerW, Height = markerH,
            Background      = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x89, 0xB4, 0xFA)),
            BorderBrush     = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(2),
            Cursor          = System.Windows.Input.Cursors.SizeAll,
            ToolTip         = "Drag to reposition the widget"
        };
        _widgetMarker.MouseLeftButtonDown += WidgetMarker_MouseLeftButtonDown;
        _widgetMarker.MouseMove           += WidgetMarker_MouseMove;
        _widgetMarker.MouseLeftButtonUp   += WidgetMarker_MouseLeftButtonUp;

        System.Windows.Controls.Canvas.SetLeft(_widgetMarker, markerLeft);
        System.Windows.Controls.Canvas.SetTop(_widgetMarker, markerTop);
        System.Windows.Controls.Panel.SetZIndex(_widgetMarker, 10);
        ScreenMapCanvas.Children.Add(_widgetMarker);
    }

    private void WidgetMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _markerDragging     = true;
        _markerDragStart    = e.GetPosition(ScreenMapCanvas);
        _markerDragOrigLeft = System.Windows.Controls.Canvas.GetLeft(_widgetMarker!);
        _markerDragOrigTop  = System.Windows.Controls.Canvas.GetTop(_widgetMarker!);
        _widgetMarker!.CaptureMouse();
        e.Handled = true;
    }

    private void WidgetMarker_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_markerDragging || _widgetMarker == null) return;

        var pos     = e.GetPosition(ScreenMapCanvas);
        double newL = _markerDragOrigLeft + (pos.X - _markerDragStart.X);
        double newT = _markerDragOrigTop  + (pos.Y - _markerDragStart.Y);

        newL = Math.Max(0, Math.Min(newL, ScreenMapCanvas.ActualWidth  - _widgetMarker.Width));
        newT = Math.Max(0, Math.Min(newT, ScreenMapCanvas.ActualHeight - _widgetMarker.Height));

        System.Windows.Controls.Canvas.SetLeft(_widgetMarker, newL);
        System.Windows.Controls.Canvas.SetTop(_widgetMarker, newT);

        _editPosX = ((newL - _mapLeft) / _mapScale + _mapOffsetX) / _dpiScaleX;
        _editPosY = ((newT - _mapTop)  / _mapScale + _mapOffsetY) / _dpiScaleY;

        OnPropertyChanged(nameof(WidgetPositionText));

        if (_livePreviewTarget != null)
        {
            _livePreviewTarget.Left = _editPosX;
            _livePreviewTarget.Top  = _editPosY;
        }

        e.Handled = true;
    }

    private void WidgetMarker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_markerDragging) return;
        _markerDragging = false;
        _widgetMarker?.ReleaseMouseCapture();
        e.Handled = true;
    }

    // ── Dialog buttons ───────────────────────────────────────────────────────

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        _widget.UpdateInterval      = _updateInterval;
        _widget.BackgroundColor     = _bgColorHex;
        _widget.BarBackgroundColor  = _barBgColorHex;
        _widget.BackgroundOpacity   = _bgOpacityPercent / 100.0;
        _widget.FontScalePercent  = _fontScalePercent;
        _widget.EmbedInWallpaper  = _embedInWallpaper;
        _widget.ShowTitle         = _showTitle;
        _widget.ShowBatteryIcon   = _showBatteryIcon;
        _widget.ShowPercentage    = _showPercentage;
        _widget.ShowColorBar      = _showColorBar;
        _widget.ShowStatusText       = _showStatusText;
        _widget.ShowTimeRemaining    = _showTimeRemaining;
        _widget.ShowDeviceBatteries  = _showDeviceBatteries;

        var config = DisplayService.GetCurrentDisplayConfiguration();
        DisplayService.SaveDisplayPosition(_widget, config, (int)_editPosX, (int)_editPosY);
        if (_livePreviewTarget != null)
        {
            _livePreviewTarget.Left = _editPosX;
            _livePreviewTarget.Top  = _editPosY;
        }

        SettingsService.Save(App.Settings);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _livePreviewTarget?.ApplyBackground(_origBgColor, _origBgOpacityPercent / 100.0);
        _livePreviewTarget?.ApplyBarBackground(_origBarBgColor);
        _livePreviewTarget?.ApplyFontScale(_origFontScalePercent);
        _livePreviewTarget?.ApplyVisibilitySettings(
            _origShowTitle, _origShowBatteryIcon, _origShowPercentage,
            _origShowColorBar, _origShowStatusText, _origShowTimeRemaining, _origShowDeviceBatteries);
        if (_livePreviewTarget != null)
        {
            _livePreviewTarget.Left = _origPosX;
            _livePreviewTarget.Top  = _origPosY;
        }
        DialogResult = false;
        Close();
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
