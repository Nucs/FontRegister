using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Linq;
using FontRegister.Abstraction;

namespace FontRegister;

public class WindowsFontInstaller : IFontInstaller
{
    private static readonly string[] _supportedExtensions = { ".ttf", ".otf", ".fon", ".ttc" };

    [DllImport("gdi32.dll")]
    private static extern int AddFontResourceA(string lpszFilename);

    [DllImport("gdi32.dll")]
    private static extern bool RemoveFontResourceA(string lpszFilename);

    private readonly ISystemNotifier? _systemNotifier;

    public WindowsFontInstaller()
    {
    }

    public WindowsFontInstaller(ISystemNotifier systemNotifier)
    {
        _systemNotifier = systemNotifier;
    }

    public bool InstallFont(string fontPath)
    {
        string? fileName = null;
        try
        {
            fileName = Path.GetFileName(fontPath);
            fontPath = Path.GetFullPath(fontPath).Replace("/", "\\"); //normalize
            fontPath = Path.ChangeExtension(fontPath, Path.GetExtension(fontPath)?.ToLower());

            if (!File.Exists(fontPath))
            {
                Console.WriteLine($"{fontPath}: Font file path not found.");
                return false;
            }

            var fileExtension = Path.GetExtension(fontPath);
            var fontName = Path.GetFileNameWithoutExtension(fontPath);
            fontName = char.ToUpper(fontName[0]) + fontName.Substring(1); //first letter capital

            var localFontDir = GetLocalFontDirectory();

            //check if font already installed, our normalized version vs given version
            if (File.Exists(Path.Combine(localFontDir, fileName)) || File.Exists(Path.Combine(localFontDir, Path.GetFileName(fontPath)))) 
            {
                Console.WriteLine($"{fileName}: Font already installed.");
                return false;
            }

            // Adjust font name based on file type
            var registryFontName = fileExtension switch
            {
                //similar behavior in default windows font installer
                ".otf" => $"{fontName} (OpenType)",
                ".ttc" => $"{fontName} (TrueType)",
                ".fon" => $"{fontName} (VGA res)",
                _ => fontName
            };

            var destPath = Path.Combine(localFontDir, fileName);

            // Copy the font file
            int copyAttempts = 0;
            while (true)
            {
                try
                {
                    copyAttempts++;
                    if (!File.Exists(destPath))
                        File.Copy(fontPath, destPath, overwrite: true);
                    break;
                }
                catch (Exception e)
                {
                    Thread.Sleep(100);
                    if (copyAttempts > 10)
                        throw new InvalidOperationException($"{fileName}: Failed to copy font file: {e.Message}");
                }
            }

            // Add the font resource
            if (AddFontResourceA(destPath) == 0)
            {
                throw new InvalidOperationException($"{fileName}: Failed to add font resource.");
            }

            using (var currentVersion = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (!currentVersion!.GetSubKeyNames().Contains("Fonts"))
                {
                    currentVersion.CreateSubKey("Fonts")!.Dispose();
                }
            }

            // Add to current user registry
            using (var fontsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true)!)
            {
                if (fontsKey == null)
                {
                    throw new InvalidOperationException($"{fileName}: Unable to open the fonts registry key.");
                }

                fontsKey.SetValue(registryFontName, fontPath);
            }

            Console.WriteLine($"{fileName}: Font installed successfully.");

            _systemNotifier?.NotifyFontChange();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{fileName ?? fontPath}: Error installing font: {ex}");
            return false;
        }
    }

    public bool UninstallFont(string fontNameOrPath)
    {
        string? fileName = null; //the file name with extension
        string? fontPath = null; //the full path of the font file in local font directory
        try
        {
            if (fontNameOrPath.Contains("\\") || fontNameOrPath.Contains("/") || Path.IsPathRooted(fontNameOrPath) || fontNameOrPath.Contains(".."))
                fontNameOrPath = Path.GetFullPath(fontNameOrPath).Replace("/", "\\"); //normalize

            var localFontDir = GetLocalFontDirectory();
            //handle full path inside local font directory passed
            if (Path.IsPathRooted(fontNameOrPath))
            {
                fileName = Path.GetFileName(fontNameOrPath);
                fileName = Path.ChangeExtension(fileName, Path.GetExtension(fileName)?.ToLower());
                fontNameOrPath = Path.GetFullPath(fontNameOrPath).Replace("/", "\\"); //normalize
                if (!fontNameOrPath.StartsWith(localFontDir, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"{fileName}: Cannot uninstall fonts outside the local font directory, path: {fontNameOrPath}, expected: {Path.Combine(localFontDir, fontNameOrPath)}");
            }
            else
            {
                //handle just font name passed, search in local font directory for all supported extensions
                if (!Path.HasExtension(fontNameOrPath))
                {
                    fileName = fontNameOrPath; //for error message, expected to be replaced
                    foreach (var ext in _supportedExtensions)
                    {
                        var potentialFileName = fontNameOrPath + ext;
                        var potentialPath = Path.Combine(localFontDir, potentialFileName);
                        if (File.Exists(potentialPath))
                        {
                            if (fontPath != null)
                                throw new InvalidOperationException($"{fileName}: Multiple font files found with the same name but different extensions ({Path.GetExtension(fontPath)}, {ext}). Please specify extension.");

                            fontPath = potentialPath;
                            fileName = potentialFileName;
                        }
                    }
                }
                else
                {
                    //handle font name with extension passed
                    fileName = fontNameOrPath;
                    fileName = Path.ChangeExtension(fileName, Path.GetExtension(fileName)?.ToLower());
                    fontPath = Path.Combine(localFontDir, fontNameOrPath);
                    if (!_supportedExtensions.Contains(Path.GetExtension(fontNameOrPath), StringComparer.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"{fileName}: Unsupported font extension: {Path.GetExtension(fontNameOrPath)}");
                }
            }

            if (fontPath == null || !File.Exists(fontPath))
            {
                Console.WriteLine($"{fileName}: Font not found.");
                return false;
            }

            // Remove the font resource
            if (!RemoveFontResourceA(fontPath))
            {
                //read error
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"{fileName}: Failed to remove font resource. (errorcode: {error})");
            }

            // Delete the font file
            int deletionAttempts = 0;
            while (true)
            {
                try
                {
                    deletionAttempts++;
                    if (File.Exists(fontPath))
                        File.Delete(fontPath);
                    break;
                }
                catch (DirectoryNotFoundException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Thread.Sleep(100);
                    if (deletionAttempts > 10)
                        throw new InvalidOperationException($"{fileName}: Failed to delete font file: {e.Message}");
                }
            }

            // Ensure fonts registry key exists
            using (var currentVersion = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (!currentVersion!.GetSubKeyNames().Contains("Fonts"))
                {
                    currentVersion.CreateSubKey("Fonts")!.Dispose();
                }
            }

            // Remove from current user registry
            using (var fontsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true)!)
            {
                if (fontsKey == null)
                    throw new InvalidOperationException($"{fileName}: Unable to open the fonts registry key.");

                var installedFontsValueNames = fontsKey.GetValueNames();

                // Attempt deleting when registry value is just the file name or full path
                var registryValueName = installedFontsValueNames
                    .FirstOrDefault(regName =>
                    {
                        if (Path.IsPathRooted(regName))
                            regName = Path.GetFullPath(regName);
                        if (regName.Contains("\\"))
                            regName = regName.Replace("/", "\\");

                        return regName.Equals(fontPath, StringComparison.OrdinalIgnoreCase) ||
                               regName.ToLower().Contains(fileName.ToLower());
                    });

                if (registryValueName != null)
                {
                    fontsKey.DeleteValue(registryValueName);
                }
                else
                {
                    Console.WriteLine($"{fileName}: Font not found in registry.");
                }
            }

            Console.WriteLine($"{fileName}: Font uninstalled successfully.");

            _systemNotifier?.NotifyFontChange();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{fileName}: Error uninstalling font: {ex.Message}");
            return false;
        }
    }

    private string GetLocalFontDirectory()
    {
        var localFontDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Fonts"
        );

        //normalize path
        localFontDir = Path.GetFullPath(localFontDir).Replace("/", "\\");

        Directory.CreateDirectory(localFontDir);
        return localFontDir;
    }
}