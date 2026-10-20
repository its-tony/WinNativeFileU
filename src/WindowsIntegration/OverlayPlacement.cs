namespace WinNativeFileU.WindowsIntegration;

internal readonly record struct OverlayPlacement(int X, int Y, int Width, int Height)
{
    internal static OverlayPlacement Calculate(int cursorX, int cursorY,
        int left, int top, int right, int bottom, uint dpi, int widthDip = 360, int heightDip = 160)
    {
        if (dpi == 0 || right <= left || bottom <= top || widthDip <= 0 || heightDip <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), "Positive DPI and work area required.");
        int workWidth = checked(right - left), workHeight = checked(bottom - top);
        int width = (int)Math.Min(Math.Max(1L, (long)widthDip * dpi / 96), workWidth);
        int height = (int)Math.Min(Math.Max(1L, (long)heightDip * dpi / 96), workHeight);
        int x = (int)Math.Clamp((long)cursorX - width / 2, left, (long)right - width);
        int y = (int)Math.Clamp((long)cursorY - height / 2, top, (long)bottom - height);
        return new(x, y, width, height);
    }
}
