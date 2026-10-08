-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200003' bên dưới thành ID tương ứng
local DaMinhChau = Scripts[200102]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function DaMinhChau:OnPreCheckCondition(scene, item, player, otherParams)

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
function DaMinhChau:OnUse(scene, item, player, otherParams)

	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Sử dụng trong Tần Lăng để kháng lại chướng khí")
	dialog:AddSelection(1, "Sử dụng")
	--lấy ra số lượng dmc đã dùng trước đó
	local numberUse = player:GetNumberDMCHasUsed()
	if numberUse < 0 then
		numberUse = 0
	end
	-- số lượng DMC cần để có buff
	local numberDMC = player:GetMultiplyNightPearl()
	
	local needUse = numberDMC - numberUse
	if needUse > 10 then
		needUse = 10
	end
	if needUse > 1 then
		dialog:AddSelection(3, "Sử dụng toàn bộ(tối đa "..needUse..")")
	end
	
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
function DaMinhChau:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 2 then
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID == 1 then
		-- check xem đang ở bản đồ nào? có phải đang trong tần lăng hay ko?
		local mapID = player:GetMapCode()
		if (mapID < 1536 or mapID > 1541) then
			player:AddNotification("Bản đồ hiện tại không thể sử dụng vật phẩm này")
			GUI.CloseDialog(player)
			return;
		end
		-- kiểm tra nếu có buff r thì ko cho cắn nữa
		if player:HasBuff(1413) then
			player:AddNotification("Bạn đã dùng đủ số lượng Dạ Minh Châu và có buff Dạ Minh Châu!")
			GUI.CloseDialog(player)
			return;
		end
		--lấy ra số lượng dmc đã dùng trước đó
		local numberUse = player:GetNumberDMCHasUsed()
		if numberUse < 0 then
			numberUse = 0
		end
		
		numberUse = numberUse + 1
		-- số lượng DMC cần để có buff
		local numberDMC = player:GetMultiplyNightPearl()
		
		local minusDMC = numberDMC - numberUse
		
		-- nếu đủ số lượng thì add buff dạ minh châu
		if minusDMC <= 0 then
			-- adđ buff dạ minh châu thời gian tương ứng với hệ số phi phong 
			local durationTicks = 1000 * 3600
		
			player:AddBuffWithDuration(1413, 10, durationTicks, 1, true)
			-- thêm thông tin là dùng đủ ở map nào để ko cần cắn lại từ đầu
			player:SetMapHasBuff(mapID)
			numberUse = 0
			player:AddNotification("Bạn đã dùng đủ số lượng Dạ Minh Châu và có buff Dạ Minh Châu! Trong thời gian có hiệu lực, vui lòng không thay đổi cấp độ tài phú")
			
			
		else
			player:AddNotification(string.format("Bạn đã dùng %d Dạ Minh Châu, bạn cần %d viên nữa để giải độc trong Tần Lăng!", numberUse, minusDMC))
		end
		-- xóa vật phẩm
		Player.RemoveItem(player,item:GetID())
		
		player:SetNumberDMCUsed(numberUse)
		
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Sử dụng thành công!")
		dialog:Show(item, player)
		return
	end
	
	if selectionID == 3 then
		-- check xem đang ở bản đồ nào? có phải đang trong tần lăng hay ko?
		local mapID = player:GetMapCode()
		if (mapID < 1536 or mapID > 1541) then
			player:AddNotification("Bản đồ hiện tại không thể sử dụng vật phẩm này")
			GUI.CloseDialog(player)
			return;
		end
		-- kiểm tra nếu có buff r thì ko cho cắn nữa
		if player:HasBuff(1413) then
			player:AddNotification("Bạn đã dùng đủ số lượng Dạ Minh Châu và có buff Dạ Minh Châu!")
			GUI.CloseDialog(player)
			return;
		end
		
		local numberUse = player:GetNumberDMCHasUsed()
		if numberUse < 0 then
			numberUse = 0
		end
		-- số lượng DMC cần để có buff
		local numberDMC = player:GetMultiplyNightPearl()
		
		local needUse = numberDMC - numberUse
		if needUse > 10 then
			needUse = 10
		end
				
		-- lấy số lượng trong túi
		local in_bag = Player.CountItemInBag(player, item.GetItemID())
		
		if in_bag < needUse then
			needUse = in_bag
		end
		
		numberUse = numberUse + needUse
		
		local minusDMC = numberDMC - numberUse
		
		-- nếu đủ số lượng thì add buff dạ minh châu
		if minusDMC <= 0 then
			-- adđ buff dạ minh châu thời gian tương ứng với hệ số phi phong 
			local durationTicks = 1000 * 3600
		
			player:AddBuffWithDuration(1413, 10, durationTicks, 1, true)
			-- thêm thông tin là dùng đủ ở map nào để ko cần cắn lại từ đầu
			player:SetMapHasBuff(mapID)
			numberUse = 0
			player:AddNotification("Bạn đã dùng đủ số lượng Dạ Minh Châu và có buff Dạ Minh Châu! Trong thời gian có hiệu lực, vui lòng không thay đổi cấp độ tài phú")
			
			
		else
			player:AddNotification(string.format("Bạn đã dùng %d Dạ Minh Châu, bạn cần %d viên nữa để giải độc trong Tần Lăng!", numberUse, minusDMC))
		end
		-- xóa vật phẩm
		Player.RemoveItem(player,item:GetItemID(), needUse)
		
		player:SetNumberDMCUsed(numberUse)
		
		local dialog = GUI.CreateItemDialog()
		dialog:AddText("Sử dụng thành công!")
		dialog:Show(item, player)
		return
		
	end
end