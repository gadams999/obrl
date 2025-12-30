# Design Document: v0.5.0 Enhancements

## Overview

This design adds three major enhancements to the Wheel Overlay application for version 0.5.0:

1. **Animated Position Transitions**: Smooth directional animations when changing positions in Single layout
2. **Configurable Grid Dimensions**: User-adjustable grid layout (2×N, 3×3, 4×2, etc.) with smart condensing
3. **Variable Position Count**: Support for 2-20 positions per profile instead of fixed 8 positions

These enhancements provide a more polished user experience, greater flexibility for different wheel hardware configurations, and better visual feedback during position changes. The design maintains the existing architecture while extending the Profile data model, enhancing the UI settings interface, and adding animation capabilities to the Single layout view.

## Architecture

### High-Level Component Changes

```
┌─────────────────────────────────────────────────────────────────┐
│                     Settings Window (ENHANCED)                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Profile Configuration                                     │  │
│  │  - Position Count: [2-20] ← NEW                          │  │
│  │  - Grid Dimensions: [Rows] × [Columns] ← NEW             │  │
│  │  - Text Labels: [Dynamic 2-20 fields] ← ENHANCED         │  │
│  │  - Layout Selection                                       │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────────┐
                    │  Profile Model       │
                    │  (ENHANCED)          │
                    │                      │
                    │  - PositionCount     │
                    │  - GridRows          │
                    │  - GridColumns       │
                    │  - TextLabels[N]     │
                    └──────────────────────┘
                              │
                              ▼
                    ┌──────────────────────┐
                    │  MainWindow          │
                    │  (ENHANCED)          │
                    │                      │
                    │  - SingleLayout      │
                    │    + Animations      │
                    │  - GridLayout        │
                    │    + Dynamic Grid    │
                    └──────────────────────┘
                              │
                              ▼
                    ┌──────────────────────┐
                    │  InputService        │
                    │  (ENHANCED)          │
                    │                      │
                    │  - Variable Button   │
                    │    Range (57-76)     │
                    └──────────────────────┘
```

### Component Interactions

1. **Settings → Profile**: User configures PositionCount and GridDimensions
2. **Profile → MainWindow**: Profile changes trigger layout updates
3. **MainWindow → SingleLayout**: Position changes trigger directional animations
4. **MainWindow → GridLayout**: Grid renders with configured dimensions
5. **InputService → MainWindow**: Button range adapts to PositionCount
6. **Profile Validation**: Grid dimensions validated against position count


## Components and Interfaces

### 1. Profile Model (ENHANCED)

**Purpose**: Store configuration data including new position count and grid dimension settings.

**New Properties**:
```csharp
public class Profile
{
    // Existing properties...
    public string Name { get; set; }
    public string DeviceGuid { get; set; }
    public List<string> TextLabels { get; set; }
    public LayoutType Layout { get; set; }
    
    // NEW properties for v0.5.0
    public int PositionCount { get; set; } = 8;  // Default 8 for compatibility
    public int GridRows { get; set; } = 2;       // Default 2 rows
    public int GridColumns { get; set; } = 4;    // Default 4 columns (2×4 grid)
    
    // Validation
    public bool IsValidGridConfiguration()
    {
        return GridRows * GridColumns >= PositionCount;
    }
    
    public void AdjustGridToDefault()
    {
        GridRows = 2;
        GridColumns = (int)Math.Ceiling(PositionCount / 2.0);
    }
}
```

**Validation Logic**:
```csharp
public class ProfileValidator
{
    public static ValidationResult ValidateGridDimensions(Profile profile)
    {
        int capacity = profile.GridRows * profile.GridColumns;
        
        if (capacity < profile.PositionCount)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = $"Grid capacity ({capacity}) must be >= position count ({profile.PositionCount})"
            };
        }
        
        if (profile.GridRows < 1 || profile.GridRows > 10)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = "Grid rows must be between 1 and 10"
            };
        }
        
        if (profile.GridColumns < 1 || profile.GridColumns > 10)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = "Grid columns must be between 1 and 10"
            };
        }
        
        return new ValidationResult { IsValid = true };
    }
    
    public static List<GridDimension> GetSuggestedDimensions(int positionCount)
    {
        var suggestions = new List<GridDimension>();
        
        // 2×N configurations
        suggestions.Add(new GridDimension(2, (int)Math.Ceiling(positionCount / 2.0)));
        
        // 3×N configurations
        if (positionCount > 3)
            suggestions.Add(new GridDimension(3, (int)Math.Ceiling(positionCount / 3.0)));
        
        // 4×N configurations
        if (positionCount > 4)
            suggestions.Add(new GridDimension(4, (int)Math.Ceiling(positionCount / 4.0)));
        
        // N×2, N×3, N×4 configurations
        suggestions.Add(new GridDimension((int)Math.Ceiling(positionCount / 2.0), 2));
        if (positionCount > 3)
            suggestions.Add(new GridDimension((int)Math.Ceiling(positionCount / 3.0), 3));
        if (positionCount > 4)
            suggestions.Add(new GridDimension((int)Math.Ceiling(positionCount / 4.0), 4));
        
        // Square-ish configurations
        int sqrt = (int)Math.Ceiling(Math.Sqrt(positionCount));
        suggestions.Add(new GridDimension(sqrt, sqrt));
        
        return suggestions.Distinct().OrderBy(d => d.Rows * d.Columns).ToList();
    }
}

public record GridDimension(int Rows, int Columns);
public record ValidationResult(bool IsValid, string Message = "");
```


### 2. Settings Window (ENHANCED)

**Purpose**: Provide UI for configuring position count and grid dimensions.

**New XAML Elements**:
```xml
<StackPanel Margin="10">
    <!-- Position Count Configuration -->
    <Label Content="Number of Positions:" />
    <ComboBox x:Name="PositionCountComboBox"
              ItemsSource="{Binding AvailablePositionCounts}"
              SelectedItem="{Binding SelectedProfile.PositionCount}"
              SelectionChanged="PositionCount_Changed"/>
    <TextBlock Text="Configure how many positions your wheel has (2-20)"
               FontSize="10" Foreground="Gray" Margin="0,2,0,10"/>
    
    <!-- Grid Dimensions Configuration -->
    <Label Content="Grid Layout Dimensions:" />
    <StackPanel Orientation="Horizontal" Margin="0,5">
        <Label Content="Rows:" Width="50"/>
        <ComboBox x:Name="GridRowsComboBox"
                  ItemsSource="{Binding AvailableRows}"
                  SelectedItem="{Binding SelectedProfile.GridRows}"
                  Width="60"
                  SelectionChanged="GridDimensions_Changed"/>
        <Label Content="×" Margin="10,0"/>
        <Label Content="Columns:" Width="70"/>
        <ComboBox x:Name="GridColumnsComboBox"
                  ItemsSource="{Binding AvailableColumns}"
                  SelectedItem="{Binding SelectedProfile.GridColumns}"
                  Width="60"
                  SelectionChanged="GridDimensions_Changed"/>
    </StackPanel>
    
    <!-- Grid Preview -->
    <Border BorderBrush="Gray" BorderThickness="1" Padding="10" Margin="0,5">
        <StackPanel>
            <TextBlock x:Name="GridCapacityText" 
                       Text="{Binding GridCapacityDisplay}"
                       FontSize="11"/>
            <ItemsControl x:Name="GridPreview" 
                          ItemsSource="{Binding GridPreviewCells}"
                          Margin="0,5">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Rows="{Binding SelectedProfile.GridRows}"
                                   Columns="{Binding SelectedProfile.GridColumns}"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border BorderBrush="LightGray" BorderThickness="1" 
                                Width="30" Height="30" Margin="2">
                            <TextBlock Text="{Binding}" 
                                     HorizontalAlignment="Center"
                                     VerticalAlignment="Center"
                                     FontSize="10"/>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </StackPanel>
    </Border>
    
    <!-- Suggested Dimensions -->
    <Label Content="Suggested Dimensions:" Margin="0,10,0,0"/>
    <ItemsControl ItemsSource="{Binding SuggestedDimensions}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Button Content="{Binding DisplayText}"
                        Command="{Binding DataContext.ApplySuggestedDimensionCommand, 
                                 RelativeSource={RelativeSource AncestorType=Window}}"
                        CommandParameter="{Binding}"
                        Margin="2"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <WrapPanel/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
    </ItemsControl>
    
    <!-- Text Labels (Dynamic) -->
    <Label Content="Position Labels:" Margin="0,15,0,5"/>
    <ScrollViewer MaxHeight="300" VerticalScrollBarVisibility="Auto">
        <ItemsControl x:Name="TextLabelsPanel"
                      ItemsSource="{Binding TextLabelInputs}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Margin="0,2">
                        <Label Content="{Binding PositionNumber}" Width="80"/>
                        <TextBox Text="{Binding Label, UpdateSourceTrigger=PropertyChanged}"
                                 Width="200"/>
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </ScrollViewer>
</StackPanel>
```

**Code-Behind**:
```csharp
public partial class SettingsWindow : Window
{
    private SettingsViewModel _viewModel;
    
    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
    }
    
    private void PositionCount_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.SelectedProfile == null) return;
        
        int newCount = (int)PositionCountComboBox.SelectedItem;
        int oldCount = _viewModel.SelectedProfile.TextLabels.Count;
        
        if (newCount < oldCount)
        {
            // Check if any positions being removed have text
            bool hasPopulatedPositions = _viewModel.SelectedProfile.TextLabels
                .Skip(newCount)
                .Any(label => !string.IsNullOrWhiteSpace(label));
            
            if (hasPopulatedPositions)
            {
                var result = MessageBox.Show(
                    $"Reducing position count will remove labels for positions {newCount + 1}-{oldCount}. Continue?",
                    "Confirm Position Count Change",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.No)
                {
                    PositionCountComboBox.SelectedItem = oldCount;
                    return;
                }
            }
        }
        
        _viewModel.UpdatePositionCount(newCount);
        ValidateAndAdjustGridDimensions();
    }
    
    private void GridDimensions_Changed(object sender, SelectionChangedEventArgs e)
    {
        ValidateGridDimensions();
    }
    
    private void ValidateAndAdjustGridDimensions()
    {
        var profile = _viewModel.SelectedProfile;
        if (profile == null) return;
        
        if (!profile.IsValidGridConfiguration())
        {
            profile.AdjustGridToDefault();
            _viewModel.RefreshGridPreview();
            
            MessageBox.Show(
                $"Grid dimensions adjusted to {profile.GridRows}×{profile.GridColumns} to accommodate {profile.PositionCount} positions.",
                "Grid Adjusted",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
    
    private void ValidateGridDimensions()
    {
        var result = ProfileValidator.ValidateGridDimensions(_viewModel.SelectedProfile);
        
        if (!result.IsValid)
        {
            MessageBox.Show(result.Message, "Invalid Grid Configuration", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.SelectedProfile.AdjustGridToDefault();
        }
        
        _viewModel.RefreshGridPreview();
    }
}
```


### 3. SettingsViewModel (NEW)

**Purpose**: Manage settings UI state and provide data binding for position count and grid configuration.

```csharp
public class SettingsViewModel : INotifyPropertyChanged
{
    private Profile _selectedProfile;
    
    public Profile SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            _selectedProfile = value;
            OnPropertyChanged();
            RefreshTextLabelInputs();
            RefreshGridPreview();
            RefreshSuggestedDimensions();
        }
    }
    
    public List<int> AvailablePositionCounts { get; } = 
        Enumerable.Range(2, 19).ToList(); // 2-20
    
    public List<int> AvailableRows { get; } = 
        Enumerable.Range(1, 10).ToList(); // 1-10
    
    public List<int> AvailableColumns { get; } = 
        Enumerable.Range(1, 10).ToList(); // 1-10
    
    private ObservableCollection<TextLabelInput> _textLabelInputs;
    public ObservableCollection<TextLabelInput> TextLabelInputs
    {
        get => _textLabelInputs;
        set { _textLabelInputs = value; OnPropertyChanged(); }
    }
    
    private ObservableCollection<string> _gridPreviewCells;
    public ObservableCollection<string> GridPreviewCells
    {
        get => _gridPreviewCells;
        set { _gridPreviewCells = value; OnPropertyChanged(); }
    }
    
    private ObservableCollection<SuggestedDimension> _suggestedDimensions;
    public ObservableCollection<SuggestedDimension> SuggestedDimensions
    {
        get => _suggestedDimensions;
        set { _suggestedDimensions = value; OnPropertyChanged(); }
    }
    
    public string GridCapacityDisplay
    {
        get
        {
            if (SelectedProfile == null) return "";
            int capacity = SelectedProfile.GridRows * SelectedProfile.GridColumns;
            return $"Grid Capacity: {capacity} (Position Count: {SelectedProfile.PositionCount})";
        }
    }
    
    public ICommand ApplySuggestedDimensionCommand { get; }
    
    public SettingsViewModel()
    {
        ApplySuggestedDimensionCommand = new RelayCommand<SuggestedDimension>(ApplySuggestedDimension);
    }
    
    public void UpdatePositionCount(int newCount)
    {
        if (SelectedProfile == null) return;
        
        int oldCount = SelectedProfile.TextLabels.Count;
        
        if (newCount > oldCount)
        {
            // Add empty labels
            for (int i = oldCount; i < newCount; i++)
            {
                SelectedProfile.TextLabels.Add("");
            }
        }
        else if (newCount < oldCount)
        {
            // Remove labels
            SelectedProfile.TextLabels.RemoveRange(newCount, oldCount - newCount);
        }
        
        SelectedProfile.PositionCount = newCount;
        RefreshTextLabelInputs();
        RefreshSuggestedDimensions();
        OnPropertyChanged(nameof(GridCapacityDisplay));
    }
    
    public void RefreshTextLabelInputs()
    {
        if (SelectedProfile == null) return;
        
        TextLabelInputs = new ObservableCollection<TextLabelInput>(
            SelectedProfile.TextLabels.Select((label, index) => 
                new TextLabelInput
                {
                    PositionNumber = $"Position {index + 1}:",
                    Label = label,
                    Index = index
                }));
    }
    
    public void RefreshGridPreview()
    {
        if (SelectedProfile == null) return;
        
        var cells = new List<string>();
        int totalCells = SelectedProfile.GridRows * SelectedProfile.GridColumns;
        
        for (int i = 0; i < totalCells; i++)
        {
            if (i < SelectedProfile.PositionCount)
            {
                cells.Add((i + 1).ToString());
            }
            else
            {
                cells.Add(""); // Empty cell
            }
        }
        
        GridPreviewCells = new ObservableCollection<string>(cells);
    }
    
    public void RefreshSuggestedDimensions()
    {
        if (SelectedProfile == null) return;
        
        var suggestions = ProfileValidator.GetSuggestedDimensions(SelectedProfile.PositionCount);
        SuggestedDimensions = new ObservableCollection<SuggestedDimension>(
            suggestions.Select(d => new SuggestedDimension
            {
                Rows = d.Rows,
                Columns = d.Columns,
                DisplayText = $"{d.Rows}×{d.Columns}"
            }));
    }
    
    private void ApplySuggestedDimension(SuggestedDimension dimension)
    {
        if (SelectedProfile == null) return;
        
        SelectedProfile.GridRows = dimension.Rows;
        SelectedProfile.GridColumns = dimension.Columns;
        OnPropertyChanged(nameof(SelectedProfile));
        RefreshGridPreview();
        OnPropertyChanged(nameof(GridCapacityDisplay));
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TextLabelInput : INotifyPropertyChanged
{
    public string PositionNumber { get; set; }
    
    private string _label;
    public string Label
    {
        get => _label;
        set { _label = value; OnPropertyChanged(); }
    }
    
    public int Index { get; set; }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SuggestedDimension
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public string DisplayText { get; set; }
}
```


### 4. SingleTextLayout (ENHANCED with Animations)

**Purpose**: Display single position text with directional transition animations.

**XAML Structure**:
```xml
<Grid x:Name="AnimationContainer">
    <!-- Current Text (animating out) -->
    <TextBlock x:Name="CurrentText"
               Text="{Binding CurrentDisplayText}"
               FontSize="{Binding FontSize}"
               Foreground="{Binding SelectedTextBrush}"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               RenderTransformOrigin="0.5,0.5">
        <TextBlock.RenderTransform>
            <TransformGroup>
                <RotateTransform x:Name="CurrentRotate"/>
                <TranslateTransform x:Name="CurrentTranslate"/>
            </TransformGroup>
        </TextBlock.RenderTransform>
    </TextBlock>
    
    <!-- Next Text (animating in) -->
    <TextBlock x:Name="NextText"
               Text="{Binding NextDisplayText}"
               FontSize="{Binding FontSize}"
               Foreground="{Binding SelectedTextBrush}"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               Opacity="0"
               RenderTransformOrigin="0.5,0.5">
        <TextBlock.RenderTransform>
            <TransformGroup>
                <RotateTransform x:Name="NextRotate"/>
                <TranslateTransform x:Name="NextTranslate"/>
            </TransformGroup>
        </TextBlock.RenderTransform>
    </TextBlock>
</Grid>
```

**Animation Code-Behind**:
```csharp
public partial class SingleTextLayout : UserControl
{
    private const double ANIMATION_DURATION_MS = 250;
    private const double ROTATION_ANGLE = 15; // degrees
    private const double TRANSLATION_DISTANCE = 50; // pixels
    
    private int _currentPosition = -1;
    private bool _isAnimating = false;
    
    public void OnPositionChanged(int newPosition, int oldPosition)
    {
        if (_isAnimating)
        {
            // Interrupt current animation
            StopCurrentAnimation();
        }
        
        // Determine direction
        bool isForward = IsForwardTransition(oldPosition, newPosition);
        
        // Start animation
        AnimateTransition(newPosition, isForward);
    }
    
    private bool IsForwardTransition(int oldPos, int newPos)
    {
        var profile = DataContext as OverlayViewModel;
        if (profile == null) return true;
        
        int positionCount = profile.Settings.ActiveProfile.PositionCount;
        
        // Handle wrap-around
        if (oldPos == positionCount - 1 && newPos == 0)
            return true; // Wrapping forward
        if (oldPos == 0 && newPos == positionCount - 1)
            return false; // Wrapping backward
        
        return newPos > oldPos;
    }
    
    private async void AnimateTransition(int newPosition, bool isForward)
    {
        _isAnimating = true;
        
        // Set up next text
        var viewModel = DataContext as OverlayViewModel;
        NextText.Text = viewModel.GetTextForPosition(newPosition);
        
        // Create animations
        var duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS);
        
        // Current text: rotate and fade out
        var currentRotateAnim = new DoubleAnimation
        {
            To = isForward ? -ROTATION_ANGLE : ROTATION_ANGLE,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        var currentTranslateAnim = new DoubleAnimation
        {
            To = isForward ? -TRANSLATION_DISTANCE : TRANSLATION_DISTANCE,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        var currentOpacityAnim = new DoubleAnimation
        {
            To = 0,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        // Next text: rotate and fade in
        var nextRotateAnim = new DoubleAnimation
        {
            From = isForward ? ROTATION_ANGLE : -ROTATION_ANGLE,
            To = 0,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        var nextTranslateAnim = new DoubleAnimation
        {
            From = isForward ? TRANSLATION_DISTANCE : -TRANSLATION_DISTANCE,
            To = 0,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        var nextOpacityAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        // Apply animations
        CurrentRotate.BeginAnimation(RotateTransform.AngleProperty, currentRotateAnim);
        CurrentTranslate.BeginAnimation(TranslateTransform.YProperty, currentTranslateAnim);
        CurrentText.BeginAnimation(OpacityProperty, currentOpacityAnim);
        
        NextRotate.BeginAnimation(RotateTransform.AngleProperty, nextRotateAnim);
        NextTranslate.BeginAnimation(TranslateTransform.YProperty, nextTranslateAnim);
        NextText.BeginAnimation(OpacityProperty, nextOpacityAnim);
        
        // Wait for animation to complete
        await Task.Delay((int)ANIMATION_DURATION_MS);
        
        // Swap texts
        CurrentText.Text = NextText.Text;
        CurrentText.Opacity = 1;
        CurrentRotate.Angle = 0;
        CurrentTranslate.Y = 0;
        
        NextText.Opacity = 0;
        NextRotate.Angle = 0;
        NextTranslate.Y = 0;
        
        _currentPosition = newPosition;
        _isAnimating = false;
    }
    
    private void StopCurrentAnimation()
    {
        // Stop all animations
        CurrentRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        CurrentTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        CurrentText.BeginAnimation(OpacityProperty, null);
        
        NextRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        NextTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        NextText.BeginAnimation(OpacityProperty, null);
        
        // Reset to stable state
        CurrentText.Opacity = 1;
        CurrentRotate.Angle = 0;
        CurrentTranslate.Y = 0;
        
        NextText.Opacity = 0;
        NextRotate.Angle = 0;
        NextTranslate.Y = 0;
        
        _isAnimating = false;
    }
}
```


### 5. GridLayout (ENHANCED with Dynamic Dimensions)

**Purpose**: Display positions in a configurable grid with smart condensing.

**XAML Structure**:
```xml
<ItemsControl x:Name="GridItemsControl"
              ItemsSource="{Binding PopulatedPositionItems}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <UniformGrid Rows="{Binding EffectiveGridRows}"
                       Columns="{Binding EffectiveGridColumns}"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border BorderBrush="{Binding BorderBrush}" 
                    BorderThickness="2"
                    Background="{Binding BackgroundBrush}"
                    Margin="5">
                <StackPanel>
                    <TextBlock Text="{Binding PositionNumber}"
                             FontSize="12"
                             Foreground="{Binding TextBrush}"
                             HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding Label}"
                             FontSize="{Binding FontSize}"
                             Foreground="{Binding TextBrush}"
                             HorizontalAlignment="Center"
                             TextWrapping="Wrap"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**ViewModel Logic**:
```csharp
public class OverlayViewModel : INotifyPropertyChanged
{
    // ... existing properties ...
    
    public int EffectiveGridRows
    {
        get
        {
            if (Settings?.ActiveProfile == null) return 2;
            
            int populatedCount = PopulatedPositions.Count;
            if (populatedCount == 0) return 1;
            
            var profile = Settings.ActiveProfile;
            int configuredCapacity = profile.GridRows * profile.GridColumns;
            
            // If all positions are populated, use configured dimensions
            if (populatedCount >= configuredCapacity)
                return profile.GridRows;
            
            // Calculate condensed dimensions maintaining aspect ratio
            return CalculateCondensedRows(populatedCount, profile.GridRows, profile.GridColumns);
        }
    }
    
    public int EffectiveGridColumns
    {
        get
        {
            if (Settings?.ActiveProfile == null) return 4;
            
            int populatedCount = PopulatedPositions.Count;
            if (populatedCount == 0) return 1;
            
            var profile = Settings.ActiveProfile;
            int configuredCapacity = profile.GridRows * profile.GridColumns;
            
            if (populatedCount >= configuredCapacity)
                return profile.GridColumns;
            
            return CalculateCondensedColumns(populatedCount, profile.GridRows, profile.GridColumns);
        }
    }
    
    private int CalculateCondensedRows(int itemCount, int configuredRows, int configuredColumns)
    {
        // Maintain aspect ratio while condensing
        double aspectRatio = (double)configuredRows / configuredColumns;
        
        // Try to maintain similar aspect ratio
        int rows = (int)Math.Ceiling(Math.Sqrt(itemCount * aspectRatio));
        int columns = (int)Math.Ceiling((double)itemCount / rows);
        
        // Ensure we don't exceed configured dimensions
        rows = Math.Min(rows, configuredRows);
        
        return rows;
    }
    
    private int CalculateCondensedColumns(int itemCount, int configuredRows, int configuredColumns)
    {
        int rows = CalculateCondensedRows(itemCount, configuredRows, configuredColumns);
        return (int)Math.Ceiling((double)itemCount / rows);
    }
    
    public ObservableCollection<GridPositionItem> PopulatedPositionItems
    {
        get
        {
            var items = new ObservableCollection<GridPositionItem>();
            
            if (Settings?.ActiveProfile == null) return items;
            
            foreach (int position in PopulatedPositions)
            {
                bool isSelected = position == CurrentPosition;
                
                items.Add(new GridPositionItem
                {
                    PositionNumber = $"#{position + 1}",
                    Label = Settings.ActiveProfile.TextLabels[position],
                    IsSelected = isSelected,
                    TextBrush = isSelected ? SelectedTextBrush : NonSelectedTextBrush,
                    BorderBrush = isSelected ? SelectedBorderBrush : NonSelectedBorderBrush,
                    BackgroundBrush = isSelected ? SelectedBackgroundBrush : Brushes.Transparent,
                    FontSize = Settings.FontSize
                });
            }
            
            return items;
        }
    }
}

public class GridPositionItem
{
    public string PositionNumber { get; set; }
    public string Label { get; set; }
    public bool IsSelected { get; set; }
    public Brush TextBrush { get; set; }
    public Brush BorderBrush { get; set; }
    public Brush BackgroundBrush { get; set; }
    public double FontSize { get; set; }
}
```


### 6. InputService (ENHANCED for Variable Position Count)

**Purpose**: Detect button presses for variable position counts (2-20 positions).

**Enhanced Logic**:
```csharp
public class InputService : IDisposable
{
    private const int BASE_BUTTON_INDEX = 57; // DirectInput 0-indexed (Button 58 in 1-indexed)
    private int _maxButtonIndex = 64; // Default for 8 positions (57-64)
    
    private Profile _activeProfile;
    
    public void SetActiveProfile(Profile profile)
    {
        _activeProfile = profile;
        _maxButtonIndex = BASE_BUTTON_INDEX + profile.PositionCount - 1;
        
        LogService.Info($"InputService configured for {profile.PositionCount} positions " +
                       $"(buttons {BASE_BUTTON_INDEX}-{_maxButtonIndex})");
    }
    
    private void PollDevice()
    {
        if (_device == null || _activeProfile == null) return;
        
        try
        {
            _device.Poll();
            var state = _device.GetCurrentState();
            
            // Check buttons in the configured range
            for (int i = BASE_BUTTON_INDEX; i <= _maxButtonIndex; i++)
            {
                if (state.Buttons[i])
                {
                    int position = i - BASE_BUTTON_INDEX;
                    
                    if (position != _lastPosition)
                    {
                        _lastPosition = position;
                        RotaryPositionChanged?.Invoke(this, position);
                        LogService.Debug($"Position changed to {position + 1}");
                    }
                    
                    return; // Only one button should be pressed at a time
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"Error polling device: {ex.Message}");
        }
    }
    
    // Test mode enhancement
    private void OnTestModeKeyDown(object sender, KeyEventArgs e)
    {
        if (!_testMode || _activeProfile == null) return;
        
        int maxPosition = _activeProfile.PositionCount - 1;
        
        switch (e.Key)
        {
            case Key.Left:
                _testModePosition--;
                if (_testModePosition < 0)
                    _testModePosition = maxPosition;
                RaiseRotaryPositionChanged(_testModePosition);
                e.Handled = true;
                break;
                
            case Key.Right:
                _testModePosition++;
                if (_testModePosition > maxPosition)
                    _testModePosition = 0;
                RaiseRotaryPositionChanged(_testModePosition);
                e.Handled = true;
                break;
        }
    }
}
```

### 7. MainWindow (ENHANCED)

**Purpose**: Coordinate position changes with animations and layout updates.

**Enhanced Position Change Handler**:
```csharp
public partial class MainWindow : Window
{
    private int _previousPosition = -1;
    
    private void OnRotaryPositionChanged(object sender, int newPosition)
    {
        Dispatcher.Invoke(() =>
        {
            // Update ViewModel
            _viewModel.CurrentPosition = newPosition;
            
            // Trigger animation for Single layout
            if (_viewModel.Settings.ActiveProfile.Layout == LayoutType.Single)
            {
                var singleLayout = FindVisualChild<SingleTextLayout>(this);
                singleLayout?.OnPositionChanged(newPosition, _previousPosition);
            }
            
            // Update other layouts (they handle their own rendering)
            _viewModel.RefreshDisplay();
            
            _previousPosition = newPosition;
        });
    }
    
    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T typedChild)
                return typedChild;
            
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
}
```

## Data Models

### Profile Data Model (Complete)

```csharp
public class Profile
{
    public string Name { get; set; }
    public string DeviceGuid { get; set; }
    public List<string> TextLabels { get; set; } = new List<string>();
    public LayoutType Layout { get; set; } = LayoutType.Single;
    
    // v0.5.0 additions
    public int PositionCount { get; set; } = 8;
    public int GridRows { get; set; } = 2;
    public int GridColumns { get; set; } = 4;
    
    // Validation
    public bool IsValidGridConfiguration()
    {
        return GridRows * GridColumns >= PositionCount;
    }
    
    public void AdjustGridToDefault()
    {
        GridRows = 2;
        GridColumns = (int)Math.Ceiling(PositionCount / 2.0);
    }
    
    // Ensure TextLabels list matches PositionCount
    public void NormalizeTextLabels()
    {
        while (TextLabels.Count < PositionCount)
        {
            TextLabels.Add("");
        }
        
        if (TextLabels.Count > PositionCount)
        {
            TextLabels.RemoveRange(PositionCount, TextLabels.Count - PositionCount);
        }
    }
}

public enum LayoutType
{
    Single,
    Vertical,
    Horizontal,
    Grid
}
```

### Settings Persistence

```csharp
public class SettingsService
{
    private const string SETTINGS_FILE = "settings.json";
    
    public Settings LoadSettings()
    {
        try
        {
            if (File.Exists(SETTINGS_FILE))
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                
                // Normalize all profiles
                foreach (var profile in settings.Profiles)
                {
                    profile.NormalizeTextLabels();
                    
                    // Validate grid configuration
                    if (!profile.IsValidGridConfiguration())
                    {
                        profile.AdjustGridToDefault();
                    }
                }
                
                return settings;
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"Error loading settings: {ex.Message}");
        }
        
        return CreateDefaultSettings();
    }
    
    public void SaveSettings(Settings settings)
    {
        try
        {
            // Normalize before saving
            foreach (var profile in settings.Profiles)
            {
                profile.NormalizeTextLabels();
            }
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SETTINGS_FILE, json);
        }
        catch (Exception ex)
        {
            LogService.Error($"Error saving settings: {ex.Message}");
        }
    }
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Forward Animation Direction

*For any* position transition in Single layout where the new position is greater than the old position (or wraps from max to 0), the current text should animate upward with negative rotation and the new text should appear from below with positive rotation.

**Validates: Requirements 1.1, 1.2**

### Property 2: Backward Animation Direction

*For any* position transition in Single layout where the new position is less than the old position (or wraps from 0 to max), the current text should animate downward with positive rotation and the new text should appear from above with negative rotation.

**Validates: Requirements 1.3, 1.4**

### Property 3: Animation Duration Bounds

*For any* position transition animation in Single layout, the animation duration should be between 200 and 300 milliseconds.

**Validates: Requirements 1.5**

### Property 4: Animation Interruption

*For any* sequence of rapid position changes in Single layout, starting a new animation should immediately stop any currently running animation and begin the new animation.

**Validates: Requirements 1.6**

### Property 5: Empty Position Animation

*For any* transition to an empty position in Single layout, the transition animation should still occur before displaying the last populated position text.

**Validates: Requirements 1.7**

### Property 6: Grid Dimension Validation

*For any* grid configuration with rows R and columns C and position count N, the validation should pass if and only if R × C ≥ N.

**Validates: Requirements 2.4, 6.1**

### Property 7: Grid Dimension Persistence

*For any* profile with grid dimensions, saving and loading the profile should preserve the exact grid dimensions.

**Validates: Requirements 2.7**

### Property 8: Empty Position Filtering

*For any* profile configuration in Grid layout with M populated positions out of N total positions, the display should render exactly M grid cells.

**Validates: Requirements 3.1**

### Property 9: Position Number Preservation

*For any* set of populated positions displayed in Grid layout, the position numbers should remain unchanged regardless of how many empty positions are filtered out.

**Validates: Requirements 3.4**

### Property 10: Grid Aspect Ratio Preservation

*For any* grid configuration with aspect ratio A (rows/columns) and fewer populated positions than capacity, the condensed grid should maintain an aspect ratio close to A (within ±0.5).

**Validates: Requirements 3.3**

### Property 11: Position Count Range Support

*For any* position count value N where 2 ≤ N ≤ 20, the system should accept and correctly configure the profile with N positions.

**Validates: Requirements 4.2**

### Property 12: Position Count Increase Preservation

*For any* profile with position count N and text labels L, increasing the position count to M (M > N) should preserve all existing labels L[0..N-1] and add M-N empty labels.

**Validates: Requirements 4.5**

### Property 13: Position Count Decrease Removal

*For any* profile with position count N and text labels L, decreasing the position count to M (M < N) should preserve labels L[0..M-1] and remove labels L[M..N-1].

**Validates: Requirements 4.7**

### Property 14: Position Count Persistence

*For any* profile with position count N, saving and loading the profile should preserve the exact position count value.

**Validates: Requirements 4.8**

### Property 15: Input Button Range

*For any* profile with position count N, the input service should monitor DirectInput buttons in the range [57, 57+N-1] (0-indexed).

**Validates: Requirements 5.1, 5.2**

### Property 16: Out-of-Range Input Filtering

*For any* profile with position count N, button presses for buttons ≥ 57+N should be ignored and not trigger position changes.

**Validates: Requirements 5.3**

### Property 17: In-Range Input Handling

*For any* profile with position count N and button press for button B where 57 ≤ B < 57+N, the display should update to show position B-57.

**Validates: Requirements 5.4**

### Property 18: Position Wrap-Around

*For any* profile with position count N, transitioning from position N-1 to position 0 (or vice versa) should be handled as a valid wrap-around transition.

**Validates: Requirements 5.5**

### Property 19: Grid Auto-Adjustment

*For any* profile where position count is changed to N and current grid dimensions cannot accommodate N positions, the grid dimensions should automatically adjust to 2 × ⌈N/2⌉.

**Validates: Requirements 6.3**

### Property 20: Grid Suggestion Validity

*For any* position count N, all suggested grid dimensions should satisfy rows × columns ≥ N.

**Validates: Requirements 6.5**

### Property 21: Animation Lag Prevention

*For any* sequence of position changes where the animation queue would cause display lag > 100ms, the system should skip or complete animations to maintain synchronization.

**Validates: Requirements 9.4**

### Property 22: Test Mode Position Range

*For any* profile with position count N in test mode, arrow key navigation should cycle through positions 0 to N-1 with wrap-around.

**Validates: Requirements 10.1, 10.2, 10.3**

### Property 23: Test Mode Grid Support

*For any* grid configuration in test mode, all grid dimensions should render correctly and respond to keyboard input.

**Validates: Requirements 10.5**


## Error Handling

### Animation Errors

1. **Animation Interruption Failure**: If an animation cannot be cleanly interrupted, log the error and reset to a stable state (no animation, current position displayed)
2. **Animation Resource Exhaustion**: If animation resources (storyboards, transforms) cannot be created, fall back to instant position changes without animation
3. **Animation Timing Errors**: If animation duration cannot be measured accurately, use default 250ms duration

### Grid Configuration Errors

1. **Invalid Grid Dimensions**: If user attempts to set rows or columns < 1 or > 10, display error message and revert to previous valid configuration
2. **Insufficient Grid Capacity**: If rows × columns < position count, automatically adjust to default 2×N configuration and notify user
3. **Grid Calculation Errors**: If condensed grid dimensions cannot be calculated, fall back to 2×N configuration

### Position Count Errors

1. **Out of Range Position Count**: If user attempts to set position count < 2 or > 20, display error message and revert to previous value
2. **Position Count Decrease with Data Loss**: If decreasing position count would remove populated positions, show confirmation dialog with list of positions to be removed
3. **Text Label Array Mismatch**: If text labels array length doesn't match position count, automatically normalize by adding empty labels or truncating

### Input Service Errors

1. **Button Range Configuration Error**: If button range cannot be configured for the position count, log error and fall back to default 8-position configuration
2. **Out of Range Button Press**: If button press is detected outside configured range, log warning and ignore the input
3. **Rapid Position Changes**: If position changes occur faster than animation can handle, queue changes and process them with lag prevention logic

### Settings Persistence Errors

1. **Invalid Profile Data**: If loaded profile has invalid grid dimensions or position count, automatically correct to valid defaults and log warning
2. **Missing Profile Fields**: If profile is missing new v0.5.0 fields (PositionCount, GridRows, GridColumns), initialize with defaults (8, 2, 4)
3. **Serialization Errors**: If profile cannot be saved, log error and keep current in-memory state, notify user of save failure

## Testing Strategy

### Dual Testing Approach

This feature will use both unit tests and property-based tests:

- **Unit tests**: Verify specific examples (default values, UI element presence, edge cases like 2 positions or 20 positions), integration points, and error conditions
- **Property tests**: Verify universal properties across all input combinations (animation directions, grid calculations, position count changes, input handling)

Both types of tests are complementary and necessary for comprehensive coverage. Unit tests catch concrete bugs in specific scenarios, while property tests verify general correctness across the input space.

### Property-Based Testing

We will use **MSTest** with **FsCheck** for property-based testing in C#. Each property test will:
- Run a minimum of 100 iterations
- Generate random position counts (2-20)
- Generate random grid dimensions (1-10 rows/columns)
- Generate random position sequences and transitions
- Generate random empty/populated position patterns
- Verify the correctness properties hold for all generated inputs

### Test Configuration

Each property test will be tagged with a comment referencing the design property:
```csharp
// Feature: v0.5.0-enhancements, Property 1: Forward Animation Direction
[TestMethod]
public void Property_ForwardAnimationDirection()
{
    Prop.ForAll<int, int>(
        Arb.From(Gen.Choose(0, 19)),  // oldPosition
        Arb.From(Gen.Choose(0, 19)),  // newPosition
        (oldPos, newPos) =>
        {
            // Arrange: Create profile and layout
            var profile = new Profile { PositionCount = 20 };
            var layout = new SingleTextLayout();
            
            // Act: Trigger transition
            bool isForward = layout.IsForwardTransition(oldPos, newPos, profile.PositionCount);
            
            // Assert: Verify direction logic
            if (oldPos == profile.PositionCount - 1 && newPos == 0)
                return isForward == true;  // Wrap forward
            if (oldPos == 0 && newPos == profile.PositionCount - 1)
                return isForward == false; // Wrap backward
            return isForward == (newPos > oldPos);
        }).QuickCheckThrowOnFailure();
}
```

### Unit Test Coverage

Unit tests will cover:

**Animation Tests**:
1. Forward transition animates upward (specific example: position 2 → 3)
2. Backward transition animates downward (specific example: position 3 → 2)
3. Animation duration is within 200-300ms range
4. Animation interruption works correctly
5. No animation on application startup
6. No animation on profile switch
7. Animation occurs for empty position transitions

**Grid Configuration Tests**:
1. Default grid dimensions are 2×4 for 8 positions
2. Grid validation accepts valid configurations (2×4 for 8 positions)
3. Grid validation rejects invalid configurations (2×2 for 8 positions)
4. Grid auto-adjusts when position count increases beyond capacity
5. Suggested dimensions are all valid for given position count
6. Grid preview displays correct number of cells

**Position Count Tests**:
1. Default position count is 8
2. Position count can be set to 2 (minimum)
3. Position count can be set to 20 (maximum)
4. Position count < 2 is rejected
5. Position count > 20 is rejected
6. Increasing position count preserves existing labels
7. Decreasing position count shows confirmation dialog
8. Text label array is normalized to match position count

**Input Service Tests**:
1. Button range is correctly configured for 8 positions (57-64)
2. Button range is correctly configured for 20 positions (57-76)
3. Button press within range triggers position change
4. Button press outside range is ignored
5. Wrap-around from position 7 to 0 works (8 positions)
6. Wrap-around from position 19 to 0 works (20 positions)

**Settings Persistence Tests**:
1. Profile with position count 12 saves and loads correctly
2. Profile with grid dimensions 3×4 saves and loads correctly
3. Profile with missing v0.5.0 fields loads with defaults
4. Profile with invalid grid dimensions is auto-corrected on load

**Test Mode Tests**:
1. Test mode with 12 positions cycles through 0-11
2. Right arrow at position 11 wraps to 0 (12 positions)
3. Left arrow at position 0 wraps to 11 (12 positions)
4. Test mode indicator shows current position number

### Integration Testing

Manual testing will verify:
1. Animation visual appearance and smoothness
2. Animation direction matches position change direction
3. Grid layout renders correctly for various dimensions (2×4, 3×3, 4×2, etc.)
4. Grid condensing maintains visual structure
5. Settings UI is intuitive and responsive
6. Position count changes update UI immediately
7. Grid dimension changes update preview immediately
8. Suggested dimensions are helpful and correct
9. Confirmation dialogs appear when appropriate
10. All layouts work correctly with variable position counts (2-20)
11. Test mode works correctly with all position counts
12. Performance is acceptable (no stuttering or lag)

### Performance Testing

While not part of automated tests, manual performance testing should verify:
1. Animations maintain 60 FPS on target hardware
2. No CPU/GPU spikes during animations
3. Rapid position changes don't cause lag
4. Grid rendering is fast for all supported dimensions
5. Settings UI is responsive when changing position count

