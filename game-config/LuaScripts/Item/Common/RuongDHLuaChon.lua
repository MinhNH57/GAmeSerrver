-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local RuongDHLuaChon = Scripts[200944]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function RuongDHLuaChon:OnPreCheckCondition(scene, item, player, otherParams)

    -- ************************** --
    -- player:AddNotification("Enter -> RuongLuaChon:OnPreCheckCondition")--
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
function RuongDHLuaChon:OnUse(scene, item, player, otherParams)

    local dialog = GUI.CreateItemDialog()
    dialog:AddText("Bạn có chắc chắn muốn sử dụng :" .. item:GetName() .. "?")

    dialog:AddSelection(1, "Ta đồng ý")
    dialog:AddSelection(2, "Ta suy nghĩ đã")

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
function RuongDHLuaChon:OnSelection(scene, item, player, selectionID, otherParams)

    if (selectionID == 1) then
		
		local dialog = GUI.CreateItemDialog()
		
		
		local number = item:GetNumberSelection()
		dialog.AddText("Bạn có thể chọn 1 trong " .. number .. " trang bị sau:")
		for i = 1, number do
			local itemID = item:GetWeaponID(0, i)
			dialog.AddSelection(100 + i, item:GetNameFromID(itemID))
		end
		
		dialog.Show(item, player)
	elseif selectionID == 2 then
        GUI.CloseDialog(player)
    else
		local subID = selectionID - 100
		local itemID = item:GetWeaponID(0, subID)
		-- Vật phẩm khóa hay ko khóa
		local binding = item:GetBindingState()
		if Player.AddItemLua(player, itemID, 1,-1,binding) then
            Player.RemoveItem(player, item:GetID())
        else
            player:AddNotification("Có lỗi khi sử dụng vật phẩm")
        end
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
function RuongDHLuaChon:OnItemSelected(scene, item, player, itemID)

    -- ************************** --

    -- ************************** --

end
