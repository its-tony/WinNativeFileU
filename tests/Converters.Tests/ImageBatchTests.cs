using System.Security.Cryptography;
using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Converters.Tests;

public sealed class ImageBatchTests
{
    [Fact]
    public async Task RealBatchHasUniqueOutputsAndPreservesInputsAndExistingFiles()
    {
        var (inputs, output) = Run();
        string one = Path.Combine(inputs, "niño.bmp"), two = Path.Combine(inputs, "second.bmp");
        ConverterTests.WriteBmp(one); ConverterTests.WriteBmp(two);
        var hashes = new[] { one, two }.Select(Hash).ToArray();
        string sentinel = Path.Combine(output, "existing.png"); File.WriteAllText(sentinel, "Existing user result");
        var engine = new LocalImageConverter();
        var result = await new ImageBatchConverter(engine).ConvertAsync([one, two], output, ImageFileFormat.Png);
        Assert.False(result.Cancelled); Assert.Equal(2, result.Files.Count);
        Assert.All(result.Files, file => { Assert.NotNull(file.Result); Assert.Null(file.Error); Assert.StartsWith(output, file.Result.OutputPath); });
        Assert.NotEqual(result.Files[0].Result!.OutputPath, result.Files[1].Result!.OutputPath);
        foreach (var file in result.Files) Assert.Equal(new ImageInfo(ImageFileFormat.Png, 3, 2), await engine.InspectAsync(file.Result!.OutputPath));
        Assert.Equal(hashes[0], Hash(one)); Assert.Equal(hashes[1], Hash(two)); Assert.Equal("Existing user result", File.ReadAllText(sentinel));
        Assert.Empty(Directory.GetFiles(output, "*.partial"));
    }

    [Fact]
    public async Task CancelledBatchRetainsOnlyPublishedResults()
    {
        var (inputs, output) = Run(); string one = Path.Combine(inputs, "one.bmp"), two = Path.Combine(inputs, "two.bmp");
        ConverterTests.WriteBmp(one); ConverterTests.WriteBmp(two); using var cancellation = new CancellationTokenSource();
        var result = await new ImageBatchConverter(new CancelAfterFirst(cancellation)).ConvertAsync([one, two], output, ImageFileFormat.Png, cancellationToken: cancellation.Token);
        Assert.True(result.Cancelled); Assert.Single(result.Files); Assert.NotNull(result.Files[0].Result);
        Assert.Single(Directory.GetFiles(output)); Assert.Empty(Directory.GetFiles(output, "*.partial"));
    }

    [Fact]
    public async Task CancelAtPublishingRemovesCurrentTemporaryWithoutPublishing()
    {
        var (inputs, output) = Run(); string one = Path.Combine(inputs, "one.bmp"); ConverterTests.WriteBmp(one);
        using var cancellation = new CancellationTokenSource();
        var observer = new CancelProgress(cancellation);
        var result = await new ImageBatchConverter(new LocalImageConverter()).ConvertAsync([one], output, ImageFileFormat.Png, observer, cancellation.Token);
        Assert.True(result.Cancelled); Assert.Empty(result.Files); Assert.Empty(Directory.GetFiles(output));
    }

    [Fact]
    public async Task ProbeRejectsUnknownDuplicateMixedAndTooManyFilesBeforeWriting()
    {
        var (inputs, output) = Run(); string one = Path.Combine(inputs, "one.bmp"), fake = Path.Combine(inputs, "fake.png");
        ConverterTests.WriteBmp(one); File.WriteAllText(fake, "Not image data");
        Assert.Throws<ConversionException>(() => LocalImageConverter.ProbeSelection([one, one]));
        Assert.Throws<ConversionException>(() => LocalImageConverter.ProbeSelection([fake]));
        Assert.Throws<ConversionException>(() => LocalImageConverter.ProbeSelection(Enumerable.Repeat(one, 9).ToArray()));
        var engine = new LocalImageConverter(); string png = Path.Combine(inputs, "one.png");
        await engine.ConvertAsync(new(one, png, ImageFileFormat.Png));
        Assert.Throws<ConversionException>(() => LocalImageConverter.ProbeSelection([one, png]));
        Assert.Empty(Directory.GetFiles(output));
    }

    [Fact]
    public async Task FullyValidatedCorruptImageProducesTypedFailureAndNoOutput()
    {
        var (inputs, output) = Run(); string one = Path.Combine(inputs, "source.bmp"), png = Path.Combine(inputs, "corrupt.png");
        ConverterTests.WriteBmp(one); await new LocalImageConverter().ConvertAsync(new(one, png, ImageFileFormat.Png));
        byte[] data = File.ReadAllBytes(png); data[^1] ^= 1; File.WriteAllBytes(png, data);
        Assert.Equal([ImageFileFormat.Bmp], LocalImageConverter.ProbeSelection([png])); // Header eligibility is not full validation.
        var result = await new ImageBatchConverter(new LocalImageConverter()).ConvertAsync([png], output, ImageFileFormat.Bmp);
        Assert.Equal(ConversionError.InvalidImage, Assert.Single(result.Files).Error); Assert.Empty(Directory.GetFiles(output));
    }

    private sealed class CancelProgress(CancellationTokenSource cancellation) : IProgress<BatchProgress>
    { public void Report(BatchProgress value) { if (value.Stage == ConversionStage.Publishing) cancellation.Cancel(); } }
    private sealed class CancelAfterFirst(CancellationTokenSource cancellation) : IImageConverter
    {
        private readonly LocalImageConverter actual = new();
        public Task<ImageInfo> InspectAsync(string path, CancellationToken token = default) => actual.InspectAsync(path, token);
        public async Task<ConversionResult> ConvertAsync(ConversionRequest request, IProgress<ConversionProgress>? progress = null, CancellationToken cancellationToken = default)
        { var result = await actual.ConvertAsync(request, progress, cancellationToken); cancellation.Cancel(); return result; }
    }
    private static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));
    private static (string Inputs, string Output) Run()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "WinNativeFileU.slnx"))) root = root.Parent;
        Assert.NotNull(root); string run = Path.Combine(root.FullName, "evidence", "private", "batch-tests", Guid.NewGuid().ToString("N"));
        string inputs = Path.Combine(run, "inputs"), output = Path.Combine(run, "outputs"); Directory.CreateDirectory(inputs); Directory.CreateDirectory(output); return (inputs, output);
    }
}
