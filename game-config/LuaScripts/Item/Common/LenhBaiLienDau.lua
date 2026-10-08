-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local ADD_SHENGWANG = {200, 400, 600,1000}
local CAMP = 701;
local LenhBaiLienDau = Scripts[200070]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function LenhBaiLienDau:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> LenhBaiLienDau:OnPreCheckCondition")--
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
function LenhBaiLienDau:OnUse(scene, item, player, otherParams)

	-- ************************** --
	-- ************************** --
	
	local dialog = GUI.CreateItemDialog()
    dialog:AddText(string.format("Ngươi chắc chắn muốn dùng <color=yellow>[%s]</color> không?", item:GetName()))
	dialog:AddSelection(1, "Chắc chắn")
	dialog:AddSelection(2, "Dùng toàn bộ(tối đa 10)")
	dialog:AddSelection(3, "Để ta suy nghĩ lại")
	dialog:Show(item, player)
	---- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function LenhBaiLienDau:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> LenhBaiLienDau:OnSelection, selectionID = " .. selectionID)--
	
	-- ************************** --
	
	if selectionID == 1 then
		local nLevel = item:GetItemLevel()
	
		if (nLevel < 1 or nLevel > 4) then
			player:AddNotification("Vật phẩm không hợp lệ!")
			return;
		
		end
		
		if player:GetFactionID()==0 then
			player:AddNotification("Chưa nhập môn phái!")
			return;
		end
		-- số ngày n ăn được bao nhiêu 
		local GetValue = Player.GetValueOfDailyRecore(player,item:GetItemID())
		
		
		if(GetValue==-1) then
			GetValue = 0;
		end
		Player.SetValueOfDailyRecore(player,item:GetItemID(),GetValue+1)
		local POINT = ADD_SHENGWANG[nLevel];
		
		for key,value in pairs(Global_CAMP) do
			if CAMP == key then
				Player.AddRetupeValue(player,CAMP,POINT)
				Player.RemoveItem(player,item:GetID())
				player:AddNotification("Hôm nay đã dùng "..(GetValue+1).." =>"..item:GetName().." + "..POINT.." "..value.NameActivity.."");
			
			end
		end
		GUI.CloseDialog(player);
	end
	
	if selectionID == 2 then
		local nLevel = item:GetItemLevel()
	
		if (nLevel < 1 or nLevel > 4) then
			player:AddNotification("Vật phẩm không hợp lệ!")
			return;
		
		end
		
		if player:GetFactionID()==0 then
			player:AddNotification("Chưa nhập môn phái!")
			return;
		end
		
		local in_bag = Player.CountItemInBag(player, item.GetItemID())
		
		local max_use = 10
		
		if in_bag < max_use then
			max_use = in_bag
		end
			
		-- số ngày n ăn được bao nhiêu 
		local GetValue = Player.GetValueOfDailyRecore(player,item:GetItemID())
		
		
		if(GetValue==-1) then
			GetValue = 0;
		end
		Player.SetValueOfDailyRecore(player,item:GetItemID(),GetValue+max_use)
		local POINT = ADD_SHENGWANG[nLevel];
		
		for key,value in pairs(Global_CAMP) do
			if CAMP == key then
				Player.AddRetupeValue(player,CAMP,POINT * max_use)
				Player.RemoveItem(player,item:GetItemID(),max_use)
				player:AddNotification("Hôm nay đã dùng "..(GetValue+max_use).." =>"..item:GetName().." + "..POINT*max_use.." "..value.NameActivity.."");
			
			end
		end
		GUI.CloseDialog(player);
	end
	
	if selectionID == 3 then
		GUI.CloseDialog(player);
	end

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function LenhBaiLienDau:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end