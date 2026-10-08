-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200034' bên dưới thành ID tương ứng

local TheDoiMonPhai = Scripts[200122]
local ITEM_RECORD = 5000013

local FACTION_COLOR = {
	[Global_FactionID.ShaoLin] = 'E1CB5B',
    [Global_FactionID.TianWang] = "E1CB5B",
    [Global_FactionID.TangMen] = "75E691",
    [Global_FactionID.WuDu] = "75E691",
    [Global_FactionID.EMei] = "75E4E6",
    [Global_FactionID.CuiYan] = "75E4E6",
    [Global_FactionID.GaiBang] = "F14848",
    [Global_FactionID.TianRen] = "F14848",
    [Global_FactionID.WuDang] = "B88518",
    [Global_FactionID.KunLun] = "B88518",
    [Global_FactionID.MingJiao] = "75E691",
    [Global_FactionID.DaLiDuanShi] = "75E4E6",
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function TheDoiMonPhai:OnPreCheckCondition(scene, item, player, otherParams)

	-- ************************** --
	--player:AddNotification("Enter -> TheDoiMonPhai:OnPreCheckCondition")--
	-- ************************** --
	return true
	-- ************************** --

end

function TheDoiMonPhai:JoinFaction(item, player, factionID) 
	GUI:ShowMessageBox(player, "Thông báo", "Xác nhận đổi sang phái " .. '<color=#' .. FACTION_COLOR[factionID] .. '>' .. Global_FactionName[factionID] .. '</color>' .. "?\nLưu ý: Sau khi thực hiện đổi phái sẽ bị mất kết nối, vui lòng kết nối lại để tiếp tục trò chơi.", 
		function ()
			local dialog = GUI:CreateItemDialog()

			local ret = player:JoinFaction(factionID)
			-- ************************** --
			if ret == -1 then
				dialog:AddText("Người chơi không tồn tại!")
			elseif ret == -2 then
				dialog:AddText("Môn phái không tồn tại!")
			elseif ret == 0 then
				dialog:AddText("Giới tính của bạn không phù hợp với môn phái này!")
			elseif ret == 1 then

				local mainFaction = player:IsHasSongTu()
				if mainFaction ~= -1 then
					-- lưu lại faction chính của người chơi
					player:SetHasSongTu(factionID)
				end

				dialog:AddText("Gia nhập <color=#" .. FACTION_COLOR[factionID] .. '>' .. player:GetFactionName() .. "</color> thành công!")
				Player:RemoveItem(player, item:GetID())
				Player:SetValueOfDailyRecore(player, ITEM_RECORD, 1)
			else
				dialog:AddText("Chuyển phái thất bại, lỗi chưa rõ!")
			end
			dialog:Show(item, player)
			Player:KickOut(player)
		end,
		function ()
			GUI:CloseDialog(player)
		end)
end

-- ****************************************************** --
--	Hàm này được gọi để thực thi Logic khi người sử dụng vật phẩm, sau khi đã thỏa mãn hàm kiểm tra điều kiện
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function TheDoiMonPhai:OnUse(scene, item, player, otherParams)
	GUI:CloseUI(player, "UIBag")

	local dialog = GUI.CreateItemDialog()
	dialog:AddText("Thẻ đổi môn phái sẽ giúp bằng hữu đổi môn phái gốc. Một ngày chỉ được đổi một lần. Yêu cầu cấp độ 50 trở lên. Sau khi đổi phái, điểm kỹ năng sẽ được làm mới.")
	dialog:AddText("")

	local usage = Player:GetValueOfDailyRecore(player, ITEM_RECORD)

	local currentFaction = player:GetFactionID()
	local mainFaction = player:IsHasSongTu()

	if usage == 1 then
		dialog:AddText("<color=#f38ba8>Hôm nay đã đổi môn phái rồi. Ngày mai hãy quay lại!</color>")
		dialog:AddSelection(99, "<color=#f38ba8>Ngày mai lại tới!</color>")
	elseif currentFaction ~= mainFaction and mainFaction ~= -1 then
		dialog:AddText("<color=#f38ba8>Bằng hữu cần phải đổi phụ tu về môn phái gốc trước!</color>")
		dialog:AddSelection(99, "<color=#f38ba8>Ta cần suy nghĩ thêm!</color>")
	elseif player:GetLevel() < 50 then
		dialog:AddText("<color=#f38ba8>Bằng hữu cần phải đạt level 50 mới có thể sử dụng vật phẩm này!</color>")
		dialog:AddSelection(99, "<color=#f38ba8>Ta sẽ quay lại sau!</color>")
	else
		dialog:AddText("<color=#a6e3a1>Hãy lựa chọn môn phái muốn đổi.</color>")
		for k, v in pairs(Global_FactionName) do
			if k > 0 and k ~= currentFaction then
				dialog:AddSelection(100 + k, '<color=#' .. FACTION_COLOR[k] .. '>' .. v .. '</color>')
			end
		end

		dialog:AddSelection(99, "<color=#f38ba8>Ta cần suy nghĩ thêm!</color>")
	end

	if player:IsGM() then
		dialog:AddSelection(999, "[GM] Reset lượt!")
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
function TheDoiMonPhai:OnSelection(scene, item, player, selectionID, otherParams)
	if selectionID > 100 and selectionID < 113 then
		local factionID = selectionID - 100
		TheDoiMonPhai:JoinFaction(item, player, factionID)
		return
	end

	if selectionID == 99 then
		GUI:CloseDialog(player)
		return
	end

	if selectionID == 999 then
		Player:SetValueOfDailyRecore(player, ITEM_RECORD, -1)
		GUI:CloseDialog(player)
		return
	end
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function TheDoiMonPhai:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end