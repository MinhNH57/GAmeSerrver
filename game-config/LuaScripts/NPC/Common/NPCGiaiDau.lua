-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng

local NPCGiaiDau = Scripts[000231]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
local TEAM_INFO = {
	{ PotID = 1, TeamID = 1, Name = "ĐAM MÊ TỬU SẮC", Player1 = "Anh7CờTỷPhú", Player2 = "SềEmQuang", Point = 3},
	
}
local MATCH_INFO = {
	--{ Day = 27 , ID = 1, ID1 = 11, Name1= "ĐAM MÊ TỬU SẮC", ID2 = 12, Name2 = "BộĐôiGạtGiò" },
}

local LEVEL_BET = {
	{ ID = 1, Level = 1, Number = 100},
	{ ID = 2, Level = 2, Number = 1000},
}

local SERVER_HAS_BUFF = {
	[21] = true, -- chỉ server 21 có bán buff hỗ trợ
}

local PLAYERS_HAS_BUFF = {
	[123456] = true, -- ví dụ player có ID 123456 có thể nhận buff hỗ trợ
}

function NPCGiaiDau:OnOpen(scene, npc, player, otherParams)

	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Xin chào " .. player:GetName() .. "! Ta là sứ giả hoạt động. Bằng hữu có thể mua gói trang bị hỗ trợ")

	-- dialog:AddSelection(1001, "Nâng cấp mặt nạ")
	-- if player:IsGM() or System:IsDateTimeInRange('2024-04-27 05:00:00', '2024-05-04 05:00:00') then
	-- 	dialog:AddSelection(1002, "Check-in Đại Lễ")
	-- end

	-- dialog:AddSelection(1, "Tham gia cược")
	-- dialog:AddSelection(3, "Xem thông tin các đội tham gia giả đấu")
	--dialog:AddSelection(8, "Dịch chuyển đến Hoàng Thành LSV giải đấu")
	-- dialog:AddSelection(2, "Để ta suy nghĩ ...")
	--dialog:AddSelection(1, "Gói trang bị hỗ trợ giải đấu")
	-- nếu player là GM hoặc nằm trong server có bán buff hỗ trợ thì mới hiển thị lựa chọn nhận buff hỗ trợ giải đấu
	if player:IsGM() or SERVER_HAS_BUFF[System:GetCreatedZoneID()] or PLAYERS_HAS_BUFF[player:GetID()] then
		dialog:AddSelection(1011, "Nhận buff hỗ trợ thường")
		dialog:AddSelection(1012, "Nhận buff hỗ trợ VIP")
	end
	dialog:AddSelection(2, "Để ta suy nghĩ ...")
	dialog:Show(npc, player)					

end


function NPCGiaiDau:DoCheckIn(scene, npc, player, checkin_level)
	local CHECKIN_CONFIG = {
		{
			Package = 56,
			MoneyType = 0,
			Money = 500000,
			MoneyName = 'Bạc khóa',
			Record = 5000117,
			Limit = 1
		},
		{
			Package = 57,
			MoneyType = 2,
			Money = 250,
			MoneyName = 'KNB',
			Record = 5000118,
			Limit = 5
		},
		{
			Package = 58,
			MoneyType = 2,
			Money = 2500,
			MoneyName = 'KNB',
			Record = 5000119,
			Limit = 2
		}
	}

	local CHECKIN_CONFIG_21 = {
		{
			Package = 59,
			MoneyType = 0,
			Money = 300000,
			MoneyName = 'Bạc khóa',
			Record = 5000117,
			Limit = 1
		},
		{
			Package = 60,
			MoneyType = 2,
			Money = 250,
			MoneyName = 'KNB',
			Record = 5000118,
			Limit = 5
		},
		{
			Package = 61,
			MoneyType = 2,
			Money = 2500,
			MoneyName = 'KNB',
			Record = 5000119,
			Limit = 3
		}
	}

	local config = CHECKIN_CONFIG[checkin_level]
	if System:GetServerID() == 21 then
		config = CHECKIN_CONFIG_21[checkin_level]
	end
	
	local checkin_today = Player:GetValueOfDailyRecore(player, config.Record)
	if checkin_today < 0 then
		checkin_today = 0
	end

	if checkin_today >= config.Limit then
		GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã check-in đủ số lượt cho phép trong ngày. Vui lòng quay trở lại vào ngày mai.")
		return
	end

	GUI:ShowMessageBox(player, "Thông báo", "Xác nhận dùng " .. config.Money .. " " .. config.MoneyName .. " để thực hiện check-in đại lễ?",
		function() 
			if Player.CheckMoney(player, config.MoneyType) < config.Money then
				GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu không có đủ ' .. config.MoneyName .. '.')
				return
			end

			if Player:SubMoney(player, config.Money, config.MoneyType, "CHECKIN_DAILE") then
				Player:SetValueOfDailyRecore(player, config.Record, checkin_today + 1)
				if not Global_ReceiveItemPack(npc, player, config.Package) then
					GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.") 
				end
			else
				GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.") 
			end
		end
	)	
end


function NPCGiaiDau:CheckInDaiLe(scene, npc, player)
	local dialog = GUI.CreateNPCDialog()
	local checkin_low = Player:GetValueOfDailyRecore(player, 5000117)
	if checkin_low < 0 then checkin_low = 0 end
	local checkin_mid = Player:GetValueOfDailyRecore(player, 5000118)
	if checkin_mid < 0 then checkin_mid = 0 end
	local checkin_high = Player:GetValueOfDailyRecore(player, 5000119)
	if checkin_high < 0 then checkin_high = 0 end

	dialog:AddText("\t- Check-in [Sơ]:" .. checkin_low .. "/1 lượt.")
	dialog:AddText("\t- Check-in [Trung]:" .. checkin_mid .. "/5 lượt.")
	dialog:AddText("\t- Check-in [Cao]:" .. checkin_high .. "/2 lượt.")

	if System:GetServerID() == 21 then
		dialog:AddSelection(1003, "Check-in [sơ] - 30v bạc khóa")
	else
		dialog:AddSelection(1003, "Check-in [sơ] - 50v bạc khóa")
	end
	dialog:AddSelection(1004, "Check-in [trung] - 250 KNB")
	dialog:AddSelection(1005, "Check-in [cao] - 2500 KNB")
	if player:IsGM() then
		dialog:AddSelection(1009, "Reset Check-in")
	end
	dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --

function NPCGiaiDau:OnSelection(scene, npc, player, selectionID, otherParams)	
	--if selectionID == 1 then
	--	if System:GetHour() < 7 or System:GetHour() > 17 then
	--		GUI:ShowMessageBox(player, "Thông báo", "Không phải thời gian để đặt cược!")
    --        GUI:CloseDialog(player)
    --        return
	--	end
	--	
	--	local dialog = GUI.CreateNPCDialog()
	--	local hasMatch = false
	--	
	--	for k, v in pairs(MATCH_INFO) do
    --        if v.Day == System:GetDate() then
    --            hasMatch = true
    --        end			
    --    end
	--	
    --    if not hasMatch then
    --        GUI:ShowMessageBox(player, "Thông báo", "Chưa có trận đấu nào sắp diễn ra, vui lòng quay lại sau!")
    --        GUI:CloseDialog(player)
    --        return
    --    end
	--	
	--	dialog:AddText("Chào mừng bạn đã tham gia cược giải đấu lần này. Hãy chọn mức cược:")
	--	for k, v in pairs(LEVEL_BET) do
	--		dialog:AddSelection(v.Level * 10, "Mức " .. v.Number .. " KNB")
	--	end
	--	dialog:Show(npc, player)
	--end
	
	if selectionID == 1 then
		GUI:ShowMessageBox(player, "Thông báo", "Xác nhận dùng 200 KNB để mua gói hỗ trợ giải đấu",
			function()
				-- xem có đủ tiền ko?
				if Player.CheckMoney(player, 2) < 200 then
					GUI:ShowMessageBox(player,"Thông báo", "Đồng hữu không có đủ 200 KNB")
				else
					-- xem vào phái Chưa
					if player.GetFactionID() == 0 then
						GUI:ShowMessageBox(player, "Thông báo", "Đồng hữu chưa vào phái")
					else
						local n_series = Player.GetSeries(player)
						local n_sex = player:GetSex()
						local lua_itempack = n_series * 10 + n_sex
						local received = Global_BattleItemPack(npc, player, lua_itempack)
						if received then
							Player.MinusMoney(player, 2, 200)
							local name = LUA_ITEMPACKS[lua_itempack].Name
							dialog:AddText("Chúc mừng bằng hữu đã nhận <color=#a6e3a1>" .. name .. "</color> thành công.")
						end
					end						
				end
			end,
			function()
				GUI:CloseDialog(player)
			end)
	end

	if selectionID == 1001 then
		if System:GetServerID() == 21 then
			GUI:OpenComposeDialog(player, "", 405, 406, 407, 408, 409)			
		else 
			GUI:OpenComposeDialog(player, "", 400, 401, 402, 403, 404)			
		end
		GUI:CloseDialog(player)
		return
	end

	if selectionID == 1002 then
		NPCGiaiDau:CheckInDaiLe(scene, npc, player)
		return
	end

	if selectionID == 1003 then
		NPCGiaiDau:DoCheckIn(scene, npc, player, 1)
		return
	end

	if selectionID == 1004 then
		NPCGiaiDau:DoCheckIn(scene, npc, player, 2)
		return
	end

	if selectionID == 1005 then
		NPCGiaiDau:DoCheckIn(scene, npc, player, 3)
		return
	end

	if selectionID == 1009 then
		Player:SetValueOfDailyRecore(player, 5000117, -1)
		Player:SetValueOfDailyRecore(player, 5000118, -1)
		Player:SetValueOfDailyRecore(player, 5000119, -1)
	end
	
	if selectionID >= 10 and selectionID < 100 then
		-- xem đủ tiền ko
		local levelKNB = selectionID / 10
		for k, v in pairs(LEVEL_BET) do
			if v.ID == levelKNB then
				if Player.CheckMoney(player, 2) < v.Number then
					GUI:ShowMessageBox(player, "Thông báo", "Bạn không đủ KNB để đặt cược!")
				end
			end
		end
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Bạn hãy chọn trận đấu để cược, lưu ý mỗi trận chỉ có thể cược 1 lần")
		for k, v in pairs(MATCH_INFO) do
            if v.Day == System:GetDate() then
                dialog:AddSelection(v.ID  + selectionID * 10, "Trận đấu: " .. v.Name1 .. " và " ..v.Name2 .. "") 
            end
        end 
		dialog:Show(npc, player)
	end
	
	if selectionID >= 100 and selectionID < 1000 then
		local matchID = selectionID % 100
		local recoreID = 1000000 + matchID
		-- xem xét đã cược trận này chưa
		if Player:GetValueOfDailyRecore(player, recoreID) ~= -1 then
			GUI:ShowMessageBox(player, "Thông báo", "Bạn đã đặt cược trận đấu này trước đó!")
		else
			local dialog = GUI.CreateNPCDialog()
			dialog:AddText("Bạn đã chọn trận đấu, hãy chọn đội thắng trong trận đấu này")
			for k, v in pairs(MATCH_INFO) do
				if v.ID == matchID then
					dialog:AddSelection(v.ID1  + selectionID * 100, "Đội: " .. v.Name1 .. "") 
					dialog:AddSelection(v.ID2  + selectionID * 100, "Đội: " .. v.Name2 .. "")
				end
			end
			dialog:Show(npc, player)
        end
	end
	
	if selectionID >= 10000 then
		local teamID = selectionID % 100
		local matchID = (selectionID - teamID) / 100
		matchID = matchID % 100
		local levelBet = (selectionID - teamID - matchID * 100) / 10000
		local recoreID = 1000000 + matchID
		-- trừ tiền
		local numberKNB = 100
		for k, v in pairs(LEVEL_BET) do
			if v.Level == levelBet then
				numberKNB = v.Number
			end
		end
		
		if Player.MinusMoney(player, 2, numberKNB) then
			-- ghi log
			System:WriteDBLog(player, 'CUOCGIAIDAU', '', '' .. player:GetID(), '', '', '' .. teamID, '' .. matchID, '' .. numberKNB, '')
			System:WriteLog(117, player:GetID() .. ' đã đặt cược cho ' .. teamID .. ' trong trận ' .. matchID .. " mức cược " .. numberKNB .. " KNB")
			-- ghi vào bộ nhớ
			local recoreID = 1000000 + matchID
			Player:SetValueOfDailyRecore(player, recoreID, 1)
			GUI:CloseDialog(player)
			GUI:ShowMessageBox(player, "Thông báo", "Bạn đã đặt cược trận đấu này thành công!")
		end		
	end
	
	if selectionID == 3 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Bảng xếp hạng các đội")
		dialog:AddSelection(4, "Bảng A")
		dialog:AddSelection(5, "Bảng B")
		--dialog:AddSelection(6, "Bảng C")
		--dialog:AddSelection(7, "Bảng D")
		dialog:Show(npc, player)
	end
	
	if selectionID == 4 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Bảng A:")
		for k, v in pairs(TEAM_INFO) do
			if v.PotID == 1 then
				dialog:AddText("\n " .. v.Name .. " (" .. v.Player1 .. ", " .. v.Player2 .. ") : " .. v.Point .. " điểm")
			end
		end
		dialog:AddSelection(2, "Đóng")
		dialog:Show(npc, player)
	end
	
	if selectionID == 5 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Bảng A:")
		for k, v in pairs(TEAM_INFO) do
			if v.PotID == 2 then
				dialog:AddText("\n " .. v.Name .. " (" .. v.Player1 .. ", " .. v.Player2 .. ") : " .. v.Point .. " điểm")
			end
		end
		dialog:AddSelection(2, "Đóng")
		dialog:Show(npc, player)
	end
	
	if selectionID == 6 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Bảng A:")
		for k, v in pairs(TEAM_INFO) do
			if v.PotID == 3 then
				dialog:AddText("\n " .. v.Name .. " (" .. v.Player1 .. ", " .. v.Player2 .. ") : " .. v.Point .. " điểm")
			end
		end
		dialog:AddSelection(2, "Đóng")
		dialog:Show(npc, player)
	end
	
	if selectionID == 7 then
		local dialog = GUI.CreateNPCDialog()
		dialog:AddText("Bảng A:")
		for k, v in pairs(TEAM_INFO) do
			if v.PotID == 4 then
				dialog:AddText("\n " .. v.Name .. " (" .. v.Player1 .. ", " .. v.Player2 .. ") : " .. v.Point .. " điểm")
			end
		end
		dialog:AddSelection(2, "Đóng")
		dialog:Show(npc, player)
	end
	
	if selectionID == 8 then
		if Global_EarlyAccess[player:GetName()] or player:IsGM() then
            dialog:AddText("Bạn đang có </color=yellow>" .. Player.CheckMoney(player, 6) ..
                           " Bạc khóa liên máy chủ</color>, bạn có muốn vào liên máy chủ không?")

            dialog:AddSelection(110011, "Vào liên máy chủ");
            dialog:AddSelection(110012, "Chưa muốn vào lúc này");

            dialog:Show(npc, player)

            return
        end
        if player:GetLevel() < 79 then
            dialog:AddText("Bạn cần đạt cấp 79 để vào liên máy chủ.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end
        if player:GetMantleValue() <= 3000000 then
            dialog:AddText(
                "Bạn cần trang bị phi phong cấp Lăng Tuyệt trở lên để vào liên máy chủ.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end
        dialog:AddText("Bạn đang có </color=yellow>" .. Player.CheckMoney(player, 6) ..
                           " Bạc khóa liên máy chủ</color>, bạn có muốn vào liên máy chủ không?")

        dialog:AddSelection(9, "Vào liên máy chủ");
        dialog:AddSelection(2, "Chưa muốn vào lúc này");

        dialog:Show(npc, player)

        return
	end
	
	if selectionID == 9 then
		player:ChangeScene(1616, 7545, 4454, 9004)
        GUI.CloseDialog(player)
        return
	end

	if selectionID == 1011 then
		if player:HasBuff(2064) then
			GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã nhận buff hỗ trợ thường, không thể nhận thêm.")
			return
		end
		if player:HasBuff(2065) then
			GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã nhận buff hỗ trợ VIP, không thể nhận thêm.")
			return
		end
		player:AddBuffWithDuration(2064, 1, 24 *60 * 60 * 1000, 1, true)
		-- thêm thông báo
		player:AddNotification("Nhận buff hỗ trợ thường thành công, buff có hiệu lực trong 24h.")
		GUI:CloseDialog(player)
		return
	end

	if selectionID == 1012 then
		-- nếu đủ tiền thì mới cho nhận buff VIP
		if Player.CheckMoney(player, 2) < 5000 then
			GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu không có đủ 5000 KNB để nhận buff hỗ trợ VIP.")
			return
		end
		-- nếu có buff VIP rồi thì không cho nhận buff VIP
		if player:HasBuff(2065) then
			GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã nhận buff hỗ trợ VIP, không thể nhận thêm.")
			return
		end
		-- nếu có buff thường rồi thì xóa đi
		if player:HasBuff(2064) then
			player:RemoveBuff(2064)
		end
		-- trừ tiền và cho nhận buff VIP
		Player.MinusMoney(player, 2, 5000)
		player:AddBuffWithDuration(2065, 1, 168 *60 * 60 * 1000, 1, true)
		-- thêm thông báo
		player:AddNotification("Nhận buff hỗ trợ VIP thành công, buff có hiệu lực trong 168h.")
		GUI:CloseDialog(player)
		return
	end
	
	if selectionID == 2 then
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

function NPCGiaiDau:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end
