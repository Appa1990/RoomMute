using System.Windows;
using System.Windows.Media;
namespace RoomMute.Views;

public sealed class LevelMeter : FrameworkElement
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(nameof(Level), typeof(double), typeof(LevelMeter),
        new FrameworkPropertyMetadata(-96d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(nameof(Threshold), typeof(double), typeof(LevelMeter),
        new FrameworkPropertyMetadata(-35d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SilenceThresholdProperty = DependencyProperty.Register(nameof(SilenceThreshold), typeof(double), typeof(LevelMeter),
        new FrameworkPropertyMetadata(-42d, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Level { get => (double)GetValue(LevelProperty); set => SetValue(LevelProperty, value); }
    public double Threshold { get => (double)GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }
    public double SilenceThreshold { get => (double)GetValue(SilenceThresholdProperty); set => SetValue(SilenceThresholdProperty, value); }
    private static readonly Brush Empty = Frozen(39, 54, 70);
    private static readonly Brush Speak = Frozen(99, 226, 183);
    private static readonly Brush Silence = Frozen(137, 184, 255);
    private static Brush Frozen(byte r, byte g, byte b) { var brush = new SolidColorBrush(Color.FromRgb(r, g, b)); brush.Freeze(); return brush; }
    private double Position(double db) => Math.Clamp((db + 96) / 96, 0, 1) * Math.Max(0, ActualWidth - 8) + 4;
    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 10) return;
        const int count = 52;
        double step = ActualWidth / count;
        double filled = Math.Clamp((Level + 96) / 96, 0, 1) * count;
        for (int i = 0; i < count; i++)
        {
            Brush brush = i < filled ? i > 47 ? Brushes.Salmon : i > 42 ? Brushes.Khaki : Speak : Empty;
            dc.DrawRoundedRectangle(brush, null, new Rect(i * step, 10, Math.Max(1, step - 3), 22), 2, 2);
        }
        DrawMarker(dc, Position(Threshold), Speak, true);
        DrawMarker(dc, Position(SilenceThreshold), Silence, false);
    }
    private static void DrawMarker(DrawingContext dc, double x, Brush brush, bool top)
    {
        dc.DrawLine(new Pen(brush, 1.5), new Point(x, top ? 5 : 10), new Point(x, top ? 32 : 38));
        var shape = new StreamGeometry();
        using (var geometry = shape.Open())
        {
            geometry.BeginFigure(new Point(x, top ? 9 : 33), true, true);
            geometry.LineTo(new Point(x - 4, top ? 3 : 39), true, false);
            geometry.LineTo(new Point(x + 4, top ? 3 : 39), true, false);
        }
        shape.Freeze();
        dc.DrawGeometry(brush, null, shape);
    }
}
