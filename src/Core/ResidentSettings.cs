namespace WinNativeFileU.Core;

public enum ResidentTheme { System, Light, Dark }
public enum ResidentShortcut { None, ControlAltW, ControlAltShiftW }

public sealed record ResidentSettings(int Version = 1, bool CloseToTray = true,
    ResidentTheme Theme = ResidentTheme.System, ResidentShortcut Shortcut = ResidentShortcut.ControlAltW)
{
    public bool IsValid => Version == 1 && Enum.IsDefined(Theme) && Enum.IsDefined(Shortcut);
}

public static class ResidentPolicy
{
    public static bool HideOnClose(bool closeToTray, bool trayReady, bool exiting) => closeToTray && trayReady && !exiting;

    public static string StartupCommand(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || executable.Length < 4 || !char.IsAsciiLetter(executable[0])
            || !executable.StartsWith(executable[..1] + @":\", StringComparison.Ordinal)
            || executable.Contains('"') || executable.Any(char.IsControl) || executable.Contains('/')
            || executable.AsSpan(2).Contains(':') || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("An absolute local executable path is required.", nameof(executable));
        string command = $"\"{executable}\" --resident --resident-minimized";
        if (command.Length > 260) throw new ArgumentException("The sign-in command exceeds the Run entry limit.", nameof(executable));
        return command;
    }
}
