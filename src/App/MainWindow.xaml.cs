using Microsoft.UI.Xaml;
using WinNativeFileU.WindowsIntegration;

namespace WinNativeFileU.Desktop;

public sealed partial class MainWindow : Window
{
    private DragOverlay? overlay;
    private string? fixtureRoot;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        string? Option(string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        try
        {
            var root = Option("--prototype-root");
            var log = Option("--prototype-log");
            if (root == null || log == null)
            {
                Status.Text = "Start with scripts/start-prototype.ps1 to prepare disposable fixtures and a private journal.";
                return;
            }
            var profile = Option("--prototype-shortcuts") switch
            {
                null or "function-keys" => PrototypeShortcutProfile.FunctionKeys,
                "letters" => PrototypeShortcutProfile.Letters,
                _ => throw new ArgumentException("Shortcut profile must be function-keys or letters.")
            };
            bool requireHeld = Array.IndexOf(args, "--prototype-require-held") >= 0;
            overlay = new DragOverlay(root, log, profile, requireHeld);
            Instructions.Text = $"Drag prepared fixture files from Explorer, tap {overlay.ActivationLabel}, release the keys, then release the files over the target. Escape cancels the source drag. No files are copied, moved or converted.";
            fixtureRoot = root;
            overlay.StatusChanged += message => Status.Text = message;
            ArmButton.IsEnabled = !requireHeld;
            CancelButton.IsEnabled = true;
            FixturesButton.IsEnabled = true;
            Status.Text = $"Ready. {overlay.ActivationLabel} shows the target at the cursor; {overlay.RejectionLabel} rejects. Only the prepared fixture folder is accepted.";
            if (requireHeld)
                Status.Text = $"Validation mode: activation requires a held mouse button. Begin dragging, then tap/release {overlay.ActivationLabel}. Prearming is disabled.";
        }
        catch (Exception ex) { Status.Text = $"Prototype unavailable: {ex.Message}"; }
        Closed += (_, _) => { overlay?.Dispose(); overlay = null; };
    }

    private void ArmTarget(object sender, RoutedEventArgs args) => overlay?.Arm();
    private void CancelTarget(object sender, RoutedEventArgs args) => overlay?.Cancel();
    private void OpenFixtures(object sender, RoutedEventArgs args)
    {
        if (fixtureRoot != null)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe")
            { UseShellExecute = true, Arguments = '"' + fixtureRoot + '"' });
    }
}
