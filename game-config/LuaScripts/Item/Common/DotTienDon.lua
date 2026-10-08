-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200082' bên dưới thành ID tương ứng

local DotTienDon = Scripts[200105]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function DotTienDon:OnPreCheckCondition(scene, item, player, otherParams)
    return true
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function DotTienDon:OnUse(scene, item, player, otherParams)
    GUI:CloseDialog(player)

    local LIMIT_DAILY_USED = 3
    local time_used = Player:GetValueOfDailyRecore(player, 100016)
    if time_used == -1 then
        time_used = 0
    end

    if time_used >= LIMIT_DAILY_USED then
        player:AddNotification("Bằng hữu đã hết số lượng sử dụng vật phẩm trong ngày.")
        return
    end

    local cotrain_times = Player:GetValueOfDailyRecore(player, 100017)
    if (cotrain_times < 0) then
        cotrain_times = 0
    end

    if cotrain_times < 2 then
        player:AddNotification("Bằng hữu vẫn còn lượt truyền công trong ngày, đừng lãng phí.")
        return
    end

    cotrain_times = cotrain_times - 1

    Player:SetValueOfDailyRecore(player, 100017, cotrain_times)
    Player:SetValueOfDailyRecore(player, 100016, time_used + 1)
    
    -- xóa vật phẩm
    Player.RemoveItem(player, item.GetID())
    player:AddNotification("Bạn đã nhận được thêm một lần truyền công gia tộc.")
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function DotTienDon:OnSelection(scene, item, player, selectionID, otherParams)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function DotTienDon:OnItemSelected(scene, item, player, itemID)
end