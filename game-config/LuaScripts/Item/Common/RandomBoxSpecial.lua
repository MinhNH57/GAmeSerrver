-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local RandomBox = Scripts[200200]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function RandomBox:OnPreCheckCondition(scene, item, player, otherParams)
    if item:GetItemID() == 20586 and player:GetLevel() < 10 then
        GUI:ShowMessageBox(player, 'Thông báo', 'Vật phẩm yêu cầu nhân vật đạt cấp độ 10 trở lên!')
        return false
    end
    -- ************************** --
    return true
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function RandomBox:OnUse(scene, item, player, otherParams)
    local openImmediately = item:GetExtParam(1) == 1
    if openImmediately then
        RandomBox:OnSelection(scene, item, player, 1, otherParams)
        return
    end

	-- ************************** --
    local dialog = GUI.CreateItemDialog()
    dialog:AddText(string.format("Ngươi chắc chắn muốn mở <color=yellow>[%s]</color> không?", item:GetName()))
    local numberDayUsed = Player:GetNumberDayUsedSpecialBox(player, item:GetID())
    if( numberDayUsed < 0)then
        if(item:GetBindingState() == 0) then
            dialog:AddText(string.format("Sau khi sử dụng thì vật phẩm sẽ bị khóa."))
        end
    else 
        local step = Player:GetStepCollectSpecialBox(player, item:GetID())
        local stepmax = Player:GetMaxStepCanCollectSpecialBox(player, item:GetID())
        dialog:AddText(string.format("Có tổng cộng là " ..stepmax.." mốc thưởng" ))
        dialog:AddText(string.format("Bạn đã nhận mốc thưởng thứ " ..step.."" ))
    end
    dialog:AddSelection(1, "Chắc chắn")
    dialog:AddSelection(100, "Ta suy nghĩ đã...")

    if player:IsGM() then
        dialog:AddSelection(101, "[GM] Reset giới hạn ngày")
        dialog:AddSelection(103, "[GM] Reset giới hạn sử dụng")
        dialog:AddSelection(102, "[GM] Tăng ngày sở hữu lên 1 ngày")
    end
    dialog:Show(item, player)
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function RandomBox:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
    if selectionID == 1 then
        if item:IsExpired() then
            player:AddNotification("Vật phẩm đã hết hạn sử dụng.")
            return
        end
        if Player.OpenRandomBoxSpecial(player, item:GetID()) == true then
            Player.CheckRemoveRandomBoxSpecial(player, item:GetID())
        end
        GUI.CloseDialog(player)
		return
	end
    -- ************************** --
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
	end

    if selectionID == 101 then
        Player:SetValueOfDailyRecore(player, item:GetItemID(), -1)
        GUI.CloseDialog(player)
    end
    if selectionID == 103 then
        Player:SetValueOfForeverRecore(player, item:GetItemID(), -1)
        GUI.CloseDialog(player)

    end
    if selectionID == 102 then
        Player:AddMoreDayOpenItem(player, item:GetID())
        GUI.CloseDialog(player)

    end
	-- ************************** --
	
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function RandomBox:OnItemSelected(scene, item, player, itemID)

    -- ************************** --

    -- ************************** --

end
