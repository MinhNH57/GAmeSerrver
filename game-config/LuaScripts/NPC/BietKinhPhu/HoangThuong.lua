-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000041' bên dưới thành ID tương ứng
local HoangThuong = Scripts[000041]

--****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng	Player.OpenShop(npc, player, 133)
--		player: Player - NPC tương ứng
-- ****************************************************** --
function HoangThuong:OnOpen(scene, npc, player, otherParams)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Chỉ cần độ thân mật đật đến 100, đẳng cấp phải đạt đến 50 thì đến chỗ ta lập lập bang hội.")

	local current_day = System:GetDate()
	if System.IsDateTimeInRange('2024-05-24 05:00:00', '2024-06-05 05:00:00') then
	dialog:AddSelection(11, "<color=#f38ba8>[Bang Hội Vô Song]</color> Nộp Vô song lệnh")
		-- dialog:AddSelection(13, "<color=#f38ba8>[Bang Hội Vô Song]</color> Báo danh Tổ đội")
	end

	if player:GetFamilyID() <= 0 then
		dialog:AddSelection(1,"Lập gia tộc")
	end
		
	if player:GetFamilyID() > 0 and player:GetFamilyRank() == Global_FamilyRank.Master then
		dialog:AddSelection(2,"Giải tán gia tộc")
	end
	if player:GetGuildID() <= 0 and player:GetFamilyID() > 0 and player:GetFamilyRank() == Global_FamilyRank.Master then
		dialog:AddSelection(3,"Lập bang hội")
	end
	
	if player:GetGuildID() > 0 then
		--dialog:AddSelection(10,"Cửa hàng bang hội")
	end
	
	if player:GetGuildID() > 0 and player:GetGuildRank() == Global_GuildRank.Master then
		dialog:AddSelection(4,"Giải tán bang hội")
	end
		dialog:AddSelection(5,"Danh sách gia tộc chiêu mộ")
		
	if player:GetGuildID() > 0 then
		dialog:AddSelection(6,"Đầu tư thêm tài sản cá nhân")
	end
	
	if player:GetGuildID() > 0 then
		dialog:AddSelection(7, "Shop bang hội")
	end
	
	if player:GetGuildID() > 0 then
		dialog:AddSelection(8, "Đổi Xu bang hội")
	end
	
	dialog:AddSelection(100, "Kết thúc đối thoại")
	dialog:Show(npc, player)
	-- ************************** --
end

function HoangThuong:BaoDanhSongLong(scene, npc, player)
	GUI:ShowInputString(player, "Nhập tên <color=#a6e3a1>chiến đội</color> tham dự <color=#a6e3a1>SONG LONG XUẤT HẢI</color>", function(teamName)
		local RECORD_ID = 5000018 -- daily record id của song long xuất hải

		local dialog = GUI:CreateNPCDialog()
		local partner = player:GetTeammateInPair()

		Player:SetValueOfWeekRecore(player, RECORD_ID, partner:GetID())
		Player:SetValueOfWeekRecore(partner, RECORD_ID, player:GetID())

		System:WriteDBLog(player, 'SONGLONGXUATHAI', '', '' .. player:GetID(), '' .. partner:GetID())
		GUI:ShowMessageBox(player, "Thông báo", "Báo danh Song Long Xuất Hải thành công!")
		GUI:ShowMessageBox(partner, "Thông báo", "Báo danh Song Long Xuất Hải thành công!")
		System:WriteLog(106, player:GetID() .. ' và ' .. partner:GetID() .. ' đã báo danh Song Long Xuất Hải! Chiến đội ' .. teamName)
		GUI:CloseDialog(player)
	end)
end

function HoangThuong:ShowBaoDanhSongLong(scene, npc, player)
	local RECORD_ID = 5000018 -- daily record id của song long xuất hải

    local dialog = GUI:CreateNPCDialog()

    local msg = 'Bằng hữu có thể tiến hành báo danh tổ đội để tham gia sự kiện <color=#a6e3a1>SONG LONG XUẤT HẢI</color> tại đây.\n\nThời gian báo danh: <color=#f9e2af>00:00 21/03/2024 đến 05:00 25/03/2024</color>.\n'
    msg = msg .. 'Thể lệ báo danh:\n' 
    msg = msg .. '    - Thành lập tổ đội gồm 2 người. Sau đó đội trưởng tiến hành báo danh.\n'
    msg = msg .. '    - 2 thành viên đều chưa tham gia báo danh ở tổ đội khác.\n'
    msg = msg .. '    - Sau khi báo danh sẽ không thể đăng ký lại, vui lòng cân nhắc kỹ lưỡng.'
    msg = '\n---------------\n' .. msg

    -- dialog:AddText(msg)

	local current_day = System:GetDate()
	if current_day < 21 then
		-- chưa đến ngày báo danh
        dialog:AddText('<color=#ed8796>Chưa đến thời gian báo danh.</color>')
        dialog:AddText(msg)
        dialog:AddSelection(100, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
	end

    if not player:HasTeam() then
        -- không có tổ đội, không cho báo danh
        dialog:AddText('<color=#ed8796>Bằng hữu không có tổ đội!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(100, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end
    
    if player:GetTeamSize() ~= 2 then
        -- tổ đội có số lượng thành viên không hợp lệ 
        dialog:AddText('<color=#ed8796>Tổ đội cần có 2 người!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(100, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    if not player:IsTeamLeader() then
        -- đội trưởng mới được báo danh
        dialog:AddText('<color=#ed8796>Chỉ có đội trưởng mới được báo danh!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(100, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    local distance = player:GetTeamDistanceInPair()
    if distance == -1 or distance > 1000 then
        dialog:AddText('<color=#ed8796>Khoảng cách giữa 2 người quá xa. Hãy di chuyển về gần nhau hơn!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(100, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

	local partner = player:GetTeammateInPair()
    if Player:GetValueWeeklyRecore(player, RECORD_ID) ~= -1 or Player:GetValueWeeklyRecore(partner, RECORD_ID) ~= -1 then
        dialog:AddText('<color=#ed8796>Trong tổ đội có thành viên đã tham gia báo danh rồi!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    dialog:AddText(msg)
    dialog:AddSelection(14, "Báo danh Hoạt động <color=#a6e3a1>SONG LONG XUẤT HẢI</color>")

    dialog:Show(npc, player)
end

function HoangThuong:NopVoSongLenh(scene, npc, player)
	local item_count = player:IsHasItem(20072)
	if item_count > 0 then
		if Player:RemoveItem(player, 20072, item_count) then
			GUI:ShowMessageBox(player, "BangHộiVôSong", "Nộp thành công " .. item_count .. " Vô Song Lệnh.")
			GUI:CloseDialog(player)
			System:WriteDBLog(player, 'BANGHOIVOSONG', '', '' .. player:GetID(), '' .. player:GetGuildID(), '', '' .. item_count)
			System:WriteLog(106, player:GetID() .. ' đã nộp ' .. item_count .. ' Vô Song Lệnh.')
		end
	end
end

function HoangThuong:MoTaVoSongLenh(scene, npc, player) 
	local dialog = GUI:CreateNPCDialog()
	dialog:AddText('Bằng hữu có thể nộp <color=#fbe66f>Vô Song Lệnh</color> để cạnh tranh tư cách tham dự sự kiện <color=#06f455>[Bang Hội Vô Song]</color> tại đây.\n\nThời gian nộp đến hết 0:00 ngày 05/06/2024.\n')
	local item_count = player:IsHasItem(20072)
	if player:GetGuildID() <= 0 then
		dialog:AddText('<color=#ed8796>Bằng hữu chưa gia nhập bang hội!</color>')
	end
	if item_count <= 0 then
		dialog:AddText('<color=#ed8796>Bằng hữu không sở hữu Vô song lệnh!</color>')
	end

	if item_count > 0 then
		dialog:AddSelection(12, "Nộp <color=#ed8796>" .. item_count .. "</color> Vô song lệnh")
	end

	dialog:AddSelection(100, 'Ta sẽ quay lại sau.')
	dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function HoangThuong:OnSelection(scene, npc, player, selectionID, otherParams)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 1 then
		if player:GetFamilyID() > 0 then
			self:ShowMessageBox(npc, player, "Bạn đã có gia tộc, không thể tạo thêm!")
			return
		end
		
		GUI.OpenUI(player, "UICreateFamily")
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 2 then
		if player:GetFamilyID() <= 0 then
			self:ShowMessageBox(npc, player, "Bạn không có gia tộc, không thể giải tán!")
			return
		elseif player:GetFamilyRank() ~= Global_FamilyRank.Master then
			self:ShowMessageBox(npc, player, "Chỉ có tộc trưởng mới có thể giải tán gia tộc!")
			return
		elseif player:GetGuildID() > 0 then
			self:ShowMessageBox(npc, player, "Bạn phải thoát khỏi bang hội trước, sau đó mới có thể giải tán gia tộc!")
			return
		elseif  player:GetFactionID() == 0 then
			self:ShowMessageBox(npc, player, "Bạn phải gia nhập môn phái mới lập được gia tộc.")
		end
		
		Player.RetainFamily(player)
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 3 then
		if player:GetGuildID() > 0 then
			self:ShowMessageBox(npc, player, "Bạn đã có bang hội, không thể tạo thêm!")
			return
		elseif player:GetFamilyID() <= 0 then
			self:ShowMessageBox(npc, player, "Bạn không có gia tộc, không thể tạo bang hội!")
			return
		elseif player:GetFamilyRank() ~= Global_FamilyRank.Master then
			self:ShowMessageBox(npc, player, "Chỉ có tộc trưởng mới có thể tạo bang!")
			return
		elseif  player:GetFactionID() == 0 then
			self:ShowMessageBox(npc, player, "Bạn phải gia nhập môn phái mới lập được bang hội.")
		end
		GUI.OpenUI(player, "UICreateGuild")
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID == 10 then
		if player:GetGuildID() <= 0 then
			self:ShowMessageBox(npc, player, "Bạn không có bang hội, không thể giải tán!")
			return
		
		end
		
		Player.OpenShop(npc, player, 226)
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 4 then
		if player:GetGuildID() <= 0 then
			self:ShowMessageBox(npc, player, "Bạn không có bang hội, không thể giải tán!")
			return
		elseif player:GetGuildRank() ~= Global_GuildRank.Master then
			self:ShowMessageBox(npc, player, "Chỉ có bang chủ mới có thể giải tán bang hội!")
			return
		end
		
		Player.RetainGuild(player)
		GUI.CloseDialog(player)
		return
	end
	if selectionID==5 then
		GUI.OpenUI(player, "UIFamilyList")
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID==6 then
		dialog:AddText("Đầu tư thêm tài sản cá nhân sẽ giúp bạn tăng cổ phần nắm giữ ở đại hội cổ đông mỗi tuần. cập nhật mỗi thứ 2. Bạn muốn đầu tư thêm bao nhiêu cổ phần?")
		dialog:AddSelection(61,"Đầu tư thêm 10 vạn Bạc")
		dialog:AddSelection(62,"Đầu tư thêm 50 vạn Bạc")
		dialog:AddSelection(63,"Đầu tư thêm 100 vạn Bạc")
		dialog:AddSelection(64,"Đầu tư thêm 500 vạn Bạc")
		dialog:Show(npc, player)
		return
	end
	
	if selectionID==61 or selectionID==62 or selectionID==63 or selectionID==64 then
		local moneyInvest = 100000;
		if selectionID == 61 then
			moneyInvest= 100000;
		elseif selectionID ==62 then	
			moneyInvest= 500000;
		elseif selectionID ==63 then	
			moneyInvest= 1000000;
		elseif selectionID ==64 then	
			moneyInvest= 5000000;
		end
		
		local ret = Player.InvestInGuild(player, moneyInvest);
		
		if ret == 1 then
			self:ShowMessageBox(npc, player, "Đầu tư thành công")
		elseif ret ==-1 then
			self:ShowMessageBox(npc, player, "Bạn phải gia nhập bang hội trước đã")
		elseif ret ==-2 then
			self:ShowMessageBox(npc, player, "Bạc trên người không đủ")
		elseif ret ==-3 or ret ==-4 then
			self:ShowMessageBox(npc, player, "Có lỗi xảy ra.")
		end
		
		return
	end

	if selectionID == 11 then
		HoangThuong:MoTaVoSongLenh(scene, npc, player)
		return
	end
	
	if selectionID == 12 then
		HoangThuong:NopVoSongLenh(scene, npc, player)
		return
	end

	if selectionID == 13 then
		HoangThuong:ShowBaoDanhSongLong(scene, npc, player)
		return
	end

	if selectionID == 14 then
		HoangThuong:BaoDanhSongLong(scene, npc, player)
		return
	end
	
	if selectionID == 7 then
		-- kiểm tra xem có mang thẻ bang hội trogng người ko
		if player:IsHasItem(25233) <= 0 then
			HoangThuong:ShowMessageBox(npc, player, "Bạn phải có thẻ bang hội trong túi đồ, tham gia hoạt động Hỏa Kỳ Lân để có cơ hội nhận thẻ.")
			return
		else
			Player.OpenShop(npc, player, 1477)
			GUI.CloseDialog(player)
		end
	end
	
	if selectionID == 8 then
		if player:IsHasItem(25233) <= 0 then
			HoangThuong:ShowMessageBox(npc, player, "Bạn phải có thẻ bang hội trong túi đồ, tham gia hoạt động Hỏa Kỳ Lân để có cơ hội nhận thẻ.")
			return
		else
			-- hiện inputNumber để đổi lấy tiền 
			GUI:ShowInputNumber(player, "Nhập số xu bang hội (1 xu = 1000 bạc)", function(value)
				-- kiểm tra xem đủ bạc trong người ko
				local moneyNeed = value * 1000
				if Player.CheckMoney(player, 1) < moneyNeed then
					HoangThuong:ShowMessageBox(npc, player, "Bạn không có đủ bạc để đổi")
					return
				else
					-- kiểm tra đủ chỗ trống ko
					local slots = value / 1000
					slots = slots + 1
					
					if Player.HasFreeBagSpaces(player, slots) == false then
						HoangThuong:ShowMessageBox(npc, player, "Túi đồ đã đầy, hãy sắp xếp lại túi đồ")
						return
					else
						-- thêm item xu vào
						Player.AddItemLua(player, 25234, value, -1, 1)
						-- trừ tiền
						Player.MinusMoney(player, 1, moneyNeed)
						HoangThuong:ShowMessageBox(npc, player, "Bạn đã đổi thành công")
						return
					end
				end
			end)
		end
	end

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function HoangThuong:OnItemSelected(scene, npc, player, itemID,otherParams)

	-- ************************** --

	-- ************************** --

end

-- ****************************************************** --
-- ****************************************************** --
function HoangThuong:ShowMessageBox(npc, player, msg)

	-- ************************** --
	local dialog = GUI.CreateNPCDialog()
	dialog:AddText(msg)
	dialog:Show(npc, player)
	-- ************************** --

end
