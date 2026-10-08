-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local RuongLuaChon = Scripts[200107]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function RuongLuaChon:OnPreCheckCondition(scene, item, player, otherParams)

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
function RuongLuaChon:OnUse(scene, item, player, otherParams)

	local FactionID = player:GetFactionID()
	-- Nếu chưa gia nhập môn phái
	if FactionID < 1 then
		player:AddNotification("Bạn chưa gia nhập môn phái!!!")
	else
		local dialog = GUI.CreateItemDialog()
		dialog.AddText("Bạn có thể chọn 1 trong 2 trang bị sau:")
		local number = item:GetNumberSelection()
		for i = 1, number do
			local itemID = item:GetWeaponID(FactionID, i)
			dialog:AddItem(itemID, 1, 1)
		end
		dialog:Show(item, player)
	end
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function RuongLuaChon:OnSelection(scene, item, player, selectionID, otherParams)

end


function RuongLuaChon:OnItemSelected(scene, item, player, itemSelection)
    GUI:CloseDialog(player)
    
    local itemID = itemSelection:GetItemID()
    local itemCount = itemSelection:GetItemCount()
    local binding = itemSelection:GetBinding()
	local levelItem = 4

    local neededSlot = Player:GetTotalSpacesNeedToTakeItem(itemID, itemCount)

    if not Player:HasFreeBagSpaces(player, neededSlot) then
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu cần " .. neededSlot .. " ô trống trong túi đồ để nhận vật phẩm.")
        return
    end

    Player:RemoveItem(player, item:GetID())
    if Player.AddItemLua(player, itemID, 1, -1, binding, levelItem) then
        Player.RemoveItem(player, item:GetID())
    else
        player:AddNotification("Có lỗi khi sử dụng vật phẩm")
    end
end
