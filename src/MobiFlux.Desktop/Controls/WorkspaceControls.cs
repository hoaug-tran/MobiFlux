using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MobiFlux.Desktop.Controls;

public sealed class RouteVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, parameter) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
public sealed class NotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
}
public sealed class AdaptivePanel : Panel
{
    public static readonly DependencyProperty MinimumItemWidthProperty = DependencyProperty.Register(nameof(MinimumItemWidth), typeof(double), typeof(AdaptivePanel), new FrameworkPropertyMetadata(240d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public double MinimumItemWidth { get => (double)GetValue(MinimumItemWidthProperty); set => SetValue(MinimumItemWidthProperty, value); }
    private const double Gap = 16;
    private int _columns = 1;
    private double _height;
    protected override Size MeasureOverride(Size available)
    {
        var width = double.IsInfinity(available.Width) ? MinimumItemWidth : available.Width;
        _columns = Math.Max(1, Math.Min(InternalChildren.Count, (int)((width + Gap) / (MinimumItemWidth + Gap))));
        if (_columns > 2 && InternalChildren.Count % _columns != 0) _columns--;
        var itemWidth = Math.Max(0, (width - Gap * (_columns - 1)) / _columns);
        _height = 0;
        foreach (UIElement child in InternalChildren) { child.Measure(new Size(itemWidth, double.PositiveInfinity)); _height = Math.Max(_height, child.DesiredSize.Height); }
        var rows = (int)Math.Ceiling(InternalChildren.Count / (double)_columns);
        return new Size(width, rows == 0 ? 0 : rows * _height + (rows - 1) * Gap);
    }
    protected override Size ArrangeOverride(Size final)
    {
        var width = Math.Max(0, (final.Width - Gap * (_columns - 1)) / _columns);
        for (var i = 0; i < InternalChildren.Count; i++) InternalChildren[i].Arrange(new Rect(i % _columns * (width + Gap), i / _columns * (_height + Gap), width, _height));
        return final;
    }
}
public sealed class InverseBooleanVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
public sealed class DialogHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Math.Max(160, (value is double height ? height : 600) - 180);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
