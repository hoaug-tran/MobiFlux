using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace MobiFlux.Desktop;

public partial class MainWindow : FluentWindow
{
    private readonly DashboardViewModel _viewModel;
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.RefreshCommand.ExecuteAsync(null);
        SizeChanged += (_, _) => SidebarColumn.Width = new GridLength(ActualWidth < 1120 ? 184 : 224);
        Closed += (_, _) => _viewModel.Dispose();
    }
}
