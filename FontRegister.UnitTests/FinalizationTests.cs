using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Polly;

namespace FontRegister.UnitTests;

[TestFixture]
[NonParallelizable]
[Order(int.MaxValue)] // Ensures this fixture runs last
public class FinalizationTests
{
    [Test]
    public void Empty()
    {
    }
    
    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        // Code to execute after all other test fixtures
        Console.WriteLine("Executing final cleanup after all test fixtures.");
        CleanupTestFonts();
        CleanupFontsFolders();
    }

    private void CleanupTestFonts()
    {
        // Clean up external fonts from registry
        using (var userKey = Registry.CurrentUser.OpenSubKey(FontConsts.FontRegistryKey))
        using (var machineKey = Registry.LocalMachine.OpenSubKey(FontConsts.FontRegistryKey))
        {
            if (userKey != null)
            {
                foreach (var fontName in userKey.GetValueNames())
                {
                    if (fontName.StartsWith("TestFont_"))
                    {
                        TryDeleteFile(fontName, InstallationScope.User);
                    }
                }
            }

            if (machineKey != null)
            {
                foreach (var fontName in machineKey.GetValueNames())
                {
                    if (fontName.StartsWith("TestFont_"))
                    {
                        TryDeleteFile(fontName, InstallationScope.Machine);
                    }
                }
            }
        }
        
        // Clean up font files from font directories in parallel
        var userFiles = Directory.GetFiles(FontConsts.GetLocalFontDirectory(), "TestFont_*.*");
        var machineFiles = Directory.GetFiles(FontConsts.GetMachineFontDirectory(), "TestFont_*.*");

        Parallel.ForEach(userFiles, file =>
        {
            TryDeleteFile(file, InstallationScope.User);
        });

        Parallel.ForEach(machineFiles, file =>
        {
            TryDeleteFile(file, InstallationScope.Machine);
        });
    }

    private void CleanupFontsFolders() 
    {
        Parallel.ForEach(Directory.GetDirectories(Environment.ExpandEnvironmentVariables("%TEMP%"), "TestFonts_*", SearchOption.TopDirectoryOnly), directory =>
        {
            Policy.Handle<Exception>()
                .WaitAndRetry(10, _ => TimeSpan.FromMilliseconds(10))
                .Execute(() =>
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                });
        });
    }

    private void TryDeleteFile(string filePath, InstallationScope scope)
    {
        try
        {
            if (scope == InstallationScope.Machine)
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
}
