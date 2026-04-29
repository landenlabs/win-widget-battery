// Copyright (c) 2026
using System.Runtime.InteropServices;
using WinWidgetBattery.Models;

namespace WinWidgetBattery.Services;

public class BatteryService
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus sps);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    public BatteryInfo GetBatteryStatus()
    {
        var info = new BatteryInfo();

        try
        {
            if (GetSystemPowerStatus(out var status))
            {
                info.BatteryPercentage = status.BatteryLifePercent;
                info.IsCharging = status.ACLineStatus == 1;

                if (status.BatteryLifeTime != uint.MaxValue && status.BatteryLifeTime > 0)
                {
                    info.TimeRemaining = TimeSpan.FromSeconds(status.BatteryLifeTime);
                }

                info.Status = GetStatusText(status);
            }
        }
        catch
        {
            info.Status = "Error reading battery";
        }

        return info;
    }

    private static string GetStatusText(SystemPowerStatus status)
    {
        return status.ACLineStatus switch
        {
            1 => "Charging",
            0 => status.BatteryLifePercent switch
            {
                >= 50 => "Discharging",
                >= 20 => "Low Battery",
                _ => "Critical Battery"
            },
            _ => "Unknown"
        };
    }
}
