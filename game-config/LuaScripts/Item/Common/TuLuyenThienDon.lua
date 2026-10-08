-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200003' bên dưới thành ID tương ứng
local TuLuyenThienDon = Scripts[200094]

-- ************************** --
local Use_Max = 2
local Use_OneItem = 20
-- ************************** --

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TuLuyenThienDon:OnPreCheckCondition(scene, item, player, otherParams)

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
function TuLuyenThienDon:OnUse(scene, item, player, otherParams)
	if item:IsExpired() then
        player.AddNotification("Vật phẩm đã hết hạn sử dụng!")
        return
    end

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Sử dụng để nâng cấp mật tịch đang trang bị lên cấp 100.")
	dialog:AddSelection(1, "Sử dụng Tu Luyện Thiên Đơn")
	dialog:AddSelection(1000, "Suy nghĩ thêm...")
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
function TuLuyenThienDon:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 1000 then
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 1 then
        if Player:SetBookLevelAndExp(player, 100, 0) then
            Player.RemoveItem(player, item:GetID())
			GUI.CloseDialog(player)
			player.AddNotification("Nâng cấp mật tịch thành công!")
        else
            local dialog = GUI.CreateItemDialog()
            dialog:AddText("Đã có lỗi xảy ra. Vui lòng trang bị mật tịch và thử lại sau!")
            dialog:AddSelection(1000, "Kết thúc.")
            dialog:Show(item, player)
        end
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
function TuLuyenThienDon:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end


function TuLuyenThienDon:ShowDialog(item, player, text)

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	dialog:AddText(text)
	dialog:Show(item, player)
	-- ************************** --
	
end