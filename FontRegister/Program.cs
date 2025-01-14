using System.Security.Principal;
using FontRegister.Abstraction;

namespace FontRegister;

/// <summary>
/// Main entry point for the Font Registration utility.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point that handles font installation and uninstallation commands.
    /// </summary>
    /// <param name="args">Command line arguments for specifying operations and font paths.</param>
    /// <returns>0 for success, 1 for errors or failures.</returns>
    public static int Main(string[] args)
    {
        try
        {
            if (!WinApi.CheckAdministratorAccess())
            {
                throw new UnauthorizedAccessException("This tool requires administrator privileges to manage fonts.");
            }

            if (args.Length < 1)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLower().TrimStart('-').TrimStart();

            // Default to user scope
            bool? useMachineWide = null;
            bool restartFontCache = false;
            var remainingArgs = new List<string>();

            // Parse scope flags and collect remaining arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                switch (arg)
                {
                    case "install":
                    case "uninstall":
                        continue;
                    case "--machine":
                    case "-m":
                    case "--all-users":
                        if (useMachineWide.HasValue)
                        {
                            Console.WriteLine("Please specify only one of --user or --machine.");
                            PrintUsage();
                            return 1;
                        }

                        useMachineWide = true;
                        break;
                    case "--user":
                    case "-u":
                        if (useMachineWide.HasValue)
                        {
                            Console.WriteLine("Please specify only one of --user or --machine.");
                            PrintUsage();
                            return 1;
                        }

                        useMachineWide = false;
                        break;
                    case "--restart-font-cache":
                    case "--clear-cache":
                        restartFontCache = true;
                        break;
                    default:
                        remainingArgs.Add(args[i]);
                        break;
                }
            }

            useMachineWide ??= false;

            if (remainingArgs.Count == 0 && restartFontCache)
            {
                WinApi.RestartAndClearFontCacheService();
                Console.WriteLine("Windows Font Cache service restarted.");
                return 0;
            }

            // Create the appropriate installer based on scope
            ISystemNotifier systemNotifier = new WindowsSystemNotifier();
            IFontInstaller installer = useMachineWide.Value
                ? new WindowsFontInstaller(systemNotifier, InstallationScope.Machine)
                : new WindowsFontInstaller(systemNotifier, InstallationScope.User);

            FontManager fontManager = new FontManager(installer);

            try
            {
                switch (command)
                {
                    case "install":
                        if (!remainingArgs.Any())
                        {
                            Console.WriteLine("Please provide at least one file or directory path for installation.");
                            PrintUsage();
                            return 1;
                        }

                        fontManager.InstallFonts(remainingArgs.ToArray());

                        break;
                    case "uninstall":
                        if (!remainingArgs.Any())
                        {
                            Console.WriteLine("Please provide at least one font name for uninstallation.");
                            PrintUsage();
                            return 1;
                        }

                        fontManager.UninstallFonts(remainingArgs.ToArray());
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
            finally
            {
                if (restartFontCache)
                {
                    WinApi.RestartAndClearFontCacheService();
                    Console.WriteLine("Windows Font Cache service restarted.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }

        return 0;
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: FontManager <command> [options] [paths...]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  install <path1> [path2] [path3] ... : Install fonts from specified files or directories");
        Console.WriteLine("  uninstall <fontName1> [fontName2] [fontName3] ... : Uninstall specified fonts");
        Console.WriteLine("Options:");
        Console.WriteLine("  --user, -u        : Install for current user only (default)");
        Console.WriteLine("  --machine, -m     : Install for all users (requires admin rights)");
        Console.WriteLine("  --all-users       : Same as --machine");
        Console.WriteLine("  --clear-cache, --restart-font-cache");
        Console.WriteLine("                    : Restart the Windows Font Cache service after operation");
        Console.WriteLine("                      refreshing font list and removing cached uninstalled fonts.");
        Console.WriteLine("                      This command physically deletes %LOCALAPPDATA%\\**\\FontCache directories");
    }
}
