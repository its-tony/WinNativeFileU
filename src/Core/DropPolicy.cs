namespace WinNativeFileU.Core;

/// <summary>Restrictive policy for the fixture-only M1 experiment, with no file I/O.</summary>
public static class DropPolicy
{
    public const uint Copy = 1;
    public const int MaximumFiles = 32;
    public static uint Effect(uint allowed, bool accepted, bool cancelled) =>
        accepted && !cancelled && (allowed & Copy) != 0 ? Copy : 0;

    public static bool IsWithinFixtureRoot(string root, string path)
    {
        // Explicit Windows drive paths only; exclude UNC, device and drive-relative paths.
        static bool LocalDrive(string p) => p.Length >= 3 && char.IsAsciiLetter(p[0])
            && p[1] == ':' && p[2] == '\\' && !p.Contains('/') && !p[2..].Contains(':');
        if (!LocalDrive(root) || !LocalDrive(path)) return false;
        var basePath = Path.GetFullPath(root).TrimEnd('\\') + "\\";
        var full = Path.GetFullPath(path);
        return full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }
}
