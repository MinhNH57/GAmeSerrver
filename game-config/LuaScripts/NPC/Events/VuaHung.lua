-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local VuaHung = Scripts[000237]

function VuaHung:OnOpen(scene, npc, player, otherParams)
	local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Mừng Sinh nhật Kiếm Vương 1 ADNX.")
    
    if (System:IsDateTimeInRange('2025-03-24 00:00:00', '2025-04-07 23:59:59')) then
        dialog:AddSelection(1, "Dâng Hương")
    end
    
    
    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)
end

function VuaHung:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 99 then
        GUI:CloseDialog(player)
        return
    end

    if selectionID == 1 then
        GUI:CloseDialog(player)
		GUI.OpenUI(player, "UIOffering")
        return
    end
end

function VuaHung:OnItemSelected(scene, npc, player, itemID, otherParams)

	-- ************************** --
	
	-- ************************** --

end