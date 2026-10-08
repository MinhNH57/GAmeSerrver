-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local PineTree = Scripts[000225]

function PineTree:OnOpen(scene, npc, player, otherParams)
	local dialog = GUI.CreateNPCDialog()

    dialog:AddSelection(2, "Cửa hàng đổi phiếu Sinh nhật")
    
    if (player:IsGM()) then
        dialog:AddSelection(3, "Hiệu ứng")
        dialog:AddSelection(4, "<color=#0486dc>[GM]</color> Cây thông cấp 1")
        dialog:AddSelection(5, "<color=#0486dc>[GM]</color> Cây thông cấp 2")
        dialog:AddSelection(6, "<color=#0486dc>[GM]</color> Cây thống cấp 3")    
    end

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
end

function PineTree:ChangePine(scene, level) 
    local cakeID = 20167
    if level == 2 then
        cakeID = 20168
    elseif level == 3 then
        cakeID = 20169
    end
        
    local builder = EventManager:CreateNPCBuilder(scene)

    builder.SetResID(cakeID)
    builder.SetPos(5075, 3350)
    builder.SetName("Cây thông Noel")
    builder.SetScriptID(225)
    builder.Build()
end

function PineTree:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 2 then
        Player:OpenShop(npc, player, 1460)
        GUI:CloseDialog(player)
        return
    end
    
    if selectionID == 3 then
        local builder = EventManager:CreateDynamicAreaBuilder(scene)

        builder.SetResID(20170)
        builder.SetPos(5075, 3350)
        builder.SetName("")
        builder.SetLifeTime(2)
        builder.SetRadius(0)
        builder.Build()

        GUI:CloseDialog(player)
    end

    if selectionID == 4 then
        PineTree:ChangePine(scene, 1)
        GUI:CloseDialog(player)
    end

    if selectionID == 5 then
        PineTree:ChangePine(scene, 2)
        GUI:CloseDialog(player)
    end

    if selectionID == 6 then
        PineTree:ChangePine(scene, 3)
        GUI:CloseDialog(player)
    end
end

function PineTree:OnItemSelected(scene, npc, player, itemID, otherParams)
end