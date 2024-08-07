﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using FontRegister;
using Microsoft.Win32;
using NUnit.Framework;

namespace FontRegAuto.Tests
{
    [TestFixture("Historic.otf")]
    [TestFixture("Mang Kenapa.otf")]
    [TestFixture("Mang Kenapa.ttf")]
    public class IntegrationTests
    {
        private const string TEST_FONT_PATTERN = @"TestFont_\w+";
        private Random _random = new Random();
        private string _tempFontDirectory;
        private string _userFontDirectory;

        public string FileName { get; set; }

        public IntegrationTests(string fileName)
        {
            FileName = fileName;
        }

        [SetUp]
        public void Setup()
        {
            _tempFontDirectory = Path.Combine(Path.GetTempPath(), "TestFonts_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFontDirectory);

            _userFontDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Fonts"
            );

            // Create multiple test fonts
            for (int i = 0; i < 5; i++)
            {
                CreateTestFont();
            }
        }

        [TearDown]
        public void TearDown()
        {
            CleanupTestFonts();
            try
            {
                Directory.Delete(_tempFontDirectory, true);
            }
            catch (IOException)
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
                    TryDeleteFile(file, maxRetries: 15);
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

        private void TryDeleteFile(string filePath, int maxRetries)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Deleted font file {filePath}");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    if (i == maxRetries - 1) Console.WriteLine($"Failed to delete font file {filePath} after {maxRetries} attempts. Retrying...");

                    System.Threading.Thread.Sleep(100); // Wait a bit before retrying
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1) Console.WriteLine($"Failed to delete font file {filePath} after {maxRetries} attempts. Retrying...");

                    System.Threading.Thread.Sleep(100); // Wait a bit before retrying
                }
            }
        }

        [Test]
        public void CommandLine_InstallFont_ShouldSucceed()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            var args = new[] { "install", randomFontPath };

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(IsFontInstalled(Path.GetFileNameWithoutExtension(randomFontPath)), "Font was not successfully installed.");
        }

        [Test]
        public void CommandLine_InstallMultipleFonts_ShouldSucceed()
        {
            // Arrange
            var randomFontPaths = GetRandomTestFontPaths(3);
            var args = new[] { "install" }.Concat(randomFontPaths).ToArray();

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            foreach (var fontPath in randomFontPaths)
            {
                Assert.IsTrue(IsFontInstalled(Path.GetFileNameWithoutExtension(fontPath)), $"Font {fontPath} was not successfully installed.");
            }
        }

        [Test]
        public void CommandLine_UninstallFont_ShouldSucceed()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            string fontName = Path.GetFileNameWithoutExtension(randomFontPath);
            Program.Main(new[] { "install", randomFontPath });

            // Act
            var result = Program.Main(new[] { "uninstall", fontName });

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsFalse(IsFontInstalled(fontName), "Font was not successfully uninstalled.");
        }

        [Test]
        public void CommandLine_InvalidArguments_ShouldFail()
        {
            // Arrange
            var args = new[] { "invalid-arg" };

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
        }

        [Test]
        public void CommandLine_NoArguments_ShouldPrintUsage()
        {
            // Arrange
            var args = new string[0];

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
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

        private bool IsFontInstalled(string fontName)
        {
            // Check in user font directory
            if (File.Exists(Path.Combine(_userFontDirectory, fontName + ".otf")) ||
                File.Exists(Path.Combine(_userFontDirectory, fontName + ".ttf")))
            {
                // Check registry
                using (var fontsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts"))
                {
                    if (fontsKey != null)
                    {
                        return fontsKey.GetValueNames().Any(n => n.StartsWith(fontName, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            return false;
        }
    }

    internal class EmbeddedResourceHelper
    {
        public static byte[] ReadEmbeddedResource(string resourceNameEndsWith)
        {
            var assembly = typeof(EmbeddedResourceHelper).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.FirstOrDefault(r => r.EndsWith(resourceNameEndsWith));

            if (resourceName == null)
                throw new ArgumentException($"Resource ending with '{resourceNameEndsWith}' not found in assembly.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly.");

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}