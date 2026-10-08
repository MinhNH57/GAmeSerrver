-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200082' bên dưới thành ID tương ứng

local TangGioiHanSach = Scripts[206970]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TangGioiHanSach:OnPreCheckCondition(scene, item, player, otherParams)
    local START_DATE = "2025-09-01 05:00:00"

    local monthsFromStart = System:GetMonthsFromDateTime(START_DATE)
    local maxUses = 2 + (monthsFromStart * 2)
	local itemType = item:GetExtParam(2)
    local usedCount = player:GetBookUseCount(itemType)
    
    if usedCount >= maxUses then
        player:AddNotification(string.format("Đã đạt giới hạn sử dụng (%d lần)!", maxUses))
        return false
    end

    return true
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function TangGioiHanSach:OnUse(scene, item, player, otherParams)

	-- ************************** --
	-- ************************** --
	
	local itemID = item:GetItemID()
	local limitIncreased = item:GetExtParam(1) or 5
	local itemType = item:GetExtParam(2)
	
	-- Kiểm tra loại vật phẩm (1 = Võ Lâm Mật Tịch, 2 = Tẩy Tủy Kinh)
	if (itemType ~= 1 and itemType ~= 2) then
		player:AddNotification("Vật phẩm không hợp lệ!")
		return
	end

	local bookType = "Võ Lâm Mật Tịch (trung)"
	if (itemType == 2) then
		bookType = "Tảy Tủy Kinh (trung)"
	end

	GUI:ShowMessageBox(player, "Thông báo", 
		"Xác nhận tăng giới hạn sử dụng " .. bookType .. " thêm " .. limitIncreased .. " lần?",
		function()
			Player.IncreaseBookUseLimit(player, itemID, itemType, limitIncreased)
			player:SaveBookUseCount(itemType, 1)
			GUI:CloseDialog(player)
		end,
		function ()
			GUI:CloseDialog(player)
		end
	)
	-- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TangGioiHanSach:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> VoLamMatTich:OnSelection, selectionID = " .. selectionID)--
	
	-- ************************** --
	
	item:FinishUsing(player)
	

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TangGioiHanSach:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end