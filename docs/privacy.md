---
title: Privacy Policy
---

# WinWidgetBattery Privacy Policy

**Effective date:** November 23, 2026
**Publisher:** LanDen Labs

WinWidgetBattery is a free Windows desktop widget that shows your PC's battery level and the
battery levels of connected Bluetooth and USB devices. This policy explains what the app reads,
what leaves your computer, and what is stored.

## Summary

- WinWidgetBattery has **no accounts, no ads, no analytics, no telemetry, no tracking and no crash
  reporting**.
- It is **free**. There are no fees, subscriptions or in-app purchases.
- LanDen Labs **does not collect, receive or store any of your data**. There is no
  WinWidgetBattery server.
- The app **makes no internet or network connections**. Everything it shows is read on your PC and
  shown only to you.

## Information the app reads on your PC

To show battery status, WinWidgetBattery reads, locally and without administrator rights:

- your PC's battery charge, whether it is plugged in, and the estimated time remaining, from the
  Windows power status (`GetSystemPowerStatus`),
- for devices that are already paired or plugged in (mice, keyboards, headsets, earbuds,
  controllers), the **device name and battery level**. It gets these about every 30 seconds from
  three places:
  - USB and Bluetooth HID devices (mice, keyboards, controllers) that report a standard battery
    value. The app asks the device for that value and its product name,
  - Bluetooth LE devices that offer the standard Bluetooth Battery Service. The app reads the
    battery level that Windows has cached for the device,
  - the battery level Windows itself records for paired Bluetooth devices (for example headsets).

  The app **does not pair with, scan for, or connect to new devices**, and it reads nothing from a
  device except its name and battery level,
- only if you turn on the *Sleep Countdown* display (off by default): how long it has been since
  your last keyboard or mouse input, and the sleep and hibernate timeouts of your active Windows
  power plan. These are used to show "Sleep in ..." on the widget. The app reads only the **time**
  of your last input, never what you typed or clicked,
- the size and layout of your monitors, to place the widget.

None of this is sent to LanDen Labs or anyone else, and device names and battery readings are
**not saved**. They are shown on the widget and refreshed while it runs.

## Network activity

WinWidgetBattery doesn't connect to the internet or your local network, and it doesn't check for
updates. If you installed it from the MSIX package, Windows handles updates when you install a newer
release.

## Actions you choose to take

Some features open another program only when you click them:

- Clicking the **battery icon**, or **Usage** in the right-click menu, opens the battery
  page in Windows **Settings**.
- The **GitHub** and **Privacy policy** links in the **About** dialog open those pages in your web
  browser.

## Information stored on your PC

WinWidgetBattery saves your settings in `%AppData%\WinWidgetBattery\settings.json`: each widget's
position on each monitor layout, colors, opacity, font scale, update interval, which display parts
are shown, whether the widget is embedded in the wallpaper layer, and whether *Auto-start on login*
is on.

If you turn on *Auto-start on login* in the portable (zip) build, the app adds a `WinWidgetBattery`
entry to your user's Windows startup (Run) registry key. In the MSIX build it uses the Windows
*Startup apps* setting instead.

When the app is installed from the MSIX package, Windows keeps this file inside the app's private
package folder (under `%LocalAppData%\Packages\`) instead.

The app keeps no log files or battery history. This data never leaves your PC. To delete it,
uninstall the app (uninstalling the MSIX removes its package folder), or delete the
`%AppData%\WinWidgetBattery` folder.

## Children

WinWidgetBattery is a general-purpose utility. It is not directed at children and collects no
personal information from anyone.

## License

WinWidgetBattery is open source under the
[Apache License 2.0](https://github.com/landenlabs/win-widget-battery/blob/main/LICENSE.txt). You
can read all of its source code on [GitHub](https://github.com/landenlabs/win-widget-battery) and
confirm what this policy describes.

## Changes to this policy

If the app's behavior changes in a way that affects privacy, this page will be updated and the
effective date above will change. The history of this page is public on
[GitHub](https://github.com/landenlabs/win-widget-battery/commits/main/docs/privacy.md).

## Contact

For questions about this policy, open an issue at
[github.com/landenlabs/win-widget-battery/issues](https://github.com/landenlabs/win-widget-battery/issues).
