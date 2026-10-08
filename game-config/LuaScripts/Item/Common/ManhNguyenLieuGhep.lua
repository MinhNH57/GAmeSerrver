local ManhNguyenLieu = Scripts[200128]

local CONFIGs = {
	[20601] = 141,
	[20595] = 100003
}

function ManhNguyenLieu:YenTieuLau()
	return true
end

function ManhNguyenLieu:OnPreCheckCondition(scene, item, player, otherParams)
	local itemID = item:GetItemID()
	local config = CONFIGs[itemID]
	if config == nil then
		GUI:ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng liên hệ quản trị viên.")
		return false
	end
	if itemID == 20601 then
		local petCount = Pet:GetNumberOfPets(player)
		if petCount >= 10 then
			GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu đã có đủ số lượng bạn đồng hành. Vui lòng giải tán bạn đồng hành trước khi sử dụng vật phẩm.")
			return false
		end
		return true
	end
	return true
end

function ManhNguyenLieu:OnUse(scene, item, player, otherParams)
	GUI:CloseUI(player, "UIBag")
	local itemID = item:GetItemID()
	local config = CONFIGs[itemID]
	GUI:OpenComposeDialog(player, "", config)
end


function ManhNguyenLieu:OnSelection(scene, item, player, selectionID, otherParams)
end


function ManhNguyenLieu:OnItemSelected(scene, item, player, itemID)
end