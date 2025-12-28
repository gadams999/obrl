using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;

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

        private StackPanel? _settingsPanel;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            Loaded += SettingsWindow_Loaded;
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

            // --- 3. Dynamic Text Labels ---
            AddLabel($"Text Labels ({wheelDef.TextFieldCount} positions for {wheelDef.DeviceName})");
            
            // Ensure lists are synced
            while (currentProfile.TextLabels.Count < wheelDef.TextFieldCount) currentProfile.TextLabels.Add("");
            // If we have too many (switched from big wheel to small), we just ignore the extras in UI

            _labelTextBoxes = new System.Windows.Controls.TextBox[wheelDef.TextFieldCount];
            for (int i = 0; i < wheelDef.TextFieldCount; i++)
            {
                var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                var label = new TextBlock { Text = $"Position {i + 1}:", Width = 80, VerticalAlignment = VerticalAlignment.Center };
                var val = (i < currentProfile.TextLabels.Count) ? currentProfile.TextLabels[i] : "";
                var textBox = new System.Windows.Controls.TextBox { Width = 150, Text = val };
                _labelTextBoxes[i] = textBox;
                panel.Children.Add(label);
                panel.Children.Add(textBox);
                _settingsPanel.Children.Add(panel);
            }

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
            _fontSizeSlider = AddSlider(20, 100, 10, _settings.FontSize);

            AddLabel("Item Spacing (pixels)");
            _spacingSlider = AddSlider(2, 20, 2, _settings.ItemSpacing);
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
