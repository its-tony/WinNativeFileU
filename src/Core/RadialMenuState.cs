namespace WinNativeFileU.Core;

public readonly record struct RadialPoint(double X, double Y);

/// <summary>Pure radial selection geometry. Hover never confirms or performs an operation.</summary>
public sealed class RadialMenuState
{
    public const double InnerRadiusRatio = 0.23;
    public const double OuterRadiusRatio = 0.48;
    public const double GapDegrees = 4;
    public int Count { get; }
    public int? SelectedIndex { get; private set; }
    public int? ConfirmedIndex { get; private set; }

    public RadialMenuState(int count)
    {
        if (count is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(count));
        Count = count;
    }

    public int? HitTest(double x, double y, double size)
    {
        ValidateSize(size);
        if (!double.IsFinite(x) || !double.IsFinite(y)) return null;
        double dx = x - size / 2, dy = y - size / 2;
        double radius = Math.Sqrt(dx * dx + dy * dy);
        if (radius < size * InnerRadiusRatio || radius > size * OuterRadiusRatio) return null;
        double step = 2 * Math.PI / Count;
        double angle = (Math.Atan2(dy, dx) + Math.PI / 2 + step / 2 + 2 * Math.PI) % (2 * Math.PI);
        double within = angle % step, halfGap = GapDegrees * Math.PI / 360;
        return within < halfGap || within > step - halfGap ? null : (int)(angle / step);
    }

    public RadialPoint LabelPoint(int index, double size)
    {
        ValidateSize(size);
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
        double angle = -Math.PI / 2 + 2 * Math.PI * index / Count;
        return new(size / 2 + Math.Cos(angle) * size * 0.355,
            size / 2 + Math.Sin(angle) * size * 0.355);
    }

    public void Select(int? index)
    {
        if (index is < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
        SelectedIndex = index;
    }

    public void Move(int delta) => Select((int)(((long)(SelectedIndex ?? (delta < 0 ? 0 : -1)) + delta) % Count + Count) % Count);
    public int? Confirm() => ConfirmedIndex = SelectedIndex;
    public void Cancel() => SelectedIndex = ConfirmedIndex = null;

    private static void ValidateSize(double size)
    {
        if (!double.IsFinite(size) || size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
    }
}
