namespace FontRegister.Abstraction;

/// <summary>
/// Defines the contract for font installation operations.
/// </summary>
public interface IFontInstaller
{
    /// <summary>
    /// Installs a font from the specified file path.
    /// </summary>
    /// <param name="fontPath">The full path to the font file to install.</param>
    /// <param name="installAsExternalFontPath">
    /// If true, the installation will assign the given <paramref name="fontPath"/> as the font location.<br/>
    /// If false, the font will be copied to the system font directory and installed from there.
    /// </param>
    /// <returns>True if installation was successful, false otherwise.</returns>
    (bool InstalledSuccessfully, FontIdentification? Identfication) InstallFont(string fontPath, bool installAsExternalFontPath);

    /// <summary>
    /// Uninstalls a font using either its name or file path.
    /// </summary>
    /// <param name="fontNameOrPath">The font name or full path to uninstall.</param>
    /// <returns>True if uninstallation was successful, false otherwise.</returns>
    (bool UninstalledSuccessfully, FontIdentification? Identification) UninstallFont(string fontNameOrPath);
}
