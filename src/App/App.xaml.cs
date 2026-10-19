using Microsoft.UI.Xaml;

namespace WinNativeFileU.Desktop;

public partial class App : Application
{
    private Window? window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow(Environment.GetCommandLineArgs());
        window.Activate();
    }
}
