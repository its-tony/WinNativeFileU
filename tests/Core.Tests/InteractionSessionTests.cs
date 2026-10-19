using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Core.Tests;

public sealed class InteractionSessionTests
{
    [Fact]
    public void ActivationAloneCannotSupplyFiles()
    {
        var session = new InteractionSession();
        session.Activate();
        Assert.Empty(session.Files);
        Assert.Throws<InvalidOperationException>(() => session.ReceiveDrop(["fixture.txt"]));
    }

    [Fact]
    public void CancellationDiscardsSessionAndRejectsLateDrop()
    {
        var session = new InteractionSession();
        session.Activate();
        session.EnterAcceptedTarget();
        session.Reset();
        Assert.Equal(InteractionState.Idle, session.State);
        Assert.Empty(session.Files);
        Assert.Throws<InvalidOperationException>(() => session.ReceiveDrop(["fixture.txt"]));
    }

    [Fact]
    public void AcceptedDropUsesIndependentReadOnlySnapshot()
    {
        var paths = new[] { @"C:\Fixtures\niño.png", @"C:\Fixtures\two.jpg" };
        var session = new InteractionSession();
        session.Activate();
        session.EnterAcceptedTarget();
        session.ReceiveDrop(paths);
        paths[0] = "changed";
        Assert.Equal(@"C:\Fixtures\niño.png", session.Files[0]);
        Assert.Equal(InteractionState.Received, session.State);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)session.Files)[0] = "changed");
        Assert.Throws<InvalidOperationException>(session.Activate);
        session.Reset();
        Assert.Empty(session.Files);
    }

    [Fact]
    public void InvalidDropDoesNotCommitPartialSelection()
    {
        var session = new InteractionSession();
        session.Activate();
        session.EnterAcceptedTarget();
        Assert.Throws<ArgumentException>(() => session.ReceiveDrop(["valid", " "]));
        Assert.Equal(InteractionState.Tracking, session.State);
        Assert.Empty(session.Files);
        Assert.Throws<ArgumentException>(() => session.ReceiveDrop([]));
    }
}
