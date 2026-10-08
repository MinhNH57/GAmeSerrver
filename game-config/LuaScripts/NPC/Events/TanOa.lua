-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local TanOa = Scripts[000222]

local NORMAL_FISH = { 20219, 20220, 20223 }
local NORMAL_REWARD = 20206
local SPECIAL_FISH = { 20225, 20226, 20227, 20228, 20229 }
local SPECIAL_REWARD = 20207

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function TanOa:OnOpen(scene, npc, player, otherParams)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Tại đây có giám định và thu mua cá giá cả hấp dẫn.")
    dialog:AddText("")
    -- dialog:AddText("Bằng hữu cũng có thể theo dõi bảng xếp hạng cao thủ câu cá và nhận thưởng tại đây.")
    -- dialog:AddText("")
    -- local fishes_in_bag = ''
    -- local has_fishes = false
    -- for k, v in ipairs(NORMAL_FISH) do
    --     local fish_binded = Player:CountItemInBag(player, v, 1)
    --     local fish_unbinded = Player:CountItemInBag(player, v, 0)
        
    --     if fish_binded + fish_unbinded > 0 then
    --         has_fishes = true
    --         fishes_in_bag = fishes_in_bag .. '\t- ' .. System:GetItemName(v) .. ': ' .. fish_binded .. ' (khóa), ' .. fish_unbinded .. ' (không khóa)\n'
    --     end
    -- end

    -- for k, v in ipairs(SPECIAL_FISH) do
    --     local fish_binded = Player:CountItemInBag(player, v, 1)
    --     local fish_unbinded = Player:CountItemInBag(player, v, 0)
        
    --     if fish_binded + fish_unbinded > 0 then
    --         has_fishes = true
    --         fishes_in_bag = fishes_in_bag .. '\t- ' .. System:GetItemName(v) .. ': ' .. fish_binded .. ' (khóa), ' .. fish_unbinded .. ' (không khóa)\n'
    --     end
    -- end

    -- if has_fishes then
    --     dialog:AddText("Bằng hữu đang có:")
    --     dialog:AddText(fishes_in_bag)
    -- end

    -- dialog:AddSelection(2, "Tiệm Giám định Cá")
    -- dialog:AddSelection(3, "Tiệm Giám định cá (khóa)")

    -- dialog:AddSelection(7, "Giám định toàn bộ cá!")

    -- dialog:AddSelection(4, "Bảng xếp hạng Cao thủ câu cá (hôm qua)")
    -- dialog:AddSelection(5, "Bảng xếp hạng Cao thủ câu cá (hôm nay)")

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
	-- ************************** --

end


function TanOa:NhanThuongGiamDinhCa(scene, npc, player)
    if System:GetHour() < 6 or System:GetHour() > 22 then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText('Bằng hữu có thể nhận thưởng từ 7:00 đến 23:00 hằng ngày.')
        dialog:Show(npc, player)
        return
    end

    local ITEMID = 20210
    local RECORD = 5000014

    if not Player.HasFreeBagSpaces(player, 1) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần " .. 1 .. " ô trống trong túi đồ để nhận vật phẩm.")
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    if Player:GetValueOfDailyRecore(player, RECORD) ~= -1 then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu đã nhận gói vật phẩm này rồi!")
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    else
        Player:SetValueOfDailyRecore(player, RECORD, 1)
    end

    Player.AddItemLuaAndNotify(player, ITEMID, 1, 0, 1)
    GUI:CloseDialog(player)
end

function TanOa:GiamDinhCaAll(npc, player)
    local player_fishes = {}

    local total_normal_binded = 0
    local total_normal_unbinded = 0
    for k, v in ipairs(NORMAL_FISH) do
        local fish_binded = Player:CountItemInBag(player, v, 1)
        local fish_unbinded = Player:CountItemInBag(player, v, 0)
        total_normal_binded = total_normal_binded + fish_binded
        total_normal_unbinded = total_normal_unbinded + fish_unbinded

        Player:RemoveItem(player, v, fish_binded)
        Player:RemoveItem(player, v, fish_unbinded)
    
        if fish_binded + fish_unbinded > 0 then
            player_fishes[v] = fish_binded + fish_unbinded
        end
    end

    if total_normal_binded > 0 then
        Player:AddItemLuaAndNotify(player, NORMAL_REWARD, total_normal_binded, 0, 1)
    end
    if total_normal_unbinded > 0 then
        Player:AddItemLuaAndNotify(player, NORMAL_REWARD, total_normal_unbinded, 0, 0)
    end
        

    local total_special_binded = 0
    local total_special_unbinded = 0
    for k, v in ipairs(SPECIAL_FISH) do
        local fish_binded = Player:CountItemInBag(player, v, 1)
        local fish_unbinded = Player:CountItemInBag(player, v, 0)
        total_special_binded = total_special_binded + fish_binded
        total_special_unbinded = total_special_unbinded + fish_unbinded

        Player:RemoveItem(player, v, fish_binded)
        Player:RemoveItem(player, v, fish_unbinded)

        if fish_binded + fish_unbinded > 0 then
            player_fishes[v] = fish_binded + fish_unbinded
        end
    end

    if total_special_binded > 0 then
        Player:AddItemLuaAndNotify(player, SPECIAL_REWARD, total_special_binded, 0, 1)
    end
    if total_special_unbinded > 0 then
        Player:AddItemLuaAndNotify(player, SPECIAL_REWARD, total_special_unbinded, 0, 0)
    end

    if total_normal_binded + total_normal_unbinded + total_special_binded + total_special_unbinded > 0 then
        System:GiamDinhCa(player, player_fishes)
    else 
        GUI:ShowMessageBox(player, "Câu cá Mùa thu", "Bằng hữu không có cá trong túi đồ!")
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TanOa:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 2 then
        Player:OpenShop(npc, player, 1457)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 3 then
        Player:OpenShop(npc, player, 1458)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 4 then
        local dialog = GUI:CreateNPCDialog(npc, player)
        if System:GetMinute() < 5 then
            dialog:AddText("Bảng xếp hạng đang cập nhật!")
            dialog:Show(npc, player)
            return
        end
        dialog:AddText(System:GetGDCYesterday())
        if System:IsGDCYesterDayWinner(player) then
            dialog:AddSelection(6, "Nhận thưởng Cao thủ câu cá!")
        end
        dialog:AddSelection(99, "Kết thúc đối thoại")
        dialog:Show(npc, player)
        return
    end

    if selectionID == 5 then
        local dialog = GUI:CreateNPCDialog(npc, player)
        if System:GetMinute() < 5 then
            dialog:AddText("Bảng xếp hạng đang cập nhật!")
            dialog:Show(npc, player)
            return
        end
        dialog:AddText(System:GetGDCToday())
        dialog:AddSelection(99, "Kết thúc đối thoại")
        dialog:Show(npc, player)
        return
    end

    if selectionID == 6 then
        TanOa:NhanThuongGiamDinhCa(scene, npc, player)
        return
    end

    if selectionID == 7 then
        TanOa:GiamDinhCaAll(npc, player)
        return
    end

    GUI:CloseDialog(player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TanOa:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end