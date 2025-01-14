namespace FontRegister.Abstraction;

/// <summary>
/// Defines the contract for font installation operations.
/// </summary>
public interface IFontInstaller
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fontNameOrPath"></param>
    /// <returns></returns>
    FontIdentification? IdentifyFont(string fontNameOrPath);
    
    /// <summary>
    /// Installs a font from the specified file path.
    /// </summary>
    /// <param name="fontPath">The full path to the font file to install.</param>
    /// <returns>True if installation was successful, false otherwise.</returns>
    (bool InstalledSuccessfully, FontIdentification? Identfication) InstallFont(string fontPath);

    /// <summary>
    /// Uninstalls a font using either its name or file path.
    /// </summary>
    /// <param name="fontNameOrPath">The font name or full path to uninstall.</param>
    /// <returns>True if uninstallation was successful, false otherwise.</returns>
    (bool UninstalledSuccessfully, FontIdentification? Identification) UninstallFont(string fontNameOrPath);
}
