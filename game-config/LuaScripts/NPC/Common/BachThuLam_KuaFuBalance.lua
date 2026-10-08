-- Bach Thu Lam rieng cho Lien server can bang.
local BachThuLam_KuaFuBalance = Scripts[400040]

local KUA_FU_CITY_MAP_ID = 1642
local SELECT_FACTION_BASE = 10000
local SELECT_ROUTE_BASE = 20000
local SELECTION_SELECT_FACTION = 1
local SELECTION_RECEIVE_EQUIPMENT = 2
local SELECTION_REISSUE = 3
local SELECTION_RECEIVE_LEVEL_129 = 4
local SELECTION_RESET_POTENTIAL = 5
local SELECTION_RESET_SKILLS = 6
local SELECTION_DELETE_TEMP_EQUIPMENT = 7
local SELECTION_CONFIRM_DELETE_TEMP_EQUIPMENT = 8
local SELECTION_LEAVE = 9
local SELECTION_RECEIVE_BASIC_BOOKS = 10
local SELECTION_RECEIVE_MIDDLE_BOOKS = 11
local SELECTION_RECEIVE_HIGH_BOOKS = 12
local SELECTION_MAX_BOOK_EXP = 13
local SELECTION_RECEIVE_KUAFU_BIND_MONEY = 14
local SELECTION_RECEIVE_DIVINE_PET = 15
local SELECTION_CLOSE = 999

local KUAFU_BIND_MONEY_AMOUNT = 10000000
local KUAFU_BIND_MONEY_TYPE = 6
local DIVINE_PET_ITEM_ID = 3508

local Factions = {
    [1] = { Name = "Thiếu Lâm", Routes = { [1] = "Đao Thiếu", [2] = "Côn Thiếu" } },
    [2] = { Name = "Thiên Vương", Routes = { [1] = "Thương Thiên", [2] = "Chùy Thiên" } },
    [3] = { Name = "Đường Môn", Routes = { [1] = "Hãm Tĩnh", [2] = "Tụ Tiễn" } },
    [4] = { Name = "Ngũ Độc", Routes = { [1] = "Đao Độc", [2] = "Chưởng Độc" } },
    [5] = { Name = "Nga My", Routes = { [1] = "Chưởng Nga", [2] = "Phụ Trợ" } },
    [6] = { Name = "Thúy Yên", Routes = { [1] = "Kiếm Thúy", [2] = "Đao Thúy" } },
    [7] = { Name = "Cái Bang", Routes = { [1] = "Chưởng Cái", [2] = "Côn Cái" } },
    [8] = { Name = "Thiên Nhẫn", Routes = { [1] = "Chiến Nhẫn", [2] = "Ma Nhẫn" } },
    [9] = { Name = "Võ Đang", Routes = { [1] = "Khí Võ", [2] = "Kiếm Võ" } },
    [10] = { Name = "Côn Lôn", Routes = { [1] = "Đao Côn", [2] = "Kiếm Côn" } },
    [11] = { Name = "Minh Giáo", Routes = { [1] = "Chùy Minh", [2] = "Kiếm Minh" } },
    [12] = { Name = "Đoàn Thị", Routes = { [1] = "Chỉ Đoàn", [2] = "Khí Đoàn" } }
}

local FactionBooks = {
    [1] = { Basic = { 3232, 3233 }, High = { 3280, 3281 }, Middle = { 3256, 3257 } },
    [2] = { Basic = { 3234, 3235 }, High = { 3282, 3283 }, Middle = { 3258, 3259 } },
    [3] = { Basic = { 3236, 3237 }, High = { 3284, 3285 }, Middle = { 3260, 3261 } },
    [4] = { Basic = { 3238, 3239 }, High = { 3286, 3287 }, Middle = { 3262, 3263 } },
    [5] = { Basic = { 3240, 3241 }, High = { 3288, 3289 }, Middle = { 3264, 3265 } },
    [6] = { Basic = { 3242, 3243 }, High = { 3290, 3291 }, Middle = { 3266, 3267 } },
    [7] = { Basic = { 3244, 3245 }, High = { 3292, 3293 }, Middle = { 3268, 3269 } },
    [8] = { Basic = { 3246, 3247 }, High = { 3294, 3295 }, Middle = { 3270, 3271 } },
    [9] = { Basic = { 3248, 3249 }, High = { 3296, 3297 }, Middle = { 3272, 3273 } },
    [10] = { Basic = { 3250, 3251 }, High = { 3298, 3299 }, Middle = { 3274, 3275 } },
    [11] = { Basic = { 3252, 3253 }, High = { 3300, 3301 }, Middle = { 3276, 3277 } },
    [12] = { Basic = { 3254, 3255 }, High = { 3302, 3303 }, Middle = { 3278, 3279 } }
}

function BachThuLam_KuaFuBalance:ShowDialog(npc, player, text)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText(text)
    dialog:AddSelection(SELECTION_CLOSE, "Kết thúc đối thoại")
    dialog:Show(npc, player)
end

function BachThuLam_KuaFuBalance:IsValidMap(player)
    return player:GetMapCode() == KUA_FU_CITY_MAP_ID
end

function BachThuLam_KuaFuBalance:HasSelectedFaction(player)
    return Player.KuaFuBalanceHasSelectedFaction(player)
end

function BachThuLam_KuaFuBalance:ShowMainDialog(npc, player)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Liên server cân bằng dùng phái/nhánh tạm riêng. Hãy chọn phái/nhánh trước, sau đó nhận set trang bị phù hợp.")
    dialog:AddSelection(SELECTION_SELECT_FACTION, "Chọn phái/nhánh tạm")
    dialog:AddSelection(SELECTION_RECEIVE_EQUIPMENT, "Nhận set đồ theo phái hiện tại")
    dialog:AddSelection(SELECTION_REISSUE, "Cấp lại set đồ hiện tại")
    dialog:AddSelection(SELECTION_RECEIVE_BASIC_BOOKS, "Nhận Mật Tịch (sơ) Theo Phái")
    dialog:AddSelection(SELECTION_RECEIVE_MIDDLE_BOOKS, "Nhận Mật Tịch (trung) Theo Phái")
    dialog:AddSelection(SELECTION_RECEIVE_HIGH_BOOKS, "Nhận Mật Tịch (cao) Theo Phái")
    dialog:AddSelection(SELECTION_MAX_BOOK_EXP, "Nhận Max EXP mật tịch")
    dialog:AddSelection(SELECTION_RECEIVE_KUAFU_BIND_MONEY, "Nhận 1000 vạn bạc khóa LSV")
    dialog:AddSelection(SELECTION_RECEIVE_DIVINE_PET, "Nhận Thần Thú")
    dialog:AddSelection(SELECTION_RECEIVE_LEVEL_129, "Nhận cấp 129 tạm thời")
    dialog:AddSelection(SELECTION_RESET_POTENTIAL, "Tẩy điểm tiềm năng")
    dialog:AddSelection(SELECTION_RESET_SKILLS, "Tẩy điểm kỹ năng")
    dialog:AddSelection(SELECTION_DELETE_TEMP_EQUIPMENT, "Xóa toàn bộ đồ tạm")
    dialog:AddSelection(SELECTION_LEAVE, "Rời hoạt động")
    dialog:AddSelection(SELECTION_CLOSE, "Ta sẽ quay lại sau")
    dialog:Show(npc, player)
end

function BachThuLam_KuaFuBalance:ShowFactionDialog(npc, player)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Chọn môn phái tạm. Trang bị sẽ được nhận riêng ở menu chính.")

    for factionID = 1, 12 do
        local faction = Factions[factionID]
        if faction ~= nil then
            dialog:AddSelection(SELECT_FACTION_BASE + factionID, faction.Name)
        end
    end

    dialog:AddSelection(SELECTION_CLOSE, "Ta sẽ quay lại sau")
    dialog:Show(npc, player)
end

function BachThuLam_KuaFuBalance:ShowRouteDialog(npc, player, factionID)
    local faction = Factions[factionID]
    if faction == nil then
        self:ShowDialog(npc, player, "Môn phái không hợp lệ.")
        return
    end

    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Chọn nhánh của " .. faction.Name .. ". Sau khi chọn xong, quay lại menu chính để nhận trang bị.")

    for routeID = 1, 2 do
        local routeName = faction.Routes[routeID]
        if routeName ~= nil then
            dialog:AddSelection(SELECT_ROUTE_BASE + factionID * 10 + routeID, routeName)
        end
    end

    dialog:AddSelection(SELECTION_SELECT_FACTION, "Chọn lại môn phái")
    dialog:AddSelection(SELECTION_CLOSE, "Ta sẽ quay lại sau")
    dialog:Show(npc, player)
end

function BachThuLam_KuaFuBalance:SelectTemporaryFaction(npc, player, factionID, routeID)
    local faction = Factions[factionID]
    if faction == nil or faction.Routes[routeID] == nil then
        self:ShowDialog(npc, player, "Lựa chọn phái/nhánh tạm không hợp lệ.")
        return
    end

    if Player.KuaFuBalanceSelectFaction(player, factionID, routeID) then
        self:ShowDialog(npc, player, "Đã chọn phái/nhánh tạm: <color=yellow>" .. faction.Name .. " - " .. faction.Routes[routeID] .. "</color>. Hãy nhận set trang bị ở menu chính.")
    else
        self:ShowMainDialog(npc, player)
    end
end

function BachThuLam_KuaFuBalance:ReceiveTemporaryEquipment(npc, player)
    if Player.KuaFuBalanceReceiveEquipment(player) then
        self:ShowDialog(npc, player, "Đã nhận set đồ tạm thời.")
    else
        self:ShowMainDialog(npc, player)
    end
end

function BachThuLam_KuaFuBalance:ReceiveTemporaryLevel129(npc, player)
    if Player.KuaFuBalanceSetTemporaryLevel(player, 129) then
        self:ShowDialog(npc, player, "Đã nhận cấp 129 tạm thời.")
    else
        self:ShowMainDialog(npc, player)
    end
end

function BachThuLam_KuaFuBalance:ResetTemporaryPotential(npc, player)
    if Player.KuaFuBalanceResetPotential(player) then
        self:ShowDialog(npc, player, "Đã tẩy điểm tiềm năng tạm thời.")
    else
        self:ShowMainDialog(npc, player)
    end
end

function BachThuLam_KuaFuBalance:ResetTemporarySkills(npc, player)
    if Player.KuaFuBalanceResetSkills(player) then
        self:ShowDialog(npc, player, "Đã tẩy điểm kỹ năng tạm thời.")
    else
        self:ShowMainDialog(npc, player)
    end
end

function BachThuLam_KuaFuBalance:ShowDeleteTemporaryEquipmentConfirm(npc, player)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Thao tác này chỉ xóa toàn bộ trang bị tạm đã nhận trong Liên server cân bằng, không xóa trang bị gốc. Ngươi có chắc muốn xóa không?")
    dialog:AddSelection(SELECTION_CONFIRM_DELETE_TEMP_EQUIPMENT, "Đồng ý xóa đồ tạm")
    dialog:AddSelection(SELECTION_CLOSE, "Hủy")
    dialog:Show(npc, player)
end

function BachThuLam_KuaFuBalance:DeleteTemporaryEquipment(npc, player)
    if Player.KuaFuBalanceDeleteTemporaryEquipment(player) then
        self:ShowDialog(npc, player, "Đã xóa toàn bộ đồ tạm.")
    else
        self:ShowMainDialog(npc, player)
    end
end

function BachThuLam_KuaFuBalance:ReceiveFactionBooks(npc, player, bookType, bookName)
    -- guard phái tạm đã check ở OnSelection, GetFactionID ở đây lấy phái hiện tại để map vào FactionBooks
    local factionID = player:GetFactionID()

    local factionBooks = FactionBooks[factionID]
    if factionBooks == nil or factionBooks[bookType] == nil then
        self:ShowDialog(npc, player, "Chưa có cấu hình mật tịch cho phái hiện tại.")
        return
    end

    for _, itemID in ipairs(factionBooks[bookType]) do
        if not Player.KuaFuBalanceAddTemporaryItem(player, itemID, 1, Player.GetSeries(player), 1) then
            self:ShowDialog(npc, player, "Cấp " .. bookName .. " thất bại, vui lòng kiểm tra túi đồ rồi thử lại.")
            return
        end
    end

    self:ShowDialog(npc, player, "Đã nhận " .. bookName .. " theo phái hiện tại.")
end

function BachThuLam_KuaFuBalance:ReceiveMaxBookExp(npc, player)
    if Player.KuaFuBalanceSetTemporaryBookLevelAndExp(player, 100, 0) then
        self:ShowDialog(npc, player, "Đã nhận Max EXP mật tịch đang mặc.")
    else
        self:ShowDialog(npc, player, "Cần mặc mật tịch trước khi nhận Max EXP.")
    end
end

function BachThuLam_KuaFuBalance:ReceiveKuaFuBindMoney(npc, player)
    if Player.AddMoney(player, KUAFU_BIND_MONEY_AMOUNT, KUAFU_BIND_MONEY_TYPE, "KuaFuBalance") then
        self:ShowDialog(npc, player, "Đã nhận 1000 vạn bạc khóa LSV.")
    else
        self:ShowDialog(npc, player, "Nhận bạc khóa LSV thất bại, vui lòng thử lại.")
    end
end

function BachThuLam_KuaFuBalance:ReceiveDivinePet(npc, player)
    if Player.KuaFuBalanceAddTemporaryItem(player, DIVINE_PET_ITEM_ID, 1, -1, 1) then
        self:ShowDialog(npc, player, "Đã nhận Thần Thú.")
    else
        self:ShowDialog(npc, player, "Nhận Thần Thú thất bại, vui lòng kiểm tra túi đồ rồi thử lại.")
    end
end

function BachThuLam_KuaFuBalance:OnOpen(scene, npc, player, otherParams)
    if not self:IsValidMap(player) then
        self:ShowDialog(npc, player, "NPC này chỉ hỗ trợ trong Hoàng Thành LSV Cân Bằng.")
        return
    end

    self:ShowMainDialog(npc, player)
end

function BachThuLam_KuaFuBalance:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == SELECTION_CLOSE then
        GUI.CloseDialog(player)
        return
    end

    if not self:IsValidMap(player) then
        self:ShowDialog(npc, player, "NPC này chỉ hỗ trợ trong Hoàng Thành LSV Cân Bằng.")
        return
    end

    if selectionID == SELECTION_SELECT_FACTION then
        self:ShowFactionDialog(npc, player)
        return
    end

    -- Các thao tác dưới đây yêu cầu đã chọn phái/nhánh tạm
    local needFactionSelections = {
        [SELECTION_RECEIVE_EQUIPMENT] = true,
        [SELECTION_REISSUE] = true,
        [SELECTION_RECEIVE_BASIC_BOOKS] = true,
        [SELECTION_RECEIVE_MIDDLE_BOOKS] = true,
        [SELECTION_RECEIVE_HIGH_BOOKS] = true,
        [SELECTION_MAX_BOOK_EXP] = true,
        [SELECTION_RECEIVE_KUAFU_BIND_MONEY] = true,
        [SELECTION_RECEIVE_DIVINE_PET] = true,
        [SELECTION_RECEIVE_LEVEL_129] = true,
        [SELECTION_RESET_POTENTIAL] = true,
        [SELECTION_RESET_SKILLS] = true,
        [SELECTION_DELETE_TEMP_EQUIPMENT] = true,
        [SELECTION_CONFIRM_DELETE_TEMP_EQUIPMENT] = true,
    }
    if needFactionSelections[selectionID] and not self:HasSelectedFaction(player) then
        self:ShowDialog(npc, player, "Ngươi chưa chọn phái/nhánh tạm. Hãy chọn \"Chọn phái/nhánh tạm\" trước khi thực hiện thao tác này.")
        return
    end

    if selectionID == SELECTION_RECEIVE_EQUIPMENT then
        self:ReceiveTemporaryEquipment(npc, player)
        return
    end

    if selectionID == SELECTION_REISSUE then
        self:ReceiveTemporaryEquipment(npc, player)
        return
    end

    if selectionID == SELECTION_RECEIVE_BASIC_BOOKS then
        self:ReceiveFactionBooks(npc, player, "Basic", "Mật Tịch (sơ)")
        return
    end

    if selectionID == SELECTION_RECEIVE_MIDDLE_BOOKS then
        self:ReceiveFactionBooks(npc, player, "Middle", "Mật Tịch (trung)")
        return
    end

    if selectionID == SELECTION_RECEIVE_HIGH_BOOKS then
        self:ReceiveFactionBooks(npc, player, "High", "Mật Tịch (cao)")
        return
    end

    if selectionID == SELECTION_MAX_BOOK_EXP then
        self:ReceiveMaxBookExp(npc, player)
        return
    end

    if selectionID == SELECTION_RECEIVE_KUAFU_BIND_MONEY then
        self:ReceiveKuaFuBindMoney(npc, player)
        return
    end

    if selectionID == SELECTION_RECEIVE_DIVINE_PET then
        self:ReceiveDivinePet(npc, player)
        return
    end

    if selectionID == SELECTION_RECEIVE_LEVEL_129 then
        self:ReceiveTemporaryLevel129(npc, player)
        return
    end

    if selectionID == SELECTION_RESET_POTENTIAL then
        self:ResetTemporaryPotential(npc, player)
        return
    end

    if selectionID == SELECTION_RESET_SKILLS then
        self:ResetTemporarySkills(npc, player)
        return
    end

    if selectionID == SELECTION_DELETE_TEMP_EQUIPMENT then
        self:ShowDeleteTemporaryEquipmentConfirm(npc, player)
        return
    end

    if selectionID == SELECTION_CONFIRM_DELETE_TEMP_EQUIPMENT then
        self:DeleteTemporaryEquipment(npc, player)
        return
    end

    if selectionID == SELECTION_LEAVE then
        Player.KuaFuBalanceLeave(player)
        return
    end

    if selectionID > SELECT_FACTION_BASE and selectionID < SELECT_ROUTE_BASE then
        local factionID = selectionID - SELECT_FACTION_BASE
        self:ShowRouteDialog(npc, player, factionID)
        return
    end

    if selectionID > SELECT_ROUTE_BASE then
        local value = selectionID - SELECT_ROUTE_BASE
        local factionID = math.floor(value / 10)
        local routeID = value - factionID * 10
        self:SelectTemporaryFaction(npc, player, factionID, routeID)
        return
    end

    self:ShowMainDialog(npc, player)
end

function BachThuLam_KuaFuBalance:OnItemSelected(scene, npc, player, selectedItemInfo, otherParams)
end
