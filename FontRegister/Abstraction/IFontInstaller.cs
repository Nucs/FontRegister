namespace FontRegister.Abstraction;

public interface IFontInstaller
{
    void InstallFont(string fontPath);
    void UninstallFont(string fontName);
}