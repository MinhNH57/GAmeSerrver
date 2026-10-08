-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng
local CoPhongHa = Scripts[000151]

local CHECKIN_CONFIG = {{
	Record = 9980004,
    Package = {
        Name = "Check-In Khai Giảng [Sơ]",
        BagSpace = 4,
        Items = {
			{ ItemID = 20391, Number = 1, Binding = 1 }, -- túi huyền tinh ngẫu nhiên
			{ ItemID = 20378, Number = 5, Binding = 1 }, -- rương danh vọng (sơ)
			{ ItemID = 20051, Number = 5, Binding = 1 }, -- rương mảnh trang bị đồng hành sơ cấp
			{ ItemID = 506, Number = 50, Binding = 1 },	-- 50 ngũ hành hồn thạch

		}
    },
    MoneyType = 1,
    Money = 300000,
    MoneyName = 'Bạc',
    Limit = 1
}, {
	Record = 9980005,
    Package = {
        Name = "Check-In Khai Giảng [Trung]",
        BagSpace = 4,
        Items = {
			{ ItemID = 20628, Number = 1, Binding = 1 }, -- túi huyền tinh ngẫu nhiên (trung)
			{ ItemID = 20378, Number = 10, Binding = 1 }, -- rương danh vọng (sơ)
			{ ItemID = 20379, Number = 10, Binding = 1 }, -- rương mảnh trang bị đồng hành trung cấp
			{ ItemID = 506, Number = 100, Binding = 1 }, -- 100 ngũ hành hồn thạch
            { ItemID = 25270, Number = 50, Binding = 1 } -- 50 kinh nghiệm chân nguyên
		}
    },
    MoneyType = 2,
    Money = 300,
    MoneyName = 'KNB',
    Limit = 5
}, {
	Record = 9980006,
    Package = {
        Name = "Check-In Khai Giảng [Cao]",
        BagSpace = 4,
        Items = {
			{ ItemID = 20629, Number = 1, Binding = 1 }, -- túi huyền tinh ngẫu nhiên (cao)
			{ ItemID = 20529, Number = 20, Binding = 1 }, -- rương mảnh trang bị đồng hành (cao)
			{ ItemID = 1026, Number = 10, Binding = 1 }, -- chiến thư du long
			{ ItemID = 25269, Number = 1, Binding = 1 }, -- 1 thiệp như ý
            { ItemID = 25270, Number = 200, Binding = 1 }
		},
	},
	MoneyType = 2,
	Money = 2500,
	MoneyName = 'KNB',
	Limit = 5
}}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function CoPhongHa:OnOpen(scene, npc, player, otherParams)
	local dialog = GUI.CreateNPCDialog()
	-- dialog:AddText("Xin chào " .. player:GetName() .. "! Chuỗi sự kiện Mầm Non Đất Nước:")
    -- dialog:AddText("\t- Con Ngoan Trò Giỏi")
    -- dialog:AddText("\t- Tích Lũy Chuyển Xu")
    -- dialog:AddText("\t- Ưu Đãi Nạp Đơn")
    -- dialog:AddText("\t- Check-In Khai Giảng")
    -- dialog:AddText("\t- Nâng Cấp Mặt Nạ")
    -- dialog:AddText("\t- Giảm Giá Phi Phong")
    -- dialog:AddText("\t- Vòng Quay Học Trò")
    -- dialog:AddText("\t- Đạo tặc Sinh nhật")
    -- dialog:AddText("Chúc các đại hiệp chơi game vui vẻ.")
    
    --dialog:AddSelection(1, "Ghép vật phẩm sự kiện")
    --dialog:AddSelection(2, "Bảng xếp hạng sự kiện")
    --dialog:AddSelection(3, "Nâng cấp mặt nạ")
    -- dialog:AddSelection(1002, "Check-in Khai Giảng")
    -- dialog:AddSelection(5, "Vòng quay Tình Nhân")
   
    -- if EventManager:IsUnionShopAvailable() or player:IsGM() then
        --     dialog:AddSelection(5, "Thống kê hoàn trả Ưu đãi Đoàn Kết")
        -- end
        
    -- dialog:AddSelection(4, "Tiệm đổi thưởng Sinh Nhật")
    dialog:AddSelection(6, "Tiệm Võ Lâm Minh Chủ")
	dialog:AddSelection(9, "<color=#51e1fb>Kết thúc đối thoại.</color>")

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
    if selectionID == 4 then
        -- tiệm đổi thưởng sinh nhật
        Player.OpenShop(npc, player, 1490)
        -- tiệm ưu đãi đoàn kết
        -- if System:GetServerID() < 24 then
        --     Player.OpenShop(npc, player, 1486)
        -- else
        --     Player.OpenShop(npc, player, 1487)
        -- end 

        -- tiệm đá quý
        -- Player.OpenShop(npc, player, 1485)
        return
    end

    if selectionID == 5 then
        -- thống kê ưu đãi đoàn kết
        local uiName = "CustomUI/UuDaiDoanKet"
        if System:GetServerID() == 24 then
            uiName = "CustomUI/UuDaiDoanKet_23"
        end
        GUI:OpenUIWithStringDict(player, uiName, EventManager:GetEventShopSellInfo(player))

        -- Vòng quay Tình Nhân
        -- CoPhongHa:ShowXu(scene, npc, player)
        return
    end

    if selectionID == 101 then
        GUI:OpenSeashell(player)
        return
    end

    if selectionID == 102 then
        CoPhongHa:ReceiveXu(scene, npc, player)
        return
    end

    if selectionID == 103 then
        Player:OpenShop(npc, player, 1482)
        return
    end

    if selectionID == 104 then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Bảng xếp hạng Vòng Quay Tình Nhân (Liên server)")
        dialog:AddText("")
        dialog:AddText(EventManager:TopSeashells())
        dialog:Show(npc, player)
        return
    end

	if selectionID == 6 then
        -- mở tiệm võ lâm minh chủ
		Player.OpenShop(npc, player, 1461)
		GUI.CloseDialog(player)
        return
	end

	local dialog = GUI.CreateNPCDialog()

	if selectionID == 1 then
        -- ghép vật phẩm sự kiện
        GUI:CloseDialog(player)
        if System:GetServerID() == 24 then
            GUI:OpenComposeDialog(player, "", 100003, 100002)
        else
           GUI:OpenComposeDialog(player, "", 100000, 100001, 100002)
        end
		return
	end

    if selectionID == 1002 then
        CoPhongHa:CheckInDaiLe(scene, npc, player)
        return
    end

    if selectionID == 1003 then
        CoPhongHa:DoCheckIn(scene, npc, player, 1)
        return
    end

    if selectionID == 1004 then
        CoPhongHa:DoCheckIn(scene, npc, player, 2)
        return
    end

    if selectionID == 1005 then
        CoPhongHa:DoCheckIn(scene, npc, player, 3)
        return
    end

    if selectionID == 1009 then
        Player:SetValueOfDailyRecore(player, CHECKIN_CONFIG[1].Record, -1)
        Player:SetValueOfDailyRecore(player, CHECKIN_CONFIG[2].Record, -1)
        Player:SetValueOfDailyRecore(player, CHECKIN_CONFIG[3].Record, -1)
        return
    end

    if selectionID == 2 then
        EventManager:ShopTopEventRanking(player)
        return
    end

    if selectionID == 3 then
        if System:GetServerID() >= 21 then
			GUI:OpenComposeDialog(player, "", 405, 406, 407, 408, 409)			
		else 
			GUI:OpenComposeDialog(player, "", 400, 401, 402, 403, 404)			
		end
		GUI:CloseDialog(player)
		return
    end

	if selectionID == 9 then
		GUI.CloseDialog(player)
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
function CoPhongHa:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end

function CoPhongHa:DoCheckIn(scene, npc, player, checkin_level)
    local config = CHECKIN_CONFIG[checkin_level]

    local checkin_today = Player:GetValueOfDailyRecore(player, config.Record)
    if checkin_today < 0 then
        checkin_today = 0
    end

    if checkin_today >= config.Limit then
        GUI:ShowMessageBox(player, "Thông báo",
            "Bằng hữu đã check-in đủ số lượt cho phép trong ngày. Vui lòng quay trở lại vào ngày mai.")
        return
    end

    if not Player:HasFreeBagSpaces(player, config.Package.BagSpace) then
        GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu cần ' .. config.Package.BagSpace .. ' ô trống trong túi đồ để nhận vật phẩm.')
        return
    end

    GUI:ShowMessageBox(player, "Thông báo", "Xác nhận dùng " .. config.Money .. " " .. config.MoneyName ..
        " để thực hiện Check-in Khai Giảng?", function()
        if Player.CheckMoney(player, config.MoneyType) < config.Money then
            GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu không có đủ ' .. config.MoneyName .. '.')
            return
        end

        if Player:SubMoney(player, config.Money, config.MoneyType, "CHECKIN_DAILE") then
            Player:SetValueOfDailyRecore(player, config.Record, checkin_today + 1)
            if not Global_ReceiveItemPackByConfig(npc, player, config.Package) then
                GUI:ShowMessageBox(player, "Thông báo",
                    "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.")
            end
        else
            GUI:ShowMessageBox(player, "Thông báo",
                "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.")
        end
    end)
end

function CoPhongHa:CheckInDaiLe(scene, npc, player)
    local dialog = GUI.CreateNPCDialog()
    local checkin_low = Player:GetValueOfDailyRecore(player, CHECKIN_CONFIG[1].Record)
    if checkin_low < 0 then
        checkin_low = 0
    end
    local checkin_mid = Player:GetValueOfDailyRecore(player, CHECKIN_CONFIG[2].Record)
    if checkin_mid < 0 then
        checkin_mid = 0
    end
    local checkin_high = Player:GetValueOfDailyRecore(player, CHECKIN_CONFIG[3].Record)
    if checkin_high < 0 then
        checkin_high = 0
    end

    dialog:AddText("\t- Check-in [Sơ]:" .. checkin_low .. "/" .. CHECKIN_CONFIG[1].Limit .. " lượt.")
    dialog:AddText("\t- Check-in [Trung]:" .. checkin_mid .. "/" .. CHECKIN_CONFIG[2].Limit .. " lượt.")
    dialog:AddText("\t- Check-in [Cao]:" .. checkin_high .. "/" .. CHECKIN_CONFIG[3].Limit .. " lượt.")

    dialog:AddSelection(1003, "Check-in [sơ] - " .. CHECKIN_CONFIG[1].Money .. " " .. CHECKIN_CONFIG[1].MoneyName)
    dialog:AddSelection(1004, "Check-in [trung] - " .. CHECKIN_CONFIG[2].Money .. " " .. CHECKIN_CONFIG[2].MoneyName)
    dialog:AddSelection(1005, "Check-in [cao] - " .. CHECKIN_CONFIG[3].Money .. " " .. CHECKIN_CONFIG[3].MoneyName)
    if player:IsGM() then
        dialog:AddSelection(1009, "Reset Check-in")
    end
    dialog:Show(npc, player)
end

function CoPhongHa:ShowXu(scene, npc, player)
    local totalXu = Player:GetTotalCharge(player, '2024-08-05 05:00:00', '2024-08-20 05:00:00')
    local xuReceived = Player:GetValueOfMonthRecord(player, 5000121)
    if xuReceived < 0 then
        xuReceived = 0
    end

    local xuToReceive = totalXu / 50 - xuReceived

    local dialog = GUI:CreateNPCDialog()
    dialog:AddText('Bằng hữu có thể tham gia hoạt động Vòng quay Tình Nhân tại đây.')
    dialog:AddText('')
    dialog:AddText('Vòng quay Tình Nhân cần tiêu hao Vé Tình nhân để tham gia. Vé Tình nhân có thể mua tại Kỳ Trân Các.')
    dialog:AddText('Tham gia quay vòng quay có thể nhận được phiếu đổi thưởng. Sử dụng phiếu đổi thưởng trong Tiệm Đổi thưởng Tình nhân để đổi lấy các vật phẩm giá trị.')
    dialog:AddText('Trong thời gian từ 5:00 05/8 đến 5:00 12/8, với mỗi 50KNB được chuyển vào trò chơi, bằng hữu sẽ nhận được 1 Vé Tình Nhân miễn phí.')
    dialog:AddText('')
    dialog:AddText('Bằng hữu đã nạp <color=#a6e3a1>' .. totalXu .. ' KNB</color>, đã nhận <color=#a6e3a1>' .. xuReceived .. ' Vé Tình Nhân</color>. Có thể nhận thêm <color=#a6e3a1>' .. xuToReceive .. ' vé</color>.')
    dialog:AddSelection(101, 'Tham gia Vòng quay Tình Nhân')
    if System:IsDateTimeInRange('2024-08-05 05:00:00', '2024-08-13 05:00:00') then
        dialog:AddSelection(102, 'Nhận Vé Tình Nhân')
    end
    dialog:AddSelection(103, 'Tiệm đổi thưởng')
    dialog:AddSelection(104, 'Bảng xếp hạng Vòng quay')
    dialog:Show(npc, player)
end

function CoPhongHa:ReceiveXu(scene, npc, player)
    if not Player:HasFreeBagSpaces(player, 1) then
        GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm.')
        return
    end

    local totalXu = Player:GetTotalCharge(player, '2024-08-05 05:00:00', '2024-08-13 05:00:00')
    local xuReceived = Player:GetValueOfMonthRecord(player, 5000121)
    if xuReceived < 0 then
        xuReceived = 0
    end

    local xuToReceive = totalXu / 50 - xuReceived
    if xuToReceive <= 0 then 
        GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu đã nhận đủ số lượng vé. Hãy nạp thêm ADNXu để nhận thêm vé miễn phí.')
        return
    end
    Player:SetValueOfMonthRecord(player, 5000121, totalXu / 50)
    Player.AddItemLuaAndNotify(player, 20661, xuToReceive, 0, 1, 0,
                    '2024-08-20 05:00:00')

    QuanBBR:OnOpen(scene, npc, player, nil)

end