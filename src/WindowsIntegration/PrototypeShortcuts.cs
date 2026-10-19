namespace WinNativeFileU.WindowsIntegration;

public enum PrototypeShortcutProfile { FunctionKeys, Letters }

internal sealed record PrototypeShortcuts(string Name, uint Modifiers, uint ActivationKey,
    uint RejectionKey, string ActivationLabel, string RejectionLabel)
{
    // Only these explicit fixture-prototype bindings may be registered. No key capture.
    internal static PrototypeShortcuts For(PrototypeShortcutProfile profile) => profile switch
    {
        PrototypeShortcutProfile.FunctionKeys => new("function-keys", 0x4000, 0x77, 0x78, "F8", "F9"),
        PrototypeShortcutProfile.Letters => new("letters", 0x4003, 0x44, 0x58, "Ctrl+Alt+D", "Ctrl+Alt+X"),
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown prototype shortcut profile.")
    };
}
