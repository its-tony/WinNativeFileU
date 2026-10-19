namespace WinNativeFileU.Core;

/// <summary>Event-time hints, not proof of the source application's drag state.</summary>
public readonly record struct PointerObservation(bool LeftButtonDown, bool EscapeDown)
{
    public bool KeepReceiverAfterLeave => LeftButtonDown && !EscapeDown;
}
