using System.Runtime.InteropServices;
using Microsoft.Win32;
using FontRegister.Abstraction;

namespace FontRegister;
public class WindowsMachineFontInstaller : IFontInstaller
{
    private readonly ISystemNotifier? _systemNotifier;

    public WindowsMachineFontInstaller()
    {
    }

    public WindowsMachineFontInstaller(ISystemNotifier systemNotifier)
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

            var fontDir = FontConsts.GetMachineFontDirectory();

            //check if font already installed, our normalized version vs given version
            if (File.Exists(Path.Combine(fontDir, fileName)) || File.Exists(Path.Combine(fontDir, Path.GetFileName(fontPath))))
            {
                Console.WriteLine($"{fileName}: Font already installed.");
                return false;
            }

            // Adjust font name based on file type
            var registryFontName = FontConsts.GetRegistryFontName(fileExtension, fontName);

            var destPath = Path.Combine(fontDir, fileName);

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
            if (WinApi.AddFontResource(destPath) == 0)
            {
                throw new InvalidOperationException($"{fileName}: Failed to add font resource.");
            }

            using (var currentVersion = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (!currentVersion!.GetSubKeyNames().Contains(FontConsts.FontRegistryKeyName))
                {
                    currentVersion.CreateSubKey(FontConsts.FontRegistryKeyName)!.Dispose();
                }
            }

            // Add to current machine registry
            using (var fontsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true)!)
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

            var fontDir = FontConsts.GetMachineFontDirectory();
            //handle full path inside local font directory passed
            if (Path.IsPathRooted(fontNameOrPath))
            {
                fileName = Path.GetFileName(fontNameOrPath);
                fileName = Path.ChangeExtension(fileName, Path.GetExtension(fileName)?.ToLower());
                fontNameOrPath = Path.GetFullPath(fontNameOrPath).Replace("/", "\\"); //normalize
                fontPath = fontNameOrPath;
                if (!fontNameOrPath.StartsWith(fontDir, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"{fileName}: Cannot uninstall fonts outside the local font directory, path: {fontNameOrPath}, expected: {Path.Combine(fontDir, fontNameOrPath)}");
            }
            else
            {
                //handle just font name passed, search in local font directory for all supported extensions
                if (!Path.HasExtension(fontNameOrPath))
                {
                    fileName = fontNameOrPath; //for error message, expected to be replaced
                    foreach (var ext in FontConsts.SupportedExtensions)
                    {
                        var potentialFileName = fontNameOrPath + ext;
                        var potentialPath = Path.Combine(fontDir, potentialFileName);
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
                    fontPath = Path.Combine(fontDir, fontNameOrPath);
                    if (!FontConsts.SupportedExtensions.Contains(Path.GetExtension(fontNameOrPath), StringComparer.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"{fileName}: Unsupported font extension: {Path.GetExtension(fontNameOrPath)}");
                }
            }

            var success = false;

            // Remove the font resource
            // usually this method takes care of both file and registry.
            //RemoveFontResourceW always returns true so success relies if file existed before that
            if (fontPath != null && !WinApi.RemoveFontResource(fontPath))
            {
                //read error
                var error = Marshal.GetLastWin32Error();
                Console.WriteLine($"{fileName}: Failed to remove font resource. (errorcode: {error})");
            }
            else
            {
                success = true;
            }

            // Delete the font file
            if (fontPath == null || !File.Exists(fontPath))
            {
                if (!success)
                    Console.WriteLine($"{fileName}: Font file not found.");
            }
            else
            {
                int deletionAttempts = 0;
                while (true)
                {
                    try
                    {
                        deletionAttempts++;
                        if (File.Exists(fontPath))
                        {
                            File.Delete(fontPath);
                            success = true;
                        }

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
            }

            // Ensure fonts registry key exists
            using (var currentVersion = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (!currentVersion!.GetSubKeyNames().Contains(FontConsts.FontRegistryKeyName))
                {
                    currentVersion.CreateSubKey(FontConsts.FontRegistryKeyName)!.Dispose();
                }
            }

            // Remove from current machine registry
            using (var fontsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true)!)
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
                    success = true;
                }
                else
                {
                    if (!success)
                        Console.WriteLine($"{fileName}: Font not found in registry.");
                }
            }

            if (success)
                Console.WriteLine($"{fileName}: Font uninstalled successfully.");
            else
                Console.WriteLine($"{fileName}: Font not found anywhere and is probably uninstalled.");

            _systemNotifier?.NotifyFontChange();
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{fileName}: Error uninstalling font: {ex.Message}");
            return false;
        }
    }
}
