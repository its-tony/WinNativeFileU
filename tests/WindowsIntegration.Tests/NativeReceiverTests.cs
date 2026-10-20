using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.WindowsIntegration.Tests;

public sealed class NativeReceiverTests
{
    [Theory]
    [InlineData("multiple")]
    [InlineData("moveOnly")]
    [InlineData("cancel")]
    [InlineData("leaveReenter")]
    [InlineData("escapeLeave")]
    [InlineData("unsupported")]
    [InlineData("outside")]
    [InlineData("limit")]
    [InlineData("lateDrop")]
    [InlineData("cancelIdle")]
    [InlineData("leaveAtReceiver")]
    [InlineData("leaveUnknown")]
    [InlineData("heldGateAllowed")]
    [InlineData("heldGateIdle")]
    [InlineData("heldGateEscape")]
    [InlineData("heldGateButton")]
    public void HiddenNativeReceiverHonorsFixtureBoundary(string scenario) => OnSta(() =>
    {
        // New evidence only; never delete preexisting data or use global temp.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "WinNativeFileU.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string run = Path.Combine(directory.FullName, "evidence", "private", "native-tests", Guid.NewGuid().ToString("N"));
        string fixtures = Path.Combine(run, "fixtures");
        Directory.CreateDirectory(fixtures);
        string first = Path.Combine(fixtures, "one.txt"), unicode = Path.Combine(fixtures, "niño-dos.txt");
        File.WriteAllText(first, "native fixture one");
        File.WriteAllText(unicode, "native fixture two");
        byte[] firstHash = SHA256.HashData(File.ReadAllBytes(first));
        byte[] secondHash = SHA256.HashData(File.ReadAllBytes(unicode));
        string log = Path.Combine(run, "journal.jsonl");
        var pointer = new PointerObservation(scenario != "heldGateIdle", scenario == "heldGateEscape");
        bool requireHeld = scenario.StartsWith("heldGate", StringComparison.Ordinal);
        var paths = scenario switch
        {
            "outside" => [Path.Combine(run, "outside.txt")],
            "limit" => Enumerable.Repeat(first, 33).ToArray(),
            _ => new[] { first, unicode }
        };
        var data = new FixtureDataObject(paths, scenario != "unsupported");
        bool? pointerOverReceiver = scenario switch { "leaveAtReceiver" => true, "leaveUnknown" => null, _ => false };
        using (var target = new DragOverlay(fixtures, log, nativeTestMode: true, () => pointer,
            isPointerOverReceiver: () => pointerOverReceiver, requireHeldActivation: requireHeld))
        {
            if (requireHeld && scenario != "heldGateButton") target.ActivateFromHotkey(pointer);
            else target.Arm();
            if (requireHeld)
                Assert.Equal(scenario == "heldGateAllowed" ? InteractionState.AwaitingData : InteractionState.Idle, target.SessionState);
            if (scenario == "cancelIdle")
            {
                string? status = null;
                target.StatusChanged += message => status = message;
                target.Cancel();
                Assert.Equal("Receiver closed. No file operation was performed.", status);
                Assert.Equal(InteractionState.Idle, target.SessionState);
            }
            uint mask = scenario == "moveOnly" ? 2u : 7u;
            uint effect = mask;
            Assert.Equal(0, target.DragEnter(data, 1, default, ref effect));
            bool rejected = scenario is "moveOnly" or "unsupported" or "outside" or "limit" or "cancelIdle"
                or "heldGateIdle" or "heldGateEscape" or "heldGateButton";
            Assert.Equal(rejected ? 0u : 1u, effect);
            if (scenario == "cancel") target.Cancel();
            if (scenario == "leaveReenter")
            {
                target.DragLeave();
                Assert.Equal(InteractionState.AwaitingData, target.SessionState);
                effect = mask;
                target.DragEnter(data, 1, default, ref effect);
                Assert.Equal(1u, effect);
            }
            if (scenario == "escapeLeave")
            {
                pointer = new PointerObservation(true, true);
                target.DragLeave();
                Assert.Equal(InteractionState.Idle, target.SessionState);
            }
            if (scenario == "lateDrop")
            {
                pointer = new PointerObservation(false, false);
                target.DragLeave();
                Assert.Equal(InteractionState.Idle, target.SessionState);
            }
            if (scenario is "leaveAtReceiver" or "leaveUnknown")
            {
                // The source may end after Escape has already been released while
                // the mouse remains held. Leave must not keep a stale receiver.
                target.DragLeave();
                Assert.Equal(InteractionState.Idle, target.SessionState);
            }
            effect = mask;
            target.DragOver(1, default, ref effect);
            bool accepted = !rejected && scenario is not ("cancel" or "escapeLeave" or "lateDrop" or "leaveAtReceiver" or "leaveUnknown");
            Assert.Equal(accepted ? 1u : 0u, effect);
            effect = mask;
            target.Drop(data, 0, default, ref effect);
            Assert.Equal(accepted ? 1u : 0u, effect);
            Assert.Equal(InteractionState.Idle, target.SessionState);
        }
        var events = File.ReadLines(log).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToArray();
        var drops = events.Where(e => e.GetProperty("name").GetString() == "DropAccepted").ToArray();
        bool shouldAccept = scenario is "multiple" or "leaveReenter" or "heldGateAllowed";
        Assert.Equal(shouldAccept ? 1 : 0, drops.Length);
        if (shouldAccept)
        {
            Assert.Equal(2, drops[0].GetProperty("details").GetProperty("count").GetInt32());
            Assert.Equal(unicode, drops[0].GetProperty("details").GetProperty("files")[1].GetString());
            Assert.True(data.MediumsCreated >= 2);
        }
        Assert.Contains(events, e => e.GetProperty("name").GetString() == "RegisterDragDrop" && e.GetProperty("details").GetProperty("hr").GetInt32() == 0);
        Assert.Contains(events, e => e.GetProperty("name").GetString() == "RevokeDragDrop" && e.GetProperty("details").GetProperty("hr").GetInt32() == 0);
        if (requireHeld && scenario != "heldGateAllowed")
            Assert.Contains(events, e => e.GetProperty("name").GetString() == "ActivationDeclined");
        Assert.Equal(firstHash, SHA256.HashData(File.ReadAllBytes(first)));
        Assert.Equal(secondHash, SHA256.HashData(File.ReadAllBytes(unicode)));
        Assert.Equal(2, Directory.GetFiles(fixtures).Length);
    });

    [Theory]
    [InlineData("sector")]
    [InlineData("centerThenSector")]
    [InlineData("sectorThenCenterDrop")]
    [InlineData("reject")]
    public void RadialDropRechecksSelectionWithoutChangingFixtureSafety(string scenario) => OnSta(() =>
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "WinNativeFileU.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string run = Path.Combine(directory.FullName, "evidence", "private", "native-tests", Guid.NewGuid().ToString("N"));
        string fixtures = Path.Combine(run, "fixtures");
        Directory.CreateDirectory(fixtures);
        string file = Path.Combine(fixtures, "radial.txt");
        File.WriteAllText(file, "Disposable radial boundary fixture.");
        byte[] before = SHA256.HashData(File.ReadAllBytes(file));
        string log = Path.Combine(run, "journal.jsonl");
        var presentation = new FakePresentation { Selection = scenario == "centerThenSector" ? null : "PNG" };
        var data = new FixtureDataObject([file]);
        using (var target = new DragOverlay(fixtures, log, true, () => new(true, false), presentation: presentation))
        {
            Assert.NotEqual(0, target.InputWindow);
            target.Arm();
            uint effect = 7;
            target.DragEnter(data, 1, default, ref effect);
            Assert.Equal(scenario == "centerThenSector" ? 0u : 1u, effect);
            presentation.Selection = "PNG";
            effect = 7; target.DragOver(1, default, ref effect);
            Assert.Equal(1u, effect);
            if (scenario == "sectorThenCenterDrop") presentation.Selection = null;
            if (scenario == "reject") target.Cancel();
            effect = 7; target.Drop(data, 0, default, ref effect);
            Assert.Equal(scenario is "sector" or "centerThenSector" ? 1u : 0u, effect);
            Assert.Equal(InteractionState.Idle, target.SessionState);
        }
        Assert.True(presentation.Initialized);
        Assert.True(presentation.Disposed);
        var events = File.ReadLines(log).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToArray();
        var accepted = events.Where(e => e.GetProperty("name").GetString() == "DropAccepted").ToArray();
        Assert.Contains(events, e => e.GetProperty("name").GetString() == "RegisterInputDragDrop" && e.GetProperty("details").GetProperty("hr").GetInt32() == 0);
        Assert.Contains(events, e => e.GetProperty("name").GetString() == "RevokeInputDragDrop" && e.GetProperty("details").GetProperty("hr").GetInt32() == 0);
        Assert.Equal(scenario is "sector" or "centerThenSector" ? 1 : 0, accepted.Length);
        if (accepted.Length > 0)
        {
            Assert.Equal("PNG", accepted[0].GetProperty("details").GetProperty("selectedFormat").GetString());
            Assert.True(accepted[0].GetProperty("details").GetProperty("previewOnly").GetBoolean());
        }
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(file)));
        Assert.Single(Directory.GetFiles(fixtures));
    });

    private sealed class FakePresentation : IOverlayPresentation
    {
        public int WidthDip => 460;
        public int HeightDip => 460;
        public string? Selection { get; set; }
        public bool Initialized { get; private set; }
        public bool Disposed { get; private set; }
        public void Initialize(nint parentWindow) { Assert.NotEqual(0, parentWindow); Initialized = true; }
        public void Resize(int width, int height) { }
        public void Reset() { }
        public void Reject() => Selection = null;
        public string? SelectAt(double x, double y, double width, double height) => Selection;
        public void Dispose() => Disposed = true;
    }

    private static void OnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Native receiver test timed out; inspect private journal.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
