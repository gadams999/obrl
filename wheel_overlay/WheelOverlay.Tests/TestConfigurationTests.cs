using WheelOverlay.Tests.Infrastructure;

namespace WheelOverlay.Tests;

public class TestConfigurationTests
{
    [Fact]
    public void IsRunningInCI_WhenGitHubActionsIsTrue_ReturnsTrue()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");

            // Act
            var result = TestConfiguration.IsRunningInCI();

            // Assert
            Assert.True(result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", originalValue);
        }
    }

    [Fact]
    public void IsRunningInCI_WhenGitHubActionsIsFalse_ReturnsFalse()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "false");

            // Act
            var result = TestConfiguration.IsRunningInCI();

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", originalValue);
        }
    }

    [Fact]
    public void IsRunningInCI_WhenGitHubActionsIsNotSet_ReturnsFalse()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);

            // Act
            var result = TestConfiguration.IsRunningInCI();

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", originalValue);
        }
    }

    [Fact]
    public void IsRunningInCI_WhenGitHubActionsIsEmpty_ReturnsFalse()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "");

            // Act
            var result = TestConfiguration.IsRunningInCI();

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", originalValue);
        }
    }

    [Fact]
    public void GetIterationCount_ReturnsTenForFastTestsOrHundredOtherwise()
    {
        // Act
        var count = TestConfiguration.GetIterationCount();

        // Assert
#if FAST_TESTS
        Assert.Equal(10, count);
#else
        Assert.Equal(100, count);
#endif
    }

    [Fact]
    public void GetConfigurationName_ReturnsCorrectConfigurationName()
    {
        // Act
        var configName = TestConfiguration.GetConfigurationName();

        // Assert
#if FAST_TESTS
        Assert.Equal("FastTests", configName);
#elif DEBUG
        Assert.Equal("Debug", configName);
#else
        Assert.Equal("Release", configName);
#endif
    }

    [Fact]
    public void GetConfigurationName_IsConsistentWithIterationCount()
    {
        // Act
        var configName = TestConfiguration.GetConfigurationName();
        var iterationCount = TestConfiguration.GetIterationCount();

        // Assert
        if (configName == "FastTests")
        {
            Assert.Equal(10, iterationCount);
        }
        else
        {
            Assert.Equal(100, iterationCount);
            Assert.True(configName == "Debug" || configName == "Release");
        }
    }
}
