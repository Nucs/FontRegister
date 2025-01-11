﻿namespace FontRegister.Abstraction;

/// <summary>
/// Defines the contract for font installation operations.
/// </summary>
public interface IFontInstaller
{
    /// <summary>
    /// Installs a font from the specified file path.
    /// </summary>
    /// <param name="fontPath">The full path to the font file to install.</param>
    /// <returns>True if installation was successful, false otherwise.</returns>
    bool InstallFont(string fontPath);

    /// <summary>
    /// Uninstalls a font using either its name or file path.
    /// </summary>
    /// <param name="fontNameOrPath">The font name or full path to uninstall.</param>
    /// <returns>True if uninstallation was successful, false otherwise.</returns>
    bool UninstallFont(string fontNameOrPath);
}
