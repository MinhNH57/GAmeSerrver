local DaoTacThangTu = Scripts[232]

function DaoTacThangTu:OnOpen(scene, npc, player, otherParams)

	local dialog = GUI.CreateNPCDialog()

    dialog:AddSelection(1, "Khiêu chiến")
    dialog:AddSelection(2, "Khiêu chiến (vé diệt phỉ)")

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
	-- ************************** --

end

function DaoTacThangTu:OnSelection(scene, npc, player, selectionID, otherParams)
    GUI:CloseDialog(player)
    if selectionID == 1 then
        local record = 5000114
        local daily_count = Player:GetValueOfDailyRecore(player, record)
        if (daily_count < 0) then
            daily_count = 0
        end

        if (daily_count >= 5) then
            GUI:ShowMessageBox(player, "Thông báo", "Hôm nay đã khiêu chiến miễn phí đủ 5 lần. Hãy dùng [Vé diệt phỉ] để tiến hành khiêu chiến đặc biệt hoặc quay lại vào ngày mai!")
            return
        end

        Player:SetValueOfDailyRecore(player, record, daily_count + 1)

        DaoTacThangTu:DeployMonster(scene, npc, player, 'FREE')
        return
    end
    if selectionID == 2 then
        local ticket = player:IsHasItem(20552)
        if ticket <= 0 then
            GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu cần sở hữu [Vé diệt phỉ] để tiến hành khiêu chiến. [Vé diệt phỉ] có thể mua ở Kỳ Trân Các.")
            return
        end
        if Player:RemoveItem(player, 20552, 1) then
            DaoTacThangTu:DeployMonster(scene, npc, player, 'VIP')
            return
        else
            GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng thử lại sau.")
            return
        end
    end
end


function DaoTacThangTu:DeployMonster(scene, npc, player, isVIP) 
    if EventManager:GetBossCaller(npc:GetTag() .. '_VIP') ~= -1 or EventManager:GetBossCaller(npc:GetTag() .. '_FREE') ~= -1 then
        GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng thử lại sau.")
        return
    end

    local tag = npc:GetTag() .. '_' .. isVIP
    local builder = EventManager:CreateMonsterBuilder(scene)
    builder:SetTag(tag)
    EventManager:SaveBossCaller(tag, player)
    if isVIP == 'FREE' then
        builder:SetTitle('Tinh Anh')
    elseif isVIP == 'VIP' then
        builder:SetTitle('Thủ Lĩnh')
    end

    builder:SetResID(9838)
    builder:SetPos(npc:GetPos())
    builder:SetMaxHP(30000000)
    if System:GetServerID() == 21 or System:GetServerID() == 47 then
        builder:SetMaxHP(1000000)
    end
    builder:SetAIType(3) -- set là boss
    builder:SetAIScriptID(100001) -- set sử dụng bộ kỹ năng khai báo trong Monsters.xm
    builder:SetDone(
        function (monster)
            EventManager:AddBoss(monster)
        end
    )
    if builder.build() then
        System:NotifyAll('Đạo tặc Sinh nhật đã được triệu hồi')
        EventManager:DeleteNPC(npc)
    end
end