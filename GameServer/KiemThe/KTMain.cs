using GameServer.KiemThe.CopySceneEvents;
using GameServer.KiemThe.Core.Activity.CardMonth;
using GameServer.KiemThe.Core.Activity.DaySeriesLoginEvent;
using GameServer.KiemThe.Core.Activity.DownloadBouns;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Activity.EveryDayOnlineEvent;
using GameServer.KiemThe.Core.Activity.LevelUpEvent;
using GameServer.KiemThe.Core.Activity.LuckyCircle;
using GameServer.KiemThe.Core.Activity.PlayerPray;
using GameServer.KiemThe.Core.Activity.RechageEvent;
using GameServer.KiemThe.Core.Activity.SeashellCircle;
using GameServer.KiemThe.Core.Activity.TotalLoginEvent;
using GameServer.KiemThe.Core.BulletinManager;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.MonsterAIScript;
using GameServer.KiemThe.Core.Rechage;
using GameServer.KiemThe.Core.Repute;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Core.Title;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents;
using GameServer.KiemThe.GameEvents.GuildWarManager;
using GameServer.KiemThe.GameEvents.LotteryEvent;
using GameServer.KiemThe.GameEvents.TangBaoDo;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.KiemThe.Logic.Manager.Skill.PoisonTimer;
using GameServer.KiemThe.LuaSystem;
using GameServer.Logic;
using GameServer.Logic.RefreshIconState;
using System;
using System.Threading;
using GameServer.KiemThe.Core.Activity.ChargePromotion;
using GameServer.KiemThe.Entities.Player;
using GameServer.KiemThe.GameEvents.BaoThach;
using GameServer.KiemThe.GameEvents.Medal;
using GameServer.KiemThe.GameEvents.CheckIn;
using GameServer.KiemThe.GameEvents.DailyCheckin;
using GameServer.KiemThe.GameEvents.DaoPhong;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.GameEvents.GiftExchange;
using GameServer.KiemThe.GameEvents.SlotMachine;
using GameServer.KiemThe.GameEvents.RechargeEvent;
using GameServer.KiemThe.GameEvents.QuestEvent;
using GameServer.KiemThe.HotFix;
using GameServer.KiemThe.Utilities;
using GameServer.KiemThe.GameEvents.DoThach;
using GameServer.KiemThe.Core.BattlePass;
using GameServer.KiemThe.GameEvents.VongQuayMayMan;
using GameServer.KiemThe.GameEvents.Core;
using GameServer.KiemThe.GameEvents.TowerDefense;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.GameEvents.ThienPhuc;

namespace GameServer.KiemThe
{
    /// <summary>
    /// Gọi đến ở quá trình đọc dữ liệu
    /// </summary>
    public static class KTMain
    {
        /// <summary>
        /// Khởi tạo dữ liệu trước khi tải danh sách bản đồ
        /// </summary>
        public static void InitFirst()
        {
            // Thiết lập hệ thống
            Console.WriteLine("Loading ServerConfig...");
            ServerConfig.Init();
            KTGlobal.LoadSpecialUsers();

            // Đọc dữ liệu thuộc tính cộng của môn phái và nhánh
            KFaction.Init();

            // Đọc dữ liệu thuộc tính cơ bản của đối tượng
            KNpcSetting.Init();

            // Đọc dữ liệu thuộc tính cơ bản khi thăng cấp của nhân vật
            KPlayerSetting.Init();

            // Tải danh sách Tân Thủ Thôn cho phép người chơi mới tạo được vào
            KTGlobal.LoadNewbieVillages();

            // Load ds RanDomNames
            //KTGlobal.LoadRandomNames();
            //Console.WriteLine("RandomFileName: {0}", KTGlobal.RandomNames);

            // Tải danh sách giftcode
            KTGlobal.LoadGiftCode();
            //Console.WriteLine("GiftCodeFileName: {0}", KTGlobal.giftCodes);

            // Tải danh sách drop của Hải tặc
            KTGlobal.LoadDropHaiTac();

            KTGlobal.LoadPKPunish();

            // Đọc dữ liệu lọc nội dung Chat
            KTChatFilter.Init();

            // Đọc dữ liệu danh hiệu
            KTTitleManager.Init();

            // Ngũ hành tương khắc
            KTGlobal.LoadAccrueSeries();
            KSpecialStateManager.Init();

            // Đọc dữ liệu PropertyDictionary
            KTGlobal.ReadPropertyDictionary();

            // Đọc dữ liệu kỹ năng
            KSkill.LoadSkillData();

            // Đọc dữ liệu Avatar nhân vật
            KRoleAvarta.Init();

            GuildTagManager.Setup();

            #region Timers
            // Khởi tạo luồng quản lý Captcha
            Console.WriteLine("Starting KTCaptchaManager...");
            KTCaptchaManager.Init();

            // Khởi tạo luồng quản lý thời gian của Người
            Console.WriteLine("Starting KTPlayerTimerManager...");
            KTPlayerTimerManager.Init();

            // Khởi tạo luồng quản lý thời gian của Quái
            Console.WriteLine("Starting KTMonsterTimerManager...");
            KTMonsterTimerManager.Init();

            // Khởi tạo luồng quản lý thời gian của BOT
            Console.WriteLine("Starting KTBotTimerManager...");
            KTBotTimerManager.Init();

            // Khởi tạo luồng quản lý kỹ năng
            Console.WriteLine("Starting KTSkillManager...");
            KTSkillManager.Init();

            // Khởi tạo luồng quản lý đạn bay
            Console.WriteLine("Starting KTBulletManager...");
            KTBulletManager.Init();

            // Khởi tạo luông quản lý Task
            Console.WriteLine("Starting KTTaskManager...");
            KTTaskManager.Init();

            // Khởi tạo luông quản lý ScheduleTask
            Console.WriteLine("Starting KTScheduleTaskManager...");
            KTScheduleTaskManager.Init();

            // Khởi tạo luông quản lý Trúng độc
            Console.WriteLine("Starting KTPoisonTimerManager...");
            KTPoisonTimerManager.Init();

            // Khởi tạo luồng quản lý Buff
            Console.WriteLine("Starting KTBuffManager...");
            KTBuffManager.Init();

            // Khởi tạo luồng quản lý di chuyển Quái
            Console.WriteLine("Starting KTMonsterStoryBoard...");
            KTMonsterStoryBoardEx.Init();

            // Khởi tạo luồng quản lý di chuyển Người
            Console.WriteLine("Starting KTPlayerStoryBoard...");
            KTPlayerStoryBoardEx.Init();

            // Khởi tạo luồng quản lý di chuyển các đối tượng khác
            Console.WriteLine("Starting KTOtherObjectStoryBoard...");
            KTOtherObjectStoryBoard.Init();

            // Khởi tạo quản lý phụ bản
            Console.WriteLine("Starting KTCopySceneTimerManager...");
            CopySceneEventManager.Init();

            // Khởi tạo luồng quản lý khu vực động
            Console.WriteLine("Starting KTDynamicAreaTimerManager...");
            KTDynamicAreaTimerManager.Init();
            #endregion

            // Khởi tạo Monster AI Script
            KTMonsterAIScriptManager.Init();

            Console.WriteLine("Starting Lua-Env...");
            // Khởi tạo môi trường Lua
            KTLuaEnvironment.Init();

            // Tải danh sách GM
            Console.WriteLine("Loading GMList...");
            KTGMCommandManager.LoadGMList();

            // Tải danh sách GM
            Console.WriteLine("Loading BanList...");
            KTGMCommandManager.LoadBanList();

            // Tải danh sách vật phẩm
            Console.WriteLine("Loading Items...");
            ItemManager.ItemSetup();

            // Tải danh sách kỹ năng sống
            Console.WriteLine("Loading LifeSkill.....");
            ItemCraftingManager.Setup();
            
            Console.WriteLine("Loading Compose...");
            KT_TCPHandler.SetupCompose();

            Console.WriteLine("Loading ItemRefine.....");
            ItemRefine.Setup();

            TaskManager.getInstance().Setup();

            SideTaskManager.getInstance().Setup();

            Console.WriteLine("Loading RandomBox.....");
            ItemRandomBox.Setup();


            Console.WriteLine("Loading RandomBox Special.....");
            ItemBoxSpecial.Setup();

            Console.WriteLine("Loading EventTime...");
            EventTimeManager.Init();

            // Auto-reset global event data for new periods
            Console.WriteLine("Processing event global data reset...");
            EventAutoResetManager.ProcessServerStartup();

            // Tải danh sách vật phẩm Drop
            Console.WriteLine("Loading ItemDrop...");
            ItemDropManager.Setup();

            // Tải dữ liệu Tu Luyện Châu
            Console.WriteLine("Loading XiuLianZhu...");
            ItemXiuLianZhuManager.Init();

            // Tải danh sách kinh nghiệm khi quái chết
            Console.WriteLine("Loading MonsterDeadExp...");
            MonsterDeadHelper.ExpInit();

            EliteMonsterManager.getInstance().SetupAllBoss();

            // Tải danh sách cửa hàng
            Console.WriteLine("Loading Shops...");
            ShopManager.Init();
            // Quản lý nạp thẻ
            RechageServiceManager.StartService();

            // Tải danh sách sự kiện
            Console.WriteLine("Loading Activities...");
            KTActivityManager.Init();
            
            CauCaMuaThuManager.Setup();

            DanphongManager.Init();
            BaoThachManager.Init();
            MedalManager.Init();
            DoThachManager.Init();
            DailyCheckinManager.Init();
            RefundEventManager.Init();
            ItemManager.LoadFixTrangBiBachNgan();
            HotFixVuKhiTanLang.Init();

            MaxLevelGreenProp.Init();

            // KTGlobal.LoadTienDongNgocNu();
            BulletinManager.Setup();

            #region Activity
            UnionShop.Init();
            TopEventRanking.Init();
            // Khởi tạo dữ liệu vòng quay sò
            KTSeashellCircleManager.Init();
            // Khởi tạo dữ liệu vòng quay may mắn
            KTLuckyCircleManager.Init();
            // Khởi tạo dữ liệu chúc phúc
            KTPlayerPrayManager.Init();
            TangBaoDo.Init();
            DayActivityManager.Setup();
            //LotteryManager.Setup();
            EveryDayOnlineManager.Setup();
            SevenDayLoginManager.Setup();
            TotalLoginManager.Setup();
            CardMonthManager.Setup();
            LevelUpEventManager.Setup();
            RechageManager.Setup();
            IconStateManager.Setup();
            ReputeManager.Setup();
            PetManager.SetUp();
            DownloadBounsManager.Setup();
            QuestEventManager.Init();
            OfferingManager.Init();
            ComeBackManager.Init();
            ServerMergeRewardManager.Init();
            GiftExchangeManager.Init();
            GiftExchangePlayerManager.Init();
            DailyRechargeEventManager.Init();

            //chan nguyen
            ChanNguyenManager.SetUpExpCN();
            ChanNguyenManager.SetUpPropUp();
            ChanNguyenManager.SetUpPropInfo();
            ChanNguyenManager.SetUpCNCreate();
            ChanNguyenManager.SetUpCNAH();
            ChanNguyenManager.SetUpCNTL();

            // Thien Phuc
            ThienPhucManager.Instance.Init();

            // thuong moc nhiem vu BVD, TH, HT
            TaskAwardManager.SetUp();
            BetPackData.SetUp();

            BattlePassManager.Init();
            ItemEnhance.Init();
            FishingManager.Init();

            GlobalNew.InitSpecialItems();
            GlobalNew.ParseTitleToItemMap();

            // thông tin vật phẩm sử dụng 
            KPlayer.LoadConfigBonusPlayerAttr();
            // load config item datetiem
            ItemDateTime.LoadConfig();
            #endregion

            #region Sự kiện Game
            // Khởi tạo các sự kiện trong Game
            GameMapEventsManager.Init();

            Console.WriteLine("Starting TowerDefenseManager...");
            TowerDefenseManager.Init();
            if (GameManager.ServerLineID != 99)
            {
                GuidWarManager.getInstance().Setup();   
            }
            
            GameEventRegistry.Register(FlowerTowerManager.Instance);
            GameEventRegistry.Register(SlotMachineManager.Instance);
            GameEventRegistry.Register(ExchangeTicketManager.Instance);
            GameEventRegistry.Register(ChargePromotionManager.Instance);
            GameEventRegistry.Register(RechargeEventManager.Instance);
            GameEventRegistry.Register(ComposeEventManager.Instance);
            GameEventRegistry.Register(CheckInManager.Instance);
            GameEventRegistry.InitAll();

            KT_TCPHandler.InitTamLong();

            LuckySpinManager.Init();
            #endregion
        }

        /// <summary>
    /// Khởi tạo dữ liệu sau khi tải danh sách bản đồ
        /// </summary>
        public static void InitAfterLoadingMap()
        {
            // Tải danh sách tự tìm đường
            Console.WriteLine("Initializing AutoPath...");
            KTAutoPathManager.Init();

            // Thông báo thông tin số luồng hệ thống
            KTMain.ReportThreadsCount();
        }

        /// <summary>
        /// Thông báo thông tin số luồng của hệ thống
        /// </summary>
        private static void ReportThreadsCount()
        {
            Console.WriteLine("============SYSTEM THREADING REPORTS============");
            ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
            Console.WriteLine("Thread pool limitation: Workers = {0}, CompletionPorts = {1}", workerThreads, completionPortThreads);
            Console.WriteLine("================================================");
        }
    }
}
