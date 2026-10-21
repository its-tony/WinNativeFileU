using System.Text.Json;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using WinNativeFileU.Converters;
using WinNativeFileU.Core;
using WinNativeFileU.WindowsIntegration;
using Windows.Graphics;

namespace WinNativeFileU.Desktop;

public sealed class ResidentWindow : Window
{
    private readonly ResidentInstance instance;
    private readonly ResidentSettingsStore store;
    private readonly StartupRegistration? startup;
    private readonly StreamWriter journal;
    private readonly ComboBox theme = new() { Header = "Appearance", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox shortcut = new() { Header = "Show settings shortcut", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly CheckBox closeToTray = new() { Content = "Keep running when I close this window" };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock startupStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button startupAction = new();
    private readonly Button save = new() { Content = "Save settings" };
    private readonly Button hide = new() { Content = "Hide to tray" };
    private readonly StackPanel content = new() { Padding = new Thickness(28), Spacing = 16 };
    private readonly bool isolated;
    private ResidentSettings settings = new();
    private ResidentHost? host;
    private readonly ImageConversionPanel conversion;
    private DragOverlay? conversionOverlay;
    private bool exiting, closeAfterConversion;

    public ResidentWindow(string root, ResidentInstance ownership, bool isolatedProfile)
    {
        instance = ownership; isolated = isolatedProfile;
        root = LocalImageConverter.ValidateLocalDirectory(root);
        store = new ResidentSettingsStore(root);
        journal = new StreamWriter(new FileStream(Path.Combine(root, "resident-" + Guid.NewGuid().ToString("N") + ".jsonl"), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        if (!isolated)
        {
            try { startup = new StartupRegistration(new CurrentUserStartupValue(), Environment.ProcessPath ?? throw new InvalidOperationException("Executable path unavailable.")); }
            catch (Exception ex) { Log("StartupUnavailable", new { type = ex.GetType().Name }); }
        }
        Title = "WinNativeFileU — Resident settings";
        content.Children.Add(new TextBlock { Text = "WinNativeFileU", FontSize = 30 });
        content.Children.Add(new TextBlock { Text = "Look for the blue icon with a white diamond in the notification area or its hidden icons. Open settings from that icon or your shortcut; choose Exit to stop the app.", TextWrapping = TextWrapping.Wrap });
        foreach (var value in Enum.GetValues<ResidentTheme>()) theme.Items.Add(value.ToString());
        foreach (string label in new[] { "Disabled", "Ctrl+Alt+W", "Ctrl+Alt+Shift+W" }) shortcut.Items.Add(label);
        AutomationProperties.SetName(theme, "Resident appearance"); AutomationProperties.SetName(shortcut, "Resident shortcut");
        AutomationProperties.SetAutomationId(status, "ResidentStatus"); AutomationProperties.SetAutomationId(startupStatus, "StartupStatus");
        conversion = new ImageConversionPanel(this, Log);
        conversion.JobStarted += () => ShowSettings("conversion");
        conversion.JobFinished += () => { if (closeAfterConversion) Close(); };
        content.Children.Add(conversion);
        var preferences = new StackPanel { Spacing = 12 };
        preferences.Children.Add(theme); preferences.Children.Add(shortcut); preferences.Children.Add(closeToTray); preferences.Children.Add(save);
        preferences.Children.Add(new TextBlock { Text = "Start at sign-in", FontSize = 20 });
        preferences.Children.Add(startupStatus); preferences.Children.Add(startupAction);
        content.Children.Add(new Expander { Header = "Resident settings", Content = preferences, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var exit = new Button { Content = "Exit WinNativeFileU" }; actions.Children.Add(hide); actions.Children.Add(exit);
        content.Children.Add(actions);
        var checkTray = new Button { Content = "Check tray icon" };
        checkTray.Click += (_, _) =>
        {
            if (host == null) { status.Text = "The notification icon is unavailable."; return; }
            var location = host.QueryTrayLocation();
            status.Text = location.Located
                ? "Windows reports a location for the blue diamond icon. It may be under hidden icons; this does not confirm that it is visible."
                : "Windows did not report a location for the notification icon. Keep this window available and use Exit to stop the app.";
        };
        content.Children.Add(checkTray); content.Children.Add(status);
        Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        content.ActualThemeChanged += (_, _) => UpdateBackground();

        try { settings = store.Load(); }
        catch (Exception ex) { save.IsEnabled = false; status.Text = "Settings could not be loaded; the existing file was preserved. Repair it before saving."; Log("SettingsLoadFailed", new { type = ex.GetType().Name }); }
        theme.SelectedIndex = (int)settings.Theme; shortcut.SelectedIndex = (int)settings.Shortcut; closeToTray.IsChecked = settings.CloseToTray;
        ApplyTheme();
        try
        {
            host = new ResidentHost(Log);
            host.ShowRequested += () => ShowSettings("tray-or-hotkey");
            host.HideRequested += HideSettings;
            host.ExitRequested += Exit;
            host.TrayUnavailable += () => { ShowSettings("tray-unavailable"); status.Text = "Notification icon unavailable. This window stays available; Exit stops the app."; };
            try { host.SetShortcut(settings.Shortcut); }
            catch (Exception ex) { status.Text = "Shortcut unavailable. Choose another shortcut or use the notification icon."; Log("ShortcutUnavailable", new { type = ex.GetType().Name }); }
            if (status.Text.Length == 0) status.Text = "Ready. Closing this window keeps the app available when the option is selected.";
        }
        catch (Exception ex) { hide.IsEnabled = save.IsEnabled = false; status.Text = "Notification area unavailable. Closing this window will exit."; Log("ResidentUnavailable", new { type = ex.GetType().Name }); }
        RefreshStartup();
        try
        {
            conversionOverlay = new DragOverlay(root, Path.Combine(root, "conversion-drop-" + Guid.NewGuid().ToString("N") + ".jsonl"),
                PrototypeShortcutProfile.Letters, requireHeldActivation: true, presentation: new RadialOverlayPresentation(conversionMode: true), fileDropHandler: conversion);
            conversionOverlay.StatusChanged += message => { if (!conversion.IsBusy) status.Text = message; };
            Log("ConversionFlowReady", new { radial = true, startupChanged = false });
        }
        catch (Exception ex) { status.Text = "Drag conversion unavailable. Choose images still works: " + ex.Message; Log("ConversionRadialUnavailable", new { type = ex.GetType().Name }); }
        save.Click += (_, _) => SaveSettings(); hide.Click += (_, _) => HideSettings(); exit.Click += (_, _) => Exit();
        startupAction.Click += (_, _) => ChangeStartup();
        AppWindow.Closing += (_, args) =>
        {
            if (ResidentPolicy.HideOnClose(settings.CloseToTray, host?.TrayReady == true, exiting)) { args.Cancel = true; HideSettings(); return; }
            if (conversion.IsBusy) { args.Cancel = true; exiting = true; closeAfterConversion = true; conversion.Cancel(); status.Text = "Waiting for selection/conversion cancellation and cleanup…"; }
        };
        Closed += (_, _) =>
        {
            try { conversionOverlay?.Dispose(); conversion.Dispose(); host?.Dispose(); instance.Dispose(); Log("ResidentClosed", new { }); }
            finally { journal.Dispose(); }
        };
        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.Resize(new SizeInt32(Math.Min(1320, work.Width - 48), Math.Min(1350, work.Height - 48)));
        Log("ResidentReady", new { processId = Environment.ProcessId, isolated, settings, startupChanged = false, tray = host?.TrayReady == true });
    }

    public void Start(bool minimized)
    {
        ShowSettings("launch");
        if (minimized) HideSettings();
    }
    private void ShowSettings(string origin) { AppWindow.Show(); Activate(); Log("SettingsShown", new { origin }); }
    private void HideSettings()
    {
        if (host?.TrayReady != true) { status.Text = "The notification icon is unavailable; this window remains open."; return; }
        AppWindow.Hide(); Log("SettingsHidden", new { });
    }
    private void Exit() { exiting = true; Log("ExitRequested", new { }); Close(); }
    private void ApplyTheme()
    {
        content.RequestedTheme = settings.Theme switch { ResidentTheme.Light => ElementTheme.Light, ResidentTheme.Dark => ElementTheme.Dark, _ => ElementTheme.Default };
        if (Content is ScrollViewer view) view.RequestedTheme = content.RequestedTheme;
        UpdateBackground();
    }
    private void UpdateBackground()
    {
        if (Content is ScrollViewer view) view.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            content.ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 32, 32, 32) : Microsoft.UI.Colors.White);
    }
    private void SaveSettings()
    {
        if (host == null) return;
        var candidate = new ResidentSettings(1, closeToTray.IsChecked == true, (ResidentTheme)theme.SelectedIndex, (ResidentShortcut)shortcut.SelectedIndex);
        try
        {
            host.SetShortcut(candidate.Shortcut);
            try { store.Save(candidate); }
            catch { host.SetShortcut(settings.Shortcut); throw; }
            settings = candidate; ApplyTheme(); status.Text = "Settings saved. Sign-in startup was not changed."; Log("SettingsSaved", settings);
        }
        catch (Exception ex) { status.Text = "Settings were not saved: " + ex.Message; Log("SettingsSaveFailed", new { type = ex.GetType().Name }); }
    }
    private void RefreshStartup()
    {
        if (isolated) { startupStatus.Text = "Isolated test profile: sign-in startup cannot be changed here."; startupAction.Content = "Sign-in unchanged"; startupAction.IsEnabled = false; return; }
        if (startup == null) { startupStatus.Text = "Sign-in startup unavailable for this executable path."; startupAction.Content = "Sign-in unavailable"; startupAction.IsEnabled = false; return; }
        try
        {
            var state = startup.State;
            startupStatus.Text = state switch { StartupState.Enabled => "Enabled for this executable at your next sign-in.", StartupState.Disabled => "Disabled. Enable only if you want the app to start hidden at sign-in.", _ => "Another command exists for this app name. It will be preserved." };
            startupAction.Content = state == StartupState.Enabled ? "Disable start at sign-in" : "Enable start at sign-in";
            startupAction.IsEnabled = state != StartupState.Conflict;
        }
        catch (Exception ex) { startupStatus.Text = "Sign-in settings unavailable."; startupAction.IsEnabled = false; Log("StartupReadFailed", new { type = ex.GetType().Name }); }
    }
    private void ChangeStartup()
    {
        if (isolated || startup == null) return;
        try { startup.SetEnabled(startup.State != StartupState.Enabled); Log("StartupChangedByUser", new { state = startup.State.ToString() }); }
        catch (Exception ex) { status.Text = "Sign-in setting was not changed: " + ex.Message; Log("StartupChangeFailed", new { type = ex.GetType().Name }); }
        RefreshStartup();
    }
    private void Log(string name, object details) => journal.WriteLine(JsonSerializer.Serialize(new { at = DateTimeOffset.Now, name, details }));
}
