-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local TinhKhiTan = Scripts[200022]

-- ************************** --
local ADD_SHENGWANG = {500, 1000, 1500,5000}
local USED_LIMIT = 10
-- ************************** --

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TinhKhiTan:OnPreCheckCondition(scene, item, player, otherParams)

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
function TinhKhiTan:OnUse(scene, item, player, otherParams)

	-- ************************** --
	
	local dialog = GUI.CreateItemDialog()
    dialog:AddText(string.format("Ngươi chắc chắn muốn dùng <color=yellow>[%s]</color> không?", item:GetName()))
	dialog:AddSelection(1, "Chắc chắn")
	-- Số lượng đã ăn trong ngày
	local totalUsedToday = Player.GetValueOfDailyRecore(player, item:GetItemID())
	if totalUsedToday == -1 then
		totalUsedToday = 0
	end
	
	local max_use = USED_LIMIT - totalUsedToday
	
	if max_use > 1 then
		dialog:AddSelection(2, "Dùng toàn bộ (tối đa "..max_use..")")
	end
	
	dialog:AddSelection(3, "Để ta suy nghĩ lại")
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
function TinhKhiTan:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	
	if selectionID == 1 then
		-- Cấp độ
		local nLevel = item:GetItemLevel()
		-- Toác
		if nLevel < 1 or nLevel > 4 then
			player:AddNotification("Vật phẩm không hợp lệ!")
			return
		end
		
		-- Số lượng đã ăn trong ngày
		local totalUsedToday = Player.GetValueOfDailyRecore(player, item:GetItemID())
		-- Chỉnh lại giá trị nếu sang ngày mới
		if totalUsedToday == -1 then
			totalUsedToday = 0
		end
		-- Nếu vượt quá giới hạn ăn trong ngày
		if totalUsedToday >= USED_LIMIT then
			player:AddNotification("Mỗi ngày chỉ có thể sử dụng tối đa 10 bình!")
			return
		end
		
		-- Tăng số lượng đã ăn trong ngày
		totalUsedToday = totalUsedToday + 1
		Player.SetValueOfDailyRecore(player, item:GetItemID(), totalUsedToday)
		
		-- Xóa vật phẩm
		Player.RemoveItem(player, item:GetID())
		
		-- Lượng Tinh lực có
		local addPoint = ADD_SHENGWANG[nLevel]
		-- Thêm tinh lực tương ứng
		Player.AddCurGatherPoint(player, addPoint)
		
		player:AddNotification("Sử dụng thành công, ngày hôm nay đã dùng tổng cộng " .. totalUsedToday .. "/" .. USED_LIMIT .. " " ..item:GetName() .. ".");
		-- ************************** --
		if addPoint == 500 then
			EventManager:UpdateTaskProgress(player, 5, 1)
		elseif addPoint == 1000 then
			EventManager:UpdateTaskProgress(player, 8, 1)
		elseif addPoint == 1500 then
			EventManager:UpdateTaskProgress(player, 15, 1)
		end
		GUI.CloseDialog(player);
	end
	
	if selectionID == 2 then
		-- Cấp độ
		local nLevel = item:GetItemLevel()
		-- Toác
		if nLevel < 1 or nLevel > 4 then
			player:AddNotification("Vật phẩm không hợp lệ!")
			return
		end
		
		-- Số lượng đã ăn trong ngày
		local totalUsedToday = Player.GetValueOfDailyRecore(player, item:GetItemID())
		-- Chỉnh lại giá trị nếu sang ngày mới
		if totalUsedToday == -1 then
			totalUsedToday = 0
		end
		-- Nếu vượt quá giới hạn ăn trong ngày
		if totalUsedToday >= USED_LIMIT then
			player:AddNotification("Mỗi ngày chỉ có thể sử dụng tối đa 10 bình!")
			return
		end
		
		--lấy số lượng trong túi
		local in_bag = Player.CountItemInBag(player, item.GetItemID())
		
		-- lấy số lượng có thể ăn
		local max_use = USED_LIMIT - totalUsedToday
		
		if in_bag < max_use then
			max_use = in_bag
		end
		
		-- Tăng số lượng đã ăn trong ngày
		totalUsedToday = totalUsedToday + max_use
		Player.SetValueOfDailyRecore(player, item:GetItemID(), totalUsedToday)
		
		-- Xóa vật phẩm
		Player.RemoveItem(player, item:GetItemID(), max_use)
		
		-- Lượng Tinh lực có
		local addPoint = ADD_SHENGWANG[nLevel]
		-- Thêm tinh lực tương ứng
		Player.AddCurGatherPoint(player, addPoint * max_use)
		
		player:AddNotification("Sử dụng thành công, ngày hôm nay đã dùng tổng cộng " .. totalUsedToday .. "/" .. USED_LIMIT .. " " ..item:GetName() .. ".");
		-- ************************** --
		if addPoint == 500 then
			EventManager:UpdateTaskProgress(player, 5, max_use)
		elseif addPoint == 1000 then
			EventManager:UpdateTaskProgress(player, 8, max_use)
		elseif addPoint == 1500 then
			EventManager:UpdateTaskProgress(player, 15, max_use)
		end
		GUI.CloseDialog(player);
	end
	
	if selectionID == 3 then
		GUI.CloseDialog(player);
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
function TinhKhiTan:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end