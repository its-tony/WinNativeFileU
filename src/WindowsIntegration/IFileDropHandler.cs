namespace WinNativeFileU.WindowsIntegration;

/// <summary>Explicit non-fixture adapter. Eligibility is bounded/read-only; acceptance must only enqueue owned path copies.</summary>
public interface IFileDropHandler
{
    bool CanAccept { get; }
    IReadOnlyList<string> GetAvailableFormats(IReadOnlyList<string> paths);
    bool TryAccept(IReadOnlyList<string> paths, string format);
}
