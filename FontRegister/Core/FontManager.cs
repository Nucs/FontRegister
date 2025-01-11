using FontRegister.Abstraction;

namespace FontRegister;

//TODO: support exit code

public class FontManager
{
    private readonly IFontInstaller _fontInstaller;

    public FontManager(IFontInstaller fontInstaller)
    {
        _fontInstaller = fontInstaller;
    }

    public void InstallFonts(string[] paths)
    {
        foreach (string path in paths.Select(Path.GetFullPath))
        {
            if (Directory.Exists(path))
            {
                string[] fontFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".fon", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (string fontFile in fontFiles)
                {
                    _fontInstaller.InstallFont(fontFile);
                }
            }
            else if (File.Exists(path))
            {
                _fontInstaller.InstallFont(path);
            }
            else
            {
                Console.WriteLine($"Invalid path: {path}. Skipping.");
            }
        }
    }

    public void UninstallFonts(string[] fontNames)
    {
        foreach (string fontName in fontNames)
        {
            _fontInstaller.UninstallFont(fontName);
        }
    }
}