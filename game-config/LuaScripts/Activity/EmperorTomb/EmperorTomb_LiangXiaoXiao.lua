-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000000' bên dưới thành ID tương ứng
local EmperorTomb_LiangXiaoXiao = Scripts[400030]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - Người chơi tương ứng
-- ****************************************************** --
function EmperorTomb_LiangXiaoXiao:OnOpen(scene, npc, player)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Lâu lắm rồi, ta chưa thấy một tên trộm nào dám lởn vởn quanh đây... Nơi này vào thì dễ ra thì khó... Ta thấy tiếc cho ngươi quá...")
	-- if Global_EarlyAccess[player:GetName()] then
	dialog:AddSelection(1, "Ta muốn vào Tần Lăng")
	if scene:GetID() ~= 1616 then
		dialog:AddSelection(2, "Ta muốn đăng ký vào tần lăng")
	end
	-- end
	dialog:AddSelection(100, "Thôi đi!")
	dialog:Show(npc, player)
	-- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - Người chơi tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function EmperorTomb_LiangXiaoXiao:OnSelection(scene, npc, player, selectionID)

	-- ************************** --
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 1 then
		if System:GetHour() < 7 then
			player:AddNotification("Tần Lăng hiện chưa mở, hãy quay lại sau!")
			return
		end

		-- khung giờ TL ở các server này là đến 17:00
		if System:GetServerID() < 18 and System:GetHour() >= 17 then
			player:AddNotification("Tần Lăng hiện chưa mở, hãy quay lại sau!")
			return
		end

		-- Kiểm tra điều kiện
		local ret = EventManager.EmperorTomb_EnterMap_CheckCondition(player)
		-- Nếu không thỏa mãn
		if ret ~= "OK" then
			EmperorTomb_LiangXiaoXiao:ShowNotify(npc, player, ret)
			return
		end
		
		-- Vào Tần Lăng
		EventManager.EmperorTomb_MoveToMap(player)
		
		return
	end
	-- ************************** --
	if selectionID == 2 then
		-- kiểm tra điều kiện
		local ret = EventManager.EmperorTomp_Register_CheckCondition(player)
		if ret ~= "OK" then
			EmperorTomb_LiangXiaoXiao:ShowNotify(npc, player, ret)
			return
		end
		-- đăng ký
		EventManager.EmperorTomp_Register(player)
		EmperorTomb_LiangXiaoXiao:ShowNotify(npc, player, ret)
		return
	end
	-- ************************** --
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectedItemInfo: SelectItem - Vật phẩm được chọn
--		otherParams: Key-Value {number, string} - Danh sách các tham biến khác
-- ****************************************************** --
function EmperorTomb_LiangXiaoXiao:OnItemSelected(scene, npc, player, selectedItemInfo, otherParams)

	-- ************************** --
	
	-- ************************** --

end

-- ======================================================= --
-- ======================================================= --
function EmperorTomb_LiangXiaoXiao:ShowNotify(npc, player, text)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText(text)
	dialog:AddSelection(100, "Kết thúc đối thoại")
	dialog:Show(npc, player)
	-- ************************** --

end