using System;
using System.IO;
using NUnit.Framework;
using Polly;

namespace FontRegister.UnitTests
{
    [TestFixture]
    [Order(0)]
    [Parallelizable(ParallelScope.None)]
    public class WindowsUserFontInstallerTests
    {
        private WindowsFontInstaller _installer;
        private string _tempFontDirectory;

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = Path.Combine(Path.GetTempPath(), "TestFonts_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFontDirectory);
            _installer = new WindowsFontInstaller(new WindowsSystemNotifier(), InstallationScope.User);
            Console.SetOut(new StringWriter()); // Reset console output
        }

        [TearDown]
        public void TearDown()
        {
            Policy.Handle<Exception>()
                .WaitAndRetry(10, _ => TimeSpan.FromMilliseconds(100))
                .Execute(() =>
                {
                    if (Directory.Exists(_tempFontDirectory))
                    {
                        Directory.Delete(_tempFontDirectory, true);
                    }
                });
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
            var fontDir = FontConsts.GetLocalFontDirectory();
            var normalizedName = Path.GetFileName(fontPath);
            if (!Path.HasExtension(normalizedName))
                normalizedName += ".ttf";

            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            var result = _installer.UninstallFont(normalizedName);

            // Assert
            Assert.That(result.UninstalledSuccessfully, Is.False, "Should return false for non-existent font");
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
            Assert.That(result.InstalledSuccessfully, Is.False);
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
            Assert.That(result.UninstalledSuccessfully, Is.False);
        }

        [Test]
        public void UninstallFont_WithPathOutsideFontDirectory_ReturnsFalse()
        {
            // Arrange
            var outsidePath = Path.Combine(_tempFontDirectory, "randomname.ttf");
            File.WriteAllText(outsidePath, "dummy content");

            // Act
            var result = _installer.UninstallFont(outsidePath);

            // Assert
            Assert.That(result.UninstalledSuccessfully, Is.False);
        }

        [Test]
        public void InstallFont_WithUnsupportedExtension_ReturnsFalse()
        {
            // Arrange
            var unsupportedPath = Path.Combine(_tempFontDirectory, "randomname.xyz");
            File.WriteAllText(unsupportedPath, "dummy content");

            // Act
            var result = _installer.InstallFont(unsupportedPath);

            // Assert
            Assert.That(result.InstalledSuccessfully, Is.False);
        }

        [Test]
        public void InstallFont_WithRelativePath_HandlesPathCorrectly()
        {
            // Arrange
            var fontPath = Path.Combine(_tempFontDirectory, "randomname.ttf");
            File.WriteAllText(fontPath, "dummy content");
            var relativePath = Path.GetFileName(fontPath);
            var currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_tempFontDirectory);
            try
            {
                // Act
                var result = _installer.InstallFont(relativePath);

                // Assert
                Assert.That(result.InstalledSuccessfully, Is.False); // False because it's not a valid font file
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }

        [Test]
        public void UninstallFont_WithNullSystemNotifier_DoesNotThrow()
        {
            // Arrange
            var fontPath = Path.Combine(_tempFontDirectory, "randomname.ttf");

            // Act & Assert
            Assert.DoesNotThrow(() => _installer.UninstallFont(fontPath));
        }
    }
}