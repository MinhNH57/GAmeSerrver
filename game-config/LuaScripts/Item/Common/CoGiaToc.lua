-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local CoGiaToc = Scripts[200098]

local DurationFlag = 60*5
local RangeFlag = 1500
local TimePerTick = 10
local ExpPerTick = 1500
-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function CoGiaToc:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> CoGiaToc:OnPreCheckCondition")--
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
function CoGiaToc:OnUse(scene, item, player, otherParams)

	local familyRank = player.GetFamilyRank();
	
	if(familyRank == Global_FamilyRank.Master) then
		local dialog = GUI.CreateItemDialog()

		dialog:AddText("Bạn có xác nhận sẽ cắm cờ gia tộc tại đây không?")

		dialog:AddSelection(1, "Cắm cờ gia tộc")
		dialog:AddSelection(2, "Để ta suy nghĩ đã")

		dialog:Show(item, player)
	else
		player:AddNotification("Chỉ có tộc trưởng mới có thể cắm cờ gia tộc");
	end

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function CoGiaToc:OnSelection(scene, item, player, selectionID, otherParams)

	if selectionID == 1  then
		local MapID = scene:GetID()
		Player.RemoveItem(player, item:GetID())

		local ExpEarn = ExpPerTick;

		if Global_IsIn304Event() then
			ExpEarn = ExpPerTick * 2
		end
		Player.PutFamilyFlag(player, MapID, RangeFlag, DurationFlag, TimePerTick, ExpEarn, item:GetID())
        
		GUI.CloseDialog(player)
		
    elseif selectionID == 2 then
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
function CoGiaToc:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end