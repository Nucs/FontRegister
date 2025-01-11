using FontRegister.Abstraction;
using static FontRegister.WinApi;

namespace FontRegister;

/// <summary>
/// Implements system notification for font changes on Windows.
/// </summary>
public class WindowsSystemNotifier : ISystemNotifier
{
    /// <summary>
    /// Notifies the Windows system and running applications about font changes.
    /// Broadcasts WM_FONTCHANGE message and updates shell.
    /// </summary>
    public void NotifyFontChange()
    {
        // Broadcast WM_FONTCHANGE message
        BroadcastFontChangeMessage();

        // Notify shell of the change
        NotifyShell();
    }

    /// <summary>
    /// Broadcasts a WM_FONTCHANGE message to all top-level windows.
    /// This notifies applications that they need to reload their font cache.
    /// </summary>
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

    /// <summary>
    /// Notifies the Windows Shell of system-wide font changes.
    /// This ensures Explorer and other shell components refresh their font lists.
    /// </summary>
    private void NotifyShell()
    {
        const int SHCNE_ASSOCCHANGED = 0x08000000;
        const uint SHCNF_IDLIST = 0x0000;
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}
