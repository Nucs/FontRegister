namespace FontRegister;

public static class FontConsts
{
    /// <summary>
    /// The supported fonts as per https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-addfontresourcew
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".ttf", ".otf", ".fon", ".ttc", ".fnt" };

    public static string GetLocalFontDirectory()
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
}