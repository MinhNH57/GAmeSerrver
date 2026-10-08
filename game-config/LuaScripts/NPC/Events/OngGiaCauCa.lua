-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng
local OngGiaCauCa = Scripts[000221]

local EVENT = {
    [202310] = {
        Name = '[Câu cá Mùa thu]',
        EventTimeFunc = Global_IsIn202310Event
    }
}

local NORMAL_FISH = { 20219, 20220, 20223 }
local FISH_RATE = {
    [20219] = 30,
    [20220] = 65,
    [20223] = 5,
    [20225] = 32,
    [20226] = 12,
    [20227] = 45,
    [20228] = 3,
    [20229] = 8,
}
local SPECIAL_FISH = { 20225, 20226, 20227, 20228, 20229 }

local COMPOSE_FORMULA = {
    [10026] = {
        EventID = 202310,
		ItemIDs = NORMAL_FISH,
		Name = "",
        Extra = "(dùng Bạc khóa, không Thính)",
		Require = {
			{ ItemID = 20211, Name = "Phao Câu Cá", Number = 1},
			{ ItemID = 20212, Name = "Dây Câu", Number = 1},
			{ ItemID = 20213, Name = "Mồi Câu", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
		},
        Expiry = Global_ItemExpiry202310,
        Binding = 1
	},
    [10027] = {
        EventID = 202310,
		ItemIDs = NORMAL_FISH,
		Name = "",
        Extra = "(dùng Bạc, không Thính)",
		Require = {
			{ ItemID = 20211, Name = "Phao Câu Cá", Number = 1},
			{ ItemID = 20212, Name = "Dây Câu", Number = 1},
			{ ItemID = 20213, Name = "Mồi Câu", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
		},
        Expiry = Global_ItemExpiry202310,
        Binding = 0
	},
    [10028] = {
        EventID = 202310,
		ItemIDs = SPECIAL_FISH,
		Name = "",
        Extra = "(dùng Bạc khóa, thêm Thính)",
		Require = {
            { ItemID = 20211, Name = "Phao Câu Cá", Number = 1},
			{ ItemID = 20212, Name = "Dây Câu", Number = 1},
			{ ItemID = 20213, Name = "Mồi Câu", Number = 1},
			{ ItemID = 20214, Name = "Thính", Number = 1},
            { ItemID = 0, Name = "Bạc khóa", Number = 10000}
		},
        Expiry = Global_ItemExpiry202310,
        Binding = 1
	},
    [10029] = {
        EventID = 202310,
		ItemIDs = SPECIAL_FISH,
		Name = "",
        Extra = "(dùng Bạc, thêm Thính)",
		Require = {
            { ItemID = 20211, Name = "Phao Câu Cá", Number = 1},
			{ ItemID = 20212, Name = "Dây Câu", Number = 1},
			{ ItemID = 20213, Name = "Mồi Câu", Number = 1},
			{ ItemID = 20214, Name = "Thính", Number = 1},
            { ItemID = 1, Name = "Bạc", Number = 10000}
		},
        Expiry = Global_ItemExpiry202310,
        Binding = 0
	},
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function OngGiaCauCa:OnOpen(scene, npc, player, otherParams)

	local dialog = GUI.CreateNPCDialog()
	dialog:AddText("Câu cá là 1 thú vui, câu cá là 1 tinh thần. Bây giờ là mùa nước xanh cá béo, ngươi có thể đem dụng cụ đến đây, ta sẽ câu cá giúp ngươi.")
    dialog:AddText("Cần câu ta đã có, chỉ cần thêm dây câu, phao, mồi câu. Nếu có thêm thính, thì sẽ câu được cá ngon hơn.")
    dialog:AddText("Lệ phí mỗi lần câu là 1 vạn bạc.")
    dialog:AddText("Cá câu được có thể đem đến gặp Tần Oa để giám định. Hắn sẽ báo đáp hậu hĩnh cho ngươi.")

    for k, v in pairs(COMPOSE_FORMULA) do
        if EVENT[v.EventID] ~= nil then
            local event = EVENT[v.EventID]
            if event:EventTimeFunc() then
                dialog:AddSelection(k, "<color=#f38ba8>" .. event.Name .. "</color>" .. " " .. v.Name .. " " .. "<color=#bac2de>" .. v.Extra .. "</color>")
            end
        end
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
function OngGiaCauCa:OnSelection(scene, npc, player, selectionID, otherParams)

    System:WriteToConsole(selectionID)
	local dialog = GUI.CreateNPCDialog()

	if COMPOSE_FORMULA[selectionID] ~= nil then
        OngGiaCauCa:ShowComposeNumberOption(npc, player, selectionID)
        return
	end

    if selectionID == 700000 then
        OngGiaCauCa:ShowComposeNumberOption(npc, player, selectedItemID)
        return
    end

    if selectionID > 700000 and selectionID < 800000 then
        OngGiaCauCa:ComposeItem(npc, player, selectedItemID, selectionID - 700000)
        return
    end

    if selectionID == 9 then
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
function OngGiaCauCa:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end

function OngGiaCauCa:shuffle(t)
    local tbl = {}
    for i = 1, #t do
      tbl[i] = t[i]
    end
    for i = #tbl, 2, -1 do
      local j = math.random(i)
      tbl[i], tbl[j] = tbl[j], tbl[i]
    end
    return tbl
  end

function OngGiaCauCa:ComposeItem(npc, player, formulaID, number)
    System.WriteToConsole("Đang ghép " .. formulaID)
    local dialog = GUI:CreateNPCDialog()

    local formula = COMPOSE_FORMULA[formulaID]
    System.WriteToConsole("Đang ghép " .. number .. " " .. formula.Name)

    for k, v in pairs(formula.Require) do
        if v.ItemID >= 100 then
            -- là item
            local needed = v.Number * number
            local in_bag = Player:CountItemInBag(player, v.ItemID)
            if in_bag < needed then
                dialog:AddText("Ngươi không có đủ dụng cụ để câu cá!")
                dialog:Show(npc, player)
                return
            end 
        else
            -- là tiền
            local needed = v.Number * number
            local in_bag = Player:CheckMoney(player, v.ItemID)
            if in_bag < needed then
                dialog:AddText("Ngươi cần " .. needed .. " " .. v.Name .. " để câu cá " .. number .. " lượt!")
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
            Player:SubMoney(player, needed, v.ItemID, "COMPOSE_" .. k)
        end
    end

    local message = ''

    -- tính toán số lượng cá nhận được
    local fishes = OngGiaCauCa:shuffle(formula.ItemIDs)
    local total_fishes = #fishes
    local remain = number
    local remain_percent = 100
    local result = {}

    for k, v in ipairs(NORMAL_FISH) do
        result[v] = 0
    end

    for k, v in ipairs(SPECIAL_FISH) do
        result[v] = 0
    end

    for k, v in ipairs(fishes) do
        if remain == 0 then
            break
        end

        local received = 0
        if k == total_fishes then
            received = remain
        else 
            local random_result = remain * (FISH_RATE[v] + math.random(-3, 4)) * 1.0 / remain_percent
            System:WriteToConsole('' .. v .. ' ' .. random_result)
            received = math.floor(random_result + 0.5)
        end

        if received < 0 then
            received = 0
        end
        if received > remain then
            received = remain
        end

        result[v] = received

        if received > 0 then
            message = message .. string.format('- %d [fish#%d]\n', received, v)
            Player:AddItemLua(player, v, received, 0, formula.Binding, 0, formula.Expiry)
        end
        remain = remain - received
        remain_percent = remain_percent - FISH_RATE[v]
    end

    local interactive_popup = "Messages/UIFishingInteractive"
    if formulaID == 10028 or formulaID == 10029 then
        interactive_popup = "Messages/UIFishingInteractiveSpecial"
    end

    -- GUI:ShowNotification(player, message)
    GUI:OpenUIWithStringParams(player, interactive_popup, 
        "Fish20219", result[20219],
        "Fish20220", result[20220],
        "Fish20223", result[20223],
        "Fish20225", result[20225],
        "Fish20226", result[20226],
        "Fish20227", result[20227],
        "Fish20228", result[20228],
        "Fish20229", result[20229])
    GUI:CloseDialog(player)

    -- dialog:AddText("Bằng hữu đã đổi " .. number .. " " .. formula.Name .. ". Hãy kiểm tra trong túi đồ nhé!")
    -- dialog:AddSelection(700000, "Ta muốn đổi tiếp.")
    -- dialog:AddSelection(9, "Cảm ơn")
    -- dialog:Show(npc, player)
end

function OngGiaCauCa:ShowComposeNumberOption(npc, player, itemID)
    selectedItemID = itemID

    local maximum = 999999
    local formula = COMPOSE_FORMULA[itemID]

    local bagSpaceNeeded = #formula.ItemIDs
    if not Player.HasFreeBagSpaces(player,bagSpaceNeeded) then
        local dialog = GUI:CreateNPCDialog()
        dialog:AddText(
            "Cần để trống tối thiểu <color=green>" .. bagSpaceNeeded  .. " ô trống</color> trong túi đồ để đựng cá!")
        dialog:Show(npc, player)
        return
    end

    for k, v in pairs(formula.Require) do
        if v.ItemID >= 100 then
            local in_bag = Player.CountItemInBag(player, v.ItemID)
            local can_exchange = math.floor(in_bag / v.Number)
            maximum = math.min(maximum, can_exchange)
        end
    end
    System.WriteToConsole("max " .. maximum)

    local dialog = GUI:CreateNPCDialog()
    local description = "Mỗi lượt câu cá cần:"
        local add_comma = false
        for k, v in pairs(formula.Require) do
            -- if add_comma then
            --     description = "\n    - " .. description .. ", "
            -- end
            -- add_comma = true
            description = description .. "\n    - " .. v.Number .. " " .. v.Name
        end

    if maximum == 0 then
        dialog:AddText(description .. "\n\nNgươi không có đủ nguyên liệu để câu cá.")
        dialog:AddSelection(9, "Ta sẽ quay lại sau.")
        dialog:Show(npc, player)
        return
    end
    dialog:AddText(description .. "\n\nNgươi muốn câu bao nhiêu lần?")

    local options = {1, 5, 10, 20, 50, 100, 500}
    for i = 1, 7 do
        if options[i] <= maximum then
            local quantity = options[i]
            dialog:AddSelection(700000 + quantity, "Câu " .. quantity .. " lần")
        else
            break
        end
    end

    dialog:AddSelection(9, "Để ta suy nghĩ thêm.")
    dialog:Show(npc, player)
end