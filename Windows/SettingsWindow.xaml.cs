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

    // ── Originals for Cancel restore ────────────────────────────────────────

    private readonly string _origBgColor;
    private readonly int _origBgOpacityPercent;
    private readonly int _origUpdateInterval;
    private readonly int _origFontScalePercent;

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsWindow(WidgetSettings widget, WidgetWindow? livePreviewTarget = null)
    {
        _widget = widget;
        _livePreviewTarget = livePreviewTarget;

        InitializeComponent();
        Topmost = true;

        // Snapshot originals so Cancel can restore the live preview
        _origBgColor          = string.IsNullOrEmpty(widget.BackgroundColor) ? "#1E1E2E" : widget.BackgroundColor;
        _origBgOpacityPercent = (int)Math.Round(widget.BackgroundOpacity * 100);
        if (_origBgOpacityPercent == 0) _origBgOpacityPercent = 80;
        _origUpdateInterval   = widget.UpdateInterval;
        _origFontScalePercent = widget.FontScalePercent > 0 ? widget.FontScalePercent : 100;

        // Load working copies
        _bgColorHex       = _origBgColor;
        _bgOpacityPercent = _origBgOpacityPercent;
        _updateInterval   = widget.UpdateInterval;
        _fontScalePercent = _origFontScalePercent;
        _embedInWallpaper = widget.EmbedInWallpaper;

        OnPropertyChanged(nameof(BgColorHex));
        OnPropertyChanged(nameof(BgColorBrush));
        OnPropertyChanged(nameof(BgOpacityPercent));
        OnPropertyChanged(nameof(UpdateInterval));
        OnPropertyChanged(nameof(FontScalePercent));
        OnPropertyChanged(nameof(EmbedInWallpaper));

        UpdateColorHexLabel();
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

    private void LivePreviewBackground()
    {
        _livePreviewTarget?.ApplyBackground(_bgColorHex, _bgOpacityPercent / 100.0);
        UpdateColorHexLabel();
    }

    private void UpdateColorHexLabel()
    {
        ColorHexLabel.Text = _bgColorHex.ToUpperInvariant();
    }

    // ── Dialog buttons ───────────────────────────────────────────────────────

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        _widget.UpdateInterval    = _updateInterval;
        _widget.BackgroundColor   = _bgColorHex;
        _widget.BackgroundOpacity = _bgOpacityPercent / 100.0;
        _widget.FontScalePercent  = _fontScalePercent;
        _widget.EmbedInWallpaper  = _embedInWallpaper;
        SettingsService.Save(App.Settings);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _livePreviewTarget?.ApplyBackground(_origBgColor, _origBgOpacityPercent / 100.0);
        _livePreviewTarget?.ApplyFontScale(_origFontScalePercent);

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
