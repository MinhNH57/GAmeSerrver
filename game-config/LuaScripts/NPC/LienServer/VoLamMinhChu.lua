-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng

local VoLamMinhChu = Scripts[000229]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --

function VoLamMinhChu:OnOpen(scene, npc, player, otherParams)

	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Xin chào " .. player:GetName() .. "! Ta là sứ giả Võ Lâm Minh Chủ.")
	if Player.StateLeagueBattle() ~= 0 then
		dialog:AddSelection(2, "Tham gia vào giải đấu")
		-- Nhận thưởng khi đến thời gian nhận thưởng
		if Player.StateLeagueBattle() == 13 and not Player.KuaFuBalanceIsActive(player) then
			dialog:AddSelection(3, "Nhận thưởng cược VLMC")
			dialog:AddSelection(4, "Nhận thưởng tham gia VLMC")
			dialog:AddSelection(5, "Nhận thưởng xếp hạng VLMC")
		end
	end
	
	
	if not Player.KuaFuBalanceIsActive(player) then
		dialog:AddSelection(7, "Săn Boss Hỏa Kỳ Lân")
	
		dialog:AddSelection(8, "Báo danh Boss Hỏa Kỳ Lân")
	end
	
	dialog:AddSelection(6, "<color=#51e1fb>Kết thúc đối thoại.</color>")
	dialog:Show(npc, player)					

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --

function VoLamMinhChu:OnSelection(scene, npc, player, selectionID, otherParams)

	local dialog = GUI.CreateNPCDialog()
	
	if selectionID == 2 then
		Player.JoinLeagueBattle(npc, player)
		return
	end
	
	if selectionID == 3 then
		if Player.KuaFuBalanceIsActive(player) then
			VoLamMinhChu:ShowMessageBox(npc, player, "Ở Liên Server Cân Bằng không thể sử dụng chức năng này!")
			return
		end
		Player.GetAwardBetting(player)
		return
	end
	
	if selectionID == 4 then
		if Player.KuaFuBalanceIsActive(player) then
			VoLamMinhChu:ShowMessageBox(npc, player, "Ở Liên Server Cân Bằng không thể sử dụng chức năng này!")
			return
		end
		Player.GetAwardParticipation(player)
		return
	end
	
	if selectionID == 5 then
		if Player.KuaFuBalanceIsActive(player) then
			VoLamMinhChu:ShowMessageBox(npc, player, "Ở Liên Server Cân Bằng không thể sử dụng chức năng này!")
			return
		end
		Player.GetAwardTop(player)
		return
	end
	
	if selectionID == 6 then
		GUI.CloseDialog(player)
	end
	
	if selectionID == 7 then
		-- Kiểm tra điều kiện
		local ret = EventManager.BossUnicon_EnterMap_CheckCondition(player)
		-- Nếu không thỏa mãn
		if ret ~= "OK" then
			VoLamMinhChu:ShowMessageBox(npc, player, ret)
			return
		end
		
		-- Vào Tần Lăng
		EventManager.BossUnicon_MoveToMap(player)
		
		return
	end
	
	if selectionID == 8 then
		-- kiểm tra điều kiện
		local ret = EventManager.BossUnicon_Register_CheckCondition(player)
		if ret ~= "OK" then
			VoLamMinhChu:ShowMessageBox(npc, player, ret)
			return
		end
		-- đăng ký
		EventManager.BossUnicon_Register(player)
		VoLamMinhChu:ShowMessageBox(npc, player, ret)
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

function VoLamMinhChu:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end

function VoLamMinhChu:ShowMessageBox(npc, player, msg)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText(msg)
	dialog:Show(npc, player)
	-- ************************** --

end
