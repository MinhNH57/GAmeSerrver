-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '00000' bên dưới thành ID tương ứng
local BachThuLam = Scripts[000000]

-- ************************** --
local ChangeNameCard = 2167
local ChangeNameRecord = {
	RecordID = 7777777,
	RecordValue = 123456
}
-- ************************** --
local ChangeFactionCard = 2168
-- ************************** --

--****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function BachThuLam:OnOpen(scene, npc, player, otherParams)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Chào mừng bằng hữu đến với <color=green>Kiếm Thế 2009 - Mobile</color>. Ta là <color=yellow>Bạch Thu Lâm</color>, thủ lĩnh nghĩa quân. Nếu bằng hữu gặp khó khăn gì, cần hỗ trợ hãy tới chỗ ta")
	dialog:AddSelection(1, "Ta muốn đổi tên")
	dialog:AddSelection(2, "Xóa vật phẩm")
	dialog:AddSelection(3, "Xếp chồng vật phẩm")
	if Player.GetValueForeverRecore(player, ChangeNameRecord.RecordID) ~= ChangeNameRecord.RecordValue then
		dialog:AddSelection(4, "Nhận miễn phí Thẻ đổi tên")
	end
	dialog:AddSelection(5, "Ta muốn đổi phái")
	dialog:AddSelection(1000, "Kết thúc đối thoại")
	dialog:Show(npc, player)
	-- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function BachThuLam:OnSelection(scene, npc, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 1000 then
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 1 then
		-- Gọi hàm đổi tên
		GUI.OpenChangeName(player)
		
		-- Đóng khung
		BachThuLam:ShowDialog(npc, player, "Đối với các bằng hữu sử dụng <color=green>IOS</color> chưa cập nhật được phiên bản mới vui lòng tới <color=green>Lễ Quan</color> chọn kích hoạt GiftCode sử dụng mã quà tặng <color=green>CNAME</color> <b>dấu cách</b> Tên bạn muốn đổi nhấn kích hoạt để thực hiện đổi tên")
		return
	end
	-- ************************** --
	if selectionID == 2 then
		-- Mở khung xóa vật phẩm
		GUI.OpenRemoveItems(player)
		
		-- Đóng khung
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 3 then
		-- Mở khung ghép vật phẩm
		GUI.OpenMergeItems(player)
		
		-- Đóng khung
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 4 then
		-- Nếu đã nhận rồi
		if Player.GetValueForeverRecore(player, ChangeNameRecord.RecordID) == ChangeNameRecord.RecordValue then
			BachThuLam:ShowDialog(npc, player, "Bằng hữu đã nhận miễn phí <color=green>Thẻ đổi tên</color>, không thể nhận thêm!")
			return
		end
		
		-- Nếu túi không đủ chỗ trống
		if Player.HasFreeBagSpaces(player, 1) == false then
			BachThuLam:ShowDialog(npc, player, "Bằng hữu cần sắp xếp <color=green>1 ô trống</color>, trong túi để nhận!")
			return
		end
		
		-- Đánh dấu
		Player.SetValueOfForeverRecore(player, ChangeNameRecord.RecordID, ChangeNameRecord.RecordValue)
		
		-- Thêm Thẻ đổi tên
		Player.AddItemLua(player, ChangeNameCard, 1, -1, 1)
		
		BachThuLam:ShowDialog(npc, player, "Bằng hữu đã nhận thành công <color=green>Thẻ đổi tên</color>!")
		return
	end
	-- ************************** --
	if selectionID == 5 then
		
		return
	end
	-- ************************** --
	
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function BachThuLam:OnItemSelected(scene, npc, player, itemID,otherParams)

	-- ************************** --

	-- ************************** --

end


function BachThuLam:ShowDialog(npc, player, text)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText(text)
	dialog:AddSelection(1000, "Kết thúc đối thoại")
	dialog:Show(npc, player)
	-- ************************** --
	
end