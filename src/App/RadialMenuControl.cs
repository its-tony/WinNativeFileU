using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinNativeFileU.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using PathShape = Microsoft.UI.Xaml.Shapes.Path;

namespace WinNativeFileU.Desktop;

/// <summary>M2 visual component. No files, data objects or conversion operations.</summary>
public sealed class RadialMenuControl : UserControl
{
    private const double Side = 420;
    private static readonly string[] Labels = ["PNG", "JPEG", "WebP", "AVIF", "TIFF", "BMP", "PDF"];
    private readonly RadialMenuState state = new(Labels.Length);
    private readonly Canvas canvas = new() { Width = Side, Height = Side, Background = new SolidColorBrush(Colors.Transparent) };
    private readonly List<PathShape> sectors = [];
    private readonly List<ToggleButton> buttons = [];
    private readonly TextBlock centerTitle = new() { Text = "Choose a format", FontSize = 15, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock centerDetail = new() { Text = "VISUAL PREVIEW", FontSize = 10, CharacterSpacing = 100, TextAlignment = TextAlignment.Center };
    private readonly UISettings settings = new();
    public bool ReduceMotion { get; set; }
    public bool HighContrast { get; set; }
    public bool ExternalDragSelection { get; set; }
    public event Action<string>? PreviewChanged;

    public RadialMenuControl()
    {
        Width = Height = Side;
        Content = canvas;
        AutomationProperties.SetName(this, "Output format radial preview");
        for (int i = 0; i < Labels.Length; i++)
        {
            int index = i;
            var path = new PathShape { Data = Wedge(i), StrokeThickness = 1, IsHitTestVisible = false };
            sectors.Add(path); canvas.Children.Add(path);
            var label = new TextBlock { Text = Labels[i], FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
            var button = new ToggleButton { Content = label, Width = 86, Height = 44, Padding = new Thickness(0), BorderThickness = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent), UseSystemFocusVisuals = true };
            AutomationProperties.SetName(button, Labels[i] + " format preview");
            AutomationProperties.SetHelpText(button, "Select a visual example. No conversion is performed.");
            button.GotFocus += (_, _) => Select(index);
            button.Click += (_, _) => { Select(index); Confirm(); };
            var point = state.LabelPoint(i, Side);
            Canvas.SetLeft(button, point.X - button.Width / 2); Canvas.SetTop(button, point.Y - button.Height / 2);
            buttons.Add(button);
        }
        // Labels are above every sector, with native focus and Toggle automation patterns.
        foreach (var button in buttons) canvas.Children.Add(button);
        var center = new StackPanel { Width = 146, Spacing = 8, IsHitTestVisible = false };
        center.Children.Add(centerTitle); center.Children.Add(centerDetail);
        Canvas.SetLeft(center, (Side - center.Width) / 2); Canvas.SetTop(center, 184);
        canvas.Children.Add(center);
        canvas.PointerMoved += OnPointerMoved;
        canvas.PointerExited += (_, _) => { if (!ExternalDragSelection) Select(null); };
        KeyDown += OnKeyDown;
        ActualThemeChanged += (_, _) => Paint();
        Loaded += (_, _) => { Paint(); Animate(canvas, 0.97f); };
        Paint();
    }

    public void Reset()
    {
        state.Cancel(); Paint();
        PreviewChanged?.Invoke("Selection cleared. No files opened or converted.");
    }

    public void RefreshAppearance() => Paint();

    public string? SelectDragPoint(double x, double y)
    {
        int? index = state.HitTest(x, y, Side);
        Select(index);
        return index is int selected ? Labels[selected] : null;
    }

    public void ShowRejection()
    {
        state.Cancel(); Paint();
        centerTitle.Text = "Drop rejected";
        centerDetail.Text = "NO FILE OPERATION";
    }

    private void Select(int? index)
    {
        if (state.SelectedIndex == index) return;
        state.Select(index); Paint();
        if (index is int selected) Animate(buttons[selected], 0.96f);
    }

    private void Confirm()
    {
        if (state.Confirm() is int selected)
            PreviewChanged?.Invoke($"{Labels[selected]} selected in preview. No files opened or converted.");
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (ExternalDragSelection) return;
        var point = args.GetCurrentPoint(canvas).Position;
        Select(state.HitTest(point.X, point.Y, Side));
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs args)
    {
        switch (args.Key)
        {
            case VirtualKey.Right: case VirtualKey.Down: state.Move(1); FocusSelected(); break;
            case VirtualKey.Left: case VirtualKey.Up: state.Move(-1); FocusSelected(); break;
            case VirtualKey.Home: state.Select(0); FocusSelected(); break;
            case VirtualKey.End: state.Select(Labels.Length - 1); FocusSelected(); break;
            case VirtualKey.Enter: case VirtualKey.Space: Confirm(); break;
            case VirtualKey.Escape: Reset(); break;
            default: return;
        }
        args.Handled = true;
    }

    private void FocusSelected()
    {
        Paint();
        if (state.SelectedIndex is int index) buttons[index].Focus(FocusState.Keyboard);
    }

    private void Paint()
    {
        bool dark = ActualTheme == ElementTheme.Dark, contrast = HighContrast;
        Color background = contrast ? settings.GetColorValue(UIColorType.Background) : dark ? Color.FromArgb(235, 36, 42, 53) : Color.FromArgb(235, 246, 245, 241);
        Color foreground = contrast ? settings.GetColorValue(UIColorType.Foreground) : dark ? Colors.White : Color.FromArgb(255, 35, 43, 55);
        Color accent = contrast ? settings.GetColorValue(UIColorType.Accent) : Color.FromArgb(255, 255, 151, 106);
        for (int i = 0; i < sectors.Count; i++)
        {
            bool selected = state.SelectedIndex == i;
            sectors[i].Fill = new SolidColorBrush(selected ? (contrast ? foreground : accent) : background);
            sectors[i].Stroke = new SolidColorBrush(contrast ? foreground : dark ? Color.FromArgb(255, 78, 86, 102) : Color.FromArgb(255, 211, 211, 208));
            buttons[i].IsChecked = selected;
            buttons[i].Foreground = new SolidColorBrush(selected ? (contrast ? background : Color.FromArgb(255, 37, 27, 24)) : foreground);
        }
        centerTitle.Foreground = centerDetail.Foreground = new SolidColorBrush(foreground);
        centerTitle.Text = state.SelectedIndex is int index ? Labels[index] : ExternalDragSelection ? "Drag to choose" : "Choose a format";
        centerDetail.Text = state.SelectedIndex is null ? (ExternalDragSelection ? "DROP ON A SECTOR" : "VISUAL PREVIEW") : ExternalDragSelection ? "EXAMPLE FORMAT" : "SELECTED";
    }

    private void Animate(UIElement element, float from)
    {
        if (ReduceMotion || !settings.AnimationsEnabled || HighContrast) return;
        var visual = ElementCompositionPreview.GetElementVisual(element);
        visual.CenterPoint = new Vector3((float)(element is FrameworkElement f ? f.ActualWidth / 2 : 0), (float)(element is FrameworkElement g ? g.ActualHeight / 2 : 0), 0);
        var animation = visual.Compositor.CreateVector3KeyFrameAnimation();
        animation.InsertKeyFrame(0, new Vector3(from, from, 1)); animation.InsertKeyFrame(1, Vector3.One);
        animation.Duration = TimeSpan.FromMilliseconds(120);
        visual.StartAnimation("Scale", animation);
    }

    private Geometry Wedge(int index)
    {
        double step = 2 * Math.PI / Labels.Length, halfGap = RadialMenuState.GapDegrees * Math.PI / 360;
        double start = -Math.PI / 2 + step * (index - 0.5) + halfGap, end = start + step - 2 * halfGap;
        double outer = Side * RadialMenuState.OuterRadiusRatio, inner = Side * RadialMenuState.InnerRadiusRatio;
        Point At(double angle, double radius) => new(Side / 2 + Math.Cos(angle) * radius, Side / 2 + Math.Sin(angle) * radius);
        var figure = new PathFigure { StartPoint = At(start, outer), IsClosed = true };
        figure.Segments.Add(new ArcSegment { Point = At(end, outer), Size = new Size(outer, outer), SweepDirection = SweepDirection.Clockwise });
        figure.Segments.Add(new LineSegment { Point = At(end, inner) });
        figure.Segments.Add(new ArcSegment { Point = At(start, inner), Size = new Size(inner, inner), SweepDirection = SweepDirection.Counterclockwise });
        var geometry = new PathGeometry(); geometry.Figures.Add(figure); return geometry;
    }
}
