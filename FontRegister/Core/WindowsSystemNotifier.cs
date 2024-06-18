using System.Runtime.InteropServices;
using FontRegister.Abstraction;

namespace FontRegister;

public class WindowsSystemNotifier : ISystemNotifier
{
    private const int HWND_BROADCAST = 0xffff;
    private const uint WM_FONTCHANGE = 0x001D;
    private const uint SMTO_NORMAL = 0x0000;
    private const uint FontChangeFlags = 0;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        UIntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult);

    [DllImport("shell32.dll")]
    private static extern int SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    public void NotifyFontChange()
    {
        // Broadcast WM_FONTCHANGE message
        BroadcastFontChangeMessage();

        // Notify shell of the change
        NotifyShell();
    }

    private void BroadcastFontChangeMessage()
    {
        SendMessageTimeout(
            (IntPtr)HWND_BROADCAST,
            WM_FONTCHANGE,
            UIntPtr.Zero,
            IntPtr.Zero,
            SMTO_NORMAL,
            1000,
            out _);
    }

    private void NotifyShell()
    {
        const int SHCNE_ASSOCCHANGED = 0x08000000;
        const uint SHCNF_IDLIST = 0x0000;
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}