-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng
local QuanOTT = Scripts[000228]

local OPTION_NAME = {
    [10] = "Búa",
    [11] = "Bao",
    [12] = "Kéo"
}

local OPTION_NAME_ICON = {
    ['10'] = 'rock',
    ['11'] = 'paper',
    ['12'] = 'scissors'
}

local ACCUMULATE_REWARD_5 = 20327
local ACCUMULATE_REWARD_10 = 20328
local ACCUMULATE_REWARD_15 = 20577

local ACCUMULATE_CONFIG = {
    [5] = { RewardID = 20327, Record = 5000142 },
    [10] = { RewardID = 20328, Record = 5000143 },
    [15] = { RewardID = 20577, Record = 5000144 }
}

function QuanOTT:OnOpen(scene, npc, player, otherParams)
    local dialog = GUI.CreateNPCDialog()

    dialog:AddText(
        "Hoạt động Vui Game Dân Gian diễn ra vào các khung giờ lẻ từ 23/07/2024 đến 01/08/2024 tại Vân Trung Trấn. Mỗi lượt chơi cách nhau 2 giờ.")
    dialog:AddText("")
    dialog:AddText(
        "Hoạt động diễn ra theo thể thức Oẳn Tù Tì. Mỗi khi bắt đầu một lượt chơi, người chơi đăng ký tham gia bằng cách lựa chọn chiến thuật [Kéo, Búa, Bao] và gói đặt cược [50 vạn bạc khóa, 100 KNB, 1000 KNB]. Thời gian đăng ký mỗi lượt là 15 phút.")
    dialog:AddText("")
    dialog:AddText(
        "Sau khi hết thời gian đăng ký, hệ thống sẽ tiến hành bắt cặp trận đấu ngẫu nhiên giữa các người chơi. Kết quả trận đấu được tính dựa trên lựa chọn của người chơi: Kéo thắng Bao, Bao thắng Búa, Búa thắng Kéo. Giá trị gói cược không ảnh hưởng tới kết quả trận đấu, chỉ ảnh hưởng tới phần thường được nhận.")
    dialog:AddText("")
    dialog:AddText(
        "Người chơi có thể xem lại lịch sử tham gia hoạt động trong ngày và nhận phần thưởng dựa theo gói cược đã lựa chọn. Lưu ý nhận thưởng trước khi qua ngày mới.")

    dialog:AddSelection(1, "Lựa chọn chiến thuật.")
    dialog:AddSelection(2, "Lịch sử tham gia và nhận thưởng.")
    dialog:AddSelection(3, "Xem tích lũy thắng/thua")
    dialog:AddSelection(4, "Xem bảng xếp hạng Bàn tay vàng")
    dialog:AddSelection(5, "Xem chi tiết phần thưởng")

    if player:IsGM() then
        dialog:AddSelection(999, "[GM] Bắt đầu vòng mới")
    end
    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
end

function QuanOTT:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 5 then
        QuanOTT:ShowRewards(scene, npc, player)
        return
    end
    if selectionID == 1 then
        QuanOTT:SelectOptions(scene, npc, player)
        return
    end

    if selectionID == 2 then
        QuanOTT:ShowHistory(scene, npc, player)
        return
    end

    if selectionID == 3 then
        --GUI:ShowMessageBox(player, "Vui Dân Gian", "Tính năng đang bảo trì.\nVui lòng quay lại vào ngày mai.")
         QuanOTT:ShowAccumulation(scene, npc, player)
        return
    end

    if selectionID == 4 then
        QuanOTT:ShowRanking(scene, npc, player)
        return
    end

    if selectionID >= 100 and selectionID <= 199 then
        QuanOTT:ShowMatch(scene, npc, player, selectionID - 100)
        return
    end

    if selectionID == 999 then
        OTT:ForceStart()
        return
    end

    if selectionID == 99 then
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 98 then
        QuanOTT:OnOpen(scene, npc, player, otherParams)
        return
    end

    if selectionID == 90 then
        QuanOTT:ReceiveAccumulationReward(scene, npc, player, 5)
        return
    end

    if selectionID == 91 then
        QuanOTT:ReceiveAccumulationReward(scene, npc, player, 10)
        return
    end

    if selectionID == 92 then
        QuanOTT:ReceiveAccumulationReward(scene, npc, player, 15)
        return
    end

    if selectionID >= 20321 and selectionID <= 20328 or selectionID == 20577 then
        QuanOTT:ShowRewardDetails(scene, npc, player, selectionID)
        return
    end
end

function QuanOTT:ReceiveAccumulationReward(scene, npc, player, type)
    GUI:CloseDialog(player)

    if not Player.HasFreeBagSpaces(player, 1) then
        GUI:ShowMessageBox(player, "Vui Dân Gian", "Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm.")
        return
    end

    local reward_config = ACCUMULATE_CONFIG[type]

    local record = reward_config.Record
    local reward_id = reward_config.RewardID
    local accumulation = OTT:GetWinCount1000(player)
    local can_receive = math.floor((accumulation + 15 - type) / 15)

    local received = Player:GetValueForeverRecore(player, record)
    if received == -1 then
        received = 0
    end


    System:WriteToConsole(received)
    System:WriteToConsole(can_receive)
    if received >= can_receive then
        GUI:ShowMessageBox(player, "Vui Dân Gian", "Bằng hữu đã nhận hết tích lũy phần thưởng này rồi.")
        return
    end

    Player:SetValueOfForeverRecore(player, record, received + 1)
    Player:AddItemLuaAndNotify(player, reward_id, 1, 0, 1)
    GUI:ShowMessageBox(player, "Vui Dân Gian", "Nhận thưởng thành công.")
end

function QuanOTT:ShowAccumulation(scene, npc, player)
    local dialog = GUI:CreateNPCDialog()
    local accumulation = OTT:GetWinCount1000(player)
    dialog:AddText("Bằng hữu đã tích lũy được " .. accumulation .. " chiến thắng với gói cược 1000 KNB.")
    dialog:AddText("")
    dialog:AddText(
        "Khi tích lũy thắng gói 1000KNB đạt mốc chỉ định là 5, 10 và 15. Người chơi sẽ nhận được rương thưởng tương ứng. Tích lũy thắng khi đạt mốc 15 sẽ reset lại về 0 và nhận rương thưởng tích lũy thắng lại từ đầu.")

    local received5 = Player:GetValueForeverRecore(player, ACCUMULATE_CONFIG[5].Record)
    if received5 < 0 then
        received5 = 0
    end
    local can_receive5 = math.floor((accumulation + 10) / 15)

    local received10 = Player:GetValueForeverRecore(player, ACCUMULATE_CONFIG[10].Record)
    if received10 < 0 then
        received10 = 0
    end
    local can_receive10 = math.floor((accumulation + 5) / 15)

    local received15 = Player:GetValueForeverRecore(player, ACCUMULATE_CONFIG[15].Record)
    if received15 < 0 then
        received15 = 0
    end
    local can_receive15 = math.floor(accumulation / 15)

    dialog:AddText("Bằng hữu đã nhận thưởng " .. received5 .. "/" .. can_receive5 .. " rương Tích Lũy 5.")
    dialog:AddText("Bằng hữu đã nhận thưởng " .. received10 .. "/" .. can_receive10 .. " rương Tích Lũy 10.")
    dialog:AddText("Bằng hữu đã nhận thưởng " .. received15 .. "/" .. can_receive15 .. " rương Tích Lũy 15.")

    if received5 < can_receive5 then
        dialog:AddSelection(90, "Nhận 1 rương Tích Lũy 5")
    end

    if received10 < can_receive10 then
        dialog:AddSelection(91, "Nhận 1 rương Tích Lũy 10")
    end

    if received15 < can_receive15 then
        dialog:AddSelection(92, "Nhận 1 rương Tích Lũy 15")
    end

    dialog:AddSelection(5, "Xem chi tiết phần thưởng")

    dialog:Show(npc, player)
end

function QuanOTT:ShowRanking(scene, npc, player)
    local top_10 = OTT:GetTop10Rank()
    local dialog = GUI:CreateNPCDialog()
    if top_10 == nil then
        dialog:AddText("Bảng xếp hạng đang cập nhật!")
    else
        dialog:AddText("Điểm của mỗi người chơi được tính như sau:")
        dialog:AddText("\tGói cược 10v bạc khóa: thắng được 1 điểm, hòa/thua được 0 điểm.")
        dialog:AddText("\tGói cược 100 KNB: thắng được 2 điểm, hòa/thua được 1 điểm.")
        dialog:AddText("\tGói cược 1000 KNB: thắng được 6 điểm, hòa/thua được 3 điểm.")
        dialog:AddText("")
        dialog:AddText("Bảng xếp hạng hiện tại:")
        dialog:AddText("")
        for k, v in ipairs(top_10) do
            dialog:AddText("" .. k .. ". " .. v .. " điểm")
        end
    end
    dialog:Show(npc, player)
end

function QuanOTT:ShowRewardDetails(scene, npc, player, rewardID)
    local dialog = GUI:CreateNPCDialog()
    dialog:AddText("Trong mỗi " .. System:GetItemName(rewardID) .. " gồm những vật phẩm dưới đây.")

    local rewards = {
        [20327] = { -- tích lũy 5
            { ItemID = 191,   Number = 3 },
            { ItemID = 20270, Number = 3 },
            { ItemID = 25143, Number = 20 },
        },
        [20328] = { -- tích lũy 10
            { ItemID = 192,   Number = 2 },
            { ItemID = 20270, Number = 10 },
            { ItemID = 25143, Number = 40 },
        },
        [20577] = { -- tích lũy 15
            { ItemID = 193,   Number = 1 },
            { ItemID = 20406, Number = 1 },
            { ItemID = 819,   Number = 1 },
        },
        [20321] = { -- thắng 100 knb
            { ItemID = 190,   Number = 1 },
            { ItemID = 20378, Number = 20 },
            { ItemID = 1055,  Number = 2 },
            { ItemID = 600,   Number = 2 },
        },
        [20322] = { -- thua 100 knb
            { ItemID = 189,   Number = 2 },
            { ItemID = 20378, Number = 8 },
            { ItemID = 1055,  Number = 1 },
            { ItemID = 600,   Number = 1 },
        },
        [20323] = { -- thắng 1000 knb
            { ItemID = 191,   Number = 1 },
            { ItemID = 20485, Number = 25 },
            { ItemID = 20345, Number = 3 },
            { ItemID = 20270, Number = 1 },
        },
        [20324] = { -- thua 1000 knb
            { ItemID = 190,   Number = 2 },
            { ItemID = 20485, Number = 10 },
            { ItemID = 20345, Number = 1 },
            { ItemID = 1055,  Number = 3 },
        },
        [20325] = { -- thắng 100v bạc
            { ItemID = 189,   Number = 2 },
            { ItemID = 20378, Number = 5 },
            { ItemID = 1054,  Number = 5 },
        },
        [20326] = { -- thua 100v bạc
            { ItemID = 188,   Number = 2 },
            { ItemID = 20378, Number = 3 },
            { ItemID = 1054,  Number = 1 },
        }
    }

    local details = rewards[rewardID]

    for k, v in ipairs(details) do
        dialog:AddItem(v.ItemID, v.Number, 0)
    end

    dialog:SetItemSelectable(false)

    dialog:AddSelection(98, "Quay lại")
    dialog:Show(npc, player)
end

function QuanOTT:ShowRewards(scene, npc, player)
    local dialog = GUI:CreateNPCDialog()
    dialog:AddText(
        "Mỗi lượt chơi, giành chiến thắng sẽ nhận được Rương Chiến thắng, hòa hoặc thua cuộc sẽ nhận được Rương An ủi.")

    local rewards = { 20327, 20328, 20577, 20321, 20322, 20323, 20324, 20325, 20326 }
    for k, v in ipairs(rewards) do
        dialog:AddSelection(v, 'Xem ' .. System:GetItemName(v))
    end

    dialog:Show(npc, player)
end

function QuanOTT:ShowMatch(scene, npc, player, round_index)
    local info = OTT:ShowMatch(player, round_index)
    System:WriteToConsole(info['player1'])
    System:WriteToConsole(OPTION_NAME_ICON[info['player1_option']])
    GUI:OpenUIWithStringParams(player, "Messages/OTT", "player1_name", info["player1"], "player1_status",
        OPTION_NAME_ICON[info["player1_option"]], "player2_name", info["player2"], "player2_status",
        OPTION_NAME_ICON[info["player2_option"]], "win_status", info["match_result"])
    GUI:CloseDialog(player)
end

function QuanOTT:ShowHistory(scene, npc, player)
    local dialog = GUI:CreateNPCDialog()

    if System:GetHour() < 7 then
        dialog:AddText("Hôm nay bằng hữu chưa tham gia vòng chơi nào!")
        dialog:Show(npc, player)
        return
    end

    local rounds = OTT:GetHistory(player)
    if rounds == nil then
        dialog:AddText("Hôm nay bằng hữu chưa tham gia vòng chơi nào!")
        dialog:Show(npc, player)
        return
    end

    dialog:AddText(
        "Bằng hữu có thể xem lịch sử tham gia hoạt động trong ngày tại đây. Sau khi xem lịch sử lần đầu của một vòng, phần thưởng của vòng đó sẽ được tự động gửi vào túi đồ của bằng hữu.")
    dialog:AddText(
        "Lưu ý: Đảm bảo túi đồ của bằng hữu có ít nhất 1 ô trống để nhận thưởng.")
    dialog:AddText("")
    dialog:AddText("Lịch sử tham gia hoạt động sẽ bị xóa sau khi sang ngày mới!")

    for k, v in pairs(rounds) do
        dialog:AddSelection(100 + k, v)
    end
    dialog:Show(npc, player)
end

function QuanOTT:SelectOptions(scene, npc, player)
    local dialog = GUI:CreateNPCDialog()

    if OTT:GetState() ~= 1 then
        dialog:AddText("Hiện tại chưa diễn ra vòng chơi mới. Vui lòng quay lại sau.")
        dialog:Show(npc, player)
        return
    end

    dialog:AddText(
        "Sự kiện Vui Năm Mới chơi theo thể thức Oẳn Tù Tì. Bằng hữu hãy lựa chọn chiến thuật dưới đây.")

    dialog:AddText("")
    dialog:AddText('Bước 1: Chọn Bao - Búa - Kéo bằng cách click vào hình tương ứng và nhấn "Chọn"')
    dialog:AddText('Bước 2: Chọn "Xác nhận chọn"')


    if OTT:AlreadyBet(player) then
        local bet_record = OTT:GetBetRecord(player)
        local option = bet_record:GetX()
        local value = bet_record:GetY()
        local money_name = "KNB"
        if value == 1000000 then
            money_name = "Bạc khóa"
        end
        dialog:AddText("Bằng hữu đã đăng ký lượt chơi lần này với chiến thuật " ..
            OPTION_NAME[option] .. " và gói cược " .. value .. money_name .. ".")
    else
        dialog:AddItem(10, 1, 0)
        dialog:AddItem(11, 1, 0)
        dialog:AddItem(12, 1, 0)
    end
    dialog:Show(npc, player)
end

function QuanOTT:SelectBetValue(scene, npc, player, option)
    local dialog = GUI:CreateNPCDialog()

    if OTT:GetState() ~= 1 then
        dialog:AddText("Hiện tại chưa diễn ra vòng chơi mới. Vui lòng quay lại sau.")
        dialog:Show(npc, player)
        return
    end

    dialog:AddText(
        "Sự kiện Vui Năm Mới chơi theo thể thức Oẳn Tù Tì. Bằng hữu hãy lựa chọn gói cược dưới đây.")

    dialog:AddText("")
    dialog:AddText(
        'Bước 1: Lựa chọn mức cược bằng cách click vào hình mức cược bạn muốn và nhấn "Chọn". Có 3 mức cược tương ứng: 50 vạn bạc khóa, 100 KNB, 1000 KNB')
    dialog:AddText('Bước 2: Chọn "Xác nhận chọn"')
    dialog:AddText('Bước 3: Sau khi hiện thông báo xác nhận, chọn "OK" để hoàn thành đặt cược')

    if OTT:AlreadyBet(player) then
        local bet_record = OTT:GetBetRecord(player)
        local option = bet_record:GetX()
        local value = bet_record:GetY()

        local money_name = "KNB"
        if value == 1000000 then
            money_name = "Bạc khóa"
        end
        dialog:AddText("Bằng hữu đã đăng ký lượt chơi lần này với chiến thuật " ..
            OPTION_NAME[option] .. " và gói cược " .. value .. " " .. money_name .. ".")
    else
        dialog:AddItem(1, 1000000, 0)
        dialog:AddItem(3, 100, 0)
        dialog:AddItem(3, 1000, 0)
    end
    dialog:AddParam(1, option)
    dialog:Show(npc, player)
end

function QuanOTT:OnItemSelected(scene, npc, player, item, otherParams)
    System:WriteToConsole(item:GetItemID())
    System:WriteToConsole(item:GetItemCount())

    if otherParams == nil or otherParams[1] == nil then
        QuanOTT:SelectBetValue(scene, npc, player, item:GetItemID())
        return
    end

    local selected_option = tonumber(otherParams[1])
    local bet_value = item:GetItemCount()
    local money_type = 2
    local money_name = "KNB"
    if bet_value == 1000000 then
        money_type = 0
        money_name = "Bạc khóa"
    end

    System:WriteToConsole(OPTION_NAME[selected_option])
    System:WriteToConsole(bet_value)
    System:WriteToConsole(money_name)
    System:WriteToConsole(money_type)

    GUI:ShowMessageBox(player, "Vui Dân Gian",
        "Xác nhận lựa chọn chiến thuật " ..
        OPTION_NAME[selected_option] .. " với gói cược " .. bet_value .. " " .. money_name .. "?", function()
            if OTT:GetState() == 1 then
                if Player:SubMoney(player, bet_value, money_type, "OTT_BET_" .. selected_option) then
                    OTT:Bet(player, selected_option, bet_value)
                    GUI:ShowMessageBox(player, "Vui Dân Gian", "Lựa chọn thành công!")
                else
                    GUI:ShowMessageBox(player, "Vui Dân Gian", "Bằng hữu không đem theo đủ tiền!")
                end
            else
                GUI:ShowMessageBox(player, "Vui Dân Gian", "Hiện tại chưa diễn ra vòng chơi mới. Vui lòng quay lại sau.")
            end
            GUI:CloseDialog(player)
        end)
end
