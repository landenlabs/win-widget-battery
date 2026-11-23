# Microsoft Store submission: WinWidgetBattery

Copy-paste material for Partner Center (https://partner.microsoft.com/dashboard). Fill in the
sections in the order Partner Center shows them.

## Product identity (Product management > Product identity)

Copy these three values into the repo Actions **Variables** (Settings > Secrets and variables >
Actions > Variables) so the release workflow builds a matching Store package:

| Partner Center field | Repo variable |
| --- | --- |
| Package/Identity/Name | `MSIX_IDENTITY_NAME` |
| Package/Identity/Publisher (`CN=...`) | `MSIX_PUBLISHER` |
| Package/Properties/PublisherDisplayName | `MSIX_PUBLISHER_DISPLAY_NAME` |

Then push a `v*` tag. The workflow's build artifact contains `dist-store/WinWidgetBattery-<ver>-x64-store.msix`
and `-arm64-store.msix`. They are unsigned, which is what the Store expects. Upload both under
**Packages**.

## Properties

| Field | Value |
| --- | --- |
| Category | Utilities + tools (subcategory: none) |
| Privacy policy URL | https://landenlabs.github.io/win-widget-battery/privacy.html |
| Website | https://landenlabs.github.io/win-widget-battery/ |
| Support contact | https://github.com/landenlabs/win-widget-battery/issues |
| Collects personal information? | No |
| Product declarations | No, it does not access, collect or transmit personal information |
| System requirements | Windows 10 version 1809 (build 17763) or later; x64 or ARM64 |

## Age ratings (IARC questionnaire)

Category: **Utility / productivity**. Answer **No** to everything: no violence, no user
interaction or chat, no sharing of location or personal info, no purchases, no unrestricted web
access. Expected rating: 3+ / Everyone.

## Pricing and availability

Free. No in-app purchases. All markets.

## Restricted capability justification

Partner Center asks why the package declares `runFullTrust`. Suggested text:

> WinWidgetBattery is a WPF desktop app packaged with the Desktop Bridge. It needs runFullTrust to
> run as a normal Win32 process: it reads the system battery and power status
> (GetSystemPowerStatus), the active power plan's sleep and hibernate timeouts and the last-input
> time (for an optional sleep countdown), and the battery level of already-paired devices through
> the HID API (hid.dll / SetupAPI), Bluetooth LE Battery Service (GATT) and Windows Bluetooth
> device properties. It also draws a transparent, always-on-desktop widget window. It does not
> pair with or scan for devices, makes no internet connections and collects no user data.

## Store listing (English, United States)

**Product name:** WinWidgetBattery

**Short description** (up to 270 characters):

> A lightweight, transparent desktop widget that shows your PC's battery level, charging state and
> time remaining, plus the battery levels of your Bluetooth and USB mice, keyboards, headsets and
> controllers. No ads, no account, no telemetry.

**Description:**

> WinWidgetBattery puts a small, transparent battery gauge right on your desktop, so you can see
> how much charge you have left at a glance without opening the taskbar flyout.
>
> The widget shows the battery percentage, whether you are charging or discharging, and the
> estimated time remaining, with a color-coded bar: green when charging or above 50%, yellow from
> 20 to 49%, and red below 20%. It also lists the battery levels of your paired Bluetooth and
> plugged-in USB devices, such as mice, keyboards, headsets, earbuds and game controllers. An
> optional countdown shows how long until your PC sleeps or hibernates from inactivity. Click the
> battery icon to open the Windows battery settings.
>
> Make it yours: pick the colors, opacity and font scale, drag a corner to resize it, and turn
> each part of the display on or off. Run several widgets, place them on any monitor, and
> WinWidgetBattery remembers where each one goes for each monitor layout. It can start
> automatically when you sign in.
>
> Private by design: WinWidgetBattery has no account, ads, analytics or telemetry, and it never
> connects to the internet. Everything it shows stays on your PC.
>
> Free and open source under the Apache 2.0 license.

**What's new in this version:** (fill in per release)

**Product features** (one per line, up to 20):

```
Battery percentage, charging state and time remaining on your desktop
Color-coded battery bar (green, yellow, red)
Battery levels of Bluetooth and USB devices (mice, keyboards, headsets, controllers)
Optional countdown to sleep or hibernate
Transparent widget that sits on your wallpaper
Custom colors, opacity, size and font scale
Multiple widgets, multi-monitor aware
Optional start at sign-in
No ads, no account, no telemetry, no internet connections
```

**Search terms** (up to 7):

```
battery monitor
battery widget
desktop widget
bluetooth battery
battery level
laptop battery
device battery
```

**Copyright and trademark info:** © 2026 LanDen Labs

**Additional license terms:** Apache License 2.0,
https://github.com/landenlabs/win-widget-battery/blob/main/LICENSE.txt

## Images

| Partner Center slot | File |
| --- | --- |
| Store logo, 1:1 (300 x 300) | `store-assets/tile-300x300.png` (**TODO: not created yet.** Make a 300 x 300 PNG from `screens/icon.png`) |
| Screenshots (Desktop; at least 1, recommended 1366 x 768 or larger) | See note below |

The Store requires desktop screenshots of at least 1366 x 768. The images in `screens/` are cropped
to single windows and are too small. Capture full-screen shots with the widget on the desktop, for
example one with the widget (with device batteries showing) on a wallpaper and one with Settings
open.
