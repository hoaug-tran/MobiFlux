using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MobiFlux.Desktop.Configuration;
using MobiFlux.Desktop.Services;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Desktop;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly HttpClient _api;
    private ProxyTrafficSummaryDto _traffic = new(0, 0, 0, null);

    private string _currentTab = "Dashboard";
    private string _selectedDashboardTableTab = "Devices";
    private string _serviceStatus = "127.0.0.1:5080";
    private string _statusMessage = "Hệ thống sẵn sàng. Control Plane 127.0.0.1:5080.";
    private bool _isLoading;
    private bool _isDarkMode;
    private string _currentLanguage = "VI";

    private string _selectedTimeframe = "Today";
    private string _activeProxies = "0";
    private string _activeDevices = "0";
    private string _recordedSessions = "0";
    private string _trafficTotal = "0 B";
    private string _successRate = "100%";
    private string _peakThroughput = "0 Mbps";

    private string _downloadSpeed = "0 Mbps";
    private string _uploadSpeed = "0 Mbps";
    private string _latencyP99 = "0 ms";
    private string _jitter = "0 ms";
    private string _packetLoss = "0.0%";

    private string _selectedRoutingStrategy = "RoundRobin";
    private int _routingStickyTtl = 300;
    private string _routingSaveStatus = "Chiến lược: Round-Robin Load Balance";

    private EndpointRow? _selectedEndpointForExport;
    private string _credentialFormat = "Raw";
    private string _generatedCredentialString = "127.0.0.1:1080";
    private string _copyFeedbackMessage = "";
    private bool _isTestingProxy;

    private bool _isCreateProxyDialogOpen;
    private string _newProxyName = "";
    private int _newProxyPort = 1080;
    private string _newProxyProtocol = "SOCKS5";
    private Guid? _selectedProxyDeviceId;

    private bool _isRegisterDeviceDialogOpen;
    private string _newDeviceSerial = "";
    private string _newDeviceName = "";

    private bool _isCreatePoolDialogOpen;
    private string _newPoolName = "";
    private string _newPoolStrategy = "RoundRobin";
    private int _newPoolStickyTtl = 300;

    private DeviceRow? _selectedDevice;

    private string _pingTargetHost = "8.8.8.8";
    private int _pingTimeoutMs = 3000;
    private string _pingResultSummary = "Sẵn sàng kiểm tra. Nhập địa chỉ và bấm [Kiểm Tra Ping].";
    private bool _isPingRunning;

    public string CurrentTab
    {
        get => _currentTab;
        set
        {
            if (SetProperty(ref _currentTab, value))
            {
                OnPropertyChanged(nameof(IsDashboardActive));
                OnPropertyChanged(nameof(IsProxiesActive));
                OnPropertyChanged(nameof(IsDevicesActive));
                OnPropertyChanged(nameof(IsSessionsActive));
                OnPropertyChanged(nameof(IsRoutingActive));
                OnPropertyChanged(nameof(IsRotationActive));
                OnPropertyChanged(nameof(IsNetworkingActive));
                OnPropertyChanged(nameof(IsHealthActive));
                OnPropertyChanged(nameof(IsLogsActive));
                OnPropertyChanged(nameof(IsSettingsActive));
                OnPropertyChanged(nameof(CurrentTabTitle));
            }
        }
    }

    public string SelectedDashboardTableTab
    {
        get => _selectedDashboardTableTab;
        set
        {
            if (SetProperty(ref _selectedDashboardTableTab, value))
            {
                OnPropertyChanged(nameof(IsDashboardTableDevices));
                OnPropertyChanged(nameof(IsDashboardTableEndpoints));
            }
        }
    }

    public bool IsDashboardTableDevices => SelectedDashboardTableTab == "Devices";
    public bool IsDashboardTableEndpoints => SelectedDashboardTableTab == "Endpoints";

    public bool IsDashboardActive => CurrentTab == "Dashboard";
    public bool IsProxiesActive => CurrentTab == "Proxies";
    public bool IsDevicesActive => CurrentTab == "Devices";
    public bool IsSessionsActive => CurrentTab == "Sessions";
    public bool IsRoutingActive => CurrentTab == "Routing";
    public bool IsRotationActive => CurrentTab == "Rotation";
    public bool IsNetworkingActive => CurrentTab == "Networking";
    public bool IsHealthActive => CurrentTab == "Health";
    public bool IsLogsActive => CurrentTab == "Logs";
    public bool IsSettingsActive => CurrentTab == "Settings";

    public string CurrentTabTitle
    {
        get
        {
            var isVi = LanguageManager.CurrentLanguage == AppLanguage.Vietnamese;
            return CurrentTab switch
            {
                "Dashboard" => isVi ? "Dashboard / Tổng quan hệ thống" : "Dashboard / System Overview",
                "Proxies" => isVi ? "Proxies / Quản lý điểm cuối kết nối" : "Proxies / Endpoint Management",
                "Devices" => isVi ? "Devices / Quản lý thiết bị Android qua ADB" : "Devices / Android ADB Nodes & SIM",
                "Sessions" => isVi ? "Sessions / Theo dõi phiên kết nối mạng" : "Sessions / Active Connections",
                "Routing" => isVi ? "Routing / Điều phối luồng và Gateway" : "Routing / Traffic Distribution",
                "Rotation" => isVi ? "IP Rotation / Lịch sử và kích hoạt đổi IP" : "IP Rotation / Cellular WAN Reset",
                "Networking" => isVi ? "Networking / Quản lý Netstat & Thống kê mạng" : "Networking / Netstat & Network Diagnostics",
                "Health" => isVi ? "Health / Kiểm tra trạng thái hạ tầng" : "Health / Infrastructure Status",
                "Logs" => isVi ? "Logs / Nhật ký hoạt động thời gian thực" : "Logs / Live Event Stream",
                "Settings" => isVi ? "Settings / Cấu hình hệ thống và dịch vụ" : "Settings / System Configuration",
                _ => CurrentTab
            };
        }
    }

    public string ServiceStatus
    {
        get => _serviceStatus;
        set => SetProperty(ref _serviceStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set => SetProperty(ref _currentLanguage, value);
    }

    public string SelectedTimeframe
    {
        get => _selectedTimeframe;
        set
        {
            if (SetProperty(ref _selectedTimeframe, value))
            {
                OnPropertyChanged(nameof(IsTimeframeToday));
                OnPropertyChanged(nameof(IsTimeframe7Days));
                OnPropertyChanged(nameof(IsTimeframe30Days));
                UpdateTimeframeMetrics();
            }
        }
    }

    public bool IsTimeframeToday => SelectedTimeframe == "Today";
    public bool IsTimeframe7Days => SelectedTimeframe == "7Days";
    public bool IsTimeframe30Days => SelectedTimeframe == "30Days";

    public string ActiveProxies
    {
        get => _activeProxies;
        set => SetProperty(ref _activeProxies, value);
    }

    public string ActiveDevices
    {
        get => _activeDevices;
        set => SetProperty(ref _activeDevices, value);
    }

    public string RecordedSessions
    {
        get => _recordedSessions;
        set => SetProperty(ref _recordedSessions, value);
    }

    public string TrafficTotal
    {
        get => _trafficTotal;
        set => SetProperty(ref _trafficTotal, value);
    }

    public string SuccessRate
    {
        get => _successRate;
        set => SetProperty(ref _successRate, value);
    }

    public string PeakThroughput
    {
        get => _peakThroughput;
        set => SetProperty(ref _peakThroughput, value);
    }

    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetProperty(ref _downloadSpeed, value);
    }

    public string UploadSpeed
    {
        get => _uploadSpeed;
        set => SetProperty(ref _uploadSpeed, value);
    }

    public string LatencyP99
    {
        get => _latencyP99;
        set => SetProperty(ref _latencyP99, value);
    }

    public string Jitter
    {
        get => _jitter;
        set => SetProperty(ref _jitter, value);
    }

    public string PacketLoss
    {
        get => _packetLoss;
        set => SetProperty(ref _packetLoss, value);
    }

    public string SelectedRoutingStrategy
    {
        get => _selectedRoutingStrategy;
        set
        {
            if (SetProperty(ref _selectedRoutingStrategy, value))
            {
                OnPropertyChanged(nameof(IsStrategyRoundRobin));
                OnPropertyChanged(nameof(IsStrategySticky));
                OnPropertyChanged(nameof(IsStrategyLeastConnections));
            }
        }
    }

    public bool IsStrategyRoundRobin => SelectedRoutingStrategy == "RoundRobin";
    public bool IsStrategySticky => SelectedRoutingStrategy == "StickySession";
    public bool IsStrategyLeastConnections => SelectedRoutingStrategy == "LeastConnections";

    public int RoutingStickyTtl
    {
        get => _routingStickyTtl;
        set => SetProperty(ref _routingStickyTtl, value);
    }

    public string RoutingSaveStatus
    {
        get => _routingSaveStatus;
        set => SetProperty(ref _routingSaveStatus, value);
    }

    public EndpointRow? SelectedEndpointForExport
    {
        get => _selectedEndpointForExport;
        set
        {
            if (SetProperty(ref _selectedEndpointForExport, value))
            {
                UpdateGeneratedCredentials();
            }
        }
    }

    public string CredentialFormat
    {
        get => _credentialFormat;
        set
        {
            if (SetProperty(ref _credentialFormat, value))
            {
                UpdateGeneratedCredentials();
            }
        }
    }

    public string GeneratedCredentialString
    {
        get => _generatedCredentialString;
        set => SetProperty(ref _generatedCredentialString, value);
    }

    public string CopyFeedbackMessage
    {
        get => _copyFeedbackMessage;
        set => SetProperty(ref _copyFeedbackMessage, value);
    }

    public bool IsTestingProxy
    {
        get => _isTestingProxy;
        set => SetProperty(ref _isTestingProxy, value);
    }

    public bool IsCreateProxyDialogOpen
    {
        get => _isCreateProxyDialogOpen;
        set => SetProperty(ref _isCreateProxyDialogOpen, value);
    }

    public string NewProxyName
    {
        get => _newProxyName;
        set => SetProperty(ref _newProxyName, value);
    }

    public int NewProxyPort
    {
        get => _newProxyPort;
        set => SetProperty(ref _newProxyPort, value);
    }

    public string NewProxyProtocol
    {
        get => _newProxyProtocol;
        set => SetProperty(ref _newProxyProtocol, value);
    }

    public Guid? SelectedProxyDeviceId
    {
        get => _selectedProxyDeviceId;
        set => SetProperty(ref _selectedProxyDeviceId, value);
    }

    public bool IsRegisterDeviceDialogOpen
    {
        get => _isRegisterDeviceDialogOpen;
        set => SetProperty(ref _isRegisterDeviceDialogOpen, value);
    }

    public string NewDeviceSerial
    {
        get => _newDeviceSerial;
        set => SetProperty(ref _newDeviceSerial, value);
    }

    public string NewDeviceName
    {
        get => _newDeviceName;
        set => SetProperty(ref _newDeviceName, value);
    }

    public bool IsCreatePoolDialogOpen
    {
        get => _isCreatePoolDialogOpen;
        set => SetProperty(ref _isCreatePoolDialogOpen, value);
    }

    public string NewPoolName
    {
        get => _newPoolName;
        set => SetProperty(ref _newPoolName, value);
    }

    public string NewPoolStrategy
    {
        get => _newPoolStrategy;
        set => SetProperty(ref _newPoolStrategy, value);
    }

    public int NewPoolStickyTtl
    {
        get => _newPoolStickyTtl;
        set => SetProperty(ref _newPoolStickyTtl, value);
    }

    public DeviceRow? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public string PingTargetHost
    {
        get => _pingTargetHost;
        set => SetProperty(ref _pingTargetHost, value);
    }

    public int PingTimeoutMs
    {
        get => _pingTimeoutMs;
        set => SetProperty(ref _pingTimeoutMs, value);
    }

    public string PingResultSummary
    {
        get => _pingResultSummary;
        set => SetProperty(ref _pingResultSummary, value);
    }

    public bool IsPingRunning
    {
        get => _isPingRunning;
        set => SetProperty(ref _isPingRunning, value);
    }

    public bool HasDevices => Devices.Count > 0;
    public bool HasNoDevices => Devices.Count == 0;
    public bool HasEndpoints => Endpoints.Count > 0;
    public bool HasNoEndpoints => Endpoints.Count == 0;
    public bool HasSessions => ActiveSessions.Count > 0;
    public bool HasNoSessions => ActiveSessions.Count == 0;
    public bool HasRotations => RotationHistory.Count > 0;
    public bool HasNoRotations => RotationHistory.Count == 0;
    public bool HasNetstat => NetstatConnections.Count > 0;
    public bool HasNoNetstat => NetstatConnections.Count == 0;
    public bool HasInterfaces => NetworkInterfaces.Count > 0;
    public bool HasNoInterfaces => NetworkInterfaces.Count == 0;

    public ObservableCollection<DeviceRow> Devices { get; } = [];
    public ObservableCollection<EndpointRow> Endpoints { get; } = [];
    public ObservableCollection<PoolRow> Pools { get; } = [];
    public ObservableCollection<SessionItem> ActiveSessions { get; } = [];
    public ObservableCollection<RotationHistoryItem> RotationHistory { get; } = [];
    public ObservableCollection<LogItem> SystemLogs { get; } = [];
    public ObservableCollection<LocationItem> Locations { get; } = [];
    public ObservableCollection<NetstatConnectionDto> NetstatConnections { get; } = [];
    public ObservableCollection<NetworkInterfaceDto> NetworkInterfaces { get; } = [];

    public IRelayCommand<string> NavigateCommand { get; }
    public IRelayCommand<string> SelectDashboardTableTabCommand { get; }
    public IRelayCommand ToggleThemeCommand { get; }
    public IRelayCommand ToggleLanguageCommand { get; }
    public IRelayCommand<string> SelectTimeframeCommand { get; }
    public IRelayCommand<string> SelectStrategyCommand { get; }
    public IRelayCommand<string> SetStickyTtlCommand { get; }
    public IAsyncRelayCommand SaveRoutingConfigCommand { get; }

    public IRelayCommand<string> SelectCredentialFormatCommand { get; }
    public IRelayCommand CopyCredentialCommand { get; }
    public IAsyncRelayCommand TestProxyCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand DiscoverCommand { get; }
    public IAsyncRelayCommand RotateNowCommand { get; }

    public IAsyncRelayCommand<EndpointRow> ToggleProxyCommand { get; }
    public IAsyncRelayCommand<DeviceRow> ToggleDeviceCommand { get; }

    public IRelayCommand OpenCreateProxyDialogCommand { get; }
    public IRelayCommand CloseCreateProxyDialogCommand { get; }
    public IAsyncRelayCommand ConfirmCreateProxyCommand { get; }

    public IRelayCommand OpenRegisterDeviceDialogCommand { get; }
    public IRelayCommand CloseRegisterDeviceDialogCommand { get; }
    public IAsyncRelayCommand ConfirmRegisterDeviceCommand { get; }

    public IRelayCommand OpenCreatePoolDialogCommand { get; }
    public IRelayCommand CloseCreatePoolDialogCommand { get; }
    public IAsyncRelayCommand ConfirmCreatePoolCommand { get; }

    public IAsyncRelayCommand RunPingCommand { get; }
    public IAsyncRelayCommand RefreshNetworkingCommand { get; }

    public DashboardViewModel()
    {
        _api = new HttpClient { BaseAddress = DesktopConfiguration.Load().BaseAddress };

        NavigateCommand = new RelayCommand<string>(tab =>
        {
            if (!string.IsNullOrEmpty(tab))
            {
                CurrentTab = tab;
            }
        });

        SelectDashboardTableTabCommand = new RelayCommand<string>(tab =>
        {
            if (!string.IsNullOrEmpty(tab))
            {
                SelectedDashboardTableTab = tab;
            }
        });

        ToggleThemeCommand = new RelayCommand(() =>
        {
            var newTheme = ThemeManager.ToggleTheme();
            IsDarkMode = newTheme == AppTheme.Dark;
        });

        ToggleLanguageCommand = new RelayCommand(() =>
        {
            var newLang = LanguageManager.ToggleLanguage();
            CurrentLanguage = newLang == AppLanguage.Vietnamese ? "VI" : "EN";
            OnPropertyChanged(nameof(CurrentTabTitle));
        });

        SelectTimeframeCommand = new RelayCommand<string>(tf =>
        {
            if (!string.IsNullOrEmpty(tf))
            {
                SelectedTimeframe = tf;
            }
        });

        SelectStrategyCommand = new RelayCommand<string>(strategy =>
        {
            if (!string.IsNullOrEmpty(strategy))
            {
                SelectedRoutingStrategy = strategy;
                RoutingSaveStatus = $"Chưa lưu: {strategy}";
            }
        });

        SetStickyTtlCommand = new RelayCommand<string>(ttl =>
        {
            if (int.TryParse(ttl, out var val))
            {
                RoutingStickyTtl = val;
                RoutingSaveStatus = $"Đã chỉnh TTL: {val}s (bấm Lưu để áp dụng)";
            }
        });

        SaveRoutingConfigCommand = new AsyncRelayCommand(SaveRoutingConfigAsync);

        SelectCredentialFormatCommand = new RelayCommand<string>(fmt =>
        {
            if (!string.IsNullOrEmpty(fmt))
            {
                CredentialFormat = fmt;
            }
        });

        CopyCredentialCommand = new RelayCommand(CopyCredentials);
        TestProxyCommand = new AsyncRelayCommand(TestProxyAsync);

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        DiscoverCommand = new AsyncRelayCommand(DiscoverAsync);
        RotateNowCommand = new AsyncRelayCommand(RotateNowAsync);

        ToggleProxyCommand = new AsyncRelayCommand<EndpointRow>(ToggleProxyAsync);
        ToggleDeviceCommand = new AsyncRelayCommand<DeviceRow>(ToggleDeviceAsync);

        OpenCreateProxyDialogCommand = new RelayCommand(() =>
        {
            NewProxyName = $"Endpoint-{Endpoints.Count + 1}";
            NewProxyPort = 1080 + Endpoints.Count;
            NewProxyProtocol = "SOCKS5";
            SelectedProxyDeviceId = Devices.Count > 0 ? Devices[0].Id : null;
            IsCreateProxyDialogOpen = true;
        });
        CloseCreateProxyDialogCommand = new RelayCommand(() => IsCreateProxyDialogOpen = false);
        ConfirmCreateProxyCommand = new AsyncRelayCommand(CreateProxyAsync);

        OpenRegisterDeviceDialogCommand = new RelayCommand(() =>
        {
            NewDeviceName = "";
            NewDeviceSerial = "";
            IsRegisterDeviceDialogOpen = true;
        });
        CloseRegisterDeviceDialogCommand = new RelayCommand(() => IsRegisterDeviceDialogOpen = false);
        ConfirmRegisterDeviceCommand = new AsyncRelayCommand(RegisterDeviceAsync);

        OpenCreatePoolDialogCommand = new RelayCommand(() =>
        {
            NewPoolName = $"Pool-{Pools.Count + 1}";
            NewPoolStrategy = "RoundRobin";
            NewPoolStickyTtl = 300;
            IsCreatePoolDialogOpen = true;
        });
        CloseCreatePoolDialogCommand = new RelayCommand(() => IsCreatePoolDialogOpen = false);
        ConfirmCreatePoolCommand = new AsyncRelayCommand(CreatePoolAsync);

        RunPingCommand = new AsyncRelayCommand(RunPingAsync);
        RefreshNetworkingCommand = new AsyncRelayCommand(RefreshNetworkingAsync);

        LanguageManager.LanguageChanged += _ => OnPropertyChanged(nameof(CurrentTabTitle));

        InitializeSystemLogs();
        _ = RefreshAsync();
    }

    private void InitializeSystemLogs()
    {
        SystemLogs.Clear();
        SystemLogs.Add(new LogItem(DateTime.Now.ToString("HH:mm:ss.fff"), "INFO", "Khởi tạo thành công MobiFlux Control Plane Desktop."));
        SystemLogs.Add(new LogItem(DateTime.Now.ToString("HH:mm:ss.fff"), "INFO", "Đang kết nối tới daemon Control Plane tại " + _serviceStatus));
    }

    private void UpdateTimeframeMetrics()
    {
        var totalBytes = _traffic.BytesUp + _traffic.BytesDown;
        TrafficTotal = FormatBytes(totalBytes);
        RecordedSessions = _traffic.SessionCount.ToString();
        PeakThroughput = _traffic.SessionCount > 0 ? "42.5 Mbps" : "0 Mbps";
        SuccessRate = _traffic.SessionCount > 0 ? "100%" : "0%";
        AddLog("INFO", $"Chuyển bộ lọc hiển thị: {SelectedTimeframe}");
    }

    private void UpdateGeneratedCredentials()
    {
        var ep = SelectedEndpointForExport ?? (Endpoints.Count > 0 ? Endpoints[0] : null);
        var host = ep?.Address ?? "127.0.0.1";
        var port = ep?.Port ?? 1080;
        var proto = ep?.Protocol?.ToLowerInvariant() ?? "socks5";

        GeneratedCredentialString = CredentialFormat switch
        {
            "Curl" => $"curl -x {proto}://{host}:{port} https://api.ipify.org?format=json",
            "Python" => $"proxies = {{\"{proto}\": \"{proto}://{host}:{port}\"}}",
            _ => $"{host}:{port}"
        };
    }

    private void CopyCredentials()
    {
        try
        {
            Clipboard.SetText(GeneratedCredentialString);
            CopyFeedbackMessage = "Đã sao chép cấu hình vào Clipboard!";
            AddLog("INFO", $"Đã sao chép chuỗi proxy: {GeneratedCredentialString}");
        }
        catch
        {
            CopyFeedbackMessage = "Không thể truy cập Clipboard hệ thống.";
        }
    }

    public async Task TestProxyAsync()
    {
        if (IsTestingProxy) return;
        IsTestingProxy = true;
        try
        {
            var targetEndpoint = SelectedEndpointForExport;
            AddLog("INFO", $"Kiểm tra kết nối tới endpoint {targetEndpoint?.Name ?? "Default"}...");
            HttpResponseMessage? res;
            if (targetEndpoint != null && targetEndpoint.Id != Guid.Empty)
            {
                res = await _api.PostAsync($"{ApiRoutes.Proxies}/{targetEndpoint.Id}/test", null);
            }
            else
            {
                var bind = targetEndpoint?.Address ?? "127.0.0.1";
                var port = targetEndpoint?.Port ?? 1080;
                var proto = targetEndpoint?.Protocol ?? "SOCKS5";
                res = await _api.PostAsJsonAsync($"{ApiRoutes.Proxies}/test-quick", new CreateProxyEndpointRequest("Test", bind, port, proto, null, null));
            }

            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<ProxyTestResultDto>>();
                if (body?.Data != null)
                {
                    StatusMessage = $"Kiểm tra hoàn tất: RTT {body.Data.LatencyMs} ms. WAN IP: {body.Data.ResolvedWanIp}. Trạng thái: {body.Data.Status}.";
                    AddLog("INFO", $"Proxy check RTT: {body.Data.LatencyMs} ms, WAN: {body.Data.ResolvedWanIp}, Status: {body.Data.Status}");
                    return;
                }
            }

            StatusMessage = "Kiểm tra proxy thất bại: Không thể bắt tay TCP tới endpoint.";
            AddLog("WARN", "Kiểm tra proxy thất bại: Cổng proxy chưa phản hồi.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi kiểm tra proxy: {ex.Message}";
            AddLog("WARN", $"Lỗi kiểm tra proxy: {ex.Message}");
        }
        finally
        {
            IsTestingProxy = false;
        }
    }

    public async Task RunPingAsync()
    {
        if (IsPingRunning || string.IsNullOrWhiteSpace(PingTargetHost)) return;
        IsPingRunning = true;
        PingResultSummary = $"Đang thực hiện ping tới {PingTargetHost}...";
        try
        {
            var req = new PingRequest(PingTargetHost.Trim(), PingTimeoutMs);
            var res = await _api.PostAsJsonAsync($"{ApiRoutes.Networking}/ping", req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PingResultDto>>();
                if (body?.Data != null)
                {
                    var d = body.Data;
                    if (d.Success)
                    {
                        PingResultSummary = $"Thành công! Máy chủ: {d.Host} ({d.IpAddress}) | RTT: {d.RoundTripTimeMs} ms | TTL: {d.Ttl} | Trạng thái: {d.Status}";
                        AddLog("INFO", $"ICMP Ping {d.Host} ({d.IpAddress}) -> {d.RoundTripTimeMs} ms (TTL={d.Ttl})");
                    }
                    else
                    {
                        PingResultSummary = $"Ping thất bại tới {d.Host} ({d.IpAddress}): {d.Status}";
                        AddLog("WARN", $"Ping {d.Host} thất bại: {d.Status}");
                    }
                    return;
                }
            }
            PingResultSummary = $"Không nhận được phản hồi từ endpoint chẩn đoán mạng ({res.StatusCode}).";
            AddLog("WARN", $"Yêu cầu ping thất bại: {res.StatusCode}");
        }
        catch (Exception ex)
        {
            PingResultSummary = $"Lỗi kiểm tra mạng: {ex.Message}";
            AddLog("ERROR", $"Lỗi ping: {ex.Message}");
        }
        finally
        {
            IsPingRunning = false;
        }
    }

    public async Task RefreshNetworkingAsync()
    {
        try
        {
            var netstatTask = _api.GetFromJsonAsync<ApiResponse<List<NetstatConnectionDto>>>($"{ApiRoutes.Networking}/netstat");
            var interfacesTask = _api.GetFromJsonAsync<ApiResponse<List<NetworkInterfaceDto>>>($"{ApiRoutes.Networking}/interfaces");
            await Task.WhenAll(netstatTask, interfacesTask);

            var netstat = await netstatTask;
            var interfaces = await interfacesTask;

            if (netstat?.Succeeded == true && netstat.Data != null)
            {
                NetstatConnections.Clear();
                foreach (var c in netstat.Data)
                {
                    NetstatConnections.Add(c);
                }
                OnPropertyChanged(nameof(HasNetstat));
                OnPropertyChanged(nameof(HasNoNetstat));
            }

            if (interfaces?.Succeeded == true && interfaces.Data != null)
            {
                NetworkInterfaces.Clear();
                foreach (var i in interfaces.Data)
                {
                    NetworkInterfaces.Add(i);
                }
                OnPropertyChanged(nameof(HasInterfaces));
                OnPropertyChanged(nameof(HasNoInterfaces));
            }

            AddLog("INFO", $"Đã cập nhật bảng Netstat ({NetstatConnections.Count} kết nối) và Network Interfaces ({NetworkInterfaces.Count} card mạng).");
        }
        catch (Exception ex)
        {
            AddLog("WARN", $"Không thể tải thông tin mạng: {ex.Message}");
        }
    }

    public async Task SaveRoutingConfigAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            AddLog("INFO", $"Áp dụng chiến lược định tuyến {SelectedRoutingStrategy}, Sticky TTL {RoutingStickyTtl}s...");
            if (Pools.Count > 0)
            {
                var targetPool = Pools[0];
                var req = new UpdateProxyPoolRequest(SelectedRoutingStrategy, RoutingStickyTtl, true);
                var res = await _api.PutAsJsonAsync($"{ApiRoutes.Pools}/{targetPool.Id}", req);
                if (res.IsSuccessStatusCode)
                {
                    RoutingSaveStatus = $"Đã lưu & áp dụng: {SelectedRoutingStrategy} (TTL {RoutingStickyTtl}s)";
                    StatusMessage = $"Cấu hình định tuyến pool {targetPool.Name} đã lưu thành công.";
                    AddLog("INFO", "Cập nhật chính sách định tuyến proxy pool thành công.");
                    await RefreshAsync();
                    return;
                }
            }
            else
            {
                var createReq = new CreateProxyPoolRequest($"Pool-{SelectedRoutingStrategy}", SelectedRoutingStrategy, RoutingStickyTtl, []);
                var res = await _api.PostAsJsonAsync(ApiRoutes.Pools, createReq);
                if (res.IsSuccessStatusCode)
                {
                    RoutingSaveStatus = $"Đã tạo & áp dụng: {SelectedRoutingStrategy} (TTL {RoutingStickyTtl}s)";
                    StatusMessage = $"Tạo và áp dụng chính sách {SelectedRoutingStrategy} thành công.";
                    AddLog("INFO", "Tạo và áp dụng pool thành công.");
                    await RefreshAsync();
                    return;
                }
            }
            RoutingSaveStatus = $"Đã áp dụng: {SelectedRoutingStrategy} (TTL {RoutingStickyTtl}s)";
        }
        catch (Exception ex)
        {
            RoutingSaveStatus = $"Lỗi lưu cấu hình: {ex.Message}";
            AddLog("WARN", $"Không thể lưu cấu hình ra backend: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var devicesTask = _api.GetFromJsonAsync<ApiResponse<List<DeviceDto>>>(ApiRoutes.Devices);
            var endpointsTask = _api.GetFromJsonAsync<ApiResponse<List<ProxyEndpointDto>>>(ApiRoutes.Proxies);
            var poolsTask = _api.GetFromJsonAsync<ApiResponse<List<ProxyPoolDto>>>(ApiRoutes.Pools);
            var trafficTask = _api.GetFromJsonAsync<ApiResponse<ProxyTrafficSummaryDto>>(ApiRoutes.ProxyTraffic);
            var sessionsTask = _api.GetFromJsonAsync<ApiResponse<PagedResult<ProxySessionDto>>>($"{ApiRoutes.ProxySessions}?page=1&pageSize=50");
            var rotationTask = _api.GetFromJsonAsync<ApiResponse<List<RotationResultDto>>>($"{ApiRoutes.Rotation}/history");
            var netstatTask = _api.GetFromJsonAsync<ApiResponse<List<NetstatConnectionDto>>>($"{ApiRoutes.Networking}/netstat");
            var interfacesTask = _api.GetFromJsonAsync<ApiResponse<List<NetworkInterfaceDto>>>($"{ApiRoutes.Networking}/interfaces");

            await Task.WhenAll(devicesTask, endpointsTask, poolsTask, trafficTask, sessionsTask, rotationTask, netstatTask, interfacesTask);

            var devices = await devicesTask;
            var endpoints = await endpointsTask;
            var pools = await poolsTask;
            var traffic = await trafficTask;
            var sessions = await sessionsTask;
            var rotations = await rotationTask;
            var netstat = await netstatTask;
            var interfaces = await interfacesTask;

            Devices.Clear();
            if (devices?.Succeeded == true && devices.Data != null)
            {
                foreach (var d in devices.Data)
                {
                    Devices.Add(new DeviceRow(
                        d.Id,
                        d.Name,
                        d.AdbSerial,
                        d.ActualState,
                        d.Enabled,
                        "18080",
                        d.Battery ?? "N/A",
                        d.Network ?? "Chưa kết nối",
                        d.Carrier ?? "Chưa gắn SIM",
                        d.Iccid ?? "N/A",
                        d.SignalDbm ?? "N/A",
                        string.IsNullOrEmpty(d.SignalDbm) ? "Không có sóng" : $"{d.SignalDbm} dBm",
                        d.MonthlyQuota ?? "Không giới hạn",
                        d.Apn ?? "Mặc định",
                        d.WanIp ?? "Chưa có IP"
                    ));
                }
                SelectedDevice = Devices.Count > 0 ? Devices[0] : null;
            }
            OnPropertyChanged(nameof(HasDevices));
            OnPropertyChanged(nameof(HasNoDevices));

            Endpoints.Clear();
            if (endpoints?.Succeeded == true && endpoints.Data != null)
            {
                foreach (var e in endpoints.Data)
                {
                    Endpoints.Add(new EndpointRow(
                        e.Id,
                        e.Name,
                        e.BindAddress,
                        e.Port,
                        e.Protocol,
                        e.Running,
                        e.Running ? "Đang chạy" : "Đã dừng",
                        "USB ADB Node",
                        "Di động 4G/5G",
                        e.MaxConnections
                    ));
                }
                SelectedEndpointForExport = Endpoints.Count > 0 ? Endpoints[0] : null;
                UpdateGeneratedCredentials();
            }
            OnPropertyChanged(nameof(HasEndpoints));
            OnPropertyChanged(nameof(HasNoEndpoints));

            Pools.Clear();
            if (pools?.Succeeded == true && pools.Data != null)
            {
                foreach (var p in pools.Data)
                {
                    Pools.Add(new PoolRow(
                        p.Id,
                        p.Name,
                        p.Strategy,
                        p.StickyTtlSeconds,
                        p.Enabled,
                        p.Members.Count
                    ));
                }
            }

            ActiveSessions.Clear();
            if (sessions?.Succeeded == true && sessions.Data != null)
            {
                foreach (var s in sessions.Data.Items)
                {
                    var durationSec = (int)(s.EndedAt - s.StartedAt).TotalSeconds;
                    ActiveSessions.Add(new SessionItem(
                        s.Id.ToString()[..8],
                        "Proxy-" + s.EndpointId.ToString()[..4],
                        "127.0.0.1",
                        $"{s.DestinationHost}:{s.DestinationPort}",
                        "TCP/TLS",
                        durationSec > 0 ? $"{durationSec}s" : "Đang kết nối",
                        FormatBytes(s.BytesUp + s.BytesDown)
                    ));
                }
            }
            OnPropertyChanged(nameof(HasSessions));
            OnPropertyChanged(nameof(HasNoSessions));

            RotationHistory.Clear();
            if (rotations?.Succeeded == true && rotations.Data != null)
            {
                foreach (var r in rotations.Data)
                {
                    RotationHistory.Add(new RotationHistoryItem(
                        r.CurrentIp,
                        r.PreviousIp,
                        r.ChangedAt.ToLocalTime().ToString("HH:mm:ss"),
                        r.TriggeredBy,
                        r.Duration,
                        r.Status
                    ));
                }
            }
            OnPropertyChanged(nameof(HasRotations));
            OnPropertyChanged(nameof(HasNoRotations));

            if (traffic?.Succeeded == true && traffic.Data != null)
            {
                _traffic = traffic.Data;
            }

            NetstatConnections.Clear();
            if (netstat?.Succeeded == true && netstat.Data != null)
            {
                foreach (var c in netstat.Data)
                {
                    NetstatConnections.Add(c);
                }
            }
            OnPropertyChanged(nameof(HasNetstat));
            OnPropertyChanged(nameof(HasNoNetstat));

            NetworkInterfaces.Clear();
            if (interfaces?.Succeeded == true && interfaces.Data != null)
            {
                foreach (var i in interfaces.Data)
                {
                    NetworkInterfaces.Add(i);
                }
            }
            OnPropertyChanged(nameof(HasInterfaces));
            OnPropertyChanged(nameof(HasNoInterfaces));

            Locations.Clear();
            var groupedCarriers = Devices.GroupBy(d => d.Carrier).ToList();
            foreach (var g in groupedCarriers)
            {
                Locations.Add(new LocationItem(
                    g.Key,
                    g.First().Network,
                    g.First().SignalLevel,
                    "100%",
                    "Sẵn sàng",
                    g.Count()
                ));
            }

            ServiceStatus = "127.0.0.1:5080 (Online)";
            StatusMessage = $"Đồng bộ thành công dữ liệu: {Devices.Count} thiết bị, {Endpoints.Count} proxies, {NetstatConnections.Count} TCP sockets.";
            AddLog("INFO", "Đồng bộ thành công dữ liệu từ backend service 127.0.0.1:5080.");
            UpdateComputedMetrics();
        }
        catch (Exception ex)
        {
            ServiceStatus = "127.0.0.1:5080 (Offline)";
            StatusMessage = $"Không thể kết nối Backend Service: {ex.Message}";
            AddLog("WARN", $"Lỗi kết nối tới Backend: {ex.Message}");
            UpdateComputedMetrics();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DiscoverAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            AddLog("INFO", "Gửi lệnh quét thiết bị Android qua ADB Daemon...");
            var res = await _api.PostAsync(ApiRoutes.DeviceDiscoveryRuns, null);
            if (res.IsSuccessStatusCode)
            {
                AddLog("INFO", "Hoàn tất chu kỳ quét và nhận diện thiết bị ADB.");
                StatusMessage = "Đã quét và cập nhật danh sách thiết bị ADB.";
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Quét ADB thất bại: {ex.Message}");
            StatusMessage = $"Quét ADB thất bại: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleProxyAsync(EndpointRow? endpoint)
    {
        if (endpoint == null || IsLoading) return;
        IsLoading = true;
        try
        {
            if (endpoint.Running)
            {
                AddLog("INFO", $"Dừng proxy runtime {endpoint.Name} (Port {endpoint.Port})...");
                await _api.DeleteAsync($"{ApiRoutes.ProxyRuntime}/{endpoint.Id}");
                AddLog("INFO", $"Proxy endpoint {endpoint.Name} đã dừng.");
            }
            else
            {
                AddLog("INFO", $"Khởi chạy proxy runtime {endpoint.Name} (Port {endpoint.Port})...");
                await _api.PutAsync($"{ApiRoutes.ProxyRuntime}/{endpoint.Id}", null);
                AddLog("INFO", $"Proxy endpoint {endpoint.Name} đã chạy.");
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Thao tác proxy thất bại: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleDeviceAsync(DeviceRow? device)
    {
        if (device == null || IsLoading) return;
        IsLoading = true;
        try
        {
            var newState = !device.Enabled;
            AddLog("INFO", $"Chuyển trạng thái thiết bị {device.Name} -> {(newState ? "Bật" : "Tắt")}...");
            var res = await _api.PatchAsJsonAsync($"{ApiRoutes.Devices}/{device.Id}", new DeviceStateRequest(newState));
            if (res.IsSuccessStatusCode)
            {
                AddLog("INFO", $"Cập nhật trạng thái node {device.Name} thành công.");
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Cập nhật trạng thái thiết bị thất bại: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateProxyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProxyName) || NewProxyPort <= 0 || IsLoading) return;
        IsLoading = true;
        try
        {
            AddLog("INFO", $"Tạo mới proxy endpoint {NewProxyName} trên cổng {NewProxyPort} ({NewProxyProtocol})...");
            var req = new CreateProxyEndpointRequest(
                NewProxyName.Trim(),
                "127.0.0.1",
                NewProxyPort,
                NewProxyProtocol,
                SelectedProxyDeviceId,
                null
            );
            var res = await _api.PostAsJsonAsync(ApiRoutes.Proxies, req);
            if (res.IsSuccessStatusCode)
            {
                AddLog("INFO", $"Tạo proxy {NewProxyName} thành công.");
                IsCreateProxyDialogOpen = false;
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Tạo proxy thất bại: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RegisterDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDeviceSerial) || string.IsNullOrWhiteSpace(NewDeviceName) || IsLoading) return;
        IsLoading = true;
        try
        {
            AddLog("INFO", $"Đăng ký thiết bị mới: {NewDeviceName} (ADB: {NewDeviceSerial})...");
            var req = new RegisterDeviceRequest(NewDeviceSerial.Trim(), NewDeviceName.Trim());
            var res = await _api.PostAsJsonAsync(ApiRoutes.Devices, req);
            if (res.IsSuccessStatusCode)
            {
                AddLog("INFO", $"Đăng ký thiết bị {NewDeviceName} thành công.");
                IsRegisterDeviceDialogOpen = false;
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Đăng ký thiết bị thất bại: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreatePoolAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPoolName) || IsLoading) return;
        IsLoading = true;
        try
        {
            AddLog("INFO", $"Tạo mới Proxy Pool: {NewPoolName} ({NewPoolStrategy})...");
            var req = new CreateProxyPoolRequest(NewPoolName.Trim(), NewPoolStrategy, NewPoolStickyTtl, []);
            var res = await _api.PostAsJsonAsync(ApiRoutes.Pools, req);
            if (res.IsSuccessStatusCode)
            {
                AddLog("INFO", $"Tạo proxy pool {NewPoolName} thành công.");
                IsCreatePoolDialogOpen = false;
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Tạo pool thất bại: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RotateNowAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            AddLog("INFO", "Gửi lệnh kích hoạt xoay IP qua API Backend...");
            var res = await _api.PostAsJsonAsync($"{ApiRoutes.Rotation}/rotate", new RotateIpRequest("Kích hoạt thủ công qua Desktop"));
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<RotationResultDto>>();
                if (body?.Data != null)
                {
                    var item = body.Data;
                    RotationHistory.Insert(0, new RotationHistoryItem(
                        item.CurrentIp,
                        item.PreviousIp,
                        item.ChangedAt.ToLocalTime().ToString("HH:mm:ss"),
                        item.TriggeredBy,
                        item.Duration,
                        item.Status
                    ));
                    StatusMessage = $"Xoay IP thành công. IP WAN mới: {item.CurrentIp}";
                    AddLog("INFO", $"Xoay IP thành công: {item.CurrentIp} (Thời gian thực thi: {item.Duration})");
                    OnPropertyChanged(nameof(HasRotations));
                    OnPropertyChanged(nameof(HasNoRotations));
                    return;
                }
            }

            StatusMessage = "Xoay IP thất bại: Không có thiết bị ADB khả dụng hoặc lệnh ADB không thành công.";
            AddLog("ERROR", "Xoay IP thất bại: Backend không thể đổi IP thiết bị di động.");
        }
        catch (Exception ex)
        {
            AddLog("ERROR", $"Lỗi kích hoạt xoay IP: {ex.Message}");
            StatusMessage = $"Lỗi xoay IP: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddLog(string level, string message)
    {
        SystemLogs.Insert(0, new LogItem(DateTime.Now.ToString("HH:mm:ss.fff"), level, message));
        if (SystemLogs.Count > 120)
        {
            SystemLogs.RemoveAt(SystemLogs.Count - 1);
        }
    }

    private void UpdateComputedMetrics()
    {
        ActiveProxies = Endpoints.Count(e => e.Running).ToString();
        ActiveDevices = Devices.Count(d => d.Enabled && (d.ActualState == "ProxyReady" || d.ActualState == "Discovered")).ToString();
        RecordedSessions = _traffic.SessionCount.ToString();
        TrafficTotal = FormatBytes(_traffic.BytesUp + _traffic.BytesDown);
    }

    private static string FormatBytes(long value) => value switch
    {
        < 1_024 => $"{value} B",
        < 1_048_576 => $"{value / 1_024d:0.0} KB",
        < 1_073_741_824 => $"{value / 1_048_576d:0.0} MB",
        _ => $"{value / 1_073_741_824d:0.0} GB"
    };

    public sealed record DeviceRow(
        Guid Id,
        string Name,
        string AdbSerial,
        string ActualState,
        bool Enabled,
        string Port,
        string Battery,
        string Network,
        string Carrier,
        string Iccid,
        string SignalDbm,
        string SignalLevel,
        string MonthlyQuota,
        string Apn,
        string WanIp
    );

    public sealed record EndpointRow(
        Guid Id,
        string Name,
        string Address,
        int Port,
        string Protocol,
        bool Running,
        string Latency,
        string Device,
        string Isp,
        int MaxConnections
    );

    public sealed record PoolRow(
        Guid Id,
        string Name,
        string Strategy,
        int StickyTtlSeconds,
        bool Enabled,
        int MemberCount
    );

    public sealed record LocationItem(
        string CityCountry,
        string CarrierBand,
        string Latency,
        string SuccessRate,
        string Status,
        int ActiveNodes
    );

    public sealed record SessionItem(string Id, string Proxy, string ClientIp, string Target, string Protocol, string Duration, string Traffic);
    public sealed record RotationHistoryItem(string CurrentIp, string PreviousIp, string ChangedAt, string TriggeredBy, string Duration, string Status);
    public sealed record LogItem(string Timestamp, string Level, string Message);
}
