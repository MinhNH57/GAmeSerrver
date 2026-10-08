-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local ADD_SHENGWANG = {860,4400,5700,100,50,500,480,300,480}
local TieuDuLongLenh = Scripts[200067]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TieuDuLongLenh:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> TieuDuLongLenh:OnPreCheckCondition")--
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
function TieuDuLongLenh:OnUse(scene, item, player, otherParams)

	-- ************************** --
	-- ************************** --
	local dialog = GUI.CreateItemDialog()
    dialog:AddText("Hãy đưa ra lựa chọn của bạn:")
    dialog:AddSelection(1, "Mở 1")
	dialog:AddSelection(3, "Mở toàn bộ (tối đa 100)")
    dialog:AddSelection(2, "Ta suy nghĩ đã")
	dialog:Show(item, player)

	-- ************************** --
	-- ************************** --
	
	---- số ngày n ăn được bao nhiêu 
	--local GetValue = Player.GetValueOfDailyRecore(player,item:GetItemID())
	----if (GetValue >= USED_LIMIT) then -- một ngày có thể ăn được bao nhiêu lệnh bài
	--	--player:AddNotification("Mỗi ngày chỉ có thể sử dụng 1 vật phẩm này")
	--	--return;
	----end
	--
	--if(GetValue==-1) then
	--	GetValue = 0;
	--end
	--
	--Player.SetValueOfDailyRecore(player,item:GetItemID(),GetValue+1)
	--
	--local szFactionName = player:GetFactionName();
	--
	---- lấy phái
	----local DBID = 300 + player:GetFactionID();
	----loại lệnh bài
	


	-- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function TieuDuLongLenh:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	
	if selectionID == 2 then
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID == 1 then
		local nLevel = item:GetItemLevel()

		if (nLevel < 1 or nLevel > 9 ) then
			player:AddNotification("Vật phẩm không hợp lệ!")
			return;
	
		end
	
		if player:GetFactionID()==0 then
			player:AddNotification("Chưa nhập môn phái!")
			return;
		end
		
		local GetValue = Player.GetValueOfDailyRecore(player,item:GetItemID())
		if(GetValue==-1) then
			GetValue = 0;
		end
		
		local POINT = ADD_SHENGWANG[nLevel];
		--AddRetupeValue : cậu lệnh ăn lệnh bài
		if(nLevel == 1 ) then
			Player.AddRetupeValue(player,504,POINT)
		elseif(nLevel == 2 ) then
			Player.AddRetupeValue(player,801,POINT)
		elseif(nLevel == 3) then
			Player.AddRetupeValue(player,701,POINT)
		elseif(nLevel == 4 ) then
			Player.AddRetupeValue(player,502,POINT)
		elseif(nLevel == 5 ) then
			Player.AddRetupeValue(player,1001,POINT)
		elseif(nLevel == 6 ) then
			Player.AddRetupeValue(player,505,POINT)
		elseif(nLevel == 7 ) then
			Player.AddRetupeValue(player,1101,POINT)
		elseif(nLevel == 8 ) then
			Player.AddRetupeValue(player,506,POINT)
		elseif(nLevel == 9 ) then
			Player.AddRetupeValue(player,1201,POINT)
		end
		Player.RemoveItem(player,item:GetItemID(), 1)
	
		player:AddNotification("Hôm nay đã dùng "..(GetValue+1).."==> "..item:GetName().."+"..POINT.."");
		
		Player.SetValueOfDailyRecore(player,item:GetItemID(),GetValue+1)
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID == 3 then
		local nLevel = item:GetItemLevel()

		if (nLevel < 1 or nLevel > 9 ) then
			player:AddNotification("Vật phẩm không hợp lệ!")
			return;
	
		end
	
		if player:GetFactionID()==0 then
			player:AddNotification("Chưa nhập môn phái!")
			return;
		end
		
		local GetValue = Player.GetValueOfDailyRecore(player,item:GetItemID())
		if(GetValue==-1) then
			GetValue = 0;
		end
		
		-- số lượng item trong túi đồ
		local remainItem = Player:CountItemInBag(player, item:GetItemID())
		-- giới hạn mở 100 cái
		local qualityItem = 100
		if remainItem < qualityItem then
			qualityItem = remainItem
		end
		
		local POINT = ADD_SHENGWANG[nLevel];
		--AddRetupeValue : cậu lệnh ăn lệnh bài
		if(nLevel == 1 ) then
			Player.AddRetupeValue(player,504,POINT  * qualityItem)
		elseif(nLevel == 2 ) then
			Player.AddRetupeValue(player,801,POINT  * qualityItem)
		elseif(nLevel == 3) then
			Player.AddRetupeValue(player,701,POINT  * qualityItem)
		elseif(nLevel == 4 ) then
			Player.AddRetupeValue(player,502,POINT  * qualityItem)
		elseif(nLevel == 5 ) then
			Player.AddRetupeValue(player,1001,POINT  * qualityItem)
		elseif(nLevel == 6 ) then
			Player.AddRetupeValue(player,505,POINT  * qualityItem)
		elseif(nLevel == 7 ) then
			Player.AddRetupeValue(player,1101,POINT  * qualityItem)
		elseif(nLevel == 8 ) then
			Player.AddRetupeValue(player,506,POINT  * qualityItem)
		elseif(nLevel == 9 ) then
			Player.AddRetupeValue(player,1201,POINT  * qualityItem)
		end
		Player.RemoveItem(player,item:GetItemID(), qualityItem)
	
		player:AddNotification("Hôm nay đã dùng "..(GetValue + qualityItem).."==> "..item:GetName().."+"..(POINT * qualityItem).."");
		
		Player.SetValueOfDailyRecore(player,item:GetItemID(),GetValue + qualityItem)
		GUI.CloseDialog(player)
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
function TieuDuLongLenh:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end

function TieuDuLongLenh:ShowDialog(item, player, text)

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	dialog:AddText(text)
	dialog:Show(item, player)
	-- ************************** --
	
end