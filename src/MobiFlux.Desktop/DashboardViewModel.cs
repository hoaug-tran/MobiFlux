using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Microsoft.AspNetCore.SignalR.Client;
using MobiFlux.Desktop.Configuration;
using MobiFlux.Desktop.Services;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Desktop;

public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    private HttpClient _api;
    private HubConnection? _hub;
    private bool _connectingHub;
    private bool _refreshingConfiguration;
    private long _configurationGeneration;
    private DateTimeOffset? _lastLiveSample;
    private bool _liveConnected;
    private int _readyDevices, _runningProxies;
    private readonly DispatcherTimer _refreshTimer;
    private readonly CancellationTokenSource _lifetime = new();
    private Guid? _editingEndpointId;
    [ObservableProperty] private string currentTab = "Dashboard";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanUsePage))] private bool isBusy;
    [ObservableProperty] private bool serviceOnline;
    [ObservableProperty] private string statusMessage = "Đang kết nối dịch vụ…";
    [ObservableProperty] private string lastSync = "Chưa đồng bộ";
    [ObservableProperty] private string apiAddress;
    [ObservableProperty] private string advertisedAddress = "";
    [ObservableProperty] private bool autoRefresh = true;
    [ObservableProperty] private string serviceExecutable = "";
    [ObservableProperty] private DeviceDto? selectedDevice;
    [ObservableProperty] private ProxyEndpointDto? selectedEndpoint;
    [ObservableProperty] private ProxyPoolDto? selectedPool;
    [ObservableProperty] private string endpointName = "";
    [ObservableProperty] private string endpointLimit = "";
    [ObservableProperty] private bool endpointEnabled = true;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanUsePage))] private bool proxyDialogOpen;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanUsePage))] private bool deviceDialogOpen;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanUsePage))] private bool poolDialogOpen;
    [ObservableProperty] private string newProxyName = "";
    [ObservableProperty] private string newBindAddress = "127.0.0.1";
    [ObservableProperty] private string newProxyPort = "";
    [ObservableProperty] private string newProxyProtocol = "Socks5";
    [ObservableProperty] private Guid? targetDeviceId;
    [ObservableProperty] private Guid? targetPoolId;
    [ObservableProperty] private bool usePool;
    [ObservableProperty] private string proxyUsername = "";
    [ObservableProperty] private string proxyPassword = "";
    [ObservableProperty] private string allowedClients = "";
    [ObservableProperty] private string newDeviceName = "";
    [ObservableProperty] private string newDeviceSerial = "";
    [ObservableProperty] private string newPoolName = "";
    [ObservableProperty] private string routingStrategy = "RoundRobin";
    [ObservableProperty] private string stickyTtl = "300";
    [ObservableProperty] private bool poolEnabled = true;
    [ObservableProperty] private string probeResult = "Chưa kiểm tra";
    [ObservableProperty] private string pingHost = "1.1.1.1";
    [ObservableProperty] private string pingResult = "Chưa kiểm tra";
    [ObservableProperty] private string trafficTotal = "—";
    [ObservableProperty] private string recordedSessions = "—";
    [ObservableProperty] private string liveStatus = "Đang kết nối";
    [ObservableProperty] private string activeConnections = "—";
    [ObservableProperty] private string uploadSpeed = "—";
    [ObservableProperty] private string downloadSpeed = "—";
    [ObservableProperty] private string liveTrafficTotal = "—";
    [ObservableProperty] private string liveSessionCount = "—";
    [ObservableProperty] private string liveFailedSessions = "—";
    [ObservableProperty] private IReadOnlyList<TrafficSampleDto> trafficSamples = Array.Empty<TrafficSampleDto>();
    [ObservableProperty] private string bytesUp = "0 B";
    [ObservableProperty] private string bytesDown = "0 B";
    [ObservableProperty] private string sessionPageLabel = "Trang 1";
    [ObservableProperty] private Guid? sessionEndpointId;
    [ObservableProperty] private Guid? sessionDeviceId;
    private int _sessionPage = 1;
    private int _sessionTotal;
    public string[] Protocols { get; } = ["Socks5", "HttpConnect"];
    public string[] Strategies { get; } = ["RoundRobin", "LeastConnections", "WeightedRoundRobin", "Random"];
    public NavigationItem[] Navigation { get; } = [new("Dashboard", "Tổng quan", "Grid24"), new("Proxies", "Proxy", "Server24"), new("Devices", "Điện thoại", "Phone24"), new("Routing", "Pool và định tuyến", "Branch24"), new("Rotation", "Đổi IP", "ArrowSync24"), new("Sessions", "Lịch sử kết nối", "History24"), new("Networking", "Mạng Windows", "Globe24"), new("Health", "Chẩn đoán", "HeartPulse24"), new("Logs", "Nhật ký", "DocumentText24")];
    public ObservableCollection<DeviceDto> Devices { get; } = [];
    public ObservableCollection<ProxyEndpointDto> Endpoints { get; } = [];
    public ObservableCollection<ProxyPoolDto> Pools { get; } = [];
    public ObservableCollection<ProxySessionDto> Sessions { get; } = [];
    public ObservableCollection<RotationResultDto> Rotations { get; } = [];
    public ObservableCollection<NetworkInterfaceDto> Interfaces { get; } = [];
    public ObservableCollection<NetstatConnectionDto> Connections { get; } = [];
    public ObservableCollection<LogItem> Logs { get; } = [];
    public ObservableCollection<MemberChoice> Members { get; } = [];
    public bool CanUsePage => !IsBusy && !ProxyDialogOpen && !DeviceDialogOpen && !PoolDialogOpen;
    public bool HasEndpoint => SelectedEndpoint is not null;
    public bool HasDevice => SelectedDevice is not null;
    public string ActiveProxies => _liveConnected ? _runningProxies.ToString() : "—";
    public string ActiveDevices => _liveConnected ? _readyDevices.ToString() : "—";
    public string ServiceStatus => ServiceOnline ? "Dịch vụ đang kết nối" : "Không kết nối được dịch vụ";
    public string CurrentTitle => Navigation.FirstOrDefault(item => item.Key == CurrentTab)?.Title ?? "Cài đặt";
    public string DialogTitle => _editingEndpointId is null ? "Tạo proxy" : "Sửa proxy";
    public bool IsCreatingEndpoint => _editingEndpointId is null;
    public string ConnectionString
    {
        get
        {
            if (SelectedEndpoint is not { } endpoint) return "Chưa chọn endpoint";
            var host = endpoint.BindAddress is "0.0.0.0" or "::" ? AdvertisedAddress.Trim() : endpoint.BindAddress;
            if (string.IsNullOrWhiteSpace(host)) return "Nhập IP LAN của Windows trong Cài đặt.";
            if (host.Contains(':') && !host.StartsWith('[')) host = $"[{host}]";
            var scheme = endpoint.Protocol == "Socks5" ? "socks5" : "http";
            var auth = string.IsNullOrEmpty(endpoint.Username) ? "" : Uri.EscapeDataString(endpoint.Username) + "@";
            return $"{scheme}://{auth}{host}:{endpoint.Port}";
        }
    }
    public DashboardViewModel()
    {
        var settings = DesktopConfiguration.LoadPreferences();
        ApiAddress = settings.ApiAddress ?? DesktopConfiguration.Load().BaseAddress.ToString();
        AdvertisedAddress = settings.AdvertisedAddress ?? "";
        AutoRefresh = settings.AutoRefresh;
        ServiceExecutable = settings.ServiceExecutable ?? FindServiceExecutable();
        _api = CreateClient(ApiAddress);
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _refreshTimer.Tick += OnRefreshTick;
        _refreshTimer.Start();
    }
    private async void OnRefreshTick(object? sender, EventArgs e)
    {
        if (AutoRefresh && !IsBusy && !_refreshingConfiguration && !ProxyDialogOpen && !DeviceDialogOpen && !PoolDialogOpen && CurrentTab is not ("Routing" or "Settings" or "Rotation"))
        {
            _refreshingConfiguration = true;
            try { await RefreshCoreAsync(background: true); _lastBackgroundError = null; }
            catch (Exception exception) { var message = UserMessage(exception); StatusMessage = message; if (_lastBackgroundError != message) { Notify(message, true); Log("ERROR", $"{exception.GetType().Name}: {UserMessage(exception)}"); _lastBackgroundError = message; } }
            finally { _refreshingConfiguration = false; }
        }
        if (!_connectingHub && _hub?.State is null or HubConnectionState.Disconnected) await ConnectLiveAsync();
    }
    partial void OnCurrentTabChanged(string value) => OnPropertyChanged(nameof(CurrentTitle));
    partial void OnServiceOnlineChanged(bool value) { OnPropertyChanged(nameof(ServiceStatus)); OnPropertyChanged(nameof(ActiveProxies)); OnPropertyChanged(nameof(ActiveDevices)); }
    partial void OnAdvertisedAddressChanged(string value) => OnPropertyChanged(nameof(ConnectionString));
    partial void OnSelectedEndpointChanged(ProxyEndpointDto? value)
    {
        EndpointName = value?.Name ?? ""; EndpointLimit = (value?.MaxConnections ?? 64).ToString(); EndpointEnabled = value?.Enabled ?? true;
        OnPropertyChanged(nameof(ConnectionString));
        ToggleProxyCommand.NotifyCanExecuteChanged(); EditProxyCommand.NotifyCanExecuteChanged(); DeleteProxyCommand.NotifyCanExecuteChanged(); TestProxyCommand.NotifyCanExecuteChanged(); CopyConnectionCommand.NotifyCanExecuteChanged();
    }
    partial void OnSelectedDeviceChanged(DeviceDto? value) { ToggleDeviceCommand.NotifyCanExecuteChanged(); RotateCommand.NotifyCanExecuteChanged(); }
    partial void OnSelectedPoolChanged(ProxyPoolDto? value)
    {
        RoutingStrategy = value?.Strategy ?? "RoundRobin"; StickyTtl = (value?.StickyTtlSeconds ?? 300).ToString(); PoolEnabled = value?.Enabled ?? true;
        foreach (var member in Members)
        {
            var saved = value?.Members.FirstOrDefault(item => item.DeviceId == member.Id);
            member.Selected = saved is not null; member.Weight = (saved?.Weight ?? 1).ToString(); member.Priority = (saved?.Priority ?? 0).ToString();
        }
    }
    private static HttpClient CreateClient(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != "http") throw new ArgumentException("API điều khiển phải là HTTP loopback trên máy Windows.");
        return new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(100) };
    }
    [ObservableProperty] private string notificationMessage = "";
    [ObservableProperty] private string notificationTitle = "";
    [ObservableProperty] private bool notificationOpen;
    private string? _lastBackgroundError;
    private void Notify(string message, bool error = false)
    {
        NotificationTitle = error ? "Chưa hoàn tất thao tác" : "Kết quả thao tác";
        NotificationMessage = message;
        NotificationOpen = true;
    }
    [RelayCommand] private void DismissNotification() => NotificationOpen = false;
    private void Log(string level, string message)
    {
        Logs.Insert(0, new(DateTimeOffset.Now.ToString("HH:mm:ss"), level, message));
        while (Logs.Count > 500) Logs.RemoveAt(Logs.Count - 1);
    }
    private async Task ExecuteAsync(Func<Task> action, string? success = null)
    {
        if (IsBusy) return;
        IsBusy = true;
        Interlocked.Increment(ref _configurationGeneration);
        var previousMessage = StatusMessage;
        try { await action(); if (success is not null) { StatusMessage = success; Log("INFO", success); } if (StatusMessage != previousMessage) Notify(StatusMessage); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception exception) { StatusMessage = UserMessage(exception); Notify(StatusMessage, true); Log("ERROR", $"{exception.GetType().Name}: {UserMessage(exception)}"); }
        finally { IsBusy = false; }
    }
    private async Task<T> ReadAsync<T>(string route)
    {
        using var response = await _api.GetAsync(route, _lifetime.Token);
        await EnsureAsync(response);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_lifetime.Token);
        return body is { Succeeded: true, Data: not null } ? body.Data : throw new IOException(body?.Error?.Message ?? "Dữ liệu API không hợp lệ.");
    }
    private static async Task EnsureAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        ApiResponse<object>? body = null;
        try { body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(); }
        catch (JsonException) { }
        catch (NotSupportedException) { }
        var details = body?.Error?.ValidationErrors is { } errors ? string.Join("; ", errors.Values.SelectMany(value => value)) : body?.Error?.Message;
        throw new IOException($"{details ?? "Dịch vụ chưa xử lý được yêu cầu. Thử lại sau."} Mã tra cứu: {body?.CorrelationId ?? response.StatusCode.ToString()}");
    }
    private async Task SendAsync(HttpMethod method, string route, object? body = null)
    {
        using var request = new HttpRequestMessage(method, route) { Content = body is null ? null : JsonContent.Create(body) };
        using var response = await _api.SendAsync(request, _lifetime.Token);
        await EnsureAsync(response);
    }
    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items) { var next = items.ToArray(); if (collection.SequenceEqual(next)) return; collection.Clear(); foreach (var item in next) collection.Add(item); }
    [RelayCommand] private void Navigate(string route) => CurrentTab = route;
    [RelayCommand] private async Task RefreshAsync() { await ExecuteAsync(() => RefreshCoreAsync()); await ConnectLiveAsync(); }
    private async Task ConnectLiveAsync()
    {
        if (_connectingHub || _lifetime.IsCancellationRequested || _hub?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting) return;
        _connectingHub = true;
        try
        {
            if (_hub is not null) await _hub.DisposeAsync();
            _hub = new HubConnectionBuilder().WithUrl(new Uri(_api.BaseAddress!, "hubs/runtime")).WithAutomaticReconnect().Build();
            _hub.On<RuntimeSnapshotDto>("RuntimeUpdated", snapshot =>
            {
                if (!_lifetime.IsCancellationRequested) _ = Application.Current.Dispatcher.InvokeAsync(() => ApplyRuntime(snapshot));
            });
            _hub.Reconnecting += exception => { _ = Application.Current.Dispatcher.InvokeAsync(() => MarkLiveDisconnected("Đang kết nối lại")); return Task.CompletedTask; };
            _hub.Closed += exception => { _ = Application.Current.Dispatcher.InvokeAsync(() => MarkLiveDisconnected("Đã ngắt kết nối")); return Task.CompletedTask; };
            _hub.Reconnected += connectionId => { _ = Application.Current.Dispatcher.InvokeAsync(() => { TrafficSamples = Array.Empty<TrafficSampleDto>(); _lastLiveSample = null; }); return Task.CompletedTask; };
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token); timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await _hub.StartAsync(timeout.Token);
            LiveStatus = "Đang chờ dữ liệu";
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException) { LiveStatus = "Chưa kết nối"; }
        finally { _connectingHub = false; }
    }
    private void ApplyRuntime(RuntimeSnapshotDto snapshot)
    {
        if (_lifetime.IsCancellationRequested) return;
        if (_lastLiveSample is { } last && snapshot.At <= last) return;
        _lastLiveSample = snapshot.At;
        _liveConnected = true; _readyDevices = snapshot.ReadyDevices; _runningProxies = snapshot.RunningProxies;
        OnPropertyChanged(nameof(ActiveProxies)); OnPropertyChanged(nameof(ActiveDevices));
        ActiveConnections = snapshot.ActiveConnections.ToString();
        UploadSpeed = $"{snapshot.Traffic.UploadMbps:0.###} Mbps"; DownloadSpeed = $"{snapshot.Traffic.DownloadMbps:0.###} Mbps";
        LiveTrafficTotal = FormatBytes(snapshot.BytesUp + snapshot.BytesDown); LiveSessionCount = snapshot.OpenedSessions.ToString(); LiveFailedSessions = snapshot.FailedSessions.ToString();
        TrafficSamples = TrafficSamples.Append(snapshot.Traffic).TakeLast(snapshot.HistorySamples).ToArray();
        LiveStatus = $"Trực tiếp · {snapshot.At.ToLocalTime():HH:mm:ss}";
    }
    private void MarkLiveDisconnected(string message)
    {
        if (_lifetime.IsCancellationRequested) return;
        LiveStatus = message; ActiveConnections = "—"; UploadSpeed = "—"; DownloadSpeed = "—";
        _liveConnected = false; OnPropertyChanged(nameof(ActiveProxies)); OnPropertyChanged(nameof(ActiveDevices));
    }
    private async Task RefreshCoreAsync(bool background = false)
    {
        var generation = Interlocked.Read(ref _configurationGeneration);
        try
        {
            var deviceTask = ReadAsync<List<DeviceDto>>(ApiRoutes.Devices);
            var endpointTask = ReadAsync<List<ProxyEndpointDto>>(ApiRoutes.Proxies);
            var poolTask = ReadAsync<List<ProxyPoolDto>>(ApiRoutes.Pools);
            var trafficTask = ReadAsync<ProxyTrafficSummaryDto>(ApiRoutes.ProxyTraffic);
            var sessionsTask = ReadAsync<PagedResult<ProxySessionDto>>($"{ApiRoutes.ProxySessions}?page={_sessionPage}&pageSize=50&endpointId={SessionEndpointId}&deviceId={SessionDeviceId}");
            var rotationsTask = ReadAsync<List<RotationResultDto>>($"{ApiRoutes.Rotation}/history");
            await Task.WhenAll(deviceTask, endpointTask, poolTask, trafficTask, sessionsTask, rotationsTask);
            if (background && (IsBusy || generation != Interlocked.Read(ref _configurationGeneration) || ProxyDialogOpen || DeviceDialogOpen || PoolDialogOpen)) return;
            var deviceId = SelectedDevice?.Id; var endpointId = SelectedEndpoint?.Id; var poolId = SelectedPool?.Id;
            Replace(Devices, await deviceTask); Replace(Endpoints, await endpointTask); Replace(Pools, await poolTask);
            Replace(Members, Devices.Select(device => new MemberChoice(device.Id, device.Name)));
            SelectedDevice = Devices.FirstOrDefault(device => device.Id == deviceId) ?? Devices.FirstOrDefault();
            SelectedEndpoint = Endpoints.FirstOrDefault(endpoint => endpoint.Id == endpointId) ?? Endpoints.FirstOrDefault();
            SelectedPool = Pools.FirstOrDefault(pool => pool.Id == poolId) ?? Pools.FirstOrDefault();
            var traffic = await trafficTask;
            TrafficTotal = FormatBytes(traffic.BytesUp + traffic.BytesDown); BytesUp = FormatBytes(traffic.BytesUp); BytesDown = FormatBytes(traffic.BytesDown); RecordedSessions = traffic.SessionCount.ToString();
            var page = await sessionsTask; Replace(Sessions, page.Items); _sessionTotal = page.TotalCount;
            SessionPageLabel = $"Trang {_sessionPage} / {Math.Max(1, (int)Math.Ceiling(_sessionTotal / 50d))} · {_sessionTotal} phiên đã đóng";
            Replace(Rotations, await rotationsTask);
            ServiceOnline = true; LastSync = DateTime.Now.ToString("HH:mm:ss");
            OnPropertyChanged(nameof(ActiveProxies)); OnPropertyChanged(nameof(ActiveDevices)); OnPropertyChanged(nameof(ConnectionString));
            if (!background) StatusMessage = $"Đã cập nhật lúc {LastSync}.";
        }
        catch { ServiceOnline = false; throw; }
    }
    [RelayCommand] private Task DiscoverAsync() => ExecuteAsync(async () => { await SendAsync(HttpMethod.Post, ApiRoutes.DeviceDiscoveryRuns); await RefreshCoreAsync(); }, "Đã quét ADB và kiểm tra agent cellular.");
    [RelayCommand(CanExecute = nameof(HasEndpoint))] private Task ToggleProxyAsync() => ExecuteAsync(async () =>
    {
        var endpoint = SelectedEndpoint ?? throw new InvalidOperationException("Chọn proxy cần bật/tắt.");
        await SendAsync(endpoint.Running ? HttpMethod.Delete : HttpMethod.Put, $"{ApiRoutes.ProxyRuntime}/{endpoint.Id}");
        await RefreshCoreAsync();
    }, "Đã cập nhật trạng thái proxy.");
    [RelayCommand(CanExecute = nameof(HasDevice))] private Task ToggleDeviceAsync() => ExecuteAsync(async () =>
    {
        var device = SelectedDevice ?? throw new InvalidOperationException("Chọn điện thoại cần bật/tắt.");
        await SendAsync(HttpMethod.Patch, $"{ApiRoutes.Devices}/{device.Id}", new DeviceStateRequest(!device.Enabled));
        await RefreshCoreAsync();
    }, "Đã cập nhật điện thoại.");
    [RelayCommand] private void OpenProxy()
    {
        _editingEndpointId = null; NewProxyName = $"Proxy-{Endpoints.Count + 1}";
        NewProxyPort = Enumerable.Range(1080, 20000).First(port => Endpoints.All(endpoint => endpoint.Port != port)).ToString();
        TargetDeviceId = SelectedDevice?.Id; TargetPoolId = SelectedPool?.Id; ProxyUsername = ""; ProxyPassword = "";
        OnPropertyChanged(nameof(DialogTitle)); OnPropertyChanged(nameof(IsCreatingEndpoint)); ProxyDialogOpen = true;
    }
    [RelayCommand(CanExecute = nameof(HasEndpoint))] private void EditProxy()
    {
        if (SelectedEndpoint is not { } endpoint) { StatusMessage = "Chọn proxy cần chỉnh sửa."; return; }
        _editingEndpointId = endpoint.Id; NewProxyName = endpoint.Name; EndpointLimit = endpoint.MaxConnections.ToString(); EndpointEnabled = endpoint.Enabled;
        OnPropertyChanged(nameof(DialogTitle)); OnPropertyChanged(nameof(IsCreatingEndpoint)); ProxyDialogOpen = true;
    }
    [RelayCommand] private void CloseDialogs() { ProxyDialogOpen = false; DeviceDialogOpen = false; PoolDialogOpen = false; ProxyPassword = ""; }
    [RelayCommand] private Task SaveProxyAsync() => ExecuteAsync(async () =>
    {
        if (_editingEndpointId is { } id) await SendAsync(HttpMethod.Put, $"{ApiRoutes.Proxies}/{id}", new UpdateProxyEndpointRequest(NewProxyName, ReadNumber(EndpointLimit, "Giới hạn kết nối", 1, 65535), EndpointEnabled));
        else await SendAsync(HttpMethod.Post, ApiRoutes.Proxies, new CreateProxyEndpointRequest(NewProxyName.Trim(), NewBindAddress.Trim(), ReadNumber(NewProxyPort, "Cổng proxy", 1, 65535), NewProxyProtocol, UsePool ? null : TargetDeviceId, UsePool ? TargetPoolId : null, string.IsNullOrWhiteSpace(ProxyUsername) ? null : ProxyUsername, string.IsNullOrEmpty(ProxyPassword) ? null : ProxyPassword, AllowedClients));
        CloseDialogs(); await RefreshCoreAsync();
    }, "Đã lưu proxy endpoint.");
    [RelayCommand(CanExecute = nameof(HasEndpoint))] private Task DeleteProxyAsync() => ExecuteAsync(async () =>
    {
        var endpoint = SelectedEndpoint ?? throw new InvalidOperationException("Chọn proxy cần xóa.");
        if (MessageBox.Show($"Dừng và xóa proxy {endpoint.Name}? Lịch sử kết nối được giữ lại.", "Xóa proxy", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await SendAsync(HttpMethod.Delete, $"{ApiRoutes.Proxies}/{endpoint.Id}"); await RefreshCoreAsync();
    });
    [RelayCommand] private void OpenDevice() { NewDeviceName = ""; NewDeviceSerial = ""; DeviceDialogOpen = true; }
    [RelayCommand] private Task SaveDeviceAsync() => ExecuteAsync(async () =>
    {
        await SendAsync(HttpMethod.Post, ApiRoutes.Devices, new RegisterDeviceRequest(NewDeviceSerial.Trim(), NewDeviceName.Trim()));
        CloseDialogs(); await RefreshCoreAsync();
    }, "Đã đăng ký điện thoại; bấm Quét ADB để kiểm tra.");
    [RelayCommand] private Task DeletePoolAsync() => ExecuteAsync(async () =>
    {
        var pool = SelectedPool ?? throw new InvalidOperationException("Chọn pool cần xóa.");
        if (MessageBox.Show($"Xóa pool {pool.Name}? Không thể hoàn tác. Pool đang được proxy sử dụng sẽ được giữ lại.", "Xóa pool", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await SendAsync(HttpMethod.Delete, $"{ApiRoutes.Pools}/{pool.Id}");
        await RefreshCoreAsync();
        StatusMessage = "Đã xóa pool.";
    });    [RelayCommand] private void OpenPool() { NewPoolName = $"Pool-{Pools.Count + 1}"; foreach (var member in Members) member.Selected = false; PoolDialogOpen = true; }
    private ProxyPoolMemberRequest[] SelectedMembers() => Members.Where(member => member.Selected).Select(member => new ProxyPoolMemberRequest(member.Id, ReadNumber(member.Weight, "Trọng số", 1, 10000), ReadNumber(member.Priority, "Ưu tiên", 0, int.MaxValue))).ToArray();
    [RelayCommand] private Task SavePoolAsync() => ExecuteAsync(async () =>
    {
        var members = SelectedMembers(); if (members.Length == 0) throw new InvalidOperationException("Chọn ít nhất một điện thoại cho pool.");
        if (PoolDialogOpen) await SendAsync(HttpMethod.Post, ApiRoutes.Pools, new CreateProxyPoolRequest(NewPoolName.Trim(), RoutingStrategy, ReadNumber(StickyTtl, "Sticky TTL", 0, 86400), members));
        else
        {
            var pool = SelectedPool ?? throw new InvalidOperationException("Chọn pool cần cập nhật.");
            await SendAsync(HttpMethod.Put, $"{ApiRoutes.Pools}/{pool.Id}", new UpdateProxyPoolRequest(RoutingStrategy, ReadNumber(StickyTtl, "Sticky TTL", 0, 86400), PoolEnabled, members));
        }
        CloseDialogs(); await RefreshCoreAsync();
    }, "Đã lưu chiến lược và thành viên pool.");
    [RelayCommand(CanExecute = nameof(HasEndpoint))] private Task TestProxyAsync() => ExecuteAsync(async () =>
    {
        var endpoint = SelectedEndpoint ?? throw new InvalidOperationException("Chọn proxy cần kiểm tra.");
        var credentials = new ProxyProbeCredentials(endpoint.Username, ProxyPassword);
        ProxyPassword = "";
        using var response = await _api.PostAsJsonAsync($"{ApiRoutes.Proxies}/{endpoint.Id}/test", credentials, _lifetime.Token);
        await EnsureAsync(response);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyTestResultDto>>(_lifetime.Token);
        var result = body?.Data ?? throw new IOException("Không nhận được kết quả kiểm tra.");
        ProbeResult = $"{(result.Reachable ? "HTTPS thành công" : "HTTPS thất bại")} · {result.LatencyMs} ms · IP: {(string.IsNullOrEmpty(result.ResolvedWanIp) ? "chưa xác minh" : result.ResolvedWanIp)}\n{result.Status}";
        StatusMessage = ProbeResult; Log(result.Reachable ? "INFO" : "WARN", ProbeResult);
    });
    [RelayCommand(CanExecute = nameof(HasDevice))] private Task RotateAsync() => ExecuteAsync(async () =>
    {
        var device = SelectedDevice ?? throw new InvalidOperationException("Chọn điện thoại cần đổi IP.");
        using var response = await _api.PostAsJsonAsync($"{ApiRoutes.Rotation}/rotate", new RotateIpRequest("Desktop", device.Id), _lifetime.Token);
        await EnsureAsync(response);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RotationResultDto>>(_lifetime.Token);
        await RefreshCoreAsync(); StatusMessage = result?.Data?.Status ?? "Chưa có kết quả đổi IP."; Log("INFO", StatusMessage);
    });
    [RelayCommand] private Task LoadNetworkingAsync() => ExecuteAsync(async () =>
    {
        var interfaces = ReadAsync<List<NetworkInterfaceDto>>($"{ApiRoutes.Networking}/interfaces");
        var connections = ReadAsync<List<NetstatConnectionDto>>($"{ApiRoutes.Networking}/netstat");
        await Task.WhenAll(interfaces, connections); Replace(Interfaces, await interfaces); Replace(Connections, await connections);
    }, "Đã tải giao diện và kết nối mạng Windows.");
    [RelayCommand] private Task PingAsync() => ExecuteAsync(async () =>
    {
        using var response = await _api.PostAsJsonAsync($"{ApiRoutes.Networking}/ping", new PingRequest(PingHost), _lifetime.Token); await EnsureAsync(response);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PingResultDto>>(_lifetime.Token);
        PingResult = body?.Data is { } result ? $"{result.Host} → {result.IpAddress}: {result.Status}, {result.RoundTripTimeMs} ms" : "Không có kết quả ping.";
    });
    [RelayCommand] private Task ApplySessionFiltersAsync() => ExecuteAsync(async () => { _sessionPage = 1; await RefreshCoreAsync(); });
    [RelayCommand] private Task ClearSessionFiltersAsync() => ExecuteAsync(async () => { SessionEndpointId = null; SessionDeviceId = null; _sessionPage = 1; await RefreshCoreAsync(); });
    [RelayCommand] private Task NextSessionsAsync() => ExecuteAsync(async () => { if (_sessionPage * 50 < _sessionTotal) _sessionPage++; await RefreshCoreAsync(); });
    [RelayCommand] private Task PreviousSessionsAsync() => ExecuteAsync(async () => { _sessionPage = Math.Max(1, _sessionPage - 1); await RefreshCoreAsync(); });
    [RelayCommand] private Task SaveSettingsAsync() => ExecuteAsync(async () =>
    {
        var client = CreateClient(ApiAddress);
        try { DesktopConfiguration.SavePreferences(new(ApiAddress, AdvertisedAddress, AutoRefresh, ServiceExecutable, ThemeManager.CurrentTheme.ToString())); }
        catch { client.Dispose(); throw; }
        if (_hub is not null) { await _hub.DisposeAsync(); _hub = null; }
        _api.Dispose(); _api = client;
        await RefreshCoreAsync();
        TrafficSamples = Array.Empty<TrafficSampleDto>(); _lastLiveSample = null;
        await ConnectLiveAsync();
    }, "Đã lưu cài đặt trên máy này.");
    [RelayCommand] private void ToggleTheme()
    {
        ThemeManager.ToggleTheme();
        try { DesktopConfiguration.SavePreferences(DesktopConfiguration.LoadPreferences() with { Theme = ThemeManager.CurrentTheme.ToString() }); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { StatusMessage = $"Đã đổi theme; không lưu được lựa chọn: {exception.Message}"; }
    }
    private static string FindServiceExecutable()
    {
        var packaged = Path.Combine(AppContext.BaseDirectory, "MobiFlux.Service.exe");
        if (File.Exists(packaged)) return packaged;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (!File.Exists(Path.Combine(directory.FullName, "MobiFlux.sln"))) continue;
            var binaries = Path.Combine(directory.FullName, "src", "MobiFlux.Service", "bin");
            return Directory.Exists(binaries) ? Directory.EnumerateFiles(binaries, "MobiFlux.Service.exe", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() ?? "" : "";
        }
        return "";
    }
    [RelayCommand] private void BrowseService()
    {
        var dialog = new OpenFileDialog { Filter = "MobiFlux Service (MobiFlux.Service.exe)|MobiFlux.Service.exe" };
        if (dialog.ShowDialog() == true) ServiceExecutable = dialog.FileName;
    }
    [RelayCommand] private Task StartServiceAsync() => ExecuteAsync(async () =>
    {
        if (ServiceOnline) { StatusMessage = "Service đã kết nối."; return; }
        if (!File.Exists(ServiceExecutable) || !string.Equals(Path.GetFileName(ServiceExecutable), "MobiFlux.Service.exe", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Chọn MobiFlux.Service.exe đã build hoặc đóng gói.");
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MobiFlux", "service");
        Directory.CreateDirectory(dataDirectory);
        var start = new ProcessStartInfo(Path.GetFullPath(ServiceExecutable)) { WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(ServiceExecutable))!, UseShellExecute = false, CreateNoWindow = true };
        start.Environment["MOBIFLUX_urls"] = _api.BaseAddress!.GetLeftPart(UriPartial.Authority);
        start.Environment["MOBIFLUX_Storage__DatabasePath"] = Path.Combine(dataDirectory, "mobiflux.db");
        using var process = Process.Start(start) ?? throw new IOException("Không khởi chạy được service.");
        for (var attempt = 0; attempt < 15; attempt++)
        {
            await Task.Delay(500, _lifetime.Token);
            try { await RefreshCoreAsync(); Log("INFO", "Đã khởi chạy MobiFlux Service."); return; }
            catch (HttpRequestException) { }
            if (process.HasExited) throw new IOException("Service đã dừng khi khởi động. Kiểm tra logs trong thư mục service.");
        }
        throw new IOException("Service chưa sẵn sàng; kiểm tra cổng API và nhật ký.");
    });
    [RelayCommand] private void ToggleLanguage() => LanguageManager.ToggleLanguage();
    [RelayCommand(CanExecute = nameof(HasEndpoint))] private void CopyConnection() { if (SelectedEndpoint is null) { StatusMessage = "Chọn endpoint trước khi sao chép."; return; } try { Clipboard.SetText(ConnectionString); StatusMessage = "Đã sao chép địa chỉ; nhập mật khẩu riêng trên thiết bị client nếu được yêu cầu."; } catch (Exception exception) { StatusMessage = UserMessage(exception); } }
    [RelayCommand] private void ClearLogs() => Logs.Clear();
    [RelayCommand] private void ExportLogs()
    {
        var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = $"mobiflux-log-{DateTime.Now:yyyyMMdd-HHmmss}.json" };
        if (dialog.ShowDialog() != true) return;
        try { System.IO.File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(Logs, new JsonSerializerOptions { WriteIndented = true })); StatusMessage = "Đã xuất nhật ký thao tác."; } catch (Exception exception) { StatusMessage = UserMessage(exception); }
    }
    private static string UserMessage(Exception exception) => exception switch
    {
        HttpRequestException => "Không kết nối được MobiFlux Service. Kiểm tra dịch vụ và địa chỉ API rồi thử lại.",
        OperationCanceledException => "Dịch vụ phản hồi quá lâu. Kiểm tra kết nối rồi thử lại.",
        JsonException => "Dữ liệu dịch vụ không hợp lệ. Kiểm tra phiên bản ứng dụng và service.",
        UnauthorizedAccessException => "Không có quyền truy cập tệp hoặc thư mục. Chọn vị trí khác rồi thử lại.",
        ArgumentException or InvalidOperationException or IOException => exception.Message,
        _ => "Thao tác chưa hoàn tất. Xem nhật ký để tra cứu lỗi và thử lại."
    };

    private static int ReadNumber(string raw, string field, int minimum, int maximum)
    {
        if (!int.TryParse(raw, out var value) || value < minimum || value > maximum) throw new ArgumentException($"{field} phải là số nguyên từ {minimum} đến {maximum}.");
        return value;
    }
    public static string FormatBytes(long value) => value switch { < 1024 => $"{value} B", < 1048576 => $"{value / 1024d:0.0} KB", < 1073741824 => $"{value / 1048576d:0.0} MB", _ => $"{value / 1073741824d:0.0} GB" };
    public void Dispose() { _refreshTimer.Stop(); _refreshTimer.Tick -= OnRefreshTick; _lifetime.Cancel(); if (_hub is not null) _ = _hub.DisposeAsync(); _api.Dispose(); }
    public sealed record NavigationItem(string Key, string Title, string Icon);
    public sealed record LogItem(string Time, string Level, string Message);
}
public sealed partial class MemberChoice(Guid id, string name) : ObservableObject
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    [ObservableProperty] private bool selected;
    [ObservableProperty] private string weight = "1";
    [ObservableProperty] private string priority = "0";
}
