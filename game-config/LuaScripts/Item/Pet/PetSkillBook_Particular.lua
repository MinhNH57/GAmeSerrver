local PetSkillBook_Particular = Scripts[200945]

local CONFIG = {
	[1] = {
		MIN_NUMBER_SKILLS = 1,
		MAX_NUMBER_SKILLS = 6,
		SUCCESS_RATEs = {
			[40] = 10,
			[80] = 20,
			[100] = 30,
			[120] = 40
		}
	},
	[2] = {
		MIN_NUMBER_SKILLS = 1,
		MAX_NUMBER_SKILLS = 6,
		SUCCESS_RATEs = {
			[30] = 10,
			[60] = 15,
			[80] = 20,
			[100] = 25,
			[120] = 30
		}
	}
}

function PetSkillBook_Particular:OnPreCheckCondition(scene, item, player, otherParams)
	return true
end

function PetSkillBook_Particular:OnUse(scene, item, player, otherParams)
	local bookLevel = item:GetItemLevel()
	local minNumberSkillRequired = CONFIG[bookLevel].MIN_NUMBER_SKILLS
	local maxNumberSkillRequired = CONFIG[bookLevel].MAX_NUMBER_SKILLS
	local dialog = GUI:CreateItemDialog()

	dialog:AddText("Sách kỹ năng đồng hành có tỉ lệ giúp bạn đồng hành hiện tại nhận được thêm một kỹ năng mới.")
	if minNumberSkillRequired == maxNumberSkillRequired then
		dialog:AddText("Chỉ có bạn đồng hành " .. minNumberSkillRequired .. " kỹ năng hoặc Yến Tiểu Lâu mới có thể sử dụng.")
	else 
		dialog:AddText("Chỉ có bạn đồng hành có số kỹ năng trong khoảng từ " .. minNumberSkillRequired .. " đến " .. maxNumberSkillRequired .. " hoặc Yến Tiểu Lâu mới có thể sử dụng.")
	end
	
	dialog:AddText("")
	
	local hasValidPet = false
	local pets = Pet:GetPets(player, minNumberSkillRequired, maxNumberSkillRequired, true)
	if pets == nil then
		dialog:AddText("Bằng hữu không có bạn đồng hành thỏa mãn điều kiện học kỹ năng mới.")
		dialog:Show(item, player)
		return
	end

    for k, v in ipairs(pets) do
		hasValidPet = true
		dialog:AddSelection(100 + k, v:GetName() .. ' [' .. v:GetEnhanceLevel() .. '] ' )
    end

	dialog:AddText("Hãy lựa chọn bạn đồng hành.")
	
	dialog:Show(item, player)
	-- ************************** --

end

function PetSkillBook_Particular:AddSkill(player, book, pet)
	local skillID = book:GetItemValue() -- id của skill muốn add, nếu 0 thì là random
	if skillID == 0 then
		skillID = -1
	end

	if not Pet:CanLearnNewSkill(pet, skillID) then
		if skillID == -1 then
			GUI:ShowMessageBox(player, "Đồng hành", "Bạn đồng hành đã có đủ 10 kỹ năng, không thể học thêm kỹ năng mới.")
		else
			GUI:ShowMessageBox(player, "Đồng hành", "Bạn đồng hành không thể học thêm kỹ năng " .. System:GetSkillName(skillID) .. '.')
		end
		return
	end

	local dialogMessage = "Xác nhận sử dụng " .. book:GetName() .. "?"

	GUI:ShowMessageBox(player, "Đồng hành", dialogMessage,
		function() 
			local bookLevel = book:GetItemLevel() -- cấp độ sách, dùng để tính rate
			System:WriteToConsole(bookLevel)
			local config_rate = CONFIG[bookLevel].SUCCESS_RATEs
			local success_rate = 0
			
			local petLevel = pet:GetEnhanceLevel()
			if petLevel <= 40 then
				success_rate = config_rate[40]
			elseif petLevel <= 80 then
				success_rate = config_rate[80]
			elseif petLevel <= 100 then
				success_rate = config_rate[100]
			else
				success_rate = config_rate[120]
			end

			if (Pet:GetStarsRank(pet) >= 7 and pet:GetItemID() ~= 25184) then
				success_rate = 0
			end
				
			System:WriteToConsole(success_rate)
			
			local POOL_SIZE = 100000
			local random = math.random(1, POOL_SIZE)

			if random > POOL_SIZE / 100 * success_rate then
				GUI:ShowMessageBox(player, "Đồng hành", "Thêm kỹ năng đồng hành thất bại.")
				Player:RemoveItem(player, book:GetID())
				return
			end
			Player:RemoveItem(player, book:GetID())
			local result = Pet:AddNewSkillToPet(player, pet, skillID)
			if result > 0 then
				GUI:ShowMessageBox(player, "Đồng hành", "Thêm kỹ năng " .. System:GetSkillName(result) .. " thành công.")
			else
				GUI:ShowMessageBox(player, "Đồng hành", "Thêm kỹ năng đồng hành thất bại.")
			end
		end,
		function()
			GUI:CloseDialog(player)
		end
	)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function PetSkillBook_Particular:OnSelection(scene, item, player, selectionID, otherParams)
	local bookLevel = item:GetItemLevel()

	GUI:CloseDialog(player)

	if selectionID >= 100 and selectionID <= 110 then
		-- lựa chọn bạn đồng hành để add kỹ năng
		local pets = Pet:GetPets(player, CONFIG[bookLevel].MIN_NUMBER_SKILLS, CONFIG[bookLevel].MAX_NUMBER_SKILLS, true)
		local selectedPet = pets[selectionID - 100]
		PetSkillBook_Particular:AddSkill(player, item, selectedPet)
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
function PetSkillBook_Particular:OnItemSelected(scene, item, player, itemID)

	-- ************************** --

	-- ************************** --

end