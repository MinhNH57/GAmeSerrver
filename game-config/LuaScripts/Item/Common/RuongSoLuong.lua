-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local RuongSoLuong = Scripts[200108]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function RuongSoLuong:OnPreCheckCondition(scene, item, player, otherParams)

    -- ************************** --
    -- player:AddNotification("Enter -> RuongSoLuong:OnPreCheckCondition")--
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
function RuongSoLuong:OnUse(scene, item, player, otherParams)

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
function RuongSoLuong:OnSelection(scene, item, player, selectionID, otherParams)

    if (selectionID == 1) then
		-- Lấy ra môn phái của người chơi
		local FactionID = player:GetFactionID()
		-- Nếu chưa gia nhập môn phái
		if FactionID < 1 then
			player:AddNotification("Bạn chưa gia nhập môn phái!!!")
			return
		end
		-- Lấy ra cấp của người chơi
		local Level = player:GetLevel()
		-- Lấy ra cấp của rương
		local LevelRuong = item:GetItemLevel()
		-- Nếu chưa đủ cấp
		if Level < LevelRuong then
			player:AddNotification(string.format("Bạn chưa đủ cấp để mở, yêu cầu cấp %d", LevelRuong))
			return
		end
		
		-- lấy ra ngũ hành của người chơi
		local series = Player.GetSeries(player)
		-- lấy ra giới tính
		local sex = player:GetSex()
		-- lấy ra số lượng
		local number = item:GetNumberAdd()
		-- Nếu ko đủ chỗ trống
		if not Player.HasFreeBagSpaces(player, number) then
			player:AddNotification(string.format("Bạn cần tối thiểu %d chỗ trống trong túi đồ", number))
			return
		end
		
		-- Vật phẩm khóa hay ko khóa
		local binding = item:GetBindingState()
		
		-- add đồ theo giới tính, ngũ hành
		for i = 1, number do
			local itemID = item:GetCraftItemID(series, sex, i)
			Player.AddItemLua(player, itemID, 1, -1, binding) 
		end
		
		-- xóa item hiện tại
		Player.RemoveItem(player, item:GetID())
		
		GUI.CloseDialog(player)
	end
	
	if selectionID == 2 then
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
function RuongSoLuong:OnItemSelected(scene, item, player, itemID)

    -- ************************** --

    -- ************************** --

end
