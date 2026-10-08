-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local BirthCake = Scripts[000224]

local XU_RECORD = 5000019

local START_TIME = '2023-12-12 05:00:00'
local END_TIME = '2023-12-15 05:00:00'

function BirthCake:OnOpen(scene, npc, player, otherParams)
	local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Mừng Sinh nhật Kiếm Vương 1 ADNX.")
    
    dialog:AddSelection(1, "Vòng quay Sinh nhật")
    dialog:AddSelection(7, "Nhận Xu Sinh nhật")
    dialog:AddSelection(2, "Cửa hàng đổi phiếu Sinh nhật")
    
    if (player:IsGM()) then
        dialog:AddSelection(3, "Pháo hoa")
        dialog:AddSelection(4, "<color=#0486dc>[GM]</color> Bánh 1 tầng")
        dialog:AddSelection(5, "<color=#0486dc>[GM]</color> Bánh 2 tầng")
        dialog:AddSelection(6, "<color=#0486dc>[GM]</color> Bánh 3 tầng")    
    end

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
end

function BirthCake:ChangeCake(scene, level) 
    System:RemoveCake()
    local cakeID = 20163
    if level == 1 then
        cakeID = 20165
    elseif level == 2 then
        cakeID = 20166
    end
        
    local builder = EventManager:CreateNPCBuilder(scene)

    builder.SetResID(cakeID)
    builder.SetPos(5075, 3350)
    builder.SetName("Bánh kem Sinh nhật")
    builder.SetScriptID(224)
    builder.Build()
end

function BirthCake:ReceiveCoupon(scene, npc, player)
    local received_coin = Player:GetValueWeeklyRecore(player, XU_RECORD)
    if received_coin == -1 then
        received_coin = 0
    end

    local total_charge = Player:GetTotalCharge(player, START_TIME, END_TIME)

    local number_receive = total_charge / 100 - received_coin
    if number_receive < 0 or number_receive > total_charge / 100 then
        GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng thử lại sau.")
        return
    end

    if not Player.HasFreeBagSpaces(player, number_receive / 100 + 1) then
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu cần " .. number_receive / 100 + 1 .. " ô trống trong túi đồ để nhận vật phẩm.")
        return
    end

    Player:SetValueOfWeekRecore(player, XU_RECORD, total_charge / 100)
    Player.AddItemLuaAndNotify(player, 20271, number_receive, 0, 1, 0, '2023-12-15 05:00:00')
end

function BirthCake:ShowCouponInfo(scene, npc, player)
    local dialog = GUI:CreateNPCDialog()

    local received_coin = Player:GetValueWeeklyRecore(player, XU_RECORD)
    if received_coin == -1 then
        received_coin = 0
    end

    local total_charge = Player:GetTotalCharge(player, START_TIME, END_TIME)

    if received_coin > total_charge / 100 then
        received_coin = total_charge / 100
        Player:SetValueOfWeekRecore(player, XU_RECORD, received_coin)
    end

    dialog:AddText("Trong khoảng thời gian diễn ra sự kiện Vòng quay sinh nhật, với mỗi 10 ADNXu được chuyển vào, bằng hữu sẽ nhận được 1 Xu Sinh nhật.")
    dialog:AddText("")
    dialog:AddText("Đã chuyển " .. total_charge / 10 .. " ADNXu.")
    dialog:AddText("Đã nhận " .. received_coin .. '/' .. total_charge / 100 .. " Xu Sinh Nhật.")

    if (received_coin < total_charge / 100) then
        dialog:AddSelection(8, "Nhận " .. total_charge / 100 - received_coin .. " xu Sinh Nhật.")
    end

    dialog:AddSelection(99, "Quay lại")

    dialog:Show(npc, player)
end

function BirthCake:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 99 then
        BirthCake:OnOpen(scene, npc, player, nil)    
        return
    end

    if selectionID == 8 then
        GUI:CloseDialog(player)
        BirthCake:ReceiveCoupon(scene, npc, player)
        return
    end

    if selectionID == 7 then
        BirthCake:ShowCouponInfo(scene, npc, player)
        return
    end
    if selectionID == 2 then
        Player:OpenShop(npc, player, 1460)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 1 then
        GUI:OpenSeashell(player)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 3 then
        local builder = EventManager:CreateDynamicAreaBuilder(scene)

        builder.SetResID(20164)
        builder.SetPos(5075, 3350)
        builder.SetName("")
        builder.SetLifeTime(2)
        builder.SetRadius(0)
        builder.Build()

        GUI:CloseDialog(player)
    end

    if selectionID == 4 then
        BirthCake:ChangeCake(scene, 1)
        GUI:CloseDialog(player)
    end

    if selectionID == 5 then
        BirthCake:ChangeCake(scene, 2)
        GUI:CloseDialog(player)
    end

    if selectionID == 6 then
        BirthCake:ChangeCake(scene, 3)
        GUI:CloseDialog(player)
    end
end

function BirthCake:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end