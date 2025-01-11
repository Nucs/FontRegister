using System.Runtime.InteropServices;
using System.Security.Principal;

namespace FontRegister;

internal static class WinApi
{
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
    
    
    public static bool CheckAdministratorAccess()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}