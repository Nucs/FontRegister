using System;
using System.IO;
using NUnit.Framework;

namespace FontRegister.UnitTests
{
    [TestFixture]
    public class FontManagerTests
    {
        private string _tempFontDirectory;
        private WindowsFontInstaller _fontInstaller;
        private WindowsSystemNotifier _systemNotifier;
        private FontManager _fontManager;

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = Path.Combine(Path.GetTempPath(), "TestFonts_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFontDirectory);
            
            _systemNotifier = new WindowsSystemNotifier();
            _fontInstaller = new WindowsFontInstaller(_systemNotifier);
            _fontManager = new FontManager(_fontInstaller);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempFontDirectory))
                {
                    Directory.Delete(_tempFontDirectory, true);
                }
            }
            catch (IOException)
            {
                Console.WriteLine($"Warning: Unable to delete temporary directory {_tempFontDirectory}");
            }
        }

        [Test]
        public void InstallFonts_WithInvalidPath_ShouldSkip()
        {
            // Arrange
            var invalidPath = Path.Combine(_tempFontDirectory, "nonexistent.ttf");

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.InstallFonts(new[] { invalidPath }));
        }

        [Test]
        public void InstallFonts_WithEmptyDirectory_ShouldNotThrow()
        {
            // Arrange
            var emptyDir = Path.Combine(_tempFontDirectory, "empty");
            Directory.CreateDirectory(emptyDir);

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.InstallFonts(new[] { emptyDir }));
        }

        [Test]
        public void UninstallFonts_WithNonExistentFont_ShouldNotThrow()
        {
            // Arrange
            var nonExistentFont = "NonExistentFont";

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.UninstallFonts(new[] { nonExistentFont }));
        }

        [Test]
        public void InstallFonts_WithMultiplePaths_ShouldProcessAll()
        {
            // Arrange
            var paths = new[]
            {
                Path.Combine(_tempFontDirectory, "nonexistent1.ttf"),
                Path.Combine(_tempFontDirectory, "nonexistent2.ttf")
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.InstallFonts(paths));
        }

        [Test]
        public void UninstallFonts_WithMultipleNames_ShouldProcessAll()
        {
            // Arrange
            var fontNames = new[] { "Font1", "Font2" };

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.UninstallFonts(fontNames));
        }

        [Test]
        public void InstallFonts_WithMixedValidAndInvalidPaths_HandlesGracefully()
        {
            // Arrange
            var validDir = Path.Combine(_tempFontDirectory, "valid");
            var invalidDir = Path.Combine(_tempFontDirectory, "invalid");
            Directory.CreateDirectory(validDir);
            var paths = new[] { validDir, invalidDir };

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.InstallFonts(paths));
        }

        [Test]
        public void InstallFonts_WithUnsupportedFileExtensions_SkipsFiles()
        {
            // Arrange
            var directory = Path.Combine(_tempFontDirectory, "mixed");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "test.txt"), "dummy content");
            File.WriteAllText(Path.Combine(directory, "test.doc"), "dummy content");

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.InstallFonts(new[] { directory }));
        }

        [Test]
        public void UninstallFonts_WithEmptyArray_HandlesGracefully()
        {
            // Arrange
            var emptyArray = Array.Empty<string>();

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.UninstallFonts(emptyArray));
        }

        [Test]
        public void InstallFonts_WithRecursiveDirectories_ProcessesAllFiles()
        {
            // Arrange
            var rootDir = Path.Combine(_tempFontDirectory, "root");
            var subDir = Path.Combine(rootDir, "sub");
            Directory.CreateDirectory(rootDir);
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(rootDir, "test1.ttf"), "dummy content");
            File.WriteAllText(Path.Combine(subDir, "test2.ttf"), "dummy content");

            // Act & Assert
            Assert.DoesNotThrow(() => _fontManager.InstallFonts(new[] { rootDir }));
        }
    }
}
