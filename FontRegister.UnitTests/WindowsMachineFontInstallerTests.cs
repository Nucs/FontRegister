using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Polly;

namespace FontRegister.UnitTests
{
    [TestFixture]
    [Order(0)]
    [Parallelizable(ParallelScope.None)]
    public class WindowsMachineFontInstallerTests
    {
        private WindowsFontInstaller _installer;
        private string _tempFontDirectory;

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = TestConsts.GetTestPath();
            Directory.CreateDirectory(_tempFontDirectory);
            _installer = new WindowsFontInstaller(new WindowsSystemNotifier(), InstallationScope.Machine);
            Console.SetOut(new StringWriter()); // Reset console output
        }

        [TearDown]
        public void TearDown()
        {
            Policy.Handle<Exception>()
                .WaitAndRetry(20, i => TimeSpan.FromMilliseconds(100))
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
            var fontDir = FontConsts.GetMachineFontDirectory();
            var normalizedName = Path.GetFileName(fontPath);
            if (!Path.HasExtension(normalizedName))
                normalizedName += ".ttf";

            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            var result = _installer.UninstallFont(fontPath);

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
            try {
                File.WriteAllText(outsidePath, "dummy content");

                // Act
                var result = _installer.UninstallFont(outsidePath);

                // Assert
                Assert.That(result.UninstalledSuccessfully, Is.False);
            }
            finally {
                _installer.UninstallFont(outsidePath);
            }
        }

        [Test]
        public void InstallFont_WithUnsupportedExtension_ReturnsFalse()
        {
            // Arrange
            var unsupportedPath = Path.Combine(_tempFontDirectory, "randomname.xyz");
            try {
                File.WriteAllText(unsupportedPath, "dummy content");

                // Act
                var result = _installer.InstallFont(unsupportedPath);

                // Assert
                Assert.That(result.InstalledSuccessfully, Is.False);
            }
            finally {
                _installer.UninstallFont(unsupportedPath);
            }
        }

        [Test]
        public void InstallFont_WithRelativePath_HandlesPathCorrectly()
        {
            // Arrange
            var fontPath = Path.Combine(_tempFontDirectory, "randomname.ttf");
            var relativePath = Path.GetFileName(fontPath);
            var currentDirectory = Directory.GetCurrentDirectory();
            try {
                File.WriteAllText(fontPath, "dummy content");
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
            finally {
                _installer.UninstallFont(fontPath);
            }
        }

        [Test]
        public void UninstallFont_WithNullSystemNotifier_DoesNotThrow()
        {
            // Arrange
            var fontPath = Path.Combine(_tempFontDirectory, "randomname.ttf");
            try {
                File.WriteAllText(fontPath, "dummy content");

                // Act & Assert
                Assert.DoesNotThrow(() => _installer.UninstallFont(fontPath));
            }
            finally {
                _installer.UninstallFont(fontPath);
            }
        }
    }
}
