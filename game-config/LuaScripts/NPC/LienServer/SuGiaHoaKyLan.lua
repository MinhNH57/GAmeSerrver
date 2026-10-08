-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng

local SuGiaHoaKyLan = Scripts[000235]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --

function SuGiaHoaKyLan:OnOpen(scene, npc, player, otherParams)

	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Xin chào " .. player:GetName() .. "! Ta là sứ giả hoạt động Săn Boss Kỳ Lân.")
	local damageGuild = EventManager.BossUnicon_GetDamageGuild(player)
	if damageGuild ~= "" then
		dialog:AddText("\n Sát thương bang của bạn: " .. damageGuild .. "")
	end
	dialog:AddSelection(2, "Nhận thưởng Damge lên Boss")
	
	dialog:AddSelection(3, "<color=#51e1fb>Kết thúc đối thoại.</color>")
	
	--if Player.StateLeagueBattle() ~= 0 then
	--	dialog:AddSelection(3, "Nhận cược giải đấu")
	--end
	dialog:Show(npc, player)					

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --

function SuGiaHoaKyLan:OnSelection(scene, npc, player, selectionID, otherParams)

	local dialog = GUI.CreateNPCDialog()
	
	if selectionID == 1 then
		-- xem có  phải thằng kết thúc boss ko?
		
		return
	end
	
	if selectionID == 2 then
		-- xem có nằm trong ds dame ko?
		if EventManager.BossUnicon_CheckDamageUnicon(player) == 1 then
			EventManager.BossUnicon_GetAwardDamageUnicon(player)
		else
			SuGiaHoaKyLan:ShowNotify(npc, player, "Bạn không nằm trong danh sách nhận thưởng")
		end
		return
	end
	
	if selectionID == 3 then
		GUI.CloseDialog(player)
	end
	
	
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --

function SuGiaHoaKyLan:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end

function SuGiaHoaKyLan:ShowNotify(npc, player, text)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText(text)
	dialog:AddSelection(6, "Kết thúc đối thoại")
	dialog:Show(npc, player)
	-- ************************** --

end
