// Copyright (c) 2026
using System.Windows.Media;

namespace WinWidgetBattery.Models;

public class BatteryInfo
{
    public int BatteryPercentage { get; set; }
    public bool IsCharging { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? TimeRemaining { get; set; }

    public SolidColorBrush GetStatusColor()
    {
        if (IsCharging)
            return new SolidColorBrush(Colors.LimeGreen);

        return BatteryPercentage switch
        {
            >= 50 => new SolidColorBrush(Colors.LimeGreen),
            >= 20 => new SolidColorBrush(Colors.Yellow),
            _ => new SolidColorBrush(Colors.Red)
        };
    }

    public string GetStatusEmoji()
    {
        if (IsCharging)
            return "🔌";

        return BatteryPercentage switch
        {
            >= 90 => "🔋",
            >= 70 => "🔋",
            >= 50 => "🔋",
            >= 30 => "🪫",
            _ => "🪫"
        };
    }
}

public class AppSettings
{
    public List<WidgetSettings> Widgets { get; set; } = [];
}

public class WidgetSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;
    public int UpdateInterval { get; set; } = 1000; // milliseconds
    public bool ShowTimeRemaining { get; set; } = true;
}
