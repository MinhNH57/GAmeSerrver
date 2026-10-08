 -- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '0000152' bên dưới thành ID tương ứng
local ChuongMon = Scripts[0000152]

-- ************************** --
local LenhBaiTayTuyDao = 336
local TuLuyenChau = 215

local itemID = {
	[1] = {
		ItemID1 = 3747,			-- Thiếu Lâm Phái
		ItemID2 = 3757,
		ItemID3 = 3864,
		ItemID4 = 3232,
		ItemID5 = 3233,
	},
	[2] = {
		ItemID1 = 3767,			-- Thiên Vương Bang
		ItemID2 = 3777,
		ItemID3 = 3865,
		ItemID4 = 3234,
		ItemID5 = 3235,
	},
	[3] = {
		ItemID1 = 3834,			-- Đường Môn
		ItemID2 = 3844,
		ItemID3 = 3866,
		ItemID4 = 3236,
		ItemID5 = 3237,
	},
	[4] = {
		ItemID1 = 3727,			-- Ngũ Độc Giáo
		ItemID2 = 3747,
		ItemID3 = 3867,
		ItemID4 = 3238,
		ItemID5 = 3239,
	},
	[5] = {
		ItemID1 = 3727,			-- Nga My Phái
		ItemID2 = 3737,
		ItemID3 = 3868,
		ItemID4 = 3240,
		ItemID5 = 3241,
	},
	[6] = {
		ItemID1 = 3747,			-- Thúy Yên Môn
		ItemID2 = 3737,
		ItemID3 = 3869,
		ItemID4 = 3242,
		ItemID5 = 3243,
	},
	[7] = {
		ItemID1 = 3757,			-- Cái Bang Phái
		ItemID2 = 3727,
		ItemID3 = 3870,
		ItemID4 = 3244,
		ItemID5 = 3245,
	},
	[8] = {
		ItemID1 = 3767,			-- Thiên Nhẫn Giáo
		ItemID2 = 3747,
		ItemID3 = 3871,
		ItemID4 = 3246,
		ItemID5 = 3247,
	},
	[9] = {
		ItemID1 = 3737,			-- Võ Đang Phái
		ItemID2 = 3737,
		ItemID3 = 3872,
		ItemID4 = 3248,
		ItemID5 = 3249,
	},
	[10] = {
		ItemID1 = 3747,			-- Côn Lôn Phái
		ItemID2 = 3737,
		ItemID3 = 3873,
		ItemID4 = 3250,
		ItemID5 = 3251,
	},
	[11] = {
		ItemID1 = 3737,			-- Minh Giáo
		ItemID2 = 3777,
		ItemID3 = 3874,
		ItemID4 = 3252,
		ItemID5 = 3253,
	},
	[12] = {
		ItemID1 = 3727,			-- Đoàn Thị Phái
		ItemID2 = 3737,
		ItemID3 = 3875,
		ItemID4 = 3254,
		ItemID5 = 3255,
	},
	[13] = {
		ItemID1 = 3727,			-- Cổ Mộ Phái (tạm dùng bộ vật phẩm Đoàn Thị)
		ItemID2 = 3737,
		ItemID3 = 3875,
		ItemID4 = 3254,
		ItemID5 = 3255,
	},
}
-- ************************** --

--****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function ChuongMon:OnOpen(scene, npc, player,otherParams)

	-- ************************** --
	-- Nếu chưa có môn phái
	if player:GetFactionID() == 0 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Chưởng Môn: Ngươi có cốt cách dị thường tư chất hơn người, mai đây ắt sẽ làm nên nghiệp lớn. <br><color=#00ff44>Nếu muốn quay về Tân Thủ Thôn, hãy đến gặp người Truyền Tống Môn Phái</color>.")
		dialog:AddSelection(1, "Sư Phụ, xin nhận của đệ tử một lạy")
		dialog:AddSelection(2, "Để ta suy nghĩ sau")
		dialog:Show(npc, player)
	-- Nếu môn phái tồn tại
	elseif Global_NameMapItemPhaiID[scene:GetID()] ~= nil then
		-- Thông tin môn phái tương ứng
		local factionInfo = Global_NameMapItemPhaiID[scene:GetID()]
		
		-- Nếu trùng với môn phái này
		if factionInfo.FactionID == player:GetFactionID() then
			local dialog = GUI.CreateNPCDialog()
			dialog:AddText("Chưởng Môn: Lâu lắm không gặp ngươi " .. player:GetFactionName() .. ", nhận thấy ngươi rất có tố chất, xứng danh với môn đồ bản phái. Ngươi muốn sư phụ giúp gì?")
			dialog:AddSelection(3, "Nhận Ngũ hành ấn")
			dialog:AddSelection(4, "Nhận Mật tịch Môn Phái (sơ)")
			dialog:AddSelection(5, "Nhận Tu Luyện Châu")
			dialog:AddSelection(6, "Ta muốn mua trang bị thi đấu mốn phái")
			dialog:AddSelection(7, "Hoạt động Thi Đấu Môn Phái/Hoa Sơn Luận Kiếm")
			dialog:AddSelection(8, "Được phong Đại sư huynh(Đại sư tỷ)")
			dialog:AddSelection(9, "Ta muốn vào Tẩy Tủy Đảo Song tu các môn phái khác")
			dialog:AddSelection(10, "Ta muốn vào Tẩy Tủy Đảo đổi Song tu môn phái")
			local mainFaction = player:IsHasSongTu()
			local subFaction = player:GetResFaction()
			if (mainFaction == -1 and subFaction ~= -1) or (mainFaction ~= -1 and subFaction == -1) then
				dialog:AddSelection(11, "[Hỗ trợ] Khắc phục lỗi Song Tu")
			end
			dialog:AddSelection(2, "Để ta suy nghĩ sau")
			dialog:Show(npc, player)
		-- Nếu ở phái khác
		else
			local dialog = GUI.CreateNPCDialog()
			dialog:AddText("Chưởng Môn: Ngươi đến bản phái phải chăng là muốn thách thức tỷ võ?")
			dialog:AddSelection(2, "Kết thúc đối thoại")
			dialog:Show(npc, player)
			
		end
	else
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Chưa tìm thấy cấu hình môn phái cho bản đồ hiện tại (Map ID: " .. tostring(scene:GetID()) .. ").")
		dialog:AddSelection(2, "Kết thúc đối thoại")
		dialog:Show(npc, player)
	end
end

	-- ************************** --


-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function ChuongMon:OnSelection(scene, npc, player, selectionID,otherParams)
	if selectionID == 11 then
		Player:SetValueOfForeverRecore(player, 3000001, -1)
		Player:SetValueOfForeverRecore(player, 3000002, -1)
		Player:SetValueOfForeverRecore(player, 3000003, -1)
		Player:SetValueOfForeverRecore(player, 3000004, -1)
		Player:SetValueOfForeverRecore(player, 3000005, -1)
		Player:SetValueOfForeverRecore(player, 3000006, -1)
		Player:SetValueOfForeverRecore(player, 3000007, -1)
		Player:SetValueOfForeverRecore(player, 3000008, -1)
		Player:SetValueOfForeverRecore(player, 3000009, -1)
		player:AddNotification("Reset trạng thái song tu thành công")
		GUI.CloseDialog(player)
	end
	
	-- ************************** --
	if selectionID == 2 then
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 1 then
		-- Nếu đã có phái thì toác
		if player:GetFactionID() ~= 0 then
			self:ShowDialog(npc, player, "Ngươi đã gia nhập môn phái, không thể gia nhập thêm!")
			return
		end
		
		-- Nếu toác
		if Global_NameMapItemPhaiID[scene:GetID()] == nil then
			self:ShowDialog(npc, player, "Thông tin môn phái bị lỗi, hãy thử lại sau!")
			return
		end
		
		-- Kiểm tra túi đủ chỗ trống không
		if Player.HasFreeBagSpaces(player, 5) == false then
			self:ShowDialog(npc, player, "Ta có phần quà nhập môn dành cho ngươi, hãy sắp xếp <color=green>2 ô trống</color> trong túi để nhận!")
			return
		end
		
		-- kiểm tra xem đã hoàn thành xong nhiệm vụ Phục Mệnh Thu Di chưa?
		if  Player.IsCompleteTask(player, 122) == false then
			self:ShowDialog(npc, player, "Ngươi hãy hoàn thành các nhiệm vụ trước đó rồi quay lại sau.")
			return
		end
		
		-- Thông tin môn phái
		local factionInfo = Global_NameMapItemPhaiID[scene:GetID()]
		local factionItems = itemID[factionInfo.FactionID]
		if factionItems == nil then
			self:ShowDialog(npc, player, "Trang bị nhập môn của môn phái này chưa được cấu hình!")
			return
		end
		
		-- Cho vào môn phái
		self:JoinFaction(scene, npc, player, factionInfo.FactionID)
		
		---- Thêm 3 lệnh bài Tẩy Tủy Đảo
		--Player.AddItemLua(player, LenhBaiTayTuyDao, 3, -1, 1)
		
		-- Thêm trang bị hỗ trợ người chơi mới
		if factionInfo.FactionID == 9 then
			Player.AddItemLua(player, factionItems.ItemID1, 1, Player.GetSeries(player), 1)
		else
			Player.AddItemLua(player, factionItems.ItemID1, 1, Player.GetSeries(player), 1)
			Player.AddItemLua(player, factionItems.ItemID2, 1, Player.GetSeries(player), 1)
		end
		Player.AddItemLua(player, factionItems.ItemID3, 1, Player.GetSeries(player), 1)
		Player.AddItemLua(player, factionItems.ItemID4, 1, Player.GetSeries(player), 1)
		Player.AddItemLua(player, factionItems.ItemID5, 1, Player.GetSeries(player), 1) 
		return
	end
	-- ************************** --
	if selectionID == 3 then
		-- Nếu toác
		if Global_NameMapItemPhaiID[scene:GetID()] == nil then
			self:ShowDialog(npc, player, "Thông tin môn phái bị lỗi, hãy thử lại sau!")
			return
		end
		
		-- Thông tin môn phái
		local factionInfo = Global_NameMapItemPhaiID[scene:GetID()]
		
		-- Nếu không phải đệ tử môn phái
		if player:GetFactionID() ~= factionInfo.FactionID then
			self:ShowDialog(npc, player, "Ngươi không phải đệ tử bản phái!")
			return
		end
		
		-- Kiểm tra túi đủ chỗ trống không
		if Player.HasFreeBagSpaces(player, 1) == false then
			self:ShowDialog(npc, player, "Túi của ngươi đã đầy, hãy sắp xếp <color=green>1 ô trống</color> trong túi để nhận Ngũ Hành Ấn!")
			return
		end
		
		-- Thêm vật phẩm
		Player.AddItemLua(player, factionInfo.press, 1, Player.GetSeries(player), 1)
		
		-- Thông báo nhận thành công
		self:ShowDialog(npc, player, "Nhận Ngũ Hành Ấn thành công!")
		return
	end
	-- ************************** --
	if selectionID == 4 then
		-- Nếu toác
		if Global_NameMapItemPhaiID[scene:GetID()] == nil then
			self:ShowDialog(npc, player, "Thông tin môn phái bị lỗi, hãy thử lại sau!")
			return
		end
		
		-- Thông tin môn phái
		local factionInfo = Global_NameMapItemPhaiID[scene:GetID()]
		
		-- Nếu không phải đệ tử môn phái
		if player:GetFactionID() ~= factionInfo.FactionID then
			self:ShowDialog(npc, player, "Ngươi không phải đệ tử bản phái!")
			return
		end
		
		-- Kiểm tra túi đủ chỗ trống không
		if Player.HasFreeBagSpaces(player, 2) == false then
			self:ShowDialog(npc, player, "Túi của ngươi đã đầy, hãy sắp xếp <color=green>2 ô trống</color> trong túi để nhận Mật tịch môn phái (sơ)!")
			return
		end
		
		-- Thêm vật phẩm
		Player.AddItemLua(player, factionInfo.secret1, 1, Player.GetSeries(player), 1)
		Player.AddItemLua(player, factionInfo.secret2, 1, Player.GetSeries(player), 1)
		
		-- Thông báo nhận thành công
		self:ShowDialog(npc, player, "Nhận Ngũ Hành Ấn thành công!")

		return
	end
	-- ************************** --
	if selectionID == 5 then
		-- Nếu toác
		if Global_NameMapItemPhaiID[scene:GetID()] == nil then
			self:ShowDialog(npc, player, "Thông tin môn phái bị lỗi, hãy thử lại sau!")
			return
		end
		
		-- Thông tin môn phái
		local factionInfo = Global_NameMapItemPhaiID[scene:GetID()]
		
		-- Nếu không phải đệ tử môn phái
		if player:GetFactionID() ~= factionInfo.FactionID then
			self:ShowDialog(npc, player, "Ngươi không phải đệ tử bản phái!")
			return
		end
		
		-- Kiểm tra túi đủ chỗ trống không
		if Player.HasFreeBagSpaces(player, 1) == false then
			self:ShowDialog(npc, player, "Túi của ngươi đã đầy, hãy sắp xếp <color=green>1 ô trống</color> trong túi để nhận Mật tịch môn phái (sơ)!")
			return
		end
		
		-- Thêm vật phẩm
		Player.AddItemLua(player, TuLuyenChau, 1, -1, 1)
		
		-- Thông báo nhận thành công
		self:ShowDialog(npc, player, "Nhận Tu Luyện Châu thành công!")
		return
	end
	-- ************************** --
	if selectionID == 6 then
		if player:GetLevel() >= 10 then
			if player:GetFactionID() == 1 then
				Player.OpenShop(npc, player,25)
			elseif player:GetFactionID() == 2 then
				Player.OpenShop(npc, player, 26)
			elseif player:GetFactionID() == 3 then
				Player.OpenShop(npc, player, 27)
			elseif player:GetFactionID() == 4 then
				Player.OpenShop(npc, player, 29)
			elseif player:GetFactionID() == 5 then
				Player.OpenShop(npc, player, 31)
			elseif player:GetFactionID() == 6 then
				Player.OpenShop(npc, player, 32)
			elseif player:GetFactionID() == 7 then
				Player.OpenShop(npc, player, 34)
			elseif player:GetFactionID() == 8 then
				Player.OpenShop(npc, player, 33)
			elseif player:GetFactionID() == 9 then
				Player.OpenShop(npc, player, 35)
			elseif player:GetFactionID() == 10 then
				Player.OpenShop(npc, player, 36)
			elseif player:GetFactionID() == 11 then
				Player.OpenShop(npc, player, 28)
			elseif player:GetFactionID() == 12 then
				Player.OpenShop(npc, player, 30)
			elseif player:GetFactionID() == 13 then
				self:ShowDialog(npc, player, "Cửa hàng trang bị thi đấu Cổ Mộ chưa được cấu hình!")
				return
			end
			GUI.CloseDialog(player)
		else
			self:ShowDialog(npc, player, "Ngươi chưa đủ cấp để có thể tham quan cửa hàng!")
		end
		
		return
	end
	-- ************************** --
	if selectionID==103 then
		-- Nếu toác
		if Global_NameMapItemPhaiID[scene:GetID()] == nil then
			self:ShowDialog(npc, player, "Thông tin môn phái bị lỗi, hãy thử lại sau!")
			return
		end
		
		-- Thông tin môn phái
		local factionInfo = Global_NameMapItemPhaiID[scene:GetID()]
		
		-- Nếu không phải đệ tử môn phái
		if player:GetFactionID() ~= factionInfo.FactionID then
			self:ShowDialog(npc, player, "Ngươi không phải đệ tử bản phái!")
			return
		end
		
		-- Đưa vào đấu trường thi đấu môn phái
		Player.JoinFactionBattle(npc, player)
		return
	end
	
	if selectionID == 7 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Hoạt động Thi Đấu Môn Phái diễn ra vào tối thứ 6 hàng tuần.\nHoạt động Hoa Sơn Luận Kiếm diễn ra vào tối thứ 2 hàng tuần.\nThời gian bắt đầu báo danh: 19:00.\n<color=#f38ba8>Điểm của mỗi lần thi đấu sẽ được thay đổi ở những lần thi tiếp theo</color>.")
		dialog:AddSelection(103, "Ta muốn tham gia thi đấu")
		dialog:AddSelection(2, "Ta đã hiểu")
		dialog:Show(npc, player)
		return
	end
	-- ************************** --
	if selectionID == 8 then
		self:ShowDialog(npc, player, "Chức năng này đang được phát triển!")
		return
	end
	-- ************************** --
	if selectionID == 9 then
		--đã vào phái chưa?
		if player:GetFactionID() <= 0 then
			self:ShowDialog(npc, player, "Bạn chưa vào phái!")
			return
		end
		
		-- xem đủ level >=100 chưa?
		if player:GetLevel() < 79 then
			self:ShowDialog(npc, player, "Bạn hãy đạt level 79 rồi quay lại!")
			return
		end
		
		-- xem đã song tu chưa?
		if player:IsHasSongTu() > 0 then
			System.WriteToConsole("Song Tu ID" .. player:IsHasSongTu() .. " " .. player:GetResFaction())
			self:ShowDialog(npc, player, "Mỗi người chỉ được miễn phí 1 lần song tu!")
			return
		end
		
		-- xem trên người có tu luyện châu chưa?
		local numberTLC = player:IsHasItem(215)
		if numberTLC <=0 then
			self:ShowDialog(npc, player, "Bạn hãy mang theo Tu luyện châu trước!")
			return
		end
		
		-- xem đủ 3 ngày chưa?
		if player:CheckDaySongTu() == false then
			self:ShowDialog(npc, player, "3 ngày mới được song tu 1 lần")
			return
		end
		
		-- lưu lại faction chính của người chơi
		player:SetHasSongTu(player:GetFactionID())
		
		-- set trạng thái trước khi vào
		player:SetStateSongTu(1)
		
		-- cho vào tẩy tủy đảo
		player:ChangeScene(255, 1750, 1930)
		GUI:CloseDialog(player)
		return
	end
	
	if selectionID == 10 then
		-- xem đủ level >=100 chưa?
		if player:GetLevel() < 79 then
			self:ShowDialog(npc, player, "Bạn hãy đạt level 79 rồi quay lại!")
			return
		end
		-- xem trên người có tu luyện châu chưa?
		local numberTLC = player:IsHasItem(215)
		if numberTLC <=0 then
			self:ShowDialog(npc, player, "Bạn hãy mang theo Tu luyện châu trước!")
			return
		end
		
		-- xem đã có môn phái phụ chưa
		if player:GetResFaction() <= 0 then
			self:ShowDialog(npc, player, "Bạn chưa có môn phái Song Tu để thay đổi!")
			return
		end
		
		---- xem đủ 3 ngày chưa?
		--if player:CheckDaySongTu() == false then
		--	self:ShowDialog(npc, player, "3 ngày mới được song tu 1 lần")
		--	return
		--end
		
		-- xem đã quá số lần chưa?
		local numChangeSongTu = player:GetNumChangeSongTu()
		
		if numChangeSongTu < 0 then 
			numChangeSongTu = 0
		end
		
		if  numChangeSongTu >=1 then 
			self:ShowDialog(npc, player, "Bạn đã hết lượt đổi Song tu, cần dùng bổ tu lệnh để đổi")
			return
		end
		
		-- lưu lại số lượt đổi song tu		
		player:SetNumChangeSongTu()
		
		-- set trạng thái trước khi vào
		player:SetStateSongTu(1)
		
		-- cho vào tẩy tủy đảo
		player:ChangeScene(255, 1750, 1930)
		GUI:CloseDialog(player)
		return
	end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function ChuongMon:OnItemSelected(scene, npc, player, itemID,otherParams)

	-- ************************** --
	
	-- ************************** --

end


function ChuongMon:JoinFaction(scene, npc, player, factionID)
	
	-- ************************** --
	local ret = player:JoinFaction(factionID)
	-- ************************** --
	if ret == -1 then
		ChuongMon:ShowDialog(npc, player, "Người chơi không tồn tại!")
		return
	elseif ret == -2 then
		ChuongMon:ShowDialog(npc, player, "Môn phái không tồn tại!")
		return
	elseif ret == 0 then
		ChuongMon:ShowDialog(npc, player, "Giới tính của bạn không phù hợp với môn phái này!")
		return
	elseif ret == 1 then
		-- ChuongMon:ShowDialog(npc, player, "Gia nhập phái <color=blue>" .. player:GetFactionName() .. "</color> thành công! Nhận được vũ khí môn phái")
		GUI:CloseDialog(player)
		GUI:OpenUI(player, "Messages/UIFactionInstruction-2")
		GUI:OpenUI(player, "Messages/UIFactionInstruction")
		return
	else
		ChuongMon:ShowDialog(npc, player, "Chuyển phái thất bại, lỗi chưa rõ!")
		return
	end
	-- ************************** --

end


function ChuongMon:ShowDialog(npc, player, text)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText(text)
	dialog:Show(npc, player)
	-- ************************** --
	
end
