using Xunit;

namespace WheelOverlay.Tests.Infrastructure
{
    /// <summary>
    /// Collection definition for tests that modify the settings file.
    /// Tests in this collection run sequentially to avoid race conditions
    /// when multiple tests read/write the same settings.json file.
    /// </summary>
    [CollectionDefinition("SettingsFile")]
    public class SettingsFileCollection : ICollectionFixture<SettingsFileFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Fixture for settings file tests.
    /// Ensures proper cleanup between tests.
    /// </summary>
    public class SettingsFileFixture : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup is handled by individual tests
        }
    }
}
