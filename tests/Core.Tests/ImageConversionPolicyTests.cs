using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Core.Tests;

public sealed class ImageConversionPolicyTests
{
    [Fact]
    public void SignaturesDoNotTrustFilenameOrTruncatedPngHeader()
    {
        Assert.Equal(ImageFileFormat.Png, ImageConversionPolicy.DetectSignature([137, 80, 78, 71, 13, 10, 26, 10]));
        Assert.Equal(ImageFileFormat.Bmp, ImageConversionPolicy.DetectSignature("BMdata"u8));
        Assert.Null(ImageConversionPolicy.DetectSignature("pretend.png"u8));
        Assert.Null(ImageConversionPolicy.DetectSignature([137, 80, 78, 71]));
        Assert.Null(ImageConversionPolicy.DetectSignature([]));
    }

    [Theory]
    [InlineData(0u, 5u, false)]
    [InlineData(4000u, 4000u, true)]
    [InlineData(4001u, 4000u, false)]
    [InlineData(16385u, 1u, false)]
    [InlineData(uint.MaxValue, uint.MaxValue, false)]
    public void PixelAndSideLimitsDoNotOverflow(uint width, uint height, bool allowed) =>
        Assert.Equal(allowed, ImageConversionPolicy.DimensionsAllowed(width, height));

    [Fact]
    public void OnlyTwoRealCrossFormatConversionsAreAdvertised()
    {
        Assert.True(ImageConversionPolicy.CanConvert(ImageFileFormat.Png, ImageFileFormat.Bmp));
        Assert.True(ImageConversionPolicy.CanConvert(ImageFileFormat.Bmp, ImageFileFormat.Png));
        Assert.False(ImageConversionPolicy.CanConvert(ImageFileFormat.Png, ImageFileFormat.Png));
        Assert.False(ImageConversionPolicy.CanConvert((ImageFileFormat)99, ImageFileFormat.Png));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageConversionPolicy.Extension((ImageFileFormat)99));
    }
}
