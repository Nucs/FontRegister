using FontRegister.Abstraction;
using static FontRegister.WinApi;

namespace FontRegister;

public class WindowsSystemNotifier : ISystemNotifier
{
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