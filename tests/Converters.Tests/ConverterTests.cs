using System.Buffers.Binary;
using System.Security.Cryptography;
using WinNativeFileU.Converters;
using WinNativeFileU.Core;
using Windows.Graphics.Imaging;
using Xunit;

namespace WinNativeFileU.Converters.Tests;

public sealed class ConverterTests
{
    private readonly LocalImageConverter converter = new();

    [Fact]
    public async Task NativeCodecRoundTripPreservesPixelsDimensionsUnicodeAndOriginal()
    {
        var run = new Run();
        // BMP bytes are generated independently of the codec under test; extension intentionally misleading.
        string original = run.File("niño original.data");
        WriteBmp(original);
        byte[] hash = Hash(original);
        string png = run.File("nuevo.png"), bmp = run.File("nuevo.bmp");
        var stages = new List<ConversionStage>();
        var inspected = await converter.InspectAsync(original);
        Assert.Equal(new ImageInfo(ImageFileFormat.Bmp, 3, 2), inspected);
        var first = await converter.ConvertAsync(new(original, png, ImageFileFormat.Png), new Observer(p => stages.Add(p.Stage)));
        Assert.Equal(new ImageInfo(ImageFileFormat.Png, 3, 2), first.Image);
        Assert.Equal(new[] { ConversionStage.Validating, ConversionStage.Decoding, ConversionStage.Encoding, ConversionStage.Publishing, ConversionStage.Completed }, stages);
        Assert.True(first.BytesWritten > 0);
        Assert.Equal(Pixels, await DecodePixels(png));
        byte[] pngHash = Hash(png);
        await converter.ConvertAsync(new(png, bmp, ImageFileFormat.Bmp));
        Assert.Equal(Pixels, await DecodePixels(bmp));
        Assert.Equal(hash, Hash(original));
        Assert.Equal(pngHash, Hash(png));
        Assert.Empty(run.Partials());
    }

    [Fact]
    public async Task ExistingDestinationAndSameSourceCannotBeReplaced()
    {
        var run = new Run(); string input = run.File("original.bmp"), output = run.File("existing.png");
        WriteBmp(input); File.WriteAllText(output, "Existing user content");
        byte[] sourceHash = Hash(input), outputHash = Hash(output);
        Assert.Equal(ConversionError.OutputExists, (await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Png)))).Code);
        Assert.Equal(ConversionError.OutputExists, (await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, input, ImageFileFormat.Bmp)))).Code);
        Assert.Equal(sourceHash, Hash(input)); Assert.Equal(outputHash, Hash(output));
        Assert.Empty(run.Partials());
    }

    [Fact]
    public async Task DestinationCreatedAtPublishWinsWithoutBeingOverwritten()
    {
        var run = new Run(); string input = run.File("input.bmp"), output = run.File("race.png"); WriteBmp(input);
        var observer = new Observer(p => { if (p.Stage == ConversionStage.Publishing) File.WriteAllText(output, "Other writer wins"); });
        var error = await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Png), observer));
        Assert.Equal(ConversionError.OutputExists, error.Code);
        Assert.Equal("Other writer wins", File.ReadAllText(output));
        Assert.Empty(run.Partials());
    }

    [Fact]
    public async Task ConcurrentWritersProduceOneCompleteOutputAndOneCollision()
    {
        var run = new Run(); string input = run.File("input.bmp"), output = run.File("shared.png"); WriteBmp(input);
        async Task<ConversionError?> Attempt()
        {
            try { await converter.ConvertAsync(new(input, output, ImageFileFormat.Png)); return null; }
            catch (ConversionException ex) { return ex.Code; }
        }
        var results = await Task.WhenAll(Task.Run(Attempt), Task.Run(Attempt));
        Assert.Single(results, x => x == null);
        Assert.Single(results, x => x == ConversionError.OutputExists);
        Assert.Equal(Pixels, await DecodePixels(output));
        Assert.Empty(run.Partials());
    }

    [Theory]
    [InlineData(ConversionStage.Validating)]
    [InlineData(ConversionStage.Decoding)]
    [InlineData(ConversionStage.Encoding)]
    [InlineData(ConversionStage.Publishing)]
    public async Task CancellationBeforePublishLeavesNoResultOrTemporary(ConversionStage stage)
    {
        var run = new Run(); string input = run.File("input.bmp"), output = run.File("cancel.png"); WriteBmp(input);
        byte[] hash = Hash(input); using var cancellation = new CancellationTokenSource();
        bool sawTemporary = false;
        var observer = new Observer(p =>
        {
            if (p.Stage == ConversionStage.Publishing) sawTemporary = run.Partials().Length == 1;
            if (p.Stage == stage) cancellation.Cancel();
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Png), observer, cancellation.Token));
        if (stage == ConversionStage.Publishing) Assert.True(sawTemporary);
        Assert.False(File.Exists(output)); Assert.Empty(run.Partials()); Assert.Equal(hash, Hash(input));
    }

    [Fact]
    public async Task LateCancellationAndObserverFailureDoNotErasePublishedResult()
    {
        var run = new Run(); string input = run.File("input.bmp"), output = run.File("complete.png"); WriteBmp(input);
        using var cancellation = new CancellationTokenSource();
        var observer = new Observer(p =>
        {
            if (p.Stage == ConversionStage.Completed) cancellation.Cancel();
            throw new InvalidOperationException("Observer failure");
        });
        var result = await converter.ConvertAsync(new(input, output, ImageFileFormat.Png), observer, cancellation.Token);
        Assert.Equal(output, result.OutputPath); Assert.Equal(Pixels, await DecodePixels(output)); Assert.Empty(run.Partials());
    }

    [Fact]
    public async Task AlphaIsRejectedForBmpRatherThanSilentlyFlattened()
    {
        var run = new Run(); string input = run.File("alpha.png"), output = run.File("alpha.bmp");
        byte[] rgba = [.. Pixels]; rgba[3] = 64;
        await WritePng(input, rgba);
        byte[] hash = Hash(input);
        var error = await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Bmp)));
        Assert.Equal(ConversionError.TransparencyNotSupported, error.Code);
        Assert.False(File.Exists(output)); Assert.Empty(run.Partials()); Assert.Equal(hash, Hash(input));
    }

    [Fact]
    public async Task BadPngCrcAndTruncationAreRejectedBeforePublication()
    {
        var run = new Run(); string input = run.File("broken.png"), output = run.File("broken.bmp");
        await WritePng(input, Pixels);
        byte[] bytes = File.ReadAllBytes(input); bytes[^1] ^= 1; File.WriteAllBytes(input, bytes);
        Assert.Equal(ConversionError.InvalidImage, (await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Bmp)))).Code);
        File.WriteAllBytes(input, bytes[..25]);
        Assert.Equal(ConversionError.InvalidImage, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(input))).Code);
        Assert.False(File.Exists(output)); Assert.Empty(run.Partials());
    }

    [Fact]
    public async Task ApngMarkerCannotBeSilentlyTurnedIntoAStaticImage()
    {
        var run = new Run(); string input = run.File("animated-marker.png"); await WritePng(input, Pixels);
        byte[] bytes = File.ReadAllBytes(input);
        // Bounded acTL marker inserted after IHDR. Policy rejects the marker before any animation decoding.
        byte[] marker = new byte[20]; BinaryPrimitives.WriteUInt32BigEndian(marker, 8); "acTL"u8.CopyTo(marker.AsSpan(4));
        File.WriteAllBytes(input, [.. bytes[..33], .. marker, .. bytes[33..]]);
        Assert.Equal(ConversionError.UnsupportedInput, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(input))).Code);
    }

    [Fact]
    public async Task OversizedBmpDimensionsAndInputBytesAreRejected()
    {
        var run = new Run(); string input = run.File("huge.bmp"); WriteBmp(input);
        byte[] bytes = File.ReadAllBytes(input); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18), 20_000); File.WriteAllBytes(input, bytes);
        Assert.Equal(ConversionError.LimitsExceeded, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(input))).Code);
        using (var file = new FileStream(input, FileMode.Open, FileAccess.Write)) file.SetLength(ImageConversionPolicy.MaximumInputBytes + 1);
        Assert.Equal(ConversionError.LimitsExceeded, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(input))).Code);
    }

    [Theory]
    [InlineData("relative.png")]
    [InlineData(@"\\server\share\x.png")]
    [InlineData(@"\\?\C:\x.png")]
    [InlineData(@"C:\x.png:stream")]
    [InlineData(@"C:\NUL.png")]
    [InlineData(@"C:\COM1.bmp")]
    [InlineData(@"C:\COM¹.bmp")]
    [InlineData(@"C:\NUL .png")]
    [InlineData(@"C:\bad.png ")]
    public async Task UnsafeSyntaxIsRejectedBeforeOpeningAnything(string input) =>
        Assert.Equal(ConversionError.UnsafePath, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(input))).Code);

    [Fact]
    public async Task UnknownDataDirectoryAndWrongOutputFormatFailClearly()
    {
        var run = new Run(); string input = run.File("input.bmp"); File.WriteAllText(input, "Not really a bitmap");
        Assert.Equal(ConversionError.UnsupportedInput, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(input))).Code);
        Assert.Equal(ConversionError.UnsafePath, (await Assert.ThrowsAsync<ConversionException>(() => converter.InspectAsync(run.Root))).Code);
        WriteBmp(input);
        Assert.Equal(ConversionError.IncompatibleTarget, (await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, run.File("wrong.jpg"), ImageFileFormat.Png)))).Code);
        Assert.Equal(ConversionError.IncompatibleTarget, (await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, run.File("same.bmp"), ImageFileFormat.Bmp)))).Code);
        Assert.Empty(run.Partials());
    }

    [Fact]
    public async Task CleanupFailureIsReportedInsteadOfClaimingNoTemporaryRemains()
    {
        var run = new Run(); string input = run.File("input.bmp"), output = run.File("blocked.png"); WriteBmp(input);
        using var cancellation = new CancellationTokenSource(); FileStream? lockFile = null;
        var observer = new Observer(p =>
        {
            if (p.Stage != ConversionStage.Publishing) return;
            lockFile = new FileStream(Assert.Single(run.Partials()), FileMode.Open, FileAccess.Read, FileShare.Read);
            cancellation.Cancel();
        });
        try
        {
            var error = await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Png), observer, cancellation.Token));
            Assert.Equal(ConversionError.CleanupFailed, error.Code);
            Assert.Single(run.Partials()); Assert.False(File.Exists(output));
        }
        finally { lockFile?.Dispose(); }
        // Retain this deliberately blocked temporary as private evidence, not a universal cleanup claim.
    }

    [Fact]
    public async Task ValidContainerWithInvalidDeflateIsReportedAsAnImageFailure()
    {
        var run = new Run(); string input = run.File("bad-data.png"), output = run.File("bad.bmp"); await WritePng(input, Pixels);
        byte[] bytes = File.ReadAllBytes(input);
        for (int offset = 8; offset < bytes.Length;)
        {
            int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset)));
            if (bytes.AsSpan(offset + 4, 4).SequenceEqual("IDAT"u8))
            {
                bytes[offset + 8] = 0; // Invalid zlib header, with container CRC repaired.
                uint crc = uint.MaxValue;
                foreach (byte value in bytes.AsSpan(offset + 4, length + 4))
                {
                    crc ^= value;
                    for (int bit = 0; bit < 8; bit++) crc = (crc & 1) == 0 ? crc >> 1 : 0xEDB88320u ^ (crc >> 1);
                }
                BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset + 8 + length), ~crc);
                break;
            }
            offset += length + 12;
        }
        File.WriteAllBytes(input, bytes);
        var error = await Assert.ThrowsAsync<ConversionException>(() => converter.ConvertAsync(new(input, output, ImageFileFormat.Bmp)));
        Assert.Equal(ConversionError.InvalidImage, error.Code);
        Assert.False(File.Exists(output)); Assert.Empty(run.Partials());
    }

    private sealed class Observer(Action<ConversionProgress> callback) : IProgress<ConversionProgress>
    { public void Report(ConversionProgress value) => callback(value); }

    private sealed class Run
    {
        public string Root { get; }
        public Run()
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !System.IO.File.Exists(Path.Combine(root.FullName, "WinNativeFileU.slnx"))) root = root.Parent;
            Assert.NotNull(root);
            Root = Path.Combine(root.FullName, "evidence", "private", "converter-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }
        public string File(string name) => Path.Combine(Root, name);
        public string[] Partials() => Directory.GetFiles(Root, ".winnu-*.partial");
    }

    private static readonly byte[] Pixels = [0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255, 0, 255, 255, 255, 255, 0, 255, 255, 255, 255, 255, 255];
    private static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

    internal static void WriteBmp(string path)
    {
        byte[] bmp = new byte[78]; "BM"u8.CopyTo(bmp); BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54); BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), 3); BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), -2);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1); BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), 24);
        for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++) Pixels.AsSpan((y * 3 + x) * 4, 3).CopyTo(bmp.AsSpan(54 + y * 12 + x * 3));
        File.WriteAllBytes(path, bmp);
    }

    private static async Task WritePng(string path, byte[] pixels)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);
        using var stream = file.AsRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, 3, 2, 96, 96, pixels);
        await encoder.FlushAsync();
    }

    private static async Task<byte[]> DecodePixels(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var stream = file.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        Assert.Equal(3u, decoder.PixelWidth); Assert.Equal(2u, decoder.PixelHeight);
        return (await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, new BitmapTransform(), ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb)).DetachPixelData();
    }
}
