local BaoThachDaiSu = Scripts[0000238]

function BaoThachDaiSu:OnOpen(scene, npc, player,otherParams)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddSelection(9, "Đổi bảo thạch")
    dialog:AddSelection(1, "Đục lỗ trang bị")
    dialog:AddSelection(2, "Khảm nạm bảo thạch")
    dialog:AddSelection(3, "Tháo gỡ bảo thạch")
    dialog:AddSelection(4, "Hợp thành bảo thạch")
    dialog:AddSelection(5, "Tẩy luyện bảo thạch")
    dialog:AddSelection(6, "Để lần sau vậy!")
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
end 