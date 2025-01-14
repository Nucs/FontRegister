using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Diagnostics;

namespace FontRegister;

/// <summary>
/// Provides access to Windows API functions for font management and system notifications.
/// </summary>
internal static class WinApi
{
    /// <summary>
    /// Restarts the Windows Font Cache Service using command line
    /// </summary>
    /// <returns>True if service was successfully restarted, false otherwise</returns>
    public static bool RestartAndClearFontCacheService()
    {
        try
        {
            // Stop the service
            var stopProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "stop FontCache",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            stopProcess.Start();
            stopProcess.WaitForExit(30000); // 30 second timeout

            foreach (var cache in FontConsts.GetFontCacheDirectories())
            {
                int deletionAttempts = 0;
                while (true)
                {
                    deletionAttempts++;
                    try
                    {
                        if (Directory.Exists(cache))
                        {
                            Directory.Delete(cache, true);
                        }

                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        if (deletionAttempts > 3)
                        {
                            Console.WriteLine($"Failed to delete font cache directory {cache} entirely, but most of the cache are deleted. Some application is locking some of the files.");
                            break;
                        }
                    }
                }
            }

            {
                var cache = Path.Combine(FontConsts.GetLocalFontDirectory(), "Deleted");
                int deletionAttempts = 0;
                while (true)
                {
                    deletionAttempts++;
                    try
                    {
                        if (Directory.Exists(cache))
                        {
                            Directory.Delete(cache, true);
                        }

                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        if (deletionAttempts > 3)
                        {
                            Console.WriteLine($"Failed to delete font cache directory {cache}");
                            break;
                        }
                    }
                }
            }

            {
                var cache = Path.Combine(FontConsts.GetMachineFontDirectory(), "Deleted");
                int deletionAttempts = 0;
                while (true)
                {
                    deletionAttempts++;
                    try
                    {
                        if (Directory.Exists(cache))
                        {
                            Directory.Delete(cache, true);
                        }

                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        if (deletionAttempts > 3)
                        {
                            Console.WriteLine($"Failed to delete font cache directory {cache}");
                            break;
                        }
                    }
                }
            }

            // Start the service
            var startProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "start FontCache",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            startProcess.Start();
            startProcess.WaitForExit(30000); // 30 second timeout

            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "AddFontResourceW")]
    public static extern int AddFontResource(string lpszFilename);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RemoveFontResourceW")]
    public static extern bool RemoveFontResource(string lpszFilename);

    public const int HWND_BROADCAST = 0xffff;
    public const uint WM_FONTCHANGE = 0x001D;
    public const uint SMTO_NORMAL = 0x0000;
    public const uint FontChangeFlags = 0;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        UIntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult);

    [DllImport("shell32.dll")]
    public static extern int SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);


    /// <summary>
    /// Checks if the current process has administrator privileges.
    /// </summary>
    /// <returns>True if running with administrator privileges, false otherwise.</returns>
    public static bool CheckAdministratorAccess()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}