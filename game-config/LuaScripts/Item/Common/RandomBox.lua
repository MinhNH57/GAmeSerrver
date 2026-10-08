-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local RandomBox = Scripts[200060]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function RandomBox:OnPreCheckCondition(scene, item, player, otherParams)
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
    -- tắt rương mảnh đồng hành
    -- if item:GetItemID() == 20051 then
    --     local dialog = GUI.CreateItemDialog()
    --     dialog:AddText("Tính năng sắp ra mắt!")
    --     dialog:Show(item, player)
    --     return
    -- end
	
	-- nếu là lì xì ất tỵ thì phải đeo phi phong 5 trở lên
	if item:GetItemID() == 25308 then
		if player:GetMantleLevel() < 5 then
			player:AddNotification("Bạn cần trang bị Phi phong 5 trở lên để sử dụng vật phẩm này")
			return
		end
	end

    local openImmediately = item:GetExtParam(1) == 1
    local enableOpenAll = item:GetExtParam(1) == 2
    if openImmediately then
        RandomBox:OnSelection(scene, item, player, 1, otherParams)
        return
    end

	-- ************************** --
    local dialog = GUI.CreateItemDialog()
    dialog:AddText(string.format("Ngươi chắc chắn muốn mở <color=yellow>[%s]</color> không?", item:GetName()))

    local itemsToPreview = System:GetRandomBoxPreview(player, item:GetItemValue())
    if itemsToPreview ~= nil then
        dialog:AddText("")
        dialog:AddText("Có cơ hội nhận được các vật phẩm sau:")
        dialog:SetItemSelectable(false)

        for k, v in pairs(itemsToPreview) do
            if k == 11 then
                -- tinh hoạt lực cộng thẳng. đổi lại id để hiển thị
                dialog:AddItem(5, v, 0)
                dialog:AddItem(6, v, 0)
            else
                dialog:AddItem(k, v, 0)
            end
        end
    end


    dialog:AddSelection(1, "Chắc chắn")
    if enableOpenAll then
        dialog:AddSelection(2, "Mở toàn bộ (tối đa 100 vật phẩm/lượt)") 
        if player:IsGM() then
            dialog:AddSelection(3, "[GM] Mở toàn bộ (tối đa 1000 vật phẩm/lượt)")
        end
    end
    dialog:AddSelection(100, "Ta suy nghĩ đã...")
    if player:IsGM() then
        dialog:AddSelection(101, "[GM] Reset giới hạn")
        dialog:AddSelection(102, '[GM] Mở test 1000 vật phẩm')
        dialog:AddSelection(103, '[GM] Mở test 5000 vật phẩm')
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
        if Player.OpenRandomBox(player, item:GetID()) == true then
            Player.RemoveItem(player, item:GetID())
            System:WriteLog(113, '' .. player:GetName() .. '[' .. player:GetID() .. '] hoàn thành mở RANDOMBOX, itemid ' .. item:GetItemID() .. ', dbid ' .. item:GetID())
        else 
            System:WriteLog(113, '' .. player:GetName() .. '[' .. player:GetID() .. '] mở lỗi RANDOMBOX, itemid ' .. item:GetItemID() .. ', dbid ' .. item:GetID())
        end
        GUI.CloseDialog(player)
		return
	end

    if selectionID == 2 then
        GUI.CloseDialog(player);
        Player:OpenAllRandomBox(player, item:GetItemID(), 100)
    end

    if selectionID == 3 then
        GUI.CloseDialog(player);
        Player:OpenAllRandomBox(player, item:GetItemID(), 1000)
    end

    -- ************************** --
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
	end

    if selectionID == 102 then
        Player:OpenBatchRandomBox(player, item:GetItemID(), 1000)
        return
    end

    if selectionID == 103 then
        Player:OpenBatchRandomBox(player, item:GetItemID(), 5000)
        return
    end

    if selectionID == 101 then
        Player:SetValueOfDailyRecore(player, item:GetItemValue(), -1)
        Player:SetValueOfForeverRecore(player, item:GetItemValue(), -1)
        Player:SetValueOfMonthRecord(player, item:GetItemValue(), -1)

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
