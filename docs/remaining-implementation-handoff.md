# Bàn giao hoàn thiện và nghiệm thu MobiFlux

## Phạm vi và điều kiện hoàn thành

Tài liệu dành cho người tiếp quản source trên Windows có Android/SIM thật. Không coi build, dữ liệu mẫu hoặc listener mở là bằng chứng cellular proxy hoạt động. Mọi biểu đồ phải lấy số đo API/SignalR; chưa có mẫu phải hiện trạng thái chờ, không nội suy thành số liệu thực.

Hiện đã có .NET control plane, WPF, React web, Android source, SOCKS5/CONNECT TCP, pool routing, sessions, ping/interfaces/netstat và test host. Chưa đủ bằng chứng để gọi sản phẩm hoàn tất. Roadmap gốc nằm tại docs/mobile-proxy-management-platform-plan.md; đối chiếu mục 70–78 trước khi nghiệm thu.

## 1. Chuẩn bị môi trường

1. Cài .NET SDK theo global.json, Node tương thích lockfile, Java 17, Android SDK Platform 36 và build-tools phù hợp Gradle.
2. Đặt ANDROID_HOME tới SDK; kiểm tra adb version, java -version và android/gradlew.bat --version.
3. Cắm ít nhất hai Android, bật USB debugging và chấp nhận khóa RSA. adb devices -l phải hiện device, không offline/unauthorized.
4. Mỗi máy có SIM và data hoạt động. Ghi Android version, carrier, APN, IPv4/IPv6, root/non-root; không đưa ICCID đầy đủ hay thông tin bí mật vào báo cáo.
5. Sao lưu DB đang dùng trước khi đổi schema; dùng DB riêng cho test phá hủy. Không chạy test vào dữ liệu vận hành.

## 2. Thứ tự triển khai còn thiếu

| Ưu tiên | Hạng mục | Điểm sửa chính | Tiêu chí nghiệm thu |
|---|---|---|---|
| P0 | Build và cài Android agent | android/app, Gradle wrapper | assembleDebug và testDebugUnitTest pass; APK cài/mở foreground service được trên Android 8 và Android mới |
| P0 | Cellular fail-closed thật | android CellularTunnelServer, CellularStreamBridge; Infrastructure ADB/reconciliation | Có Wi-Fi vẫn chỉ ra SIM; tắt data/rút USB/reboot làm kết nối thất bại, không đổi sang IP Windows/Wi-Fi |
| P0 | Quản trị LAN có xác thực | Service middleware/config, Web auth, Desktop connection | Hiện chỉ local. Muốn LAN phải thêm TLS, credential/token lưu an toàn, expiry/revoke, rate limit, audit; test 401/403/cross-origin/rebinding; không bỏ guard để mở port trần |
| P0 | Secrets và log redaction | Infrastructure secret storage, Service logging, Desktop export | Không trả/lưu plaintext password trong UI, URL, log hay export; test negative với chuỗi canary |
| P1 | Health incident và recovery | Infrastructure workers, Domain persistence, Service controllers | Incident có thời điểm mở/đóng, lý do; bounded retry/backoff; hỏng agent không tự đánh dấu ProxyReady; hiện cả web và WPF |
| P1 | Audit, diagnostics, retention | Application feature slices, SQLite repositories, Service API | Mutation có actor/time/resource/result/correlation; lọc và phân trang; retention có giới hạn; không lưu payload nhạy cảm |
| P1 | Backup/restore | Infrastructure storage, Service lifecycle, Desktop/Web settings | Backup SQLite nhất quán, kiểm tra integrity, restore khi service dừng; test DB lỗi, hết đĩa, quyền tệp; không ghi đè DB khi chưa xác nhận |
| P1 | CRUD và chi tiết thiết bị | Application Devices, Web App, WPF DashboardViewModel/MainWindow | Tên/enable/group/capability, telemetry và lịch sử; xóa khi đang được proxy/pool dùng phải chặn hoặc xác nhận rõ |
| P1 | Pool đầy đủ | Routing resolver, pool handlers, hai UI | Sticky TTL, RR/weighted/priority/failover/least-connections theo capability thực; test cô lập pool, concurrent load, thiết bị offline và sticky hết hạn |
| P1 | Phân trang đồng nhất | Query handlers, controllers, React tables, WPF collections | Devices/proxies/pools/rotation/audit/logs không tải vô hạn; page/pageSize có giới hạn; sort ổn định; reset trang khi đổi filter |
| P1 | Hoàn thiện toast WPF đã có overlay | Desktop view model, native notification host | Đã có overlay, nút đóng, ánh xạ lỗi và dedup lỗi nền. Còn queue/severity trực quan/chi tiết-copy mã lỗi và nghiệm thu native |
| P1 | Lỗi không phải JSON và crash | Desktop HTTP reader/App lifecycle; Web API/error boundary | Service tắt/timeout/HTML 502/JSON lỗi không lộ Failed to fetch hoặc stack; log kỹ thuật giữ correlation; không nuốt lỗi mất dữ liệu |
| P1 | Đồng bộ dashboard | Web dashboard/realtime, WPF TrafficChart | Cùng định nghĩa counter/rate/time window; reconnect/reset không tạo spike; ngừng nhận dữ liệu hiện stale; bounded history |
| P1 | Theme và accessibility native | Desktop Themes/Controls/MainWindow | Light mặc định; dark đủ tương phản; toàn control dùng style; 100/125/150/200% DPI không cắt dialog; keyboard/focus/screen reader |
| P2 | Đóng gói Windows | publish Service/Desktop, installer | Clean machine chạy được, service install/uninstall cần quyền rõ; không hardcode đường dẫn máy dev; signed artifact/checksum và rollback |
| P2 | Hiệu năng và soak | Proxy hot path, bounded queues, telemetry workers | Chạy 24–72 giờ, nhiều client và 2+ phone; theo dõi CPU/RAM/handles/thread/DB size; không leak sau reconnect |

Không hứa UDP/full tunnel/router/distributed đã có. Các phần này thuộc phase sau/non-goals bản local; cần đặc tả và nghiệm thu riêng nếu đưa vào release.

## 3. Lệnh kiểm tra host

Chạy từ repository root:

```powershell
dotnet restore MobiFlux.sln --configfile NuGet.Config
dotnet build MobiFlux.sln --no-restore
dotnet run --project tests/MobiFlux.Tests --no-build
pwsh -File tests/Test-ControlPlane.ps1
npm --prefix src/MobiFlux.Web ci
npm --prefix src/MobiFlux.Web run build
npm --prefix src/MobiFlux.Web run lint
node src/MobiFlux.Web/check-notifications.mjs
android/gradlew.bat -p android testDebugUnitTest assembleDebug
adb devices -l
```

## 4. Nghiệm thu thiết bị theo ca

1. Cài APK build ra từ android/app/build/outputs/apk/debug. Mở app, bật agent, ghi nhận foreground notification và cellular capability.
2. Qua web/WPF quét ADB; chỉ coi sẵn sàng khi agent báo cellular. Tạo SOCKS5 riêng cho từng phone và HTTP CONNECT.
3. Từ client dùng curl --proxy với endpoint thật truy cập hai dịch vụ HTTPS xác định IP. Đối chiếu SIM, không dùng kết quả ping Windows để chứng minh egress.
4. Bật Wi-Fi trên phone và giữ data. Lặp lại; DNS/socket vẫn qua cellular. Kiểm tra IPv4/IPv6 và hostname do DNS cellular trả về.
5. Tắt cellular, rút USB, kill agent, reboot phone, cho Windows sleep/wake. Request mới phải fail-closed; request cũ kết thúc có closeReason. Phục hồi không sinh listener trùng hoặc vòng retry vô hạn.
6. Hai phone vào pool. Chạy đủ request để quan sát weight/priority; ngắt từng phone và xác minh route eligibility/sticky theo chính sách, không chọn thiết bị chết.
7. Đổi IP với thiết bị hỗ trợ; ghi IP trước/sau và duration. Carrier giữ IP phải báo không đổi, không báo thành công giả.
8. LAN proxy phải có xác thực và ACL. Sai mật khẩu/IP ngoài ACL phải bị từ chối. Quản trị LAN vẫn bị chặn cho tới khi P0 auth/TLS hoàn tất.
9. Chạy nhiều kết nối, gửi half-close, timeout, malformed SOCKS/CONNECT, cancel và shutdown. Counter không âm, không giữ active connection sau close.
10. Lưu log đã che bí mật, build version, model/OS/carrier, bước tái hiện, IP đã che, timestamp và kết quả từng ca. Fail ca nào mở issue ca đó, không đánh dấu release đạt.

## 5. Nghiệm thu giao diện

- Web: 360/390/768/1440/1920 px; sidebar thu/mở, refresh giữ trạng thái; mọi route trực tiếp không 404; không overflow toàn trang.
- WPF: 900x600 tới màn hình lớn, nhiều DPI; tất cả navigation, CRUD, confirmation, loading/empty/error, focus và tab order.
- Tắt service khi hai UI đang mở: thông báo tiếng Việt có hướng khắc phục, không dữ liệu giả và không success toast sai.
- Thêm 0/1/1000 bản ghi: lọc/phân trang/sort/export nhất quán, không treo UI; export không chứa secrets.
- Theme sáng/tối: kiểm tra text/table/dialog/chart/tooltip/disabled/focus; không dùng màu làm tín hiệu duy nhất.

## 6. Tiêu chuẩn phát hành

Chỉ phát hành sau khi P0/P1 cần cho scope đã đóng, test host + hardware pass, backup/restore đã thử và release chạy trên máy sạch. Người nghiệm thu ký danh sách pass/fail kèm artifact, không dựa vào mô tả trong README cũ. Installer/signing, auth LAN, incident/audit/retention và soak vẫn là công việc thực, không chỉ là tài liệu.

## Cập nhật kiểm tra host

- Build solution, web build/lint, control-plane smoke và network error checks đã pass ở đợt hiện tại.
- WPF đã có lọc lịch sử theo proxy/điện thoại, reset về trang 1 khi áp dụng/xóa bộ lọc; cần nghiệm thu binding với dữ liệu phiên thật.
- Default light trên web và desktop. Không thêm seed devices/proxy/traffic mẫu vào môi trường vận hành.
- Đã chia vendor bundle: entry 102,06 kB, Material 332,96 kB, React 206,76 kB; build hết cảnh báo chunk lớn. Tổng tài nguyên không giảm tương ứng; chưa có benchmark hoặc soak chứng minh performance.

### Bằng chứng kiểm tra trình duyệt

- Context sạch: theme preference null, body rgb(250,250,250), dashboard render và nhận mẫu SignalR thật.
- Các route devices/proxies/pools/rotation/sessions/networking/settings render qua navigation; viewport thực tế 500 px không overflow toàn trang.
- Công cụ resize yêu cầu 390 px nhưng Chrome trả innerWidth 500; không coi đây là nghiệm thu 390 px.
- JSON hỏng trả 400 với envelope/correlation; malformed GUID filter trả 400; correlation header quá dài bị thay mới. Smoke test pass.
- WPF build pass; chưa có nghiệm thu màn hình native hoặc Android cellular thật.

### Backup và native regression bổ sung

- POST /api/v1/mobiflux/maintenance/backup dùng SQLite BackupDatabase, stream xuống client, Cache-Control no-store, semaphore và cleanup tệp tạm.
- Web settings có nút tải backup. Build/lint pass; chưa nghiệm thu download qua browser hoặc desktop.
- Smoke tải backup HTTP, mở read-only, PRAGMA integrity_check và ba bảng bắt buộc pass. Chưa chứng minh phục hồi dữ liệu có fixtures; restore chưa triển khai.
- tests/Test-DesktopStartup.ps1 pass: startup, chọn chín navigation items, đo sidebar thu/mở bằng UI Automation.
- Còn nguyên các mục chưa làm: LAN auth/TLS, audit/retention, restore an toàn, installer/signing, Android SDK/APK và SIM egress/USB failure/soak.
