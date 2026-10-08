-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local PhieuCapDo = Scripts[200947]

function PhieuCapDo:OnPreCheckCondition(scene, item, player, otherParams)
    return player:CanProcessDowngradeLevel(item:GetItemValue())
end

function PhieuCapDo:OnUse(scene, item, player, otherParams)
    GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu chắc chắn muốn sử dụng " .. item:GetName() .. " để nhận cấp độ " .. item:GetItemValue() .. "?",
        function()
            player:SetLevel(item:GetItemValue())
            Player.RemoveItem(player, item:GetID())
            GUI:ShowMessageBox(player, "Thông báo", "Thiết lập cấp độ thành công.")
        end,
        function()
        end
    )
end

function PhieuCapDo:OnSelection(scene, item, player, selectionID, otherParams)
end

function PhieuCapDo:OnItemSelected(scene, item, player, itemID)
end
