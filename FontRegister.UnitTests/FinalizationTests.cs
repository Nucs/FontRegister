using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Polly;

namespace FontRegister.UnitTests;

[TestFixture]
[NonParallelizable]
[Order(int.MaxValue)] // Ensures this fixture runs last
public class FinalizationTests
{
    private const string TEST_FONT_PATTERN = @"TestFont_@_?\w+";

    [Test]
    public void Empty()
    {
    }
    
    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        // Code to execute after all other test fixtures
        Console.WriteLine("Executing final cleanup after all test fixtures.");
        CleanupTestFonts("");
        CleanupFontsFolders();
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


    private void CleanupTestFonts(string testName)
    {
        var regex = new Regex(TEST_FONT_PATTERN.Replace("@", testName), RegexOptions.IgnoreCase);

        //AI! in here we must find all external fonts by registry and then remove them using TryDelete 
        
        // Clean up font files from font directory based on scope
        foreach (var file in Directory.GetFiles(FontConsts.GetLocalFontDirectory(), "*.*"))
        {
            if (regex.IsMatch(Path.GetFileNameWithoutExtension(file)))
            {
                TryDeleteFile(file, InstallationScope.User);
            }
        }

        foreach (var file in Directory.GetFiles(FontConsts.GetMachineFontDirectory(), "*.*"))
        {
            if (regex.IsMatch(Path.GetFileNameWithoutExtension(file)))
            {
                TryDeleteFile(file, InstallationScope.Machine);
            }
        }
        
        
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