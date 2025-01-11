﻿using System.Security.Principal;
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

            if (args.Length < 1)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLower().TrimStart('-').TrimStart();
            
            // Default to user scope
            bool useMachineWide = false;
            var remainingArgs = new List<string>();

            // Parse scope flags and collect remaining arguments
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                switch (arg)
                {
                    case "--machine":
                    case "-m":
                    case "--all-users":
                        useMachineWide = true;
                        break;
                    case "--user":
                    case "-u":
                        useMachineWide = false;
                        break;
                    default:
                        remainingArgs.Add(args[i]);
                        break;
                }
            }

            // Create the appropriate installer based on scope
            ISystemNotifier systemNotifier = new WindowsSystemNotifier();
            IFontInstaller installer = useMachineWide 
                ? new WindowsMachineFontInstaller(systemNotifier)
                : new WindowsUserFontInstaller(systemNotifier);

            FontManager fontManager = new FontManager(installer);

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
        Console.WriteLine("Usage: FontManager <command> [options] [paths...]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  install <path1> [path2] [path3] ... : Install fonts from specified files or directories");
        Console.WriteLine("  uninstall <fontName1> [fontName2] [fontName3] ... : Uninstall specified fonts");
        Console.WriteLine("Options:");
        Console.WriteLine("  --user, -u        : Install for current user only (default)");
        Console.WriteLine("  --machine, -m     : Install for all users (requires admin rights)");
        Console.WriteLine("  --all-users       : Same as --machine");
    }
}
