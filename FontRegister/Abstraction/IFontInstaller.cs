namespace FontRegister.Abstraction;

/// <summary>
/// Defines the contract for font installation operations.
/// </summary>
public interface IFontInstaller
{
    /// <summary>
    /// Identifies a font by its name or path, returning details about its installation if found.
    /// </summary>
    /// <param name="fontNameOrPath">The font name, filename, or full path to identify. Can be:
    /// - Full path to font file (e.g. "C:/Windows/Fonts/arial.ttf")
    /// - Font filename with extension (e.g. "arial.ttf")
    /// - Font name without extension (e.g. "Arial")
    /// - Font name as shown in Windows (e.g. "Arial (TrueType)")
    /// - Relative path (e.g. "./fonts/arial.ttf")
    /// - Parent relative path (e.g. "../fonts/arial.ttf")
    /// - Forward slash path (e.g. "fonts/arial.ttf")
    /// </param>
    /// <returns>A FontIdentification object if the font is found, null otherwise.</returns>
    FontIdentification? IdentifyFont(string fontNameOrPath);
    
    /// <summary>
    /// Installs a font from the specified file path.
    /// </summary>
    /// <param name="fontPath">The full path to the font file to install. Can be:
    /// - Absolute path (e.g. "C:/Windows/Fonts/arial.ttf")
    /// - Relative path (e.g. "./fonts/arial.ttf")
    /// - Parent relative path (e.g. "../fonts/arial.ttf")
    /// - Forward slash path (e.g. "fonts/arial.ttf")
    /// The path will be normalized and converted to absolute path before installation.
    /// </param>
    /// <returns>A tuple containing:
    /// - InstalledSuccessfully: True if installation was successful, false otherwise
    /// - Identification: Font identification details if available, null if installation failed</returns>
    (bool InstalledSuccessfully, FontIdentification? Identfication) InstallFont(string fontPath);

    /// <summary>
    /// Uninstalls a font using either its name or file path.
    /// </summary>
    /// <param name="fontNameOrPath">The font name, filename, or full path to uninstall. Can be:
    /// - Full path to font file (e.g. "C:/Windows/Fonts/arial.ttf")
    /// - Font filename with extension (e.g. "arial.ttf")
    /// - Font name without extension (e.g. "Arial")
    /// - Font name as shown in Windows (e.g. "Arial (TrueType)")
    /// - Relative path (e.g. "./fonts/arial.ttf")
    /// - Parent relative path (e.g. "../fonts/arial.ttf")
    /// - Forward slash path (e.g. "fonts/arial.ttf")
    /// The path will be normalized and converted to absolute path before uninstallation.
    /// </param>
    /// <returns>A tuple containing:
    /// - UninstalledSuccessfully: True if uninstallation was successful, false otherwise
    /// - Identification: Font identification details if available, null if uninstallation failed</returns>
    (bool UninstalledSuccessfully, FontIdentification? Identification) UninstallFont(string fontNameOrPath);
}
