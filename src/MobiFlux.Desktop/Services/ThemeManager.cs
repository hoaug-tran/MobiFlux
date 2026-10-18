using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MobiFlux.Desktop.Services;

public enum AppTheme { Light, Dark }

public static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;
    public static event Action? ThemeChanged;
    public static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        ApplicationThemeManager.Apply(theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light, WindowBackdropType.None, updateAccent: false);
        var resources = Application.Current.Resources.MergedDictionaries;
        var replacement = new ResourceDictionary { Source = new Uri(theme == AppTheme.Dark ? "Themes/DesignTokens.Dark.xaml" : "Themes/DesignTokens.Light.xaml", UriKind.Relative) };
        var index = resources.ToList().FindIndex(dictionary => dictionary.Source?.OriginalString.Contains("DesignTokens", StringComparison.Ordinal) == true);
        if (index >= 0) resources[index] = replacement; else resources.Add(replacement);
        foreach (Window window in Application.Current.Windows)
        {
            if (window is FluentWindow fluent) fluent.WindowBackdropType = WindowBackdropType.None;
            window.SetResourceReference(Window.BackgroundProperty, "Brush.Canvas");
            window.SetResourceReference(Window.ForegroundProperty, "Brush.Text");
        }
        ThemeChanged?.Invoke();
    }
    public static AppTheme ToggleTheme()
    {
        var next = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
        ApplyTheme(next);
        return next;
    }
}
