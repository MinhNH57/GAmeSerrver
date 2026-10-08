# 📊 Hướng Dẫn Hệ Thống Logging — Serilog + Grafana Loki

> Tài liệu nội bộ dành cho team dev. Cập nhật lần cuối: 2026-04-10

---

## 📌 Tổng Quan

Hệ thống logging của chúng ta sử dụng **Serilog** kết hợp **Grafana Loki** để ghi log tập trung (centralized logging).  
Mọi log từ tất cả Game Server sẽ được đẩy lên **1 nơi duy nhất** (Grafana Dashboard), giúp team có thể:

- Xem log **realtime** từ tất cả server cùng lúc
- **Tìm kiếm & lọc** log theo RoleID, ItemID, LogType, Server ID...
- Không cần SSH vào từng server để đọc file log nữa

### Cơ chế hoạt động: Dual-Write

```
LogManager.WriteLog(...)
        │
        ├──► Ghi file .txt (như cũ, giữ nguyên)
        │
        └──► Gửi lên Grafana Loki (qua Serilog)
               │
               └──► Xem trên Grafana Dashboard (http://45.125.237.201:3000)
```

**Nghĩa là:** Log vẫn được ghi ra file `.txt` như trước. Đồng thời, log cũng được gửi lên Loki. Nếu Loki chết, file log vẫn có — không mất dữ liệu.

---

## 🏗️ Kiến Trúc Module

Toàn bộ code logging nằm trong **`Tmsk.Contract/Logging/`** (shared library).  
Bất kỳ project nào reference `Tmsk.Contract` đều có thể sử dụng ngay.

```
Tmsk.Contract/
  └── Logging/
      ├── IStructuredLogger.cs      ← Interface (không phụ thuộc Serilog)
      ├── LokiLoggerBuilder.cs      ← Builder Pattern để cấu hình
      └── LokiLogger.cs             ← Implementation (Serilog + Loki)

GameServer/
  └── Tools/
      └── SerilogBootstrap.cs       ← Thin wrapper, chỉ gọi Builder
```

### Luồng khởi tạo

```
Program.cs → OnStartServer()
    │
    └── SerilogBootstrap.Initialize(serverId)
            │
            └── LokiLoggerBuilder
                    .ReadFromAppConfig()    ← Đọc URL, labels từ config
                    .WithServerId(serverId) ← Lấy ID server lúc runtime
                    .HookIntoLogManager()   ← Tự đăng ký callback
                    .Build()                ← Tạo LokiLogger
```

### Luồng shutdown

```
Program.cs → OnExitServer()
    │
    └── SerilogBootstrap.CloseAndFlush()
            │
            └── LokiLogger.Dispose()  ← Flush hết log còn lại, đóng kết nối
```

---

## ⚙️ Cấu Hình (`app.config`)

Mỗi Game Server có file `app.config` riêng.  
**Deploy nhiều server khác nhau chỉ cần sửa file này, KHÔNG cần build lại code.**

```xml
<appSettings>
    <!-- URL của Grafana Loki (trỏ đến center server) -->
    <add key="LokiUrl" value="http://45.125.237.201:3100" />

    <!-- Tên ứng dụng (phân biệt nếu có nhiều loại server) -->
    <add key="LokiAppName" value="kv1-gameserver" />

    <!-- Môi trường: dev / test / production -->
    <add key="LokiEnvironment" value="production" />
</appSettings>
```

### Giải thích từng field

| Key | Ý nghĩa | Ví dụ | Label trên Grafana |
|-----|---------|-------|-------------------|
| `LokiUrl` | Địa chỉ Loki endpoint | `http://45.125.237.201:3100` | — |
| `LokiAppName` | Loại ứng dụng | `kv1-gameserver` | `app=kv1-gameserver` |
| `LokiEnvironment` | Môi trường chạy | `production` | `environment=production` |
| *(tự động)* | ServerLine (lấy từ `AppConfig.xml`) | `1`, `2`, `47`... | `cluster=1` |
| *(tự động)* | ID server (lấy từ code) | `1`, `2`, `47`... | `server_id=1` |

> **Lưu ý:** `server_id` được code tự động lấy từ `GameManager.ServerId` lúc runtime. Không cần khai báo trong config.

---

## 📝 Cách Ghi Log Trong Code

### Ghi log bằng `WriteLog`

```csharp
// Ghi log — tự động vừa ghi file .txt VÀ đẩy lên Loki
LogManager.WriteLog(LogTypes.BaoThach, string.Format("{0}({1}) nhận được item({2})", client.RoleName, client.RoleID, reward.itemId));
```

- Log sẽ ghi ra file `.txt` **VÀ** đẩy lên Loki
- Trên Grafana, dùng `|=` để tìm text hoặc `|~` để tìm regex — rất đơn giản, không cần field

### Các LogTypes thường dùng

| LogType | Mục đích |
|---------|----------|
| `LogTypes.Info` | Thông tin chung |
| `LogTypes.Error` | Lỗi |
| `LogTypes.Exception` | Ngoại lệ |
| `LogTypes.Trade` | Giao dịch |
| `LogTypes.Rechage` | Nạp thẻ |
| `LogTypes.KNB` | KNB (tiền) |
| `LogTypes.Item` | Vật phẩm |
| `LogTypes.BaoThach` | Bảo Thạch |
| `LogTypes.LuckySpin` | Vòng quay may mắn |
| `LogTypes.Shop` | Shop |
| `LogTypes.Mail` | Mail |
| `LogTypes.Chat` | Chat |

---

## 🔍 Xem Log Trên Grafana

### Truy cập Dashboard

1. Mở trình duyệt → `http://45.125.237.201:3000`
2. Đăng nhập (mặc định: `admin` / `admin`)
3. Vào menu **Explore** (biểu tượng la bàn bên trái)
4. Chọn Data Source: **Loki**

### Các câu query hay dùng

```logql
# Xem tất cả log của app
{app="kv1-gameserver"}

# Lọc theo server cụ thể
{app="kv1-gameserver", server_id="1"}

# Lọc theo loại log
{app="kv1-gameserver", LogType="BaoThach"}

# Lọc theo cụm máy chủ
{app="kv1-gameserver", cluster="Cum1"}

# Lọc theo môi trường
{app="kv1-gameserver", environment="production"}

# Tìm kiếm các cụm text trong log
{app="kv1-gameserver"} |= "RoleID" |= "14201" (lọc ra các log có chữ RoleID và 14201)

# Tìm theo regex
{app="kv1-gameserver"} |~ "RoleID.*123" (lọc ra các log có chữ RoleID và 123)
```

---

## 🚀 Hướng Dẫn Setup Cho Project Mới

### Phần A — Setup Loki + Grafana trên Center Server

> Chỉ cần làm **1 lần duy nhất** trên center server.
> Nếu center server đã có Loki + Grafana chạy rồi thì **bỏ qua** phần này, nhảy xuống **Phần B**.

#### Yêu cầu

- Windows Server (hoặc Windows 10/11)
- Mở được port **3100** (Loki) và **3000** (Grafana)
- Khoảng **200MB** dung lượng trống

#### A1. Tải Loki

1. Truy cập: https://github.com/grafana/loki/releases
2. Tải file `loki-windows-amd64.exe.zip` (phiên bản mới nhất)
3. Giải nén vào thư mục, ví dụ: `D:\Loki\`

#### A2. Tạo file cấu hình Loki

Tạo file `D:\Loki\loki-config.yaml` với nội dung:

```yaml
auth_enabled: false

server:
  http_listen_port: 3100

common:
  path_prefix: ./loki-data
  storage:
    filesystem:
      chunks_directory: ./loki-data/chunks
      rules_directory: ./loki-data/rules
  replication_factor: 1
  ring:
    kvstore:
      store: inmemory

schema_config:
  configs:
    - from: 2020-10-24
      store: tsdb
      object_store: filesystem
      schema: v13
      index:
        prefix: index_
        period: 24h

limits_config:
  reject_old_samples: false
  ingestion_rate_mb: 16
  ingestion_burst_size_mb: 32

# Giữ log trong 30 ngày (tùy chỉnh)
table_manager:
  retention_deletes_enabled: true
  retention_period: 720h
```

#### A3. Khởi chạy Loki

Mở **CMD** hoặc **PowerShell** tại `D:\Loki\`:

```bat
loki-windows-amd64.exe -config.file=loki-config.yaml
```

Kiểm tra Loki đã chạy:
```
curl http://localhost:3100/ready
```
Nếu trả về `ready` → OK ✅

> **Mẹo:** Để Loki chạy nền khi tắt CMD, dùng **nssm** (Non-Sucking Service Manager) để đăng ký Loki thành Windows Service:
> ```bat
> nssm install Loki "D:\Loki\loki-windows-amd64.exe" "-config.file=D:\Loki\loki-config.yaml"
> nssm start Loki
> ```

#### A4. Tải Grafana

1. Truy cập: https://grafana.com/grafana/download?platform=windows
2. Tải bản **Standalone Windows Binaries** (`.zip`)
3. Giải nén vào, ví dụ: `D:\Grafana\`

#### A5. Khởi chạy Grafana

```bat
cd D:\Grafana\bin
grafana-server.exe
```

Grafana sẽ chạy trên port **3000**.

> Tương tự Loki, có thể đăng ký thành Windows Service bằng **nssm**:
> ```bat
> nssm install Grafana "D:\Grafana\bin\grafana-server.exe"
> nssm start Grafana
> ```

#### A6. Mở Firewall

Mở PowerShell **với quyền Admin**, chạy:

```powershell
netsh advfirewall firewall add rule name="Loki" dir=in action=allow protocol=TCP localport=3100
netsh advfirewall firewall add rule name="Grafana" dir=in action=allow protocol=TCP localport=3000
```

#### A7. Cấu hình Grafana kết nối Loki

1. Mở trình duyệt → `http://<IP-Center-Server>:3000`
2. Đăng nhập lần đầu: **admin** / **admin** (sẽ yêu cầu đổi mật khẩu)
3. Vào **Menu trái** → **Connections** → **Data Sources** → **Add data source**
4. Chọn **Loki**
5. Trong ô **URL** điền: `http://localhost:3100` (vì Loki và Grafana cùng máy)
6. Nhấn **Save & Test** → nếu thấy "Data source successfully connected" → OK ✅

#### Tóm tắt cấu trúc thư mục trên Center Server

```
D:\Loki\
  ├── loki-windows-amd64.exe
  ├── loki-config.yaml
  └── loki-data\          ← Dữ liệu log (tự tạo)

D:\Grafana\
  ├── bin\
  │   └── grafana-server.exe
  ├── conf\
  └── data\               ← Dữ liệu Grafana (dashboard, users...)
```

---

### Phần B — Setup Code Trong Project Mới

> Đảm bảo center server đã có Loki + Grafana chạy (Phần A) trước khi làm phần này.

#### B1. Đảm bảo project reference `Tmsk.Contract`

Project mới cần reference tới `Tmsk.Contract.csproj`. Nếu đã có thì bỏ qua bước này.

#### B2. Thêm NuGet Packages vào `Tmsk.Contract` (nếu chưa có)

Kiểm tra file `Tmsk.Contract/packages.config` đã có:

```xml
<package id="Serilog" version="3.1.1" targetFramework="net472" />
<package id="Serilog.Sinks.Grafana.Loki" version="8.3.0" targetFramework="net472" />
<package id="Serilog.Formatting.Compact" version="1.1.0" targetFramework="net472" />
<package id="Serilog.Sinks.PeriodicBatching" version="3.1.0" targetFramework="net472" />
```

#### B3. Thêm cấu hình vào `app.config` của project mới

```xml
<appSettings>
    <add key="LokiUrl" value="http://45.125.237.201:3100" />
    <add key="LokiAppName" value="ten-project-moi" />
    <add key="LokiEnvironment" value="production" />
</appSettings>
```

#### B4. Khởi tạo Logger khi app start

```csharp
using Server.Logging;

// Khi khởi động
var logger = new LokiLoggerBuilder()
    .ReadFromAppConfig()
    .WithServerId(serverId)       // Nếu có server ID không thì cho vào config
    .HookIntoLogManager()         // Nếu muốn tự động bắt log từ LogManager
    .EnableSelfLog()              // Bật debug mode (tắt khi ra production)
    .Build();

// Ghi log trực tiếp (không qua LogManager)
logger.Information("Server {ServerName} đã khởi động", "MyServer");
logger.Write("MyLogType", "Player {PlayerID} đăng nhập", 456);
```

#### B5. Đóng Logger khi app shutdown

```csharp
// Flush hết log còn lại trước khi tắt app
logger.Dispose();
```

#### B6. Kiểm tra trên Grafana

Mở Grafana → Explore → query:
```logql
{app="ten-project-moi"}
```

Nếu thấy log xuất hiện → setup thành công! 🎉

---

## 🛠️ Xử Lý Sự Cố

### Log không lên Grafana

1. **Kiểm tra LokiUrl** trong `app.config` đúng chưa
2. **Kiểm tra firewall**: port `3100` (Loki) và `3000` (Grafana) phải mở
3. **Bật SelfLog** bằng `.EnableSelfLog()` rồi xem console output để debug
4. **Kiểm tra Loki** có đang chạy không: `curl http://45.125.237.201:3100/ready`

### Log bị mất khi server crash

- Do Serilog gửi log theo batch (mỗi vài giây). Nếu server crash đột ngột, batch cuối có thể chưa gửi kịp.
- **Giải pháp:** File log `.txt` vẫn có đầy đủ (cơ chế Dual-Write)

### Console bị spam log Serilog debug

- Tắt `.EnableSelfLog()` trong `SerilogBootstrap.cs` hoặc code khởi tạo

---

## 📁 Danh Sách File Liên Quan

| File | Vị trí | Mục đích |
|------|--------|----------|
| `IStructuredLogger.cs` | `Tmsk.Contract/Logging/` | Interface logging |
| `LokiLoggerBuilder.cs` | `Tmsk.Contract/Logging/` | Builder cấu hình |
| `LokiLogger.cs` | `Tmsk.Contract/Logging/` | Implementation Serilog+Loki |
| `SerilogBootstrap.cs` | `GameServer/Tools/` | Thin wrapper cho GameServer |
| `LogManager.cs` | `Tmsk.Contract/Tools/` | Log manager gốc (ghi file .txt + delegate) |
| `app.config` | `GameServer/` | Cấu hình Loki URL, labels |
| `packages.config` | `Tmsk.Contract/` | NuGet packages Serilog |
