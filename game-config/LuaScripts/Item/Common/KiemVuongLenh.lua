-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local KiemVuongLenh = Scripts[206971]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function KiemVuongLenh:OnPreCheckCondition(scene, item, player, otherParams)
    -- ************************** --
    return true
    -- ************************** --
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function KiemVuongLenh:OnUse(scene, item, player, otherParams)
    local dialog = GUI.CreateItemDialog()
    local bundleID = item:GetExtParam(1)
    
    local text = string.format("Mở khóa Kiếm Vương Lệnh [%s]?", 
        bundleID == 0 and "Siêu cấp" or "Cao cấp")
    
    dialog:AddText(text)
    dialog:AddSelection(101, "Mở khóa")
    dialog:AddSelection(100, "Ta chưa cần")
    dialog:Show(item, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function KiemVuongLenh:OnSelection(scene, item, player, selectionID, otherParams)
    if selectionID == 101 then
        local bundleID = item:GetExtParam(1)
        if Player.UnlockBattlePassElite(player, bundleID) then
            Player.RemoveItem(player,item:GetID())
        end
        GUI.CloseDialog(player)
    else
        GUI.CloseDialog(player)
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function KiemVuongLenh:OnItemSelected(scene, item, player, itemID)

end
