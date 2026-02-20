using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;

namespace WheelOverlay
{
    public partial class SettingsWindow : Window
    {
        private AppSettings? _settings;
        public event EventHandler? SettingsChanged;

        // UI Controls
        private System.Windows.Controls.ComboBox? _profileComboBox;
        private System.Windows.Controls.ComboBox? _layoutComboBox;
        private System.Windows.Controls.ComboBox? _deviceComboBox;
        private Slider? _fontSizeSlider;
        private System.Windows.Controls.TextBox? _selectedColorTextBox;
        private System.Windows.Controls.TextBox? _nonSelectedColorTextBox;
        private Slider? _spacingSlider;
        private Slider? _opacitySlider;
        private System.Windows.Controls.TextBox[]? _labelTextBoxes;
        
        // New v0.5.0 controls
        private System.Windows.Controls.ComboBox? _positionCountComboBox;
        private System.Windows.Controls.ComboBox? _gridRowsComboBox;
        private System.Windows.Controls.ComboBox? _gridColumnsComboBox;
        private ItemsControl? _gridPreviewControl;
        private TextBlock? _gridCapacityText;
        private ItemsControl? _suggestedDimensionsControl;
        
        // Conditional visibility controls (overlay-visibility-and-ui-improvements)
        private TextBlock? _targetExeDisplay;
        private System.Windows.Controls.Button? _browseButton;
        private System.Windows.Controls.Button? _clearButton;

        private StackPanel? _settingsPanel;
        private SettingsViewModel? _viewModel;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _viewModel = new SettingsViewModel();
            Loaded += SettingsWindow_Loaded;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            }
        }

        private Profile? GetCurrentProfile()
        {
            if (_settings == null) return null;
            
            var profile = _settings.Profiles.FirstOrDefault(p => p.Id == _settings.SelectedProfileId);
            if (profile == null)
            {
                // Fallback / Self-healing
                var validProfiles = GetProfilesForCurrentDevice();
                if (!validProfiles.Any())
                {
                    profile = CreateNewProfile("Default", _settings.SelectedDeviceName);
                }
                else
                {
                    profile = validProfiles.First();
                }
                _settings.SelectedProfileId = profile.Id;
            }
            return profile;
        }

        private List<Profile> GetProfilesForCurrentDevice()
        {
            if (_settings == null) return new List<Profile>();
            
            return _settings.Profiles
                .Where(p => p.DeviceName == _settings.SelectedDeviceName)
                .ToList();
        }

        private Profile CreateNewProfile(string name, string deviceName)
        {
            if (_settings == null) throw new InvalidOperationException("Settings not initialized");
            
            var wheelDef = WheelDefinition.SupportedWheels.FirstOrDefault(w => w.DeviceName == deviceName) 
                           ?? WheelDefinition.SupportedWheels[0];

            var profile = new Profile
            {
                Name = name,
                DeviceName = deviceName,
                TextLabels = Enumerable.Repeat("DASH", wheelDef.TextFieldCount).ToList() // Default filler
            };
            
            // If it's the Alpha, give it the nice defaults
            if (deviceName == "BavarianSimTec Alpha")
            {
               profile.TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" };
            }

            _settings.Profiles.Add(profile);
            return profile;
        }



        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Find SettingsPanel manually since XAML binding isn't working
            _settingsPanel = FindName("SettingsPanel") as StackPanel;
            
            if (_settingsPanel == null)
            {
                System.Windows.MessageBox.Show("Could not find SettingsPanel control!", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            ShowDisplaySettings();
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is System.Windows.Controls.ListBoxItem selectedItem && selectedItem.Tag != null)
            {
                // Save current category values before switching
                SaveCurrentCategoryValues();
                
                string category = selectedItem.Tag.ToString()!;
                switch (category)
                {
                    case "Display":
                        ShowDisplaySettings();
                        break;
                    case "Appearance":
                        ShowAppearanceSettings();
                        break;
                    case "Advanced":
                        ShowAdvancedSettings();
                        break;
                }
            }
        }

        private void SaveCurrentCategoryValues()
        {
            if (_settings == null) return;
            
            var profile = GetCurrentProfile();
            if (profile == null) return;

            // Save layout
            if (_layoutComboBox?.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                profile.Layout = Enum.Parse<DisplayLayout>(selectedItem.Tag.ToString()!);
            }

            // Save device selection
            if (_deviceComboBox?.SelectedItem is string selectedDevice)
            {
                _settings.SelectedDeviceName = selectedDevice;
            }

            // Save position count
            if (_positionCountComboBox?.SelectedItem is int positionCount)
            {
                profile.PositionCount = positionCount;
            }

            // Save grid dimensions
            if (_gridRowsComboBox?.SelectedItem is int gridRows)
            {
                profile.GridRows = gridRows;
            }
            if (_gridColumnsComboBox?.SelectedItem is int gridColumns)
            {
                profile.GridColumns = gridColumns;
            }

            // Save text labels
            if (_labelTextBoxes != null)
            {
                for (int i = 0; i < _labelTextBoxes.Length; i++)
                {
                    if (_labelTextBoxes[i] != null)
                    {
                        // Ensure list is big enough
                        while (profile.TextLabels.Count <= i) profile.TextLabels.Add("");
                        
                        profile.TextLabels[i] = _labelTextBoxes[i].Text;
                    }
                }
            }

            // Save other values
            if (_fontSizeSlider != null) _settings.FontSize = (int)_fontSizeSlider.Value;
            if (_selectedColorTextBox != null) _settings.SelectedTextColor = _selectedColorTextBox.Text;
            if (_nonSelectedColorTextBox != null) _settings.NonSelectedTextColor = _nonSelectedColorTextBox.Text;
            if (_spacingSlider != null) _settings.ItemSpacing = (int)_spacingSlider.Value;
            if (_opacitySlider != null) _settings.MoveOverlayOpacity = (int)_opacitySlider.Value;
        }

        private void ShowDisplaySettings()
        {
            if (_settingsPanel == null || _settings == null) return;
            _settingsPanel.Children.Clear();

            var currentProfile = GetCurrentProfile();
            if (currentProfile == null) return;

            var wheelDef = WheelDefinition.SupportedWheels.FirstOrDefault(w => w.DeviceName == currentProfile.DeviceName)
                           ?? WheelDefinition.SupportedWheels[0];

            // Title
            var title = new TextBlock { Text = "Display & Device Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            _settingsPanel.Children.Add(title);

            // --- 1. Device Selection ---
            AddLabel("Wheel Device");
            _deviceComboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            
            foreach (var wheel in WheelDefinition.SupportedWheels)
            {
                _deviceComboBox.Items.Add(wheel.DeviceName);
            }
            
            _deviceComboBox.SelectedItem = _settings.SelectedDeviceName;
            
            _deviceComboBox.SelectionChanged += (s, e) =>
            {
                if (_deviceComboBox.SelectedItem is string newDeviceName && newDeviceName != _settings.SelectedDeviceName)
                {
                    _settings.SelectedDeviceName = newDeviceName;
                    
                    // Force profile switch to a valid one for the new device
                    _settings.SelectedProfileId = Guid.Empty; 
                    GetCurrentProfile(); // Self-healing will create/select a profile
                    
                    // Rebuild UI to reflect new device and profile
                    ShowDisplaySettings();
                }
            };
            
            _settingsPanel.Children.Add(_deviceComboBox);

            // --- 2. Profile Section ---
            AddLabel("Profile (for this device)");
            var profilePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            
            _profileComboBox = new System.Windows.Controls.ComboBox { Width = 200, Margin = new Thickness(0, 0, 10, 0) };
            var profiles = GetProfilesForCurrentDevice();
            foreach (var p in profiles)
            {
                var item = new ComboBoxItem { Content = p.Name, Tag = p.Id };
                _profileComboBox.Items.Add(item);
                if (p.Id == currentProfile.Id) _profileComboBox.SelectedItem = item;
            }
            
            // Handle Profile Switching
            _profileComboBox.SelectionChanged += (s, e) =>
            {
                if (_profileComboBox.SelectedItem is ComboBoxItem selected && (Guid)selected.Tag != _settings.SelectedProfileId)
                {
                    SaveCurrentCategoryValues(); // Save old profile first
                    _settings.SelectedProfileId = (Guid)selected.Tag;
                    ShowDisplaySettings(); // Rebuild UI
                }
            };

            var newBtn = new System.Windows.Controls.Button { Content = "New", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            newBtn.Click += (s, e) => 
            {
                SaveCurrentCategoryValues();
                var newProfile = CreateNewProfile($"New Profile", currentProfile.DeviceName);
                // Copy values
                newProfile.Layout = currentProfile.Layout;
                newProfile.TextLabels = new List<string>(currentProfile.TextLabels);
                _settings.SelectedProfileId = newProfile.Id;
                ShowDisplaySettings();
            };

            var renameBtn = new System.Windows.Controls.Button { Content = "Rename", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            renameBtn.Click += (s, e) =>
            {
                var inputDialog = new System.Windows.Window
                {
                    Title = "Rename Profile",
                    Width = 350,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var panel = new StackPanel { Margin = new Thickness(10) };
                panel.Children.Add(new TextBlock { Text = "Enter new profile name:", Margin = new Thickness(0, 0, 0, 10) });
                
                var textBox = new System.Windows.Controls.TextBox { Text = currentProfile.Name, Margin = new Thickness(0, 0, 0, 10) };
                panel.Children.Add(textBox);

                var buttonPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
                var okBtn = new System.Windows.Controls.Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 5, 0) };
                okBtn.Click += (sender, args) => { inputDialog.DialogResult = true; inputDialog.Close(); };
                var cancelBtn = new System.Windows.Controls.Button { Content = "Cancel", Width = 70 };
                cancelBtn.Click += (sender, args) => { inputDialog.DialogResult = false; inputDialog.Close(); };
                
                buttonPanel.Children.Add(okBtn);
                buttonPanel.Children.Add(cancelBtn);
                panel.Children.Add(buttonPanel);

                inputDialog.Content = panel;

                if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    currentProfile.Name = textBox.Text.Trim();
                    ShowDisplaySettings(); // Refresh
                }
            };

            var delBtn = new System.Windows.Controls.Button { Content = "Delete", Width = 60 };
            delBtn.Click += (s, e) =>
            {
                if (profiles.Count <= 1)
                {
                    System.Windows.MessageBox.Show("Cannot delete the only profile.", "Warning");
                    return;
                }
                
                if (System.Windows.MessageBox.Show($"Delete profile '{currentProfile.Name}'?", "Confirm", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                {
                    _settings.Profiles.Remove(currentProfile);
                    _settings.SelectedProfileId = _settings.Profiles.First(p => p.DeviceName == currentProfile.DeviceName).Id;
                    ShowDisplaySettings();
                }
            };

            profilePanel.Children.Add(_profileComboBox);
            profilePanel.Children.Add(newBtn);
            profilePanel.Children.Add(renameBtn);
            profilePanel.Children.Add(delBtn);
            _settingsPanel.Children.Add(profilePanel);

            // Set ViewModel's selected profile (currentProfile is guaranteed non-null due to early return above)
            _viewModel!.SelectedProfile = currentProfile!;
            
            // --- Conditional Visibility Section (overlay-visibility-and-ui-improvements) ---
            AddLabel("Conditional Visibility");
            var visibilityHelp = new TextBlock 
            { 
                Text = "Show overlay only when this application is running:", 
                FontSize = 11, 
                Foreground = System.Windows.Media.Brushes.Gray, 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            _settingsPanel.Children.Add(visibilityHelp);
            
            var filePanel = new StackPanel 
            { 
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            _targetExeDisplay = new TextBlock 
            { 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                MinWidth = 200
            };
            UpdateTargetDisplay(currentProfile.TargetExecutablePath);
            
            _browseButton = new System.Windows.Controls.Button 
            { 
                Content = "Browse...",
                Width = 80,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _browseButton.Click += OnBrowseClick;
            
            _clearButton = new System.Windows.Controls.Button 
            { 
                Content = "Clear",
                Width = 60
            };
            _clearButton.Click += OnClearClick;
            _clearButton.IsEnabled = !string.IsNullOrEmpty(currentProfile.TargetExecutablePath);
            
            filePanel.Children.Add(_targetExeDisplay);
            filePanel.Children.Add(_browseButton);
            filePanel.Children.Add(_clearButton);
            _settingsPanel.Children.Add(filePanel);
            
            // --- NEW v0.5.0: Position Count Configuration ---
            AddLabel("Number of Positions");
            _positionCountComboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 0, 0, 5) };
            foreach (int count in _viewModel.AvailablePositionCounts)
            {
                _positionCountComboBox.Items.Add(count);
            }
            _positionCountComboBox.SelectedItem = _viewModel.SelectedProfile.PositionCount;
            _positionCountComboBox.SelectionChanged += PositionCount_Changed;
            _settingsPanel.Children.Add(_positionCountComboBox);
            
            var positionCountHelp = new TextBlock 
            { 
                Text = "Configure how many positions your wheel has (2-20)", 
                FontSize = 10, 
                Foreground = System.Windows.Media.Brushes.Gray, 
                Margin = new Thickness(0, 2, 0, 10) 
            };
            _settingsPanel.Children.Add(positionCountHelp);

            // --- NEW v0.5.0: Grid Dimensions Configuration ---
            AddLabel("Grid Layout Dimensions");
            var gridDimensionsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            
            var rowsLabel = new TextBlock { Text = "Rows:", Width = 50, VerticalAlignment = VerticalAlignment.Center };
            _gridRowsComboBox = new System.Windows.Controls.ComboBox { Width = 60, Margin = new Thickness(0, 0, 10, 0) };
            foreach (int row in _viewModel.AvailableRows)
            {
                _gridRowsComboBox.Items.Add(row);
            }
            _gridRowsComboBox.SelectedItem = _viewModel.SelectedProfile.GridRows;
            _gridRowsComboBox.SelectionChanged += GridDimensions_Changed;
            
            var timesLabel = new TextBlock { Text = "×", Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            
            var columnsLabel = new TextBlock { Text = "Columns:", Width = 70, VerticalAlignment = VerticalAlignment.Center };
            _gridColumnsComboBox = new System.Windows.Controls.ComboBox { Width = 60 };
            foreach (int col in _viewModel.AvailableColumns)
            {
                _gridColumnsComboBox.Items.Add(col);
            }
            _gridColumnsComboBox.SelectedItem = _viewModel.SelectedProfile.GridColumns;
            _gridColumnsComboBox.SelectionChanged += GridDimensions_Changed;
            
            gridDimensionsPanel.Children.Add(rowsLabel);
            gridDimensionsPanel.Children.Add(_gridRowsComboBox);
            gridDimensionsPanel.Children.Add(timesLabel);
            gridDimensionsPanel.Children.Add(columnsLabel);
            gridDimensionsPanel.Children.Add(_gridColumnsComboBox);
            _settingsPanel.Children.Add(gridDimensionsPanel);

            // --- NEW v0.5.0: Grid Preview ---
            var gridPreviewBorder = new Border 
            { 
                BorderBrush = System.Windows.Media.Brushes.Gray, 
                BorderThickness = new Thickness(1), 
                Padding = new Thickness(10), 
                Margin = new Thickness(0, 5, 0, 10) 
            };
            
            var gridPreviewPanel = new StackPanel();
            
            _gridCapacityText = new TextBlock 
            { 
                Text = _viewModel.GridCapacityDisplay, 
                FontSize = 11, 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            gridPreviewPanel.Children.Add(_gridCapacityText);
            
            _gridPreviewControl = new ItemsControl { Margin = new Thickness(0, 5, 0, 0) };
            _gridPreviewControl.ItemsSource = _viewModel.GridPreviewCells;
            
            var gridPreviewTemplate = new ItemsPanelTemplate();
            var uniformGridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
            uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, _viewModel.SelectedProfile.GridRows);
            uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, _viewModel.SelectedProfile.GridColumns);
            gridPreviewTemplate.VisualTree = uniformGridFactory;
            _gridPreviewControl.ItemsPanel = gridPreviewTemplate;
            
            var cellTemplate = new DataTemplate();
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.LightGray);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.WidthProperty, 30.0);
            borderFactory.SetValue(Border.HeightProperty, 30.0);
            borderFactory.SetValue(Border.MarginProperty, new Thickness(2));
            
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
            
            borderFactory.AppendChild(textBlockFactory);
            cellTemplate.VisualTree = borderFactory;
            _gridPreviewControl.ItemTemplate = cellTemplate;
            
            gridPreviewPanel.Children.Add(_gridPreviewControl);
            gridPreviewBorder.Child = gridPreviewPanel;
            _settingsPanel.Children.Add(gridPreviewBorder);

            // --- NEW v0.5.0: Suggested Dimensions ---
            AddLabel("Suggested Dimensions");
            _suggestedDimensionsControl = new ItemsControl { Margin = new Thickness(0, 5, 0, 15) };
            _suggestedDimensionsControl.ItemsSource = _viewModel.SuggestedDimensions;
            
            var wrapPanelTemplate = new ItemsPanelTemplate();
            var wrapPanelFactory = new FrameworkElementFactory(typeof(WrapPanel));
            wrapPanelTemplate.VisualTree = wrapPanelFactory;
            _suggestedDimensionsControl.ItemsPanel = wrapPanelTemplate;
            
            var buttonTemplate = new DataTemplate();
            var buttonFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
            buttonFactory.SetBinding(System.Windows.Controls.Button.ContentProperty, new System.Windows.Data.Binding("DisplayText"));
            buttonFactory.SetValue(System.Windows.Controls.Button.MarginProperty, new Thickness(2));
            buttonFactory.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(SuggestedDimension_Click));
            buttonTemplate.VisualTree = buttonFactory;
            _suggestedDimensionsControl.ItemTemplate = buttonTemplate;
            
            _settingsPanel.Children.Add(_suggestedDimensionsControl);

            // --- 3. Dynamic Text Labels ---
            AddLabel($"Position Labels");
            
            // Ensure lists are synced with PositionCount
            while (currentProfile.TextLabels.Count < currentProfile.PositionCount) currentProfile.TextLabels.Add("");
            if (currentProfile.TextLabels.Count > currentProfile.PositionCount)
            {
                currentProfile.TextLabels.RemoveRange(currentProfile.PositionCount, currentProfile.TextLabels.Count - currentProfile.PositionCount);
            }

            // Add ScrollViewer for text labels
            var scrollViewer = new ScrollViewer 
            { 
                MaxHeight = 300, 
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 5, 0, 15)
            };
            
            var labelsPanel = new StackPanel();
            _labelTextBoxes = new System.Windows.Controls.TextBox[currentProfile.PositionCount];
            for (int i = 0; i < currentProfile.PositionCount; i++)
            {
                var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                var label = new TextBlock { Text = $"Position {i + 1}:", Width = 80, VerticalAlignment = VerticalAlignment.Center };
                var val = (i < currentProfile.TextLabels.Count) ? currentProfile.TextLabels[i] : "";
                var textBox = new System.Windows.Controls.TextBox { Width = 200, Text = val };
                _labelTextBoxes[i] = textBox;
                panel.Children.Add(label);
                panel.Children.Add(textBox);
                labelsPanel.Children.Add(panel);
            }
            
            scrollViewer.Content = labelsPanel;
            _settingsPanel.Children.Add(scrollViewer);

            // --- 4. Layout Section ---
            AddLabel("Display Layout");
            _layoutComboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            _layoutComboBox.Items.Add(CreateComboBoxItem("Single Text", "Single"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Vertical List", "Vertical"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Horizontal List", "Horizontal"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Grid", "Grid"));
            
            foreach (System.Windows.Controls.ComboBoxItem item in _layoutComboBox.Items)
            {
                if (item.Tag.ToString() == currentProfile.Layout.ToString())
                {
                    _layoutComboBox.SelectedItem = item;
                    break;
                }
            }
            _settingsPanel.Children.Add(_layoutComboBox);

            // --- 5. Font Size & Spacing ---
            AddLabel("Font Size");
            _fontSizeSlider = AddSlider(10, 80, 1, _settings.FontSize);

            AddLabel("Item Spacing (pixels)");
            _spacingSlider = AddSlider(0, 20, 1, _settings.ItemSpacing);
        }

        private void ShowAppearanceSettings()
        {
            if (_settingsPanel == null || _settings == null) return;
            _settingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Appearance Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            _settingsPanel.Children.Add(title);

            AddLabel("Selected Text Color");
            _selectedColorTextBox = AddColorPicker(_settings.SelectedTextColor);

            AddLabel("Non-Selected Text Color");
            _nonSelectedColorTextBox = AddColorPicker(_settings.NonSelectedTextColor);
        }

        private System.Windows.Controls.TextBox AddColorPicker(string initialColor)
        {
            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var textBox = new System.Windows.Controls.TextBox { Text = initialColor, Width = 100, VerticalAlignment = VerticalAlignment.Center };
            var pickButton = new System.Windows.Controls.Button { Content = "Pick", Width = 50, Margin = new Thickness(10, 0, 0, 0) };
            
            pickButton.Click += (s, e) =>
            {
                var dialog = new System.Windows.Forms.ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var color = dialog.Color;
                    textBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
            };

            panel.Children.Add(textBox);
            panel.Children.Add(pickButton);
            _settingsPanel?.Children.Add(panel);
            return textBox;
        }

        private void ShowAdvancedSettings()
        {
            if (_settingsPanel == null || _settings == null) return;
            _settingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Advanced Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            _settingsPanel.Children.Add(title);

            AddLabel("Move Overlay Opacity (%)");
            _opacitySlider = AddSlider(0, 100, 10, _settings.MoveOverlayOpacity);
        }

        private void AddLabel(string text)
        {
            if (_settingsPanel == null) return;
            var label = new TextBlock { Text = text, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _settingsPanel.Children.Add(label);
        }

        private Slider AddSlider(double min, double max, double tickFreq, double value)
        {
            if (_settingsPanel == null) return new Slider();
            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var slider = new Slider { Minimum = min, Maximum = max, Width = 200, TickFrequency = tickFreq, IsSnapToTickEnabled = true, Value = value };
            var valueText = new TextBlock { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            valueText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Value") { Source = slider });
            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            _settingsPanel.Children.Add(panel);
            return slider;
        }

        private System.Windows.Controls.ComboBoxItem CreateComboBoxItem(string content, string tag)
        {
            return new System.Windows.Controls.ComboBoxItem { Content = content, Tag = tag };
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;

            // Save current category values
            SaveCurrentCategoryValues();

            _settings.Save();
            
            // Notify that settings changed
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PositionCount_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_positionCountComboBox == null || _viewModel?.SelectedProfile == null) return;
            
            int newCount = (int)_positionCountComboBox.SelectedItem;
            int oldCount = _viewModel.SelectedProfile.TextLabels.Count;
            
            if (newCount < oldCount)
            {
                // Check if any positions being removed have text
                bool hasPopulatedPositions = _viewModel.SelectedProfile.TextLabels
                    .Skip(newCount)
                    .Any(label => !string.IsNullOrWhiteSpace(label));
                
                if (hasPopulatedPositions)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Reducing position count will remove labels for positions {newCount + 1}-{oldCount}. Continue?",
                        "Confirm Position Count Change",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (result == System.Windows.MessageBoxResult.No)
                    {
                        _positionCountComboBox.SelectedItem = oldCount;
                        return;
                    }
                }
            }
            
            _viewModel.UpdatePositionCount(newCount);
            ValidateAndAdjustGridDimensions();
            
            // Refresh the display to show updated text label inputs
            ShowDisplaySettings();
        }

        private void GridDimensions_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_gridRowsComboBox == null || _gridColumnsComboBox == null || _viewModel?.SelectedProfile == null) return;
            
            // Update profile with new dimensions
            _viewModel.SelectedProfile.GridRows = (int)_gridRowsComboBox.SelectedItem;
            _viewModel.SelectedProfile.GridColumns = (int)_gridColumnsComboBox.SelectedItem;
            
            ValidateGridDimensions();
        }

        private void ValidateAndAdjustGridDimensions()
        {
            var profile = _viewModel?.SelectedProfile;
            if (profile == null) return;
            
            if (!profile.IsValidGridConfiguration())
            {
                profile.AdjustGridToDefault();
                _viewModel?.RefreshGridPreview();
                
                // Update the combo boxes
                if (_gridRowsComboBox != null)
                    _gridRowsComboBox.SelectedItem = profile.GridRows;
                if (_gridColumnsComboBox != null)
                    _gridColumnsComboBox.SelectedItem = profile.GridColumns;
                
                System.Windows.MessageBox.Show(
                    $"Grid dimensions adjusted to {profile.GridRows}×{profile.GridColumns} to accommodate {profile.PositionCount} positions.",
                    "Grid Adjusted",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        private void ValidateGridDimensions()
        {
            var profile = _viewModel?.SelectedProfile;
            if (profile == null) return;
            
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            if (!result.IsValid)
            {
                System.Windows.MessageBox.Show(result.Message, "Invalid Grid Configuration", 
                          System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                profile.AdjustGridToDefault();
                
                // Update the combo boxes
                if (_gridRowsComboBox != null)
                    _gridRowsComboBox.SelectedItem = profile.GridRows;
                if (_gridColumnsComboBox != null)
                    _gridColumnsComboBox.SelectedItem = profile.GridColumns;
            }
            
            _viewModel?.RefreshGridPreview();
            
            // Update grid preview panel
            if (_gridPreviewControl != null)
            {
                var gridPreviewTemplate = new ItemsPanelTemplate();
                var uniformGridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
                uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, profile.GridRows);
                uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, profile.GridColumns);
                gridPreviewTemplate.VisualTree = uniformGridFactory;
                _gridPreviewControl.ItemsPanel = gridPreviewTemplate;
            }
            
            // Update capacity display
            if (_gridCapacityText != null && _viewModel != null)
            {
                _gridCapacityText.Text = _viewModel.GridCapacityDisplay;
            }
        }

        private void SuggestedDimension_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is SuggestedDimension dimension)
            {
                if (_viewModel?.SelectedProfile == null) return;
                
                _viewModel.SelectedProfile.GridRows = dimension.Rows;
                _viewModel.SelectedProfile.GridColumns = dimension.Columns;
                
                // Update the combo boxes
                if (_gridRowsComboBox != null)
                    _gridRowsComboBox.SelectedItem = dimension.Rows;
                if (_gridColumnsComboBox != null)
                    _gridColumnsComboBox.SelectedItem = dimension.Columns;
                
                _viewModel.RefreshGridPreview();
                
                // Update grid preview panel
                if (_gridPreviewControl != null)
                {
                    var gridPreviewTemplate = new ItemsPanelTemplate();
                    var uniformGridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
                    uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, dimension.Rows);
                    uniformGridFactory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, dimension.Columns);
                    gridPreviewTemplate.VisualTree = uniformGridFactory;
                    _gridPreviewControl.ItemsPanel = gridPreviewTemplate;
                }
                
                // Update capacity display
                if (_gridCapacityText != null)
                {
                    _gridCapacityText.Text = _viewModel.GridCapacityDisplay;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        // --- Conditional Visibility UI Methods (overlay-visibility-and-ui-improvements) ---
        
        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Target Application",
                    Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    
                    // Validate file exists
                    if (!System.IO.File.Exists(selectedPath))
                    {
                        Services.LogService.Info($"Selected file does not exist: {selectedPath}");
                        System.Windows.MessageBox.Show(
                            "The selected file does not exist. Please select a valid executable file.",
                            "File Not Found",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    // Validate file extension
                    var extension = System.IO.Path.GetExtension(selectedPath);
                    if (!string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Services.LogService.Info($"Selected file is not an executable: {selectedPath}");
                        System.Windows.MessageBox.Show(
                            "Please select an executable file (.exe).",
                            "Invalid File Type",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    if (_viewModel?.SelectedProfile != null)
                    {
                        _viewModel.SelectedProfile.TargetExecutablePath = selectedPath;
                        UpdateTargetDisplay(selectedPath);
                        Services.LogService.Info($"Target executable set to: {selectedPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error selecting target executable", ex);
                System.Windows.MessageBox.Show(
                    "An error occurred while selecting the file. Please try again.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.SelectedProfile != null)
                {
                    _viewModel.SelectedProfile.TargetExecutablePath = null;
                    UpdateTargetDisplay(null);
                    Services.LogService.Info("Target executable cleared - overlay will be always visible");
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error clearing target executable", ex);
                System.Windows.MessageBox.Show(
                    "An error occurred while clearing the selection. Please try again.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void UpdateTargetDisplay(string? path)
        {
            try
            {
                if (_targetExeDisplay == null) return;
                
                if (string.IsNullOrEmpty(path))
                {
                    _targetExeDisplay.Text = "(None - always visible)";
                    if (_clearButton != null)
                        _clearButton.IsEnabled = false;
                }
                else
                {
                    // Safely extract filename, handling invalid path characters
                    try
                    {
                        _targetExeDisplay.Text = System.IO.Path.GetFileName(path);
                    }
                    catch (ArgumentException)
                    {
                        // Path contains invalid characters, display as-is
                        _targetExeDisplay.Text = path;
                        Services.LogService.Info($"Path contains invalid characters: {path}");
                    }
                    
                    if (_clearButton != null)
                        _clearButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error updating target display", ex);
                // Fail gracefully - just show the path as-is
                if (_targetExeDisplay != null && !string.IsNullOrEmpty(path))
                {
                    _targetExeDisplay.Text = path;
                }
            }
        }
    }
}
