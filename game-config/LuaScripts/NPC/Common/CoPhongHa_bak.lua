

-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng
local CoPhongHa = Scripts[000151]

local EVENT = {
    [83] = {
        Name = "[Sự kiện 8/3]",
        StartDate = "2023-03-07 05:00:00",
        EndDate = "2023-03-29 00:00:00",
        GM = false,
    },
    [304] = {
        Name = '[Vụ mùa bội thu]',
        StartDate = '2023-04-25 05:00:00',
        EndDate = '2023-05-23 00:00:00',
        GM = true,
    },
    [106] = {
        Name = '[Giải Nhiệt Mùa Hè]',
        StartDate = '2023-06-12 05:00:00',
        EndDate = '2023-07-03 05:00:00',
        GM = true,
    },
    [2307] = {
        Name = "[Truy tìm Kho báu]",
        StartDate = '2023-07-10 00:00:00',
        EndDate = '2023-08-05 05:00:00',
        GM = true,
    },
    [2308] = {
        Name = '[Đậu đỏ kết duyên]',
        StartDate = '2023-08-14 05:00:00',
        EndDate = '2023-09-12 00:00:00',
        GM = true,
    },
    [2109] = {
        Name = '[Phá Cỗ Trung Thu]',
        StartDate = '2023-09-21 05:00:00',
        EndDate = '2023-10-11 05:00:00'
    }
}

local COMPOSE_FORMULA = {
	[10001] = {
        EventID = 83,
		ItemID = 15210,
		Name = "Bó Hoa hồng (khóa)",
        Extra = "(dùng Bạc khóa)",
		Require = {
			{ ItemID = 15190, Name = "Giấy gói hoa", Number = 1},
			{ ItemID = 15191, Name = "Dây ruy-băng", Number = 1},
			{ ItemID = 15192, Name = "Hoa hồng đỏ (khóa)", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
		},
        Expiry = Global_ItemExpiry83,
        Binding = 1
	},
    [10002] = {
        EventID = 83,
		ItemID = 15210,
		Name = "Bó Hoa hồng (khóa)",
        Extra = "(dùng Bạc)",
		Require = {
			{ ItemID = 15190, Name = "Giấy gói hoa", Number = 1},
			{ ItemID = 15191, Name = "Dây ruy-băng", Number = 1},
			{ ItemID = 15192, Name = "Hoa hồng đỏ (khóa)", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
		},
        Expiry = Global_ItemExpiry83,
        Binding = 1
	},
    [10003] = {
        EventID = 83,
		ItemID = 15212,
		Name = "Bó Hoa hồng đỏ",
        Extra = "(dùng Bạc khóa)",
		Require = {
			{ ItemID = 15190, Name = "Giấy gói hoa", Number = 1},
			{ ItemID = 15191, Name = "Dây ruy-băng", Number = 1},
			{ ItemID = 15193, Name = "Hoa hồng đỏ", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
		},
        Expiry = Global_ItemExpiry83,
        Binding = 0,
	},
    [10004] = {
        EventID = 83,
		ItemID = 15212,
		Name = "Bó Hoa hồng đỏ",
        Extra = "(dùng Bạc)",
		Require = {
			{ ItemID = 15190, Name = "Giấy gói hoa", Number = 1},
			{ ItemID = 15191, Name = "Dây ruy-băng", Number = 1},
			{ ItemID = 15193, Name = "Hoa hồng đỏ", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
		},
        Expiry = Global_ItemExpiry83,
        Binding = 0
	},
    [10005] = {
        EventID = 304,
        ItemID = 15199,
        Name = 'Bao thóc (khóa)',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 15194, Name = "Nước", Number = 1},
            { ItemID = 15195, Name = "Đất màu mỡ", Number = 1},
            { ItemID = 15196, Name = "Hạt giống (khóa)", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
        },
        Expiry = Global_ItemExpiry304,
        Binding = 1
    },
    [10006] = {
        EventID = 304,
        ItemID = 15199,
        Name = 'Bao thóc (khóa)',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 15194, Name = "Nước", Number = 1},
            { ItemID = 15195, Name = "Đất màu mỡ", Number = 1},
            { ItemID = 15196, Name = "Hạt giống (khóa)", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
        },
        Expiry = Global_ItemExpiry304,
        Binding = 1
    },
    [10007] = {
        EventID = 304,
        ItemID = 15218,
        Name = 'Bao gạo',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 15194, Name = "Nước", Number = 1},
            { ItemID = 15195, Name = "Đất màu mỡ", Number = 1},
            { ItemID = 15197, Name = "Hạt giống", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
        },
        Expiry = Global_ItemExpiry304,
        Binding = 0
    },
    [10008] = {
        EventID = 304,
        ItemID = 15218,
        Name = 'Bao gạo',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 15194, Name = "Nước", Number = 1},
            { ItemID = 15195, Name = "Đất màu mỡ", Number = 1},
            { ItemID = 15197, Name = "Hạt giống", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
        },
        Expiry = Global_ItemExpiry304,
        Binding = 0
    },
    [10009] = {
        EventID = 106,
        ItemID = 15701,
        Name = 'Kem Vani (Khóa)',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 15696, Name = "Que làm Kem", Number = 1},
            { ItemID = 15697, Name = "Sữa tươi", Number = 1},
            { ItemID = 15698, Name = "Hương liệu", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
        },
        Expiry = Global_ItemExpiry0106,
        Binding = 1
    },
    [10010] = {
        EventID = 106,
        ItemID = 15701,
        Name = 'Kem Vani',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 15696, Name = "Que làm Kem", Number = 1},
            { ItemID = 15697, Name = "Sữa tươi", Number = 1},
            { ItemID = 15698, Name = "Hương liệu", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
        },
        Expiry = Global_ItemExpiry0106,
        Binding = 0
    },
    [10011] = {
        EventID = 106,
        ItemID = 15702,
        Name = 'Kem Trái Cây',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 15701, Name = "Kem Vani", Number = 1},
            { ItemID = 15699, Name = "Trái Cây", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
        },
        Expiry = Global_ItemExpiry0106,
        Binding = 1
    },
    [10012] = {
        EventID = 106,
        ItemID = 15702,
        Name = 'Kem Trái Cây',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 15701, Name = "Kem Vani", Number = 1},
            { ItemID = 15699, Name = "Trái Cây", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
        },
        Expiry = Global_ItemExpiry0106,
        Binding = 0
    },
    [10013] = {
        EventID = 2307,
        ItemID = 20043,
        Name = 'Kho báu',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 20039, Name='Bản đồ', Number = 1 },
            { ItemID = 20040, Name='Xẻng', Number = 1 },
            { ItemID = 20041, Name='Dây thừng', Number = 1 },
            { ItemID = 1, Name='Bạc', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2307,
        Binding = 0
    },
    [10014] = {
        EventID = 2307,
        ItemID = 20043,
        Name = 'Kho báu',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 20039, Name='Bản đồ', Number = 1 },
            { ItemID = 20040, Name='Xẻng', Number = 1 },
            { ItemID = 20041, Name='Dây thừng', Number = 1 },
            { ItemID = 0, Name='Bạc khóa', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2307,
        Binding = 1
    },
    [10015] = {
        EventID = 2307,
        ItemID = 20044,
        Name = 'Kho báu Thần bí',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 20043, Name='Kho báu', Number = 1 },
            { ItemID = 20042, Name='Manh mối kho báu', Number = 1 },
            { ItemID = 1, Name='Bạc', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2307,
        Binding = 0
    },
    [10016] = {
        EventID = 2307,
        ItemID = 20044,
        Name = 'Kho báu Thần bí',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 20043, Name='Kho báu', Number = 1 },
            { ItemID = 20042, Name='Manh mối kho báu', Number = 1 },
            { ItemID = 0, Name='Bạc khóa', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2307,
        Binding = 1
    },

    [10017] = {
        EventID = 2308,
        ItemID = 20078,
        Name = 'Chè Đậu Đỏ',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 20074, Name='Đường', Number = 1 },
            { ItemID = 20075, Name='Nước', Number = 1 },
            { ItemID = 20076, Name='Đậu Đỏ', Number = 1 },
            { ItemID = 1, Name='Bạc', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2308,
        Binding = 0
    },
    [10018] = {
        EventID = 2308,
        ItemID = 20078,
        Name = 'Chè Đậu Đỏ',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 20074, Name='Đường', Number = 1 },
            { ItemID = 20075, Name='Nước', Number = 1 },
            { ItemID = 20076, Name='Đậu Đỏ', Number = 1 },
            { ItemID = 0, Name='Bạc khóa', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2308,
        Binding = 1
    },
    [10019] = {
        EventID = 2308,
        ItemID = 20079,
        Name = 'Chè Đậu Đỏ Đặc Biệt',
        Extra = '(dùng Bạc)',
        Require = {
            { ItemID = 20078, Name='Chè Đậu Đỏ', Number = 1 },
            { ItemID = 20077, Name='Hương vị Đặc biệt', Number = 1 },
            { ItemID = 1, Name='Bạc', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2308,
        Binding = 0
    },
    [10020] = {
        EventID = 2308,
        ItemID = 20079,
        Name = 'Chè Đậu Đỏ Đặc Biệt',
        Extra = '(dùng Bạc khóa)',
        Require = {
            { ItemID = 20078, Name='Chè Đậu Đỏ', Number = 1 },
            { ItemID = 20077, Name='Hương vị Đặc biệt', Number = 1 },
            { ItemID = 0, Name='Bạc khóa', Number = 10000 },
        },
        Expiry = Global_ItemExpiry2308,
        Binding = 1
    },
    [10021] = {
        EventID = 2109,
		ItemID = 20114,
		Name = "Bánh Nướng",
        Extra = "(dùng Bạc khóa)",
		Require = {
			{ ItemID = 20110, Name = "Đường", Number = 1},
			{ ItemID = 20111, Name = "Bột Mì", Number = 1},
			{ ItemID = 20112, Name = "Khuôn Bánh", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
		},
        Expiry = Global_ItemExpiry2109,
        Binding = 1
	},
    [10022] = {
        EventID = 2109,
		ItemID = 20114,
		Name = "Bánh Nướng",
        Extra = "(dùng Bạc)",
		Require = {
			{ ItemID = 20110, Name = "Đường", Number = 1},
			{ ItemID = 20111, Name = "Bột Mì", Number = 1},
			{ ItemID = 20112, Name = "Khuôn Bánh", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
		},
        Expiry = Global_ItemExpiry2109,
        Binding = 0
	},
    [10023] = {
        EventID = 2109,
		ItemID = 20115,
		Name = "Bánh Nướng Trứng Muối",
        Extra = "(dùng Bạc khóa)",
		Require = {
			{ ItemID = 20114, Name = "Bánh Nướng", Number = 1},
			{ ItemID = 20113, Name = "Trứng Muối", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
		},
        Expiry = Global_ItemExpiry2109,
        Binding = 1
	},
    [10024] = {
        EventID = 2109,
		ItemID = 20115,
		Name = "Bánh Nướng Trứng Muối",
        Extra = "(dùng Bạc)",
		Require = {
			{ ItemID = 20114, Name = "Bánh Nướng", Number = 1},
			{ ItemID = 20113, Name = "Trứng Muối", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
		},
        Expiry = Global_ItemExpiry2109,
        Binding = 0
	},
}
local LotteryAward = {
    [2] = 10000,
    [4] = 100000,
    [6] = 1000000
}
local selectedItemID = -1

function CoPhongHa:NhanThuongTienDongNgocNu(scene, npc, player)
    local ITEMID = 20099
    local RECORD = 5000011

    if not Player.HasFreeBagSpaces(player, 1) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần " .. 1 .. " ô trống trong túi đồ để nhận vật phẩm.")
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    if Player:GetValueOfDailyRecore(player, RECORD) ~= -1 then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu đã nhận gói vật phẩm này rồi!")
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    else
        Player:SetValueOfDailyRecore(player, RECORD, 1)
    end

    Player.AddItemLuaAndNotify(player, ITEMID, 1, 0, 1)
    GUI:CloseDialog(player)
end

function CoPhongHa:PhatThuongTienDongNgocNu(scene, npc, player)
    if System:GetHour() < 9 then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Chưa đến giờ mở thưởng, vui lòng trở lại sau 9:00.")
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return    
    end
    
    local dialog = GUI:CreateNPCDialog()
    dialog:AddText(System:GetTDNNLuaMessage())

    if System:IsTDNNWinner(player:GetID()) then
        dialog:AddSelection(7, "Nhận thưởng")
    end

    dialog:AddSelection(9, "Ta sẽ trở lại sau.")

    dialog:Show(npc, player)
end

function CoPhongHa:BaoDanhTienDongNgocNu(scene, npc, player)
    local RECORD_ID = 5000010 -- daily record id của tiên đồng ngọc nữ
    local dialog = GUI:CreateNPCDialog()
    local partner = player:GetTeammateInPair()

    Player:SetValueOfDailyRecore(player, RECORD_ID, 1)
    Player:SetValueOfDailyRecore(partner, RECORD_ID, 1)

    System:WriteDBLog(player, 'TIENDONGNGOCNU', '', '' .. player:GetID(), '' .. partner:GetID())
    dialog:AddText("Báo danh thành công. Bằng hữu hãy quay lại vào 9:00 sáng hôm sau để kiểm tra kết quả.")
    dialog:Show(npc, player)
end

function CoPhongHa:TienDongNgocNu(scene, npc, player)
    local RECORD_ID = 5000010 -- daily record id của tiên đồng ngọc nữ
    local CHE_DAU_DO_ID = 200116
    local CHE_HAT_SEN_ID = 200117

    local dialog = GUI:CreateNPCDialog()

    local msg = 'Thể lệ hoạt động Tiên đồng Ngọc Nữ:\n\n';
    msg = msg .. 'Trong thời gian diễn ra sự kiện, mỗi ngày bằng hữu có thể rủ bạn bè đến gặp Sứ giả hoạt động để tiến hành báo danh. Kết thúc ngày, 5 cặp đôi may mắn nhất sẽ được lựa chọn để tiến hành trao thưởng.\n\n'
    msg = msg .. 'Thời gian mở trao thưởng là 9:00 sáng ngày tiếp theo. Phần thưởng bao gồm huyền tinh, bảo thạch tăng sức mạnh, danh hiệu sự kiện...\n\n'
    msg = msg .. 'Các thức và điều kiện báo danh:\n' 
    msg = msg .. '    - Thành lập tổ đội gồm 2 người, 1 nam và 1 nữ. Chỉ đội trưởng mới được thực hiện báo danh.\n'
    msg = msg .. '    - 2 thành viên phải tham gia sự kiện Đậu đỏ Kết duyên: trong ngày phải sử dụng ít nhất 1 vật phẩm Chè đậu đỏ Hạt sen hoặc Chè đậu đỏ Đặc biệt. Nếu đã hết số lượt mở các vật phẩm này thì coi như đủ điều kiện.\n'
    msg = msg .. '    - 2 thành viên đều chưa tham gia báo danh trong ngày.'
    msg = '\n---------------\n' .. msg

    -- dialog:AddText(msg)

    if not player:HasTeam() then
        -- không có tổ đội, không cho báo danh
        dialog:AddText('<color=#ed8796>Bằng hữu không có tổ đội!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end
    
    if player:GetTeamSize() ~= 2 then
        -- tổ đội có số lượng thành viên không hợp lệ 
        dialog:AddText('<color=#ed8796>Tổ đội cần có 1 nam và 1 nữ!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    local partner = player:GetTeammateInPair()
    if player:GetSex() + partner:GetSex() ~= 1 then
        -- giới tính không hợp lệ
        dialog:AddText('<color=#ed8796>Tổ đội cần có 1 nam và 1 nữ!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    if not player:IsTeamLeader() then
        -- đội trưởng mới được báo danh
        dialog:AddText('<color=#ed8796>Chỉ có đội trưởng mới được báo danh!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    local distance = player:GetTeamDistanceInPair()
    if distance == -1 or distance > 1000 then
        dialog:AddText('<color=#ed8796>Khoảng cách giữa 2 người quá xa. Hãy di chuyển về gần nhau hơn!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    if Player:GetValueOfDailyRecore(player, RECORD_ID) == 1 or Player:GetValueOfDailyRecore(partner, RECORD_ID) == 1 then
        dialog:AddText('<color=#ed8796>Trong tổ đội có thành viên đã tham gia báo danh hôm nay rồi!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    local playerInEvent = Player:GetValueOfDailyRecore(player, CHE_DAU_DO_ID) >= 0 
        or Player:GetValueOfDailyRecore(player, CHE_HAT_SEN_ID) >= 0
        or Player:GetValueForeverRecore(player, CHE_DAU_DO_ID) >= 4998
        or Player:GetValueForeverRecore(player, CHE_HAT_SEN_ID) >= 298

    local partnerInEvent = Player:GetValueOfDailyRecore(partner, CHE_DAU_DO_ID) >= 0 
        or Player:GetValueOfDailyRecore(partner, CHE_HAT_SEN_ID) >= 0
        or Player:GetValueForeverRecore(partner, CHE_DAU_DO_ID) >= 4998
        or Player:GetValueForeverRecore(partner, CHE_HAT_SEN_ID) >= 298

    if not (playerInEvent and partnerInEvent) then
        dialog:AddText('<color=#ed8796>Cả 2 thành viên cần phải tham gia hoạt động Đậu đỏ Kết duyên trong ngày!</color>')
        dialog:AddText(msg)
        dialog:AddSelection(9, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    dialog:AddText(msg)
    dialog:AddSelection(5, "Báo danh Hoạt động Tiên đồng Ngọc Nữ")

    dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function CoPhongHa:OnOpen(scene, npc, player, otherParams)

	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Xin chào " .. player:GetName() .. "! Ta là sứ giả hoạt động, có rất nhiều sự kiện ngày lễ đặc biệt được tổ chức ở chỗ ta, các hạ muốn tìm hiểu loại nào?")
    dialog:AddSelection(1, "Ghép vật phẩm sự kiện") 
    -- local current_day = System:GetDate()
	-- if current_day <= 11 then   
    --     dialog:AddSelection(4, '[Đậu đỏ Kết duyên] Báo danh Tiên đồng ngọc nữ')
    -- end
    -- dialog:AddSelection(6, '[Đậu đỏ Kết duyên] Kết quả Tiên đồng ngọc nữ')
    --dialog:AddSelection(3, "<color=#f38ba8>[Giải Nhiệt Mùa Hè]</color> Thông tin xổ số trúng thưởng")
	--[[ dialog:AddSelection(1, "<color=#fbe66f>[Hệ thống]</color> <color=#06f455>Nhận đồng trên web</color>.")
	dialog:AddSelection(2, "<color=#51e1fb>Nhận thưởng hoạt động </color>.") ]]
    -- dialog:AddSelection(8, "Tiệm Hoa Đăng")
	dialog:AddSelection(9, "<color=#51e1fb>Kết thúc đối thoại.</color>")
    if (player:IsGM()) then
        dialog:AddSelection(1000, "Mở 5000 Túi nguyên liệu")
        dialog:AddSelection(1010, "Mở 200 Túi nguyên liệu")
        dialog:AddSelection(1001, "Mở 5000 Bánh nướng")
        dialog:AddSelection(1011, "Mở 200 Bánh nướng")
        dialog:AddSelection(1002, "Mở 5000 Bánh nướng trứng muối")
        dialog:AddSelection(1012, "Mở 200 Bánh nướng trứng muối")
        dialog:AddSelection(1003, "Mở 5000 Bánh dẻo")
        dialog:AddSelection(1013, "Mở 200 Bánh dẻo")
        dialog:AddSelection(1004, "Mở 5000 Hộp bánh")
        dialog:AddSelection(1014, "Mở 200 Hộp bánh")
		dialog:AddSelection(1015, "Mở 100 Rương mảnh đồng hành")
        -- dialog:AddSelection(1005, "Mở 5000 Dây cương Ngưu Lang-Chức Nữ")
        -- dialog:AddSelection(1006, "Mở 5000 Dây cương Ngưu Lang-Chức Nữ Đặc Biệt")
        -- dialog:AddSelection(1007, "Mở 5000 Dây cương Ngưu Lang-Chức Nữ (Vĩnh viễn)")
        -- dialog:AddSelection(1008, "Mở 5000 rương Tiên đồng Ngọc Nữ")
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
function CoPhongHa:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 1000 then
        Player:OpenBatchRandomBox(player, 20109, 5000)
        return
    end

    if selectionID == 1001 then
        Player:OpenBatchRandomBox(player, 20114, 5000)
        return
    end

    if selectionID == 1002 then
        Player:OpenBatchRandomBox(player, 20115, 5000)
        return
    end

    if selectionID == 1003 then
        Player:OpenBatchRandomBox(player, 20116, 5000)
        return
    end

    if selectionID == 1004 then
        Player:OpenBatchRandomBox(player, 20117, 5000)
        return
    end

    if selectionID == 1010 then
        Player:OpenBatchRandomBox(player, 20109, 200)
        return
    end

    if selectionID == 1011 then
        Player:OpenBatchRandomBox(player, 20114, 200)
        return
    end

    if selectionID == 1012 then
        Player:OpenBatchRandomBox(player, 20115, 200)
        return
    end

    if selectionID == 1013 then
        Player:OpenBatchRandomBox(player, 20116, 200)
        return
    end

    if selectionID == 1014 then
        Player:OpenBatchRandomBox(player, 20117, 200)
        return
    end
	
	if selectionID == 1015 then
        Player:OpenBatchRandomBox(player, 20051, 100)
        return
    end

    if selectionID == 8 then
        Player:OpenShop(npc, player, 1455)
        return
    end

    -- if selectionID == 1005 then
    --     Player:OpenBatchRandomBox(player, 20082, 5000)
    --     return
    -- end

    -- if selectionID == 1006 then
    --     Player:OpenBatchRandomBox(player, 20083, 5000)
    --     return
    -- end

    -- if selectionID == 1007 then
    --     Player:OpenBatchRandomBox(player, 20084, 5000)
    --     return
    -- end

    -- if selectionID == 1008 then
    --     Player:OpenBatchRandomBox(player, 20099, 5000)
    --     return
    -- end

    -- if selectionID == 4 then
    --     CoPhongHa:TienDongNgocNu(scene, npc, player)
    --     return
    -- end

    -- if selectionID == 5 then 
    --     CoPhongHa:BaoDanhTienDongNgocNu(scene, npc, player)
    --     return
    -- end

    -- if selectionID == 6 then 
    --     CoPhongHa:PhatThuongTienDongNgocNu(scene, npc, player)
    --     return
    -- end

    -- if selectionID == 7 then
    --     CoPhongHa:NhanThuongTienDongNgocNu(scene, npc, player)
    -- end

    -- if selectionID == 3 then
    --     local dialog = GUI.CreateNPCDialog()
    --     dialog:AddText("Ăn kem trúng lớn. Hãy mở các vật phẩm sự kiện để nhận được mã số dự thưởng. Mỗi ngày sẽ quay số và chọn 6 số ngẫu nhiên. Kiểm tra các vé của bạn để có cơ hội nhận những phần quà hấp dẫn.")
    --     if Player:HadLotteryYesterday() then
    --         dialog:AddSelection(2000, "Nhận thưởng mã số may mắn")
    --     end
    --     dialog:AddSelection(2001, "Mã số của bạn ngày hôm nay")
    --     if (player:IsGM()) then
    --         dialog:AddSelection(2006, "Reset thưởng xổ số")
    --     end
    --     dialog:AddSelection(9, "Ta sẽ quay lại sau.")
    --     dialog:Show(npc, player)
    --     return
    -- end

	local dialog = GUI.CreateNPCDialog()

	if selectionID == 1 then
		local counter = 0
		local dialog = GUI.CreateNPCDialog()
		for k, v in pairs(COMPOSE_FORMULA) do
			if EVENT[v.EventID] ~= nil then
                local event = EVENT[v.EventID]
                if System:IsDateTimeInRange(event.StartDate, event.EndDate) then
                    dialog:AddSelection(k, "<color=#f38ba8>" .. event.Name .. "</color>" .. " " .. v.Name .. " " .. "<color=#bac2de>" .. v.Extra .. "</color>")
				    counter = counter + 1
                end
			end
		end
		if counter == 0 then
			dialog:AddText("Hiện chưa có sự kiện đang diễn ra.")
			dialog:AddSelection(9, "Ta sẽ quay lại sau.")
		else
			dialog:AddText("Bằng hữu muốn ghép vật phẩm nào?")
		end
		dialog:Show(npc, player)
	end

	if COMPOSE_FORMULA[selectionID] ~= nil then
        CoPhongHa:ShowComposeNumberOption(npc, player, selectionID)
        return
	end

    if selectionID == 700000 then
        CoPhongHa:ShowComposeNumberOption(npc, player, selectedItemID)
        return
    end

    if selectionID > 700000 and selectionID < 800000 then
        CoPhongHa:ComposeItem(npc, player, selectedItemID, selectionID - 700000)
    end
    
	if selectionID == 2000 then
        local dialog = GUI.CreateNPCDialog()
        local numberWin = Player:NumberWinLottery()
        dialog:AddText("Con số may mắn hôm qua là " .. numberWin.."" )
        local CheckAwardTicket = player:CheckAwardTicket()

        if CheckAwardTicket > 0 then
            local TicketWin = player:GetTicketWin()
            local numberPlayerWin = Player:GetNumberPlayerWin(CheckAwardTicket)
            local moneyAward = math.floor(LotteryAward[CheckAwardTicket] / numberPlayerWin)
            dialog:AddText("Chúc mừng bạn đã may mắn trúng thưởng. Con số trúng thưởng của bạn là " ..TicketWin.."")
            dialog:AddText("Hôm nay có " ..numberPlayerWin.. " người trúng thưởng. Phần thưởng của bạn là " ..moneyAward.." KNB khóa")
            local GetValue = Player.GetValueOfDailyRecore(player, 80005)
            if GetValue == -1 then
                dialog:AddSelection(2004,"Nhận phần thưởng giải trúng "..CheckAwardTicket.. " số")
            else
                dialog.AddText("Bạn đã nhận phần thưởng này rồi!")
            end
        end
        dialog:AddSelection(2005, "Các mã số bạn đã sở hữu hôm qua.")
        dialog:Show(npc,player)
	end
    if selectionID == 2001 then
        local dialog = GUI.CreateNPCDialog()
        dialog:AddText("00h00 hôm nay sẽ tiến hành quay số trúng thưởng. Hãy đi kiếm thật nhiều mã số để tăng tỷ lệ trúng thưởng!")
        local current_date = System:GetDateOfYear()
        local textTickedUsed = player:GetTickedUsed(current_date)
        if textTickedUsed == "" then
            dialog:AddText("Bạn chưa có mã số dự thưởng nào!")
        else
            dialog:AddText("Hệ thống chỉ hiển thị tối đa 10 số của bạn. Những mã số hôm nay bạn có: ")
            dialog:AddText(textTickedUsed)
        end
        dialog:Show(npc,player)
	end

    
    if selectionID == 2004 then
        local CheckAwardTicket = player:CheckAwardTicket()
        local numberPlayerWin = Player:GetNumberPlayerWin(CheckAwardTicket)
        local moneyAward = math.floor(LotteryAward[CheckAwardTicket] / numberPlayerWin)
        local isOK = Player.AddMoney(player, moneyAward, 3, "Lottery")
        if isOK then
            Player.SetValueOfDailyRecore(player, 80005, 1)
            GUI.ShowNotification(player, "Bạn đã nhận thưởng giải trúng "..CheckAwardTicket.." số thành công")
        end
        GUI.CloseDialog(player)
    end

    if selectionID == 2005 then
        local dialog = GUI.CreateNPCDialog()
        local current_date = System:GetDateOfYear()
        local textTickedUsed = player:GetTickedUsed(current_date - 1)

        if textTickedUsed == "" then
            dialog:AddText("Bạn chưa có mã số dự thưởng nào")
        else
            dialog:AddText("Hệ thống chỉ hiển thị tối đa 10 số của bạn. Các mã số bạn đã sở hữu hôm qua: ")
            dialog:AddText(textTickedUsed)
        end
        dialog:Show(npc,player)
    end
	if selectionID == 2006 then
        Player.SetValueOfDailyRecore(player, 80005, -1)
		GUI.CloseDialog(player)
	end
	if selectionID == 9 then
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
function CoPhongHa:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end


function CoPhongHa:ComposeItem(npc, player, itemID, number)
    local dialog = GUI:CreateNPCDialog()

    local formula = COMPOSE_FORMULA[itemID]
    System.WriteToConsole("Đang ghép " .. number .. " " .. formula.Name)

    for k, v in pairs(formula.Require) do
        if v.ItemID >= 100 then
            -- là item
            local needed = v.Number * number
            local in_bag = Player:CountItemInBag(player, v.ItemID)
            if in_bag < needed then
                dialog:AddText("Bằng hữu không có đủ nguyên liệu để ghép " .. number .. " " .. formula.Name .. "!")
                dialog:Show(npc, player)
                return
            end 
        else
            -- là tiền
            local needed = v.Number * number
            local in_bag = Player:CheckMoney(player, v.ItemID)
            if in_bag < needed then
                dialog:AddText("Bằng hữu cần " .. needed .. " " .. v.Name .. " để ghép " .. number .. " " .. formula.Name .. "!")
                dialog:AddSelection(9, "Ta sẽ quay lại sau.")
                dialog:Show(npc, player)
                return
            end
        end
    end

    for k, v in pairs(formula.Require) do
        local needed = v.Number * number
        if v.ItemID >= 100 then
            Player:RemoveItem(player, v.ItemID, needed)
        else 
            Player:SubMoney(player, needed, v.ItemID, "COMPOSE_" .. formula.ItemID)
        end
    end

    Player:AddItemLuaAndNotify(player, formula.ItemID, number, 0, formula.Binding, 0, formula.Expiry)
    dialog:AddText("Bằng hữu đã đổi " .. number .. " " .. formula.Name .. ". Hãy kiểm tra trong túi đồ nhé!")
    dialog:AddSelection(700000, "Ta muốn đổi tiếp.")
    dialog:AddSelection(9, "Cảm ơn")
    dialog:Show(npc, player)
end

function CoPhongHa:ShowComposeNumberOption(npc, player, itemID)
    if not Player.HasFreeBagSpaces(player, 1) then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText(
            "Cần để trống tối thiểu <color=green>1 ô trống</color> trong túi đồ để ghép vật phẩm!")
        dialog:Show(npc, player)
        return
    end

    selectedItemID = itemID

    local maximum = 999999
    local formula = COMPOSE_FORMULA[itemID]
    for k, v in pairs(formula.Require) do
        if v.ItemID >= 100 then
            local in_bag = Player.CountItemInBag(player, v.ItemID)
            local can_exchange = math.floor(in_bag / v.Number)
            maximum = math.min(maximum, can_exchange)
        end
    end
    System.WriteToConsole("max " .. maximum)

    local dialog = GUI:CreateNPCDialog()
    local description = "Mỗi " .. formula.Name .. " cần:"
        local add_comma = false
        for k, v in pairs(formula.Require) do
            -- if add_comma then
            --     description = "\n    - " .. description .. ", "
            -- end
            -- add_comma = true
            description = description .. "\n    - " .. v.Number .. " " .. v.Name
        end

    if maximum == 0 then
        dialog:AddText(description .. "\n\nBằng hữu không có đủ nguyên liệu để ghép " .. formula.Name .. ".")
        dialog:AddSelection(9, "Ta sẽ quay lại sau.")
        dialog:Show(npc, player)
        return
    end
    dialog:AddText(description .. "\n\nBằng hữu muốn ghép bao nhiêu " .. formula.Name .. "?")

    local options = {1, 5, 10, 20, 50, 100, 500}
    for i = 1, 7 do
        if options[i] <= maximum then
            local quantity = options[i]
            dialog:AddSelection(700000 + quantity, "Ghép " .. quantity .. " " .. formula.Name)
        else
            break
        end
    end

    dialog:AddSelection(9, "Để ta suy nghĩ thêm.")
    dialog:Show(npc, player)
end