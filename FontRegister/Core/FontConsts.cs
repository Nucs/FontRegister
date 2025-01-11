namespace FontRegister;

/// <summary>
/// Contains constant values and utility methods for font management.
/// </summary>
public static class FontConsts
{
    /// <summary>
    /// The supported fonts as per https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-addfontresourcew
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".ttf", ".otf", ".fon", ".ttc", ".fnt" };

    public const string FontRegistryKeyName = "Fonts";
    public const string FontRegistryKey = @"Software\Microsoft\Windows NT\CurrentVersion\Fonts";
    public const string ParentFontRegistryKey = @"Software\Microsoft\Windows NT\CurrentVersion";

    /// <summary>
    /// Gets the path to the current user's font directory.
    /// </summary>
    /// <returns>The full path to the local fonts directory.</returns>
    /// <remarks>
    /// Creates the directory if it doesn't exist.
    /// The path is normalized to use backslashes for Windows compatibility.
    /// </remarks>
    public static string GetLocalFontDirectory()
    {
        var fontDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Fonts"
        );

        //normalize path
        fontDir = Path.GetFullPath(fontDir).Replace("/", "\\");

        Directory.CreateDirectory(fontDir);
        return fontDir;
    }

    /// <summary>
    /// Gets the path to the Windows system font directory.
    /// </summary>
    /// <returns>The full path to the Windows Fonts directory.</returns>
    /// <remarks>
    /// Creates the directory if it doesn't exist.
    /// The path is normalized to use backslashes for Windows compatibility.
    /// Typically located at C:\Windows\Fonts.
    /// </remarks>
    public static string GetMachineFontDirectory()
    {
        var machineFontDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Fonts"
        );

        //normalize path
        machineFontDir = Path.GetFullPath(machineFontDir).Replace("/", "\\");

        Directory.CreateDirectory(machineFontDir);
        return machineFontDir;
    }
    
    public static IEnumerable<string> GetFontCacheDirectories()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var results = new List<string>();
        
        void SearchDirectory(string directory)
        {
            try
            {
                // Check current directory for FontCache folders
                foreach (var dir in Directory.GetDirectories(directory, "FontCache"))
                {
                    results.Add(Path.GetFullPath(dir).Replace("/", "\\"));
                }

                // Recursively search subdirectories
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    try
                    {
                        SearchDirectory(dir);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip directories we can't access
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
                return;
            }
        }

        SearchDirectory(localAppData);
        return results;
    }


    /// <summary>
    /// Formats the font name for registry storage based on its file extension.
    /// </summary>
    /// <param name="fileExtension">The font file extension (.ttf, .otf, etc.)</param>
    /// <param name="fontName">The base name of the font</param>
    /// <returns>The formatted font name for registry storage</returns>
    /// <remarks>
    /// Different font types get different suffixes in the registry:
    /// - OpenType (.otf) fonts get "(OpenType)"
    /// - TrueType Collection (.ttc) fonts get "(TrueType)"
    /// - Raster fonts (.fon) get "(VGA res)"
    /// - Bitmap fonts (.fnt) use the name as-is
    /// This matches Windows' default font installer behavior.
    /// </remarks>
    public static string GetRegistryFontName(string fileExtension, string fontName)
    {
        return fileExtension switch
        {
            //similar behavior in default windows font installer
            ".otf" => $"{fontName} (OpenType)",
            ".ttc" => $"{fontName} (TrueType)",
            ".fon" => $"{fontName} (VGA res)",
            ".fnt" => fontName,
            _ => fontName
        };
    }
}
