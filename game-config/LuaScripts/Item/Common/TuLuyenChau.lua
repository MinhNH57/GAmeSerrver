-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200003' bên dưới thành ID tương ứng
local TuLuyenChau = Scripts[200009]


-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TuLuyenChau:OnPreCheckCondition(scene, item, player, otherParams)

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
function TuLuyenChau:OnUse(scene, item, player, otherParams)

	-- ************************** --
	local changeFaction = 0
	local factionID = player:GetFactionID()
	local mainFaction = player:IsHasSongTu()
	local resFaction = player:GetResFaction()
	local nameFaction = ""
	if mainFaction == factionID then
		nameFaction = player:GetNameFaction(resFaction)
		changeFaction = resFaction
	else
		nameFaction = player:GetNameFaction(mainFaction)
		changeFaction = mainFaction
	end
	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Đặt tay lên cảm thấy khí huyết cuộn dâng. Có thể mở <color=orange>trạng thái tu luyện</color> để nhận <color=green>kinh nghiệm đánh quái x2</color> và tăng <color=green>10 điểm may mắn</color><br>" .. string.format("Mỗi ngày sẽ nhận được <color=green>%.1f giờ Tu Luyện</color>. Mỗi giờ tu luyện theo cấp hiện tại sẽ tương đương <color=green>%d kinh nghiệm tu luyện</color>.<br><br>", player:GetXiuLianZhu_TimeAddedPerDay() / 10, player:GetXiuLianZhu_ExpAddedByHour(10)) .. string.format("   - Kinh nghiệm Tu Luyện còn: <color=yellow>%d điểm</color><br>", player:GetXiuLianZhu_Exp()) .. string.format("   - Thời gian tu luyện tích lũy còn: <color=yellow>%.1f giờ</color>", player:GetXiuLianZhu_TimeLeft() / 10))
	dialog:AddSelection(1, "Tiến hành Tu Luyện")
	if mainFaction ~= -1 then
		dialog:AddSelection(2, string.format("Thay đổi đến %s", nameFaction))
	end
	-- if Global_EarlyAccess[player:GetName()] then
	dialog:AddSelection(3, "Truyền công Gia tộc")
	-- end
	dialog:AddSelection(1000, "Kết thúc đối thoại")
	dialog:Show(item, player)
	-- ************************** --

end

local CO_TRAIN_ERROR = {
	[-1] = "Bằng hữu hãy ngồi xuống để chuẩn bị thực hiện truyền công!",
	[-2] = "Truyền công yêu cầu 2 thành viên cùng gia tộc ở trong cùng 1 tổ đội.",
	[-3] = "Bằng hữu đã sử dụng hết số lượt truyền công trong ngày.",
	[-4] = "Cấp độ không phù hợp để truyền công.",
	[-5] = "Truyền công chỉ có thể thực hiện tại các bản đồ an toàn.",
	[-6] = "Bằng hữu cần phải gia nhập gia tộc để thực hiện truyền công.",
	[-7] = "Không thể truyền công cho người có cấp độ bằng hoặc cao hơn.",
	[-8] = "Cấp độ không đạt yêu cầu để truyền công.",
	[-9] = "Khoảng cách hai người ở quá xa. Hãy di chuyển lại gần nhau để thực hiện truyền công.",
	[-10] = "Đối phương đã hết số lượt truyền công trong ngày.",
	[-11] = "Hãy hoàn thành hành động hiện tại trước."
}

function TuLuyenChau:PreTruyenCong(scene, item, player)
	local dialog = GUI:CreateItemDialog()
	local des = "Bằng hữu chuẩn bị tiến hành truyền công cho thành viên khác trong gia tộc. Để thực hiện truyền công cần thỏa mãn các điều kiện sau:"
	des = des .. "\n- Thành lập tổ đội bao gồm hai thành viên cùng một gia tộc."
	des = des .. "\n- Người truyền công cần cấp độ tối thiểu là 80. Người nhận công lực cần cấp độ tối thiểu là 60."
	des = des .. "\n- Hai người phải ở gần nhau trong bản đồ an toàn và ở trạng thái ngồi."
	des = des .. "\n\nQuá trình truyền công đòi hỏi sự tập trung cao độ. Bằng hữu chú ý không được thay đổi trạng thái và giữ kết nối mạng ổn định, thiết lập lại tính năng tự động tổ đội và nhận lời mời để đảm bảo trạng thái tổ đội không bị thay đổi."
	des = des .. "\n\nSau khi truyền công xong sẽ nhận được phần thưởng là điểm tu luyện mật tịch."

	local srcCount = Player:GetValueOfDailyRecore(player, 100017)
	if srcCount == -1 then
		srcCount = 0
	end

	srcCount = 2 - srcCount

	des = des .. "\n\nHôm nay còn " .. srcCount .. " lần truyền công."

	dialog:AddText(des)

	dialog:AddSelection(4, "Tiến hành truyền công.")
	dialog:AddSelection(1000, "Để sau.")
	dialog:Show(item, player)
end

function TuLuyenChau:TruyenCong(scene, item, player)
	local status = player:CanStartCoTrain(80, 60, 2)
	-- mã lỗi truyền công
	-- -1: chưa ngồi
	-- -2: chưa có tổ đội, hoặc tổ đội không có 2 người, hoặc tổ đội không cùng gia tộc
	-- -3: đã quá số lượt truyền/nhận trong ngày
	-- -4: level không hợp lệ
	-- -5: không phải bản đồ an toàn
	-- -6: chưa có gia tộc
	-- -7: không thể truyền công cho người có level cao hơn

	if status < 0 then
		if CO_TRAIN_ERROR[status] ~= nil then
			player:AddNotification(CO_TRAIN_ERROR[status])
		else
			player:AddNotification("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
		end
		return
	end

	player:StartCoTrain(180, 165, 125)

	GUI:CloseDialog(player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TuLuyenChau:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 1000 then
		GUI.CloseDialog(player)
		return
	end

	if selectionID == 3 then
		TuLuyenChau:PreTruyenCong(scene, item, player)
		return
	end

	if selectionID == 4 then 
		TuLuyenChau:TruyenCong(scene, item, player)
		return
	end

	-- ************************** --
	if selectionID == 1 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Chọn số giờ muốn Tu Luyện.")
		dialog:AddSelection(10, "Mở Tu Luyện <color=yellow>0.5 giờ</color>")
		dialog:AddSelection(11, "Mở Tu Luyện <color=yellow>1 giờ</color>")
		dialog:AddSelection(12, "Mở Tu Luyện <color=yellow>1.5 giờ</color>")
		dialog:AddSelection(13, "Mở Tu Luyện <color=yellow>2 giờ</color>")
		dialog:AddSelection(14, "Mở Tu Luyện <color=yellow>4 giờ</color>")
		dialog:AddSelection(15, "Mở Tu Luyện <color=yellow>6 giờ</color>")
		dialog:AddSelection(16, "Mở Tu Luyện <color=yellow>8 giờ</color>")
		dialog:AddSelection(1000, "Ta muốn suy nghĩ thêm")
		dialog:Show(item, player)
		return
	end
	-- ************************** --
	if selectionID >= 10 and selectionID <= 16 then
		local nTime = 0
		if selectionID == 10 then
			nTime = 5
		elseif selectionID == 11 then
			nTime = 10
		elseif selectionID == 12 then
			nTime = 15
		elseif selectionID == 13 then
			nTime = 20
		elseif selectionID == 14 then
			nTime = 40
		elseif selectionID == 15 then
			nTime = 60
		elseif selectionID == 16 then
			nTime = 80
		end
		
		-- Nếu sử dụng thành công
		if Player.UseXiuLianZhu(player, nTime) == true then
			local dialog = GUI.CreateItemDialog()
			dialog:AddText("Mở Tu Luyện thành công!")
			dialog:Show(item, player)
		end
		return
	end
	-- ************************** --

	if selectionID == 2 then
		-- xem có phải đang ở map an toàn ko?
		-- xem đã đến thời gian có thể đổi chưa
		local dialog = GUI.CreateItemDialog()
		--if player:CheckSafeArea() == false then
		--	player:AddNotification("Bạn đang ở khu vực không an toàn!")
		--	GUI.CloseDialog(player)
		--	return;
		--end
		local changeFaction = 0
		local factionID = player:GetFactionID()
		local mainFaction = player:IsHasSongTu()
		local resFaction = player:GetResFaction()
		if mainFaction == factionID then
			changeFaction = resFaction
		else
			changeFaction = mainFaction
		end
		
		if player:CheckTimeSongTu() == false then
			player:AddNotification("Chưa đủ 30 phút để đổi phái!")
			GUI.CloseDialog(player)
			return;
		end
		-- thực hiện đổi
		if player:SongTu(changeFaction, false) < 0 then
			dialog:AddText("Đổi môn phái bị lỗi!")
			dialog:Show(item, player)
			return
		end
		--ghi lại thời gian đổi
		
		
		dialog:AddText("Đổi sang môn phái thành công!!!")
		dialog:Show(item, player)
	end
	
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TuLuyenChau:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end