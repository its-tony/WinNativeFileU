using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace WinNativeFileU.WindowsIntegration;

[ComVisible(true), Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IOleDropTarget
{
    [PreserveSig] int DragEnter([MarshalAs(UnmanagedType.Interface)] IDataObject data, uint keys, NativePoint point, ref uint effect);
    [PreserveSig] int DragOver(uint keys, NativePoint point, ref uint effect);
    [PreserveSig] int DragLeave();
    [PreserveSig] int Drop([MarshalAs(UnmanagedType.Interface)] IDataObject data, uint keys, NativePoint point, ref uint effect);
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePoint { public int X, Y; }

internal static class Native
{
    internal delegate nint WindowProc(nint hwnd, uint msg, nuint wp, nint lp);
    internal delegate bool MonitorEnumProc(nint monitor, nint dc, ref Rect rect, nint data);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WindowClass
    {
        public uint Size, Style;
        public WindowProc Proc;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Menu;
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
        public nint SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct PaintInfo
    {
        public nint DC; public int Erase; public Rect Paint; public int Restore, Update;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Reserved;
    }

    [DllImport("ole32.dll")] internal static extern int OleInitialize(nint reserved);
    [DllImport("ole32.dll")] internal static extern void OleUninitialize();
    [DllImport("ole32.dll")] internal static extern int RegisterDragDrop(nint hwnd, [MarshalAs(UnmanagedType.Interface)] IOleDropTarget target);
    [DllImport("ole32.dll")] internal static extern int RevokeDragDrop(nint hwnd);
    [DllImport("ole32.dll")] internal static extern void ReleaseStgMedium(ref STGMEDIUM medium);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] internal static extern uint DragQueryFileW(nint drop, uint index, StringBuilder? path, uint size);
    [DllImport("kernel32.dll")] internal static extern nuint GlobalSize(nint memory);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern nint GetModuleHandleW(string? module);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern ushort RegisterClassExW(ref WindowClass windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern nint CreateWindowExW(uint exStyle, string cls, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern nint DefWindowProcW(nint hwnd, uint msg, nuint wp, nint lp);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyWindow(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnregisterClassW(string cls, nint instance);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnregisterHotKey(nint hwnd, int id);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool ShowWindow(nint hwnd, int command);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetLayeredWindowAttributes(nint hwnd, uint colorKey, byte alpha, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UpdateWindow(nint hwnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern int SetWindowRgn(nint hwnd, nint region, [MarshalAs(UnmanagedType.Bool)] bool redraw);
    [DllImport("gdi32.dll")] internal static extern nint CreateEllipticRgn(int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DeleteObject(nint obj);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsChild(nint parent, nint child);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool ScreenToClient(nint hwnd, ref NativePoint point);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern short GetKeyState(int key);
    [DllImport("user32.dll")] internal static extern int GetMessageTime();
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorEnumProc callback, nint data);
    [DllImport("user32.dll")] internal static extern nint MonitorFromPoint(NativePoint point, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint BeginPaint(nint hwnd, out PaintInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool EndPaint(nint hwnd, ref PaintInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetClientRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] internal static extern int FillRect(nint dc, ref Rect rect, nint brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int DrawTextW(nint dc, string text, int length, ref Rect rect, uint format);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool InvalidateRect(nint hwnd, nint rect, bool erase);
    [DllImport("gdi32.dll")] internal static extern uint SetTextColor(nint dc, uint color);
    [DllImport("gdi32.dll")] internal static extern int SetBkMode(nint dc, int mode);
}
