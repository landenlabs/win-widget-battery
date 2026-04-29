# Battery Widget - WinWidgetBattery

A modern desktop widget application for Windows that displays real-time battery status and charge level. Built with WPF and .NET 10.

## Features

- **Real-time Battery Monitoring**: Displays current battery percentage with live updates
- **Status Indicators**: Shows charging status, low battery warnings, and critical battery alerts
- **Color-Coded Display**: 
  - 🔌 Green when charging
  - 🟢 Green for 50-100% charge
  - 🟡 Yellow for 20-50% charge  
  - 🔴 Red for critical battery (<20%)
- **Time Remaining**: Shows estimated battery runtime (when available)
- **Visual Battery Bar**: Graphical representation of current charge level
- **Draggable Widget**: Move the widget anywhere on your screen by dragging
- **System Tray Integration**: Quick access menu in the system tray
- **Multiple Widgets**: Support for multiple battery widgets on screen
- **Persistent Settings**: Widget positions and preferences are saved automatically

## UI Components

### Main Widget Display
- **Battery Icon & Percentage**: Large, easy-to-read display of current charge level
- **Status Text**: Shows "Charging", "Discharging", "Low Battery", or "Critical Battery"
- **Time Remaining**: Estimated hours/minutes remaining on current charge
- **Visual Bar**: Horizontal bar showing battery level at a glance

### Right-Click Context Menu
- **Settings**: Configure widget options
- **About**: View version information
- **Remove Widget**: Delete this widget instance
- **Exit**: Close the application

### System Tray
- Double-click to add a new widget
- Right-click for menu options

## Architecture

### Project Structure

```
WinWidgetBattery/
├── Models/
│   └── AppSettings.cs         # Data models for battery info and settings
├── Services/
│   ├── BatteryService.cs      # Battery status monitoring using WinAPI
│   ├── SettingsService.cs     # Persistence layer for widget settings
│   └── TrayIconService.cs     # System tray integration
├── ViewModels/
│   └── BatteryViewModel.cs    # MVVM view model for battery data
├── Windows/
│   └── WidgetWindow.xaml(.cs) # Main widget UI
├── App.xaml(.cs)              # Application entry point
└── MainWindow.xaml(.cs)       # Hidden main window
```

### Key Components

#### BatteryService
- Uses Windows API (`GetSystemPowerStatus`) to retrieve battery information
- Returns battery percentage, charging status, and estimated time remaining
- Updates are triggered by a `DispatcherTimer` (default: 1 second interval)

#### SettingsService
- Persists widget configuration to JSON in AppData
- Stores widget positions, update intervals, and visibility settings
- Supports single-instance detection via Mutex

#### TrayIconService
- Manages system tray icon and context menu
- Handles widget creation/deletion from tray menu

#### WidgetWindow
- Custom WPF window with:
  - No window chrome (transparent, frameless)
  - Always-on-top behavior
  - Draggable interface
  - Smooth animations and hover effects

## Technical Details

### Technology Stack
- **Framework**: .NET 10 (net10.0-windows)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Platform**: Windows Forms integration for system tray
- **Language**: C# 12 with nullable reference types enabled

### Single Instance Detection
The application uses a named Mutex to prevent multiple instances from running simultaneously.

### Data Persistence
Settings are stored in:
```
%APPDATA%\WinWidgetBattery\settings.json
```

### Update Interval
Default update interval: 1000ms (1 second)
Configurable per widget in future versions

## Usage

### Launch the Application
Simply run `WinWidgetBattery.exe`

### Add Widgets
1. Click the system tray icon
2. Select "Add Widget"
3. A new battery widget will appear on your desktop

### Move Widgets
- Click and drag the widget border to reposition
- Position is automatically saved

### Configure
- Right-click widget → "Settings" for configuration options
- Settings are persisted between sessions

### Remove Widgets
- Right-click widget → "Remove Widget"
- Confirm deletion

## Requirements

- Windows 10 or later
- .NET 10 Runtime
- Working battery (laptop or UPS)

## Building

```bash
dotnet build
dotnet run
```

## Future Enhancements

- [ ] Multiple language support
- [ ] Customizable colors and themes
- [ ] Adjustable update intervals
- [ ] Sound notifications for low battery
- [ ] Task scheduler integration for automatic startup
- [ ] Battery health information
- [ ] Power plan display
- [ ] Temperature monitoring

## License

Copyright © 2026

## Credits

Based on the WinWidgetTime example project architecture, adapted for battery monitoring.
