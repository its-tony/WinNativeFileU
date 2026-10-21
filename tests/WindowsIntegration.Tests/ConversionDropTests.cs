using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.WindowsIntegration.Tests;

public sealed class ConversionDropTests
{
    [Theory]
    [InlineData("accept")]
    [InlineData("center")]
    [InlineData("disabled")]
    [InlineData("changed")]
    [InlineData("busy")]
    [InlineData("queueReject")]
    [InlineData("moveOnly")]
    [InlineData("tooMany")]
    [InlineData("cancel")]
    public void ExplicitAdapterQueuesOnlyValidatedCopyWithOwnedPaths(string scenario) => OnSta(() =>
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "WinNativeFileU.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        string run = Path.Combine(root.FullName, "evidence", "private", "conversion-drop-tests", Guid.NewGuid().ToString("N"));
        string logs = Path.Combine(run, "logs"); Directory.CreateDirectory(logs);
        string input = Path.Combine(run, "outside-log-root.data"); File.WriteAllText(input, "Synthetic adapter payload, not an image codec trial.");
        byte[] hash = SHA256.HashData(File.ReadAllBytes(input));
        var handler = new Handler(scenario);
        var data = new FixtureDataObject(scenario == "tooMany" ? Enumerable.Repeat(input, 9).ToArray() : [input]);
        var presentation = new Presentation { Selection = scenario == "center" ? null : "PNG" };
        using (var target = new DragOverlay(logs, Path.Combine(logs, "events.jsonl"), true, () => new(true, false), presentation: presentation, fileDropHandler: handler))
        {
            target.Arm(); uint effect = scenario == "moveOnly" ? 2u : 7u;
            target.DragEnter(data, 1, default, ref effect);
            if (scenario == "cancel") target.Cancel();
            effect = scenario == "moveOnly" ? 2u : 7u;
            target.Drop(data, 0, default, ref effect);
            Assert.Equal(scenario == "accept" ? 1u : 0u, effect);
            Assert.Equal(InteractionState.Idle, target.SessionState);
        }
        Assert.Equal(scenario == "accept" ? 1 : 0, handler.Queued.Count);
        if (scenario == "accept")
        {
            Assert.Equal(input, handler.Queued[0]); Assert.Equal("PNG", handler.Chosen); Assert.True(handler.Probes >= 2);
            Assert.True(data.MediumsCreated >= 2); // Native mediums released; managed snapshot remains usable.
        }
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(input)));
    });

    private sealed class Handler(string scenario) : IFileDropHandler
    {
        public bool CanAccept => scenario != "busy";
        public int Probes;
        public List<string> Queued { get; } = [];
        public string? Chosen;
        public IReadOnlyList<string> GetAvailableFormats(IReadOnlyList<string> paths)
        { Probes++; return scenario == "disabled" || (scenario == "changed" && Probes > 1) ? ["BMP"] : ["PNG"]; }
        public bool TryAccept(IReadOnlyList<string> paths, string format)
        {
            if (scenario == "queueReject") return false;
            Queued.AddRange(paths.ToArray()); Chosen = format; return true;
        }
    }
    private sealed class Presentation : IOverlayPresentation
    {
        public int WidthDip => 400; public int HeightDip => 400;
        public string? Selection;
        public void Initialize(nint window) { }
        public void Resize(int width, int height) { }
        public void Reset() { }
        public void Reject() { }
        public string? SelectAt(double x, double y, double width, double height) => Selection;
        public void Dispose() { }
    }
    private static void OnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
