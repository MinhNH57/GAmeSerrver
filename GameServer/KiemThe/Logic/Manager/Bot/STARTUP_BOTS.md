# BOT xuất hiện khi khởi động server

Server gọi KTBotManager.SpawnStartupBots sau khi tải xong bản đồ và tự tìm đường.
Mỗi tiến trình chỉ gọi tạo một lần. Khởi động lại sẽ tạo lại nhóm BOT trong bộ nhớ;
không có hồ sơ nhân vật hoặc tiến trình được lưu bởi tính năng này.

## Cấu hình

File nguồn: game-config/Config/Config/KT_Setting/StartupBots.xml.
Bản cấu hình chạy Debug đã được thêm tại GameServer/bin/Debug/Config/Config/KT_Setting/StartupBots.xml.
Khi triển khai sang thư mục khác, chép file vào Config/KT_Setting/StartupBots.xml
bên dưới thư mục tài nguyên Global.AbsoluteGameResPath (cùng nơi chứa NewbieVillages.xml).
Cần build và sử dụng GameServer.exe mới, rồi khởi động lại để áp dụng.

- Enabled="true": bật; "false": tắt.
- Count="50": số BOT mỗi lần khởi động, hợp lệ 0..500. Đây là giới hạn cấu hình,
  không phải khẳng định server chịu được 500 BOT.
- SpawnRadius="300": độ lệch tọa độ quanh điểm vào làng, hợp lệ 0..2000.

BOT được chia đều giữa các làng đang bật trong NewbieVillages.xml và có bản đồ đã tải.
Mỗi BOT thử tối đa 30 vị trí trong vùng; bỏ qua khi không tìm được ô đi được.
Cấu hình hiện tại có làng map 5, tâm (5090, 3270).
Thiếu file, tắt hoặc cấu hình lỗi sẽ không làm dừng khởi động server; xem log [StartupBots].
Sử dụng mẫu BOT GM hiện có (cấp 100, trang bị có sẵn); chưa thêm AI nhiệm vụ/hoạt động.
Chưa có lịch tăng 50 BOT mỗi 30 phút hoặc điều tiết theo tải/người chơi.

## Kiểm tra trong game

1. Khởi động bản build mới với Enabled=true, Count=5 để thử trước.
2. Tìm log [StartupBots] Created 5/5 BOTs.
3. Vào map 5 gần (5090,3270), kiểm tra BOT hiển thị và di chuyển.
4. Thoát vào lại bản đồ, xác nhận vẫn nhìn thấy BOT.
5. Khởi động lại, kiểm tra chỉ tạo lại số lượng cấu hình.
6. Đặt Enabled=false, khởi động lại và kiểm tra log Disabled, không tạo BOT khởi động.
