using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GameServer.KiemThe.GameEvents.GuildWarManager
{

    /// <summary>
    /// Thực thể quản lý sự kiện trên 1 bản đồ
    /// </summary>
    public class GuildWarMap
    {
        /// <summary>
        /// Thông tin bản đồ ID
        /// </summary>
        public int MapId { get; set; }


        public bool IsFightMap { get; set; }

        /// <summary>
        /// Thông tin bản đồ
        /// </summary>
        public string MapName { get; set; }


        public GuildWardConfig _Config { get; set; }
        /// <summary>
        ///Tên bang hội
        /// </summary>
        public int BeLongGuild { get; set; } = -1;

        /// <summary>
        /// Tên bang hội
        /// </summary>
        public string BeLongGuildName { get; set; }

        private bool isNeedBoss = false;

        private bool _isKillBoss { get; set; }
        public bool isKillBoss
        {
            get => isNeedBoss ? _isKillBoss : true;
            set => _isKillBoss = value;
        }


        //Danh sách long trụ lãnh thổ
        public ConcurrentDictionary<int, Monster> AllBudding = new ConcurrentDictionary<int, Monster>();

        //Danh sách quái 
        public ConcurrentDictionary<int, Monster> AllSystemMonster = new ConcurrentDictionary<int, Monster>();
        /// <summary>
        /// Danh sách bang có thể đánh ở bản đồ này
        /// </summary>
        public List<int> TotalGuildCanJoinBattle { get; set; } = new List<int>();
        //Dict quản lý số điểm có trên bản đồ này để phân biết bang nào chiếm  ưu thế bang nào không chiếm ưu thế

        //MÁU QUÁI CHUẨN
        public double HPRate = 1;

        public GuildWarMap(GuildWardConfig Config)
        {
            
            this._Config = Config;
            this.isKillBoss = false;
        }


        /// <summary>
        /// Đếm số long trụ hiện tại của bang
        /// </summary>
        /// <param name="GuildID"></param>
        /// <returns></returns>
        public int CountGuildObjective(int GuildID)
        {

            int COUNT = AllBudding.Values.Where(x => x.Camp == GuildID).Count();


            return COUNT;

        }

        private bool CanReceiveMonsterScore(int GuildID)
        {
            if (_Config != null && ServerConfig.Instance.IsKuaFuBalance)
            {
                return true;
            }

            return CountGuildObjective(GuildID) > 0;
        }

        /// <summary>
        /// Update số điểm của bang hội này
        /// </summary>
        /// <param name="GuildID"></param>
        /// <param name="GuildName"></param>
        /// <param name="Score"></param>
        public void UpdateGuildScore(int GuildID, string GuildName, int Score)
        {
            // Nếu bang này mà không nằm trong danh sách được cộng điểm thì thôi
            if (!TotalGuildCanJoinBattle.Contains(GuildID))
            {
                return;
            }


            GuidWarManager.getInstance().UpdateGuildPoint(GuildID, GuildName, Score, this.MapId, this.MapName);
        }



        /// <summary>
        /// Tạo trụ khi sự kiện bắt đầu
        /// </summary>
        /// <param name="GuildID"></param>
        /// <param name="GuildName"></param>
        public void CreateBuildingWhenStart()
        {

            LogManager.WriteLog(LogTypes.GuildWarManager, "Create CreateBuildingWhenStart:" + MapName);

            try
            {
                OpenConfig FindConfig = null;

                if (this.IsFightMap)
                {
                    FindConfig = _Config.WarMapConfigs.Where(x => x.FightMapID == this.MapId).FirstOrDefault();
                }
                else
                {
                    FindConfig = _Config.WarMapConfigs.Where(x => x.MapID == this.MapId).FirstOrDefault();
                }


                if (FindConfig != null)
                {
                    // Duyệt tất cả quái để tạo
                    foreach (ObjectivePostion _Monster in FindConfig.ObjectPostion.Where(x => x.IsMonster == false))
                    {

                        CreateLongTru(_Monster, this.BeLongGuild, this.BeLongGuildName);
                    }


                    // Nếu bản đồ này là lần đầu tiên tranh đoạt thì tạo quái ra
                    if (this.BeLongGuild == -1)
                    {
                        foreach (ObjectivePostion _Monster in FindConfig.ObjectPostion.Where(x => x.IsMonster == true))
                        {
                            // Set cho quái CAMP 1000 để nó ko đánh trụ
                            CreateMonster(_Monster, 1000);
                        }

                    }
                    else // NẾu trụ đã có bang thì có lệ sẽ xuất hiện quân chiếm đóng
                    {
                        int RANDOM = KTGlobal.GetRandomNumber(0, 100);

                        // Có 30% có tỉ lệ có quái tấn công
                        if (RANDOM < 30)
                        {
                            foreach (ObjectivePostion _Monster in FindConfig.ObjectPostion.Where(x => x.IsMonster == true))
                            {
                                // Set cho quái CAMP 1000 để nó ko đánh trụ
                                CreateMonster(_Monster, 1000, true);
                            }

                        }

                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "Create CreateBuildingWhenStart:" + MapName + "==>END");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }


            try
            {
                //quản lý quái thường

                var monsterZoneList = GameManager.MonsterZoneMgr.GetMonsterZoneListByMapCode(this.MapId);
                foreach (var monsterZone in monsterZoneList)
                {
                    monsterZone.SetZoneCampForEvent(-1);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }


        public void CreateMonster(ObjectivePostion _Monster, int CAMP, bool needBoss = false)
        {

            try
            {
                this.isNeedBoss = needBoss;
                MonsterAIType aiType = MonsterAIType.Special_Boss;

                /// Ngũ hành
                KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;

                int RandomSeri = KTGlobal.GetRandomNumber(1, 5);
                series = (KE_SERIES_TYPE)RandomSeri;

                /// Hướng quay
                KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;

                int RandomDir = KTGlobal.GetRandomNumber(0, 7);

                dir = (KiemThe.Entities.Direction)RandomDir;


                string TITLE = "";


                int Random = new Random().Next(0, 100);

                // nếu đây là boss thì có tỉ lệ ra hoặc không ra
                if (_Monster.ID == 3408 || _Monster.ID == 3409 || _Monster.ID == 3410 || _Monster.ID == 3411 || _Monster.ID == 3412)
                {
                    // Nếu như tỉ lệ mà random mà nhỏ hơn 30 thì đéo ra boss
                    if (needBoss)
                    {
                        int RANODMTIMERESPAN = new Random().Next(300000, 900000);

                        var Funtion = KTKTAsyncTask.Instance.ScheduleExecuteAsync(new DelayFuntionAsyncTask("CreateMonster", new Action(() => CreateMonsterByDelay(_Monster, CAMP))), RANODMTIMERESPAN);
                    }
                    else
                    {
                        if (Random < 30)
                        {
                            return;
                        }
                        else
                        {
                            int RANODMTIMERESPAN = new Random().Next(300000, 900000);

                            var Funtion = KTKTAsyncTask.Instance.ScheduleExecuteAsync(new DelayFuntionAsyncTask("CreateMonster", new Action(() => CreateMonsterByDelay(_Monster, CAMP))), RANODMTIMERESPAN);
                        }
                    }
                }
                else
                {

                    //double BaseHP = 40385163 * HPRate;
                    double BaseHP = _Monster.BaseHP * HPRate;
                    // BaseHP = 100000;

                    //LogManager.WriteLog(LogTypes.GuildWarManager, "[" + this.MapId + "]CREATE MONSTER VOI CAMP :" + CAMP);
                    /// AIType
                    // Cứ 20s lại mọc lại 1 con
                    GameManager.MonsterZoneMgr.AddDynamicMonsters(this.MapId, _Monster.ID, -1, 1, _Monster.Posx, _Monster.PosY, _Monster.Name, TITLE, (int)BaseHP, 90, dir, series, aiType, 20000, -1, 100001, "", (monster) =>{
                        int IDLOCALTION = _Monster.Posx * _Monster.PosY;

                        if (this.AllSystemMonster.ContainsKey(IDLOCALTION))
                        {
                            AllSystemMonster[IDLOCALTION] = monster;
                        }
                        else
                        {
                            AllSystemMonster.TryAdd(IDLOCALTION, monster);
                        }
                        if (!string.IsNullOrEmpty(monster.MonsterInfo.Auras))
                        {
                            foreach (string skillString in monster.MonsterInfo.Auras.Split(';'))
                            {
                                string[] fields = skillString.Split('_');
                                int skillID = int.Parse(fields[0]);
                                int skilllevel = int.Parse(fields[1]);
                                int cooldown = int.Parse(fields[2]);

                                SkillDataEx skillData = KSkill.GetSkillData(skillID);
                                SkillLevelRef skill = new SkillLevelRef()
                                {
                                    Data = skillData,
                                    AddedLevel = skilllevel,
                                    Exp = cooldown,
                                };
                                monster.UseSkill(skill.SkillID, skill.Level, monster);
                            }
                        }
                        
                    }, CAMP, new Func<bool>(() =>
                   {

                       if (GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_START)
                       {
                           return true;
                       }
                       else
                       {
                           return false;
                       }
                   }),
                    (_GameObject) =>
                     {
                         if (GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_START || GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
                         {
                             if (_GameObject is KPlayer)
                             {
                                 KPlayer _Player = (KPlayer)_GameObject;
                                 int guildWarGuildId = GuidWarManager.getInstance().GetGuildWarGuildId(_Player);
                                 string guildWarGuildName = GuidWarManager.getInstance().GetGuildWarGuildName(_Player);

                                 if (CanReceiveMonsterScore(guildWarGuildId))
                                 {
                                     if (_Player.TeamID != -1)
                                     {

                                         KPlayer _Lead = KTTeamManager.GetTeamLeader(_Player.TeamID);

                                         List<KPlayer> TotalMember = _Player.Teammates;

                                         foreach (KPlayer member in TotalMember)
                                         {
                                             if (!Global.InCircle(new Point(member.PosX, member.PosY), _Player.CurrentPos, 1000))
                                             {
                                                 continue;
                                             }
                                             // Cộng cho mỗi thằng 100 điểm
                                             GuidWarManager.getInstance().UpdateScore(member, _Monster.KillPoint, 0, 0, false);
                                         }


                                        // string MSG = "Tổ đội của " + KTGlobal.CreateStringByColor(_Lead.RoleName, ColorType.Yellow) + " đã có công đánh bại" + KTGlobal.CreateStringByColor(_Monster.Name, ColorType.Yellow);

                                         //KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player.GuildID, MSG, null, "");

                                     }
                                     else
                                     {
                                         //string MSG = "Người chơi " + KTGlobal.CreateStringByColor(_Player.RoleName, ColorType.Yellow) + " đã có công đánh bại" + KTGlobal.CreateStringByColor(_Monster.Name, ColorType.Yellow);

                                         //KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player.GuildID, MSG, null, "");

                                         // Cộng cho thằng latst hit trụ 100 điểm

                                         GuidWarManager.getInstance().UpdateScore(_Player, _Monster.KillPoint, 0, 0, false);

                                     }
                                     this.UpdateGuildScore(guildWarGuildId, guildWarGuildName, _Monster.KillPoint);
                                     // Cứ mỗi lần giết con NPC sẽ được 37 điểm
                                     //GuidWarManager.getInstance().UpdateScore(_Player, 37, 0, 0, false);

                                     //string NotifyKillBoss = "Chúc mừng Bang hội của bạn đã đánh bại <color=yellow>" + _Monster.Name + "</color>";

                                     // Thông báo cho toàn bộ các bang tham chiến biết thôn tin về boss lãnh thổ
                                     //KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player.GuildID, NotifyProtect, null, "");
                                 }
                             }
                         }

                     });
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }


        public void CreateMonsterByDelay(ObjectivePostion _Monster, int CAMP)
        {

            try
            {
                MonsterAIType aiType = MonsterAIType.Special_Boss;

                /// Ngũ hành
                KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;


                int RandomSeri = KTGlobal.GetRandomNumber(1, 5);

                series = (KE_SERIES_TYPE)RandomSeri;

                /// Hướng quay
                KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;

                int RandomDir = KTGlobal.GetRandomNumber(0, 7);

                dir = (KiemThe.Entities.Direction)RandomDir;


                string TITLE = "";


                //if (BeLongGuild == -1)
                //{
                //    //  TITLE = "<b><color=#00ff2a>Hệ Thống</color></b>";


                //}
                //else
                //{
                //    CAMP = BeLongGuild;
                //    // TITLE = "<b><color=#00ff2a>" + this.BeLongGuildName + "</color></b>";

                //}


                string NotifyProtect = "Boss lãnh thổ <b>[" + _Monster.Name + "]</b> đã xuất hiện tại <color=yellow>" + this.MapName + "</color> hãy mau chóng tiêu diệt!";

                // Thông báo cho toàn bộ các bang tham chiến biết thôn tin về boss lãnh thổ
                foreach (int GUILD in TotalGuildCanJoinBattle)
                {
                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, GUILD, NotifyProtect, null, "");
                }



                //double BaseHP = 40385163 * HPRate * 10;
                double BaseHP = _Monster.BaseHP * HPRate;
                // BaseHP = 100000;
                Monster thisBoss = null;
                GameManager.MonsterZoneMgr.AddDynamicMonsters(this.MapId, _Monster.ID, -1, 1, _Monster.Posx, _Monster.PosY, _Monster.Name, TITLE, (int)BaseHP, 90, dir, series, aiType, -1, -1, 100001, "", (monster) =>
                {
                    int IDLOCALTION = _Monster.Posx * _Monster.PosY;

                    if (this.AllSystemMonster.ContainsKey(IDLOCALTION))
                    {
                        AllSystemMonster[IDLOCALTION] = monster;
                    }
                    else
                    {
                        AllSystemMonster.TryAdd(IDLOCALTION, monster);
                    }

                    if (!string.IsNullOrEmpty(monster.MonsterInfo.Auras))
                    {
                        foreach (string skillString in monster.MonsterInfo.Auras.Split(';'))
                        {
                            string[] fields = skillString.Split('_');
                            int skillID = int.Parse(fields[0]);
                            int skilllevel = int.Parse(fields[1]);

                            SkillDataEx skillData = KSkill.GetSkillData(skillID);
                            SkillLevelRef skill = new SkillLevelRef()
                            {
                                Data = skillData,
                                AddedLevel = skilllevel,
                            };
                            monster.UseSkill(skill.SkillID, skill.Level, monster);
                        }
                    }

                    thisBoss = monster;
                }, CAMP, null,
                 (_GameObject) =>
                 {
                     if (GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_START || GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
                     {

                         this.isKillBoss = true;
                         if (_GameObject is KPlayer)
                         {
                             KPlayer _Player = (KPlayer)_GameObject;
                             int guildWarGuildId = GuidWarManager.getInstance().GetGuildWarGuildId(_Player);
                             string guildWarGuildName = GuidWarManager.getInstance().GetGuildWarGuildName(_Player);

                             if (CanReceiveMonsterScore(guildWarGuildId))
                             {
                                 if (_Player.TeamID != -1)
                                 {

                                     KPlayer _Lead = KTTeamManager.GetTeamLeader(_Player.TeamID);

                                     List<KPlayer> TotalMember = _Player.Teammates;

                                     foreach (KPlayer member in TotalMember)
                                     {
                                         if (Global.InCircle(new Point(member.PosX, member.PosY), _Player.CurrentPos, 1000) && member.CurrentMapCode == _Player.CurrentMapCode)
                                         {
                                             // Cộng cho mỗi thằng 100 điểm
                                             GuidWarManager.getInstance().UpdateScore(member, _Monster.KillPoint, 0, 0, false);
                                         }
                                        
                                     }


                                     string MSG = "Tổ đội của " + KTGlobal.CreateStringByColor(_Lead.RoleName, ColorType.Yellow) + " đã có công đánh bại" + KTGlobal.CreateStringByColor(_Monster.Name, ColorType.Yellow);

                                     KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player.GuildID, MSG, null, "");

                                 }
                                 else
                                 {
                                     string MSG = "Người chơi " + KTGlobal.CreateStringByColor(_Player.RoleName, ColorType.Yellow) + " đã có công đánh bại" + KTGlobal.CreateStringByColor(_Monster.Name, ColorType.Yellow);

                                     KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player.GuildID, MSG, null, "");

                                     // Cộng cho thằng latst hit trụ 100 điểm

                                     GuidWarManager.getInstance().UpdateScore(_Player, _Monster.KillPoint, 0, 0, false);

                                 }
                                 this.UpdateGuildScore(guildWarGuildId, guildWarGuildName, _Monster.KillPoint);
                                 
                             }

                             {
                                 List<ItemData> dropItems = new List<ItemData>();
                                 ItemData itemData = ItemManager.GetItemTemplate(591);

                                 itemData.BindType = 0;

                                 for (int i = 0; i < 10; i++)
                                 {
                                     dropItems.Add(itemData);
                                 }


                                 /// Nếu tồn tại danh sách vật phẩm sẽ rơi
                                 if (dropItems.Count > 0)
                                 {
                                     /// Tạo vật phẩm rơi dưới đất tương ứng
                                     ItemDropManager.CreateDropToMap(dropItems, thisBoss);

                                     dropItems.Clear();
                                 }
                             }
                         }
                     }

                 });
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }
        /// <summary>
        /// Thưởng EXP cho người chơi khi bảo vệ long trụ
        /// </summary>
        public void UpdateEXP()
        {

            try
            {
                foreach (Monster _Monster in AllBudding.Values.ToList())
                {
                    List<KPlayer> objs = GameManager.ClientMgr.GetMapClients(this.MapId);

                    if (objs != null)
                    {
                        /// Duyệt danh sách
                        foreach (KPlayer obj in objs)
                        {
                            if (obj.GuildID == _Monster.Camp)
                            {
                                string NotifyProtect = "Ngươi có công bảo vệ long trụ, nhận được <color=yellow>5</color> điểm tích lũy";

                                PlayerManager.ShowNotification(obj, NotifyProtect);

                                //GameManager.ClientMgr.ProcessRoleExperience(_Player, 16000, true, false, true, "TDLT");

                                GuidWarManager.getInstance().UpdateScore(obj, 5, 0, 0, false);
                            }
                        }
                    }


                    // Ông nào đứng gần trụ
                    //List<KPlayer> friends = KTLogic.GetNearByPeacePlayersReductCost(_Monster, 500);

                    //foreach (KPlayer _Player in friends)
                    //{

                    //    if (_Player.GuildID == _Monster.Camp)
                    //    {
                    //        //string NotifyProtect = "Ngươi có công bảo vệ long trụ, nhận được <color=yellow>5</color> điểm tích lũy và 1600 EXP";
                    //        string NotifyProtect = "Ngươi có công bảo vệ long trụ, nhận được <color=yellow>5</color> điểm tích lũy";

                    //        PlayerManager.ShowNotification(_Player, NotifyProtect);

                    //        //GameManager.ClientMgr.ProcessRoleExperience(_Player, 16000, true, false, true, "TDLT");

                    //        GuidWarManager.getInstance().UpdateScore(_Player, 5, 0, 0, false);

                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }




        /// <summary>
        /// Tạo ra cột khi bắt đầu
        /// </summary>
        /// <param name="_Monster"></param>
        /// <param name="IsBoss"></param>
        public void CreateLongTru(ObjectivePostion _Monster, int _BelongGuid, string _GuildName)
        {
            try
            {
                MonsterAIType aiType = MonsterAIType.Special_Boss;

                /// Ngũ hành
                KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;

                int RandomSeri = KTGlobal.GetRandomNumber(1, 5);
                series = (KE_SERIES_TYPE)RandomSeri;

                /// Hướng quay
                KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;

                int RandomDir = KTGlobal.GetRandomNumber(0, 7);

                dir = (KiemThe.Entities.Direction)RandomDir;

                string TITLE = "";

                int CAMP = 1000;

                // Nếu không phỉa
                if (_BelongGuid == -1)
                {
                    // Nếu ko thì set ttitle là hệ thống
                    TITLE = "<b><color=#00ff2a>Long Trụ Hệ Thống</color></b>";

                }
                else
                {
                    //Nếu trụ này thuộc về bang nào đó
                    CAMP = _BelongGuid;
                    TITLE = "<b><color=#00ff2a>" + _GuildName + "</color></b>";
                }

                // FIx máu trụ
                //double BaseHP = (453755700 / 2) * HPRate;
                double BaseHP = _Monster.BaseHP * HPRate;
                // BaseHP = 100000;
                /// AIType

                // Console.Write(this);

                //LogManager.WriteLog(LogTypes.GuildWarManager, "[" + this.MapId + "]CREATE LONG TRU VOI CAMP :" + CAMP);

                GameManager.MonsterZoneMgr.AddDynamicMonsters(this.MapId, _Monster.ID, -1, 1, _Monster.Posx, _Monster.PosY, _Monster.Name, TITLE, (int)BaseHP, 90, dir, series, aiType, -1, -1, -1, "", (monster) =>
                {

                    int IDLOCALTION = _Monster.Posx * _Monster.PosY;

                    if (AllBudding.ContainsKey(IDLOCALTION))
                    {
                        AllBudding[IDLOCALTION] = monster;
                    }
                    else
                    {
                        AllBudding.TryAdd(IDLOCALTION, monster);
                    }

                    monster.IsStatic = true;
                    monster.LocalMapName = TITLE;
                    monster.UpdatePositionToLocalMapContinuously = true;
                    monster.m_IgnoreAllSeriesStates = true;

                }, CAMP, null, (_GameObject) =>
                {

                    if (GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_START || GuildWarManager.GuidWarManager.getInstance().BATTLESTATE == GUILDWARDSTATUS.STATUS_PREPAREEND)
                    {
                        // Gọi sự kiện khi trụ này toác

                        // Nếu trụ này bị người chơi đập vỡ thì lại tạo 1 long trụ mới với CAMP là camp của bang này
                        if (_GameObject is KPlayer)
                        {
                            KPlayer _Client = (KPlayer)_GameObject;
                            int guildWarGuildId = GuidWarManager.getInstance().GetGuildWarGuildId(_Client);
                            string guildWarGuildName = GuidWarManager.getInstance().GetGuildWarGuildName(_Client);


                            this.CreateLongTru(_Monster, guildWarGuildId, guildWarGuildName);

                            if (this.BeLongGuild != -1)
                            {

                                if (guildWarGuildId != this.BeLongGuild)
                                {
                                    string MSG = KTGlobal.CreateStringByColor("Long trụ tại lãnh thổ [" + this.MapName + "] đang bị bang [" + guildWarGuildName + "] chiếm mất , hãy mau chóng cử ngưỡi canh giữ!", ColorType.Importal);

                                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, this.BeLongGuild, MSG, null, "");
                                }

                            }

                            List<int> listAddScore = new List<int>();
                            if (_Client.TeamID != -1)
                            {

                                KPlayer _Lead = KTTeamManager.GetTeamLeader(_Client.TeamID);

                                List<KPlayer> TotalMember = _Client.Teammates;

                                foreach (KPlayer member in TotalMember)
                                {
                                    if (!Global.InCircle(new Point(member.PosX, member.PosY), _Client.CurrentPos, 1000))
                                    {
                                        continue;
                                    }
                                    // Cộng cho mỗi thằng 300 điểm
                                    GuidWarManager.getInstance().UpdateScore(member, _Monster.KillPoint, 1, 0, false);
                                    listAddScore.Add(member.RoleID);
                                }


                                string MSG = "Tổ đội của " + KTGlobal.CreateStringByColor(_Lead.RoleName, ColorType.Yellow) + " đã có công phá vỡ Long Trụ của địch";

                                KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Lead.GuildID, MSG, null, "");

                            }
                            else
                            {
                                string MSG = "Người chơi " + KTGlobal.CreateStringByColor(_Client.RoleName, ColorType.Yellow) + " đã có công phá vỡ Long Trụ của địch";

                                KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Client.GuildID, MSG, null, "");

                                // Cộng cho thằng latst hit trụ 500 điểm

                                GuidWarManager.getInstance().UpdateScore(_Client, _Monster.KillPoint, 1, 0, false);
                                listAddScore.Add(_Client.RoleID);

                            }


                            // Cộng cho bang này 100 điểm tích lũy
                            this.UpdateGuildScore(guildWarGuildId, guildWarGuildName, _Monster.KillPoint);

                            if (GameManager.IsKuaFuServer)
                            {
                                LogManager.WriteLog(
                                    LogTypes.GuildWarManager,
                                    string.Format(
                                        "[TDLT-LSV][LongTru] Phá Long Trụ. MapID={0}, MapName={1}, Pos=({2},{3}), OldCamp={4}, NewGuildID={5}, NewGuildName={6}, RoleID={7}, RoleName={8}, GuildScoreAdd={9}.",
                                        this.MapId,
                                        this.MapName,
                                        _Monster.Posx,
                                        _Monster.PosY,
                                        CAMP,
                                        guildWarGuildId,
                                        guildWarGuildName,
                                        _Client.RoleID,
                                        _Client.RoleName,
                                        _Monster.KillPoint));
                            }
                            
                            {   // cộng điểm cho toàn bộ người chơi cùng bang trong map
                                List<KPlayer> objs = GameManager.ClientMgr.GetMapClients(this.MapId);
                                
                                if (objs != null)
                                {
                                    /// Duyệt danh sách
                                    foreach (KPlayer obj in objs)
                                    {
                                        if (GuidWarManager.getInstance().GetGuildWarGuildId(obj) == guildWarGuildId && listAddScore.FindIndex(x => x == obj.RoleID) == -1)
                                        {
                                            GuidWarManager.getInstance().UpdateScore(obj, _Monster.KillPoint/2, 1, 0, false);
                                        }
                                    }
                                }
                            }

                        }
                        else if (_GameObject is Monster)
                        {
                            // Nếu là quái đập vỡ long trụ thì tạo 1 cái long trụ rỗng
                            Monster _Client = (Monster)_GameObject;

                            this.CreateLongTru(_Monster, -1, "");

                            if (this.BeLongGuild != -1)
                            {
                                //int COUNTLONGTRU = CountGuildObjective(this.BeLongGuild);

                                //if (COUNTLONGTRU <= 3)
                                {
                                    string MSG = KTGlobal.CreateStringByColor("Lãnh thổ [" + this.MapName + "] đã bị quân Lưu vong nơi đó phản công, hãy mau quay về canh giữ!", ColorType.Importal);

                                    KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, this.BeLongGuild, MSG, null, "");
                                }

                            }


                        }

                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }

        public void ClearAllMonster()
        {
            try
            {
                foreach (Monster monster in AllBudding.Values.ToList())
                {
                    monster.MonsterZoneNode?.DestroyMonster(monster);
                }

                foreach (Monster monster in AllSystemMonster.Values.ToList())
                {
                    monster.MonsterZoneNode?.DestroyMonster(monster);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }

            try
            {
                //quản lý quái thường

                var monsterZoneList = GameManager.MonsterZoneMgr.GetMonsterZoneListByMapCode(this.MapId);
                foreach (var monsterZone in monsterZoneList)
                {
                    monsterZone.ResetMonsterCamp();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }



        public void RemoveAllForbid()
        {

            try
            {
                List<KPlayer> objs = GameManager.ClientMgr.GetMapClients(this.MapId);

                if (objs != null)
                {
                    /// Duyệt danh sách
                    foreach (KPlayer obj in objs)
                    {
                        obj.ForbidUsingSkill = false;
                        obj.TempTitle = "";
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }


        public void ActiveAllStatusAndPKModelInMap()
        {
            try
            {
                List<KPlayer> objs = GameManager.ClientMgr.GetMapClients(this.MapId);
                LogManager.WriteLog(LogTypes.GuildWarManager, "[" + this.MapId + "][" + this.MapName + "] CALL RELOAD CAMP :" + objs.Count);

                if (objs != null)
                {
                    /// Duyệt danh sách
                    foreach (KPlayer obj in objs)
                    {
                        GuidWarManager.getInstance().OnEnterMap(obj);
                    }
                }

                LogManager.WriteLog(LogTypes.GuildWarManager, "[" + this.MapId + "][" + this.MapName + "] CALL RELOAD CAMP ENDDD :" + objs.Count);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.GuildWarManager, "BUG :" + ex.ToString());
            }
        }



    }
}
