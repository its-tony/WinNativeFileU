using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using WinNativeFileU.WindowsIntegration;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace WinNativeFileU.Desktop;

/// <summary>WinUI island in the owned native OLE window; never activates a Window or reads file data.</summary>
public sealed class RadialOverlayPresentation : IOverlayPresentation
{
    private DesktopWindowXamlSource? source;
    private RadialMenuControl? radial;
    private Border? root;
    private ThemeSettings? themeSettings;
    private bool rejected;
    private readonly UISettings settings = new();
    public int WidthDip => 460;
    public int HeightDip => 460;

    public void Initialize(nint parentWindow)
    {
        source = new DesktopWindowXamlSource();
        source.Initialize(Win32Interop.GetWindowIdFromWindow(parentWindow));
        radial = new RadialMenuControl { ExternalDragSelection = true, IsHitTestVisible = false };
        root = new Border
        {
            Padding = new Thickness(20),
            Child = new Viewbox { Stretch = Stretch.Uniform, Child = radial },
            IsHitTestVisible = false
        };
        source.Content = root;
        themeSettings = ThemeSettings.CreateForWindowId(Win32Interop.GetWindowIdFromWindow(parentWindow));
        themeSettings.Changed += OnThemeChanged;
        root.ActualThemeChanged += OnActualThemeChanged;
        ApplyAppearance();
        source.SiteBridge.Show();
    }

    public void Resize(int clientWidth, int clientHeight) =>
        source?.SiteBridge.MoveAndResize(new RectInt32(0, 0, Math.Max(1, clientWidth), Math.Max(1, clientHeight)));

    public void Reset() { rejected = false; radial?.Reset(); }
    public void Reject() { rejected = true; radial?.ShowRejection(); }

    public string? SelectAt(double x, double y, double clientWidth, double clientHeight)
    {
        if (rejected || radial?.XamlRoot == null || root == null) return null;
        // OLE screen/client coordinates are physical pixels; XAML transforms use DIPs.
        double scale = radial.XamlRoot.RasterizationScale;
        var transform = root.TransformToVisual(radial);
        Point point = transform.TransformPoint(new Point(x / scale, y / scale));
        return radial.SelectDragPoint(point.X, point.Y);
    }

    private void OnThemeChanged(ThemeSettings sender, object args) => root?.DispatcherQueue.TryEnqueue(ApplyAppearance);
    private void OnActualThemeChanged(FrameworkElement sender, object args) => ApplyAppearance();

    private void ApplyAppearance()
    {
        if (source == null || radial == null || root == null) return;
        bool contrast = themeSettings?.HighContrast == true;
        bool dark = root.ActualTheme == ElementTheme.Dark;
        source.SystemBackdrop = !contrast && DesktopAcrylicController.IsSupported() ? new DesktopAcrylicBackdrop() : null;
        root.Background = new SolidColorBrush(contrast ? settings.GetColorValue(UIColorType.Background)
            : dark ? Color.FromArgb(235, 23, 27, 34) : Color.FromArgb(235, 250, 249, 246));
        radial.HighContrast = contrast;
        radial.RefreshAppearance();
        if (rejected) radial.ShowRejection();
    }

    public void Dispose()
    {
        if (themeSettings != null) themeSettings.Changed -= OnThemeChanged;
        if (root != null) root.ActualThemeChanged -= OnActualThemeChanged;
        if (source != null) { source.Content = null; source.Dispose(); source = null; }
        radial = null; root = null; themeSettings = null;
    }
}
