namespace FontRegister;

public static class FontConsts
{
    /// <summary>
    /// The supported fonts as per https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-addfontresourcew
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".ttf", ".otf", ".fon", ".ttc", ".fnt" };

    public const string FontRegistryKeyName = "Fonts";
    public const string FontRegistryKey = @"Software\Microsoft\Windows NT\CurrentVersion\Fonts";
    public const string ParentFontRegistryKey = @"Software\Microsoft\Windows NT\CurrentVersion";

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