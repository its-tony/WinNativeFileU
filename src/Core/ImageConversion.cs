namespace WinNativeFileU.Core;

public enum ImageFileFormat { Png, Bmp }
public enum ConversionStage { Validating, Decoding, Encoding, Publishing, Completed }
public enum ConversionError
{
    UnsafePath, UnsupportedInput, InvalidImage, LimitsExceeded, IncompatibleTarget,
    TransparencyNotSupported, OutputExists, IoFailure, CleanupFailed
}

public sealed record ImageInfo(ImageFileFormat Format, uint Width, uint Height);
public sealed record ConversionRequest(string SourcePath, string OutputPath, ImageFileFormat TargetFormat);
public sealed record ConversionProgress(ConversionStage Stage);
public sealed record ConversionResult(string OutputPath, ImageInfo Image, long BytesWritten);

public sealed class ConversionException(ConversionError code, string message, Exception? inner = null) : Exception(message, inner)
{
    public ConversionError Code { get; } = code;
}

public interface IImageConverter
{
    Task<ImageInfo> InspectAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<ConversionResult> ConvertAsync(ConversionRequest request,
        IProgress<ConversionProgress>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>Initial engine boundary, not an extension-based compatibility promise.</summary>
public static class ImageConversionPolicy
{
    public const long MaximumInputBytes = 64L * 1024 * 1024;
    public const ulong MaximumPixels = 16_000_000;
    public const uint MaximumSide = 16_384;

    public static ImageFileFormat? DetectSignature(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> png = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.StartsWith(png)) return ImageFileFormat.Png;
        if (bytes.StartsWith("BM"u8)) return ImageFileFormat.Bmp;
        return null;
    }

    public static bool DimensionsAllowed(uint width, uint height) => width > 0 && height > 0
        && width <= MaximumSide && height <= MaximumSide && (ulong)width * height <= MaximumPixels;

    public static bool CanConvert(ImageFileFormat source, ImageFileFormat target) =>
        Enum.IsDefined(source) && Enum.IsDefined(target) && source != target;

    public static string Extension(ImageFileFormat format) => format switch
    {
        ImageFileFormat.Png => ".png",
        ImageFileFormat.Bmp => ".bmp",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
