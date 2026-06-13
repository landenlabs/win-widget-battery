// Copyright (c) 2026
using System.Runtime.InteropServices;

namespace WinWidgetBattery.Services;

/// <summary>
/// Reads user idle time and the active power plan's sleep/hibernate timeouts to
/// estimate how long until the CPU stops working (sleep or hibernate, whichever
/// fires first). Windows has no idle-triggered shutdown, so only sleep and
/// hibernate are considered. The monitor / display-off timeout is ignored.
/// </summary>
public class PowerInfoService
{
    public enum IdleAction { None, Sleep, Hibernate }

    /// <summary>
    /// Result of an idle-countdown query.
    /// <para><see cref="IsNever"/> is true when neither sleep nor hibernate is
    /// configured (the CPU will not stop from idle).</para>
    /// </summary>
    public readonly record struct IdleCountdown(IdleAction Action, TimeSpan? Remaining, bool IsNever)
    {
        public string Format()
        {
            if (IsNever)
                return "Sleep: Never";
            if (Remaining is not { } t)
                return "";

            var label = Action == IdleAction.Hibernate ? "Hibernate" : "Sleep";
            string time;
            if (t.TotalHours >= 1)
                time = $"{(int)t.TotalHours}h {t.Minutes}m";
            else if (t.TotalMinutes >= 1)
                time = $"{t.Minutes}m {t.Seconds}s";
            else
                time = $"{t.Seconds}s";
            return $"{label} in {time}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadACValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, out uint AcValueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadDCValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, out uint DcValueIndex);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    // Standard power-setting GUIDs (winnt.h / powrprof).
    private static Guid GUID_SLEEP_SUBGROUP    = new("238C9FA8-0AAD-41ED-83F4-97BE242C8F20");
    private static Guid GUID_STANDBY_TIMEOUT   = new("29F6C1DB-86DA-48C5-9FDB-F2B67B1F44DA");
    private static Guid GUID_HIBERNATE_TIMEOUT = new("9D7815A6-7EE4-497E-8888-515A05F02364");

    // Windows encodes "Never" as 0 but also uses large sentinels (e.g. 0x7FFFFFFF).
    // Any timeout beyond a year is meaningless as a countdown, so treat it as Never.
    private const uint NeverThresholdSeconds = 365u * 24 * 60 * 60;

    private static bool IsActive(uint seconds) => seconds is > 0 and < NeverThresholdSeconds;

    /// <summary>Time since the last keyboard/mouse input across the session.</summary>
    public TimeSpan GetIdleTime()
    {
        var lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref lii))
            return TimeSpan.Zero;

        // Both values are GetTickCount-domain (uint, wraps ~49.7 days); unchecked
        // subtraction yields the correct delta across a wrap.
        uint idleMs = unchecked((uint)Environment.TickCount - lii.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }

    /// <summary>
    /// Timeout in seconds for a sleep-subgroup setting on the active scheme.
    /// Returns 0 ("Never") on any failure or when the setting is disabled.
    /// </summary>
    private static uint ReadTimeoutSeconds(ref Guid setting, bool onAc)
    {
        if (PowerGetActiveScheme(IntPtr.Zero, out var schemePtr) != 0)
            return 0;

        try
        {
            var scheme = Marshal.PtrToStructure<Guid>(schemePtr);
            uint res = onAc
                ? PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref GUID_SLEEP_SUBGROUP, ref setting, out uint value)
                : PowerReadDCValueIndex(IntPtr.Zero, ref scheme, ref GUID_SLEEP_SUBGROUP, ref setting, out value);
            return res == 0 ? value : 0;
        }
        catch
        {
            return 0;
        }
        finally
        {
            LocalFree(schemePtr);
        }
    }

    /// <summary>
    /// How long until the CPU stops from inactivity, using the timeouts for the
    /// current power source (<paramref name="onAc"/>). Picks whichever of sleep
    /// or hibernate fires first; reports <see cref="IdleCountdown.IsNever"/> when
    /// neither is configured.
    /// </summary>
    public IdleCountdown GetIdleCountdown(bool onAc)
    {
        uint sleepSec     = ReadTimeoutSeconds(ref GUID_STANDBY_TIMEOUT, onAc);
        uint hibernateSec = ReadTimeoutSeconds(ref GUID_HIBERNATE_TIMEOUT, onAc);

        // The CPU stops at the soonest configured action; 0 / large sentinels mean "Never".
        var action = IdleAction.None;
        uint timeoutSec = 0;
        if (IsActive(sleepSec))
        {
            action = IdleAction.Sleep;
            timeoutSec = sleepSec;
        }
        if (IsActive(hibernateSec) && (timeoutSec == 0 || hibernateSec < timeoutSec))
        {
            action = IdleAction.Hibernate;
            timeoutSec = hibernateSec;
        }

        if (action == IdleAction.None)
            return new IdleCountdown(IdleAction.None, null, IsNever: true);

        double remaining = Math.Max(0, timeoutSec - GetIdleTime().TotalSeconds);
        return new IdleCountdown(action, TimeSpan.FromSeconds(remaining), IsNever: false);
    }
}
