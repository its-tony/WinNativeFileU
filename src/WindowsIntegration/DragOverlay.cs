using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using WinNativeFileU.Core;

namespace WinNativeFileU.WindowsIntegration;

/// <summary>Fixture-only OLE experiment, owned by the WinUI STA. No file mutations.</summary>
[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
public sealed class DragOverlay : IOleDropTarget, IDisposable
{
    private readonly string root;
    private readonly string className = "WinNativeFileU.M1." + Guid.NewGuid().ToString("N");
    private readonly Native.WindowProc procedure;
    private readonly StreamWriter journal;
    private readonly bool nativeTestMode;
    private readonly Func<PointerObservation> readPointer;
    private readonly Func<bool?> isPointerOverReceiver;
    private readonly PrototypeShortcuts shortcuts;
    private readonly InteractionSession session = new();
    private nint hwnd;
    private bool oleInitialized, registered, activationRegistered, rejectionRegistered, disposed, cancelled, acceptable, dragInside, overLogged, logHealthy = true;
    private readonly int threadId = Environment.CurrentManagedThreadId;
    private string caption = "M1 fixture receiver\nDrop: record paths only; originals untouched";
    public event Action<string>? StatusChanged;
    internal InteractionState SessionState => session.State;
    public string ActivationLabel => shortcuts.ActivationLabel;
    public string RejectionLabel => shortcuts.RejectionLabel;

    public DragOverlay(string fixtureRoot, string logPath, PrototypeShortcutProfile profile = PrototypeShortcutProfile.FunctionKeys)
        : this(fixtureRoot, logPath, false, ReadPointer, profile) { }

    // Hidden receiver tests use native OLE registration and HGLOBAL decoding,
    // but neither register hotkeys nor show/move any desktop window.
    internal DragOverlay(string fixtureRoot, string logPath, bool nativeTestMode, Func<PointerObservation> readPointer,
        PrototypeShortcutProfile profile = PrototypeShortcutProfile.FunctionKeys, Func<bool?>? isPointerOverReceiver = null)
    {
        shortcuts = PrototypeShortcuts.For(profile);
        this.nativeTestMode = nativeTestMode;
        this.readPointer = readPointer;
        this.isPointerOverReceiver = isPointerOverReceiver ?? (nativeTestMode ? () => false : ObservePointerOverReceiver);
        root = Path.GetFullPath(fixtureRoot);
        if (!Directory.Exists(root) || !DropPolicy.IsWithinFixtureRoot(root, Path.Combine(root, "probe")))
            throw new ArgumentException("An existing local fixture directory is required.");
        if (new DriveInfo(Path.GetPathRoot(root)!).DriveType != DriveType.Fixed || HasReparseAncestor(root))
            throw new ArgumentException("Fixture root must be on a fixed drive without reparse ancestors.");
        journal = new StreamWriter(new FileStream(logPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        procedure = Procedure;
        try
        {
            int hr = Native.OleInitialize(0);
            Log("OleInitialize", new { hr });
            Marshal.ThrowExceptionForHR(hr);
            oleInitialized = true;
            var cls = new Native.WindowClass
            {
                Size = (uint)Marshal.SizeOf<Native.WindowClass>(), Proc = procedure,
                Instance = Native.GetModuleHandleW(null), Background = 6, Name = className
            };
            if (Native.RegisterClassExW(ref cls) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            Log("WindowClassRegistered", new { });
            hwnd = Native.CreateWindowExW(0x08000088, className, "WinNativeFileU M1 Drop Target",
                0x80800000, 0, 0, 360, 160, 0, 0, cls.Instance, 0);
            if (hwnd == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            Log("WindowCreated", new { hwnd = hwnd.ToInt64() });
            hr = Native.RegisterDragDrop(hwnd, this);
            Marshal.ThrowExceptionForHR(hr);
            registered = true;
            Log("RegisterDragDrop", new { hr, hwnd = hwnd.ToInt64() });
            activationRegistered = !nativeTestMode && Native.RegisterHotKey(hwnd, 8, shortcuts.Modifiers, shortcuts.ActivationKey);
            rejectionRegistered = !nativeTestMode && Native.RegisterHotKey(hwnd, 9, shortcuts.Modifiers, shortcuts.RejectionKey);
            Log("Hotkeys", new { profile = shortcuts.Name, activation = ActivationLabel, rejection = RejectionLabel,
                activationRegistered, rejectionRegistered });
            if (!nativeTestMode && (!activationRegistered || !rejectionRegistered))
                throw new Win32Exception($"{ActivationLabel}/{RejectionLabel} could not be registered; no hotkey is assumed available.");
            Log("Ready", new { root, os = Environment.OSVersion.Version.ToString(), runtime = Environment.Version.ToString(), apartment = Thread.CurrentThread.GetApartmentState().ToString() });
        }
        catch { Dispose(); throw; }
    }

    public void Arm() => Arm("button");

    private void Arm(string origin)
    {
        AssertThread();
        if (session.State != InteractionState.Idle)
        {
            Log("ArmIgnored", new { state = session.State.ToString(), pointer = readPointer() });
            StatusChanged?.Invoke($"Target already armed. Re-enter it, or use {RejectionLabel} to reject/close.");
            return;
        }
        if (!logHealthy) throw new IOException("Private journal unavailable; receiver disabled.");
        session.Activate();
        cancelled = acceptable = false;
        if (nativeTestMode) { Log("ArmedForNativeTest", new { profile = shortcuts.Name }); return; }
        caption = $"M1 fixture receiver\nDrag fixture files here. {RejectionLabel} rejects.\nRelease {ActivationLabel}: target stays open.";
        var before = Native.GetForegroundWindow();
        if (!Native.GetCursorPos(out var cursor)) { Finish("CursorUnavailable"); return; }
        var monitor = Native.MonitorFromPoint(cursor, 2);
        var info = new Native.MonitorInfo { Size = (uint)Marshal.SizeOf<Native.MonitorInfo>() };
        if (!Native.GetMonitorInfoW(monitor, ref info)) { Finish("MonitorUnavailable"); return; }
        // First move while hidden so GetDpiForWindow reflects the cursor's monitor.
        Native.SetWindowPos(hwnd, -1, cursor.X, cursor.Y, 360, 160, 0x10);
        uint dpi = Native.GetDpiForWindow(hwnd);
        int width = Math.Min((int)(360 * dpi / 96), info.Work.Right - info.Work.Left);
        int height = Math.Min((int)(160 * dpi / 96), info.Work.Bottom - info.Work.Top);
        int x = Math.Clamp(cursor.X - width / 2, info.Work.Left, info.Work.Right - width);
        int y = Math.Clamp(cursor.Y - height / 2, info.Work.Top, info.Work.Bottom - height);
        if (!Native.SetWindowPos(hwnd, -1, x, y, width, height, 0x50)) { Finish("ShowFailed"); return; }
        Native.InvalidateRect(hwnd, 0, true);
        Log("Armed", new { origin, profile = shortcuts.Name, before = before.ToInt64(), after = Native.GetForegroundWindow().ToInt64(), cursor, x, y, width, height, dpi, pointer = readPointer() });
        StatusChanged?.Invoke($"Target armed. Release {ActivationLabel}; drop only prepared fixtures.");
    }

    public void Cancel()
    {
        AssertThread();
        cancelled = true;
        if (dragInside)
        {
            caption = "Cancelled: this target rejects drops.\nRelease here or cancel source with Escape.\nNo file operation will run.";
            Native.InvalidateRect(hwnd, 0, true);
            Log("CancelLatched", new { });
            StatusChanged?.Invoke("Rejecting until Drop/Leave; source drag is not cancelled by hiding a window.");
        }
        else
        {
            Finish("CancelIdle");
            StatusChanged?.Invoke("Receiver closed. No file operation was performed.");
        }
    }

    public int DragEnter(IDataObject data, uint keys, NativePoint point, ref uint effect)
    {
        uint allowed = effect;
        effect = 0;
        dragInside = true;
        overLogged = false;
        acceptable = false;
        try
        {
            AssertThread();
            if (session.State != InteractionState.AwaitingData || cancelled || !logHealthy) return 0;
            var files = ReadFiles(data);
            acceptable = true;
            session.EnterAcceptedTarget();
            effect = DropPolicy.Effect(allowed, acceptable, cancelled);
            caption = $"Valid fixture selection: {files.Length} file(s)\nRelease to record paths only.\nEscape cancels the source; {RejectionLabel} rejects.";
            if (!Log("DragEnter", new { allowed, effect, keys, count = files.Length, x = point.X, y = point.Y })) effect = 0;
        }
        catch (Exception ex) { Log("DragEnterRejected", new { type = ex.GetType().Name, hr = ex.HResult }); }
        Native.InvalidateRect(hwnd, 0, true);
        return 0;
    }

    public int DragOver(uint keys, NativePoint point, ref uint effect)
    {
        effect = DropPolicy.Effect(effect, acceptable && session.State == InteractionState.Tracking && logHealthy, cancelled);
        if (!overLogged) { overLogged = true; if (!Log("DragOver", new { effect, keys, x = point.X, y = point.Y })) effect = 0; }
        return 0;
    }

    public int DragLeave()
    {
        try
        {
            var pointer = readPointer();
            bool? pointerOverReceiver = isPointerOverReceiver();
            // Escape may already be up when Leave reaches this thread. Only a
            // positive observation of leaving this window permits reentry wait.
            bool keepVisible = !cancelled && logHealthy && pointer.KeepReceiverAfterLeave
                && pointerOverReceiver is false;
            Log("DragLeave", new { state = session.State.ToString(), cancelled, pointer, pointerOverReceiver, keepVisible });
            if (keepVisible)
            {
                session.Reset(); session.Activate();
                acceptable = dragInside = overLogged = false;
                caption = $"Pointer left target: re-enter to receive.\nNo drop was accepted.\n{RejectionLabel} rejects/closes this receiver.";
                Native.InvalidateRect(hwnd, 0, true);
                StatusChanged?.Invoke("Pointer left while the button was held. Re-enter target; no files received.");
            }
            else
            {
                Finish(pointerOverReceiver switch
                {
                    true => "LeaveOverReceiver",
                    null => "LeaveLocationUnknown",
                    _ => "LeaveOrSourceCancel"
                });
                StatusChanged?.Invoke("Target closed after Leave. No drop received; source cancellation is not confirmed by Leave alone.");
            }
        }
        catch { session.Reset(); }
        return 0;
    }

    public int Drop(IDataObject data, uint keys, NativePoint point, ref uint effect)
    {
        uint allowed = effect;
        effect = 0;
        try
        {
            if (session.State != InteractionState.Tracking || DropPolicy.Effect(allowed, acceptable && logHealthy, cancelled) == 0)
            { Log("DropRejected", new { allowed, cancelled }); return 0; }
            var files = ReadFiles(data);
            session.ReceiveDrop(files);
            if (Log("DropAccepted", new { allowed, effect = DropPolicy.Copy, keys, count = files.Length, files }))
            {
                effect = DropPolicy.Copy;
                StatusChanged?.Invoke($"Received {files.Length} fixture file(s). No file operation performed.");
            }
        }
        catch (Exception ex) { Log("DropRejected", new { type = ex.GetType().Name, hr = ex.HResult }); }
        finally { Finish("DropFinished"); }
        return 0;
    }

    private string[] ReadFiles(IDataObject data)
    {
        var format = new FORMATETC { cfFormat = 15, dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -1, tymed = TYMED.TYMED_HGLOBAL };
        if (data.QueryGetData(ref format) != 0) throw new NotSupportedException("CF_HDROP required.");
        data.GetData(ref format, out var medium);
        try
        {
            if (medium.tymed != TYMED.TYMED_HGLOBAL || medium.unionmember == 0
                || Native.GlobalSize(medium.unionmember) < 20 || Native.GlobalSize(medium.unionmember) > 2_097_152)
                throw new NotSupportedException("Bounded HGLOBAL required.");
            uint count = Native.DragQueryFileW(medium.unionmember, uint.MaxValue, null, 0);
            if (count == 0 || count > DropPolicy.MaximumFiles) throw new NotSupportedException("Selection exceeds experiment limit.");
            var files = new List<string>();
            for (uint i = 0; i < count; i++)
            {
                uint length = Native.DragQueryFileW(medium.unionmember, i, null, 0);
                if (length == 0 || length > 32767) throw new NotSupportedException("Invalid path length.");
                var path = new StringBuilder((int)length + 1);
                if (Native.DragQueryFileW(medium.unionmember, i, path, length + 1) != length) throw new NotSupportedException("Incomplete path.");
                string full = path.ToString();
                if (!DropPolicy.IsWithinFixtureRoot(root, full) || HasReparseAncestor(full)) throw new NotSupportedException("Outside fixture boundary.");
                var attrs = File.GetAttributes(full);
                if ((attrs & (FileAttributes.Directory | FileAttributes.Offline | FileAttributes.ReparsePoint)) != 0)
                    throw new NotSupportedException("Physical resident fixture file required.");
                files.Add(Path.GetFullPath(full));
            }
            return files.ToArray();
        }
        finally { Native.ReleaseStgMedium(ref medium); }
    }

    private static bool HasReparseAncestor(string path)
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
        return false;
    }

    private void Finish(string reason)
    {
        session.Reset(); acceptable = false; cancelled = false; dragInside = false;
        Native.ShowWindow(hwnd, 0);
        Log("SessionEnded", new { reason });
    }
    private bool Log(string name, object details)
    {
        try { journal.WriteLine(JsonSerializer.Serialize(new { at = DateTimeOffset.Now, name, details }, new JsonSerializerOptions { IncludeFields = true })); return true; }
        catch (IOException) { logHealthy = false; return false; }
    }
    private void AssertThread()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException("STA owner thread required.");
    }

    // Only two event-time signals; no polling or capture of arbitrary typing.
    private static PointerObservation ReadPointer() => new(Native.GetAsyncKeyState(1) < 0, Native.GetAsyncKeyState(0x1B) < 0);

    private bool? ObservePointerOverReceiver()
    {
        if (!Native.GetCursorPos(out var cursor)) return null;
        nint atPoint = Native.WindowFromPoint(cursor);
        return atPoint == 0 ? null : atPoint == hwnd;
    }

    private nint Procedure(nint window, uint msg, nuint wp, nint lp)
    {
        try
        {
            if (msg == 0x312)
            {
                if (wp is not (8 or 9)) return Native.DefWindowProcW(window, msg, wp, lp);
                Log("HotkeyReceived", new { id = (int)wp, profile = shortcuts.Name,
                    shortcut = wp == 8 ? ActivationLabel : RejectionLabel, state = session.State.ToString(), pointer = readPointer() });
                if (wp == 8) Arm("hotkey"); else Cancel();
                return 0;
            }
            if (msg == 0x21) return 3; // MA_NOACTIVATE
            if (msg == 0x10) { Cancel(); return 0; }
            if (msg == 0x2E0) Log("DpiChanged", new { dpi = (uint)wp & 0xFFFF });
            if (msg == 0xF)
            {
                var dc = Native.BeginPaint(window, out var paint);
                try
                {
                    Native.GetClientRect(window, out var rect);
                    Native.FillRect(dc, ref rect, 6);
                    rect.Left += 16; rect.Top += 16; rect.Right -= 16;
                    Native.SetTextColor(dc, 0); Native.SetBkMode(dc, 1);
                    Native.DrawTextW(dc, caption, -1, ref rect, 0x10);
                }
                finally { Native.EndPaint(window, ref paint); }
                return 0;
            }
        }
        catch (Exception ex) { Log("WindowError", new { type = ex.GetType().Name, hr = ex.HResult }); }
        return Native.DefWindowProcW(window, msg, wp, lp);
    }

    public void Dispose()
    {
        if (disposed) return;
        if (registered) { int hr = Native.RevokeDragDrop(hwnd); Log("RevokeDragDrop", new { hr }); registered = false; }
        if (activationRegistered) Native.UnregisterHotKey(hwnd, 8);
        if (rejectionRegistered) Native.UnregisterHotKey(hwnd, 9);
        if (hwnd != 0) Native.DestroyWindow(hwnd);
        Native.UnregisterClassW(className, Native.GetModuleHandleW(null));
        if (oleInitialized) Native.OleUninitialize();
        journal.Dispose(); disposed = true;
        GC.KeepAlive(procedure);
    }
}
