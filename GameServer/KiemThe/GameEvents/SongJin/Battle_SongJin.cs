using GameServer.KiemThe.GameEvents.SongJin;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Logic.Manager.Battle
{
    /// <summary>
    /// Điều khiển toàn bộ tống kim
    /// </summary>
    public class Battle_SongJin : IEventListener
    {
        #region BattelDEF

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public BattelStatus BATTLESTATE = BattelStatus.STATUS_NULL;

        /// <summary>
        /// Config hoạt động
        /// </summary>
        ///

        public int BattleLevel { get; set; }

        public int TotalSongScore { get; set; }

        public int TotalJinScore { get; set; }

        public long LastTick { get; set; }

        public bool IsBossReswpan { get; set; }
        public long LastNofity { get; set; }

        private bool IsGuildEvent = false;

        public static string GuildEvent_SongGroup = "";
        public static string GuildEvent_JinGroup = "";

        public bool showdown()
        {
            GlobalEventSource.getInstance().removeListener((int)EventTypes.MonsterDead, this);

            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayerDead, this);

            GlobalEventSource.getInstance().removeListener((int)EventTypes.PlayEnterMap, this);

            return true;
        }

        public bool startup()
        {
            // Đăng ký event khi quái chết với hệ thống
            GlobalEventSource.getInstance().registerListener((int)EventTypes.MonsterDead, this);
            // Đăng ký event khi có thằng nào chết với hệ thống
            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayerDead, this);

            GlobalEventSource.getInstance().registerListener((int)EventTypes.PlayEnterMap, this);

            BATTLESTATE = BattelStatus.STATUS_NULL;

            return true;
        }

        /// <summary>
        /// Config hoạt động
        /// </summary>

        private BattleConfig _BattleConfig = new BattleConfig();

        /// <summary>
        /// Bảng xếp hạng send về client nếu có người request
        /// </summary>
        public SongJinBattleRankingInfo CurRanking = new SongJinBattleRankingInfo();

        /// <summary>
        ///  Bảng ranking update động suốt thời gian có kill
        /// </summary>

        public List<BattlePlayer> SongJinRankking = new List<BattlePlayer>();
        private Dictionary<int, int> _RankMap = new Dictionary<int, int>();
        /// <summary>
        /// Total Register Tống
        /// </summary>

        public ConcurrentDictionary<int, BattlePlayer> SongCampRegister = new ConcurrentDictionary<int, BattlePlayer>();

        /// <summary>
        /// Total Register Kim
        /// </summary>
        public ConcurrentDictionary<int, BattlePlayer> JinCampRegister = new ConcurrentDictionary<int, BattlePlayer>();

        #endregion BattelDEF

        public void Revice(KPlayer Player)
        {
            var Camp = Player.Camp;

            var _Find = _BattleConfig.Region.Where(x => x.RegionCamp == Camp).FirstOrDefault();
            if (_Find != null)
            {
                PlayerManager.Relive(Player, _BattleConfig.MapID, _Find.PosX, _Find.PosY, 100, 100, 100);
            }
        }

        #region Battel Register

        /// <summary>
        /// Đăng ký vào tống kim
        /// </summary>
        /// <param name="KPlayer"></param>
        /// <param name="Camp"></param>
        public int Register(KPlayer KPlayer, int Camp, int Level)
        {
            if (BATTLESTATE == BattelStatus.STATUS_PREPARE)
            {
                // IsGuildEvent = false;
                // GuildEvent_JinGroup = "";
                // GuildEvent_SongGroup = "";
                // var songIDGroup = "";
                // var jinIDGroup = "";
                var maxPlayerRegister = -1;
                if (_BattleConfig.DayOfMonth != null && _BattleConfig.DayOfMonth.Count > 0)
                {
                    var now = DateTime.Now;
                    // DayOfMonth dayOfMonth = _BattleConfig.DayOfMonth.Where(x => x.Day == now.Day).FirstOrDefault();
                    foreach (var config in _BattleConfig.DayOfMonth)
                    {
                        if (config.Day != now.Day) 
                            continue;
                        // tính số phút tính từ nửa đêm để so sánh thời gian
                        var realtimeMinute = now.Hour * 60 + now.Minute;
                        var configMinute = config.Hour * 60 + config.Minute;
                        var registerTime = configMinute - _BattleConfig.RegisterDualtion / 60;
                        if (realtimeMinute >= registerTime && realtimeMinute <= configMinute)
                        {
                            GuildEvent_SongGroup = config.SongIDGroup;
                            GuildEvent_JinGroup = config.JinIDGroup;
                            maxPlayerRegister = config.MaxPlayers;
                            IsGuildEvent = true;
                        }
                    }
                }
                

                // Nếu Camp = 10 thì là đăng ký bên tống
                if (Camp == 10)
                { 
                    if (GuildEvent_SongGroup != "")
                    {
                        if (KPlayer.GuildName != GuildEvent_SongGroup)
                        {
                            return -10;
                        }    

                        if (SongCampRegister.Count >= maxPlayerRegister && maxPlayerRegister != -1)
                        {
                            return -30;
                        }

                        if (!SongCampRegister.ContainsKey(KPlayer.RoleID))
                        {
                            var _Battle = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 10, 0);

                            SongCampRegister.TryAdd(KPlayer.RoleID, _Battle);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        }
                        else
                        {
                            if (SongCampRegister.TryGetValue(KPlayer.RoleID, out var _Battle))
                            {
                                _Battle.Player = KPlayer;

                                this.MovePlayerToArena(KPlayer, Camp);                                
                                return 0;
                            }
                        }
                    }
                    else
                    {
                        // Nếu số lượng lệch quá 3 người thì thông báo là ko được
                        if (SongCampRegister.Count - JinCampRegister.Count > 3 && GameManager.ServerId != 9001)
                        {
                            return -100;
                        }
                        else
                        {
                            if (!SongCampRegister.ContainsKey(KPlayer.RoleID))
                            {
                                var _Battle = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 10, 0);

                                SongCampRegister.TryAdd(KPlayer.RoleID, _Battle);

                                this.MovePlayerToArena(KPlayer, Camp);

                                return 0;
                            }
                            else
                            {
                                if (SongCampRegister.TryGetValue(KPlayer.RoleID, out var _Battle))
                                {
                                    _Battle.Player = KPlayer;

                                    this.MovePlayerToArena(KPlayer, Camp);

                                    return 0;
                                }
                            }
                        }
                    }
                }
                else if (Camp == 20)
                {
                    if (GuildEvent_JinGroup != "")
                    {
                        if (KPlayer.GuildName != GuildEvent_JinGroup)
                        {
                            return -20;
                        }

                        if (JinCampRegister.Count >= maxPlayerRegister && maxPlayerRegister != -1 && GameManager.ServerId != 9001)
                        {
                            return -30;
                        }

                        if (!JinCampRegister.ContainsKey(KPlayer.RoleID))
                        {
                            var _Battle = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 20, 0);

                            JinCampRegister.TryAdd(KPlayer.RoleID, _Battle);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        }
                        else
                        {
                            if (JinCampRegister.TryGetValue(KPlayer.RoleID, out var _Battle))
                            {
                                _Battle.Player = KPlayer;

                                this.MovePlayerToArena(KPlayer, Camp);

                                return 0;
                            }
                        }
                    }
                    if (JinCampRegister.Count - SongCampRegister.Count > 3 && GameManager.ServerId != 9001)
                    {
                        return -200;
                    }
                    else
                    {
                        if (!JinCampRegister.ContainsKey(KPlayer.RoleID))
                        {
                            var _Battle = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 20, 0);

                            JinCampRegister.TryAdd(KPlayer.RoleID, _Battle);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        }
                        else
                        {
                            if (JinCampRegister.TryGetValue(KPlayer.RoleID, out var _Battle))
                            {
                                _Battle.Player = KPlayer;

                                this.MovePlayerToArena(KPlayer, Camp);

                                return 0;
                            }
                        }
                    }
                }
                else
                {
                    return -3;
                }
            } // Nếu chiến trường đang diễn ra
            else if (BATTLESTATE == BattelStatus.STATUS_START)
            {
                var BattlePlayerSaveInBXH = SongJinRankking.FirstOrDefault(x => x.Player.RoleID == (KPlayer.RoleID));
                if (BattlePlayerSaveInBXH == null)
                {
                    BattlePlayerSaveInBXH = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, Camp, 0);
                }

                if (Camp == 10)
                {
                    if (!SongCampRegister.ContainsKey(KPlayer.RoleID))
                    {
                        if (IsGuildEvent || GameManager.ServerLineID == 99) 
                        {
                            if (KPlayer.GuildName != GuildEvent_SongGroup) 
                            {
                                return -10;
                            }

                            // nếu đang ở bên kia r thì thôi ko cho vào bên còn lại
                            if (BattlePlayerSaveInBXH.Camp == 20)
                            {
                                return -5;
                            }    

                            if (BattlePlayerSaveInBXH.Camp == 10)
                            {
                                SongCampRegister.TryAdd(KPlayer.RoleID, BattlePlayerSaveInBXH);
                                this.MovePlayerToArena(KPlayer, Camp);
                                return 0;
                            }   

                            // thêm vào danh sách đăng ký và cho vào map
                            var _BattleRegister = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 10, 0);

                            SongCampRegister.TryAdd(KPlayer.RoleID, _BattleRegister);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        } 
                        else 
                        {
                            // cho phép đăng ký kể cả khi đẫ bắt đầu 12/06/2022
                            
                            // kiểm tra xem hai bên có cân bằng ng chơi ko?
                            if (SongCampRegister.Count - JinCampRegister.Count > 3 && GameManager.ServerId != 9001)
                            {
                                return -100;
                            }

                            // nếu đang ở bên kia r thì thôi ko cho vào bên còn lại
                            if (BattlePlayerSaveInBXH.Camp == 20)
                            {
                                return -5;
                            }    

                            if (BattlePlayerSaveInBXH.Camp == 10)
                            {
                                SongCampRegister.TryAdd(KPlayer.RoleID, BattlePlayerSaveInBXH);
                                this.MovePlayerToArena(KPlayer, Camp);
                                return 0;
                            }   

                            // thêm vào danh sách đăng ký và cho vào map
                            var _BattleRegister = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 10, 0);

                            SongCampRegister.TryAdd(KPlayer.RoleID, _BattleRegister);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        }
                    }
                    if (SongCampRegister.TryGetValue(KPlayer.RoleID, out var _Battle))
                    {
                        _Battle.Player = KPlayer;

                        this.MovePlayerToArena(KPlayer, Camp);

                        return 0;
                    }
                }
                else if (Camp == 20)
                {
                    if (!JinCampRegister.ContainsKey(KPlayer.RoleID))
                    {
                        if (IsGuildEvent || GameManager.ServerLineID == 99)
                        {
                            if (KPlayer.GuildName != GuildEvent_JinGroup) 
                            {
                                return -20;
                            }

                            // nếu đang ở bên kia r thì thôi ko cho vào bên còn lại
                            if (BattlePlayerSaveInBXH.Camp == 10)
                            {
                                return -5;
                            }

                            if (BattlePlayerSaveInBXH.Camp == 20)
                            {
                                JinCampRegister.TryAdd(KPlayer.RoleID, BattlePlayerSaveInBXH);
                                this.MovePlayerToArena(KPlayer, Camp);
                                return 0;
                            }    
                            

                            // thêm vào danh sách đăng ký và cho vào map
                            var _BattleRegister = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 20, 0);

                            JinCampRegister.TryAdd(KPlayer.RoleID, _BattleRegister);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        } else {
                            // cho phép đăng ký kể cả khi đẫ bắt đầu 12/06/2022

                            // kiểm tra xem hai bên có cân bằng ng chơi ko?
                            if (JinCampRegister.Count - SongCampRegister.Count > 3 && GameManager.ServerId != 9001)
                            {
                                return -100;
                            }

                            // nếu đang ở bên kia r thì thôi ko cho vào bên còn lại
                            if (BattlePlayerSaveInBXH.Camp == 10)
                            {
                                return -5;
                            }

                            if (BattlePlayerSaveInBXH.Camp == 20)
                            {
                                JinCampRegister.TryAdd(KPlayer.RoleID, BattlePlayerSaveInBXH);
                                this.MovePlayerToArena(KPlayer, Camp);
                                return 0;
                            }    
                            

                            // thêm vào danh sách đăng ký và cho vào map
                            var _BattleRegister = new BattlePlayer(KPlayer, 0, 0, 1, 0, 0, 20, 0);

                            JinCampRegister.TryAdd(KPlayer.RoleID, _BattleRegister);

                            this.MovePlayerToArena(KPlayer, Camp);

                            return 0;
                        }
                    }

                    if (JinCampRegister.TryGetValue(KPlayer.RoleID, out var _Battle))
                    {
                        _Battle.Player = KPlayer;

                        this.MovePlayerToArena(KPlayer, Camp);

                        return 0;
                    }
                }
                else
                {
                    return -3;
                }
            }
            else
            {
                return -4;
            }

            return -4;
        }

        #endregion Battel Register

        #region ClearBattle()

        public void ResetBattle()
        {
            TotalSongScore = 0;
            TotalJinScore = 0;
            ClearMonster();
            BATTLESTATE = BattelStatus.STATUS_NULL;
            LastTick = 0;
            CurRanking = new SongJinBattleRankingInfo();
            SongCampRegister.Clear();
            SongJinRankking = new List<BattlePlayer>();
            JinCampRegister.Clear();
            IsGuildEvent = false;
            GuildEvent_JinGroup = "";
            GuildEvent_SongGroup = "";
        }

        #endregion ClearBattle()

        /// <summary>
        /// Chuyển người chơi tới tống kim
        /// </summary>
        /// <param name="KPlayer"></param>
        /// <param name="Camp"></param>
        public void MovePlayerToArena(KPlayer KPlayer, int Camp)
        {
            GlobalEventSource.getInstance().fireEvent(new TongKimCheckInEventObject(KPlayer, GameManager.IsKuaFuServer));

            KPlayer.PKMode = (int)PKMode.Custom;
            KPlayer.Camp = Camp;

            KT_TCPHandler.RecordCopySceneInfoToDB(KPlayer, _BattleConfig.MapID, KPlayer.CurrentMapCode, (int)KPlayer.CurrentPos.X, (int)KPlayer.CurrentPos.Y, -1);

            var _Find = _BattleConfig.Region.Where(x => x.RegionCamp == Camp).FirstOrDefault();

            if (_Find != null)
            {
                GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), KPlayer, _BattleConfig.MapID, _Find.PosX, _Find.PosY, 0, false);
                GlobalEventSource.getInstance().fireEvent(new MoveToArenaEventObject(KPlayer));
            }
        }

        #region MonsterControler

        public void ClearMonster()
        {
            var objs = GameManager.MonsterMgr.GetObjectsByMap(_BattleConfig.MapID);
            if (objs != null)
            {
                // Duyệt danh sách
                foreach (var obj in objs)
                {
                    obj.MonsterZoneNode?.DestroyMonster(obj as Monster);
                }
            }
        }

        public void CreateMonster()
        {
            foreach (var _Monster in _BattleConfig.BattelMonster)
            {
                var Tag = "";

                // Nếu là boss thì delay xuất hiện
                if (_Monster.IsBoss)
                {
                    Tag = "Boss";

                    var Funtion = KTKTAsyncTask.Instance.ScheduleExecuteAsync(new DelayFuntionAsyncTask("CreateMonster", new Action(() => CreateMonster(_Monster, true))), _Monster.RebornTime);
                }
                else // Nếu không phải thì mọc luôn
                {
                    CreateMonster(_Monster);
                }
            }
        }

        public void CreateMonster(MonsterBattle _Monster, bool IsBoss = false)
        {
            var aiType = MonsterAIType.Normal;
            if (IsBoss)
            {
                if (_Monster.Camp == 10)
                {
                    var ProtectNotify = _Monster.Name + " đã xuất hiện tại [" + _Monster.PosX + "|" + _Monster.PosY + "] hãy mau tới bảo vệ";
                    NotifyAllSongCampRegister(ProtectNotify);
                }
                else if (_Monster.Camp == 20)
                {
                    var ProtectNotify = _Monster.Name + " đã xuất hiện tại [" + _Monster.PosX + "|" + _Monster.PosY + "] hãy mau tới bảo vệ";
                    NotifyAllJinCampRegister(ProtectNotify);
                }

                aiType = MonsterAIType.Elite;

                this.IsBossReswpan = true;
            }
            // Ngũ hành
            var series = KE_SERIES_TYPE.series_none;

            var RandomSeri = KTGlobal.GetRandomNumber(1, 5);
            series = (KE_SERIES_TYPE)RandomSeri;

            // Hướng quay
            var dir = KiemThe.Entities.Direction.NONE;

            var RandomDir = KTGlobal.GetRandomNumber(0, 7);

            dir = (KiemThe.Entities.Direction)RandomDir;

            var Level = KTGlobal.GetRandomNumber(_BattleConfig.MinLevel, _BattleConfig.MaxLevel);

            // AIType

            var hp = -1;
            if (IsBoss && GameManager.ServerLineID == 99) {
                hp = 250000000;
            }

            GameManager.MonsterZoneMgr.AddDynamicMonsters(_BattleConfig.MapID, _Monster.ID, -1, 1, _Monster.PosX, _Monster.PosY, _Monster.Name, "", hp, Level, dir, series, aiType, _Monster.RespawnTick, -1, -1, "", null, _Monster.Camp);
        }

        public void ProtectBossBounds()
        {
            if (this.IsBossReswpan && (BATTLESTATE == BattelStatus.STATUS_START || BATTLESTATE == BattelStatus.STATUS_PREPAREEND))
            {
                var _TotalMonster = this.GetAllBossLive();

                foreach (var _Monster in _TotalMonster)
                {
                    var friends = KTLogic.GetNearByPeacePlayersReductCost(_Monster, 500);

                    foreach (var _Player in friends)
                    {
                        var NotifyProtect = "Ngươi có công bảo vệ " + _Monster.MonsterInfo.Name + ", nhận được <color=yellow>15</color> điểm tích lũy";

                        PlayerManager.ShowNotification(_Player, NotifyProtect);

                        BattlePlayer _FindKill = null;

                        if (_Player.Camp == 10)
                        {
                            SongCampRegister.TryGetValue(_Player.RoleID, out _FindKill);
                        }
                        else if (_Player.Camp == 20)
                        {
                            JinCampRegister.TryGetValue(_Player.RoleID, out _FindKill);
                        }

                        if (_FindKill != null)
                        {
                            var KillCurenScore = _FindKill.Score;

                            var ScoreGet = 15;

                            var TotalScore = KillCurenScore + ScoreGet;

                            _FindKill.Score = TotalScore;

                            if (_Player.Camp == 10)
                            {
                                TotalSongScore += ScoreGet;
                            }
                            else if (_Player.Camp == 20)
                            {
                                TotalJinScore += ScoreGet;
                            }
                            UpdateTitleRank(_FindKill.Player, _FindKill.Rank);

                            // Gửi notify cho thằng giết
                            //NotifySocreForPlayer(_FindKill, true);
                        }
                    }
                }
            }
        }

        public List<Monster> GetAllBossLive()
        {
            var objs = GameManager.MonsterMgr.GetObjectsByMap(_BattleConfig.MapID);

            var MonsterOut = new List<Monster>();

            var AllBoss = _BattleConfig.BattelMonster.GroupBy(x => x.ID).Select(y => y.FirstOrDefault()).ToList();
            // Duyệt danh sách
            foreach (var obj in objs)
            {
                foreach (var _Monster in AllBoss)
                {
                    if (_Monster.IsBoss)
                    {
                        if ((obj).MonsterInfo.ExtensionID == _Monster.ID)
                        {
                            MonsterOut.Add((obj));
                        }
                    }
                }
            }

            return MonsterOut;
        }

        #endregion MonsterControler

        public void intBattle(int Level)
        {
            var Battle_Config = "";

            if (Level == 1)
            {
                Battle_Config = "Config/KT_Battle/Battle_SongJin_Low.xml";
                if (GameManager.IsKuaFuServer)
                {
                    Battle_Config = "Config/KT_Battle/Battle_SongJin_Low_kuafu.xml";
                } 
                else if (!ServerConfig.Instance.KuaFuServerEnable)
                {
                    Battle_Config = "Config/KT_Battle/Battle_SongJin_Low_KuaFuDisable.xml";
                }    
            }
            else if (Level == 2)
            {
                Battle_Config = "Config/KT_Battle/Battle_SongJin_Mid.xml";
                if (!ServerConfig.Instance.KuaFuServerEnable)
                {
                    Battle_Config = "Config/KT_Battle/Battle_SongJin_Mid_KuaFuDisable.xml";
                }
            }
            else if (Level == 3)
            {
                Battle_Config = "Config/KT_Battle/Battle_SongJin_High.xml";
            }

            var Files = Global.GameResPath(Battle_Config);

            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(BattleConfig));
                _BattleConfig = serializer.Deserialize(stream) as BattleConfig;
            }
        }

        public bool initialize(int Level)
        {
            this.BattleLevel = Level;

            var TimerName = "Battle_SongJin_" + Level;

            intBattle(Level);

            //        Thread t = new Thread(() => {
            //            while (true)
            //{
            //                this.ProsecBattle(null, null);
            //                Thread.Sleep(2000);
            //            }
            //        });
            //        t.IsBackground = false;
            //        t.Start();

            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask(TimerName, ProsecBattle), 5 * 1000, 2000);

            LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Stating Battle OK!");

            return true;
        }

        public bool CanUsingTeleport()
        {
            return BATTLESTATE == BattelStatus.STATUS_START || BATTLESTATE == BattelStatus.STATUS_PREPAREEND;
        }

        public BattelStatus GetBattelState()
        {
            return BATTLESTATE;
        }

        #region SENDPREADINGNOTIFY

        public void SendPreadingNotify(KPlayer Player, int Sec, int TotalTong, int TotalKim, bool IsNotiyText)
        {
            this.PlayChangeState(Player, 1);

            if (IsNotiyText)
            {
                var _Notify = new G2C_EventNotification();

                _Notify.EventName = "Chiến trường sẽ bắt đầu sau:";

                _Notify.ShortDetail = "TIME|" + Sec;

                _Notify.TotalInfo = new List<string>();

                if (IsGuildEvent || GameManager.ServerLineID == 99)
                {
                    _Notify.TotalInfo.Add($"Báo Danh {GuildEvent_SongGroup}: " + TotalTong);
                    _Notify.TotalInfo.Add($"Báo Danh {GuildEvent_JinGroup}: " + TotalKim);
                } else {
                    int tongBots = SongJinBotManager.ActiveCount(this, 10);
                    int kimBots = SongJinBotManager.ActiveCount(this, 20);
                    _Notify.TotalInfo.Add("Báo Danh Tống : " + (TotalTong + tongBots));
                    _Notify.TotalInfo.Add("Báo Danh Kim : " + (TotalKim + kimBots));
                }

                _Notify.TotalInfo.Add("");

                if (Player.IsOnline())
                {
                    Player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
                }
                else
                {
                    //Console.WriteLine("OFFLINE");
                }
            }
        }


        /// <summary>
        /// Cứ 5s spam camp 1 lần
        /// </summary>
        public void SpamCamp()
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    if (PlayerBattle.Player.PKMode != (int)PKMode.Custom)
                    {
                        PlayerBattle.Player.PKMode = (int)PKMode.Custom;
                    }
                    if (PlayerBattle.Player.Camp != 10)
                    {
                        PlayerBattle.Player.Camp = 10;
                    }

                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    if (PlayerBattle.Player.PKMode != (int)PKMode.Custom)
                    {
                        PlayerBattle.Player.PKMode = (int)PKMode.Custom;
                    }
                    if (PlayerBattle.Player.Camp != 20)
                    {
                        PlayerBattle.Player.Camp = 20;
                    }

                }
            }
        }

        public void UpdatePreading(bool IsNotiyText)
        {
            var SEC = (LastTick + _BattleConfig.RegisterDualtion * 1000) - TimeUtil.NOW();

            var FinalSec = (int)(SEC / 1000);

            var MSG = "Chiến trường sắp bắt đầu, thời gian còn: [" + FinalSec + "] giây.";

            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    if (PlayerBattle.Player.PKMode != (int)PKMode.Custom)
                    {
                        PlayerBattle.Player.PKMode = (int)PKMode.Custom;
                    }

                    if (PlayerBattle.Player.Camp != PlayerBattle.Camp)
                    {
                        PlayerBattle.Player.Camp = PlayerBattle.Camp;
                    }

                    PlayerManager.ShowNotification(PlayerBattle.Player, MSG);

                    SendPreadingNotify(PlayerBattle.Player, FinalSec, SongCampRegister.Count, JinCampRegister.Count, IsNotiyText);
                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    if (PlayerBattle.Player.PKMode != (int)PKMode.Custom)
                    {
                        PlayerBattle.Player.PKMode = (int)PKMode.Custom;
                    }
                    if (PlayerBattle.Player.Camp != PlayerBattle.Camp)
                    {
                        PlayerBattle.Player.Camp = PlayerBattle.Camp;
                    }

                    PlayerManager.ShowNotification(PlayerBattle.Player, MSG);

                    SendPreadingNotify(PlayerBattle.Player, FinalSec, SongCampRegister.Count, JinCampRegister.Count, IsNotiyText);
                }
            }
        }

        #endregion SENDPREADINGNOTIFY

        #region NotifyBattleInfo

        public void SendBattleNotify(BattlePlayer InputPlayer, int Sec)
        {
            var _Notify = new G2C_EventNotification();

            _Notify.EventName = "Tống Kim Công Báo";
            if (IsGuildEvent || GameManager.ServerLineID == 99)
            {
                _Notify.EventName = "Chiến Báo";
            }
            if (Sec > 0)
            {
                _Notify.ShortDetail = "TIME|" + Sec;
            }
            else
            {
                _Notify.ShortDetail = "Chiến trường đã kết thúc!";
            }

            _Notify.TotalInfo = new List<string>();

            _Notify.TotalInfo.Add("Giết Địch : " + InputPlayer.Kill);

            _Notify.TotalInfo.Add("Bị Giết : " + InputPlayer.BeKill);

            _Notify.TotalInfo.Add("Tích Lũy : " + InputPlayer.Score);

            _Notify.TotalInfo.Add("Hạng Hiện Tại :" + (GetRankInBxh(InputPlayer.Player.RoleID) + 1));

            _Notify.TotalInfo.Add("");

            if (InputPlayer.Player.IsOnline())
            {
                InputPlayer.Player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        public void SendBattleNotifyEnd(BattlePlayer InputPlayer, int Sec)
        {
            var _Notify = new G2C_EventNotification();

            _Notify.EventName = "Thời gian nhận thưởng còn :";
            if (Sec > 0)
            {
                _Notify.ShortDetail = "TIME|" + Sec;
            }

            _Notify.TotalInfo = new List<string>();

            _Notify.TotalInfo.Add("");

            if (InputPlayer.Player.IsOnline())
            {
                InputPlayer.Player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        public void SendKillStreak(KPlayer InputPlayer, int Count)
        {
            var _State = new G2C_KillStreak();

            _State.KillNumber = Count;

            if (InputPlayer.IsOnline())
            {
                InputPlayer.SendPacket<G2C_KillStreak>((int)TCPGameServerCmds.CMD_KT_KILLSTREAK, _State);
            }
        }

        public void UpdateBattleNotify()
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    var SEC = (LastTick + (_BattleConfig.BattleDualtion * 1000)) - TimeUtil.NOW();

                    var FinalSec = (int)(SEC / 1000);

                    SendBattleNotify(PlayerBattle, FinalSec);
                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    var SEC = (LastTick + (_BattleConfig.BattleDualtion * 1000)) - TimeUtil.NOW();

                    var FinalSec = (int)(SEC / 1000);

                    SendBattleNotify(PlayerBattle, FinalSec);
                }
            }
        }

        public void UpdateBattleNotifyAdward()
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    var SEC = (LastTick + (_BattleConfig.ReviceAwardDualtion * 1000)) - TimeUtil.NOW();

                    var FinalSec = (int)(SEC / 1000);

                    SendBattleNotifyEnd(PlayerBattle, FinalSec);
                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    var SEC = (LastTick + (_BattleConfig.ReviceAwardDualtion * 1000)) - TimeUtil.NOW();

                    var FinalSec = (int)(SEC / 1000);

                    SendBattleNotifyEnd(PlayerBattle, FinalSec);
                }
            }
        }

        /// <summary>
        /// Gửi thành tích hiện tại cho player
        /// </summary>
        /// <param name="Player"></param>
        public void NotifySocreForPlayer(BattlePlayer PlayerBattle, bool ShowKillStreak)
        {
            var SEC = (LastTick + (_BattleConfig.BattleDualtion * 1000)) - TimeUtil.NOW();

            var FinalSec = (int)(SEC / 1000);

            SendBattleNotify(PlayerBattle, FinalSec);

            if (ShowKillStreak)
            {
                if (PlayerBattle.KillStreak > 2)
                {
                    this.SendKillStreak(PlayerBattle.Player, PlayerBattle.KillStreak);

                    if (PlayerBattle.KillStreak % 6 == 0 && PlayerBattle.KillStreak > 0)
                    {
                        var Notify = KTGlobal.CreateStringByColor("Quân [" + Battel_SonJin_Manager.GetNameCamp(PlayerBattle.Camp) + "] - [" + GetRankTitile(PlayerBattle.Player, PlayerBattle.Rank) + " - " + PlayerBattle.Player.RoleName + "] liên tiếp đẩy lùi " + PlayerBattle.KillStreak + " quân địch, nhận thưởng Liên Trảm 150 điểm tích lũy", ColorType.Yellow);
                        if (IsGuildEvent || GameManager.ServerLineID == 99) 
                        {
                            var GuildName = PlayerBattle.Camp == 10 ? GuildEvent_SongGroup : GuildEvent_JinGroup;
                            Notify = KTGlobal.CreateStringByColor("[" + GuildName + "] - [" + GetRankTitile(PlayerBattle.Player, PlayerBattle.Rank) + " - " + PlayerBattle.Player.RoleName + "] liên tiếp đẩy lùi " + PlayerBattle.KillStreak + " quân địch, nhận thưởng Liên Trảm 150 điểm tích lũy", ColorType.Yellow);
                        }

                        NotifyAllBattle(Notify);
                    }
                }
            }
        }

        public string GetRankTitile(KPlayer Player, int Rank)
        {
            var find = _BattleConfig.Rank.Where(x => x.RankID == Rank).FirstOrDefault();

            if (find != null)
            {
                return find.RankTitle;
            }
            else
            {
                return "Binh sĩ";
            }
        }

        public void NotifyAllSongCampRegister(string MSG)
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    PlayerManager.ShowNotification(PlayerBattle.Player, MSG);
                }
            }
        }

        public void NotifyAllJinCampRegister(string MSG)
        {
            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    PlayerManager.ShowNotification(PlayerBattle.Player, MSG);
                }
            }
        }

        public void NotifyAllBattle(string MSG)
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    KTGlobal.SendSystemEventNotificationToPlayer(PlayerBattle.Player, MSG);
                    // PlayerManager.ShowNotification(PlayerBattle.Player, MSG);
                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    KTGlobal.SendSystemEventNotificationToPlayer(PlayerBattle.Player, MSG);
                    // PlayerManager.ShowNotification(PlayerBattle.Player, MSG);
                }
            }
        }

        #endregion NotifyBattleInfo

        #region UpdateBXH

        public SongJinBattleRankingInfo RankingBuilder(KPlayer Client)
        {
            var _Rank = new SongJinBattleRankingInfo();
            _Rank.JinTotalScore = TotalJinScore + "";
            _Rank.SongTotalScore = TotalSongScore + "";
            _Rank.PlayerRanks = new List<SongJinRanking>();

            lock (SongJinRankking)
            {
                // Chỉ lấy ra 10 người
                var _Soft = SongJinRankking.Take(10).ToList();

                for (var i = 0; i < _Soft.Count; i++)
                {
                    var _Player = _Soft[i];

                    var _PlayerRank = new SongJinRanking();
                    if (_Player.Camp == 10)
                    {
                        _PlayerRank.Camp = "Tống";
                    }
                    else
                    {
                        _PlayerRank.Camp = "Kim";
                    }

                    _PlayerRank.Faction = _Player.Player.m_cPlayerFaction.GetFactionId();
                    _PlayerRank.KillCount = _Player.Kill;
                    _PlayerRank.Level = _Player.Player.m_Level;
                    _PlayerRank.Rank = i + 1;
                    _PlayerRank.MaxKillStreak = _Player.MaxKillStreak;
                    _PlayerRank.PlayerName = _Player.Player.RoleName;
                    _PlayerRank.Score = _Player.Score;

                    _Rank.PlayerRanks.Add(_PlayerRank);
                }

                var findHost = SongJinRankking.Where(x => x.Player.RoleID == Client.RoleID).FirstOrDefault();
                if (findHost != null)
                {
                    var _PlayerRank = new SongJinRanking();
                    if (findHost.Camp == 10)
                    {
                        _PlayerRank.Camp = "Tống";
                    }
                    else
                    {
                        _PlayerRank.Camp = "Kim";
                    }

                    _PlayerRank.Faction = findHost.Player.m_cPlayerFaction.GetFactionId();
                    _PlayerRank.KillCount = findHost.Kill;
                    _PlayerRank.Level = findHost.Player.m_Level;
                    _PlayerRank.Rank = GetRankInBxh(Client.RoleID) + 1;
                    _PlayerRank.MaxKillStreak = findHost.MaxKillStreak;
                    _PlayerRank.PlayerName = findHost.Player.RoleName;
                    _PlayerRank.Score = findHost.Score;

                    _Rank.PlayerRanks.Add(_PlayerRank);
                }
            }

            return _Rank;
        }

        public void UpadateBXH()
        {
            lock (SongJinRankking)
            {
                SongJinRankking.Clear();
                _RankMap.Clear();

                var _SongCampRegister = SongCampRegister.Select(x => x.Value).ToList();

                SongJinRankking.AddRange(_SongCampRegister);

                var _JinCampRegister = JinCampRegister.Select(x => x.Value).ToList();

                SongJinRankking.AddRange(_JinCampRegister);

                //SOFT LẠI DANH SÁCH Theo danh sách giảm dần
                var _Soft = SongJinRankking.OrderByDescending(x => x.Score).ToList();

                // GÁN LẠI SOFT = DANH SÁCH MỚI ĐÃ SẮP XẾP
                SongJinRankking = _Soft;

                for (int i = 0; i < SongJinRankking.Count; i++)
                {
                    if (SongJinRankking[i].Player != null)
                    {
                        _RankMap[SongJinRankking[i].Player.RoleID] = i;
                    }
                }
            }
        }

        #endregion UpdateBXH

        #region BattleState

        public void OnChangeBattleState(KPlayer KPlayer, int Min)
        {
            this.PlayChangeState(KPlayer, 1);

            this.SendPreadingNotify(KPlayer, Min, SongCampRegister.Count, JinCampRegister.Count, true);

            UpdateTitleRank(KPlayer, 1);
        }

        public void PlayChangeState(KPlayer Player, int State)
        {
            var _State = new G2C_EventState();

            _State.EventID = 20;
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

        public void ForceStartBattle()
        {
            ResetBattle();
            LastTick = TimeUtil.NOW();
            BATTLESTATE = BattelStatus.STATUS_PREPARE;

            KTGlobal.SendSystemEventNotification("Đại chiến Tống Kim sắp bắt đầu, hiện đang tiến hành báo danh, người muốn tham chiến hãy nhanh chóng tìm Quan Mộ Binh (chiến trường) hoặc dùng Chiêu Thư Tống Kim, thời gian báo danh còn: 10 phút.");
        }

        public bool IsInBattleSongJin(int PlayerMapCode)
        {
            if (_BattleConfig.MapID == PlayerMapCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion BattleState

        #region Phần thưởng

        /// <summary>
        /// Lấy ra rank hiện tại trong bản xếp hạng
        /// </summary>
        /// <param name="RoleID"></param>
        /// <returns></returns>
        public int GetRankInBxh(int RoleID)
        {
            lock (SongJinRankking)
            {
                if (_RankMap.TryGetValue(RoleID, out int rank))
                {
                    return rank;
                }
                return -1;
            }
        }

        public void CheckAward(GameMap map, NPC npc, KPlayer client)
        {
            BattlePlayer _FindPlayer = null;

            if (client.Camp == 10)
            {
                SongCampRegister.TryGetValue(client.RoleID, out _FindPlayer);
            }
            else if (client.Camp == 20)
            {
                JinCampRegister.TryGetValue(client.RoleID, out _FindPlayer);
            }

            if (_FindPlayer == null)
            {
                PlayerManager.ShowNotification(client, "[" + client.RoleName + "][" + client.Camp + "] Không tìm thấy người chơi!");
                return;
            }
            var Text = "";

            var _NpcDialog = new KNPCDialog();

            var Selections = new Dictionary<int, string>();

            // Nếu mà chiến trường đã kết thúc thì add thêm cái nhận thưởng
            if (BATTLESTATE == BattelStatus.STATUS_END)
            {
                if (_FindPlayer.Score == 0 || IsGuildEvent || GameManager.ServerLineID == 99)
                {
                    Text = "Chiến trường đã kết thúc. Hôm nay người làm rất tốt.";
                }
                else
                {
                    var MoreAward = "";
                    var MoreAwardExtras = "";

                    var _Find = _BattleConfig.BattelAward.OrderByDescending(x => x.Score).Where(x => x.Score <= _FindPlayer.Score).FirstOrDefault();

                    var PlayRank = GetRankInBxh(client.RoleID) + 1;

                    var _FindExtrass = _BattleConfig.BattelAwardExtras.Where(x => x.Rank == PlayRank).FirstOrDefault();

                    if (_FindExtrass != null)
                    {
                        MoreAwardExtras = "Thưởng Top :" + PlayRank + "<br><br>" + "Danh Vọng Chiến Trường:" + _FindExtrass.Point;
                    }

                    if (_Find != null)
                    {
                        MoreAward = "Bạc Khóa :" + _Find.Money + "<br><br>" + "Danh Vọng Chiến Trường:" + _Find.Point;
                    }

                    var UYDANH = "";

                    if (PlayRank == 1)
                    {
                        UYDANH = "Uy danh +15";
                        //if (GameManager.IsKuaFuServer)
                        {
                            UYDANH += "<br><b><color=#00ff2a>";
                            UYDANH += "<br><b><color=#00ff2a>";
                        }
                    }
                    else if (PlayRank >= 2 && PlayRank <= 10)
                    {
                        UYDANH = "Uy danh +8";
                        // if (GameManager.IsKuaFuServer)
                        {
                            UYDANH += "<br><b><color=#00ff2a>";
                            UYDANH += "<br><b><color=#00ff2a>";
                        }
                    }
                    else if (PlayRank >= 11 && PlayRank <= 20)
                    {
                        UYDANH = "Uy danh +4";
                        //  if (GameManager.IsKuaFuServer)
                        {
                            UYDANH += "<br><b><color=#00ff2a>";
                            UYDANH += "<br><b><color=#00ff2a>";
                        }
                    }
                    else
                    {
                        UYDANH = "Uy danh +2";
                    }

                    if (_FindPlayer.Score >= 1000 || _FindPlayer.IsReviceAward == false)
                    {
                        Text = "Điểm tích lũy của ngươi là :" + _FindPlayer.Score + "<br><br>Đã giết : " + _FindPlayer.Kill + " quân địch!<br><br>Xếp hạng của ngươi : " + GetRankInBxh(_FindPlayer.Player.RoleID) + "<br><br>Phần thưởng :<br><br>Exp :" + GetExpReward(_FindPlayer.Player.m_Level, _FindPlayer.Score) + "<br><br>" + MoreAward + "<br><br>" + MoreAwardExtras + "<br><br>" + UYDANH;

                        if (!_FindPlayer.IsReviceAward)
                        {
                            // Nếu có Điểm tích lũy thì cho nhận thưởng
                            Selections.Add(-1, "Nhận Thưởng");
                        }
                    }
                    else
                    {
                        Text = "Điểm tích lũy của ngươi là :" + _FindPlayer.Score + "<br><br>Đã giết : " + _FindPlayer.Kill + " quân địch!<br><br>Xếp hạng của ngươi : " + GetRankInBxh(_FindPlayer.Player.RoleID) + "<br><br>Phần thưởng :<br><br>" + KTGlobal.CreateStringByColor("Không đủ điều kiện nhận thưởng", ColorType.Importal);
                    }
                }
            }
            else
            {
                Text = "Chiến trường đang diễn ra, hãy đợi kết thúc để đổi lấy phần thưởng.";

                Selections.Add(-3, "Ta muốn nhận quân rương quân nhu");
            }

            // Menu này lúc nào cũng có
            // Nếu đang xảy ra chiến trường thì ko cho rời khỏi chiến trường
            //if (BATTLESTATE != BattelStatus.STATUS_START)
            {
                Selections.Add(-2, "Ta muốn rời khỏi chiến trường");
            }  

            Action<TaskCallBack> ActionWork = (x) => DoActionSelect(_FindPlayer, npc, x);

            _NpcDialog.OnSelect = ActionWork;

            _NpcDialog.Selections = Selections;

            _NpcDialog.Text = Text;

            _NpcDialog.Show(npc, client);
        }

        /// <summary>
        /// Click chọn acc
        /// </summary>
        /// <param name="findPlayer"></param>
        /// <param name="npc"></param>
        /// <param name="x"></param>
        private void DoActionSelect(BattlePlayer findPlayer, NPC npc, TaskCallBack x)
        {
            if (x.SelectID == -1)
            {
                // Thực hiện cho nó nhận xong thì đóng dialog
                ReviceAward(findPlayer, npc, x.SelectID);

                KT_TCPHandler.CloseDialog(findPlayer.Player);
            }
            else if (x.SelectID == -2)
            {
                // Thực hiện đóng khung
                KT_TCPHandler.CloseDialog(findPlayer.Player);
                // Nếu có tích lũy
                if (findPlayer.Score > 0)
                {
                    var MSG = "";
                    if (BATTLESTATE != BattelStatus.STATUS_END)
                    {
                        MSG = "Chiến trường chưa kết thúc nếu người rời đi điểm tích lũy sẽ không được tính!";
                    }
                    else
                    {
                        MSG = "Ngươi có chắc muốn rời đi?Ta khuyên ngươi hãy nhận thưởng trước!";
                    }

                    PlayerManager.ShowMessageBox(findPlayer.Player, "Chú ý ", MSG, new Action(() => MoveOutConfirm(findPlayer)));
                }
                else // Nếu không có tích lũy thì chim cút luôn
                {
                    MoveOutConfirm(findPlayer);
                }
            }
            else if (x.SelectID == -3)
            {
                // Nếu đã nhận đủ 4 rương mỗi ngày
                if (findPlayer.Player.ReviceMedicineOfDay > 1)
                {
                    KTLuaLib_Player.SendMSG("Hôm nay người đã nhận hết quân nhu rồi", findPlayer.Player, npc);
                }
                else
                {
                    if (!KTGlobal.IsHaveSpace(1, findPlayer.Player))
                    {
                        KTLuaLib_Player.SendMSG("Túi đồ không đủ chỗ trống. Vui lòng thu dọn hành trang", findPlayer.Player, npc);
                        return;
                    }
                    var ITEMLEVEL = 0;

                    if (this.BattleLevel == 1)
                    {
                        ITEMLEVEL = 255;
                    }
                    if (this.BattleLevel == 2)
                    {
                        ITEMLEVEL = 259;
                    }
                    if (this.BattleLevel == 3)
                    {
                        //ITEMLEVEL = 261;
                        ITEMLEVEL = 259;
                    }

                    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, findPlayer.Player, ITEMLEVEL, 1, 0, "TONGKIMRUONG", false, 1, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, false))
                    {
                        PlayerManager.ShowNotification(findPlayer.Player, "Có lỗi khi nhận rương");
                    }
                    // Update số lần nhận quân nhu
                    findPlayer.Player.ReviceMedicineOfDay = findPlayer.Player.ReviceMedicineOfDay + 1;

                    KT_TCPHandler.CloseDialog(findPlayer.Player);
                }
            }
        }

        public void MoveOutConfirm(BattlePlayer PlayerBattle)
        {
            if (PlayerBattle.Player.Camp == 10)
            {
                if (SongCampRegister.TryRemove(PlayerBattle.Player.RoleID, out var Remove))
                {
                    //trả trạng thái lại cho nhân vật
                    this.PlayChangeState(PlayerBattle.Player, 0);

                    KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.Player, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);

                    GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.Player, preMapCode, prePosX, prePosY, 0, false);
                    // Set lại camp cho người chơi về -1
                    PlayerBattle.Player.Camp = -1;
                    PlayerBattle.Player.TempTitle = "";
                    // set lại hủy phụ bản cho nv
                    PlayerBattle.Player.CurrentCopyMapID = -1;
                    // Xóa thông tin phụ bản cũ
                    KT_TCPHandler.RecordCopySceneInfoToDB(PlayerBattle.Player, -1, -1, -1, -1, -1);
                }
            }
            else if (PlayerBattle.Player.Camp == 20)
            {
                if (JinCampRegister.TryRemove(PlayerBattle.Player.RoleID, out var Remove))
                {
                    //trả trạng thái lại cho nhân vật
                    this.PlayChangeState(PlayerBattle.Player, 0);

                    KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.Player, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);

                    GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.Player, preMapCode, prePosX, prePosY, 0, false);
                    // Set lại camp cho người chơi về -1
                    PlayerBattle.Player.Camp = -1;
                    PlayerBattle.Player.TempTitle = "";
                    // set lại hủy phụ bản cho nv
                    PlayerBattle.Player.CurrentCopyMapID = -1;
                    // Xóa thông tin phụ bản cũ
                    KT_TCPHandler.RecordCopySceneInfoToDB(PlayerBattle.Player, -1, -1, -1, -1, -1);
                }
            }
        }

        /// <summary>
        /// Nhận Exp
        /// </summary>
        /// <param name="nLevel"></param>
        /// <param name="nBouns"></param>
        /// <returns></returns>
        public int GetExpReward(double nLevel, double nBouns)
        {
            double Exp = 0;

            Exp = Math.Floor((700 + Math.Floor((nLevel - 40) / 5) * 100) * 60 * 7 / 3000) * nBouns;

            return (int)Exp;
        }

        /// <summary>
        /// Người chơi nhận thưởng
        /// </summary>
        /// <param name="_Player"></param>
        // Thưởng theo tích lũy
        public void ReviceAward(BattlePlayer _Player, NPC npc, int X)
        {
            if (!_Player.IsReviceAward)
            {
                var Score = _Player.Score;

                var _Find = _BattleConfig.BattelAward.OrderByDescending(x => x.Score).Where(x => x.Score <= Score).FirstOrDefault();

                if (_Find != null)
                {
                    //if (!KTGlobal.IsHaveSpace(1, _Player.Player))
                    //{
                    //    PlayerManager.ShowNotification(_Player.Player, "Cần ít nhất 1 ô trống để nhận thưởng");
                    //    return;
                    //}

                    var Money = _Find.Money;
                    var Point = _Find.Point;
                    string Items = _Find.Items;

                    var PointType = _Find.PointType;

                    var PlayRank = GetRankInBxh(_Player.Player.RoleID) + 1;

                    
                    GlobalEventSource.getInstance().fireEvent(new TongKimReceiveRankingEventObject(_Player.Player, PlayRank));

                    // Nhận thêm phần quà thưởng thêm
                    var _FindExtrass = _BattleConfig.BattelAwardExtras.Where(x => x.Rank == PlayRank).FirstOrDefault();

                    if (_FindExtrass != null)
                    {
                        Point = Point + _FindExtrass.Point;
                        //Items = _FindExtrass.Items;
                    }


                    var _FindPlus = _BattleConfig.BattelAwardPluses.Where(x=> x.RankMin <= PlayRank && PlayRank <= x.RankMax).FirstOrDefault();

                    if (_FindPlus!= null)
                    {
                        if (string.IsNullOrEmpty(Items))
                        {
                            Items = _FindPlus.Items;
                        }
                        else if (!string.IsNullOrEmpty(_FindPlus.Items))
                        {
                            Items = $"{Items}#{_FindPlus.Items}";
                        }
                    }

                    if (!string.IsNullOrEmpty(Items))
                    {
                        var itemList = Items.Split('#');
                        var totalSpaceNeed = 0;
                        foreach (var item in itemList)
                        {
                            var paramItem = item.Split('_');
                            var itemID = Int32.Parse(paramItem[0]);
                            var itemNum = Int32.Parse(paramItem[1]);
                            totalSpaceNeed += KTGlobal.GetTotalSpacesNeedToTakeItem(itemID, itemNum);
                        }

                        if (!KTGlobal.IsHaveSpace(totalSpaceNeed, _Player.Player))
                        {
                            PlayerManager.ShowMessageBox(_Player.Player, "Thông báo", $"Cần ít nhất {totalSpaceNeed} ô trống để nhận thưởng");
                            return;
                        }
                        
                        foreach (var item in itemList)
                        {
                            var paramItem = item.Split('_');

                            var itemID = Int32.Parse(paramItem[0]);
                            var itemNum = Int32.Parse(paramItem[1]);
                            var itemBinding = Int32.Parse(paramItem[2]);
                            var itemEndTime = Int32.Parse(paramItem[3]);

                            var strEndTime = Global.ConstGoodsEndTime;
                            if (itemEndTime != 0)
                            {
                                var dt = DateTime.Now.AddMinutes(itemEndTime);

                                // "1900-01-01 12:00:00";
                                strEndTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            }


                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player.Player, itemID, itemNum, 0, "SONGJINBATTLE", false, itemBinding, false, strEndTime))
                            {
                                PlayerManager.ShowNotification(_Player.Player, "Có lỗi khi nhận vật phẩm");
                            }
                        }
                    }

                    var MoneyPresinal = 0;
                    var MoneyGuild = 0;
                    var oldPrestige = _Player.Player.Prestige;
                    if (PlayRank == 1)
                    {
                        MoneyPresinal = 100000;

                        MoneyGuild = 150;
                        _Player.Player.Prestige += 15;
                        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tống Kim, uy danh tăng từ {2} lên {3}", _Player.Player.RoleName, 15, oldPrestige, _Player.Player.Prestige));
                        //if (GameManager.IsKuaFuServer)
                        //{
                        //    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player.Player, 189, 1, 0, "SONGJINBATTLE", false, 1, false, Global.ConstGoodsEndTime))
                        //    {
                        //        PlayerManager.ShowNotification(_Player.Player, "Có lỗi khi nhận phần thưởng");
                        //    }

                        //    KTGlobal.AddMoney(_Player.Player, 100000, MoneyType.BacKhoa, "SONGJINBATTLE");
                        //}

                        // Thêm kinh nghiệm cho top 1
                        GameManager.ClientMgr.ProcessRoleExperience(_Player.Player, (_Player.Player.m_Level * 5000), true, false, true, "BATTLE_SONG_JIN");
                    }
                    else if (PlayRank >= 2 && PlayRank <= 10)
                    {
                        MoneyPresinal = 50000;

                        MoneyGuild = 150;

                        _Player.Player.Prestige += 8;
                        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tống Kim, uy danh tăng từ {2} lên {3}", _Player.Player.RoleName, 8, oldPrestige, _Player.Player.Prestige));

                        //if (GameManager.IsKuaFuServer)
                        //{
                        //    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player.Player, 188, 1, 0, "SONGJINBATTLE", false, 1, false, Global.ConstGoodsEndTime))
                        //    {
                        //        PlayerManager.ShowNotification(_Player.Player, "Có lỗi khi nhận phần thưởng");
                        //    }

                        //    KTGlobal.AddMoney(_Player.Player, 50000, MoneyType.BacKhoa, "SONGJINBATTLE");
                        //}

                        // Thêm kinh nghiệm cho top 2 - 10
                        if (PlayRank == 2)
                            GameManager.ClientMgr.ProcessRoleExperience(_Player.Player, (_Player.Player.m_Level * 3000), true, false, true, "BATTLE_SONG_JIN");
                        else if (PlayRank == 3)
                            GameManager.ClientMgr.ProcessRoleExperience(_Player.Player, (_Player.Player.m_Level * 2000), true, false, true, "BATTLE_SONG_JIN");
                        else if (PlayRank >=4)
                            GameManager.ClientMgr.ProcessRoleExperience(_Player.Player, (_Player.Player.m_Level * 1000), true, false, true, "BATTLE_SONG_JIN");
                    }
                    else if (PlayRank >= 11 && PlayRank <= 20)
                    {
                        MoneyPresinal = 10000;

                        MoneyGuild = 150;

                        _Player.Player.Prestige += 4;
                        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tống Kim, uy danh tăng từ {2} lên {3}", _Player.Player.RoleName, 4, oldPrestige, _Player.Player.Prestige));

                        //if (GameManager.IsKuaFuServer)
                        //{
                        //    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player.Player, 187, 1, 0, "SONGJINBATTLE", false, 1, false, Global.ConstGoodsEndTime))
                        //    {
                        //        PlayerManager.ShowNotification(_Player.Player, "Có lỗi khi nhận phần thưởng");
                        //    }

                        //    KTGlobal.AddMoney(_Player.Player, 20000, MoneyType.BacKhoa, "SONGJINBATTLE");
                        //}
                    }
                    else 
                    {
                        _Player.Player.Prestige += 2;
                        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tống Kim, uy danh tăng từ {2} lên {3}", _Player.Player.RoleName, 1, oldPrestige, _Player.Player.Prestige));
                    }

                    // Cộng tài sản cá nhân
                    if (MoneyPresinal > 0 && _Player.Player.GuildID > 0)
                    {
                        //Update tài sản bang hội
                        KTGlobal.AddMoney(_Player.Player, MoneyPresinal, MoneyType.GuildMoney, "BOSSVLCT");
                    }
                    // Cộng tài sản bang
                    if (MoneyGuild > 0 && _Player.Player.GuildID > 0)
                    {
                        //Update tài sản bang hội
                        KTGlobal.UpdateGuildMoney(MoneyGuild, _Player.Player.GuildID, _Player.Player);
                    }

                    var ExpRecvice = (long)GetExpReward(_Player.Player.m_Level, Score);

                    if (ExpRecvice > 0)
                    {
                        GameManager.ClientMgr.ProcessRoleExperience(_Player.Player, ExpRecvice, true, false, true, "BATTLE_SONG_JIN");
                    }

                    KTGlobal.AddRepute(_Player.Player, PointType, Point);

                    KTGlobal.AddMoney(_Player.Player, Money, MoneyType.BacKhoa, "BATTLE_SONG_JIN");

                    // Nhận xong set lại điểm cho thằng này về 0
                    _Player.IsReviceAward = true;
                }
                else
                {
                    PlayerManager.ShowMessageBox(_Player.Player, "Thông báo", "KHông đủ điều kiện để nhận thưởng");
                }
            }
        }

        #endregion Phần thưởng

        public void MovePlayerToSafeZone()
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;
                var _Find = _BattleConfig.Region.Where(x => x.RegionCamp == PlayerBattle.Camp).FirstOrDefault();

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    PlayerBattle.LastPoint = PlayerBattle.Player.CurrentPos;
                    if (_Find != null)
                    {
                        PlayerManager.Relive(PlayerBattle.Player, _BattleConfig.MapID, _Find.PosX, _Find.PosY, 100, 100, 100);
                    }
                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;

                var _Find = _BattleConfig.Region.Where(x => x.RegionCamp == PlayerBattle.Camp).FirstOrDefault();

                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    PlayerBattle.LastPoint = PlayerBattle.Player.CurrentPos;
                    if (_Find != null)
                    {
                        PlayerManager.Relive(PlayerBattle.Player, _BattleConfig.MapID, _Find.PosX, _Find.PosY, 100, 100, 100);
                    }
                }
            }

            var StartVerifyTeleport = VerifyTelePortAsync();
        }

        public async System.Threading.Tasks.Task VerifyTelePortAsync()
        {
            // LOOP 10 lần cho nó chắc
            for (var i = 0; i < 10; i++)
            {
                // Trong 60s nếu mà nhân vật chưa được mvoe ra thì thực hiện move lại
                if (BATTLESTATE == BattelStatus.STATUS_END && TimeUtil.NOW() - LastTick < 120000)
                {
                    foreach (var entry in SongCampRegister)
                    {
                        var PlayerBattle = entry.Value;

                        var _Find = _BattleConfig.Region.Where(x => x.RegionCamp == PlayerBattle.Camp).FirstOrDefault();

                        if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                        {
                            var Dist = KTGlobal.GetDistanceBetweenPoints(PlayerBattle.Player.CurrentPos, PlayerBattle.LastPoint);

                            if (Dist < 1000)
                            {
                                PlayerManager.Relive(PlayerBattle.Player, _BattleConfig.MapID, _Find.PosX, _Find.PosY, 100, 100, 100);
                            }
                        }
                    }

                    foreach (var entry in JinCampRegister)
                    {
                        var PlayerBattle = entry.Value;

                        var _Find = _BattleConfig.Region.Where(x => x.RegionCamp == PlayerBattle.Camp).FirstOrDefault();

                        if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                        {
                            var Dist = KTGlobal.GetDistanceBetweenPoints(PlayerBattle.Player.CurrentPos, PlayerBattle.LastPoint);

                            if (Dist < 1000)
                            {
                                PlayerManager.Relive(PlayerBattle.Player, _BattleConfig.MapID, _Find.PosX, _Find.PosY, 100, 100, 100);
                            }
                        }
                    }
                }

                // Nửa giây tick 1 phát
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(500));

                i++;
            }
        }

        /// <summary>
        /// Đưa tất cả người chơi ra ngoài
        /// </summary>
        public void MovePlayerOut()
        {
            foreach (var entry in SongCampRegister)
            {
                var PlayerBattle = entry.Value;

                // Nếu người chơi còn ở trong bản đồ
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    this.PlayChangeState(PlayerBattle.Player, 0);

                    KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.Player, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);

                    GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.Player, preMapCode, prePosX, prePosY, 0, false);

                    // Set lại camp cho người chơi về -1
                    PlayerBattle.Player.Camp = -1;
                    PlayerBattle.Player.TempTitle = "";
                    // set lại hủy phụ bản cho nv
                    PlayerBattle.Player.CurrentCopyMapID = -1;
                    // Xóa thông tin phụ bản cũ
                    KT_TCPHandler.RecordCopySceneInfoToDB(PlayerBattle.Player, -1, -1, -1, -1, -1);
                }
            }

            foreach (var entry in JinCampRegister)
            {
                var PlayerBattle = entry.Value;

                // Nếu người chơi còn ở trong bản đồ
                if (PlayerBattle.Player.CurrentMapCode == _BattleConfig.MapID)
                {
                    this.PlayChangeState(PlayerBattle.Player, 0);

                    KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.Player, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);

                    GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.Player, preMapCode, prePosX, prePosY, 0, false);

                    PlayerBattle.Player.Camp = -1;
                    PlayerBattle.Player.TempTitle = "";
                    // set lại hủy phụ bản cho nv
                    PlayerBattle.Player.CurrentCopyMapID = -1;
                    // Xóa thông tin phụ bản cũ
                    KT_TCPHandler.RecordCopySceneInfoToDB(PlayerBattle.Player, -1, -1, -1, -1, -1);
                }
            }
        }

        public void MoveAgianIfMising()
        {
            var objs = GameManager.ClientMgr.GetMapClients(_BattleConfig.MapID);
            if (objs != null)
            {
                // Duyệt danh sách
                foreach (var obj in objs)
                {
                    this.PlayChangeState(obj, 0);

                    KT_TCPHandler.GetPlayerCopySceneInfoFromDB(obj, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);
                    GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), obj, preMapCode, prePosX, prePosY, 0, false);

                    obj.Camp = -1;
                    obj.TempTitle = "";
                    // set lại hủy phụ bản cho nv
                    obj.CurrentCopyMapID = -1;
                    // Xóa thông tin phụ bản cũ
                    KT_TCPHandler.RecordCopySceneInfoToDB(obj, -1, -1, -1, -1, -1);
                }
            }
        }

        private void ProsecBattle(object sender, EventArgs e)
        {
            try
            {
                // Nếu chiến trường chưa bắt đầu
                if (BATTLESTATE == BattelStatus.STATUS_NULL)
                {
                    // Start a real registration phase so players and BOTs share the same state.
                    if (_BattleConfig.IgnoreScheduleForTesting && !IsGuildEvent &&
                        GameManager.ServerLineID != 99 &&
                        (_BattleConfig.DayOfMonth == null || _BattleConfig.DayOfMonth.Count == 0))
                    {
                        LastTick = TimeUtil.NOW();
                        BATTLESTATE = BattelStatus.STATUS_PREPARE;
                        LogManager.WriteLog(LogTypes.SongJinBattle,
                            "[" + _BattleConfig.BattleName + "] TestMode IgnoreScheduleForTesting=true ==> STATUS_PREPARE");
                        KTGlobal.SendSystemEventNotification(
                            "[" + _BattleConfig.BattleName + "] Đang mở báo danh thử nghiệm, thời gian còn: " +
                            _BattleConfig.RegisterDualtion / 60 + " phút.");
                        return;
                    }

                    if (_BattleConfig.DayOfMonth != null && _BattleConfig.DayOfMonth.Count > 0)
                    {
                        var Now = DateTime.Now;
                        // DayOfMonth dayOfMonth = _BattleConfig.DayOfMonth.Where(x => x.Day == Now.Day).FirstOrDefault();
                        foreach (var dayOfMonth in _BattleConfig.DayOfMonth)
                        // if (dayOfMonth != null)
                        {
                            var _RegisterDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, dayOfMonth.Hour, dayOfMonth.Minute, 0).AddSeconds(_BattleConfig.RegisterDualtion * -1);
                            if (Now.Hour == _RegisterDate.Hour && Now.Minute == _RegisterDate.Minute && Now.Second >= _RegisterDate.Second && BATTLESTATE == BattelStatus.STATUS_NULL)
                            {
                                LastTick = TimeUtil.NOW();

                                BATTLESTATE = BattelStatus.STATUS_PREPARE;

                                LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                                var msg = "Đại chiến Tống Kim sắp bắt đầu, hiện đang tiến hành báo danh, người muốn tham chiến hãy nhanh chóng tìm Quan Mộ Binh (chiến trường) hoặc dùng Chiêu Thư Tống Kim, thời gian báo danh còn: " + _BattleConfig.RegisterDualtion / 60 + " phút.";
                                if (IsGuildEvent || GameManager.ServerLineID == 99)
                                {
                                    msg = "Đại chiến Bang Hội Vô Song sắp bắt đầu, hiện đang tiến hành báo danh, người muốn tham chiến hãy nhanh chóng tìm NPC Báo Danh theo đúng Bang Hội để tham chiến. Thời gian báo danh còn: 15 phút.";
                                }
                                KTGlobal.SendSystemEventNotification(msg);
                            }
                        }    
                    }    
                    else
                    {
                        var DayOfWeek = _BattleConfig.DayOfWeek;

                        var Today = TimeUtil.GetWeekDay1To7(DateTime.Now);

                        if (DayOfWeek.Contains(Today))
                        {
                            var Now = DateTime.Now;

                            var NowHours = Now.Hour;

                            var FindNext = _BattleConfig.OpenTime.Where(x => x.Hours >= NowHours).FirstOrDefault();
                            if (FindNext != null)
                            {
                                var NextHour = FindNext.Hours;
                                var NextMin = FindNext.Minute;

                                var checkKuafua = false;
                                if (!String.IsNullOrEmpty(_BattleConfig.TimeOpenKuafua) && !GameManager.IsKuaFuServer)
                                {
                                    var timeOpenKuafua = _BattleConfig.TimeOpenKuafua.Split(';');

                                    foreach (var time in timeOpenKuafua)
                                    {
                                        var timeKuafua = time.Split('_');

                                        var dayKuafua = Convert.ToInt32(timeKuafua[0]);
                                        var hourKuafua = Convert.ToInt32(timeKuafua[1]);
                                        var minKuafua = Convert.ToInt32(timeKuafua[2]);

                                        if (Today == dayKuafua && NextHour == hourKuafua && NextMin == minKuafua)
                                        {
                                            checkKuafua = true;
                                            break;
                                        }
                                    }
                                }

                                // Neu trung voi thoi gian lsv Tk thi bo qua
                                if (checkKuafua)
                                {
                                    return;
                                }

                                var _RegisterDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, NextHour, NextMin, 0).AddSeconds(_BattleConfig.RegisterDualtion * -1);



                                if (Now.Hour == _RegisterDate.Hour && Now.Minute == _RegisterDate.Minute && Now.Second >= _RegisterDate.Second && BATTLESTATE == BattelStatus.STATUS_NULL)
                                {
                                    LastTick = TimeUtil.NOW();

                                    BATTLESTATE = BattelStatus.STATUS_PREPARE;

                                    LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                                    var msg = "Đại chiến Tống Kim sắp bắt đầu, hiện đang tiến hành báo danh, người muốn tham chiến hãy nhanh chóng tìm Quan Mộ Binh (chiến trường) hoặc dùng Chiêu Thư Tống Kim, thời gian báo danh còn: " + _BattleConfig.RegisterDualtion / 60 + " phút.";
                                    if (IsGuildEvent || GameManager.ServerLineID == 99)
                                    {
                                        msg = "Đại chiến Bang Hội Vô Song sắp bắt đầu, hiện đang tiến hành báo danh, người muốn tham chiến hãy nhanh chóng tìm NPC Báo Danh theo đúng Bang Hội để tham chiến. Thời gian báo danh còn: 15 phút";
                                    }
                                    KTGlobal.SendSystemEventNotification(msg);
                                }
                            }
                        }
                    }    
                }
                // Nếu là trạng thái chuẩn bị
                else if (BATTLESTATE == BattelStatus.STATUS_PREPARE)
                {
                    // Cứ 5s update thông tin 1 lần
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && BATTLESTATE == BattelStatus.STATUS_PREPARE)
                    {
                        LastNofity = TimeUtil.NOW();
                        UpdatePreading(true);
                    }

                    if (TimeUtil.NOW() >= LastTick + _BattleConfig.RegisterDualtion * 1000 && BATTLESTATE == BattelStatus.STATUS_PREPARE)
                    {
                        LastTick = TimeUtil.NOW();

                        if (JinCampRegister.Count + SongJinBotManager.ActiveCount(this, 20) >= 1 &&
                            SongCampRegister.Count + SongJinBotManager.ActiveCount(this, 10) >= 1)
                        {
                            BATTLESTATE = BattelStatus.STATUS_START;

                            if (IsGuildEvent || GameManager.ServerLineID == 99)
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường Bang Hội Vô Song đã chính thức bắt đầu.");
                            } else
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường Tống Kim [" + _BattleConfig.BattleName + "] đã chính thức bắt đầu.");
                            }

                            LastNofity = TimeUtil.NOW();

                            UpdateBattleNotify();

                            CreateMonster();
                        }
                        else
                        {
                            BATTLESTATE = BattelStatus.STATUS_NULL;

                            LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());

                            NotifyAllBattle("Thời gian chuẩn bị đã hết nhưng 2 bên Tống và Kim không tập hợp đủ số lượng quân nên chiến trường tạm hoãn giao chiến");
                            MovePlayerOut();
                            ResetBattle();
                        }
                    }
                }
                else if (BATTLESTATE == BattelStatus.STATUS_START || BATTLESTATE == BattelStatus.STATUS_PREPAREEND)
                {
                    //NOtify về mỗi 5s
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && (BATTLESTATE == BattelStatus.STATUS_START || BATTLESTATE == BattelStatus.STATUS_PREPAREEND))
                    {
                        LastNofity = TimeUtil.NOW();

                        SpamCamp();
                        ProtectBossBounds();
                        UpadateBXH();
                        UpdateBattleNotify();
                    }

                    //  THông báo còn 5p nữa kết thúc chiến trường và chuyển trạng thái về chuẩn bị kết thúc

                    if (TimeUtil.NOW() >= LastTick + ((_BattleConfig.BattleDualtion * 1000) - (5 * 60 * 1000)) && BATTLESTATE == BattelStatus.STATUS_START)
                    {
                        if (IsGuildEvent || GameManager.ServerLineID == 99)
                        {
                            KTGlobal.SendSystemEventNotification("Chỉ còn 5 phút nữa chiến trường Bang Hội Vô Song sẽ kết thúc,Các anh hùng hào kiệt hãy mau đẩy lui quân địch!");
                        }
                        else
                        {
                            KTGlobal.SendSystemEventNotification("Chỉ còn 5 phút nữa chiến trường Tống Kim sẽ kết thúc,Các anh hùng hào kiệt hãy mau đẩy lui quân địch!");
                        }

                        BATTLESTATE = BattelStatus.STATUS_PREPAREEND;

                        LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                    }

                    // Nếu mà chiến trường fax kết thúc
                    if (TimeUtil.NOW() >= LastTick + (_BattleConfig.BattleDualtion * 1000) && BATTLESTATE == BattelStatus.STATUS_PREPAREEND)
                    {
                        LastTick = TimeUtil.NOW();

                        // Gọi Update Bảng xếp hạng lần cuối
                        UpadateBXH();

                        if (IsGuildEvent || GameManager.ServerLineID == 99)
                        {
                            if (TotalSongScore > TotalJinScore)
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor("Bang Hội Vô Song", ColorType.Done) + "] đã kết thúc. " + GuildEvent_SongGroup + " thế như chẻ tre, thắng lợi tuyệt đối!");
                            }
                            else if (TotalJinScore > TotalSongScore)
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor("Bang Hội Vô Song", ColorType.Done) + "] đã kết thúc. " + GuildEvent_JinGroup + " thế như chẻ tre, thắng lợi tuyệt đối!.");
                            }
                            else
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor("Bang Hội Vô Song", ColorType.Done) + "] Hai bên bất phân thắng bại, chọn ngày đấu tiếp!");
                            }
                        } else {
                            if (TotalSongScore > TotalJinScore)
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor(_BattleConfig.BattleName, ColorType.Done) + "] đã kết thúc. Quân Tống thế như chẻ tre, thắng lợi tuyệt đối!");
                            }
                            else if (TotalJinScore > TotalSongScore)
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor(_BattleConfig.BattleName, ColorType.Done) + "] đã kết thúc. Quân Kim thế như chẻ tre, thắng lợi tuyệt đối!.");
                            }
                            else
                            {
                                KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor(_BattleConfig.BattleName, ColorType.Done) + "] Hai bên bất phân thắng bại, chọn ngày đấu tiếp!");
                            }
                        }

                        // Chuyển sang trạng thái end
                        BATTLESTATE = BattelStatus.STATUS_END;

                        // Chuyển người chơi về khu an toàn
                        this.MovePlayerToSafeZone();

                        // Xóa hết monster trên map
                        this.ClearMonster();

                        LastNofity = TimeUtil.NOW();

                        // Thông báo nhận thưởng tới người chơi
                        if (GameManager.ServerLineID != 99)
                            UpdateBattleNotifyAdward();

                        LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                    }
                }

                // Cho 1 phút để nhận thưởng làm ABC trong chiến trường
                else if (BATTLESTATE == BattelStatus.STATUS_END)
                {
                    // Cứ 5s check 1 lần xem có player đã thoát ra khỏi map tong kim chưa, nếu rồi thì trả lại data cho player
                    if(TimeUtil.NOW() - LastNofity >= 5 * 1000 && BATTLESTATE == BattelStatus.STATUS_END)
                        {
                            LastNofity = TimeUtil.NOW();
                        foreach (var entry in SongCampRegister)
                        {
                            var PlayerBattle = entry.Value;

                            if (PlayerBattle.Player.CurrentMapCode != _BattleConfig.MapID && PlayerBattle.Player.Camp != -1)
                            {
                                this.PlayChangeState(PlayerBattle.Player, 0);

                                var preMapCodeNew = PlayerBattle.Player.CurrentMapCode;
                                var prePosXNew = PlayerBattle.Player.PosX;
                                var prePosYNew = PlayerBattle.Player.PosY;

                                KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.Player, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);

                                GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.Player, preMapCodeNew, prePosXNew, prePosYNew, 0, false);

                                PlayerBattle.Player.Camp = -1;
                                PlayerBattle.Player.TempTitle = "";
                            }
                        }

                        foreach (var entry in JinCampRegister)
                        {
                            var PlayerBattle = entry.Value;

                            if (PlayerBattle.Player.CurrentMapCode != _BattleConfig.MapID && PlayerBattle.Player.Camp != -1)
                            {
                                this.PlayChangeState(PlayerBattle.Player, 0);

                                var preMapCodeNew = PlayerBattle.Player.CurrentMapCode;
                                var prePosXNew = PlayerBattle.Player.PosX;
                                var prePosYNew = PlayerBattle.Player.PosY;

                                KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.Player, out var copySceneID, out var preMapCode, out var prePosX, out var prePosY, out var copySceneCreateTicks);

                                GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.Player, preMapCodeNew, prePosXNew, prePosYNew, 0, false);

                                PlayerBattle.Player.Camp = -1;
                                PlayerBattle.Player.TempTitle = "";
                            }
                        }
                    }

                    // Cho người chơi 5p để nhận thưởng

                    if (TimeUtil.NOW() >= LastTick + (_BattleConfig.ReviceAwardDualtion * 1000) && BATTLESTATE == BattelStatus.STATUS_END)
                    {
                        LastTick = TimeUtil.NOW();

                        //Hết 5 phút thì chuyển người chơi ra khỏi map
                        MovePlayerOut();

                        // Chuyển sang trạng thái chờ clear
                        BATTLESTATE = BattelStatus.STATUS_CLEAR;

                        LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                    }
                }
                else if (BATTLESTATE == BattelStatus.STATUS_CLEAR)
                {
                    // Đợi tiếp 60s nữa và dọn lại chiến trường chuẩn bị cho lần sau
                    if (TimeUtil.NOW() >= LastTick + (60 * 1000) && BATTLESTATE == BattelStatus.STATUS_CLEAR)
                    {
                        MoveAgianIfMising();
                        ResetBattle();
                        BATTLESTATE = BattelStatus.STATUS_NULL;

                        LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "]==> " + ex.ToString());
            }
            finally
            {
                SongJinBotManager.Publish(this, _BattleConfig, IsGuildEvent || GameManager.ServerLineID == 99);
            }
        }

        public void ForceEndBattle()
        {
            LastTick = TimeUtil.NOW();

            // Gọi Update Bảng xếp hạng lần cuối
            UpadateBXH();

            if (IsGuildEvent || GameManager.ServerLineID == 99)
            {
                if (TotalSongScore > TotalJinScore)
                {
                    KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor("Bang Hội Vô Song", ColorType.Done) + "] đã kết thúc. " + GuildEvent_SongGroup + " thế như chẻ tre, thắng lợi tuyệt đối!");
                }
                else if (TotalJinScore > TotalSongScore)
                {
                    KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor("Bang Hội Vô Song", ColorType.Done) + "] đã kết thúc. " + GuildEvent_JinGroup + " thế như chẻ tre, thắng lợi tuyệt đối!.");
                }
                else
                {
                    KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor("Bang Hội Vô Song", ColorType.Done) + "] Hai bên bất phân thắng bại, chọn ngày đấu tiếp!");
                }
            } else {
                if (TotalSongScore > TotalJinScore)
                {
                    KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor(_BattleConfig.BattleName, ColorType.Done) + "] đã kết thúc. Quân Tống thế như chẻ tre, thắng lợi tuyệt đối!");
                }
                else if (TotalJinScore > TotalSongScore)
                {
                    KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor(_BattleConfig.BattleName, ColorType.Done) + "] đã kết thúc. Quân Kim thế như chẻ tre, thắng lợi tuyệt đối!.");
                }
                else
                {
                    KTGlobal.SendSystemEventNotification("Chiến trường [" + KTGlobal.CreateStringByColor(_BattleConfig.BattleName, ColorType.Done) + "] Hai bên bất phân thắng bại, chọn ngày đấu tiếp!");
                }
            }

            // Chuyển sang trạng thái end
            BATTLESTATE = BattelStatus.STATUS_END;

            // Chuyển người chơi về khu an toàn
            this.MovePlayerToSafeZone();

            // Xóa hết monster trên map
            this.ClearMonster();

            LastNofity = TimeUtil.NOW();

            // Tông báo nhận thưởng tới người chơi
            if (GameManager.ServerLineID != 99)
                UpdateBattleNotifyAdward();

            LogManager.WriteLog(LogTypes.SongJinBattle, "[" + _BattleConfig.BattleName + "] Battle Change State ==> " + BATTLESTATE.ToString());
        }

        #region Score

        /// <summary>
        /// Lấy ra điểm khi A Kill B
        /// </summary>
        /// <param name="KillRank"></param>
        /// <param name="BeKillRank"></param>
        /// <returns></returns>
        public int GetKillScore(int KillRank, int BeKillRank, bool IsMonster)
        {
            var SCORE = 75 + (BeKillRank - KillRank) * 15;

            if (IsMonster)
            {
                SCORE = SCORE / 2;
            }
            return SCORE;
        }

        public void UpdateTitleRank(KPlayer Player, int Rank)
        {
            var find = _BattleConfig.Rank.Where(x => x.RankID == Rank).FirstOrDefault();

            if (find != null)
            {
                string newTitle = "";
                if (Player.Camp == 10)
                {
                    if (GameManager.ServerLineID != 99)
                        newTitle = "<color=#" + find.Color + ">Tống " + find.RankTitle + "</color>";
                    else
                        newTitle = "<color=#" + find.Color + ">" + GuildEvent_SongGroup + " " + find.RankTitle + "</color>";
                }
                else if (Player.Camp == 20)
                {
                    if (GameManager.ServerLineID != 99)
                        newTitle = "<color=#" + find.Color + ">Kim " + find.RankTitle + "</color>";
                    else
                        newTitle = "<color=#" + find.Color + ">" + GuildEvent_JinGroup + " " + find.RankTitle + "</color>";
                }

                if (!string.IsNullOrEmpty(newTitle) && Player.TempTitle != newTitle)
                {
                    Player.TempTitle = newTitle;
                }
            }
        }

        public int GetRank(int Score)
        {
            var find = _BattleConfig.Rank.OrderByDescending(x => x.Score).Where(x => x.Score <= Score).FirstOrDefault();

            if (find != null)
            {
                return find.RankID;
            }
            else
            {
                return 1;
            }
        }

        public int GetMonsterRank(int MonsterID)
        {
            var find = _BattleConfig.MonsterRank.Where(x => x.MonsterID == MonsterID).FirstOrDefault();

            if (find != null)
            {
                return find.RankID;
            }
            else
            {
                return 1;
            }
        }

        #endregion Score

        #region KillEventProsecc

        public void processEvent(EventObject eventObject)
        {
            if (eventObject.getEventType() == (int)EventTypes.MonsterDead)
            {
                var obj = eventObject as MonsterDeadEventObject;
                var monster = obj.getMonster();
                var client = obj.getAttacker();
                PlayerKillMonster(client, monster);
            }
            else if (eventObject.getEventType() == (int)EventTypes.PlayerDead)
            {
                var playerDeadEvent = eventObject as PlayerDeadEventObject;
                if (null != playerDeadEvent)
                {
                    if (playerDeadEvent.Type == PlayerDeadEventTypes.ByRole)
                    {
                        OnKillRole(playerDeadEvent.getAttackerRole(), playerDeadEvent.getPlayer());
                    }
                }
            }
            else if (eventObject.getEventType() == (int)EventTypes.PlayEnterMap)
            {
                var PlayEvent = eventObject as PlayerEnterMap;
                if (null != PlayEvent)
                {
                    if (PlayEvent.player.Camp == 10 || PlayEvent.player.Camp == 20)
                    {
                        var SEC = (LastTick + _BattleConfig.RegisterDualtion * 1000) - TimeUtil.NOW();

                        var FinalSec = (int)(SEC / 1000);

                        OnChangeBattleState(PlayEvent.player, FinalSec);
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người này giết người chơi kia
        /// </summary>
        /// <param name="kPlayer1"></param>
        /// <param name="kPlayer2"></param>
        private void OnKillRole(KPlayer Kill, KPlayer Bekill)
        {
            // Nếu chiến trường đã bắt đầu
            if (BATTLESTATE == BattelStatus.STATUS_START || BATTLESTATE == BattelStatus.STATUS_PREPAREEND)
            {
                // Check xem có đang trong chiến trường không
                if ((Kill.Camp == 10 || Kill.Camp == 20) && (Bekill.Camp == 10 || Bekill.Camp == 20))
                {
                    BattlePlayer _FindKill = null;

                    if (Kill.m_Level >= _BattleConfig.MinLevel && Kill.m_Level < _BattleConfig.MaxLevel)
                    {
                        if (Kill.Camp == 10)
                        {
                            SongCampRegister.TryGetValue(Kill.RoleID, out _FindKill);
                        }
                        else if (Kill.Camp == 20)
                        {
                            JinCampRegister.TryGetValue(Kill.RoleID, out _FindKill);
                        }

                        BattlePlayer _FinBeKill = null;

                        if (Bekill.Camp == 10)
                        {
                            SongCampRegister.TryGetValue(Bekill.RoleID, out _FinBeKill);
                        }
                        else if (Bekill.Camp == 20)
                        {
                            JinCampRegister.TryGetValue(Bekill.RoleID, out _FinBeKill);
                        }

                        if (_FindKill != null && _FinBeKill != null)
                        {
                            GlobalEventSource.getInstance().fireEvent(new TongKimKillEventObject(Kill, Bekill));
                            
                            var KillCurenScore = _FindKill.Score;

                            var KillCurRank = _FindKill.Rank;

                            var BeKillCurRank = _FinBeKill.Rank;

                            var ScoreGet = GetKillScore(KillCurRank, BeKillCurRank, false);

                            var TotalScore = KillCurenScore + ScoreGet;

                            // Nếu giết liên tục 3 người
                            if (_FindKill.KillStreak + 1 > 3)
                            {
                                TotalScore = TotalScore + 150;
                            }

                            _FindKill.Kill = _FindKill.Kill + 1;

                            _FindKill.KillStreak = _FindKill.KillStreak + 1;
                            if (_FindKill.MaxKillStreak < _FindKill.KillStreak)
                            {
                                _FindKill.MaxKillStreak = _FindKill.KillStreak;
                            }
                            _FindKill.Score = TotalScore;
                            _FindKill.Rank = GetRank(TotalScore);

                            if (Kill.Camp == 10)
                            {
                                TotalSongScore += ScoreGet;
                            }
                            else if (Kill.Camp == 20)
                            {
                                TotalJinScore += ScoreGet;
                            }

                            // Reset lại KillStreak
                            _FinBeKill.KillStreak = 0;
                            _FinBeKill.BeKill = _FinBeKill.BeKill + 1;

                            UpdateTitleRank(_FindKill.Player, _FindKill.Rank);

                            // Gửi notify cho thằng giết
                            NotifySocreForPlayer(_FindKill, true);

                            // Gửi notify cho thằng bị giết
                            NotifySocreForPlayer(_FinBeKill, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update điểm khi người chơi giết monster
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        private void PlayerKillMonster(KPlayer client, Monster monster)
        {
            // Nếu chiến trường đã bắt đầu
            if (BATTLESTATE == BattelStatus.STATUS_START || BATTLESTATE == BattelStatus.STATUS_PREPAREEND)
            {
                // Check xem có đang trong chiến trường không
                if ((client.Camp == 10 || client.Camp == 20) && (monster.Camp == 10 || monster.Camp == 20))
                {
                    BattlePlayer _Find = null;

                    // Nếu mà đúng là chiến trường đang diễn ra
                    if (client.m_Level >= _BattleConfig.MinLevel && client.m_Level < _BattleConfig.MaxLevel)
                    {
                        if (client.Camp == 10)
                        {
                            SongCampRegister.TryGetValue(client.RoleID, out _Find);
                        }
                        else if (client.Camp == 20)
                        {
                            JinCampRegister.TryGetValue(client.RoleID, out _Find);
                        }

                        var CurRank = _Find.Rank;

                        var MonsterID = monster.MonsterInfo.ExtensionID;

                        var RankOfMon = GetMonsterRank(MonsterID);

                        var ScoreGet = GetKillScore(CurRank, RankOfMon, true);

                        _Find.Score = _Find.Score + ScoreGet;

                        _Find.Rank = GetRank(_Find.Score);

                        UpdateTitleRank(_Find.Player, _Find.Rank);

                        if (client.Camp == 10)
                        {
                            TotalSongScore += ScoreGet;
                        }
                        else if (client.Camp == 20)
                        {
                            TotalJinScore += ScoreGet;
                        }

                        NotifySocreForPlayer(_Find, false);

                        if (monster.MonsterInfo.ExtensionID == 2520 || monster.MonsterInfo.ExtensionID == 2514)
                        {
                            // giết soái
                            if (IsGuildEvent || GameManager.ServerLineID == 99)
                            {
                                // nếu là sự kiện bang hội vô song thì end luôn
                                var winnerGroup = "";
                                if (_Find.Camp == 10) 
                                    winnerGroup = GuildEvent_SongGroup;
                                else
                                    winnerGroup = GuildEvent_JinGroup;
                                var winnerMessage = KTGlobal.CreateStringByColor("Cấp Báo", ColorType.Importal) + " phe " + KTGlobal.CreateStringByColor(winnerGroup, ColorType.Accpect) + " dũng cảm đánh bại <color=yellow>Nguyên Soái</color>, giành chiến thắng.";
                                NotifyAllBattle(winnerMessage);    

                                if (client.Camp == 10)
                                {
                                    TotalSongScore += TotalSongScore + 30000000;
                                }
                                else if (client.Camp == 20)
                                {
                                    TotalJinScore += TotalJinScore + 30000000;
                                }

                                ForceEndBattle();
                                return;
                            }

                            var Notify = KTGlobal.CreateStringByColor("Cấp Báo", ColorType.Importal) + " phe " + KTGlobal.CreateStringByColor(Battel_SonJin_Manager.GetNameCamp(_Find.Camp), ColorType.Accpect) + " dũng cảm đánh bại <color=yellow>" + monster.MonsterInfo.Name + "</color>, toàn quân " + KTGlobal.CreateStringByColor(Battel_SonJin_Manager.GetNameCamp(_Find.Camp), ColorType.Accpect) + " nhận được 10.000 điểm tích lũy.";

                            NotifyAllBattle(Notify);

                            if (client.Camp == 10)
                            {
                                TotalSongScore = TotalSongScore + 10000;
                            }
                            else if (client.Camp == 20)
                            {
                                TotalJinScore = TotalJinScore + 10000;
                            }
                        }
                    }
                }
            }
        }

        #endregion KillEventProsecc
    }
}