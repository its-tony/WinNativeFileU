namespace WinNativeFileU.Core;

public sealed record BatchFileResult(string SourcePath, ConversionResult? Result, ConversionError? Error, string? Message);
public sealed record BatchProgress(int FileNumber, int FileCount, string SourcePath, ConversionStage Stage);
public sealed record BatchConversionResult(bool Cancelled, IReadOnlyList<BatchFileResult> Files);

public static class ImageBatchPolicy
{
    public const int MaximumFiles = 8;
    public static IReadOnlyList<ImageFileFormat> CommonTargets(IEnumerable<ImageFileFormat> sources)
    {
        var snapshot = sources.Take(MaximumFiles + 1).ToArray();
        if (snapshot.Length is 0 or > MaximumFiles || snapshot.Any(x => !Enum.IsDefined(x))) return [];
        return Enum.GetValues<ImageFileFormat>().Where(target => snapshot.All(source => ImageConversionPolicy.CanConvert(source, target))).ToArray();
    }
}
