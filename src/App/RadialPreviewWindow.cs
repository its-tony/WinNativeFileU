using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace WinNativeFileU.Desktop;

/// <summary>Focusable M2 design surface, separate from the verified M1 OLE experiment.</summary>
public sealed class RadialPreviewWindow : Window
{
    private readonly Grid root = new() { Padding = new Thickness(32), RowSpacing = 24 };
    private readonly Grid layout = new() { ColumnSpacing = 28, RowSpacing = 24 };
    private readonly RadialMenuControl radial = new();
    private readonly Viewbox radialBox = new() { Stretch = Stretch.Uniform, Width = 420, Height = 420 };
    private readonly Border radialCard = new() { Padding = new Thickness(20), CornerRadius = new CornerRadius(24), BorderThickness = new Thickness(1) };
    private readonly StackPanel options = new() { Spacing = 14 };
    private readonly TextBlock status = new() { Text = "Select a sector to explore the visual preview. No files opened or converted.", TextWrapping = TextWrapping.Wrap, FontSize = 14 };
    private readonly TextBlock materialStatus = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly Slider size = new() { Minimum = 320, Maximum = 480, Value = 420, StepFrequency = 20 };
    private ThemeSettings? themeSettings;
    private readonly UISettings settings = new();
    private readonly StreamWriter? journal;
    private readonly ScrollViewer scrollViewer = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };

    public RadialPreviewWindow(string? logPath)
    {
        if (logPath != null)
            journal = new StreamWriter(new FileStream(logPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        Title = "WinNativeFileU — M2 Radial Preview";
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = new StackPanel { Spacing = 8 };
        header.Children.Add(new TextBlock { Text = "WinNativeFileU", FontSize = 14, CharacterSpacing = 60 });
        header.Children.Add(new TextBlock { Text = "A format, within reach.", FontSize = 32, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        header.Children.Add(new TextBlock { Text = "Radial interface preview · M2", FontSize = 14 });
        root.Children.Add(header);
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        radialBox.Child = radial;
        radialCard.Child = radialBox;
        layout.Children.Add(radialCard);
        Grid.SetColumn(options, 1);
        layout.Children.Add(options);
        Grid.SetRow(layout, 1); root.Children.Add(layout);
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        AutomationProperties.SetAutomationId(status, "PreviewStatus");
        Grid.SetRow(status, 2); root.Children.Add(status);
        options.Children.Add(new TextBlock { Text = "Make it yours", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        options.Children.Add(new TextBlock { Text = "Theme" });
        var theme = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, ItemsSource = new[] { "System", "Light", "Dark" }, SelectedIndex = 0 };
        AutomationProperties.SetName(theme, "Preview theme");
        theme.SelectionChanged += (_, _) =>
        {
            // Backdrop theme follows Window.Content, so theme the outer scroll host too.
            scrollViewer.RequestedTheme = theme.SelectedIndex switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
            Log("Theme", new { value = theme.SelectedItem?.ToString() });
        };
        options.Children.Add(theme);
        options.Children.Add(new TextBlock { Text = "Menu size" });
        AutomationProperties.SetName(size, "Radial preview size");
        size.ValueChanged += (_, _) => { FitRadial(); Log("Size", new { value = size.Value }); };
        options.Children.Add(size);
        var motion = new CheckBox { Content = "Reduce motion" };
        motion.Click += (_, _) => { radial.ReduceMotion = motion.IsChecked == true; Log("ReduceMotion", new { enabled = radial.ReduceMotion }); };
        options.Children.Add(motion);
        var reset = new Button { Content = "Clear selection" };
        reset.Click += (_, _) => radial.Reset();
        options.Children.Add(reset);
        options.Children.Add(new TextBlock { Text = "Hover to select. Click or Enter to preview. Arrow keys move around the ring; Escape clears selection.", TextWrapping = TextWrapping.Wrap, FontSize = 13 });
        options.Children.Add(materialStatus);
        options.Children.Add(new TextBlock { Text = "Format labels are visual examples. Explorer integration and conversion are not part of this preview.", TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        radial.PreviewChanged += text => { status.Text = text; Log("PreviewSelection", new { text }); };
        root.SizeChanged += (_, _) =>
        {
            bool narrow = root.ActualWidth - root.Padding.Left - root.Padding.Right < 720;
            Grid.SetColumn(options, narrow ? 0 : 1); Grid.SetRow(options, narrow ? 1 : 0);
            layout.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(260);
            FitRadial();
        };
        radialCard.SizeChanged += (_, _) => FitRadial();
        root.ActualThemeChanged += (_, _) => { if (root.IsLoaded) ApplyAppearance(); };
        scrollViewer.Content = root;
        Content = scrollViewer;
        root.Loaded += (_, _) =>
        {
            themeSettings = ThemeSettings.CreateForWindowId(AppWindow.Id);
            themeSettings.Changed += OnHighContrastChanged;
            ApplyAppearance();
            Log("Ready", new { previewOnly = true, acrylicSupported = DesktopAcrylicController.IsSupported(), highContrast = themeSettings.HighContrast, animationsEnabled = settings.AnimationsEnabled });
        };
        Closed += (_, _) => { if (themeSettings != null) themeSettings.Changed -= OnHighContrastChanged; Log("Closed", new { }); journal?.Dispose(); };
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.Resize(new SizeInt32(Math.Min(1320, area.Width - 48), Math.Min(1200, area.Height - 48)));
    }

    private void FitRadial()
    {
        double available = radialCard.ActualWidth > 0 ? radialCard.ActualWidth - 40 : size.Value;
        radialBox.Width = radialBox.Height = Math.Max(1, Math.Min(size.Value, available));
    }

    private void OnHighContrastChanged(ThemeSettings sender, object args) =>
        DispatcherQueue.TryEnqueue(ApplyAppearance);

    private void ApplyAppearance()
    {
        bool contrast = themeSettings?.HighContrast == true, dark = root.ActualTheme == ElementTheme.Dark;
        bool acrylic = !contrast && DesktopAcrylicController.IsSupported();
        SystemBackdrop = acrylic ? new DesktopAcrylicBackdrop() : null;
        root.Background = new SolidColorBrush(contrast ? settings.GetColorValue(UIColorType.Background)
            : dark ? Color.FromArgb(acrylic ? (byte)220 : (byte)255, 23, 27, 34)
            : Color.FromArgb(acrylic ? (byte)220 : (byte)255, 250, 249, 246));
        radialCard.Background = new SolidColorBrush(contrast ? settings.GetColorValue(UIColorType.Background) : dark ? Color.FromArgb(120, 18, 23, 32) : Color.FromArgb(120, 255, 255, 255));
        radialCard.BorderBrush = new SolidColorBrush(contrast ? settings.GetColorValue(UIColorType.Foreground) : dark ? Color.FromArgb(255, 64, 73, 88) : Color.FromArgb(255, 211, 213, 214));
        materialStatus.Text = contrast ? "High contrast · solid surface" : acrylic ? "Acrylic · managed by Windows" : "Solid surface · Acrylic unavailable";
        radial.HighContrast = contrast;
        radial.RefreshAppearance();
    }

    private void Log(string name, object details) => journal?.WriteLine(JsonSerializer.Serialize(new { at = DateTimeOffset.Now, name, details }));
}
