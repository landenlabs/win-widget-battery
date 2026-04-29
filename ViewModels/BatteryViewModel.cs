// Copyright (c) 2026
using System.ComponentModel;
using System.Windows.Media;
using WinWidgetBattery.Models;

namespace WinWidgetBattery.ViewModels;

public class BatteryViewModel : INotifyPropertyChanged
{
    private BatteryInfo _batteryInfo = new();
    private string _displayText = "";
    private SolidColorBrush _statusColor = new(Colors.White);

    public event PropertyChangedEventHandler? PropertyChanged;

    public BatteryInfo BatteryInfo
    {
        get => _batteryInfo;
        set
        {
            if (_batteryInfo != value)
            {
                _batteryInfo = value;
                OnPropertyChanged(nameof(BatteryInfo));
                UpdateDisplayText();
            }
        }
    }

    public string DisplayText
    {
        get => _displayText;
        set
        {
            if (_displayText != value)
            {
                _displayText = value;
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public SolidColorBrush StatusColor
    {
        get => _statusColor;
        set
        {
            if (_statusColor != value)
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    private void UpdateDisplayText()
    {
        var emoji = _batteryInfo.GetStatusEmoji();
        var percent = _batteryInfo.BatteryPercentage;
        var status = _batteryInfo.Status;

        DisplayText = $"{emoji} {percent}%";
        StatusColor = _batteryInfo.GetStatusColor();
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
