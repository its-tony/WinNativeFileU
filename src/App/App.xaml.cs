using Microsoft.UI.Xaml;

namespace WinNativeFileU.Desktop;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, args) => RecordPreviewFailure(args.Exception);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var commandLine = Environment.GetCommandLineArgs();
            int logIndex = Array.IndexOf(commandLine, "--preview-log");
            window = commandLine.Contains("--radial-preview")
                ? new RadialPreviewWindow(logIndex >= 0 && logIndex + 1 < commandLine.Length ? commandLine[logIndex + 1] : null)
                : new MainWindow(commandLine);
            window.Activate();
        }
        catch (Exception ex) { RecordPreviewFailure(ex); throw; }
    }

    private static void RecordPreviewFailure(Exception exception)
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--preview-log");
            if (!args.Contains("--radial-preview") || index < 0 || index + 1 >= args.Length) return;
            using var file = new FileStream(args[index + 1] + ".failure.json", FileMode.CreateNew, FileAccess.Write);
            System.Text.Json.JsonSerializer.Serialize(file, new { at = DateTimeOffset.Now, exception = exception.ToString() });
        }
        catch { /* Diagnostics must never overwrite an existing file or hide the original failure. */ }
    }
}
