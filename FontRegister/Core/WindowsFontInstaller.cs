using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Linq;
using FontRegister.Abstraction;

namespace FontRegister;

public class WindowsFontInstaller : IFontInstaller
{
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

    public void InstallFont(string fontPath)
    {
        try
        {
            string fileName = Path.GetFileName(fontPath);
            string fileExtension = Path.GetExtension(fontPath).ToLower();
            string fontName = Path.GetFileNameWithoutExtension(fontPath);

            // Adjust font name based on file type
            string registryFontName = fileExtension == ".otf" //similar behavior in default windows font installer
                ? $"{fontName} (OpenType)"
                : fontName;

            string localFontDir = GetLocalFontDirectory();
            string destPath = Path.Combine(localFontDir, fileName);

            // Copy the font file
            File.Copy(fontPath, destPath, true);

            // Add the font resource
            if (AddFontResourceA(destPath) == 0)
            {
                throw new InvalidOperationException("Failed to add font resource.");
            }

            using (var currentVersion = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (!currentVersion!.GetSubKeyNames().Contains("Fonts"))
                {
                    currentVersion.CreateSubKey("Fonts")!.Dispose();
                }
            }

            // Add to current user registry
            using (RegistryKey fontsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true)!)
            {
                if (fontsKey == null)
                {
                    throw new InvalidOperationException("Unable to open the fonts registry key.");
                }

                fontsKey.SetValue(registryFontName, destPath);
            }

            Console.WriteLine($"Font {registryFontName} installed successfully.");

            _systemNotifier?.NotifyFontChange();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing font {Path.GetFileName(fontPath)}: {ex}");
        }
    }

    public void UninstallFont(string fontName)
    {
        try
        {
            string localFontDir = GetLocalFontDirectory();
            string[] possibleExtensions = { ".ttf", ".otf", ".fon", ".ttc" };
            string fontPath = null;

            foreach (var ext in possibleExtensions)
            {
                string possiblePath = Path.Combine(localFontDir, fontName + ext);
                if (File.Exists(possiblePath))
                {
                    fontPath = possiblePath;
                    break;
                }
            }

            if (fontPath == null)
            {
                Console.WriteLine($"Font {fontName} not found.");
                return;
            }

            // Remove the font resource
            if (!RemoveFontResourceA(fontPath))
            {
                //read error
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException("Failed to remove font resource. (" + error + ")");
            }

            // Delete the font file
            File.Delete(fontPath);

            // Remove from current user registry
            using (var currentVersion = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (!currentVersion!.GetSubKeyNames().Contains("Fonts"))
                {
                    currentVersion.CreateSubKey("Fonts")!.Dispose();
                }
            }

            using (RegistryKey fontsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true)!)
            {
                if (fontsKey == null)
                {
                    throw new InvalidOperationException("Unable to open the fonts registry key.");
                }

                string registryValueName = fontsKey.GetValueNames()
                    .FirstOrDefault(name => name.StartsWith(fontName, StringComparison.OrdinalIgnoreCase));

                if (registryValueName != null)
                {
                    fontsKey.DeleteValue(registryValueName);
                }
                else
                {
                    Console.WriteLine($"Font {fontName} not found in registry.");
                }
            }

            Console.WriteLine($"Font {fontName} uninstalled successfully.");

            _systemNotifier.NotifyFontChange();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling font {fontName}: {ex.Message}");
        }
    }

    private string GetLocalFontDirectory()
    {
        string localFontDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Fonts"
        );

        Directory.CreateDirectory(localFontDir);
        return localFontDir;
    }
}