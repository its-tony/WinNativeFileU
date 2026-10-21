using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.WindowsIntegration.Tests;

public sealed class ResidentTests
{
    [Fact]
    public void SettingsRoundTripPreservesPreviousVersion()
    {
        string root = Run(); var store = new ResidentSettingsStore(root);
        Assert.Equal(new ResidentSettings(), store.Load());
        store.Save(new());
        byte[] first = File.ReadAllBytes(Path.Combine(root, "settings.json"));
        var changed = new ResidentSettings(Theme: ResidentTheme.Dark, Shortcut: ResidentShortcut.ControlAltShiftW, CloseToTray: false);
        store.Save(changed);
        Assert.Equal(changed, store.Load());
        Assert.Equal(first, File.ReadAllBytes(Assert.Single(Directory.GetFiles(root, "*.previous"))));
        Assert.Empty(Directory.GetFiles(root, "*.partial"));
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("{\"Version\":2}")]
    [InlineData("{\"Version\":1,\"Theme\":99}")]
    [InlineData("{\"Version\":1,\"Unexpected\":true}")]
    public void InvalidSettingsArePreservedInsteadOfOverwritten(string content)
    {
        string root = Run(), path = Path.Combine(root, "settings.json"); File.WriteAllText(path, content);
        byte[] hash = SHA256.HashData(File.ReadAllBytes(path)); var store = new ResidentSettingsStore(root);
        Assert.ThrowsAny<Exception>(() => store.Load()); Assert.ThrowsAny<Exception>(() => store.Save(new()));
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(path)));
        Assert.Empty(Directory.GetFiles(root, "*.partial"));
    }

    [Fact]
    public void OversizedSettingsAndDirectoryAtSettingsPathAreRejected()
    {
        string root = Run(); File.WriteAllText(Path.Combine(root, "settings.json"), new string(' ', 4097));
        Assert.Throws<InvalidDataException>(() => new ResidentSettingsStore(root).Load());
        string other = Run(); Directory.CreateDirectory(Path.Combine(other, "settings.json"));
        Assert.Throws<IOException>(() => new ResidentSettingsStore(other).Save(new()));
    }

    [Fact]
    public void StartupLoadingNeverEnablesAnythingAndConflictsArePreserved()
    {
        var memory = new StartupMemory(); var registration = new StartupRegistration(memory, @"C:\app.exe");
        Assert.Equal(StartupState.Disabled, registration.State); Assert.Equal(0, memory.Writes);
        registration.SetEnabled(true); Assert.Equal(StartupState.Enabled, registration.State); Assert.Equal(1, memory.Writes);
        registration.SetEnabled(false); Assert.Equal(StartupState.Disabled, registration.State);
        memory.Value = "Another program or different path";
        Assert.Equal(StartupState.Conflict, registration.State);
        Assert.Throws<InvalidOperationException>(() => registration.SetEnabled(true));
        Assert.Throws<InvalidOperationException>(() => registration.SetEnabled(false));
        Assert.Equal("Another program or different path", memory.Value);
        // In-memory value adapter only; this test never writes the real Run registry key.
    }

    [Fact]
    public void NamedOwnershipRejectsOtherThreadAndCanBeReacquired()
    {
        string root = Run();
        using (var first = new ResidentInstance(root))
        {
            Assert.True(first.Acquired);
            bool duplicate = true;
            var contender = new Thread(() => { using var second = new ResidentInstance(root); duplicate = second.Acquired; });
            contender.Start(); Assert.True(contender.Join(TimeSpan.FromSeconds(5)));
            Assert.False(duplicate);
        }
        using var again = new ResidentInstance(root); Assert.True(again.Acquired);
    }

    [Fact]
    public void NativeTrayHotkeyConflictAndDisposalUseOwnedResources() => OnSta(() =>
    {
        var events = new List<string>(); var host = new ResidentHost((name, _) => events.Add(name));
        nint hwnd = host.Handle;
        using (host)
        {
            Assert.True(host.TrayReady); Assert.True(ResidentNative.IsWindow(hwnd));
            host.SetShortcut(ResidentShortcut.ControlAltW);
            int shown = 0; host.ShowRequested += () => shown++;
            using (var other = new ResidentHost((_, _) => { }))
            {
                Assert.Throws<System.ComponentModel.Win32Exception>(() => other.SetShortcut(ResidentShortcut.ControlAltW));
                other.SetShortcut(ResidentShortcut.ControlAltShiftW);
                Assert.Throws<System.ComponentModel.Win32Exception>(() => host.SetShortcut(ResidentShortcut.ControlAltShiftW));
                host.DispatchHotkeyForTest(); Assert.Equal(1, shown); // Direct callback, not physical keyboard delivery.
            }
            host.SetShortcut(ResidentShortcut.ControlAltShiftW);
            host.DispatchSelectForTest(); Assert.Equal(2, shown); // Synthetic icon notification, not an Explorer click.
            host.SetShortcut(ResidentShortcut.None); host.DispatchHotkeyForTest(); Assert.Equal(2, shown);
        }
        Assert.False(ResidentNative.IsWindow(hwnd)); Assert.Contains("TrayRemoved", events);
        host.Dispose(); // Idempotent; subsequent ownership can register the released shortcuts.
        using var next = new ResidentHost((_, _) => { }); next.SetShortcut(ResidentShortcut.ControlAltW);
    });

    private sealed class StartupMemory : IStartupValueStore
    {
        public string? Value; public int Writes;
        public string? Read() => Value;
        public void Enable(string expectedCommand) { Value = expectedCommand; Writes++; }
        public void Disable(string expectedCommand) { Value = null; Writes++; }
    }
    private static string Run()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "WinNativeFileU.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string path = Path.Combine(directory.FullName, "evidence", "private", "resident-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path); return path;
    }
    private static void OnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Native resident test timed out.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
