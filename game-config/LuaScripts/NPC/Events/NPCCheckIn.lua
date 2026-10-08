local NPCCheckIn = Scripts[000236]

local START_DATE = '2024-08-30 05:00:00'
local END_DATE = '2024-09-10 05:00:00'

local CHECKIN_CONFIG = {{
	Record = 9980004,
    Package = {
        Name = "Check-In Đại Lễ [Sơ]",
        BagSpace = 4,
        Items = {
			{ ItemID = 20391, Number = 1, Binding = 1 }, -- túi huyền tinh ngẫu nhiên
			{ ItemID = 20378, Number = 5, Binding = 1 }, -- rương danh vọng (sơ)
			{ ItemID = 20051, Number = 5, Binding = 1 }, -- rương mảnh trang bị đồng hành sơ cấp
			{ ItemID = 506, Number = 5, Binding = 1 },	-- ngũ hành hồn thạch

		}
    },
    MoneyType = 1,
    Money = 300000,
    MoneyName = 'Bạc',
    Limit = 1
}, {
	Record = 9980005,
    Package = {
        Name = "Check-In Đại Lễ [Trung]",
        BagSpace = 4,
        Items = {
			{ ItemID = 20628, Number = 1, Binding = 1 }, -- túi huyền tinh ngẫu nhiên (trung)
			{ ItemID = 20378, Number = 10, Binding = 1 }, -- rương danh vọng (sơ)
			{ ItemID = 20379, Number = 10, Binding = 1 }, -- rương mảnh trang bị đồng hành trung cấp
			{ ItemID = 506, Number = 200, Binding = 1 } -- rương check-in trung
		}
    },
    MoneyType = 2,
    Money = 300,
    MoneyName = 'KNB',
    Limit = 5
}, {
	Record = 9980006,
    Package = {
        Name = "Check-In Đại Lễ [Cao]",
        BagSpace = 4,
        Items = {
			{ ItemID = 20629, Number = 1, Binding = 1 }, -- túi huyền tinh ngẫu nhiên (cao)
			{ ItemID = 20529, Number = 20, Binding = 1 }, -- rương mảnh trang bị đồng hành (cao)
			{ ItemID = 1026, Number = 10, Binding = 1 }, -- chiến thư du long
			{ ItemID = 506, Number = 1000, Binding = 1 } -- rương check-in (cao)
		},
	},
	MoneyType = 2,
	Money = 2500,
	MoneyName = 'KNB',
	Limit = 5
}}

function NPCCheckIn:OnOpen(scene, npc, player, otherParams)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Xin chào " .. player:GetName() .. "! Ta là sứ giả Đại Lễ.")

    if System:IsDateTimeInRange(START_DATE, END_DATE) then
        dialog:AddSelection(1002, "Check-in Đại Lễ")
    end

    dialog:Show(npc, player)
end

function NPCCheckIn:DoCheckIn(scene, npc, player, checkin_level)
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
        " để thực hiện Check-In Đại Lễ?", function()
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

function NPCCheckIn:CheckInDaiLe(scene, npc, player)
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

function NPCCheckIn:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 1002 then
        NPCCheckIn:CheckInDaiLe(scene, npc, player)
        return
    end

    if selectionID == 1003 then
        NPCCheckIn:DoCheckIn(scene, npc, player, 1)
        return
    end

    if selectionID == 1004 then
        NPCCheckIn:DoCheckIn(scene, npc, player, 2)
        return
    end

    if selectionID == 1005 then
        NPCCheckIn:DoCheckIn(scene, npc, player, 3)
        return
    end

    if selectionID == 1009 then
        Player:SetValueOfDailyRecore(player, CHECKIN_CONFIG[1].Record, -1)
        Player:SetValueOfDailyRecore(player, CHECKIN_CONFIG[2].Record, -1)
        Player:SetValueOfDailyRecore(player, CHECKIN_CONFIG[3].Record, -1)
    end

    if selectionID == 9 then
        NPCCheckIn:OnOpen(scene, npc, player, otherParams)
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

function NPCCheckIn:OnItemSelected(scene, npc, player, item, otherParams)
end
