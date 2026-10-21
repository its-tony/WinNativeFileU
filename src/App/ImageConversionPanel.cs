using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using WinNativeFileU.Converters;
using WinNativeFileU.Core;
using WinNativeFileU.WindowsIntegration;

namespace WinNativeFileU.Desktop;

/// <summary>One owned conversion job at a time. OLE acceptance only snapshots and queues; codecs run on a worker.</summary>
public sealed class ImageConversionPanel : UserControl, IFileDropHandler, IDisposable
{
    private readonly Window owner;
    private readonly Action<string, object> log;
    private readonly ImageBatchConverter converter = new(new LocalImageConverter());
    private readonly Button folderButton = new() { Content = "Choose output folder" };
    private readonly Button filesButton = new() { Content = "Choose images" };
    private readonly Button convertButton = new() { Content = "Convert selected", IsEnabled = false };
    private readonly Button cancelButton = new() { Content = "Cancel conversion", IsEnabled = false };
    private readonly ComboBox formats = new() { Header = "Output format", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock folderText = new() { Text = "No output folder selected.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock selectionText = new() { Text = "No images selected.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock status = new() { Text = "Choose an output folder, then choose images or drag them and tap Ctrl+Alt+D.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBox results = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 180 };
    private readonly ProgressBar progress = new() { Visibility = Visibility.Collapsed };
    private string? outputDirectory;
    private string[] selected = [];
    private CancellationTokenSource? cancellation;
    private CancellationTokenSource? pickerCancellation;
    private bool busy, picking, disposed;
    public bool IsBusy => busy || picking;
    public bool CanAccept => !disposed && !busy && !picking && outputDirectory != null;
    public event Action? JobStarted;
    public event Action? JobFinished;

    public ImageConversionPanel(Window owner, Action<string, object> journal)
    {
        this.owner = owner; log = journal;
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "Convert images", FontSize = 24 });
        panel.Children.Add(new TextBlock { Text = "PNG RGB/RGBA8 ↔ BMP24/32 · Up to 8 images · New files only. BMP requires opaque pixels. Metadata is not retained; results use sRGB / 96 DPI.", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(folderButton); panel.Children.Add(folderText); panel.Children.Add(filesButton); panel.Children.Add(selectionText); panel.Children.Add(formats);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var open = new Button { Content = "Open output folder" };
        actions.Children.Add(convertButton); actions.Children.Add(cancelButton); actions.Children.Add(open);
        panel.Children.Add(actions); panel.Children.Add(progress); panel.Children.Add(status); panel.Children.Add(results);
        Content = panel;
        AutomationProperties.SetName(formats, "Image output format");
        AutomationProperties.SetAutomationId(folderText, "ConversionOutputDirectory");
        AutomationProperties.SetAutomationId(status, "ImageJobStatus"); AutomationProperties.SetAutomationId(results, "ImageJobResults");
        folderButton.Click += async (_, _) => await ChooseFolder(); filesButton.Click += async (_, _) => await ChooseFiles();
        convertButton.Click += (_, _) =>
        {
            if (formats.SelectedItem is ComboBoxItem { Tag: string format } && !TryAccept(selected, format)) status.Text = "A conversion is already active or an output folder is required.";
        };
        cancelButton.Click += (_, _) => Cancel();
        open.Click += (_, _) =>
        {
            try { if (outputDirectory != null) Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, Arguments = '"' + outputDirectory + '"' }); }
            catch (Exception ex) { status.Text = "The output folder could not be opened: " + ex.Message; }
        };
    }

    private async Task ChooseFolder()
    {
        if (busy || picking) return;
        picking = true; UpdateControls();
        using var selectionCancellation = new CancellationTokenSource(); pickerCancellation = selectionCancellation;
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker(); picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(owner));
            var folder = await picker.PickSingleFolderAsync().AsTask(selectionCancellation.Token);
            if (folder == null) { status.Text = "Folder selection cancelled. The previous output folder is unchanged."; return; }
            string path = LocalImageConverter.ValidateLocalDirectory(folder.Path);
            outputDirectory = path; folderText.Text = path;
            status.Text = "Output folder selected. Drag images, tap and release Ctrl+Alt+D while holding the mouse, then drop on an enabled format. Ctrl+Alt+X rejects the drag.";
            log("OutputFolderSelected", new { path });
        }
        catch (OperationCanceledException) { status.Text = "Folder selection cancelled."; }
        catch (Exception ex) { status.Text = "Output folder unavailable: " + ex.Message; log("OutputFolderFailed", new { type = ex.GetType().Name }); }
        finally { picking = false; pickerCancellation = null; UpdateControls(); JobFinished?.Invoke(); }
    }

    private async Task ChooseFiles()
    {
        if (busy || picking) return;
        picking = true; UpdateControls();
        using var selectionCancellation = new CancellationTokenSource(); pickerCancellation = selectionCancellation;
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker(); picker.FileTypeFilter.Add(".png"); picker.FileTypeFilter.Add(".bmp");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(owner));
            var files = await picker.PickMultipleFilesAsync().AsTask(selectionCancellation.Token);
            if (files.Count == 0) { status.Text = "Image selection cancelled. No conversion started."; return; }
            string[] paths = files.Select(file => file.Path).ToArray();
            var targets = LocalImageConverter.ProbeSelection(paths);
            selected = paths; formats.Items.Clear();
            foreach (var target in targets) formats.Items.Add(new ComboBoxItem { Content = target.ToString().ToUpperInvariant(), Tag = target.ToString().ToUpperInvariant() });
            formats.SelectedIndex = 0; selectionText.Text = string.Join(", ", paths.Select(Path.GetFileName));
            status.Text = "Images selected. Choose a format and Convert selected. Full validation runs during conversion.";
            log("ImagesSelected", new { paths, targets });
        }
        catch (OperationCanceledException) { status.Text = "Image selection cancelled."; }
        catch (Exception ex)
        {
            selected = []; formats.Items.Clear(); selectionText.Text = "Selection is not eligible.";
            status.Text = "Cannot use this selection: " + ex.Message; log("ImageSelectionFailed", new { type = ex.GetType().Name });
        }
        finally { picking = false; pickerCancellation = null; UpdateControls(); JobFinished?.Invoke(); }
    }

    public IReadOnlyList<string> GetAvailableFormats(IReadOnlyList<string> paths)
    {
        if (!CanAccept) return [];
        try { return LocalImageConverter.ProbeSelection(paths).Select(format => format.ToString().ToUpperInvariant()).ToArray(); }
        catch (Exception ex) { status.Text = "Cannot convert this selection: " + ex.Message; throw; }
    }

    public bool TryAccept(IReadOnlyList<string> paths, string format)
    {
        if (!CanAccept || paths.Count is 0 or > ImageBatchPolicy.MaximumFiles || format is not ("PNG" or "BMP")) return false;
        string[] snapshot = paths.ToArray(); string output = outputDirectory!;
        var target = format == "PNG" ? ImageFileFormat.Png : ImageFileFormat.Bmp;
        var tokenSource = new CancellationTokenSource(); string job = Guid.NewGuid().ToString("N");
        try { log("ImageJobQueueRequested", new { job, paths = snapshot, output, format }); }
        catch { tokenSource.Dispose(); return false; }
        cancellation = tokenSource; busy = true; results.Text = ""; UpdateControls();
        if (DispatcherQueue.TryEnqueue(() => _ = Execute(snapshot, output, target, tokenSource, job))) return true;
        busy = false; cancellation = null; tokenSource.Dispose(); UpdateControls(); return false;
    }

    private async Task Execute(string[] paths, string output, ImageFileFormat target, CancellationTokenSource tokenSource, string job)
    {
        try
        {
            JobStarted?.Invoke();
            status.Text = "Validating selected images…"; log("ImageJobStarted", new { job });
            var updates = new Progress<BatchProgress>(p =>
            {
                if (disposed || cancellation != tokenSource) return;
                if (!tokenSource.IsCancellationRequested) status.Text = $"{p.FileNumber}/{p.FileCount} · {Path.GetFileName(p.SourcePath)} · {p.Stage}";
            });
            var result = await Task.Run(() => converter.ConvertAsync(paths, output, target, updates, tokenSource.Token));
            int saved = result.Files.Count(file => file.Result != null), failed = result.Files.Count(file => file.Error != null);
            results.Text = string.Join(Environment.NewLine, result.Files.Select(file => file.Result != null
                ? $"Saved {Path.GetFileName(file.SourcePath)} → {file.Result.OutputPath}"
                : $"{Path.GetFileName(file.SourcePath)} · {file.Error}: {file.Message}"));
            status.Text = $"{(result.Cancelled ? "Cancelled" : "Finished")}: {saved} new file(s), {failed} error(s). Originals unchanged. Completed files remain after cancellation.";
            log("ImageJobFinished", new { job, result });
        }
        catch (Exception ex) { status.Text = "Job stopped: " + ex.Message + " Already completed results remain."; log("ImageJobFailed", new { job, type = ex.GetType().Name, message = ex.Message }); }
        finally
        {
            busy = false; cancellation = null; tokenSource.Dispose(); UpdateControls(); JobFinished?.Invoke();
        }
    }

    public void Cancel() { cancellation?.Cancel(); pickerCancellation?.Cancel(); if (busy) status.Text = "Cancelling before the next publication. Completed results will remain."; }
    private void UpdateControls()
    {
        folderButton.IsEnabled = filesButton.IsEnabled = formats.IsEnabled = !busy && !picking;
        convertButton.IsEnabled = CanAccept && selected.Length != 0 && formats.SelectedItem != null;
        cancelButton.IsEnabled = busy; progress.IsIndeterminate = busy; progress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }
    public void Dispose()
    {
        if (IsBusy) throw new InvalidOperationException("Wait for conversion or selection cancellation before disposing the panel.");
        disposed = true;
    }
}
