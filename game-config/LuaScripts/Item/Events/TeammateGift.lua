local TeammateGift = Scripts[200130]

local GIFT_POINT = {
    [20831] = 1,
    [20832] = 4,
    [20794] = 12,
    [20795] = 60
}

function TeammateGift:OnPreCheckCondition(scene, item, player, otherParams)
    --if player:IsGM() then
    --    return true
    --end

    if not player:HasTeam() then
        -- không có tổ đội, không cho tặng quà
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu hãy lập tổ đội với người thương để có thể tặng quà!")
        return false
    end

    if player:GetTeamSize() ~= 2 then
        -- tổ đội có số lượng thành viên không hợp lệ 
        GUI:ShowMessageBox(player, "Thông báo", "Chỉ có thể tặng quà cho 1 người thương!\n(Tổ đội hiện tại có nhiều hơn 2 thành viên)")
        return false
    end

     local partner = player:GetTeammateInPair()
     if player:GetSex() + partner:GetSex() ~= 1 then
         -- giới tính không hợp lệ
         local otherSex = "nữ"
         if player:GetSex() == 1 then
             otherSex = "nam"
         end
         GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu chỉ có thể tặng quà cho bạn " .. otherSex .. "!")
         return false
     end

	return true
end


function TeammateGift:UpdateGift(player, giftNumber, isReceived)
    local currentSent = player.GetRoleParamsInt32FromDB("GiftExchange_Sent", 0)
    local currentReceived = player.GetRoleParamsInt32FromDB("GiftExchange_Received", 0)

    if isReceived then
        currentReceived = currentReceived + giftNumber
        player.SaveRoleParamsInt32ValueToDB("GiftExchange_Received", currentReceived, true)
    else
        currentSent = currentSent + giftNumber
        player.SaveRoleParamsInt32ValueToDB("GiftExchange_Sent", currentSent, true)
    end
end


function TeammateGift:OnUse(scene, item, player, otherParams)
    local partner = player:GetTeammateInPair()
    --if player:IsGM() then
    --    partner = player
    --end
    
    local itemName = item:GetName()
    local itemCount = item:GetItemCount()

    local currentSent = player.GetRoleParamsInt32FromDB("GiftExchange_Sent", 0)
    local currentReceived = player.GetRoleParamsInt32FromDB("GiftExchange_Received", 0)
    
    GUI.ShowMessageBox(player, "Thông báo", "Bạn đang có " .. currentSent .. " điểm nhân duyên, " .. currentReceived .. " điểm hấp dẫn. Xác nhận tặng " .. itemCount .. " " .. itemName .. " cho " .. partner:GetName() .. "?", 
        function()
            local itemCount = item:GetItemCount()
            local itemID = item:GetItemID()

            --if player:IsGM() then
            --    Player:RemoveItemBySlotCount(player, item:GetID())
            --
            --    TeammateGift:UpdateGift(player, itemCount * GIFT_POINT[itemID], true)
            --    TeammateGift:UpdateGift(player, itemCount * GIFT_POINT[itemID], false)
            --    return
            --end

            if not player:HasTeam() then
                GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra, vui lòng thử lại!")
                return
            end
        
            if player:GetTeamSize() ~= 2 then
                GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra, vui lòng thử lại!")
                return
            end
        
            local partner = player:GetTeammateInPair()
            if player:GetSex() + partner:GetSex() ~= 1 then
                GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra, vui lòng thử lại!")
                return
            end

            Player:RemoveItemBySlotCount(player, item:GetID())
            
            TeammateGift:UpdateGift(partner, itemCount * GIFT_POINT[itemID], true)
            TeammateGift:UpdateGift(player, itemCount * GIFT_POINT[itemID], false)
        end, nil
    )
end

function TeammateGift:OnSelection(scene, item, player, selectionID, otherParams)

end

function TeammateGift:OnItemSelected(scene, item, player, itemID)

end