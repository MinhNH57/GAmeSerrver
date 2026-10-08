-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200082' bên dưới thành ID tương ứng

local MatTichDon = Scripts[200101]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function MatTichDon:OnPreCheckCondition(scene, item, player, otherParams)
    return true
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function MatTichDon:OnUse(scene, item, player, otherParams)
    local exp_to_add = item:GetExtParam(1)
    local exp_to_show = exp_to_add / 100 * 4
    local times = math.floor(exp_to_add / 5000)
    local remain = exp_to_add - times * 5000
    for i = 1, times, 1 do
        if not Player:AddBookExp(player, 5000) then
            player:AddNotification("Đã có lỗi xảy ra. Vui lòng trang bị mật tịch và thử lại!")
            return
        end
    end

    if remain > 0 then
        if not Player:AddBookExp(player, remain) then
            player:AddNotification("Đã có lỗi xảy ra. Vui lòng trang bị mật tịch và thử lại!")
            return
        end 
    end
    
    -- xóa vật phẩm
    Player.RemoveItem(player, item.GetID())
    player:AddNotification("Bạn đã nhận được <color=#f38ba8>" .. exp_to_show .. "</color> điểm kinh nghiệm mật tịch.")
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function MatTichDon:OnSelection(scene, item, player, selectionID, otherParams)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function MatTichDon:OnItemSelected(scene, item, player, itemID)
end