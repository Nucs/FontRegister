namespace FontRegister.UnitTests;

public class Program
{
    public static void Main()
    {
//single file
var notifier = new WindowsFontInstaller(new WindowsSystemNotifier());
notifier.InstallFont("C:/myfonts/myfont.ttf");

//in bulk
var fontManager = new FontManager(notifier);
fontManager.InstallFonts(new string[] { "C:/myfonts", "C:/myfonts2/myfont.ttf" });
        
    }
}