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

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Set layout
            foreach (ComboBoxItem item in LayoutComboBox.Items)
            {
                if (item.Tag.ToString() == _settings.Layout.ToString())
                {
                    LayoutComboBox.SelectedItem = item;
                    break;
                }
            }

            // Set other values
            FontSizeSlider.Value = _settings.FontSize;
            SelectedColorTextBox.Text = _settings.SelectedTextColor;
            NonSelectedColorTextBox.Text = _settings.NonSelectedTextColor;
            SpacingSlider.Value = _settings.ItemSpacing;
            OpacitySlider.Value = _settings.MoveOverlayOpacity;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Save layout
            if (LayoutComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _settings.Layout = System.Enum.Parse<DisplayLayout>(selectedItem.Tag.ToString()!);
            }

            // Save other values
            _settings.FontSize = (int)FontSizeSlider.Value;
            _settings.SelectedTextColor = SelectedColorTextBox.Text;
            _settings.NonSelectedTextColor = NonSelectedColorTextBox.Text;
            _settings.ItemSpacing = (int)SpacingSlider.Value;
            _settings.MoveOverlayOpacity = (int)OpacitySlider.Value;

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
