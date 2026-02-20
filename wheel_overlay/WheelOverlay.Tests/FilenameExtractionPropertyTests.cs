using System;
using System.IO;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace WheelOverlay.Tests
{
    public class FilenameExtractionPropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 4: Filename Extraction from Path
        // Validates: Requirements 1.9
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_FilenameExtractionFromPath()
        {
            return Prop.ForAll(
                GenerateValidFilePaths(),
                fullPath =>
                {
                    // Act - Extract filename from path
                    var filename = Path.GetFileName(fullPath);
                    
                    // Assert - Filename should not contain directory separators
                    var hasNoBackslash = !filename.Contains("\\");
                    var hasNoForwardSlash = !filename.Contains("/");
                    var isNotEmpty = !string.IsNullOrEmpty(filename);
                    
                    return (hasNoBackslash && hasNoForwardSlash && isNotEmpty)
                        .Label($"Filename '{filename}' extracted from path '{fullPath}' should not contain path separators");
                });
        }
        
        /// <summary>
        /// Generates valid file paths with various structures for testing.
        /// </summary>
        private static Arbitrary<string> GenerateValidFilePaths()
        {
            var pathGen = from drive in Gen.Elements('C', 'D', 'E')
                          from dirCount in Gen.Choose(0, 5)
                          from dirs in Gen.ListOf(dirCount, Gen.Elements("Program Files", "Windows", "Users", "Test", "Apps", "Games"))
                          from filename in Gen.Elements("notepad.exe", "explorer.exe", "test.exe", "game.exe", "app.exe")
                          select BuildPath(drive, dirs, filename);
            
            return Arb.From(pathGen);
        }
        
        private static string BuildPath(char drive, Microsoft.FSharp.Collections.FSharpList<string> directories, string filename)
        {
            var path = $"{drive}:\\";
            foreach (var dir in directories)
            {
                path = Path.Combine(path, dir);
            }
            return Path.Combine(path, filename);
        }
    }
}
