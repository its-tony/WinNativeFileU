using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Core.Tests;

public sealed class ImageBatchPolicyTests
{
    [Fact]
    public void OnlyCommonRealConversionsAreOffered()
    {
        Assert.Equal([ImageFileFormat.Png], ImageBatchPolicy.CommonTargets([ImageFileFormat.Bmp, ImageFileFormat.Bmp]));
        Assert.Equal([ImageFileFormat.Bmp], ImageBatchPolicy.CommonTargets([ImageFileFormat.Png]));
        Assert.Empty(ImageBatchPolicy.CommonTargets([ImageFileFormat.Png, ImageFileFormat.Bmp]));
        Assert.Empty(ImageBatchPolicy.CommonTargets([]));
        Assert.Empty(ImageBatchPolicy.CommonTargets([(ImageFileFormat)99]));
        Assert.Empty(ImageBatchPolicy.CommonTargets(Enumerable.Repeat(ImageFileFormat.Bmp, 9)));
    }
}
