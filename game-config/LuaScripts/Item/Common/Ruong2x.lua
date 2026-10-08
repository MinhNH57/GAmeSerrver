-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local Ruong2x = Scripts[200091]

-- đồ 2x theo phái
local vukhi2x = {
    [1] = {
        [1] = 10032, -- thieu lam
        [2] = 10042
    },
    [2] = {
        [1] = 10052, -- Thiên Vương Bang
        [2] = 10062
    },
    [3] = {
        [1] = 12629, -- Đường Môn
        [2] = 12639
    },
    [4] = {
        [1] = 10072, -- Ngũ Độc Giáo
        [2] = 10082
    },
    [5] = {
        [1] = 10112, -- Nga My Phái
        [2] = 10122
    },
    [6] = {
        [1] = 10122, -- Thúy Yên Môn
        [2] = 10092
    },
    [7] = {
        [1] = 10152, -- Cái Bang Phái
        [2] = 10132
    },
    [8] = {
        [1] = 10142, -- Thiên Nhẫn Giáo
        [2] = 10162
    },
    [9] = {
        [1] = 10192, -- Võ Đang Phái
        [2] = 10182
    },
    [10] = {
        [1] = 10172, -- Côn Lôn Phái
        [2] = 10202
    },
    [11] = {
        [1] = 10952, -- Minh Giáo
        [2] = 10942
    },
    [12] = {
        [1] = 10102, -- Đoàn Thị Phái
        [2] = 10122
    }
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function Ruong2x:OnPreCheckCondition(scene, item, player, otherParams)

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
function Ruong2x:OnUse(scene, item, player, otherParams)

    -- ************************** --
    local factionID = player:GetFactionID()
    if factionID == 0 then
        self:ShowDialog(item, player, "Bạn chưa vào phái")
        return
    end

    local subID = player:GetRouteID()
    if subID == 0 then
        self:ShowDialog(item, player, "Bạn chưa chọn nhánh tu luyện")
        return
    end

    local dialog = GUI.CreateItemDialog()
    dialog:AddText(string.format(
        "Ngươi chắc chắn muốn mở <color=yellow>[%d]</color>,  <color=yellow>[%d]</color> không?",
        factionID, subID))

    dialog:AddSelection(1, "Chắc chắn")
    dialog:AddSelection(100, "Ta suy nghĩ đã...")
    dialog:Show(item, player)
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function Ruong2x:OnSelection(scene, item, player, selectionID, otherParams)

    -- ************************** --
    if selectionID == 1 then
        local factionID = player:GetFactionID()
        local subID = player:GetRouteID()
        -- Xóa vật phẩm trước
        Player.RemoveItem(player, item:GetID())
        Player.AddItemLua(player, vukhi2x[factionID][subID], 1, -1, 1)
        GUI.CloseDialog(player)
        return
    end
    -- ************************** --
    if selectionID == 100 then
        GUI.CloseDialog(player)
        return
    end
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function Ruong2x:OnItemSelected(scene, item, player, itemID)

    -- ************************** --

    -- ************************** --

end
