using System.Runtime.InteropServices;
using Microsoft.Win32;
using FontRegister.Abstraction;

namespace FontRegister;

/// <summary>
/// Implements font installation operations for the current user's context.
/// </summary>
public class WindowsFontInstaller : IFontInstaller
{
    private readonly InstallationScope _scope;
    private readonly ISystemNotifier? _systemNotifier;

    /// <summary>
    /// Initializes a new instance of the WindowsUserFontInstaller class.
    /// </summary>
    public WindowsFontInstaller(InstallationScope scope)
    {
        _scope = scope;
    }

    /// <summary>
    /// Initializes a new instance of the WindowsUserFontInstaller class with a system notifier.
    /// </summary>
    /// <param name="systemNotifier">The system notifier to use for font change notifications.</param>
    public WindowsFontInstaller(ISystemNotifier systemNotifier, InstallationScope scope)
    {
        _systemNotifier = systemNotifier;
        _scope = scope;
    }

    public FontIdentification? IdentifyFont(string fontNameOrPath)
    {
        fontNameOrPath = TryNormalizePath(fontNameOrPath);
        var logName = Path.GetFileName(fontNameOrPath);

        var identifcation = IdentifyFontByFullPath(fontNameOrPath, logName)
                            ?? IdentifyFontByNameWithExtension(Path.GetFileName(fontNameOrPath), logName)
                            ?? IdentifyFontByNameWithoutExtension(Path.GetFileNameWithoutExtension(fontNameOrPath), logName);

        return identifcation;
    }

    /// <inheritdoc cref="IFontInstaller.InstallFont"/>
    //K:\MyFonts\TestFont_15f8920e.otf
    //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e.otf
    //C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.3231.0_x64__8wekyb3d8bbwe\TestFont_15f8920e.otf
    //./TestFont_15f8920e
    //../Fonts/TestFont_15f8920e.otf
    public (bool InstalledSuccessfully, FontIdentification? Identfication) InstallFont(string fontPath)
    {
        string? logName = null;
        FontIdentification? identifcation = null;
        try
        {
            fontPath = TryNormalizePath(fontPath);
            logName = Path.GetFileName(fontPath);

            identifcation = IdentifyFont(fontPath);
            if (identifcation != null)
            {
                Console.WriteLine($"{Path.GetFileName(identifcation.FontPath)}: Font already installed.");
                return (false, identifcation);
            }


            if (!File.Exists(fontPath))
            {
                Console.WriteLine($"{logName}: Font file path not found.");
                return (false, null);
            }

            //at this point we are sure we can install the font
            using (var currentVersion = FontRegistrationRootKey().OpenSubKey(FontConsts.ParentFontRegistryKey))
            {
                if (!currentVersion!.GetSubKeyNames().Contains(FontConsts.FontRegistryKeyName))
                {
                    currentVersion.CreateSubKey(FontConsts.FontRegistryKeyName)!.Dispose();
                }
            }

            identifcation = new FontIdentification()
            {
                FontPath = fontPath,
                FontExtension = Path.GetExtension(fontPath)
            };

            //figure and adjust font name based on file type
            var fontName = Path.GetFileNameWithoutExtension(identifcation.FontPath);
            fontName = char.ToUpper(fontName[0]) + fontName.Substring(1); //first letter capital
            identifcation.RegistryValueName = FontConsts.GetRegistryFontName(identifcation.FontExtension, fontName);
            identifcation.RegistryRawValue = Path.Combine(FontInstallationDirectory(), Path.GetFileName(identifcation.FontPath));
            
            if (identifcation.FontPath.Length > 256)
                throw new PathTooLongException($"{logName}: Font file path is too long.");
            
            //copy the font file
            int copyAttempts = 0;
            while (true)
            {
                try
                {
                    copyAttempts++;
                    if (!File.Exists(identifcation.RegistryRawValue))
                        File.Copy(identifcation.FontPath, identifcation.RegistryRawValue, overwrite: true);
                    break;
                }
                catch (Exception e)
                {
                    Thread.Sleep(100);
                    if (copyAttempts > 10)
                        throw new InvalidOperationException($"{logName}: Failed to copy font file: {e.Message}");
                }
            }

            if (WinApi.AddFontResource(identifcation.FontPath) == 0)
            {
                throw new InvalidOperationException($"{logName}: Failed to add font resource. (errorcode: {Marshal.GetLastWin32Error()})");
            }

            using (var fontsKey = FontRegistrationRootKey().OpenSubKey(FontConsts.FontRegistryKey, true)!)
            {
                if (fontsKey == null)
                {
                    throw new InvalidOperationException($"{logName}: Unable to open the fonts registry key.");
                }

                fontsKey.SetValue(identifcation.RegistryValueName, identifcation.RegistryRawValue);
            }


            Console.WriteLine($"{logName}: Font installed successfully.");

            _systemNotifier?.NotifyFontChange();
            return (true, identifcation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{logName}: Error installing font: {ex}");
            return (false, identifcation);
        }
    }

    /// <inheritdoc cref="IFontInstaller.UninstallFont"/>
    //fontNameOrPath can be:
    //K:\MyFonts\TestFont_15f8920e.otf
    //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e.otf
    //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e
    //C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.3231.0_x64__8wekyb3d8bbwe\TestFont_15f8920e.otf
    //TestFont_15f8920e
    //TestFont_15f8920e.otf
    //TestFont_15f8920e (OpenType)
    public (bool UninstalledSuccessfully, FontIdentification? Identification) UninstallFont(string fontNameOrPath)
    {
        string? logName = null; //the file name with extension
        FontIdentification? identifcation = null;
        try
        {
            // Ensure fonts registry key exists
            using (var currentVersion = FontRegistrationRootKey().OpenSubKey(FontConsts.ParentFontRegistryKey))
            {
                if (!currentVersion!.GetSubKeyNames().Contains(FontConsts.FontRegistryKeyName))
                {
                    currentVersion.CreateSubKey(FontConsts.FontRegistryKeyName)!.Dispose();
                }
            }

            fontNameOrPath = TryNormalizePath(fontNameOrPath);

            //handle full path, potentially inside local font directory passed
            if (Path.IsPathRooted(fontNameOrPath))
            {
                //this handles options:
                //K:\MyFonts\TestFont_15f8920e.otf
                //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e.otf
                //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e
                //C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.3231.0_x64__8wekyb3d8bbwe\TestFont_15f8920e.otf
                if (!fontNameOrPath.StartsWith(FontInstallationDirectory(), StringComparison.OrdinalIgnoreCase))
                {
                    //K:\MyFonts\TestFont_15f8920e.otf
                    //C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.3231.0_x64__8wekyb3d8bbwe\TestFont_15f8920e.otf
                    logName = Path.GetFileName(fontNameOrPath);
                    identifcation = IdentifyFontByFullPath(fontNameOrPath, logName);

                    if (identifcation == null)
                    {
                        //we got a full path thats outside the font directory, we search for the file name and not that file.
                        return UninstallFont(Path.HasExtension(fontNameOrPath) ? Path.GetFileName(fontNameOrPath) : Path.GetDirectoryName(fontNameOrPath)!);
                    }

                    fontNameOrPath = Path.GetFileName(identifcation.FontPath);
                }
                else
                {
                    //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e.otf
                    //C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e
                    return UninstallFont(Path.HasExtension(fontNameOrPath) ? Path.GetFileName(fontNameOrPath) : Path.GetDirectoryName(fontNameOrPath)!);
                }
            }

            //here we are left with unidentified options:
            //TestFont_15f8920e
            //TestFont_15f8920e.otf
            //TestFont_15f8920e (OpenType)
            var fileName = fontNameOrPath;
            if (identifcation == null && Path.HasExtension(fileName))
            {
                //TestFont_15f8920e.otf
                logName = fileName;
                //we received a font inside the font directory, we can proceed
                identifcation = IdentifyFontByNameWithExtension(fileName, logName);
                if (identifcation == null)
                {
                    //we failed to match with extension, try without extension
                    fileName = Path.GetFileNameWithoutExtension(fileName);
                }
            }

            //here we are left with unidentified options:
            //TestFont_15f8920e
            //TestFont_15f8920e (OpenType)
            if (identifcation == null)
            {
                logName ??= fileName;
                identifcation = IdentifyFontByNameWithoutExtension(fileName, logName);
            }

            //no matter how we tried to identify, we failed.
            if (identifcation == null)
            {
                //attempt cleanup deletion without registry
                identifcation = AttemptDeleteFontFile(fontNameOrPath);
                if (identifcation != null)
                    return (true, identifcation);
            }

            if (identifcation == null)
            {
                Console.WriteLine($"{logName}: Font not found.");
                return (false, null);
            }

            //at this point we have identified the font successfully in registry.

            if (!FontConsts.SupportedExtensions.Contains(identifcation.FontExtension, StringComparer.OrdinalIgnoreCase))
                Console.WriteLine($"{logName}: Warning! Unsupported font has been identified: {identifcation.FontExtension}");


            // Remove the font resource
            // usually this method takes care of both file and registry.
            if (!WinApi.RemoveFontResource(identifcation.FontPath))
                Console.WriteLine($"{logName}: Failed to remove font resource. (errorcode: {Marshal.GetLastWin32Error()})");

            // Remove from current user registry
            using (var fontsKey = FontRegistrationRootKey().OpenSubKey(FontConsts.FontRegistryKey, true))
            {
                fontsKey!.DeleteValue(identifcation.RegistryValueName);
            }

            // Delete the font file
            if (!File.Exists(identifcation.FontPath))
            {
                Console.WriteLine($"{logName}: Font file not found.");
            }
            else if (identifcation.FontPath.StartsWith(FontInstallationDirectory(), StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteFile(identifcation.FontPath, logName);
            }

            Console.WriteLine($"{logName}: Font uninstalled successfully.");

            _systemNotifier?.NotifyFontChange();
            return (true, identifcation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{logName}: Error uninstalling font: {ex.Message}");
            return (false, identifcation);
        }
    }

    private static void TryDeleteFile(string fontPath, string? logName)
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
                if (deletionAttempts > 100) //totalling 10 seconds
                    throw new InvalidOperationException($"{logName}: Failed to delete font file: {e.Message}");
            }
        }
    }

    private FontIdentification? AttemptDeleteFontFile(string fontNameOrPath)
    {
        FontIdentification? identification = null;
        fontNameOrPath = TryNormalizePath(fontNameOrPath);
        var logName = Path.IsPathRooted(fontNameOrPath) ? Path.GetFileName(fontNameOrPath) : fontNameOrPath;

        //some fonts might not be registered but still exist in the font directory
        var fontPath = Path.Combine(FontInstallationDirectory(), fontNameOrPath);

        //if file doesn't exist, we failed to identify.
        if (!File.Exists(fontPath))
            return null;

        identification = new FontIdentification()
        {
            FontPath = fontPath,
            FontExtension = Path.GetExtension(fontPath)
        };

        //we try to remove softly, it will likely fail but we try anyways.
        WinApi.RemoveFontResource(identification.FontPath);

        //delete the detected file
        TryDeleteFile(identification.FontPath, logName);
        Console.WriteLine($"{logName}: Font uninstalled successfully.");

        return identification;
    }

    private static string TryNormalizePath(string fontNameOrPath)
    {
        if (fontNameOrPath.Contains("\\") || fontNameOrPath.Contains("/") || Path.IsPathRooted(fontNameOrPath) || fontNameOrPath.Contains("..") || fontNameOrPath.StartsWith("."))
            fontNameOrPath = Path.GetFullPath(fontNameOrPath).Replace("/", "\\"); //normalize

        if (Path.HasExtension(fontNameOrPath))
            fontNameOrPath = Path.ChangeExtension(fontNameOrPath, Path.GetExtension(fontNameOrPath).ToLower());

        return fontNameOrPath;
    }

    private FontIdentification? IdentifyFontByNameWithExtension(string fileNameWithExtension, string logName)
    {
        //fileNameWithExtension can be:
        //TestFont_15f8920e.otf
        using var fontsKey = FontRegistrationRootKey().OpenSubKey(FontConsts.FontRegistryKey, true);
        var fileWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);

        if (fontsKey == null)
            throw new InvalidOperationException($"{logName}: Unable to open the fonts registry key.");

        var fullyQualifiedFontPath = Path.GetFullPath(Path.Combine(FontInstallationDirectory(), fileNameWithExtension)).Replace("/", "\\"); //normalize


        var installedFontsNames = fontsKey.GetValueNames();
        var registryValueName =
            //first pass we try to match exact names
            installedFontsNames
                .FirstOrDefault(regName =>
                {
                    var regValue = (string?)fontsKey.GetValue(regName);
                    if (string.IsNullOrWhiteSpace(regValue))
                        return false;

                    regValue = TryNormalizePath(regValue);

                    //match by TestFont_15f8920e.otf
                    return regValue.Equals(fileNameWithExtension, StringComparison.OrdinalIgnoreCase)
                           //match by TestFont_15f8920e
                           || regValue.Equals(fileWithoutExtension, StringComparison.OrdinalIgnoreCase)
                           //match by C:\Users\ELI\AppData\Local\Microsoft\Windows\Fonts\TestFont_15f8920e.otf
                           || regValue.Equals(fullyQualifiedFontPath, StringComparison.OrdinalIgnoreCase);
                });

        if (string.IsNullOrWhiteSpace(registryValueName))
            return null;

        var registryValueForFontPath = (string?)fontsKey.GetValue(registryValueName);
        if (registryValueForFontPath == null)
            return null;

        return new FontIdentification()
        {
            RegistryValueName = registryValueName,
            RegistryRawValue = (string?)fontsKey.GetValue(registryValueName),
            FontPath = TryNormalizePath(registryValueForFontPath),
            FontExtension = Path.GetExtension(registryValueForFontPath)
        };
    }

    private FontIdentification? IdentifyFontByFullPath(string fullPathToFont, string logName)
    {
        //fileNameWithExtension can be:
        //K:\MyFonts\TestFont_15f8920e.otf
        //C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.3231.0_x64__8wekyb3d8bbwe\TestFont_15f8920e.otf

        using var fontsKey = FontRegistrationRootKey().OpenSubKey(FontConsts.FontRegistryKey, true);

        if (fontsKey == null)
            throw new InvalidOperationException($"{logName}: Unable to open the fonts registry key.");


        var installedFontsNames = fontsKey.GetValueNames();
        var registryValueName =
            //first pass we try to match exact names
            installedFontsNames
                .FirstOrDefault(regName =>
                {
                    var regValue = (string?)fontsKey.GetValue(regName);
                    if (string.IsNullOrWhiteSpace(regValue))
                        return false;

                    regValue = TryNormalizePath(regValue);

                    //match by:
                    //K:\MyFonts\TestFont_15f8920e.otf
                    //C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.3231.0_x64__8wekyb3d8bbwe\TestFont_15f8920e.otf
                    return regValue.Equals(fullPathToFont, StringComparison.OrdinalIgnoreCase);
                });

        if (string.IsNullOrWhiteSpace(registryValueName))
            return null;

        var registryValueForFontPath = (string?)fontsKey.GetValue(registryValueName);
        if (registryValueForFontPath == null)
            return null;

        return new FontIdentification()
        {
            RegistryValueName = registryValueName,
            RegistryRawValue = (string?)fontsKey.GetValue(registryValueName),
            FontPath = TryNormalizePath(registryValueForFontPath),
            FontExtension = Path.GetExtension(registryValueForFontPath)
        };
    }

    private FontIdentification? IdentifyFontByNameWithoutExtension(string fontNameWithoutExtension, string logName)
    {
        //fontNameWithoutExtension can be:
        //TestFont_15f8920e
        //TestFont_15f8920e (OpenType)
        using var fontsKey = FontRegistrationRootKey().OpenSubKey(FontConsts.FontRegistryKey, true);

        if (fontsKey == null)
            throw new InvalidOperationException($"{logName}: Unable to open the fonts registry key.");

        var installedFontsNames = fontsKey.GetValueNames();

        //1. search by Name with fileNameWithoutExtension, case-insensitive

        // Attempt deleting when registry value is just the file name or full path
        var registryValueName =
            //first pass we try to match exact names
            installedFontsNames
                .FirstOrDefault(regName => string.Equals(regName, fontNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            //second pass we try to match by start of the name
            ?? installedFontsNames
                .FirstOrDefault(regName => regName.ToLower().StartsWith(fontNameWithoutExtension.ToLower()));

        if (string.IsNullOrWhiteSpace(registryValueName))
            return null;

        var registryValueForFontPath = (string?)fontsKey.GetValue(registryValueName);
        if (registryValueForFontPath == null)
            return null;

        if (!Path.IsPathRooted(registryValueForFontPath))
            registryValueForFontPath = Path.Combine(FontInstallationDirectory(), registryValueForFontPath);

        if (Path.HasExtension(registryValueForFontPath))
            registryValueForFontPath = Path.ChangeExtension(registryValueForFontPath, Path.GetExtension(registryValueForFontPath).ToLower());

        return new FontIdentification()
        {
            RegistryValueName = registryValueName,
            RegistryRawValue = (string?)fontsKey.GetValue(registryValueName),
            FontPath = registryValueForFontPath,
            FontExtension = Path.GetExtension(registryValueForFontPath)
        };
    }

    private string FontInstallationDirectory()
    {
        return _scope switch
        {
            InstallationScope.User => FontConsts.GetLocalFontDirectory(),
            InstallationScope.Machine => FontConsts.GetMachineFontDirectory(),
            _ => throw new ArgumentOutOfRangeException(nameof(_scope), _scope, null)
        };
    }

    private RegistryKey FontRegistrationRootKey()
    {
        return _scope switch
        {
            InstallationScope.User => Registry.CurrentUser,
            InstallationScope.Machine => Registry.LocalMachine,
            _ => throw new ArgumentOutOfRangeException(nameof(_scope), _scope, null)
        };
    }
}