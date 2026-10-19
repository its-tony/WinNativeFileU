namespace WinNativeFileU.Core;

public enum InteractionState { Idle, AwaitingData, Tracking, Received }

/// <summary>
/// Pure session boundary for a future Windows adapter. It neither observes drags
/// nor cancels the source application's drag operation.
/// </summary>
public sealed class InteractionSession
{
    private IReadOnlyList<string> files = Array.Empty<string>();
    public InteractionState State { get; private set; }
    public IReadOnlyList<string> Files => files;

    public void Activate()
    {
        if (State != InteractionState.Idle)
            throw new InvalidOperationException("Finish or cancel the current session first.");
        State = InteractionState.AwaitingData;
    }

    // The adapter must call this only after accepting a real data object.
    public void EnterAcceptedTarget()
    {
        if (State != InteractionState.AwaitingData)
            throw new InvalidOperationException("An activation is required.");
        State = InteractionState.Tracking;
    }

    // No filesystem operation is performed. Format/path validation belongs to
    // the future Windows adapter; these values must come from an accepted Drop.
    public void ReceiveDrop(IEnumerable<string> acceptedPaths)
    {
        if (State != InteractionState.Tracking)
            throw new InvalidOperationException("A drop requires an accepted target.");
        ArgumentNullException.ThrowIfNull(acceptedPaths);
        var snapshot = acceptedPaths.ToArray();
        if (snapshot.Length == 0 || snapshot.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("A nonempty selection is required.", nameof(acceptedPaths));
        files = Array.AsReadOnly(snapshot);
        State = InteractionState.Received;
    }

    public void Reset()
    {
        files = Array.Empty<string>();
        State = InteractionState.Idle;
    }
}
