namespace WinNativeFileU.WindowsIntegration;

/// <summary>Optional owned UI on the receiver STA. Labels are diagnostic examples, not conversion capabilities.</summary>
public interface IOverlayPresentation : IDisposable
{
    int WidthDip { get; }
    int HeightDip { get; }
    void Initialize(nint parentWindow);
    void Resize(int clientWidth, int clientHeight);
    void Reset();
    void Reject();
    string? SelectAt(double x, double y, double clientWidth, double clientHeight);
}
