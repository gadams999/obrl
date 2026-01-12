using System.Diagnostics;
using System.IO;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for build and deployment processes.
    /// Validates that the application builds correctly in both Debug and Release configurations.
    /// Requirements: 12.7
    /// </summary>
    public class BuildTests
    {
        private readonly string _solutionDirectory;
        private readonly string _projectDirectory;

        public BuildTests()
        {
            // Navigate up from test project to solution directory
            _solutionDirectory = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", ".."
            ));
            
            _projectDirectory = Path.Combine(_solutionDirectory, "WheelOverlay");
        }

        /// <summary>
        /// Helper method to execute dotnet build command and capture output.
        /// </summary>
        private (int exitCode, string output, string error) ExecuteBuild(string configuration)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{_projectDirectory}\" -c {configuration}",
                WorkingDirectory = _solutionDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return (process.ExitCode, output, error);
        }

        /// <summary>
        /// Helper method to check if build output contains errors or warnings.
        /// </summary>
        private bool HasErrorsOrWarnings(string output)
        {
            // Check for error/warning indicators in build output
            return output.Contains(" error ", StringComparison.OrdinalIgnoreCase) ||
                   output.Contains(" warning ", StringComparison.OrdinalIgnoreCase) ||
                   output.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that the application builds successfully in Debug configuration.
        /// Verifies exit code is 0 and no errors or warnings are present.
        /// Requirements: 12.1, 12.2, 12.7
        /// </summary>
        [Fact]
        public void DebugBuild_CompletesSuccessfully()
        {
            // Act
            var (exitCode, output, error) = ExecuteBuild("Debug");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.False(HasErrorsOrWarnings(output), 
                $"Build output contains errors or warnings:\n{output}\n{error}");
        }

        /// <summary>
        /// Tests that the application builds successfully in Release configuration.
        /// Verifies exit code is 0 and no errors or warnings are present.
        /// Requirements: 12.1, 12.2, 12.7
        /// </summary>
        [Fact]
        public void ReleaseBuild_CompletesSuccessfully()
        {
            // Act
            var (exitCode, output, error) = ExecuteBuild("Release");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.False(HasErrorsOrWarnings(output), 
                $"Build output contains errors or warnings:\n{output}\n{error}");
        }

        /// <summary>
        /// Tests that the build produces a valid executable file.
        /// Verifies WheelOverlay.exe exists and has non-zero file size.
        /// Requirements: 12.3, 12.7
        /// </summary>
        [Fact]
        public void Build_ProducesValidExecutable()
        {
            // Arrange
            var debugOutputPath = Path.Combine(_projectDirectory, "bin", "Debug", "net10.0-windows", "WheelOverlay.exe");
            var releaseOutputPath = Path.Combine(_projectDirectory, "bin", "Release", "net10.0-windows", "WheelOverlay.exe");

            // Act - Build in both configurations to ensure executable exists
            ExecuteBuild("Debug");
            ExecuteBuild("Release");

            // Assert - Check Debug executable
            Assert.True(File.Exists(debugOutputPath), 
                $"Debug executable not found at: {debugOutputPath}");
            var debugFileInfo = new FileInfo(debugOutputPath);
            Assert.True(debugFileInfo.Length > 0, 
                "Debug executable has zero file size");

            // Assert - Check Release executable
            Assert.True(File.Exists(releaseOutputPath), 
                $"Release executable not found at: {releaseOutputPath}");
            var releaseFileInfo = new FileInfo(releaseOutputPath);
            Assert.True(releaseFileInfo.Length > 0, 
                "Release executable has zero file size");
        }
    }
}

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Property-based tests for build integrity.
    /// Validates that builds are consistent and produce valid output.
    /// </summary>
    public class BuildIntegrityPropertyTests
    {
        private readonly string _solutionDirectory;
        private readonly string _projectDirectory;

        public BuildIntegrityPropertyTests()
        {
            // Navigate up from test project to solution directory
            _solutionDirectory = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", ".."
            ));
            
            _projectDirectory = Path.Combine(_solutionDirectory, "WheelOverlay");
        }

        /// <summary>
        /// Helper method to execute dotnet build command and capture output.
        /// </summary>
        private (int exitCode, string output, string error) ExecuteBuild(string configuration)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{_projectDirectory}\" -c {configuration}",
                WorkingDirectory = _solutionDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return (process.ExitCode, output, error);
        }

        /// <summary>
        /// Property test for build output integrity.
        /// Feature: dotnet10-upgrade-and-testing, Property 15: Build Output Integrity
        /// Validates: Requirements 12.1, 12.2, 12.3, 12.7
        /// Tests that multiple builds produce consistent, valid output.
        /// </summary>
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 15: Build Output Integrity")]
        public FsCheck.Property Property_BuildOutputIntegrity()
        {
            return Prop.ForAll(
                Arb.From(Gen.Elements("Debug", "Release")),
                configuration =>
                {
                    // Act - Execute build
                    var (exitCode, output, error) = ExecuteBuild(configuration);

                    // Assert - Build should succeed
                    bool buildSucceeded = exitCode == 0;
                    
                    // Assert - No errors or warnings
                    bool noErrors = !output.Contains(" error ", StringComparison.OrdinalIgnoreCase) &&
                                   !output.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);
                    
                    bool noWarnings = !output.Contains(" warning ", StringComparison.OrdinalIgnoreCase);

                    // Assert - Executable exists
                    var exePath = Path.Combine(_projectDirectory, "bin", configuration, "net10.0-windows", "WheelOverlay.exe");
                    bool executableExists = File.Exists(exePath);
                    
                    // Assert - Executable has valid size
                    bool validSize = false;
                    if (executableExists)
                    {
                        var fileInfo = new FileInfo(exePath);
                        validSize = fileInfo.Length > 0;
                    }

                    bool allChecksPass = buildSucceeded && noErrors && noWarnings && executableExists && validSize;

                    return allChecksPass
                        .Label($"Build {configuration} should succeed without errors/warnings and produce valid executable. " +
                               $"ExitCode={exitCode}, NoErrors={noErrors}, NoWarnings={noWarnings}, " +
                               $"ExeExists={executableExists}, ValidSize={validSize}");
                });
        }
    }
}
