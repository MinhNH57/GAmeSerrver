-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200003' bên dưới thành ID tương ứng
local GoToNPC = Scripts[200121]

local DialogInfo = {
    -- [20110] = {
    --     Text = "Vật phẩm làm bánh Trung thu. Tới gặp Cổ Phong Hà để sử dụng.",
    --     NPCName = "Tới gặp Cổ Phong Hà",
    --     NPCID = 3601,
    --     MapId = -1
    -- },
    -- [20111] = {
    --     Text = "Vật phẩm làm bánh Trung thu. Tới gặp Cổ Phong Hà để sử dụng.",
    --     NPCName = "Tới gặp Cổ Phong Hà",
    --     NPCID = 3601,
    --     MapId = -1
    -- },
    -- [20112] = {
    --     Text = "Vật phẩm làm bánh Trung thu. Tới gặp Cổ Phong Hà để sử dụng.",
    --     NPCName = "Tới gặp Cổ Phong Hà",
    --     NPCID = 3601,
    --     MapId = -1
    -- },
    -- [20113] = {
    --     Text = "Vật phẩm làm bánh Trung thu. Tới gặp Cổ Phong Hà để sử dụng.",
    --     NPCName = "Tới gặp Cổ Phong Hà",
    --     NPCID = 3601,
    --     MapId = -1
    -- },
    -- [20108] = {
    --     Text = "Vật phẩm sự kiện Trung thu. Dùng để tham gia hoạt động Thả đèn hoa đăng hoặc đổi vật phẩm tại NPC Cổ Phong Hà.",
    --     NPCName = "Đi thả đèn hoa đăng",
    --     NPCID = 20159,
    --     MapId = 5
    -- },
    [506] = {
        Text = "Vật phẩm dùng để nâng cao hiệu quả thuộc tính của Ngũ Hành Ấn. Sử dụng tại Dã Luyện Đại Sư.",
        NPCName = "Tới gặp Dã Luyện Đại Sư",
        NPCID = 3574,
        MapId = -1
    },
    [20211] = {
        Text = "Vật dụng câu cá. Tới gặp Ông già câu cá để được hướng dẫn.",
        NPCName = "Tới gặp Ông già câu cá",
        NPCID = 20160,
        MapId = 5
    },
    [20212] = {
        Text = "Vật dụng câu cá. Tới gặp Ông già câu cá để được hướng dẫn.",
        NPCName = "Tới gặp Ông già câu cá",
        NPCID = 20160,
        MapId = 5
    },
    [20213] = {
        Text = "Vật dụng câu cá. Tới gặp Ông già câu cá để được hướng dẫn.",
        NPCName = "Tới gặp Ông già câu cá",
        NPCID = 20160,
        MapId = 5
    },
    [20214] = {
        Text = "Vật dụng câu cá. Tới gặp Ông già câu cá để được hướng dẫn.",
        NPCName = "Tới gặp Ông già câu cá",
        NPCID = 20160,
        MapId = 5
    }
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function GoToNPC:OnPreCheckCondition(scene, item, player, otherParams)

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
function GoToNPC:OnUse(scene, item, player, otherParams)
    local dialogInfo = DialogInfo[item:GetItemID()]
    System:WriteToConsole(item:GetItemID())
    if dialogInfo == nil then
        return
    end

    local dialog = GUI.CreateItemDialog()
    dialog:AddText(dialogInfo.Text)

    if player:GetClientVer() > 33 then
        dialog:AddSelection(1, dialogInfo.NPCName)
        if item:GetItemID() == 20108 then
            dialog:AddSelection(20108, "Tới gặp Cổ Phong Hà")
        end        
    end

	dialog:AddSelection(999, "Để sau...")
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
function GoToNPC:OnSelection(scene, item, player, selectionID, otherParams)
    GUI.CloseDialog(player)
	-- ************************** --
	if selectionID == 999 then
		return
	end
	
	if selectionID == 1 then
		local dialogInfo = DialogInfo[item:GetItemID()]
        if dialogInfo == nil then
            return
        end

        System:GoToNPC(player, dialogInfo.NPCID, dialogInfo.MapId)
        return
	end

    if selectionID == 20108 then
        System:GoToNPC(player, 3601, -1)
        return
    end
end