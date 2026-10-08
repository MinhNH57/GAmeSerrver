-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000033' bên dưới thành ID tương ứng
local DaLuyenDaiSu = Scripts[000033]

--****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function DaLuyenDaiSu:OnOpen(scene, npc, player, otherParams)

	-- ************************** --
	Player.SetLastEquipMasterNPC(player, npc)
	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Cường hóa làm tăng thuộc tính của trang bị, cũng như giúp kích hoạt những thuộc tính ẩn, cấp cường hóa càng cao thuộc tính trang bị càng cao.")
	dialog:AddSelection(1, "Cường hóa vật phẩm")
	dialog:AddSelection(3, "Ghép Huyền Tinh")
	dialog:AddSelection(16, "Luyện Hóa Đồ")
	dialog:AddSelection(15, "Ngũ Hành Ấn/Lôi Đình Ấn")
	dialog:AddSelection(17, "Trang bị đồng hành")
	dialog:AddSelection(4, "Phân Giải Huyền Tinh Cấp Cao")
	dialog:AddSelection(6, "Muốn tách huyền tinh từ trang bị cao cấp")
	-- dialog:AddSelection(18, "Chế tạo vật phẩm hỗ trợ")
	dialog:AddSelection(19, "Cường hóa cao cấp [17, 18, 19, 20]")
	-- dialog:AddSelection(8, "Sửa trang bị")
	dialog:AddSelection(9, "Ta chỉ đi ngang qua")
	dialog:Show(npc, player)
	-- ************************** --

end

function DaLuyenDaiSu:OnRefineOperator(scene, npc, player)
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Luyện hóa trang bị có thể nâng sức mạnh vật phẩm lên cấp độ mới. Ta có thể giúp bằng hữu thực hiện các thao tác liên quan đến trang bị luyện hóa.")
	
	dialog:AddSelection(2, "Luyện hóa vật phẩm")
	dialog:AddSelection(11, "Ghép luyện hóa đồ")

	dialog:Show(npc, player)
end

function DaLuyenDaiSu:OnPetOpetaror(scene, npc, player)
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Ta có thể giúp bằng hữu nâng cấp trang bị của bạn đồng hành.")
	
	dialog:AddSelection(10, "Chế tạo trang bị đồng hành [Bích Huyết]")
	dialog:AddSelection(7, "Nâng cấp trang bị đồng hành" )

	dialog:Show(npc, player)
end

function DaLuyenDaiSu:OnSignetOperator(scene, npc, player) 
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Ngũ Hành Ấn/Lôi Đình Ấn giúp tăng hiệu quả ngũ hành. Ta có thể giúp bằng hữu nâng cao sức mạnh của Ngũ Hành Ấn/Lôi Đình Ấn.")
	dialog:AddText("Ngũ Hành Ấn có thể nhận miễn phí từ chưởng môn của môn phái đang tu luyện.")
	dialog:AddText("Sử dụng mảnh Lôi Đình Ấn có thể đúc được Lôi Đình Ấn từ Ngũ Hành Ấn, cũng như tăng giới hạn cấp cường/nhược hóa của Lôi Đình Ấn.")
	dialog:AddText("Lôi Đình Ấn có thể nâng cấp, tối đa 25 bậc để đạt sức mạnh mới.")

	dialog:AddSelection(5, "Tăng hiệu quả cường/nhược hóa")
	dialog:AddSelection(12, "Đúc Lôi Đình Ấn từ Ngũ Hành Ấn")
	dialog:AddSelection(13, "Tăng giới hạn cấp cường/nhược hóa")
	dialog:AddSelection(14, "Nâng cấp Lôi Đình Ấn")

	dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function DaLuyenDaiSu:OnSelection(scene, npc, player, selectionID,otherParams)
	if selectionID == 18 then 
		GUI:OpenComposeDialog(player, "", 1000, 1008, 1001, 1009, 1002, 1010, 1003, 1011, 1004, 1012, 1005, 1013, 1006, 1014, 1007, 1015)
		return
	end
	
	if selectionID == 16 then
		DaLuyenDaiSu:OnRefineOperator(scene, npc, player)
		return	
	end

	if selectionID == 17 then
		DaLuyenDaiSu:OnPetOpetaror(scene, npc, player)
		return	
	end

	if selectionID == 15 then
		DaLuyenDaiSu:OnSignetOperator(scene, npc, player)
		return
	end

	if selectionID == 12 then
		GUI:CloseDialog(player)
		GUI:OpenComposeDialog(player, "", 5)
		return
	end

	if selectionID == 13 then
		GUI:CloseDialog(player)
		GUI:OpenComposeDialog(player, "", 6, 7, 8, 9)
		return
	end

	if selectionID == 14 then
		GUI:CloseDialog(player)
		GUI:OpenComposeDialog(player, "", 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35)
		return
	end

	if selectionID == 11 then
		GUI:CloseDialog(player)
		GUI:OpenComposeDialog(player, "", 0, 1, 2, 3, 4)
		return
	end

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	if selectionID == 1 then
		GUI.OpenUI(player, "UIEnhance")
	elseif selectionID == 2 then
		GUI.OpenUI(player, "UIEquipLevelUp")
	
	elseif selectionID == 3 then
		GUI.OpenUI(player, "UICrystalStoneSynthesis")
	elseif selectionID == 4 then
		GUI.OpenUI(player, "UISplitCrystalStone", 10, 85)
	elseif selectionID == 5 then
		GUI.OpenUI(player, "UISignetEnhance")
	elseif selectionID == 6 then	
		GUI.OpenUI(player, "UISplitEquipCrystalStones", 1, 70)
	elseif selectionID == 7 then
		GUI.CloseDialog(player)
		-- nâng cấp trang bị đồng hành
		GUI:OpenComposeDialog(player, "", 
            100, 101, 147, 
            102, 103, 148,
            104, 105, 150,
            106, 107, 149,
            108, 109, 151,
            128, 145, 155,
            129, 146, 156,
            130, 142, 152,
            131, 144, 154, 
            132, 157, 162,
            133, 158, 163,
            134, 159, 164,
            135, 160, 165,
            136, 161, 166,
            137, 138, 139, 140)
		-- Player.LevelUpEquipPet(player)
	elseif selectionID == 8 then
		GUI.CloseDialog(player)
		Player.RepairAllItem(player)
	elseif selectionID == 9 then
		GUI.CloseDialog(player)
	elseif selectionID == 10 then
		-- chế tạo trang bị đồng hành bích huyết
		GUI:OpenComposeDialog(player, "", 114, 116, 117, 118, 110, 111, 112, 113, 115, 119, 120, 121, 122, 123, 124, 125, 126, 127) 
	elseif selectionID == 19 then
		GUI.OpenUI(player, "UIAdvanceEnhance")
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
function DaLuyenDaiSu:OnItemSelected(scene, npc, player, itemID,otherParams)

	-- ************************** --

	-- ************************** --

end