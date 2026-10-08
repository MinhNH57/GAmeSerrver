-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000151' bên dưới thành ID tương ứng
local MaBaLac = Scripts[000226]

function MaBaLac:OnOpen(scene, npc, player, otherParams)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Ta có thể giúp bằng hữu thăng cấp thú cưỡi.")
    
    -- dialog:AddSelection(5, "Đổi dây cương")
    -- dialog:AddSelection(6, "Nâng cấp Thỏ Ngọc")
    -- dialog:AddSelection(7, "Nâng cấp Mạch Thượng Phi Sương")
    

    dialog:Show(npc, player)
end

function MaBaLac:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 5 then
        GUI:OpenComposeDialog(player, "", 300)
        return
    end

    if selectionID == 6 then
        if System:GetServerID() < 19 then
            GUI:OpenComposeDialog(player, "", 301, 302, 303, 304, 305, 306)
        else
            GUI:OpenComposeDialog(player, "", 311, 312, 313, 314, 315, 316)
        end
        return
    end

    if selectionID == 7 then
        if System:GetServerID() < 19 then
            GUI:OpenComposeDialog(player, "", 351, 352, 353, 354, 355, 356)
        else
            GUI:OpenComposeDialog(player, "", 361, 362, 363, 364, 365, 366)
        end
        return
    end
end

function MaBaLac:OnItemSelected(scene, npc, player, itemID, otherParams)
end

function MaBaLac:shuffle(t)
    local tbl = {}
    for i = 1, #t do
        tbl[i] = t[i]
    end
    for i = #tbl, 2, -1 do
        local j = math.random(i)
        tbl[i], tbl[j] = tbl[j], tbl[i]
    end
    return tbl
end
