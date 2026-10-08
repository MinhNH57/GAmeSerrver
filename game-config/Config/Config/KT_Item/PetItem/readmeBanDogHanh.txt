•Bạn đồng hành
Server: Các file xml liên quan trong config/PetItem
PetInfo.xml là file config các file cấu hình liên quan đến bạn đồng hành, các file xml còn lại là config trang bị của pet và config pet.
Chi tiết: Bạn đồng hành coi như 1 trang bị đeo lên người chơi nhưng sẽ không hiển thị trong túi đồ mà hiển thị tại UI dành riêng cho bạn đồng hành.
Mọi bạn đồng hành sẽ được đánh dấu trong bagIndex là -1, DetailType của bạn đồng hành = 20 và cũng là ID vị trí trang bị bạn đồng hành trên player. Các đồ của bạn đồng hành cũng được tạo với ID từ 21->30 
Các logic liên quan : 
Tạo ra 1 bạn đồng hành (CreateItem trong ItemManager) sẽ khởi tạo lưu lại các chỉ số quan trọng của bạn đồng hành trong hàm tạo. 
Số kỹ năng, chi tiết kỹ năng lưu và chỉ số cơ bản của bạn đồng hành sẽ lưu trong PetPropety. Cấp độ là frog_level của đồ các chỉ số còn lại được lưu vào OtherParam của chính nó (lưu nhiều chỉ số 1 nơi thì 
được ngăn cách bởi _).
PetManager.cs quản lý việc tạo ra bạn đồng hành, xóa bạn đồng hành,các gói tin gửi về client lúc bắt bạn đồng hành, thu hồi và tăng giảm các chỉ số như kinh nghiệm, cấp độ, lĩnh ngộ, thân mật ...
PetItemManager.cs quản lý tất cả các đồ liên quan đến bạn đồng hành như Tinh phách tán, bồ đề quả, sách kinh nghiệm, … Tất cả các chỉ số phần này đều ở trong PetInfo. 
PetGiftManager.cs quản lý việc tặng quà cho bạn đồng hành làm tương tự giống cường hóa huyền tinh file config loại quà, giá trị quà cũng trong PetInfo.xml.
Khi trang bị bạn đồng hành sẽ khởi tạo tại KItem như 1 đồ bình thường attack các chỉ số cũng như kĩ năng của bạn đồng hành trong hàm InitPetPropety(). 
Khi trang bị thành công sẽ check các đồ của đồng hành có đang được deo không sẽ kích hoạt theo. Tương tự như đeo, khi tháo bạn đồng cũng sẽ ko kích hoạt trang bị bạn đồng hành và xóa hết hiệu ứng kĩ năng đi.
Trang bị bạn đồng hành khi ko có hay có bạn đồng hành đều đeo được nhưng chỉ kích hoạt khi có bạn đồng hành đang được gọi.

Đã commet chi tiết trong tất cả code liên quan.

Các gói tin giao tiếp với client 
        CMD_SPR_LEVELUPPET = 50302,
        CMD_SPR_REMOVEPET = 50303,
        CMD_SPR_USEITEMTAKEPET = 50304,
        CMD_SPR_STARTTAKEPET = 50305,
        CMD_SPR_SENDGIFTPET = 50306,
Client: UI tương tự các UI khác đã commet chi tiết trên code
-Lỗi Rơi sách kinh nghiệm khi train quái trên cấp 115 đang bị lỗi như quái tinh anh thủ lĩnh của sự kiện (chưa check chỉ quái map dã ngoại mới được rơi)
•Điểm năng động
File config DayActivityScore.xml
Script : DayActivity.cs, DayActivityManager.cs
Đã comment chi tiết trong các file code.
