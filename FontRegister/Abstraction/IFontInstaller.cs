namespace FontRegister.Abstraction;

public interface IFontInstaller
{
    bool InstallFont(string fontPath);
    bool UninstallFont(string fontNameOrPath);
}