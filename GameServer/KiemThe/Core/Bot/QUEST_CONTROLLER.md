# Khung Auto nhiệm vụ BOT

BotQuestController được tạo sẵn trong mỗi KTBot và mặc định Idle.
KTBotTimerManager gọi Tick và bỏ qua đi ngẫu nhiên khi controller đang chạy.
Xóa timer BOT sẽ Stop controller và hủy các hành động do adapter quản lý.

Để nối một nhiệm vụ thật:
1. Viết lớp triển khai IBotQuestActions.
2. ExecuteStep nhận BOT, ID nhiệm vụ và trạng thái hiện tại. Mỗi lần gọi xử lý ngắn,
   trả Running khi chờ, Completed khi server xác nhận bước thành công, Failed khi lỗi.
3. SelectQuest kiểm tra nhiệm vụ được chỉ định có hợp lệ; không tự chọn ID khác.
4. AcceptQuest/SubmitQuest phải dùng luật nhiệm vụ server, chống nhận hoặc thưởng lặp.
5. Cancel dừng di chuyển/chiến đấu do adapter bắt đầu, không xóa nhiệm vụ của server.
6. Trên luồng xử lý BOT, gọi:

```csharp
bot.QuestController.Start(questId, questActions, TimeSpan.FromMinutes(5));
```

Quan sát State, QuestId, IsRunning, LastError. Stop() để dừng.
Hoàn thành một nhiệm vụ sẽ dừng, không tự nhận tiếp. Chết sẽ tạm dừng bộ đếm timeout;
sau hồi sinh tiếp tục bước đang làm. Adapter phải kiểm tra lại vị trí/mục tiêu khi tiếp tục.
Không gọi Start/Stop đồng thời với Tick từ luồng GM/admin khác; cần chuyển yêu cầu về scheduler.
Khung chưa có logic nhiệm vụ thực, tìm đường, chiến đấu, hồi sinh hoặc lưu database.

## Main-quest adapter

BotMainQuestActions is now wired for startup BOTs when MainQuestsEnabled=true.
Read MAIN_QUEST_BOTS.md for exact supported objectives and limits. In particular,
BOT rewards and durable quest storage are not implemented.
