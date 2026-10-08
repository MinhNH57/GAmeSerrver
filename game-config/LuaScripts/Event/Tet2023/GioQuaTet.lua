-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng

local QUA_TET_RECORD_KEY = 700001

local QUA_TET_EXP = 100000

local GioQuaTet = Scripts[QUA_TET_RECORD_KEY]

-- các hàm nhận thưởng
local function AddMoney(player, money, type)
    return Player.AddMoney(player, money, type, "TET2023")
end

local function AddItem(player, ID, Number)
    return Player.AddItemLuaAndNotify(player, ID, Number, 0, 0)
end

local function AddHorse(player, duration, other)
    local horseId = 15187
    return Player.AddItemLuaAndNotify(player, horseId, 1, 0, 1, 0, duration * 24 * 60)
end

local USAGE_LIMIT = 2500
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
    ['Hổ Cát Tường [15 ngày]'] = 15000,
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
    ['Ngũ hành hồn thạch 10'] = {AddItem, 506, 10}, -- 50%
    ['Ngũ hành hồn thạch 20'] = {AddItem, 506, 20}, -- 45%
    ['Ngũ hành hồn thạch 30'] = {AddItem, 506, 30}, -- 5%
    -- Huyền tinh 15000000
    ['Huyền tinh 3'] = {AddItem, 185, 1}, -- 80%
    ['Huyền tinh 4'] = {AddItem, 186, 1}, -- 15%
    ['Huyền tinh 5'] = {AddItem, 187, 1}, -- 4%
    ['Huyền tinh 6'] = {AddItem, 188, 1}, -- 1%
    -- Bánh 20000
    ['Bánh chưng'] = {AddItem, 15179, 1},
    ['Bánh dày'] = {AddItem, 15180, 1},
    -- Tinh hoạt lực 100000
    ['Tinh khí tán (1000)'] = {AddItem, 349, 1},
    ['Hoạt khí tán (1000)'] = {AddItem, 352, 1},
    -- Ngựa
    ['Hổ Cát Tường [7 ngày]'] = {AddHorse, 7, 0},
    ['Hổ Cát Tường [15 ngày]'] = {AddHorse, 15, 0}
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function GioQuaTet:OnPreCheckCondition(scene, item, player, otherParams)
    if item:IsExpired() then
        player.AddNotification("Vật phẩm đã hết hạn sử dụng!")
        return false
    end

    local totalTimeUse = Player.GetValueForeverRecore(player, QUA_TET_RECORD_KEY)
    if totalTimeUse == -1 then -- chưa có record thì coi là 0
        totalTimeUse = 0
    end

    -- nếu số lượt dùng đạt tối đa thì thôi không cho dùng
    if totalTimeUse >= USAGE_LIMIT then
        player.AddNotification("Bạn đã hết lượt đổi quà Tết!")
        return false
    end

    -- nếu hòm đồ đầy thì không cho dùng
    if not Player.HasFreeBagSpaces(player, 1) then
        player.AddNotification("Cần để trống tối thiểu <color=green>1 ô trống</color> trong túi đồ để nhận quà Tết!")
        return false
    end

    return true
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function GioQuaTet:OnUse(scene, item, player, otherParams)
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

    func = RECEIVE_REWARD[reward]

    if func[1](player, func[2], func[3]) then
		-- mỗi lần mở quà là được EXP
		player.AddExp(QUA_TET_EXP)
        player.AddNotification("Mở giỏ quà Tết nhận được " .. reward)
        Player.RemoveItem(player, item:GetID())
        local totalTimeUse = Player.GetValueForeverRecore(player, QUA_TET_RECORD_KEY)
		if totalTimeUse == -1 then -- chưa có record thì coi là 0
			totalTimeUse = 0
		end
        Player.SetValueOfForeverRecore(player, QUA_TET_RECORD_KEY, totalTimeUse + 1)
        player.AddNotification("Bạn đã dùng " .. (totalTimeUse + 1) .. "/" .. USAGE_LIMIT .. " lượt đổi quà")
        System:WriteLog(103, "" .. player:GetName() .. " mở Giỏ quà tết nhận được " .. reward)

        if (reward == 'Hổ Cát Tường [7 ngày]' 
            or reward == 'Hổ Cát Tường [15 ngày]' 
            or reward == 'Huyền tinh 6' 
            or reward == 'Bánh chưng' 
            or reward == 'Bánh dày' 
            or reward == 'Tinh khí tán (1000)' 
            or reward == 'Hoạt khí tán (1000)') then
            System:NotifyAll('Chúc mừng ' .. player:GetName() .. ' mở Giỏ quà tết nhận được ' .. reward .. '!')
        end
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function GioQuaTet:OnSelection(scene, item, player, selectionID, otherParams)

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function GioQuaTet:OnItemSelected(scene, item, player, itemID)

end
