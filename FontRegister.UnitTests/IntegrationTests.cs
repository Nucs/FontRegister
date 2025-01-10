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
    [TestFixture("Historic.otf")]
    [TestFixture("Mang Kenapa.otf")]
    [TestFixture("Mang Kenapa.ttf")]
    [TestFixture("steelfis.fon")]
    [TestFixture("meiryo.ttc")]
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetFileInformationByHandleEx(
            IntPtr hFile,
            int fileInformationClass,
            out FILE_PROCESS_IDS_USING_FILE_INFORMATION outInfo,
            int dwBufferSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILE_PROCESS_IDS_USING_FILE_INFORMATION
        {
            public uint ProcessIdCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public uint[] ProcessIds;
        }

        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_SHARE_DELETE = 0x00000004;
        private const uint OPEN_EXISTING = 3;
        private const int FileProcessIdsUsingFileInformation = 47;

        private bool ReleaseFileLock(string filePath)
        {
            IntPtr handle = CreateFile(
                filePath,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.ToInt64() == -1)
            {
                return false;
            }

            try
            {
                var info = new FILE_PROCESS_IDS_USING_FILE_INFORMATION();
                int result = GetFileInformationByHandleEx(
                    handle,
                    FileProcessIdsUsingFileInformation,
                    out info,
                    Marshal.SizeOf<FILE_PROCESS_IDS_USING_FILE_INFORMATION>());

                if (result != 0 && info.ProcessIdCount > 0)
                {
                    foreach (uint processId in info.ProcessIds)
                    {
                        try
                        {
                            var process = Process.GetProcessById((int)processId);
                            if (process.ProcessName.Equals("fontdrvhost", StringComparison.OrdinalIgnoreCase) ||
                                process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                            {
                                continue; // Skip essential Windows processes
                            }

                            process.Kill();
                            process.WaitForExit(1000);
                        }
                        catch (ArgumentException)
                        {
                            // Process already terminated
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to terminate process: {ex.Message}");
                        }
                    }

                    return true;
                }
            }
            finally
            {
                CloseHandle(handle);
            }

            return false;
        }

        //TODO: test installing same twice and assert behavior
        //TODO: test uninstalling twice

        private void TryDeleteFile(string filePath)
        {
            try
            {
                new WindowsFontInstaller().UninstallFont(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to uninstall font {filePath}: {e.Message}");
            }
        }

        [Test]
        public void CommandLine_InstallFont_ShouldSucceed()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            var args = new[] { "install", randomFontPath };

            // Act
            var result = FontRegister.Program.Main(args);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            Assert.IsTrue(IsFontInstalled(Path.GetFileNameWithoutExtension(randomFontPath)), "Font was not successfully installed.");
        }

        [Test]
        public void CommandLine_InstallMultipleFonts_ShouldSucceed()
        {
            // Arrange
            var randomFontPaths = GetRandomTestFontPaths(3);
            var args = new[] { "install" }.Concat(randomFontPaths).ToArray();

            // Act
            var result = FontRegister.Program.Main(args);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            foreach (var fontPath in randomFontPaths)
            {
                Assert.IsTrue(IsFontInstalled(Path.GetFileNameWithoutExtension(fontPath)), $"Font {fontPath} was not successfully installed.");
            }
        }
        
        [Test]
        public void CommandLine_InstallSameFontTwice_ShouldReturnSuccessAndWarn()
        {
            // Arrange
            string randomFontPath = GetRandomTestFontPath();
            var args = new[] { "install", randomFontPath };

            // Act
            var firstResult = FontRegister.Program.Main(args);
            var secondResult = FontRegister.Program.Main(args);

            // Assert
            Assert.That(firstResult, Is.EqualTo(0), "First installation should succeed");
            Assert.That(secondResult, Is.EqualTo(0), "Second installation should succeed but warn");
            Assert.IsTrue(IsFontInstalled(Path.GetFileNameWithoutExtension(randomFontPath)), "Font should remain installed");
        }

        [Test]
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
            Assert.IsFalse(IsFontInstalled(fontName), "Font should remain uninstalled");
        }

        [Test]
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
            Assert.IsFalse(IsFontInstalled(fontName), "Font was not successfully uninstalled.");
        }

        [Test]
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

        private bool IsFontInstalled(string fontName)
        {
            //AI! retry policy 5 attempts, 100ms, 200ms, 400ms, 800ms, 1600ms
            // Check in user font directory
            if (File.Exists(Path.Combine(_userFontDirectory, fontName + ".otf")) ||
                File.Exists(Path.Combine(_userFontDirectory, fontName + ".ttf")) ||
                File.Exists(Path.Combine(_userFontDirectory, fontName + ".fon")) ||
                File.Exists(Path.Combine(_userFontDirectory, fontName + ".ttc")))
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
