# WinWidgetBattery Privacy Policy

**Effective date:** November 20, 2026
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
- The app **makes no internet or network connections**. Everything it shows is read on your PC.

## Information the app reads on your PC

To show battery status, WinWidgetBattery reads, locally:

- your PC's battery charge, charging state and estimated time remaining, from Windows power status,
- for Bluetooth and USB devices that are already paired or plugged in (mice, keyboards, headsets,
  controllers), the **device name and battery level** that Windows reports, or that the device
  reports through its standard battery service. The app **does not pair with, scan for, or connect
  to new devices**, and it reads nothing from a device except its battery level,
- the size and layout of your monitors, to place the widget.

None of this is sent to LanDen Labs or anyone else, and device names and battery readings are
**not saved**. They are shown on the widget and refreshed while it runs.

## Network activity

WinWidgetBattery doesn't connect to the internet or your local network, and it doesn't check for
updates. If you installed it from the MSIX package, Windows handles updates when you install a newer
release.

## Actions you choose to take

Some features open another program only when you click them:

- Clicking the **battery icon** or **Battery usage** opens the battery page in Windows **Settings**.
- The **GitHub** link in the **About** dialog opens the project page in your web browser.

## Information stored on your PC

WinWidgetBattery saves your settings in `%AppData%\WinWidgetBattery\settings.json`: each widget's
position on each monitor layout, colors, opacity, font scale, update interval, which display parts
are shown, and whether *Auto-start on login* is on.

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
[GitHub](https://github.com/landenlabs/win-widget-battery/commits/main/privacy.md).

## Contact

For questions about this policy, open an issue at
[github.com/landenlabs/win-widget-battery/issues](https://github.com/landenlabs/win-widget-battery/issues).
