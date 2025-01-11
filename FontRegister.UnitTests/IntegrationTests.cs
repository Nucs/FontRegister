using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using NUnit.Framework;
using Polly;

namespace FontRegister.UnitTests
{
    public enum InstallationScope
    {
        User,
        Machine
    }

    [TestFixture("Historic.otf", InstallationScope.User)]
    [TestFixture("Mang Kenapa.otf", InstallationScope.User)]
    [TestFixture("Mang Kenapa.ttf", InstallationScope.User)]
    [TestFixture("steelfis.fon", InstallationScope.User)]
    [TestFixture("meiryo.ttc", InstallationScope.User)]
    [TestFixture("Mang_Kenapa.fnt", InstallationScope.User)]
    [TestFixture("JetBrainsMono-Regular.otf", InstallationScope.User)]
    [TestFixture("Historic.otf", InstallationScope.Machine)]
    [TestFixture("Mang Kenapa.otf", InstallationScope.Machine)]
    [TestFixture("Mang Kenapa.ttf", InstallationScope.Machine)]
    [TestFixture("steelfis.fon", InstallationScope.Machine)]
    [TestFixture("meiryo.ttc", InstallationScope.Machine)]
    [TestFixture("Mang_Kenapa.fnt", InstallationScope.Machine)]
    [TestFixture("JetBrainsMono-Regular.otf", InstallationScope.Machine)]
    public class IntegrationTests
    {
        private readonly InstallationScope _scope;
        private const string TEST_FONT_PATTERN = @"TestFont_\w+";

        private Random _random = new Random();
        private string _tempFontDirectory;
        private string _userFontDirectory;

        public string FileName { get; set; }

        public IntegrationTests(string fileName, InstallationScope scope)
        {
            _scope = scope;
            FileName = fileName;
        }

        private string[] GetScopeArgs()
        {
            return _scope == InstallationScope.Machine ? new[] { "--machine" } : new[] { "--user" };
        }

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = Path.Combine(Path.GetTempPath(), "TestFonts_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFontDirectory);
            while (!Directory.Exists(_tempFontDirectory))
            {
                Thread.Sleep(10);
            }

            _userFontDirectory = FontConsts.GetLocalFontDirectory();

            // Create multiple test fonts
            for (int i = 0; i < 5; i++)
            {
                CreateTestFont();
            }
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("Test Completed");
            Console.WriteLine("---");
            CleanupTestFonts();
            try
            {
                Directory.Delete(_tempFontDirectory, true);
            }
            catch (IOException)
            {
                Console.WriteLine($"Warning: Unable to delete temporary directory {_tempFontDirectory}. It may need manual cleanup.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Warning: Unable to delete temporary directory {_tempFontDirectory}. It may need manual cleanup.");
            }
        }

        private void CleanupTestFonts()
        {
            var regex = new Regex(TEST_FONT_PATTERN, RegexOptions.IgnoreCase);

            // Clean up font files from user font directory
            foreach (var file in Directory.GetFiles(_userFontDirectory, "*.*"))
            {
                if (regex.IsMatch(Path.GetFileNameWithoutExtension(file)))
                {
                    TryDeleteFile(file);
                }
            }

            // Clean up from registry
            using (var fontsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true))
            {
                if (fontsKey != null)
                {
                    foreach (var fontName in fontsKey.GetValueNames())
                    {
                        if (regex.IsMatch(fontName))
                        {
                            try
                            {
                                fontsKey.DeleteValue(fontName, false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to remove font from registry {fontName}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void TryDeleteFile(string filePath)
        {
            try
            {
                new WindowsUserFontInstaller().UninstallFont(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to uninstall font {filePath}: {e.Message}");
            }
        }

        [Test]
        [Retry(3)]
        public void CommandLine_InstallFont_ShouldSucceed()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            var args = new[] { "install" }
                .Concat(GetScopeArgs())
                .Concat(new[] { randomFontPath })
                .ToArray();

            // Act
            var result = FontRegister.Program.Main(args);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            Assert.That(IsFontInstalled(Path.GetFileNameWithoutExtension(randomFontPath)), Is.True, "Font was not successfully installed.");
        }

        [Test]
        [Retry(5)]
        public void CommandLine_InstallMultipleFonts_ShouldSucceed()
        {
            // Arrange
            var randomFontPaths = GetRandomTestFontPaths(3);
            var args = new[] { "install" }
                .Concat(GetScopeArgs())
                .Concat(randomFontPaths)
                .ToArray();

            // Act
            var result = FontRegister.Program.Main(args);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            foreach (var fontPath in randomFontPaths)
            {
                Assert.That(IsFontInstalled(Path.GetFileNameWithoutExtension(fontPath)), Is.True, $"Font {fontPath} was not successfully installed.");
            }
        }

        [Test]
        [Retry(3)]
        public void CommandLine_InstallSameFontTwice_ShouldReturnSuccessAndWarn()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            var args = new[] { "install" }
                .Concat(GetScopeArgs())
                .Concat(new[] { randomFontPath })
                .ToArray();

            // Act
            var firstResult = FontRegister.Program.Main(args);
            var secondResult = FontRegister.Program.Main(args);

            // Assert
            Assert.That(firstResult, Is.EqualTo(0), "First installation should succeed");
            Assert.That(secondResult, Is.EqualTo(0), "Second installation should succeed but warn");
            Assert.That(IsFontInstalled(Path.GetFileNameWithoutExtension(randomFontPath)), Is.True, "Font should remain installed");
        }

        [Test]
        [Retry(3)]
        public void CommandLine_UninstallSameFontTwice_ShouldReturnSuccessAndWarn()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            string fontName = Path.GetFileNameWithoutExtension(randomFontPath);

            // Install first
            FontRegister.Program.Main(new[] { "install", randomFontPath });

            // Act
            var firstResult = FontRegister.Program.Main(new[] { "uninstall", fontName });
            var secondResult = FontRegister.Program.Main(new[] { "uninstall", fontName });

            // Assert
            Assert.That(firstResult, Is.EqualTo(0), "First uninstallation should succeed");
            Assert.That(secondResult, Is.EqualTo(0), "Second uninstallation should succeed but warn");
            Assert.That(IsFontInstalled(fontName, true), Is.False, "Font should remain uninstalled");
        }

        [Test]
        [Retry(3)]
        public void CommandLine_UninstallFont_ShouldSucceed()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            string fontName = Path.GetFileNameWithoutExtension(randomFontPath);
            FontRegister.Program.Main(new[] { "install", randomFontPath });

            // Act
            var result = FontRegister.Program.Main(new[] { "uninstall", fontName });

            // Assert
            Assert.That(result, Is.EqualTo(0));
            Assert.That(IsFontInstalled(fontName, true), Is.False, "Font was not successfully uninstalled.");
        }

        [Test]
        [Retry(3)]
        public void CommandLine_InvalidArguments_ShouldFail()
        {
            // Arrange
            var args = new[] { "invalid-arg" };

            // Act
            var result = FontRegister.Program.Main(args);

            // Assert
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        [Retry(3)]
        public void CommandLine_NoArguments_ShouldPrintUsage()
        {
            // Arrange
            var args = new string[0];

            // Act
            var result = FontRegister.Program.Main(args);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            // Note: You might want to capture console output to verify usage information is printed
        }

        private string CreateTestFont()
        {
            string fontName = $"TestFont_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string fontPath = Path.Combine(_tempFontDirectory, $"{fontName}{Path.GetExtension(FileName)}");

            // Create a minimal OTF file
            byte[] minimalOtfFile = EmbeddedResourceHelper.ReadEmbeddedResource(FileName);

            File.WriteAllBytes(fontPath, minimalOtfFile);

            return fontPath;
        }

        private string GetRandomTestFontPath()
        {
            var testFonts = Directory.GetFiles(_tempFontDirectory, "*" + Path.GetExtension(FileName));

            if (testFonts.Length == 0)
            {
                return CreateTestFont();
            }

            return testFonts[_random.Next(testFonts.Length)];
        }

        private string[] GetRandomTestFontPaths(int count)
        {
            var fontPaths = new List<string>();
            for (int i = 0; i < count; i++)
            {
                fontPaths.Add(GetRandomTestFontPath());
            }

            return fontPaths.ToArray();
        }

        private bool IsFontInstalled(string fontName, bool checkingIfUninstalled = false)
        {
            var retries = checkingIfUninstalled
                ? new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200) }
                : new[]
                {
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromMilliseconds(400),
                    TimeSpan.FromMilliseconds(800),
                    TimeSpan.FromMilliseconds(1600),
                    TimeSpan.FromMilliseconds(3000),
                    TimeSpan.FromMilliseconds(6000),
                };
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(retries);
            try
            {
                return retryPolicy.Execute(() =>
                {
                    string fontDirectory = _scope == InstallationScope.Machine ? FontConsts.GetMachineFontDirectory() : _userFontDirectory;
                    RegistryKey registryKey = _scope == InstallationScope.Machine
                        ? Registry.LocalMachine.OpenSubKey(FontConsts.FontRegistryKey)
                        : Registry.CurrentUser.OpenSubKey(FontConsts.FontRegistryKey);

                    // Check in font directory
                    if (FontConsts.SupportedExtensions.Any(ext =>
                            File.Exists(Path.Combine(fontDirectory, fontName + ext))))
                    {
                        // Check registry
                        using (registryKey)
                        {
                            if (registryKey != null)
                            {
                                if (registryKey.GetValueNames().Any(n => n.Contains(fontName, StringComparison.OrdinalIgnoreCase)))
                                    return true;
                            }
                        }
                    }

                    throw new Exception("Font not found");
                });
            }
            catch (Exception)
            {
                Console.WriteLine("Looking for font: " + fontName);
                if (!checkingIfUninstalled)
                {
                    using var registryKey = _scope == InstallationScope.Machine
                        ? Registry.LocalMachine.OpenSubKey(FontConsts.FontRegistryKey)
                        : Registry.CurrentUser.OpenSubKey(FontConsts.FontRegistryKey);

                    if (registryKey != null)
                    {
                        Console.WriteLine("Installed fonts:");
                        foreach (var fontNameKey in registryKey.GetValueNames())
                        {
                            Console.WriteLine(fontNameKey);
                        }
                    }
                }

                return false;
            }
        }
    }
}