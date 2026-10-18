using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MobiFlux.Desktop.Services;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Desktop.Controls;

public sealed class TrafficChart : FrameworkElement
{
    public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(nameof(Samples), typeof(IReadOnlyList<TrafficSampleDto>), typeof(TrafficChart), new FrameworkPropertyMetadata(Array.Empty<TrafficSampleDto>(), FrameworkPropertyMetadataOptions.AffectsRender));
    public IReadOnlyList<TrafficSampleDto> Samples { get => (IReadOnlyList<TrafficSampleDto>)GetValue(SamplesProperty); set => SetValue(SamplesProperty, value); }
    private int? _hover;
    public TrafficChart()
    {
        MinHeight = 220;
        Cursor = Cursors.Cross;
        Loaded += (_, _) => ThemeManager.ThemeChanged += InvalidateVisual;
        Unloaded += (_, _) => ThemeManager.ThemeChanged -= InvalidateVisual;
        MouseMove += OnMouseMove;
        MouseLeave += (_, _) => { _hover = null; InvalidateVisual(); };
    }
    private Brush Brush(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;
    private void Label(DrawingContext context, string value, Point point, Brush brush, double size = 12)
    {
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        context.DrawText(text, point);
    }
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        context.DrawRectangle(Brushes.Transparent, null, bounds);
        var plot = new Rect(62, 12, Math.Max(1, ActualWidth - 78), Math.Max(1, ActualHeight - 44));
        var maximum = Samples.Count == 0 ? 0 : Samples.Max(sample => Math.Max(sample.UploadMbps, sample.DownloadMbps));
        var scale = maximum > 0 ? maximum * 1.15 : 1;
        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4;
            context.DrawLine(new Pen(Brush("Brush.Border"), 0.5), new Point(plot.Left, y), new Point(plot.Right, y));
            Label(context, maximum > 0 ? $"{scale * (4 - i) / 4:0.##}" : i == 4 ? "0" : "", new Point(0, y - 7), Brush("Brush.Muted"));
        }
        if (Samples.Count > 1)
        {
            for (var i = 0; i < 4; i++)
            {
                var index = (int)Math.Round((Samples.Count - 1) * i / 3d);
                Label(context, Samples[index].At.ToLocalTime().ToString("HH:mm:ss"), new Point(plot.Left + plot.Width * i / 3 - (i == 3 ? 48 : 0), plot.Bottom + 12), Brush("Brush.Muted"));
            }
            DrawSeries(context, plot, scale, sample => sample.DownloadMbps, Brush("Brush.Accent"), true);
            DrawSeries(context, plot, scale, sample => sample.UploadMbps, Brush("Brush.Success"), false);
        }
        if (maximum == 0) Label(context, Samples.Count == 0 ? "Đang chờ dữ liệu" : "Chưa có lưu lượng", new Point(plot.Left + 16, plot.Top + plot.Height / 2), Brush("Brush.Muted"), 14);
        if (_hover is { } selected && selected < Samples.Count && Samples.Count > 1)
        {
            var x = plot.Left + plot.Width * selected / (Samples.Count - 1);
            var pen = new Pen(Brush("Brush.Muted"), 1) { DashStyle = DashStyles.Dash };
            context.DrawLine(pen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            context.DrawEllipse(Brush("Brush.Surface"), new Pen(Brush("Brush.Accent"), 2), new Point(x, plot.Bottom - Samples[selected].DownloadMbps / scale * plot.Height), 4, 4);
        }
    }
    private void DrawSeries(DrawingContext context, Rect plot, double maximum, Func<TrafficSampleDto, double> value, Brush stroke, bool fill)
    {
        var points = Samples.Select((sample, index) => new Point(plot.Left + plot.Width * index / (Samples.Count - 1), plot.Bottom - value(sample) / maximum * plot.Height)).ToArray();
        var line = new StreamGeometry();
        using (var geometry = line.Open()) { geometry.BeginFigure(points[0], false, false); geometry.PolyLineTo(points.Skip(1).ToArray(), true, false); }
        line.Freeze();
        if (fill && stroke is SolidColorBrush color)
        {
            var area = new StreamGeometry();
            using (var geometry = area.Open()) { geometry.BeginFigure(new Point(plot.Left, plot.Bottom), true, true); geometry.PolyLineTo(points, true, false); geometry.LineTo(new Point(plot.Right, plot.Bottom), true, false); }
            area.Freeze();
            var gradient = new LinearGradientBrush(Color.FromArgb(50, color.Color.R, color.Color.G, color.Color.B), Color.FromArgb(0, color.Color.R, color.Color.G, color.Color.B), new Point(0, 0), new Point(0, 1)); gradient.Freeze();
            context.DrawGeometry(gradient, null, area);
        }
        context.DrawGeometry(null, new Pen(stroke, 2), line);
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (Samples.Count == 0) return;
        var index = Math.Clamp((int)Math.Round((e.GetPosition(this).X - 62) / Math.Max(1, ActualWidth - 78) * (Samples.Count - 1)), 0, Samples.Count - 1);
        if (_hover == index) return;
        _hover = index;
        var sample = Samples[index];
        ToolTip = $"{sample.At.ToLocalTime():HH:mm:ss}\nTải xuống {sample.DownloadMbps:0.###} Mbps\nTải lên {sample.UploadMbps:0.###} Mbps";
        InvalidateVisual();
    }
}
