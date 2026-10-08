-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200034' bên dưới thành ID tương ứng

local VongSang = Scripts[200120]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function VongSang:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> VongSang:OnPreCheckCondition")--
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
function VongSang:OnUse(scene, item, player, otherParams)
	GUI:CloseUI(player, "UIBag")
	-- *
	local ItemID = item:GetItemID()
	local Itemlevel = item:GetItemLevel()
	local NameItem = item:GetName()

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	
	local MSG = "Ngươi có muốn dùng "..NameItem.." "
	local dialog = GUI.CreateItemDialog()
	dialog:AddText(MSG)


	local ExpiryDate = item:GetExpiryDate()
	if ExpiryDate == "1900-01-01 12:00:00" then
		dialog:AddText("Vật phẩm sẽ được kích hoạt hạn sử dụng sau lần sử dụng đầu tiên.")
	else 
		dialog:AddText("Vật phẩm sẽ hết hạn vào " .. ExpiryDate)
	end

	dialog:AddSelection(1, "Bật " .. NameItem)
	dialog:AddSelection(2, "Tắt " .. NameItem)
    dialog:AddSelection(999, "Ta suy nghĩ đã")
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
function VongSang:OnSelection(scene, item, player, selectionID, otherParams)
	-- ************************** --
	--player:AddNotification("Enter -> VongSang:OnSelection, selectionID = " .. selectionID)--
    local IDBuff = item:GetExtParam(1) 
	local duration = item:GetExtParam(2)
	local LevelBuff = item:GetExtParam(3)
	if (LevelBuff == -1) then
		LevelBuff = 1
	end

	if duration ~= -1 then
		if not System:StartCountdownGood(player, item, duration) then
			GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi khi sử dụng vật phẩm")
			return
		end
	end

	-- ************************** --
	if selectionID == 1 then
		if(player:HasBuff(IDBuff)) then
			player:RemoveBuff(IDBuff)
		end
		local duration = item:GetDurationLeft()
		if duration == -100 then
			return
		end
		if duration == -1 then
			duration = 86400000
		end

		duration = math.min(duration, 86400000)
		player:AddBuffWithDuration(IDBuff, LevelBuff, duration, 1, true)
		GUI.CloseDialog(player)
		return
	end

	if selectionID == 2 then
		if(player:HasBuff(IDBuff)) then
			player:RemoveBuff(IDBuff)
		end

		GUI.CloseDialog(player)
		return
	end

	if selectionID == 999 then
		GUI.CloseDialog(player)
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
function VongSang:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end