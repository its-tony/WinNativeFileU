using Xunit;

namespace WinNativeFileU.WindowsIntegration.Tests;

public sealed class OverlayPlacementTests
{
    [Theory]
    [InlineData(500, 500, 0, 0, 1920, 1040, 96, 320, 420, 360, 160)]
    [InlineData(2500, 1400, 0, 0, 2560, 1400, 144, 2020, 1160, 540, 240)]
    [InlineData(-1920, -1080, -1920, -1080, 0, -40, 192, -1920, -1080, 720, 320)]
    [InlineData(100, 80, 0, 0, 200, 120, 288, 0, 0, 200, 120)]
    [InlineData(int.MinValue, int.MinValue, -1920, -1080, 0, -40, 96, -1920, -1080, 360, 160)]
    [InlineData(int.MaxValue, int.MaxValue, 0, 0, 1920, 1040, 96, 1560, 880, 360, 160)]
    public void FitsWorkAreaAtDifferentScalesAndVirtualScreenEdges(int cursorX, int cursorY,
        int left, int top, int right, int bottom, int dpi, int x, int y, int width, int height)
    {
        var placement = OverlayPlacement.Calculate(cursorX, cursorY, left, top, right, bottom, (uint)dpi);
        Assert.Equal(new OverlayPlacement(x, y, width, height), placement);
        Assert.InRange(placement.X, left, right - placement.Width);
        Assert.InRange(placement.Y, top, bottom - placement.Height);
    }

    [Theory]
    [InlineData(0, 0, 2560, 1528, 144, 0, 0, 690)]
    [InlineData(2560, 1528, 2560, 1528, 144, 1870, 838, 690)]
    [InlineData(100, 100, 320, 240, 192, 0, 0, 240)]
    public void RadialSquareFitsAvailableWorkArea(int cursorX, int cursorY, int right, int bottom, int dpi, int x, int y, int expectedHeight)
    {
        var placement = OverlayPlacement.Calculate(cursorX, cursorY, 0, 0, right, bottom, (uint)dpi, 460, 460);
        Assert.Equal(x, placement.X);
        Assert.Equal(y, placement.Y);
        Assert.Equal(expectedHeight, placement.Height);
        Assert.InRange(placement.Width, 1, right);
    }

    [Theory]
    [InlineData(0, 0, 1920, 1080, 0)]
    [InlineData(100, 0, 100, 1080, 144)]
    [InlineData(0, 100, 1920, 50, 144)]
    public void RejectsUnavailableDpiOrInvalidWorkArea(int left, int top, int right, int bottom, int dpi) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OverlayPlacement.Calculate(0, 0, left, top, right, bottom, (uint)dpi));
}
