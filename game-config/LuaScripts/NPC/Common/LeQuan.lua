-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000148' bên dưới thành ID tương ứng
local LeQuan = Scripts[000149]

-- forward declaration
local FUNCTION_TABLE
local LEQUAN_PACKAGES

local IDS_CHARGED_OVER_2000 = {1999930198, 1999916852, 1999930103, 1999911745, 1999930237, 1999912844, 1999914471,
                               1999914327, 1999930185, 1999914724, 1999930225, 1999935986, 1999914479, 1999910495,
                               1999917776, 1999911550, 1999930142, 1999914398, 1999909901, 1999932179, 1999912833,
                               1999917782, 1999930189, 1999930241}

function LeQuan.ExchangeGatherMakePoint(scene, npc, player, selectionID, otherParams)
    GUI:OpenComposeDialog(player, "", 1016, 1017, 1018, 1019)
end

function LeQuan.ExchangeMoney(scene, npc, player, selectionID, otherParams)
    local dialog = GUI.CreateNPCDialog()
    local dayOfWeek = System:GetDayOfWeek()
    local hour = System:GetHour()
    local minute = System:GetMinute()
    if dayOfWeek == 1 and hour == 0 and minute < 15 then
        dialog:AddText("Chưa đến giờ sử dụng ưu đãi, vui lòng quay lại sau!")
        dialog:Show(npc, player)
        return
    end
    Player.ChangeMoney(npc, player)
end

function LeQuan.ExchangeToken(scene, npc, player, selectionID, otherParams)
    local dialog = GUI.CreateNPCDialog()
    local dayOfWeek = System:GetDayOfWeek()
    local hour = System:GetHour()
    local minute = System:GetMinute()
    if dayOfWeek == 1 and hour == 0 and minute < 15 then
        dialog:AddText("Chưa đến giờ sử dụng ưu đãi, vui lòng quay lại sau!")
        dialog:Show(npc, player)
        return
    end
    Player.ChangeBoundToken(npc, player)
end

function LeQuan.BuyGatherMakePoints(scene, npc, player, selectionID, otherParams)
    local dialog = GUI.CreateNPCDialog()
    local dayOfWeek = System:GetDayOfWeek()
    local hour = System:GetHour()
    local minute = System:GetMinute()
    if dayOfWeek == 1 and hour == 0 and minute < 15 then
        dialog:AddText("Chưa đến giờ sử dụng ưu đãi, vui lòng quay lại sau!")
        dialog:Show(npc, player)
        return
    end
    Player.BuyGatherMakerPoint(npc, player)
end

-- function LeQuan.ReceiveReturnAward(scene, npc, player, selectionID, otherParams)
--     if player:GetLevel() < 10 then
--         dialog:AddText("Bằng hữu cần đạt cấp độ 10 để nhận gói vật phẩm.")
--         dialog:Show(npc, player)
--         return
--     end
--     local package_return_award = LEQUAN_PACKAGES.RETURN_AWARD
--     if selectionID == 18 then
--         package_return_award = LEQUAN_PACKAGES.RETURN_AWARD_VIP
--     end

--     local received = Global_ReceiveItemPackByConfig(npc, player, package_return_award)
--     if received then
--         local name = package_return_award.Name
--         dialog:AddText("Chúc mừng bằng hữu đã nhận <color=#a6e3a1>" .. name .. "</color> thành công.")
--         dialog:Show(npc, player)
--     end
-- end

function LeQuan.IncreaseLevel(scene, npc, player, selectionID, otherParams)
    if not ListContainsValue(IDS_CHARGED_OVER_2000, player:GetID()) then
        GUI:ShowMessageBox(player, "Thông báo",
            "Bằng hữu không đủ điều kiện để nhận hỗ trợ này.")
        return
    end

    if player:GetMantleLevel() < 5 then
        GUI:ShowMessageBox(player, "Thông báo",
            "Bằng hữu cần trang bị Phi Phong cấp Ngự Không trở lên để nhận hỗ trợ.")
        return
    end

    local player_level = 120

    if player_level > player:GetLevel() then
        player:SetLevel(player_level)
    else
        GUI:ShowMessageBox(player, "Thông báo", "Cấp độ nhân vật không phù hợp để nhận hỗ trợ!")
        return
    end
end

function LeQuan.MergeServerSupport(scene, npc, player, selectionID, otherParams)
    local dialog = GUI:CreateNPCDialog()
    dialog:AddText("Bằng hữu có thể nhận các hỗ trợ sau khi gộp máy chủ tại đây!")

    dialog:AddSelection(7, FUNCTION_TABLE[7].Text) -- nhận cấp độ 119
    dialog:AddSelection(8, FUNCTION_TABLE[8].Text) -- thường
    dialog:AddSelection(9, FUNCTION_TABLE[9].Text) -- bạch kim
    dialog:AddSelection(10, FUNCTION_TABLE[10].Text) -- kim cương
    dialog:AddSelection(11, FUNCTION_TABLE[11].Text) -- ruby
    dialog:Show(npc, player)
end

function LeQuan.ReceiveMergeServerSupport(scene, npc, player, selectionID, otherParams)
    local package = nil
    local createdZoneID = player:GetCreatedZoneID()

    if not player:IsGM() then
        if (createdZoneID ~= 25 and createdZoneID ~= 26 and createdZoneID ~= 29) or
            not System:IsDateTimeInRange('2025-07-29 05:00:00', '2025-08-03 23:59:59') then
            return
        end
    end

    if selectionID == 8 then
        if not ListContainsValue(IDS_CHARGED_OVER_2000, player:GetID()) then
            GUI:ShowMessageBox(player, "Thông báo",
                "Bằng hữu không đủ điều kiện để nhận hỗ trợ này.")
            return
        end

        if player:GetMantleLevel() < 5 then
            GUI:ShowMessageBox(player, "Thông báo",
                "Bằng hữu cần trang bị Phi Phong cấp Ngự Không trở lên để nhận gói vật phẩm.")
            return
        end

        package = LEQUAN_PACKAGES.MERGE_NORMAL_25_26
        if createdZoneID == 29 then
            package = LEQUAN_PACKAGES.MERGE_NORMAL_29
        end
    elseif selectionID == 9 then
        package = LEQUAN_PACKAGES.MERGE_PLATINUM_25_26
        if createdZoneID == 29 then
            package = LEQUAN_PACKAGES.MERGE_PLATINUM_29
        end
    elseif selectionID == 10 then
        package = LEQUAN_PACKAGES.MERGE_DIAMOND_25_26
        if createdZoneID == 29 then
            package = LEQUAN_PACKAGES.MERGE_DIAMOND_29
        end
    elseif selectionID == 11 then
        package = LEQUAN_PACKAGES.MERGE_RUBY_25_26
        if createdZoneID == 29 then
            package = LEQUAN_PACKAGES.MERGE_RUBY_29
        end
	elseif selectionID == 99 then
		package = LEQUAN_PACKAGES.DEN_BU_ALL_SERVER_01
	end
    if package.Money ~= nil and package.Money > 0 then
        GUI:ShowMessageBox(player, "Thông báo",
            "Xác nhận dùng " .. package.Money .. " " .. Global_TypeMoneyName[package.MoneyType] .. " để nhận " ..
                package.Name .. "?", function()
                Global_ReceiveItemPackByConfig(npc, player, package)
            end, nil)
    else
        Global_ReceiveItemPackByConfig(npc, player, package)
    end
end

function LeQuan.DenBuLienServer(scene, npc, player, selectionID, otherParams)
    -- local dialog = GUI.CreateNPCDialog()
    -- check cấp độ
    if player:GetLevel() < 80 then
        GUI:ShowMessageBox(player, "Thông báo",
            "Bằng hữu cần đạt level 80 trở lên để nhận gói vật phẩm.")
        return
    end
    -- check phi Phong
    if player:GetMantleLevel() < 5 then
        GUI:ShowMessageBox(player, "Thông báo",
            "Bằng hữu cần trang bị Phi Phong cấp Ngự Không trở lên để nhận gói vật phẩm.")
        return
    end
    -- đủ yêu cầu thì cho nhận gói hỗ trợ
    local package = LEQUAN_PACKAGES.DENBULIENSERVER

    local received = Global_ReceiveItemPackByConfig(npc, player, package)
    if received then
        local name = package.Name
        dialog:AddText("Chúc mừng bằng hữu đã nhận <color=#a6e3a1>" .. name .. "</color> thành công.")
        dialog:Show(npc, player)
    end

end
function LeQuan.CheckHistoryItem(scene, npc, player, selectionID, otherParams)
	Player.CheckHistoryItem(player)
end

function LeQuan.DenBuBaoTriAllServer(scene, npc, player, selectionID, otherParams)
	if player:GetLevel() < 89 then
        GUI:ShowMessageBox(player, "Thông báo",
            "Bằng hữu cần đạt level 89 trở lên để nhận gói vật phẩm.")
        return
    end
	
	local package = LEQUAN_PACKAGES.DEN_BU_ALL_SERVER_01

    local received = Global_ReceiveItemPackByConfig(npc, player, package)
    if received then
		Player.AddScoreMonthActivity(player, 200)
        local name = package.Name
        dialog:AddText("Chúc mừng bằng hữu đã nhận <color=#a6e3a1>" .. name .. "</color> thành công.")
        dialog:Show(npc, player)
    end
end


-- function LeQuan.ResetPotentialPoints(scene, npc, player, selectionID, otherParams)
--     local record = Player:GetValueWeeklyRecore(player, 740341)

--     if record > 0 then
--         GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã sử dụng lượt phân phối lại miễn phí rồi!")
--         return
--     end

--     player:ResetPotentialPoints()
--     GUI:ShowMessageBox(player, "Thông báo", "Điểm tiềm năng đã được phân phối lại thành công!")
--     Player:SetValueOfWeekRecore(player, 740341, 1)
-- end

FUNCTION_TABLE = {
    [66] = {
        Text = "Đổi Tinh Hoạt Lực",
        Condition = true,
        Exec = LeQuan.ExchangeGatherMakePoint
    },
    [3] = {
        Text = "<color=#f38ba8>[Uy danh]</color> Mua tinh hoạt ưu đãi",
        Condition = true,
        Exec = LeQuan.BuyGatherMakePoints
    },
    [4] = {
        Text = "<color=#f38ba8>[Uy danh]</color> Đổi bạc thường",
        Condition = true,
        Exec = LeQuan.ExchangeMoney
    },
    [5] = {
        Text = "<color=#f38ba8>[Uy danh]</color> Đổi KNB khóa",
        Condition = true,
        Exec = LeQuan.ExchangeToken
    },
    [6] = {
        Text = "<color=#f38ba8>[Hệ thống]</color>Hỗ trợ Gộp Máy Chủ",
        Exec = LeQuan.MergeServerSupport
    },
    [7] = {
        Text = "Nhận cấp độ 120",
        Exec = LeQuan.IncreaseLevel
    },
    [8] = {
        Text = "Nhận gói Liên thông [Miễn phí]",
        Exec = LeQuan.ReceiveMergeServerSupport
    },
    [9] = {
        Text = "Nhận gói Liên thông [Bạch Kim] [2.000 KNB]",
        Exec = LeQuan.ReceiveMergeServerSupport
    },
    [10] = {
        Text = "Nhận gói Liên thông [Kim Cương] [5.000 KNB]",
        Exec = LeQuan.ReceiveMergeServerSupport
    },
    [11] = {
        Text = "Nhận gói Liên thông [Ruby] [20.000 KNB]",
        Exec = LeQuan.ReceiveMergeServerSupport
    },
    -- [17] = {
    --     Text = "<color=#f38ba8>[Hệ thống]</color> Nhận phần quà trở lại",
    --     Condition = true,
    --     Exec = LeQuan.ReceiveReturnAward
    -- },
    -- [18] = {
    --     Text = "<color=#f38ba8>[Hệ thống]</color> Nhận phần quà trở lại (đặc biệt)",
    --     Condition = true,
    --     Exec = LeQuan.ReceiveReturnAward
    -- },
    -- [20] = {
    --     Text = "<color=#f38ba8>[Hệ thống]</color> Phân phối lại điểm tiềm năng.",
    --     Condition = true,
    --     Exec = LeQuan.ResetPotentialPoints
    -- },
    [21] = {
        Text = "Nhận đền bù hoạt động liên server",
        Exec = LeQuan.DenBuLienServer
    },
	[22] = {
        Text = "Xem thông tin vật phẩm đã sử dụng",
        Exec = LeQuan.CheckHistoryItem
    },
	[99] = {
		Text = "Nhận đền bù bảo trì",
        Exec = LeQuan.DenBuBaoTriAllServer
	},
    [2] = {
        Text = "Ta sẽ trở lại sau",
        Exec = function(scene, npc, player, selectionID, otherParams)
            GUI.CloseDialog(player)
        end
    },
    [100] = {
        Text = "[GM] Nhận ngựa Admin",
        Exec = function(scene, npc, player, selectionID, otherParams)
            for i = 20130, 20180 do
                Player.AddItemLuaAndNotify(player, i, 1, 0, 1)
            end
        end
    }
}

function LeQuan:OnLoad(filePath)
    local configPath = 'LuaScripts/' .. filePath:gsub(".lua", ".json")
    local configStr = System:ReadAllText(configPath)
    local config = json.parse(configStr)
    LEQUAN_PACKAGES = config.Packages
end

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function LeQuan:OnOpen(scene, npc, player, otherParams)
    -- ************************** --
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Bằng hữu, làm thế nào ta có thể giúp bạn ?.")

    -- if System:IsDateTimeInRange('2024-12-12 00:00:00', '2024-12-29 23:59:59') then
    --     dialog:AddSelection(20, FUNCTION_TABLE[20].Text)
    -- end

    local createdZoneID = player:GetCreatedZoneID()

    -- if (player:IsGM()) then
    --     dialog:AddSelection(6, FUNCTION_TABLE[6].Text)
    -- else
    --     if ((createdZoneID == 25 or createdZoneID == 26 or createdZoneID == 29) and
    --         System:IsDateTimeInRange('2025-07-29 10:00:00', '2025-08-03 23:59:59')) then
    --         dialog:AddSelection(6, FUNCTION_TABLE[6].Text)
    --     end
    -- end

    dialog:AddSelection(3, FUNCTION_TABLE[3].Text)
    dialog:AddSelection(4, FUNCTION_TABLE[4].Text)
    dialog:AddSelection(5, FUNCTION_TABLE[5].Text)
    dialog:AddSelection(66, FUNCTION_TABLE[66].Text)
    dialog:AddSelection(22, FUNCTION_TABLE[22].Text)
    dialog:AddSelection(99, FUNCTION_TABLE[99].Text)
	

    dialog:AddSelection(67, "Nhận danh hiệu chuyển đổi")
    -- if (System:GetServerID() == 27 and System:IsDateTimeInRange('2025-02-18 00:00:00', '2025-03-03 23:59:59')) or
    --         player:IsGM() then
    --     if System:CanGetReturnAwards(player) or player:IsGM() then
    --         dialog:AddSelection(17, FUNCTION_TABLE[17].Text)
    --     end

    --     if System:CanGetReturnAwardsVIP(player) or player:IsGM() then
    --         dialog:AddSelection(18, FUNCTION_TABLE[18].Text)
    --     end
    -- end

    if System:IsDateTimeInRange('2025-06-12 05:00:00', '2025-06-12 23:59:59') then
        dialog:AddSelection(21, FUNCTION_TABLE[21].Text)
    end

    if player:IsGM() then
        dialog:AddSelection(100, FUNCTION_TABLE[100].Text)
        -- dialog:AddSelection(21, FUNCTION_TABLE[21].Text)
    end

    dialog:AddSelection(2, "Kết thúc đối thoại")
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
function LeQuan:OnSelection(scene, npc, player, selectionID, otherParams)
    local functionFromTable = FUNCTION_TABLE[selectionID]
    if functionFromTable ~= nil then
        functionFromTable.Exec(scene, npc, player, selectionID, otherParams)
        return
    end

    if selectionID == 67 then
		player:TitlesToItems()
        return
    end

    if selectionID == 100030 then
        GUI.OpenUI(player, "UIGiftCode")
        GUI.CloseDialog(player)
        return
    end
end

function LeQuan:OnItemSelected(scene, npc, player, itemID, otherParams)
end
