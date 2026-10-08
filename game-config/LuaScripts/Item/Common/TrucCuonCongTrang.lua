
-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local TrucCuonCongTrang = Scripts[200131]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TrucCuonCongTrang:OnPreCheckCondition(scene, item, player, otherParams)
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
function TrucCuonCongTrang:OnUse(scene, item, player, otherParams)
	local amount = item:GetExtParam(1)
	if amount == nil or amount <= 0 then
		player:AddNotification("Vật phẩm không hợp lệ!")
		return
	end

	local count = item:GetItemCount()
	if count <= 1 then
		if not Player.UseMedalMeritScroll(player, item:GetID(), amount) then
			player:AddNotification("Có lỗi khi sử dụng vật phẩm.")
		end
		return
	end

	local dialog = GUI.CreateItemDialog()
	dialog:AddText(string.format("Ngươi muốn sử dụng bao nhiêu trục <color=yellow>[%s]</color>?", item:GetName()))
	dialog:AddSelection(1, "Sử dụng 1 cái")
	
	if count >= 10 then
		dialog:AddSelection(10, "Sử dụng 10 cái")
	end
	
	dialog:AddSelection(999, string.format("Sử dụng toàn bộ (tối đa 100 cái, hiện có %d cái)", count))
	dialog:AddSelection(100, "Ta suy nghĩ đã...")
	dialog:Show(item, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TrucCuonCongTrang:OnSelection(scene, item, player, selectionID, otherParams)
	local amount = item:GetExtParam(1)
	if amount == nil or amount <= 0 then
		player:AddNotification("Vật phẩm không hợp lệ!")
		GUI.CloseDialog(player)
		return
	end

	local count = item:GetItemCount()
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
	end

	local useCount = 0
	if selectionID == 1 then
		useCount = 1
	elseif selectionID == 10 then
		useCount = 10
	elseif selectionID == 999 then
		useCount = math.min(100, count)
	end

	if useCount > 0 then
		if useCount > count then
			player:AddNotification("Số lượng vật phẩm không đủ!")
			GUI.CloseDialog(player)
			return
		end

		if not Player.UseMedalMeritScrollMultiple(player, item:GetID(), amount, useCount) then
			player:AddNotification("Có lỗi khi sử dụng vật phẩm.")
		end
	end
	GUI.CloseDialog(player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TrucCuonCongTrang:OnItemSelected(scene, item, player, itemID)

end
