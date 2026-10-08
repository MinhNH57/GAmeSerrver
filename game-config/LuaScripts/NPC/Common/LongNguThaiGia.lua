-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000148' bên dưới thành ID tương ứng
local LongNguThaiGia = Scripts[000148]

--****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
local numberBookCN = 0
local idBookTrade = 0
local idItemBookCN = 25271
local idItemBookPet = 1057
local idItemBookPetPro = 1058
local idItemExpCN = 25270
local recordExpCN = 25270
local ExpTradeExpCN = {
    [1] = {
		Exp = 100000000,
		NumberBC = 0,
		NumberExpCN = 10
	},
	[2] = {
		Exp = 100000000,
		NumberBC = 1,
		NumberExpCN = 10
	},
	[3] = {
		Exp = 150000000,
		NumberBC = 1,
		NumberExpCN = 10
	},
	[4] = {
		Exp = 150000000,
		NumberBC = 1,
		NumberExpCN = 10
	},
	[5] = {
		Exp = 150000000,
		NumberBC = 1,
		NumberExpCN = 10
	}
}
function LongNguThaiGia:OnOpen(scene, npc, player,otherParams)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Ta có thể giúp gì cho ngươi?")
	dialog:AddSelection(1, "Tạp Hóa Nguyệt Ảnh Thạch")
	dialog:AddSelection(2, "Tạp Hóa Du Long")
	dialog:AddSelection(3, "Vào Mật Thất Du Long")
	dialog:AddSelection(4, "Giải tán bạn đồng hành cấp cao")
	dialog:AddSelection(5, "Hủy giải tán bạn đồng hành cấp cao")
	
	dialog:AddSelection(7, "Chế tạo vật phẩm hỗ trợ")
	if player:IsGM() or Global_EarlyAccess[player:GetName()] then
		dialog:AddSelection(6, "Tạp hóa Admin")
	end
	dialog:AddSelection(8, "Đổi kinh nghiệm chân nguyên")
	dialog:AddSelection(100, "Ta chỉ tình cờ đi qua")
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
function LongNguThaiGia:OnSelection(scene, npc, player, selectionID, otherParams)

	if selectionID == 7 then
		GUI:OpenComposeDialog(player, "", 1000, 1008, 1001, 1009, 1002, 1010, 1003, 1011, 1004, 1012, 1005, 1013, 1006, 1014, 1007, 1015, 1020, 1021, 1022)
		return
	end

	-- ************************** --
	if selectionID == 1 then
		Player.OpenShop(npc, player, 166)
		GUI.CloseDialog(player)
	elseif selectionID == 2 then
		Player.OpenShop(npc, player, 167)
		GUI.CloseDialog(player)
	elseif selectionID == 3 then
		local dialog = GUI.CreateNPCDialog()

		-- Kiểm tra điều kiện
		local ret = EventManager.YouLong_CheckCondition(player)
		if ret ~= "OK" then
			dialog:AddText(ret)
			dialog:Show(npc, player)
			return
		end
		
		-- Vào phụ bản
		EventManager.YouLong_Begin(player)
		-- dialog:AddText("Chức năng chưa mở")
		dialog:Show(npc, player)
	elseif selectionID == 4 then
		player:ShowDialogRemovePetNPC()
	elseif selectionID == 5 then
		player:ShowDialogUnRemovePetNPC()
	elseif selectionID == 6 then
		Player.OpenShop(npc, player, 1456)
	elseif selectionID == 8 then
		-- kiểm tra xem có sách kinh nghiệm chân nguyên ko
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Hãy chọn hình thức đổi")
		dialog:AddText("\n - Sử dụng sách kinh nghiệm đồng hành, trong đó, sách kinh nghiệm đồng hành - thường nhận 10 điểm kinh nghiệm chân nguyên, sách kinh nghiệm đồng hành (truyền thuyết) nhận 800 kinh nghiệm chân nguyên")
		dialog:AddText("\n - Đổi kinh nghiệm của nhân vật bản thân thành kinh nghiệm chân nguyên, khi đổi kinh nghiệm chân nguyên cần trang bị phi phong Ngự Không trở lên, người chơi sẽ bị trừ đi số lượng điểm kinh nghiệm của nhân vật và bí cảnh tương ứng số lượt, 1 ngày có thể đổi kinh nghiệm tối đa 5 lần.")
		dialog:AddText("        + Lượt 1 cần 100000000 exp, nhận được 10 kinh nghiệm chân nguyên")
		dialog:AddText("        + Lượt 2 cần 100000000 exp và 1 bí cảnh nhận được 10 kinh nghiệm chân nguyên")
		dialog:AddText("        + Lượt 3 cần 150000000 exp và 1 bí cảnh nhận được 10 kinh nghiệm chân nguyên")
		dialog:AddText("        + Lượt 4 cần 150000000 exp và 1 bí cảnh nhận được 10 kinh nghiệm chân nguyên")
		dialog:AddText("        + Lượt 5 cần 150000000 exp và 1 bí cảnh nhận được 10 kinh nghiệm chân nguyên")
		dialog:AddText("\nKinh nghiệm chân nguyên nhận được khi đổi sẽ khóa")
		dialog:AddSelection(9, "Đổi sách kinh nghiệm chân nguyên")
		dialog:AddSelection(10, "Đổi sách exp đồng hành")
		dialog:AddSelection(11, "Đổi sách exp đồng hành truyền thuyết")
		dialog:AddSelection(14, "Đổi kinh nghiệm bản thân")
		dialog:Show(npc, player)
	elseif selectionID == 9 then
		numberBookCN = Player.CountItemInBag(player, idItemBookCN) 
		idBookTrade = idItemBookCN
		if  numberBookCN > 0 then
			local dialog = GUI.CreateNPCDialog()
			dialog:AddText("Bạn đang có " .. numberBookCN .. " quyển sách kinh nghiệm chân nguyên. Hãy chọn hình thức đổi")
			dialog:AddSelection(12, "Đổi 1 quyển")
			dialog:AddSelection(13, "Đổi " .. numberBookCN .. " quyển")
			dialog:Show(npc, player)
		else
			GUI:ShowMessageBox(player, "Thông báo", "Bạn không có sách kinh nghiệm chân nguyên nào.")
		end
	elseif selectionID == 10 then
		numberBookCN = Player.CountItemInBag(player, idItemBookPet) 
		idBookTrade = idItemBookPet
		if  numberBookCN > 0 then
			local dialog = GUI.CreateNPCDialog()
			dialog:AddText("Bạn đang có " .. numberBookCN .. " quyển sách kinh nghiệm đồng hành. Hãy chọn hình thức đổi")
			dialog:AddSelection(12, "Đổi 1 quyển")
			dialog:AddSelection(13, "Đổi " .. numberBookCN .. " quyển")
			dialog:Show(npc, player)
		else
			GUI:ShowMessageBox(player, "Thông báo", "Bạn không có sách kinh nghiệm đồng hành nào.")
		end
	elseif selectionID == 11 then
		numberBookCN = Player.CountItemInBag(player, idItemBookPetPro) 
		idBookTrade = idItemBookPetPro
		if  numberBookCN > 0 then
			local dialog = GUI.CreateNPCDialog()
			dialog:AddText("Bạn đang có " .. numberBookCN .. " quyển sách kinh nghiệm đồng hành truyền thuyết. Hãy chọn hình thức đổi")
			dialog:AddSelection(12, "Đổi 1 quyển")
			dialog:AddSelection(13, "Đổi " .. numberBookCN .. " quyển")
			dialog:Show(npc, player)
		else
			GUI:ShowMessageBox(player, "Thông báo", "Bạn không có sách kinh nghiệm đồng hành truyền thuyết nào.")
		end
	elseif selectionID == 12 then
		Player.RemoveItem(player, idBookTrade, 1)
		if idBookTrade == idItemBookPetPro then
			Player.AddItemLua(player, idItemExpCN, 800 , 0, 1)
		else
			Player.AddItemLua(player, idItemExpCN, 10 , 0, 1)
		end
		GUI.CloseDialog(player)
		return
	elseif selectionID == 13 then
		Player.RemoveItem(player, idBookTrade, numberBookCN)
		if idBookTrade == idItemBookPetPro then
			Player.AddItemLua(player, idItemExpCN, 800 * numberBookCN , 0, 1)
		else
			Player.AddItemLua(player, idItemExpCN, 10 * numberBookCN , 0, 1)
		end
		GUI.CloseDialog(player)
		return
	elseif selectionID == 14 then
		if player:GetMantleLevel() < 5 then
			GUI:ShowMessageBox(player, "Thông báo", "Bạn phải đeo phi phong Ngự Không trở lên")
			return
		else
			local recordExpTrade = Player:GetValueOfDailyRecore(player, recordExpCN)
			if recordExpTrade < 0 then
				recordExpTrade = 0
			end
			if recordExpTrade >= 5 then
				GUI:ShowMessageBox(player, "Thông báo", "Bạn đã đạt giới hạn đổi kinh nghiệm bản thân trong ngày hôm nay. Hãy quay lại vào ngày mai.")
				return
			else
				if Player.CountItemInBag(player, 590) > 0 then
					if Player:MinusExperience(player, ExpTradeExpCN[recordExpTrade + 1].Exp) == true then
						if ExpTradeExpCN[recordExpTrade + 1].NumberBC > 0 then
							Player.RemoveItem(player, 590, ExpTradeExpCN[recordExpTrade + 1].NumberBC)
						end
						Player.AddItemLua(player, idItemExpCN, ExpTradeExpCN[recordExpTrade + 1].NumberExpCN , 0, 1)
						Player:SetValueOfDailyRecore(player, recordExpCN, recordExpTrade + 1)
						GUI:ShowMessageBox(player, "Thông báo", "Bạn đã đổi thành công, nhận được " .. ExpTradeExpCN[recordExpTrade + 1].NumberExpCN .. " kinh nghiệm chân nguyên");
						return
					else
						GUI:ShowMessageBox(player, "Thông báo", "Bạn chưa có đủ kinh nghiệm, cần " .. ExpTradeExpCN[recordExpTrade + 1].Exp .. " kinh nghiệm");
						return
					end
				else
					GUI:ShowMessageBox(player, "Thông báo", "Bạn không có bí cảnh")
					return
				end
			end
		end
		
	elseif selectionID == 100 then
		GUI.CloseDialog(player)
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
function LongNguThaiGia:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --

	-- ************************** --

end