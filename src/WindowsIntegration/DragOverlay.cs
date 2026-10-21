using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using WinNativeFileU.Core;

namespace WinNativeFileU.WindowsIntegration;

/// <summary>Owned OLE receiver: fixture-only by default, or an explicit bounded conversion queue adapter.</summary>
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
    private readonly bool requireHeldActivation;
    private readonly IOverlayPresentation? presentation;
    private readonly IFileDropHandler? fileDropHandler;
    private IReadOnlyList<string> availableFormats = [];
    private bool presentationReady;
    private bool resizingPresentation;
    private string? selectedFormat;
    private readonly InteractionSession session = new();
    private nint hwnd, inputWindow;
    private bool inputRegistered;
    private bool oleInitialized, registered, activationRegistered, rejectionRegistered, disposed, cancelled, acceptable, dragInside, overLogged, logHealthy = true;
    private readonly int threadId = Environment.CurrentManagedThreadId;
    private string caption = "M1 fixture receiver\nDrop: record paths only; originals untouched";
    public event Action<string>? StatusChanged;
    internal InteractionState SessionState => session.State;
    internal nint InputWindow => inputWindow;
    public string ActivationLabel => shortcuts.ActivationLabel;
    public string RejectionLabel => shortcuts.RejectionLabel;

    public DragOverlay(string fixtureRoot, string logPath, PrototypeShortcutProfile profile = PrototypeShortcutProfile.FunctionKeys,
        bool requireHeldActivation = false, IOverlayPresentation? presentation = null, IFileDropHandler? fileDropHandler = null)
        : this(fixtureRoot, logPath, false, ReadPointer, profile, requireHeldActivation: requireHeldActivation, presentation: presentation, fileDropHandler: fileDropHandler) { }

    // Hidden receiver tests use native OLE registration and HGLOBAL decoding,
    // but neither register hotkeys nor show/move any desktop window.
    internal DragOverlay(string fixtureRoot, string logPath, bool nativeTestMode, Func<PointerObservation> readPointer,
        PrototypeShortcutProfile profile = PrototypeShortcutProfile.FunctionKeys, Func<bool?>? isPointerOverReceiver = null,
        bool requireHeldActivation = false, IOverlayPresentation? presentation = null, IFileDropHandler? fileDropHandler = null)
    {
        shortcuts = PrototypeShortcuts.For(profile);
        this.nativeTestMode = nativeTestMode;
        this.requireHeldActivation = requireHeldActivation;
        this.presentation = presentation;
        this.fileDropHandler = fileDropHandler;
        if (fileDropHandler != null && presentation == null) throw new ArgumentException("Conversion drops require a format presentation.");
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
            hwnd = Native.CreateWindowExW(0x08000088, className, fileDropHandler != null ? "WinNativeFileU Conversion Radial" : presentation == null ? "WinNativeFileU M1 Drop Target" : "WinNativeFileU M2 Radial Target",
                presentation == null ? 0x80800000u : 0x80000000u, 0, 0, 360, 160, 0, 0, cls.Instance, 0);
            if (hwnd == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            Log("WindowCreated", new { hwnd = hwnd.ToInt64() });
            if (presentation != null)
            {
                presentation.Initialize(hwnd);
                presentationReady = true;
                // WinUI owns a child input site. A separate child owned by this
                // receiver captures OLE without replacing any framework registration.
                inputWindow = Native.CreateWindowExW(0x08080000, className, "WinNativeFileU radial input",
                    0x40000000, 0, 0, 1, 1, hwnd, 0, cls.Instance, 0);
                if (inputWindow == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
                // Alpha zero and WS_EX_TRANSPARENT pass input through. Alpha one
                // keeps this surface hit-testable with a 1/255 visual contribution.
                if (!Native.SetLayeredWindowAttributes(inputWindow, 0, 1, 2)) throw new Win32Exception(Marshal.GetLastWin32Error());
                hr = Native.RegisterDragDrop(inputWindow, this);
                Marshal.ThrowExceptionForHR(hr);
                inputRegistered = true;
                Log("RegisterInputDragDrop", new { hr, hwnd = inputWindow.ToInt64(), alpha = 1 });
                ResizePresentation();
                Log("PresentationReady", new { type = presentation.GetType().Name, presentation.WidthDip, presentation.HeightDip });
            }
            hr = Native.RegisterDragDrop(hwnd, this);
            Marshal.ThrowExceptionForHR(hr);
            registered = true;
            Log("RegisterDragDrop", new { hr, hwnd = hwnd.ToInt64() });
            activationRegistered = !nativeTestMode && Native.RegisterHotKey(hwnd, 8, shortcuts.Modifiers, shortcuts.ActivationKey);
            rejectionRegistered = !nativeTestMode && Native.RegisterHotKey(hwnd, 9, shortcuts.Modifiers, shortcuts.RejectionKey);
            Log("Hotkeys", new { profile = shortcuts.Name, activation = ActivationLabel, rejection = RejectionLabel,
                activationRegistered, rejectionRegistered, requireHeldActivation });
            if (!nativeTestMode && (!activationRegistered || !rejectionRegistered))
                throw new Win32Exception($"{ActivationLabel}/{RejectionLabel} could not be registered; no hotkey is assumed available.");
            if (!nativeTestMode) ObserveDisplays();
            Log("Ready", new { root, os = Environment.OSVersion.Version.ToString(), runtime = Environment.Version.ToString(), apartment = Thread.CurrentThread.GetApartmentState().ToString(), requireHeldActivation, conversionMode = fileDropHandler != null });
        }
        catch (Exception ex) { Log("InitializationFailed", new { type = ex.GetType().Name, hr = ex.HResult }); Dispose(); throw; }
    }

    public void Arm()
    {
        AssertThread();
        if (requireHeldActivation) { DeclineActivation("button", readPointer()); return; }
        Arm("button");
    }

    internal void ActivateFromHotkey(PointerObservation pointer)
    {
        AssertThread();
        if (requireHeldActivation && !pointer.KeepReceiverAfterLeave)
        { DeclineActivation("hotkey", pointer); return; }
        Arm("hotkey", pointer);
    }

    private void DeclineActivation(string origin, PointerObservation pointer)
    {
        Log("ActivationDeclined", new { origin, pointer, reason = "HeldMouseHotkeyRequired" });
        StatusChanged?.Invoke($"Validation mode: begin dragging first, keep the mouse held, then tap/release {ActivationLabel}. Button prearming is disabled.");
    }

    private void Arm(string origin, PointerObservation? activationPointer = null)
    {
        AssertThread();
        if (fileDropHandler?.CanAccept == false) { Log("ActivationDeclined", new { origin, reason = "ConversionBusyOrOutputRequired" }); return; }
        if (session.State != InteractionState.Idle)
        {
            Log("ArmIgnored", new { state = session.State.ToString(), pointer = readPointer() });
            StatusChanged?.Invoke($"Target already armed. Re-enter it, or use {RejectionLabel} to reject/close.");
            return;
        }
        if (!logHealthy) throw new IOException("Private journal unavailable; receiver disabled.");
        session.Activate();
        cancelled = acceptable = false;
        selectedFormat = null;
        presentation?.Reset();
        if (fileDropHandler != null) { availableFormats = []; presentation?.SetAvailableFormats(availableFormats); }
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
        var placement = OverlayPlacement.Calculate(cursor.X, cursor.Y,
            info.Work.Left, info.Work.Top, info.Work.Right, info.Work.Bottom, dpi,
            presentation?.WidthDip ?? 360, presentation?.HeightDip ?? 160);
        var (x, y, width, height) = placement;
        if (!Native.SetWindowPos(hwnd, -1, x, y, width, height, 0x50)) { Finish("ShowFailed"); return; }
        if (inputWindow != 0)
        {
            Native.InvalidateRect(inputWindow, 0, true);
            Native.UpdateWindow(inputWindow);
            Log("InputHitTest", new { atCursor = Native.WindowFromPoint(cursor).ToInt64(), expected = inputWindow.ToInt64(), atInput = Native.WindowFromPoint(cursor) == inputWindow });
        }
        Native.InvalidateRect(hwnd, 0, true);
        Log("Armed", new { origin, profile = shortcuts.Name, before = before.ToInt64(), after = Native.GetForegroundWindow().ToInt64(), cursor, x, y, width, height, dpi, work = info.Work, requireHeldActivation, pointer = activationPointer ?? readPointer() });
        StatusChanged?.Invoke(fileDropHandler != null ? $"Radial ready. Release {ActivationLabel} and drop on an enabled PNG/BMP sector." : $"Target armed. Release {ActivationLabel}; drop only prepared fixtures.");
    }

    public void Cancel()
    {
        AssertThread();
        cancelled = true;
        presentation?.Reject();
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
            if (!PrepareFormats(files)) return 0;
            acceptable = true;
            session.EnterAcceptedTarget();
            effect = DropPolicy.Effect(allowed, SelectFormat(point), cancelled);
            caption = $"Valid fixture selection: {files.Length} file(s)\nRelease to record paths only.\nEscape cancels the source; {RejectionLabel} rejects.";
            if (!Log("DragEnter", new { allowed, effect, keys, count = files.Length, x = point.X, y = point.Y })) effect = 0;
        }
        catch (Exception ex) { acceptable = false; Log("DragEnterRejected", new { type = ex.GetType().Name, hr = ex.HResult }); }
        Native.InvalidateRect(hwnd, 0, true);
        return 0;
    }

    public int DragOver(uint keys, NativePoint point, ref uint effect)
    {
        try
        {
            effect = DropPolicy.Effect(effect, acceptable && session.State == InteractionState.Tracking && logHealthy && SelectFormat(point), cancelled);
        }
        catch (Exception ex) { effect = 0; acceptable = false; Log("PresentationError", new { type = ex.GetType().Name, hr = ex.HResult }); }
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
                selectedFormat = null;
                presentation?.Reset();
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
            if (session.State != InteractionState.Tracking || DropPolicy.Effect(allowed, acceptable && logHealthy && SelectFormat(point), cancelled) == 0)
            { Log("DropRejected", new { allowed, cancelled }); return 0; }
            var files = ReadFiles(data);
            if (!PrepareFormats(files) || !SelectFormat(point)) { Log("DropRejected", new { reason = "ChangedOrIneligibleSelection" }); return 0; }
            session.ReceiveDrop(files);
            if (fileDropHandler != null)
            {
                if (selectedFormat == null || !Log("ConversionDropValidated", new { count = files.Length, selectedFormat })
                    || !fileDropHandler.TryAccept(Array.AsReadOnly(files), selectedFormat))
                { Log("DropRejected", new { reason = "JobNotQueued" }); return 0; }
                effect = DropPolicy.Copy; // Queue commit: later diagnostic failure cannot turn this accepted operation into NONE.
                Log("DropAccepted", new { allowed, effect, keys, count = files.Length, files, selectedFormat, previewOnly = false, conversionQueued = true });
                return 0;
            }
            if (Log("DropAccepted", new { allowed, effect = DropPolicy.Copy, keys, count = files.Length, files, selectedFormat, previewOnly = presentation != null }))
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
            if (count == 0 || count > (fileDropHandler == null ? DropPolicy.MaximumFiles : ImageBatchPolicy.MaximumFiles)) throw new NotSupportedException("Selection exceeds the file limit.");
            var files = new List<string>();
            for (uint i = 0; i < count; i++)
            {
                uint length = Native.DragQueryFileW(medium.unionmember, i, null, 0);
                if (length == 0 || length > 32767) throw new NotSupportedException("Invalid path length.");
                var path = new StringBuilder((int)length + 1);
                if (Native.DragQueryFileW(medium.unionmember, i, path, length + 1) != length) throw new NotSupportedException("Incomplete path.");
                string full = path.ToString();
                if (fileDropHandler != null)
                {
                    if (!IsLocalDriveSyntax(full) || full.Length > 1024) throw new NotSupportedException("Bounded absolute local image paths required.");
                    files.Add(full); // The explicit conversion handler checks attributes/header before any file can be accepted.
                    continue;
                }
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

    private static bool IsLocalDriveSyntax(string path) => path.Length >= 3 && char.IsAsciiLetter(path[0])
        && path[1] == ':' && path[2] == '\\' && !path.Contains('/') && !path.AsSpan(2).Contains(':');

    private bool PrepareFormats(IReadOnlyList<string> files)
    {
        if (fileDropHandler == null) return true;
        if (!fileDropHandler.CanAccept) return false;
        availableFormats = fileDropHandler.GetAvailableFormats(files).ToArray();
        if (availableFormats.Any(format => format is not ("PNG" or "BMP"))) throw new NotSupportedException("Unsupported conversion target.");
        presentation?.SetAvailableFormats(availableFormats);
        return availableFormats.Count != 0;
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

    private bool SelectFormat(NativePoint point)
    {
        if (presentation == null) return true;
        if (cancelled || !Native.ScreenToClient(hwnd, ref point) || !Native.GetClientRect(hwnd, out var rect)) return false;
        string? format = presentation.SelectAt(point.X, point.Y, rect.Right, rect.Bottom);
        if (fileDropHandler != null && (format == null || !availableFormats.Contains(format))) format = null;
        if (format != selectedFormat)
        {
            selectedFormat = format;
            if (!Log("RadialSelection", new { selectedFormat, x = point.X, y = point.Y, width = rect.Right, height = rect.Bottom })) return false;
        }
        return format != null;
    }

    private void ResizePresentation()
    {
        if (!presentationReady || resizingPresentation || !Native.GetClientRect(hwnd, out var rect)) return;
        resizingPresentation = true;
        try
        {
            presentation!.Resize(rect.Right, rect.Bottom);
            if (presentation.IsCircular)
            {
                nint region = Native.CreateEllipticRgn(0, 0, rect.Right, rect.Bottom);
                if (region == 0) throw new Win32Exception("Could not create radial window region.");
                if (Native.SetWindowRgn(hwnd, region, true) == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    Native.DeleteObject(region); // Ownership transfers only on success.
                    throw new Win32Exception(error);
                }
            }
            if (inputWindow != 0 && !Native.SetWindowPos(inputWindow, 0, 0, 0, rect.Right, rect.Bottom, 0x50))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally { resizingPresentation = false; }
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

    private void ObserveDisplays()
    {
        var displays = new List<object>();
        Native.MonitorEnumProc callback = (nint monitor, nint dc, ref Native.Rect rect, nint data) =>
        {
            var info = new Native.MonitorInfo { Size = (uint)Marshal.SizeOf<Native.MonitorInfo>() };
            if (!Native.GetMonitorInfoW(monitor, ref info)) return true;
            // Own hidden HWND only; no cursor movement, focus change or settings change.
            bool moved = Native.SetWindowPos(hwnd, 0, info.Work.Left, info.Work.Top, 1, 1, 0x14);
            displays.Add(new { monitor = info.Monitor, work = info.Work,
                primary = (info.Flags & 1) != 0, dpi = moved ? Native.GetDpiForWindow(hwnd) : 0u });
            return true;
        };
        bool enumerated = Native.EnumDisplayMonitors(0, 0, callback, 0);
        Log("Displays", new { enumerated, displayCount = Native.GetSystemMetrics(80),
            swappedButtons = Native.GetSystemMetrics(23) != 0, displays });
        GC.KeepAlive(callback);
    }

    private bool? ObservePointerOverReceiver()
    {
        if (!Native.GetCursorPos(out var cursor)) return null;
        nint atPoint = Native.WindowFromPoint(cursor);
        return atPoint == 0 ? null : atPoint == hwnd || (presentation != null && Native.IsChild(hwnd, atPoint));
    }

    private nint Procedure(nint window, uint msg, nuint wp, nint lp)
    {
        try
        {
            if (msg == 0x312)
            {
                if (wp is not (8 or 9)) return Native.DefWindowProcW(window, msg, wp, lp);
                var pointer = readPointer();
                Log("HotkeyReceived", new { id = (int)wp, profile = shortcuts.Name,
                    shortcut = wp == 8 ? ActivationLabel : RejectionLabel, state = session.State.ToString(), pointer,
                    queuedLeftButtonDown = Native.GetKeyState(1) < 0,
                    messageAgeMs = unchecked((uint)Environment.TickCount - (uint)Native.GetMessageTime()), requireHeldActivation });
                if (wp == 8) ActivateFromHotkey(pointer); else Cancel();
                return 0;
            }
            if (msg == 0x21) return 3; // MA_NOACTIVATE
            if (msg == 5 && window == hwnd) ResizePresentation(); // WM_SIZE for parent only.
            if (msg == 0x10) { Cancel(); return 0; }
            if (msg == 0x2E0) Log("DpiChanged", new { dpi = (uint)wp & 0xFFFF });
            if (msg == 0xF)
            {
                var dc = Native.BeginPaint(window, out var paint);
                try
                {
                    if (presentation != null && window != inputWindow) return 0;
                    Native.GetClientRect(window, out var rect);
                    Native.FillRect(dc, ref rect, 6);
                    if (window == inputWindow) return 0;
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
        if (inputRegistered) { int hr = Native.RevokeDragDrop(inputWindow); Log("RevokeInputDragDrop", new { hr }); inputRegistered = false; }
        if (registered) { int hr = Native.RevokeDragDrop(hwnd); Log("RevokeDragDrop", new { hr }); registered = false; }
        if (activationRegistered) Native.UnregisterHotKey(hwnd, 8);
        if (rejectionRegistered) Native.UnregisterHotKey(hwnd, 9);
        presentationReady = false;
        presentation?.Dispose();
        if (inputWindow != 0) { Native.DestroyWindow(inputWindow); inputWindow = 0; }
        if (hwnd != 0) Native.DestroyWindow(hwnd);
        Native.UnregisterClassW(className, Native.GetModuleHandleW(null));
        if (oleInitialized) Native.OleUninitialize();
        journal.Dispose(); disposed = true;
        GC.KeepAlive(procedure);
    }
}
