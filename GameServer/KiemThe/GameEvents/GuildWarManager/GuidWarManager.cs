using GameServer.Core.Executor;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.KiemThe.GameEvents.GuildWarManager
{
    /// <summary>
    /// Class sẽ quản lý toàn bộ việc chiến đấu của ward
    /// </summary>
    public class GuidWarManager
    {
        #region GuidWarManagerDef

        private static GuidWarManager instance = new GuidWarManager();

        /// <summary>
        /// Config của
        /// </summary>
        public GuildWardConfig _Config = new GuildWardConfig();

        /// <summary>
        /// Thời gian tick gần đây nhất
        /// </summary>
        public long LastTick { get; set; }

        public long LastProtectTick { get; set; }

        public long LastUpdateMiniMap { get; set; }

        public long LastUpdateScore { get; set; }

        private long _scoreReachedOrder;

        /// <summary>
        /// Thời gian notify gần đây nhất
        /// </summary>
        public long LastNofity { get; set; }

        public bool IsLoadingMiniMap { get; set; }

        public long LastSpamCamp { get; set; }

        public GUILDWARDSTATUS BATTLESTATE { get; set; }

        public List<GuildWarMiniMap> _CaheMiniMap = new List<GuildWarMiniMap>();
        /// <summary>
        /// Danh sách các bản đồ đã được tuyên chiến từ NPC
        /// </summary>

        public List<GuildMapRegisterFight> TotalMapRegisterFight = new List<GuildMapRegisterFight>();

        /// <summary>
        /// Danh sách bản đồ sẽ chạy sự kiện ward
        /// </>
        public ConcurrentDictionary<int, GuildWarMap> TotalMapWillBeWar = new ConcurrentDictionary<int, GuildWarMap>();

        // Cần 1 cái j đó để lưu điểm tích lũy của từng thằng ở đây
        /// <summary>
        ///  Chứa toàn bộ điểm của đợt công thành chiến của các người chơi được tham gia war
        /// </summary>
        public ConcurrentDictionary<int, ConcurrentDictionary<int, GuildWarPlayer>> WarScore = new ConcurrentDictionary<int, ConcurrentDictionary<int, GuildWarPlayer>>();

        // Danh sách điểm số của GUILD update liên tục
        public ConcurrentDictionary<int, List<TerritoryReport>> TotalReportUpdate = new ConcurrentDictionary<int, List<TerritoryReport>>();

        // Tạo 1 danh sách để lưu điểm của các bang lại
        public ConcurrentDictionary<int, List<GuildTotalScore>> TotalScore = new ConcurrentDictionary<int, List<GuildTotalScore>>();

        /// <summary>
        /// Dict chứa kết quả cuối cùng của quộc chiến
        /// </summary>
        public ConcurrentDictionary<int, List<FightResult>> FinalResult = new ConcurrentDictionary<int, List<FightResult>>();

        public BlockingCollection<string> TotalColorDict = new BlockingCollection<string>();

        //Bgwork do work
        public BackgroundWorker worker = new BackgroundWorker();

        public ConcurrentDictionary<int, string> ColorSelect = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// Toàn bộ thông tin Guild Info
        /// </summary>
        public ConcurrentDictionary<int, string> TotalGuildInfo = new ConcurrentDictionary<int, string>();

        // CÁI NÀY KEY CÓ LẼ LÊN LÀ BANG

        /// <summary>
        /// Đường dẫn tới files confgi
        /// </summary>
        public string _GuildMapConfig = "Config/KT_Battle/GuildWarConfig.xml";

        private const string LocalGuildWarConfig = "Config/KT_Battle/GuildWarConfig.xml";
        private const string LocalGuildWarEndNotification = "Tranh đoạt lãnh thổ đã tới hồi kết thúc! Hãy tới Quan Lãnh Thổ để nhận thưởng";
        private const string KuaFuGuildWarEndNotification = "Tranh đoạt lãnh thổ liên server đã kết thúc!";

        private readonly KuaFuGuildWarLogic _kuaFuGuildWarLogic = new KuaFuGuildWarLogic();

        private bool IsKuaFuGuildWar
        {
            get
            {
                return _kuaFuGuildWarLogic.IsEnabled;
            }
        }

        private string GetGuildWarEndNotification()
        {
            return IsKuaFuGuildWar ? KuaFuGuildWarEndNotification : LocalGuildWarEndNotification;
        }

        /// <summary>
        /// Hàm Instance()
        /// </summary>
        /// <returns></returns>
        public static GuidWarManager getInstance()
        {
            return instance;
        }

        #endregion GuidWarManagerDef

        /// <summary>
        /// Đếm số thành viên đã tuyên chiến bản đồ này
        /// </summary>
        /// <param name="MapID"></param>
        /// <returns></returns>
        public int CountRegisterFight(int MapID)
        {
            int Count = TotalMapRegisterFight.Where(x => x.MapId == MapID).Count();

            return Count;
        }

        public bool IsKuaFuBalanceMap(int mapCode)
        {
            if (!IsKuaFuGuildWar || _Config == null || !ServerConfig.Instance.IsKuaFuBalance || _Config.WarMapConfigs == null)
            {
                return false;
            }

            return _Config.WarMapConfigs.Any(x => x != null && x.ReadMapID == mapCode);
        }

        /// <summary>
        /// Setup
        /// </summary>
        public void Setup()
        {
            SysConOut.WriteLine("Loading Guilds War...");
            LastUpdateMiniMap = TimeUtil.NOW();
            IsLoadingMiniMap = false;

            //Thiết lập 1 BGWWROK MỚI
            worker = new BackgroundWorker();
            worker.DoWork += Event_Prosecc;
            worker.RunWorkerCompleted += Evemt_Complete;

            LoadConfig();

            // Khởi tạo timer tính toán khởi chạy cho sự kiện cứ 2 giấy tick 1 lần delay 5s
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("GuldWarManager", ProsecBattle), 5 * 1000, 2000);

            //Setup Dict Màu trước
            TotalColorDict.Add("#f44336");
            TotalColorDict.Add("#744700");
            TotalColorDict.Add("#ce7e00");
            TotalColorDict.Add("#8fce00");
            TotalColorDict.Add("#2986cc");
            TotalColorDict.Add("#16537e");
            TotalColorDict.Add("#6a329f");
            TotalColorDict.Add("#c90076");
            TotalColorDict.Add("#4c1130");
            TotalColorDict.Add("#f70067");
            TotalColorDict.Add("#0a0a0a");
            TotalColorDict.Add("#a405fa");

            TotalColorDict.Add("#19212e");
            TotalColorDict.Add("#911744");
            TotalColorDict.Add("#115c1e");

            this.SetupMiniMap();
        }

        public void ReloadConfig()
        {
            LoadConfig();
            SetupMiniMap();
        }

        private bool IsContinuousKuaFuBalance()
        {
            return IsKuaFuGuildWar && _Config != null && ServerConfig.Instance.IsKuaFuBalance && _Config.IsContinuous;
        }

        private bool IsOpenTime(DateTime now)
        {
            if (_Config == null || _Config.OpenTime == null)
            {
                return false;
            }

            if (IsContinuousKuaFuBalance())
            {
                return now.TimeOfDay >= new TimeSpan(_Config.OpenTime.Hours, _Config.OpenTime.Minute, 0);
            }

            return now.Hour == _Config.OpenTime.Hours && now.Minute == _Config.OpenTime.Minute;
        }

        private void LoadConfig()
        {
            _GuildMapConfig = _kuaFuGuildWarLogic.GetConfigPath(LocalGuildWarConfig);
            string Files = Global.GameResPath(_GuildMapConfig);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(GuildWardConfig));

                _Config = serializer.Deserialize(stream) as GuildWardConfig;
            }
        }

        private void Evemt_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG:" + e.Error.ToString());
            }
        }

        private void Event_Prosecc(object sender, DoWorkEventArgs e)
        {
            try
            {
                ///NẾu đang không có battle nào diễn ra
                if (BATTLESTATE == GUILDWARDSTATUS.STATUS_NULL || BATTLESTATE == GUILDWARDSTATUS.STATUS_CLEAR)
                {
                    if (TimeUtil.NOW() - LastUpdateMiniMap >= 300000 && IsLoadingMiniMap == false)
                    {
                        IsLoadingMiniMap = true;

                        //Call SETUP MINIMAP
                        this.SetupMiniMap();
                    }

                    List<int> DayOfWeek = _Config.DayOfWeek;

                    int Today = TimeUtil.GetWeekDay1To7(DateTime.Now);

                    //Check xem hôm này có phỉa là ngày diễn ra sự kiện
                    if (DayOfWeek.Contains(Today))
                    {
                        // Nếu là ngày diễn ra sự kiện
                        DateTime Now = DateTime.Now;

                        if (IsOpenTime(Now) && (BATTLESTATE == GUILDWARDSTATUS.STATUS_NULL || BATTLESTATE == GUILDWARDSTATUS.STATUS_CLEAR))
                        {
                            LastTick = TimeUtil.NOW();

                            if (IsKuaFuGuildWar)
                            {
                                BATTLESTATE = GUILDWARDSTATUS.STATUS_PREPARSTART;
                                CreateAllActiveWarMap();
                                SetupMiniMap();
                                KTGlobal.SendSystemEventNotification("Tranh đoạt lãnh thổ liên server đã mở, hãy đến Quan Lãnh Thổ để vào Hoa Sơn chuẩn bị chiến đấu. Trong 10 phút chuẩn bị sẽ không thể PK!");
                            }
                            else
                            {
                                // Chuyển sang trạng thái cho phép tuyên chiến
                                BATTLESTATE = GUILDWARDSTATUS.STATUS_REGISTER;
                                KTGlobal.SendSystemEventNotification("Lãnh thổ chiến đang bước vào thời kỳ tuyên chiến, hãy đến Quan Lãnh Thổ các thành để xác định mục tiêu chinh chiến!");
                            }

                            LogManager.WriteLog(LogTypes.GuildWarManager, "Battle Change State ==> " + BATTLESTATE.ToString());
                        }
                    }
                } // Nếu là thời gian đăng ký thì làm gì đó
                else if (BATTLESTATE == GUILDWARDSTATUS.STATUS_REGISTER)
                {
                    if (TimeUtil.NOW() >= LastTick + ((_Config.Activity.SIGN_UP_DULATION * 1000) - (5 * 60 * 1000)) && BATTLESTATE == GUILDWARDSTATUS.STATUS_REGISTER)
                    {
                        BATTLESTATE = GUILDWARDSTATUS.STATUS_PREPARSTART;

                        if (IsKuaFuGuildWar)
                        {
                            CreateAllActiveWarMap();
                            SetupMiniMap();
                        }

                        KTGlobal.SendSystemEventNotification(IsKuaFuGuildWar ? "Tranh đoạt lãnh thổ liên server bước vào thời gian chuẩn bị, trong 10 phút chuẩn bị sẽ không thể PK!" : "Chỉ còn 5 phút nữa tranh đoạt lãnh thổ sẽ bắt đầu Các Bang Hội hãy mau chóng tới NPC Quan Lãnh Thổ để tuyên chiến!");

                        LogManager.WriteLog(LogTypes.GuildWarManager, " Battle Change State ==> " + BATTLESTATE.ToString());
                    }
                } // Start sự kiện tranh đoạt lãnh thổ
                else if (BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART)
                {
                    if (TimeUtil.NOW() >= LastTick + (_Config.Activity.SIGN_UP_DULATION * 1000) && BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART)
                    {
                        //this.SetupMiniMap();

                        LastTick = TimeUtil.NOW();

                        // Khởi tọa toàn bộ các bản đồ có war
                        BATTLESTATE = GUILDWARDSTATUS.STATUS_START;

                        KTGlobal.SendSystemEventNotification("Lãnh thổ chiến đã bước vào thời kỳ Chinh Chiến,các Bang Hội hãy mau di chuyển tới địa điểm chiến đấu!");

                        // Khởi tạo toàn bộ bản đồ war
                        if (!IsKuaFuGuildWar)
                        {
                            this.CreateAllActiveWarMap();
                        }

                        this.CreateMapEvent();

                        this.FillMoreInfoToMiniMap();

                        LogManager.WriteLog(LogTypes.GuildWarManager, " Battle Change State ==> " + BATTLESTATE.ToString());
                        // CREATE TOÀN BỘ BẢN ĐỒ CHIẾN
                    }
                }
                else if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND) // Nếu chiến trường đang bắt dầu
                {
                    if (TimeUtil.NOW() - LastUpdateScore >= 60 * 1000 && (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND))
                    {
                        LastUpdateScore = TimeUtil.NOW();

                        // Cứ mỗi 60s sẽ cộng điểm cho bang đó tùy vào số trụ chiểm được
                        UpdateScoreForGuildEvery60SEC();
                    }

                    // Cứ 30s spam 1 lần camp
                    if (TimeUtil.NOW() - LastProtectTick >= 30 * 1000 && (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND))
                    {
                        LastProtectTick = TimeUtil.NOW();

                        this.SpamCamp();
                        // Cứ mỗi 30s sẽ cộng điểm bảo vệ cho người chơi và EXP
                        UpdateExpEvery30Sec();
                    }

                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND))
                    {
                        LastNofity = TimeUtil.NOW();

                        // Thực hiện udpate guild ranking đã sửa lại
                        UpdateGuildRanking();
                        // Gửi thông báo mỗi 5s đã sửa lại
                        NotifyToAllEvery5Secon();

                    }

                    if (TimeUtil.NOW() >= LastTick + ((_Config.Activity.WAR_DULATION * 1000) - (5 * 60 * 1000)) && BATTLESTATE == GUILDWARDSTATUS.STATUS_START)
                    {
                        KTGlobal.SendSystemEventNotification("Chỉ còn 5 phút nữa tranh đoạt sẽ kết thúc,các Bang hãy mau chóng đẩy sập long trụ!");

                        BATTLESTATE = GUILDWARDSTATUS.STATUS_PREPAREEND;

                        LogManager.WriteLog(LogTypes.GuildWarManager, " Battle Change State ==> " + BATTLESTATE.ToString());
                    }

                    if (TimeUtil.NOW() >= LastTick + (_Config.Activity.WAR_DULATION * 1000) && BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
                    {
                        LastTick = TimeUtil.NOW();

                        KTGlobal.SendSystemEventNotification(GetGuildWarEndNotification());

                        // Chuyển sang trạng thái end
                        BATTLESTATE = GUILDWARDSTATUS.STATUS_END;

                        //Gọi update bảng xếp hạng lần cuối cùng
                        UpdateGuildRanking();
                        // Thiết lập lãnh thổ đọc ghi DB


                        // Gửi thông báo mỗi 5s đã sửa lại
                        NotifyToAllEvery5Secon();

                        // Clear toàn bộ cột trụ quá , boss,
                        // Unlock status cho toàn bộ thành viên tham gia
                        ResetBattle();

                        if (IsKuaFuGuildWar)
                        {
                            _kuaFuGuildWarLogic.SendRewards(_Config, WarScore, TotalScore);
                            _kuaFuGuildWarLogic.MoveAllPlayersBackToCity(TotalMapWillBeWar, ChangeState);
                        }
                        else
                        {
                            SetupFinishTerritory();

                            // Thực hiện ghi vào DB
                            WriterToDatabase();

                            //Reload lại toàn bộ thông tin lãnh thổ
                            ReloadAllTerritoryInfo();
                        }

                        //todo : Làm thêm quả xếp hạng ở NPC khi nó ấn vào phần thưởng

                        LogManager.WriteLog(LogTypes.GuildWarManager, " Battle Change State ==> " + BATTLESTATE.ToString());

                        //_CaheMiniMap = new List<GuildWarMiniMap>();

                        this.SetupMiniMap();
                        if (!IsKuaFuGuildWar)
                        {
                            string CMDUPDATE = $"0:0:0:0";
                            Global.SendToDB((int)TCPGameServerCmds.CMD_KT_GUILD_UPGRADE_OFFICE_RANK, CMDUPDATE, GameManager.LocalServerId);
                        }

                        logPlayerScoreWhenEnd();
                    }
                }
                else if (BATTLESTATE == GUILDWARDSTATUS.STATUS_END)
                {
                    if (TimeUtil.NOW() >= LastTick + (_Config.Activity.WAR_CLEAR * 1000) && BATTLESTATE == GUILDWARDSTATUS.STATUS_END)
                    {
                        LastTick = TimeUtil.NOW();

                        BATTLESTATE = GUILDWARDSTATUS.STATUS_CLEAR;

                        LogManager.WriteLog(LogTypes.GuildWarManager, " Battle Change State ==> " + BATTLESTATE.ToString());

                        TotalMapRegisterFight.Clear();

                        WarScore.Clear();

                        FinalResult.Clear();

                        TotalMapWillBeWar.Clear();

                        TotalScore.Clear();

                        this.SetupMiniMap();

                        var curDay = DateTime.Now.DayOfWeek;
                        if (curDay == DayOfWeek.Sunday)
                        {
                            KTGlobal.SendSystemEventNotification("Hoạt động Lãnh thổ chiến đã kết thúc! Bang chủ hãy đến gặp Phó Thiếu Khanh ở Lâm An Phủ để nâng cấp quan hàm bang hội trước 24h hôm nay nhé.");
                        }
                        else
                        {
                            KTGlobal.SendSystemEventNotification("Hoạt động Lãnh thổ chiến đã kết thúc!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }

        public void SetupMiniMap()
        {
            _CaheMiniMap = new List<GuildWarMiniMap>();
            try
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "SETUP MINI MAP");

                if (IsKuaFuGuildWar)
                {
                    _kuaFuGuildWarLogic.SetupMiniMap(_Config, _CaheMiniMap);
                    return;
                }

                TerritoryInfo _GetAllTeroryInfo = this.GetTotalTerritoryInfo(0);

                if (_GetAllTeroryInfo != null)
                {
                    if (_GetAllTeroryInfo.Territorys != null)
                    {
                        // Lấy ra tất cả các lãnh thổ hiện tại của máy chủ
                        foreach (Territory _Territory in _GetAllTeroryInfo.Territorys)
                        {
                            //Đọc ra bản đồ
                            int MAPID = _Territory.MapID;
                            // Đang thuộc về bang nào
                            int Guild_ID = _Territory.GuildID;
                            // lấy ra bản đồ cơ sở từ DB cái này ko cần sửa
                            var Find = _Config.WarMapConfigs.Where(x => x.MapID == MAPID).FirstOrDefault();

                            GuildWarMiniMap _Demo1 = new GuildWarMiniMap();
                            _Demo1.GuildID = _Territory.GuildID;
                            _Demo1.GuildName = _Territory.GuildName;
                            _Demo1.Tax = _Territory.Tax;
                            _Demo1.MapID = _Territory.MapID;

                            if (Find.IsFightMapID)
                            {
                                _Demo1.MapType = 2;
                            }
                            else
                            {
                                if (_Territory.IsMainCity == 1)
                                {
                                    _Demo1.MapType = 1;
                                }
                                else
                                {
                                    _Demo1.MapType = 0;
                                }
                            }

                            if (ColorSelect.TryGetValue(Guild_ID, out string Color))
                            {
                                _Demo1.HexColor = Color;
                            }
                            else
                            {
                                string _ColorSelct = TotalColorDict.Take();
                                _Demo1.HexColor = _ColorSelct;
                                ColorSelect.TryAdd(Guild_ID, _ColorSelct);
                            }

                            _CaheMiniMap.Add(_Demo1);

                            // Nếu như đang trong thời gian tranh đoạt xảy ra lấy ra tất cả các map liền kề để nhấp nháy
                            //if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
                            //{
                            //    if (!this.IsTanThuThon(Find.MapID))
                            //    {
                            //        // Lấy ra toàn bộ các bản đồ liền kề với bản đồ này
                            //        GuildMapConfig NearMapConfig = _Config.NearMapConfig.Where(x => x.MapId == Find.MapID).FirstOrDefault();

                            //        if (NearMapConfig != null)
                            //        {
                            //            List<int> TotalMapNear = NearMapConfig.NearMap;

                            //            foreach (int MapNear in TotalMapNear)
                            //            {
                            //                var FindMapWar = _Config.WarMapConfigs.Where(x => x.MapID == MapNear).FirstOrDefault();

                            //                if (FindMapWar != null)
                            //                {
                            //                    var findExits = _CaheMiniMap.Where(x => x.MapID == MapNear).FirstOrDefault();
                            //                    if (findExits == null)
                            //                    {
                            //                        _Demo1 = new GuildWarMiniMap();
                            //                        _Demo1.GuildID = _Territory.GuildID;
                            //                        _Demo1.GuildName = "Bản đồ liền kề" + _Territory.GuildName;
                            //                        _Demo1.Tax = FindMapWar.Star;
                            //                        _Demo1.MapID = FindMapWar.MapID;
                            //                        _Demo1.MapType = 3;

                            //                        if (ColorSelect.TryGetValue(Guild_ID, out string ColorPick))
                            //                        {
                            //                            _Demo1.HexColor = ColorPick;
                            //                        }
                            //                        else
                            //                        {
                            //                            string _ColorSelct = TotalColorDict.Take();
                            //                            _Demo1.HexColor = _ColorSelct;
                            //                            ColorSelect.TryAdd(Guild_ID, _ColorSelct);
                            //                        }

                            //                        _CaheMiniMap.Add(_Demo1);
                            //                    }
                            //                }
                            //            }
                            //        }
                            //    }
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG GetAllMiniMapInfo :" + ex.ToString());
            }
        }

        /// <summary>
        /// Fill thông tin vào bản đồ mini map chỉ 1 lần duy nhất khi vào game
        /// </summary>
        public void FillMoreInfoToMiniMap()
        {
            if (IsKuaFuGuildWar)
            {
                return;
            }

            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
            {
                try
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "START FillMoreInfoToMiniMap !");

                    TerritoryInfo _GetAllTeroryInfo = this.GetTotalTerritoryInfo(0);

                    List<int> listMapNotied = new List<int>();

                    foreach (GuildMapRegisterFight _MapFight in TotalMapRegisterFight.ToList())
                    {
                        // Lãnh thổ nào mà bang mình tuyên chiến
                        //  if (_MapFight.GuildID == client.GuildID)
                        {
                            // Tìm lại  để chắc chắn bản đồ này có nằm trong danh sách sẽ mở tranh đoạt
                            var FindMapWar = _Config.WarMapConfigs.Where(x => x.MapID == _MapFight.MapId).FirstOrDefault();
                            // Nếu như bản đồ này có mở tranh đoạt
                            if (FindMapWar != null)
                            {
                                var findExits = _CaheMiniMap.Where(x => x.MapID == FindMapWar.MapID).FirstOrDefault();
                                if (findExits == null)
                                {
                                    GuildWarMiniMap _Demo1 = new GuildWarMiniMap();

                                    _Demo1.GuildID = _MapFight.GuildID;
                                    _Demo1.GuildName = "Tuyên Chiến :" + this.CountRegisterFight(_MapFight.MapId);
                                    _Demo1.Tax = 0;
                                    _Demo1.MapID = FindMapWar.MapID;
                                    _Demo1.MapType = 3;

                                    if (ColorSelect.TryGetValue(_MapFight.GuildID, out string Color))
                                    {
                                        _Demo1.HexColor = Color;
                                    }
                                    else
                                    {
                                        if (TotalColorDict.Count > 0)
                                        {
                                            string _ColorSelct = TotalColorDict.Take();
                                            _Demo1.HexColor = _ColorSelct;
                                            ColorSelect.TryAdd(_MapFight.GuildID, _ColorSelct);
                                        }
                                        else
                                        {
                                            _Demo1.HexColor = "#115c1e";
                                            ColorSelect.TryAdd(_MapFight.GuildID, "#115c1e");
                                        }
                                    }


                                    _CaheMiniMap.Add(_Demo1);
                                }
                                else
                                {
                                    findExits.MapType = 3;
                                }
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.GuildWarManager, "Bản đô [" + _MapFight.MapName + "] không mở tranh đoạt lãnh thổ");
                            }
                        }
                        if (listMapNotied.FindIndex(x => x == _MapFight.MapId) == -1) 
                        {//send message
                            listMapNotied.Add(_MapFight.MapId);

                            var allMap = TotalMapRegisterFight.FindAll(x => x.MapId == _MapFight.MapId);
                            var allGuildName = "";
                            for (int i = 0; i < allMap.Count; i++)
                            {
                                if (i != 0)
                                {
                                    allGuildName += ", ";
                                }
                                allGuildName += "[" + allMap[i].GuildName + "]";
                            }
                            if (_GetAllTeroryInfo != null)
                            {
                                if (_GetAllTeroryInfo.Territorys != null)
                                {
                                    var terInfo = _GetAllTeroryInfo.Territorys.Find(x => x.MapID == _MapFight.MapId);
                                    if(terInfo != null)
                                    {
                                        KTGlobal.SendSystemChat($"Bang hội {allGuildName} tuyên chiến lãnh thổ [{_MapFight.MapName}] của bang hội [{terInfo.MapName}]!");
                                    }
                                    else
                                    {
                                        KTGlobal.SendSystemChat($"Bang hội {allGuildName} tuyên chiến lãnh thổ [{_MapFight.MapName}]!");
                                    }
                                }
                                else
                                {
                                    KTGlobal.SendSystemChat($"Bang hội {allGuildName} tuyên chiến lãnh thổ [{_MapFight.MapName}]!");
                                }
                            }
                            else
                            {
                                KTGlobal.SendSystemChat($"Bang hội {allGuildName} tuyên chiến lãnh thổ [{_MapFight.MapName}]!");
                            }
                        }
                    }

                    LogManager.WriteLog(LogTypes.GuildWarManager, "END FillMoreInfoToMiniMap!");
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Lấy ra toàn bộ thông tin bản đồ mini map
        /// </summary>
        /// <returns></returns>
        public List<GuildWarMiniMap> GetAllMiniMapInfo(KPlayer client)
        {
            return _CaheMiniMap;
        }

        /// <summary>
        /// Tạo toàn bộ ActiveWarMap
        /// </summary>
        public void CreateAllActiveWarMap()
        {
            if (IsKuaFuGuildWar)
            {
                _kuaFuGuildWarLogic.CreateActiveWarMap(_Config, TotalMapWillBeWar);
                return;
            }

            // Lấy ra toàn bộ bản đồ lãnh thổ của game
            TerritoryInfo _GetAllTeroryInfo = this.GetTotalTerritoryInfo(0);

            LogManager.WriteLog(LogTypes.GuildWarManager, "START CreateAllActiveWarMap");

            try
            {
                if (_GetAllTeroryInfo != null)
                {
                    if (_GetAllTeroryInfo.Territorys != null)
                    {
                        LogManager.WriteLog(LogTypes.GuildWarManager, "CREATE ALL ACTIVE WAR:" + _GetAllTeroryInfo.Territorys.Count);

                        // Lấy ra tất cả các lãnh thổ hiện tại của máy chủ
                        foreach (Territory _Territory in _GetAllTeroryInfo.Territorys)
                        {
                            //Đọc ra bản đồ
                            int MAPID = _Territory.MapID;

                            // Đang thuộc về bang nào
                            int Guild_ID = _Territory.GuildID;

                            // Phải lưu cái thông tin này vào thông tin bang hội để lấy ra sau này cần vì mình ko viết Packet ghi vào DB
                            if (!TotalGuildInfo.ContainsKey(Guild_ID))
                            {
                                TotalGuildInfo.TryAdd(Guild_ID, _Territory.GuildName);
                            }

                            // lấy ra bản đồ cơ sở từ DB cái này ko cần sửa
                            var Find = _Config.WarMapConfigs.Where(x => x.MapID == MAPID).FirstOrDefault();

                            LogManager.WriteLog(LogTypes.GuildWarManager, "TÌM MAP CONFIG CỦA BẢN ĐỒ:" + Find.MapName);

                            if (!TotalMapWillBeWar.ContainsKey(Find.ReadMapID))
                            {   // Adđ luôn bản đồ này vào dánh ách sẽ active war
                                LogManager.WriteLog(LogTypes.GuildWarManager, "START MAP WAR :" + Find.MapName);
                                GuildWarMap _WarMap = new GuildWarMap(_Config);

                                _WarMap.MapName = Find.MapName;
                                _WarMap.BeLongGuild = Guild_ID;
                                _WarMap.BeLongGuildName = _Territory.GuildName;
                                _WarMap.MapId = Find.ReadMapID;
                                _WarMap.IsFightMap = Find.IsFightMapID;
                                // Add cho bang này vào danh sách
                                _WarMap.TotalGuildCanJoinBattle.Add(_Territory.GuildID);
                                // Add Bản đồ này vào danh sách sẽ dựng cột lên
                                TotalMapWillBeWar.TryAdd(Find.ReadMapID, _WarMap);
                            }
                            else
                            {
                                if (TotalMapWillBeWar.TryGetValue(Find.ReadMapID, out GuildWarMap _OutValue))
                                {
                                    // Add thằng này vào danh sách có thể tham gia trận chiến
                                    _OutValue.BeLongGuild = Guild_ID;
                                    _OutValue.BeLongGuildName = _Territory.GuildName;
                                    _OutValue.TotalGuildCanJoinBattle.Add(_Territory.GuildID);
                                }
                            }

                            // FIND TIẾP DANH SÁCH LIỀN KỀ
                            // Nếu đây không là là tân thủ thôn thì tìm các bản đồ liền kề

                            //if (!this.IsTanThuThon(Find.MapID))
                            //{
                            //    LogManager.WriteLog(LogTypes.GuildWarManager, "Tìm bản đồ liền kề của  bản đồ :" + Find.MapName);

                            //    // Lấy ra toàn bộ các bản đồ liền kề với bản đồ này
                            //    GuildMapConfig NearMapConfig = _Config.NearMapConfig.Where(x => x.MapId == Find.MapID).FirstOrDefault();

                            //    if (NearMapConfig != null)
                            //    {
                            //        List<int> TotalMapNear = NearMapConfig.NearMap;

                            //        foreach (int MapNear in TotalMapNear)
                            //        {
                            //            var FindMapWar = _Config.WarMapConfigs.Where(x => x.MapID == MapNear).FirstOrDefault();

                            //            if (FindMapWar != null)
                            //            {
                            //                if (!TotalMapWillBeWar.ContainsKey(FindMapWar.MapID))
                            //                {
                            //                    GuildWarMap _WarMap = new GuildWarMap(_Config);

                            //                    _WarMap.IsFightMap = false;
                            //                    _WarMap.MapId = FindMapWar.MapID;
                            //                    _WarMap.MapName = FindMapWar.MapName;

                            //                    // NẾu bang này không có trong danh sách thì thêm vào
                            //                    if (!_WarMap.TotalGuildCanJoinBattle.Contains(Guild_ID))
                            //                    {
                            //                        _WarMap.TotalGuildCanJoinBattle.Add(Guild_ID);
                            //                    }

                            //                    // Add Bản đồ này vào danh sách sẽ dựng cột lên
                            //                    TotalMapWillBeWar.TryAdd(FindMapWar.MapID, _WarMap);
                            //                }
                            //                else
                            //                {
                            //                    if (TotalMapWillBeWar.TryGetValue(FindMapWar.MapID, out GuildWarMap _OutValue))
                            //                    {
                            //                        if (!_OutValue.TotalGuildCanJoinBattle.Contains(Guild_ID))
                            //                        {
                            //                            // Thêm bang này vào danh sách có thể tham gia war
                            //                            _OutValue.TotalGuildCanJoinBattle.Add(Guild_ID);
                            //                        }
                            //                    }
                            //                }
                            //            }
                            //            else
                            //            {
                            //                LogManager.WriteLog(LogTypes.GuildWarManager, "CANT FILD MAP CONFIG :" + MapNear);
                            //            }
                            //        }
                            //    }
                            //    else
                            //    {
                            //        LogManager.WriteLog(LogTypes.GuildWarManager, "Không tìm thấy bản đồ liền kề của bản đồ :" + Find.MapName);
                            //    }
                            //}
                        }
                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "END CreateAllActiveWarMap");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG CreateAllActiveWarMap:" + ex.ToString());
            }
            // STEP 2 FILL NỐT các bản đồ đã đăng ký vào bản đồ chiến

            foreach (GuildMapRegisterFight _MapFight in TotalMapRegisterFight)
            {
                // Tìm lại  để chắc chắn bản đồ này có nằm trong danh sách sẽ mở tranh đoạt
                var FindMapWar = _Config.WarMapConfigs.Where(x => x.MapID == _MapFight.MapId).FirstOrDefault();

                if (!TotalGuildInfo.ContainsKey(_MapFight.GuildID))
                {
                    TotalGuildInfo.TryAdd(_MapFight.GuildID, _MapFight.GuildName);
                }

                // Nếu như bản đồ này có mở tranh đoạt
                if (FindMapWar != null)
                {
                    // Nếu bản đồ này chưa từng có trong danh sách ward

                    if (!TotalMapWillBeWar.ContainsKey(FindMapWar.ReadMapID))
                    {
                        LogManager.WriteLog(LogTypes.GuildWarManager, "START MAP WAR :" + FindMapWar.MapName);
                        // Tạo mới 1 bản đồ nếu mà chưa tồn tại
                        GuildWarMap _WarMap = new GuildWarMap(_Config);

                        // Nếu là tân thủ thôn
                        _WarMap.MapId = FindMapWar.ReadMapID;
                        _WarMap.IsFightMap = FindMapWar.IsFightMapID;
                        _WarMap.MapName = FindMapWar.MapName;

                        // Add cho bang này vào danh sách
                        if (!_WarMap.TotalGuildCanJoinBattle.Contains(_MapFight.GuildID))
                        {
                            _WarMap.TotalGuildCanJoinBattle.Add(_MapFight.GuildID);
                        }

                        // Add Bản đồ này vào danh sách sẽ dựng cột lên
                        TotalMapWillBeWar.TryAdd(FindMapWar.ReadMapID, _WarMap);
                    }
                    else
                    {
                        // NẾu mà bản đồ này có nằm trong danh sách các bản đồ sẽ khởi tạo war
                        if (TotalMapWillBeWar.TryGetValue(FindMapWar.ReadMapID, out GuildWarMap _OutValue))
                        {
                            if (!_OutValue.TotalGuildCanJoinBattle.Contains(_MapFight.GuildID))
                            {
                                _OutValue.TotalGuildCanJoinBattle.Add(_MapFight.GuildID);
                            }
                        }
                    }
                }
                else
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "Bản đô [" + _MapFight.MapName + "] không mở tranh đoạt lãnh thổ");
                }
            }
        }

        #region ScoreDef

        /// <summary>
        /// Cứ mỗi 60s mỗi trụ sẽ cộng cho bang nào đang chiếm giữ 10 điểm
        /// </summary>
        public void UpdateScoreForGuildEvery60SEC()
        {
            try
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "UpdateScoreForGuildEvery60SEC");

                foreach (KeyValuePair<int, GuildWarMap> entry in TotalMapWillBeWar)
                {
                    GuildWarMap MapEvent = entry.Value;

                    List<int> GuildBeWar = MapEvent.TotalGuildCanJoinBattle;

                    foreach (int GuildID in GuildBeWar)
                    {
                        int TotalLongTru = MapEvent.CountGuildObjective(GuildID);

                        if (TotalLongTru > 0)
                        {
                            int FINALPOINT = TotalLongTru * 10;

                            if (TotalGuildInfo.TryGetValue(GuildID, out string GUILDNAME))
                            {
                                this.UpdateGuildPoint(GuildID, GUILDNAME, FINALPOINT, MapEvent.MapId, MapEvent.MapName);
                            }
                            //ĐOẠN NÀY CẦN XEM LẠI
                        }
                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "UpdateScoreForGuildEvery60SEC END");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }

        public void UpdateExpEvery30Sec()
        {
            try
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "UpdateExpEvery30Sec");

                foreach (KeyValuePair<int, GuildWarMap> entry in TotalMapWillBeWar)
                {
                    GuildWarMap MapEvent = entry.Value;
                    MapEvent.UpdateEXP();
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "UpdateExpEvery30Sec END");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }

        public void UpdateGuildRanking()
        {
            ConcurrentDictionary<int, List<TerritoryReport>> TmpReport = new ConcurrentDictionary<int, List<TerritoryReport>>();

            try
            {
                // Duyệt toàn bộ điểm các của GUILD

                LogManager.WriteLog(LogTypes.GuildWarManager, "DO UpdateGuildRanking");

                foreach (KeyValuePair<int, List<GuildTotalScore>> entry in TotalScore)
                {
                    int MapID = entry.Key;

                    var Find = _Config.WarMapConfigs.Where(x => x.ReadMapID == MapID).FirstOrDefault();

                    List<GuildTotalScore> TotalGuildScore = entry.Value;

                    foreach (GuildTotalScore _Recore in TotalGuildScore)
                    {
                        TmpReport.TryGetValue(_Recore.GuildID, out List<TerritoryReport> _OutValue);

                        if (_OutValue != null)
                        {
                            TerritoryReport _Report = new TerritoryReport();
                            _Report.MapName = Find.MapName;
                            _Report.TotalPoint = _Recore.Score;
                            _Report.Rank = _Recore.TmpRank;

                            _OutValue.Add(_Report);
                        }
                        else
                        {
                            List<TerritoryReport> _NewListValue = new List<TerritoryReport>();

                            TerritoryReport _Report = new TerritoryReport();
                            _Report.MapName = Find.MapName;
                            _Report.TotalPoint = _Recore.Score;
                            _Report.Rank = _Recore.TmpRank;

                            _NewListValue.Add(_Report);

                            TmpReport.TryAdd(_Recore.GuildID, _NewListValue);
                        }
                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "END UpdateGuildRanking");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }

            this.TotalReportUpdate = TmpReport;
            // Sau khi duyệt xong bắt đầu xếp hạng
        }

        public void UpdateGuildPoint(int GuildID, string GuildName, int Score, int MapID, string MapName)
        {
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
            {
                // Nếu như có cái bản đồ này trong danh sách rồi thì
                if (TotalScore.TryGetValue(MapID, out List<GuildTotalScore> _GuildValue))
                {
                    // Lấy ra thằng có điểm cao nhất
                    int TopGuild = -1;
                    var Find = _GuildValue.OrderByDescending(x => x.Score).FirstOrDefault();

                    if (Find != null)
                    {
                        TopGuild = Find.GuildID;
                    }

                    // Nếu như tìm thấy nó trong danh sách
                    var find = _GuildValue.Where(x => x.GuildID == GuildID).FirstOrDefault();
                    if (find != null)
                    {
                        find.Score = find.Score + Score;
                        if (Score > 0)
                        {
                            find.ScoreReachedOrder = GetNextScoreReachedOrder();
                        }
                        int RankSoft = _GuildValue.OrderByDescending(x => x.Score).ToList().FindIndex(x => x.GuildID == GuildID);
                        find.TmpRank = RankSoft + 1;
                    }
                    else
                    {
                        GuildTotalScore _Score = new GuildTotalScore();
                        _Score.GuildID = GuildID;
                        _Score.GuildName = GuildName;
                        _Score.Score = Score;
                        if (Score > 0)
                        {
                            _Score.ScoreReachedOrder = GetNextScoreReachedOrder();
                        }

                        _GuildValue.Add(_Score);

                        int RankSoft = _GuildValue.OrderByDescending(x => x.Score).ToList().FindIndex(x => x.GuildID == GuildID);
                        _Score.TmpRank = RankSoft + 1;
                    }

                    if (TopGuild != -1)
                    {
                        var FindAfter = _GuildValue.OrderByDescending(x => x.Score).FirstOrDefault();

                        if (FindAfter != null)
                        {
                            if (TopGuild != Find.GuildID)
                            {
                                long Now = TimeUtil.NOW();

                                string MSG = "Chiến báo : Bang ta đã dành được lợi thế ở <b>" + KTGlobal.CreateStringByColor(MapName, ColorType.Importal) + "</b>";

                                string MSGNeft = "Chiến báo : Bang ta đã bị thất thế tại  <b>" + KTGlobal.CreateStringByColor(MapName, ColorType.Importal) + "</b>";

                                // Nếu đã qua 1 phút mà chưa thông báo gì
                                if (Now - Find.LastArlet > 60000)
                                {
                                    Find.LastArlet = Now;
                                    // Thông báo cho bang bị mất lợi thế
                                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, Find.GuildID, MSGNeft, null, "");
                                }

                                if (Now - FindAfter.LastArlet > 60000)
                                {
                                    FindAfter.LastArlet = Now;
                                    // Thông báo cho bang đang có lợi thế
                                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, Find.GuildID, MSG, null, "");
                                }
                            }
                        }
                    }

                    // TODO THỰC HIỆN SOFT LẠI CHO NÓ NHẸ
                }
                else
                {
                    List<GuildTotalScore> _NewGuildValue = new List<GuildTotalScore>();

                    GuildTotalScore _Score = new GuildTotalScore();
                    _Score.GuildID = GuildID;
                    _Score.GuildName = GuildName;
                    _Score.Score = Score;
                    if (Score > 0)
                    {
                        _Score.ScoreReachedOrder = GetNextScoreReachedOrder();
                    }
                    _Score.TmpRank = 1;

                    _NewGuildValue.Add(_Score);

                    // add vào trong map
                    TotalScore.TryAdd(MapID, _NewGuildValue);
                }
            }
        }

        // Xử lý điểm khi người chơi giết người chơi
        public void OnDie(GameObject Kill, GameObject BeKill, int curMapCode)
        {
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
            {
                try
                {
                    if(!TotalMapWillBeWar.TryGetValue(curMapCode, out GuildWarMap outMap))
                    {
                        return;
                    }
                    if (Kill is KPlayer && BeKill is KPlayer)
                    {
                        KPlayer kPlayer_Kill = (KPlayer)Kill;

                        KPlayer kPlayer_BeKill = (KPlayer)BeKill;

                        int CurentBeKilLScore = 0;
                        int beKillGuildWarGuildId = GetGuildWarGuildId(kPlayer_BeKill);

                        // Lấy ra danh sách các thành viên trong bang hội
                        if (WarScore.TryGetValue(beKillGuildWarGuildId, out ConcurrentDictionary<int, GuildWarPlayer> _OutList))
                        {
                            _OutList.TryGetValue(kPlayer_BeKill.RoleID, out GuildWarPlayer _PlayerOut);

                            if (_PlayerOut != null)
                            {
                                CurentBeKilLScore = _PlayerOut.Score;
                            }
                        }

                        int MaxScoreCanGet = GetPointCanGet(CurentBeKilLScore);

                        UpdateScore(kPlayer_Kill, MaxScoreCanGet, 0, 1, false);
                        UpdateScore(kPlayer_BeKill, 0, 0, 0, true);

                        //if (kPlayer_Kill.TeamID != -1)
                        //{
                        //    int MaxScoreCanGetByTeam = GetPointCanGetByTeam(CurentBeKilLScore);

                        //    List<KPlayer> TotalMember = kPlayer_Kill.Teammates;

                        //    foreach (KPlayer member in TotalMember)
                        //    {
                        //        // Nếu như không nằm trong bán kính thì không cho ăn ké
                        //        if (!Global.InCircle(new Point(member.PosX, member.PosY), kPlayer_Kill.CurrentPos, 1000))
                        //        {
                        //            continue;
                        //        }

                        //        if (member.RoleID != kPlayer_BeKill.RoleID)
                        //        {
                        //            //Cho ăn ké của đồng  đội
                        //            UpdateScore(member, MaxScoreCanGetByTeam, 0, 1, false);
                        //        }
                        //    }
                        //}
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "OnDie TOÁC :" + ex.ToString());
                }
            }
        }

        public void SendKillStreak(KPlayer InputPlayer, int Count)
        {
            G2C_KillStreak _State = new G2C_KillStreak();

            _State.KillNumber = Count;

            if (InputPlayer.IsOnline())
            {
                InputPlayer.SendPacket<G2C_KillStreak>((int)TCPGameServerCmds.CMD_KT_KILLSTREAK, _State);
            }
        }

        public int GetPointCanGetByTeam(int CurentScore)
        {
            int Point = 0;

            if (CurentScore >= 0 && CurentScore < 2000)
            {
                Point = 1;
            }
            else if (CurentScore >= 2000 && CurentScore < 6000)
            {
                Point = 2;
            }
            else if (CurentScore >= 6000 && CurentScore < 8000)
            {
                Point = 7;
            }
            else if (CurentScore >= 8000 && CurentScore < 10000)
            {
                Point = 15;
            }
            else if (CurentScore >= 10000)
            {
                Point = 30;
            }

            return Point;
        }

        public int GetPointCanGet(int CurentScore)
        {
            int Point = 0;

            if (CurentScore >= 0 && CurentScore < 2000)
            {
                Point = 37;
            }
            else if (CurentScore >= 2000 && CurentScore < 6000)
            {
                Point = 75;
            }
            else if (CurentScore >= 6000 && CurentScore < 8000)
            {
                Point = 100;
            }
            else if (CurentScore >= 8000 && CurentScore < 10000)
            {
                Point = 150;
            }
            else if (CurentScore >= 10000)
            {
                Point = 300;
            }

            return Point;
        }

        public string GetRankTitleByPoint(int CurentScore)
        {
            string Title = "";

            if (CurentScore >= 0 && CurentScore < 2000)
            {
                Title = "<color=#ffffff>Binh sĩ</color>";
            }
            else if (CurentScore >= 2000 && CurentScore < 6000)
            {
                Title = "<color=#109de8>Hiệu úy</color>";
            }
            else if (CurentScore >= 6000 && CurentScore < 8000)
            {
                Title = "<color=#b268c4>Thống lĩnh</color>";
            }
            else if (CurentScore >= 8000 && CurentScore < 10000)
            {
                Title = "<color=#fac241>Phó tướng</color>";
            }
            else if (CurentScore >= 10000)
            {
                Title = "<color=#f2fa00>Đại Tướng</color>";
            }

            return Title;
        }

        public void UpdateScore(KPlayer _Player, int Score, int DesotryCount, int KillCount, bool IsRessetStreak)
        {
            int GuildID = GetGuildWarGuildId(_Player);

            try
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "Update SCORE :" + _Player.RoleName + "| POINT :" + Score);
                // Chắn chắn thằng này phải có trong danh sách mới được tính điểm
                if (WarScore.TryGetValue(GuildID, out ConcurrentDictionary<int, GuildWarPlayer> _OutList))
                {
                    if (_OutList.TryGetValue(_Player.RoleID, out GuildWarPlayer find))
                    {
                        if (find != null)
                        {
                            // Cộng điểm cho thằng người chơi này
                            find.Score = find.Score + Score;
                            if (Score > 0)
                            {
                                find.ScoreReachedOrder = GetNextScoreReachedOrder();
                            }
                            // Nếu như có dấu hiệu phá hủy
                            if (DesotryCount > 0)
                            {
                                // Cộng tích lũy công trình
                                find.DestroyCount = find.DestroyCount + DesotryCount;
                            }
                            // Nếu như có ghi nhận giết người
                            if (KillCount > 0)
                            {
                                // Cập số người đã giết
                                find.KillCount = find.KillCount + KillCount;
                            }
                            // nếu bị reset Steak
                            if (IsRessetStreak)
                            {
                                // Thì thực hiện reset lại streak
                                find.CurentKillSteak = 0;
                            }
                            else
                            {
                                find.CurentKillSteak = find.CurentKillSteak + KillCount;

                                if (find.CurentKillSteak > find.MaxKillSteak)
                                {
                                    find.MaxKillSteak = find.CurentKillSteak;
                                }

                                if (find.CurentKillSteak > 3)
                                {
                                    SendKillStreak(_Player, find.CurentKillSteak);
                                }
                            }

                            // Lấy ra danh hiệu của người chơi có thể nhận được
                            string Title = GetRankTitleByPoint(find.Score);

                            if (_Player.TempTitle != Title)
                            {
                                //Sét lại danh hiệu cho người chơi
                                _Player.TempTitle = Title;
                            }

                            //  Tạo ra 1 cái TMP để lấy thứ tự
                            List<GuildWarPlayer> _TMP = _OutList.Values.OrderByDescending(x => x.Score).ToList();

                            //WarScore[_Player.GuildID] = _TMP;

                            var findIndex = _TMP.FindIndex(x => x._Player.RoleID == _Player.RoleID);

                            find.CurentRank = findIndex + 1;

                            // TODO : UPDATE TILE RANK FOR PLAYER

                            SendNotifyLoop(find);
                        }
                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "Update SCORE DONE :" + _Player.RoleName + "| POINT :" + Score);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "UpdateScore TOÁC :" + ex.ToString());
            }

            //  Console.WriteLine("Update SCORE :" + _Player.RoleName + "| POINT :" + Score);
        }

        #endregion ScoreDef

        #region DatabaseRequest

        /// <summary>
        /// lấy ra thông tin lãnh thổ của bang hiện tại
        /// </summary>
        /// <param name="GuildID"></param>
        /// <param name="serverId"></param>
        /// <returns></returns>
        public TerritoryInfo GetTerritoryInfo(int GuildID, int serverId = GameManager.LocalServerId)
        {
            try
            {
                byte[] bytesData = null;
                if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_KT_GUILD_TERRITORY, string.Format("{0}", GuildID), out bytesData, serverId))
                {
                    return null;
                }

                if (null == bytesData || bytesData.Length <= 6)
                {
                    return null;
                }

                Int32 length = BitConverter.ToInt32(bytesData, 0);

                TerritoryInfo TerritoryData = DataHelper.BytesToObject<TerritoryInfo>(bytesData, 6, length - 2);

                if (null == TerritoryData)
                {
                    return null;
                }

                return TerritoryData;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Update Data
        /// </summary>
        public bool UpdateTerritoryData(int MapID, string MapName, int GuildID, int Star, int Tax, int ZoneID, int IsMainCity, int Type)
        {

            string CMDUPDATE = MapID + ":" + DataHelper.EncodeBase64(MapName) + ":" + GuildID + ":" + Star + ":" + Tax + ":" + ZoneID + ":" + IsMainCity + ":" + Type;

            string[] UpdateValue = Global.SendToDB((int)TCPGameServerCmds.CMD_KT_GUILD_UPDATE_TERRITORY, CMDUPDATE, GameManager.LocalServerId);

            if (UpdateValue[1] == "1")
            {
                if (Type == 0)
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + Type + "] Xóa lãnh thổ thành công :" + MapName);

                    string Notify = "Lảnh thổ đã bị đánh mất:<b>" + KTGlobal.CreateStringByColor(MapName, ColorType.Importal) + "</b>";

                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, GuildID, Notify, null, "");
                    return true;
                }
                else
                {
                    if (Type == 1)
                    {
                        string Notify = "Bang hội đã chiếm đóng được:<b>" + KTGlobal.CreateStringByColor(MapName, ColorType.Green) + "</b>";

                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, GuildID, Notify, null, "");
                    }
                    else if (Type == 2)
                    {
                        string Notify = "Bang hội đã thiết lập <b>" + KTGlobal.CreateStringByColor(MapName, ColorType.Green) + "</b> làm thành chính.";

                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, GuildID, Notify, null, "");
                    }
                    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + Type + "] Cập nhật lãnh thổ thành công :" + MapName + "| MapID :" + MapID + "| GuildID :" + GuildID + "| Star :" + Star + "| Tax :" + Tax + "| ZoneID :" + ZoneID + "| IsMainCity :" + IsMainCity);
                    return true;
                }
            }
            else
            {
                if (Type == 0)
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + Type + "] XXXXóa lãnh thổ thất bại :" + MapName);
                    return false;
                }
                else
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + Type + "] CCCCCập nhật lãnh thổ thất bại :" + MapName + "| MapID :" + MapID + "| GuildID :" + GuildID + "| Star :" + Star + "| Tax :" + Tax + "| ZoneID :" + ZoneID + "| IsMainCity :" + IsMainCity);
                    return false;
                }
            }
        }

        /// <summary>
        /// Lấy ra toàn bộ thông tin của bản đô fcuar máy chủ hiện tại
        /// </summary>
        /// <param name="GuildID"></param>
        /// <param name="serverId"></param>
        /// <returns></returns>
        public TerritoryInfo GetTotalTerritoryInfo(int GuildID, int serverId = GameManager.LocalServerId)
        {
            byte[] bytesData = null;
            if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_KT_GUILD_ALLTERRITORY, string.Format("{0}", GuildID), out bytesData, serverId))
            {
                return null;
            }

            if (null == bytesData || bytesData.Length <= 6)
            {
                return null;
            }

            Int32 length = BitConverter.ToInt32(bytesData, 0);

            TerritoryInfo TerritoryData = DataHelper.BytesToObject<TerritoryInfo>(bytesData, 6, length - 2);

            if (null == TerritoryData)
            {
                return null;
            }

            return TerritoryData;
        }

        public MiniGuildInfo GetMiniGuildInfo(int GuildID, int serverId = GameManager.LocalServerId)
        {
            byte[] bytesData = null;
            if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer3(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_KT_GUILD_GETMINIGUILDINFO, string.Format("{0}", GuildID), out bytesData, serverId))
            {
                return null;
            }

            if (null == bytesData || bytesData.Length <= 6)
            {
                return null;
            }

            Int32 length = BitConverter.ToInt32(bytesData, 0);

            MiniGuildInfo _MiniGUild = DataHelper.BytesToObject<MiniGuildInfo>(bytesData, 6, length - 2);

            if (null == _MiniGUild)
            {
                return null;
            }

            return _MiniGUild;
        }

        public void ReloadAllTerritoryInfo()
        {
            string CMDUPDATE = "1";

            string[] UpdateValue = Global.SendToDB((int)TCPGameServerCmds.CMD_KT_GUILD_ALLTERRITORY, CMDUPDATE, GameManager.LocalServerId);
        }

        /// <summary>
        ///  Kiểm tra xem đây có phải tân thủ thôn hay không
        /// </summary>
        /// <param name="MapID"></param>
        /// <returns></returns>
        public bool IsTanThuThon(int MapID)
        {
            var find = _Config.WarMapConfigs.Where(x => x.MapID == MapID).FirstOrDefault();
            if (find != null)
            {
                return find.IsFightMapID;
            }

            return false;
        }

        public void ThuHoiThonTanThuMoiThu7()
        {
            var curDay = DateTime.Now.DayOfWeek;
            if (curDay == DayOfWeek.Saturday)
            {
                TerritoryInfo _GetAllTeroryInfo = this.GetTotalTerritoryInfo(0);

                if (_GetAllTeroryInfo != null)
                {
                    if (_GetAllTeroryInfo.Territorys != null)
                    {
                        var listThonTanThu = _GetAllTeroryInfo.Territorys.FindAll(x => IsTanThuThon(x.MapID));
                        if (!listThonTanThu.IsNullOrEmpty())
                        {
                            foreach (var ter in listThonTanThu)
                            {
                                //_GetAllTeroryInfo.Territorys.Count(x => x.GuildID == ter)
                            }
                        }
                    }
                }
            }
        }

        public int GetTimeSetMainCity(int GuildID, int serverId = GameManager.LocalServerId)
        {
            string strcmd = $"{GuildID}:{0}:{0}";
            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_GUILD_GET_SET_TIME_SETUP_MAIN_CITY, strcmd, GameManager.LocalServerId);
            if (null == dbFields) return -1;

            return Convert.ToInt32(dbFields[1]);
        }

        #endregion DatabaseRequest

        #region NpClick

        /// <summary>
        /// Quản lãnh thổ click
        /// </summary>
        /// <param name="map"></param>
        /// <param name="npc"></param>
        /// <param name="client"></param>
        public void NpcClick(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            Dictionary<int, string> Selections = new Dictionary<int, string>();

            if (_kuaFuGuildWarLogic.TryShowNpcDialog(npc, client, BATTLESTATE, TotalMapWillBeWar, (x) => DoActionSelect(client, npc, x)))
            {
                return;
            }

            //Console.Write(_Config);

            // Nếu mà chiến trường đã kết thúc thì add thêm cái nhận thưởng
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_REGISTER || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART)
            {
                if (client.GuildID > 0)
                {
                    // _NpcDialog.Text = "Có thể tấn công lãnh thổ lân cận đã bị chiếm lĩnh.Bang của bạn có thể tuyên chiến với các lãnh thổ sau:";

                    if (client.GuildRank == (int)GuildRank.Master)
                    {
                        _NpcDialog.Text += "Phí dụng tuyên chiến là 500 vạn bạc một lần.<br>";
                        _NpcDialog.Text = "Điều kiện tuyên chiến:<br>Bang hội phải đủ 10.000 uy danh.<br>Bang hội phải đủ 30 người.<br>Quỹ bang hội phải từ 1000 vạn.<br><br>";
                        
                        
                        TerritoryInfo territoryInfo = GetTerritoryInfo(client.GuildID);

                        if (territoryInfo != null)
                        {
                            //Console.WriteLine(territoryInfo);

                            // Nếu như trường hợp còn cái dái khô không có lãnh thổ nào thì cho nó được đnáh chiếm các map tân thủ thôn
                            if (territoryInfo.Territorys == null)
                            {
                                _NpcDialog.Text += "<b>Vì Bang Hội của bạn chưa chiếm được 1 lãnh thổ nào.Nên bang hội của bạn chỉ có thể chọn 1 trong số các Tân Thủ Thôn Sau để chiếm đóng</b><br>Sau khi chiếm đóng được 1 tân thủ thôn bất kỳ.Ở lần tranh đoạt đợt sau Bang Hội của bạn có thể chọn tùy ý 1 lãnh thổ để chiếm đóng!";

                                List<OpenConfig> TotalMapCanFight = _Config.WarMapConfigs.Where(x => x.FightMapID > 0).ToList();

                                // Chỉ lấy ra tân thủ thôn nếu như thằng này chưa chiếm được lãnh thổ nào
                                foreach (OpenConfig _OpenMap in TotalMapCanFight)
                                {
                                    Selections.Add(_OpenMap.MapID, _OpenMap.MapName + " | Đã tuyên chiến:" + CountRegisterFight(_OpenMap.MapID));
                                }
                            } // Nếu như bang này đã có lãnh thổ
                            else if (territoryInfo.Territorys != null)
                            {
                                // TH1 : Nếu bang này chỉ có 1 lãnh thổ
                                if (territoryInfo.Territorys.Count > 0)
                                {
                                    // Kiểm tra xem các lãnh thổ bang này chiếm được có phải là tân thủ thôn hay không
                                    bool IsAllTanThuThon = true;

                                    foreach (Territory _Terriory in territoryInfo.Territorys)
                                    {
                                        //Nếu đây là tân thủ thôn
                                        if (!IsTanThuThon(_Terriory.MapID))
                                        {
                                            IsAllTanThuThon = false;
                                            break;
                                        }
                                    }

                                    // Nếu tất cả đều là tân thủ thôn thì được khiếu chiến 1 bản đồ bất kỳ
                                    if (IsAllTanThuThon)
                                    {
                                        _NpcDialog.Text += "<b><color=yellow>Vì Bang Hội của bạn đang chiếm giữ Tân Thủ Thôn và chưa có 1 lãnh thổ nào nên Bang Hội của bạn sẽ được chọn 1 lãnh thổ bất kỳ để tuyên chiến để xây dựng thành chính</b></color><br><br>Hãy cân nhắc kỹ trước khi chọn vì nó sẽ là vị trí chiến lược ảnh hưởng rất lớn tới chiến thuật sau này!";

                                        // Chỉ lấy ra tân thủ thôn nếu như thằng này chưa chiếm được lãnh thổ nào
                                        foreach (OpenConfig _OpenMap in _Config.WarMapConfigs.Where(x => x.FightMapID == -1))
                                        {
                                            Selections.Add(_OpenMap.MapID, _OpenMap.MapName + " | Đã tuyên chiến:" + CountRegisterFight(_OpenMap.MapID));
                                        }
                                    } // Nếu như đã có lãnh thổ
                                    else
                                    {
                                        var findMainCity = territoryInfo.Territorys.Where(x => x.IsMainCity == 1).FirstOrDefault();

                                        // Chắc chắn là có thành chính thì mới có liền kề
                                        if (findMainCity != null)
                                        {
                                            _NpcDialog.Text += "<b>Bang bạn đang có [" + findMainCity.MapName + "] đang là thành chính!Bạn chỉ có thể đánh chiếm các khu liền với lãnh thổ của bang mình bao gồm:<br><br>";
                                        }
                                        else
                                        {
                                            _NpcDialog.Text += "<b>Bang bạn chỉ có thể đánh chiếm các khu liền với lãnh thổ của bang mình bao gồm:<br><br>";
                                        }

                                        // Duyệt lại toàn bộ các lãnh thổ

                                        foreach (Territory _AreadyFix in territoryInfo.Territorys)
                                        {
                                            // Chắc chắn bản đồ này phỉa không có tân thủ thôn
                                            if (!IsTanThuThon(_AreadyFix.MapID))
                                            {
                                                // Tìm ra bản đồ liền kề với lãnh thổ hiện tại
                                                GuildMapConfig NearMapConfig = _Config.NearMapConfig.Where(x => x.MapId == _AreadyFix.MapID).FirstOrDefault();

                                                if (NearMapConfig != null)
                                                {
                                                    // Lấy ra toàn bộ bản đồ liền kề
                                                    List<int> TotalMapNear = NearMapConfig.NearMap;

                                                    foreach (int MapNear in TotalMapNear)
                                                    {
                                                        // Tìm xem có bản đồ này không
                                                        var FindOpenConfig = _Config.WarMapConfigs.Where(x => x.MapID == MapNear).FirstOrDefault();

                                                        if (FindOpenConfig != null)
                                                        {
                                                            // Tức là lãnh thổ này phỉa chưa có trong lãnh thổ của bang thì mới được phép tuyên chuyến
                                                            var FINDEXITS = territoryInfo.Territorys.Where(x => x.MapID == MapNear).FirstOrDefault();

                                                            if (FINDEXITS == null)
                                                            {
                                                                if (!Selections.ContainsKey(FindOpenConfig.MapID))
                                                                {
                                                                    _NpcDialog.Text += "<color=green>" + FindOpenConfig.MapName + "</color><br>";
                                                                    Selections.Add(FindOpenConfig.MapID, FindOpenConfig.MapName + " | Đã tuyên chiến:" + CountRegisterFight(FindOpenConfig.MapID));
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            LogManager.WriteLog(LogTypes.GuildWarManager, "[" + client.GuildName + "] Bản đồ liền kề này không mở tranh đoạt :" + MapNear);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + client.GuildName + "] không tìm thấy bản đồ liền kề của :" + _AreadyFix.MapID);
                                                }
                                            }
                                            else
                                            {
                                                LogManager.WriteLog(LogTypes.GuildWarManager, "[" + client.GuildName + "] Kiểm tra bản đồ :" + _AreadyFix.MapID + "==> Là tân thủ thôn nên bỏ qua");
                                            }
                                        }
                                    }
                                }
                                else // Nếu chưa có lãnh thổ nào thì cho đánh thôn
                                {
                                    _NpcDialog.Text += "<b>Vì Bang Hội của bạn chưa chiếm được 1 lãnh thổ nào.Nên bang hội của bạn chỉ có thể chọn 1 trong số các Tân Thủ Thôn Sau để chiếm đóng</b><br>Sau khi chiếm đóng được 1 tân thủ thôn bất kỳ.Ở lần tranh đoạt đợt sau Bang Hội của bạn có thể chọn tùy ý 1 lãnh thổ để chiếm đóng!";

                                    List<OpenConfig> TotalMapCanFight = _Config.WarMapConfigs.Where(x => x.FightMapID > 0).ToList();

                                    // Chỉ lấy ra tân thủ thôn nếu như thằng này chưa chiếm được lãnh thổ nào
                                    foreach (OpenConfig _OpenMap in TotalMapCanFight)
                                    {
                                        Selections.Add(_OpenMap.MapID, _OpenMap.MapName + " | Đã tuyên chiến:" + CountRegisterFight(_OpenMap.MapID));
                                    }
                                }
                            }
                        }
                        else
                        {
                            _NpcDialog.Text += "<b>Vì Bang Hội của bạn chưa chiếm được 1 lãnh thổ nào.Nên bang hội của bạn chỉ có thể chọn 1 trong số các Tân Thủ Thôn Sau để chiếm đóng</b><br>Sau khi chiếm đóng được 1 tân thủ thôn bất kỳ.Ở lần tranh đoạt đợt sau Bang Hội của bạn có thể chọn tùy ý 1 lãnh thổ để chiếm đóng!";

                            List<OpenConfig> TotalMapCanFight = _Config.WarMapConfigs.Where(x => x.FightMapID > 0).ToList();

                            // Chỉ lấy ra tân thủ thôn nếu như thằng này chưa chiếm được lãnh thổ nào
                            foreach (OpenConfig _OpenMap in TotalMapCanFight)
                            {
                                Selections.Add(_OpenMap.MapID, _OpenMap.MapName + " | Đã tuyên chiến:" + CountRegisterFight(_OpenMap.MapID));
                            }
                        }
                    }
                    else
                    {
                        _NpcDialog.Text = "Hãy bảo bang chủ của ngươi tới gặp ta!";
                    }
                }
                else
                {
                    _NpcDialog.Text = "Hãy gia nhập một bang hội rồi hãy quay lại gặp ta!";
                }
            }
            else if (BATTLESTATE == GUILDWARDSTATUS.STATUS_END)
            {
                bool isWin = false;
                _NpcDialog.Text = "Xin chào :" + client.RoleName + " Bang hội <b>" + KTGlobal.CreateStringByColor(client.GuildName, ColorType.Green) + "</b> sau thời gian chinh chiến Bang của bạn đã đạt được kết quả sau:<br><br><br> ";

                if (FinalResult.TryGetValue(client.GuildID, out List<FightResult> result))
                {
                    if (result != null)
                    {
                        List<FightResult> ChiemDuoc = result.Where(x => x.Type == 0).ToList();
                        if (ChiemDuoc.Count > 0)
                        {
                            _NpcDialog.Text += "<b><color=green>Lãnh thổ đánh chiếm được :</color></b><br>";

                            foreach (FightResult _LanhTho in ChiemDuoc)
                            {
                                _NpcDialog.Text += "<b> " + _LanhTho.MapName + " </b><br>";
                                isWin = true;
                            }
                        }

                        List<FightResult> BaoVe = result.Where(x => x.Type == 1).ToList();
                        if (BaoVe.Count > 0)
                        {
                            _NpcDialog.Text += "<b><color=green>Lãnh đã bảo vệ thành công :</color></b><br>";

                            foreach (FightResult _LanhTho in BaoVe)
                            {
                                _NpcDialog.Text += "<b> " + _LanhTho.MapName + " </b><br>";
                                isWin = true;
                            }
                        }

                        List<FightResult> DeMat = result.Where(x => x.Type == 2).ToList();
                        if (DeMat.Count > 0)
                        {
                            _NpcDialog.Text += "<b><color=red>Lãnh thổ bị đánh chiếm :</color></b><br>";

                            foreach (FightResult _LanhTho in DeMat)
                            {
                                _NpcDialog.Text += "<b> " + _LanhTho.MapName + " </b><br>";
                            }
                        }

                        List<FightResult> ThuHoi = result.Where(x => x.Type == 3).ToList();
                        if (ThuHoi.Count > 0)
                        {
                            _NpcDialog.Text += "<b><color=red>Lãnh thổ bị hệ thống thu hồi :</color></b><br>";

                            foreach (FightResult _LanhTho in ThuHoi)
                            {
                                _NpcDialog.Text += "<b> " + _LanhTho.MapName + " </b><br>";
                            }
                        }
                    }
                }
                _NpcDialog.Text += $"<br><br>Người chơi có tài phú ít nhất  1000 và đeo phi phong {KTLuaLib_System.GetMantleNameFromLevel(ServerConfig.Instance.MantleLevelForGuildWar)} trở lên mới có thể nhận thưởng<br>";
                _NpcDialog.Text += "<br><br>Trong giai đoạn chinh chiến bạn nhận được :<br>";

                // Lấy ra dict chứa điểm của thằng này
                // Thử lấy ra
                if (WarScore.TryGetValue(client.GuildID, out ConcurrentDictionary<int, GuildWarPlayer> _Out))
                {
                    int playScore = 0;
                    if (_Out != null)
                    {
                        _Out.TryGetValue(client.RoleID, out GuildWarPlayer find);

                        if (find != null)
                        {
                            _NpcDialog.Text += "<b>" + find.Score + " điểm </b> công trạng cá nhân!<br>";
                            playScore = find.Score;
                        }
                    }

                    int SelectIndex = _Out.Values.OrderByDescending(x => x.Score).ToList().FindIndex(x => x._Player.RoleID == client.RoleID) + 1;

                    _NpcDialog.Text += $"Xếp hạng thứ <color=green>{SelectIndex}</color> toàn bang!<br>";

                    int BoxCanEarn = 0;
                    var configPointAward = _Config.Award.FindAll(x => x.Point < playScore && x.type == "Point").OrderByDescending(x => x.Point).FirstOrDefault();
                    if (configPointAward!= null)
                    {
                        BoxCanEarn = configPointAward.BoxCount;
                    }
                    else
                    {
                        _NpcDialog.Text += $"Không đủ điều kiện nhận thưởng công trạng cá nhân!<br>";
                    }

                    int FinalDanhVong = 0;
                    int FinalExp = 0;
                    int FinalBacKhoa = 0;
                    int FinalUyDanh = 0;

                    var configRankAwards = _Config.Award.FindAll(x => x.type == "rank");
                    foreach (var rankAward in configRankAwards)
                    {
                        string strRank = rankAward.Rank;
                        string[] ranks = strRank.Split(':');
                        if(ranks.Length == 1)
                        {
                            if (SelectIndex == Convert.ToInt32(ranks[0]))
                            {
                                if(playScore< rankAward.Point)
                                {
                                    _NpcDialog.Text += $"Không đủ điều kiện nhận thưởng xếp hạng, cần tối thiểu {rankAward.Point} điểm công trạng cá nhân!<br>";
                                }
                                else
                                {
                                    FinalDanhVong = rankAward.DanhVongTDLT;
                                    FinalExp = rankAward.Exp * client.m_Level;
                                    FinalUyDanh = rankAward.Prestige;
                                    FinalBacKhoa = rankAward.BacKhoa;
                                }
                            }
                        }
                        else
                        {
                            if (SelectIndex >= Convert.ToInt32(ranks[0]) && SelectIndex <= Convert.ToInt32(ranks[1]))
                            {
                                if (playScore < rankAward.Point)
                                {
                                    _NpcDialog.Text += $"Không đủ điều kiện nhận thưởng xếp hạng, cần tối thiểu {rankAward.Point} điểm công trạng cá nhân!<br>";
                                }
                                else
                                {
                                    FinalDanhVong = rankAward.DanhVongTDLT;
                                    FinalExp = rankAward.Exp * client.m_Level;
                                    FinalUyDanh = rankAward.Prestige;
                                    FinalBacKhoa = rankAward.BacKhoa;
                                }
                            }
                        }
                    }
                    
                    

                    if (!isWin)
                    {
                        BoxCanEarn = (int)Math.Ceiling((double)BoxCanEarn / 2);
                        FinalDanhVong = (int)Math.Ceiling((double)FinalDanhVong / 2);
                        FinalExp = (int)Math.Ceiling((double)FinalExp / 2);
                        FinalUyDanh = (int)Math.Ceiling((double)FinalUyDanh / 2);
                        FinalBacKhoa = (int)Math.Ceiling((double)FinalBacKhoa / 2);

                        _NpcDialog.Text += $"Bang hội đã thua trong lần tranh đoạt lãnh thổ lần này nên chỉ nhận được một nửa phần thưởng<br><br>";
                    }

                    

                    _NpcDialog.Text += $"Thưởng điểm công trạng cá nhân <color=green>{BoxCanEarn}</color> rương tranh đoạt lãnh thổ!<br>";
                    _NpcDialog.Text += $"Thưởng xếp hạng <color=green>{FinalExp}</color> điểm kinh nghiệm, <color=green>{FinalBacKhoa}</color> bạc khóa, <color=green>{FinalUyDanh}</color> uy danh, <color=green>{FinalDanhVong}</color> điểm danh vọng tranh đoạt lãnh thổ, !<br>";

                    
                }

                Selections.Add(1, "Ta muốn nhận thưởng");

                Selections.Add(1000, "Ta sẽ quay lại sau!");
            }
            else
            {
                _NpcDialog.Text = "Tranh đoạn chưa bắt đầu vui lòng quay lại sau";
            }

            //// Menu này lúc nào cũng có

            //Selections.Add(-2, "Ta muốn rời khỏi chiến trường");

            Action<TaskCallBack> ActionWork = (x) => DoActionSelect(client, npc, x);

            _NpcDialog.OnSelect = ActionWork;

            _NpcDialog.Selections = Selections;

            //_NpcDialog.Text = Text;

            _NpcDialog.Show(npc, client);
        }

        public int GetPercentInGuild(KPlayer client)
        {
            int Pecent = -1;
            int guildWarGuildId = GetGuildWarGuildId(client);

            if (WarScore.TryGetValue(guildWarGuildId, out ConcurrentDictionary<int, GuildWarPlayer> _Out))
            {
                if (_Out != null)
                {
                    //SORT LẠI
                    List<GuildWarPlayer> _TMP = _Out.Values.OrderByDescending(x => x.Score).ToList();

                    if (_TMP != null)
                    {
                        int YourIndex = _TMP.FindIndex(x => x._Player.RoleID == client.RoleID);

                        int Total = _TMP.Count;

                        Pecent = (YourIndex * 100) / Total;
                    }
                }
            }

            return Pecent;
        }

        private void DoActionSelect(KPlayer client, NPC npc, TaskCallBack x)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            Dictionary<int, string> Selections = new Dictionary<int, string>();
            // TODO CLICK VÀO CÁI GÌ

            LogManager.WriteLog(LogTypes.GuildWarManager, $"[{client.GuildID}][{client.GuildName}][{client.RoleName}]   CLICK TO :  {x.SelectID}");

            if (_kuaFuGuildWarLogic.TryHandleNpcSelection(_Config, TotalMapWillBeWar, client, npc, x, BATTLESTATE, ChangeState))
            {
                return;
            }

            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_REGISTER || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART)
            {
                // Tức là tuyên chiến
                if (x.SelectID < 1000)
                {
                    var find = _Config.WarMapConfigs.Where(g => g.MapID == x.SelectID).FirstOrDefault();
                    if (find != null)
                    {
                        _NpcDialog.Text += "Phí dụng tuyên chiến là 500 vạn bạc một lần.<br>";
                        _NpcDialog.Text = "Điều kiện tuyên chiến:<br>Bang hội phải đủ 3.000 uy danh.<br>Bang hội phải đủ 20 người.<br>Quỹ bang hội phải từ 1.000 vạn.<br><br>";
                        _NpcDialog.Text = "Bạn có chắc chắn muốn tuyên chiến với lãnh thổ :" + find.MapName;

                        int Count = CountRegisterFight(find.MapID);

                        if (Count > 0)
                        {
                            _NpcDialog.Text += "<br><br>Hiện bản đồ này được tuyển chiến bởi các Bang :<br><br>";

                            List<GuildMapRegisterFight> _RegíterFight = TotalMapRegisterFight.Where(g => g.MapId == x.SelectID).ToList();

                            foreach (GuildMapRegisterFight _Guild in _RegíterFight)
                            {
                                _NpcDialog.Text += "<b><color=red>" + _Guild.GuildName + "</color></b>";
                            }
                        }

                        Selections.Add(x.SelectID, "Ta Muốn Tuyên Chiến");

                        Selections.Add(1000, "Ta sẽ quay lại sau!");
                    }
                }
            }
            else if (BATTLESTATE == GUILDWARDSTATUS.STATUS_END)
            {
                //Click Nhận thưởng
                if (x.SelectID == 1)
                {
                    if (WarScore.TryGetValue(client.GuildID, out ConcurrentDictionary<int, GuildWarPlayer> _Out))
                    {
                        if (_Out != null)
                        {
                            _Out.TryGetValue(client.RoleID, out GuildWarPlayer FindExits);

                            //  var FindExits = _Out.Where(xx => xx._Player.RoleID == client.RoleID).FirstOrDefault();

                            if (FindExits != null)
                            {
                                if (client.GetTotalValue() / 10000 < 1000)
                                {
                                    _NpcDialog.Text += "Không đủ 1000 tài phú, không thể nhận thưởng!";
                                }
                                else if (Global.GetMantleLevel(client) < ServerConfig.Instance.MantleLevelForGuildWar)
                                {
                                    _NpcDialog.Text += $"Cần đeo phi phong {KTLuaLib_System.GetMantleNameFromLevel(ServerConfig.Instance.MantleLevelForGuildWar)} trở lên mới có thể nhận thưởng!";
                                }
                                else if (!FindExits.IsReviceReward)
                                {
                                    


                                    bool isWin = false;
                                    if (FinalResult.TryGetValue(client.GuildID, out List<FightResult> result))
                                    {
                                        if (result != null)
                                        {
                                            List<FightResult> ChiemDuoc = result.Where(y => y.Type == 0 || y.Type == 1).ToList();
                                            if (ChiemDuoc.Count > 0)
                                            {
                                                foreach (FightResult _LanhTho in ChiemDuoc)
                                                {
                                                    isWin = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }


                                    int playScore = 0;
                                    if (_Out != null)
                                    {
                                        _Out.TryGetValue(client.RoleID, out GuildWarPlayer find);

                                        if (find != null)
                                        {
                                            playScore = find.Score;
                                        }
                                    }

                                    int SelectIndex = _Out.Values.OrderByDescending(y => y.Score).ToList().FindIndex(y => y._Player.RoleID == client.RoleID) + 1;

                                    int BoxCanEarn = 0;
                                    var configPointAward = _Config.Award.FindAll(y => y.Point < playScore && y.type == "Point").OrderByDescending(y => y.Point).FirstOrDefault();
                                    if (configPointAward != null)
                                    {
                                        BoxCanEarn = configPointAward.BoxCount;
                                    }


                                    int FinalDanhVong = 0;
                                    int FinalExp = 0;
                                    int FinalBacKhoa = 0;
                                    int FinalUyDanh = 0;

                                    var configRankAwards = _Config.Award.FindAll(y => y.type == "rank");
                                    foreach (var rankAward in configRankAwards)
                                    {
                                        string strRank = rankAward.Rank;
                                        string[] ranks = strRank.Split(':');
                                        if (ranks.Length == 1)
                                        {
                                            if (SelectIndex == Convert.ToInt32(ranks[0]))
                                            {
                                                if (playScore < rankAward.Point)
                                                {
                                                    //_NpcDialog.Text += $"Không đủ điều kiện nhận thưởng xếp hạng, cần tối thiểu {rankAward.Point} điểm công trạng cá nhân!<br>";
                                                }
                                                else
                                                {
                                                    FinalDanhVong = rankAward.DanhVongTDLT;
                                                    FinalExp = rankAward.Exp * client.m_Level;
                                                    FinalUyDanh = rankAward.Prestige;
                                                    FinalBacKhoa = rankAward.BacKhoa;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (SelectIndex >= Convert.ToInt32(ranks[0]) && SelectIndex <= Convert.ToInt32(ranks[1]))
                                            {
                                                if (playScore < rankAward.Point)
                                                {
                                                    //_NpcDialog.Text += $"Không đủ điều kiện nhận thưởng xếp hạng, cần tối thiểu {rankAward.Point} điểm công trạng cá nhân!<br>";
                                                }
                                                else
                                                {
                                                    FinalDanhVong = rankAward.DanhVongTDLT;
                                                    FinalExp = rankAward.Exp * client.m_Level;
                                                    FinalUyDanh = rankAward.Prestige;
                                                    FinalBacKhoa = rankAward.BacKhoa;
                                                }
                                            }
                                        }
                                    }



                                    if (!isWin)
                                    {
                                        BoxCanEarn = (int)Math.Ceiling((double)BoxCanEarn / 2);
                                        FinalDanhVong = (int)Math.Ceiling((double)FinalDanhVong / 2);
                                        FinalExp = (int)Math.Ceiling((double)FinalExp / 2);
                                        FinalUyDanh = (int)Math.Ceiling((double)FinalUyDanh / 2);
                                        FinalBacKhoa = (int)Math.Ceiling((double)FinalBacKhoa / 2);

                                        //_NpcDialog.Text += $"Bang hội đã thua trong lần tranh đoạt lãnh thổ lần này nên chỉ nhận được một nửa phần thưởng<br><br>";
                                    }


                                    if (KTGlobal.IsHaveSpace(BoxCanEarn, client))
                                    {
                                        string textLog = "";
                                        if (isWin)
                                        {
                                            textLog += $"[{client.RoleID}][{client.RoleName}][{client.GuildName}] Nhan thuong win";
                                            textLog += $"[cá nhân {BoxCanEarn} rương]";
                                            textLog += $"[xếp hạng {FinalExp} exp, {FinalBacKhoa} bạc khóa, {FinalUyDanh} ud, {FinalDanhVong} dv tdlt]";
                                        }
                                        else
                                        {
                                            textLog += $"[{client.RoleID}][{client.RoleName}][{client.GuildName}] Nhan thuong lose";
                                            textLog += $"[cá nhân {BoxCanEarn} rương]";
                                            textLog += $"[xếp hạng {FinalExp} exp, {FinalBacKhoa} bạc khóa, {FinalUyDanh} ud, {FinalDanhVong} dv tdlt]";
                                        }
                                        LogManager.WriteLog(LogTypes.GuildWarManager, textLog);

                                        FindExits.IsReviceReward = true;

                                        _NpcDialog.Text += "Nhận thưởng thành công!";

                                        int CAMP = 801;

                                        //exp
                                        GameManager.ClientMgr.ProcessRoleExperience(client, FinalExp, true, false, true, "TDLT");

                                        //danh vong tdlt
                                        KTGlobal.AddRepute(client, CAMP, FinalDanhVong);

                                        //bạc khóa
                                        KTGlobal.AddMoney(client, FinalBacKhoa, MoneyType.BacKhoa, "TDLT");

                                        //Update tiền cho bagn hội
                                        client.Prestige += FinalUyDanh;

                                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, 591, BoxCanEarn, 0, "TDLT", false, 1, false, Global.ConstGoodsEndTime))
                                        {
                                            PlayerManager.ShowNotification(client, "Có lỗi khi nhận vật phẩm chế tạo");
                                        }

                                        
                                    }
                                    else
                                    {
                                        _NpcDialog.Text += "Túi đồ của bạn không đủ chỗ trống!";
                                    }

                                }
                                else
                                {
                                    _NpcDialog.Text += "Bạn đã nhận thưởng rồi!";
                                }
                            }
                            else
                            {
                                _NpcDialog.Text += "Không tìm thấy người chơi";
                            }
                        }
                    }
                }
            }

            Action<TaskCallBack> ActionWork = (h) => DoConfirm(client, npc, h);

            _NpcDialog.OnSelect = ActionWork;

            _NpcDialog.Selections = Selections;

            _NpcDialog.Show(npc, client);
        }

        public void JoinKuaFuGuildWar(NPC npc, KPlayer client)
        {
            if (!IsKuaFuGuildWar)
            {
                NpcClick(npc, client);
                return;
            }

            _kuaFuGuildWarLogic.EnterWarMapFromNpc(_Config, TotalMapWillBeWar, client, npc, BATTLESTATE);
        }

        public void LeaveKuaFuGuildWar(KPlayer client)
        {
            if (!IsKuaFuGuildWar)
            {
                return;
            }

            _kuaFuGuildWarLogic.LeaveWarMapFromNpc(TotalMapWillBeWar, client, ChangeState);
        }

        public bool IsInKuaFuGuildWarMap(KPlayer client)
        {
            return IsKuaFuGuildWar && _kuaFuGuildWarLogic.IsInWarMap(client, TotalMapWillBeWar);
        }

        /// <summary>
        /// xác nhận cái việc chọn
        /// </summary>
        /// <param name="client"></param>
        /// <param name="npc"></param>
        /// <param name="h"></param>
        private void DoConfirm(KPlayer client, NPC npc, TaskCallBack h)
        {
            //Console.WriteLine(h.SelectID);

            KNPCDialog _NpcDialog = new KNPCDialog();

            // Nếu là muốn nghĩa lại
            if (h.SelectID == 1000)
            {
                KT_TCPHandler.CloseDialog(client);
            }
            else
            {
                if (BATTLESTATE == GUILDWARDSTATUS.STATUS_REGISTER || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART)
                {
                    int GuildId = client.GuildID;

                    MiniGuildInfo _GetInfo = GetMiniGuildInfo(GuildId);

                    // Mở lại đoạn này tránh spam bang hội
                   
                    if (_GetInfo != null)
                    {
                        if (GameManager.IsLiveServer)
                        {
                            if (_GetInfo.TotalPrestige < 3000)
                            {
                                _NpcDialog.Text = "Bang hội phải đủ 10.000 uy danh mới có thể tuyên chiến!";
                                _NpcDialog.Show(npc, client);

                                return;
                            }

                            if (_GetInfo.TotalMember < 20)
                            {
                                _NpcDialog.Text = "Bang hội phải đủ 30 người mới có thể tuyên chiến";
                                _NpcDialog.Show(npc, client);

                                return;
                            }

                            if (_GetInfo.MoneyStore < 10000000)
                            {
                                _NpcDialog.Text = "Quỹ bang hội phải từ 1000 vạn mới có thể tuyên chiến";
                                _NpcDialog.Show(npc, client);

                                return;
                            }
                        }


                        //var find = TotalMapRegisterFight.Where(x => x.GuildID == GuildId && x.MapId == h.SelectID).FirstOrDefault();
                        var find = TotalMapRegisterFight.Where(x => x.GuildID == GuildId).FirstOrDefault();
                        if (find != null)
                        {
                            //_NpcDialog.Text = "Bang hội của bạn đã tuyên chiến với lãnh thổ này rồi";
                            _NpcDialog.Text = $"Bang hội của bạn đã tuyên chiến với lãnh thổ [{KTGlobal.CreateStringByColor(find.MapName, ColorType.Green)}] rồi";
                            _NpcDialog.Show(npc, client);

                            return;
                        }
                        else
                        {
                            int moneyNeed = 5000000;
                            string CMDUPDATE = $"1:{client.GuildID}:{h.SelectID}:{moneyNeed}";

                            string[] Pram = Global.SendToDB((int)TCPGameServerCmds.CMD_KT_GUILD_SETCITY, CMDUPDATE, GameManager.LocalServerId);

                            int Status = Int32.Parse(Pram[0]);

                            if (Status > 0)
                            {
                               
                            }
                            else if (Status == -5)
                            {
                                _NpcDialog.Text = $"Quỹ bang không đủ {moneyNeed / 10000} vạn không thể khiêu chiến";
                                _NpcDialog.Show(npc, client);
                                return;
                            }
                            else if (Status == -2)
                            {
                                _NpcDialog.Text = "Bang hội không tồn tại";
                                _NpcDialog.Show(npc, client);
                                return;
                            }
                            
                            else
                            {
                                _NpcDialog.Text = "Có lỗi khi thanh toán chi phí khiêu chiến";
                                _NpcDialog.Show(npc, client);
                                return;
                            }

                            // Nếu bang này trước đó đã chiếm được 1 tân thủ thôn
                            TerritoryInfo territoryInfo = GetTerritoryInfo(client.GuildID);

                            bool IsAllTanThuThon = true;

                            if (territoryInfo != null)
                            {
                                if (territoryInfo.Territorys != null)
                                {
                                    foreach (Territory _Terriory in territoryInfo.Territorys)
                                    {
                                        //Nếu đây là tân thủ thôn
                                        if (!IsTanThuThon(_Terriory.MapID))
                                        {
                                            IsAllTanThuThon = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            // Nếu tất cả đều là tân thủ thôn thì được khiếu chiến 1 bản đồ bất kỳ
                            if (IsAllTanThuThon)
                            {
                                var FindRegister = TotalMapRegisterFight.Where(x => x.GuildID == GuildId).FirstOrDefault();

                                if (FindRegister != null)
                                {
                                    _NpcDialog.Text = "Trong lần tuyên chiến này bạn chỉ có thể tuyên chiến 1 lãnh thổ để thiết lập căn cứ thành chính không thể tuyên chiến thêm lãnh thổ khác nữa";
                                }
                                else
                                {
                                    OpenConfig _Find = _Config.WarMapConfigs.Where(x => x.MapID == h.SelectID).FirstOrDefault();
                                    if (_Find != null)
                                    {
                                        GuildMapRegisterFight _ActiveFight = new GuildMapRegisterFight();
                                        _ActiveFight.MapId = _Find.MapID;
                                        _ActiveFight.GuildName = client.GuildName;
                                        _ActiveFight.GuildID = client.GuildID;
                                        _ActiveFight.MapName = _Find.MapName;

                                        TotalMapRegisterFight.Add(_ActiveFight);

                                        _NpcDialog.Text = "Tuyên chiến thành công với lãnh thổ <b>[" + _Find.MapName + "]</b>";

                                        string MSG = KTGlobal.CreateStringByColor("Bang hội đã Tuyên Chiến Thành công với lãnh thổ [" + _Find.MapName + "] !", ColorType.Importal);

                                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.GuildID, MSG, null, "");
                                    }
                                }
                            }
                            else // Nếu không phải tức là cho nó khiêu chiến thỏa mái
                            {
                                OpenConfig _Find = _Config.WarMapConfigs.Where(x => x.MapID == h.SelectID).FirstOrDefault();
                                if (_Find != null)
                                {
                                    GuildMapRegisterFight _ActiveFight = new GuildMapRegisterFight();
                                    _ActiveFight.MapId = _Find.MapID;
                                    _ActiveFight.GuildName = client.GuildName;
                                    _ActiveFight.GuildID = client.GuildID;
                                    _ActiveFight.MapName = _Find.MapName;

                                    TotalMapRegisterFight.Add(_ActiveFight);

                                    _NpcDialog.Text = "Tuyên chiến thành công với lãnh thổ <b>[" + _Find.MapName + "]</b>";

                                    {
                                        string MSG = KTGlobal.CreateStringByColor("Bang hội đã Tuyên Chiến Thành công với lãnh thổ [" + _Find.MapName + "] !", ColorType.Importal);

                                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.GuildID, MSG, null, "");
                                    }

                                    {
                                        var miniMapInfo = _CaheMiniMap.Find(x => x.MapID == h.SelectID);
                                        if(miniMapInfo!= null)
                                        {
                                            string MSG = KTGlobal.CreateStringByColor("Bang hội [" + client.GuildName + "] đã Tuyên Chiến với lãnh thổ [" + _Find.MapName + "] !", ColorType.Importal);

                                            KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, miniMapInfo.GuildID, MSG, null, "");
                                        }
                                    }
                                }
                            }

                            _NpcDialog.Show(npc, client);
                        }
                    }
                }
            }
        }

        #endregion NpClick

        #region GMCommand

        /// <summary>
        /// Lệnh gm chạy để bắt đầu chiến đấu
        /// </summary>
        public void BattleForceStart()
        {
            LastTick = TimeUtil.NOW();

            if (IsKuaFuGuildWar)
            {
                BATTLESTATE = GUILDWARDSTATUS.STATUS_PREPARSTART;
                CreateAllActiveWarMap();
                SetupMiniMap();
                KTGlobal.SendSystemEventNotification("Tranh đoạt lãnh thổ liên server đã mở, hãy đến Quan Lãnh Thổ để vào Hoa Sơn chuẩn bị chiến đấu. Trong 10 phút chuẩn bị sẽ không thể PK!");
            }
            else
            {
                BATTLESTATE = GUILDWARDSTATUS.STATUS_REGISTER;
                KTGlobal.SendSystemEventNotification("Lãnh thổ chiến đang bước vào thời kỳ tuyên chiến, hãy đến Quan Lãnh Thổ các thành để xác định mục tiêu chinh chiến!");
            }

            LogManager.WriteLog(LogTypes.GuildWarManager, "Battle Change State ==> " + BATTLESTATE.ToString());
        }

        /// <summary>
        /// Hàm GM gọi end battle nếu mà toác thì nó vẫn ngon
        /// </summary>
        public void BattleForceEnd()
        {
            LastTick = TimeUtil.NOW();

            KTGlobal.SendSystemEventNotification(GetGuildWarEndNotification());

            // Chuyển sang trạng thái end
            BATTLESTATE = GUILDWARDSTATUS.STATUS_END;

            //Gọi update bảng xếp hạng lần cuối cùng
            UpdateGuildRanking();
            // Thiết lập lãnh thổ đọc ghi DB

            // Clear toàn bộ cột trụ quá , boss,
            // Unlock status cho toàn bộ thành viên tham gia
            ResetBattle();

            if (IsKuaFuGuildWar)
            {
                _kuaFuGuildWarLogic.SendRewards(_Config, WarScore, TotalScore);
                _kuaFuGuildWarLogic.MoveAllPlayersBackToCity(TotalMapWillBeWar, ChangeState);
            }
            else
            {
                SetupFinishTerritory();

                // Thực hiện ghi vào DB
                WriterToDatabase();

                //Reload lại toàn bộ thông tin lãnh thổ
                ReloadAllTerritoryInfo();
            }

            //todo : Làm thêm quả xếp hạng ở NPC khi nó ấn vào phần thưởng

            LogManager.WriteLog(LogTypes.GuildWarManager, " Battle Change State ==> " + BATTLESTATE.ToString());



            SetupMiniMap();
        }

        #endregion GMCommand

        #region Battle Controller

        public void SwitchOffStatus()
        {
            try
            {
                foreach (KeyValuePair<int, ConcurrentDictionary<int, GuildWarPlayer>> entry in WarScore)
                {
                    // Lấy ra danh sách người chơi
                    ConcurrentDictionary<int, GuildWarPlayer> TotalPlayer = entry.Value;

                    // Set lại state cho người chơi
                    foreach (GuildWarPlayer _client in TotalPlayer.Values)
                    {
                        this.ChangeState(_client._Player, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, ex.ToString());
            }
        }

        public void CreateMapEvent()
        {
            foreach (KeyValuePair<int, GuildWarMap> entry in TotalMapWillBeWar)
            {
                GuildWarMap MapEvent = entry.Value;

                // Tạo long trụ khi sự bắt đầu
                MapEvent.CreateBuildingWhenStart();

                //Acvie sang trạng thái chiến đấu cho người chơi
                MapEvent.ActiveAllStatusAndPKModelInMap();
            }
        }

        /// <summary>
        /// Spam camp cho người chơi
        /// </summary>
        public void SpamCamp()
        {
            LogManager.WriteLog(LogTypes.GuildWarManager, "CALL SPAM CAMP ");

            try
            {
                foreach (KeyValuePair<int, GuildWarMap> entry in TotalMapWillBeWar)
                {
                    GuildWarMap MapEvent = entry.Value;

                    //Acvie sang trạng thái chiến đấu cho người chơi
                    MapEvent.ActiveAllStatusAndPKModelInMap();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }

        public void ResetBattle()
        {
            ResetMap();
            SwitchOffStatus();
        }

        /// <summary>
        /// Setup lại lãnh thổ sau khi kết túc
        /// </summary>
        public void SetupFinishTerritory()
        {
            var curDay = DateTime.Now.DayOfWeek;
            // Duyệt ra tất cả các lãnh thổ
            foreach (KeyValuePair<int, GuildWarMap> entry in TotalMapWillBeWar)
            {
                GuildWarMap MapEvent = entry.Value;
                int MAPID = entry.Key;
                
                //Lấy ra thông tin bản đồ
                var find = _Config.WarMapConfigs.Where(x => x.ReadMapID == MAPID).FirstOrDefault();
                //  Tìm ra cái bản đồ này
                if (find != null)
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, $"[Log info][{curDay}][{MapEvent.BeLongGuild}] ==> MAPNAME : [{find.MapName}][{find.MapID}] is tan thu thon: {IsTanThuThon(find.MapID)}");
                    //Lấy ra thực thể quản lý điểm ở cái bản đồ này
                    TotalScore.TryGetValue(MAPID, out List<GuildTotalScore> _OutList);

                    //Nếu như có dánh sách điểm
                    if (_OutList != null)
                    {
                        // Ghi lại logs điểm các bên cho đỡ cãi
                        foreach (GuildTotalScore _GuildLogs in _OutList)
                        {
                            LogManager.WriteLog(LogTypes.GuildWarManager, "[" + _GuildLogs.GuildName + "][" + _GuildLogs.GuildID + "] ==> MAPNAME : [" + find.MapName + "] |  SCORE :" + _GuildLogs.Score);
                        }
                        // Lấy ra  thằng có điểm cao nhất
                        var FindTopGuild = _OutList.OrderByDescending(x => x.Score).FirstOrDefault();
                        LogManager.WriteLog(LogTypes.GuildWarManager, "[TOP ON THIS MAP][" + FindTopGuild.GuildName + "][" + FindTopGuild.GuildID + "] ==> MAPNAME : [" + find.MapName + "] |  SCORE :" + FindTopGuild.Score + " | chủ cũ: " + MapEvent.BeLongGuild + "_" + MapEvent.BeLongGuildName);

                        if (FindTopGuild != null)
                        {
                            //Lấy ra danh sách điểm cuối cùng của cái bang này xem bản đồ nào có kết quả thế nào
                            if (FinalResult.TryGetValue(FindTopGuild.GuildID, out List<FightResult> TotalMap))
                            {
                                FightResult _Rep = new FightResult();
                                _Rep.MapID = find.MapID;
                                _Rep.MapName = find.MapName;
                                _Rep.IsFightMap = find.IsFightMapID;

                                // Nếu map này trước đó đã có chủ
                                if (MapEvent.BeLongGuild != -1)
                                {
                                    // Gán lại chủ cũ vào kết quả
                                    _Rep.OldGuildID = MapEvent.BeLongGuild;

                                    if (MapEvent.BeLongGuild == FindTopGuild.GuildID)
                                    {
                                        if (curDay == DayOfWeek.Saturday && IsTanThuThon(find.MapID))
                                        {
                                            string Notify = "Lãnh thổ bị thu hồi sau 1 tuần vào tối thứ 7:<b>" + KTGlobal.CreateStringByColor(find.MapName, ColorType.Importal) + "</b>";

                                            KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapEvent.BeLongGuild, Notify, null, "");


                                            LogManager.WriteLog(LogTypes.GuildWarManager, "[Thu hồi map vào mỗi thứ 7][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                            _Rep.NewGuildID = FindTopGuild.GuildID;
                                            _Rep.NewGuildName = FindTopGuild.GuildName;
                                            _Rep.Type = 3;

                                            TotalMap.Add(_Rep);
                                            continue;
                                        }

                                        if ( TotalMapRegisterFight.FindIndex(x => x.MapId == find.MapID) == -1)//thủ hệ thống 
                                        {
                                            if( TotalMapWillBeWar.TryGetValue(find.MapID, out GuildWarMap outMap))
                                            {
                                                if (!outMap.isKillBoss) // thủ hệ thống mà ko giết đc boss nguyên soái công thành -> mất map
                                                {
                                                    LogManager.WriteLog(LogTypes.GuildWarManager, "[thủ hệ thống mà ko giết đc boss nguyên soái công thành][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                                    _Rep.NewGuildID = FindTopGuild.GuildID;
                                                    _Rep.NewGuildName = FindTopGuild.GuildName;
                                                    _Rep.Type = 3;

                                                    TotalMap.Add(_Rep);
                                                    continue;
                                                }
                                            }
                                        }
                                        
                                        if (MapEvent.CountGuildObjective(FindTopGuild.GuildID) == 0)// nếu map của mình, mà ko giữ đc trụ nào -> hệ thống thu hồi
                                        {
                                            LogManager.WriteLog(LogTypes.GuildWarManager, "[có tham chiến ở lãnh thổ của mình,nhưng bị npc hệ thống chiếm hết long trụ][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                            _Rep.NewGuildID = FindTopGuild.GuildID;
                                            _Rep.NewGuildName = FindTopGuild.GuildName;
                                            _Rep.Type = 3;
                                        }
                                        else
                                        {
                                            _Rep.NewGuildID = FindTopGuild.GuildID;
                                            _Rep.NewGuildName = FindTopGuild.GuildName;
                                            _Rep.Type = 1;
                                        }
                                    }
                                    else
                                    {
                                        //Nếu là bị chiếm thì ghi vào cái GUILD mới
                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;
                                        _Rep.Type = 0;

                                        if (FinalResult.TryGetValue(MapEvent.BeLongGuild, out List<FightResult> BelongGuildTotalMap))
                                        {
                                            FightResult _RepBelong = new FightResult();
                                            _RepBelong.MapID = find.MapID;
                                            _RepBelong.MapName = find.MapName;
                                            _RepBelong.Type = 2;
                                            _RepBelong.IsFightMap = find.IsFightMapID;
                                            _RepBelong.NewGuildID = FindTopGuild.GuildID;
                                            _RepBelong.NewGuildName = FindTopGuild.GuildName;
                                            _RepBelong.OldGuildID = MapEvent.BeLongGuild;
                                            BelongGuildTotalMap.Add(_RepBelong);
                                        }
                                        else
                                        {
                                            FightResult _RepBelong = new FightResult();
                                            _RepBelong.MapID = find.MapID;
                                            _RepBelong.MapName = find.MapName;
                                            _RepBelong.Type = 2;
                                            _RepBelong.IsFightMap = find.IsFightMapID;
                                            _RepBelong.NewGuildID = FindTopGuild.GuildID;
                                            _RepBelong.NewGuildName = FindTopGuild.GuildName;
                                            _RepBelong.OldGuildID = MapEvent.BeLongGuild;

                                            List<FightResult> NewBelongGuildTotalMap = new List<FightResult>();
                                            NewBelongGuildTotalMap.Add(_RepBelong);
                                            FinalResult.TryAdd(MapEvent.BeLongGuild, NewBelongGuildTotalMap);
                                        }
                                    }
                                }
                                else
                                {
                                    if (MapEvent.CountGuildObjective(FindTopGuild.GuildID) >= 3)
                                    {
                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;
                                        _Rep.OldGuildID = MapEvent.BeLongGuild;
                                        _Rep.Type = 0;
                                    }
                                    else
                                    {
                                        LogManager.WriteLog(LogTypes.GuildWarManager, "[CHIẾM LÃNH THỔ HỆ THỐNG THẤT BẠI DO KHÔNG CHIỂM ĐỦ 3 LONG TRỤ][" + FindTopGuild.GuildName + "][" + FindTopGuild.GuildID + "] ==> MAPNAME : [" + find.MapName + "] |  SCORE :" + FindTopGuild.Score);
                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;
                                        _Rep.OldGuildID = MapEvent.BeLongGuild;
                                        _Rep.Type = 3;
                                    }
                                }

                                TotalMap.Add(_Rep);
                            }
                            else
                            {
                                FightResult _Rep = new FightResult();
                                _Rep.MapID = find.MapID;
                                _Rep.MapName = find.MapName;
                                _Rep.IsFightMap = find.IsFightMapID;

                                if (MapEvent.BeLongGuild != -1)
                                {
                                    _Rep.OldGuildID = MapEvent.BeLongGuild;
                                    if (MapEvent.BeLongGuild == FindTopGuild.GuildID)
                                    {
                                        if (curDay == DayOfWeek.Saturday && IsTanThuThon(find.MapID))
                                        {
                                            string Notify = "Lãnh thổ bị thu hồi sau 1 tuần vào tối thứ 7:<b>" + KTGlobal.CreateStringByColor(find.MapName, ColorType.Importal) + "</b>";

                                            KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapEvent.BeLongGuild, Notify, null, "");


                                            LogManager.WriteLog(LogTypes.GuildWarManager, "[Thu hồi map vào mỗi thứ 7][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                            _Rep.NewGuildID = FindTopGuild.GuildID;
                                            _Rep.NewGuildName = FindTopGuild.GuildName;
                                            _Rep.Type = 3;

                                            List<FightResult> NewTotalMap1 = new List<FightResult>();
                                            NewTotalMap1.Add(_Rep);

                                            // Thực hiện adđ vào
                                            FinalResult.TryAdd(FindTopGuild.GuildID, NewTotalMap1);
                                            continue;
                                        }

                                        if (TotalMapRegisterFight.FindIndex(x => x.MapId == find.MapID) == -1)//thủ hệ thống 
                                        {
                                            if (TotalMapWillBeWar.TryGetValue(find.MapID, out GuildWarMap outMap))
                                            {
                                                if (!outMap.isKillBoss) // thủ hệ thống mà ko giết đc boss nguyên soái công thành -> mất map
                                                {
                                                    LogManager.WriteLog(LogTypes.GuildWarManager, "[thủ hệ thống mà ko giết đc boss nguyên soái công thành][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                                    _Rep.NewGuildID = FindTopGuild.GuildID;
                                                    _Rep.NewGuildName = FindTopGuild.GuildName;
                                                    _Rep.Type = 3;

                                                    List<FightResult> NewTotalMap1 = new List<FightResult>();
                                                    NewTotalMap1.Add(_Rep);

                                                    // Thực hiện adđ vào
                                                    FinalResult.TryAdd(FindTopGuild.GuildID, NewTotalMap1);
                                                    continue;
                                                }
                                            }
                                        }

                                        // nếu map của mình, mà ko giữ đc trụ nào -> hệ thống thu hồi
                                        if (MapEvent.CountGuildObjective(FindTopGuild.GuildID) == 0)
                                        {
                                            LogManager.WriteLog(LogTypes.GuildWarManager, "[có tham chiến ở lãnh thổ của mình,nhưng bị npc hệ thống chiếm hết long trụ][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                            _Rep.NewGuildID = FindTopGuild.GuildID;
                                            _Rep.NewGuildName = FindTopGuild.GuildName;
                                            _Rep.Type = 3;
                                        }
                                        else
                                        {
                                            _Rep.NewGuildID = FindTopGuild.GuildID;
                                            _Rep.NewGuildName = FindTopGuild.GuildName;
                                            _Rep.Type = 1;
                                        }
                                    }
                                    else
                                    {
                                        _Rep.Type = 0;
                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;

                                        if (FinalResult.TryGetValue(MapEvent.BeLongGuild, out List<FightResult> BelongGuildTotalMap))
                                        {
                                            FightResult _RepBelong = new FightResult();
                                            _RepBelong.MapID = find.MapID;
                                            _RepBelong.MapName = find.MapName;
                                            _RepBelong.Type = 2;
                                            _RepBelong.IsFightMap = find.IsFightMapID;
                                            _RepBelong.NewGuildID = FindTopGuild.GuildID;
                                            _RepBelong.NewGuildName = FindTopGuild.GuildName;
                                            _RepBelong.OldGuildID = MapEvent.BeLongGuild;
                                            BelongGuildTotalMap.Add(_RepBelong);
                                        }
                                        else
                                        {
                                            FightResult _RepBelong = new FightResult();
                                            _RepBelong.MapID = find.MapID;
                                            _RepBelong.MapName = find.MapName;
                                            _RepBelong.Type = 2;
                                            _RepBelong.IsFightMap = find.IsFightMapID;

                                            _RepBelong.NewGuildID = FindTopGuild.GuildID;
                                            _RepBelong.NewGuildName = FindTopGuild.GuildName;
                                            _RepBelong.OldGuildID = MapEvent.BeLongGuild;

                                            List<FightResult> NewBelongGuildTotalMap = new List<FightResult>();
                                            NewBelongGuildTotalMap.Add(_RepBelong);

                                            FinalResult.TryAdd(MapEvent.BeLongGuild, NewBelongGuildTotalMap);
                                        }
                                    }
                                }
                                else
                                {
                                    if (MapEvent.CountGuildObjective(FindTopGuild.GuildID) >= 3)
                                    {
                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;
                                        _Rep.OldGuildID = MapEvent.BeLongGuild;
                                        _Rep.Type = 0;
                                    }
                                    else
                                    {
                                        LogManager.WriteLog(LogTypes.GuildWarManager, "[CHIẾM LÃNH THỔ HỆ THỐNG THẤT BẠI DO KHÔNG CHIỂM ĐỦ 3 LONG TRỤ][" + FindTopGuild.GuildName + "][" + FindTopGuild.GuildID + "] ==> MAPNAME : [" + find.MapName + "] |  SCORE :" + FindTopGuild.Score);

                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;
                                        _Rep.OldGuildID = MapEvent.BeLongGuild;
                                        // Để mất
                                        _Rep.Type = 3;
                                    }
                                }

                                List<FightResult> NewTotalMap = new List<FightResult>();
                                NewTotalMap.Add(_Rep);

                                // Thực hiện adđ vào
                                FinalResult.TryAdd(FindTopGuild.GuildID, NewTotalMap);
                            }
                        }
                        else
                        {
                            // nếu ko có top -> map ko có ai đánh -> check xem quái có đánh chiếm hết trụ ko 
                            

                            FightResult _Rep = new FightResult();
                            _Rep.MapID = find.MapID;
                            _Rep.MapName = find.MapName;
                            _Rep.IsFightMap = find.IsFightMapID;
                            _Rep.OldGuildID = MapEvent.BeLongGuild; // map có chủ thì mới có trogn danh sách war

                            if (MapEvent.BeLongGuild != -1)
                            {
                                if (curDay == DayOfWeek.Friday && IsTanThuThon(find.MapID))
                                {
                                    string Notify = "Lãnh thổ bị thu hồi sau 1 tuần vào tối thứ 7:<b>" + KTGlobal.CreateStringByColor(find.MapName, ColorType.Importal) + "</b>";

                                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, MapEvent.BeLongGuild, Notify, null, "");


                                    LogManager.WriteLog(LogTypes.GuildWarManager, "[Thu hồi map vào mỗi thứ 7][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                    _Rep.NewGuildID = MapEvent.BeLongGuild;
                                    _Rep.NewGuildName = MapEvent.BeLongGuildName;
                                    _Rep.Type = 3;

                                    if (FinalResult.TryGetValue(FindTopGuild.GuildID, out List<FightResult> TotalMap1))
                                    {
                                        TotalMap1.Add(_Rep);
                                    }
                                    else
                                    {
                                        List<FightResult> NewTotalMap = new List<FightResult>();
                                        NewTotalMap.Add(_Rep);

                                        // Thực hiện adđ vào
                                        FinalResult.TryAdd(FindTopGuild.GuildID, NewTotalMap);
                                    }
                                    continue;
                                }
                            }

                            if (TotalMapRegisterFight.Find(x => x.MapId == find.MapID) == null)//thủ hệ thống 
                            {
                                if (TotalMapWillBeWar.TryGetValue(find.MapID, out GuildWarMap outMap))
                                {
                                    if (!outMap.isKillBoss) // thủ hệ thống mà ko giết đc boss nguyên soái công thành -> mất map
                                    {
                                        LogManager.WriteLog(LogTypes.GuildWarManager, "[thủ hệ thống mà ko giết đc boss nguyên soái công thành][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                        _Rep.NewGuildID = FindTopGuild.GuildID;
                                        _Rep.NewGuildName = FindTopGuild.GuildName;
                                        _Rep.Type = 3;

                                        if (FinalResult.TryGetValue(FindTopGuild.GuildID, out List<FightResult> TotalMap1))
                                        {
                                            TotalMap1.Add(_Rep);
                                        }
                                        else
                                        {
                                            List<FightResult> NewTotalMap = new List<FightResult>();
                                            NewTotalMap.Add(_Rep);

                                            // Thực hiện adđ vào
                                            FinalResult.TryAdd(FindTopGuild.GuildID, NewTotalMap);
                                        }
                                        continue;
                                    }
                                }
                            }

                            // nếu map của mình, mà ko giữ đc trụ nào -> hệ thống thu hồi
                            if (MapEvent.CountGuildObjective(MapEvent.BeLongGuild) == 0)
                            {
                                LogManager.WriteLog(LogTypes.GuildWarManager, "[Khoong tham chiến ở lãnh thổ của mình, bị npc hệ thống chiếm hết long trụ][" + MapEvent.BeLongGuildName + "][" + MapEvent.BeLongGuild + "] ==> MAPNAME : [" + find.MapName + "]");
                                _Rep.NewGuildID = FindTopGuild.GuildID;
                                _Rep.NewGuildName = FindTopGuild.GuildName;
                                _Rep.Type = 3;
                            }
                            else
                            {
                                _Rep.NewGuildID = FindTopGuild.GuildID;
                                _Rep.NewGuildName = FindTopGuild.GuildName;
                                _Rep.Type = 1;
                            }

                            if (FinalResult.TryGetValue(FindTopGuild.GuildID, out List<FightResult> TotalMap))
                            {
                                TotalMap.Add(_Rep);
                            }
                            else
                            {
                                List<FightResult> NewTotalMap = new List<FightResult>();
                                NewTotalMap.Add(_Rep);

                                // Thực hiện adđ vào
                                FinalResult.TryAdd(FindTopGuild.GuildID, NewTotalMap);
                            }
                        }
                    }

                }
            }

            //foreach (KeyValuePair<int, List<FightResult>> entry in FinalResult)
            //{
            //    int GUILDID = entry.Key;

            //    List<FightResult> _Total = entry.Value;

            //    //Nếu có 1 bản đồ không phải là lãnh thỏ
            //    var FindFist = _Total.Where(x => x.IsFightMap == false && x.Type == 0 && x.Type == 1).FirstOrDefault();

            //    if (FindFist != null)
            //    {
            //        var FindTanThuThon = _Total.Where(x => x.IsFightMap == true).ToList();

            //        foreach (FightResult _FightResult in FindTanThuThon)
            //        {
            //            //SET LẠI KIỂU HỆ THỐNG THU HỒI
            //            _FightResult.Type = 3;

            //            LogManager.WriteLog(LogTypes.GuildWarManager, "[" + _FightResult.MapName + "] WILL BE REMOVE :" + GUILDID);
            //        }

            //        FinalResult[GUILDID] = _Total;
            //    }

            //}
        }

        //Code gì đó để ghi vào DB
        public void WriterToDatabase()
        {
            foreach (KeyValuePair<int, List<FightResult>> entry in this.FinalResult)
            {
                int GUILDID = entry.Key;

                List<FightResult> _Total = entry.Value;

                //Lấy 1 bản đồ đã chiếm được
                var FindNormalMap = _Total.Where(x => x.IsFightMap == false && (x.Type == 0 || x.Type == 1)).FirstOrDefault();

                foreach (FightResult _Fight in _Total)
                {
                    int Type = 1;

                    //if (_Fight.Type == 2 || _Fight.Type == 3)
                    //{
                    //    Type = 0;
                    //}

                    var FindRedMap = _Config.WarMapConfigs.Where(x => x.MapID == _Fight.MapID).FirstOrDefault();

                    // Nếu kết quả trả về là 2 thì cần xác định 2 thứ
                    if (_Fight.Type == 2)
                    {
                        // tức là thằng mới này bị làm mất bản đồ này nên ko cần xử lý
                        if (_Fight.OldGuildID != _Fight.NewGuildID && _Fight.OldGuildID == GUILDID)
                        {
                            string Notify = "Lãnh thổ đã bị đánh mất:<b>" + KTGlobal.CreateStringByColor(FindRedMap.MapName, ColorType.Importal) + "</b>";

                            KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Fight.OldGuildID, Notify, null, "");

                            LogManager.WriteLog(LogTypes.GuildWarManager, "[" + GUILDID + "] Bản đồ này do bang để mất nên không cần chỉnh sửa DB  :" + FindRedMap.MapName);

                            continue;
                        }
                    }
                    if (_Fight.Type == 3)
                    {
                        string Notify = "Lãnh thổ đã bị đánh mất hoặc chưa chiếm thành công:<b>" + KTGlobal.CreateStringByColor(FindRedMap.MapName, ColorType.Importal) + "</b>";

                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Fight.OldGuildID, Notify, null, "");

                        //LogManager.WriteLog(LogTypes.GuildWarManager, "[" + GUILDID + "] Bản đồ này do bang để mất nên không cần chỉnh sửa DB  :" + FindRedMap.MapName);
                        Type = 0;
                    }

                    if (_Fight.Type == 0)
                    {
                        KTGlobal.SendSystemChat($"Bang hội [{_Fight.NewGuildName}] tại tranh hoạt động tranh đoạt lãnh thổ đã thành công chiếm được [{FindRedMap.MapName}]!");
                    }
                    else if (_Fight.Type == 1)
                    {
                        KTGlobal.SendSystemChat($"Bang hội [{_Fight.NewGuildName}] tại tranh hoạt động tranh đoạt lãnh thổ đã thành công bảo vệ được [{FindRedMap.MapName}]!");
                    }
                    //else if (_Fight.Type == 3)
                    //{
                    //    KTGlobal.SendSystemChat($"Bang hội [{_Fight.NewGuildName}] tại tranh hoạt động tranh đoạt lãnh thổ đã thành công bảo vệ được [{FindRedMap.MapName}]!");
                    //}

                    bool IsMustRemove = false;
                    // Nếu mà tìm được 1 bản đồ là lãnh thổ rồi mà thằng này còn có tân thủ thôn thì chắc chắn phải xóa
                    if (FindNormalMap != null)
                    {
                        LogManager.WriteLog(LogTypes.GuildWarManager, "[" + GUILDID + "] Tìm được bản đồ bình thường đã chiếm được là :" + FindNormalMap.MapName);
                        IsMustRemove = true;
                    }

                    if (FindRedMap != null)
                    {
                        // nếu đây là tân thủ thôn mà phải loại bỏ vì đã có lãnh thổ thì mới loại bỏ
                        if (FindRedMap.IsFightMapID && IsMustRemove)
                        {
                            LogManager.WriteLog(LogTypes.GuildWarManager, "[" + GUILDID + "]CHECK BẢN ĐỒ :" + FindRedMap.MapName + "========> CẦN PHẢI XÓA");

                            Type = 0;
                        }
                        //else
                        //{
                        //    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + GUILDID + "]CHECK BẢN ĐỒ :" + FindRedMap.MapName + "========> CẦN PHỈA XÓA :" + IsMustRemove.ToString()); ;
                        //}
                        var FindMapWar = _Config.WarMapConfigs.Where(x => x.MapID == _Fight.MapID).FirstOrDefault();
                        //UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, FindMapWar.Star, 0, GameManager.ServerLineID, 0, Type);

                        if (FindRedMap.IsFightMapID)
                        {
                            // Nếu đây là tân thủ thôn thì ko thể là thành chính
                            UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, FindMapWar.Star, 0, GameManager.ServerLineID, 0, Type);
                        }
                        else // Nếu đây là map giã ngoại mà lã lãnh thổ đầu tiên thì cho nó là thành chính
                        {
                            // Lấy ra danh sách lãnh thổ hiện tại của guild này
                            TerritoryInfo territoryInfo = GetTerritoryInfo(GUILDID);

                            if (territoryInfo != null)
                            {
                                if (territoryInfo.Territorys != null)
                                {

                                    var curTerritory = territoryInfo.Territorys.Find(x => x.MapID == _Fight.MapID);
                                    if (curTerritory != null)
                                    {
                                        UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, FindMapWar.Star, curTerritory.Tax, GameManager.ServerLineID, curTerritory.IsMainCity, Type);
                                    }
                                    else
                                    {
                                        UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, FindMapWar.Star, 0, GameManager.ServerLineID, 0, Type);
                                    }
                                    //var findexits = territoryInfo.Territorys.Where(x => x.IsMainCity == 1).FirstOrDefault();

                                    //if (findexits != null)
                                    //{
                                    //    UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, 2, 2, GameManager.ServerLineID, 0, Type);
                                    //}
                                    //else
                                    //{
                                    //    UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, 2, 2, GameManager.ServerLineID, 1, Type);
                                    //}
                                }
                                else // NẾU CHƯA CÓ LÃNH THỔ NÀO THÌ ĐÂY CHẮC CHẮN PHẢI LÀ THÔN
                                {
                                    UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, FindMapWar.Star, 0, GameManager.ServerLineID, 0, Type);
                                }
                            }
                            else
                            {
                                UpdateTerritoryData(_Fight.MapID, _Fight.MapName, GUILDID, FindMapWar.Star, 0, GameManager.ServerLineID, 0, Type);
                            }
                        }
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.GuildWarManager, "TOAC  CANT FIND MAP ID :" + _Fight.MapID);
                    }
                }
            }
        }

        public void ResetMap()
        {
            try
            {
                foreach (KeyValuePair<int, GuildWarMap> entry in TotalMapWillBeWar)
                {
                    GuildWarMap MapEvent = entry.Value;
                    // Tạo long trụ khi sự bắt đầu
                    MapEvent.ClearAllMonster();
                    MapEvent.RemoveAllForbid();
                }
            }
            catch
            {
            }
        }

        public bool CanChangePKStatus(KPlayer client)
        {
            // nếu đang trong thời gian chiến đấu
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND || (IsKuaFuGuildWar && BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART))
            {
                int MapCode = client.CurrentMapCode;

                if (TotalMapWillBeWar.ContainsKey(MapCode))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        #endregion Battle Controller

        /// <summary>
        /// Battle Proseccsing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>

        private void ProsecBattle(object sender, EventArgs e)
        {
            /// Nếu mà ko bận thì gọi liên tục

            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
            else
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "Bussy EVENT worker!");
            }
        }

        #region Notify To Player

        public GuildWarReport GetReport(KPlayer client)
        {
            GuildWarReport _Report = new GuildWarReport();

            // Đây là điểm của bản thân mình

            // Tích lũy cá nhân

            //SET = 0 trước
            CurrentPoint _PontCaNhan = new CurrentPoint();
            _PontCaNhan.KillCount = 0;
            _PontCaNhan.Point = 0;
            _PontCaNhan.TowerDesotryCount = 0;

            List<GuildWarRanking> _GuildWarRanking = new List<GuildWarRanking>();

            int guildWarGuildId = GetGuildWarGuildId(client);

            ///Thử lấy ra
            if (WarScore.TryGetValue(guildWarGuildId, out ConcurrentDictionary<int, GuildWarPlayer> _Out))
            {
                if (_Out != null)
                {
                    //var find = _Out.Where(x => x._Player.RoleID == client.RoleID).FirstOrDefault();

                    _Out.TryGetValue(client.RoleID, out GuildWarPlayer find);

                    if (find != null)
                    {
                        _PontCaNhan.KillCount = find.KillCount;
                        _PontCaNhan.Point = find.Score;
                        _PontCaNhan.TowerDesotryCount = find.DestroyCount;

                        _Report._CurrentPoint = _PontCaNhan;
                    }
                }

                // TẠO RA CÁI TMP ĐỂ SẮP XẾP

                List<GuildWarPlayer> _TMP = _Out.Values.OrderByDescending(x => x.Score).ToList();

                int MAX = _TMP.Count;

                if (MAX > 20)
                {
                    MAX = 20;
                }
                // Đây alf điểm của 20 thằng trong bang
                for (int i = 0; i < MAX; i++)
                {
                    GuildWarPlayer _TmpPlayer = _TMP[i];
                    GuildWarRanking _Rank = new GuildWarRanking();
                    _Rank.Point = _TmpPlayer.Score;
                    _Rank.RoleName = _TmpPlayer._Player.RoleName;
                    _GuildWarRanking.Add(_Rank);
                }
            }

            _Report._GuildWarRanking = _GuildWarRanking;
            // Tới phần khó rồi lấy ra các lãnh thổ bang mình đang có và đứng hạng mấy
            List<TerritoryReport> _TerritoryReport = new List<TerritoryReport>();

            TotalReportUpdate.TryGetValue(guildWarGuildId, out _TerritoryReport);

            _Report._TerritoryReport = _TerritoryReport;

            return _Report;
        }

        public bool CanRespwan(KPlayer client)
        {
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
            {
                if (TotalMapWillBeWar.TryGetValue(client.CurrentMapCode, out GuildWarMap _OutMap))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanBypassGuildForKuaFuGuildWarTest(KPlayer client)
        {
            return _kuaFuGuildWarLogic.CanBypassGuildForTest(client);
        }

        public int GetGuildWarGuildId(KPlayer client)
        {
            return _kuaFuGuildWarLogic.GetGuildWarGuildId(client);
        }

        public string GetGuildWarGuildName(KPlayer client)
        {
            return _kuaFuGuildWarLogic.GetGuildWarGuildName(client);
        }

        private void RegisterGuildWarPlayer(KPlayer client)
        {
            _kuaFuGuildWarLogic.RegisterPlayer(client, TotalGuildInfo, WarScore);
        }

        private long GetNextScoreReachedOrder()
        {
            return Interlocked.Increment(ref _scoreReachedOrder);
        }

        /// <summary>
        /// Xử lý sự kiện khi người chơi vào bản đồ này ĐM phần này toác cái cứt j đó ==> tới việc lỗi camp
        /// </summary>
        /// <param name="client"></param>
        public void OnEnterMap(KPlayer client)
        {
            // Nếu đang trong thời gian Battle xảy ra
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND || (IsKuaFuGuildWar && BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART))
            {
                try
                {
                    //   LogManager.WriteLog(LogTypes.GuildWarManager, "CHECK STATE PLAYER :" + client.RoleName + "| ROLEID " + client.RoleID + "| GUILDNAME :" + client.GuildName + "|MAPCODE :" + client.CurrentMapCode);

                    //Nếu như thằng này có bang hội | TODO sửa lại cấm bọn ko có bang hội cũng phải toạch khi vào bản đồ này
                    if (client.GuildID > 0 || CanBypassGuildForKuaFuGuildWarTest(client))
                    {
                        // Nếu bản đồ này đang nằm trong danh sách đánh
                        if (TotalMapWillBeWar.TryGetValue(client.CurrentMapCode, out GuildWarMap _OutValue))
                        {
                            if (_OutValue != null)
                            {
                                int guildWarGuildId = GetGuildWarGuildId(client);
                                if (IsKuaFuGuildWar && !_OutValue.TotalGuildCanJoinBattle.Contains(guildWarGuildId))
                                {
                                    _OutValue.TotalGuildCanJoinBattle.Add(guildWarGuildId);
                                }

                                //  LogManager.WriteLog(LogTypes.GuildWarManager, "BẢN ĐỒ CÓ NẰM TRONG KHU VỰC TRANH ĐOẠT :" + client.RoleName + "| ROLEID " + client.RoleID + "| GUILDNAME :" + client.GuildName + "|MAPCODE :" + client.CurrentMapCode);
                                // Nếu bang của bạn có xảy ra chiến sự với bang này thì thực hiện chuyển PK sang bang hội
                                if (_OutValue.TotalGuildCanJoinBattle.Contains(guildWarGuildId))
                                {
                                    //  LogManager.WriteLog(LogTypes.GuildWarManager, "NGƯỜI CHƠI NÀY ĐÃ THAM GIA VÀO KHU VỰC TRANH ĐOẠT CỦA BANG :" + client.RoleName + "| ROLEID " + client.RoleID + "| GUILDNAME :" + client.GuildName + "|MAPCODE :" + client.CurrentMapCode);

                                    // Chuyển sang STATE 1 Để hiện bảng
                                    this.ChangeState(client, 1);
                                    // Tự động chuyển sang chế độ PK Bang hội
                                    client.PKMode = (IsKuaFuGuildWar && BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPARSTART) ? (int)PKMode.Peace : (int)PKMode.Custom;
                                    // SET camp cho thằng này về bang hội
                                    client.Camp = guildWarGuildId;
                                    client.ForbidUsingSkill = false;

                                    GlobalEventSource.getInstance().fireEvent(new TranhDoatLanhThoEventObject(client));
                                    // client.StopAllActiveFights();
                                    //SEND NOTIFY cho client

                                    RegisterGuildWarPlayer(client);
                                }
                                else // NẾu ko phỉa người có thể tham gia chiến trận
                                {
                                    //  LogManager.WriteLog(LogTypes.GuildWarManager, "NGƯỜI CHƠI NÀY KHÔNG NẰM TRONG KHU VỰC MAP TRANH ĐOẠT :" + client.RoleName + "| ROLEID " + client.RoleID + "| GUILDNAME :" + client.GuildName + "|MAPCODE :" + client.CurrentMapCode);
                                    // Nhét silient vào mồm
                                    client.ForbidUsingSkill = true;
                                    client.Camp = -1;
                                    client.TempTitle = "";
                                    client.PKMode = (int)PKMode.Peace;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Nếu bản đồ này đang nằm trong danh sách đánh
                        if (TotalMapWillBeWar.TryGetValue(client.CurrentMapCode, out GuildWarMap _OutValue))
                        {
                            client.ForbidUsingSkill = true;
                            client.Camp = -1;
                            client.TempTitle = "";
                            client.PKMode = (int)PKMode.Peace;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.GuildWarManager, "[" + client.RoleName + "]OnEnterMap BUG :" + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Kích hoạt sự kiện khi người chơi rời bản đồ
        /// </summary>
        /// <param name="client"></param>
        public void OnLeaverMap(KPlayer client, GameMap toMap)
        {
            // Nếu đang trong thời gian Battle xảy ra
            if (BATTLESTATE <= GUILDWARDSTATUS.STATUS_CLEAR)
            {
                //Khả năng sẽ spam nếu cần

                // Nếu thằng này đang trong bản đồ tranh đoạt

                TotalMapWillBeWar.TryGetValue(client.CurrentMapCode, out GuildWarMap _MapOutValue);
                if (_MapOutValue != null)
                {
                    // mà thằng này cso bang
                    if (client.GuildID > 0 || CanBypassGuildForKuaFuGuildWarTest(client))
                    {
                        // Nếu như bản đồ muốn tới lại ko phải bản đồ tranh đoạt thì trả lại camp bình thường cho nó
                        if (!TotalMapWillBeWar.TryGetValue(toMap.MapCode, out GuildWarMap _OutValue))
                        {
                            client.PKMode = (int)PKMode.Peace;
                            // Mở khóa nếu là người chơi thường
                            client.ForbidUsingSkill = false;
                            this.ChangeState(client, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cập nhật điểm mỗi 5 giây
        /// </summary>
        public void NotifyToAllEvery5Secon()
        {
            try
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "DO NotifyToAllEvery5Secon");

                // lấy ra tộc
                foreach (KeyValuePair<int, ConcurrentDictionary<int, GuildWarPlayer>> entry in WarScore)
                {
                    /// Lấy ra  danh sách player
                    ConcurrentDictionary<int, GuildWarPlayer> TotalPlayer = entry.Value;

                    // send notify loop cho toàn bộ ae
                    foreach (GuildWarPlayer _Notify in TotalPlayer.Values)
                    {
                        SendNotifyLoop(_Notify);
                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "DO NotifyToAllEvery5Secon END");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }

        public void SendNotifyLoop(GuildWarPlayer player)
        {
            long SEC = (LastTick + _Config.Activity.WAR_DULATION * 1000) - TimeUtil.NOW();

            int FinalSec = (int)(SEC / 1000);

            G2C_EventNotification _Notify = new G2C_EventNotification();

            _Notify.EventName = "Tranh đoạt lãnh thổ";

            _Notify.ShortDetail = "TIME|" + FinalSec;

            _Notify.TotalInfo = new List<string>();

            _Notify.TotalInfo.Add("Tích lũy cá nhân :" + player.Score);

            _Notify.TotalInfo.Add("Phá long trụ :" + player.DestroyCount);

            _Notify.TotalInfo.Add("Giết địch :" + player.KillCount);

            //  _Notify.TotalInfo.Add("Liên trảm :" + player.MaxKillSteak);

            if (player.CurentRank != -1)
            {
                _Notify.TotalInfo.Add("Hạng Hiện Tại :" + player.CurentRank);
            }
            else
            {
                _Notify.TotalInfo.Add("Hạng Hiện Tại : Chưa xếp hạng");
            }

            if (player._Player.IsOnline())
            {
                player._Player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
            else
            {
                //Console.WriteLine("OFFLINE");
            }
        }

        #endregion Notify To Player

        /// <summary>
        /// Đổi state sang 50 có phím bấm Công báo ở góc và thời gian còn lại
        /// </summary>
        /// <param name="Player"></param>
        public void ChangeState(KPlayer Player, int State)
        {
            G2C_EventState _State = new G2C_EventState();

            _State.EventID = 50;
            _State.State = State;
            if (Player.IsOnline())
            {
                Player.SendPacket<G2C_EventState>((int)TCPGameServerCmds.CMD_KT_EVENT_STATE, _State);
            }
            else
            {
                //Console.WriteLine("OFFLINE");
            }
        }

        public void SetupMainCity(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            Dictionary<int, string> Selections = new Dictionary<int, string>();

            TerritoryInfo territoryInfo = GetTerritoryInfo(client.GuildID);
            int nMoneyNeed = 0;
            if (client.GuildID > 0)
            {
                // _NpcDialog.Text = "Có thể tấn công lãnh thổ lân cận đã bị chiếm lĩnh.Bang của bạn có thể tuyên chiến với các lãnh thổ sau:";

                if (client.GuildRank == (int)GuildRank.Master)
                {

                    if (territoryInfo.Territorys != null)
                    {
                        var findMainCity = territoryInfo.Territorys.Where(x => x.IsMainCity == 1).FirstOrDefault();

                        int nTimeSetupMainCity = 0;
                        // Chắc chắn là có thành chính thì mới có liền kề
                        if (findMainCity != null)
                        {
                            _NpcDialog.Text += "<b>Thành chính của bang hội bạn là  <color=green>" + findMainCity.MapName + "</color>.</b><br>Chọn lãnh thổ muốn đổi làm thành chính:<br><br>";
                            nTimeSetupMainCity = GetTimeSetMainCity(client.GuildID);

                            nMoneyNeed = this.getMoneyNeedToSetupMainCity(nTimeSetupMainCity);
                            _NpcDialog.Text += $"Bang hội của bạn đã thiết lập thành chính <color=yellow>{nTimeSetupMainCity}</color> lần, để thiết lập thành chính lần này cần {nMoneyNeed / 10000}vạn quỹ bang.";
                        }
                        else
                        {
                            _NpcDialog.Text += "<b>Bang hội của bạn chưa có thành chính</b><br>Chọn lãnh thổ muốn thiết lập làm thành chính:<br><br>";
                            nTimeSetupMainCity = 0;
                            nMoneyNeed = this.getMoneyNeedToSetupMainCity(nTimeSetupMainCity);
                            _NpcDialog.Text += $"Bang hội của bạn đã thiết lập thành chính <color=yellow>{nTimeSetupMainCity}</color> lần, để thiết lập thành chính lần này cần {nMoneyNeed / 10000}vạn quỹ bang.";
                        }

                        // Duyệt lại toàn bộ các lãnh thổ

                        foreach (Territory _AreadyFix in territoryInfo.Territorys)
                        {
                            // Chắc chắn bản đồ này phỉa không có tân thủ thôn
                            if (!IsTanThuThon(_AreadyFix.MapID))
                            {
                                if (_AreadyFix.IsMainCity == 0)
                                {
                                    Selections.Add(_AreadyFix.MapID, _AreadyFix.MapName);
                                }
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.GuildWarManager, "[" + client.GuildName + "] Kiểm tra bản đồ :" + _AreadyFix.MapID + "==> Là tân thủ thôn nên bỏ qua");
                            }
                        }
                    }
                    else
                    {
                        _NpcDialog.Text += "<b>Vì Bang Hội của bạn chưa chiếm được 1 lãnh thổ nào.Nên bang hội của bạn không thể thiết lập thành chính</b>";
                        Selections.Add(1000000, "Kết thúc đối thoại");
                    }
                }
                else
                {
                    _NpcDialog.Text = "Hãy bảo bang chủ của ngươi tới gặp ta!";
                    Selections.Add(1000000, "Kết thúc đối thoại");
                }
            }
            else
            {
                _NpcDialog.Text = "Hãy gia nhập một bang hội rồi hãy quay lại gặp ta!";
                Selections.Add(1000000, "Kết thúc đối thoại");
            }

            _NpcDialog.OnSelect = (data) =>
            {


                if (data.SelectID < 1000000)
                {
                    string CMDUPDATE = $"0:{client.GuildID}:{data.SelectID}:{nMoneyNeed}";

                    string[] Pram = Global.SendToDB((int)TCPGameServerCmds.CMD_KT_GUILD_SETCITY, CMDUPDATE, GameManager.LocalServerId);

                    int Status = Int32.Parse(Pram[0]);

                    if (Status > 0)
                    {
                        _NpcDialog.Text = "Thiết lập thành chính thành công!";
                        this.SetupMiniMap();
                    }
                    else if (Status == -3)
                    {
                        _NpcDialog.Text = $"Quỹ bang không đủ {nMoneyNeed / 10000}vạn không thể thiết lập thành chính";
                    }
                    else if (Status == -1)
                    {
                        _NpcDialog.Text = "Bang hội không tồn tại";
                    }
                    else if (Status == -2)
                    {
                        _NpcDialog.Text = "Bản đồ lãnh thổ không tồn tại";
                    }
                    else
                    {
                        _NpcDialog.Text = "Có lỗi khi thiết lập thành chính";
                    }



                    _NpcDialog.Selections = new Dictionary<int, string>()
                                {
                                    { 1000000, "Kết thúc đối thoại" }
                                };
                    _NpcDialog.ShowDialog(client);
                }


                /// Nếu đáp án chính xác
                else if (data.SelectID == 1000000)
                {
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(_NpcDialog);
                }

            };
            /// Hiện khung
            _NpcDialog.Selections = Selections;
            _NpcDialog.ShowDialog(client);

        }

        private int getMoneyNeedToSetupMainCity(int nTimeSetup)
        {
            int[] nValues = { 2000000, 3000000, 5000000 };
            if (nTimeSetup >= nValues.Length)
            {
                return nValues[nValues.Length - 1];
            }
            else
            {
                return nValues[nTimeSetup];
            }
        }

        public void UpgradeGuildOfficeRank(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            if (_kuaFuGuildWarLogic.TryShowOfficeRankNotice(npc, client))
            {
                return;
            }

            _NpcDialog.Text += "Sau khi xin tăng cấp Quan hàm bang hội, bắt đầu từ thứ 2 tuần sau, thủ lĩnh và thành viên cổ đông sẽ nhận được Quan hàm cao hơn.";
            Dictionary<int, string> Selections = new Dictionary<int, string>();

            TerritoryInfo territoryInfo = GetTerritoryInfo(client.GuildID);
            int nMoneyNeed = 0;
            if (territoryInfo != null)
            {
                nMoneyNeed = territoryInfo.MoneyNeedToUpgradeOfficeRank;
                if (client.GuildID > 0)
                {
                    // _NpcDialog.Text = "Có thể tấn công lãnh thổ lân cận đã bị chiếm lĩnh.Bang của bạn có thể tuyên chiến với các lãnh thổ sau:";

                    //if ((territoryInfo.GuildOfficeRank == 0 && client.GuildRank == (int)GuildRank.Master) || (client.OfficeRank == territoryInfo.GuildOfficeRank + 2))
                    if (client.GuildRank == (int)GuildRank.Master)
                    {
                        if (territoryInfo.CountTerritoryNeedToUpgradeOfficeRank == -1)
                        {
                            _NpcDialog.Text += $"Bang hội của ngươi đã tăng đến cấp  <color=yellow>{territoryInfo.GuildOfficeRankNextWeek}</color>, đã là cấp tối đa ở phiên bản hiện tại, không thể tiếp tục tăng cấp<br>";
                            Selections = new Dictionary<int, string>()
                        {
                            { 1000000, "Kết thúc đối thoại" }
                        };
                        }
                        else
                        {
                            int territoryCount = 0;
                            if (territoryInfo.Territorys != null)
                            {
                                territoryCount = territoryInfo.Territorys.Count;
                            }

                            if (territoryInfo.GuildOfficeRankNextWeek == 0)
                            {
                                _NpcDialog.Text += $"Bang hội của ngươi muốn tăng dến cấp <color=yellow>1</color> cần:<br>1. Chiếm lĩnh  <color=yellow>1</color> lãnh thổ<br>2.Phí tăng cấp cần  <color=yellow>{nMoneyNeed / 10000} vạn</color> quỹ bang<br><br> Ngươi có muốn tăng đến cấp kế tiếp?";
                            }
                            else
                            {
                                _NpcDialog.Text += $"Bang hội của ngươi đã tăng đến cấp  <color=yellow>{territoryInfo.GuildOfficeRankNextWeek}</color>, muốn tăng dến cấp  <color=yellow>{territoryInfo.GuildOfficeRankNextWeek + 1}</color> cần:<br>1. Chiếm lĩnh  <color=yellow>{territoryInfo.CountTerritoryNeedToUpgradeOfficeRank}</color> lãnh thổ<br>2.Phí tăng cấp cần  <color=yellow>{nMoneyNeed / 10000} vạn</color> quỹ bang<br><br> Ngươi có muốn tăng đến cấp kế tiếp?";
                            }
                            Selections = new Dictionary<int, string>()
                        {
                            { 1, "Đồng ý"},
                            { 1000000, "Để ta suy nghĩ đã" }
                        };
                        }
                    }
                    else
                    {
                        //_NpcDialog.Text = "Hãy bảo Thủ lĩnh của ngươi tới gặp ta!";
                        _NpcDialog.Text = "Hãy bảo Bang chủ của ngươi tới gặp ta!";
                        Selections.Add(1000000, "Kết thúc đối thoại");
                    }
                }
                else
                {
                    _NpcDialog.Text = "Hãy gia nhập một bang hội rồi hãy quay lại gặp ta!";
                    Selections.Add(1000000, "Kết thúc đối thoại");
                }
            }
            else
            {
                _NpcDialog.Text = "Có lỗi xảy ra, vui lòng liên hệ với quản trị viên!";
                Selections.Add(1000000, "Kết thúc đối thoại");
            }
            _NpcDialog.OnSelect = (data) =>
            {

                if (data.SelectID == 1)
                {
                    string CMDUPDATE = $"1:{client.GuildID}:{territoryInfo.GuildOfficeRankNextWeek + 1}:{nMoneyNeed}";

                    string[] Pram = Global.SendToDB((int)TCPGameServerCmds.CMD_KT_GUILD_UPGRADE_OFFICE_RANK, CMDUPDATE, GameManager.LocalServerId);

                    int Status = Int32.Parse(Pram[0]);

                    if (Status > 0)
                    {
                        _NpcDialog.Text = "Tăng cấp quan hàm thành công!";
                    }
                    else if (Status == -3)
                    {
                        _NpcDialog.Text = $"Quỹ bang không đủ {nMoneyNeed / 10000} vạn không thể thiết lập thành chính";
                    }
                    else if (Status == -1)
                    {
                        _NpcDialog.Text = "Bang hội không tồn tại";
                    }
                    else if (Status == -2)
                    {
                        _NpcDialog.Text = "Bang hội không đủ điều kiện tăng cấp quan hàm";
                    }
                    else
                    {
                        _NpcDialog.Text = "Có lỗi xảy ra khi tăng cấp quan hàm";
                    }



                    _NpcDialog.Selections = new Dictionary<int, string>()
                                {
                                    { 1000000, "Kết thúc đối thoại" }
                                };
                    _NpcDialog.ShowDialog(client);
                }


                /// Nếu đáp án chính xác
                else if (data.SelectID == 1000000)
                {
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(_NpcDialog);
                }

            };
            /// Hiện khung
            _NpcDialog.Selections = Selections;
            _NpcDialog.ShowDialog(client);

        }

        public bool checkMapAllowPk(int mapId)
        {
            if (BATTLESTATE == GUILDWARDSTATUS.STATUS_START || BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
            {
                if (this.TotalMapWillBeWar.TryGetValue(mapId, out GuildWarMap _OutValue))
                {
                    return true;
                }

            }
            return false;
        }

        public void ReceiveMunitionGuildWar(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            // chua tuyen chien la ko dc nhan quan nhu
            if (this.TotalMapRegisterFight.FindIndex(x => x.GuildID == client.GuildID)==-1)
            {
                _NpcDialog.Text = "Chỉ thành viên của bang đã tham gia tuyên chiến mới được nhận quân nhu";
            }
            else
            {
                // tuyen chien roi thi check xem da nhan quan nhu chua
                var valueDaily = client.GetValueOfDailyRecore(579);
                if(valueDaily == -1)
                {
                    // chua nhan thi cho nhan
                    if (KTGlobal.IsHaveSpace(2, client))
                    {

                        _NpcDialog.Text += "Nhận quân nhu thành công!";

                        string strEndTime = Global.ConstGoodsEndTime;
                        DateTime dt = DateTime.Now.AddMinutes(60);
                        strEndTime = dt.ToString("yyyy-MM-dd HH:mm:ss");

                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, 579, 1, 0, "TDLT", false, 1, false, strEndTime))
                        {
                            PlayerManager.ShowNotification(client, "Có lỗi khi nhận vật phẩm!");
                        }
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, 580, 1, 0, "TDLT", false, 1, false, strEndTime))
                        {
                            PlayerManager.ShowNotification(client, "Có lỗi khi nhận vật phẩm!");
                        }
                        client.SetValueOfDailyRecore(579, 1);
                    }
                    else
                    {
                        _NpcDialog.Text += "Túi đồ của bạn cần ít nhất 2 chỗ trống!";
                    }
                }
                else
                {
                    _NpcDialog.Text = "Hôm nay đã nhận quân nhu rồi!";
                }
            }

            _NpcDialog.Selections = new Dictionary<int, string>()
                                {
                                    { 1000000, "Kết thúc đối thoại" }
                                };

            _NpcDialog.OnSelect = (data) => {
                if (data.SelectID == 1000000)
                {
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(_NpcDialog);
                }
            };
            _NpcDialog.ShowDialog(client);
        }
        void logPlayerScoreWhenEnd()
        {
            foreach (var guildId in WarScore.Keys)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, $"[StartLogGuidPlayerScore][{guildId}]");
                if (WarScore.TryGetValue(guildId, out ConcurrentDictionary<int, GuildWarPlayer> _Out))
                {
                    foreach (var roleId in _Out.Keys)
                    {
                        if (_Out.TryGetValue(roleId, out GuildWarPlayer _playerWar))
                        {
                            LogManager.WriteLog(LogTypes.GuildWarManager, $"[{roleId}] [Score: {_playerWar.Score}]");
                        }
                    }
                }
            }
            
        }
    }
}
