// Copyright (c) 2026
namespace WinWidgetBattery.Models;

public enum DeviceConnectionType { UsbHid, BluetoothHid, BluetoothLE }

public class DeviceBatteryInfo
{
    public string Name { get; set; } = string.Empty;
    public int Percent { get; set; }
    public DeviceConnectionType ConnectionType { get; set; }

    public string Icon => ConnectionType is DeviceConnectionType.BluetoothLE
        or DeviceConnectionType.BluetoothHid ? "🔵" : "🔌";
}
