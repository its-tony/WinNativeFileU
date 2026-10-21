using System.ComponentModel;
using System.Runtime.InteropServices;
using WinNativeFileU.Core;

namespace WinNativeFileU.WindowsIntegration;

/// <summary>Event-driven owned HWND, notification icon and one registered show shortcut; no input hook or timer.</summary>
public sealed class ResidentHost : IDisposable
{
    internal const uint TrayMessage = 0x8032;
    private readonly string className = "WinNativeFileU.Resident." + Guid.NewGuid().ToString("N");
    private readonly Native.WindowProc procedure;
    private readonly Action<string, object> log;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private nint hwnd;
    private bool disposed, classRegistered;
    private int hotkeyId;
    private ResidentShortcut shortcut;
    private readonly uint taskbarCreated;
    public bool TrayReady { get; private set; }
    internal nint Handle => hwnd;
    public event Action? ShowRequested;
    public event Action? HideRequested;
    public event Action? ExitRequested;
    public event Action? TrayUnavailable;

    public ResidentHost(Action<string, object> journal)
    {
        log = journal; procedure = Procedure;
        try
        {
            var cls = new Native.WindowClass { Size = (uint)Marshal.SizeOf<Native.WindowClass>(), Proc = procedure,
                Instance = Native.GetModuleHandleW(null), Name = className };
            if (Native.RegisterClassExW(ref cls) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            classRegistered = true;
            // Hidden top-level window receives TaskbarCreated broadcasts; not a message-only window.
            hwnd = Native.CreateWindowExW(0x80, className, "WinNativeFileU Resident Events", 0, 0, 0, 0, 0, 0, 0, cls.Instance, 0);
            if (hwnd == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            taskbarCreated = ResidentNative.RegisterWindowMessageW("TaskbarCreated");
            if (taskbarCreated == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            AddIcon();
            Write("ResidentHostReady", new { tray = TrayReady, eventDriven = true });
        }
        catch { Dispose(); throw; }
    }

    public void SetShortcut(ResidentShortcut value)
    {
        AssertThread();
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == shortcut) return;
        int candidate = hotkeyId == 1 ? 2 : 1;
        if (value != ResidentShortcut.None)
        {
            uint modifiers = value == ResidentShortcut.ControlAltW ? 0x4003u : 0x4007u;
            if (!Native.RegisterHotKey(hwnd, candidate, modifiers, 0x57))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Shortcut unavailable. The previous shortcut was preserved.");
        }
        if (hotkeyId != 0)
        {
            bool removed = Native.UnregisterHotKey(hwnd, hotkeyId);
            Write("ResidentHotkeyRemoved", new { success = removed });
            if (!removed)
            {
                if (value != ResidentShortcut.None) Native.UnregisterHotKey(hwnd, candidate);
                throw new Win32Exception("The previous shortcut could not be removed; the new setting was not applied.");
            }
        }
        hotkeyId = value == ResidentShortcut.None ? 0 : candidate;
        shortcut = value;
        Write("ResidentShortcut", new { shortcut = value.ToString(), registered = hotkeyId != 0 });
    }

    private ResidentNative.IconData Data() => new()
    {
        Size = (uint)Marshal.SizeOf<ResidentNative.IconData>(), Window = hwnd, Id = 1,
        Flags = 0x87, Callback = TrayMessage, Icon = ResidentNative.LoadIconW(0, (nint)32512),
        Tip = "WinNativeFileU — Open settings", Info = "", InfoTitle = "", Version = 4
    };

    private void AddIcon()
    {
        var data = Data();
        if (data.Icon == 0 || !ResidentNative.Shell_NotifyIconW(0, ref data)) throw new Win32Exception("Notification icon could not be added.");
        TrayReady = true;
        if (!ResidentNative.Shell_NotifyIconW(4, ref data))
        {
            RemoveIcon();
            throw new Win32Exception("Notification icon version 4 could not be selected.");
        }
        Write("TrayAdded", new { version = 4 });
    }

    private void RemoveIcon()
    {
        if (!TrayReady) return;
        var data = Data();
        Write("TrayRemoved", new { success = ResidentNative.Shell_NotifyIconW(2, ref data) });
        TrayReady = false;
    }

    private nint Procedure(nint window, uint message, nuint wp, nint lp)
    {
        try
        {
            if (message == taskbarCreated && taskbarCreated != 0)
            {
                TrayReady = false; // Explorer discarded all registrations.
                try { AddIcon(); Write("TrayRecreated", new { }); }
                catch (Exception ex) { Write("TrayRecoveryFailed", new { type = ex.GetType().Name }); TrayUnavailable?.Invoke(); }
                return 0;
            }
            if (message == 0x312 && hotkeyId != 0 && (int)wp == hotkeyId)
            { Write("ResidentHotkey", new { shortcut = shortcut.ToString() }); ShowRequested?.Invoke(); return 0; }
            if (message == TrayMessage && ((long)lp >> 16 & 0xFFFF) == 1)
            {
                uint notification = (uint)((long)lp & 0xFFFF);
                if (notification is 0x400 or 0x401) { Write("TrayShow", new { }); ShowRequested?.Invoke(); }
                else if (notification == 0x7B) ShowMenu();
                return 0;
            }
            if (message == 0x16 && wp != 0) { ExitRequested?.Invoke(); return 0; } // End-session, not a user close-to-tray.
        }
        catch (Exception ex) { Write("ResidentCallbackFailed", new { type = ex.GetType().Name }); }
        return Native.DefWindowProcW(window, message, wp, lp);
    }

    private void ShowMenu()
    {
        nint menu = ResidentNative.CreatePopupMenu();
        if (menu == 0) return;
        try
        {
            if (!ResidentNative.AppendMenuW(menu, 0, 1, "Open settings")
                || !ResidentNative.AppendMenuW(menu, 0, 2, "Hide settings")
                || !ResidentNative.AppendMenuW(menu, 0, 3, "Exit WinNativeFileU")) return;
            if (!Native.GetCursorPos(out var point)) return;
            ResidentNative.SetForegroundWindow(hwnd);
            uint command = ResidentNative.TrackPopupMenuEx(menu, 0x182, point.X, point.Y, hwnd, 0);
            ResidentNative.PostMessageW(hwnd, 0, 0, 0); // Complete the documented foreground-menu dismissal sequence.
            Write("TrayMenu", new { command });
            if (command == 1) ShowRequested?.Invoke();
            if (command == 2) HideRequested?.Invoke();
            if (command == 3) ExitRequested?.Invoke();
        }
        finally { ResidentNative.DestroyMenu(menu); }
    }

    internal void DispatchHotkeyForTest() => Procedure(hwnd, 0x312, (nuint)hotkeyId, 0);
    internal void DispatchSelectForTest() => Procedure(hwnd, TrayMessage, 0, (nint)((1 << 16) | 0x400));

    private void Write(string name, object details) { try { log(name, details); } catch { /* Never unwind a native callback for diagnostics. */ } }
    private void AssertThread()
    {
        if (disposed || Environment.CurrentManagedThreadId != thread) throw new InvalidOperationException("Resident host requires its live UI thread.");
    }
    public void Dispose()
    {
        if (disposed) return;
        AssertThread();
        if (hotkeyId != 0) { Write("ResidentHotkeyRemoved", new { success = Native.UnregisterHotKey(hwnd, hotkeyId) }); hotkeyId = 0; }
        RemoveIcon();
        if (hwnd != 0) { Write("ResidentWindowDestroyed", new { success = Native.DestroyWindow(hwnd) }); hwnd = 0; }
        if (classRegistered) Write("ResidentClassRemoved", new { success = Native.UnregisterClassW(className, Native.GetModuleHandleW(null)) });
        disposed = true;
    }
}

internal static class ResidentNative
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct IconData
    {
        public uint Size; public nint Window; public uint Id, Flags, Callback; public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags; public Guid Guid; public nint BalloonIcon;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool Shell_NotifyIconW(uint message, ref IconData data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern nint LoadIconW(nint instance, nint resource);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern uint RegisterWindowMessageW(string message);
    [DllImport("user32.dll")] internal static extern nint CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool AppendMenuW(nint menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")] internal static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint hwnd, nint parameters);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool PostMessageW(nint hwnd, uint message, nuint wp, nint lp);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindow(nint hwnd);
}
