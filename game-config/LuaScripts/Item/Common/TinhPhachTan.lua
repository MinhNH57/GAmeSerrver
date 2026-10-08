-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local TinhPhachTan = Scripts[200110]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TinhPhachTan:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> BanDoBiCanh:OnPreCheckCondition")--
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
function TinhPhachTan:OnUse(scene, item, player, otherParams)

	-- ************************** --
	-- ************************** --
	local dialog = GUI.CreateItemDialog()
    dialog:AddText(string.format("Ngươi chắc chắn muốn sử dụng <color=yellow>[%s]</color> để tăng độ thân mật cho tất cả các bạn đồng hành không?", item:GetName()))
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
function TinhPhachTan:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> BanDoBiCanh:OnSelection, selectionID = " .. selectionID)--
	
	-- ************************** --
	if selectionID == 1 then
        if item:IsExpired() then
            player:AddNotification("Vật phẩm đã hết hạn sử dụng.")
            return
        end
		if player:UpdateThanMatPet(item:GetID(), item.GetItemLevel()) == true then
			player:AddNotification(string.format("Tăng điểm thân mật cho tất cả Bạn đồng hành thành công!"))
		end
        GUI.CloseDialog(player)
		return
	end
    -- ************************** --
	if selectionID == 100 then
		GUI.CloseDialog(player)
		return
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
function TinhPhachTan:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end