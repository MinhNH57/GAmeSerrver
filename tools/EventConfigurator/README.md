# KV1 Event Configurator

Công cụ web nội bộ dùng để quản lý và cấu hình nhanh các Event trong game Kiếm Thế 1, hỗ trợ thiết lập thời gian hiển thị nút sự kiện và hiệu chỉnh trực tiếp phần thưởng, nhiệm vụ, hộp quà mà không cần thông qua thao tác chỉnh sửa XML/JSON thủ công phức tạp.

## 🚀 Tính Năng Chính
- **Quản lý Global Time**: Tuỳ chỉnh lịch trình cho toàn bộ Cụm Server (`EventsTimeConfig.xml`).
- **Event Ghép (EventCompose)**: Cấu hình `MonthlyCompose.json` (tỷ lệ rớt, vật phẩm ghép) và `EventCompose.xml` (công thức, tiêu hao).
- **Ngũ Phúc Lâm Môn**: Giới hạn sự kiện quay ô (`SlotMachine.xml`).
- **Recharge Events (Nạp Thẻ)**: Sửa tham số mốc nạp `RechargeEvent_OldServer.xml`.
- **Nạp Đơn & Tích Lũy Chuyển Xu**: Chỉnh sửa vật phẩm ưu đãi mốc điểm trong `RechargeAward.xml`.
- **Bách Bảo Rương & Thiên Tầng Tháp**: Chỉnh sửa tỷ lệ, thưởng tầng trong `KTSeashellCircle-EVENT.xml` và `FlowerTower.xml`.
- **Ưu Đãi Display**: Cấu hình hiển thị Giao diện Ưu đãi `UuDaiDisplay.xml`.
- **Sửa Box Quà Modal Tích Hợp**: Trace ID `ItemBox` -> `ItemID` (script_items_recharge) -> `IDBox` (RandomBoxes packages & milestones) và chỉnh sửa trực tiếp thông qua Giao diện Popup cho cả cụm thường lẫn Server 38.
- **Tính năng Tìm kiếm nhanh**: Ô lọc dữ liệu tại mỗi Tab giúp tìm ID/Tên cực nhanh.
- **Giao diện Premium White**: Thiết kế tối giản, hiện đại, độ tương phản cao, tối ưu cho làm việc lâu dài.
- **Export Mode Toggle**: Sinh file chỉnh sửa lưu riêng ở folder `/exports` thay vì lưu đè trực tiếp vào code server.

## 🛠 Hướng Dẫn Cài Đặt (Setup trên Máy Khác)

### Yêu Cầu Hệ Thống
1. **Node.js**: Phiên bản v14 hoặc cao hơn. Khuyến nghị bản LTS ([Tải Node.js tại đây](https://nodejs.org/)).
2. **NPM**: Đi kèm với Node.js.

### Quy Trình Cài Đặt

1. **Copy Thư Mục Tool**
   Đầu tiên, sao chép toàn bộ thư mục `tools/EventConfigurator` từ project hiện tại sang thiết bị/máy chủ mới. Tuy nhiên, lưu ý giữ nguyên cả cấu trúc tương đối với `game-config` nếu máy ảo nằm cùng source. Bằng không, cần điều chỉnh đường dẫn Absolute (Xem bước Cấu Hình bên dưới).

2. **Cài Đặt Package (Phụ thuộc)**
   Mở Command Prompt/Terminal ở thư mục `EventConfigurator` (nơi chứa file `package.json`), và chạy:
   ```bash
   npm install
   ```
   Hệ thống sẽ tải toàn bộ libs cần thiết (như: `express`, `xml2js`, `cors`).

3. **Cấu Hình Đường Dẫn (NẾU CẦN)**
   Mặc định, server Node.js đang trỏ đường dẫn tới `../../game-config/Config/Config` theo cấu trúc Folder gốc.
   Nếu đem máy khác mà Source Config Server nằm ở thư mục hoàn toàn khác (ví dụ `D:\KT_Server\Config`), bạn hãy mở file `server.js` và cập nhật dòng 13:
   ```javascript
   // Sửa đường dẫn trỏ tới thư mục 'Config' của game server nếu khác
   const kv1ConfigPath = path.join(__dirname, '../../game-config/Config/Config');
   ```

4. **Khởi Động Nhanh (One-click)**
   Để thuận tiện, hãy sử dụng file khởi động tự động ở thư mục gốc project:
   - File: `Start_EventConfigurator.bat` (nằm cạnh thư mục `tools`).
   - Tác dụng: Tự động bật Server CMD và mở trình duyệt web truy cập [http://localhost:3000](http://localhost:3000).

   *Hoặc chạy thủ công:*
   Mở terminal tại `tools/EventConfigurator` và gõ `npm start`.

5. **Truy Cập Giao Diện**
   Mở trình duyệt Web và truy cập: [http://localhost:3000](http://localhost:3000)

6. **Di Chuyển Sang Máy Khác (Portability)**
   Công cụ đã được thiết lập để có thể mang đi bất cứ đâu:
   - Chạy file `package_tool.bat` trong thư mục `tools/EventConfigurator` để tạo thư mục `Release_EventConfigurator`.
   - Copy toàn bộ thư mục `Release_EventConfigurator` sang máy mới.
   - Tại máy mới:
     1. Mở file `.env` và sửa đường dẫn `KV1_CONFIG_PATH` trỏ đến thư mục Config của game trên máy đó.
     2. Chạy `Start_Tool.bat` để tự động cài đặt thư viện và khởi động.

---
*Lưu ý: Công cụ tự viết đè (overwrite) trực tiếp file Config. Để đảm bảo an toàn nếu chưa tự tin, có thể bật **Chế độ Xuất file (Export)** trên góc màn hình UI.*
