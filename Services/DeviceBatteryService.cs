// Copyright (c) 2026
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using WinWidgetBattery.Models;

namespace WinWidgetBattery.Services;

public class DeviceBatteryService
{
    // ── HID P/Invoke ─────────────────────────────────────────────────────────

    [DllImport("hid.dll", SetLastError = true)]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HidD_GetProductString(SafeFileHandle handle, StringBuilder buffer, uint bufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HidD_GetFeatureReport(SafeFileHandle handle, byte[] buffer, int bufferSize);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HidD_GetInputReport(SafeFileHandle handle, byte[] buffer, int bufferSize);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS caps);

    [DllImport("hid.dll")]
    private static extern int HidP_GetValueCaps(short reportType, [Out] HIDP_VALUE_CAPS[] valueCaps,
        ref ushort valueCapsLength, IntPtr preparsedData);

    [DllImport("hid.dll")]
    private static extern int HidP_GetUsageValue(short reportType, ushort usagePage, ushort linkCollection,
        ushort usage, out uint usageValue, IntPtr preparsedData, byte[] report, uint reportLength);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator,
        IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
        ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize, out uint requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize, out uint requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess,
        uint shareMode, IntPtr securityAttributes, uint creationDisposition,
        uint flagsAndAttributes, IntPtr templateFile);

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_VALUE_CAPS
    {
        public ushort UsagePage;
        public byte ReportID;
        [MarshalAs(UnmanagedType.I1)] public bool IsAlias;
        public ushort BitField;
        public ushort LinkCollection;
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        [MarshalAs(UnmanagedType.I1)] public bool IsRange;
        [MarshalAs(UnmanagedType.I1)] public bool IsStringRange;
        [MarshalAs(UnmanagedType.I1)] public bool IsDesignatorRange;
        [MarshalAs(UnmanagedType.I1)] public bool IsAbsolute;
        public uint HasNull;
        public byte Reserved;
        public ushort BitSize;
        public ushort ReportCount;
        public ushort Reserved2;
        public ushort Reserved3;
        public ushort Reserved4;
        public ushort Reserved5;
        public ushort Reserved6;
        public uint LogicalMin;
        public uint LogicalMax;
        public uint PhysicalMin;
        public uint PhysicalMax;
        // Union: Range or NotRange — we only need NotRange.Usage
        public ushort UsageMin;    // NotRange.Usage when IsRange=false
        public ushort UsageMax;
        public ushort StringMin;
        public ushort StringMax;
        public ushort DesignatorMin;
        public ushort DesignatorMax;
        public ushort DataIndexMin;
        public ushort DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public UIntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        public uint cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DevicePath;
    }

    private const uint DIGCF_PRESENT = 0x02;
    private const uint DIGCF_DEVICEINTERFACE = 0x10;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x01;
    private const uint FILE_SHARE_WRITE = 0x02;
    private const uint OPEN_EXISTING = 3;
    private const int HIDP_STATUS_SUCCESS = 0x00110000;
    private const short HidP_Input = 0;
    private const short HidP_Feature = 2;
    private const ushort USAGE_PAGE_GENERIC_DEVICE_CONTROLS = 0x06;
    private const ushort USAGE_BATTERY_STRENGTH = 0x20;

    // ── State ─────────────────────────────────────────────────────────────────

    private List<DeviceBatteryInfo> _cache = [];
    private readonly Lock _lock = new();
    private CancellationTokenSource? _cts;

    // ── Public API ────────────────────────────────────────────────────────────

    public List<DeviceBatteryInfo> GetDeviceBatteries()
    {
        lock (_lock)
            return [.. _cache];
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        return RunLoop(_cts.Token);
    }

    public void Stop() => _cts?.Cancel();

    // ── Background loop ───────────────────────────────────────────────────────

    private async Task RunLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var results = new List<DeviceBatteryInfo>();
            results.AddRange(ScanHidDevices());
            results.AddRange(await ScanBleDevicesAsync(ct));

            // Add Classic BT and any BLE devices not found via GATT
            foreach (var r in await ScanBluetoothPropertiesAsync(ct))
            {
                if (!results.Any(x => string.Equals(x.Name, r.Name, StringComparison.OrdinalIgnoreCase)))
                    results.Add(r);
            }

            lock (_lock)
                _cache = results;

            try { await Task.Delay(30_000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    // ── HID scan ──────────────────────────────────────────────────────────────

    private List<DeviceBatteryInfo> ScanHidDevices()
    {
        var results = new List<DeviceBatteryInfo>();
        HidD_GetHidGuid(out var hidGuid);

        var deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (deviceInfoSet == new IntPtr(-1))
            return results;

        try
        {
            uint index = 0;
            var interfaceData = new SP_DEVICE_INTERFACE_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
            };

            while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index++, ref interfaceData))
            {
                var path = GetDevicePath(deviceInfoSet, ref interfaceData);
                if (path == null) continue;

                var info = TryReadHidBattery(path);
                if (info != null)
                    results.Add(info);
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return results;
    }

    private static string? GetDevicePath(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA interfaceData)
    {
        SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0,
            out uint requiredSize, IntPtr.Zero);

        var detailData = new SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            cbSize = (uint)(IntPtr.Size == 8 ? 8 : 6)
        };

        if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData,
                ref detailData, requiredSize, out _, IntPtr.Zero))
            return null;

        return detailData.DevicePath;
    }

    private static DeviceBatteryInfo? TryReadHidBattery(string devicePath)
    {
        var handle = CreateFile(devicePath,
            GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

        if (handle.IsInvalid)
        {
            // Try read-only (some system devices block write)
            handle = CreateFile(devicePath,
                GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        }

        if (handle.IsInvalid) return null;

        using (handle)
        {
            if (!HidD_GetPreparsedData(handle, out var preparsed)) return null;

            try
            {
                if (HidP_GetCaps(preparsed, out var caps) != HIDP_STATUS_SUCCESS) return null;

                var featureCap = FindBatteryValueCap(preparsed, caps, HidP_Feature);
                var inputCap   = FindBatteryValueCap(preparsed, caps, HidP_Input);
                if (featureCap == null && inputCap == null)
                    return null;

                int? percent = ReadBatteryPercent(handle, preparsed, caps, featureCap, inputCap);
                if (percent == null) return null;

                var name = new StringBuilder(256);
                string deviceName = HidD_GetProductString(handle, name, (uint)(name.Capacity * 2))
                    ? name.ToString().Trim('\0').Trim()
                    : "Unknown HID Device";

                if (string.IsNullOrWhiteSpace(deviceName)) return null;

                bool isBt = devicePath.Contains("bthenum", StringComparison.OrdinalIgnoreCase)
                         || devicePath.Contains("bthhfenum", StringComparison.OrdinalIgnoreCase);

                return new DeviceBatteryInfo
                {
                    Name = deviceName,
                    Percent = percent.Value,
                    ConnectionType = isBt ? DeviceConnectionType.BluetoothHid : DeviceConnectionType.UsbHid
                };
            }
            finally
            {
                HidD_FreePreparsedData(preparsed);
            }
        }
    }

    private static HIDP_VALUE_CAPS? FindBatteryValueCap(IntPtr preparsed, HIDP_CAPS caps, short reportType)
    {
        ushort count = reportType == HidP_Feature
            ? caps.NumberFeatureValueCaps
            : caps.NumberInputValueCaps;

        if (count == 0) return null;

        var valueCaps = new HIDP_VALUE_CAPS[count];
        if (HidP_GetValueCaps(reportType, valueCaps, ref count, preparsed) != HIDP_STATUS_SUCCESS)
            return null;

        foreach (var vc in valueCaps.Take(count))
        {
            if (vc.UsagePage == USAGE_PAGE_GENERIC_DEVICE_CONTROLS &&
                !vc.IsRange && vc.UsageMin == USAGE_BATTERY_STRENGTH)
                return vc;
        }
        return null;
    }

    private static int? ReadBatteryPercent(SafeFileHandle handle, IntPtr preparsed, HIDP_CAPS caps,
        HIDP_VALUE_CAPS? featureCap, HIDP_VALUE_CAPS? inputCap)
    {
        // Try Feature report first
        if (featureCap != null && caps.FeatureReportByteLength > 0)
        {
            var report = new byte[caps.FeatureReportByteLength];
            if (HidD_GetFeatureReport(handle, report, report.Length))
            {
                if (HidP_GetUsageValue(HidP_Feature, USAGE_PAGE_GENERIC_DEVICE_CONTROLS, 0,
                        USAGE_BATTERY_STRENGTH, out uint val, preparsed,
                        report, (uint)report.Length) == HIDP_STATUS_SUCCESS)
                {
                    uint logMax = featureCap.Value.LogicalMax > 0 ? featureCap.Value.LogicalMax : 100u;
                    return (int)Math.Round(val * 100.0 / logMax);
                }
            }
        }

        // Fall back to Input report (common for Bluetooth HID devices)
        if (inputCap != null && caps.InputReportByteLength > 0)
        {
            var report = new byte[caps.InputReportByteLength];
            if (HidD_GetInputReport(handle, report, report.Length))
            {
                if (HidP_GetUsageValue(HidP_Input, USAGE_PAGE_GENERIC_DEVICE_CONTROLS, 0,
                        USAGE_BATTERY_STRENGTH, out uint val, preparsed,
                        report, (uint)report.Length) == HIDP_STATUS_SUCCESS)
                {
                    uint logMax = inputCap.Value.LogicalMax > 0 ? inputCap.Value.LogicalMax : 100u;
                    return (int)Math.Round(val * 100.0 / logMax);
                }
            }
        }

        return null;
    }

    // ── Bluetooth device-property scan (Classic BR/EDR + BLE fallback) ───────

    // Windows stores the HFP/AVRCP-sourced battery level in PKEY_Devices_BatteryPlusCharging.
    // This catches Classic BT earbuds, headsets, and any BLE device that skipped GATT.
    private static async Task<List<DeviceBatteryInfo>> ScanBluetoothPropertiesAsync(CancellationToken ct)
    {
        var results = new List<DeviceBatteryInfo>();
        const string batteryKey = "{49CD1F76-5626-4B17-A4E8-18B4AA1A2213} 10";
        string[] props = [batteryKey];

        try
        {
            // Classic Bluetooth (BR/EDR)
            var classicSelector = BluetoothDevice.GetDeviceSelector();
            var classicDevices = await DeviceInformation.FindAllAsync(classicSelector, props).AsTask(ct);
            AddFromDeviceInfos(classicDevices, results, DeviceConnectionType.BluetoothHid, batteryKey);

            // BLE devices (fallback for those without Battery Service GATT)
            var leSelector = BluetoothLEDevice.GetDeviceSelector();
            var leDevices = await DeviceInformation.FindAllAsync(leSelector, props).AsTask(ct);
            AddFromDeviceInfos(leDevices, results, DeviceConnectionType.BluetoothLE, batteryKey);
        }
        catch (OperationCanceledException) { }
        catch { }

        return results;
    }

    private static void AddFromDeviceInfos(
        IReadOnlyList<DeviceInformation> devices,
        List<DeviceBatteryInfo> results,
        DeviceConnectionType connType,
        string batteryKey)
    {
        foreach (var d in devices)
        {
            if (!d.Properties.TryGetValue(batteryKey, out var v) || v == null) continue;
            int pct = v switch
            {
                byte b  => b,
                int  i  => i,
                uint u  => (int)u,
                _       => -1
            };
            if (pct is < 0 or > 100) continue;
            if (string.IsNullOrWhiteSpace(d.Name)) continue;
            if (results.Any(x => string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase))) continue;

            results.Add(new DeviceBatteryInfo { Name = d.Name, Percent = pct, ConnectionType = connType });
        }
    }

    // ── BLE GATT scan ─────────────────────────────────────────────────────────

    private static async Task<List<DeviceBatteryInfo>> ScanBleDevicesAsync(CancellationToken ct)
    {
        var results = new List<DeviceBatteryInfo>();

        try
        {
            var selector = GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.Battery);
            var deviceInfos = await DeviceInformation.FindAllAsync(selector).AsTask(ct);

            foreach (var deviceInfo in deviceInfos)
            {
                if (ct.IsCancellationRequested) break;
                var info = await TryReadBleBatteryAsync(deviceInfo, ct);
                if (info != null)
                    results.Add(info);
            }
        }
        catch (OperationCanceledException) { }
        catch { /* BLE not available or no adapter */ }

        return results;
    }

    private static async Task<DeviceBatteryInfo?> TryReadBleBatteryAsync(
        DeviceInformation deviceInfo, CancellationToken ct)
    {
        BluetoothLEDevice? bleDevice = null;
        GattDeviceService? service = null;

        try
        {
            bleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id).AsTask(ct);
            if (bleDevice == null) return null;

            var servicesResult = await bleDevice
                .GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Cached)
                .AsTask(ct);

            if (servicesResult.Status != GattCommunicationStatus.Success ||
                servicesResult.Services.Count == 0)
                return null;

            service = servicesResult.Services[0];

            var charsResult = await service
                .GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel, BluetoothCacheMode.Cached)
                .AsTask(ct);

            if (charsResult.Status != GattCommunicationStatus.Success ||
                charsResult.Characteristics.Count == 0)
                return null;

            var characteristic = charsResult.Characteristics[0];
            var readResult = await characteristic.ReadValueAsync(BluetoothCacheMode.Cached).AsTask(ct);

            if (readResult.Status != GattCommunicationStatus.Success) return null;

            var reader = DataReader.FromBuffer(readResult.Value);
            byte batteryLevel = reader.ReadByte();

            string name = bleDevice.Name;
            if (string.IsNullOrWhiteSpace(name)) return null;

            return new DeviceBatteryInfo
            {
                Name = name,
                Percent = batteryLevel,
                ConnectionType = DeviceConnectionType.BluetoothLE
            };
        }
        catch { return null; }
        finally
        {
            service?.Dispose();
            bleDevice?.Dispose();
        }
    }
}
