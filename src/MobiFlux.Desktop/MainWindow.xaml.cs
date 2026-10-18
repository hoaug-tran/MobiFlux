using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;
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
        SourceInitialized += (_, _) => FitInitialWindow();
        Loaded += async (_, _) => await _viewModel.RefreshCommand.ExecuteAsync(null);
        SizeChanged += (_, _) => SidebarColumn.Width = new GridLength(ActualWidth < 1120 ? 184 : 224);
        Closed += (_, _) => _viewModel.Dispose();
    }

    private void FitInitialWindow()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(handle, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        var scale = GetDpiForWindow(handle) / 96d;
        if (scale <= 0) scale = 1;
        var workWidth = info.Work.Right - info.Work.Left;
        var workHeight = info.Work.Bottom - info.Work.Top;
        var width = Math.Min((int)Math.Round(Width * scale), workWidth);
        var height = Math.Min((int)Math.Round(Height * scale), workHeight);
        MinWidth = Math.Min(MinWidth, workWidth / scale);
        MinHeight = Math.Min(MinHeight, workHeight / scale);
        Width = width / scale;
        Height = height / scale;
        SetWindowPos(handle, IntPtr.Zero,
            info.Work.Left + (workWidth - width) / 2,
            info.Work.Top + (workHeight - height) / 2,
            width, height, 0x0014);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
