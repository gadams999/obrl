using System.Text.RegularExpressions;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Unit tests for the VersionInfo class.
    /// Validates version format and string generation.
    /// </summary>
    public class VersionInfoTests
    {
        /// <summary>
        /// Tests that the Version is read from assembly and follows semantic versioning format (MAJOR.MINOR.PATCH).
        /// Requirements: 2.1, 2.2
        /// </summary>
        [Fact]
        public void Version_FollowsSemanticVersioningFormat()
        {
            // Arrange
            var semanticVersionRegex = new Regex(@"^\d+\.\d+\.\d+$");

            // Act
            var version = VersionInfo.Version;

            // Assert
            Assert.Matches(semanticVersionRegex, version);
        }

        /// <summary>
        /// Tests that the Version matches the current assembly version (0.5.4).
        /// Requirements: 2.1, 2.2
        /// </summary>
        [Fact]
        public void Version_MatchesAssemblyVersion()
        {
            // Act
            var version = VersionInfo.Version;

            // Assert - Version should be 0.5.4 based on current AssemblyVersion
            Assert.Equal("0.5.4", version);
        }

        /// <summary>
        /// Tests that GetFullVersionString returns the expected format.
        /// Requirements: 2.1, 2.2
        /// </summary>
        [Fact]
        public void GetFullVersionString_ReturnsCorrectFormat()
        {
            // Act
            var fullVersionString = VersionInfo.GetFullVersionString();

            // Assert
            Assert.Equal("Wheel Overlay v0.5.4", fullVersionString);
        }

        /// <summary>
        /// Tests that GetFullVersionString includes the product name.
        /// Requirements: 2.1, 2.2
        /// </summary>
        [Fact]
        public void GetFullVersionString_ContainsProductName()
        {
            // Act
            var fullVersionString = VersionInfo.GetFullVersionString();

            // Assert
            Assert.Contains(VersionInfo.ProductName, fullVersionString);
        }

        /// <summary>
        /// Tests that GetFullVersionString includes the version number.
        /// Requirements: 2.1, 2.2
        /// </summary>
        [Fact]
        public void GetFullVersionString_ContainsVersion()
        {
            // Act
            var fullVersionString = VersionInfo.GetFullVersionString();

            // Assert
            Assert.Contains(VersionInfo.Version, fullVersionString);
        }

        /// <summary>
        /// Tests that ProductName is not null or empty.
        /// Requirements: 2.1
        /// </summary>
        [Fact]
        public void ProductName_IsNotNullOrEmpty()
        {
            // Act
            var productName = VersionInfo.ProductName;

            // Assert
            Assert.False(string.IsNullOrEmpty(productName));
        }

        /// <summary>
        /// Tests that Copyright is not null or empty.
        /// Requirements: 2.1
        /// </summary>
        [Fact]
        public void Copyright_IsNotNullOrEmpty()
        {
            // Act
            var copyright = VersionInfo.Copyright;

            // Assert
            Assert.False(string.IsNullOrEmpty(copyright));
        }

        /// <summary>
        /// Tests that Version does not contain a fourth component (no build number).
        /// Requirements: 2.7
        /// </summary>
        [Fact]
        public void Version_DoesNotContainBuildNumber()
        {
            // Act
            var version = VersionInfo.Version;
            var parts = version.Split('.');

            // Assert
            Assert.Equal(3, parts.Length);
        }
    }
}
