local BaoThachDaiSu = Scripts[0000238]

function BaoThachDaiSu:OnOpen(scene, npc, player,otherParams)
    local dialog = GUI.CreateNPCDialog()
    --if player:IsGM() == false then
        dialog:AddSelection(9, "Đổi bảo thạch")
        dialog:AddSelection(1, "Đục lỗ trang bị")
        dialog:AddSelection(2, "Khảm nạm bảo thạch")
        dialog:AddSelection(3, "Tháo gỡ bảo thạch")
        dialog:AddSelection(4, "Hợp thành bảo thạch")
        dialog:AddSelection(5, "Tẩy luyện bảo thạch")
        --dialog:AddSelection(11, "Test UI Voice Chat")
        dialog:AddSelection(6, "Để lần sau vậy!")
    --else
        --dialog:AddText("Xin chào " .. player:GetName() .. "!")
        --dialog:AddSelection(6, "Kết thúc hội thoại!")
    --end
    dialog:Show(npc, player)
end

function BaoThachDaiSu:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 6 then
        GUI.CloseDialog(player)
    end
    if selectionID < 6 then
        GUI.OpenUI(player, "UIBaoThach", selectionID)
        GUI.CloseDialog(player)
    end
    if(selectionID == 9) then
        GUI:OpenComposeDialog(player, "UIComposeBT", 1500,1501,1502,1503,1504,1505)
    end
    if(selectionID == 11) then
        GUI:OpenComposeDialog(player, "MainGame/UIVoiceChat", 0)
    end
end 
