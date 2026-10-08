local DuTuNgu = Scripts[010013]

function DuTuNgu:OnOpen(scene, npc, player, otherParams)
    local dialog = GUI.CreateNPCDialog()

    dialog:AddText(
        "<color=#ffff33>" .. npc:GetName() .. "</color>: " ..
        "Cổ Mộ thanh tĩnh, võ học uyên thâm. " ..
        "Nếu bằng hữu có duyên, có thể lựa chọn gia nhập bổn môn."
    )
    dialog:AddSelection(1, "Đưa ta tới gặp bang chủ")
    dialog:AddSelection(2, "Rời khỏi")
    dialog:Show(npc, player)
end

function DuTuNgu:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 1 then
        GUI.CloseDialog(player)
        local mapCoMo = Global_NameMapPhaiID[2000]
        if mapCoMo ~= nil then
            player:ChangeScene(mapCoMo.ID, mapCoMo.PosX, mapCoMo.PosY)
        end
    elseif selectionID == 2 then
        GUI.CloseDialog(player)
    end
end

function DuTuNgu:OnItemSelected(scene, npc, player, itemID, otherParams)
end
