-- Mỗi khi Script được thực thi, ID tương ứng sẽ được lưu trong hệ thống, tại bảng 'Scripts'
-- Dạng đối tượng là dạng Class, được khởi tạo mặc định bởi hệ thống, và sau đó được lưu tại bảng
-- Khi sử dụng dạng Class, cần phải kế thừa Class được hệ thống sinh ra, và dòng lệnh bên dưới để làm điều đó
-- ID Script được khai báo ở file ScriptIndex.xml, thay thế giá trị '000000' bên dưới thành ID tương ứng
local NPC_Test = Scripts[000000]
local Record2 = 101120
local AcitvityRecore = {
    -- hai tac
    HaiTac = 11111,
    -- Bach Ho 
    BVD = 11112,
    -- Tiêu Dao Cốc
    XoYo = 100000,
    --		Tổng số ải đã vượt qua ngày hôm nay
    XoYo_StagesPassedToday = 100001,
    -- Bí cảnh
    MiJing = 100002,
    -- Du Long Các
    YouLong = 100003
}
local secretID = {
    [1] = {
        ItemID1 = 3280, -- Thiếu Lâm Phái
        ItemID2 = 3281,
        ItemID3 = 3256,
        ItemID4 = 3257
    },
    [2] = {
        ItemID1 = 3282, -- Thiên Vương Bang
        ItemID2 = 3283,
        ItemID3 = 3258,
        ItemID4 = 3259
    },
    [3] = {
        ItemID1 = 3284, -- Đường Môn
        ItemID2 = 3285,
        ItemID3 = 3260,
        ItemID4 = 3261
    },
    [4] = {
        ItemID1 = 3286, -- Ngũ Độc Giáo
        ItemID2 = 3287,
        ItemID3 = 3262,
        ItemID4 = 3263
    },
    [5] = {
        ItemID1 = 3288, -- Nga My Phái
        ItemID2 = 3289,
        ItemID3 = 3264,
        ItemID4 = 3265
    },
    [6] = {
        ItemID1 = 3290, -- Thúy Yên Môn
        ItemID2 = 3291,
        ItemID3 = 3266,
        ItemID4 = 3267
    },
    [7] = {
        ItemID1 = 3292, -- Cái Bang Phái
        ItemID2 = 3293,
        ItemID3 = 3268,
        ItemID4 = 3269
    },
    [8] = {
        ItemID1 = 3294, -- Thiên Nhẫn Giáo
        ItemID2 = 3295,
        ItemID3 = 3270,
        ItemID4 = 3271
    },
    [9] = {
        ItemID1 = 3296, -- Võ Đang Phái
        ItemID2 = 3297,
        ItemID3 = 3272,
        ItemID4 = 3273
    },
    [10] = {
        ItemID1 = 3298, -- Côn Lôn Phái
        ItemID2 = 3299,
        ItemID3 = 3274,
        ItemID4 = 3275
    },
    [11] = {
        ItemID1 = 3300, -- Minh Giáo
        ItemID2 = 3301,
        ItemID3 = 3276,
        ItemID4 = 3277
    },
    [12] = {
        ItemID1 = 3302, -- Đoàn Thị Phái
        ItemID2 = 3303,
        ItemID3 = 3278,
        ItemID4 = 3279
    }

}
local SetBelongings = {
    [1] = {
        ItemID1 = 9639, -- Thiếu Kim
        ItemID2 = 4810,
        ItemID3 = 5870,
        ItemID4 = 7084,
        ItemID5 = 6562,
        ItemID6 = 11707,
        ItemID7 = 12961,
        ItemID8 = 12207,
        ItemID9 = 4078,
        ItemID10 = 11174,
        ItemID11 = 10600,
        ItemID12 = 10610,
        ItemID13 = 10770,

        ItemID14 = 9649,
        ItemID15 = 4820,
        ItemID16 = 5880,
        ItemID17 = 6572,
        ItemID18 = 12217,
        ItemID19 = 11707,
        ItemID20 = 12961,
        ItemID21 = 4078,
        ItemID22 = 7094

    },
    [2] = {
        ItemID1 = 9679, -- Thiếu Mộc
        ItemID2 = 4850,
        ItemID3 = 5890,
        ItemID4 = 7104,
        ItemID5 = 6582,
        ItemID6 = 11717,
        ItemID7 = 12971,
        ItemID8 = 12227,
        ItemID9 = 4088,
        ItemID10 = 10620,
        ItemID11 = 11030,
        ItemID12 = 10630,
        ItemID13 = 12707,
        ItemID14 = 12717,
        ItemID15 = 11020,
        ItemID30 = 10350,

        ItemID16 = 9709,
        ItemID17 = 4860,
        ItemID18 = 5900,
        ItemID19 = 7114,
        ItemID20 = 6592,
        ItemID21 = 11717,
        ItemID22 = 12971,
        ItemID23 = 4088,
        ItemID24 = 12237

    },
    [3] = {
        ItemID1 = 9739, -- Thiếu thủy
        ItemID2 = 4890,
        ItemID3 = 5910,
        ItemID4 = 7124,
        ItemID5 = 6602,
        ItemID6 = 11727,
        ItemID7 = 12981,
        ItemID8 = 12247,
        ItemID9 = 4098,
        ItemID10 = 10640,
        ItemID11 = 10670,
        ItemID12 = 10650,
        ItemID13 = 10660,

        ItemID16 = 9749,
        ItemID17 = 4900,
        ItemID18 = 5920,
        ItemID19 = 7134,
        ItemID20 = 6612,
        ItemID21 = 11727,
        ItemID22 = 12981,
        ItemID23 = 12257,
        ItemID24 = 4098
    },
    [4] = {
        ItemID1 = 9779, -- Thiếu Hỏa
        ItemID2 = 4930,
        ItemID3 = 5930,
        ItemID4 = 7144,
        ItemID5 = 6622,
        ItemID6 = 11737,
        ItemID7 = 12991,
        ItemID8 = 12267,
        ItemID9 = 4108,
        ItemID10 = 10710,
        ItemID11 = 10690,
        ItemID12 = 10680,
        ItemID13 = 10700,
        ItemID30 = 10350,

        ItemID16 = 9789,
        ItemID17 = 4940,
        ItemID18 = 5940,
        ItemID19 = 7154,
        ItemID20 = 6632,
        ItemID21 = 11737,
        ItemID22 = 12991,
        ItemID23 = 12277,
        ItemID24 = 4108
    },
    [5] = {
        ItemID1 = 9819, -- Thiếu Thổ
        ItemID2 = 4970,
        ItemID3 = 5950,
        ItemID4 = 7164,
        ItemID5 = 6642,
        ItemID6 = 11747,
        ItemID7 = 13001,
        ItemID8 = 12287,
        ItemID9 = 4118,
        ItemID10 = 10720,
        ItemID11 = 10730,
        ItemID12 = 10740,
        ItemID13 = 10750,

        ItemID16 = 9829,
        ItemID17 = 4980,
        ItemID18 = 5960,
        ItemID19 = 7174,
        ItemID20 = 6652,
        ItemID21 = 11747,
        ItemID22 = 13001,
        ItemID23 = 12297,
        ItemID24 = 4118
    }
}
local SetBelongings89 = {
    [1] = {
        ItemID1 = 11756, -- Thiếu Kim
        ItemID2 = 13067,
        ItemID3 = 12543,
        ItemID4 = 6661,
        ItemID5 = 7183,
        ItemID6 = 5969,
        ItemID7 = 5029,
        ItemID8 = 4127,
        ItemID9 = 9638,
        ItemID10 = 10758,
        ItemID11 = 10768,
        ItemID12 = 10778,
        ItemID13 = 10788,

        ItemID14 = 11756,
        ItemID15 = 13067,
        ItemID16 = 12550,
        ItemID17 = 6671,
        ItemID18 = 7193,
        ItemID19 = 5979,
        ItemID20 = 5039,
        ItemID21 = 4127,
        ItemID22 = 9648

    },
    [2] = {
        ItemID1 = 11766, -- Thiếu Mộc
        ItemID2 = 13068,
        ItemID3 = 12557,
        ItemID4 = 6681,
        ItemID5 = 7203,
        ItemID6 = 5989,
        ItemID7 = 5069,
        ItemID8 = 4137,
        ItemID9 = 9678,

        ItemID10 = 10798,
        ItemID11 = 10998,
        ItemID12 = 10808,
        ItemID13 = 11008,
        ItemID14 = 12635,
        ItemID15 = 12645,
        ItemID30 = 10350,

        ItemID16 = 11766,
        ItemID17 = 13068,
        ItemID18 = 12564,
        ItemID19 = 6691,
        ItemID20 = 7213,
        ItemID21 = 5999,
        ItemID22 = 5079,
        ItemID23 = 4137,
        ItemID24 = 9688

    },
    [3] = {
        ItemID1 = 11776, -- Thiếu thủy
        ItemID2 = 13069,
        ItemID3 = 12571,
        ItemID4 = 6701,
        ItemID5 = 7223,
        ItemID6 = 6009,
        ItemID7 = 5089,
        ItemID8 = 4147,
        ItemID9 = 9718,

        ItemID10 = 10818,
        ItemID11 = 10828,
        ItemID12 = 10838,
        ItemID13 = 10848,

        ItemID16 = 11776,
        ItemID17 = 13069,
        ItemID18 = 12578,
        ItemID19 = 6711,
        ItemID20 = 7233,
        ItemID21 = 6019,
        ItemID22 = 5099,
        ItemID23 = 4147,
        ItemID24 = 9728
    },
    [4] = {
        ItemID1 = 11786, -- Thiếu Hỏa
        ItemID2 = 13070,
        ItemID3 = 12585,
        ItemID4 = 6721,
        ItemID5 = 7243,
        ItemID6 = 6029,
        ItemID7 = 5129,
        ItemID8 = 4157,
        ItemID9 = 9758,

        ItemID10 = 10888,
        ItemID11 = 10858,
        ItemID12 = 10868,
        ItemID13 = 10878,
        ItemID30 = 10350,

        ItemID16 = 11786,
        ItemID17 = 13070,
        ItemID18 = 12592,
        ItemID19 = 6731,
        ItemID20 = 7253,
        ItemID21 = 6039,
        ItemID22 = 5139,
        ItemID23 = 4157,
        ItemID24 = 9768
    },
    [5] = {
        ItemID1 = 11796, -- Thiếu Thổ
        ItemID2 = 13071,
        ItemID3 = 12599,
        ItemID4 = 6741,
        ItemID5 = 7263,
        ItemID6 = 6049,
        ItemID7 = 5169,
        ItemID8 = 4167,
        ItemID9 = 9798,

        ItemID10 = 10898,
        ItemID11 = 10908,
        ItemID12 = 10918,
        ItemID13 = 10928,

        ItemID16 = 11796,
        ItemID17 = 13071,
        ItemID18 = 12606,
        ItemID19 = 6751,
        ItemID20 = 7273,
        ItemID21 = 6059,
        ItemID22 = 5179,
        ItemID23 = 4167,
        ItemID24 = 9808
    }
}
local MantleSoPhuong = {
	[1] = {
		Item1 = 3625,
		Item2 = 3635
	},
	[2] = {
		Item1 = 3645,
		Item2 = 3655
	},
	[3] = {
		Item1 = 3665,
		Item2 = 3675
	},
	[4] = {
		Item1 = 3685,
		Item2 = 3695
	},
	[5] = {
		Item1 = 3705,
		Item2 = 3715
	}
}
local RECORD_DENBU_LENHBAI = 9900001
local RECORD_DENBU_MATTICHMAX = 9900002
-- local RECORD_DENBU_LENHBAI2 = 9900003
-- local RECORD_DENBU_LENHBAI2 = 9900006
local RECORD_DENBU_LENHBAI2 = 9900007
local RECORD_DENBU_SONGTU = 9900004
local RECORD_DENBU_BOOK = 9900005
local RECORD_100K_EXP_BOOK = 9900008 -- hỗ trợ all nhận 50 mật tịch đơn 2000
local RECORD_DENBU_KNB_NGUYETANHTHACH = 9900010 -- hỗ trợ nhận KNB cho các user mua nguyệt ảnh nguyên thạch giá cao
local RECORD_DENBU_KNB_ROLLBACK_303 = 9900011 -- trả lại KNB do rollback 30/3/2023
local RECORD_DENBU_DMC = 9900012 -- đền bù Dạ Minh Châu cho người chơi
local RECORD_QUA_HO_TRO_S10 = 9900013 -- gói hỗ trợ S10

local LIST_KNB_ROLLBACK_303 = {
    [1999806819] = 10000,
    [1999806196] = 10000,
    [1999810919] = 2000,
    [1999806384] = 1000,
    [1999806200] = 5000,
    [1999806670] = 1000,
    [1999812222] = 200,
    [1999813206] = 500,
    [1999806241] = 2000,
    [1999807811] = 200,
    [1999806443] = 500,
    [1999812882] = 4000,
    [1999806229] = 500,
    [1999806327] = 200,
    [1999806199] = 5000,
    [1999806188] = 10000,
    [1999806315] = 1000,
    [1999806636] = 1500,
    [1999811007] = 200,
    [1999810016] = 500,
    [1999806284] = 4000,
    [1999806246] = 1000,
    [1999806147] = 1000,
    [1999806224] = 1000,
    [1999806295] = 3000,
    [1999813671] = 2000,
    [1999806294] = 200,
    [1999813121] = 3000,
    [1999814574] = 1600,
    [1999806343] = 2000,
    [1999806214] = 10000,
    [1999806240] = 1000,
    [1999806358] = 200,
    [1999806256] = 3000,
    [1999806390] = 1000,
    [1999808937] = 1400,
    [1999806167] = 7000,
    [1999806616] = 500,
    [1999806173] = 2500,
    [1999806142] = 3000,
    [1999807023] = 4000,
    [1999808435] = 2500,
    [1999806237] = 4000,
    [1999806182] = 1000,
    [1999806542] = 10000,
    [1999806362] = 2000,
    [1999806190] = 2000,
    [1999806233] = 500,
    [1999807286] = 500,
    [1999809451] = 12000,
    [1999806586] = 5000,
    [1999808717] = 50000,
    [1999806425] = 5000,
    [1999808017] = 200,
    [1999814127] = 2000,
    [1999806209] = 2000,
    [1999811960] = 4000,
    [1999807327] = 500,
    [1999806191] = 200,
    [1999806332] = 10000,
    [1999814862] = 1000,
    [1999810080] = 200,
    [1999814040] = 1500,
    [1999806254] = 10000,
    [1999806146] = 2200,
    [1999806133] = 2000,
    [1999807350] = 1000,
    [1999806195] = 500,
    [1999807238] = 500,
    [1999812835] = 1000,
    [1999814061] = 500,
    [1999806268] = 2000,
    [1999807181] = 500,
    [1999806372] = 1700,
    [1999814858] = 200,
    [1999809411] = 600,
    [1999806473] = 200,
    [1999806755] = 5000,
    [1999812368] = 200,
    [1999806374] = 2000,
    [1999808391] = 600,
    [1999811367] = 1000,
    [1999806645] = 1000,
    [1999807610] = 500,
    [1999806440] = 500,
    [1999806394] = 500,
    [1999806404] = 500,
    [1999806239] = 200
}
--Thông tin đền bù DMC
local INFO_DMC_DENBU = {
    [3] = 10,
    [4] = 10,
    [5] = 15,
    [6] = 20,
    [7] = 25,
    [8] = 30,
    [9] = 40,
    [10] = 50,
}
function NPC_Test:ResetDenBuTayTuy(npc, player)
    -- Player:SetValueOfForeverRecore(player, RECORD_DENBU_LENHBAI, -1)
    -- Player:SetValueOfForeverRecore(player, RECORD_DENBU_MATTICHMAX, -1)
    -- Player:SetValueOfForeverRecore(player, RECORD_DENBU_SONGTU, -1)
    -- Player:SetValueOfForeverRecore(player, RECORD_DENBU_LENHBAI2, -1)
end

function NPC_Test:ShowDenBuMatTichMax(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- dialog.AddText(
    --     "Bạn sẽ nhận được Tu luyện thiên đơn, có tác dụng nâng cấp độ tu luyện của mật tịch.")
    -- dialog.AddSelection(30403, "Đồng ý!")
    -- dialog.AddSelection(999, "Để ta suy nghĩ thêm...")
    -- dialog.Show(npc, player)
end

function NPC_Test:NhanDenBuMatTichMax(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_mattichmax = Player.GetValueForeverRecore(player, RECORD_DENBU_MATTICHMAX)
    -- if LIST_DENBU_LENHBAI[player:GetID()] ~= nil and LIST_DENBU_LENHBAI[player:GetID()] > 0 and record_mattichmax == -1 then
    --     if Player.AddItemLuaAndNotify(player, 15205, 1, 0, 0) then
    --         Player.SetValueOfForeverRecore(player, RECORD_DENBU_MATTICHMAX, 1)
    --         dialog.AddText("Nhận vật phẩm thành công!")
    --     else
    --         dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    --     end
    -- else
    --     dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    -- end

    -- dialog.Show(npc, player)
end
function NPC_Test:ConfirmNhanDenBuDMC(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- dialog.AddText("Bạn sẽ nhận được số lượng Dạ Minh Châu theo cấp độ phi phong đang đeo trên người.")
    -- dialog.AddText("Cấp Lăng Tuyệt nhận 10 Dạ Minh Châu")
    -- dialog.AddText("Cấp Kinh Thế nhận 10 Dạ Minh Châu")
    -- dialog.AddText("Cấp Ngự Không nhận 15 Dạ Minh Châu")
    -- dialog.AddText("Cấp Hỗn Thiên nhận 20 Dạ Minh Châu")
    -- dialog.AddText("Cấp Sồ Phượng nhận 25 Dạ Minh Châu")
    -- dialog.AddText("Cấp Tiềm Long nhận 30 Dạ Minh Châu")
    -- dialog.AddText("Cấp Chí Tôn nhận 40 Dạ Minh Châu")
    -- dialog.AddText("Cấp Vô Song nhận 50 Dạ Minh Châu")
    -- local valueMantle = player:GetMantleLevel()
    -- if(valueMantle > 2) then
    --     local number_dmc = INFO_DMC_DENBU[valueMantle]
    --     dialog.AddSelection(30413, "Nhận " .. number_dmc .. " Dạ Minh Châu")
    -- else
    --     dialog.AddText("Cấp độ phi phong của bạn mặc trên người chưa đủ điều kiện để nhận Dạ Minh Châu")
    -- end
    -- dialog.AddSelection(999, "Ta sẽ nhận sau")
    -- dialog:Show(npc, player)
end

function NPC_Test:NhanDenBuDMC(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record = Player:GetValueWeeklyRecore(player, RECORD_DENBU_DMC)
    -- if record == -1 then
    --     local valueMantle = player:GetMantleLevel()
    --     if valueMantle > 2  then
    --         local number_dmc = INFO_DMC_DENBU[valueMantle]
    --         if Player:AddItemLuaAndNotify(player, 796, number_dmc, 0, 1, 0) then
    --             Player:SetValueOfWeekRecore(player, RECORD_DENBU_DMC, 1)
    --             dialog:AddText("Bằng hữu đã nhận đền bù Dạ Minh Châu thành công")
    --         else
    --             dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    --         end  
    --     else
    --         dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    --     end

    -- else 
    --     dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    -- end
    -- dialog:Show(npc, player)
end

function NPC_Test:NhanQuaHoTro(npc, player) 
    GUI:CloseDialog(player)
    if System:GetServerID() ~= 10 or player:GetLevel() < 50 then
        return
    end
    local dialog = GUI:CreateNPCDialog()
    local record = Player:GetValueWeeklyRecore(player, RECORD_QUA_HO_TRO_S10)
    if record ~= -1 then
        dialog:AddText("Bằng hữu đã nhận quà hỗ trợ rồi!")
        dialog:Show(npc, player)
        return
    end

    if not Player.HasFreeBagSpaces(player, 9) then
        dialog:AddText("Bằng hữu cần 9 ô trống trong túi đồ để nhận quà hỗ trợ!")
        dialog:Show(npc, player)
        return
    end

    Player:SetValueOfWeekRecore(player, RECORD_QUA_HO_TRO_S10, 1) -- ghi nhận đã nhận hỗ trợ
    player:AddExp(50000000) -- 500 vạn EXP
    Player:AddItemLuaAndNotify(player, 391, 5, -1, 1) -- 5 HT7 khóa
    Player:AddItemLuaAndNotify(player, 796, 10, -1, 1) -- 10 dạ minh châu khóa
    Player:AddItemLuaAndNotify(player, 15216, 25, -1, 1) -- 25 mật tịch đơn (2000) khóa
end

function NPC_Test:NhanDenBuKNB303(npc, player)
    local dialog = GUI:CreateNPCDialog()
    local record = Player:GetValueWeeklyRecore(player, RECORD_DENBU_KNB_ROLLBACK_303)
    if LIST_KNB_ROLLBACK_303[player:GetID()] ~= nil and record == -1 then
        local number_knb = LIST_KNB_ROLLBACK_303[player:GetID()]
        if Player:AddMoney(player, number_knb, 2, "DENBU_ROLLBACK_303") then
            Player:SetValueOfWeekRecore(player, RECORD_DENBU_KNB_ROLLBACK_303, 1)
            dialog:AddText("Nhận hỗ trợ thành công!")
        else
            dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
        end
    else 
        dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    end
    dialog:Show(npc, player)
end

function NPC_Test:NhanKNBNguyetAnhNguyenThach(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_knb = Player:GetValueForeverRecore(player, RECORD_DENBU_KNB_NGUYETANHTHACH)
    -- if LIST_KNB_NANT[player:GetName()] ~= nil and record_knb == -1 then
    --     local number_knb = LIST_KNB_NANT[player:GetName()]
    --     if Player:AddMoney(player, number_knb, 2, "DENBU_NANT") then
    --         Player:SetValueOfForeverRecore(player, RECORD_DENBU_KNB_NGUYETANHTHACH, 1)
    --         dialog:AddText("Nhận hỗ trợ thành công!") 
    --     else
    --         dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau.")
    --     end
    -- else
    --     dialog:AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau.")
    -- end

    -- dialog:Show(npc, player)
end

function NPC_Test:NhanDenBuLenhBaiTayTuy(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_lenhbai = Player.GetValueForeverRecore(player, RECORD_DENBU_LENHBAI)
    -- if LIST_DENBU_LENHBAI[player:GetID()] ~= nil and LIST_DENBU_LENHBAI[player:GetID()] > 0 and record_lenhbai == -1 then
    --     local number_lenhbai = LIST_DENBU_LENHBAI[player:GetID()]
    --     if Player.HasFreeBagSpaces(player, number_lenhbai) then
    --         if Player.AddItemLuaAndNotify(player, 336, LIST_DENBU_LENHBAI[player:GetID()], 0, 1) then
    --             Player.SetValueOfForeverRecore(player, RECORD_DENBU_LENHBAI, 1)
    --             dialog.AddText("Nhận hỗ trợ thành công!")
    --         else
    --             dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    --         end
    --     else
    --         dialog.AddText("Bạn cần " .. number_lenhbai .. " ô trống để nhận vật phẩm.")
    --     end
    -- else
    --     dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    -- end

    -- dialog.Show(npc, player)
end

function NPC_Test:NhanDenBuKyNangMatTichCao(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_bookcc = Player.GetValueForeverRecore(player, RECORD_DENBU_BOOK)
    -- if LIST_DENBU_BOOK[player:GetID()] and record_bookcc == -1 then
    --     local number_lenhbai = LIST_DENBU_BOOK[player:GetID()]
    --     Player.SetValueOfForeverRecore(player, RECORD_DENBU_BOOK, 1)
    --     for i = 1241, 1264, 1 do
    --         player:AddSkillLevel(i, 3)
    --     end
    -- else
    --     dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    -- end

    -- dialog.Show(npc, player)
end

function NPC_Test:NhanBoTuLenh(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_lenhbai = Player.GetValueForeverRecore(player, RECORD_DENBU_LENHBAI2)
    -- if LIST_DENBU_LENHBAI2[player:GetName()] and record_lenhbai == -1 then
    --     if Player.HasFreeBagSpaces(player, 1) then
    --         if Player.AddItemLuaAndNotify(player, 931, 1, 0, 1) then
    --             Player.SetValueOfForeverRecore(player, RECORD_DENBU_LENHBAI2, 1)
    --             dialog.AddText("Nhận hỗ trợ thành công!")
    --         else
    --             dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    --         end
    --     else
    --         dialog.AddText("Bạn cần 1 ô trống để nhận vật phẩm.")
    --     end
    -- else
    --     dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    -- end

    -- dialog.Show(npc, player)
end

function NPC_Test:NhanDenBuSongTu(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_songtu = Player.GetValueForeverRecore(player, RECORD_DENBU_SONGTU)
    -- if (LIST_DENBU_SONGTU[player:GetName()] or
    --     (LIST_DENBU_LENHBAI[player:GetID()] ~= nil and LIST_DENBU_LENHBAI[player:GetID()] > 0)) and record_songtu == -1 then
    --     if Player.HasFreeBagSpaces(player, 3) then
    --         if Player.AddItemLuaAndNotify(player, 15205, 2, 0, 1, 0, 3 * 24 * 60) and
    --             Player.AddItemLuaAndNotify(player, 506, 300, 0, 1) then
    --             Player.SetValueOfForeverRecore(player, RECORD_DENBU_SONGTU, 1)
    --             dialog.AddText("Nhận hỗ trợ thành công!")
    --         else
    --             dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    --         end
    --     else
    --         dialog.AddText("Bạn cần 3 ô trống để nhận vật phẩm.")
    --     end
    -- else
    --     dialog.AddText("Đã có lỗi xảy ra. Vui lòng thử lại sau!")
    -- end

    -- dialog.Show(npc, player)
end

function NPC_Test:Nhan50MatTichDon(npc, player)
    -- local dialog = GUI:CreateNPCDialog()
    -- local record_mattichdon = Player:GetValueForeverRecore(player, RECORD_100K_EXP_BOOK)
    -- if record_mattichdon == -1 then
    --     if Player:HasFreeBagSpaces(player, 1) then
    --         if Player:AddItemLuaAndNotify(player, 15216, 20, 0, 1, 0) then
    --             dialog:AddText("Bằng hữu đã nhận được 20 Mật tịch đơn (2000)")
    --             Player:SetValueOfForeverRecore(player, RECORD_100K_EXP_BOOK, 1)
    --         end
    --     else 
    --         dialog:AddText("Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm.")
    --     end
    -- else 
    --     dialog:AddText("Bằng hữu đã nhận hỗ trợ này rồi!")
    -- end

    -- dialog:Show(npc, player)
end

local CRYSTAL = {
    [1999806256] = {
        { Receive = 189, Require = {[189] = 3, [188] = 1}},
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 189, Require = {[187] = 4}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 2, [187] = 2}},
    },
    [1999806819] = {
        { Receive = 189, Require = {[189] = 3, [187] = 1}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[389] = 1, [390] = 1, [188] = 2}},
        { Receive = 189, Require = {[187] = 4}},
        { Receive = 188, Require = {[187] = 2, [188] = 2}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 189, Require = {[189] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 189, Require = {[188] = 2, [189] = 3}},
        { Receive = 188, Require = {[187] = 11}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 189, Require = {[189] = 3}},
        { Receive = 188, Require = {[187] = 10}},
        { Receive = 189, Require = {[188] = 5, [189] = 1, [187] = 8}},
        { Receive = 189, Require = {[188] = 1, [189] = 3}},
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999806342] = {
        { Receive = 189, Require = {[388] = 6, [387] = 3, [389] = 2}},
    },
    [1999806818] = {
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999807238] = {
        { Receive = 188, Require = {[187] = 10}},
        { Receive = 189, Require = {[188] = 6, [187] = 2, [189] = 1}},
    },
    [1999808391] = {
        { Receive = 188, Require = {[187] = 8, [188] = 1}},
        { Receive = 188, Require = {[187] = 1}},
        { Receive = 188, Require = {[187] = 1}},
        { Receive = 188, Require = {[187] = 1}},
        { Receive = 188, Require = {[187] = 1}},
        { Receive = 189, Require = {[188] = 1}},
        { Receive = 188, Require = {[187] = 1}},
        { Receive = 188, Require = {[187] = 1}},
        { Receive = 189, Require = {[188] = 1}},
        { Receive = 189, Require = {[188] = 1}},
        { Receive = 189, Require = {[188] = 1}},
        { Receive = 188, Require = {[187] = 1}},
    },
    [1999806670] = {
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999806233] = {
        { Receive = 188, Require = {[187] = 11}},
        { Receive = 190, Require = {[190] = 2, [187] = 10, [189] = 2}},
        { Receive = 190, Require = {[190] = 3, [189] = 1}},
        { Receive = 189, Require = {[188] = 9, [187] = 5}},
        { Receive = 191, Require = {[188] = 3, [189] = 3}},
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
    },
    [1999806231] = {
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[187] = 11}},
        { Receive = 188, Require = {[187] = 12}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[187] = 8, [188] = 1}},
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
        { Receive = 188, Require = {[187] = 2}},
        { Receive = 190, Require = {[188] = 1, [190] = 3}},
        { Receive = 190, Require = {[190] = 3, [188] = 1}},
    },
    [1999806374] = {
        { Receive = 190, Require = {[190] = 3, [189] = 1, [187] = 1}},
        { Receive = 190, Require = {[190] = 3}},
        { Receive = 188, Require = {[187] = 11}},
    },
    [1999806200] = {
        { Receive = 188, Require = {[187] = 9}},
    },
    [1999806390] = {
        { Receive = 188, Require = {[186] = 1, [387] = 9, [388] = 3}},
    },
    [1999806188] = {
        { Receive = 188, Require = {[390] = 3, [186] = 1}},
        { Receive = 188, Require = {[188] = 2, [390] = 1, [388] = 1, [389] = 1}},
    },
    [1999806384] = {
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 189, Require = {[188] = 2}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 190, Require = {[189] = 2, [188] = 1}},
    },
    [1999806554] = {
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
        { Receive = 188, Require = {[388] = 3, [387] = 12}},
        { Receive = 188, Require = {[188] = 2, [388] = 5, [389] = 1, [187] = 2, [387] = 5}},
    },
    [1999806239] = {
        { Receive = 189, Require = {[189] = 3, [187] = 1, [188] = 1}},
        { Receive = 189, Require = {[189] = 2, [187] = 5, [188] = 2}},
    },
    [1999806146] = {
        { Receive = 189, Require = {[187] = 4}},
        { Receive = 190, Require = {[188] = 4}},
    },
    [1999807181] = {
        { Receive = 189, Require = {[189] = 3}},
        { Receive = 188, Require = {[187] = 10}},
        { Receive = 188, Require = {[188] = 2, [187] = 4}},
    },
    [1999806241] = {
        { Receive = 189, Require = {[189] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999808717] = {
        { Receive = 189, Require = {[187] = 8}},
        { Receive = 188, Require = {[187] = 9}},
        { Receive = 188, Require = {[188] = 3, [185] = 1, [186] = 1, [187] = 1}},
        { Receive = 188, Require = {[187] = 8, [188] = 1}},
    },
    [1999813206] = {
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999813671] = {
        { Receive = 188, Require = {[387] = 2, [388] = 3, [187] = 1}},
    },
    [1999806237] = {
        { Receive = 190, Require = {[188] = 4}},
    },
    [1999806240] = {
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
        { Receive = 191, Require = {[189] = 2, [187] = 5, [188] = 5}},
        { Receive = 190, Require = {[190] = 2, [187] = 8, [189] = 3, [188] = 2}},
    },
    [1999809411] = {
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
        { Receive = 188, Require = {[188] = 2, [187] = 4}},
        { Receive = 188, Require = {[188] = 2, [187] = 3}},
    },
    [1999806229] = {
        { Receive = 190, Require = {[189] = 2, [187] = 2, [188] = 1}},
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
    },
    [1999806309] = {
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999806196] = {
        { Receive = 190, Require = {[188] = 4}},
    },
    [1999806457] = {
        { Receive = 188, Require = {[389] = 1, [187] = 1, [186] = 1}},
    },
    [1999812341] = {
        { Receive = 188, Require = {[386] = 3, [387] = 2, [388] = 3, [186] = 2, [185] = 1}},
    },
    [1999806288] = {
        { Receive = 188, Require = {[386] = 4, [186] = 1, [387] = 1, [389] = 1, [187] = 1}},
    },
    [1999811896] = {
        { Receive = 188, Require = {[186] = 1, [390] = 1, [188] = 2}},
        { Receive = 188, Require = {[388] = 1, [188] = 2, [390] = 1}},
    },
    [1999806359] = {
        { Receive = 188, Require = {[389] = 2, [185] = 1, [386] = 2}},
    },
    [1999806564] = {
        { Receive = 188, Require = {[388] = 1, [387] = 2, [389] = 1, [187] = 1}},
    },
    [1999806284] = {
        { Receive = 188, Require = {[186] = 1, [188] = 2, [388] = 1, [389] = 2}},
    },
    [1999810016] = {
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999814329] = {
        { Receive = 188, Require = {[389] = 2, [387] = 6}},
    },
    [1999806254] = {
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
        { Receive = 188, Require = {[188] = 3}},
    },
    [1999807081] = {
        { Receive = 188, Require = {[186] = 1, [187] = 1}},
    },
    [1999806372] = {
        { Receive = 188, Require = {[188] = 3, [187] = 1}},
    },
    [1999806223] = {
        { Receive = 188, Require = {[388] = 3, [186] = 1}},
    } 
}

local CRAFT = {
    [1999806457] = {
        { Receive = 4964, Require = {[2197] = 1, [2196] = 2}},
        { Receive = 4564, Require = {[2197] = 1, [2196] = 2}},
    },
    [1999813140] = {
        { Receive = 3973, Require = {[2338] = 2, [2337] = 2}},
    },
    [1999807350] = {
        { Receive = 9236, Require = {[2200] = 1, [2199] = 1}},
    },
    [1999806384] = {
        { Receive = 10257, Require = {[2200] = 4, [2199] = 4}},
    },
    [1999812835] = {
        { Receive = 12660, Require = {[2196] = 4, [2195] = 5}},
        { Receive = 12661, Require = {[2197] = 4, [2196] = 4}},
    },
    [1999806440] = {
        { Receive = 5747, Require = {[2240] = 1, [2239] = 1}},
        { Receive = 9416, Require = {[2240] = 1, [2239] = 1}},
        { Receive = 6961, Require = {[2200] = 1, [2199] = 1}},
    },
    [1999806417] = {
        { Receive = 4587, Require = {[2240] = 1, [2239] = 2}},
        { Receive = 5747, Require = {[2240] = 1, [2239] = 1}},
        { Receive = 9416, Require = {[2240] = 1, [2239] = 1}},
    },
    [1999806240] = {
        { Receive = 4016, Require = {[2341] = 2, [2340] = 2}},
        { Receive = 11615, Require = {[2221] = 2, [2220] = 1}},
        { Receive = 3996, Require = {[2341] = 2, [2340] = 2}},
    },
    [1999806383] = {
        { Receive = 6400, Require = {[2361] = 1, [2360] = 1}},
    },
    [1999806374] = {
        { Receive = 12879, Require = {[2201] = 2, [2200] = 1}},
        { Receive = 11625, Require = {[2221] = 2, [2220] = 1}},
    },
    [1999808391] = {
        { Receive = 6389, Require = {[2360] = 1, [2359] = 2}},
        { Receive = 4977, Require = {[2200] = 1, [2199] = 2}},
        { Receive = 4577, Require = {[2200] = 1, [2199] = 2}},
        { Receive = 9687, Require = {[2381] = 1, [2380] = 1}},
        { Receive = 9287, Require = {[2381] = 1, [2380] = 1}},
        { Receive = 6390, Require = {[2361] = 1, [2360] = 2}},
        { Receive = 6450, Require = {[2361] = 1, [2360] = 1}},
        { Receive = 4858, Require = {[2381] = 1, [2380] = 1}},
        { Receive = 6911, Require = {[2200] = 1, [2199] = 1}},
    },
    [1999807610] = {
        { Receive = 4427, Require = {[2380] = 1, [2379] = 1}},
        { Receive = 9256, Require = {[2380] = 1, [2379] = 1}},
    },
    [1999806214] = {
        { Receive = 10268, Require = {[2241] = 4, [2240] = 4}},
    },
    [1999806636] = {
        { Receive = 12868, Require = {[2200] = 2, [2199] = 1}},
        { Receive = 12084, Require = {[2220] = 2, [2219] = 1}},
        { Receive = 12898, Require = {[2200] = 2, [2199] = 1}},
    },
    [1999806209] = {
        { Receive = 6880, Require = {[2199] = 1, [2198] = 1}},
        { Receive = 10624, Require = {[2237] = 4, [2236] = 4}},
    },
    [1999812458] = {
        { Receive = 10347, Require = {[2200] = 4, [2199] = 4}},
    },
    [1999808405] = {
        { Receive = 10371, Require = {[2194] = 4}},
    },
    [1999813121] = {
        { Receive = 9656, Require = {[2380] = 1, [2379] = 1}},
        { Receive = 4427, Require = {[2380] = 1, [2379] = 1}},
        { Receive = 6559, Require = {[2360] = 1, [2359] = 1}},
    },
    [1999806381] = {
        { Receive = 5714, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 9343, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 4514, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 4514, Require = {[2237] = 1, [2236] = 1}},
    },
    [1999806542] = {
        { Receive = 10375, Require = {[2198] = 4, [2197] = 3}},
    },
    [1999808154] = {
        { Receive = 6409, Require = {[2360] = 1, [2359] = 1}},
        { Receive = 6409, Require = {[2360] = 1, [2359] = 1}},
    },
    [1999812882] = {
        { Receive = 5685, Require = {[2238] = 1, [2237] = 1}},
        { Receive = 5685, Require = {[2238] = 1, [2237] = 1}},
        { Receive = 5685, Require = {[2238] = 1, [2237] = 1}},
        { Receive = 9294, Require = {[2238] = 1, [2237] = 1}},
    },
    [1999806614] = {
        { Receive = 12864, Require = {[2196] = 2, [2195] = 1}},
        { Receive = 12864, Require = {[2196] = 2, [2195] = 1}},
        { Receive = 12864, Require = {[2196] = 2, [2195] = 1}},
        { Receive = 12864, Require = {[2196] = 2, [2195] = 1}},
        { Receive = 12864, Require = {[2196] = 2, [2195] = 1}},
        { Receive = 12864, Require = {[2196] = 2, [2195] = 1}},
    },
    [1999806733] = {
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6353, Require = {[2354] = 1}},
        { Receive = 6374, Require = {[2355] = 1, [2354] = 1}},
        { Receive = 6374, Require = {[2355] = 1, [2354] = 1}},
        { Receive = 6374, Require = {[2355] = 1, [2354] = 1}},
        { Receive = 6373, Require = {[2354] = 1}},
        { Receive = 6375, Require = {[2356] = 1, [2355] = 1}},
        { Receive = 6375, Require = {[2356] = 1, [2355] = 1}},
        { Receive = 6375, Require = {[2356] = 1, [2355] = 1}},
        { Receive = 5661, Require = {[2234] = 1}},
        { Receive = 5661, Require = {[2234] = 1}},
        { Receive = 5661, Require = {[2234] = 1}},
        { Receive = 5661, Require = {[2234] = 1}},
        { Receive = 5661, Require = {[2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5663, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5663, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5663, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5663, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5662, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5663, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5663, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 6376, Require = {[2357] = 1, [2356] = 2}},
        { Receive = 9773, Require = {[2377] = 1, [2376] = 2}},
    },
    [1999809451] = {
        { Receive = 4447, Require = {[2380] = 1, [2379] = 1}},
        { Receive = 4447, Require = {[2380] = 1, [2379] = 1}},
        { Receive = 4447, Require = {[2380] = 1, [2379] = 1}},
        { Receive = 6359, Require = {[2360] = 1, [2359] = 1}},
        { Receive = 7102, Require = {[2201] = 1, [2200] = 1}},
        { Receive = 4408, Require = {[2201] = 1, [2200] = 1}},
    },
    [1999806188] = {
        { Receive = 11735, Require = {[2221] = 2, [2220] = 1}},
    },
    [1999806819] = {
        { Receive = 4528, Require = {[2201] = 1, [2200] = 1}},
    },
    [1999806342] = {
        { Receive = 6921, Require = {[2200] = 1, [2199] = 2}},
        { Receive = 6921, Require = {[2200] = 1, [2199] = 2}},
    },
    [1999806190] = {
        { Receive = 11615, Require = {[2221] = 2, [2220] = 1}},
    },
    [1999806358] = {
        { Receive = 6586, Require = {[2357] = 1, [2356] = 2}},
        { Receive = 4474, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 4874, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 4474, Require = {[2237] = 1, [2236] = 1}},
    },
    [1999807286] = {
        { Receive = 10302, Require = {[2195] = 4, [2194] = 4}},
    },
    [1999806284] = {
        { Receive = 4568, Require = {[2201] = 1, [2200] = 2}},
        { Receive = 4448, Require = {[2381] = 1, [2380] = 1}},
    },
    [1999806586] = {
        { Receive = 11639, Require = {[2214] = 2, [2215] = 2}},
        { Receive = 11640, Require = {[2216] = 2, [2215] = 2}},
        { Receive = 11640, Require = {[2216] = 2, [2215] = 2}},
        { Receive = 11740, Require = {[2216] = 2, [2215] = 2}},
        { Receive = 11740, Require = {[2216] = 2, [2215] = 2}},
        { Receive = 11640, Require = {[2216] = 2, [2215] = 2}},
        { Receive = 11641, Require = {[2217] = 2, [2216] = 2}},
        { Receive = 11641, Require = {[2217] = 2, [2216] = 2}},
        { Receive = 11641, Require = {[2217] = 2, [2216] = 2}},
        { Receive = 11641, Require = {[2217] = 2, [2216] = 2}},
        { Receive = 11641, Require = {[2217] = 2, [2216] = 2}},
        { Receive = 11641, Require = {[2217] = 2, [2216] = 2}},
    },
    [1999807023] = {
        { Receive = 5695, Require = {[2238] = 1, [2237] = 1}},
        { Receive = 5695, Require = {[2238] = 1, [2237] = 1}},
        { Receive = 10707, Require = {[2200] = 4, [2199] = 4}},
    },
    [1999806473] = {
        { Receive = 12898, Require = {[2200] = 2, [2199] = 1}},
        { Receive = 12858, Require = {[2200] = 2, [2199] = 2}},
    },
    [1999812798] = {
        { Receive = 4103, Require = {[2338] = 2, [2337] = 2}},
    },
    [1999806523] = {
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
        { Receive = 4533, Require = {[2196] = 1, [2195] = 1}},
    },
    [1999814040] = {
        { Receive = 12021, Require = {[2217] = 2, [2216] = 1}},
        { Receive = 12021, Require = {[2217] = 2, [2216] = 1}},
        { Receive = 12021, Require = {[2217] = 2, [2216] = 1}},
        { Receive = 12865, Require = {[2197] = 2, [2196] = 1}},
        { Receive = 12021, Require = {[2217] = 2, [2216] = 1}},
    },
    [1999814862] = {
        { Receive = 4471, Require = {[2234] = 1}},
        { Receive = 3979, Require = {[2334] = 1}},
        { Receive = 3979, Require = {[2334] = 1}},
        { Receive = 3979, Require = {[2334] = 1}},
        { Receive = 3979, Require = {[2334] = 1}},
        { Receive = 3979, Require = {[2334] = 1}},
        { Receive = 3979, Require = {[2334] = 1}},
        { Receive = 3980, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 3980, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 3980, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 12018, Require = {[2214] = 1}},
        { Receive = 10251, Require = {[2194] = 4}},
        { Receive = 10251, Require = {[2194] = 4}},
    },
    [1999806980] = {
        { Receive = 3985, Require = {[2340] = 2, [2339] = 3}},
        { Receive = 4005, Require = {[2340] = 2, [2339] = 2}},
    },
    [1999806295] = {
        { Receive = 10388, Require = {[2201] = 4, [2200] = 4}},
    },
    [1999811960] = {
        { Receive = 10305, Require = {[2198] = 4, [2197] = 3}},
        { Receive = 10305, Require = {[2198] = 4, [2197] = 3}},
        { Receive = 10305, Require = {[2198] = 4, [2197] = 3}},
    },
    [1999814329] = {
        { Receive = 10345, Require = {[2198] = 4, [2197] = 3}},
    },
    [1999806199] = {
        { Receive = 12968, Require = {[2200] = 2, [2199] = 1}},
    },
    [1999808685] = {
        { Receive = 4586, Require = {[2239] = 1, [2238] = 2}},
        { Receive = 4507, Require = {[2240] = 1, [2239] = 1}},
        { Receive = 4507, Require = {[2240] = 1, [2239] = 1}},
    },
    [1999806332] = {
        { Receive = 6901, Require = {[2200] = 1, [2199] = 1}},
    },
    [1999813560] = {
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5681, Require = {[2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5682, Require = {[2235] = 1, [2234] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5883, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5683, Require = {[2236] = 1, [2235] = 1}},
        { Receive = 5884, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5884, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5884, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5684, Require = {[2237] = 1, [2236] = 1}},
        { Receive = 5884, Require = {[2237] = 1, [2236] = 1}},
    },
    [1999807291] = {
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12852, Require = {[2194] = 1}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12853, Require = {[2195] = 2, [2194] = 2}},
        { Receive = 12854, Require = {[2196] = 2, [2195] = 2}},
        { Receive = 12854, Require = {[2196] = 2, [2195] = 2}},
        { Receive = 3970, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 3970, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 3970, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 3970, Require = {[2335] = 2, [2334] = 2}},
        { Receive = 3981, Require = {[2336] = 2, [2335] = 2}},
        { Receive = 3981, Require = {[2336] = 2, [2335] = 2}},
        { Receive = 3981, Require = {[2336] = 2, [2335] = 2}},
        { Receive = 3969, Require = {[2334] = 1}},
        { Receive = 3969, Require = {[2334] = 1}},
        { Receive = 3969, Require = {[2334] = 1}},
        { Receive = 3971, Require = {[2336] = 2, [2335] = 1}},
        { Receive = 3982, Require = {[2337] = 2, [2336] = 2}},
        { Receive = 3982, Require = {[2337] = 2, [2336] = 2}},
    },
    [1999808002] = {
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
    },
    [1999806233] = {
        { Receive = 6650, Require = {[2361] = 1, [2360] = 1}},
        { Receive = 6640, Require = {[2361] = 1, [2360] = 1}},
    },
    [1999806616] = {
        { Receive = 4428, Require = {[2381] = 1, [2380] = 1}},
    },
    [1999808937] = {
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11608, Require = {[2214] = 1}},
        { Receive = 11609, Require = {[2214] = 2, [2215] = 2}},
        { Receive = 11609, Require = {[2214] = 2, [2215] = 2}},
        { Receive = 11609, Require = {[2214] = 2, [2215] = 2}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
        { Receive = 11610, Require = {[2216] = 2, [2215] = 1}},
    },
    [1999806645] = {
        { Receive = 12989, Require = {[2201] = 2, [2200] = 1}},
        { Receive = 12889, Require = {[2201] = 2, [2200] = 1}},
        { Receive = 12899, Require = {[2201] = 2, [2200] = 1}},
        { Receive = 12880, Require = {[2202] = 2, [2201] = 2}},
    },
    [1999806425] = {
        { Receive = 6371, Require = {[2362] = 1, [2361] = 1}},
    },
    [1999808731] = {
        { Receive = 10345, Require = {[2198] = 4, [2197] = 3}},
    },
    [1999806256] = {
        { Receive = 10248, Require = {[2201] = 4, [2200] = 4}},
    },
    [1999806237] = {
        { Receive = 4005, Require = {[2340] = 2, [2339] = 2}},
    },
    [1999806241] = {
        { Receive = 3997, Require = {[2342] = 2, [2341] = 2}},
        { Receive = 11646, Require = {[2222] = 2, [2221] = 2}},
    },
    [1999807238] = {
        { Receive = 11615, Require = {[2221] = 2, [2220] = 1}},
    },
    [1999806231] = {
        { Receive = 10719, Require = {[2202] = 4, [2201] = 4}},
    },
    [1999806670] = {
        { Receive = 4588, Require = {[2241] = 1, [2240] = 2}},
        { Receive = 4588, Require = {[2241] = 1, [2240] = 2}},
    },
    [1999806239] = {
        { Receive = 6420, Require = {[2361] = 1, [2360] = 1}},
    },
    [1999806147] = {
        { Receive = 4082, Require = {[2337] = 2, [2336] = 2}},
        { Receive = 4082, Require = {[2337] = 2, [2336] = 2}},
        { Receive = 3982, Require = {[2337] = 2, [2336] = 2}},
        { Receive = 4082, Require = {[2337] = 2, [2336] = 2}},
    },
    [1999806554] = {
        { Receive = 12878, Require = {[2200] = 2, [2199] = 1}},
    },
    [1999809974] = {
        { Receive = 6970, Require = {[2199] = 1, [2198] = 1}},
    },
    [1999806145] = {
        { Receive = 10368, Require = {[2201] = 4, [2200] = 4}},
        { Receive = 10368, Require = {[2201] = 4, [2200] = 4}},
    },
    [1999808017] = {
        { Receive = 10578, Require = {[2201] = 4, [2200] = 4}},
    },
    [1999814822] = {
        { Receive = 10232, Require = {[2315] = 4, [2194] = 4}},
        { Receive = 10232, Require = {[2315] = 4, [2194] = 4}},
        { Receive = 10232, Require = {[2315] = 4, [2194] = 4}},
    },
    [1999806327] = {
        { Receive = 10302, Require = {[2195] = 4, [2194] = 4}},
        { Receive = 10302, Require = {[2195] = 4, [2194] = 4}},
        { Receive = 10302, Require = {[2195] = 4, [2194] = 4}},
        { Receive = 10302, Require = {[2195] = 4, [2194] = 4}},
    }        
}

local ENHANCE = {
    [1999806231] = {
        { Receive = 12898, Require = {[188] = 2, [187] = 1}, Level = 9},
    },
    [1999806390] = {
        { Receive = 4347, Require = {[187] = 2, [185] = 1}, Level = 8},
        { Receive = 10348, Require = {[189] = 2, [187] = 7, [188] = 3}, Level = 10},
    },
    [1999806384] = {
        { Receive = 6798, Require = {[187] = 2, [188] = 1}, Level = 8},
        { Receive = 10257, Require = {[189] = 2, [187] = 5, [188] = 2}, Level = 10},
    },
    [1999806239] = {
        { Receive = 10167, Require = {[189] = 2, [187] = 10, [188] = 3}, Level = 10},
    },
    [1999806374] = {
        { Receive = 10347, Require = {[191] = 1, [190] = 2, [188] = 2, [189] = 1}, Level = 12},
        { Receive = 6419, Require = {[188] = 3, [189] = 2, [187] = 8, [190] = 1}, Level = 12},
    },
    [1999806394] = {
        { Receive = 4015, Require = {[188] = 3, [187] = 5}, Level = 11},
        { Receive = 4587, Require = {[187] = 4, [188] = 2}, Level = 10},
    },
    [1999807350] = {
        { Receive = 11672, Require = {[187] = 3}, Level = 8},
    },
    [1999806164] = {
        { Receive = 10967, Require = {[188] = 4, [186] = 1, [189] = 1, [187] = 1}, Level = 9},
    },
    [1999806241] = {
        { Receive = 5647, Require = {[187] = 6}, Level = 9},
    }      
}

function NPC_Test:Crafting303_Select(npc, player)
    local dialog = GUI.CreateNPCDialog()
    local crafts = CRAFT[player:GetID()]
    for i, v in ipairs(crafts) do
        dialog:AddSelection(40000 + i, '[Chế tạo vật phẩm] Lựa chọn '.. i)
    end
    dialog:Show(npc, player)
end

function NPC_Test:Crafting303_Confirm(npc, player, selection) 
    local dialog = GUI:CreateNPCDialog()

    local record = Player:GetValueWeeklyRecore(player, 9930000 + selection)
    if record ~= -1 then
        dialog:AddText('Bằng hữu đã nhận hỗ trợ này rồi!')
        dialog:Show(npc, player)
        return
    end

    if CRAFT[player:GetID()] == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local recipe = CRAFT[player:GetID()][selection]
    if recipe == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local require_items = recipe['Require']
    
    System.WriteToConsole(recipe['Receive'])

    for item_id, item_count in pairs(require_items) do
        System.WriteToConsole(item_id)
        local in_bag = Player:CountItemInBag(player, item_id)
        if in_bag < item_count then
            dialog:AddText("Bằng hữu không có đủ nguyên liệu!")
            dialog:Show(npc, player)
            return
        end 
    end

    for item_id, item_count in pairs(require_items) do
        Player:RemoveItem(player, item_id, item_count)
    end

    Player:AddItemLuaAndNotify(player, recipe['Receive'], 1, -1, 0)
    dialog:AddText('Bằng hữu đã nhận hỗ trợ thành công!')
    Player:SetValueOfWeekRecore(player, 9930000 + selection, 1)
    System:WriteLog(39, '' .. player:GetName() .. ' nhận hỗ trợ [Chế tạo vật phẩm][' .. selection .. '] ' ..  System:GetItemName(recipe['Receive']))

    dialog:Show(npc, player)
end

function NPC_Test:Crafting303(npc, player, selection)
    local dialog = GUI:CreateNPCDialog()

    local record = Player:GetValueWeeklyRecore(player, 9930000 + selection)
    if record ~= -1 then
        dialog:AddText('Bằng hữu đã nhận hỗ trợ này rồi!')
        dialog:Show(npc, player)
        return
    end

    if CRAFT[player:GetID()] == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local recipe = CRAFT[player:GetID()][selection]
    if recipe == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local require_items = recipe['Require']
    local des = "Đổi:\n"

    for item_id, item_count in pairs(require_items) do
        des = des .. '- ' .. item_count .. ' x ' .. System:GetItemName(item_id) .. '\n'
    end

    des = des .. '\nNhận:\n' .. '- 1 x ' .. System:GetItemName(recipe['Receive']) .. '\n'

    dialog:AddText(des)
    dialog:AddSelection(41000 + selection, "Đồng ý!")
    dialog:Show(npc, player)
end

function NPC_Test:Crystal303_Select(npc, player)
    local dialog = GUI.CreateNPCDialog()
    local crafts = CRYSTAL[player:GetID()]
    for i, v in ipairs(crafts) do
        dialog:AddSelection(50000 + i, '[Ghép huyền tinh] Lựa chọn '.. i)
    end
    dialog:Show(npc, player)
end

function NPC_Test:Crystal303_Confirm(npc, player, selection) 
    local dialog = GUI:CreateNPCDialog()

    local record = Player:GetValueWeeklyRecore(player, 9940000 + selection)
    if record ~= -1 then
        dialog:AddText('Bằng hữu đã nhận hỗ trợ này rồi!')
        dialog:Show(npc, player)
        return
    end

    if CRYSTAL[player:GetID()] == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local recipe = CRYSTAL[player:GetID()][selection]
    if recipe == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local require_items = recipe['Require']
    
    System.WriteToConsole(recipe['Receive'])
    local binding = 0

    for item_id, item_count in pairs(require_items) do
        System.WriteToConsole(item_id)
        local in_bag = Player:CountItemInBag(player, item_id)
        if in_bag < item_count then
            dialog:AddText("Bằng hữu không có đủ nguyên liệu!")
            dialog:Show(npc, player)
            return
        end 
    end

    for item_id, item_count in pairs(require_items) do
        Player:RemoveItem(player, item_id, item_count)
        if item_id > 300 then
            binding = 1
        end
    end

    Player:AddItemLuaAndNotify(player, recipe['Receive'], 1, -1, binding)
    dialog:AddText('Bằng hữu đã nhận hỗ trợ thành công!')
    Player:SetValueOfWeekRecore(player, 9940000 + selection, 1)
    System:WriteLog(39, '' .. player:GetName() .. ' nhận hỗ trợ [Ghép huyền tinh][' .. selection .. '] ' ..  System:GetItemName(recipe['Receive']))
    dialog:Show(npc, player)
end

function NPC_Test:Crystal303(npc, player, selection)
    local dialog = GUI:CreateNPCDialog()

    local record = Player:GetValueWeeklyRecore(player, 9940000 + selection)
    if record ~= -1 then
        dialog:AddText('Bằng hữu đã nhận hỗ trợ này rồi!')
        dialog:Show(npc, player)
        return
    end

    if CRYSTAL[player:GetID()] == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local recipe = CRYSTAL[player:GetID()][selection]
    if recipe == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local require_items = recipe['Require']
    local binding = 0
    local des = "Đổi:\n"

    for item_id, item_count in pairs(require_items) do
        des = des .. '- ' .. item_count .. ' x ' .. System:GetItemName(item_id) .. '\n'
        if item_id > 300 then
            binding = 1
        end
    end

    des = des .. '\nNhận:\n' .. '- 1 x ' .. System:GetItemName(recipe['Receive'])
    if binding == 1 then
        des = des .. ' - Khóa'
    end

    dialog:AddText(des)
    dialog:AddSelection(51000 + selection, "Đồng ý!")
    dialog:Show(npc, player)
end

function NPC_Test:Enhance303_Select(npc, player)
    local dialog = GUI.CreateNPCDialog()
    local crafts = ENHANCE[player:GetID()]
    for i, v in ipairs(crafts) do
        dialog:AddSelection(60000 + i, '[Cường hóa vật phẩm] Lựa chọn '.. i)
    end
    dialog:Show(npc, player)
end

function NPC_Test:Enhance303_Confirm(npc, player, selection) 
    local dialog = GUI:CreateNPCDialog()

    local record = Player:GetValueWeeklyRecore(player, 9950000 + selection)
    if record ~= -1 then
        dialog:AddText('Bằng hữu đã nhận hỗ trợ này rồi!')
        dialog:Show(npc, player)
        return
    end

    if ENHANCE[player:GetID()] == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local recipe = ENHANCE[player:GetID()][selection]
    if recipe == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local require_items = recipe['Require']
    
    System.WriteToConsole(recipe['Receive'])
    local binding = 0

    local item = Player:GetItemWithLevel(player, recipe['Receive'], recipe['Level'] - 1)
    if item == nil then
        dialog:AddText("Bằng hữu không có vật phẩm phù hợp!")
        dialog:Show(npc, player)
        return
    end

    for item_id, item_count in pairs(require_items) do
        System.WriteToConsole(item_id)
        local id_khoa = item_id
        local id_khongkhoa = item_id
        if item_id > 300 then
            id_khongkhoa = id_khoa - 202
        else 
            id_khoa = id_khongkhoa + 202
        end
        local in_bag = Player:CountItemInBag(player, id_khoa)
        local in_bag_khongkhoa = Player:CountItemInBag(player, id_khongkhoa)
        if in_bag + in_bag_khongkhoa < item_count then
            dialog:AddText("Bằng hữu không có đủ nguyên liệu!")
            dialog:Show(npc, player)
            return 
        end 
    end

    for item_id, item_count in pairs(require_items) do
        local id_khoa = item_id
        local id_khongkhoa = item_id
        if item_id > 300 then
            id_khongkhoa = id_khoa - 202
        else 
            id_khoa = id_khongkhoa + 202
        end

        local count = Player:CountItemInBag(player, id_khoa, 1)
        if count < item_count then
            Player:RemoveItem(player, id_khoa, 1, count)
            item_count = item_count - count
        else 
            Player:RemoveItem(player, id_khoa, 1, item_count)
            goto continue
        end

        count = Player:CountItemInBag(player, id_khongkhoa, 1)
        if count < item_count then
            Player:RemoveItem(player, id_khongkhoa, 1, count)
            item_count = item_count - count
        else 
            Player:RemoveItem(player, id_khongkhoa, 1, item_count)
            goto continue
        end

        count = Player:CountItemInBag(player, id_khoa, 0)
        if count < item_count then
            Player:RemoveItem(player, id_khoa, 0, count)
            item_count = item_count - count
        else 
            Player:RemoveItem(player, id_khoa, 0, item_count)
            goto continue
        end

        count = Player:CountItemInBag(player, id_khongkhoa, 0)
        if count < item_count then
            Player:RemoveItem(player, id_khongkhoa, 0, count)
            item_count = item_count - count
        else 
            Player:RemoveItem(player, id_khongkhoa, 0, item_count)
            goto continue
        end

        ::continue::
    end

    if item:GetBindingState() == 1 then
        binding = 1
    end

    Player:EquipEnhance(player, item, recipe['Level'], binding)
    dialog:AddText('Bằng hữu đã nhận hỗ trợ thành công!')
    Player:SetValueOfWeekRecore(player, 9950000 + selection, 1)
    System:WriteLog(39, '' .. player:GetName() .. ' nhận hỗ trợ [Cường hóa vật phẩm][' .. selection .. '] ' ..  System:GetItemName(recipe['Receive']) .. ' lên cấp ' .. recipe['Level'])
    dialog:Show(npc, player)
end

function NPC_Test:Enhance303(npc, player, selection)
    local dialog = GUI:CreateNPCDialog()

    local record = Player:GetValueWeeklyRecore(player, 9950000 + selection)
    if record ~= -1 then
        dialog:AddText('Bằng hữu đã nhận hỗ trợ này rồi!')
        dialog:Show(npc, player)
        return
    end

    if ENHANCE[player:GetID()] == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local recipe = ENHANCE[player:GetID()][selection]
    if recipe == nil then
        dialog:AddText('Đã có lỗi xảy ra. Vui lòng thử lại sau!')
        dialog:Show(npc, player)
        return
    end

    local require_items = recipe['Require']
    local des = "Đổi:\n"

    for item_id, item_count in pairs(require_items) do
        local item_name = System:GetItemName(item_id)
        item_name = item_name:gsub(' %- Khóa', '')
        des = des .. '- ' .. item_count .. ' x ' .. item_name .. '\n'
    end

    des = des .. '- 1 x ' .. System:GetItemName(recipe['Receive']) .. ' [Cấp ' .. (recipe['Level'] - 1) .. ']'

    des = des .. '\nNhận:\n' .. '- 1 x ' .. System:GetItemName(recipe['Receive']) .. ' [Cấp ' .. recipe['Level'] .. ']'

    dialog:AddText(des)
    dialog:AddSelection(61000 + selection, "Đồng ý!")
    dialog:Show(npc, player)
end

function NPC_Test:OnFactionQuest(scene, npc, player)
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText("Xem ra đã đễn lúc phải cho ngươi đi luyện công phu thượng thừa của thập nhị môn phái rồi.\n\nThập nhị môn phái có lịch sử lâu đời, võ công của phái nào cũng bác đại uyên thâm, cần phải tìm hiểu kỹ càng. Cũng chính vì lý do này, ngươi cần phải chọn kỹ sẽ vào môn phái nào. Trong thôn này có <color=#DD0000>đệ tử tiếp dẫn</color> của thập nhị môn phái, tốt nhất là ngươi hãy gặp họ trước, để hiểu được trường phái võ thuật của từng môn phái.\n\nTa cũng có thể cung cấp cho ngươi những thông tin cần thiết về từng môn phái.")
    dialog:AddSelection(55555, "Tìm hiểu môn phái ")
    dialog:Show(npc, player)
end

-- ****************************************************** --
--	Hàm này được gọi khi người chơi ấn vào NPC
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		otherParams: Key-Value {number, string} - Danh sách các tham biến khác
-- ****************************************************** --
function NPC_Test:OnOpen(scene, npc, player, otherParams)
    if player.GetLevel() >= 10 and player:GetFactionID() == 0 then 
        NPC_Test:OnFactionQuest(scene, npc, player)
        return
    end

    -- ************************** --
    local dialog = GUI.CreateNPCDialog()

    -- local record_knb_303 = Player:GetValueWeeklyRecore(player, RECORD_DENBU_KNB_ROLLBACK_303)
    -- if LIST_KNB_ROLLBACK_303[player:GetID()] ~= nil and record_knb_303 == -1 then
    --     dialog:AddSelection(30402, "<color=#f38ba8>[Hỗ trợ]</color> Nhận lại " .. LIST_KNB_ROLLBACK_303[player:GetID()] .. " KNB")
    -- end

    -- if CRAFT[player:GetID()] ~= nil then
    --     dialog:AddSelection(30403, '<color=#f38ba8>[Hỗ trợ]</color> Chế tạo vật phẩm')
    -- end

    -- if CRYSTAL[player:GetID()] ~= nil then 
    --     dialog:AddSelection(30404, '<color=#f38ba8>[Hỗ trợ]</color> Ghép huyền tinh')
    -- end

    -- if ENHANCE[player:GetID()] ~= nil then
    --     dialog:AddSelection(30407, '<color=#f38ba8>[Hỗ trợ]</color> Cường hóa vật phẩm')
    -- end

    -- if ListContainsValue(LUA_ITEMPACKS[12].Roles, player:GetID()) then
    --     dialog:AddSelection(30408, '<color=#f38ba8>[Hỗ trợ]</color> Nhận Dây cương sự kiện')
    -- end

    local record2 = Player.GetValueForeverRecore(player, Record2)
    if System:GetServerID() == 99 then
        dialog:AddSelection(1, "Môn phái muốn gia nhập.")
        dialog:AddSelection(11111, "Nhận cấp 119")
        dialog:AddSelection(10004, "Nhận <color=red>Nhận set đồ theo hệ 119 +14</color>")
		dialog:AddSelection(10001, "Nhận <color=red> Mật Tịch (trung) Theo Phái </color>")
        dialog:AddSelection(10002, "Nhận <color=red> Mật Tịch (cao) Theo Phái </color>")
		dialog:AddSelection(10015, "Nhận thần thú")
		dialog:AddSelection(10026, "Nhận Lệnh bài tẩy tủy đảo")
		dialog:AddSelection(22222, "Nhận Max EXP mật tịch")
		dialog:AddSelection(10028, "Nhận Rương du long")
		dialog:AddSelection(10029, "Nhận Phi phong (Sồ Phượng)")
		dialog:AddSelection(10030, "Xóa túi đồ")
    end
    if player:IsGM() then
        dialog:AddSelection(100031, "Reset Record")
        dialog:AddSelection(1, "Môn phái muốn gia nhập.")
        -- dialog:AddSelection(2, "Add 10 Bot")
        -- dialog:AddSelection(3, "Add 20 Bot")
        dialog:AddSelection(30000, "Ta muốn đổi tên")
        dialog:AddSelection(30001, "Xóa vật phẩm")
        dialog:AddSelection(30002, "Ghép vật phẩm")
        dialog:AddSelection(22222, "EXP BOOK")
        dialog:AddSelection(10012, "Nhận cấp 89")
        dialog:AddSelection(10024, "Nhận cấp 99")
        dialog:AddSelection(10000, "Nhận cấp 109")
        dialog:AddSelection(11111, "Nhận cấp 119")
        dialog:AddSelection(10023, "Nhận <color=red>Nhận set đồ theo hệ 89 +12</color>")
        dialog:AddSelection(10025, "Nhận <color=red>Nhận set đồ theo hệ 89 +8</color>")
        dialog:AddSelection(10004, "Nhận <color=red>Nhận set đồ theo hệ 119 +14</color>")
        dialog:AddSelection(10001, "Nhận <color=red> Mật Tịch (trung) Theo Phái </color>")
        dialog:AddSelection(10002, "Nhận <color=red> Mật Tịch (cao) Theo Phái </color>")
        dialog:AddSelection(10003, "Nhận <color=red>3 Huyền Tinh 8</color>")
        dialog:AddSelection(10005, "Nhận <color=red>Nhận 1000 uy danh </color>")
        dialog:AddSelection(10006, "Làm mới số lượt đi phụ bản trong ngày")
        dialog:AddSelection(10007, "Nhận 5 bản đồ bí Cảnh")
        dialog:AddSelection(10008, "Nhận 100 Nguyệt Ảnh Thạch")
        dialog:AddSelection(10009, "Nhận 50 Chiến thư Du Long")
        dialog:AddSelection(10010, "Nhận Lệnh bài Thi đấu môn phái (sơ)")
        dialog:AddSelection(10015, "Nhận thần thú")
        dialog:AddSelection(100020, "Nhận 10000 tinh lực,hoạt lực")
        dialog:AddSelection(100030, "GiftCode")
        dialog:AddSelection(30405, "[GM] Reset Đền bù Tẩy Tủy")
        dialog:AddSelection(30406, "[GM] Truyền công")
    end
		dialog:AddSelection(30444, "Reset điểm ban đầu chân nguyên")
		
	if player:IsGM() or player:GetID() == 1999901529 then
		dialog:AddSelection(99999, "Reset Pet Yến Tiểu Lâu")
	end
    if Global_Enalbe_KuaFu() or Global_EarlyAccess[player:GetName()] or player:IsGM() then
        dialog:AddSelection(10011, "Dịch chuyển đến Hoàng Thành liên Server")
        dialog:AddSelection(10018, "Liên Server Cân Bằng")
        if Global_EarlyAccess[player:GetName()] then
            dialog:AddSelection(110013, "reset luot BHD lien sv")
        end
		dialog:AddSelection(10016, "Liên Server Tam Long Tranh Bá")
    end
	
	if player:IsGM() then
		dialog:AddSelection(10017, "Liên Server Test")
	end

    -- if System:GetServerID() == 10 and player:GetLevel() >= 50 then
    --     dialog:AddSelection(30414, "[Hệ thống] Nhận gói quà hỗ trợ")
    -- end

    dialog:AddText("Xin chào " .. player:GetName() .. "!")
    dialog:AddSelection(30400, "Xóa vật phẩm nhiệm vụ")

    -- local record_lenhbai = Player.GetValueForeverRecore(player, RECORD_DENBU_LENHBAI)
    -- if LIST_DENBU_LENHBAI[player:GetID()] ~= nil and LIST_DENBU_LENHBAI[player:GetID()] > 0 and record_lenhbai == -1 then
    --     dialog:AddSelection(30402, "[Hỗ trợ] Nhận " .. LIST_DENBU_LENHBAI[player:GetID()] ..
    --         " Lệnh bài Tẩy Tủy Đảo")
    -- end

    -- local record_mattichmax = Player.GetValueForeverRecore(player, RECORD_DENBU_MATTICHMAX)
    -- if LIST_DENBU_LENHBAI[player:GetID()] ~= nil and LIST_DENBU_LENHBAI[player:GetID()] > 0 and record_mattichmax == -1 then
    -- 	dialog:AddSelection(30403, "[Hỗ trợ] Nhận Tu luyện thiên đơn")
    -- end

    -- local record_lenhbai2 = Player.GetValueForeverRecore(player, RECORD_DENBU_LENHBAI2)
    -- if LIST_DENBU_LENHBAI2[player:GetName()] and record_lenhbai2 == -1 then
    --     dialog:AddSelection(30407, "[Hỗ trợ] Nhận Bổ Tu Lệnh")
    -- end

    -- local record_songtu = Player.GetValueForeverRecore(player, RECORD_DENBU_SONGTU)
    -- if (LIST_DENBU_SONGTU[player:GetName()] or
    --     (LIST_DENBU_LENHBAI[player:GetID()] ~= nil and LIST_DENBU_LENHBAI[player:GetID()] > 0)) and record_songtu == -1 then
    --     dialog:AddSelection(30408, "[Hỗ trợ] Nhận 2 Tu luyện thiên đơn và 300 NHHT")
    -- end

    -- local record_bookcc = Player.GetValueForeverRecore(player, RECORD_DENBU_BOOK)
    -- if LIST_DENBU_BOOK[player:GetID()] and record_bookcc == -1 then
    --     dialog:AddSelection(30409, "[Hỗ trợ] Nhận +3 cấp kỹ năng của mật tịch cao cấp")
    -- end

    -- local record_mattichdon = Player.GetValueForeverRecore(player, RECORD_100K_EXP_BOOK)
    -- if record_mattichdon == -1 then
    --     dialog:AddSelection(30410, "[Hỗ trợ] Nhận 20 Mật tịch đơn (2000)")
    -- end

    -- local record_knb_nant = Player:GetValueForeverRecore(player, RECORD_DENBU_KNB_NGUYETANHTHACH)
    -- if LIST_KNB_NANT[player:GetName()] ~= nil and record_knb_nant == -1 then
    --     dialog:AddSelection(30411, "[Hỗ trợ] Nhận đền bù KNB mua Nguyệt ảnh nguyên thạch")
    -- end

    -- local record_dmc = Player:GetValueWeeklyRecore(player, RECORD_DENBU_DMC)
    -- if record_dmc == -1 then
    --     dialog:AddSelection(30412, "[Hỗ trợ] Nhận đền bù Dạ Minh Châu")
    -- end

    dialog:Show(npc, player)
    -- ************************** --

end

function NPC_Test:ConfirmRemoveTaskItem(npc, player)
    local dialog = GUI.CreateNPCDialog()
    dialog.AddText(
        "Ta sẽ giúp bằng hữu tiêu hủy các vật phẩm của các nhiệm vụ đã hoàn thành. Bằng hữu muốn tiếp tục chứ?")
    dialog.AddText(Player:GetListTaskItemToBeRemoved(player))
    dialog.AddSelection(30401, "Đồng ý!")
    dialog.AddSelection(999, "Không, cảm ơn!")
    dialog:Show(npc, player)
end

function NPC_Test:RemoveCompletedTaskItem(player)
    Player.RemoveCompletedTaskItem(player)
    GUI.CloseDialog(player)
end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi ấn vào một trong số các chức năng cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectionID: number - ID chức năng
--		otherParams: Key-Value {number, string} - Danh sách các tham biến khác
-- ****************************************************** --
function NPC_Test:OnSelection(scene, npc, player, selectionID, otherParams)

	if selectionID == 99999 then
		player:FixPetOut10Skill()
		GUI.CloseDialog(player)
	end
	if selectionID == 10026 then
		local dialog = GUI:CreateNPCDialog()
        Player.AddItemLua(player, 336, 1, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red> lệnh bài tẩy tủy đảo </color>thành công")
        dialog:Show(npc, player)
        return
    end
	if selectionID == 10027 then
        local dialog = GUI:CreateNPCDialog()
		Player.AddItemLua(player, 15216, 1, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red> mật tịch đơn (10m Exp) </color>thành công")
        dialog:Show(npc, player)
        return
    end
	if selectionID == 10028 then
        local dialog = GUI:CreateNPCDialog()
		Player.AddItemLua(player, 579, 1, -1, 1)
		Player.AddItemLua(player, 580, 1, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red> rương du long </color>thành công")
        dialog:Show(npc, player)
        return
    end
	if selectionID == 10029 then
		local dialog = GUI:CreateNPCDialog()
		if player:GetFactionID() == 0 then
            dialog:AddText("" .. npc:GetName() ..
                               ": Bạn chưa gia nhập phái ,hãy gia nhập <color=red> môn phái</color> rồi quay lại <color=red> nhận phi phong.</color>")
            dialog:Show(npc, player)
        elseif player:GetFactionID() ~= 0 then
			local seriesID = Player.GetSeries(player)
			if player:GetSex() == 0 then
				Player.AddItemLua(player, MantleSoPhuong[seriesID].Item1, 1, -1, 1)
				dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red> phi phong </color>thành công")
				dialog:Show(npc, player)
				return
			else
				Player.AddItemLua(player, MantleSoPhuong[seriesID].Item2, 1, -1, 1)
				dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red> phi phong </color>thành công")
				dialog:Show(npc, player)
				return
			end
		end
    end
	if selectionID == 10030 then
		local dialog = GUI:CreateNPCDialog()
		player:ClearBag()
		dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã xóa túi đồ thành công")
        dialog:Show(npc, player)
        return
	end
    if selectionID == 30408 then
        local received = Global_ReceiveItemPack(npc, player, 12)
		if received then
            local dialog = GUI:CreateNPCDialog()
			local name = LUA_ITEMPACKS[12].Name
			dialog:AddText("Chúc mừng bằng hữu đã nhận <color=#a6e3a1>" .. name .. "</color> thành công.")
			dialog:Show(npc, player)
		end
        return
    end
    if selectionID == 30403 then
        NPC_Test:Crafting303_Select(npc, player)
        return
    end

    if selectionID >= 40000 and selectionID < 41000 then
        NPC_Test:Crafting303(npc, player, selectionID - 40000)
        return
    end

    if selectionID >= 41000 and selectionID < 42000 then
        NPC_Test:Crafting303_Confirm(npc, player, selectionID - 41000)
        return
    end

    if selectionID == 30404 then
        NPC_Test:Crystal303_Select(npc, player)
        return
    end

    if selectionID >= 50000 and selectionID < 51000 then
        NPC_Test:Crystal303(npc, player, selectionID - 50000)
        return
    end

    if selectionID >= 51000 and selectionID < 52000 then
        NPC_Test:Crystal303_Confirm(npc, player, selectionID - 51000)
        return
    end

    if selectionID == 30407 then
        NPC_Test:Enhance303_Select(npc, player)
        return
    end

    if selectionID == 55555 then
        GUI:CloseDialog(player)
        GUI:OpenUI(player, "MainGame/UIFactionSelect")
    end
    if selectionID >= 60000 and selectionID < 61000 then
        NPC_Test:Enhance303(npc, player, selectionID - 60000)
        return
    end

    if selectionID >= 61000 and selectionID < 62000 then
        NPC_Test:Enhance303_Confirm(npc, player, selectionID - 61000)
        return
    end

    if selectionID == 30402 then
        NPC_Test:NhanDenBuKNB303(npc, player)
        return
    end

    if(selectionID == 30412) then 
        NPC_Test:ConfirmNhanDenBuDMC(npc, player)
        return
    end

    if(selectionID == 30413) then 
        NPC_Test:NhanDenBuDMC(npc, player)
        return
    end

    -- if (selectionID == 30414) then
    --     NPC_Test:NhanQuaHoTro(npc, player)
    --     return
    -- end

    if selectionID == 999 then
        GUI.CloseDialog(player)
        return
    end

    -- ************************** --
    local dialog = GUI.CreateNPCDialog()
    local TotalPrestige = player:GetPrestige()
    if selectionID == 30000 then
        -- Gọi hàm đổi tên
        GUI.OpenChangeName(player)

        -- Đóng khung
        GUI.CloseDialog(player)
        return
    end
    -- ************************** --
    if selectionID == 30001 then
        -- Mở khung xóa vật phẩm
        GUI.OpenRemoveItems(player)

        -- Đóng khung
        GUI.CloseDialog(player)
        return
    end
    if selectionID == 30002 then
        -- Mở khung ghép vật phẩm
        GUI.OpenMergeItems(player)

        -- Đóng khung
        GUI.CloseDialog(player)
        return
    end
    if selectionID == 30003 then
        local record = Player.GetValueForeverRecore(player, Record2)

        -- Nếu đã nhận rồi
        if record == 1 then
            BachThuLam:ShowDialog(npc, player,
                "Bằng hữu đã nhận <color=green>Thẻ đổi tên</color> trước đó, không thể nhận thêm!")
            return
        end
        -- Đánh dấu đã nhận rồi
        Player.SetValueOfForeverRecore(player, Record2, 1)
        Player.AddItemLua(player, 2167, 1, -1, 1)
        BachThuLam:ShowDialog(npc, player,
            "Nhận <color=green>Thẻ đổi tên</color> thành công. Rất cảm ơn bằng hữu đã luôn đồng hành cùng <color=green>Kiếm Thế 2009 - Mobile</color>!")
        return
    end

    if selectionID == 30400 then
        NPC_Test:ConfirmRemoveTaskItem(npc, player)
        return
    end

    if selectionID == 30401 then
        NPC_Test:RemoveCompletedTaskItem(player)
        return
    end
	
	if selectionID == 30444 then
		Player.ResetBaiscProps(player)
		GUI.CloseDialog(player)
        return
	end

    if selectionID == 10005 then

        player:SetPrestige(1000)
        dialog:AddText("" .. npc:GetName() .. "Bạn đã nhận </color=red>1000 uy danh </color>thành công")

        dialog:Show(npc, player)
        return
    end

    if selectionID == 22222 then
        Player.SetBookLevelAndExp(player, 100, 0)
        dialog:AddText("Nhận thành công")
        dialog:Show(npc, player)
        return
    end

    if selectionID == 10006 then
        player:ResetCopySceneEnterTimes(11111)
        player:ResetCopySceneEnterTimes(11112)
        player:ResetCopySceneEnterTimes(100000)
        player:ResetCopySceneEnterTimes(100001)
        player:ResetCopySceneEnterTimes(100002)
        player:ResetCopySceneEnterTimes(100003)
        dialog:AddText("" .. npc:GetName() .. "Mới số lượt đi phụ bản trong ngày thành công")
        dialog:Show(npc, player)
    end
    if selectionID == 10007 then

        Player.AddItemLua(player, 590, 5, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red>5 bản đồ bí Cảnh </color>thành công")
        dialog:Show(npc, player)
        return
    end
    if selectionID == 10008 then
        Player.AddItemLua(player, 928, 100, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red>100 Nguyệt ảnh thạch </color>thành công")
        dialog:Show(npc, player)
        return
    end
    if selectionID == 10009 then
        Player.AddItemLua(player, 1026, 50, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red>50 Chiến Thư-Mật Thất Du Long </color>thành công")
        dialog:Show(npc, player)
        return
    end
    if selectionID == 10010 then
        Player.AddItemLua(player, 338, 10, -1, 1)
        dialog:AddText("" .. npc:GetName() ..
                           "Bạn đã nhận </color=red>10 lệnh bài Thi đấu môn phái(sơ) </color>thành công")
        dialog:Show(npc, player)
        return
    end
    if selectionID == 10011 then
        -- if Global_EarlyAccess[player:GetName()] or player:IsGM() then
            -- dialog:AddText("Bạn đang có </color=yellow>" .. Player.CheckMoney(player, 6) ..
                           -- " Bạc khóa liên máy chủ</color>, bạn có muốn vào liên máy chủ không?")

            -- dialog:AddSelection(110011, "Vào liên máy chủ");
			-- if player:GetLevel() < 90 then
				-- dialog:AddSelection(1100111, "Vào liên máy chủ 89");
			-- end
            -- dialog:AddSelection(110012, "Chưa muốn vào lúc này");

            -- dialog:Show(npc, player)

            -- return
        -- end
        if player:GetLevel() < 80 then
            dialog:AddText("Bạn cần đạt cấp 60 để vào liên máy chủ 80 hoặc cấp 90 để vào liên máy chủ thường.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end
        if player:GetMantleValue() < 3000000 then
            dialog:AddText(
                "Bạn cần trang bị phi phong cấp Kinh Thế trở lên để vào liên máy chủ 89 hoặc phi phong " .. System:GetMantleNameFromLevel(GLOBAL_SERVERCONFIG.MantleLevelForKuaFu) .. " để vào liên máy chủ thường.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end
		
		
        dialog:AddText("Bạn đang có </color=yellow>" .. Player.CheckMoney(player, 6) ..
                           " Bạc khóa liên máy chủ</color>, bạn có muốn vào liên máy chủ không?")
                           dialog:AddText(
                            "Bạn cần trang bị phi phong cấp Ngự Không trở lên để vào liên máy chủ 89 hoặc phi phong " .. System:GetMantleNameFromLevel(GLOBAL_SERVERCONFIG.MantleLevelForKuaFu) .. " để vào liên máy chủ thường.")
		if player:GetLevel() >= 90 and player:GetMantleLevel() >= GLOBAL_SERVERCONFIG.MantleLevelForKuaFu then
			dialog:AddSelection(110011, "Vào liên máy chủ");
		end
		if player:GetLevel() < 120 and player:GetMantleValue() >= 18000000 then
			dialog:AddSelection(1100111, "Vào liên máy chủ 89");
		end
        dialog:AddSelection(110012, "Chưa muốn vào lúc này");

        dialog:Show(npc, player)

        return
    end

    if selectionID == 10018 then
        if not Global_Enalbe_KuaFu_Balance() and not player:IsGM() then
            dialog:AddText("Hoạt động Liên Server Cân Bằng chỉ mở từ 16:00 đến 23:59 vào Thứ 5 và Chủ Nhật hàng tuần.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end

        if player:GetMantleLevel() < 6 then
            dialog:AddText("Bạn cần trang bị Phi Phong cấp 6 trở lên để vào Liên Server Cân Bằng.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end

        dialog:AddText("Vào Hoàng Thành Liên Server để chọn phái và nhận trang bị cân bằng tại Bạch Thu Lâm.")
        dialog:AddSelection(110018, "Vào Liên Server Cân Bằng")
        dialog:AddSelection(110012, "Chưa muốn vào lúc này")
        dialog:Show(npc, player)
        return
    end
	
    if selectionID == 110011 then
		player:ChangeScene(1616, 7545, 4454)
        GUI.CloseDialog(player)
        return
    end

    if selectionID == 110018 then
        if not Global_Enalbe_KuaFu_Balance() and not player:IsGM() then
            GUI.CloseDialog(player)
            return
        end

        player:ChangeScene(1642, 9052, 4385, 9007)
        GUI.CloseDialog(player)
        return
    end
	
	 if selectionID == 1100111 then
		player:ChangeScene(1616, 7545, 4454, 9998)
        GUI.CloseDialog(player)
        return
    end
	
	
    if selectionID == 110012 then
        GUI.CloseDialog(player)
        return
    end
	
	if selectionID == 10016 then
        if Global_EarlyAccess[player:GetName()] or player:IsGM() then
            dialog:AddText("Bạn đang có </color=yellow>" .. Player.CheckMoney(player, 6) ..
                           " Bạc khóa liên máy chủ</color>, bạn có muốn vào liên máy chủ không?")

            dialog:AddSelection(110016, "Vào liên máy chủ");
            dialog:AddSelection(110012, "Chưa muốn vào lúc này");

            dialog:Show(npc, player)

            return
        end
        if player:GetLevel() < 79 then
            dialog:AddText("Bạn cần đạt cấp 79 để vào liên máy chủ.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end
        if player:GetMantleValue() <= 3000000 then
            dialog:AddText(
                "Bạn cần trang bị phi phong cấp Lăng Tuyệt trở lên để vào liên máy chủ.")
            dialog:AddSelection(999, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return
        end
        dialog:AddText("Bạn đang có </color=yellow>" .. Player.CheckMoney(player, 6) ..
                           " Bạc khóa liên máy chủ</color>, bạn có muốn vào liên máy chủ không?")

        dialog:AddSelection(110016, "Vào liên máy chủ");
        dialog:AddSelection(110012, "Chưa muốn vào lúc này");

        dialog:Show(npc, player)

        return
    end
	
	if selectionID == 10017 then
		player:ChangeScene(1616, 7545, 4454, 9997)
        GUI.CloseDialog(player)
        return
	end
	
    if selectionID == 110016 then
        player:ChangeScene(1616, 7545, 4454, 9999)
        GUI.CloseDialog(player)
        return
    end
	
	
    if selectionID == 110012 then
        GUI.CloseDialog(player)
        return
    end
	
    if selectionID == 110013 then
        Player.SetValueOfDailyRecore(player, 100012, -1)
        GUI.CloseDialog(player)
        return
    end
    if selectionID == 10012 then
        player:SetLevel(89)
        dialog:AddText("Thiết lập cấp độ 89 thành công")
        dialog:Show(npc, player)
    end
    if selectionID == 10024 then
        player:SetLevel(99)
        dialog:AddText("Thiết lập cấp độ 99 thành công")
        dialog:Show(npc, player)
    end
    if selectionID == 10013 then
        Player.OpenShop(npc, player, 226)
        GUI.CloseDialog(player)

    end
    if selectionID == 10014 then
        player:SetPrayTimes(100)
        dialog:AddText("nhận 100 lượt quay chúc chúc phúc thành công !")
        dialog:Show(npc, player)
    end
    if selectionID == 10015 then
        Player.AddItemLua(player, 3508, 1, -1, 1)
        dialog:AddText("" .. npc:GetName() .. "Bạn đã nhận </color=red>thần thú </color>thành công")
        dialog:Show(npc, player)
        return
    end
    if selectionID == 100020 then
        Player.AddCurGatherPoint(player, 10000)
        Player.AddMakePoint(player, 10000)
        dialog:AddText("" .. npc:GetName() .. "Nhận tinh lực hoạt lực thành công")
        dialog:Show(npc, player)
    end
    if selectionID == 100030 then
        GUI.OpenUI(player, "UIGiftCode")
        GUI.CloseDialog(player)
        return
    end
    if selectionID == 10002 then
        if player:GetFactionID() == 0 then
            dialog:AddText("" .. npc:GetName() ..
                               ": Bạn chưa gia nhập phái ,hãy gia nhập <color=red> môn phái</color> rồi quay lại <color=red> nhận Nhận Mật Tịch (cao).</color>")
            dialog:Show(npc, player)
        else
            if player:GetFactionID() == 1 then
                Player:AddItemLua(player, secretID[1].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[1].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 2 then
                Player:AddItemLua(player, secretID[2].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[2].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 3 then
                Player:AddItemLua(player, secretID[3].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[3].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 4 then
                Player:AddItemLua(player, secretID[4].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[4].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 5 then
                Player:AddItemLua(player, secretID[5].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[5].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 6 then
                Player:AddItemLua(player, secretID[6].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[6].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 7 then
                Player:AddItemLua(player, secretID[7].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[7].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 8 then
                Player:AddItemLua(player, secretID[8].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[8].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 9 then
                Player:AddItemLua(player, secretID[9].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[9].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 10 then
                Player:AddItemLua(player, secretID[10].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[10].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 11 then
                Player:AddItemLua(player, secretID[11].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[11].ItemID2, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 12 then
                Player:AddItemLua(player, secretID[12].ItemID1, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[12].ItemID2, 1, Player.GetSeries(player), 1)
            end
            dialog:AddText("" .. npc:GetName() .. "Bạn đã nhận </color=red>Mật tịch (cao)</color>thành công")
            dialog:Show(npc, player)
        end
    elseif selectionID == 10001 then
        if player:GetFactionID() == 0 then
            dialog:AddText("" .. npc:GetName() ..
                               ": Bạn chưa gia nhập phái ,hãy gia nhập <color=red> môn phái</color> rồi quay lại <color=red> nhận Nhận Mật Tịch (trung).</color>")
            dialog:Show(npc, player)
        else
            if player:GetFactionID() == 1 then
                Player:AddItemLua(player, secretID[1].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[1].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 2 then
                Player:AddItemLua(player, secretID[2].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[2].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 3 then
                Player:AddItemLua(player, secretID[3].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[3].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 4 then
                Player:AddItemLua(player, secretID[4].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[4].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 5 then
                Player:AddItemLua(player, secretID[5].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[5].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 6 then
                Player:AddItemLua(player, secretID[6].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[6].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 7 then
                Player:AddItemLua(player, secretID[7].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[7].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 8 then
                Player:AddItemLua(player, secretID[8].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[8].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 9 then
                Player:AddItemLua(player, secretID[9].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[9].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 10 then
                Player:AddItemLua(player, secretID[10].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[10].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 11 then
                Player:AddItemLua(player, secretID[11].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[11].ItemID4, 1, Player.GetSeries(player), 1)
            elseif player:GetFactionID() == 12 then
                Player:AddItemLua(player, secretID[12].ItemID3, 1, Player.GetSeries(player), 1)
                Player:AddItemLua(player, secretID[12].ItemID4, 1, Player.GetSeries(player), 1)
            end
            dialog:AddText("" .. npc:GetName() ..
                               "Bạn đã nhận </color=red>Mật tịch (trung)</color>thành công")
            dialog:Show(npc, player)
        end
        -- if Player.CheckMoney(player,2) >= 10000000 then
        --	dialog:AddText("Trên người ngươi quá nhiều <color=#ffd24d>(Đồng)</color>không thể nhân được nữa.")
        --	dialog:Show(npc, player)
        -- else
        --	Player.AddMoney(player,5000000,2)
        --	dialog:AddText("Ngươi đã nhận <color=#ffd24d>500 vạn (đồng)</color>thành công")
        --	dialog:Show(npc, player)
        -- end
    elseif selectionID == 10000 then
        player:SetLevel(109)
        dialog:AddText("Thiết lập cấp độ 109 thành công")
        dialog:Show(npc, player)
    elseif selectionID == 11111 then
        player:SetLevel(119)
        dialog:AddText("Thiết lập cấp độ 109 thành công")
        dialog:Show(npc, player)
    elseif selectionID == 10003 then
        Player:AddItemLua(player, 190, 3, 0, 1)
        dialog:AddText("Bạn dã nhận được 3 viên huyền tinh 8")
        dialog:Show(npc, player)
    elseif selectionID == 10004 then -- NPC_Test:SetBelonging(npc, player, ItemID,Number,Series,LockStatus) set do +14
        if player:GetFactionID() == 0 then
            dialog:AddText("" .. npc:GetName() ..
                               ": Bạn chưa gia nhập phái ,hãy gia nhập <color=red> môn phái</color> rồi quay lại <color=red> nhận Nhận Mật Tịch (cao).</color>")
            dialog:Show(npc, player)
        elseif player:GetFactionID() ~= 0 then
            if Player.GetSeries(player) == 1 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[1].ItemID22, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 2 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID15, 1, -1, 1)

                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[2].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 3 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID13, 1, -1, 1)

                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[3].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 4 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID30, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID24, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID30, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 5 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[5].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging(npc, player, SetBelongings[4].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end

            end
        else
            dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
            dialog:Show(npc, player)
        end
    end

    if selectionID == 14 then
        GUI.CloseDialog(player)
    end
    if selectionID == 10023 then -- NPC_Test:SetBelonging(npc, player, ItemID,Number,Series,LockStatus) set do +12
        if player:GetFactionID() == 0 then
            dialog:AddText("" .. npc:GetName() ..
                               ": Bạn chưa gia nhập phái ,hãy gia nhập <color=red> môn phái</color> rồi quay lại <color=red> nhận Nhận Mật Tịch (cao).</color>")
            dialog:Show(npc, player)
        elseif player:GetFactionID() ~= 0 then
            if Player.GetSeries(player) == 1 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[1].ItemID22, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 2 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID15, 1, -1, 1)

                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[2].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 3 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID13, 1, -1, 1)

                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[3].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 4 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID30, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID24, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID30, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 5 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[5].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89(npc, player, SetBelongings89[4].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end

            end
        else
            dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
            dialog:Show(npc, player)
        end
    end
    if selectionID == 10025 then -- NPC_Test:SetBelonging(npc, player, ItemID,Number,Series,LockStatus) set do  + 8
        if player:GetFactionID() == 0 then
            dialog:AddText("" .. npc:GetName() ..
                               ": Bạn chưa gia nhập phái ,hãy gia nhập <color=red> môn phái</color> rồi quay lại <color=red> nhận Nhận Mật Tịch (cao).</color>")
            dialog:Show(npc, player)
        elseif player:GetFactionID() ~= 0 then
            if Player.GetSeries(player) == 1 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[1].ItemID22, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 2 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID15, 1, -1, 1)

                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[2].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 3 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID13, 1, -1, 1)

                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[3].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 4 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID30, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID14, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID15, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID24, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID30, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end
            elseif Player.GetSeries(player) == 5 then
                if player:GetSex() == 0 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID1, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID2, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID3, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID4, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID5, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID6, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID7, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID8, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID9, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID13, 1, -1, 1)
                elseif player:GetSex() == 1 then
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID10, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID11, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID12, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID13, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID16, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID17, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID18, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID19, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID20, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID21, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID22, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[5].ItemID23, 1, -1, 1)
                    NPC_Test:SetBelonging89Set8(npc, player, SetBelongings89[4].ItemID24, 1, -1, 1)
                else
                    dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
                    dialog:Show(npc, player)
                end

            end
        else
            dialog:AddText("<color=red>Lỗi rồi</color>.Hãy báo cho GM để được hỗ trợ!!!")
            dialog:Show(npc, player)
        end
    end
    if selectionID == 1 then
        if player.GetLevel() >= 50 then
            local dialog = GUI.CreateNPCDialog()
            dialog:AddText("Chọn môn phái muốn gia nhập.")
            for key, value in pairs(Global_FactionName) do
                dialog:AddSelection(100 + key, value)
            end
            dialog:Show(npc, player)

            return
        else
            local dialog = GUI.CreateNPCDialog()
            dialog:AddText("Bạn chưa đủ cấp độ, hãy lên level 50 rồi quay lại!")
            dialog:Show(npc, player);
        end

    end
    -- ************************** --
    if selectionID >= 100 and selectionID <= #Global_FactionName + 100 then
        NPC_Test:JoinFaction(scene, npc, player, selectionID - 100)

        return
    end
    -- ************************** --
    if selectionID == 2 or selectionID == 3 then
        local randomArmor = {
            Female = {3181, 3182, 3183, 3184, 3185, 3186, 3187, 3188, 3189, 3190, 3191},
            Male = {3171, 3172, 3173, 3174, 3175, 3176, 3177, 3178, 3179, 3180}
        }
        local randomWeapon = {3766, 3776, 3786, 3736, 3746, 3756}
        local randomHelm = {
            Female = {3429, 3430, 3431, 3432, 3433, 3434, 3435, 3436, 3437, 3438},
            Male = {3419, 3420, 3421, 3422, 3423, 3424, 3425, 3426, 3427, 3428}
        }
        local randomMantle = {
            Female = {3669, 3670, 3671, 3672, 3673, 3674, 3675, 3676, 3677, 3678},
            Male = {3619, 3620, 3621, 3622, 3623, 3624, 3625, 3626, 3627, 3628}
        }
        local randomHorse = {3459, 3460, 3461, 3462, 3463, 3464, 3465, 3466, 3467, 3468, 3469, 3470}
        local randRange = 500

        local count = 1
        if selectionID == 2 then
            count = 10
        elseif selectionID == 3 then
            count = 20
        end

        for i = 1, count do
            local npcPos = npc:GetPos()
            local randPosX, randPosY = npcPos:GetX() + math.random(randRange) - math.random(randRange),
                npcPos:GetY() + math.random(randRange) - math.random(randRange)
            local randSex = math.random(0, 1)
            local randRiding = math.random(0, 1)
            local randMaxHP = math.random(100, 2000)
            local randHP = math.random(50, randMaxHP)
            local randSeries = math.random(1, 5)

            local botBuilder = EventManager.CreateBotBuilder(scene)
            botBuilder:SetPos(randPosX, randPosY)
            botBuilder:SetName("Bot")
            botBuilder:SetTitle("Bot Title")
            botBuilder:SetLevel(100)
            botBuilder:SetFaction(3, 1)
            botBuilder:SetSex(randSex)
            if randSex == 0 then
                botBuilder:SetAvarta(2)
            else
                botBuilder:SetAvarta(101)
            end
            if randRiding == 0 then
                botBuilder:SetIsRiding(false)
            else
                botBuilder:SetIsRiding(true)
            end
            botBuilder:SetAIID(-1)
            botBuilder:SetDirection(-1)
            botBuilder:AddEquip(randomWeapon[math.random(#randomWeapon)], randSeries, 16)
            botBuilder:AddEquip(randomHorse[math.random(#randomHorse)], 0, 0)
            if randSex == 0 then
                botBuilder:AddEquip(randomArmor.Male[math.random(#randomArmor.Male)], randSeries, 0)
                botBuilder:AddEquip(randomHelm.Male[math.random(#randomHelm.Male)], randSeries, 0)
                botBuilder:AddEquip(randomMantle.Male[math.random(#randomMantle.Male)], 0, 0)
            else
                botBuilder:AddEquip(randomArmor.Female[math.random(#randomArmor.Female)], randSeries, 0)
                botBuilder:AddEquip(randomHelm.Female[math.random(#randomHelm.Female)], randSeries, 0)
                botBuilder:AddEquip(randomMantle.Female[math.random(#randomMantle.Female)], 0, 0)
            end
            botBuilder:SetHP(randHP)
            botBuilder:SetHPMax(randMaxHP)
            botBuilder:Build()
        end

        NPC_Test:ShowDialog(npc, player, "Tạo BOT thành công!")
        return
    end
    -- ************************** --
    if selectionID == 4 then
        player:ChangeScene(25, 11530, 2070)
        return
    end
    -- ************************** --
    if selectionID == 5 then
        GUI.OpenUI(player, "UISignetEnhance")

        return
    end
    -- ************************** --
    if selectionID == 6 then
        GUI.OpenUI(player, "UIEnhance")

        return
    end
    -- ************************** --
    if selectionID == 7 then
        GUI.OpenUI(player, "UICrystalStoneSynthesis")

        return
    end
    -- ************************** --
    if selectionID == 8 then
        GUI.OpenUI(player, "UISplitEquipCrystalStones", 8, 80)

        return
    end
    -- ************************** --
    if selectionID == 9 then
        GUI.OpenUI(player, "UISplitCrystalStone", 10, 80)

        return
    end
    -- ************************** --
    if selectionID == 10 then
        -- Boss
        local Boss = {
            ID = 2661,
            Name = "",
            Title = "",
            BaseHP = 10000,
            HPIncreaseEachLevel = 10000,
            AIType = 2,
            SpawnAfter = 10000,
            ScriptID = -1
        }

        local level = 120

        local monsterBuilder = EventManager.CreateMonsterBuilder(scene)
        monsterBuilder:SetResID(Boss.ID)
        monsterBuilder:SetPos(player:GetPos())
        monsterBuilder:SetName(Boss.Name)
        monsterBuilder:SetTitle(Boss.Title)
        monsterBuilder:SetLevel(level)
        monsterBuilder:SetMaxHP(Boss.BaseHP + level * Boss.HPIncreaseEachLevel)
        monsterBuilder:SetDirection(-1)
        monsterBuilder:SetSeries(0)
        monsterBuilder:SetAIType(Boss.AIType)
        monsterBuilder:SetScriptID(Boss.ScriptID)
        monsterBuilder:SetTag("Boss")
        monsterBuilder:SetRespawnTick(-1)
        monsterBuilder:Build()

        return
    end
    -- ************************** --
    if selectionID == 11 then
        local dialog = GUI.CreateNPCDialog()
        dialog:AddText("Danh sách vật phẩm kèm Selection")
        dialog:AddSelection(111, "Selection 1")
        dialog:AddSelection(112, "Selection 2")
        dialog:AddItem(132, 100, 1)
        dialog:AddItem(132, 50, 0)
        dialog:AddItem(164, 1, 1)
        dialog:SetItemSelectable(false)
        dialog:Show(npc, player)
        return
    end
    -- ************************** --
    if selectionID == 111 and selectionID == 112 then
        local dialog = GUI.CreateNPCDialog()
        dialog:AddText("Chọn Selection 1")
        dialog:Show(npc, player)
        return
    end
    -- ************************** --
    if selectionID == 12 then
        local dialog = GUI.CreateNPCDialog()
        dialog:AddText("Danh sách vật phẩm kèm Selection")
        dialog:AddSelection(111, "Selection 1")
        dialog:AddSelection(112, "Selection 2")
        dialog:AddItem(132, 100, 1)
        dialog:AddItem(132, 50, 0)
        dialog:AddItem(164, 1, 1)
        dialog:SetItemSelectable(true)
        dialog:Show(npc, player)
        return
    end
    -- ************************** --
    if selectionID == 13 then
        player:ChangeScene(25, 6945, 2784)
        return
    end
    -- ************************** --

    if selectionID == 100031 then
        if player:IsGM() then
            GUI:ShowInputNumber(player, "ID Record", function(value)
                Player:SetValueOfDailyRecore(player, value, -1)
                Player:SetValueOfWeekRecore(player, value, -1)
                Player:SetValueOfForeverRecore(player, value, -1)
                Player:SetValueOfMonthRecord(player, value, -1)
                player.AddNotification("Reset record " .. value .. "thành công.")
            end) 
        end
    end

end

-- ****************************************************** --
--	Hàm này được gọi khi có sự kiện người chơi chọn một trong các vật phẩm, và ấn nút Xác nhận cung cấp bởi NPC thông qua NPC Dialog
--		scene: Scene - Bản đồ hiện tại
--		npc: NPC - NPC tương ứng
--		player: Player - NPC tương ứng
--		selectedItemInfo: SelectItem - Vật phẩm được chọn
--		otherParams: Key-Value {number, string} - Danh sách các tham biến khác
-- ****************************************************** --
function NPC_Test:OnItemSelected(scene, npc, player, selectedItemInfo, otherParams)

    -- ************************** --
    local itemID = selectedItemInfo:GetItemID()
    local itemNumber = selectedItemInfo:GetItemCount()
    local binding = selectedItemInfo:GetBinding()
    -- ************************** --
    NPC_Test:ShowDialog(npc, player, string.format("Chọn vật phẩm ID: %d, Count: %d, Binding: %d!", itemID,
        itemNumber, binding))
    -- ************************** --

end

-- ======================================================= --
-- ======================================================= --
function NPC_Test:JoinFaction(scene, npc, player, factionID)

    -- ************************** --
    local ret = player:JoinFaction(factionID)
    -- ************************** --
    if ret == -1 then
        NPC_Test:ShowDialog(npc, player, "Người chơi không tồn tại!")
        return
    elseif ret == -2 then
        NPC_Test:ShowDialog(npc, player, "Môn phái không tồn tại!")
        return
    elseif ret == 0 then
        NPC_Test:ShowDialog(npc, player, "Giới tính của bạn không phù hợp với môn phái này!")
        return
    elseif ret == 1 then
        NPC_Test:ShowDialog(npc, player,
            "Gia nhập phái <color=blue>" .. player:GetFactionName() .. "</color> thành công!")
        return
    else
        NPC_Test:ShowDialog(npc, player, "Chuyển phái thất bại, lỗi chưa rõ!")
        return
    end
    -- ************************** --

end

-- ======================================================= --
-- ======================================================= --
function NPC_Test:GetValueTest(scene, npc, player)

    -- ************************** --
    return 1, 2
    -- ************************** --

end

-- ======================================================= --
-- ======================================================= --
function NPC_Test:ShowDialog(npc, player, text)

    -- ************************** --
    local dialog = GUI.CreateNPCDialog()
    dialog:AddText(text)
    dialog:Show(npc, player)
    -- ************************** --

end
function NPC_Test:PlayerSetLevel(npc, player, level)

    -- ************************** --
    player:SetLevel(level)
    NPC_Test:ShowDialog(npc, player, "Thiết lập cấp độ " .. level .. " thành công")
    -- ************************** --

end
function NPC_Test:SetBelonging(npc, player, ItemID, Number, Series, LockStatus)

    if ItemID == nil then
        return
    end
    if Series == nil then
        return
    end
    -- ************************** --
    Player.AddItemLua(player, ItemID, Number, Series, LockStatus, 14)
    NPC_Test:ShowDialog(npc, player, "Nhận phẩm thành công theo hệ <color=red>Thành công !+14</color>")
    -- ************************** --
end
function NPC_Test:SetBelonging89(npc, player, ItemID, Number, Series, LockStatus)

    if ItemID == nil then
        return
    end
    if Series == nil then
        return
    end
    -- ************************** --
    Player.AddItemLua(player, ItemID, Number, Series, LockStatus, 12)
    NPC_Test:ShowDialog(npc, player, "Nhận phẩm thành công theo hệ <color=red>Thành công !+12</color>")
    -- ************************** --
end
function NPC_Test:EquipEnhance(npc, player, ItemID, enhLevel)
    -- ************************** --
    Player.EquipEnhance(player, ItemID, enhLevel) -- ************************** --
end

