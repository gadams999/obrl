# Design Document: About Dialog and Smart Text Display

## Overview

This design adds three main features to the Wheel Overlay application:
1. An "About Wheel Overlay" dialog accessible from the system tray context menu
2. Smart text condensing that hides empty positions and provides visual feedback when empty positions are selected
3. Test mode for development that simulates wheel input using keyboard arrow keys

The design maintains the existing architecture while adding new UI components, enhancing the display logic to handle sparse text configurations intelligently, and providing a keyboard-based testing mechanism for development without physical hardware.

## Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────────┐
│                     System Tray Icon                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Context Menu                                         │  │
│  │  - Settings...                                        │  │
│  │  - Config Mode                                        │  │
│  │  - Minimize                                           │  │
│  │  - About Wheel Overlay  ← NEW                        │  │
│  │  - Exit                                               │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ├─────────────────────────────┐
                              │                             │
                              ▼                             ▼
                    ┌──────────────────┐        ┌──────────────────────┐
                    │  AboutWindow     │        │  MainWindow          │
                    │  (NEW)           │        │  (ENHANCED)          │
                    │                  │        │                      │
                    │  - App Name      │        │  - OverlayViewModel  │
                    │  - Version       │        │  - Layout Views      │
                    │  - Description   │        │  - Flash Animation   │
                    │  - GitHub Link   │        │    (NEW)             │
                    │  - Copyright     │        │  - Position Filter   │
                    └──────────────────┘        │    (NEW)             │
                                                │  - Test Mode         │
                                                │    (NEW)             │
                                                └──────────────────────┘
                                                          │
                                                          ▼
                                                ┌──────────────────────┐
                                                │  InputService        │
                                                │  (ENHANCED)          │
                                                │                      │
                                                │  - DirectInput       │
                                                │  - Keyboard Input    │
                                                │    (NEW)             │
                                                └──────────────────────┘
```

### Component Interactions

1. **System Tray → About Dialog**: User clicks "About" menu item → App.xaml.cs opens AboutWindow
2. **MainWindow → ViewModel**: Position changes trigger ViewModel updates
3. **ViewModel → Layout Views**: ViewModel filters empty positions and manages flash state
4. **Layout Views → UI**: Views render only populated positions with appropriate styling
5. **InputService → ViewModel**: Keyboard or DirectInput events trigger position changes
6. **Test Mode → InputService**: Command-line flag enables keyboard input simulation

## Components and Interfaces

### 1. AboutWindow (NEW)

**Purpose**: Display application information in a modal dialog.

**XAML Structure**:
```xml
<Window>
  <StackPanel>
    <Image Source="app.ico" />
    <TextBlock Text="Wheel Overlay" FontSize="24" />
    <TextBlock Text="Version X.Y.Z" />
    <TextBlock Text="Description..." TextWrapping="Wrap" />
    <TextBlock>
      <Hyperlink NavigateUri="https://github.com/...">
        GitHub Repository
      </Hyperlink>
    </TextBlock>
    <TextBlock Text="Copyright © ..." />
    <Button Content="Close" />
  </StackPanel>
</Window>
```

**Properties**:
- `WindowStyle`: `ToolWindow` (for minimal chrome)
- `ResizeMode`: `NoResize`
- `WindowStartupLocation`: `CenterScreen`
- `ShowInTaskbar`: `False`
- `Width`: `400`
- `Height`: `300`

**Code-Behind**:
```csharp
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadVersionInfo();
    }

    private void LoadVersionInfo()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetName().Version;
        VersionTextBlock.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) 
        { 
            UseShellExecute = true 
        });
        e.Handled = true;
    }
}
```

### 2. App.xaml.cs (ENHANCED)

**New Method**:
```csharp
private void ShowAboutDialog()
{
    var aboutWindow = new AboutWindow
    {
        Owner = MainWindow
    };
    aboutWindow.ShowDialog();
}
```

**Context Menu Addition**:
```csharp
private void BuildContextMenu()
{
    // ... existing menu items ...
    
    var aboutMenuItem = new MenuItem { Header = "About Wheel Overlay" };
    aboutMenuItem.Click += (s, e) => ShowAboutDialog();
    
    _contextMenu.Items.Add(new Separator());
    _contextMenu.Items.Add(aboutMenuItem);
    _contextMenu.Items.Add(exitMenuItem);
}
```

### 3. OverlayViewModel (ENHANCED)

**New Properties**:
```csharp
public class OverlayViewModel : INotifyPropertyChanged
{
    // Existing properties...
    
    private bool _isFlashing;
    public bool IsFlashing
    {
        get => _isFlashing;
        set { _isFlashing = value; OnPropertyChanged(); }
    }
    
    private int _lastPopulatedPosition = 0;
    public int LastPopulatedPosition
    {
        get => _lastPopulatedPosition;
        private set { _lastPopulatedPosition = value; OnPropertyChanged(); }
    }
    
    public List<int> PopulatedPositions { get; private set; }
}
```

**New Methods**:
```csharp
private void UpdatePopulatedPositions()
{
    var profile = Settings.ActiveProfile;
    if (profile == null) return;
    
    PopulatedPositions = new List<int>();
    for (int i = 0; i < profile.TextLabels.Count; i++)
    {
        if (!string.IsNullOrWhiteSpace(profile.TextLabels[i]))
        {
            PopulatedPositions.Add(i);
        }
    }
    OnPropertyChanged(nameof(PopulatedPositions));
}

private void HandlePositionChange(int newPosition)
{
    bool isEmptyPosition = !PopulatedPositions.Contains(newPosition);
    
    if (isEmptyPosition)
    {
        TriggerFlashAnimation();
        // Don't update LastPopulatedPosition
    }
    else
    {
        StopFlashAnimation();
        LastPopulatedPosition = newPosition;
    }
    
    CurrentPosition = newPosition;
}

private async void TriggerFlashAnimation()
{
    IsFlashing = true;
    await Task.Delay(500);
    IsFlashing = false;
}

private void StopFlashAnimation()
{
    IsFlashing = false;
}
```

### 4. Layout Views (ENHANCED)

**VerticalLayout.xaml / HorizontalLayout.xaml / GridLayout.xaml**:

Update ItemsSource binding to use filtered list:
```xml
<ItemsControl ItemsSource="{Binding PopulatedPositionLabels}">
```

**ViewModel Property**:
```csharp
public List<string> PopulatedPositionLabels
{
    get
    {
        var profile = Settings.ActiveProfile;
        if (profile == null) return new List<string>();
        
        return PopulatedPositions
            .Select(i => profile.TextLabels[i])
            .ToList();
    }
}
```

**Flash Animation Style**:
```xml
<Style x:Key="FlashingTextStyle" TargetType="TextBlock">
    <Style.Triggers>
        <DataTrigger Binding="{Binding IsFlashing}" Value="True">
            <DataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard RepeatBehavior="Forever">
                        <ColorAnimation 
                            Storyboard.TargetProperty="Foreground.Color"
                            From="{Binding SelectedTextColor}"
                            To="{Binding NonSelectedTextColor}"
                            Duration="0:0:0.25"
                            AutoReverse="True"/>
                    </Storyboard>
                </BeginStoryboard>
            </DataTrigger.EnterActions>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

### 6. InputService (ENHANCED)

**Purpose**: Handle input from both physical DirectInput devices and keyboard (in test mode).

**New Properties**:
```csharp
public class InputService : IDisposable
{
    // Existing properties...
    
    private bool _testMode;
    private int _testModePosition = 0;
    private const int TEST_MODE_MAX_POSITION = 7; // 0-indexed, 8 positions
    
    public bool TestMode
    {
        get => _testMode;
        set
        {
            _testMode = value;
            if (_testMode)
            {
                EnableKeyboardInput();
            }
            else
            {
                DisableKeyboardInput();
            }
        }
    }
}
```

**New Methods**:
```csharp
private void EnableKeyboardInput()
{
    // Register global keyboard hook or window-level key handler
    Application.Current.MainWindow.KeyDown += OnTestModeKeyDown;
}

private void DisableKeyboardInput()
{
    if (Application.Current.MainWindow != null)
    {
        Application.Current.MainWindow.KeyDown -= OnTestModeKeyDown;
    }
}

private void OnTestModeKeyDown(object sender, KeyEventArgs e)
{
    if (!_testMode) return;
    
    switch (e.Key)
    {
        case Key.Left:
            _testModePosition--;
            if (_testModePosition < 0)
                _testModePosition = TEST_MODE_MAX_POSITION;
            RaiseRotaryPositionChanged(_testModePosition);
            e.Handled = true;
            break;
            
        case Key.Right:
            _testModePosition++;
            if (_testModePosition > TEST_MODE_MAX_POSITION)
                _testModePosition = 0;
            RaiseRotaryPositionChanged(_testModePosition);
            e.Handled = true;
            break;
    }
}

private void RaiseRotaryPositionChanged(int position)
{
    RotaryPositionChanged?.Invoke(this, position);
}
```

**Start Method Enhancement**:
```csharp
public void Start(string deviceName)
{
    // Check for test mode flag
    var args = Environment.GetCommandLineArgs();
    if (args.Contains("--test-mode") || args.Contains("/test"))
    {
        TestMode = true;
        LogService.Info("Test mode enabled - using keyboard input");
        RaiseDeviceConnected();
        return;
    }
    
    // Existing DirectInput initialization...
}
```

### 7. MainWindow (ENHANCED for Test Mode)

**Test Mode Indicator**:

Add visual indicator in XAML:
```xml
<Border x:Name="TestModeIndicator" 
        Background="#FFFF00" 
        Padding="5"
        Visibility="{Binding IsTestMode, Converter={StaticResource BoolToVisibilityConverter}}"
        HorizontalAlignment="Right"
        VerticalAlignment="Top">
    <TextBlock Text="TEST MODE" 
               FontWeight="Bold" 
               Foreground="Black"/>
</Border>
```

**ViewModel Property**:
```csharp
public bool IsTestMode { get; set; }
```

**Initialization**:
```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ... existing initialization ...
    
    _viewModel.IsTestMode = _inputService.TestMode;
}
```

### 5. SingleTextLayout.xaml (ENHANCED)

**Display Logic**:
```csharp
public string DisplayedText
{
    get
    {
        var profile = Settings.ActiveProfile;
        if (profile == null) return "";
        
        int displayPosition = PopulatedPositions.Contains(CurrentPosition)
            ? CurrentPosition
            : LastPopulatedPosition;
        
        return profile.TextLabels[displayPosition];
    }
}

public Brush DisplayedTextColor
{
    get
    {
        bool isEmptyPosition = !PopulatedPositions.Contains(CurrentPosition);
        return isEmptyPosition 
            ? NonSelectedTextBrush 
            : SelectedTextBrush;
    }
}
```

## Data Models

### Test Mode Configuration

**Command-Line Arguments**:
- `--test-mode` or `/test`: Enable test mode
- Example: `WheelOverlay.exe --test-mode`

**Test Wheel Profile**:
- Based on BavarianSimTec Alpha configuration
- 8 positions (0-7 internally, displayed as 1-8)
- Default labels: "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF"

**Position Simulation State**:
- **TestModePosition**: `int` - Current simulated position (0-7), wraps around at boundaries, initialized to 0

### AboutWindow Data

No new data models required. Version information is read directly from assembly metadata using `System.Reflection.Assembly`.

### Position Filtering Data

**PopulatedPositions**: `List<int>`
- Contains indices of positions that have non-empty text
- Updated whenever profile or text labels change
- Used to filter display in all layouts

**LastPopulatedPosition**: `int`
- Tracks the most recent position that had text
- Used in Single layout when empty position is selected
- Initialized to first populated position on startup

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Version Reading Consistency

*For any* assembly with version metadata, reading the version should return a non-null, valid version string that matches the assembly's Version attribute.

**Validates: Requirements 5.1, 5.3**

### Property 2: Populated Position Filtering

*For any* profile configuration with N positions where M positions have text (M ≤ N), the display in Vertical, Horizontal, or Grid layouts should render exactly M items.

**Validates: Requirements 6.1, 6.3**

### Property 3: Position Number Preservation

*For any* set of populated positions, the position numbers associated with each text label should remain unchanged regardless of how many empty positions are filtered out.

**Validates: Requirements 6.4**

### Property 4: Configuration Change Reactivity

*For any* change to the text labels configuration, the display should immediately update to show only the newly populated positions.

**Validates: Requirements 6.5**

### Property 5: Empty Position Flash Trigger

*For any* empty position selection in Vertical, Horizontal, Grid, or Single layouts, a flash animation should be triggered.

**Validates: Requirements 7.1, 8.3**

### Property 6: Flash Animation Duration

*For any* flash animation trigger, the animation should last approximately 500 milliseconds (±50ms tolerance).

**Validates: Requirements 7.3, 8.4**

### Property 7: Flash Termination on Populated Selection

*For any* sequence where an empty position is selected followed by a populated position, the flash animation should stop and normal highlighting should resume.

**Validates: Requirements 7.4**

### Property 8: Flash Restart on Empty Selection

*For any* empty position selection that occurs while a flash animation is already running, the animation should restart from the beginning.

**Validates: Requirements 7.5**

### Property 9: Single Layout Last Position Display

*For any* empty position selection in Single layout, the displayed text should be the text from the most recently selected populated position.

**Validates: Requirements 8.1**

### Property 10: Single Layout Empty Position Color

*For any* empty position displayed in Single layout, the text color should be the non-selected color.

**Validates: Requirements 8.2**

### Property 11: Populated Selection No Flash

*For any* populated position selection following an empty position in Single layout, the text should display in selected color without flashing.

**Validates: Requirements 8.5**

### Property 12: Test Mode Position Increment

*For any* current position P in test mode (0 ≤ P ≤ 7), pressing the Right arrow key should result in position (P + 1) mod 8.

**Validates: Requirements 9.3, 9.6**

### Property 13: Test Mode Position Decrement

*For any* current position P in test mode (0 ≤ P ≤ 7), pressing the Left arrow key should result in position (P - 1 + 8) mod 8.

**Validates: Requirements 9.4, 9.5**

### Property 14: Test Mode Activation

*For any* application start with the `--test-mode` or `/test` command-line flag, the system should enable keyboard input and disable DirectInput device detection.

**Validates: Requirements 9.1, 9.8**

## Error Handling

### About Dialog Errors

1. **Version Read Failure**: If assembly version cannot be read, display "Version Unknown"
2. **Hyperlink Navigation Failure**: Catch and log exceptions when opening URLs, show error message to user
3. **Dialog Creation Failure**: Log error and fail gracefully without crashing application

### Position Filtering Errors

1. **No Populated Positions**: If all positions are empty, display a placeholder message "No labels configured"
2. **Invalid Position Index**: Validate position indices before accessing text labels array
3. **Null Profile**: Check for null profile before accessing text labels

### Flash Animation Errors

1. **Animation Cancellation**: Ensure flash animation can be cancelled cleanly when position changes
2. **Concurrent Animations**: Prevent multiple flash animations from running simultaneously
3. **Disposed ViewModel**: Check if ViewModel is disposed before starting animations

## Testing Strategy

### Dual Testing Approach

This feature will use both unit tests and property-based tests:

- **Unit tests**: Verify specific examples, edge cases (all empty, all populated, startup scenarios), and UI element presence
- **Property tests**: Verify universal properties across all input combinations (position filtering, flash behavior, color selection)

### Property-Based Testing

We will use **MSTest** with **FsCheck** for property-based testing in C#. Each property test will:
- Run a minimum of 100 iterations
- Generate random position configurations (various combinations of empty/populated positions)
- Generate random position selection sequences
- Verify the correctness properties hold for all generated inputs

### Test Configuration

Each property test will be tagged with a comment:
```csharp
// Feature: about-dialog, Property 2: Populated Position Filtering
[TestMethod]
public void Property_PopulatedPositionFiltering()
{
    Prop.ForAll<Profile>(profile =>
    {
        // Test implementation
    }).QuickCheckThrowOnFailure();
}
```

### Unit Test Coverage

Unit tests will cover:
1. About dialog displays all required elements (name, version, description, link, copyright)
2. About menu item exists in context menu at correct position
3. Dialog is modal and has correct window properties
4. Close button and Escape key close the dialog
5. Version matches assembly metadata
6. Edge case: All positions empty
7. Edge case: All positions populated
8. Edge case: First position empty on startup
9. Flash animation timing (500ms ±50ms)
10. Test mode: Command-line flag detection
11. Test mode: Left arrow decrements position with wrap-around
12. Test mode: Right arrow increments position with wrap-around
13. Test mode: Visual indicator displays when test mode is active
14. Test mode: DirectInput disabled when test mode is active

### Integration Testing

Manual testing will verify:
1. About dialog appearance and layout
2. GitHub link opens in browser
3. Flash animation visual appearance
4. Smooth transitions between positions
5. Behavior across all layout types (Single, Vertical, Horizontal, Grid)
