using FontRegister.Abstraction;

namespace FontRegister;

/// <summary>
/// Manages font installation and uninstallation operations using the provided font installer.
/// </summary>
public class FontManager
{
    /// <summary>
    /// The font installer implementation to use for font operations.
    /// </summary>
    private readonly IFontInstaller _fontInstaller;

    public FontManager(IFontInstaller fontInstaller)
    {
        _fontInstaller = fontInstaller;
    }

    /// <summary>
    /// Installs fonts from the specified file or directory paths.
    /// </summary>
    /// <param name="paths">Array of paths to font files or directories containing fonts.</param>
    /// <remarks>
    /// For directory paths, all supported font files (.ttf, .otf, .fon, .ttc) will be installed.
    /// Invalid paths will be skipped with a warning message.
    /// </remarks>
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
                    _fontInstaller.InstallFont(fontFile, false /*TODO: use args*/);
                }
            }
            else if (File.Exists(path))
            {
                _fontInstaller.InstallFont(path, false /*TODO: use args*/);
            }
            else
            {
                Console.WriteLine($"Invalid path: {path}. Skipping.");
            }
        }
    }

    /// <summary>
    /// Uninstalls the specified fonts by name or path.
    /// </summary>
    /// <param name="fontNames">Array of font names or paths to uninstall.</param>
    public void UninstallFonts(string[] fontNames)
    {
        foreach (string fontName in fontNames)
        {
            _fontInstaller.UninstallFont(fontName);
        }
    }
}
