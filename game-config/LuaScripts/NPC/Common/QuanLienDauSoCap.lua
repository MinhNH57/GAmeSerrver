-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000020' bên dưới thành ID tương ứng
local QuanLienDauSoCap = Scripts[000021]

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
-- ****************************************************** --
function QuanLienDauSoCap:OnOpen(scene, npc, player, otherParams)

    -- ************************** --
    local szTeamBattleType = EventManager.TeamBattle_GetCurrentMonthTeamBattleType()
    -- ************************** --
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText(
        "Võ thuật là công cụ so tài của các vị anh hùng hào kiệt từ xưa đến nay, và tiếp tục lưu truyền ngàn đời về sau. <color=green>Võ lâm liên đấu</color> chính là đấu trường nơi các vị anh hùng hào kiệt xưng bá thiên hạ. Trong tháng này, loại hình võ lâm liên đấu là <color=yellow>" ..
            szTeamBattleType .. "</color>, các vị có thể tự do lập thành chiến đội thi đấu.")
    dialog:AddText("")
    dialog:AddText(
        "<color=orange>Thời gian đăng ký chiến đội:</color> từ ngày 1 đến ngày 4, yêu cầu lập tổ đội đủ số thành viên theo loại hình liên đấu, tối thiểu mỗi thành viên đủ 3000 tài phú mang theo phi phong Kinh Thế trở lên.")
    dialog:AddText("")
    dialog:AddText(
        "<color=orange>Thời gian nhận thưởng:</color> từ 26 đến 29 hàng tháng trong trạng thái tổ đội như khi đăng ký, nhận thưởng ở NPC Quan Liên Đấu Sơ.")
    dialog:AddText("")
    dialog:AddText(
        "<color=orange>Lưu ý:</color> <color=yellow>Bảng xếp hạng chiến đội</color> sẽ được <color=green>cập nhật</color> sau mỗi đợt <color=yellow>bảo trì hàng ngày</color>. Cuối tháng, chỉ các chiến đội có <color=yellow>tổng số trận</color> thi đấu <color=green>từ 30 trở lên</color> mới có thể được xếp hạng nhận thưởng, nhận thưởng sau 7h sáng.")

    -- Nếu đã đăng ký chiến đội
    if EventManager.TeamBattle_IsRegistered(player) == true then
        dialog:AddSelection(1, "Xem tình hình chiến đội bản thân")
        -- Nếu đang là thời gian đăng ký
    elseif EventManager.TeamBattle_IsRegisterTime() == true then
        dialog:AddSelection(2, "Báo danh chiến đội")
    end
    -- Nếu hôm nay có trận đấu
    if EventManager.TeamBattle_IsBattleTimeToday() == true then
        dialog:AddSelection(3, "Tới hội trường liên đấu")
    end

    -- Nếu có phần thưởng
    if EventManager.TeamBattle_IsHavingAwards(player) == true then
        dialog:AddSelection(4, "Nhận thưởng <color=green>Võ lâm liên đấu</color>")
    end

    if EventManager.TeamBattle_IsRegistered(player) and EventManager.TeamBattle_IsRegisterTime() == true then
        dialog:AddSelection(7, "Hủy chiến đội đã đăng ký trước đó")
    end

    dialog:AddSelection(5, "Xem bảng xếp hạng Top chiến đội")

    --dialog:AddSelection(6, "Mua trang bị danh vọng liên đấu")
    dialog:AddSelection(100, "Kết thúc đối thoại")
    dialog:Show(npc, player)
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
-- ****************************************************** --
function QuanLienDauSoCap:OnSelection(scene, npc, player, selectionID, otherParams)

    -- ************************** --
    if selectionID == 100 then
        GUI.CloseDialog(player)
        return
    end
    -- ************************** --
    if selectionID == 1 then
        local szTeamInfoText = EventManager.TeamBattle_GetMyTeamInfo(player)
        QuanLienDauSoCap:ShowNotify(npc, player, szTeamInfoText)
        return
    end
    -- ************************** --
    if selectionID == 2 then
        if System:GetDate() == 1 and System:GetHour() < 7 then
            GUI:ShowMessageBox(player, "Thông báo",
                "Báo danh chiến đội bắt đầu từ 7:00 ngày 1 hằng tháng.")
            return
        end
        -- Nếu không phải thời gian đăng ký
        if EventManager.TeamBattle_IsRegisterTime() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Hiện không phải thời gian đăng ký <color=green>Võ lâm liên đấu</color>, hãy quay lại vào tháng sau!")
            return
            -- Nếu chưa có nhóm
        elseif player:HasTeam() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Ngươi phải lập nhóm trước, sau đó mới có thể báo danh <color=green>Võ lâm liên đấu</color>.")
            return
            -- Nếu không phải nhóm trưởng
        elseif player:IsTeamLeader() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Ngươi không phải trưởng nhóm, chỉ có trưởng nhóm mới có thể báo danh <color=green>Võ lâm liên đấu</color>.")
            return
        end

        -- Mở bảng nhập tên nhóm
        GUI.ShowInputString(player,
            "Nhập tên <color=yellow>chiến đội</color> tham dự <color=green>Võ lâm liên đấu</color>",
            function(teamName)
                -- Thực hiện báo danh
                local szCreateTeamResult = EventManager.TeamBattle_CreateTeam(player, teamName)
                QuanLienDauSoCap:ShowNotify(npc, player, szCreateTeamResult)
            end)
        return
    end
    -- ************************** --
    if selectionID == 3 then
        -- Nếu hôm nay có trận đấu
        if EventManager.TeamBattle_IsBattleTimeToday() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Hôm nay không diễn ra <color=green>Võ lâm liên đấu</color>, hãy quay lại hôm khác!")
            return
            -- Nếu chưa có nhóm
        elseif player:HasTeam() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Ngươi phải lập nhóm trước, sau đó mới có thể báo danh trận đấu kế tiếp.")
            return
            -- Nếu không phải nhóm trưởng
        elseif player:IsTeamLeader() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Ngươi không phải trưởng nhóm, chỉ có trưởng nhóm mới có thể dẫn đội vào hội trường.")
            return
            -- Nếu chiến đội chưa đăng ký tham gia Võ lâm liên đấu
        elseif EventManager.TeamBattle_IsRegistered(player) == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Chiến đội chưa đăng ký tham gia <color=green>Võ lâm liên đấu</color> tháng này.")
            return
            -- Nếu party ko đúng số lượng ngưới chiến đội đã báo danh
        elseif EventManager.TeamBattle_CheckMembers(player) == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Chiến đội chưa đúng số lượng thánh viên đã đăng ký <color=green>Võ lâm liên đấu</color>")
            return
        elseif EventManager.TeamBattle_MoveToBattleHall(player) == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Có thành viên đang ở vị trí quá xa, yêu cầu ở cùng 1 map!")
            return
        end
    end
    -- ************************** --
    if selectionID == 4 then
        -- Nếu chưa có nhóm
        if player:HasTeam() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Ngươi phải lập nhóm và tập hợp thành viên nhóm tới chỗ ta mới có thể nhận thưởng <color=green>Võ lâm liên đấu</color>.")
            return
            -- Nếu không phải nhóm trưởng
        elseif player:IsTeamLeader() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Ngươi không phải trưởng nhóm, chỉ có trưởng nhóm mới có thể báo danh <color=green>Võ lâm liên đấu</color>.")
            return
        end

        local szGetAwardText = EventManager.TeamBattle_GetAwards(player)
        QuanLienDauSoCap:ShowNotify(npc, player, szGetAwardText)
        return
    end
    -- ************************** --
    if selectionID == 5 then
        -- Thực hiện truy vấn
        EventManager.TeamBattle_QueryTopTeam(player)

        -- GUI.CloseDialog(player)
        return
    end
    -- ************************** --
    if selectionID == 6 then
        Player.OpenShop(npc, player, 134)
        GUI.CloseDialog(player)
        return
    end
    -- ************************** --
    if selectionID == 7 then
        -- Nếu không phải thời gian đăng ký
        if EventManager.TeamBattle_IsRegisterTime() == false then
            QuanLienDauSoCap:ShowNotify(npc, player,
                "Hiện không phải thời gian đăng ký <color=green>Võ lâm liên đấu</color>, hãy quay lại vào tháng sau!")
            return
        end
        -- thực hiện xóa
        local ret = EventManager.DeleteTeam(player)
        if ret == 1 then
            QuanLienDauSoCap:ShowNotify(npc, player, "Đã xóa đội thành công")
            return
        elseif ret == 2 then
            QuanLienDauSoCap:ShowNotify(npc, player, "Bạn chưa thể xóa vì chưa đến 1 ngày")
            return
        elseif ret == 0 then
            QuanLienDauSoCap:ShowNotify(npc, player, "Xóa thất bại! Không tìm thấy đội")
            return
        end
    end
    -- ************************** --

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		itemID: number - ID vật phẩm được chọn
-- ****************************************************** --
function QuanLienDauSoCap:OnItemSelected(scene, npc, player, itemID, otherParams)

    -- ************************** --

    -- ************************** --

end

-- ======================================================= --
-- ======================================================= --
function QuanLienDauSoCap:ShowNotify(npc, player, text)

    -- ************************** --
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText(text)
    dialog:AddSelection(100, "Kết thúc đối thoại")
    dialog:Show(npc, player)
    -- ************************** --

end
