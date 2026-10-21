using Microsoft.Win32;
using WinNativeFileU.Core;

namespace WinNativeFileU.WindowsIntegration;

public enum StartupState { Disabled, Enabled, Conflict }

public interface IStartupValueStore
{
    string? Read();
    void Enable(string expectedCommand);
    void Disable(string expectedCommand);
}

/// <summary>Sign-in is an explicit user action, never a consequence of loading settings.</summary>
public sealed class StartupRegistration(IStartupValueStore store, string executable)
{
    private readonly string command = ResidentPolicy.StartupCommand(executable);
    public StartupState State => store.Read() is not { } value ? StartupState.Disabled
        : value.Equals(command, StringComparison.Ordinal) ? StartupState.Enabled : StartupState.Conflict;

    public void SetEnabled(bool enabled)
    {
        if (State == StartupState.Conflict) throw new InvalidOperationException("A different sign-in command exists. It was preserved.");
        if (enabled) store.Enable(command); else store.Disable(command);
    }
}

public sealed class CurrentUserStartupValue : IStartupValueStore
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Name = "WinNativeFileU";
    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key);
        object? value = key?.GetValue(Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return value == null ? null : value as string ?? "<unsupported value kind>";
    }
    public void Enable(string expectedCommand)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key, writable: true);
        Check(key, expectedCommand);
        key.SetValue(Name, expectedCommand, RegistryValueKind.String);
    }
    public void Disable(string expectedCommand)
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key, writable: true);
        if (key == null) return;
        Check(key, expectedCommand);
        key.DeleteValue(Name, throwOnMissingValue: false); // Only this app's matching value; never the key.
    }
    private static void Check(RegistryKey key, string expected)
    {
        object? current = key.GetValue(Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (current != null && (current is not string text || text != expected))
            throw new InvalidOperationException("The sign-in value changed. It was preserved.");
    }
}
