local ChanLongBaoRuong = Scripts[200124]

function ChanLongBaoRuong:OnPreCheckCondition(scene, item, player, otherParams)
	return true
end

function ChanLongBaoRuong:OnUse(scene, item, player, otherParams)
	local dialog = GUI:CreateItemDialog()

	dialog:AddText("Chân Long Bảo Rương chứa các trang bị Chân Long.")
	
	dialog:AddSelection(1, "Mở rương")
	dialog:AddSelection(999, "Để ta suy nghĩ thêm")

	if player:IsGM() then
		dialog:AddSelection(9999, "[GM] Reset")
	end
	
	dialog:Show(item, player)
	-- ************************** --

end


function ChanLongBaoRuong:OnSelection(scene, item, player, selectionID, otherParams)
	GUI:CloseDialog(player)
	if selectionID == 99 then
		return
	end

	if selectionID == 9999 then
		System:SetGlobalGamePreference('chanlongbaoruong', 0)
		return
	end

	if selectionID == 1 then
		local chest_opened_all_server = System:GetGlobalGamePreference_Int('chanlongbaoruong')
		local randombox_config = 200214
		if chest_opened_all_server >= 5 then 
			randombox_config = 200216
		end

		if Player:OpenRandomBoxByConfigID(player, item, randombox_config) then
			Player:RemoveItem(player, item:GetID())
			System:SetGlobalGamePreference('chanlongbaoruong', chest_opened_all_server + 1)
		end		
	end
end


function ChanLongBaoRuong:OnItemSelected(scene, item, player, itemID)
end