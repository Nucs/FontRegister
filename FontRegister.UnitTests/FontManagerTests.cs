using System;
using System.IO;
using FontRegister.Abstraction;
using NUnit.Framework;
using Polly;

namespace FontRegister.UnitTests
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class FontManagerTests
    {
        private readonly bool _machineWide;
        private string _tempFontDirectory;
        private IFontInstaller _fontInstaller;
        private WindowsSystemNotifier _systemNotifier;
        private FontManager _fontManager;

        public FontManagerTests(bool machineWide)
        {
            _machineWide = machineWide;
        }

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = TestConsts.GetTestPath();
            Directory.CreateDirectory(_tempFontDirectory);

            _systemNotifier = new WindowsSystemNotifier();
            _fontInstaller = _machineWide ? new WindowsFontInstaller(_systemNotifier, InstallationScope.Machine) : new WindowsFontInstaller(_systemNotifier, InstallationScope.User);
            _fontManager = new FontManager(_fontInstaller);
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
            var txtFile = Path.Combine(directory, TestConsts.GetTestFontFileName("test.txt"));
            var docFile = Path.Combine(directory, TestConsts.GetTestFontFileName("test.doc"));
            try {
                File.WriteAllText(txtFile, "dummy content");
                File.WriteAllText(docFile, "dummy content");

                // Act & Assert
                Assert.DoesNotThrow(() => _fontManager.InstallFonts(new[] { directory }));
            }
            finally {
                _fontInstaller.UninstallFont(txtFile);
                _fontInstaller.UninstallFont(docFile);
            }
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
            var font1Path = Path.Combine(rootDir, TestConsts.GetTestFontFileName("test1.ttf"));
            var font2Path = Path.Combine(subDir, TestConsts.GetTestFontFileName("test2.ttf"));
            try {
                File.WriteAllText(font1Path, "dummy content");
                File.WriteAllText(font2Path, "dummy content");

                // Act & Assert
                Assert.DoesNotThrow(() => _fontManager.InstallFonts(new[] { rootDir }));
            }
            finally {
                _fontInstaller.UninstallFont(font1Path);
                _fontInstaller.UninstallFont(font2Path);
            }
        }
    }
}
