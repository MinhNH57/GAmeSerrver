-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local TiecBatNgo = Scripts[000227]

local START_DATE = '2024-02-08 05:00:00'
local END_DATE = '2024-02-15 00:00:00'

local FREE_REWARD = 20388
local VIP_REWARD_1 = 20389
local VIP_REWARD_2 = 20390

local FREE_REWARD_20 = 20400
local VIP_REWARD_1_20 = 20401
local VIP_REWARD_2_20 = 20402

function TiecBatNgo:OnOpen(scene, npc, player, otherParams)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Chúc mừng Năm mới 2024!!!!!")

    if player:IsGM() or System.IsDateTimeInRange(START_DATE, END_DATE) then    
        dialog:AddText("")
        dialog:AddText("Hằng ngày, bằng hữu có thể đến đây để nhận miễn phí một phần quà Chữ TẾT, gồm có: huyền tinh, điểm danh vọng, vật phẩm hỗ trợ bạn đồng hành.")
        dialog:AddText("")
        dialog:AddText("Hơn nữa, chỉ với 250KNB, bằng hữu có thể nhận thêm một Câu đối Tết (tối đa 5 lượt/ngày), gồm có: huyền tinh cấp cao, điểm danh vọng, tinh hoạt lực, vật phẩm hỗ trợ bạn đồng hành cao cấp.")
        
        dialog:AddSelection(1, "Nhận Chữ TẾT")
        dialog:AddSelection(2, "Mua Câu đối Tết <color=#eba0ac>(250 KNB)</color>")

        if player:IsGM() then
            dialog:AddSelection(9999, "[GM] Reset")
        end
    end
    
    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
	-- ************************** --

end

function TiecBatNgo:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 9999 then
        Player:SetValueOfDailyRecore(player, 5000103, -1)
        Player:SetValueOfDailyRecore(player, 5000102, -1)
        return
    end

    if selectionID == 1 then
        TiecBatNgo:ReceiveSurprise(scene, npc, player)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 2 then
        TiecBatNgo:ReceiveVIPSurprise(scene, npc, player)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 99 then
        GUI:CloseDialog(player)
    end
end

function TiecBatNgo:ReceiveVIPSurprise(scene, npc, player)
    local record = 5000103

    local total_received = Player:GetValueOfDailyRecore(player, record)
    if total_received < 0 then
        total_received = 0
    end

    if total_received >= 5 then
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã nhận đủ 5 Câu đối Tết của ngày hôm nay rồi!\nVui lòng quay trở lại vào ngày mai!")
        return
    end

    if not Player:HasFreeBagSpaces(player, 1) then
        GUI:ShowMessageBox(player, "Thông báo", "<color=#eba0ac>Bằng hữu cần 1 ô trống trong túi đồ.</color>")
        return
    end

    if Player:CheckMoney(player, 2) < 250 then
        GUI:ShowMessageBox(player, "Thông báo", "<color=#eba0ac>Bằng hữu không có đủ 250 KNB.</color>")
        return
    end

    if Player:SubMoney(player, 250, 2) then
        local reward_id = VIP_REWARD_1
        if total_received >= 3 then
            reward_id = VIP_REWARD_2
        end

        if System:GetServerID() == 20 or System:GetServerID() == 998 then
            reward_id = VIP_REWARD_1_20
            if total_received >= 3 then
                reward_id = VIP_REWARD_2_20
            end
        end

        Player:AddItemLuaAndNotify(player, reward_id, 1, 0, 1)
        Player:SetValueOfDailyRecore(player, record, total_received + 1)
    end
end

function TiecBatNgo:ReceiveSurprise(scene, npc, player)
    local record = 5000102
    if Player:GetValueOfDailyRecore(player, record) ~= -1 then
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã nhận Chữ TẾT của ngày hôm nay rồi!\nVui lòng quay trở lại vào ngày mai!")
        return
    end

    if not Player:HasFreeBagSpaces(player, 1) then
        GUI:ShowMessageBox(player, "Thông báo", "<color=#eba0ac>Bằng hữu cần 1 ô trống trong túi đồ.</color>")
        return
    end

    local reward_id = FREE_REWARD
    if System:GetServerID() == 20 or System:GetServerID() == 998 then
        reward_id = FREE_REWARD_20
    end

    Player:AddItemLuaAndNotify(player, reward_id, 1, 0, 1)

    Player:SetValueOfDailyRecore(player, record, 1)

    GUI:CloseDialog(player)
end


function TiecBatNgo:OnItemSelected(scene, npc, player, itemID, otherParams)

end