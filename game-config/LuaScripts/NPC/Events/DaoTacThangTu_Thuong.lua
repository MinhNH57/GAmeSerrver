local DaoTacThangTu_Thuong = Scripts[233]

function DaoTacThangTu_Thuong:GetName(rid)
    return EventManager:GetNameFromRid(rid)
end

function DaoTacThangTu_Thuong:OnOpen(scene, npc, player, otherParams)
    local boss_tag = npc:GetTag()
	local dialog = GUI.CreateNPCDialog()
    local bossCaller = EventManager:GetBossCaller(boss_tag)

    local is_VIP = false
    if boss_tag:find('_VIP', 1, true) ~= nil then
        is_VIP = true
    end

    dialog:AddText('' .. DaoTacThangTu_Thuong:GetName(bossCaller) .. ' đã mở khiêu chiến Đạo tặc Sinh nhật.')
    dialog:AddText('')
    dialog:AddText('Dưới đây là chiến tích của các đại hiệp đã góp công sức nhiều nhất trong chiến đấu:')

    local rank = 1
    local damageRecord = EventManager:GetTop3DamageDealerToBoss(boss_tag)
    for k, v in pairs(damageRecord) do
        dialog:AddText('\t- ' .. DaoTacThangTu_Thuong:GetName(k) .. ' đã gây ra ' .. v .. ' sát thương.')
        rank = rank + 1
    end
    dialog:AddText('')

    local winner_rid = EventManager:GetTop1DamageDealerToBoss(boss_tag)

    local is_rewarded = false
    local current_tag = npc:GetTag()
    if current_tag:find('_DONE', 1, true) ~= nil then
        is_rewarded = true
    end

    if is_VIP then
        dialog:AddText('' .. DaoTacThangTu_Thuong:GetName(winner_rid) .. ' sẽ nhận được phần thưởng.')
        if player:GetID() == winner_rid and not is_rewarded then
            dialog:AddSelection(1, "Nhận thưởng")
        end
    else 
        if winner_rid == bossCaller then
            dialog:AddText('' .. DaoTacThangTu_Thuong:GetName(winner_rid) .. ' sẽ nhận được phần thưởng.')
        end
        if player:GetID() == winner_rid and player:GetID() == bossCaller and not is_rewarded then
            dialog:AddSelection(2, "Nhận thưởng")
        end
    end

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
	-- ************************** --

end

function DaoTacThangTu_Thuong:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 1 then
        DaoTacThangTu_Thuong:ReceiveAward(scene, npc, player, 20550)
        return
    end
    if selectionID == 2 then
        DaoTacThangTu_Thuong:ReceiveAward(scene, npc, player, 20549)
        return
    end
    GUI:CloseDialog(player)
end

function DaoTacThangTu_Thuong:ReceiveAward(scene, npc, player, rewardID)
    local current_tag = npc:GetTag()
    if current_tag:find('_DONE', 1, true) ~= nil then
        GUI:ShowMessageBox(player, 'Thông báo', 'Phần thưởng đã được trao, vui lòng chờ đến lần khiêu chiến tiếp theo.')
        return
    end

    if not Player.HasFreeBagSpaces(player, 1) then
        GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu cần 1 ô trống trong túi đồ để nhận phần thưởng.')
        return
    end

    npc:SetTag(npc:GetTag() .. '_DONE')
    GUI:CloseDialog(player)
    Player.AddItemLuaAndNotify(player, rewardID, 1, 0, 1)
end