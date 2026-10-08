-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000060' bên dưới thành ID tương ứng 
local QuanBBR = Scripts[000230]

function QuanBBR:OnOpen(scene, npc, player, otherParams)

    local dialog = GUI.CreateNPCDialog()

    dialog:AddText(
        "Hoạt động <color=#a6e3a1>Vòng quay May mắn Đại Lễ 2024</color> diễn ra từ <color=#a6e3a1>5:00 9/5 đến 5:00 17/5</color>.")
    dialog:AddText("")
    dialog:AddText("Sử dụng <color=#a6e3a1>Xu Đại Lễ</color> để tham gia quay vòng quay may mắn và nhận được <color=#a6e3a1>Phiếu quà Đại Lễ</color> và nhiều phần thưởng giá trị. <color=#a6e3a1>Xu Đại Lễ</color> có thể mua tại Kỳ Trân Các.")
    dialog:AddText("")
    dialog:AddText("Với mỗi <color=#a6e3a1>50 KNB</color> được nạp, bằng hữu sẽ nhận được <color=#a6e3a1>1 Xu Đại Lễ</color> miễn phí.")
    dialog:AddText("")
    dialog:AddText("<color=#a6e3a1>Phiếu quà Đại Lễ</color> có thể đổi được nhiều phần thưởng giá trị tại <color=#a6e3a1>Tiệm Quà Đại Lễ</color>.")

    dialog:AddSelection(1, "Vòng quay may mắn Đại Lễ 2024")
    dialog:AddSelection(2, "Tiệm quà Đại Lễ 2024")
    dialog:AddSelection(3, "Nhận xu Đại Lễ miễn phí")

    dialog:AddSelection(99, "Kết thúc đối thoại")
    dialog:Show(npc, player)

end

function QuanBBR:OnSelection(scene, npc, player, selectionID, otherParams)
    if selectionID == 2 then
        Player.OpenShop(npc, player, 1476)
		GUI.CloseDialog(player)
        return
    end
    if selectionID == 1 then
        GUI:CloseDialog(player)
        GUI:OpenSeashell(player)
        return
    end
    if selectionID == 3 then
        QuanBBR:ShowXu(scene, npc, player)
        return
    end
    if selectionID == 4 then
        QuanBBR:ReceiveXu(scene, npc, player)
        return
    end
    if selectionID == 99 then
        GUI:CloseDialog(player)
        return
    end
end

function QuanBBR:ShowXu(scene, npc, player)
    local totalXu = Player:GetTotalCharge(player, '2024-05-09 05:00:00', '2024-05-17 05:00:00')
    local xuReceived = Player:GetValueOfMonthRecord(player, 5000121)
    if xuReceived < 0 then
        xuReceived = 0
    end

    local xuToReceive = totalXu / 50 - xuReceived

    local dialog = GUI:CreateNPCDialog()
    dialog:AddText('Bằng hữu đã nạp <color=#a6e3a1>' .. totalXu .. ' KNB</color>, đã nhận <color=#a6e3a1>' .. xuReceived .. ' Xu Đại Lễ</color>. Có thể nhận thêm <color=#a6e3a1>' .. xuToReceive .. ' Xu</color>.')
    dialog:AddSelection(4, 'Nhận Xu Đại Lễ')
    dialog:Show(npc, player)
end

function QuanBBR:ReceiveXu(scene, npc, player)
    if not Player:HasFreeBagSpaces(player, 1) then
        GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm.')
        return
    end

    local totalXu = Player:GetTotalCharge(player, '2024-05-09 05:00:00', '2024-05-17 05:00:00')
    local xuReceived = Player:GetValueOfMonthRecord(player, 5000121)
    if xuReceived < 0 then
        xuReceived = 0
    end

    local xuToReceive = totalXu / 50 - xuReceived
    if xuToReceive <= 0 then 
        GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu đã nhận đủ số lượng xu. Hãy nạp thêm ADNXu để nhận thêm xu miễn phí.')
        return
    end
    Player:SetValueOfMonthRecord(player, 5000121, totalXu / 50)
    Player.AddItemLuaAndNotify(player, 20578, xuToReceive, 0, 1, 0,
                    '2024-05-17 05:00:00')

    QuanBBR:OnOpen(scene, npc, player, nil)

end