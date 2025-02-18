﻿using System;
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
        private const string TEST_FONT_PATTERN = @"TestFont_@_?\w+";

        private readonly Random _random = new Random();
        private string _tempDirectory;
        private string _fontDirectory;

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
            _tempDirectory = TestConsts.GetTestPath();
            Directory.CreateDirectory(_tempDirectory);
            while (!Directory.Exists(_tempDirectory))
            {
                Thread.Sleep(10);
            }

            _fontDirectory = _scope == InstallationScope.User ? FontConsts.GetLocalFontDirectory() : FontConsts.GetMachineFontDirectory();

            // Create multiple test fonts
            CreateTestFont();
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("Test Completed");
            Console.WriteLine("---");
            var testName = TestConsts.GetTestFontFileName(FileName, "", false);
            CleanupTestFonts(testName);
        }

        private void CleanupTestFonts(string testName)
        {
            // Clean up font files from font directory based on scope
            foreach (var file in Directory.GetFiles(_fontDirectory, "*.*"))
            {
                if (file.Contains(testName))
                {
                    TryDeleteFile(file);
                }
            }
        }

        private void TryDeleteFile(string filePath)
        {
            try
            {
                if (_scope == InstallationScope.Machine)
                {
                    new WindowsFontInstaller(InstallationScope.Machine).UninstallFont(filePath);
                }
                else
                {
                    new WindowsFontInstaller(InstallationScope.User).UninstallFont(filePath);
                }
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
        public void CommandLine_InstallSameFontTwice_Update_ShouldReturnSuccess()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            var args = new[] { "install" }
                .Concat(GetScopeArgs())
                .Concat(new[] { "--update" })
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


        [Test]
        [Retry(3)]
        public void CommandLine_UninstallDoesntDeleteExternalFont()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            string fontName = Path.GetFileNameWithoutExtension(randomFontPath);

            // Install first
            FontRegister.Program.Main(new[] { "install", randomFontPath });

            // Act
            var firstResult = FontRegister.Program.Main(new[] { "uninstall", randomFontPath });

            // Assert
            Assert.That(firstResult, Is.EqualTo(0), "First uninstallation should succeed");
            Assert.That(File.Exists(randomFontPath), Is.True, "First uninstallation should succeed");
            Assert.That(IsFontInstalled(fontName, true), Is.False, "Font should remain uninstalled");
        }

        [Test]
        [Retry(3)]
        public void CommandLine_UninstallDoesntDeleteExternalFont_RelativePath()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            string fontName = Path.GetFileNameWithoutExtension(randomFontPath);
            var currentDirectory = Environment.CurrentDirectory;
            try
            {
                var subOfRandomPath = Path.GetFullPath(Path.Combine(randomFontPath, "../../../"));
                var relativePath = Path.GetRelativePath(subOfRandomPath, randomFontPath);
                Environment.CurrentDirectory = subOfRandomPath;

                Assert.That(File.Exists(relativePath), Is.True, "First uninstallation should succeed");

                // Install first
                FontRegister.Program.Main(new[] { "install", relativePath });

                // Act
                var firstResult = FontRegister.Program.Main(new[] { "uninstall", relativePath });

                // Assert
                Assert.That(firstResult, Is.EqualTo(0), "First uninstallation should succeed");
                Assert.That(File.Exists(relativePath), Is.True, "First uninstallation should succeed");
                Assert.That(IsFontInstalled(fontName, true), Is.False, "Font should remain uninstalled");
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }

        private string CreateTestFont()
        {
            var fontPath = TestConsts.GetTestFontFilePath(_tempDirectory, FileName);

            // Create a minimal OTF file
            byte[] fontFile = EmbeddedResourceHelper.ReadEmbeddedResource(FileName);

            File.WriteAllBytes(fontPath, fontFile);

            return fontPath;
        }

        private string GetRandomTestFontPath()
        {
            var testFonts = Directory.GetFiles(_tempDirectory, "*" + Path.GetExtension(FileName));

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
                    RegistryKey registryKey = _scope == InstallationScope.Machine
                        ? Registry.LocalMachine.OpenSubKey(FontConsts.FontRegistryKey)
                        : Registry.CurrentUser.OpenSubKey(FontConsts.FontRegistryKey);

                    // Check registry first
                    using (registryKey)
                    {
                        if (registryKey != null)
                        {
                            var matchingKeys = registryKey.GetValueNames()
                                .Where(n => n.Contains(fontName, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            foreach (var key in matchingKeys)
                            {
                                var fontPath = (string?)registryKey.GetValue(key);
                                if (!string.IsNullOrEmpty(fontPath))
                                {
                                    // For external fonts, check if the file exists at the registered path
                                    if (Path.IsPathRooted(fontPath))
                                    {
                                        if (File.Exists(fontPath))
                                            return true;
                                    }
                                    // For regular installations, check in font directory
                                    else if (File.Exists(Path.Combine(_fontDirectory, fontPath)))
                                    {
                                        return true;
                                    }
                                }
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