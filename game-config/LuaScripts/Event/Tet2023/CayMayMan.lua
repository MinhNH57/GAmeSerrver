-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000148' bên dưới thành ID tương ứng
local CayMayMan = Scripts[700006]

local ITEMID_MUTDUA = 15181
local ITEMID_RUOUVANG = 15182
local ITEMID_HATDIEU = 15183
local ITEMID_TRA = 15184
local ITEMID_GIO = 15185
local ITEMID_GIOQUATET = 15178

-- các hàm nhận thưởng
local function AddMoney(player, money, type)
    return Player.AddMoney(player, money, type, "TET2023")
end

local function AddItem(player, ID, Number)
    return Player.AddItemLuaAndNotify(player, ID, Number, 0, 0)
end

local function AddHorse(player, duration, other)
    local horseId = math.random(3463, 3467)
    return Player.AddItemLuaAndNotify(player, horseId, 1, 0, 1, 0, duration * 24 * 60)
end

local PROBABILITY_DEFINE = {
    -- KNB Khóa 42330000
    ['KNB Khóa 30'] = 23281500, -- 55%
    ['KNB Khóa 50'] = 16932000, -- 40%
    ['KNB Khóa 100'] = 2116500, -- 5%
    -- Bạc 17000000
    ['Bạc 2000'] = 8500000, -- 50%
    ['Bạc 3000'] = 7650000, -- 45%
    ['Bạc 5000'] = 850000, -- 5%
    -- Bạc Khóa 20000000
    ['Bạc Khóa 30000'] = 11000000, -- 55%
    ['Bạc Khóa 50000'] = 8000000, -- 40%
    ['Bạc Khóa 100000'] = 1000000, -- 5%
    -- Ngũ hành hồn thạch 5000000
    ['Ngũ hành hồn thạch 10'] = 2500000, -- 50%
    ['Ngũ hành hồn thạch 20'] = 2250000, -- 45%
    ['Ngũ hành hồn thạch 30'] = 250000, -- 5%
    -- Huyền tinh 15000000
    ['Huyền tinh 3'] = 12000000, -- 80%
    ['Huyền tinh 4'] = 2250000, -- 15%
    ['Huyền tinh 5'] = 600000, -- 4%
    ['Huyền tinh 6'] = 150000, -- 1%
    -- Bánh 20000
    ['Bánh chưng'] = 10000,
    ['Bánh dày'] = 10000,
    -- Tinh hoạt lực 535000
    ['Tinh khí tán (1000)'] = 267500,
    ['Hoạt khí tán (1000)'] = 267500,
    -- Ngựa
    ['Hổ Cát Tường [7 ngày]'] = 100000,
    ['Hổ Cát Tường [15 ngày]'] = 15000
}

local RECEIVE_REWARD = {
    -- KNB Khóa 42830000
    ['KNB Khóa 30'] = {AddMoney, 30, 3}, -- 55%
    ['KNB Khóa 50'] = {AddMoney, 50, 3}, -- 40%
    ['KNB Khóa 100'] = {AddMoney, 100, 3}, -- 5%
    -- Bạc 15000000
    ['Bạc 2000'] = {AddMoney, 2000, 1}, -- 50%
    ['Bạc 3000'] = {AddMoney, 3000, 1}, -- 45%
    ['Bạc 5000'] = {AddMoney, 5000, 1}, -- 5%
    -- Bạc Khóa 20000000
    ['Bạc Khóa 30000'] = {AddMoney, 30000, 0}, -- 55%
    ['Bạc Khóa 50000'] = {AddMoney, 50000, 0}, -- 40%
    ['Bạc Khóa 100000'] = {AddMoney, 100000, 0}, -- 5%
    -- Ngũ hành hồn thạch 5000000
    ['Ngũ hành hồn thạch 10'] = {AddItem, 10, 506}, -- 50%
    ['Ngũ hành hồn thạch 20'] = {AddItem, 20, 506}, -- 45%
    ['Ngũ hành hồn thạch 30'] = {AddItem, 30, 506}, -- 5%
    -- Huyền tinh 15000000
    ['Huyền tinh 3'] = {AddItem, 1, 185}, -- 80%
    ['Huyền tinh 4'] = {AddItem, 1, 186}, -- 15%
    ['Huyền tinh 5'] = {AddItem, 1, 187}, -- 4%
    ['Huyền tinh 6'] = {AddItem, 1, 188}, -- 1%
    -- Bánh 20000
    ['Bánh chưng'] = {AddItem, 1, 15179},
    ['Bánh dày'] = {AddItem, 1, 15180},
    -- Tinh hoạt lực 100000
    ['Tinh khí tán (1000)'] = {AddItem, 1, 349},
    ['Hoạt khí tán (1000)'] = {AddItem, 1, 352},
    -- Ngựa
    ['Hổ Cát Tường [7 ngày]'] = {AddHorse, 1, 7, 0},
    ['Hổ Cát Tường [15 ngày]'] = {AddHorse, 1, 15, 0}
}

function CayMayMan:ExchangeRemainGioQua(npc, player)
	local remainGioQua = Player:CountItemInBag(player, 15185)
	local addMoneySuccess = Player:AddMoney(player, remainGioQua * 20, 2, "TET2023_BACK")
	if addMoneySuccess then
		Player:RemoveItem(player, 15185, remainGioQua)
		local dialog = GUI:CreateNPCDialog()
		dialog:AddText("Bạn đã đổi giỏ quà thành công.")
		dialog:AddSelection(2, 'Cảm ơn')
		dialog:Show(npc, player)
	else
		local dialog = GUI:CreateNPCDialog()
		dialog:AddText("Đã có lỗi khi đổi giỏ quà.")
		dialog:AddSelection(2, 'Thoát')
		dialog:Show(npc, player)
	end
end

function CayMayMan:Open100Tet(npc, player)
    local received = {
        -- KNB Khóa 42830000
        ['KNB Khóa 30'] = 0, -- 55%
        ['KNB Khóa 50'] = 0, -- 40%
        ['KNB Khóa 100'] = 0, -- 5%
        -- Bạc 15000000
        ['Bạc 2000'] = 0, -- 50%
        ['Bạc 3000'] = 0, -- 45%
        ['Bạc 5000'] = 0, -- 5%
        -- Bạc Khóa 20000000
        ['Bạc Khóa 30000'] = 0, -- 55%
        ['Bạc Khóa 50000'] = 0, -- 40%
        ['Bạc Khóa 100000'] = 0, -- 5%
        -- Ngũ hành hồn thạch 5000000
        ['Ngũ hành hồn thạch 10'] = 0, -- 50%
        ['Ngũ hành hồn thạch 20'] = 0, -- 45%
        ['Ngũ hành hồn thạch 30'] = 0, -- 5%
        -- Huyền tinh 15000000
        ['Huyền tinh 3'] = 0, -- 80%
        ['Huyền tinh 4'] = 0, -- 15%
        ['Huyền tinh 5'] = 0, -- 4%
        ['Huyền tinh 6'] = 0, -- 1%
        -- Bánh 20000
        ['Bánh chưng'] = 0,
        ['Bánh dày'] = 0,
        -- Tinh hoạt lực 100000
        ['Tinh khí tán (1000)'] = 0,
        ['Hoạt khí tán (1000)'] = 0,
        -- Ngựa
        ['Ngựa 9x 7 ngày'] = 0,
        ['Ngựa 9x 15 ngày'] = 0
    }

    for i = 1, 100 do
        -- tạo pool xác suất
        local pool = 0
        for k, v in pairs(PROBABILITY_DEFINE) do
            pool = pool + v
        end

        -- random ra 1 điểm trong pool
        local rand = math.random(1, pool)

        -- duyệt cộng dồn xác suất của từng vật phẩm
        -- nếu xác suất cộng dồn lớn hơn điểm random -> điểm random nằm trong đoạn xác suất của vật phẩm
        -- thì trả về vật phẩm
        local accumulatedProbability = 0
        local reward = nil
        for k, v in pairs(PROBABILITY_DEFINE) do
            accumulatedProbability = accumulatedProbability + v
            if rand <= accumulatedProbability then
                reward = k
                break
            end
        end

        local item = RECEIVE_REWARD[reward]

        received[reward] = received[reward] + item[2]
    end

    local r = ''

    for k, v in pairs(received) do
        r = r .. v .. '\n'
    end

    System.WriteToConsole(r)

    local dialog = GUI.CreateNPCDialog()
    dialog:AddText(r)
    dialog:Show(npc, player)
end

-- sự kiện đổi quà tết 2023
-- đổi giỏ quà tết từ các vật phẩm mứt dừa, rượu vang, hạt điều, trà, giỏ quà
function CayMayMan:ExchangeTetGift(scene, npc, player, quantity)
    local dialog = GUI.CreateNPCDialog()

    local total_MutDua = Player.CountItemInBag(player, ITEMID_MUTDUA)
    local total_RuouVang = Player.CountItemInBag(player, ITEMID_RUOUVANG)
    local total_HatDieu = Player.CountItemInBag(player, ITEMID_HATDIEU)
    local total_Tra = Player.CountItemInBag(player, ITEMID_TRA)
    local total_GioQua = Player.CountItemInBag(player, ITEMID_GIO)

    if total_GioQua < quantity or total_MutDua < quantity or total_HatDieu < quantity or total_RuouVang < quantity or
        total_Tra < quantity then
        dialog:AddText("Bằng hữu không có đủ nguyên liệu để đổi " .. quantity .. " giỏ quà!")
        dialog:Show(npc, player)
        return
    end

    Player.RemoveItem(player, ITEMID_MUTDUA, quantity)
    Player.RemoveItem(player, ITEMID_RUOUVANG, quantity)
    Player.RemoveItem(player, ITEMID_HATDIEU, quantity)
    Player.RemoveItem(player, ITEMID_TRA, quantity)
    Player.RemoveItem(player, ITEMID_GIO, quantity)

    Player.AddItemLuaAndNotify(player, ITEMID_GIOQUATET, quantity, 0, 0, 0, "2023-03-01 00:00:00")

    dialog:AddText("Bằng hữu đã đổi " .. quantity .. " giỏ quà. Hãy kiểm tra trong túi đồ nhé!")

    dialog:AddSelection(700000, "Ta muốn đổi tiếp.")
    dialog:AddSelection(2, "Cảm ơn.")

    dialog:Show(npc, player)
end

function CayMayMan:ShowExchangeTetGiftOption(npc, player)
    local dialog = GUI.CreateNPCDialog()

    -- nếu hòm đồ đầy thì không cho dùng
    if not Player.HasFreeBagSpaces(player, 1) then
        dialog:AddText(
            "Cần để trống tối thiểu <color=green>1 ô trống</color> trong túi đồ để đổi quà Tết!")
        dialog:Show(npc, player)
        return
    end

    -- đếm số lượng nguyên liệu và tính số giỏ quà tối đa
    local total_MutDua = Player.CountItemInBag(player, ITEMID_MUTDUA)
    local total_RuouVang = Player.CountItemInBag(player, ITEMID_RUOUVANG)
    local total_HatDieu = Player.CountItemInBag(player, ITEMID_HATDIEU)
    local total_Tra = Player.CountItemInBag(player, ITEMID_TRA)
    local total_GioQua = Player.CountItemInBag(player, ITEMID_GIO)

    local maximum = total_MutDua
    if (maximum > total_RuouVang) then
        maximum = total_RuouVang
    end
    if (maximum > total_HatDieu) then
        maximum = total_HatDieu
    end
    if (maximum > total_Tra) then
        maximum = total_Tra
    end
    if (maximum > total_GioQua) then
        maximum = total_GioQua
    end

    if maximum == 0 then
        -- nếu không đổi được thì thông báo
        dialog:AddText(
            "Bằng hữu không có đủ nguyên liệu làm giỏ quà Tết! Các nguyên liệu bao gồm: mứt dừa, hạt điều, rượu vang, trà San tuyết cổ thụ, giỏ quà.")
    else
        -- nếu đổi được thì đưa ra các lựa chọn đổi
        dialog:AddText("Bằng hữu muốn đổi bao nhiêu giỏ quà Tết?")

        local options = {1, 5, 10, 20, 50, 100}
        for i = 1, 6 do
            if options[i] <= maximum then
                local quantity = options[i]
                dialog:AddSelection(700000 + quantity, "Đổi " .. quantity .. " giỏ quà")
            else
                break
            end
        end
    end

    dialog.Show(npc, player)
end

local LIXI_DAILY_KEY = 700002
local LIXI_EXP = 10000000

function CayMayMan:LiXiTet(npc, player)
    if not Global_IsInTet2023LiXiEvent() then
        return
    end

    if Player.GetValueOfDailyRecore(player, LIXI_DAILY_KEY) == -1 then
        Player.SetValueOfDailyRecore(player, LIXI_DAILY_KEY, 1)
        player.AddExp(LIXI_EXP)
        local dialog = GUI.CreateNPCDialog()
        dialog:AddText(
            'Bằng hữu đã được lì xì 10.000.000 EXP. Chúc bằng hữu năm mới nhiều may mắn.')
        dialog:AddSelection(2, 'Cảm ơn')
        dialog:Show(npc, player)
    else
        local dialog = GUI.CreateNPCDialog()
        dialog:AddText('Bằng hữu đã nhận lì xì ngày hôm nay rồi. Hãy quay lại sau nhé!')
        dialog:AddSelection(2, 'Cảm ơn')
        dialog:Show(npc, player)
    end
end

local LIST_PLAYER_EXCHANGE_GIOQUA = {
	[201779] = true,
	[354426] = true,
	[246372] = true,
	[216570] = true,
	[312535] = true,
	[199852106] = true,
	[308118] = true,
	[199851185] = true,
	[373386] = true,
	[199831802] = true,
	[277896] = true,
	[252925] = true,
	[199862381] = true,
	[199864494] = true,
	[199858945] = true,
	[246381] = true,
	[199852271] = true,
	[199839612] = true
}

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function CayMayMan:OnSelection(scene, npc, player, selectionID, otherParams)
    -- ************************** --
    local dialog = GUI.CreateNPCDialog()

    if selectionID == 699998 then
        CayMayMan:LiXiTet(npc, player)
    end

	if selectionID == 699997 then
		CayMayMan:ExchangeRemainGioQua(npc, player)
	end

    if selectionID == 699999 then
        CayMayMan:Open100Tet(npc, player)
        return
    end

    if selectionID == 700000 then
        CayMayMan:ShowExchangeTetGiftOption(npc, player)
    end

    if selectionID > 700000 then
        CayMayMan:ExchangeTetGift(scene, npc, player, selectionID - 700000)
    end

    if selectionID == 2 then
        GUI.CloseDialog(player)
    end

    -- if selectionID == 7 then
    --     for i = 1, 1000, 1 do
    --         player:StartSeashellCircle()
    --     end
    -- end

end

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function CayMayMan:OnOpen(scene, npc, player, otherParams)

    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Cung chúc tân xuân Quý Mão 2023.")

	if (LIST_PLAYER_EXCHANGE_GIOQUA[player:GetID()]) then
		dialog:AddSelection(699997, "[Hỗ trợ] Đổi Giỏ quà sang KNB")
	end

    if Global_IsInTet2023LiXiEvent() then
        dialog:AddSelection(699998, "Nhận lì xì Tết")
    end
    if Global_IsInTet2023Event() then
        dialog:AddSelection(700000, "Đổi giỏ quà Tết")
        -- dialog:AddSelection(699999, "[TEST] Mở 100 giỏ quà Tết")
    end
    dialog:AddSelection(2, "Kết thúc đối thoại")
    dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function CayMayMan:OnItemSelected(scene, npc, player, itemID, otherParams)

end
