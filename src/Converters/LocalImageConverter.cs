using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using WinNativeFileU.Core;
using Windows.Graphics.Imaging;

namespace WinNativeFileU.Converters;

/// <summary>Single-image Windows codec engine. All writes are new outputs; no original is mutated.</summary>
public sealed class LocalImageConverter : IImageConverter
{
    /// <summary>Bounded header-only eligibility probe for UI/OLE. Full container/pixel validation still runs at conversion.</summary>
    public static ImageFileFormat ProbeLocalImage(string sourcePath)
    {
        string path = LocalPath(sourcePath);
        CheckAncestors(path, input: true);
        using var file = OpenSource(path);
        Span<byte> header = stackalloc byte[32];
        int count = (int)Math.Min(file.Length, header.Length);
        file.ReadExactly(header[..count]);
        var format = ImageConversionPolicy.DetectSignature(header[..count]);
        if (format == null) throw Error(ConversionError.UnsupportedInput, "Choose actual PNG or BMP images.");
        uint width, height;
        if (format == ImageFileFormat.Png)
        {
            if (count < 29 || !header.Slice(12, 4).SequenceEqual("IHDR"u8)) throw Error(ConversionError.InvalidImage, "PNG header is incomplete.");
            if (header[24] != 8 || header[25] is not (2 or 6) || header[28] != 0)
                throw Error(ConversionError.UnsupportedInput, "Choose noninterlaced 8-bit RGB/RGBA PNG images.");
            width = BinaryPrimitives.ReadUInt32BigEndian(header[16..]); height = BinaryPrimitives.ReadUInt32BigEndian(header[20..]);
        }
        else
        {
            if (count < 30) throw Error(ConversionError.InvalidImage, "BMP header is incomplete.");
            if (BinaryPrimitives.ReadUInt32LittleEndian(header[14..]) is not (40 or 108 or 124)
                || BinaryPrimitives.ReadUInt16LittleEndian(header[28..]) is not (24 or 32))
                throw Error(ConversionError.UnsupportedInput, "Choose 24/32-bit Windows BMP images.");
            int w = BinaryPrimitives.ReadInt32LittleEndian(header[18..]); long h = Math.Abs((long)BinaryPrimitives.ReadInt32LittleEndian(header[22..]));
            if (w <= 0 || h > uint.MaxValue) throw Error(ConversionError.LimitsExceeded, "Image dimensions are outside the supported limits.");
            width = (uint)w; height = (uint)h;
        }
        if (!ImageConversionPolicy.DimensionsAllowed(width, height)) throw Error(ConversionError.LimitsExceeded, "Image exceeds the supported dimensions.");
        return format.Value;
    }

    public static IReadOnlyList<ImageFileFormat> ProbeSelection(IReadOnlyList<string> paths)
    {
        if (paths.Count is 0 or > ImageBatchPolicy.MaximumFiles || paths.Any(path => path.Length > 1024)
            || paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Count)
            throw Error(ConversionError.LimitsExceeded, "Choose one to eight distinct images.");
        var targets = ImageBatchPolicy.CommonTargets(paths.Select(ProbeLocalImage));
        if (targets.Count == 0) throw Error(ConversionError.IncompatibleTarget, "Choose images with the same source format for this batch.");
        return targets;
    }

    public static string ValidateLocalDirectory(string path)
    {
        string full = LocalPath(path);
        CheckAncestors(full, input: false);
        return full;
    }

    public async Task<ImageInfo> InspectAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            string path = LocalPath(sourcePath);
            CheckAncestors(path, input: true);
            await using var file = OpenSource(path);
            ImageFileFormat format = await CheckContainerAsync(file, cancellationToken);
            file.Position = 0;
            using var random = file.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(DecoderId(format), random).AsTask(cancellationToken);
            return CheckDecoder(decoder, format);
        }
        catch (OperationCanceledException) { throw; }
        catch (ConversionException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or COMException or ArgumentException)
        { throw Classify(ex); }
    }

    public async Task<ConversionResult> ConvertAsync(ConversionRequest request,
        IProgress<ConversionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? temporary = null;
        Exception? failure = null;
        try
        {
            Report(progress, ConversionStage.Validating);
            cancellationToken.ThrowIfCancellationRequested();
            string source = LocalPath(request.SourcePath), output = LocalPath(request.OutputPath);
            CheckAncestors(source, input: true);
            CheckAncestors(Path.GetDirectoryName(output)!, input: false);
            if (!Enum.IsDefined(request.TargetFormat) || !Path.GetExtension(output).Equals(ImageConversionPolicy.Extension(request.TargetFormat), StringComparison.OrdinalIgnoreCase))
                throw Error(ConversionError.IncompatibleTarget, "Choose a matching PNG or BMP output extension.");
            if (source.Equals(output, StringComparison.OrdinalIgnoreCase) || File.Exists(output) || Directory.Exists(output))
                throw Error(ConversionError.OutputExists, "The destination already exists. Choose a new name.");

            await using var inputFile = OpenSource(source);
            var sourceFormat = await CheckContainerAsync(inputFile, cancellationToken);
            if (!ImageConversionPolicy.CanConvert(sourceFormat, request.TargetFormat))
                throw Error(ConversionError.IncompatibleTarget, "This engine converts PNG to BMP or BMP to PNG.");
            inputFile.Position = 0;
            using var input = inputFile.AsRandomAccessStream();
            Report(progress, ConversionStage.Decoding);
            cancellationToken.ThrowIfCancellationRequested();
            var decoder = await BitmapDecoder.CreateAsync(DecoderId(sourceFormat), input).AsTask(cancellationToken);
            var info = CheckDecoder(decoder, sourceFormat);
            var provider = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                new BitmapTransform(), ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb).AsTask(cancellationToken);
            byte[] pixels = provider.DetachPixelData();
            if (pixels.LongLength != (long)info.Width * info.Height * 4)
                throw Error(ConversionError.InvalidImage, "The decoder returned an unexpected pixel buffer.");
            if (request.TargetFormat == ImageFileFormat.Bmp)
                for (int i = 3; i < pixels.Length; i += 4)
                    if (pixels[i] != 255) throw Error(ConversionError.TransparencyNotSupported, "BMP output requires an opaque image; transparency will not be discarded.");

            Report(progress, ConversionStage.Encoding);
            cancellationToken.ThrowIfCancellationRequested();
            // Unique owned temporary, in the destination directory for a same-volume publish.
            string candidate = Path.Combine(Path.GetDirectoryName(output)!, ".winnu-" + Guid.NewGuid().ToString("N") + ".partial");
            await using (var file = new FileStream(candidate, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                temporary = candidate; // Never delete a path that this operation did not create.
                using var random = file.AsRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(EncoderId(request.TargetFormat), random).AsTask(cancellationToken);
                encoder.IsThumbnailGenerated = false;
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    request.TargetFormat == ImageFileFormat.Bmp ? BitmapAlphaMode.Ignore : BitmapAlphaMode.Straight,
                    info.Width, info.Height, 96, 96, pixels);
                await encoder.FlushAsync().AsTask(cancellationToken);
                file.Flush(flushToDisk: true);
            }
            Report(progress, ConversionStage.Publishing);
            cancellationToken.ThrowIfCancellationRequested();
            // Recheck detected links before publishing. This is not a hostile-filesystem sandbox.
            CheckAncestors(Path.GetDirectoryName(output)!, input: false);
            long length = new FileInfo(temporary).Length;
            try { File.Move(temporary, output, overwrite: false); }
            catch (IOException ex) when (File.Exists(output) || Directory.Exists(output))
            { throw new ConversionException(ConversionError.OutputExists, "The destination was created by another operation. It was not replaced.", ex); }
            temporary = null; // Commit point: late cancellation cannot remove a completed result.
            Report(progress, ConversionStage.Completed);
            return new(output, new(request.TargetFormat, info.Width, info.Height), length);
        }
        catch (OperationCanceledException ex) { failure = ex; throw; }
        catch (ConversionException ex) { failure = ex; throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or COMException or ArgumentException)
        { failure = ex; throw Classify(ex); }
        finally
        {
            if (temporary != null)
            {
                try { File.Delete(temporary); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                { throw new ConversionException(ConversionError.CleanupFailed, $"Could not remove the owned temporary file: {temporary}", failure == null ? ex : new AggregateException(failure, ex)); }
            }
        }
    }

    private static FileStream OpenSource(string path)
    {
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length is <= 0 or > ImageConversionPolicy.MaximumInputBytes)
        { file.Dispose(); throw Error(ConversionError.LimitsExceeded, "Input must be nonempty and at most 64 MiB."); }
        return file;
    }

    private static async Task<ImageFileFormat> CheckContainerAsync(FileStream file, CancellationToken token)
    {
        byte[] header = new byte[32];
        int count = (int)Math.Min(file.Length, header.Length);
        await file.ReadExactlyAsync(header.AsMemory(0, count), token);
        var format = ImageConversionPolicy.DetectSignature(header.AsSpan(0, count));
        if (format == null) throw Error(ConversionError.UnsupportedInput, "Only actual PNG and BMP image data is supported.");
        if (format == ImageFileFormat.Png)
        {
            using var compressed = new MemoryStream();
            byte[]? imageHeader = null;
            file.Position = 8;
            bool first = true, ended = false;
            for (int chunks = 0; file.Position < file.Length && chunks < 100_000; chunks++)
            {
                token.ThrowIfCancellationRequested();
                byte[] chunk = new byte[8];
                await file.ReadExactlyAsync(chunk, token);
                uint length = BinaryPrimitives.ReadUInt32BigEndian(chunk);
                if (length > file.Length - file.Position - 4)
                    throw Error(ConversionError.InvalidImage, "Truncated PNG container.");
                if (chunk.AsSpan(4).SequenceEqual("acTL"u8))
                    throw Error(ConversionError.UnsupportedInput, "Animated PNG is outside this engine's single-image scope.");
                if (first && (!chunk.AsSpan(4).SequenceEqual("IHDR"u8) || length != 13))
                    throw Error(ConversionError.InvalidImage, "PNG must begin with a valid IHDR.");
                uint crc = PngCrc.Update(uint.MaxValue, chunk.AsSpan(4));
                byte[] payload = new byte[Math.Min(length, 65_536u)];
                uint remaining = length;
                while (remaining > 0)
                {
                    int size = (int)Math.Min(remaining, (uint)payload.Length);
                    await file.ReadExactlyAsync(payload.AsMemory(0, size), token);
                    crc = PngCrc.Update(crc, payload.AsSpan(0, size));
                    if (chunk.AsSpan(4).SequenceEqual("IDAT"u8)) await compressed.WriteAsync(payload.AsMemory(0, size), token);
                    remaining -= (uint)size;
                }
                byte[] checksum = new byte[4];
                await file.ReadExactlyAsync(checksum, token);
                if (~crc != BinaryPrimitives.ReadUInt32BigEndian(checksum))
                    throw Error(ConversionError.InvalidImage, "PNG checksum validation failed.");
                if (first)
                {
                    if (payload[8] != 8 || payload[9] is not (2 or 6) || payload[12] != 0)
                        throw Error(ConversionError.UnsupportedInput, "This engine accepts noninterlaced 8-bit RGB/RGBA PNG images.");
                    if (payload[10] != 0 || payload[11] != 0)
                        throw Error(ConversionError.InvalidImage, "Unknown PNG compression or filter method.");
                    if (!ImageConversionPolicy.DimensionsAllowed(BinaryPrimitives.ReadUInt32BigEndian(payload), BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(4))))
                        throw Error(ConversionError.LimitsExceeded, "Image exceeds the pixel or side limit.");
                    first = false;
                    imageHeader = payload;
                }
                if (chunk.AsSpan(4).SequenceEqual("IEND"u8)) { ended = length == 0; break; }
            }
            if (!ended || file.Position != file.Length) throw Error(ConversionError.InvalidImage, "PNG container is incomplete or has trailing data.");
            if (imageHeader == null || compressed.Length == 0) throw Error(ConversionError.InvalidImage, "PNG has no image data.");
            compressed.Position = 0;
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            int channels = imageHeader[9] == 6 ? 4 : 3;
            int rowBytes = checked((int)BinaryPrimitives.ReadUInt32BigEndian(imageHeader) * channels + 1);
            uint rows = BinaryPrimitives.ReadUInt32BigEndian(imageHeader.AsSpan(4));
            byte[] row = new byte[rowBytes];
            for (uint y = 0; y < rows; y++)
            {
                await zlib.ReadExactlyAsync(row, token);
                if (row[0] > 4) throw Error(ConversionError.InvalidImage, "Unknown PNG scanline filter.");
            }
            if (await zlib.ReadAsync(new byte[1], token) != 0)
                throw Error(ConversionError.InvalidImage, "PNG has excess decompressed image data.");
        }
        else
        {
            if (count < 30) throw Error(ConversionError.InvalidImage, "Truncated BMP header.");
            uint dib = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(14));
            ushort bits = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(28));
            if (dib is not (40 or 108 or 124) || bits is not (24 or 32))
                throw Error(ConversionError.UnsupportedInput, "This engine accepts 24/32-bit Windows BMP images.");
            int width = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(18));
            long height = Math.Abs((long)BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(22)));
            if (width <= 0 || height == 0 || height > uint.MaxValue
                || !ImageConversionPolicy.DimensionsAllowed((uint)width, (uint)height))
                throw Error(ConversionError.LimitsExceeded, "Image exceeds the pixel or side limit.");
        }
        return format.Value;
    }

    private static ImageInfo CheckDecoder(BitmapDecoder decoder, ImageFileFormat format)
    {
        if (decoder.FrameCount != 1) throw Error(ConversionError.UnsupportedInput, "Only one image frame is supported.");
        if (!ImageConversionPolicy.DimensionsAllowed(decoder.PixelWidth, decoder.PixelHeight)
            || !ImageConversionPolicy.DimensionsAllowed(decoder.OrientedPixelWidth, decoder.OrientedPixelHeight))
            throw Error(ConversionError.LimitsExceeded, "Image exceeds 16 million pixels or 16,384 pixels per side.");
        return new(format, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight);
    }

    private static string LocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length < 3 || !char.IsAsciiLetter(path[0])
            || path[1] != ':' || path[2] != '\\' || path.Contains('/') || path.AsSpan(2).Contains(':'))
            throw Error(ConversionError.UnsafePath, "Use an absolute local drive path without device syntax or alternate streams.");
        string full = Path.GetFullPath(path);
        foreach (string part in path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            string stem = part.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (part is "." or ".." || part.EndsWith(' ') || part.EndsWith('.')
                || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || stem is "CON" or "PRN" or "AUX" or "NUL" or "CLOCK$" or "CONIN$" or "CONOUT$"
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && (char.IsAsciiDigit(stem[3]) || stem[3] is '¹' or '²' or '³')))
                throw Error(ConversionError.UnsafePath, "Reserved device names and ambiguous path components are not supported.");
        }
        if (new DriveInfo(Path.GetPathRoot(full)!).DriveType != DriveType.Fixed)
            throw Error(ConversionError.UnsafePath, "Only fixed local drives are supported.");
        return full;
    }

    private static void CheckAncestors(string path, bool input)
    {
        const FileAttributes denied = FileAttributes.ReparsePoint | FileAttributes.Offline | (FileAttributes)0x400000 | (FileAttributes)0x40000;
        bool first = true;
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
        {
            var attributes = File.GetAttributes(current);
            if ((attributes & denied) != 0 || (first && input && (attributes & FileAttributes.Directory) != 0)
                || (first && !input && (attributes & FileAttributes.Directory) == 0))
                throw Error(ConversionError.UnsafePath, "Folders/links/placeholders are not accepted as image inputs; output folders must be local and ordinary.");
            first = false;
        }
    }

    private static Guid DecoderId(ImageFileFormat format) => format == ImageFileFormat.Png ? BitmapDecoder.PngDecoderId : BitmapDecoder.BmpDecoderId;
    private static Guid EncoderId(ImageFileFormat format) => format == ImageFileFormat.Png ? BitmapEncoder.PngEncoderId : BitmapEncoder.BmpEncoderId;
    private static ConversionException Error(ConversionError code, string message) => new(code, message);
    private static ConversionException Classify(Exception ex) => ex is COMException or EndOfStreamException or InvalidDataException
        ? new(ConversionError.InvalidImage, "Windows could not decode or encode this image.", ex)
        : new(ConversionError.IoFailure, "The local file operation could not be completed.", ex);

    private static void Report(IProgress<ConversionProgress>? progress, ConversionStage stage)
    {
        try { progress?.Report(new(stage)); }
        catch (Exception) { /* A notification observer cannot roll back or misreport a published result. */ }
    }
}
