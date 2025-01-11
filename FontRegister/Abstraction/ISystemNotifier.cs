namespace FontRegister.Abstraction;

/// <summary>
/// Defines the contract for notifying the system about font changes.
/// </summary>
public interface ISystemNotifier
{
    /// <summary>
    /// Notifies the system that font collection has changed.
    /// This triggers a refresh of the font cache and updates running applications.
    /// </summary>
    void NotifyFontChange();
}
