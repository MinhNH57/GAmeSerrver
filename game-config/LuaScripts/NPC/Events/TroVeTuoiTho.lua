local TroVeTuoiTho = Scripts[234]

local OFFSET = 20201 -- dùng để tính record, record gồm record tặng quà của 2 npc
local RECORD = 5000128
local CONFIGs = {
    [20201] = {
        GiftID = 20657, -- robot
        ReturnGift = {
            {
                Milestone = 200,
                Name = "Quà Tích lũy [200]",
                Record = 9980010,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 15018, Number = 100, Binding = 1 }}
            },
            {
                Milestone = 500,
                Name = "Quà Tích lũy [500]",
                Record = 9980011,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 20291, Number = 3, Binding = 1 }}
            },
            {
                Milestone = 1000,
                Name = "Quà Tích lũy [1000]",
                Record = 9980012,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 819, Number = 5, Binding = 1 }}
            },
            {
                Milestone = 3000,
                Name = "Quà Tích lũy [3000]",
                Record = 9980013,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 2017, Number = 1, Binding = 1 }}
            },
            {
                Milestone = 5000,
                Name = "Quà Tích lũy [5000]",
                Record = 9980014,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 1888, Number = 1, Binding = 1 }}
            }
        }
    },
    [20202] = {
        GiftID = 20658, -- búp bê
        ReturnGift = {
            {
                Milestone = 200,
                Name = "Quà Tích lũy [200]",
                Record = 9980015,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 20291, Number = 1, Binding = 1 }}
            },
            {
                Milestone = 500,
                Name = "Quà Tích lũy [500]",
                Record = 9980016,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 20406, Number = 1, Binding = 1 }}
            },
            {
                Milestone = 1000,
                Name = "Quà Tích lũy [1000]",
                Record = 9980017,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 20529, Number = 100, Binding = 1 }}
            },
            {
                Milestone = 3000,
                Name = "Quà Tích lũy [3000]",
                Record = 9980018,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 20555, Number = 1, Binding = 1 }}
            },
            {
                Milestone = 5000,
                Name = "Quà Tích lũy [5000]",
                Record = 9980019,
                RecordType = "Lifetime",
                BagSpace = 1,
                Items = {{ ItemID = 20601, Number = 100, Binding = 1 }}
            }
        }
    }
}

function TroVeTuoiTho:GetConfig(npc)
    local npcID = npc:GetResID()
    
    local config = CONFIGs[npcID]
    return config
end

function TroVeTuoiTho:GetGifted(npc, player)
    local gifted = Player:GetValueForeverRecore(player, RECORD + npc:GetResID() - OFFSET)
    if gifted < 0 then
        gifted = 0
    end

    return gifted
end

function TroVeTuoiTho:AddGifed(npc, player, added)
    local gifted = TroVeTuoiTho:GetGifted(npc, player)
    Player:SetValueOfForeverRecore(player, RECORD + npc:GetResID() - OFFSET, gifted + added)
end

function TroVeTuoiTho:OnOpen(scene, npc, player, otherParams)
    local config = TroVeTuoiTho:GetConfig(npc)

    local dialog = GUI:CreateNPCDialog()
    dialog:AddText('Đại hiệp hãy tặng quà cho ta. Càng nhiều quà, chơi càng vui thì ta càng biết ơn và có món quà tri ân lại đại hiệp!')
    dialog:AddText('')
    dialog:AddText('Quà mà ta muốn được nhận là ' .. System:GetItemName(config.GiftID) .. '!')
    dialog:AddText('')
    dialog:AddText('')
    
    dialog:AddItem(config.GiftID, 1, 0)
    dialog:SetItemSelectable(false)
    
    local gifted = TroVeTuoiTho:GetGifted(npc, player)

    if gifted > 0 then
        dialog:AddText('Ta đã được nhận ' .. gifted .. ' ' .. System:GetItemName(config.GiftID) .. '. Đa tạ bằng hữu!')
    end

    if System:IsDateTimeInRange('2024-07-05 05:00:00', '2024-08-20 05:00:00') then
        dialog:AddSelection(1, "Tặng quà")
        dialog:AddSelection(2, "Nhận Quà Tích lũy [200]")
        dialog:AddSelection(3, "Nhận Quà Tích lũy [500]")
        dialog:AddSelection(4, "Nhận Quà Tích lũy [1000]")
        dialog:AddSelection(5, "Nhận Quà Tích lũy [3000]")
        dialog:AddSelection(6, "Nhận Quà Tích lũy [5000]")
    end

    if player:IsGM() then
        dialog:AddSelection(999, "[GM] Reset")
    end

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
    -- ************************** --

end

function TroVeTuoiTho:OnSelection(scene, npc, player, selectionID, otherParams)
    GUI:CloseDialog(player)
    if selectionID == 1 then
        TroVeTuoiTho:TangQuaNPC(scene, npc, player)
        return
    end
    if selectionID >= 2 and selectionID <= 6 then
        TroVeTuoiTho:QuaTangThanhXuan(scene, npc, player, selectionID - 1)
        return
    end
    if selectionID == 99 then
        GUI:CloseDialog(player)
        return
    end
    if selectionID == 999 then
        Player:SetValueOfForeverRecore(player, RECORD, -1)
        Player:SetValueOfForeverRecore(player, RECORD + 1, -1)
        Player:SetValueOfForeverRecore(player, 9980010, -1)
        Player:SetValueOfForeverRecore(player, 9980011, -1)
        Player:SetValueOfForeverRecore(player, 9980012, -1)
        Player:SetValueOfForeverRecore(player, 9980013, -1)
        Player:SetValueOfForeverRecore(player, 9980014, -1)
        Player:SetValueOfForeverRecore(player, 9980015, -1)
        Player:SetValueOfForeverRecore(player, 9980016, -1)
        Player:SetValueOfForeverRecore(player, 9980017, -1)
        Player:SetValueOfForeverRecore(player, 9980018, -1)
        Player:SetValueOfForeverRecore(player, 9980019, -1)
        Player:SetValueOfForeverRecore(player, 9980020, -1)
        return
    end
end

function TroVeTuoiTho:TangQuaNPC(scene, npc, player)
    local config = TroVeTuoiTho:GetConfig(npc)

    local item_count = player:IsHasItem(config.GiftID)
    
    if item_count <= 0 then
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu không có " .. System:GetItemName(config.GiftID))
        return
    end

    GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu muốn tặng " .. item_count .. " " .. System:GetItemName(config.GiftID) .. " cho ta chứ?", 
        function()
            if Player:RemoveItem(player, config.GiftID, item_count) then
                TroVeTuoiTho:AddGifed(npc, player, item_count)
                GUI:ShowMessageBox(player, "Thông báo", "Tặng quà thành công " .. item_count .. " " .. System:GetItemName(config.GiftID))
                GUI:CloseDialog(player)
                System:WriteLog(39, '[QUATANGTHANHXUAN]' .. player:GetID() .. ' đã tặng ' .. item_count .. ' ' .. System:GetItemName(config.GiftID))
            end
        end)
end

function TroVeTuoiTho:QuaTangThanhXuan(scene, npc, player, milestone)
    local configMain = TroVeTuoiTho:GetConfig(npc)
    local config = configMain.ReturnGift[milestone]

    local gifted = TroVeTuoiTho:GetGifted(npc, player)
    if gifted < config.Milestone then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Đại hiệp chưa tặng quà đủ nhiều. Hãy tặng đủ " .. config.Milestone .. " " .. System:GetItemName(configMain.GiftID) .. " để nhận các phần quà này nhé!")
        for k, v in pairs(config.Items) do
            dialog:AddItem(v.ItemID, v.Number, 0)
            dialog:SetItemSelectable(false)
        end
        dialog:Show(npc, player)
        return
    end

    Global_ReceiveItemPackByConfig(npc, player, config)
end