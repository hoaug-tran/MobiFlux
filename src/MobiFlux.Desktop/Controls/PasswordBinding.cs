using System.Windows;
using System.Windows.Controls;

namespace MobiFlux.Desktop.Controls;

public static class PasswordBinding
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(string), typeof(PasswordBinding), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(PasswordBinding), new PropertyMetadata(false, OnEnabledChanged));
    public static string GetValue(DependencyObject target) => (string)target.GetValue(ValueProperty);
    public static void SetValue(DependencyObject target, string value) => target.SetValue(ValueProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    private static void OnValueChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is PasswordBox box && box.Password != (string?)e.NewValue) box.Password = (string?)e.NewValue ?? string.Empty;
    }
    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not PasswordBox box) return;
        box.PasswordChanged -= OnPasswordChanged;
        if (e.NewValue is true) { box.Password = GetValue(box); box.PasswordChanged += OnPasswordChanged; }
    }
    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetCurrentValue(ValueProperty, box.Password);
    }
}
