# Mobile Proxy Management Platform

## 1. Mục tiêu tài liệu

Tài liệu này mô tả kế hoạch kỹ thuật đầy đủ cho một hệ thống Mobile Proxy chạy local trên Windows, sử dụng các điện thoại Android có SIM 4G/5G làm Internet exit node.

Mục tiêu hiện tại là local-first:

- Windows là server trung tâm và là nơi quản lý toàn bộ hệ thống.
- Các điện thoại Android được cắm vào Windows qua USB/ADB.
- Box phone chỉ được xem là phần cứng gom nhiều điện thoại/USB, không giả định box có API hay khả năng điều khiển riêng.
- Người dùng trên chính máy Windows, điện thoại khác trong LAN hoặc router trong nhà có thể sử dụng proxy.
- Traffic của ứng dụng sử dụng proxy phải đi ra Internet qua SIM của Android, không được fallback sang Ethernet/Wi-Fi của Windows.
- Hệ thống phải hỗ trợ nhiều điện thoại, nhiều SIM, nhiều proxy, pool proxy, đổi IP, xoay vòng proxy, sticky session, health check, tự phục hồi, logging, audit và mở rộng về sau.
- Kiến trúc phải đủ sạch để sau này phát triển thành hệ thống nhiều Windows node hoặc dịch vụ proxy từ xa mà không phải viết lại phần lõi.

Tài liệu này không giả định trước loại SIM, loại APN, IPv4 public/private hay IPv6 của từng nhà mạng Nhật. Hệ thống phải tự phát hiện và hoạt động theo capability thực tế.

---

## 2. Phạm vi và định nghĩa

### 2.1. Windows Server

Windows là thành phần quản lý trung tâm của hệ thống, gồm:

- Windows Service chạy nền.
- Local Management API.
- Proxy Gateway.
- ADB Device Manager.
- Device Orchestrator.
- IP Rotation Engine.
- Proxy Pool Router.
- Health Monitor.
- Metrics Collector.
- SQLite database.
- WPF Desktop UI.

Windows có thể chạy proxy listener tại local hoặc LAN, nhưng listener chỉ là điểm vào.

Đường ra bắt buộc phải là:

```text
Client
  |
  v
Windows Proxy Gateway
  |
  v
ADB / Android Agent
  |
  v
Cellular Network
  |
  v
Carrier
  |
  v
Internet
```

Không được:

```text
Client
  |
  v
Windows Proxy Gateway
  |
  v
Windows Ethernet/Wi-Fi
  |
  v
Internet
```

### 2.2. Android Node

Mỗi điện thoại Android là một `DeviceNode`.

Một `DeviceNode` có thể cung cấp:

- SOCKS5 proxy.
- HTTP CONNECT proxy.
- TCP egress.
- Remote DNS qua cellular.
- IPv4 egress nếu mạng hỗ trợ.
- IPv6 egress nếu mạng hỗ trợ.
- Metrics.
- Egress IP detection.
- IP rotation capability nếu thiết bị/OS cho phép.
- Diagnostics.
- Health information.

### 2.3. Box phone

Box hiện tại được coi là:

```text
USB hub + nguồn + chỗ chứa điện thoại
```

Không tạo business logic phụ thuộc vào box.

Nếu cần tổ chức giao diện, dùng `DeviceGroup`, ví dụ:

```text
Box 01
  - Phone 01
  - Phone 02
  - Phone 03
  - ...
  - Phone 10
```

Nếu sau này box/hub có API điều khiển nguồn, có thể thêm implementation của `IPowerController` mà không thay đổi Domain.

---

## 3. Điều hệ thống bảo vệ và điều hệ thống không thể bảo đảm

### 3.1. Điều proxy này làm được

Khi một ứng dụng dùng proxy:

```text
Application
  |
  v
Proxy
  |
  v
Android SIM
  |
  v
Internet
```

dịch vụ đích sẽ nhìn thấy IP egress của mạng mobile thay vì IP Internet của Windows.

Đây là mục tiêu chính của dự án.

Ví dụ:

```text
Windows home IP: 203.x.x.x

Phone 01 mobile egress:
49.x.x.x

Application using Phone 01 proxy:
destination sees 49.x.x.x
```

### 3.2. Điều proxy không tự giải quyết

Proxy IP không đồng nghĩa với ẩn toàn bộ vị trí thiết bị.

Ứng dụng vẫn có thể biết hoặc suy đoán vị trí từ:

- GPS/location permission.
- Wi-Fi scan.
- Bluetooth scan.
- Nearby devices.
- SIM/carrier information.
- Timezone.
- Locale/language.
- Account history.
- Device identifiers.
- Browser/device fingerprint.
- WebRTC/STUN nếu UDP đi ngoài proxy.
- DNS leak.
- IPv6 leak.
- Telemetry riêng của ứng dụng.

Do đó phải phân biệt:

```text
IP location privacy
```

với:

```text
full device location privacy
```

Dự án này bảo đảm đường mạng đi qua mobile proxy. Nó không được tuyên bố rằng proxy một mình có thể làm ứng dụng không biết vị trí GPS thật.

---

## 4. Kết luận về SIM và mạng di động tại Nhật

### 4.1. Không được giả định mỗi SIM có một public IPv4

Thiết kế phải coi các trường hợp sau đều hợp lệ:

```text
Case A
Device private IPv4
Carrier NAT/CGNAT
Public IPv4 egress

Case B
Device global IPv6
NAT64/DNS64/464XLAT
IPv4 destination

Case C
Device dual stack IPv4/IPv6

Case D
Business APN with dynamic global IPv4

Case E
Business APN with fixed global IPv4
```

### 4.2. NTT Docomo

Docomo đã triển khai IPv6 single-stack cho các thiết bị hỗ trợ. Trong mô hình này thiết bị có thể chỉ được cấp IPv6 và khi truy cập đích IPv4, mạng sử dụng NAT64/DNS64 và 464XLAT.

Docomo cũng công bố các dải IP dùng cho Internet access của sp-mode và hỗ trợ APN protocol IPv4/IPv6.

Hệ quả:

- Không được yêu cầu Android phải có local IPv4.
- Không dùng local interface IPv4 để quyết định proxy có hoạt động hay không.
- Phải test egress thực tế cho cả IPv4 và IPv6.
- Android Agent phải hỗ trợ hostname resolution trên cellular network.

### 4.3. Rakuten Mobile

Rakuten Mobile công bố APN `rakuten.jp` với:

```text
APN protocol: IPv4/IPv6
APN roaming protocol: IPv4/IPv6
PDP type: IPv4/IPv6
```

Hệ quả:

- Phải hỗ trợ dual-stack.
- Không được hard-code IPv4-only.

### 4.4. au/KDDI

au có APN khác nhau theo loại kết nối/thiết bị, trong đó tài liệu hỗ trợ hiện tại cho thấy cấu hình 4G và 5G có thể khác nhau.

Hệ thống không được tự thay APN nếu không có cấu hình rõ ràng của người dùng.

APN là metadata/configuration có thể lưu theo Device/SIM, không phải logic hard-code.

### 4.5. SoftBank

Tài liệu thiết bị mobile của SoftBank cho thấy các profile có thể hỗ trợ IPv4/IPv6 và thiết bị có thể hiển thị WAN IPv4/WAN IPv6 tùy APN.

Không suy diễn rằng mọi consumer SIM SoftBank có public IPv4 inbound.

### 4.6. IIJ và bằng chứng về mô hình private/global IP

IIJ Mobile business công bố nhiều network type:

```text
Internet NAT:
- Dynamic private IPv4
- Dynamic global IPv6

Global IPv4:
- Dynamic global IPv4
- Dynamic global IPv6

Fixed global IPv4:
- Fixed global IPv4
```

Đây là lý do kiến trúc phải runtime-detect thay vì hard-code.

### 4.7. Quy tắc thiết kế

Hệ thống phải lưu riêng:

```text
Device local IPv4
Device local IPv6
Observed egress IPv4
Observed egress IPv6
Carrier
APN
Network type
ASN
Observed location
```

Không được coi:

```text
device interface IP == Internet egress IP
```

---

## 5. Kiến trúc tổng thể

```text
+--------------------------------------------------------------+
|                       WINDOWS SERVER                         |
|                                                              |
| +-----------------------+       +--------------------------+ |
| | MobileProxy.Desktop   |       | MobileProxy.Service      | |
| | WPF                   |<----->| ASP.NET Core             | |
| | MVVM                  |       | Windows Service          | |
| +-----------------------+       +-------------+------------+ |
|                                             |                |
|                       +---------------------+---------------+|
|                       |                                      ||
|               +-------v-------+                      +-------v-------+
|               | Control Plane |                      | Data Plane    |
|               | CQRS          |                      | Proxy Engine  |
|               | EF Core       |                      | TCP streams   |
|               | SQLite        |                      | no EF/CQRS    |
|               +-------+-------+                      +-------+-------+
|                       |                                      |
|                       +------------------+-------------------+
|                                          |
|                                         ADB
+------------------------------------------+-------------------+
                                           |
                       +-------------------+-------------------+
                       |                   |                   |
                 +-----v-----+       +-----v-----+       +-----v-----+
                 | Android 1 |       | Android 2 |       | Android N |
                 | Agent     |       | Agent     |       | Agent     |
                 +-----+-----+       +-----+-----+       +-----+-----+
                       |                   |                   |
                    SIM 4G/5G           SIM 4G/5G           SIM 4G/5G
                       |                   |                   |
                       +-------------------+-------------------+
                                           |
                                        Internet
```

---

## 6. Kiến trúc bắt buộc: Control Plane và Data Plane tách riêng

### 6.1. Control Plane

Control Plane quản lý:

- Devices.
- Device groups.
- SIM profiles.
- Proxy endpoints.
- Proxy pools.
- Routing policies.
- Rotation policies.
- Health policies.
- Recovery policies.
- Settings.
- Users/local operators nếu cần.
- Audit.
- History.
- Configuration.

Control Plane dùng:

- Clean Architecture.
- CQRS.
- Validation.
- EF Core.
- SQLite.

### 6.2. Data Plane

Data Plane xử lý:

```text
socket -> stream -> Android -> cellular socket
```

Data Plane tuyệt đối không đi qua:

- EF Core.
- Repository.
- CQRS handler cho từng packet.
- MediatR cho từng connection chunk.
- Database transaction cho từng byte.

Database chỉ nhận session summary/rollup sau hoặc theo interval.

---

## 7. Technology Stack

### 7.1. Windows

```text
.NET 10 LTS
ASP.NET Core
Windows Service
WPF
CommunityToolkit.Mvvm
EF Core
SQLite
FluentValidation
Serilog
SignalR
```

### 7.2. Android

```text
Kotlin
Android Foreground Service
ConnectivityManager
NetworkRequest
NetworkCapabilities
Network.getSocketFactory()
Network.getAllByName()
ADB USB
```

### 7.3. Proxy

Giai đoạn triển khai:

```text
1. SOCKS5 TCP
2. HTTP CONNECT
3. SOCKS5 UDP
4. TUN/full-tunnel gateway
```

### 7.4. Database

Local version:

```text
SQLite
```

Không cần:

```text
MySQL
MongoDB
Redis
PostgreSQL
RabbitMQ
Kafka
```

ở giai đoạn local.

### 7.5. Vì sao SQLite

Dữ liệu local chủ yếu là relational:

- Device.
- Proxy.
- Pool.
- Policy.
- History.
- Session summary.
- Incident.
- Audit.

SQLite:

- Không cần cài DB server.
- Dễ backup.
- Dễ deploy.
- EF Core có provider chính thức.
- Phù hợp single-host application.

Khi scale thành nhiều server:

```text
SQLite -> PostgreSQL
```

Khi cần shared state/distributed locking:

```text
In-memory state -> Redis
```

MongoDB không có lợi thế rõ ràng cho domain này.

---

## 8. Solution Structure

```text
MobileProxy.sln

src/
  MobileProxy.Domain/
    Devices/
    DeviceGroups/
    Sims/
    Proxies/
    Pools/
    Sessions/
    Networking/
    Rotation/
    Monitoring/
    Recovery/
    Traffic/
    Security/
    Common/

  MobileProxy.Application/
    Devices/
    DeviceGroups/
    Sims/
    Proxies/
    Pools/
    Sessions/
    Rotation/
    Monitoring/
    Recovery/
    Settings/
    Diagnostics/
    Common/

  MobileProxy.Infrastructure/
    Persistence/
    Adb/
    AndroidAgent/
    Networking/
    Security/
    Logging/
    System/
    Backup/

  MobileProxy.Proxy/
    Socks5/
    HttpConnect/
    Routing/
    Transport/
    Sessions/
    Traffic/
    Dns/
    LeakProtection/

  MobileProxy.Service/
    Api/
    Workers/
    Hubs/
    Hosting/
    Program.cs

  MobileProxy.Desktop/
    Views/
    ViewModels/
    Controls/
    Navigation/
    Services/

  MobileProxy.Contracts/
    Devices/
    Proxies/
    Events/
    Common/

android/
  MobileProxy.Agent/
    app/
    proxy/
    cellular/
    control/
    diagnostics/
    monitoring/
    security/
```

---

## 9. Dependency Rules

```text
Domain
  ^
  |
Application
  ^
  |
Infrastructure

Service -> Application
Desktop -> Contracts/API
Proxy -> Domain abstractions where necessary
```

Domain không được tham chiếu:

- EF Core.
- SQLite.
- ADB.
- WPF.
- Android.
- HTTP.
- SignalR.

---

## 10. CQRS Convention

Mỗi use case là một feature slice riêng.

Ví dụ:

```text
Application/
  Devices/
    GetDevices/
      GetDevicesQuery.cs
      GetDevicesHandler.cs

    GetDeviceById/
      GetDeviceByIdQuery.cs
      GetDeviceByIdHandler.cs

    RegisterDevice/
      RegisterDeviceCommand.cs
      RegisterDeviceHandler.cs
      RegisterDeviceValidator.cs

    EnableDevice/
      EnableDeviceCommand.cs
      EnableDeviceHandler.cs

    RotateDeviceIp/
      RotateDeviceIpCommand.cs
      RotateDeviceIpHandler.cs
      RotateDeviceIpValidator.cs
```

Tên phải biểu đạt đúng hành vi.

Không dùng:

```text
GetDevice
```

nếu là:

```text
GetDeviceById
```

Không tạo helper mới nếu hệ thống đã có abstraction/hàm tương đương.

---

## 11. Domain Model

### 11.1. DeviceNode

```text
DeviceNode
- Id
- AdbSerial
- AgentId
- Name
- Manufacturer
- Model
- AndroidVersion
- AgentVersion
- Enabled
- DesiredState
- ActualState
- GroupId
- LastSeenAt
- CreatedAt
- UpdatedAt
```

### 11.2. DeviceCapabilities

```text
DeviceCapabilities
- DeviceId
- CanProxyTcp
- CanProxyUdp
- CanResolveDnsOnCellular
- CanReadCarrier
- CanReadSignal
- CanReadBattery
- CanReadTemperature
- CanReboot
- CanRestartAgent
- CanToggleMobileData
- CanToggleAirplaneMode
- CanChangePreferredNetworkType
- IsDeviceOwner
- HasRoot
- SupportsIpv4
- SupportsIpv6
```

Capability phải được detect, không hard-code.

### 11.3. DeviceGroup

```text
DeviceGroup
- Id
- Name
- Description
- SortOrder
- Enabled
```

Use case:

```text
Box 01
Box 02
Rakuten Group
Docomo Group
Test Devices
```

### 11.4. SimProfile

Không phụ thuộc vào khả năng đọc IMSI/ICCID.

```text
SimProfile
- Id
- DeviceId
- DisplayName
- Carrier
- PlanName
- APN
- MonthlyQuotaBytes
- BillingCycleDay
- Notes
- Enabled
```

Các identifier nhạy cảm chỉ lưu nếu người dùng chủ động cung cấp hoặc Android cho phép đọc hợp lệ.

### 11.5. NetworkSnapshot

```text
NetworkSnapshot
- Id
- DeviceId
- Timestamp
- Carrier
- MCC
- MNC
- RadioType
- IsRoaming
- LocalIpv4
- LocalIpv6
- EgressIpv4
- EgressIpv6
- DnsServers
- SignalDbm
- Rsrp
- Rsrq
- Sinr
- LatencyMs
- PacketLoss
```

### 11.6. EgressIpObservation

```text
EgressIpObservation
- Id
- DeviceId
- Address
- IpVersion
- FirstSeenAt
- LastSeenAt
- DetectionReason
- Carrier
- ASN
- Country
- Region
- City
- IsDuplicateInFleet
```

Location ở đây là IP geolocation estimate, không phải GPS.

### 11.7. ProxyEndpoint

```text
ProxyEndpoint
- Id
- Name
- BindAddress
- Port
- Protocol
- TargetType
- TargetId
- Enabled
- AuthenticationMode
- DnsMode
- IpMode
- MaxConnections
- IdleTimeoutSeconds
- SessionTimeoutSeconds
- FailoverPolicyId
```

### 11.8. ProxyPool

```text
ProxyPool
- Id
- Name
- Description
- Enabled
- RoutingStrategy
- StickyMode
- StickyTtlSeconds
- HealthPolicyId
```

### 11.9. ProxyPoolMember

```text
ProxyPoolMember
- PoolId
- DeviceId
- Enabled
- Weight
- Priority
```

### 11.10. RotationPolicy

```text
RotationPolicy
- Id
- Name
- Mode
- IntervalSeconds
- TrafficThresholdBytes
- RotateAfterSession
- RequireDifferentIp
- MaxAttempts
- RetryDelaySeconds
- DrainMode
- DrainTimeoutSeconds
- Enabled
```

### 11.11. RotationExecution

```text
RotationExecution
- Id
- DeviceId
- PolicyId
- StartedAt
- FinishedAt
- OldIpv4
- NewIpv4
- OldIpv6
- NewIpv6
- Method
- Result
- Attempts
- ErrorCode
```

### 11.12. ProxySession

```text
ProxySession
- Id
- EndpointId
- DeviceId
- StartedAt
- ConnectedAt
- EndedAt
- DestinationHost
- DestinationPort
- Protocol
- BytesUp
- BytesDown
- EgressIpv4
- EgressIpv6
- CloseReason
```

Destination logging phải có privacy setting và có thể tắt.

### 11.13. TrafficRollup

```text
TrafficRollup
- Id
- DeviceId
- EndpointId
- WindowStart
- WindowEnd
- BytesUp
- BytesDown
- ConnectionCount
- FailureCount
```

### 11.14. HealthIncident

```text
HealthIncident
- Id
- DeviceId
- Component
- Severity
- ErrorCode
- Message
- StartedAt
- ResolvedAt
- RecoveryExecutionId
```

### 11.15. RecoveryPolicy

```text
RecoveryPolicy
- Id
- Name
- RestartProxy
- RestartAgent
- RecreateAdbForward
- ReconnectCellular
- RebootDevice
- MaxAttempts
- BackoffPolicy
- CooldownSeconds
```

### 11.16. AppSetting

Dùng strongly typed settings, không tạo một bảng key/value hỗn loạn nếu setting có domain riêng.

---

## 12. Database Tables

Bản local dự kiến:

```text
devices
device_capabilities
device_groups
sim_profiles

proxy_endpoints
proxy_pools
proxy_pool_members

rotation_policies
rotation_executions

network_snapshots
egress_ip_observations

proxy_sessions
traffic_rollups

health_incidents
recovery_policies
recovery_executions

audit_logs
command_executions

app_settings
schema_versions
```

Không lưu log text liên tục vào SQLite.

Log chi tiết dùng rolling files.

---

## 13. Runtime State

Các dữ liệu sau ở RAM:

```text
CurrentDeviceRuntime
CurrentProxyRuntime
ActiveSessions
CurrentTrafficCounters
CurrentHealth
CurrentNodeOperation
ADB connection state
Agent connection state
```

Không update DB mỗi packet.

Metrics được aggregate theo interval, ví dụ 30 hoặc 60 giây.

---

## 14. Android Agent

### 14.1. Vai trò

Android Agent là thành phần biến điện thoại thành mobile exit node.

Agent phải:

- Chạy foreground service.
- Chỉ bind control/proxy listener vào loopback của Android.
- Nhận command từ Windows qua ADB port forward.
- Request cellular network.
- Tạo socket trực tiếp bằng cellular `Network`.
- Resolve hostname bằng cellular `Network`.
- Không fallback sang Wi-Fi.
- Report capability.
- Report health.
- Report network state.
- Detect egress IP.
- Handle proxy stream.

### 14.2. Cellular binding

Logic bắt buộc:

```text
NetworkRequest
  - TRANSPORT_CELLULAR
  - NET_CAPABILITY_INTERNET

ConnectivityManager.requestNetwork()

onAvailable(Network cellularNetwork)
```

Socket ra Internet phải tạo bằng:

```text
cellularNetwork.socketFactory
```

DNS phải resolve bằng network tương ứng.

Không dùng default socket nếu không chứng minh được default network chính là cellular.

### 14.3. Fail closed

Nếu cellular network biến mất:

```text
existing cellular sockets -> fail
new proxy connections -> reject
```

Không tự chuyển sang Wi-Fi.

### 14.4. Android Agent API nội bộ

Ví dụ:

```text
GET  /agent/status
GET  /agent/capabilities
GET  /network/status
GET  /network/egress
POST /network/test
POST /network/rotate
POST /proxy/start
POST /proxy/stop
GET  /health
GET  /diagnostics
```

API này chỉ tồn tại sau ADB forward, không expose ra LAN.

---

## 15. ADB Management

### 15.1. ADB dùng để làm gì

ADB là management/bootstrap channel:

- Detect device.
- Authorize state.
- Install APK.
- Upgrade APK.
- Start agent.
- Stop agent.
- Restart agent.
- Port forwarding.
- Reboot device.
- Pull diagnostics.
- logcat.
- Screenshot.
- Start scrcpy.
- Read supported system diagnostics.
- Recovery.

ADB không phải business domain.

### 15.2. Port forwarding

Android Agent listen trên:

```text
127.0.0.1:27100
```

Windows map:

```text
Host port -> Android 27100
```

Mỗi device có mapping ổn định trong runtime.

### 15.3. Device identity

Không sử dụng vị trí trong output `adb devices` làm identity.

Identity ưu tiên:

```text
Internal DeviceId
AgentId
ADB serial
Device fingerprint
```

### 15.4. ADB states

Phải phân biệt:

```text
Disconnected
Unauthorized
Offline
Connected
```

`Unauthorized` phải hiển thị rõ để người dùng xác nhận RSA prompt trên điện thoại.

---

## 16. Device State Machine

```text
Disconnected
   |
   v
AdbConnected
   |
   v
AgentStarting
   |
   v
AgentReady
   |
   v
CellularConnecting
   |
   v
CellularReady
   |
   v
EgressVerifying
   |
   v
ProxyReady
```

Các state bổ sung:

```text
Disabled
Degraded
Draining
RotatingIp
RestartingAgent
Rebooting
UpdatingAgent
Recovering
Error
```

---

## 17. Desired State và Actual State

Ví dụ:

```text
DesiredState = Enabled
ActualState = Disconnected
```

Nếu device bị rút USB rồi cắm lại:

```text
Reconciliation Worker
  |
  v
detect device
  |
  v
restore agent
  |
  v
restore ADB forwarding
  |
  v
verify cellular
  |
  v
restore proxy
```

Không yêu cầu người dùng bật lại thủ công.

---

## 18. Windows Service

Windows Service phải chạy độc lập Desktop UI.

```text
Windows boot
  |
  v
MobileProxy.Service
  |
  +-> load DB
  +-> initialize ADB
  +-> discover devices
  +-> restore desired state
  +-> restore proxies
  +-> health monitoring
```

Đóng WPF UI không được làm proxy dừng.

---

## 19. Background Workers

```text
AdbDiscoveryWorker
DeviceReconciliationWorker
AgentLifecycleWorker
HealthCheckWorker
EgressIpObservationWorker
RotationSchedulerWorker
RecoveryWorker
MetricsAggregationWorker
SessionCleanupWorker
DatabaseMaintenanceWorker
```

Mỗi worker phải tôn trọng `DeviceOperationCoordinator`.

---

## 20. Device Operation Coordinator

Một device chỉ được có một destructive operation tại một thời điểm.

Ví dụ không được:

```text
RotateIp
+
Reboot
+
UpdateAgent
```

đồng thời.

State:

```text
Idle
Rotating
RestartingAgent
Rebooting
UpdatingAgent
Recovering
```

Command khác phải:

- Queue.
- Reject với `DeviceBusy`.
- Hoặc defer tùy loại operation.

---

## 21. Proxy Modes

### 21.1. Direct Device Proxy

Mỗi phone một port.

```text
127.0.0.1:12001 -> Phone 01
127.0.0.1:12002 -> Phone 02
127.0.0.1:12003 -> Phone 03
```

Ưu điểm:

- Dễ debug.
- Overhead thấp.
- Biết chính xác đang dùng phone nào.
- Phù hợp MVP.

### 21.2. Gateway Proxy

Một endpoint có thể route tới pool.

```text
127.0.0.1:13000
  |
  v
Proxy Router
  |
  +-> Phone 01
  +-> Phone 04
  +-> Phone 07
```

Gateway mode cung cấp:

- Pool.
- Sticky session.
- Load balancing.
- Failover.
- Rotation.
- Central accounting.
- Policy.

---

## 22. SOCKS5

MVP phải hỗ trợ:

- SOCKS5 CONNECT.
- IPv4 destination.
- IPv6 destination.
- Domain name destination.
- Remote DNS.
- Username/password optional.
- Connection timeout.
- Idle timeout.
- Max connections.
- Graceful close.

Domain request phải được resolve trên Android cellular network.

---

## 23. HTTP CONNECT

Sau SOCKS5 TCP:

- CONNECT host:port.
- Proxy authentication.
- Remote DNS.
- Timeout.
- Access policy.

Không MITM TLS.

Proxy chỉ tunnel bytes.

---

## 24. UDP

Không bắt buộc MVP.

Sau khi TCP ổn định có thể triển khai:

- SOCKS5 UDP ASSOCIATE.
- Custom tunnel.
- DNS UDP nếu cần.
- WebRTC-aware routing.

UDP phức tạp hơn vì:

- NAT.
- Mapping lifetime.
- Flow association.
- Packet loss.
- CGNAT.
- Mobile network changes.

---

## 25. Sử dụng proxy trên chính Windows

### 25.1. Per-app proxy

Ứng dụng hỗ trợ SOCKS/HTTP:

```text
socks5://127.0.0.1:12001
```

### 25.2. Browser

Có thể cấu hình browser hoặc profile browser sử dụng SOCKS5/HTTP proxy.

### 25.3. System proxy

Có thể thêm feature quản lý Windows proxy settings, nhưng phải hiểu:

- Không phải mọi app tuân theo Windows system proxy.
- UDP thường không đi qua HTTP system proxy.

### 25.4. Full tunnel

Phase sau có thể dùng Wintun:

```text
Windows app traffic
  |
  v
TUN
  |
  v
Proxy Gateway
  |
  v
Selected Mobile Node
```

Mode này phù hợp khi muốn force gần như toàn bộ IP traffic qua node.

---

## 26. Dùng proxy cho điện thoại khác trong nhà

Có.

### 26.1. LAN Proxy Mode

Windows bind proxy listener vào LAN IP:

```text
192.168.1.10:13000
```

Điện thoại:

```text
Phone client
  |
  v
Wi-Fi LAN
  |
  v
Windows Proxy Gateway
  |
  v
Android proxy node connected by USB
  |
  v
SIM
```

Khi bật LAN mode bắt buộc:

- Authentication.
- IP allowlist.
- Windows Firewall rule.
- Rate limit.
- Max connections.
- No open proxy.
- Configurable listen interface.

### 26.2. App hỗ trợ proxy

Nếu app cho nhập SOCKS/HTTP proxy thì dùng trực tiếp.

### 26.3. Toàn bộ traffic của điện thoại client

Android/iOS không bảo đảm mọi app tuân theo Wi-Fi HTTP proxy.

Muốn system-wide cần:

```text
VPN/TUN client on client phone
  |
  v
LAN gateway on Windows
  |
  v
Mobile Proxy Node
```

Có thể xây client riêng sau này hoặc sử dụng router gateway mode.

---

## 27. Dùng với router trong nhà

Có thể, nhưng tùy router.

### 27.1. Router hỗ trợ upstream proxy

Nếu firmware hỗ trợ SOCKS/HTTP upstream thì trỏ về Windows Gateway.

### 27.2. OpenWrt/custom router

Có thể triển khai:

```text
LAN clients
  |
  v
OpenWrt
  |
  v
TUN / transparent gateway
  |
  v
Windows MobileProxy Gateway
  |
  v
Selected Android SIM
```

### 27.3. Router dân dụng không hỗ trợ proxy

Giải pháp:

- Dùng Windows làm gateway.
- Dùng OpenWrt side-router.
- Hoặc triển khai TUN/VPN mode.

Không cố ép support cho router không có khả năng routing/proxy phù hợp.

---

## 28. Router/Gateway Policy

Có thể chọn:

```text
All devices -> Pool A

Device MAC/IP A -> Phone 01
Device MAC/IP B -> Pool Docomo
Device MAC/IP C -> Pool Rakuten
```

Đây là phase nâng cao.

---

## 29. Proxy Pool

Pool examples:

```text
All Japan
Docomo
au
SoftBank
Rakuten
High Signal
Low Latency
IPv6 Capable
Test Pool
```

### 29.1. Routing strategies

```text
RoundRobin
Random
LeastConnections
LowestLatency
LeastTraffic
Weighted
Priority
Sticky
Failover
```

### 29.2. Eligible node rule

Node chỉ được chọn nếu:

```text
Enabled
AND ProxyReady
AND CellularReady
AND AgentReady
AND Health != Critical
AND NotRotating
AND NotDraining
AND NotUpdating
AND ConnectionCount < Limit
```

---

## 30. Sticky Session

Các mode:

```text
NoSticky
StickyByClientIp
StickyByCredential
StickyBySessionKey
StickyForDuration
```

Ví dụ:

```text
same credential -> same DeviceNode for 30 minutes
```

Sticky session không có nghĩa giữ nguyên public IP nếu carrier tự đổi IP giữa chừng.

---

## 31. Proxy Selection

Client/UI phải có thể chọn:

```text
Specific device
Specific pool
Specific carrier
IPv4 required
IPv6 required
Lowest latency
New egress IP required
Sticky node
Random healthy node
```

Nếu yêu cầu không thỏa:

```text
NoEligibleNode
```

không silent fallback sang Windows Internet.

---

## 32. IP Rotation

### 32.1. Rotation modes

```text
Manual
Interval
AfterSession
AfterTrafficThreshold
OnHealthFailure
OnExplicitClientRequest
```

### 32.2. Rotation methods

Tùy capability:

```text
ReconnectCellular
ToggleMobileData
AirplaneModeCycle
RadioRestart
DeviceReboot
```

Không giả định Android stock cho phép tất cả.

### 32.3. Rotation workflow

```text
RotateIpCommand
  |
  v
Acquire device operation lock
  |
  v
State = RotatingIp
  |
  v
Stop new sessions
  |
  v
Drain or terminate existing sessions
  |
  v
Record old egress
  |
  v
Execute supported rotation method
  |
  v
Wait cellular
  |
  v
Wait Internet
  |
  v
Detect new egress
  |
  v
Compare
  |
  +-> changed -> Success
  |
  +-> same -> Unchanged
  |
  +-> no network -> Retry/Fail
```

### 32.4. Không bảo đảm mỗi rotation có IP mới

Carrier có thể:

- Gán lại IP cũ.
- Gán IP cùng NAT pool.
- Cho nhiều SIM cùng một public IPv4.

Vì vậy result phải phân biệt:

```text
SucceededChanged
SucceededUnchanged
Failed
Unsupported
```

---

## 33. Duplicate Egress IP Detection

Ví dụ:

```text
Phone 01 -> 49.10.10.10
Phone 02 -> 49.10.10.10
Phone 03 -> 126.20.30.40
```

Dashboard phải hiển thị:

```text
3 devices
2 unique IPv4 egress
```

Không báo 3 proxy IP độc lập.

---

## 34. IP History

Phải xem được:

```text
Timestamp
Device
Carrier
Old IP
New IP
Reason
Rotation method
Changed?
```

Dùng để đánh giá:

- Carrier có xoay IP tốt không.
- SIM nào hay nhận lại IP cũ.
- Duplicate rate.
- Average IP lifetime.
- Rotation success rate.

---

## 35. IP Geolocation

Có thể lookup:

```text
Country
Region
City
ASN
ISP
```

Nhưng phải ghi rõ:

```text
Estimated from egress IP
```

Không được coi là GPS.

Một phone ở Osaka có thể egress/geolocate ở khu vực khác tùy carrier gateway và database.

Do đó hệ thống không được hứa:

```text
"chọn chính xác Tokyo/Osaka"
```

nếu chỉ dựa vào cùng một box tại một vị trí.

---

## 36. Privacy and Leak Protection

Đây là module bắt buộc vì mục tiêu chính của dự án là sử dụng IP mobile thay cho IP thật của mạng local.

### 36.1. Host IP leak check

Hệ thống detect:

```text
Host egress IPv4
Host egress IPv6
```

và:

```text
Proxy egress IPv4
Proxy egress IPv6
```

Nếu proxy egress trùng host egress trong trường hợp không mong muốn:

```text
Critical: RouteLeakDetected
```

Node không được đưa vào pool.

### 36.2. DNS leak check

Test hostname qua proxy và xác minh DNS được thực hiện bằng Android cellular network.

Mặc định:

```text
DnsMode = Remote
```

### 36.3. IPv6 leak check

Nếu client có IPv6 nhưng proxy route chỉ hỗ trợ IPv4:

- Full tunnel mode phải block direct IPv6.
- Không được để app tự đi IPv6 qua Windows Internet.

### 36.4. WebRTC leak

SOCKS/HTTP proxy không tự bảo đảm WebRTC UDP đi qua proxy.

Phải có:

- Leak test.
- TUN mode nếu cần full traffic enforcement.
- Browser-specific warning.
- Option block unsupported UDP/direct traffic.

### 36.5. GPS warning

Dashboard/Documentation phải nói rõ:

```text
Mobile proxy changes network egress.
It does not override GPS permission/location APIs.
```

---

## 37. Health Model

Health không chỉ là Online/Offline.

### 37.1. Dimensions

```text
ADB
Agent
Cellular
Internet
DNS
IPv4
IPv6
Proxy
Latency
PacketLoss
Thermal
Battery
Storage
```

### 37.2. Overall state

```text
Healthy
Degraded
Critical
Unknown
```

### 37.3. Example

```text
ADB: Healthy
Agent: Healthy
Cellular: Healthy
Internet IPv4: Failed
Internet IPv6: Healthy
Proxy: Degraded
```

Không ghi chung là `Offline`.

---

## 38. Health Checks

Các check:

```text
ADB presence
Agent handshake
Cellular Network available
DNS resolution
IPv4 outbound
IPv6 outbound
Proxy TCP connect
Egress IP detection
Latency
Packet loss
Signal
Battery
Temperature
Storage
```

Các check phải có timeout riêng.

---

## 39. Automatic Recovery

Recovery ladder:

```text
Proxy failed
  |
  v
Restart proxy subsystem
  |
  v
Restart Android agent
  |
  v
Recreate ADB forward
  |
  v
Re-request cellular network
  |
  v
Supported radio/mobile recovery
  |
  v
Reboot device
  |
  v
Mark Critical
```

Có:

```text
MaxAttempts
Cooldown
ExponentialBackoff
CircuitBreaker
```

Không tạo endless reboot loop.

---

## 40. Draining

Trước rotation/reboot/update:

```text
Device -> Draining
```

Policy:

### Graceful

- Không nhận session mới.
- Chờ session đang chạy.
- Timeout rồi mới force close.

### Immediate

- Đóng session.
- Thực hiện operation ngay.

---

## 41. Sessions

UI xem được:

```text
Session ID
Device
Proxy Endpoint
Client
Destination
Protocol
Started At
Duration
Bytes Up
Bytes Down
Current Egress
State
```

Action:

```text
Terminate
Terminate All On Device
Drain Device
```

Existing TCP session không thể migrate sang phone khác mà giữ nguyên connection semantics.

---

## 42. Traffic Accounting

Count theo:

```text
Device
SIM
Proxy Endpoint
Pool
Session
Day
Month
```

Metrics:

```text
Bytes Up
Bytes Down
Current Upload Rate
Current Download Rate
Connection Count
Peak Connections
Failures
```

---

## 43. SIM Quota

Một SIM có thể cấu hình:

```text
MonthlyQuota
WarningThreshold
SoftLimit
HardLimit
BillingCycle
```

Policy:

```text
80% -> Warning
90% -> Deprioritize
100% -> Stop accepting new sessions
```

Configurable.

---

## 44. Device Temperature và Battery

Phone farm chạy 24/7 cần:

- Battery percentage.
- Charging state.
- Battery temperature.
- Thermal state nếu API hỗ trợ.
- Device temperature nếu đọc được.
- Thermal warning.
- Optional proxy throttling.
- Optional temporary disable.

Không giả định mọi model cung cấp cùng sensor/API.

---

## 45. CRUD Matrix

### 45.1. Device

Create:
- Auto register on first authorized connection.
- Manual metadata.

Read:
- List.
- Detail.
- State.
- Capability.
- Health.
- Network.
- History.

Update:
- Name.
- Group.
- Desired state.
- Notes.
- Limits.
- Policies.

Delete:
- Forget device.
- Soft-delete/history retention policy.

Actions:
- Enable.
- Disable.
- Start agent.
- Restart agent.
- Update agent.
- Reboot.
- Test.
- Rotate IP.
- Drain.
- Recover.
- Diagnostics.

### 45.2. Device Group

Create.
Read.
Update.
Delete.
Assign devices.
Remove devices.
Bulk enable.
Bulk disable.
Bulk health check.
Bulk update agent.

### 45.3. SIM Profile

Create.
Read.
Update.
Delete.
Assign to device.
Set carrier.
Set APN metadata.
Set quota.
Set billing cycle.
Set notes.

### 45.4. Proxy Endpoint

Create.
Read.
Update.
Delete.
Start.
Stop.
Restart.
Test.
Copy URI.
View sessions.
Change target.
Change authentication.
Change bind interface.

### 45.5. Proxy Pool

Create.
Read.
Update.
Delete.
Add member.
Remove member.
Enable member.
Disable member.
Set weight.
Set priority.
Set routing policy.
Set sticky policy.
Test pool.

### 45.6. Rotation Policy

Create.
Read.
Update.
Delete.
Assign to device.
Assign to pool.
Enable.
Disable.
Run now.
View executions.

### 45.7. Recovery Policy

Create.
Read.
Update.
Delete.
Assign.
Test.
View recovery history.

### 45.8. Sessions

Read active.
Read history.
Terminate.
Terminate by device.
Terminate by endpoint.
Cleanup historical records.

### 45.9. IP Observations

Read current.
Read history.
Filter by device/carrier.
Find duplicates.
Mark notes.
Retention cleanup.

### 45.10. Incidents

Read.
Filter.
Acknowledge.
Resolve.
View recovery.
Export.

### 45.11. Settings

Read.
Update.
Reset selected setting.
Export.
Import.

### 45.12. Backup

Create backup.
List backups.
Verify backup.
Restore.
Delete backup.
Export backup.

---

## 46. Application Commands

```text
RegisterDeviceCommand
ForgetDeviceCommand
RenameDeviceCommand
EnableDeviceCommand
DisableDeviceCommand
AssignDeviceToGroupCommand

InstallAgentCommand
UpdateAgentCommand
RestartAgentCommand
RebootDeviceCommand
RefreshDeviceCapabilitiesCommand

RefreshNetworkStatusCommand
TestCellularConnectionCommand
RefreshEgressIpCommand
RotateDeviceIpCommand

CreateProxyEndpointCommand
UpdateProxyEndpointCommand
DeleteProxyEndpointCommand
StartProxyEndpointCommand
StopProxyEndpointCommand
RestartProxyEndpointCommand
TestProxyEndpointCommand

CreateProxyPoolCommand
UpdateProxyPoolCommand
DeleteProxyPoolCommand
AddDeviceToPoolCommand
RemoveDeviceFromPoolCommand
ChangePoolRoutingPolicyCommand

CreateRotationPolicyCommand
UpdateRotationPolicyCommand
DeleteRotationPolicyCommand
RunRotationCommand

TerminateProxySessionCommand
DrainDeviceCommand

CreateBackupCommand
RestoreBackupCommand
ExportDiagnosticsCommand
```

---

## 47. Application Queries

```text
GetDevicesQuery
GetDeviceByIdQuery
GetDeviceRuntimeQuery
GetDeviceHealthQuery
GetDeviceNetworkHistoryQuery
GetDeviceIpHistoryQuery

GetProxyEndpointsQuery
GetProxyEndpointByIdQuery
GetProxyPoolsQuery
GetProxyPoolByIdQuery

GetActiveSessionsQuery
GetSessionHistoryQuery

GetCurrentEgressMatrixQuery
GetDuplicateEgressIpsQuery

GetTrafficSummaryQuery
GetTrafficHistoryQuery

GetIncidentsQuery
GetRotationExecutionsQuery
GetRecoveryExecutionsQuery

GetSystemHealthQuery
GetDashboardSummaryQuery
```

---

## 48. Local Management API

Default bind:

```text
127.0.0.1
```

Không bind LAN management API mặc định.

Ví dụ:

```text
GET    /api/devices
GET    /api/devices/{id}
POST   /api/devices/{id}/enable
POST   /api/devices/{id}/disable
POST   /api/devices/{id}/rotate-ip
POST   /api/devices/{id}/restart-agent
POST   /api/devices/{id}/reboot
GET    /api/devices/{id}/health
GET    /api/devices/{id}/network
GET    /api/devices/{id}/ip-history

GET    /api/proxies
POST   /api/proxies
GET    /api/proxies/{id}
PUT    /api/proxies/{id}
DELETE /api/proxies/{id}
POST   /api/proxies/{id}/start
POST   /api/proxies/{id}/stop
POST   /api/proxies/{id}/test

GET    /api/pools
POST   /api/pools
PUT    /api/pools/{id}
DELETE /api/pools/{id}

GET    /api/sessions
DELETE /api/sessions/{id}

GET    /api/traffic
GET    /api/incidents
GET    /api/system/health
```

---

## 49. Realtime Events

SignalR events:

```text
DeviceConnected
DeviceDisconnected
DeviceStateChanged
DeviceHealthChanged

AgentStateChanged
CellularStateChanged

EgressIpChanged
DuplicateIpDetected

ProxyStarted
ProxyStopped
ProxyFailed

SessionOpened
SessionClosed

TrafficUpdated

RotationStarted
RotationCompleted

RecoveryStarted
RecoveryCompleted

IncidentCreated
IncidentResolved
```

UI không poll database liên tục.

---

## 50. Desktop UI

Navigation:

```text
Dashboard
Devices
Device Groups
Proxies
Pools
Sessions
IP Management
Traffic
Health
Incidents
Diagnostics
Settings
```

### 50.1. Dashboard

Hiển thị:

```text
Total devices
Ready devices
Degraded devices
Offline devices

Unique IPv4 egress
Unique IPv6 egress

Active sessions
Current throughput
Traffic today
Traffic this month

Rotation success rate
Critical incidents
```

### 50.2. Devices

Columns:

```text
Name
Model
ADB
Agent
Carrier
Radio
Signal
Egress IPv4
Egress IPv6
Proxy
Sessions
Traffic
Latency
Temperature
Health
```

### 50.3. Device Detail

Tabs:

```text
Overview
Network
Proxy
Sessions
IP History
Traffic
Health
Diagnostics
ADB
Logs
Settings
```

### 50.4. Proxy Management

Columns:

```text
Name
Endpoint
Protocol
Target
Current egress
Sessions
Traffic
Status
```

Actions:

```text
Start
Stop
Restart
Test
Edit
Copy Proxy URI
Rotate Target
View Sessions
```

---

## 51. Proxy URI

Examples:

```text
socks5://127.0.0.1:12001
http://127.0.0.1:12101
```

LAN:

```text
socks5://192.168.1.10:13000
```

Authentication credentials phải lưu bảo mật.

---

## 52. Auto Port Assignment

Config:

```text
Direct SOCKS base port: 12000
Direct HTTP base port: 12100
Pool base port: 13000
```

Nhưng mapping được persist để reconnect không tự đổi port ngoài ý muốn.

---

## 53. Proxy Test

`Test Proxy` phải test end-to-end.

```text
Client side
  |
  v
Proxy listener
  |
  v
Selected Android
  |
  v
Cellular
  |
  v
External test endpoint
```

Result:

```text
SOCKS/HTTP handshake
DNS
IPv4 connectivity
IPv6 connectivity
Egress IPv4
Egress IPv6
Latency
Connect time
Carrier
Route leak status
```

Không báo proxy healthy chỉ vì Android Agent trả lời.

---

## 54. Error Model

Không throw message tự do khắp code.

Chuẩn hóa:

```text
ErrorCode
Message
TechnicalDetail
DeviceId
OperationId
Retryable
Timestamp
```

### 54.1. Device errors

```text
DeviceDisconnected
DeviceUnauthorized
DeviceOffline
DeviceBusy
DeviceDisabled
DeviceNotFound
UnsupportedOperation
```

### 54.2. Agent errors

```text
AgentNotInstalled
AgentVersionMismatch
AgentStartFailed
AgentHandshakeFailed
AgentUnreachable
```

### 54.3. Cellular errors

```text
CellularUnavailable
CellularRequestTimeout
NoInternetCapability
DnsResolutionFailed
Ipv4Unavailable
Ipv6Unavailable
```

### 54.4. Proxy errors

```text
ProxyBindFailed
ProxyPortInUse
ProxyAuthenticationFailed
NoEligibleNode
TargetConnectTimeout
TargetConnectionRefused
ProxyRouteLeakDetected
```

### 54.5. Rotation errors

```text
RotationUnsupported
RotationTimeout
RotationSameIp
RotationNoNetwork
RotationRecoveryFailed
```

### 54.6. ADB errors

```text
AdbNotFound
AdbServerUnavailable
AdbUnauthorized
AdbCommandTimeout
AdbForwardFailed
```

---

## 55. Error Handling Rules

- Timeout mọi external operation.
- CancellationToken xuyên suốt.
- Retry chỉ với error retryable.
- Exponential backoff.
- Circuit breaker khi device liên tục fail.
- Không swallow exception.
- Không retry destructive command vô hạn.
- Mỗi operation có correlation/operation ID.
- UI nhận error code rõ ràng.
- Technical detail vào log, không nhồi toàn bộ stack trace vào UI.

---

## 56. Idempotency

Commands nên idempotent khi có thể.

Ví dụ:

```text
EnableDevice
```

khi device đã enabled:

```text
success/no-op
```

Không error vô nghĩa.

```text
StartProxy
```

khi đã running:

```text
success/no-op
```

Destructive operations có operation ID để tránh double execution do UI retry.

---

## 57. Concurrency

### 57.1. Per-device operation lock

Một destructive command/device.

### 57.2. Endpoint lock

Start/stop/rebind proxy endpoint phải serialize.

### 57.3. Database concurrency

SQLite phù hợp local nhưng write workload phải kiểm soát:

- Short transactions.
- WAL mode nếu phù hợp.
- Không ghi per packet.
- Batch metrics.
- Single writer patterns ở nơi cần.

---

## 58. Security

### 58.1. Defaults

```text
Management API: loopback only
Proxy endpoints: loopback only
Android agent: loopback only
ADB: USB
```

### 58.2. LAN proxy

Nếu bật LAN:

- Explicit opt-in.
- Username/password hoặc token.
- IP allowlist.
- Windows Firewall.
- Rate limiting.
- Connection limit.
- Audit.
- Không cho open proxy.

### 58.3. Secrets

Dùng Windows DPAPI hoặc secret storage tương đương.

Không lưu password plaintext vào SQLite.

### 58.4. ADB

Không expose:

```text
tcp/5555
```

ra LAN/Internet mặc định.

### 58.5. Destination access policy

Có thể thêm:

- Deny private address ranges để tránh SSRF vào mạng nội bộ khi LAN users dùng proxy.
- Deny loopback targets.
- Deny metadata service ranges.
- Optional domain/IP denylist.

---

## 59. Audit

Audit các hành động:

```text
Device enabled/disabled
Proxy created/updated/deleted
Pool changed
Rotation triggered
Agent updated
Device rebooted
Settings changed
Backup restored
LAN exposure enabled
```

Audit khác application logs.

---

## 60. Logging

### 60.1. Service log

Serilog rolling file.

### 60.2. Device/agent log

Tách theo device khi cần.

### 60.3. Incident log

Important event vào DB.

### 60.4. Privacy

Destination hostname logging:

```text
Off
ErrorsOnly
Full
```

mặc định nên là `ErrorsOnly` hoặc configurable.

---

## 61. Backup and Restore

Backup gồm:

```text
SQLite database
configuration
policy
optional selected logs
```

Không cần backup runtime sessions.

Yêu cầu:

- Atomic backup.
- Backup version.
- Schema version.
- Verify before restore.
- Restore requires service quiesce.
- Auto pre-restore backup.

---

## 62. Diagnostics

System diagnostics package:

```text
App version
.NET version
Windows version
ADB version
Connected device list
Device capabilities
Agent versions
Proxy configuration
Health summary
Recent sanitized logs
Recent incidents
Database schema version
```

Không tự export secrets.

---

## 63. Performance Principles

- Async sockets.
- `System.IO.Pipelines` hoặc stream abstraction phù hợp cho data plane.
- Buffer pooling.
- Không allocate lớn mỗi read/write.
- Backpressure.
- Cancellation.
- Per-session timeout.
- Per-device max connections.
- Không synchronous DB access trên proxy hot path.
- Không log từng packet.
- Metrics aggregate.

---

## 64. Scale Strategy

### 64.1. Stage 1

```text
1 Windows
1-10 phones
SQLite
ADB USB
```

### 64.2. Stage 2

```text
1 Windows
10-50 phones
SQLite
Improved USB topology
Health/recovery
```

### 64.3. Stage 3

```text
Multiple Windows node managers
Central control plane
PostgreSQL
Remote agent/gateway
```

### 64.4. Stage 4

```text
Distributed gateway
Redis for shared ephemeral state/locks
PostgreSQL
mTLS
Node registration
Remote telemetry
```

Local architecture phải giữ domain/use cases để migration không phá Application.

---

## 65. Không microservice sớm

Version local phải là:

```text
Modular Monolith
+
Android Agent
```

Không dùng sớm:

```text
Kubernetes
Kafka
RabbitMQ
Redis
MongoDB
multiple DB servers
```

Chỉ thêm khi có requirement thật.

---

## 66. Scale Database Path

```text
Local:
SQLite

Distributed:
PostgreSQL

Distributed ephemeral state:
Redis
```

Không dùng MongoDB chỉ vì network data có JSON.

Nếu một số diagnostics linh hoạt, có thể lưu JSON column/text trong relational database.

---

## 67. Router and Multi-client Scale

Gateway nên được thiết kế để sau này phục vụ:

```text
Windows local applications
LAN phones
LAN PCs
Router
Dedicated Wi-Fi segment
Remote client
```

Mọi client đều đi qua một `ProxyEndpoint` hoặc `GatewayProfile`.

---

## 68. Gateway Profile

Entity/config tương lai:

```text
GatewayProfile
- Name
- ClientScope
- TargetPool
- Protocol
- Authentication
- DnsPolicy
- Ipv6Policy
- FailClosed
```

Ví dụ:

```text
Home Phones -> Rakuten Pool
Desktop Work -> Docomo Pool
Testing VLAN -> Random Japan Pool
```

---

## 69. Failure Scenarios

### 69.1. Rút USB

```text
ADB disconnect
-> node unavailable
-> remove from pool eligibility
-> close affected sessions
-> incident
-> desired state remains enabled
```

Cắm lại tự reconcile.

### 69.2. USB cable lỗi

Detect flapping.

Nếu reconnect liên tục:

```text
Degraded: AdbFlapping
```

thêm cooldown để tránh install/start loop.

### 69.3. ADB unauthorized

Không spam command.

State:

```text
ActionRequired: Authorize USB debugging on device
```

### 69.4. Agent crash

```text
restart agent
-> verify
-> if repeat -> incident
```

### 69.5. Cellular mất

Không fallback Wi-Fi.

```text
CellularUnavailable
-> reject new sessions
-> recovery
```

### 69.6. SIM hết data

Có thể biểu hiện:

- No connectivity.
- Captive page.
- Throttled.
- Partial Internet.

Health check phải nhận biết degraded connectivity, không chỉ network connected.

### 69.7. Carrier đổi IP tự nhiên

Egress observer detect change.

Update:

- Device current egress.
- IP history.
- Sticky metadata.
- Event `EgressIpChanged`.

Existing sessions có thể tiếp tục hoặc fail tùy carrier/NAT.

### 69.8. Nhiều phone cùng public IP

Không coi là lỗi mạng.

Mark:

```text
DuplicateEgressIp
```

và routing policy có thể chọn tránh duplicate nếu user yêu cầu diversity.

### 69.9. IPv4 không có nhưng IPv6 có

Nếu destination hỗ trợ IPv6:

```text
usable
```

Nếu client yêu cầu IPv4:

- test carrier translation.
- nếu Android socket tới IPv4 vẫn hoạt động, usable.
- nếu không, node ineligible cho `IPv4Required`.

### 69.10. DNS fail

Không fallback Windows DNS.

Try configured cellular DNS strategy rồi mark failure.

### 69.11. Windows host Internet mất

Local ADB proxy vẫn có thể hoạt động nếu Android cellular hoạt động và architecture không phụ thuộc Internet của Windows cho data plane.

Các cloud lookup phụ trợ phải không làm proxy core chết.

### 69.12. SQLite lỗi/corrupt

Proxy runtime đang hoạt động không nên lập tức crash toàn bộ process nếu có thể.

- Stop configuration mutations.
- Raise critical incident.
- Create diagnostic.
- Attempt safe shutdown/backup recovery according to policy.

### 69.13. Port conflict

Auto detect.

Không overwrite process khác.

Return:

```text
ProxyPortInUse
```

### 69.14. Windows sleep/hibernate

On resume:

```text
rescan ADB
rebuild port forwarding
reconcile agent
refresh egress
health verify
```

### 69.15. Android reboot

ADB disappears rồi quay lại.

Reconciliation tự khôi phục.

### 69.16. Android battery/thermal

Nếu critical thermal:

```text
drain
disable temporarily
recover after cooldown
```

nếu user bật policy.

---

## 70. Acceptance Rules

Một DeviceNode chỉ được `ProxyReady` nếu:

```text
ADB available
AND Agent available
AND Cellular network available
AND Internet test passed
AND Proxy test passed
AND Route verification passed
```

Không dùng:

```text
ADB connected => Ready
```

---

## 71. Fail Closed Rules

Không được fallback sang Windows Internet.

Không được fallback Android Wi-Fi khi proxy được cấu hình cellular-only.

Nếu không bảo đảm route:

```text
connection fails
```

thay vì đi sai đường.

---

## 72. MVP Acceptance Test

### Test 1

```text
Windows browser
-> SOCKS5 127.0.0.1:12001
-> Phone 01
-> SIM
-> Internet
```

Egress IP khác host IP.

### Test 2

Tắt Wi-Fi Android.

Proxy vẫn hoạt động qua cellular.

### Test 3

Bật Wi-Fi Android.

Proxy egress vẫn là cellular.

### Test 4

Mất cellular.

Proxy phải fail, không chuyển qua Wi-Fi.

### Test 5

Rút/cắm lại phone.

Proxy tự khôi phục.

### Test 6

10 phones.

Mỗi direct endpoint route đúng phone.

### Test 7

Pool endpoint.

Connection mới được route theo strategy.

### Test 8

Rotate IP.

System ghi old/new IP và kết quả chính xác.

### Test 9

Duplicate egress.

System phát hiện 2 devices chung public IPv4.

### Test 10

LAN phone sử dụng Windows proxy.

Destination thấy mobile egress.

---

## 73. Testing Strategy

### Unit tests

- Domain rules.
- Node eligibility.
- Pool selection.
- Rotation state machine.
- Health aggregation.
- Quota policy.
- Error mapping.

### Integration tests

- SQLite repositories.
- CQRS handlers.
- API.
- ADB adapter với fake process runner.
- Agent protocol.

### End-to-end tests

- Real Android.
- Real SIM.
- SOCKS.
- HTTP CONNECT.
- DNS.
- IPv4.
- IPv6.
- Disconnect/reconnect.
- Rotation.

### Soak tests

- 24h.
- 72h.
- 7 days.

Quan sát:

- memory leak.
- socket leak.
- file descriptor leak.
- ADB instability.
- phone thermal.
- reconnect stability.

---

## 74. Deployment

### Windows

Installer phải:

- Install Windows Service.
- Install WPF app.
- Deploy compatible ADB platform tools hoặc yêu cầu configured path.
- Create ProgramData directories.
- Initialize SQLite.
- Configure service recovery.
- Configure firewall only when LAN mode explicitly enabled.

### Android

First registration:

```text
Enable USB debugging
Authorize Windows
Install Agent APK
Grant normal required permissions
Start foreground service
Register device
```

Optional advanced management:

```text
Device Owner
```

chỉ khi user chủ động chuẩn hóa fleet và hiểu provisioning.

Không bắt buộc root.

---

## 75. Update Strategy

### Windows

- Versioned migrations.
- Backward-compatible config.
- Pre-update backup.
- Service graceful shutdown.

### Android

- Track agent version.
- Compatibility range.
- Staged update.
- Rollback APK option.
- Không update tất cả phone cùng lúc nếu fleet lớn.

---

## 76. Development Roadmap

### Phase 0: Foundation

- .NET solution.
- Clean Architecture.
- CQRS.
- SQLite.
- WPF shell.
- Windows Service.
- Logging.
- Error model.

### Phase 1: ADB Device Management

- Discovery.
- Registration.
- Identity.
- Device state.
- Agent install/start/update.
- Port forwarding.

### Phase 2: Android Cellular Agent

- Foreground service.
- Cellular request.
- Cellular socket binding.
- Cellular DNS.
- Status/capabilities.

### Phase 3: Direct SOCKS5

- One device.
- SOCKS5 TCP.
- Remote DNS.
- IPv4/IPv6.
- Egress verification.
- Fail closed.

### Phase 4: Multi-device

- 10+ phones.
- Stable mapping.
- Device groups.
- Reconciliation.
- Health.

### Phase 5: Proxy Management

- Endpoint CRUD.
- Start/stop/restart.
- Session view.
- Traffic.
- Proxy test.

### Phase 6: IP Management

- Egress history.
- Duplicate detection.
- Rotation.
- Rotation policies.
- IP matrix.

### Phase 7: Pools

- Pool CRUD.
- Routing.
- Sticky.
- Failover.
- Eligibility.

### Phase 8: LAN Clients

- LAN bind.
- Authentication.
- ACL.
- Firewall integration.
- Phone/client testing.

### Phase 9: Monitoring and Recovery

- Incidents.
- Recovery ladder.
- Quota.
- Thermal.
- Metrics.

### Phase 10: HTTP CONNECT

- HTTP proxy support.
- Shared policy.

### Phase 11: Full Tunnel

- Wintun.
- DNS enforcement.
- IPv6 leak protection.
- UDP policy.

### Phase 12: Router Integration

- OpenWrt/gateway guide.
- Per-client routing policy.
- Optional dedicated gateway profile.

### Phase 13: Distributed Future

- PostgreSQL.
- Remote node manager.
- Central server.
- mTLS.
- Redis only if distributed coordination requires it.

---

## 77. Non-goals của version local đầu tiên

Không cần ngay:

- Public SaaS.
- Billing.
- Customer payment.
- Kubernetes.
- Multi-region control plane.
- Public cloud gateway.
- Browser fingerprint spoofing.
- GPS spoofing.
- Root-only automation.
- Exact city guarantee.

---

## 78. Các tính năng có thể thêm khi thành provider thực thụ

Nếu tương lai bán proxy cho bên khác:

```text
Organizations
Users
Roles
API keys
Proxy credentials
Plans
Subscriptions
Quota
Concurrent connection limits
Bandwidth limits
Usage accounting
Invoices
Billing
Customer audit
Abuse handling
Destination policy
Terms acceptance
Remote gateway
Regional node managers
```

Lúc này phải kiểm tra riêng điều khoản SIM/carrier và yêu cầu pháp lý tại Nhật trước khi mở dịch vụ cho bên thứ ba.

---

## 79. Kiến trúc tương lai khi remote

```text
Client
  |
  v
Public Proxy Gateway
  |
  v
Encrypted tunnel
  |
  v
Windows Node Manager
  |
  v
Android
  |
  v
SIM
  |
  v
Internet
```

CGNAT không ngăn mô hình này vì Windows/Android side có thể chủ động tạo outbound tunnel.

Nhưng đây không phải requirement của local version.

---

## 80. Quyết định kiến trúc cuối cùng

### Windows

Windows là:

```text
Server
Control Plane
Local Proxy Gateway
Device Orchestrator
Monitoring Node
Database Host
```

### Android

Android là:

```text
Mobile Egress Node
```

### Box

Box là:

```text
Physical USB aggregation only
```

### Database

```text
SQLite local
PostgreSQL distributed future
Redis only when actually distributed
```

### Architecture

```text
Clean Architecture
CQRS for control plane
Direct high-performance data plane
Modular Monolith
```

### Proxy

```text
SOCKS5 TCP first
HTTP CONNECT next
TUN/full tunnel later
```

### Network invariant

```text
Proxy traffic must exit through Android cellular network.
No silent fallback to Windows Internet.
No silent fallback to Android Wi-Fi.
```

---

## 81. Nguồn kỹ thuật và thông tin mạng

Các nguồn dưới đây được kiểm tra cho thiết kế này vào ngày 26/09/2026.

### Android

- Android ConnectivityManager API:
  https://developer.android.com/reference/android/net/ConnectivityManager

- Android NetworkRequest API:
  https://developer.android.com/reference/kotlin/android/net/NetworkRequest

- Android Debug Bridge:
  https://developer.android.com/tools/adb

ADB chính thức hỗ trợ port forwarding từ host port tới device port.

### .NET và Database

- .NET Support Policy:
  https://dotnet.microsoft.com/en-us/platform/support/policy

Tại thời điểm kiểm tra, .NET 10 là LTS và có thời hạn hỗ trợ đến tháng 11/2028.

- EF Core SQLite provider:
  https://learn.microsoft.com/en-us/ef/core/providers/sqlite/

### NTT Docomo

- IPv6 single-stack:
  https://www.docomo.ne.jp/info/news_release/2022/01/31_00.html

- sp-mode/APN:
  https://www.docomo.ne.jp/support/for_simfree/apn.html

- sp-mode Internet access IP ranges:
  https://www.docomo.ne.jp/service/developer/smart_phone/spmode/

Docomo công bố IPv6 single-stack với NAT64/DNS64 và 464XLAT cho IPv4 compatibility trên thiết bị hỗ trợ.

### Rakuten Mobile

- APN configuration:
  https://network.mobile.rakuten.co.jp/faq/detail/00001495/

Rakuten công bố APN protocol/PDP IPv4/IPv6.

### au/KDDI

- au SIM/APN setup:
  https://www.au.com/support/service/mobile/procedure/sim/auic/

### SoftBank

- Mobile/Pocket WiFi profile and IPv4/IPv6 support:
  https://www.softbank.jp/mobile/support/manual/data-com/a201ne/detail/67124/

- Example WAN IPv4/WAN IPv6 device information:
  https://www.softbank.jp/mobile/support/manual/data-com/pocket-wifi-5g-a101zt/detail/154552/

### IIJ

- IIJ Mobile business IP address options:
  https://www.iij.ad.jp/biz/iijmobile/menu.html

IIJ công bố rõ các lựa chọn NAT/private IPv4, dynamic global IPv4, fixed global IPv4 và global IPv6 tùy network type.

---

## 82. Kết luận

Hệ thống cần được xây dựng như một Mobile Proxy Management Platform chứ không phải một ADB launcher.

Mô hình cốt lõi:

```text
Windows Server
  |
  +-- Device Management
  +-- Proxy Management
  +-- Pool/Rotation
  +-- Monitoring
  +-- SQLite
  |
  v
ADB USB
  |
  v
Android Agent
  |
  v
Cellular Network
  |
  v
Carrier egress
  |
  v
Internet
```

Local listener không làm proxy vô nghĩa.

Ví dụ:

```text
127.0.0.1:12001
```

chỉ là điểm mà application kết nối vào.

IP mà dịch vụ bên ngoài nhìn thấy phải là:

```text
Android cellular egress IP
```

chứ không phải IP của mạng Windows.

Kiến trúc phải luôn kiểm chứng điều này bằng end-to-end route verification thay vì giả định.

Đây là invariant quan trọng nhất của toàn bộ dự án.
