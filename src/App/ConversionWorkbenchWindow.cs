using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinNativeFileU.Converters;
using WinNativeFileU.Core;

namespace WinNativeFileU.Desktop;

/// <summary>M3 fixture workbench exercising the real offline engine; separate from M2's example radial.</summary>
public sealed class ConversionWorkbenchWindow : Window
{
    private readonly IImageConverter converter = new LocalImageConverter();
    private readonly ComboBox input = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox target = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBox outputName = new() { Header = "New output filename" };
    private readonly TextBlock info = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock status = new() { Text = "Inspecting input…", TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar progress = new() { IsIndeterminate = false, Visibility = Visibility.Collapsed };
    private readonly Button convert = new() { Content = "Convert", IsEnabled = false };
    private readonly Button cancel = new() { Content = "Cancel", IsEnabled = false };
    private readonly Button useResult = new() { Content = "Use result as input", IsEnabled = false };
    private readonly Image before = new() { Height = 160, Stretch = Stretch.Uniform };
    private readonly Image after = new() { Height = 160, Stretch = Stretch.Uniform };
    private readonly StreamWriter journal;
    private readonly string outputDirectory;
    private CancellationTokenSource? cancellation;
    private ImageInfo? inspected;
    private string? lastOutput;
    private int inspectionGeneration;
    private bool busy, closed, closeAfterCancel;

    public ConversionWorkbenchWindow(string rootPath, string logPath)
    {
        string root = LocalImageConverter.ValidateLocalDirectory(rootPath);
        string inputs = LocalImageConverter.ValidateLocalDirectory(Path.Combine(root, "inputs"));
        outputDirectory = LocalImageConverter.ValidateLocalDirectory(Path.Combine(root, "outputs"));
        if (!Path.GetFullPath(logPath).Equals(Path.Combine(root, "events.jsonl"), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The workbench journal must be the new run's events.jsonl.");
        journal = new StreamWriter(new FileStream(logPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        Title = "WinNativeFileU — Image conversion";
        var content = new StackPanel { Padding = new Thickness(28), Spacing = 12, MaxWidth = 820, HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Children.Add(new TextBlock { Text = "Convert a local image", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        content.Children.Add(new TextBlock { Text = "PNG ↔ BMP · Offline · New files only", FontSize = 16 });
        content.Children.Add(new TextBlock { Text = "M3 workbench uses generated images. Outputs are saved in this run's outputs folder. Metadata is not retained; pixels are normalized to sRGB / 96 DPI. Transparent PNG cannot be saved as BMP.", TextWrapping = TextWrapping.Wrap });
        input.Header = "Input image"; target.Header = "Output format";
        AutomationProperties.SetName(input, "Conversion input"); AutomationProperties.SetName(target, "Conversion output format");
        AutomationProperties.SetAutomationId(outputName, "OutputFilename");
        AutomationProperties.SetAutomationId(status, "ConversionStatus"); AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(before, "Source image preview"); AutomationProperties.SetName(after, "Converted image preview");
        content.Children.Add(input); content.Children.Add(info); content.Children.Add(target); content.Children.Add(outputName);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        actions.Children.Add(convert); actions.Children.Add(cancel); actions.Children.Add(useResult);
        var open = new Button { Content = "Open outputs" };
        actions.Children.Add(open); content.Children.Add(actions); content.Children.Add(progress); content.Children.Add(status);
        var previews = new Grid { ColumnSpacing = 20 };
        previews.ColumnDefinitions.Add(new ColumnDefinition()); previews.ColumnDefinitions.Add(new ColumnDefinition());
        var left = new StackPanel { Spacing = 6 }; left.Children.Add(new TextBlock { Text = "Source" }); left.Children.Add(before);
        var right = new StackPanel { Spacing = 6 }; right.Children.Add(new TextBlock { Text = "Result" }); right.Children.Add(after);
        previews.Children.Add(left); Grid.SetColumn(right, 1); previews.Children.Add(right); content.Children.Add(previews);
        Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        input.SelectionChanged += async (_, _) => await InspectSelection();
        convert.Click += async (_, _) => await ConvertSelection();
        cancel.Click += (_, _) => { cancellation?.Cancel(); status.Text = "Cancelling before publication…"; };
        useResult.Click += (_, _) =>
        {
            if (lastOutput == null) return;
            var choice = new ComboBoxItem { Content = Path.GetFileName(lastOutput), Tag = lastOutput };
            input.Items.Add(choice); input.SelectedItem = choice;
        };
        open.Click += (_, _) => Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, Arguments = '"' + outputDirectory + '"' });
        AppWindow.Closing += (_, args) =>
        {
            if (!busy) return;
            args.Cancel = true; closeAfterCancel = true; cancellation?.Cancel(); status.Text = "Waiting for cancellation and temporary-file cleanup…";
        };
        Closed += (_, _) => { closed = true; inspectionGeneration++; Log("Closed", new { }); journal.Dispose(); };
        foreach (string file in Directory.GetFiles(inputs).Where(p => Path.GetExtension(p).Equals(".bmp", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(p).Equals(".png", StringComparison.OrdinalIgnoreCase)).Order())
            input.Items.Add(new ComboBoxItem { Content = Path.GetFileName(file), Tag = file });
        Log("Ready", new { root, inputCount = input.Items.Count, engine = "Windows PNG/BMP", originalsReadOnly = true });
        if (input.Items.Count > 0) input.SelectedIndex = 0;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.Resize(new SizeInt32(Math.Min(1320, area.Width - 48), Math.Min(1120, area.Height - 48)));
    }

    private async Task InspectSelection()
    {
        if (busy || input.SelectedItem is not ComboBoxItem { Tag: string path }) return;
        int generation = ++inspectionGeneration;
        inspected = null; convert.IsEnabled = false;
        try
        {
            var image = await Task.Run(() => converter.InspectAsync(path));
            if (closed || generation != inspectionGeneration) return;
            inspected = image;
            info.Text = $"Detected {image.Format.ToString().ToUpperInvariant()} · {image.Width} × {image.Height} pixels";
            ImageFileFormat other = image.Format == ImageFileFormat.Bmp ? ImageFileFormat.Png : ImageFileFormat.Bmp;
            target.Items.Clear(); target.Items.Add(new ComboBoxItem { Content = other.ToString().ToUpperInvariant(), Tag = other }); target.SelectedIndex = 0;
            outputName.Text = Path.GetFileNameWithoutExtension(path) + "-converted" + ImageConversionPolicy.Extension(other);
            before.Source = new BitmapImage(new Uri(path));
            convert.IsEnabled = true; status.Text = "Ready. The original will remain unchanged.";
            Log("Inspected", new { source = path, image });
        }
        catch (Exception ex)
        {
            if (closed || generation != inspectionGeneration) return;
            status.Text = ex is ConversionException e ? $"{e.Code}: {e.Message}" : "The input could not be inspected.";
            Log("InspectionFailed", new { type = ex.GetType().Name, message = ex.Message });
        }
    }

    private async Task ConvertSelection()
    {
        if (busy || inspected == null || input.SelectedItem is not ComboBoxItem { Tag: string source }
            || target.SelectedItem is not ComboBoxItem { Tag: ImageFileFormat format }) return;
        string name = outputName.Text;
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name)
        { status.Text = "Enter a filename only, without folders."; return; }
        string destination = Path.Combine(outputDirectory, name);
        busy = true; cancellation = new CancellationTokenSource(); SetBusy(true);
        Log("Started", new { source, destination, format = format.ToString() });
        try
        {
            var updates = new Progress<ConversionProgress>(p =>
            {
                if (closed || !busy || cancellation == null) return;
                if (!cancellation.IsCancellationRequested) status.Text = p.Stage.ToString() + "…";
                Log("Progress", new { stage = p.Stage.ToString() });
            });
            var token = cancellation.Token;
            var result = await Task.Run(() => converter.ConvertAsync(new(source, destination, format), updates, token));
            lastOutput = result.OutputPath;
            status.Text = $"Saved new file: {Path.GetFileName(result.OutputPath)} · {result.Image.Width} × {result.Image.Height} · {result.BytesWritten:N0} bytes. Original unchanged.";
            Log("Completed", result);
            try { after.Source = new BitmapImage(new Uri(result.OutputPath)); }
            catch (Exception ex) { status.Text += " Preview unavailable."; Log("PreviewFailed", new { message = ex.Message }); }
        }
        catch (OperationCanceledException) { status.Text = "Cancelled. No new result was published."; Log("Cancelled", new { }); }
        catch (ConversionException ex) { status.Text = $"{ex.Code}: {ex.Message}"; Log("Failed", new { code = ex.Code.ToString(), message = ex.Message }); }
        catch (Exception ex) { status.Text = "Conversion could not be completed. See the private run journal."; Log("UnexpectedFailure", new { type = ex.GetType().Name, message = ex.Message }); }
        finally
        {
            busy = false; cancellation?.Dispose(); cancellation = null; SetBusy(false);
            if (closeAfterCancel) Close();
        }
    }

    private void SetBusy(bool value)
    {
        convert.IsEnabled = !value && inspected != null; cancel.IsEnabled = value;
        input.IsEnabled = target.IsEnabled = outputName.IsEnabled = !value;
        useResult.IsEnabled = !value && lastOutput != null;
        progress.IsIndeterminate = value; progress.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Log(string name, object details) => journal.WriteLine(JsonSerializer.Serialize(new { at = DateTimeOffset.Now, name, details }));
}
