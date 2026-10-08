-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local TruyenThongPhu = Scripts[200005]

-- danh sách map boss 95
local NameMapItemBoss95ID = {
	[3001] = {
		ID = 114,
		Name = "Nhu Tiểu Thúy[Kim 95]",
		PosX = 1030,
		PosY = 6520
	},
	[3002] = {
		ID = 115,
		Name = "Trương Thiện Đức[Mộc 95]",
		PosX = 8761,
		PosY = 959
	},
	[3003] = {
		ID = 119,
		Name = "Giả Dật Sơn[Thủy 95]",
		PosX = 9527,
		PosY = 5013
	},
	[3004] = {
		ID = 116,
		Name = "Ô Sơn Thanh[Hỏa 95]",
		PosX = 7837,
		PosY = 4142
	},
	[3005] = {
		ID = 118,
		Name = "Trần Vô Mệnh[Thổ 95]",
		PosX = 6254,
		PosY = 5588
	}
}

-- danh sách map boss 75
local NameMapItemBoss75ID = {
	[3011] = {
		ID = 105,
		Name = "Thần Thương Phương Vãn[Kim 75]",
		PosX = 11411,
		PosY = 3172
	},
	[3012] = {
		ID = 100,
		Name = "Triệu Ứng Tiên[Mộc 75]",
		PosX = 2375,
		PosY = 2893
	},
	[3013] = {
		ID = 104,
		Name = "Hương Ngọc Tiên[Thủy 75]",
		PosX = 2051,
		PosY = 1938
	},
	[3014] = {
		ID = 106,
		Name = "Man Tăng Bất Giới Hòa Thượng[Hỏa 75]",
		PosX = 8866,
		PosY = 8559
	},
	[3015] = {
		ID = 102,
		Name = "Nam Quách Nho[Thổ 75]",
		PosX = 2620,
		PosY = 2094
	}
}

-- danh sách map boss 55
local NameMapItemBoss55ID = {
	[3021] = {
		ID = 67,
		Name = "Thác Bạt Sơn Uyên[Kim 55]",
		PosX = 5905,
		PosY = 5393
	},
	[3022] = {
		ID = 75,
		Name = "Vân Tuyết Sơn[Mộc 55]",
		PosX = 5873,
		PosY = 2058
	},
	[3023] = {
		ID = 69,
		Name = "Dương Liễu[Thủy 55]",
		PosX = 3337,
		PosY = 6869
	},
	[3024] = {
		ID = 72,
		Name = "Vạn Lão Điên[Hỏa 55]",
		PosX = 8030,
		PosY = 2030
	},
	[3025] = {
		ID = 73,
		Name = "Cao Sĩ Hiền[Thổ 55]",
		PosX = 4675,
		PosY = 6038
	},
	[3026] = {
		ID = 70,
		Name = "Hình Bổ Đầu[Thổ 55]",
		PosX = 1133,
		PosY = 1344
	}
}

--danh sách map báo danh hoạt động  Truyền tống phù
local NameMapItemHoatDongID = {
	[3031] = {
		ID = 23,
		Name = "Bạch Hổ Đường",
		PosX = 6163,
		PosY = 2502
	},
	[3032] = {
		ID = 23,
		Name = "Tống Kim",
		PosX = 6204,
		PosY = 4061
	},
	[3033] = {
		ID = 341,
		Name = "Tiêu Dao Cốc",
		PosX = 2281,
		PosY = 1137
	},
	[3034] = {
		ID = 131,
		Name = "Tần Lăng",
		PosX = 12237,
		PosY = 6041
	}
}

local NameMapItemLuyenCongID = {
	[3041] = {
		ID = 87,
		Name = "Bản đồ luyện công cấp 50",
		PosX = 1727,
		PosY = 4664
	},
	[3042] = {
		ID = 96,
		Name = "Bản đồ luyện công cấp 60",
		PosX = 5383,
		PosY = 1014
	},
	[3043] = {
		ID = 103,
		Name = "Bản đồ luyện công cấp 70",
		PosX = 3997,
		PosY = 706
	},
	[3044] = {
		ID = 109,
		Name = "Bản đồ luyện công cấp 80",
		PosX = 5958,
		PosY = 6080
	},
	[3045] = {
		ID = 117,
		Name = "Bản đồ luyện công cấp 90",
		PosX = 7015,
		PosY = 874
	}
}



-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TruyenThongPhu:OnPreCheckCondition(scene, item, player, otherParams)

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
function TruyenThongPhu:OnUse(scene, item, player, otherParams)

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Ngươi muốn đi đâu?")
	dialog:AddSelection(101, "Thành thị")
	dialog:AddSelection(102, "Tân thủ thôn")
	dialog:AddSelection(103, "Môn phái")
	--dialog:AddSelection(104, "Tần Lăng")
	dialog:AddSelection(105, "Võ Lâm Cao Thủ(55)")
	dialog:AddSelection(106, "Võ Lâm Cao Thủ(75)")
	dialog:AddSelection(107, "Võ Lâm Cao Thủ(95)")
	dialog:AddSelection(108, "Tham gia hoạt động")
	dialog:AddSelection(109, "Bản đồ Luyện Công")
	dialog:AddSelection(100, "Ta chưa cần...")
	dialog:Show(item, player)
	-- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TruyenThongPhu:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 101 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu?")
		for key , value in pairs (Global_NameMapItemThanhID) do
			if value.ID ~= scene:GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
		return
	end
	-- ************************** --
	if selectionID == 102 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu?")
		for key , value in pairs (Global_NameMapItemThonID) do
			if value.ID ~= scene:GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
		return
	end
	-- ************************** --
	if selectionID == 103 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu?")
		for key , value in pairs (Global_NameMapItemPhaiID) do
			if value.ID ~= scene:GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
	end
	-- ************************** --
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
	end
	
	-- ************************** --
	if selectionID == 104 then
		---- Kiểm tra điều kiện
		--local ret = EventManager.EmperorTomb_EnterMap_CheckCondition(player)
		---- Nếu không thỏa mãn
		--if ret ~= "OK" then
		--	player:AddNotification(ret)
		--	return
		--end
		
		-- đến gặp NPC Lương Tiếu Tiếu
		player:ChangeScene(131, 12237, 6041)
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 105 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu")
		for key, value in pairs (NameMapItemBoss55ID) do
			if value.ID ~= scene.GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
	end
	-- ************************** --
	if selectionID == 106 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu")
		for key, value in pairs (NameMapItemBoss75ID) do
			if value.ID ~= scene.GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
	end
	
	-- ************************** --
	if selectionID == 107 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu")
		for key, value in pairs (NameMapItemBoss95ID) do
			if value.ID ~= scene.GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
	end
	-- ************************** --
	if selectionID == 108 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu")
		for key, value in pairs (NameMapItemHoatDongID) do
			if value.ID ~= scene.GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
	end
	-- ************************** --
	if selectionID == 109 then
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Ngươi muốn đi đâu")
		for key, value in pairs (NameMapItemLuyenCongID) do
			if value.ID ~= scene.GetID() then
				dialog:AddSelection(key, value.Name)
			end
		end
		dialog:AddSelection(100, "Ta chưa cần...")
		dialog:Show(item, player)
	end
	-- ************************** --
	if Global_NameMapItemThanhID[selectionID] ~= nil then
		player:ChangeScene(Global_NameMapItemThanhID[selectionID].ID, Global_NameMapItemThanhID[selectionID].PosX, Global_NameMapItemThanhID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif Global_NameMapItemThonID[selectionID] ~= nil then
		player:ChangeScene(Global_NameMapItemThonID[selectionID].ID, Global_NameMapItemThonID[selectionID].PosX, Global_NameMapItemThonID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif Global_NameMapItemPhaiID[selectionID] ~= nil then
		player:ChangeScene(Global_NameMapItemPhaiID[selectionID].ID, Global_NameMapItemPhaiID[selectionID].PosX, Global_NameMapItemPhaiID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif NameMapItemBoss55ID[selectionID] ~= nil then
		player:ChangeScene(NameMapItemBoss55ID[selectionID].ID, NameMapItemBoss55ID[selectionID].PosX, NameMapItemBoss55ID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif NameMapItemBoss75ID[selectionID] ~= nil then
		player:ChangeScene(NameMapItemBoss75ID[selectionID].ID, NameMapItemBoss75ID[selectionID].PosX, NameMapItemBoss75ID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif NameMapItemBoss95ID[selectionID] ~= nil then
		player:ChangeScene(NameMapItemBoss95ID[selectionID].ID, NameMapItemBoss95ID[selectionID].PosX, NameMapItemBoss95ID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif NameMapItemHoatDongID[selectionID] ~= nil then
		player:ChangeScene(NameMapItemHoatDongID[selectionID].ID, NameMapItemHoatDongID[selectionID].PosX, NameMapItemHoatDongID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	elseif NameMapItemLuyenCongID[selectionID] ~= nil then
		player:ChangeScene(NameMapItemLuyenCongID[selectionID].ID, NameMapItemLuyenCongID[selectionID].PosX, NameMapItemLuyenCongID[selectionID].PosY)
		GUI.CloseDialog(player)
		return
	end

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TruyenThongPhu:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end