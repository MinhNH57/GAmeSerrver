-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200003' bên dưới thành ID tương ứng
local BoTuLenh = Scripts[200093]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function BoTuLenh:OnPreCheckCondition(scene, item, player, otherParams)

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
function BoTuLenh:OnUse(scene, item, player, otherParams)

	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Bạn có thêm 1 lần đổi song tu môn phái sau khi sử dụng, 3 ngày sau mới thể sử dụng lại.")
	dialog:AddSelection(1, "Sử dụng")
	dialog:AddSelection(2, "Chưa muốn...")
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
function BoTuLenh:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 2 then
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID == 1 then
		-- check xem còn lượt đổi phụ tu ko?
		if player:GetNumChangeSongTu() < 1 then
			player:AddNotification("Bạn vẫn còn lượt đổi song tu, đừng lãng phí!")
			GUI.CloseDialog(player)
			return;
		end
		
		------ check xem tuần vừa rồi đã dùng chưa?
		--if player:CheckDayBoTuLenh() == false then 
		--	player:AddNotification("3 ngày mới có thể sử dụng vật phẩm này 1 lần")
		--	GUI.CloseDialog(player)
		--	return;
		--end
		
		-- set lại lượt đổi phụ tu
		player:ResetNumChangeSongTu()
		
		-- set lại lượt dùng vật phẩm
		player:SetDayBoTuLenh()
		
		-- xóa vật phẩm
		Player.RemoveItem(player,item:GetID())
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Sử dụng thành công, bạn có thêm 1 lần đổi song tu nữa!")
		dialog:Show(item, player)
		return
	end
end