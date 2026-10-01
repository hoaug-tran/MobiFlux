using Wpf.Ui.Controls;
using MobiFlux.Desktop.Services;

namespace MobiFlux.Desktop;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyTheme(AppTheme.Light);
        var viewModel = new DashboardViewModel();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.RefreshAsync();
    }
}
