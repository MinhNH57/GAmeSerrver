-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local CrystalBag = Scripts[206969]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function CrystalBag:OnPreCheckCondition(scene, item, player, otherParams)
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
function CrystalBag:OnUse(scene, item, player, otherParams)

    local dialog = GUI.CreateItemDialog()
    dialog:AddText("Rút hay cất huyền tinh?")

    dialog:AddSelection(101, "Rút huyền tinh")
    dialog:AddSelection(102, "Cất huyền tinh") 
    dialog:AddSelection(100, "Ta chưa cần")

    dialog:Show(item, player)

end

function Split(str, sep)
    local result = {}
    for match in string.gmatch(str, "([^" .. sep .. "]+)") do
        table.insert(result, match)
    end
    return result
end

function GetCrystalLevel(crystalID)
    return crystalID - 182
end


-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi vật phẩm thông qua Item Dialog
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function CrystalBag:OnSelection(scene, item, player, selectionID, otherParams)
    if (selectionID == 101) then
        -- local dialog = GUI.CreateItemDialog()
        -- local crystalBagData = player:GetRoleParamsDecodedStringWithDB("CrystalBag_Store")

        -- if crystalBagData and crystalBagData ~= "" then
        --     local crystalBagDataTable = Split(crystalBagData, ":")
        --     if #crystalBagDataTable ~= 0 then
        --         dialog:AddText("Rút huyền tinh cấp mấy?")

        --         for i = 1, #crystalBagDataTable do
        --             local crystal = Split(crystalBagDataTable[i], ";")

        --             if #crystal >= 3 then
        --                 local crystalID = tonumber(crystal[1]) or 0
        --                 local crystalCount = tonumber(crystal[2]) or 0
        --                 local crystalBinding = tonumber(crystal[3]) or 0
                        
        --                 local selectionId = crystalID * 10 + crystalBinding
        --                 local bindingText = crystalBinding == 1 and "(Khóa)" or "(Không khóa)"
                        
        --                 dialog:AddSelection(selectionId, string.format(
        --                     "Rút huyền tinh cấp %d %s (Số lượng: %d)", 
        --                     GetCrystalLevel(crystalID), 
        --                     bindingText, 
        --                     crystalCount
        --                 ))
        --             end
        --         end
        --     end
        -- else
        --     dialog:AddText("Không có huyền tinh trong túi.")
        -- end

        -- dialog:Show(item, player)
        GUI.OpenUI(player, "UICrystalBag_Withdraw")
        GUI.CloseDialog(player)
    elseif selectionID == 102 then
        GUI.OpenUI(player, "UICrystalBag_Store")
        GUI.CloseDialog(player)
    -- elseif (selectionID >= 1830 and selectionID <= 1941) then
        -- local crystalBagData = player:GetRoleParamsDecodedStringWithDB("CrystalBag_Store")
        -- local crystalBagDataTable = Split(crystalBagData, ":")
        -- local updatedData = {}

        -- local crystalID = math.floor(selectionID / 10)
        -- local targetBinding = selectionID % 10

        -- GUI:ShowInputNumber(player, "Nhập số huyền tinh muốn rút", function(value)
        --     if not value or value <= 0 then
        --         Player.ShowNotification(player, "Số lượng không hợp lệ")
        --         return
        --     end

        --     for i = 1, #crystalBagDataTable do
        --         local crystal = Split(crystalBagDataTable[i], ";")

        --         local currentCrystalID = tonumber(crystal[1]) or 0
        --         local crystalCount = tonumber(crystal[2]) or 0
        --         local crystalBinding = tonumber(crystal[3]) or 0

        --         if currentCrystalID == crystalID and crystalBinding == targetBinding then
        --             if value > crystalCount then
        --                 Player.ShowNotification(player, "Không đủ huyền tinh để rút")
        --                 return
        --             end
        --             crystalCount = crystalCount - value
        --             if not Player.AddItemLuaAndNotify(player, currentCrystalID, value, -1, crystalBinding) then
        --                 Player.ShowNotification(player, "Không đủ chỗ trống trong túi")
        --                 return
        --             end
        --         end
                
        --         if crystalCount > 0 then
        --             table.insert(updatedData, string.format("%d;%d;%d", currentCrystalID, crystalCount, crystalBinding))
        --         end
        --     end

        --     local updatedCrystalBagData = table.concat(updatedData, ":")
        --     player:SaveRoleParamsEncodedStringToDB("CrystalBag_Store", updatedCrystalBagData)

        --     GUI.CloseDialog(player)
        -- end)
    else
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
function CrystalBag:OnItemSelected(scene, item, player, itemID)

    -- ************************** --

    -- ************************** --

end
