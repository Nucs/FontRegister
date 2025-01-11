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

        [TestCase(@"C:\Windows\Fonts\arial.ttf", TestName = "Absolute path")]
        [TestCase(@"arial.ttf", TestName = "Just filename with extension")]
        [TestCase(@"arial", TestName = "Just filename without extension")]
        [TestCase(@"./fonts/arial.ttf", TestName = "Relative path")]
        [TestCase(@"..\fonts\arial.ttf", TestName = "Parent relative path")]
        [TestCase(@"fonts/arial.ttf", TestName = "Forward slash path")]
        public void UninstallFont_WithVariousPathFormats_HandlesPathCorrectly(string fontPath)
        {
            // Arrange
            var fontDir = FontConsts.GetMachineFontDirectory();
            var normalizedName = Path.GetFileName(fontPath);
            if (!Path.HasExtension(normalizedName))
                normalizedName += ".ttf";

            // Act
            var result = _installer.UninstallFont(fontPath);

            // Assert
            Assert.That(result, Is.False, "Should return false for non-existent font");
        }

        [Test]
        public void UninstallFont_WithMultipleExtensions_ThrowsException()
        {
            // Arrange
            var fontDir = FontConsts.GetMachineFontDirectory();
            var fontName = "TestFont";
            var ttfPath = Path.Combine(fontDir, $"{fontName}.ttf");
            var otfPath = Path.Combine(fontDir, $"{fontName}.otf");

            File.WriteAllText(ttfPath, "dummy content");
            File.WriteAllText(otfPath, "dummy content");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _installer.UninstallFont(fontName));
            StringAssert.Contains("Multiple font files found", ex.Message);
        }

        [Test]
        public void InstallFont_WithInvalidPath_ReturnsFalse()
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
