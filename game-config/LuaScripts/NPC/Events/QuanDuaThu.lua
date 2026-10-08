-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local QuanDuaThu = Scripts[000223]

local BET_PACKAGES = {{
    Name = "100v bạc khóa",
    Number = 1000000,
    MoneyType = 0,
    WinItem = 20407,
    LoseItem = 20410,
    Bind = 1
}, {
    Name = "150 KNB",
    Number = 150,
    MoneyType = 2,
    WinItem = 20408,
    LoseItem = 20411,
    Bind = 1
}, {
    Name = "1000 KNB",
    Number = 1000,
    MoneyType = 2,
    WinItem = 20409,
    LoseItem = 20412,
    Bind = 1
}}

local SEED_RECORD = 3823

local CHAIN_CONFIG = {
    [2] = {
        SPECIAL_BETID = 2,
        SPECIAL_JOIN_RECORD = 5000146,
        SPECIAL_WIN_RECORD = 5000147,
        LUCKY_CHEST = { 20644, 20645, 20646 }
    },
    [3] = {
        SPECIAL_BETID = 3,
        SPECIAL_JOIN_RECORD = 5000148,
        SPECIAL_WIN_RECORD = 5000149,
        LUCKY_CHEST = { 20413, 20414, 20415 }
    }
}

local MILESTONE_1 = 5
local MILESTONE_2 = 10
local MILESTONE_3 = 15

function QuanDuaThu:SpecialStat(npc, player, bet_id)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText(
        "Thống kê tích lũy tham gia bình chọn gói " .. BET_PACKAGES[bet_id].Name .. " (cập nhật sau khi nhận thưởng bình chọn mỗi lượt đua):")

    local chainConfig = CHAIN_CONFIG[bet_id]

    local total_win = Player:GetValueForeverRecore(player, chainConfig.SPECIAL_WIN_RECORD)
    if total_win == -1 then
        total_win = 0
    end

    local record_reward_win_1 = SEED_RECORD * 100000 + chainConfig.LUCKY_CHEST[1]
    local total_reward_win_1 = Player:GetValueForeverRecore(player, record_reward_win_1)
    if total_reward_win_1 < 0 then
        total_reward_win_1 = 0
    end
    local record_reward_win_2 = SEED_RECORD * 100000 + chainConfig.LUCKY_CHEST[2]
    local total_reward_win_2 = Player:GetValueForeverRecore(player, record_reward_win_2)
    if total_reward_win_2 < 0 then
        total_reward_win_2 = 0
    end
    local record_reward_win_3 = SEED_RECORD * 100000 + chainConfig.LUCKY_CHEST[3]
    local total_reward_win_3 = Player:GetValueForeverRecore(player, record_reward_win_3)
    if total_reward_win_3 < 0 then
        total_reward_win_3 = 0
    end
    dialog:AddText("  - Đã chiến thắng " .. total_win .. 
    " lần, nhận thưởng [Thần cơ Diệu toán 5] " .. total_reward_win_1 ..
    " lần, [Thần cơ Diệu toán 10] " .. total_reward_win_2 ..
    " lần, [Thần cơ Diệu toán 15] " .. total_reward_win_3 .. " lần.")
    
    if total_win >= MILESTONE_1 then
        dialog:AddSelection(bet_id * 100 + 1, "Nhận thưởng Thần cơ Diệu toán [5]")
    end

    if total_win >= MILESTONE_2 then
        dialog:AddSelection(bet_id * 100 + 2, "Nhận thưởng Thần cơ Diệu toán [10]")
    end

    if total_win >= MILESTONE_3 then
        dialog:AddSelection(bet_id * 100 + 3, "Nhận thưởng Thần cơ Diệu toán [15]")
    end

    dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function QuanDuaThu:OnOpen(scene, npc, player, otherParams)

    -- ************************** --
    local dialog = GUI.CreateNPCDialog()

    dialog:AddText(
        "Sự kiện Cuộc đua thú cưng diễn ra vào các khung giờ lẻ các ngày từ 15/7/2024 đến 22/07/2024 tại Vân Trung Trấn. Mỗi cuộc đua cách nhau 2 giờ.")
    dialog:AddText("")
    dialog:AddText(
        "Trước khi bắt đầu cuộc đua, bằng hữu có thể bình chọn và ủng hộ cho thú cưng mình yêu thích.")
    dialog:AddText("")
    dialog:AddText(
        "Sau khi kết thúc cuộc đua, có thể đến gặp ta để nhận thưởng bình chọn. Dù thú cưng bằng hữu lựa chọn có giành chiến thắng hay không, cũng đều được nhận thưởng. <color=#f9e2af>Hãy nhớ nhận thưởng trước khi cuộc đua tiếp theo diễn ra!</color>")
    dialog:AddText("")
    dialog:AddText(
        "Nếu bằng hữu nhiệt tình lựa chọn gói bình chọn cao nhất, khi tích lũy đủ 5/10/15 lần chiến thắng, sẽ nhận thêm phần thưởng giá trị.")
    dialog:AddText("")

    dialog:AddSelection(1, "Tham gia Cuộc đua Thú cưng")
    -- dialog:AddSelection(2, "Xem kết quả và nhận thưởng")
    dialog:AddSelection(11, "Nhận thưởng tích lũy")

     if player:IsGM() then
        dialog:AddSelection(111, "[GM] Bắt đầu đua")
        dialog:AddSelection(222, "[GM] Set 30 trận thắng 150knb")
        dialog:AddSelection(223, "[GM] Set 10 trận thắng 150knb")
        dialog:AddSelection(224, "[GM] Set 5 trận thắng 150knb")
        dialog:AddSelection(225, "[GM] Set 1 trận thắng 150knb")
        dialog:AddSelection(333, "[GM] Set 30 trận thắng 1000knb")
     end

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
    -- ************************** --
end

function QuanDuaThu:IsBettingState(npc, player)
    local state = System:PetRace_GetRaceStatus()
    if state == -1 then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Hiện tại không có cuộc đua nào đang diễn ra!")
        dialog:Show(npc, player)
        return false
    end
    if state ~= 1 then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Hiện tại không phải thời gian bình chọn!")
        dialog:AddText(QuanDuaThu:GetBetDescription(System:PetRace_GetBetRecord(player)))
        dialog:Show(npc, player)
        return false
    end
    return true
end

function QuanDuaThu:JoinPetRaceEvent(scene, npc, player)
    
    System:PetRace_Join(player)
    GUI:CloseDialog(player)
    -- local dialog = GUI:CreateNPCDialog()
    -- dialog:AddText("Hãy lựa chọn thú cưng yêu thích!")
    -- for k, v in pairs(racers) do
    --     dialog:AddSelection(100000 + k, v)
    -- end
    -- dialog:AddSelection(99, "Ta sẽ quay lại sau.")
    -- dialog:Show(npc, player)
end

function QuanDuaThu:SelectBetValue(scene, npc, player, racerID)
    if not QuanDuaThu:IsBettingState(npc, player) then
        return
    end

    local racers = System:PetRace_GetRacers()
    local dialog = GUI:CreateNPCDialog()

    dialog:AddText("Bạn đã lựa chọn " .. racers[racerID])
    dialog:AddText("Hãy lựa chọn gói bình chọn")

    for k, v in ipairs(BET_PACKAGES) do
        dialog:AddSelection((k + 100) * 100000 + racerID, '' .. v.Name)
    end

    dialog:Show(npc, player)
end

function QuanDuaThu:Betting(scene, npc, player, racerID, betID)
    if not QuanDuaThu:IsBettingState() then
        return
    end

    local racers = System:PetRace_GetRacers()
    local dialog = GUI:CreateNPCDialog()
    local bet_package = BET_PACKAGES[betID]
    local in_bag = Player:CheckMoney(player, bet_package.MoneyType)

    if in_bag < bet_package.Number then
        dialog:AddText("Bẳng hữu không đem theo đủ tiền!")
    else
        local bet_result = System:PetRace_Bet(player, racerID, betID)
        if bet_result then
            Player:SubMoney(player, bet_package.Number, bet_package.MoneyType, "PETRACE_BET_" .. betID)
            dialog:AddText('Đã bình chọn thành công cho ' .. racers[racerID] .. ' với gói bình chọn ' ..
                               bet_package.Name .. '.')
        else
            dialog:AddText('Bằng hữu đã bình chọn rồi!')
        end
    end
    dialog:Show(npc, player)
    return
end

function QuanDuaThu:ShowRewardInfo(npc, player)
    local state = System:PetRace_GetRaceStatus()
    if state ~= 3 then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Không phải thời gian nhận thưởng!")
        dialog:Show(npc, player)
        return
    end

    local bet_record = System:PetRace_GetBetRecord(player)
    if bet_record == nil then
        GUI:ShowMessageBox(player, "Cuộc đua thú cưng",
            "Bằng hữu không tham gia bình chọn trong lượt đua này!")
        return
    end

    local dialog = GUI:CreateNPCDialog()
    if System:PetRace_IsWinner(player) then
        dialog:AddText("Chúc mừng thú cưng bằng hữu lựa chọn đã giành chiến thắng!")
        dialog:AddText(QuanDuaThu:GetBetDescription(System:PetRace_GetBetRecord(player)))
        dialog:AddSelection(3, "Nhận thưởng Bình chọn")
    else
        dialog:AddText("Rất tiếc thú cưng bằng hữu lựa chọn đã không chiến thắng!")
        dialog:AddText(QuanDuaThu:GetBetDescription(System:PetRace_GetBetRecord(player)))
        dialog:AddSelection(3, "Nhận thưởng Bình chọn")
        dialog:AddSelection(99, "Ta sẽ quay lại sau.")
    end

    dialog:Show(npc, player)
    return
end

function QuanDuaThu:GetBetDescription(record)
    if record == nil then
        return ''
    end
    local racers = System:PetRace_GetRacers()
    local racer = racers[record.GetX()]
    local bet_package = BET_PACKAGES[record.GetY()]
    return 'Bằng hữu đã bình chọn cho ' .. racer .. ' với gói bình chọn ' .. bet_package.Name .. '!'
end

function QuanDuaThu:ReceiveReward(npc, player)
    local state = System:PetRace_GetRaceStatus()
    if state ~= 3 then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Không phải thời gian nhận thưởng!")
        dialog:Show(npc, player)
        return
    end

    if not Player.HasFreeBagSpaces(player, 1) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return
    end

    local is_winner = System:PetRace_IsWinner(player)
    local is_received = System:PetRace_IsRewardReceived(player)

    if not is_received then
        local set_received = System:PetRace_SetRewardReceived(player)
        if not set_received then
            local dialog = GUI:CreateNPCDialog()
            dialog:AddText("Đã có lỗi xảy ra, vui lòng thử lại sau!")
            dialog:AddSelection(99, "Ta sẽ quay lại sau.")
            dialog:Show(npc, player)
            return
        else
            local bet_record = System:PetRace_GetBetRecord(player)
            local bet_id = bet_record.GetY()
            local bet_package = BET_PACKAGES[bet_id]
            local dialog = GUI:CreateNPCDialog()

            local rewardItem = bet_package.WinItem
            if not is_winner then
                rewardItem = bet_package.LoseItem
            end

            Player:AddItemLuaAndNotify(player, rewardItem, 1, 0, bet_package.Bind)

            for specialId, chainConfig in pairs(CHAIN_CONFIG) do
                -- cập nhật trạng thái nhận thưởng chăm chỉ/may mắn
                if bet_id == chainConfig.SPECIAL_BETID then
                    if bet_id == chainConfig.SPECIAL_BETID then
                        local daily_join = Player:GetValueForeverRecore(player, chainConfig.SPECIAL_JOIN_RECORD)
                        if daily_join == -1 then
                            daily_join = 0
                        end
                        Player:SetValueOfForeverRecore(player, chainConfig.SPECIAL_JOIN_RECORD, daily_join + 1)
                    end

                    if is_winner then
                        local daily_win = Player:GetValueForeverRecore(player, chainConfig.SPECIAL_WIN_RECORD)
                        if daily_win == -1 then
                            daily_win = 0
                        end
                        Player:SetValueOfForeverRecore(player, chainConfig.SPECIAL_WIN_RECORD, daily_win + 1)
                    end
                end
            end

            -- cược gói 1000 KNB
            

            dialog:AddText("Bằng hữu đã nhận thưởng thành công!")
            -- TODO: add nhận thưởng
            dialog:Show(npc, player)
            return
        end
    else
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText("Bằng hữu đã nhận thưởng rồi!")
        dialog:AddSelection(99, "Ta sẽ quay lại sau.")
        dialog:Show(npc, player)
        return
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function QuanDuaThu:OnSelection(scene, npc, player, selectionID, otherParams)
    System:WriteToConsole(selectionID)
    if selectionID == 1 then
        QuanDuaThu:JoinPetRaceEvent(scene, npc, player)
        return
    end

    if selectionID == 2 then
        QuanDuaThu:ShowRewardInfo(npc, player)
        return
    end

    if selectionID == 3 then
        QuanDuaThu:ReceiveReward(npc, player)
        return
    end

    local total_win_2 = Player:GetValueForeverRecore(player, CHAIN_CONFIG[2].SPECIAL_WIN_RECORD)
    if total_win_2 == -1 then
        total_win_2 = 0
    end

    local total_win_3 = Player:GetValueForeverRecore(player, CHAIN_CONFIG[3].SPECIAL_WIN_RECORD)
    if total_win_3 == -1 then
        total_win_3 = 0
    end

    if selectionID == 201 then
        System.WriteToConsole(CHAIN_CONFIG[2].LUCKY_CHEST[1])
        QuanDuaThu:ReceiveItemOnce(npc, player, CHAIN_CONFIG[2].LUCKY_CHEST[1], total_win_2, MILESTONE_1)
        return
    end

    if selectionID == 202 then
        QuanDuaThu:ReceiveItemOnce(npc, player, CHAIN_CONFIG[2].LUCKY_CHEST[2], total_win_2, MILESTONE_2)
        return
    end

    if selectionID == 203 then
        QuanDuaThu:ReceiveItemOnce(npc, player, CHAIN_CONFIG[2].LUCKY_CHEST[3], total_win_2, MILESTONE_3)
        return
    end

    if selectionID == 301 then
        QuanDuaThu:ReceiveItemOnce(npc, player, CHAIN_CONFIG[3].LUCKY_CHEST[1], total_win_3, MILESTONE_1)
        return
    end

    if selectionID == 302 then
        QuanDuaThu:ReceiveItemOnce(npc, player, CHAIN_CONFIG[3].LUCKY_CHEST[2], total_win_3, MILESTONE_2)
        return
    end

    if selectionID == 303 then
        QuanDuaThu:ReceiveItemOnce(npc, player, CHAIN_CONFIG[3].LUCKY_CHEST[3], total_win_3, MILESTONE_3)
        return
    end

    if selectionID == 222 then
        Player:SetValueOfForeverRecore(player, 5000147, 30)
        return
    end
    if selectionID == 223 then
        Player:SetValueOfForeverRecore(player, 5000147, 10)
        return
    end
    if selectionID == 224 then
        Player:SetValueOfForeverRecore(player, 5000147, 5)
        return
    end
    if selectionID == 225 then
        Player:SetValueOfForeverRecore(player, 5000147, 1)
        return
    end

    if selectionID == 333 then
        Player:SetValueOfForeverRecore(player, 5000149, 30)
        return
    end

    if selectionID == 11 then
        System:PetRace_AccumulateUI_Open(player)
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 12 then
        QuanDuaThu:SpecialStat(npc, player, 2)
        return
    end

    if selectionID == 111 then
        GUI:CloseDialog(player)
        System:ForceStartRace(player)
    end

    if selectionID > 100000 then
        System:WriteToConsole(selectionID)
        local petID = selectionID % 100000
        local selectionType = math.floor(selectionID / 100000)
        local racers = System:PetRace_GetRacers()

        if selectionType == 1 then
            QuanDuaThu:SelectBetValue(scene, npc, player, petID)
            return
        end

        local betID = selectionType - 100
        QuanDuaThu:Betting(scene, npc, player, petID, betID)
        return
    end

    if selectionID == 99 then
        GUI:CloseDialog(player)
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function QuanDuaThu:OnItemSelected(scene, npc, player, itemID, otherParams)

    -- ************************** --

    -- ************************** --

end

function QuanDuaThu:ReceiveItemOnce(npc, player, itemID, total, interval)
    -- check ô trống
    if not Player.HasFreeBagSpaces(player, 1) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return false
    end

    local record = SEED_RECORD * 100000 + itemID -- random number để lưu record nhận item trong ngày

    local received = Player:GetValueForeverRecore(player, record)
    if received == -1 then
        received = 0
    end

    -- System:WriteToConsole('' .. received)
    -- System:WriteToConsole('JOIN:' .. total)
    -- System:WriteToConsole('' .. interval)
    -- System:WriteToConsole('' .. math.floor(total / interval))

    if received >= math.floor((total + 15 - interval) / 15) then
        GUI:ShowMessageBox(player, "Cuộc đua Thú cưng",
            "Bằng hữu đã nhận hết tích lũy vật phẩm này rồi!")
        return false
    else
        Player:SetValueOfForeverRecore(player, record, received + 1)
    end

    Player:AddItemLuaAndNotify(player, itemID, 1, 0, 1)

    return true
end
