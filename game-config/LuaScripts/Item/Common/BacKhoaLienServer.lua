-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local BacKhoaLienServer = Scripts[200943]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function BacKhoaLienServer:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> BacKhoaLienServer:OnPreCheckCondition")--
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
function BacKhoaLienServer:OnUse(scene, item, player, otherParams)

	local szMsg = string.format(
        "Bạn sử dụng <color=yellow>%s</color> có thể nhận được :\n\n    <color=yellow>%s Bạc khóa liên máy chủ</color> !",
        item.GetName(), item.GetExtParam(1));
    -- ************************** --
    -- ************************** --
    local dialog = GUI.CreateItemDialog()

    dialog:AddText(szMsg)

    dialog:AddSelection(1, "Đổi tất cả (tối đa 1000)")
    dialog:AddSelection(2, "Đổi bạc khóa liên máy chủ")
    dialog:AddSelection(3, "Để ta suy nghĩ đã")

    dialog:Show(item, player)

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function BacKhoaLienServer:OnSelection(scene, item, player, selectionID, otherParams)

	if selectionID == 2 then

        local MoneyWillBeAdd = item.GetExtParam(1);

        local szType = "Bạc khóa liên máy chủ";
        

        local szMsg = string.format("Bạn xác nhận đổi <color=yellow>%s %s</color> không?", MoneyWillBeAdd,
            szType);
        -- ************************** --
        -- ************************** --
        local dialog = GUI.CreateItemDialog()

        dialog:AddText(szMsg)

        dialog:AddSelection(4, "Ta đồng ý")
        dialog:AddSelection(5, "Để ta suy nghĩ đã")

        dialog:Show(item, player)
		return
    end
	
	if selectionID == 4 then
		local MoneyWillBeAdd = item.GetExtParam(1);
		Player.RemoveItem(player, item:GetID())
		
		if Player.AddMoney(player, MoneyWillBeAdd, 6) then
			GUI.CloseDialog(player)
		else
			player:AddNotification("Có lỗi khi sử dụng vật phẩm")
			GUI.CloseDialog(player)
		end
	end
	
	if selectionID == 1 then
		local MoneyWillBeAdd = item.GetExtParam(1);

        local szType = "Bạc khóa liên máy chủ";
		
		-- lấy số lượng đang có trong người
		local total_Item = Player.CountItemInBag(player, item:GetItemID())
		
		local money_Have = Player.CheckMoney(player, 6)
		
		local money_Can_Add = 1000000000 - money_Have
		
		local number_Can_Add = money_Can_Add / MoneyWillBeAdd
		
		if total_Item > number_Can_Add then
			total_Item = number_Can_Add
		end
		
		if total_Item > 1000 then
			total_Item = 1000
		end
		
		local szMsg = string.format("Bạn xác nhận đổi <color=yellow>%s * %s %s</color> không?", MoneyWillBeAdd, total_Item,
            szType);
			
		local dialog = GUI.CreateItemDialog()

        dialog:AddText(szMsg)

        dialog:AddSelection(6, "Ta đồng ý")
        dialog:AddSelection(5, "Để ta suy nghĩ đã")

        dialog:Show(item, player)
		return
	end

    if selectionID == 6 then
		local MoneyWillBeAdd = item.GetExtParam(1)
		-- lấy số lượng đang có trong người
		local itemID = item:GetItemID()
		local total_Item = Player.CountItemInBag(player, itemID)
		
		local money_Have = Player.CheckMoney(player, 6)
		
		local money_Can_Add = 1000000000 - money_Have
		
		local number_Can_Add = money_Can_Add / MoneyWillBeAdd
		
		if total_Item > number_Can_Add then
			total_Item = number_Can_Add
		end
		
		if total_Item > 1000 then
			total_Item = 1000
		end
		
		MoneyWillBeAdd = MoneyWillBeAdd * total_Item
		
		Player.RemoveItem(player, itemID, total_Item)
		GUI.CloseDialog(player)
		local MoneyWillBeAdd = item.GetExtParam(1) * total_Item
		
		if Player.AddMoney(player, MoneyWillBeAdd, 6) then
			GUI.CloseDialog(player)
		else
			player:AddNotification("Có lỗi khi sử dụng vật phẩm")
			GUI.CloseDialog(player)
		end
    end

    if selectionID == 3 or selectionID == 5 then
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
function BacKhoaLienServer:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end