# GUIDE Tích xu + Nạp đơn

- log tích xu của user nằm trong bảng `t_charge`

  - `orderId` là chuỗi 8 ký tự bất kỳ
  - `serverId` là id của game server mà nhân vật nạp
  - `rid` là id của nhân vật
  - `codeId` là gói nạp: 200, 500, 1000, 2000, 5000, 10000, 20000. các gói nạp có số hơn 1.000.000 là giftcode
  - `timeAdd` là thời điểm nạp

    > để fake đã nạp 1 gói chỉ cần insert 1 dòng vào bảng `t_charge` theo ý nghĩa như trên
    >
    > ```sql
    > INSERT INTO t_charge (orderId, serverId, rId, codeId, timeAdd) VALUES ('000L3IUY', 2, 210705, '5000', '2023-06-19 21:37:38');
    > ```

- api lấy thông tin nạp của nhân vật:

    ```csharp
    // lấy thông tin nạp trong ngày
    IDictionary<int, int> KTGlobal.GetPlayerTodayTotalCharge(KPlayer player)

    // lấy thông tin nạp trong một khoảng thời gian, format thời gian dạng YYYY-MM-DD HH:mm:SS
    IDictionary<int, int> KTGlobal.GetPlayerTotalRechargeByTime(KPlayer player, string startTime, string endTime)
    ```

    dictionary trả về có ý nghĩa: `key` là giá trị gói nạp, `value` là số lượt đã nạp gói này.

- api lưu lại record theo nhân vật

    ```csharp
       // gán, lấy value được lưu theo key, record được reset hàng ngày
       KPlayer.GetValueOfDailyRecore(int key)
       KPlayer.SetValueOfDailyRecore(int key, int value)

       // tương tự có các hàm lấy record theo tuần, tháng, vĩnh viễn
    ```

    `key` phải được khai báo trong file `Enumeration.cs`, enum `RecoreDef` để tránh dùng trùng của nhau

- phát thưởng cho nhân vật
  - dùng hàm tạo vật phẩm `ItemManager.CreateItem`
  - các phần thưởng được config theo các rương, dựa theo tài liệu để phát

- trao đổi gói tin server/client:
  - server
    - khai báo gói tin trong file `TCPCmdEnum.cs`
    - khai báo hàm handler trong file `TCPCmdHandler.cs`, hàm `ProcessCmd`
    - khai báo xử lý logic bằng cách tạo partial class `KT_TCPHandler_*.cs`
  - client
    - khai báo gói tin trong file `TCPCmdEnum.cs`
    - khai báo hàm handler trong file `TCPCmdHandler.cs`, hàm `ProcessServerCmd` và `PlayZone_Network.cs` hàm `ProcessNetCommand`

- UI client
  - yêu cầu dựng prefab prototype và làm hot-update
