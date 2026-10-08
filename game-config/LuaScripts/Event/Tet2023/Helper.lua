local Helper = Scripts[700005]

local RATE_UP = 2.5
local POOL_SIZE = 100000
local ITEM_ID = 15186 -- ID của vật phẩm Túi nguyên liệu

function Helper:OnMonsterDie(scene, monster, killer) 
    if not Global_IsInTet2023Event() then
        return
    end
    -- quái dưới level 50 không rơi vật phẩm
    if monster:GetLevel() < 50 then return end

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