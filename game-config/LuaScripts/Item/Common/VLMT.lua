-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200082' bên dưới thành ID tương ứng

local VLMT = Scripts[200097]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function VLMT:OnPreCheckCondition(scene, item, player, otherParams)
    return true
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function VLMT:OnUse(scene, item, player, otherParams)
    local record_key = item:GetExtParam(1)
    local point_to_add = item:GetExtParam(2)
    local usage_limit = item:GetExtParam(3)
    local totalTimeUse = Player.GetValueForeverRecore(player, record_key)
    if totalTimeUse == -1 then -- chưa có record thì coi là 0
        totalTimeUse = 0
    end
    
    -- nếu số lượt dùng đạt tối đa thì thôi không cho dùng
    if totalTimeUse >= usage_limit then
        player.AddNotification("Bạn đã sử dụng hết " .. totalTimeUse .. "/" .. usage_limit .. " " .. item:GetName() .. "!")
        return
    end

	-- ************************** --
	-- ************************** --
	if player:GetFactionID()==0 then
		player:AddNotification("Cần phải gia nhập môn phái để sử dụng " .. item:GetName() .."!")
		return;
	end
	
    -- cộng 1 điểm tiềm năng
	player.AddBonusRemainSkillPoint(point_to_add)

    -- cập nhật số lượng sử dụng
    Player.SetValueOfForeverRecore(player, record_key, totalTimeUse + 1)
    player.AddNotification("Bạn đã sử dụng " .. (totalTimeUse + 1) .. "/" .. usage_limit .. " " .. item:GetName() .. "!")

    -- xóa vật phẩm
    Player.RemoveItem(player, item.GetID())
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function VLMT:OnSelection(scene, item, player, selectionID, otherParams)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function VLMT:OnItemSelected(scene, item, player, itemID)
end