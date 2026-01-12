using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Xunit;

namespace WheelOverlay.Tests
{
    public class AboutWindowTests
    {
        [Fact]
        public void AboutWindow_DisplaysAllRequiredElements()
        {
            // Run test on STA thread
            Exception? testException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Arrange & Act
                    var aboutWindow = new AboutWindow();
                    
                    // Force the window to load its content
                    aboutWindow.Show();
                    aboutWindow.Hide();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                    // Assert - Check that all required elements exist
                    Assert.NotNull(aboutWindow);
                    
                    // Find the VersionTextBlock
                    var versionTextBlock = FindChild<TextBlock>(aboutWindow, "VersionTextBlock");
                    Assert.NotNull(versionTextBlock);
                    Assert.Contains("Wheel Overlay", versionTextBlock.Text);
                    Assert.Contains("0.5.2", versionTextBlock.Text);
                    
                    // Find the CloseButton
                    var closeButton = FindChild<Button>(aboutWindow, "CloseButton");
                    Assert.NotNull(closeButton);
                    Assert.Equal("Close", closeButton.Content);
                    
                    // Verify the window has the expected properties (which confirms it's properly configured)
                    Assert.Equal("About Wheel Overlay", aboutWindow.Title);
                    Assert.Equal(450, aboutWindow.Width);
                    Assert.Equal(450, aboutWindow.Height);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (testException != null)
                throw testException;
        }

        [Fact]
        public void AboutWindow_VersionMatchesAssemblyMetadata()
        {
            // Run test on STA thread
            Exception? testException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Arrange
                    var aboutWindow = new AboutWindow();
                    
                    // Force the window to load its content
                    aboutWindow.Show();
                    aboutWindow.Hide();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                    // Act
                    var versionTextBlock = FindChild<TextBlock>(aboutWindow, "VersionTextBlock");

                    // Assert
                    Assert.NotNull(versionTextBlock);
                    var expectedVersionString = VersionInfo.GetFullVersionString();
                    Assert.Equal(expectedVersionString, versionTextBlock.Text);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (testException != null)
                throw testException;
        }

        [Fact]
        public void AboutWindow_CloseButton_ClosesDialog()
        {
            // Run test on STA thread
            Exception? testException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Arrange
                    var aboutWindow = new AboutWindow();
                    
                    // Force the window to load its content
                    aboutWindow.Show();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    
                    var closeButton = FindChild<Button>(aboutWindow, "CloseButton");
                    Assert.NotNull(closeButton);

                    bool windowClosed = false;
                    aboutWindow.Closed += (s, e) => windowClosed = true;

                    // Act
                    closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    
                    // Give the window time to close
                    Thread.Sleep(100);

                    // Assert
                    Assert.True(windowClosed);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (testException != null)
                throw testException;
        }

        [Fact]
        public void AboutWindow_EscapeKey_ClosesDialog()
        {
            // Run test on STA thread
            Exception? testException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Arrange
                    var aboutWindow = new AboutWindow();
                    bool windowClosed = false;
                    aboutWindow.Closed += (s, e) => windowClosed = true;

                    // Act
                    aboutWindow.Show();
                    var keyEventArgs = new System.Windows.Input.KeyEventArgs(
                        System.Windows.Input.Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(aboutWindow),
                        0,
                        System.Windows.Input.Key.Escape)
                    {
                        RoutedEvent = UIElement.KeyDownEvent
                    };
                    aboutWindow.RaiseEvent(keyEventArgs);
                    
                    // Give the window time to close
                    Thread.Sleep(100);

                    // Assert
                    Assert.True(windowClosed);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (testException != null)
                throw testException;
        }

        [Fact]
        public void AboutWindow_HasCorrectWindowProperties()
        {
            // Run test on STA thread
            Exception? testException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Arrange & Act
                    var aboutWindow = new AboutWindow();

                    // Assert
                    Assert.Equal(WindowStyle.ToolWindow, aboutWindow.WindowStyle);
                    Assert.Equal(ResizeMode.NoResize, aboutWindow.ResizeMode);
                    Assert.Equal(WindowStartupLocation.CenterScreen, aboutWindow.WindowStartupLocation);
                    Assert.False(aboutWindow.ShowInTaskbar);
                    Assert.Equal(450, aboutWindow.Width);
                    Assert.Equal(450, aboutWindow.Height);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (testException != null)
                throw testException;
        }

        // Helper method to find child controls by name
        private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T? foundChild = null;

            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChild = typedChild;
                        break;
                    }
                }

                foundChild = FindChild<T>(child, childName);
                if (foundChild != null) break;
            }

            return foundChild;
        }

        // Helper method to find child controls in logical tree (for elements like Hyperlink)
        private static T? FindLogicalChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is DependencyObject dependencyChild)
                {
                    if (child is T typedChild)
                    {
                        if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                        {
                            return typedChild;
                        }
                    }

                    var foundChild = FindLogicalChild<T>(dependencyChild, childName);
                    if (foundChild != null) return foundChild;
                }
            }

            return null;
        }
    }
}
