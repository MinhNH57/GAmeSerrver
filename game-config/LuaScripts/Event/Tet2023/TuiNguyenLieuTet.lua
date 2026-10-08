-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng

local TuiNguyenLieuTet = Scripts[700004]

local function AddItem(player, ID, Number)
    return Player.AddItemLuaAndNotify(player, ID, Number, 0, 0)
end

local PROBABILITY_DEFINE = {
    ['Mứt dừa'] = 30000,
    ['Rượu vang'] = 30000,
    ['Hạt điều'] = 30000,
    ['Trà San tuyết cổ thụ'] = 10000
}

local RECEIVE_REWARD = {
    ['Mứt dừa'] = {AddItem, 15181, 1},
    ['Rượu vang'] = {AddItem, 15182, 1},
    ['Hạt điều'] = {AddItem, 15183, 1},
    ['Trà San tuyết cổ thụ'] = {AddItem, 15184, 1},
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TuiNguyenLieuTet:OnPreCheckCondition(scene, item, player, otherParams)
    -- nếu túi đồ đầy thì không cho dùng
    if not Player.HasFreeBagSpaces(player, 1) then
        player.AddNotification("Cần để trống tối thiểu <color=green>1 ô trống</color> trong túi đồ để nhận nguyên liệu Tết!")
        return false
    end

    return true
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function TuiNguyenLieuTet:OnUse(scene, item, player, otherParams)
    -- tạo pool xác suất
    local pool = 0
    for k, v in pairs(PROBABILITY_DEFINE) do
        pool = pool + v
    end

    -- random ra 1 điểm trong pool
    local rand = math.random(1, pool)

    -- duyệt cộng dồn xác suất của từng vật phẩm
    -- nếu xác suất cộng dồn lớn hơn điểm random -> điểm random nằm trong đoạn xác suất của vật phẩm
    -- thì trả về vật phẩm
    local accumulatedProbability = 0
    local reward = nil
    for k, v in pairs(PROBABILITY_DEFINE) do
        accumulatedProbability = accumulatedProbability + v
        if rand <= accumulatedProbability then
            reward = k
            break
        end
    end

    func = RECEIVE_REWARD[reward]

    if func[1](player, func[2], func[3]) then
        Player.RemoveItem(player, item:GetID())
    end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TuiNguyenLieuTet:OnSelection(scene, item, player, selectionID, otherParams)

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TuiNguyenLieuTet:OnItemSelected(scene, item, player, itemID)

end
