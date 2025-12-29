using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace WheelOverlay
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
            
            // Handle Escape key to close dialog
            this.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                }
            };
        }

        private void LoadVersionInfo()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    VersionTextBlock.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
                }
                else
                {
                    VersionTextBlock.Text = "Version Unknown";
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to read version information", ex);
                VersionTextBlock.Text = "Version Unknown";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to open GitHub link", ex);
                System.Windows.MessageBox.Show(
                    "Failed to open the GitHub repository link. Please visit:\n" + e.Uri.AbsoluteUri,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
