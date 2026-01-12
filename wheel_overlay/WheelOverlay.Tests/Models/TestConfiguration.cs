using System;

namespace WheelOverlay.Tests.Models
{
    /// <summary>
    /// Configuration model for test execution settings.
    /// Provides centralized configuration for test timeouts, paths, and other test-related settings.
    /// Requirements: 11.7
    /// </summary>
    public class TestConfiguration
    {
        /// <summary>
        /// Gets or sets whether UI automation is enabled for tests.
        /// When false, UI automation tests will be skipped.
        /// Default: true
        /// </summary>
        public bool EnableUIAutomation { get; set; } = true;

        /// <summary>
        /// Gets or sets the default timeout for test operations in milliseconds.
        /// This timeout applies to UI operations, async operations, and other time-sensitive tests.
        /// Default: 5000ms (5 seconds)
        /// </summary>
        public int TestTimeout { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the path to test data files.
        /// This directory should contain test fixtures, sample configurations, and other test resources.
        /// Default: "./TestData"
        /// </summary>
        public string TestDataPath { get; set; } = "./TestData";

        /// <summary>
        /// Gets or sets the path to temporary test output files.
        /// This directory is used for test logs, screenshots, and other temporary test artifacts.
        /// Default: "./TestOutput"
        /// </summary>
        public string TestOutputPath { get; set; } = "./TestOutput";

        /// <summary>
        /// Gets or sets whether to capture screenshots on test failures.
        /// When true, UI tests will capture screenshots when assertions fail.
        /// Default: true
        /// </summary>
        public bool CaptureScreenshotsOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable verbose logging during tests.
        /// When true, tests will output detailed diagnostic information.
        /// Default: false
        /// </summary>
        public bool VerboseLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of iterations for property-based tests.
        /// Property-based tests will generate and test this many random inputs.
        /// Minimum: 100 (as per design requirements)
        /// Default: 100
        /// </summary>
        public int PropertyTestIterations { get; set; } = 100;

        /// <summary>
        /// Creates a default test configuration with standard settings.
        /// </summary>
        /// <returns>A new TestConfiguration instance with default values</returns>
        public static TestConfiguration CreateDefault()
        {
            return new TestConfiguration();
        }

        /// <summary>
        /// Creates a test configuration optimized for CI/CD environments.
        /// Disables UI automation and screenshot capture, increases timeouts.
        /// </summary>
        /// <returns>A new TestConfiguration instance optimized for CI/CD</returns>
        public static TestConfiguration CreateForCI()
        {
            return new TestConfiguration
            {
                EnableUIAutomation = false,
                TestTimeout = 10000, // Longer timeout for CI environments
                CaptureScreenshotsOnFailure = false,
                VerboseLogging = true,
                PropertyTestIterations = 100
            };
        }

        /// <summary>
        /// Creates a test configuration optimized for fast local development.
        /// Reduces property test iterations and disables screenshot capture.
        /// </summary>
        /// <returns>A new TestConfiguration instance optimized for fast execution</returns>
        public static TestConfiguration CreateForFastExecution()
        {
            return new TestConfiguration
            {
                EnableUIAutomation = true,
                TestTimeout = 3000, // Shorter timeout for fast feedback
                CaptureScreenshotsOnFailure = false,
                VerboseLogging = false,
                PropertyTestIterations = 50 // Fewer iterations for faster tests
            };
        }

        /// <summary>
        /// Validates the configuration and throws an exception if invalid.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when configuration values are invalid</exception>
        public void Validate()
        {
            if (TestTimeout <= 0)
            {
                throw new ArgumentException("TestTimeout must be greater than 0", nameof(TestTimeout));
            }

            if (PropertyTestIterations < 1)
            {
                throw new ArgumentException("PropertyTestIterations must be at least 1", nameof(PropertyTestIterations));
            }

            if (string.IsNullOrWhiteSpace(TestDataPath))
            {
                throw new ArgumentException("TestDataPath cannot be null or empty", nameof(TestDataPath));
            }

            if (string.IsNullOrWhiteSpace(TestOutputPath))
            {
                throw new ArgumentException("TestOutputPath cannot be null or empty", nameof(TestOutputPath));
            }
        }
    }
}
