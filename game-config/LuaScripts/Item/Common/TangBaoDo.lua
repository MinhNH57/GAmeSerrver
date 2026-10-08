-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local TangBaoDo = Scripts[200012]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TangBaoDo:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> TangBaoDo:OnPreCheckCondition")--
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
function TangBaoDo:OnUse(scene, item, player, otherParams)

	-- ************************** --
	-- ************************** --
	-- 
	-- player:ReadTangBaoDo(0 ,item:GetID())
	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Mở Tàng Bảo Đồ để biết tọa độ chứa kho báu bí mật")
	dialog:AddSelection(1, "Mở Tàng Bảo Đồ")
	dialog:AddSelection(2, "Để ta suy nghĩ đã")
	-- local labanCount = player:IsHasItem(331)
	-- if(labanCount > 0) then
	-- 	dialog:AddText("Đây là Tàng bản đồ của Quân Mã Trương, bạn phải phân biệt được Tàng bản đồ mới thấy được kho báu")
	-- 	dialog:AddSelection(1, "Bắt đầu đọc")
	-- 	dialog:AddSelection(2, "Để ta suy nghĩ đã")

	-- else
	-- 	dialog:AddText("Đây là Tàng bản đồ của Quân Mã Trương, bạn phải phân biệt được Tàng bản đồ mới thấy được kho báu. Hãy mua la bàn để xem")
	-- end
	-- dialog:AddText("Chức năng chưa mở")
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
function TangBaoDo:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> TangBaoDo:OnSelection, selectionID = " .. selectionID)--
	
	-- ************************** --
	if selectionID == 1  then
		-- local MapID = scene:GetID()
		-- local GetValue = Player.GetValueForeverRecore(player, item:GetID())
		-- if(GetValue < 0) then
		-- 	player:ReadTangBaoDo(10, item:GetID())
		-- else
		-- 	player:CreateDialogFindChest()
		-- end
		GUI.CloseDialog(player)
		player:ReadTangBaoDo(0 ,item:GetID())
		
    elseif selectionID == 2 then
        GUI.CloseDialog(player)
    end
	item:FinishUsing(player)
	

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TangBaoDo:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi khi người chơi đọc bản đồ xong
--		scene: Scene - Bản đồ hiện tại
--		player: Player - NPC tương ứng
-- ****************************************************** --
function TangBaoDo:ReadMapComplete(scene, player)

	-- ************************** --

	-- ************************** --

end