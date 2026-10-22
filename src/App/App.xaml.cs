using Microsoft.UI.Xaml;

namespace WinNativeFileU.Desktop;

public partial class App : Application
{
    // Installation/uninstallation must wait for every app mode in this Windows session.
    // Keep the named handle until process termination, including asynchronous shutdown.
    private static readonly System.Threading.Mutex InstallationPresence = new(false, @"Local\WinNativeFileU.Running");
    private Window? window;

    public App()
    {
        GC.KeepAlive(InstallationPresence);
        InitializeComponent();
        UnhandledException += (_, args) => RecordPreviewFailure(args.Exception);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var commandLine = Environment.GetCommandLineArgs();
            int logIndex = Array.IndexOf(commandLine, "--preview-log");
            string? Option(string name)
            {
                int index = Array.IndexOf(commandLine, name);
                return index >= 0 && index + 1 < commandLine.Length ? commandLine[index + 1] : null;
            }
            if (!commandLine.Contains("--conversion-workbench") && !commandLine.Contains("--radial-preview") && !commandLine.Contains("--prototype-root"))
            {
                string? customRoot = Option("--resident-root");
                string root = customRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinNativeFileU");
                if (customRoot == null) Directory.CreateDirectory(root);
                var instance = new WinNativeFileU.WindowsIntegration.ResidentInstance(root);
                if (!instance.Acquired)
                {
                    instance.Dispose();
                    string duplicate = Path.Combine(root, "duplicate-" + Guid.NewGuid().ToString("N") + ".json");
                    using var file = new FileStream(duplicate, FileMode.CreateNew, FileAccess.Write);
                    System.Text.Json.JsonSerializer.Serialize(file, new { at = DateTimeOffset.Now, processId = Environment.ProcessId, result = "AlreadyRunning", startupChanged = false });
                    Exit(); return;
                }
                try
                {
                    var resident = new ResidentWindow(root, instance, customRoot != null);
                    window = resident; resident.Start(commandLine.Contains("--resident-minimized")); return;
                }
                catch { instance.Dispose(); throw; }
            }
            window = commandLine.Contains("--conversion-workbench")
                ? new ConversionWorkbenchWindow(Option("--conversion-root") ?? throw new ArgumentException("Conversion run root required."), Option("--conversion-log") ?? throw new ArgumentException("Conversion journal required."))
                : commandLine.Contains("--radial-preview")
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
            bool workbench = args.Contains("--conversion-workbench");
            int index = Array.IndexOf(args, workbench ? "--conversion-log" : "--preview-log");
            if ((!workbench && !args.Contains("--radial-preview")) || index < 0 || index + 1 >= args.Length) return;
            using var file = new FileStream(args[index + 1] + ".failure.json", FileMode.CreateNew, FileAccess.Write);
            System.Text.Json.JsonSerializer.Serialize(file, new { at = DateTimeOffset.Now, exception = exception.ToString() });
        }
        catch { /* Diagnostics must never overwrite an existing file or hide the original failure. */ }
    }
}
