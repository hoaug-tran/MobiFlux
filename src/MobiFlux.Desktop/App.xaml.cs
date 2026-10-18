using System.Windows;
using MobiFlux.Desktop.Services;
using MobiFlux.Desktop.Configuration;

namespace MobiFlux.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LanguageManager.Initialize();
        ThemeManager.ApplyTheme(DesktopConfiguration.LoadPreferences().Theme == "Dark" ? AppTheme.Dark : AppTheme.Light);
        base.OnStartup(e);
    }
}
