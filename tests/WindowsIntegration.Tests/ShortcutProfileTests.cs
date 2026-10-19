using Xunit;

namespace WinNativeFileU.WindowsIntegration.Tests;

public sealed class ShortcutProfileTests
{
    [Fact]
    public void UnknownProfileIsRejectedBeforePathsOrNativeResourcesAreOpened()
    {
        // Empty paths would fail later validation: profile rejection must happen first.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DragOverlay(string.Empty, string.Empty, (PrototypeShortcutProfile)int.MaxValue));
    }
}
