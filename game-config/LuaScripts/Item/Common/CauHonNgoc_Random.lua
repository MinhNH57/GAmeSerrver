-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi    Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200034' bên dưới thành ID tương ứng
local CauHonNgoc_Random = Scripts[200100]

-- ************************** --
local CUSTOM_BOSS = {
	-- [1] = {22608, 22609, 22610, 22611, 22612},
	[2] = {
		{ ID = 22613, Name = "Cao thủ Võ lâm [75] - Nam Đế"},
		{ ID = 22614, Name = "Cao thủ Võ lâm [75] - Tây Độc"},
		{ ID = 22615, Name = "Cao thủ Võ lâm [75] - Đông Tà"},
		{ ID = 22616, Name = "Cao thủ Võ lâm [75] - Bắc Cái"},
		{ ID = 22617, Name = "Cao thủ Võ lâm [75] - Vương Trùng Dương"}
	},
	[3] = {
		{ ID = 22608, Name = "Cao thủ Võ lâm [95] - Nam Đế"},
		{ ID = 22609, Name = "Cao thủ Võ lâm [95] - Tây Độc"},
		{ ID = 22610, Name = "Cao thủ Võ lâm [95] - Đông Tà"},
		{ ID = 22611, Name = "Cao thủ Võ lâm [95] - Bắc Cái"},
		{ ID = 22612, Name = "Cao thủ Võ lâm [95] - Vương Trùng Dươngg"}
	}
}
-- ************************** --

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function CauHonNgoc_Random:OnPreCheckCondition(scene, item, player, otherParams)

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
function CauHonNgoc_Random:OnUse(scene, item, player, otherParams)

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Trong phụ bản <color=yellow>Vượt ải gia tộc</color>, sau khi hoàn thành tiêu diệt Boss, có thể sử dụng <color=green>Câu Hồn Ngọc</color> để triệu hồi <color=orange>Võ lâm cao thủ</color>.<br><color=green>Câu Hồn Ngọc (sơ)</color> có thể triệu hồi <color=orange>Võ lâm cao thủ cấp 55</color><br><color=green>Câu Hồn Ngọc (trung)</color> có thể triệu hồi <color=orange>Võ lâm cao thủ cấp 75</color><br><color=green>Câu Hồn Ngọc (cao)</color> có thể triệu hồi <color=orange>Võ lâm cao thủ cấp 95</color>")
	dialog:AddSelection(1, "Triệu hồi Võ lâm cao thủ")
    dialog:AddSelection(1000, "Ta suy nghĩ đã")
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
function CauHonNgoc_Random:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	if selectionID == 1000 then
		GUI.CloseDialog(player)
		return
	end
	-- ************************** --
	if selectionID == 1 then
		if player:CheckSafeArea() == true then
			self:ShowDialog(item, player, "Bạn đang ở khu vực an toàn, không thể sử dụng vật phẩm!")
			return;
		end

		-- không cho thả boss ở phụ bản
		local sceneID = scene:GetID()

		if scene:IsCopyScene() then
			self:ShowDialog(item, player, "Bản đồ không hỗ trợ vật phẩm!")
			return
		end

		local itemLevel = item:GetItemLevel()

		if CUSTOM_BOSS[itemLevel] == nil then
			player:AddNotification("Vật phẩm bị lỗi. Vui lòng thử lại sau.")
			GUI.CloseDialog(player)
			return
		end

		local builder = EventManager:CreateMonsterBuilder(scene)
		local randomBoss = CUSTOM_BOSS[itemLevel][math.random(1, 5)]
		System:WriteToConsole("Spawn boss id " .. randomBoss.ID)
		builder.SetResID(randomBoss.ID)
		builder.SetPos(player:GetPos())
		builder.SetAIType(3) -- set là boss
		builder.SetAIScriptID(100001) -- set sử dụng bộ kỹ năng khai báo trong Monsters.xml
		if builder.build() then
			System:NotifyAll(player:GetName() .. " đã sử dụng vật phẩm gọi ra cao thủ võ lâm " .. randomBoss.Name .. ".")
			-- Xóa vật phẩm
			Player.RemoveItem(player, item:GetID())  
			GUI.CloseDialog(player)
		else
			player:AddNotification("Vật phẩm bị lỗi. Vui lòng thử lại sau.")
			GUI.CloseDialog(player)
			return
		end
	end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function CauHonNgoc_Random:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end


-- ======================================================= --
-- ======================================================= --
function CauHonNgoc_Random:ShowDialog(item, player, text)

	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	dialog:AddText(text)
	dialog:AddSelection(1000, "Kết thúc đối thoại")
	dialog:Show(item, player)
	-- ************************** --
	
end