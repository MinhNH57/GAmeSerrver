# Context Để Bàn Giao Quản Trị Hệ Thống EventConfigurator cho AI (AI_CONTEXT)
# Context for AI Agent Handover of EventConfigurator System

> **Lưu ý dành cho AI Agent tương lai:** Hãy đọc kỹ toàn bộ file này trước khi tiến hành update UI, thêm Tab mới hoặc sửa lại Logic Backend của công cụ KV1 Event Configurator.
> **Note for future AI Agents:** Read this file thoroughly before updating UI, adding new Tabs, or modifying Backend Logic.

## 1. Kiến trúc Tổng Quan / Architecture Overview
Đây là một SPA (Single Page Application) nguyên bản kết hợp kiến trúc Client-Server siêu mỏng:
- **Backend (`server.js`)**: Viết bằng `Express.js`, thuần túy làm Restful API đóng vai trò đọc (Get) JSON/XML từ source game, chuyển `xml2js` thành JSON object rồi đẩy cho FE. Khi lưu thì nhận JSON FE trả về, build ngược `Builder` về XML.
- **Frontend (`public/index.html` + `public/app.js` + `public/style.css`)**: Sử dụng kiến trúc hiện đại:
    - `HTML thuần` kết hợp `Bootstrap 5`.
    - `Custom CSS (style.css)`: Triển khai hệ thống Design Tokens, phong cách **Premium White (Light Mode)** với độ tương phản cao. Bao gồm rule `min-width` cho `.form-select` trong bảng và `text-overflow: ellipsis` cho input/select trong table.
    - `Vanilla JS`: Quản lý logic, Dom Update (backticks) và tính năng **Tìm kiếm nhanh (Filter)** quét cả giá trị input.

## 2. Global State Map & File Cấu Hình Target / Config File Map
Khi xử lý, UI sẽ gửi JSON body sang API. Tùy trạng thái toggle `isExport`, API save đè gốc (tại `../../game-config/Config...`) hay lưu riêng ra `tools/EventConfigurator/exports`.

Bảng Map Chức Năng - Tên File Game:
1. `EventsTimeConfig.xml`: Cấu trúc danh sách thời gian Server + nút giao diện bật Event chung. Đã hỗ trợ **Event Key Dropdown** thông minh chứa thêm các key cho Server Mới (như `UuDaiDisplay_NewServer`).
2. `MonthlyCompose.json` + `EventCompose.xml`: Thông số sự kiện Ghép. FE đã bổ sung các trường Global (Token, Price, LevelLimit, BonusItem, BonusMaxDaily...), **Bảng tương tác** cho `MaterialRate` và `DropRate`. Phần cấu hình XML hỗ trợ sửa **RecordSeed toàn cục**, thêm/xoá **Điều kiện Phi Phong (MantleRequirement)** mảng đa phần tử. Phần Công thức ghép (EventCompose.xml) đã bổ sung **Toolbar Lọc+Replace nhanh**: lọc card theo Group (Nông dân/Năng động/Sự kiện), LimitType (None/Day/Week/Month/Lifetime) và text; hàm `replaceComposeXmlField()` thay thế hàng loạt 9 loại field (Output, OutputNumber, LimitValue, LimitType, Group, material_id, material_number, fee_id, fee_number) chỉ trên các công thức đang hiển thị sau lọc.
3. `SlotMachine.xml`: Giới hạn mốc thời gian Ngũ Phúc Lâm Môn.
4. `script_items_recharge.json` + `RechargeEvent_OldServer.xml`: Lưu dữ liệu vật phẩm nạp thẻ hiển thị & Cấu hình các gói Quà Ưu đãi nạp thẻ Cấp VIP.
5. `CheckIn.xml`: Sửa mốc điểm danh Check-in theo tháng. Đã bổ sung chọn đơn vị tiền (**Bạc/KNB**) cho cột Price (Money attr) với `min-width: 80px`.
6. `KTSeashellCircle-EVENT.xml`: Sự Kiện Bách Bảo Rương. Đã refactor FE thành dạng **Tabs** (Cells, TurnRate, Exchange/Bet) và hỗ trợ **Box Editor** cho TreasureID. Nhãn ID đã Việt hóa. Cột "Tên Ô" dùng `min-width: 220px` để hiển thị đầy đủ tên vật phẩm.
7. `FlowerTower.xml`: Thiên Tầng Tháp. Đã nâng cấp toàn diện: **Thêm/Sửa/Xóa nhiệm vụ** từng tầng, chọn nhanh `EventType` (cột dùng `min-width: 300px`), tích hợp **Box Editor** cho RewardID. Lưu ý nhiệm vụ nạp là cộng dồn.
8. `RechargeAward.xml`: Ưu đãi Nạp đơn (`DayRecharge`) & Cột mốc tích lũy Chuyển Xu (`AccumulateRecharge`). Đã redesign sang **dạng bảng (table)** với các cột ID/KNB/ItemBox/BonusItem thay vì inline card. (Lưu ý node `ItemBox` của event này).
9. `UuDaiDisplay.xml`: Hiển thị gói ưu đãi màn hình Home. (Đã thiết kế lại theo dạng **List + Modal Editor**, phân làm 3 cột dựa theo thuộc tính Group: 0=Hàng ngày, 1=Giới hạn, 2=Tài Nguyên).
10. `ExchangeTicketEvent.xml`: Event Chuyển Xu Nhận Quà (Mới).
11. `VongQuayMayMan.xml`: Tân Niên Bí Bảo (Lucky Spin) - Đã cấu trúc lại giao diện lưới chia Rates và Accumulate side-by-side, bảo toàn block comment chứa cấu trúc mẫu của file XML gốc.
12. `ServerMergeRewards.xml`: Event Liên Thông Server - Hiển thị thẻ card linh hoạt, bảo toàn nguyên vẹn block text chú thích chứa các gói định dạng mẫu.
13. `BattlePassSeason.xml`: Battle Pass - Hệ thống cấu hình quy mô cực lớn chia làm 4 tab phụ (Chung, Cấp Độ, Nhiệm Vụ, Gói Nạp). Quản lý cấu trúc UI phức tạp nhưng vẫn giữ hơn 40 dòng template comments.

## 3. Logic Tra Cứu (Map) Quà Box Hộp Nạp (Box Editor) / Box Editor Logic
Điểm đáng chú ý nhất khiến app này phức tạp là **Modal Edit Box** (RandomBoxes) được tích hợp trong tính năng **Ưu Đãi & Các Event Tích Lũy**.

Dòng Chảy Data:
1. XML `RechargeAward.xml` có thuộc tính `ItemBox="1"`.
2. UI đọc List `script_items_recharge.json` tìm Item có `ItemID == 1`.
3. Object Item ID nọ chứa `ItemValue="200293"` (Cho Normal Server) và có thể mang `Override: {"38": { "ItemValue": "201557" } }` (Cho Server Mới Cụm 38).
4. Cái Value `200293` / `201557` này chính là `IDBox` của 1 Group gói quà Random khai báo ở `KT_Item/RandomBox/randomboxes_tichxu_packages.json` (Gói đơn) hoặc `randomboxes_tichxu_milestones.json` (Gói Tích lũy).
5. Khi Update Modal ở Frontend, nó lưu mảng Item mới vào JSON Memory tạm rồi Submit nguyên Json tổng về endpoint api post `/api/recharge/randomboxes-packages` hoặc `-milestones`.

## 4. Xử lý Ngày Tháng (Date-Time Picker) / Date-Time Handling
App đã chuyển từ nhập text thủ công sang dùng `input type="datetime-local"`.
- **Logic format FE**:
    - `formatToInput(str)`: Chuyển `yyyy-MM-dd HH:mm:ss` -> `yyyy-MM-ddTHH:mm:ss` (để hiển thị trên input HTML5).
    - `formatToXml(str)`: Chuyển ngược `yyyy-MM-ddTHH:mm:ss` -> `yyyy-MM-dd HH:mm:ss` (để lưu vào XML/JSON game).
- **Lưu ý**: Khi render input ngày tháng, luôn kèm `step="1"` để trình duyệt cho phép chọn đến hàng Giây.

## 5. Hệ thống Tìm kiếm & Lọc (Search Logic) / Search & Filter System
Để hỗ trợ quản lý dữ liệu lớn, app tích hợp ô tìm kiếm tại mỗi Tab:
- **UI**: `<input onkeyup="filterSection('container-id', this.value)">`.
- **Logic (`app.js`)**:
    - Hàm `getSearchableText(el)`: Thu thập **tất cả text tìm kiếm được** từ một element, bao gồm: text hiển thị (labels, spans), **giá trị input/textarea**, và **text option đang chọn trong select**. Đây là điểm khác biệt quan trọng so với `innerText` thuần — cho phép tìm kiếm theo giá trị số (ID, KNB, ItemBox...) trong ô nhập liệu.
    - Hàm `filterSection(sectionId, query)`: Quét `tbody tr`, `.list-group-item` và `.card` bên trong container target, sử dụng `getSearchableText()` để so khớp. Tự động reset hiển thị khi query trống.
- **Các tab có search bar**: Cấu Hình Thời Gian Chung, Event ghép (cả phần MaterialRate/DropRate lẫn Công thức ghép), Bách Bảo Rương, Thiên Tầng Tháp, Quản lý UDND & TLCX, Giao diện Ưu Đãi.
- **Lọc nâng cao Công thức ghép**: Tab Event ghép - phần EventCompose.xml có thêm toolbar lọc theo Group/LimitType + ô search text. Mỗi card công thức được gắn `data-group` và `data-limittype` để hàm `filterComposeXml()` ẩn/hiện DOM trực tiếp không re-render. Bộ đếm `compose-filter-count` cập nhật động.

## 6. Hướng Dẫn Thêm Event TAB Mới (Dành Cho Agent) / New Tab Guide
Nếu người dùng cung cấp 1 file XML Event mới `FooEvent.xml` và yêu cầu tạo giao diện.
Trình tự thực hiện bắt buộc:

1. **Backend (`server.js`)**: Tạo 2 endpoint GET `/api/fooevent` (Đọc `FooEvent.xml` bằng `readXml()`) và POST `/api/fooevent` (Lưu data nhận từ body bằng `writeXml()`).
2. **Frontend `index.html`**:
   - Nhân bản 1 dòng `Sidebar`: `<a href="#foo" onclick="switchTab('foo')">Tab Event Mới Foo</a>`.
   - Tạo khung bao Body tương ứng `<div id="foo-section" class="tab-section d-none">`.
     Bên trong có 1 cái Container rỗng `id="foo-form"` để chứa Input. 
     Nút Save gọi `saveFoo()`.
3. **Frontend `app.js`**:
   - Khai báo state variable `let fooData = null;`.
   - Hàm tải Data ban đầu: `async function loadFoo()` -> Get data. Gọi hàm `renderFoo()`. Đẩy `loadFoo()` vào danh sách gọi sẵn dưới cùng (nếu muốn pre-fetch) hoặc cơ chế `switchTab` để bấm mới load. LƯU Ý, đang chuộng Pre-catch tất cả ở cuối luồng logic file `app.js` cho tiện.
   - Hàm HTML Template Render DOM: `function renderFoo(data)` lặp file data tạo `<input type="datetime-local" step="1" class="form-control foo-attr" data-idx="X" data-attr="Y" value="${formatToInput(data)}">`.
   - Hàm Lưu data: `function saveFoo()` -> Dùng `document.querySelectorAll('.foo-attr').forEach` đọc Dom ghi lại vào biến `fooData`. Fetch POST gọi API là xong. Tại vòng lặp gather data, nhớ kiểm tra: `if (input.type === 'datetime-local') val = formatToXml(val);`.

- **Lưu Ý Chống Bug (Pitfalls)**:
- **`isExport` flag**: Mọi API post phải xử lý bóc `req.body.isExport`, `delete req.body.isExport` và phân luồng path ghi thành `/exports` thay vì gốc. 
- **Array Single Element trong `xml2js`**: Module này hay tự đưa Object con vào Array dù Element XML gốc chỉ có một. (Ví dụ `<Event>` sẽ được map thành `[ {$: { attr }} ]`). Nhớ parse Index 0 `Event[0]` khi DOM Node bị lồng.
- **Mô hình Bảng Tương Tác cho JSON**: Đối với các file JSON có cấu trúc Map (như `MaterialRate` trong `MonthlyCompose.json`), nên dùng bảng FE để CRUD từng dòng thay vì cho sửa Textarea. Khi save, convert mảng Dom về lại Object key-value.
- **Xử lý `script_items_recharge.json`**: Cẩn thận trường thuộc tính mảng `Override["38"]`. Có những file JSON sẽ khai báo Override này nhưng nhiều file thì không tồn tại `Override`.
- **Event Key Dropdown**: Sử dụng `<input list="event-keys-list">` phối hợp với `PREDEFINED_EVENT_KEYS` trong `app.js` để người dùng chọn nhanh key event mà không cần gõ.
- **Thứ Tự Khai Báo Bootstrap Tab Modal**: Form edit randombox là một Modal popup trôi lơ lửng, nếu không tìm thấy DOM Element, lỗi sẽ hiện Trắng Popup. Do đó Event Handler Delete Index cũng phải cẩn trọng không vượt ranh giới mảng `box.Items.splice(idx, 1)`.
- **Chiều rộng cột trong bảng**: Sử dụng `min-width` thay vì `width` cố định cho các cột có nội dung dài (tên vật phẩm, EventType select). Tránh inline `style="width:Xpx"` quá nhỏ trên input — ưu tiên để table tự co giãn hoặc dùng `min-width` đủ lớn. CSS `style.css` đã có rule `.table .form-select { min-width: 100px }` hỗ trợ toàn cục.
- **Search filter quét input value**: Hàm `filterSection` dùng `getSearchableText()` để quét cả `input.value` và `select option text`, không chỉ `innerText`. Khi thêm UI mới, đảm bảo cấu trúc DOM phù hợp (dùng `tbody tr`, `.list-group-item` hoặc `.card` để search hoạt động tự động).
