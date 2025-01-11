using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;

namespace FontRegister;
/// <summary>
/// Provides access to Windows API functions for font management and system notifications.
/// </summary>
internal static class WinApi
{
    /// <summary>
    /// Restarts the Windows Font Cache Service
    /// </summary>
    /// <returns>True if service was successfully restarted, false otherwise</returns>
    public static bool RestartFontCacheService()
    {
        try
        {
            using (var service = new ServiceController("FontCache"))
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                return true;
            }
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
