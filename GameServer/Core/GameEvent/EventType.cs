namespace GameServer.Core.GameEvent
{
    /// <summary>
    /// 事件类型定义，只允许定义大事件，同一类事件需要细分类型，请在各自逻辑内定义自己的常量来区分（示例：参见战盟事件 logic/BangHui/ZhanMengShiJian/ZhanMengShiJianManager.cs)
    /// </summary>
    public enum EventTypes
    {
        ZhanMengShiJian = 0,    //战盟事件
        XueSeChengBao = 1,      //血色城堡时间
        EMoGuangChang = 2,      //恶魔广场
        HuangJinBuDui = 3,      //黄金部队
        YeWaiBoss = 4,          //野外boss
        PKKingHuoDong = 5,      //PK之王活动
        TanWeiGouMai = 6,       //摊位购买
        MingXiang = 7,          //冥想
        ZhaoHuanUserID = 8,     //用户不登录召唤
        PlayerLevelup = 9,      //玩家升级
        PlayerDead = 10,         //玩家死亡（人打怪）
        MonsterDead = 11,        //怪物死亡
        PlayerLogout = 12,       //玩家退出
        PlayerLeaveFuBen = 13,   //玩家离开副本

        //JingJiFuBenEndForTime, //竞技场副本结束（时间到）
        PlayerInitGame = 14,         //初始化游戏角色

        MonsterBirthOn = 15,         //怪物出生
        MonsterInjured = 16,        //怪物第一次被攻击后
        MonsterBloodChanged = 17,   //怪物血量变化
        MonsterAttacked = 18,        //怪物攻击后
        MonsterLivingTime = 19,      //怪物存活时间（每隔一分钟触发一次)
        PreGotoLastMap = 20,      //玩家主动返回上一个地图
        PreInstallJunQi = 21,      //安插军旗前
        PreBangHuiAddMember = 22, //帮会增加成员前的确认事件
        PreBangHuiRemoveMember = 23, //帮会移除成员前的确认事件
        PreBangHuiChangeZhiWu = 24, //帮会改变职务前的确认事件
        PostBangHuiChange = 25, //帮会变更时间
        ProcessClickOnNpc = 26, //处理点击NPC事件
        StartPlayGame = 27,
        OnClientChangeMap = 28, //客户端请求传送
        OnCreateMonster = 29, //创建怪物时间
        ClientRegionEvent = 30,   //角色移动时区域触发事件
        SevenDayGoal = 31, // 七日目标事件

        PreMonsterInjure = 32,//CampNoAttack怪，伤害计算

        MonsterToMonsterDead = 33,        //怪物死亡(怪打怪)

        PlayEnterMap = 34,   //Sự kiện khi người chơi đã vào bản đồ thành công

        Max = 10000, //保留10000以后的编号，请勿使用

        LogOn = 10001,// Sự kiện đăng nhập
        RemoveItem = 10002,// sự kiện thêm, xóa lượng vật phẩm
        CompletedTask = 10003, // Sự kiện hoàn thành nhiệm vụ
        ChucPhuc = 10004, // Sự kiện chúc phúc
        Move_ToArena = 10005, // Sự kiện Đi đến đấu trường
        Join_DoanHoaDang = 10006, // Sự kiện đoán hoa đăng
        Join_TieuDaoCoc = 10007, // Sự kiện tham gia tiêu dao cốc
        Join_BachHoDuong = 10008,// Sự kiện tham gia bạch hổ dường
        Join_DuLongChienThu = 10009,// Sự kiện tham gia du long chiến thư
        Join_TuLuyenBiCanh = 10010,// Sự kiện tu luyện bí cảnh
        Join_TranhDoatLanhTho = 10011,// Sự kiện tham gia tranh đoạt lãnh thổ
        Change_CurGatherPoint = 10012,// Sự kiện thay đổi giá trị tinh lực hiện tại
        Join_ThiDauMonPhai = 10013, // Sự kiện tham gia thi đấu môn phái
        Receive_ThiDauMonPhai = 10014, // Sự kiện nhận giải thưởng thi đấu môn phái
        Join_LeagueBattle = 10015, //Sự Kiện tham gia giải đấu
        Kill_BachHoDuong = 10016,// Sự kiện giết địch trong bạch hổ đường
        FamilyFubenJoint = 10017,// Sự kiện tham gia vượt ải gia tộc
        OpenRandomBox = 10018, // Sự kiện mở 1 hộp ngẫu nhiên
        EquipItem = 10019, // Sự kiện trang bị item cho player
        SpinSlotMachine = 10020, //Sự kiện quay slot machine
        Receive_TongKimRanking = 10021, //Sự kiện nhận rank tống kim
        Kill_TongKim = 10022, //Sự kiện giết người trong tống kim
        Enhance_ChanNguyen = 10023, //Sự kiện cường hóa chân nguyên
        Join_PetRace = 10024, // Sự kiện tham gia đua thú
        Enhance_Weapon = 10025, //Sự kiện cường hóa vũ khí
        Enhance_Armor = 10026, //Sự kiện cường hóa giáp
        CheckIn_TongKim = 10027, //Sự kiện check in tống kim
        Join_GiaToc = 10028, //Sự kiện tham gia gia tộc
        Crafting = 10029, //Sự kiện chế tác
        Capture_Pet = 10030, //Sự kiện bắt đồng hành
        Equip_Pet = 10031, //Sự kiện trang bị đồng hành
        UseItem = 10032, //Sự kiện sử dụng item
        Join_Fishing = 10033, //Sự kiện câu cá
        Join_EmperorTomb = 10034, // Sự kiện tham gia Tần Lăng
        RemoveItemByLua = 10035,// sự kiện thêm, xóa lượng vật phẩm from lua
        Compose_Event_Item = 10036, // Sự kiện ghép item
        Turn_SeashellCircle = 10037, // Sự kiện quay bbr
        Completed_BPWeekly = 10038, // Hoàn thành nhiệm vụ battle pass tuần
        CheckIn_TeamBattle = 10039, //Sự kiện check in liên đấu
        Recharge = 10040, // Sự kiện nạp thẻ
        Spin_Lucky = 10041, // Sự kiện quay Tân Niên Bí Bảo
    }
}