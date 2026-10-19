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
            overlay = new DragOverlay(root, log);
            fixtureRoot = root;
            overlay.StatusChanged += message => Status.Text = message;
            ArmButton.IsEnabled = CancelButton.IsEnabled = true;
            FixturesButton.IsEnabled = true;
            Status.Text = "Ready. F8 shows the target at the cursor; F9 rejects. Only the prepared fixture folder is accepted.";
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
