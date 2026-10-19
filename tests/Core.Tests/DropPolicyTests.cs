using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Core.Tests;

public sealed class DropPolicyTests
{
    [Theory]
    [InlineData(0u, true, false, 0u)]
    [InlineData(2u, true, false, 0u)]
    [InlineData(7u, true, false, 1u)]
    [InlineData(1u, false, false, 0u)]
    [InlineData(1u, true, true, 0u)]
    public void NeverRequestsMoveAndHonorsCancellation(uint mask, bool accepted, bool cancelled, uint expected) =>
        Assert.Equal(expected, DropPolicy.Effect(mask, accepted, cancelled));

    [Theory]
    [InlineData(@"C:\fixtures\niño.txt", true)]
    [InlineData(@"C:\fixtures-other\file.txt", false)]
    [InlineData(@"C:\fixtures\..\outside.txt", false)]
    [InlineData(@"C:relative.txt", false)]
    [InlineData(@"\\server\share\file.txt", false)]
    [InlineData(@"\\?\C:\fixtures\file.txt", false)]
    [InlineData(@"C:\fixtures\file.txt:stream", false)]
    public void FixtureBoundaryRejectsSiblingTraversalNetworkAndDevicePaths(string path, bool expected) =>
        Assert.Equal(expected, DropPolicy.IsWithinFixtureRoot(@"C:\fixtures", path));
}
