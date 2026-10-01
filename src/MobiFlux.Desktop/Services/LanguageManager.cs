using System.Windows;
using MobiFlux.Desktop.Configuration;

namespace MobiFlux.Desktop.Services;

public enum AppLanguage
{
    Vietnamese,
    English
}

public static class LanguageManager
{
    private static AppLanguage _currentLanguage = AppLanguage.Vietnamese;

    public static AppLanguage CurrentLanguage => _currentLanguage;

    public static event Action<AppLanguage>? LanguageChanged;

    public static void Initialize()
    {
        var saved = DesktopConfiguration.LoadLanguage();
        var initial = saved == "en" ? AppLanguage.English : AppLanguage.Vietnamese;
        ApplyLanguage(initial);
    }

    public static void ApplyLanguage(AppLanguage language)
    {
        _currentLanguage = language;
        var langUri = language == AppLanguage.English
            ? new Uri("Themes/Strings.en.xaml", UriKind.Relative)
            : new Uri("Themes/Strings.vi.xaml", UriKind.Relative);

        var newDict = new ResourceDictionary { Source = langUri };
        var appResources = Application.Current.Resources.MergedDictionaries;

        bool replaced = false;
        for (int i = appResources.Count - 1; i >= 0; i--)
        {
            var dict = appResources[i];
            if (dict.Source != null && (dict.Source.OriginalString.Contains("Strings.vi.xaml") || dict.Source.OriginalString.Contains("Strings.en.xaml")))
            {
                if (!replaced)
                {
                    appResources[i] = newDict;
                    replaced = true;
                }
                else
                {
                    appResources.RemoveAt(i);
                }
            }
        }

        if (!replaced)
        {
            appResources.Add(newDict);
        }

        DesktopConfiguration.SaveLanguage(language == AppLanguage.English ? "en" : "vi");
        LanguageChanged?.Invoke(_currentLanguage);
    }

    public static AppLanguage ToggleLanguage()
    {
        var target = _currentLanguage == AppLanguage.Vietnamese ? AppLanguage.English : AppLanguage.Vietnamese;
        ApplyLanguage(target);
        return target;
    }
}
