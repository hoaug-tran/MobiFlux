using System.Windows;
using MobiFlux.Desktop.Services;

namespace MobiFlux.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LanguageManager.Initialize();
        ThemeManager.ApplyTheme(AppTheme.Light);
        base.OnStartup(e);
    }
}
