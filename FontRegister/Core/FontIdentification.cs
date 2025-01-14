namespace FontRegister;

public class FontIdentification
{
    /// <summary>
    /// Font file inside the fonts folder.
    /// </summary>
    public string FontPath { get; set; }

    /// <summary>
    /// The font type of the identified font.
    /// </summary>
    /// <example>.ttf, .otf and such</example>
    public string FontExtension { get; set; }

    /// <summary>
    /// The name of the value inside Software\Microsoft\Windows NT\CurrentVersion\Fonts registry.
    /// </summary>
    /// <remarks>AKA FontName</remarks>
    public string RegistryValueName { get; set; }

    /// <summary>
    /// Non-normalized value from registry, contains the font file name or full path to it.
    /// </summary>
    public string? RegistryRawValue { get; set; }
}