using System;
using System.Windows;
using System.Windows.Controls;
using WheelOverlay.Models;

namespace WheelOverlay
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        public event EventHandler? SettingsChanged;

        // UI Controls
        private ComboBox? _layoutComboBox;
        private ComboBox? _deviceComboBox;
        private Slider? _fontSizeSlider;
        private TextBox? _selectedColorTextBox;
        private TextBox? _nonSelectedColorTextBox;
        private Slider? _spacingSlider;
        private Slider? _opacitySlider;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Use Dispatcher to ensure UI is fully loaded
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowDisplaySettings();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is ListBoxItem selectedItem)
            {
                string category = selectedItem.Tag.ToString()!;
                switch (category)
                {
                    case "Display":
                        ShowDisplaySettings();
                        break;
                    case "Device":
                        ShowDeviceSettings();
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

        private void ShowDisplaySettings()
        {
            SettingsPanel.Children.Clear();

            // Title
            var title = new TextBlock { Text = "Display Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            SettingsPanel.Children.Add(title);

            // Layout
            AddLabel("Display Layout");
            _layoutComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            _layoutComboBox.Items.Add(CreateComboBoxItem("Single Text", "Single"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Vertical List", "Vertical"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Horizontal List", "Horizontal"));
            _layoutComboBox.Items.Add(CreateComboBoxItem("Grid", "Grid"));
            foreach (ComboBoxItem item in _layoutComboBox.Items)
            {
                if (item.Tag.ToString() == _settings.Layout.ToString())
                {
                    _layoutComboBox.SelectedItem = item;
                    break;
                }
            }
            SettingsPanel.Children.Add(_layoutComboBox);

            // Font Size
            AddLabel("Font Size");
            _fontSizeSlider = AddSlider(20, 100, 10, _settings.FontSize);

            // Item Spacing
            AddLabel("Item Spacing (pixels)");
            _spacingSlider = AddSlider(2, 20, 2, _settings.ItemSpacing);
        }

        private void ShowDeviceSettings()
        {
            SettingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Device Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            SettingsPanel.Children.Add(title);

            AddLabel("Device Selection");
            _deviceComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            foreach (var deviceName in AppSettings.DefaultDeviceNames)
            {
                _deviceComboBox.Items.Add(deviceName);
            }
            _deviceComboBox.SelectedItem = _settings.SelectedDeviceName;
            SettingsPanel.Children.Add(_deviceComboBox);
        }

        private void ShowAppearanceSettings()
        {
            SettingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Appearance Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            SettingsPanel.Children.Add(title);

            AddLabel("Selected Text Color");
            _selectedColorTextBox = new TextBox { Text = _settings.SelectedTextColor, Margin = new Thickness(0, 0, 0, 15) };
            SettingsPanel.Children.Add(_selectedColorTextBox);

            AddLabel("Non-Selected Text Color");
            _nonSelectedColorTextBox = new TextBox { Text = _settings.NonSelectedTextColor, Margin = new Thickness(0, 0, 0, 15) };
            SettingsPanel.Children.Add(_nonSelectedColorTextBox);
        }

        private void ShowAdvancedSettings()
        {
            SettingsPanel.Children.Clear();

            var title = new TextBlock { Text = "Advanced Settings", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
            SettingsPanel.Children.Add(title);

            AddLabel("Move Overlay Opacity (%)");
            _opacitySlider = AddSlider(0, 100, 10, _settings.MoveOverlayOpacity);
        }

        private void AddLabel(string text)
        {
            var label = new TextBlock { Text = text, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
            SettingsPanel.Children.Add(label);
        }

        private Slider AddSlider(double min, double max, double tickFreq, double value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var slider = new Slider { Minimum = min, Maximum = max, Width = 200, TickFrequency = tickFreq, IsSnapToTickEnabled = true, Value = value };
            var valueText = new TextBlock { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            valueText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Value") { Source = slider });
            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            SettingsPanel.Children.Add(panel);
            return slider;
        }

        private ComboBoxItem CreateComboBoxItem(string content, string tag)
        {
            return new ComboBoxItem { Content = content, Tag = tag };
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Save layout
            if (_layoutComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                _settings.Layout = Enum.Parse<DisplayLayout>(selectedItem.Tag.ToString()!);
            }

            // Save device selection
            if (_deviceComboBox?.SelectedItem is string selectedDevice)
            {
                _settings.SelectedDeviceName = selectedDevice;
            }

            // Save other values
            if (_fontSizeSlider != null) _settings.FontSize = (int)_fontSizeSlider.Value;
            if (_selectedColorTextBox != null) _settings.SelectedTextColor = _selectedColorTextBox.Text;
            if (_nonSelectedColorTextBox != null) _settings.NonSelectedTextColor = _nonSelectedColorTextBox.Text;
            if (_spacingSlider != null) _settings.ItemSpacing = (int)_spacingSlider.Value;
            if (_opacitySlider != null) _settings.MoveOverlayOpacity = (int)_opacitySlider.Value;

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
