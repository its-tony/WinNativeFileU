using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Core.Tests;

public sealed class RadialMenuStateTests
{
    [Theory]
    [InlineData(4, 320)]
    [InlineData(7, 420)]
    [InlineData(12, 512)]
    public void LabelsHitTheirSectorAtDifferentMenuSizes(int count, double size)
    {
        var menu = new RadialMenuState(count);
        for (int i = 0; i < count; i++)
        {
            var point = menu.LabelPoint(i, size);
            Assert.Equal(i, menu.HitTest(point.X, point.Y, size));
        }
    }

    [Fact]
    public void CenterOutsideGapsAndNonfiniteCoordinatesHaveNoAction()
    {
        var menu = new RadialMenuState(4);
        Assert.Null(menu.HitTest(210, 210, 420));
        Assert.Null(menu.HitTest(0, 0, 420));
        Assert.Null(menu.HitTest(315, 105, 420));
        Assert.Null(menu.HitTest(double.NaN, 100, 420));
        Assert.Null(menu.HitTest(double.PositiveInfinity, 210, 420));
    }

    [Fact]
    public void HoverAndKeyboardSelectionDoNotConfirmAndCancelClearsBoth()
    {
        var menu = new RadialMenuState(7);
        menu.Select(4);
        Assert.Null(menu.ConfirmedIndex);
        Assert.Equal(4, menu.Confirm());
        menu.Cancel();
        Assert.Null(menu.SelectedIndex);
        Assert.Null(menu.ConfirmedIndex);
        Assert.Null(menu.Confirm());
    }

    [Fact]
    public void KeyboardWrapsInBothDirectionsWithoutOverflow()
    {
        var menu = new RadialMenuState(7);
        menu.Move(-1);
        Assert.Equal(6, menu.SelectedIndex);
        menu.Move(1);
        Assert.Equal(0, menu.SelectedIndex);
        menu.Move(int.MaxValue);
        Assert.InRange(menu.SelectedIndex!.Value, 0, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => menu.Select(7));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    public void RejectsUnusableSectorCounts(int count) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadialMenuState(count));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void RejectsInvalidLayoutSize(double size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadialMenuState(7).HitTest(0, 0, size));
}
