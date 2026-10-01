using System.Windows;
using Wpf.Ui.Appearance;

namespace MobiFlux.Desktop.Services;

public enum AppTheme
{
    Light,
    Dark
}

public static class ThemeManager
{
    private static AppTheme _currentTheme = AppTheme.Light;

    public static AppTheme CurrentTheme => _currentTheme;

    public static void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;
        var themeUri = theme == AppTheme.Dark
            ? new Uri("Themes/DesignTokens.Dark.xaml", UriKind.Relative)
            : new Uri("Themes/DesignTokens.Light.xaml", UriKind.Relative);

        var newDict = new ResourceDictionary { Source = themeUri };

        var appResources = Application.Current.Resources.MergedDictionaries;
        for (int i = 0; i < appResources.Count; i++)
        {
            var dict = appResources[i];
            if (dict.Source != null && (dict.Source.OriginalString.Contains("DesignTokens.Light.xaml") || dict.Source.OriginalString.Contains("DesignTokens.Dark.xaml") || dict.Source.OriginalString.Contains("DesignTokens.xaml")))
            {
                appResources[i] = newDict;
                break;
            }
        }

        ApplicationThemeManager.Apply(theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }

    public static AppTheme ToggleTheme()
    {
        var target = _currentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
        ApplyTheme(target);
        return target;
    }
}
