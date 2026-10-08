GLOBAL_SERVERCONFIG_STR = System:ReadAllText('ServerConfig.json')
GLOBAL_SERVERCONFIG = json.parse(GLOBAL_SERVERCONFIG_STR)

--[[
    Script này được tải mặc định cùng hệ thống.
    Toàn bộ các biến, hàm bên dưới đều là Global và có thể được dùng ở mọi Script khác.
]] -- Danh sách đối tượng trong hệ thống
Global_ObjectTypes = {
    -- Người chơi
    OT_CLIENT = 0,
    -- Quái
    OT_MONSTER = 1,
    -- NPC
    OT_NPC = 5,
    -- Điểm thu thập
    OT_GROWPOINT = 9,
    -- Khu vực động
    OT_DYNAMIC_AREA = 10
}

-- Dữ liệu phụ bản
Global_Params = {
    -- Bạch Hổ Đường
    BaiHuTang_BeginRegistered = false, -- Đang mở báo danh
    BaiHuTang_Stage = 0 -- Đang ở tầng nào
    -- End
}

-- Trạng thái PK
Global_PKMode = {
    -- Hòa bình
    Peace = 0,
    -- Phe
    Team = 1,
    -- Bang hội
    Guild = 2,
    -- Đồ sát
    All = 3,
    -- Thiện ác
    Moral = 4,
    -- Tùy chọn tùy theo thiết lập sự kiện
    Custom = 5
}

-- ID hoạt động
Global_Events = {
    -- Bạch Hổ Đường
    BaiHuTang = 10
}

-- Danh sách ID môn phái
Global_FactionID = {
    -- Không có
    None = 0,
    -- Thiếu Lâm
    ShaoLin = 1,
    -- Thiên Vương
    TianWang = 2,
    -- Đường Môn
    TangMen = 3,
    -- Ngũ Độc
    WuDu = 4,
    -- Nga My
    EMei = 5,
    -- Thúy Yên
    CuiYan = 6,
    -- Cái Bang
    GaiBang = 7,
    -- Thiên Nhẫn
    TianRen = 8,
    -- Võ Đang
    WuDang = 9,
    -- Côn Lôn
    KunLun = 10,
    -- Minh Giáo
    MingJiao = 11,
    -- Đoàn Thị
    DaLiDuanShi = 12,
    -- Cổ Mộ
    CoMo = 13
}
Global_TypeMoney = {
    BacKhoa = 0,
    Bac = 1,
    Dong = 2,
    DongKhoa = 3,

    BacKhoaLienServer = 6
}
Global_TypeMoneyName = {
    [Global_TypeMoney.BacKhoa] = "Bạc Khóa",
    [Global_TypeMoney.Bac] = "Bạc",
    [Global_TypeMoney.Dong] = "KNB",
    [Global_TypeMoney.DongKhoa] = "KNB Khóa",
    [Global_TypeMoney.BacKhoaLienServer] = " Bạc Khóa liên máy chủ"
}

-- Chức vị trong bang
Global_GuildRank = {
    -- Bang chúng
    Member = 0,
    -- Bang chủ
    Master = 1,
    -- Phó bang chủ
    ViceMaster = 2,
    -- Trưởng lão
    Ambassador = 3,
    -- Tinh anh
    Elite = 4
}

-- Chức vị trong tộc
Global_FamilyRank = {
    -- Thành Viên
    Member = 0,
    -- Tộc Trưởng
    Master = 1,
    -- Tộc Phó
    ViceMaster = 2
}

Global_CAMP = {
    [101] = {
        NameActivity = "Lệnh Bài Nghĩa Quân"
    },
    [102] = {
        NameActivity = "Lệnh Bài Quân doanh"
    },
    [103] = {
        NameActivity = "Lệnh Bài Học Tạo Đồ"
    },
    [201] = {
        NameActivity = "Lệnh Bài Dương Châu"
    },
    [202] = {
        NameActivity = "Lệnh Bài Phượng Tường"
    },
    [203] = {
        NameActivity = "Lệnh Bài Tương Dương"
    },
    [301] = {
        NameActivity = "Lệnh Bài Thiếu Lâm"
    },
    [302] = {
        NameActivity = "Lệnh Bài Thiên Vương"
    },
    [303] = {
        NameActivity = "Lệnh Bài Đường Môn"
    },
    [304] = {
        NameActivity = "Lệnh Bài Ngũ Độc"
    },
    [305] = {
        NameActivity = "Lệnh Bài Nga My"
    },
    [306] = {
        NameActivity = "Lệnh Bài Thúy Yên"
    },
    [307] = {
        NameActivity = "Lệnh Bài Cái Bang"
    },
    [308] = {
        NameActivity = "Lệnh Bài Thiên Nhẫn"
    },
    [309] = {
        NameActivity = "Lệnh Bài Võ Đang"
    },
    [310] = {
        NameActivity = "Lệnh Bài Côn Lôn"
    },
    [311] = {
        NameActivity = "Lệnh Bài Minh Giáo"
    },
    [312] = {
        NameActivity = "Lệnh Bài Đại Lý Đoàn Thị"
    },
    [401] = {
        NameActivity = "Lệnh Bài Ải gia tộc"
    },
    [501] = {
        NameActivity = "Lệnh Bài Bạch Hổ-"
    },
    [502] = {
        NameActivity = "Lệnh Bài Thịnh Hạ 2008"
    },
    [503] = {
        NameActivity = "Lệnh Bài Tiêu Dao Cốc"
    },
    [504] = {
        NameActivity = "Lệnh Bài Chúc phúc"
    },
    [505] = {
        NameActivity = "Lệnh Bài Hoạt động Thịnh Hạ 2010"
    },
    [506] = {
        NameActivity = "Lệnh Bài Di tích Hàn Vũ"
    },
    [507] = {
        NameActivity = "Lệnh Bài Mỹ nhân"
    },
    [508] = {
        NameActivity = "Lệnh Bài VIP"
    },
    [601] = {
        NameActivity = "Lệnh Bài Cao thủ (kim)"
    },
    [602] = {
        NameActivity = "Lệnh Bài Cao thủ (mộc)"
    },
    [603] = {
        NameActivity = "Lệnh Bài Cao thủ (thủy)"
    },
    [604] = {
        NameActivity = "Lệnh Bài Cao thủ (hỏa)"
    },
    [605] = {
        NameActivity = "Lệnh Bài Cao thủ (thổ)"
    },
    [701] = {
        NameActivity = "Lệnh Bài Võ Lâm Liên Đấu"
    },
    [801] = {
        NameActivity = "Danh vọng Tranh Đoạt Lãnh Thổ"
    },
    [901] = {
        NameActivity = "Lệnh Bài Tần Lăng-Quan Phủ"
    },
    [902] = {
        NameActivity = "Lệnh Bài Tần Lăng-Phát Khâu Môn"
    },
    [1001] = {
        NameActivity = "Lệnh Bài Đoàn viên dân tộc"
    },
    [1101] = {
        NameActivity = "Lệnh Bài Đại Hội Võ Lâm"
    },
    [1201] = {
        NameActivity = "Lệnh Bài Liên đấu liên server"
    }
}

-- Danh sách môn phái
Global_FactionName = {
    [Global_FactionID.None] = "Không có",
    [Global_FactionID.ShaoLin] = "Thiếu Lâm",
    [Global_FactionID.TianWang] = "Thiên Vương",
    [Global_FactionID.TangMen] = "Đường Môn",
    [Global_FactionID.WuDu] = "Ngũ Độc",
    [Global_FactionID.EMei] = "Nga My",
    [Global_FactionID.CuiYan] = "Thúy Yên",
    [Global_FactionID.GaiBang] = "Cái Bang",
    [Global_FactionID.TianRen] = "Thiên Nhẫn",
    [Global_FactionID.WuDang] = "Võ Đang",
    [Global_FactionID.KunLun] = "Côn Lôn",
    [Global_FactionID.MingJiao] = "Minh Giáo",
    [Global_FactionID.DaLiDuanShi] = "Đoàn Thị",
    [Global_FactionID.CoMo] = "Cổ Mộ"
}

-- danh sách các phái Truyền thống phù Item--
-- secret1 = mật tịch 1 - 2;
-- press = ngũ hành ấn 1-2;
Global_NameMapItemPhaiID = {
    [9] = {
        ID = 9,
        Name = "Thiếu Lâm Phái",
        PosX = 4695,
        PosY = 2929,
        FactionID = 1,
        secret1 = 3232,
        secret2 = 3233,
        press = 3864
    },
    [22] = {
        ID = 22,
        Name = "Thiên Vương Bang",
        PosX = 4114,
        PosY = 3372,
        FactionID = 2,
        secret1 = 3234,
        secret2 = 3235,
        press = 3865
    },
    [18] = {
        ID = 18,
        Name = "Đường Môn ",
        PosX = 3725,
        PosY = 3215,
        FactionID = 3,
        secret1 = 3236,
        secret2 = 3237,
        press = 3866
    },
    [20] = {
        ID = 20,
        Name = "Ngũ Độc Giáo",
        PosX = 4891,
        PosY = 1518,
        FactionID = 4,
        secret1 = 3238,
        secret2 = 3239,
        press = 3867
    },
    [16] = {
        ID = 16,
        Name = "Nga My Phái",
        PosX = 6476,
        PosY = 5090,
        FactionID = 5,
        secret1 = 3240,
        secret2 = 3241,
        press = 3868
    },
    [17] = {
        ID = 17,
        Name = "Thúy Yên Môn",
        PosX = 6849,
        PosY = 3108,
        FactionID = 6,
        secret1 = 3242,
        secret2 = 3243,
        press = 3869
    },
    [15] = {
        ID = 15,
        Name = "Cái Bang Phái",
        PosX = 2557,
        PosY = 2166,
        FactionID = 7,
        secret1 = 3244,
        secret2 = 3245,
        press = 3870
    },
    [10] = {
        ID = 10,
        Name = "Thiên Nhẫn Giáo",
        PosX = 3643,
        PosY = 3911,
        FactionID = 8,
        secret1 = 3246,
        secret2 = 3247,
        press = 3871
    },
    [14] = {
        ID = 14,
        Name = "Võ Đang Phái",
        PosX = 5121,
        PosY = 3057,
        FactionID = 9,
        secret1 = 3248,
        secret2 = 3249,
        press = 3872
    },
    [12] = {
        ID = 12,
        Name = "Côn Lôn Phái",
        PosX = 3648,
        PosY = 2126,
        FactionID = 10,
        secret1 = 3250,
        secret2 = 3251,
        press = 3873
    },
    [224] = {
        ID = 224,
        Name = "Minh Giáo ",
        PosX = 4901,
        PosY = 2699,
        FactionID = 11,
        secret1 = 3252,
        secret2 = 3253,
        press = 3874
    },
    [19] = {
        ID = 19,
        Name = "Đoàn Thị Phái",
        PosX = 3464,
        PosY = 3218,
        FactionID = 12,
        secret1 = 3254,
        secret2 = 3255,
        press = 3875
    },
    [2000] = {
        ID = 2000,
        Name = "Cổ Mộ",
        PosX = 2425,
        PosY = 650,
        FactionID = 13,
        secret1 = 3254,
        secret2 = 3255,
        press = 3875
    }
}
-- danh sách thành  Truyền thống phù Item--
Global_NameMapItemThanhID = {

    [23] = {
        ID = 23,
        Name = "Biện Kinh Phủ",
        PosX = 6754,
        PosY = 3590
    },
    [26] = {
        ID = 26,
        Name = "Dương Châu Phủ",
        PosX = 8904,
        PosY = 4377
    },

    [29] = {
        ID = 29,
        Name = "Lâm An Phủ",
        PosX = 7549,
        PosY = 4455
    },
    [25] = {
        ID = 25,
        Name = "Tương Dương Phủ",
        PosX = 7860,
        PosY = 4218
    },
    [27] = {
        ID = 27,
        Name = "Thành Đô Phủ",
        PosX = 7155,
        PosY = 3175
    },
    [24] = {
        ID = 24,
        Name = "Phượng Tường Phủ",
        PosX = 8054,
        PosY = 3581
    },
    [28] = {
        ID = 28,
        Name = "Đại Lý Phủ",
        PosX = 2929,
        PosY = 2087
    }

}
-- danh sách các thôn Truyền thống phù Item --
Global_NameMapItemThonID = {
    [5] = {
        ID = 5,
        Name = "Giang Tân Thôn",
        PosX = 5237,
        PosY = 2910
    },
    [3] = {
        ID = 3,
        Name = "Vĩnh Lạc Trấn ",
        PosX = 4297,
        PosY = 3689
    },
    [8] = {
        ID = 8,
        Name = "Ba Lăng Huyện ",
        PosX = 5577,
        PosY = 3740
    },
    [2] = {
        ID = 2,
        Name = "Long Môn Trấn",
        PosX = 6255,
        PosY = 2340
    },
    [6] = {
        ID = 6,
        Name = "Thạch Cổ Trấn ",
        PosX = 7943,
        PosY = 3577
    },
    [4] = {
        ID = 4,
        Name = "Đạo Hương Thôn",
        PosX = 5204,
        PosY = 3174
    },
    [1] = {
        ID = 1,
        Name = "Vân Trung Trấn",
        PosX = 3199,
        PosY = 3595
    },
    [7] = {
        ID = 7,
        Name = "Long Tuyền Thôn ",
        PosX = 4710,
        PosY = 4288
    }
}
-- danh sách các Phai Xa phu--
Global_NameMapPhaiID = {
    [9] = {
        ID = 9,
        Name = "Thiếu Lâm Phái",
        PosX = 4695,
        PosY = 2929
    },
    [22] = {
        ID = 22,
        Name = "Thiên Vương Bang",
        PosX = 4114,
        PosY = 3372
    },
    [18] = {
        ID = 18,
        Name = "Đường Môn ",
        PosX = 3725,
        PosY = 3215
    },
    [20] = {
        ID = 20,
        Name = "Ngũ Độc Giáo",
        PosX = 4891,
        PosY = 1518
    },
    [16] = {
        ID = 16,
        Name = "Nga My Phái",
        PosX = 6476,
        PosY = 5090
    },
    [17] = {
        ID = 17,
        Name = "Thúy Yên Môn",
        PosX = 6849,
        PosY = 3108
    },
    [15] = {
        ID = 15,
        Name = "Cái Bang Phái",
        PosX = 2557,
        PosY = 2166
    },
    [10] = {
        ID = 10,
        Name = "Thiên Nhẫn Giáo",
        PosX = 3643,
        PosY = 3911
    },
    [14] = {
        ID = 14,
        Name = "Võ Đang Phái",
        PosX = 5121,
        PosY = 3057
    },
    [12] = {
        ID = 12,
        Name = "Côn Lôn Phái",
        PosX = 3648,
        PosY = 2126
    },
    [224] = {
        ID = 224,
        Name = "Minh Giáo ",
        PosX = 4901,
        PosY = 2699
    },
    [19] = {
        ID = 19,
        Name = "Đoàn Thị Phái",
        PosX = 3464,
        PosY = 3218
    },
    [2000] = {
        ID = 2000,
        Name = "Cổ Mộ",
        PosX = 2425,
        PosY = 650
    }
}
-- danh sách thành Các xa phu--
Global_NameMapThanhID = {
    [23] = {
        ID = 23,
        Name = "Biện Kinh Phủ",
        PosX = 4041,
        PosY = 2443
    },
    [26] = {
        ID = 26,
        Name = "Dương Châu Phủ",
        PosX = 8904,
        PosY = 4377
    },
    [29] = {
        ID = 29,
        Name = "Lâm An Phủ",
        PosX = 7549,
        PosY = 4455
    },
    [25] = {
        ID = 25,
        Name = "Tương Dương Phủ",
        PosX = 10975,
        PosY = 2871
    },
    [27] = {
        ID = 27,
        Name = "Thành Đô Phủ",
        PosX = 7155,
        PosY = 3175
    },
    [24] = {
        ID = 24,
        Name = "Phượng Tường Phủ",
        PosX = 8054,
        PosY = 3581
    },
    [28] = {
        ID = 28,
        Name = "Đại Lý Phủ",
        PosX = 2929,
        PosY = 2087
    }
}
-- danh sách các thôn xa phu--
Global_NameMapThonID = {
    [5] = {
        ID = 5,
        Name = "Giang Tân Thôn",
        PosX = 6141,
        PosY = 2227
    },
    [3] = {
        ID = 3,
        Name = "Vĩnh Lạc Trấn ",
        PosX = 9989,
        PosY = 4312
    },
    [8] = {
        ID = 8,
        Name = "Ba Lăng Huyện ",
        PosX = 2972,
        PosY = 5502
    },
    [2] = {
        ID = 2,
        Name = "Long Môn Trấn",
        PosX = 3512,
        PosY = 1760
    },
    [6] = {
        ID = 6,
        Name = "Thạch Cổ Trấn ",
        PosX = 5880,
        PosY = 861
    },
    [4] = {
        ID = 4,
        Name = "Đạo Hương Thôn",
        PosX = 5468,
        PosY = 2105
    },
    [1] = {
        ID = 1,
        Name = "Vân Trung Trấn",
        PosX = 8242,
        PosY = 675
    },
    [7] = {
        ID = 7,
        Name = "Long Tuyền Thôn ",
        PosX = 7467,
        PosY = 2988
    }
}

Global_KuafuMap = {123, 126, 132, 1616, 1642, 1638, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359,
                   360, 1539, 1540, 1541, 2222, 1402, 1426}

function Global_IsInTet2023Event()
    local START_DATE = '2023-01-12 00:00:00'
    local END_DATE = '2023-02-05 00:00:00'
    return System.IsDateTimeInRange(START_DATE, END_DATE)
end

function Global_IsInTet2023LiXiEvent()
    local START_DATE = '2023-01-22 00:00:00'
    local END_DATE = '2023-01-24 23:59:59'
    return System.IsDateTimeInRange(START_DATE, END_DATE)
end

Global_EarlyAccess = {
    ["<color=#0486dc>[GM]</color> HạoHiên"] = true,
    ["<color=#0486dc>[GM]</color> HoaVũCơ"] = true,
    ["<color=#0486dc>[GM]</color> HoaNhi"] = true,
    ["<color=#0486dc>[GM]</color> HoaHoaS5"] = true,
    ["<color=#0486dc>[GM]</color> 123Dzoo"] = true,
    ["<color=#0486dc>[GM]</color> MaMaMa1"] = true,
    ["<color=#0486dc>[GM]</color> HoaS4s4"] = true,
    ["<color=#0486dc>[GM]</color> TestS10"] = true,
    ["<color=#0486dc>[GM]</color> HoaTinHTe"] = true,
    ["<color=#0486dc>[GM]</color> HoaHoaS9"] = true,
    ["CôngChúa"] = true,
    ["ĐộcThiênCơ"] = true,
    ["ADNX001"] = true,
    ["ADNXS1001"] = true,
    ["ADNXS1002"] = true,
    ["ADNXS2001"] = true,
    ["ADNXS2002"] = true,
    ["ADNXS3001"] = true,
    ["ADNXS3002"] = true,
    ["ADNXS4001"] = true,
    ["ADNXS4002"] = true,
    ["ADNXS5001"] = true,
    ["ADNXS6001"] = true,
    ["ADNXS7001"] = true,
    ["ADNXS8001"] = true,
    ["ADNXS9001"] = true
}

Global_YouLong_All = true

function Global_Enalbe_KuaFu()
    local serverID = System:GetServerID()
    if serverID > 39 and serverID ~= 88 then
       return false
    end

    local current_hour = System:GetHour()
    return current_hour >= 16 and current_hour <= 23
end

function Global_Enalbe_KuaFu_Balance()
    local dayOfWeek = System:GetDayOfWeek()
    if dayOfWeek ~= 4 and dayOfWeek ~= 0 then
        return false
    end

    local current_hour = System:GetHour()
    return current_hour >= 16 and current_hour <= 23
end

-- function Global_IsIn83Event()
--     local START_DATE = '2023-03-07 05:00:00'
--     local END_DATE = '2023-03-29 00:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

function Global_IsIn304Event()
    local START_DATE = '2023-04-25 05:00:00'
    local END_DATE = '2023-05-23 00:00:00'
    return System.IsDateTimeInRange(START_DATE, END_DATE)
end

-- function Global_IsIn106Event()
--     local START_DATE = '2023-06-12 05:00:00'
--     local END_DATE = '2023-07-03 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn2307Event()
--     local START_DATE = '2023-07-21 05:00:00'
--     local END_DATE = '2023-08-05 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn2308Event()
--     local START_DATE = '2023-08-14 05:00:00'
--     local END_DATE = '2023-09-11 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn2109Event()
--     local START_DATE = '2023-09-21 05:00:00'
--     local END_DATE = '2023-10-11 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn202310Event()
--     local START_DATE = '2023-10-01 05:00:00'
--     local END_DATE = '2023-11-20 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn202311Event()
--     local START_DATE = '2023-11-28 05:00:00'
--     local END_DATE = '2023-12-12 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn202312Event()
--     local START_DATE = '2023-12-01 05:00:00'
--     local END_DATE = '2024-01-16 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

-- function Global_IsIn202401Event()
--     local START_DATE = '2024-01-23 05:00:00'
--     local END_DATE = '2024-02-14 05:00:00'
--     return System.IsDateTimeInRange(START_DATE, END_DATE)
-- end

Global_ItemExpiry83 = "2023-04-30 00:00:00"
Global_ItemExpiry304 = '2023-06-22 00:00:00'
Global_ItemExpiry0106 = '2023-07-03 05:00:00'
Global_ItemExpiry2307 = '2023-08-12 00:00:00'
Global_ItemExpiry2308 = '2023-09-18 00:00:00'
Global_ItemExpiry2109 = '2023-10-18 00:00:00'
Global_ItemExpiry202310 = '2023-11-27 00:00:00'
Global_ItemExpiry202311 = '2023-12-18 00:00:00'
Global_ItemExpiry202312 = '2024-01-21 00:00:00'

Global_DiamondVIP = {
    [1] = {200493, 1999833136, 1999819650, 205700, 200419, 206697, 199869368, 303981, 199851182, 255802, 206233,
           1999873754, 1999867813, 219002},
    [2] = {206233, 1999833136, 199851182, 255802, 362736, 219002, 200419, 303981, 277034, 1999858138, 1999830840,
           1999883122},
    [3] = {1999883122, 246248, 303981, 1999833136, 214823, 221615, 362736, 245419, 219002, 240727, 352062, 200430,
           205092, 206697, 352625, 1800203651, 1999851328, 1800220423, 283894, 1999831863, 1999873754, 202483,
           1999830989, 1999842767}
}

-- 206233
-- 200493 HạoHiên

LUA_ITEMPACKS = {
    [1] = {
        -- ưu đãi VIP
        Roles = {1999883122, 246248, 303981, 1999833136, 214823, 221615, 362736, 245419, 219002, 240727, 352062, 200430,
                 205092, 206697, 352625, 1800203651, 1999851328, 1800220423, 283894, 1999831863, 1999873754, 202483,
                 1999830989, 1999842767},
        Name = "Gói ưu đãi VIP",
        Record = 9990001,
        RecordType = "Month",
        BagSpace = 8,
        Items = {{
            ItemID = 20345, -- 20 mảnh lôi đình ấn
            Number = 20,
            Binding = 1
        }, {
            ItemID = 20344,
            Number = 10, -- 10 cờ chiến công 
            Binding = 1
        }, {
            ItemID = 310, -- 1 rương ma đao thạch cấp 10
            Number = 1,
            Binding = 1
        }, {
            ItemID = 320, -- 1 rương hộ giáp phiến cấp 10
            Number = 1,
            Binding = 1
        }, {
            ItemID = 330, -- 1 rương ngũ hành thạch cấp 10
            Number = 1,
            Binding = 1
        }, {
            ItemID = 20181, -- 1 thú cưỡi mạch thượng phi sương
            Number = 1,
            Binding = 1,
            Expiry = 43201
        }, {
            ItemID = 20529, -- 50 rương mảnh trang bị đồng hành (đặc biệt)
            Number = 50,
            Binding = 1
        }, {
            ItemID = 20364, -- 1 bùa may mắn đồng hành cao cấp
            Number = 1,
            Binding = 1
        }}
    },
    [2] = {
        -- top 1 tài phú
        Name = "Phần thưởng TOP 1 Tài phú",
        Roles = {200493},
        Record = 9990002,
        RecordType = "Week",
        BagSpace = 8,
        Items = {{
            ItemID = 15174,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 191,
            Number = 3,
            Binding = 1
        }, {
            ItemID = 490,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 492,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 746,
            Number = 100,
            Binding = 1
        }, {
            ItemID = 0,
            Number = 4000000 -- bạc khóa 400 vạn, 15000
        }, {
            ItemID = 3,
            Number = 50000 -- knb khóa 5 vạn, 15001
        }, {
            ItemID = 6,
            Number = 112
        }}
    },
    [3] = {
        -- top 2 tài phú
        Name = "Phần thưởng TOP 2 Tài phú",
        Roles = {1999864877},
        Record = 9990003,
        RecordType = "Week",
        BagSpace = 7,
        Items = {{
            ItemID = 3468,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 191,
            Number = 2,
            Binding = 1
        }, {
            ItemID = 490,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 492,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 746,
            Number = 100,
            Binding = 1
        }, {
            ItemID = 0,
            Number = 2000000 -- bạc khóa 200 vạn, 15000
        }, {
            ItemID = 3,
            Number = 20000 -- knb khóa 2 vạn, 15001
        }, {
            ItemID = 6,
            Number = 113
        }}
    },
    [4] = {
        -- top 3 tài phú
        Name = "Phần thưởng TOP 3 Tài phú",
        Roles = {1999865503},
        Record = 9990004,
        RecordType = "Week",
        BagSpace = 5,
        Items = {{
            ItemID = 191,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 490,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 492,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 746,
            Number = 100,
            Binding = 1
        }, {
            ItemID = 0,
            Number = 2000000 -- bạc khóa 200 vạn, 15000
        }, {
            ItemID = 3,
            Number = 20000 -- knb khóa 2 vạn, 15001
        }, {
            ItemID = 6,
            Number = 114
        }}
    },
    [5] = {
        -- top 1 uy danh
        Name = "Phần thưởng TOP 1 Uy danh",
        Roles = {1999864853},
        Record = 9990005,
        RecordType = "Week",
        BagSpace = 5,
        Items = {{
            ItemID = 191,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 490,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 492,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 746,
            Number = 50,
            Binding = 1
        }, {
            ItemID = 0,
            Number = 4000000 -- bạc khóa 400 vạn, 15000
        }, {
            ItemID = 3,
            Number = 50000 -- knb khóa 5 vạn, 15001
        }}
    },
    [6] = {
        -- top 2 uy danh
        Name = "Phần thưởng TOP 2 Uy danh",
        Roles = {1999864877},
        Record = 9990006,
        RecordType = "Week",
        BagSpace = 6,
        Items = {{
            ItemID = 190,
            Number = 2,
            Binding = 1
        }, {
            ItemID = 490,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 492,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 746,
            Number = 50,
            Binding = 1
        }, {
            ItemID = 0,
            Number = 2000000 -- bạc khóa 200 vạn, 15000
        }, {
            ItemID = 3,
            Number = 20000 -- knb khóa 2 vạn, 15001
        }}
    },
    [7] = {
        -- top 3 uy danh
        Name = "Phần thưởng TOP 3 Uy danh",
        Roles = {1999864958},
        Record = 9990007,
        RecordType = "Week",
        BagSpace = 8,
        Items = {{
            ItemID = 190,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 490,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 492,
            Number = 1,
            Binding = 1
        }, {
            ItemID = 746,
            Number = 50,
            Binding = 1
        }, {
            ItemID = 0,
            Number = 2000000 -- bạc khóa 200 vạn, 15000
        }, {
            ItemID = 3,
            Number = 20000 -- knb khóa 2 vạn, 15001
        }}
    },
	[10] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7763,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14172,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14040,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14162,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7579,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4174,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14010,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7706,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14005,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11437,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			}
			,
			{
				ItemID = 11435,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			}
			,
			{
				ItemID = 11434,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			}
			,
			{
				ItemID = 11436,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[11] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7764,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14031,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14041,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14163,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7580,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4174,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14011,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7706,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14005,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11437,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			}
			,
			{
				ItemID = 11435,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			}
			,
			{
				ItemID = 11434,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			}
			,
			{
				ItemID = 11436,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[20] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 15,
		Items= {
			{
				ItemID = 7783,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14173,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14042,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14164,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7585,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4175,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14012,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7709,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14006,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11438,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11439,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11452,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11453,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 12787,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 12786,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[21] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 15,
		Items= {
			{
				ItemID = 7784,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14174,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14043,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14165,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7586,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4175,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14013,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7709,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14006,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},{
				ItemID = 11438,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11439,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11452,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11453,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 12787,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 12786,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[30] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7803,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14175,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14044,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14166,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7591,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4176,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14014,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7715,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14007,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11440,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11441,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11442,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11443,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[31] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 9,
		Items= {
			{
				ItemID = 7804,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14176,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14045,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14167,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7592,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4176,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14015,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7715,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14007,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11440,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11441,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11442,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11443,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[40] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7823,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14177,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14046,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14168,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7597,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4177,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14016,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14008,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7721,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11444,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11445,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11446,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11447,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[41] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7824,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14178,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14047,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14169,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7598,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4177,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14017,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7721,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14008,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11444,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11445,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11446,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11447,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[50] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7843,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14038,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14048,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14170,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7603,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4178,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14018,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7727,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14009,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11448,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11449,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11450,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11451,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	},
	[51] = {
		Roles= {1999899432, 1999892240, 362736, 245419, 1999911849, 1999909893, 1999901529, 1999888562, 352062, 246248, 1999802640, 1999854529, 219002, 214823, 199832941, 228093, 1999883146, 1999882982,
		1999819650, 1999873754, 1999854557, 1999858138, 351565, 283894, 1999854766, 1999854571, 210705, 1999838726, 1999914797, 1999914313, 205422, 255802, 201373, 200365, 226093, 1999831917, 1999833136,
		1999854728, 353861, 226423},
		Name = "Gói hỗ trợ giải đấu",  
        BagSpace = 13,
		Items= {
			{
				ItemID = 7844,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14039,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14049,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14171,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7604,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 4178,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14019,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 7727,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14009,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 14009,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11448,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11449,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11450,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
			{
				ItemID = 11451,
				Number = 1,
				Binding = 1,
				Expiry = 90,
			},
		}
	}
}

function ListContainsValue(list, value)
    if list ~= nil then
        local found = false
        for i, v in ipairs(list) do
            if v == value then
                return true
            end
        end
    end
    return false
end

function Global_ReceiveItemPack(npc, player, packageID)
    GUI:CloseDialog(player)

    local package = LUA_ITEMPACKS[packageID]
    local record = package.Record

    System:WriteToConsole("Init Package")

    if package.Roles ~= nil then
        if not (ListContainsValue(package.Roles, player:GetID()) or player:IsGM()) then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu không nằm trong danh sách nhận gói vật phẩm.")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        end
    end

    if not Player.HasFreeBagSpaces(player, package.BagSpace) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần " .. package.BagSpace ..
                           " ô trống trong túi đồ để nhận vật phẩm.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return false
    end

    if package.RecordType == "Day" then
        if Player:GetValueOfDailyRecore(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfDailyRecore(player, record, 1)
        end
    end

    if package.RecordType == "Week" then
        if Player:GetValueWeeklyRecore(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfWeekRecore(player, record, 1)
        end
    end

    if package.RecordType == "Month" then
        if Player:GetValueOfMonthRecord(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfMonthRecord(player, record, 1)
        end
    end

    if package.RecordType == "Lifetime" then
        if Player:GetValueForeverRecore(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfForeverRecore(player, record, 1)
        end
    end

    local received = true

    for k, item in pairs(package.Items) do
        local series = 0
        local expiry = 0

        if item.ItemID <= 4 then
            -- add money
            received = received and Player:AddMoney(player, item.Number, item.ItemID, "LUA_PACKAGE_" .. packageID)
            goto continue
        end

        if item.ItemID == 5 then
            -- add exp
            player:AddExp(item.Number)
            goto continue
        end

        if item.ItemID == 6 then
            -- add title
            if not Player:SetRoleTitle(player, item.Number) then
                player:AddNotification("Không đủ ô trống trong túi đồ để nhận vật phẩm.");
                return false
            end
            goto continue
        end

        if item.ItemID == 7 then
            Player.AddCurGatherPoint(player, item.Number)
            goto continue
        end

        if item.ItemID == 8 then
            Player.AddMakePoint(player, item.Number)
            goto continue
        end

        if item.Series ~= nil then
            series = item.Series
        end

        if item.Expiry ~= nil then
            received = received and
                           Player.AddItemLuaAndNotify(player, item.ItemID, item.Number, series, item.Binding, 0,
                    item.Expiry)
        else
            received = received and Player.AddItemLuaAndNotify(player, item.ItemID, item.Number, series, item.Binding)
        end

        ::continue::
    end

    if not received then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText(
            "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
    else
        System:WriteDBLog(player, 'LUA_PACKAGE_ADD', '', '' .. player:GetID(), '', '', '', '' .. packageID)
    end

    return received
end

function Global_ReceiveItemPackByConfig(npc, player, package)
    GUI:CloseDialog(player)

    local record = package.Record

    System:WriteToConsole("Init Package " .. package.Record)

    if package.Roles ~= nil then
        if not (ListContainsValue(package.Roles, player:GetID()) or player:IsGM()) then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu không nằm trong danh sách nhận gói vật phẩm.")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        end
    end

    if package.Money ~= nil and package.Money > 0 then
        if Player.CheckMoney(player, package.MoneyType) < package.Money then
            GUI:ShowMessageBox(player, 'Thông báo', 'Bằng hữu không có đủ ' .. Global_TypeMoneyName[package.MoneyType] .. '.')
            return
        end
    end

    if not Player.HasFreeBagSpaces(player, package.BagSpace) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần " .. package.BagSpace ..
                           " ô trống trong túi đồ để nhận vật phẩm.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return false
    end

    if package.RecordType == "Day" then
        local dayLimit = 1
        if package.RecordLimit ~= nil then
            dayLimit = package.RecordLimit
        end
        local dayReceive = Player:GetValueOfDailyRecore(player, record)
        if dayReceive == -1 then
            dayReceive = 0
        end
        if dayReceive >= dayLimit then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfDailyRecore(player, record, dayReceive + 1)
        end
    end

    if package.RecordType == "Week" then
        local weekLimit = 1
        if package.RecordLimit ~= nil then
            weekLimit = package.RecordLimit
        end
        local weekReceive = Player:GetValueWeeklyRecore(player, record)
        if weekReceive == -1 then
            weekReceive = 0
        end
        if weekReceive >= weekLimit then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfWeekRecore(player, record, weekReceive + 1)
        end
    end

    if package.RecordType == "Month" then
        local monthLimit = 1
        if package.RecordLimit ~= nil then
            monthLimit = package.RecordLimit
        end
        local monthReceive = Player:GetValueOfMonthRecord(player, record)
        if monthReceive == -1 then
            monthReceive = 0
        end
        if monthReceive >= monthLimit then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfMonthRecord(player, record, monthReceive + 1)
        end
    end

    if package.RecordType == "Lifetime" then
        local lifetimeLimit = 1
        if package.RecordLimit ~= nil then
            lifetimeLimit = package.RecordLimit
        end
        if lifetimeLimit == 0 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu không đủ điều kiện nhận gói vật phẩm này!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        end
        local lifetimeReceive = Player:GetValueForeverRecore(player, record)
        if lifetimeReceive == -1 then
            lifetimeReceive = 0
        end
        if lifetimeReceive >= lifetimeLimit then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfForeverRecore(player, record, lifetimeReceive + 1)
        end
    end

    local received = true

    if package.Money ~= nil and package.Money > 0 then
        if not Player:SubMoney(player, package.Money, package.MoneyType, "LUA_ITEMPACKS") then
            GUI:ShowMessageBox(player, "Thông báo",
                    "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.")
        end
    end

    for k, item in pairs(package.Items) do
        local series = 0
        local expiry = 0

        if item.ItemID <= 4 then
            -- add money
            received = received and Player:AddMoney(player, item.Number, item.ItemID, "LUA_PACKAGE_" .. package.Record)
            goto continue
        end

        if item.ItemID == 5 then
            -- add exp
            player:AddExp(item.Number)
            goto continue
        end

        if item.ItemID == 6 then
            -- add title
            if not Player:SetRoleTitle(player, item.Number) then
                player:AddNotification("Không đủ ô trống trong túi đồ để nhận vật phẩm.");
                return false
            end
            goto continue
        end

        if item.ItemID == 7 then
            Player.AddCurGatherPoint(player, item.Number)
            goto continue
        end

        if item.ItemID == 8 then
            Player.AddMakePoint(player, item.Number)
            goto continue
        end

        if item.Series ~= nil then
            series = item.Series
        end

        if item.Expiry ~= nil then
            received = received and
                           Player.AddItemLuaAndNotify(player, item.ItemID, item.Number, series, item.Binding, 0,
                    item.Expiry)
        else
            received = received and Player.AddItemLuaAndNotify(player, item.ItemID, item.Number, series, item.Binding)
        end

        ::continue::
    end

    if not received then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText(
            "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
    else
        System:WriteDBLog(player, 'LUA_PACKAGE_ADD', '', '' .. player:GetID(), '', '', '', '' .. package.Record)
    end

    return received
end

function Global_BattleItemPack(npc, player, packageID)
    GUI:CloseDialog(player)
    local package = LUA_ITEMPACKS[packageID]
    local record = package.Record

    System:WriteToConsole("Init Package")

    if package.Roles ~= nil then
        if not (ListContainsValue(package.Roles, player:GetID()) or player:IsGM()) then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu không nằm trong danh sách nhận gói vật phẩm.")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        end
    end

    if not Player.HasFreeBagSpaces(player, package.BagSpace) then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText("Bằng hữu cần " .. package.BagSpace ..
                           " ô trống trong túi đồ để nhận vật phẩm.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
        return false
    end

    if package.RecordType == "Day" then
        if Player:GetValueOfDailyRecore(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfDailyRecore(player, record, 1)
        end
    end

    if package.RecordType == "Week" then
        if Player:GetValueWeeklyRecore(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfWeekRecore(player, record, 1)
        end
    end

    if package.RecordType == "Month" then
        if Player:GetValueOfMonthRecord(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfMonthRecord(player, record, 1)
        end
    end

    if package.RecordType == "Lifetime" then
        if Player:GetValueForeverRecore(player, record) ~= -1 then
            local dialog = GUI:CreateNPCDialog(npc, player)
            dialog:AddText("Bằng hữu đã nhận đủ số lượng gói vật phẩm này rồi!")
            -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
            dialog:Show(npc, player)
            return false
        else
            Player:SetValueOfForeverRecore(player, record, 1)
        end
    end

    local received = true

    for k, item in pairs(package.Items) do
        local series = 0
        local expiry = 0

        if item.ItemID <= 4 then
            -- add money
            received = received and Player:AddMoney(player, item.Number, item.ItemID, "LUA_PACKAGE_" .. packageID)
            goto continue
        end

        if item.ItemID == 5 then
            -- add exp
            player:AddExp(item.Number)
            goto continue
        end

        if item.ItemID == 6 then
            -- add title
            if not Player:SetRoleTitle(player, item.Number) then
                player:AddNotification("Không đủ ô trống trong túi đồ để nhận vật phẩm.");
                return false
            end
            goto continue
        end

        if item.ItemID == 7 then
            Player.AddCurGatherPoint(player, item.Number)
            goto continue
        end

        if item.ItemID == 8 then
            Player.AddMakePoint(player, item.Number)
            goto continue
        end

        if item.Series ~= nil then
            series = item.Series
        end

        if item.Expiry ~= nil then
            received = received and
                           Player.AddItemLuaAndNotify(player, item.ItemID, item.Number, series, item.Binding, 16,
                    item.Expiry)
        else
            received = received and Player.AddItemLuaAndNotify(player, item.ItemID, item.Number, series, item.Binding)
        end

        ::continue::
    end

    if not received then
        local dialog = GUI:CreateNPCDialog(npc, player)
        dialog:AddText(
            "Đã có lỗi xảy ra. Vui lòng liên hệ với quản trị viên để được hỗ trợ.")
        -- dialog:AddSelection(2, "Ta sẽ trở lại sau.")
        dialog:Show(npc, player)
    else
        System:WriteDBLog(player, 'LUA_PACKAGE_ADD', '', '' .. player:GetID(), '', '', '', '' .. packageID)
    end

    return received
end
