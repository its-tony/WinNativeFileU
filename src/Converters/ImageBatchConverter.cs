using WinNativeFileU.Core;

namespace WinNativeFileU.Converters;

/// <summary>Sequential bounded batch. Cancellation retains already published results and never removes user files.</summary>
public sealed class ImageBatchConverter(IImageConverter converter)
{
    public async Task<BatchConversionResult> ConvertAsync(IReadOnlyList<string> sources, string outputDirectory,
        ImageFileFormat target, IProgress<BatchProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        string[] files = sources.ToArray();
        var results = new List<BatchFileResult>();
        if (cancellationToken.IsCancellationRequested) return new(true, results);
        string output = LocalImageConverter.ValidateLocalDirectory(outputDirectory);
        if (!LocalImageConverter.ProbeSelection(files).Contains(target))
            throw new ConversionException(ConversionError.IncompatibleTarget, "The chosen format does not convert this entire selection.");
        for (int i = 0; i < files.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested) return new(true, results.AsReadOnly());
            string source = files[i]; int number = i + 1;
            string stem = Path.GetFileNameWithoutExtension(source);
            if (stem.Length > 80) stem = stem[..80].TrimEnd();
            if (stem.Length > 0 && char.IsHighSurrogate(stem[^1])) stem = stem[..^1];
            string destination = Path.Combine(output, stem + "-converted-" + Guid.NewGuid().ToString("N") + ImageConversionPolicy.Extension(target));
            var updates = new Observer(p =>
            {
                try { progress?.Report(new(number, files.Length, source, p.Stage)); }
                catch { /* Reporting cannot undo a completed file. */ }
            });
            try
            {
                var result = await converter.ConvertAsync(new(source, destination, target), updates, cancellationToken);
                results.Add(new(source, result, null, null));
            }
            catch (OperationCanceledException) { return new(true, results.AsReadOnly()); }
            catch (ConversionException ex)
            {
                results.Add(new(source, null, ex.Code, ex.Message));
                if (ex.Code == ConversionError.CleanupFailed) break;
            }
        }
        return new(false, results.AsReadOnly());
    }

    private sealed class Observer(Action<ConversionProgress> action) : IProgress<ConversionProgress>
    { public void Report(ConversionProgress value) => action(value); }
}
