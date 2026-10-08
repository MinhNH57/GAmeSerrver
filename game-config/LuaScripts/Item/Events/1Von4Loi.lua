local MotVonBonLoi = Scripts[200126]

local RECORD_ID = 5000113
local BOXES_CONFIG = { 200376, 200377, 200378, 200379 }

function MotVonBonLoi:OnPreCheckCondition(scene, item, player, otherParams)
	return true
end

function MotVonBonLoi:OnUse(scene, item, player, otherParams)
	if item:IsExpired() then
		GUI:ShowMessageBox(player, 'Thông báo', 'Vật phẩm đã hết hạn sử dụng!')
		return
	end

	local dialog = GUI:CreateItemDialog()

	dialog:AddText("Rương vật phẩm một vốn bốn lời.")
	dialog:AddText("")
	dialog:AddText("Quy tắc sử dụng:")
	dialog:AddText("\t- Mỗi ngày có thể mở rương <color=#a6e3a1>1 lần</color>. Một rương có thể mở <color=#a6e3a1>tối đa 4 lần</color>.")
	dialog:AddText("\t- Sau khi mở rương lần đầu tiên, vật phẩm sẽ được kích hoạt hạn sử dụng cho đến hết tuần hiện tại, cụ thể là đến <color=#a6e3a1>" .. System:GetWeekEndDateTime() .. "</color>.")
	dialog:AddText("\t- Sau khi <color=#a6e3a1>mở rương lần thứ 4</color> hoặc <color=#a6e3a1>hết hạn sử dụng</color>, vật phẩm sẽ được xóa.")
	dialog:AddText("")
	dialog:AddText("Thông tin vật phẩm:")
	local expiry_date = item:GetExpiryDate()
	local is_awared_today = Player:GetValueOfDailyRecore(player, RECORD_ID)
	local awarded_milestone = Player:GetValueWeeklyRecore(player, RECORD_ID)
	
	if expiry_date == "1900-01-01 12:00:00" then
		dialog:AddText("\t- Vật phẩm chưa được kích hoạt.")
	else
		dialog:AddText("\t- Hạn sử dụng: <color=#a6e3a1>" .. expiry_date .. "</color>.")	
		if is_awared_today == -1 then
			dialog:AddText("\t- Hôm nay <color=#a6e3a1>chưa nhận</color> phần thưởng.")
		else 
			dialog:AddText("\t- Hôm nay <color=#a6e3a1>đã nhận</color> phần thưởng.")
		end
		if awarded_milestone ~= -1 then
			dialog:AddText("\t- Tuần này đã nhận thưởng <color=#a6e3a1>" .. awarded_milestone .. "/4</color> lần.")
		end
	end
	
	if is_awared_today == -1 then
		dialog:AddSelection(1, "Mở rương")

		if awarded_milestone == -1 then
			awarded_milestone = 0
		end
		local itemsToPreview = System:GetRandomBoxPreview(player, BOXES_CONFIG[awarded_milestone + 1])
		if itemsToPreview ~= nil then
			dialog:AddText("")
			dialog:AddText("Hôm nay sẽ được nhận các vật phẩm sau:")
			dialog:SetItemSelectable(false)

			for k, v in pairs(itemsToPreview) do
				if k == 11 then
					-- tinh hoạt lực cộng thẳng. đổi lại id để hiển thị
					dialog:AddItem(5, v, 0)
					dialog:AddItem(6, v, 0)
				else
					dialog:AddItem(k, v, 0)
				end
			end
		end
	end
	dialog:AddSelection(999, "Để ta suy nghĩ thêm")

	if player:IsGM() then
		dialog:AddSelection(9999, "[GM] Reset lượt mở trong ngày")
		dialog:AddSelection(9998, "[GM] Reset toàn bộ")
	end
	
	dialog:Show(item, player)
	-- ************************** --

end

function MotVonBonLoi:OpenChest(scene, item, player)
	local expiry_date = item:GetExpiryDate()
	local awarded_milestone = Player:GetValueWeeklyRecore(player, RECORD_ID)

	if expiry_date == "1900-01-01 12:00:00" and awarded_milestone ~= -1 then
		GUI:ShowMessageBox(player, 'Thông báo', 'Tuần này đã mở rương vật phẩm khác, không thể sử dụng vật phẩm này!')
		return
	end

	-- bắt đầu đếm ngược từ thời điểm mở rương đầu tiên cho đến cuối tuần
	local duration = System:GetMinutesFromNowToWeekEnd()
	System:WriteToConsole(duration)
	System:StartCountdownGood(player, item, duration)

	-- kiểm tra xem hôm nay đã nhận thưởng chưa
	local is_awared_today = Player:GetValueOfDailyRecore(player, RECORD_ID)
	if is_awared_today ~= -1 then
		GUI:ShowMessageBox(player, 'Thông báo', 'Hôm nay bằng hữu đã nhận thưởng rồi. Hãy quay lại vào ngày mai nhé!')
		return
	end

	-- kiểm tra xem đã nhận đến mốc nào rồi
	local awarded_milestone = Player:GetValueWeeklyRecore(player, RECORD_ID)
	if awarded_milestone < 0 then
		awarded_milestone = 0
	end

	-- tăng mốc thêm 1
	awarded_milestone = awarded_milestone + 1

	if awarded_milestone < 1 or awarded_milestone > 4 then
		GUI:ShowMessageBox(player, 'Thông báo', 'Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên!')
		return
	end

	if Player:OpenRandomBoxByConfigID(player, item, BOXES_CONFIG[awarded_milestone]) then
		-- mở rương thành công, cập nhật record
		Player:SetValueOfDailyRecore(player, RECORD_ID, 1)
		Player:SetValueOfWeekRecore(player, RECORD_ID, awarded_milestone)

		if awarded_milestone == 4 then
			-- nếu đã phát thưởng đủ 4 lần thì xóa vật phẩm
			Player:RemoveItem(player, item:GetID())
		end
	end	

	GUI:CloseDialog(player)
end


function MotVonBonLoi:OnSelection(scene, item, player, selectionID, otherParams)
	if selectionID == 1 then
		MotVonBonLoi:OpenChest(scene, item, player)
		return
	end

	if selectionID == 9999 then
		Player:SetValueOfDailyRecore(player, RECORD_ID, -1)
		GUI:CloseDialog(player)
		return
	end

	if selectionID == 9998 then
		Player:SetValueOfDailyRecore(player, RECORD_ID, -1)
		Player:SetValueOfWeekRecore(player, RECORD_ID, -1)
		GUI:CloseDialog(player)
		return
	end

	if selectionID == 999 then
		GUI:CloseDialog(player)
		return
	end
end


function MotVonBonLoi:OnItemSelected(scene, item, player, itemID)
end