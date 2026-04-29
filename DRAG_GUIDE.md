# Drag-to-Position Guide

## How to Drag the Battery Widget

The battery widget supports easy dragging to reposition it anywhere on your screen.

### Basic Dragging

1. **Move your mouse** over the widget border/background
2. **Click and hold** the left mouse button on the widget
3. **Drag** the widget to your desired position
4. **Release** the mouse button to place the widget

### Drag Details

- **Drag Start**: Click on the border of the widget to begin dragging
- **Smooth Movement**: The widget follows your cursor smoothly as you drag
- **Free Positioning**: You can place the widget at any screen coordinates
- **Auto-Save**: Position is automatically saved when you release the mouse button

### Tips

- **Precise Positioning**: Move your mouse slowly for precise placement
- **Screen Edges**: You can drag the widget to screen edges, including partially off-screen if desired
- **Multiple Monitors**: The widget can be dragged to any connected monitor
- **Visual Feedback**: The widget background slightly brightens when you hover, indicating it's interactive

### Technical Implementation

#### How It Works

The dragging system uses three key events:

1. **MouseLeftButtonDown**: 
   - Records the starting mouse position
   - Captures the current window position
   - Sets the capture mode to track mouse even outside the window
   - Marks the widget as "dragging"

2. **MouseMove**:
   - Calculates the distance moved since drag started
   - Updates the window's Left and Top properties based on delta movement
   - Only updates position if left mouse button is pressed

3. **MouseLeftButtonUp**:
   - Ends the drag operation
   - Releases the mouse capture
   - Saves the new position to persistent storage

#### Code Flow

```csharp
// When user clicks on widget
private void Widget_MouseLeftButtonDown(...)
{
    _isDragging = true;
    _dragStart = e.GetPosition(null);           // Record cursor position
    _windowStartLeft = Left;                     // Record window position
    _windowStartTop = Top;
    CaptureMouse();                              // Capture mouse events
}

// While user moves mouse
private void Widget_MouseMove(...)
{
    if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
    {
        var currentPos = e.GetPosition(null);
        Left = _windowStartLeft + (currentPos.X - _dragStart.X);   // New X
        Top = _windowStartTop + (currentPos.Y - _dragStart.Y);     // New Y
    }
}

// When user releases mouse
private void Widget_MouseLeftButtonUp(...)
{
    _isDragging = false;
    ReleaseMouseCapture();                       // Stop capturing
    _settings.X = (int)Left;                     // Save position
    _settings.Y = (int)Top;
    SettingsService.Save(App.Settings);
}
```

### Data Persistence

When you release the mouse button:
- The new position coordinates (X, Y) are saved to the `WidgetSettings`
- Settings are persisted to JSON in `%APPDATA%\WinWidgetBattery\settings.json`
- When the widget restarts, it loads from the saved position

### Interaction Areas

The drag functionality is attached to the `WidgetBorder`, which means you can click and drag from:
- The semi-transparent background
- The title bar area
- The battery information display

### Disabling Drag (Advanced)

If you want to disable dragging, you can:
1. Remove the `MouseLeftButtonDown`, `MouseMove`, `MouseLeftButtonUp` event handlers from `WidgetBorder` in the XAML
2. Or set `IsHitTestVisible="False"` on the `WidgetBorder`

### Example Positions

After dragging, your widget might be positioned at:
- **Top-Right Corner**: `(1840, 10)` on a 1920x1080 screen
- **Bottom-Left Corner**: `(10, 1070)` on a 1920x1080 screen
- **Center**: `(1920/2 - width/2, 1080/2 - height/2)`
- **Across Monitors**: Coordinates are system-wide, so you can position it on any connected monitor

### Troubleshooting

**Widget doesn't move when dragging?**
- Ensure you're clicking on the `WidgetBorder` element
- Check that `MouseLeftButtonDown` event is firing (add debug output if needed)

**Position not being saved?**
- Verify `SettingsService.Save()` is being called
- Check that `%APPDATA%\WinWidgetBattery\` directory is writable
- Check the JSON file at `%APPDATA%\WinWidgetBattery\settings.json`

**Widget jumps when starting to drag?**
- This shouldn't happen with the current implementation
- If it does, check that `_dragStart` and `_windowStart*` are being set correctly

**Dragging to wrong position?**
- Ensure you're using `e.GetPosition(null)` to get screen coordinates, not relative to the control
