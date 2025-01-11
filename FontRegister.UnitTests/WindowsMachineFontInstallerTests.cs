using System;
using System.IO;
using NUnit.Framework;

namespace FontRegister.UnitTests
{
    [TestFixture]
    public class WindowsMachineFontInstallerTests
    {
        private WindowsMachineFontInstaller _installer;
        private string _tempFontDirectory;

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = Path.Combine(Path.GetTempPath(), "TestFonts_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFontDirectory);
            _installer = new WindowsMachineFontInstaller(new WindowsSystemNotifier());
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
        public void InstallFont_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var invalidPath = Path.Combine(_tempFontDirectory, "nonexistent.ttf");

            // Act
            var result = _installer.InstallFont(invalidPath);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test, Ignore("TODO")]
        public void UninstallFont_WithInvalidName_ReturnsFalse()
        {
            // Arrange
            var invalidName = "NonExistentFont";

            // Act
            var result = _installer.UninstallFont(invalidName);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void UninstallFont_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var invalidPath = Path.Combine(_tempFontDirectory, "nonexistent.ttf");

            // Act
            var result = _installer.UninstallFont(invalidPath);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void UninstallFont_WithPathOutsideFontDirectory_ReturnsFalse()
        {
            // Arrange
            var outsidePath = Path.Combine(_tempFontDirectory, "test.ttf");
            File.WriteAllText(outsidePath, "dummy content");

            // Act
            var result = _installer.UninstallFont(outsidePath);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void InstallFont_WithUnsupportedExtension_ReturnsFalse()
        {
            // Arrange
            var unsupportedPath = Path.Combine(_tempFontDirectory, "test.xyz");
            File.WriteAllText(unsupportedPath, "dummy content");

            // Act
            var result = _installer.InstallFont(unsupportedPath);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void InstallFont_WithRelativePath_HandlesPathCorrectly()
        {
            // Arrange
            var fontPath = Path.Combine(_tempFontDirectory, "test.ttf");
            File.WriteAllText(fontPath, "dummy content");
            var relativePath = Path.GetFileName(fontPath);
            Directory.SetCurrentDirectory(_tempFontDirectory);

            // Act
            var result = _installer.InstallFont(relativePath);

            // Assert
            Assert.That(result, Is.False); // False because it's not a valid font file
        }

        [Test]
        public void UninstallFont_WithNullSystemNotifier_DoesNotThrow()
        {
            // Arrange
            var fontPath = Path.Combine(_tempFontDirectory, "test.ttf");

            // Act & Assert
            Assert.DoesNotThrow(() => _installer.UninstallFont(fontPath));
        }
    }
}
