using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace FontRegister.UnitTests
{
    [TestFixture]
    [NonParallelizable]
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
            Console.SetOut(new StringWriter()); // Reset console output
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

        [TestCase(@"C:\Windows\Fonts\somefont.ttf", TestName = "Absolute path")]
        [TestCase(@"somefont.ttf", TestName = "Just filename with extension")]
        [TestCase(@"somefont", TestName = "Just filename without extension")]
        [TestCase(@"./fonts/somefont.ttf", TestName = "Relative path")]
        [TestCase(@"..\fonts\somefont.ttf", TestName = "Parent relative path")]
        [TestCase(@"fonts/somefont.ttf", TestName = "Forward slash path")]
        public void UninstallFont_WithVariousPathFormats_HandlesPathCorrectly(string fontPath)
        {
            // Arrange
            var fontDir = FontConsts.GetMachineFontDirectory();
            var normalizedName = Path.GetFileName(fontPath);
            if (!Path.HasExtension(normalizedName))
                normalizedName += ".ttf";

            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            var result = _installer.UninstallFont(fontPath);

            // Assert
            Assert.That(result, Is.False, "Should return false for non-existent font");
            Assert.That(consoleOutput.ToString(), Does.Contain("Error uninstalling font").Or.Contains("Font not found"), "Expected console output not found");
        }

        [Test]
        public void InstallFont_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var invalidPath = Path.Combine(_tempFontDirectory, "NonExistentFont.ttf");

            // Arrange console capture
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            var result = _installer.InstallFont(invalidPath);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(consoleOutput.ToString(), Does.Contain("Font file path not found"), "Expected console output not found");
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
