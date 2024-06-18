using System.Security.Principal;
using FontRegister.Abstraction;

namespace FontRegister;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (!CheckAdministratorAccess())
            {
                throw new UnauthorizedAccessException("This tool requires administrator privileges to manage fonts.");
            }

            ISystemNotifier systemNotifier = new WindowsSystemNotifier();
            IFontInstaller fontInstaller = new WindowsFontInstaller(systemNotifier);

            FontManager fontManager = new FontManager(fontInstaller);

            if (args.Length < 1)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLower().TrimStart('-').TrimStart();

            switch (command)
            {
                case "install":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide at least one file or directory path for installation.");
                        PrintUsage();
                        return 1;
                    }
                    fontManager.InstallFonts(args.Skip(1).ToArray());
                    break;
                case "uninstall":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide at least one font name for uninstallation.");
                        PrintUsage();
                        return 1;
                    }
                    fontManager.UninstallFonts(args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine("Invalid command. Use install or uninstall.");
                    PrintUsage();
                    return 1;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
        
        return 0;
    }

    static bool CheckAdministratorAccess()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: FontManager <command> [paths...]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  install <path1> [path2] [path3] ... : Install fonts from specified files or directories");
        Console.WriteLine("  uninstall <fontName1> [fontName2] [fontName3] ... : Uninstall specified fonts");
    }
}