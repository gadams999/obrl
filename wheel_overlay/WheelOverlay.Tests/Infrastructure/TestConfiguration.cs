namespace WheelOverlay.Tests.Infrastructure;

/// <summary>
/// Provides environment detection and configuration information for test execution.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Returns true if running in a CI environment.
    /// Detects CI by checking for the GITHUB_ACTIONS environment variable.
    /// </summary>
    /// <returns>True if running in CI, false otherwise.</returns>
    public static bool IsRunningInCI()
    {
        var githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        return !string.IsNullOrEmpty(githubActions) && githubActions.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the current iteration count based on the build configuration.
    /// Returns 10 for FastTests configuration, 100 for Debug/Release configurations.
    /// </summary>
    /// <returns>The iteration count (10 or 100).</returns>
    public static int GetIterationCount()
    {
#if FAST_TESTS
        return 10;
#else
        return 100;
#endif
    }

    /// <summary>
    /// Returns the name of the active build configuration.
    /// </summary>
    /// <returns>Configuration name: "FastTests", "Debug", or "Release".</returns>
    public static string GetConfigurationName()
    {
#if FAST_TESTS
        return "FastTests";
#elif DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}
