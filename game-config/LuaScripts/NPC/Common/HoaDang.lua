-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200082' bên dưới thành ID tương ứng

local HoaDang = Scripts[220]

local HoaDangPos = {
    [6075] = {250, 150, 20156},
    [7139] = {-350, 150, 20157},
    [6264] = {250, 150, 20156},
    [6483] = {250, 150, 20156},
    [7328] = {0, 0},
}

local DAILY_LIMIT = 2
local DAILY_RECORD = 5000011
local FOREVER_LIMIT = 30
local FOREVER_RECORD = 5000012

function HoaDang:ThaDen(scene, npc, player)

    local hoadang_in_bag = Player:CountItemInBag(player, 20108)
    if hoadang_in_bag == 0 then
        GUI:ShowMessageBox(player, "Thông báo", "Bạn không có hoa đăng để thả!")
        return
    end

    local usage_in_day = Player:GetValueOfDailyRecore(player, DAILY_RECORD)
    if usage_in_day >= DAILY_LIMIT and not player:IsGM() then
        GUI:ShowMessageBox(player, "Thông báo", "Bạn đã dùng hết " .. DAILY_LIMIT .. " lượt thả đèn hôm nay!")
        return
    end

    local usage_forever = Player:GetValueForeverRecore(player, FOREVER_RECORD)
    if usage_forever >= FOREVER_LIMIT and not player:IsGM() then
        GUI:ShowMessageBox(player, "Thông báo", "Bạn đã dùng hết toàn bộ " .. FOREVER_LIMIT .. " lượt thả đèn!")
        return
    end

    if not Player:RemoveItem(player, 20108, 1) then
        GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng thử lại sau!")
        return
    end

    if usage_in_day < 0 then
        usage_in_day = 0
    end

    if usage_forever < 0 then
        usage_forever = 0
    end

    usage_in_day = usage_in_day + 1
    usage_forever = usage_forever + 1
    
    Player:SetValueOfDailyRecore(player, DAILY_RECORD, usage_in_day)
    Player:SetValueOfForeverRecore(player, FOREVER_RECORD, usage_forever)
    Player:AddItemLuaAndNotify(player, 20128, 1, -1, 0)

    local builder = EventManager:CreateDynamicAreaBuilder(scene)
    local offset = HoaDangPos[npc:GetPos():GetX()]

    builder.SetResID(offset[3])
    builder.SetPos(npc:GetPos():GetX() - offset[1], npc:GetPos():GetY() - offset[2])
    builder.SetName("")
    builder.SetLifeTime(5)
    builder.SetRadius(0)
    builder.Build()

    GUI:CloseDialog(player)
end

function HoaDang:OnOpen(scene, npc, player, otherParams)
    if Global_IsIn2109Event() then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Hoạt động thả đèn hoa đăng diễn ra vào 20:00 - 20:30 hằng ngày. Bằng hữu có thể thu thập hoa đăng trong Rương Phỉ Thúy và Rương Hoàng Kim từ hoạt động tích lũy điểm năng động. Ngoài ra có thể tới gặp Cổ Phong Hà để mua thêm hoa đăng trong Tiệm hoa đăng.")

        -- thông tin thả đèn 
        dialog:AddText("\n")
        local usage_in_day = Player:GetValueOfDailyRecore(player, DAILY_RECORD)
        local usage_forever = Player:GetValueForeverRecore(player, FOREVER_RECORD)
        if usage_in_day < 0 then
            usage_in_day = 0
        end
        if usage_forever < 0 then
            usage_forever = 0
        end
        dialog:AddText("Hôm nay đã thả <color=#bac2de>" .. usage_in_day .. "/" .. DAILY_LIMIT .. "</color> đèn.")
        dialog:AddText("Tổng cộng đã thả <color=#bac2de>" .. usage_forever .. "/" .. FOREVER_LIMIT .. "</color> đèn.")
        dialog:AddText("\n")

        local currentHour = System:GetHour()
        local currentMinute = System:GetMinute()

        if currentHour == 20 and currentMinute >= 0 and currentMinute <= 30 then
            dialog:AddSelection(1, "Thả đèn") 
        else 
            if player:IsGM() then
                dialog:AddSelection(1, "Thả đèn") 
            end
            dialog:AddText('<color=#ed8796>Hiện tại không phải thời gian thả đèn. Vui lòng quay lại sau!</color>')
        end

        dialog:AddSelection(99, "Để sau")
        dialog:Show(npc, player)
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function HoaDang:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 1 then
        HoaDang:ThaDen(scene, npc, player)
        return
    end

    if selectionID == 99 then
        GUI:CloseDialog(player)
        return
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function HoaDang:OnItemSelected(scene, item, player, itemID)
end