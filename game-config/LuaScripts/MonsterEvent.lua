-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000000' bên dưới thành ID tương ứng
local MonsterEvent = Scripts[888888]

local MONTHLY_EVENT_CONFIG

function MonsterEvent:OnMonthlyEvent(scene, monster, killer)
    local RATE_UP = math.max(MonsterEvent:GetServerDropRate(), System:GetEventDropRate())
    local POOL_SIZE = 100000
    local ITEMs = MONTHLY_EVENT_CONFIG.MaterialRate -- ID của vật phẩm Túi nguyên liệu
    local LEVEL_LIMIT = MONTHLY_EVENT_CONFIG.MonsterLevelLimit

    -- quái dưới level 50 không rơi vật phẩm
    if monster:GetLevel() < LEVEL_LIMIT then return end

    local mapid = scene:GetID()

    if mapid > 137 and mapid ~= 343 then
        -- không phải map dã ngoại
        return
    end

    local main_random = math.random(1, POOL_SIZE)
    if main_random <= POOL_SIZE / 100 * RATE_UP then
        -- chắc chắn drop vật phẩm
        local dropped = false
        local accumulate = 0
        local random = math.random(1, POOL_SIZE)

        for item_id, rate in pairs(ITEMs) do
            if not dropped then
                if random <= POOL_SIZE / 100 * rate + accumulate then
                    dropped = true
                    local dropBuilder = EventManager:CreateDropToMapBuilder(scene)
                    dropBuilder.SetOwner(killer)
                    dropBuilder.AddUnlockedItem(tonumber(item_id))
                    dropBuilder.SetFromMonster(monster)
                    dropBuilder.Build()
                end    
                accumulate = accumulate + POOL_SIZE / 100 * rate
            end
        end
    end
end

function MonsterEvent:OnSpecialDrop(scene, monster, killer)
    local item_id = MONTHLY_EVENT_CONFIG.TokenBindItem
    local mapid = scene:GetID()
    if mapid == 343 then
        -- map bí cảnh vân lĩnh
        local monster_id = monster:GetMonsterID()
        if monster_id == 21048 or monster_id == 21049 then
            System:WriteToConsole("Monster: " .. monster_id)
            -- là boss tinh anh/thủ lĩnh: chắc chắn drop vật phẩm
            local dropBuilder = EventManager:CreateDropToMapBuilder(scene)
            dropBuilder.SetOwner(killer)
            dropBuilder.AddLockedItem(item_id)
            dropBuilder.SetFromMonster(monster)
            dropBuilder.Build()
            System:WriteToConsole("Drop: " .. monster_type)
        end
        return
    end

    if mapid == 1538 then
        -- map tần lăng 3, tỉ lệ drop 5%
        local RATE_UP = 5
        local POOL_SIZE = 100000
        local main_random = math.random(1, POOL_SIZE)
        if main_random <= POOL_SIZE / 100 * RATE_UP then
            local dropBuilder = EventManager:CreateDropToMapBuilder(scene)
            dropBuilder.SetOwner(killer)
            dropBuilder.AddItem(item_id)
            dropBuilder.SetFromMonster(monster)
            dropBuilder.Build()
        end
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi người chơi bị chết
--		scene: Scene - Bản đồ hiện tại
--		player: Player - NPC tương ứng
--		killerObj: {Monster, Player} - đối tượng giết
-- ****************************************************** --
function MonsterEvent:OnDie(scene, monster, killer)
    if EventManager:IsComposeEventTime() or killer:IsGM() then
        MonsterEvent:OnMonthlyEvent(scene, monster, killer)
        MonsterEvent:OnSpecialDrop(scene, monster, killer)
    end

    local monster_tag = monster:GetTag()
    if monster_tag ~= nil then
        if monster_tag:find('DaoTacThangTu', 1, true) == 1 then
            local builder = EventManager:CreateNPCBuilder(scene)
            builder.SetTag(monster:GetTag())
            builder.SetResID(20200)
            builder.SetPos(monster:GetPos():GetX(), monster:GetPos():GetY())
            builder.SetName("Đạo tặc Sinh nhật")
            builder.SetTitle("[Phần thưởng]")
            builder.SetScriptID(233)
            builder.Build()
            EventManager:RemoveDeadBoss(monster)
        end
    end
end

function MonsterEvent:DropBookExpPet(scene, monster, killer)
    local RATE_UP = 100
    local POOL_SIZE = 100000
    local ITEM_ID = 1057
    local LEVEL_LIMIT = 115

    -- tính tỉ lệ rơi theo RATE_UP
    local random = math.random(1, POOL_SIZE)

    if random <= POOL_SIZE / 100 * RATE_UP then
        local dropBuilder = EventManager:CreateDropToMapBuilder(scene)
        dropBuilder.SetOwner(killer)
        dropBuilder.AddItem(ITEM_ID)
        dropBuilder.SetFromMonster(monster)
        dropBuilder.Build()
    end
end

function MonsterEvent:OnLoad()
    local configPath = 'Config/Config/KT_GameEvents/MonthlyCompose.json'
    local configStr = System:ReadAllText(configPath)
    MONTHLY_EVENT_CONFIG = json.parse(configStr)
end

function MonsterEvent:GetServerDropRate()
    local serverId = System:GetServerID()
    local dropRates = MONTHLY_EVENT_CONFIG.DropRate
    
    for range, rate in pairs(dropRates) do
        local serverRange = Split(range, "-")
        local serverMin = tonumber(serverRange[1])
        local serverMax = tonumber(serverRange[2])
        
        if serverId >= serverMin and serverId <= serverMax then
            return rate
        end
    end
    
    return 5
end

function Split(str, sep)
    local result = {}
    for match in string.gmatch(str, "([^" .. sep .. "]+)") do
        table.insert(result, match)
    end
    return result
end