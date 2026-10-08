-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng

local TheDoiServer = Scripts[200951]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --

local listServer = {
	[1] = {
        NameServer = "Vân Trung Trấn"
    },
	[11] = {
        NameServer = "Dương Châu"
    },
	[25] = {
        NameServer = "Thiên Nhẫn"
    },
	[30] = {
        NameServer = "Đoàn Thị"
    },
	-- [32] = {
    --     NameServer = "Nhật Nguyệt Kiếm"
    -- },
	-- [33] = {
    --     NameServer = "Thanh Long"
    -- }
}

local listServerGH = {
	[1] = {
        NameServer = "Vân Trung Trấn"
    },
	[11] = {
        NameServer = "Dương Châu"
    }
}
local listServerTest = {
	[999] = {
        NameServer = "Test 999"
    },
	[998] = {
        NameServer = "Test 998"
    },
	[997] = {
        NameServer = "Test 997"
    },
	[996] = {
        NameServer = "Test 996"
    },
}

local listPlayerVIP = {1999927623}

function TheDoiServer:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> TheDoiServer:OnPreCheckCondition")--
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
function TheDoiServer:OnUse(scene, item, player, otherParams)

	-- ************************** --
	-- ************************** --
	local dialog = GUI.CreateItemDialog()
	
	local MapID = scene:GetID()
	local isKuafuMap = false
	for _, v in pairs(Global_KuafuMap) do
		if v == MapID then
			isKuafuMap = true
		end
	end
	if isKuafuMap then
		dialog:AddText("Bạn đang ở liên server không thể sử dụng thẻ chuyển Server")
		dialog:AddSelection(2, "Để sau...")
	else
		dialog:AddText("Bạn có muốn sử dụng thẻ chuyển Server?")
		dialog:AddText("Trước khi chuyển server bạn phải thoát khỏi gia tộc và bang hội hiện tại.")
		dialog:AddText("Toàn bộ vật phẩm không khóa và bạc không khóa sẽ bị xóa bỏ trước khi chuyển server.")
		dialog:AddSelection(1, "Sử dụng")
		dialog:AddSelection(2, "Để lần tới...")
	end
	
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
function TheDoiServer:OnSelection(scene, item, player, selectionID, otherParams)

	-- ************************** --
	
    if selectionID == 1 then		
		-- xét điều kiện của người chơi
		local checkDK = player:CheckChangeServer()
		if checkDK == -1 then
			player:AddNotification("Bạn cần đạt level 90 để đổi Server")
			GUI.CloseDialog(player)
			return
		elseif checkDK == -2 then
			player:AddNotification("Bạn cần thoát khỏi gia tộc trước khi đổi Server")
			GUI.CloseDialog(player)
			return
		elseif checkDK == -3 then
			player:AddNotification("Bạn cần thoát khỏi bang hội trước khi đổi Server")
			GUI.CloseDialog(player)
			return
		elseif checkDK == 1 then
			local dialog = GUI.CreateItemDialog()
			dialog:AddText("Chọn server")
			local serverOldID = System:GetServerID()
			if serverOldID > 99 then
				for k, v in pairs(listServerTest) do
					if (k ~= serverOldID) then
						dialog:AddSelection(1000 + k, "Đổi sang server [" .. k .. "] " .. v.NameServer .."")
					end
				end
			elseif serverOldID > 24 or TheDoiServer:ListContainsValue(listPlayerVIP, player:GetID()) then  
				for k, v in pairs(listServer) do
					if (k ~= serverOldID) then
						dialog:AddSelection(1000 + k, "Đổi sang server [" .. k .. "] " .. v.NameServer .."")
					end
				end
			else
				for k, v in pairs(listServerGH) do
					if (k ~= serverOldID) then
						dialog:AddSelection(1000 + k, "Đổi sang server [" .. k .. "] " .. v.NameServer .."")
					end
				end
			end	
			dialog:Show(item, player)
		end
    end
	
	if selectionID > 1000 then
		local serverNewID = selectionID - 1000
		player:ChangeServer(serverNewID)
		GUI.CloseDialog(player)
		return
	end
	
	if selectionID == 2 then
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
function TheDoiServer:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end

function TheDoiServer:ListContainsValue(list, value)
    if list ~= nil then
        local found = false
        for i, v in ipairs(list) do
            if v == value then
                return true
            end
        end
    end
    return false
end