-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '200005' bên dưới thành ID tương ứng
local PhieuKNB = Scripts[200127]

local MoneyType = 2
local TO_RETURN = {
    [205422] = 480,
    [245552] = 240,
    [351565] = 480,
    [223962] = 240,
    [210705] = 240,
    [352062] = 480,
    [240727] = 480,
    [226423] = 480,
    [199816454] = 400,
    [351991] = 240,
    [245715] = 480,
    [254563] = 240,
    [199851182] = 240,
    [1999827570] = 740,
    [273355] = 240,
    [199832941] = 240,
    [206956] = 240,
    [199832129] = 480,
    [208357] = 240,
    [223371] = 240,
    [202553] = 240,
    [254656] = 240,
    [351774] = 240,
    [1999867813] = 30,
    [1999866648] = 240,
    [1999843708] = 300,
    [1999819650] = 1680,
    [1999836602] = 300,
    [1999883175] = 90,
    [1999883146] = 740,
    [1999875924] = 1720,
    [1999883122] = 1940,
    [1999864853] = 1200,
    [1999854571] = 1200,
    [1999831917] = 1200,
    [1999843974] = 1510,
    [1800220051] = 1290,
    [1999882896] = 1440,
    [1999839616] = 240,
    [1999883145] = 740,
    [1999882982] = 240,
    [1999873750] = 1780,
    [1999851328] = 740,
    [1999873667] = 540,
    [1999884409] = 1240,
    [1800220423] = 240,
    [1999854557] = 480,
    [1999844636] = 1030,
    [1999830670] = 240,
    [1800203804] = 740,
    [1800216386] = 240,
    [1999858138] = 40,
    [1999901607] = 240,
    [1999901529] = 1500,
    [1999899432] = 2600,
    [1999901377] = 300,
    [1999899091] = 120,
    [1999901132] = 2100,
    [1999903538] = 5,
    [1999903990] = 40,
    [1999903383] = 8,
    [1999904621] = 40,
    [1999903449] = 40,
    [1999903491] = 10,
    [1999903956] = 16,
    [1999903552] = 32,
    [1999905988] = 40
}

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn sử dụng vật phẩm, kiểm tra điều kiện có được dùng hay không
--		scene: Scene - Bản đồ hiện tại
--		item: Item - Vật phẩm tương ứng
--		player: Player - NPC tương ứng
--	Return: True nếu thỏa mãn điều kiện có thể sử dụng, False nếu không thỏa mãn
-- ****************************************************** --
function PhieuKNB:OnPreCheckCondition(scene, item, player, otherParams)

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
function PhieuKNB:OnUse(scene, item, player, otherParams)
    if TO_RETURN[player:GetID()] == nil then
        GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu không được sử dụng vật phẩm này.")
        return
    end

    local amount = TO_RETURN[player:GetID()]
    -- ************************** --
    local dialog = GUI.CreateItemDialog()
    dialog:AddText("Bạn có chắc chắn muốn sử dụng " .. item:GetName() .. " để nhận được " ..
                       amount .. " KNB?")
    dialog:AddSelection(1, "Chắn chắn")
    dialog:AddSelection(2, "Ta suy nghĩ đã")
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
function PhieuKNB:OnSelection(scene, item, player, selectionID, otherParams)

    -- ************************** --
    if selectionID == 2 then
        GUI.CloseDialog(player)
        return
    end
    -- ************************** --
    if selectionID == 1 then
        -- Đóng khung

        GUI.CloseDialog(player)
        -- Lượng tiền nhận được
        local amount = TO_RETURN[player:GetID()]
        if amount == nil then
            GUI:ShowMessageBox(player, "Thông báo", "Bằng hữu không được sử dụng vật phẩm này.")
            return
        end
        -- Xóa vật phẩm
        Player.RemoveItem(player, item:GetID())
        -- Thêm tiền tương ứng
        Player.AddMoney(player, amount, MoneyType)
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
function PhieuKNB:OnItemSelected(scene, item, player, itemID)

    -- ************************** --

    -- ************************** --

end
