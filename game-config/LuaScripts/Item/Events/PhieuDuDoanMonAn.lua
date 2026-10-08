local PhieuDuDoanMonAn = Scripts[200129]

local RECORD = 5000130
local DAILY_LIMIT = 20

function PhieuDuDoanMonAn:OnPreCheckCondition(scene, item, player, otherParams)
	if not Player.HasFreeBagSpaces(player, 3) then
		GUI:ShowMessageBox(player, 'Thông báo', 'Cần 3 ô trống trong túi đồ để sử dụng vật phẩm!')
		return false
	end

	local usedToday = Player:GetValueOfDailyRecore(player, RECORD)
	if usedToday < 0 then
		usedToday = 0
	end

	if usedToday > DAILY_LIMIT then
		GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu đã tham gia tối đa ' .. DAILY_LIMIT .. ' lượt dự đoán hôm nay!')
		return false
	end

	return true
end

function PhieuDuDoanMonAn:OnUse(scene, item, player, otherParams)
	if item:IsExpired() then
		GUI:ShowMessageBox(player, 'Thông báo', 'Vật phẩm đã hết hạn sử dụng!')
		return
	end

	local dialog = GUI:CreateItemDialog()

	dialog:AddText("Phiếu Dự đoán Món ăn.")
	dialog:AddText("")
	dialog:AddText("Bằng hữu hãy dự đoán món ăn lần này. Dự đoán đúng sẽ nhận được:")
	dialog:AddText("\t- 10 Rương danh vọng [trung]")
	dialog:AddText("\t- 10 Rương mảnh trang bị đồng hành [cao]")
	dialog:AddText("\t- 100 Ngũ hành hồn thạch")

	dialog:AddText("")
	dialog:AddText("Đầu bếp luôn muốn chiều theo ý thích của khách hàng, nên xác suất dự đoán chính xác được tăng mạnh.")
	
	dialog:AddText("Thông tin vật phẩm:")

	dialog:AddItem(20666, 1, 0)
	dialog:AddItem(20667, 1, 0)
	dialog:AddItem(20668, 1, 0)
	dialog:AddItem(20669, 1, 0)
	dialog:AddItem(20670, 1, 0)

	dialog:Show(item, player)
	-- ************************** --

end

function PhieuDuDoanMonAn:OnSelection(scene, item, player, selectionID, otherParams)
	
end


function PhieuDuDoanMonAn:OnItemSelected(scene, item, player, itemSelection)
	GUI:CloseDialog(player)
    
    local itemID = itemSelection:GetItemID()
    local itemCount = itemSelection:GetItemCount()
    local binding = itemSelection:GetBinding()

	local rnd = math.random(1, 3)
	System:WriteToConsole(rnd)
	if rnd == 1 then
		GUI:ShowMessageBox(player, "Thông báo", "Chúc mừng bằng hữu đã dự đoán chính xác. Phần thưởng của bằng hữu đã được gửi vào túi đồ.")
		
		Player.AddItemLuaAndNotify(player, 20485, 10, 1, 1)
		Player.AddItemLuaAndNotify(player, 20529, 5, 1, 1)
		Player.AddItemLuaAndNotify(player, 506, 100, 1, 1)
	else
		GUI:ShowMessageBox(player, "Thông báo", "Rất tiếc, bằng hữu đã dự đoán chưa chính xác.")
	end

	Player.RemoveItem(player, item:GetItemID(), 1)

	local usedToday = Player:GetValueOfDailyRecore(player, RECORD)
	if usedToday < 0 then
		usedToday = 0
	end

	Player:SetValueOfDailyRecore(player, RECORD, usedToday + 1)
end