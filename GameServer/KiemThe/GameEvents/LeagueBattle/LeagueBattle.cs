using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using GameServer.Server;
using KF.Client;
using KF.Contract.Data;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GameServer.KiemThe.GameEvents.LeagueBattle
{
    public class LeagueBattle
    {
        /// <summary>
        /// Sự kiện tick gần nhất
        /// </summary>
        public long LastTick { get; set; }

        public long LastExpTick { get; set; }

        public long LastCreateFlag { get; set; }

        /// <summary>
        /// Thời gian notify gần đây nhất
        /// </summary>
        public long LastNofity { get; set; }

        /// <summary>
        /// Trạng thái của trận PVP 1vs1
        /// </summary>
        public ROUNDSTATE _ROUNDSTATE { get; set; }

        /// <summary>
        /// Bảng xếp hạng PVP hỗn chiến sử dụng để send về client
        /// </summary>
        public LEAGUE_PVP_RANKING_INFO _RANK = new LEAGUE_PVP_RANKING_INFO();

        /// <summary>
        /// Tổng số người đăng ký PVP
        /// </summary>
        public ConcurrentDictionary<int, LeaguePlayer> PVPARENA = new ConcurrentDictionary<int, LeaguePlayer>();

        /// <summary>
        /// Bảng xếp hạng hỗn chiến các người chơi với nhau
        /// </summary>
        public List<LeaguePlayer> BattleLeagueRank = new List<LeaguePlayer>();

        /// <summary>
        /// Sơ đồ thi đấu lôi đài
        /// </summary>
        public List<ELIMINATION_INFO> ELIMINATION_TOTAL = new List<ELIMINATION_INFO>();

        /// <summary>
        /// Danh sách cược
        /// </summary>
        private ConcurrentDictionary<int, LeagueBet> ListBets = new ConcurrentDictionary<int, LeagueBet>();

        /// <summary>
        /// check là đã tạo bảng solo chưa
        /// </summary>
        private bool checkCreateBoardSolo { get; set; }

        public LeagueState BATTLESTATE { get; set; }

        /// <summary>
        /// Đã gửi thư quà lsv chưa
        /// </summary>
        private bool isMailSentKuaFu = false;

        /// <summary>
        /// Môn phái nào
        /// </summary>
        public int FactionID { get; set; }

        /// <summary>
        /// Lấy ra tên phái
        /// </summary>
        public string GetFactionName
        {
            get
            {
                return KFaction.GetName(this.FactionID);
            }
        }

        public int GetMapCode
        {
            get
            {
                return _Config.MapFactionList.Where(x => x.FactionID == this.FactionID).FirstOrDefault().MapId;
            }
        }

        public int GetSourceServerId(KPlayer client)
        {
            if (client == null) return GameManager.LocalServerId;
            if (GameManager.IsKuaFuServer)
            {
                Tmsk.Contract.KuaFuServerLoginData kuaFuLoginData = Global.GetClientKuaFuServerLoginData(client);
                if (kuaFuLoginData != null && kuaFuLoginData.ServerId > 0)
                {
                    return kuaFuLoginData.ServerId;
                }
            }
            return GameManager.LocalServerId;
        }

        public LeagueDef _Config { get; set; }

        public LeagueBattle(int FactionID, LeagueDef Def)
        {
            this.FactionID = FactionID;
            this._Config = Def;
        }

        #region ClearBattle

        public void ResetBattle()
        {
            LastTick = 0;
            LastExpTick = 0;
            LastCreateFlag = 0;
            LastNofity = 0;
            ListBets = new ConcurrentDictionary<int, LeagueBet>();
            _RANK = new LEAGUE_PVP_RANKING_INFO();

            BATTLESTATE = LeagueState.NOTHING;
            isMailSentKuaFu = false;
            // Làm sạch danh sách đăng ký
            PVPARENA = new ConcurrentDictionary<int, LeaguePlayer>();
            //  Làm sạch bảng xếp hạng
            BattleLeagueRank = new List<LeaguePlayer>();

            //Làm sạch Sơ đồ thi đấu
            ELIMINATION_TOTAL = new List<ELIMINATION_INFO>();

            _ROUNDSTATE = ROUNDSTATE.NONE;

            checkCreateBoardSolo = false;
        }

        #endregion ClearBattle

        public void Start()
        {
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("LeagueBattle_" + this.FactionID, ProsecBattle), 5 * 1000, 2000);
        }

        public bool showdown()
        {
            return true;
        }

        #region BattleGMCOMMAND

        public void ForceStartBattle()
        {
            ResetBattle();
            LastTick = TimeUtil.NOW();
            BATTLESTATE = LeagueState.SIGN_UP;

            KTGlobal.SendSystemEventNotification("Giải đấu đã mở, hãy đến gặp NPC Hoàng Thường tại Hoàng Thành Liên Server để vào ghi danh tham dự.");
        }

        public void ForceEndBattle()
        {
            // ResetBattle();
            LastTick = TimeUtil.NOW();
            BATTLESTATE = LeagueState.CHAMPION_AWARD;

            KTGlobal.SendSystemEventNotification("Giải đấu đã kết thúc.");
        }

        private bool IsContinuousKuaFuBalance()
        {
            return GameManager.IsKuaFuServer && _Config != null && ServerConfig.Instance.IsKuaFuBalance;
        }

        private bool IsOpenTime(DateTime now)
        {
            if (_Config == null || _Config.TimeBattle == null)
            {
                return false;
            }

            if (IsContinuousKuaFuBalance())
            {
                return now.TimeOfDay >= new TimeSpan(_Config.TimeBattle.Hours, _Config.TimeBattle.Minute, _Config.TimeBattle.Second);
            }

            return now.Hour == _Config.TimeBattle.Hours && now.Minute == _Config.TimeBattle.Minute && now.Second >= _Config.TimeBattle.Second;
        }

        #endregion BattleGMCOMMAND

        #region BattelTeleport

        public bool CheckExits(KPlayer Player)
        {
            if (this.GetMapCode == Player.MapCode)
            {
                return true;
            }

            if (PVPARENA.TryGetValue(Player.RoleID, out LeaguePlayer Facetion))
            {
                if (BATTLESTATE != LeagueState.NOTHING)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Revice hồi sinh
        /// </summary>
        /// <param name="Player"></param>
        public void Revice(KPlayer Player)
        {
            if (BATTLESTATE == LeagueState.NOTHING || BATTLESTATE == LeagueState.END)
            {
                KT_TCPHandler.GetPlayerCopySceneInfoFromDB(Player, out int copySceneID, out int preMapCode, out int prePosX, out int prePosY, out long copySceneCreateTicks);

                GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), Player, preMapCode, prePosX, prePosY, 0, false);
            }

            // Thực hiện hồi sinh trong PVP
            else if (BATTLESTATE == LeagueState.MELEE_ROUND_1 || BATTLESTATE == LeagueState.MELEE_ROUND_2)
            {
                int RandomSwapn = new Random().Next(0, _Config.PVPRandomRevice.Count);

                Postion _GetPostion = _Config.PVPRandomRevice[RandomSwapn];

                PlayerManager.Relive(Player, GetMapCode, _GetPostion.X, _GetPostion.Y, 100, 100, 100);
            }
            // Nếu chết trong trận solo thì đưa ra điểm hồi sinh
            else /*if(BATTLESTATE == FactionState.ELIMINATION_ROUND_1 || BATTLESTATE == FactionState.ELIMINATION_ROUND_2 || BATTLESTATE == FactionState.ELIMINATION_ROUND_3 || BATTLESTATE == FactionState.ELIMINATION_ROUND_4)*/
            {
                PVPARENA.TryGetValue(Player.RoleID, out LeaguePlayer _OUTPLAYER);
                if (_OUTPLAYER != null)
                {
                    // set camp cho thằng này = -1 và hòa bình
                    _OUTPLAYER.player.Camp = -1;
                    _OUTPLAYER.player.PKMode = (int)PKMode.Peace;
                }

                PlayerManager.Relive(Player, GetMapCode, _Config.GoIn.X, _Config.GoIn.Y, 100, 100, 100);
            }
        }

        /// <summary>
        /// Chuyển người chơi vào đấu trường
        /// </summary>
        /// <param name="KPlayer"></param>
        public void MovePlayerToArena(KPlayer KPlayer)
        {
            if (KPlayer.TeamID != -1)
            {
                KPlayer.LeaveTeam();
            }

            // xóa toàn bộ buff hiện có trừ thức ăn + chúc phúc
            foreach (BuffDataEx buff in KPlayer.Buffs.ListBuffs)
            {
                /// nếu là thuốc thì bỏ qua
                if (buff.Skill.SkillID >= 100020 && buff.Skill.SkillID <= 100028)
                {
                    continue;
                }
                /// nếu là buff chúc phúc thì bỏ qua
                if (buff.Skill.SkillID >= 876 && buff.Skill.SkillID <= 880)
                {
                    continue;
                }
                /// Nếu là liên server thì giữ lại các buff hỗ trợ (Hộ Giáp Phiến, Ngũ Hành Thạch, Ma Đao Thạch)
                if (GameManager.IsKuaFuServer)
                {
                    if (buff.Skill.SkillID == 385 || buff.Skill.SkillID == 386 || buff.Skill.SkillID == 387)
                    {
                        continue;
                    }
                }
                /// còn lại xóa hết
                KPlayer.Buffs.RemoveBuff(buff);
            }

            // activate lại trang bị và skill bị động
            KPlayer.DeactivateAndReactivateAuraPassiveAndEnchantSkills();
            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from MovePlayerToArena() in LeagueBattle.cs");
            KPlayer.GetPlayEquipBody().ClearAllEffecEquipBody();
            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from MovePlayerToArena() in LeagueBattle.cs");
            KPlayer.GetPlayEquipBody().AttackAllEquipBody();

            // Nếu là thời gian đăng ký hoặc thời gian nghỉ
            if (BATTLESTATE == LeagueState.SIGN_UP || BATTLESTATE == LeagueState.FREE_MELEE || BATTLESTATE == LeagueState.READY_ELIMINATION || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.CHAMPION_AWARD || BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
            {
                KPlayer.PKMode = (int)PKMode.Peace;

                KPlayer.Camp = -1;
                //Cấm dùng skill toàn bộ người chơi
                KPlayer.ForbidUsingSkill = true;
                KPlayer.StopAllActiveFights();

                PlayChangeState(KPlayer, 1);
                // Ghi lại vị trí trước khi vào
                KT_TCPHandler.RecordCopySceneInfoToDB(KPlayer, this.GetMapCode, KPlayer.CurrentMapCode, (int)KPlayer.CurrentPos.X, (int)KPlayer.CurrentPos.Y, -1);
                // Chuyển người chơi vào map
                GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), KPlayer, this.GetMapCode, _Config.GoIn.X, _Config.GoIn.Y, 0, false);
            } // Nếu đang là vòng hỗn chiến thì cho vào lại
            else if (BATTLESTATE == LeagueState.MELEE_ROUND_1 || BATTLESTATE == LeagueState.MELEE_ROUND_2)
            {
                KPlayer.PKMode = (int)PKMode.Custom;

                KPlayer.Camp = KPlayer.RoleID;
                //Cấm dùng skill toàn bộ người chơi
                KPlayer.ForbidUsingSkill = false;

                PlayChangeState(KPlayer, 1);

                List<Postion> RandomPost = _Config.PVPRandomRevice;

                int Random = new Random().Next(0, RandomPost.Count);

                Postion _Position = RandomPost[Random];

                GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), KPlayer, this.GetMapCode, _Position.X, _Position.Y, 0, false);

                
            }
        }

        #endregion BattelTeleport

        public bool startup()
        {
            ResetBattle();

            this.Start();
            return true;
        }

        #region Register

        public int Register(KPlayer player)
        {
            /// Hiện tại không có giải đấu
            if (BATTLESTATE == LeagueState.NOTHING || BATTLESTATE == LeagueState.END)
            {
                return -1;
            }
            else if ((int)BATTLESTATE == (int)LeagueState.SIGN_UP)
            {
                if (player.m_Level < _Config.MIN_LEVEL)
                {
                    return -3;
                }
                /// kiểm tra đủ tài phú chưa?
                if (player.GetTotalValue() / 10000L < 3000)
                {
                    return -4;
                }
                ///kiểm tra xem đeo phi phong kinh thế chưa?
                if (player.GoodsDataList != null)
                {
                    lock (player.GoodsDataList)
                    {
                        var mantle =
                            player.GoodsDataList.FirstOrDefault(x =>
                                x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE);
                        if (mantle == null)
                        {
                            return -5;
                        }

                        var template = ItemManager.GetItemTemplate(mantle.GoodsID);

                        if (template == null)
                        {
                            return -5;
                        }

                        if (template.ItemValue / 10000L < 900)
                        {
                            return -6;
                        }
                    }
                }

                if (player.TeamID != -1)
                {
                    return -7;
                }
                //Số người đăng ký đã đến mức giới hạn 400 người，không thể tiếp nhận đăng ký，mời bạn tham gia các hoạt động khác tại đây");
                if (PVPARENA.Count >= _Config.MAX_ATTEND_PLAYER)
                {
                    return -8;
                }

                if (PVPARENA.TryGetValue(player.RoleID, out LeaguePlayer Player))
                {
                    if (Player != null)
                    {
                        // Gán lại player cho FACTIONBATTLE
                        Player.player = player;
                        Player.SourceServerId = GetSourceServerId(player);
                        Player.RoleName = player.RoleName;

                        this.MovePlayerToArena(player);

                        GlobalEventSource.getInstance().fireEvent(new ThamGiaGiaiDauEventObject(player));

                        //SideTaskManager.getInstance().ProcessTask(player, (int)SideTaskName.GiaiDau);
                    }
                    return 1;
                }
                else
                {
                    // Đăng ký thành công
                    LeaguePlayer _Player = new LeaguePlayer();
                    _Player.nArenaId = 100;
                    _Player.nDeathCount = 0;
                    _Player.nScore = 0;
                    _Player.player = player;
                    _Player.Rank = 0;
                    _Player.TotalFlagCollect = 0;
                    _Player.AreadyGetAward = false;
                    _Player.ReviceTanNhanVuong = false;
                    _Player.DamgeRecore = 0;
                    // SET CHO WIN TRẬN NÀO = KO
                    _Player.IsWinRound = false;
                    _Player.SourceServerId = GetSourceServerId(player);
                    _Player.RoleName = player.RoleName;

                    PVPARENA.TryAdd(player.RoleID, _Player);

                    this.MovePlayerToArena(player);

                    GlobalEventSource.getInstance().fireEvent(new ThamGiaGiaiDauEventObject(player));

                    //SideTaskManager.getInstance().ProcessTask(player, (int)SideTaskName.GiaiDau);

                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Player {player.RoleName} [{player.RoleID}] : đăng ký tham gia giải đấu thành công");

                    return 0;
                }
            }
            else
            {
                if ((int)BATTLESTATE == (int)LeagueState.FREE_MELEE 
                    || (int)BATTLESTATE == (int)LeagueState.MELEE_ROUND_1 
                    || (int)BATTLESTATE == (int)LeagueState.MELEE_ROUND_2 
                    || (int)BATTLESTATE == (int)LeagueState.READY_ELIMINATION 
                    || (int)BATTLESTATE == (int)LeagueState.FREE_ELIMINATION_ROUND_1 
                    || (int)BATTLESTATE == (int)LeagueState.FREE_ELIMINATION_ROUND_2 
                    || (int)BATTLESTATE == (int)LeagueState.FREE_ELIMINATION_ROUND_3
                    || (int)BATTLESTATE == (int)LeagueState.ELIMINATION_ROUND_1 
                    || (int)BATTLESTATE == (int)LeagueState.ELIMINATION_ROUND_2 
                    || (int)BATTLESTATE == (int)LeagueState.ELIMINATION_ROUND_3 
                    || (int)BATTLESTATE == (int)LeagueState.ELIMINATION_ROUND_4
                    || (int)BATTLESTATE == (int)LeagueState.CHAMPION_AWARD)
                {
                    if (PVPARENA.TryGetValue(player.RoleID, out LeaguePlayer PlayerRegister))
                    {
                        if (PlayerRegister != null)
                        {
                            // Gán lại player cho FACTIONBATTLE
                            PlayerRegister.player = player;
                            PlayerRegister.DamgeRecore = 0;
                            PlayerRegister.SourceServerId = GetSourceServerId(player);
                            PlayerRegister.RoleName = player.RoleName;
                            this.MovePlayerToArena(player);

                            GlobalEventSource.getInstance().fireEvent(new ThamGiaGiaiDauEventObject(player));

                            //SideTaskManager.getInstance().ProcessTask(player, (int)SideTaskName.GiaiDau);
                        }
                        return 1;
                    }
                    else
                    {
                        if (IsLateKuaFuBalanceMeleeRegisterState())
                        {
                            return RegisterLateKuaFuBalanceMelee(player);
                        }
                        return -2;
                    }
                }  
                else
                {
                    return -2;
                }    
            }
            //if ((int)BATTLESTATE > (int)LeagueState.SIGN_UP && (int)BATTLESTATE < (int)LeagueState.ELIMINATION_ROUND_1 )
            //{
            //    
            //}
            //if ((int)BATTLESTATE >= (int)LeagueState.ELIMINATION_ROUND_1 && (int)BATTLESTATE != (int)LeagueState.)
            //{
            //    return -2;
            //}
            //return -1;
        }

        private bool IsLateKuaFuBalanceMeleeRegisterState()
        {
            return IsContinuousKuaFuBalance()
                && (BATTLESTATE == LeagueState.MELEE_ROUND_1
                    || BATTLESTATE == LeagueState.MELEE_ROUND_2);
        }

        private int RegisterLateKuaFuBalanceMelee(KPlayer player)
        {
            if (player.m_Level < _Config.MIN_LEVEL)
            {
                return -3;
            }

            if (player.GetTotalValue() / 10000L < 3000)
            {
                return -4;
            }

            if (player.GoodsDataList != null)
            {
                lock (player.GoodsDataList)
                {
                    var mantle =
                        player.GoodsDataList.FirstOrDefault(x =>
                            x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE);
                    if (mantle == null)
                    {
                        return -5;
                    }

                    var template = ItemManager.GetItemTemplate(mantle.GoodsID);

                    if (template == null)
                    {
                        return -5;
                    }

                    if (template.ItemValue / 10000L < 900)
                    {
                        return -6;
                    }
                }
            }

            if (player.TeamID != -1)
            {
                return -7;
            }

            if (PVPARENA.Count >= _Config.MAX_ATTEND_PLAYER)
            {
                return -8;
            }

            LeaguePlayer leaguePlayer = new LeaguePlayer();
            leaguePlayer.nArenaId = 100;
            leaguePlayer.nDeathCount = 0;
            leaguePlayer.nScore = 0;
            leaguePlayer.player = player;
            leaguePlayer.Rank = 0;
            leaguePlayer.TotalFlagCollect = 0;
            leaguePlayer.AreadyGetAward = false;
            leaguePlayer.ReviceTanNhanVuong = false;
            leaguePlayer.DamgeRecore = 0;
            leaguePlayer.IsWinRound = false;
            leaguePlayer.SourceServerId = GetSourceServerId(player);
            leaguePlayer.RoleName = player.RoleName;

            if (!PVPARENA.TryAdd(player.RoleID, leaguePlayer))
            {
                if (PVPARENA.TryGetValue(player.RoleID, out LeaguePlayer playerRegister) && playerRegister != null)
                {
                    playerRegister.player = player;
                    playerRegister.DamgeRecore = 0;
                    playerRegister.SourceServerId = GetSourceServerId(player);
                    playerRegister.RoleName = player.RoleName;
                    this.MovePlayerToArena(player);

                    GlobalEventSource.getInstance().fireEvent(new ThamGiaGiaiDauEventObject(player));
                }

                return 1;
            }

            this.MovePlayerToArena(player);

            GlobalEventSource.getInstance().fireEvent(new ThamGiaGiaiDauEventObject(player));

            LogManager.WriteLog(LogTypes.LeagueBattle, $"Player {player.RoleName} [{player.RoleID}] : dang ky muon LSV can bang trong hon chien thanh cong");

            return 0;
        }

        /// <summary>
        /// Swith State của người chơi sang 1 cái bảng mới
        /// </summary>
        /// <param name="Player"></param>
        /// <param name="State"></param>
        public void PlayChangeState(KPlayer Player, int State)
        {
            G2C_EventState _State = new G2C_EventState();

            _State.EventID = 2909;
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

        #endregion Register

        #region Notify Faction

        /// <summary>
        /// Update text về client
        /// </summary>
        /// <param name="IsNotiyText"></param>
        ///
        public void UpdatePreading()
        {
            long SEC = (LastTick + _Config.TimeAct.SIGN_UP_DULATION * 1000) - TimeUtil.NOW();

            int FinalSec = (int)(SEC / 1000);

            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                if (PlayerBattle.player.TeamID != -1)
                {
                    PlayerBattle.player.LeaveTeam();
                }

                if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                {
                    // Gửi thông báo về thời gian đăng ký còn bao lâu cho bọn nó biết
                    this.SendPreadingNotify(PlayerBattle.player, FinalSec, PVPARENA.Count, true);
                }
            }
        }

        public void UpdatePreadingPvpRound2()
        {
            long SEC = (LastTick + _Config.TimeAct.MELEE_PVP_FREE_DUALTION * 1000) - TimeUtil.NOW();

            int FinalSec = (int)(SEC / 1000);

            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                {
                    // Gửi thông báo về thời gian đăng ký còn bao lâu cho bọn nó biết
                    this.SendPreadingNotify(PlayerBattle.player, FinalSec, PVPARENA.Count, false);
                }
            }
        }

        #region AWARD_REVICE


        /// <summary>
        /// CHECK AWARD
        /// </summary>
        /// <param name="map"></param>
        /// <param name="npc"></param>
        /// <param name="client"></param>
        public void NpcQuanQuan(GameMap map, NPC npc, KPlayer client)
        {
            if (GameManager.IsKuaFuServer)
            {
                KNPCDialog dialogKuaFu = new KNPCDialog();
                dialogKuaFu.Text = "Chào bạn! Đây là Đấu Trường Võ Lâm Minh Chủ Liên Server. Toàn bộ phần thưởng (Thưởng Top, Thưởng Tham Gia) sẽ được hệ thống tự động phát thẳng về hòm thư của bạn ở <color=yellow>Máy Chủ Gốc</color> ngay khi giải đấu kết thúc. Bạn không cần và không thể nhận thủ công tại đây.";
                dialogKuaFu.Selections = new Dictionary<int, string>()
                {
                    { 1000, "Kết thúc đối thoại" },
                };
                dialogKuaFu.OnSelect = (cb) => { };
                KTNPCDialogManager.AddNPCDialog(dialogKuaFu);
                dialogKuaFu.Show(npc, client);
                return;
            }

            KNPCDialog dialog = new KNPCDialog();
            dialog.Text = "Quan Quân Võ Lâm Minh Chủ: Hãy chọn chức năng.";
            dialog.Selections = new Dictionary<int, string>()
            {
                { 1, "Nhận thưởng xếp hạng" },
                { 2, "Nhận thưởng cược" },
                { 3, "Nhận thưởng tham gia" },
                { 1000, "Kết thúc đối thoại" },
            };
            dialog.OnSelect = (cb) =>
            {
                if (cb.SelectID == 1000)
                {
                    return;
                }
                if (_Config == null || _Config.AwardLeagues == null || _Config.AwardLeagues.Count <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chưa thiết lập phần thưởng");
                    return;
                }
                if (BATTLESTATE != LeagueState.CHAMPION_AWARD)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chưa đến thời gian nhận thưởng");
                    return;
                }
                if (cb.SelectID == 2)
                {
                    this.GetAwardBetting(client);
                    return;
                }
                if (cb.SelectID == 3)
                {
                    this.GetAwardParticipation(client);
                    return;
                }
                if (cb.SelectID == 1)
                {
                    this.GetAwardTop(client);
                    return;
                }
                return;
            };
            KTNPCDialogManager.AddNPCDialog(dialog);
            dialog.Show(npc, client);
        }
 
        public void GetAwardParticipation(KPlayer player)
        {
            if (BATTLESTATE != LeagueState.CHAMPION_AWARD)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Chưa đến thời gian nhận thưởng");
                return;
            }
            PVPARENA.TryGetValue(player.RoleID, out LeaguePlayer join);
            if (join == null)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn không nằm trong danh sách tham gia");
                return;
            }
            if (join.ReceivedJoinAward)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã nhận thưởng tham gia trước đó");
                return;
            }
            List<GoodsData> goodsDatas = new List<GoodsData>();
            if (!string.IsNullOrEmpty(_Config.ParticipationReward))
            {
                string[] items = _Config.ParticipationReward.Split(';');
                foreach (string item in items)
                {
                    string[] parts = item.Split('_');
                    if (parts.Length >= 3)
                    {
                        GoodsData gd = new GoodsData();
                        gd.GoodsID = Int32.Parse(parts[0]);
                        gd.GCount = Int32.Parse(parts[1]);
                        gd.Binding = Int32.Parse(parts[2]);
                        goodsDatas.Add(gd);
                    }
                }
            }
            if (goodsDatas.Count <= 0)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không tìm thấy cấu hình thưởng tham gia");
                return;
            }
            KTGlobal.SendMail(goodsDatas, player.RoleID, null, "Giải đấu", "Phần thưởng tham gia Võ Lâm Minh Chủ", 0, 0);
            join.ReceivedJoinAward = true;
            LogManager.WriteLog(LogTypes.LeagueBattle, $"JoinAward: {player.RoleName}({player.RoleID}) reward={_Config.ParticipationReward}");
            PlayerManager.ShowMessageBox(player, "Thông báo", "Đã gửi phần thưởng tham gia qua thư");
        }
 
        public void GetAwardTop(KPlayer player)
        {
            if (_Config == null || _Config.AwardLeagues == null || _Config.AwardLeagues.Count <= 0)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Chưa thiết lập phần thưởng");
                return;
            }
            if (BATTLESTATE != LeagueState.CHAMPION_AWARD)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Chưa đến thời gian nhận thưởng");
                return;
            }
            PVPARENA.TryGetValue(player.RoleID, out LeaguePlayer self);
            if (self == null)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn không nằm trong danh sách tham gia");
                return;
            }
            if (self.AreadyGetAward)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã nhận thưởng trước đó");
                return;
            }
            int rank = -1;
            ELIMINATION_INFO winnerInfo = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
            if (winnerInfo != null && winnerInfo.WinThisRound != null && winnerInfo.WinThisRound.player.RoleID == player.RoleID)
            {
                rank = 1;
            }
            else if (self.BestElimateRank == 4 && self.IsWinRound == false)
            {
                rank = 2;
            }
            else if (self.BestElimateRank == 3)
            {
                rank = 4;
            }
            else if (self.BestElimateRank == 2)
            {
                rank = 8;
            }
            else if (self.BestElimateRank == 1)
            {
                rank = 16;
            }
            if (rank == -1)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn không có phần thưởng xếp hạng");
                return;
            }
            AwardLeague award = _Config.AwardLeagues.Where(x => x.FromTop <= rank && x.ToTop >= rank).FirstOrDefault();
            if (award == null || string.IsNullOrEmpty(award.ListItems))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không tìm thấy cấu hình phần thưởng");
                return;
            }
            List<GoodsData> goodsDatas = new List<GoodsData>();
            string[] listItems = award.ListItems.Split(';');
            foreach (string item in listItems)
            {
                string[] itemInfo = item.Split('_');
                GoodsData goodData = new GoodsData();
                goodData.GoodsID = Int32.Parse(itemInfo[0]);
                goodData.GCount = Int32.Parse(itemInfo[1]);
                goodData.Binding = Int32.Parse(itemInfo[2]);
                goodsDatas.Add(goodData);
            }
            string content = "Hệ thống gửi phần thưởng Giải đấu, bạn đạt top " + rank + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
            KTGlobal.SendMail(goodsDatas, player.RoleID, null, "Giải đấu", content, 0, 0);
            //if (rank == 1)
            //{
            //    player.AddRoleTitle(117);
            //}
            self.AreadyGetAward = true;
            LogManager.WriteLog(LogTypes.LeagueBattle, $"Trao thưởng top {rank} cho {player.RoleName} RoleID={player.RoleID} gồm {award.ListItems}");
            PlayerManager.ShowMessageBox(player, "Thông báo", "Đã gửi phần thưởng qua thư");
        }


        public void DoBetting(KPlayer player, string bettorName)
        {
            if (GameServer.KiemThe.GameEvents.KuaFuBalance.KuaFuBalanceManager.Instance.IsBalanceMap(player) || GameServer.KiemThe.GameEvents.KuaFuBalance.KuaFuBalanceManager.Instance.IsInBalance(player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Trong Liên Server Cân Bằng không thể thực hiện thao tác này!");
                return;
            }

            if (BATTLESTATE != LeagueState.READY_ELIMINATION)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Chưa đến thời gian để đặt cược");
                return;
            }
            if (BattleLeagueRank.Count < _Config.MIN_ATTEND_PLAYER)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Số lượng người chơi tham gia không đủ, không thể đặt cược");
                return;
            }
            /// xem đã đặt cược trước đó
            if (this.ListBets.TryGetValue(player.RoleID, out _))
            {
                /// ko cho dặt nữa
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã đặt cược trước đó!");
                return;
            }
            // xem bettorNanme dung ko?
            if (string.IsNullOrEmpty(bettorName))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn chưa chọn người để đặt cược");
                return;
            }
            int max = BattleLeagueRank.Count;
            if (max >= 16)
            {
                max = 16;
            }
            else if (max >= 8)
            {
                max = 8;
            }
            else if (max >= 4)
            {
                max = 4;
            }
            List<LeaguePlayer> Top16League = BattleLeagueRank.GetRange(0, max);
            LeaguePlayer bettor = Top16League.Where(x => x.player.RoleName == bettorName).FirstOrDefault();
            if (bettor == null)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", $"Người chơi này không được vào vòng trong, vui lòng chọn người chơi trong top {max}!");
                return;
            }
            /// gửi bảng về nhận thông tin số liệu
            string text = $"Nhập số lượng bạc LSV muốn cược cho {bettorName}!";
            PlayerManager.ShowInputNumberBox(player, text, (number) =>
            {
                if (BATTLESTATE != LeagueState.READY_ELIMINATION)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Không phải thời gian để đặt cược");
                    return;
                }
                if (number < 100000)
                {
                    PlayerManager.ShowMessageBox(player,"Thông báo", "Số bạc LSV cược ít nhất là 10 vạn");
                    return;
                }
                if (number > 10000000)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Số bạc LSV cược lớn nhất là 1000 vạn");
                    return;
                }
                if (number % 10000 != 0)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Số bạc LSV cược phải là bội của 1 vạn");
                    return;
                }    
                /// kiểm tra xem đủ số lượng bạc trong người ko
                if (KTGlobal.GetMoneyhave(player, (KiemThe.Entities.MoneyType.KuafuMoney)) >= number)
                {
                    /// trừ tiền
                    KTGlobal.SubMoney(player, number, (KiemThe.Entities.MoneyType.KuafuMoney), "LEAGUE_BATTLE");
                    /// ghi vào danh sách cược
                    LeagueBet leagueBet = new LeagueBet
                    {
                        BettorID = bettor.player.RoleID,
                        MoneyBet = number,
                        PlayerID = player.RoleID,
                        HasAward = true,
                    };
                    this.ListBets.TryAdd(player.RoleID, leagueBet);
                    PlayerManager.ShowMessageBox(player, "Thông báo", $"Bạn đã đặt cược thành công!");
                    // thêm log
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.RoleName} RoleID={player.RoleID} đã đặt cược {number} bạc LSV vào {bettor.player.RoleName} playerID={bettor.player.RoleID}");
                }
                else
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", $"Bạn không đủ {number} bạc!");
                    return;
                }
            });
        }

        #endregion AWARD_REVICE

        /// <summary>
        /// Thông báo thời gian đăng ký còn bao lâu
        /// </summary>
        /// <param name="Player"></param>
        /// <param name="Sec"></param>
        /// <param name="TotalTong"></param>
        /// <param name="TotalKim"></param>
        /// <param name="IsNotiyText"></param>
        public void SendPreadingNotify(KPlayer Player, int Sec, int TotalTong, bool IsFistRound)
        {
            ///Chuyển trạng thái cái bảng về cái bảng notify

            this.PlayChangeState(Player, 1);

            Player.ForbidUsingSkill = true;
            Player.Camp = 0;

            ///Show cái trạng thái
            G2C_EventNotification _Notify = new G2C_EventNotification();

            _Notify.EventName = "Pk tự do bắt đầu sau :";

            _Notify.ShortDetail = "TIME|" + Sec;

            _Notify.TotalInfo = new List<string>();
            if (IsFistRound)
            {
                _Notify.TotalInfo.Add("Số người đã báo danh :" + TotalTong);
            }
            else
            {
                _Notify.TotalInfo.Add("Lần hỗn chiến thứ :" + ((int)BATTLESTATE - 1));
            }

            if (Player.IsOnline())
            {
                Player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
            else
            {
                //Console.WriteLine("OFFLINE");
            }
        }

        #endregion Notify Faction

        #region BXH

        public void UpadateBXH()
        {
            lock (BattleLeagueRank)
            {
                List<LeaguePlayer> TEMP = new List<LeaguePlayer>();

                foreach (LeaguePlayer Player in PVPARENA.Values)
                {
                    TEMP.Add(Player);
                }

                //SOFT LẠI DANH SÁCH Theo danh sách giảm dần
                List<LeaguePlayer> _Soft = TEMP.OrderByDescending(x => x.nScore).ToList();

                // GÁN LẠI SOFT = DANH SÁCH MỚI ĐÃ SẮP XẾP
                BattleLeagueRank = _Soft;
            }
        }

        public int GetRankInBxh(int RoleID)
        {
            lock (BattleLeagueRank)
            {
                var find = BattleLeagueRank.FindIndex(x => x.player.RoleID == RoleID);

                return find;
            }
        }

        public LEAGUE_PVP_RANKING_INFO RankingBuilder(KPlayer Client)
        {
            LEAGUE_PVP_RANKING_INFO _RANK_BUILD = new LEAGUE_PVP_RANKING_INFO();

            if (BATTLESTATE < LeagueState.ELIMINATION_ROUND_1)
            {
                _RANK_BUILD.State = 0;
                _RANK_BUILD.ELIMINATION_SCORE = new List<LEAGUE_ELIMINATION_SCOREBOARD>();
                _RANK_BUILD.PlayerRanks = new List<LEAGUE_PVP_RANKING>();

                // LẤY RA TOP 50 THẰNG KHỎE NHẤT TRONG PVP HỖN CHIẾN
                List<LeaguePlayer> TOP50 = new List<LeaguePlayer>();
                if (BattleLeagueRank.Count > 50)
                {
                    TOP50 = BattleLeagueRank.GetRange(0, 49);
                }
                else
                {
                    TOP50 = BattleLeagueRank;
                }

                for (int i = 0; i < TOP50.Count; i++)
                {
                    LeaguePlayer _FACTION = TOP50[i];

                    LEAGUE_PVP_RANKING _Rank = new LEAGUE_PVP_RANKING();

                    _Rank.Faction = _FACTION.player.m_cPlayerFaction.GetFactionId();
                    _Rank.KillCount = _FACTION.nScore;
                    _Rank.Level = _FACTION.player.m_Level;
                    _Rank.MaxKillStreak = _FACTION.MaxKillStreak;
                    _Rank.PlayerName = _FACTION.player.RoleName;
                    _Rank.Rank = i + 1;
                    _Rank.Score = _FACTION.nScore;
                    _RANK_BUILD.PlayerRanks.Add(_Rank);
                }

                var findHost = BattleLeagueRank.Where(x => x.player.RoleID == Client.RoleID).FirstOrDefault();
                if (findHost != null)
                {
                    LEAGUE_PVP_RANKING _Rank = new LEAGUE_PVP_RANKING();

                    _Rank.Faction = findHost.player.m_cPlayerFaction.GetFactionId();
                    _Rank.KillCount = findHost.nScore;
                    _Rank.Level = findHost.player.m_Level;
                    _Rank.MaxKillStreak = findHost.KillStreak;
                    _Rank.PlayerName = findHost.player.RoleName;
                    _Rank.Rank = GetRankInBxh(findHost.player.RoleID) + 1;
                    _Rank.Score = findHost.nScore;
                    _RANK_BUILD.PlayerRanks.Add(_Rank);
                }
                else
                {
                    LEAGUE_PVP_RANKING _Rank = new LEAGUE_PVP_RANKING();

                    _Rank.Faction = Client.m_cPlayerFaction.GetFactionId();
                    _Rank.KillCount = 0;
                    _Rank.Level = Client.m_Level;
                    _Rank.MaxKillStreak = 0;
                    _Rank.PlayerName = Client.RoleName;
                    _Rank.Rank = -1;
                    _Rank.Score = 0;
                    _RANK_BUILD.PlayerRanks.Add(_Rank);
                }    
            }
            else
            {
                _RANK_BUILD.State = 1;
                _RANK_BUILD.ELIMINATION_SCORE = new List<LEAGUE_ELIMINATION_SCOREBOARD>();
                _RANK_BUILD.PlayerRanks = new List<LEAGUE_PVP_RANKING>();

                foreach (ELIMINATION_INFO _INFO in ELIMINATION_TOTAL)
                {
                    LEAGUE_ELIMINATION_SCOREBOARD _RANK = new LEAGUE_ELIMINATION_SCOREBOARD();

                    // Định nghĩa ranking

                    _RANK.ARENAID = _INFO.ARENAID;
                    _RANK.Player_1 = _INFO.Player_1.player.RoleName;
                    _RANK.Player_2 = _INFO.Player_2.player.RoleName;
                    _RANK.ROUNDID = _INFO.ROUNDID;
                    _RANK._ROUNDSTATE = _INFO._ROUNDSTATE;

                    _RANK_BUILD.ELIMINATION_SCORE.Add(_RANK);
                }
            }

            return _RANK_BUILD;
        }

        #endregion BXH

        /// <summary>
        /// Disable attack all
        /// </summary>
        private void DisableAttackAllRegister()
        {
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                PlayerBattle.player.PKMode = (int)PKMode.Peace;
                //// Set camp cho người chơi
                PlayerBattle.player.Camp = -1;

                // mở khóa sử dụng skill
                entry.Value.player.ForbidUsingSkill = true;

                /// Ngừng toàn bộ tuyên chiến
                PlayerBattle.player.StopAllActiveFights();
            }
        }

        /// <summary>
        /// Cho mỗi thằng 1 camp
        /// </summary>
        public void EnableAttackAllRegister()
        {
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                PlayerBattle.player.PKMode = (int)PKMode.Custom;
                //// Set camp cho người chơi
                PlayerBattle.player.Camp = PlayerBattle.player.RoleID;
                // mở khóa sử dụng skill
                entry.Value.player.ForbidUsingSkill = false;
                /// Ngừng toàn bộ tuyên chiến
                PlayerBattle.player.StopAllActiveFights();
            }
        }

        /// <summary>
        /// Xử lý các sự kiện sẽ xảy ra ở thi đấu môn phía
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProsecBattle(object sender, EventArgs e)
        {
            try
            {
                // Nếu chưa báo danh
                if (BATTLESTATE == LeagueState.NOTHING)
                {
                       
                    if (_Config.WEEK_OPEN == -1)
                    {
                        List<int> DayOfWeek = _Config.OPEN_WEEK_DATE;
                        int Today = TimeUtil.GetWeekDay1To7(DateTime.Now);

                        if (DayOfWeek.Contains(Today))
                        {
                            DateTime Now = DateTime.Now;

                            if (IsOpenTime(Now) && BATTLESTATE == LeagueState.NOTHING)
                            {
                                LastTick = TimeUtil.NOW();

                                BATTLESTATE = LeagueState.SIGN_UP;

                                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + GetFactionName + "] Battle Change State ==> " + BATTLESTATE.ToString());

                                KTGlobal.SendSystemEventNotification("Giải đấu đã mở, hãy đến gặp NPC Hoàng Thường tại Hoàng Thành Liên Server để vào ghi danh tham dự");
                            }
                        }
                    }
                    else if (_Config.WEEK_OPEN > 0)
                    {
                        List<int> DayOfWeek = _Config.OPEN_WEEK_DATE;

                        int WeekToDay = TimeUtil.GetWeekOfMonth(DateTime.Now);

                        if (_Config.WEEK_OPEN == WeekToDay)
                        {
                            int Today = TimeUtil.GetWeekDay1To7(DateTime.Now);

                            if (DayOfWeek.Contains(Today))
                            {
                                DateTime Now = DateTime.Now;

                                if (IsOpenTime(Now) && BATTLESTATE == LeagueState.NOTHING)
                                {
                                    LastTick = TimeUtil.NOW();

                                    BATTLESTATE = LeagueState.SIGN_UP;

                                    LogManager.WriteLog(LogTypes.LeagueBattle, "[" + GetFactionName + "] Battle Change State ==> " + BATTLESTATE.ToString());

                                    KTGlobal.SendSystemEventNotification("Giải đấu đã mở, hãy đến gặp NPC Hoàng Thường tại Hoàng Thành Liên Server để vào ghi danh tham dự");
                                }
                            }
                        }
                    }    
                    else
                    {                        
                        DateTime Now = DateTime.Now;
                        DateTime NowNextWeek = Now.AddDays(7);
                        
                        //đây là tuần cuối của thứ này
                        if (NowNextWeek.Month != Now.Month && _Config.OPEN_WEEK_DATE.Contains(TimeUtil.GetWeekDay1To7(Now)))
                        {
                            if (IsOpenTime(Now) && BATTLESTATE == LeagueState.NOTHING)
                            {
                                LastTick = TimeUtil.NOW();
                        
                                BATTLESTATE = LeagueState.SIGN_UP;
                        
                                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + GetFactionName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                        
                                KTGlobal.SendSystemEventNotification("Giải đấu đã mở, hãy đến gặp NPC Hoàng Thường tại Hoàng Thành Liên Server để vào ghi danh tham dự");
                            }
                        } 
                    }

                }

                // Nếu đang trong trạng thái đăng ký
                else if (BATTLESTATE == LeagueState.SIGN_UP)
                {
                    // Cứ 5s update notify register về toàn bộ bọn register 1 lần
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && BATTLESTATE == LeagueState.SIGN_UP)
                    {
                        DisableAttackAllRegister();

                        LastNofity = TimeUtil.NOW();

                        UpdatePreading();
                    }

                    if (TimeUtil.NOW() >= LastTick + _Config.TimeAct.SIGN_UP_DULATION * 1000 && BATTLESTATE == LeagueState.SIGN_UP)
                    {
                        LastTick = TimeUtil.NOW();

                        BATTLESTATE = LeagueState.MELEE_ROUND_1;

                        // Mở attack cho toàn bộ
                        EnableAttackAllRegister();

                        /// Chuyển toàn bộ người chơi vào ARENA
                        MoveAllPlayerToPvp();
                    }
                }
                else if (BATTLESTATE == LeagueState.MELEE_ROUND_1)
                {
                    //Cứ mỗi 5s notify về kết quả trận đấu
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && (BATTLESTATE == LeagueState.MELEE_ROUND_1))
                    {
                        LastNofity = TimeUtil.NOW();

                        UpdateBattleNotify();
                        UpadateBXH();
                    }

                    // Nếu kết thúc vòng 1
                    if (TimeUtil.NOW() >= LastTick + (_Config.TimeAct.MELEE_PVP_DULATION * 1000) && BATTLESTATE == LeagueState.MELEE_ROUND_1)
                    {
                        LastTick = TimeUtil.NOW();

                        // Chuyển sang trạng thái nghỉ ngơi giữa trận hỗn chiến
                        BATTLESTATE = LeagueState.FREE_MELEE;

                        // Gọi Update Bảng xếp hạng lần cuối
                        UpadateBXH();

                        DisableAttackAllRegister();

                        // Chuyển toàn bộ người chơi ra ngoài đợi tới hiệp tiếp theo
                        this.MoveAllPlayerOutPvp();

                        LastNofity = TimeUtil.NOW();

                        UpdatePreadingPvpRound2();

                        LogManager.WriteLog(LogTypes.LeagueBattle, "[" + GetFactionName + "] Battle Change State ==> " + BATTLESTATE.ToString());
                    }
                }
                else if (BATTLESTATE == LeagueState.FREE_MELEE)
                {
                    // Cứ 5s update notify register về toàn bộ bọn register 1 lần
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && BATTLESTATE == LeagueState.FREE_MELEE)
                    {
                        this.DisableAttackAllRegister();
                        LastNofity = TimeUtil.NOW();
                        UpdatePreadingPvpRound2();
                    }

                    if (TimeUtil.NOW() >= LastTick + _Config.TimeAct.MELEE_PVP_FREE_DUALTION * 1000 && BATTLESTATE == LeagueState.FREE_MELEE)
                    {
                        LastTick = TimeUtil.NOW();

                        BATTLESTATE = LeagueState.MELEE_ROUND_2;

                        /// Chuyển toàn bộ người chơi vào ARENA chod đấu lần 2
                        MoveAllPlayerToPvp();

                        EnableAttackAllRegister();
                    }
                }
                else if (BATTLESTATE == LeagueState.MELEE_ROUND_2) // Nếu như đang trong trận PVP tự do
                {
                    //Cứ mỗi 5s notify về kết quả trận đấu
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && (BATTLESTATE == LeagueState.MELEE_ROUND_2))
                    {
                        LastNofity = TimeUtil.NOW();

                        UpdateBattleNotify();

                        UpadateBXH();
                    }

                    // Nếu kết thúc vòng 2
                    if (TimeUtil.NOW() >= LastTick + (_Config.TimeAct.MELEE_PVP_DULATION * 1000) && BATTLESTATE == LeagueState.MELEE_ROUND_2)
                    {
                        LastTick = TimeUtil.NOW();

                        // Gọi Update Bảng xếp hạng lần cuối
                        UpadateBXH();

                        this.DisableAttackAllRegister();

                        // Chuyển sang trạng thái nhặt cờ chuẩn bị thi đấu các thứ
                        BATTLESTATE = LeagueState.READY_ELIMINATION;

                        // Chuyển toàn bộ người chơi ra ngoài đợi tới hiệp tiếp theo
                        this.MoveAllPlayerOutPvp();

                        LastNofity = TimeUtil.NOW();

                    }
                }
                // Nếu là thời gian nghỉ thì cho nhặt cờ
                else if (BATTLESTATE == LeagueState.READY_ELIMINATION || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_3)
                {
                    if (TimeUtil.NOW() - LastNofity >= 60 * 1000 && (BATTLESTATE == LeagueState.READY_ELIMINATION))
                    {
                        /// cho thời gian để người chơi đặt cược
                        KTGlobal.SendSystemEventNotification("Thời gian để đặt cược đã mở, người chơi mở nút BXH bên cạnh, đặt cược cho quán quân giải đấu lần này");
                        LastNofity = TimeUtil.NOW();
                        int idx = 0;
                        KPlayer player;
                        while ((player = GameManager.ClientMgr.GetNextClient(ref idx)) != null)
                        {
                            PlayChangeState(player, 1);
                        }
                        UpdateBettingNotify();
                    }

                    if ((TimeUtil.NOW() >= LastTick + (_Config.TimeAct.BETTING_DULATION * 1000) && BATTLESTATE == LeagueState.READY_ELIMINATION) || (TimeUtil.NOW() >= LastTick + (_Config.TimeAct.FREE_ELIMINATION * 1000) && (BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_3)))
                    {
                        int idx = 0;
                        KPlayer player;
                        while ((player = GameManager.ClientMgr.GetNextClient(ref idx)) != null)
                        {
                            if (BattleLeagueRank.Where(x => x.player.RoleID == player.RoleID).FirstOrDefault() == null && player.CurrentMapCode != 2222)
                            {
                                PlayChangeState(player, 0);
                            }
                        }
                        if (!checkCreateBoardSolo)
                        {
                            checkCreateBoardSolo = true;
                            // Thêm đoạn này tránh bug
                            if (BattleLeagueRank.Count >= _Config.MIN_ATTEND_PLAYER)
                            {
                                CreateSoloBoard(BattleLeagueRank);
                            }
                            else
                            {
                                NotifyALLTOBATTLE("Không đủ số người tham gia đăng ký.TDMP sẽ không thể bắt đầu");
                                KTGlobal.SendFactionChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, this.FactionID, "Không đủ số người tham gia đăng ký.TDMP sẽ không thể bắt đầu");
                                BATTLESTATE = LeagueState.END;
                            }
                        }


                        // SEND BẢNG THI ĐẤU VỀ CLIENT

                        LogManager.WriteLog(LogTypes.LeagueBattle, "[" + GetFactionName + "] Battle Change State ==> " + BATTLESTATE.ToString());

                        _ROUNDSTATE = ROUNDSTATE.PREADING;

                        int ROUNID = 0;

                        // Chuyển sang trạng thái nhặt cờ chuẩn bị thi đấu các thứu

                        if (BATTLESTATE == LeagueState.READY_ELIMINATION)
                        {
                            ROUNID = 1;
                            BATTLESTATE = LeagueState.ELIMINATION_ROUND_1;
                        }
                        else if (BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_1)
                        {
                            ROUNID = 2;
                            BATTLESTATE = LeagueState.ELIMINATION_ROUND_2;
                        }
                        else if (BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_2)
                        {
                            ROUNID = 3;
                            BATTLESTATE = LeagueState.ELIMINATION_ROUND_3;
                        }
                        else if (BATTLESTATE == LeagueState.FREE_ELIMINATION_ROUND_3)
                        {
                            ROUNID = 4;
                            BATTLESTATE = LeagueState.ELIMINATION_ROUND_4;
                        }

                        NotifyALLTOBATTLE("Vòng đấu loại PVP 1VS1 đã chính thức bắt đầu!");

                        //SET LẠI TICK CHO GAME
                        LastTick = TimeUtil.NOW();

                        // MOVE TOÀN BỘ NGƯỜI CHƠI VÀO ARENA

                        LogManager.WriteLog(LogTypes.LeagueBattle, "[" + GetFactionName + "] Battle Change State ==> " + BATTLESTATE.ToString());

                        // DUYỆT RA TOÀN BỘ NGƯỜI CHƠI TRONG ROUND
                        List<ELIMINATION_INFO> ELIMINATION_TOTAL_ROUND = ELIMINATION_TOTAL.Where(x => x.ROUNDID == ROUNID).ToList();

                        foreach (ELIMINATION_INFO ARENA in ELIMINATION_TOTAL_ROUND)
                        {
                            ArenaPos _ARENAPOSTION = _Config.TotalArena.Where(x => x.ArenaID == ARENA.ARENAID).FirstOrDefault();

                            if (ARENA.Player_1.player != null && ARENA.Player_1.player.IsOnline() && ARENA.Player_1.player.MapCode == this.GetMapCode)
                            {
                                // SET Cho người chơi 1
                                ARENA.Player_1.player.PKMode = (int)PKMode.Peace;
                                ARENA.Player_1.player.Camp = -1;
                                ARENA.Player_1.player.ForbidUsingSkill = true;
                                ARENA.Player_1.DamgeRecore = 0; // rs dame
                                //Send thông báo cho người chơi biết là trận đấu đã bắt đầu
                                PlayerManager.ShowNotification(ARENA.Player_1.player, "Thời gian chuẩn bị cho trận chiến là 30s");
                                // Gửi thông báo về cho thằng này thông tin trận đấu
                                SendBattleNotify1VS1(ARENA.Player_1, _ROUNDSTATE, 60, ARENA);

                                if (_ARENAPOSTION != null)
                                {
                                    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, ARENA.Player_1.player, _ARENAPOSTION.PosX, _ARENAPOSTION.PosY, (int)ARENA.Player_1.player.CurrentDir);
                                }
                            }

                            if (ARENA.Player_2.player != null && ARENA.Player_2.player.IsOnline() && ARENA.Player_2.player.MapCode == this.GetMapCode)
                            {
                                //// Set camp cho người chơi 2
                                ARENA.Player_2.player.PKMode = (int)PKMode.Peace;
                                ARENA.Player_2.player.Camp = -1;
                                ARENA.Player_2.player.ForbidUsingSkill = true;
                                ARENA.Player_2.DamgeRecore = 0;
                                //Send thông báo cho người chơi biết là trận đấu đã bắt đầu
                                PlayerManager.ShowNotification(ARENA.Player_2.player, "Thời gian chuẩn bị cho trận chiến là 30s");
                                // Gửi thông báo về cho thằng này thông tin trận đấu
                                SendBattleNotify1VS1(ARENA.Player_2, _ROUNDSTATE, 60, ARENA);
                                // set trạng thái đã bắt đầu chiến đấu

                                if (_ARENAPOSTION != null)
                                {
                                    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, ARENA.Player_2.player, _ARENAPOSTION.PosX, _ARENAPOSTION.PosY, (int)ARENA.Player_2.player.CurrentDir);
                                }
                            }

                            ARENA._ROUNDSTATE = ROUNDSTATE.PREADING;
                        }
                    }
                }
                else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
                {
                    if (TimeUtil.NOW() - LastNofity >= 3 * 1000 && (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4))
                    {
                        int ROUNID = 0;
                        // Chuyển sang trạng thái nhặt cờ chuẩn bị thi đấu các thứu

                        if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1)
                        {
                            ROUNID = 1;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_2)
                        {
                            ROUNID = 2;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_3)
                        {
                            ROUNID = 3;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
                        {
                            ROUNID = 4;
                        }
                        LastNofity = TimeUtil.NOW();
                        // Notify tiến độ trận cho những người chơi khác
                        UpdatePVP1VS1TOOtherPlayer(ROUNID);
                    }

                    // NẾu đã qua 30s chuẩn bị
                    if (TimeUtil.NOW() - LastTick >= 30 * 1000 && _ROUNDSTATE == ROUNDSTATE.PREADING && (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4))
                    {
                        _ROUNDSTATE = ROUNDSTATE.START;

                        int ROUNID = 0;

                        // Chuyển sang trạng thái nhặt cờ chuẩn bị thi đấu các thứu

                        if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1)
                        {
                            ROUNID = 1;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_2)
                        {
                            ROUNID = 2;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_3)
                        {
                            ROUNID = 3;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
                        {
                            ROUNID = 4;
                        }

                        // LẤY RA DANH SÁCH CÁC LÔI ĐÀI
                        List<ELIMINATION_INFO> ELIMINATION_TOTAL_ROUND = ELIMINATION_TOTAL.Where(x => x.ROUNDID == ROUNID).ToList();

                        long SEC = (LastTick + (_Config.TimeAct.ELIMINATION_ROUND_ROUND * 1000)) - TimeUtil.NOW();

                        // Hết 1 phút chuẩn bị chuyển toàn bộ người chơi sang trạng thái chiến đấu
                        foreach (ELIMINATION_INFO ARENA in ELIMINATION_TOTAL_ROUND)
                        {
                            // SET Cho người chơi 1
                            ARENA.Player_1.player.PKMode = (int)PKMode.Custom;
                            ARENA.Player_1.player.Camp = ARENA.Player_1.player.RoleID;
                            ARENA.Player_1.player.ForbidUsingSkill = false;
                            // Set lại là ko thằng nào thắng
                            ARENA.Player_1.IsWinRound = false;

                            // Gửi thông báo về cho thằng này thông tin trận đấu
                            SendBattleNotify1VS1(ARENA.Player_1, _ROUNDSTATE, (int)SEC, ARENA);

                            //Send thông báo cho người chơi biết là trận đấu đã bắt đầu
                            PlayerManager.ShowNotification(ARENA.Player_1.player, "PVP đã chính thức bắt đầu!");

                            //// Set camp cho người chơi 2
                            ARENA.Player_2.player.PKMode = (int)PKMode.Custom;
                            ARENA.Player_2.player.Camp = ARENA.Player_2.player.RoleID;
                            ARENA.Player_2.player.ForbidUsingSkill = false;
                            // Set lại là ko thằng nào thắng
                            ARENA.Player_2.IsWinRound = false;

                            // Gửi thông báo về cho thằng này thông tin trận đấu
                            SendBattleNotify1VS1(ARENA.Player_2, _ROUNDSTATE, (int)SEC, ARENA);

                            //Send thông báo cho người chơi biết là trận đấu đã bắt đầu
                            PlayerManager.ShowNotification(ARENA.Player_2.player, "PVP đã chính thức bắt đầu!");

                            // set trạng thái đã bắt đầu chiến đấu
                            ARENA._ROUNDSTATE = ROUNDSTATE.START;
                        }
                    }
                    // Nếu đã kết thức trận tính toán và tạo bảng xếp hạng mới
                    if (TimeUtil.NOW() >= LastTick + (_Config.TimeAct.ELIMINATION_ROUND_ROUND * 1000) && (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4))
                    {
                        int ROUNID = 0;

                        LastTick = TimeUtil.NOW();

                        if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1)
                        {
                            ROUNID = 1;
                            BATTLESTATE = LeagueState.FREE_ELIMINATION_ROUND_1;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_2)
                        {
                            ROUNID = 2;
                            BATTLESTATE = LeagueState.FREE_ELIMINATION_ROUND_2;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_3)
                        {
                            ROUNID = 3;
                            BATTLESTATE = LeagueState.FREE_ELIMINATION_ROUND_3;
                        }
                        else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
                        {
                            ROUNID = 4;
                            // NẾU ĐÃ KẾT THÚC TRẬN 4 thì là thời gian quân vương nhận thưởng
                            BATTLESTATE = LeagueState.CHAMPION_AWARD;
                        }

                        _ROUNDSTATE = ROUNDSTATE.END;

                        // Lấy ra toàn bộ các cặp đấu chưa phân thắng bại
                        List<ELIMINATION_INFO> ELIMINATION_TOTAL_ROUND = ELIMINATION_TOTAL.Where(x => x.ROUNDID == ROUNID && x.Player_1.IsWinRound == false && x.Player_2.IsWinRound == false).ToList();

                        // Tính toán thắng thua cho FULL ARENA
                        foreach (ELIMINATION_INFO ARENA in ELIMINATION_TOTAL_ROUND)
                        {
                            lock (ARENA)
                            {
                            LogManager.WriteLog(LogTypes.LeagueBattle, "ARENAID :[" + ARENA.ARENAID + "] CHƯA PHÂN THĂNG BẠI");
                            long DAMGEPLAYER1 = ARENA.Player_1.DamgeRecore;
                            long DAMGEPLAYER2 = ARENA.Player_2.DamgeRecore;

                            if (DAMGEPLAYER1 > DAMGEPLAYER2)
                            {
                                KTGlobal.SendSystemEventNotificationToPlayer(ARENA.Player_1.player, $"Trong trận tỉ với {ARENA.Player_2.player.RoleName}, bạn đã gây ra tổng " + KTGlobal.CreateStringByColor(DAMGEPLAYER1 + "", ColorType.Importal) + " sát thương và dành chiến thắng.");
                                KTGlobal.SendSystemEventNotificationToPlayer(ARENA.Player_2.player, $"Trong trận tỉ với {ARENA.Player_1.player.RoleName}, bạn đã thua vì gây ra ít sát thương hơn. ({DAMGEPLAYER2} < {DAMGEPLAYER1})");

                                LogManager.WriteLog(LogTypes.LeagueBattle, "SỐ DAMGE GÂY RA CỦA  :[" + ARENA.ARENAID + "][" + ARENA.Player_1.player.RoleName + "] là :" + DAMGEPLAYER1 + "| Nhiều hơn :" + ARENA.Player_2.player.RoleName + " là  :" + DAMGEPLAYER2 + "===> " + ARENA.Player_1.player.RoleName + " CHIẾN THẮNG");

                                ARENA.Player_2.BestElimateRank = ROUNID;
                                ARENA.Player_2.IsWinRound = false;
                                ARENA.WinThisRound = ARENA.Player_1;
                                ARENA.Player_1.IsWinRound = true;
                            }
                            else if (DAMGEPLAYER2 > DAMGEPLAYER1)
                            {
                                KTGlobal.SendSystemEventNotificationToPlayer(ARENA.Player_2.player, $"Trong trận tỉ với {ARENA.Player_1.player.RoleName}, bạn đã gây ra tổng {KTGlobal.CreateStringByColor(DAMGEPLAYER2 + "", ColorType.Importal)} sát thương và dành chiến thắng.");
                                KTGlobal.SendSystemEventNotificationToPlayer(ARENA.Player_1.player, $"Trong trận tỉ với {ARENA.Player_2.player.RoleName}, bạn đã thua vì gây ra ít sát thương hơn. ({DAMGEPLAYER1} < {DAMGEPLAYER2})");

                                LogManager.WriteLog(LogTypes.LeagueBattle, "SỐ DAMGE GÂY RA CỦA  :[" + ARENA.ARENAID + "][" + ARENA.Player_1.player.RoleName + "] là :" + DAMGEPLAYER1 + "| Ít hơn :" + ARENA.Player_2.player.RoleName + " là  :" + DAMGEPLAYER2 + "===> " + ARENA.Player_2.player.RoleName + " CHIẾN THẮNG");

                                ARENA.Player_1.BestElimateRank = ROUNID;
                                ARENA.Player_1.IsWinRound = false;

                                ARENA.WinThisRound = ARENA.Player_2;
                                ARENA.Player_2.IsWinRound = true;
                            }
                            else
                            {
                                // Nếu thằng  người chơi 1 bị diss ra thì thằng 2 thắng
                                if ((ARENA.Player_1.player.MapCode != this.GetMapCode || !ARENA.Player_1.player.IsOnline()) && (ARENA.Player_2.player.MapCode == this.GetMapCode && ARENA.Player_2.player.IsOnline()))
                                {
                                    ARENA.Player_1.BestElimateRank = ROUNID;
                                    ARENA.Player_1.IsWinRound = false;
                                    LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ARENA.ARENAID + "]: Thằng thứ 1 [" + ARENA.Player_1.player.RoleName + "] : bị diss ra =====> PLAYER 2 [" + ARENA.Player_2.player.RoleName + "] THẮNG");
                                    ARENA.WinThisRound = ARENA.Player_2;
                                    ARENA.Player_2.IsWinRound = true;
                                }
                                else if ((ARENA.Player_1.player.MapCode == this.GetMapCode && ARENA.Player_1.player.IsOnline()) && (ARENA.Player_2.player.MapCode != this.GetMapCode || !ARENA.Player_2.player.IsOnline()))
                                {
                                    ARENA.Player_2.BestElimateRank = ROUNID;
                                    ARENA.Player_2.IsWinRound = false;

                                    LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ARENA.ARENAID + "]: Thằng thứ 2 [" + ARENA.Player_2.player.RoleName + "] : bị diss ra =====> PLAYER 1 [" + ARENA.Player_1.player.RoleName + "] THẮNG");
                                    ARENA.WinThisRound = ARENA.Player_1;
                                    ARENA.Player_1.IsWinRound = true;
                                }
                                else if (ARENA.Player_1.nScore > ARENA.Player_2.nScore)
                                {
                                    ARENA.Player_2.BestElimateRank = ROUNID;
                                    ARENA.Player_2.IsWinRound = false;

                                    LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ARENA.ARENAID + "]: ĐIỂM CỦA  :[" + ARENA.Player_1.player.RoleName + "] NHIỀU HƠN =====> PLAYER 1 [" + ARENA.Player_1.player.RoleName + "] THẮNG PLAYER 2 [" + ARENA.Player_2.player.RoleName + "]");
                                    ARENA.WinThisRound = ARENA.Player_1;
                                    ARENA.Player_1.IsWinRound = true;
                                }
                                else
                                {
                                    ARENA.Player_1.BestElimateRank = ROUNID;
                                    ARENA.Player_1.IsWinRound = false;
                                    LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ARENA.ARENAID + "]: ĐIỂM CỦA  :[" + ARENA.Player_2.player.RoleName + "] NHIỀU HƠN =====> PLAYER 2 [" + ARENA.Player_2.player.RoleName + "] THẮNG PLAYER 1 [" + ARENA.Player_1.player.RoleName + "]");
                                    ARENA.WinThisRound = ARENA.Player_2;
                                    ARENA.Player_2.IsWinRound = true;
                                }
                            }
                            //reset damage cho 2 player:
                            ARENA.Player_1.DamgeRecore = 0;
                            ARENA.Player_2.DamgeRecore = 0;
                            } // end lock(ARENA)
                        }

                        // MOVE TOÀN BỘ RA KHỎI TRẬN CHIẾN
                        ELIMINATION_TOTAL_ROUND = ELIMINATION_TOTAL.Where(x => x.ROUNDID == ROUNID).ToList();

                        foreach (ELIMINATION_INFO ARENA in ELIMINATION_TOTAL_ROUND)
                        {
                            // SET Cho người chơi 1
                            ARENA.Player_1.player.PKMode = (int)PKMode.Peace;
                            ARENA.Player_1.player.Camp = -1;
                            ARENA.Player_1.player.ForbidUsingSkill = true;
                            //Send thông báo cho người chơi biết là trận đấu đã bắt đầu
                            PlayerManager.ShowNotification(ARENA.Player_1.player, "Trận chiến đã kết thúc");

                            GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, ARENA.Player_1.player, _Config.GoIn.X, _Config.GoIn.Y, (int)ARENA.Player_1.player.CurrentDir);

                            //// Set camp cho người chơi 2
                            ARENA.Player_2.player.PKMode = (int)PKMode.Peace;
                            ARENA.Player_2.player.Camp = -1;
                            ARENA.Player_2.player.ForbidUsingSkill = true;
                            //Send thông báo cho người chơi biết là trận đấu đã bắt đầu
                            PlayerManager.ShowNotification(ARENA.Player_2.player, "Trận chiến đã kết thúc");

                            GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, ARENA.Player_2.player, _Config.GoIn.X, _Config.GoIn.Y, (int)ARENA.Player_2.player.CurrentDir);

                            ARENA._ROUNDSTATE = ROUNDSTATE.END;
                        }

                        if (ROUNID < 4)
                        {
                            // TẠO SCOREBARD
                            this.CreateTopScoreBard(ROUNID);
                        }
                        else
                        {
                            ELIMINATION_INFO _FINDQUANQUAN = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
                            if (_FINDQUANQUAN != null)
                            {
                                // kết thúc trận cuối cùng |  Move 2 thằng cuối ra ngoài
                                PlayerManager.Relive(_FINDQUANQUAN.Player_1.player, GetMapCode, _Config.GoIn.X, _Config.GoIn.Y, 100, 100, 100);
                                PlayerManager.Relive(_FINDQUANQUAN.Player_2.player, GetMapCode, _Config.GoIn.X, _Config.GoIn.Y, 100, 100, 100);

                                if (_FINDQUANQUAN.WinThisRound != null)
                                {
                                    string NOTIFY = KTGlobal.CreateStringByColor(_FINDQUANQUAN.WinThisRound.player.RoleName, ColorType.Yellow) + " đã trở thành " + KTGlobal.CreateStringByColor("Quán quân", ColorType.Importal) + " giải đấu lần này";

                                    KTGlobal.SendSystemEventNotification(NOTIFY);
                                }
                            }
                        }
                    }
                }
                // Thời gian để thằng người quán quân nhận thưởng
                else if (BATTLESTATE == LeagueState.CHAMPION_AWARD)
                {
                    if (GameManager.IsKuaFuServer)
                    {
                        this.AutoSendRewardMailsKuaFu();
                    }
                    //Cứ mỗi 5s tick cho toàn bộ người chơi
                    if (TimeUtil.NOW() - LastNofity >= 5 * 1000 && (BATTLESTATE == LeagueState.CHAMPION_AWARD))
                    {
                        LastNofity = TimeUtil.NOW();
                        // Update tìm cờ
                        /// thông báo toàn sv đến nhận thưởng cược
                        KTGlobal.SendSystemEventNotification("Hoạt động Võ Lâm Minh Chủ đã kết thúc. Người chơi sẽ nhận được thưởng tham gia hoạt động, thưởng cược và thưởng Top 16 trong vòng 48 giờ qua thư kể từ khi kết thúc hoạt động.");
                        this.LogTop();
                        this.LogBetting();
                    }

                    // Kết thúc thời gian nhận thưởng đẩy tất cả trở về chỗ vào
                    if (TimeUtil.NOW() >= LastTick + (_Config.TimeAct.REVICE_AWARD * 1000) && BATTLESTATE == LeagueState.CHAMPION_AWARD)
                    {
                        LastTick = TimeUtil.NOW();
                        // SWTICH VỀ END
                        BATTLESTATE = LeagueState.END;
                    }
                }
                // ĐẨY TOÀN BỘ NGƯỜI REGISTER RA KHỎI PVP
                else if (BATTLESTATE == LeagueState.END)
                {
                    this.MoveAllPlayerQuitMap();

                    if (IsContinuousKuaFuBalance())
                    {
                        ResetBattle();
                        return;
                    }

                    //Đợi thêm 1 giờ cho người chơi tới NPC nhận thưởng sau đó dọn lại chiến trường
                    if (TimeUtil.NOW() - LastNofity >= 60 * 60 * 1000 && (BATTLESTATE == LeagueState.END))
                    {
                        // CHUYỂN VỀ NOTHING
                        BATTLESTATE = LeagueState.NOTHING;
                        // Sau 1 giờ thi đấu môn phái tự xóa toàn bộ dữ liệu
                        ResetBattle();
                    }
                }

                // ADD EXP CHO TOÀN BỘ NGƯỜI CHƠI KHI THAM GIA THI ĐẤU
                // CỨ 30s giật 1 phát EXP cho toàn bộ người chơi trong đấu trường môn phái
                //if (TimeUtil.NOW() - LastExpTick >= 30 * 1000 && (BATTLESTATE > LeagueState.MELEE_ROUND_2 && BATTLESTATE < LeagueState.CHAMPION_AWARD))
                //{
                //    LastExpTick = TimeUtil.NOW();
                //    //Thực hiện add EXP cho người chơi
                //    ExpProsecc();
                //}

                // END ADD
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, "BUG:" + ex.ToString());
            }
        }

        private void LogTop()
        {
            List<LeaguePlayer> leaguePlayers16 = new List<LeaguePlayer>();
            leaguePlayers16 = PVPARENA.Values.Where(x => x.BestElimateRank == 1).ToList().Clone();
            if (leaguePlayers16.Count > 0 && leaguePlayers16 != null)
            {
                int Rank = 16;
                foreach (LeaguePlayer player in leaguePlayers16)
                {
                    /// ghi lại log
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                }
            }

            List<LeaguePlayer> leaguePlayers8 = new List<LeaguePlayer>();
            leaguePlayers8 = PVPARENA.Values.Where(x => x.BestElimateRank == 2).ToList().Clone();
            if (leaguePlayers8.Count > 0 && leaguePlayers8 != null)
            {
                int Rank = 8;
                foreach (LeaguePlayer player in leaguePlayers8)
                {
                    /// ghi lại log
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                }
            }

            List<LeaguePlayer> leaguePlayers4 = new List<LeaguePlayer>();
            leaguePlayers4 = PVPARENA.Values.Where(x => x.BestElimateRank == 3).ToList().Clone();
            if (leaguePlayers4.Count > 0 && leaguePlayers4 != null)
            {
                int Rank = 4;
                foreach (LeaguePlayer player in leaguePlayers4)
                {
                    /// ghi lại log
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                }
            }
            List<LeaguePlayer> leaguePlayers2 = new List<LeaguePlayer>();
            leaguePlayers2 = PVPARENA.Values.Where(x => x.BestElimateRank == 4 && x.IsWinRound == false).ToList().Clone();
            if (leaguePlayers2.Count > 0 && leaguePlayers2 != null)
            {
                int Rank = 2;
                foreach (LeaguePlayer player in leaguePlayers2)
                {
                    /// ghi lại log
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                }
            }
            /// top 1
            ELIMINATION_INFO _FINDQUANQUAN = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
            if (_FINDQUANQUAN != null && _FINDQUANQUAN.WinThisRound != null)
            {
                int RankQuanQuan = 1;
                /// ghi lại log
                LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {_FINDQUANQUAN.WinThisRound.player.RoleName},  RoleID= {_FINDQUANQUAN.WinThisRound.player.RoleID} đạt top {RankQuanQuan} Võ Lâm Minh Chủ lần này!");
            }
            else
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, "Không tìm thấy quán quân vòng 4 (có thể cả 2 người chơi đã logout)");
            }
        }

        private void LogBetting()
        {
            ELIMINATION_INFO _FINDQUANQUAN = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
            if (_FINDQUANQUAN == null || _FINDQUANQUAN.WinThisRound == null)
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, "Không thể tính cược vì không có quán quân vòng 4");
                return;
            }
            // ds người đoán đúng
            int moneyWin = 0;
            int moneyLose = 0;
            var listWinBet = this.ListBets.Values.Where(x => x.BettorID == _FINDQUANQUAN.WinThisRound.player.RoleID).ToList().Clone();

            var listLostBet = this.ListBets.Values.Where(x => x.BettorID != _FINDQUANQUAN.WinThisRound.player.RoleID).ToList().Clone();
            foreach (var win in listWinBet)
            {
                moneyWin += win.MoneyBet;
            }
            LogManager.WriteLog(LogTypes.LeagueBattle, $"Tổng tiền cược thắng: {moneyWin}");
            foreach (var lose in listLostBet)
            {
                moneyLose += lose.MoneyBet;
            }
            LogManager.WriteLog(LogTypes.LeagueBattle, $"Tổng tiền cược thua: {moneyLose}");

            if (moneyLose > 0)
            {
                foreach (var win in listWinBet)
                {
                    var moneyAward = win.MoneyBet * moneyLose / moneyWin + win.MoneyBet;
                    int numberItem = moneyAward / 10000;
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"PlayerID: {win.PlayerID} nhận được {numberItem} vé thưởng VLMC");
                }

            }
            else
            {
                foreach (var win in listWinBet)
                {
                    LogManager.WriteLog(LogTypes.LeagueBattle, $"PlayerID: {win.PlayerID} nhận được 1 vé thưởng VLMC");
                }    
            }                 
        }

        private void CreateTopScoreBard(int ROUNID)
        {
            LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ROUNID + "] SẮP XẾP VÒNG ĐẤU!");

            // Lấy ra toàn bộ các ARENA của vòng đấu trước
            List<ELIMINATION_INFO> ELIMINATION_TOTAL_ROUND = ELIMINATION_TOTAL.Where(x => x.ROUNDID == ROUNID).ToList();

            List<LeaguePlayer> TotalPlayWin = new List<LeaguePlayer>();

            foreach (ELIMINATION_INFO ARENA in ELIMINATION_TOTAL_ROUND)
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ROUNID + "] [" + ARENA.ARENAID + "] BẮT ĐẦU LẤY RA NGƯỜI THẮNG CỦA ARENA");

                if (ARENA.WinThisRound != null)
                {
                    LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ROUNID + "] [" + ARENA.ARENAID + "] BẮT ĐẦU LẤY RA NGƯỜI THẮNG CỦA ARENA===>" + ARENA.WinThisRound.player.RoleName);
                    TotalPlayWin.Add(ARENA.WinThisRound);
                }
            }

            LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ROUNID + "] TỔNG SỐ NGƯỜI THẮNG TRONG ROUND NÀY LÀ ===>" + TotalPlayWin.Count);

            // set vị trí lôi đài
            int LOIDAI = 1;

            /// Duyệt tất cả người chơi trong trận
            for (int i = 0; i < TotalPlayWin.Count; i++)
            {
                ELIMINATION_INFO _Info = new ELIMINATION_INFO();

                _Info.IsCreateBox = false;
                _Info.ARENAID = LOIDAI;
                // Đây là trận đầu tiên
                _Info.ROUNDID = ROUNID + 1;

                _Info.Player_1 = TotalPlayWin[i];
                _Info.Player_2 = TotalPlayWin[i + 1];

                _Info.Player_1.IsWinRound = false;
                _Info.Player_2.IsWinRound = false;

                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Dịch 2 người 1
                i = i + 1;

                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ROUNID + 1 + "]|[" + LOIDAI + "] ĐÃ SẮP XẾP XONG ĐỘI HÌNH CHO LÔI ĐÀI");

                LOIDAI++;
            }
        }


        public void ExpProsecc()
        {
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                // Check xem nếu ở trong bản đồ mới nhận được exp
                if (PlayerBattle.player.MapCode == this.GetMapCode)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(PlayerBattle.player, 16000, true, false, true, "LeagueBattle");
                }
            }
        }

        /// <summary>
        /// Lấy ra top 16 thằng khỏe nhất trong vòng loại PVP
        /// </summary>
        /// <param name="TOP16Player"></param>
        public void CreateSoloBoard(List<LeaguePlayer> BattleFactionRank)
        {
            // HÀM NÀY VIẾT LẠI ĐỂ SẮP XẾP TRẠNG THÁI THI ĐẤU

            // Nếu số người lớn hơn 16 thì lấy ra 16 thằng có xếp hạng cao nhất
            if (BattleFactionRank.Count >= 16)
            {
                List<LeaguePlayer> TOP16Player = BattleFactionRank.GetRange(0, 16);

                // Sắp xếp vào lôi đài 1
                ELIMINATION_INFO _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 1;
                // Đây là trận đầu tiên
                _Info.IsCreateBox = false;
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[15];
                _Info.Player_2 = TOP16Player[0];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 2
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 2;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[7];
                _Info.Player_2 = TOP16Player[8];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 3
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 3;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[3];
                _Info.Player_2 = TOP16Player[12];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 4
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 4;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[5];
                _Info.Player_2 = TOP16Player[10];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 5
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 5;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[1];
                _Info.Player_2 = TOP16Player[14];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 6
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 6;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên

                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[6];
                _Info.Player_2 = TOP16Player[9];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 7
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 7;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[2];
                _Info.Player_2 = TOP16Player[13];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Sắp xếp vào lôi đài 8
                _Info = new ELIMINATION_INFO();
                _Info.ARENAID = 8;
                _Info.IsCreateBox = false;
                // Đây là trận đầu tiên
                _Info.ROUNDID = 1;
                _Info.Player_1 = TOP16Player[4];
                _Info.Player_2 = TOP16Player[11];
                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                //TODO SEND CÁI LIST NÀY VỀ CLIENT
            }
            else if (BattleFactionRank.Count < 16 && BattleFactionRank.Count >= 8) // Nếu số người lớn hơn 8 và nhỏ hơn 16
            {
                List<LeaguePlayer> TOP8Player = BattleFactionRank.GetRange(0, 8);

                CreateTopScoreBard(1, TOP8Player);

                BATTLESTATE = LeagueState.FREE_ELIMINATION_ROUND_1;
            }
            else if (BattleFactionRank.Count < 8 && BattleFactionRank.Count >= 4) // Nếu số người lớn hơn 8 và nhỏ hơn 16
            {
                List<LeaguePlayer> TOP4Player = BattleFactionRank.GetRange(0, 4);

                CreateTopScoreBard(2, TOP4Player);

                BATTLESTATE = LeagueState.FREE_ELIMINATION_ROUND_2;
            }
            else if (BattleFactionRank.Count < 4 && BattleFactionRank.Count >= 2) // Nếu số người lớn hơn 8 và nhỏ hơn 16
            {
                List<LeaguePlayer> TOP2Player = BattleFactionRank.GetRange(0, 2);

                CreateTopScoreBard(3, TOP2Player);

                BATTLESTATE = LeagueState.FREE_ELIMINATION_ROUND_3;
            }
        }

        private void CreateTopScoreBard(int ROUNID, List<LeaguePlayer> TotalPlayWin)
        {
            int LOIDAI = 1;

            for (int i = 0; i < TotalPlayWin.Count; i++)
            {
                ELIMINATION_INFO _Info = new ELIMINATION_INFO();

                _Info.IsCreateBox = false;
                _Info.ARENAID = LOIDAI;
                // Đây là trận đầu tiên
                _Info.ROUNDID = ROUNID + 1;

                _Info.Player_1 = TotalPlayWin[i];
                _Info.Player_2 = TotalPlayWin[i + 1];

                _Info.Player_1.IsWinRound = false;
                _Info.Player_2.IsWinRound = false;

                _Info._ROUNDSTATE = ROUNDSTATE.NONE;

                ELIMINATION_TOTAL.Add(_Info);

                // Dịch 2 người 1
                i = i + 1;

                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + ROUNID + 1 + "]|[" + LOIDAI + "] ĐÃ SẮP XẾP XONG ĐỘI HÌNH CHO LÔI ĐÀI");

                LOIDAI++;
            }
        }

        public void UpdateBattleNotify()
        {
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                if (PlayerBattle.player.CurrentMapCode == this.GetMapCode)
                {
                    long SEC = (LastTick + (_Config.TimeAct.MELEE_PVP_DULATION * 1000)) - TimeUtil.NOW();

                    int FinalSec = (int)(SEC / 1000);

                    SendBattleNotify(PlayerBattle, FinalSec);
                }
            }
        }

        public void UpdateBettingNotify()
        {
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer player = entry.Value;

                if (player.player.CurrentMapCode == this.GetMapCode)
                {
                    long Sec = (LastTick + (_Config.TimeAct.FREE_ELIMINATION * 3000)) - TimeUtil.NOW();

                    int finalSec = (int)(Sec / 1000);

                    SendBettingNotify(player, finalSec);
                }    
            }
        }

        /// <summary>
        /// Thông báo cờ
        /// </summary>
        ///
        public void SendPvpToOtherPlayer(LeaguePlayer InputPlayer, int Sec, int STATUS)
        {
            PlayChangeState(InputPlayer.player, 1);

            InputPlayer.player.StopAllActiveFights();

            InputPlayer.player.PKMode = (int)PKMode.Peace;
            //// Set camp cho người chơi
            InputPlayer.player.Camp = -1;

            // mở khóa sử dụng skill
            InputPlayer.player.ForbidUsingSkill = true;

            G2C_EventNotification _Notify = new G2C_EventNotification();

            if (Sec > 0)
            {
                if (STATUS == 1)
                {
                    _Notify.EventName = "Thời gian chuẩn bị thi đấu";
                    _Notify.ShortDetail = "TIME|" + Sec;
                }
                else if (STATUS == 2)
                {
                    _Notify.EventName = "Thời gian đấu loại trực tiếp";
                    _Notify.ShortDetail = "TIME|" + Sec;
                }
            }
            else
            {
                _Notify.EventName = "Thời gian tỉ thí đã kết thúc";
                _Notify.ShortDetail = "TIME|" + 0;
            }

            _Notify.TotalInfo = new List<string>();
            _Notify.TotalInfo.Add("");
            _Notify.TotalInfo.Add("");
            _Notify.TotalInfo.Add("");

            if (InputPlayer.player.IsOnline())
            {
                InputPlayer.player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        public void AntiLagMove(Point CenterPoint, LeaguePlayer Character)
        {
            // Nếu mà nhân vật bị rollback khổng sản
            if (!KTGlobal.HasPath(Character.player.MapCode, CenterPoint, Character.player.CurrentPos))
            {
                GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, Character.player, (int)CenterPoint.X, (int)CenterPoint.Y, (int)Character.player.CurrentDir);
            }
        }

        public void UpdatePVP1VS1TOOtherPlayer(int ROUNID)
        {
            List<int> FULLPLAYER = new List<int>();

            // LẤY RA DANH SÁCH CÁC LÔI ĐÀI
            List<ELIMINATION_INFO> ELIMINATION_TOTAL_ROUND = ELIMINATION_TOTAL.Where(x => x.ROUNDID == ROUNID).ToList();

            foreach (ELIMINATION_INFO ARENAINFO in ELIMINATION_TOTAL_ROUND)
            {
                FULLPLAYER.Add(ARENAINFO.Player_1.player.RoleID);
                FULLPLAYER.Add(ARENAINFO.Player_2.player.RoleID);

                if (_ROUNDSTATE == ROUNDSTATE.PREADING)
                {
                    long SEC = (LastTick + (60 * 1000)) - TimeUtil.NOW();

                    int FinalSec = (int)(SEC / 1000);

                    SendBattleNotify1VS1(ARENAINFO.Player_1, _ROUNDSTATE, FinalSec, ARENAINFO);
                    SendBattleNotify1VS1(ARENAINFO.Player_2, _ROUNDSTATE, FinalSec, ARENAINFO);
                }

                if (_ROUNDSTATE == ROUNDSTATE.START)
                {
                    ArenaPos _ARENAPOSTION = _Config.TotalArena.Where(x => x.ArenaID == ARENAINFO.ARENAID).FirstOrDefault();

                    if (ARENAINFO._ROUNDSTATE == ROUNDSTATE.PREADING)
                    {
                        this.AntiLagMove(_ARENAPOSTION.ConvertToPoint, ARENAINFO.Player_1);
                        this.AntiLagMove(_ARENAPOSTION.ConvertToPoint, ARENAINFO.Player_2);
                    }
                    // Nếu battle đang bắt đầu
                    else if (ARENAINFO._ROUNDSTATE == ROUNDSTATE.START)
                    {
                        // Thực hiện spam camp set lại PK mode liên tục
                        ARENAINFO.Player_1.player.PKMode = (int)PKMode.Custom;
                        ARENAINFO.Player_1.player.Camp = ARENAINFO.Player_1.player.RoleID;
                        ARENAINFO.Player_1.player.ForbidUsingSkill = false;

                        ARENAINFO.Player_2.player.PKMode = (int)PKMode.Custom;
                        ARENAINFO.Player_2.player.Camp = ARENAINFO.Player_2.player.RoleID;
                        ARENAINFO.Player_2.player.ForbidUsingSkill = false;

                        this.AntiLagMove(_ARENAPOSTION.ConvertToPoint, ARENAINFO.Player_1);
                        this.AntiLagMove(_ARENAPOSTION.ConvertToPoint, ARENAINFO.Player_2);
                    }
                    else if (ARENAINFO._ROUNDSTATE == ROUNDSTATE.END)
                    {
                        ARENAINFO.Player_1.player.StopAllActiveFights();
                        // Chuyển về hòa bình nếu đã đánh xong trận
                        ARENAINFO.Player_1.player.PKMode = (int)PKMode.Peace;
                        //// Set camp cho người chơi
                        ARENAINFO.Player_1.player.Camp = -1;
                        // mở khóa sử dụng skill
                        ARENAINFO.Player_1.player.ForbidUsingSkill = true;

                        ARENAINFO.Player_2.player.StopAllActiveFights();
                        // Chuyển về hòa bình nếu đã đánh xong trận
                        ARENAINFO.Player_2.player.PKMode = (int)PKMode.Peace;
                        //// Set camp cho người chơi
                        ARENAINFO.Player_2.player.Camp = -1;
                        // mở khóa sử dụng skill
                        ARENAINFO.Player_2.player.ForbidUsingSkill = true;
                    }

                    long SEC = (LastTick + (_Config.TimeAct.ELIMINATION_ROUND_ROUND * 1000)) - TimeUtil.NOW();
                    int FinalSec = (int)(SEC / 1000);
                    SendBattleNotify1VS1(ARENAINFO.Player_1, _ROUNDSTATE, FinalSec, ARENAINFO);
                    SendBattleNotify1VS1(ARENAINFO.Player_2, _ROUNDSTATE, FinalSec, ARENAINFO);
                }
            }

            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                if (FULLPLAYER.Contains(PlayerBattle.player.RoleID))
                {
                    continue;
                }

                if (PlayerBattle.player.CurrentMapCode == this.GetMapCode)
                {
                    //long SEC = (LastTick + (_Config.TimeAct.ELIMINATION_ROUND_ROUND * 1000)) - TimeUtil.NOW();

                    //int FinalSec = (int)(SEC / 1000);

                    //SendPvpToOtherPlayer(PlayerBattle, FinalSec);

                    if (_ROUNDSTATE == ROUNDSTATE.PREADING)
                    {
                        long SEC = (LastTick + (60 * 1000)) - TimeUtil.NOW();

                        int FinalSec = (int)(SEC / 1000);

                        SendPvpToOtherPlayer(PlayerBattle, FinalSec, 1);
                    }

                    if (_ROUNDSTATE == ROUNDSTATE.START)
                    {
                        long SEC = (LastTick + (_Config.TimeAct.ELIMINATION_ROUND_ROUND * 1000)) - TimeUtil.NOW();
                        int FinalSec = (int)(SEC / 1000);

                        SendPvpToOtherPlayer(PlayerBattle, FinalSec, 2);
                    }
                }
            }
        }

        /// <summary>
        ///  Update thánh tích chiến đấu của người chơi trong vòng PK tự do
        /// </summary>
        /// <param name="InputPlayer"></param>
        /// <param name="Sec"></param>
        public void SendBattleNotifyAward(LeaguePlayer InputPlayer, int Sec)
        {
            PlayChangeState(InputPlayer.player, 1);

            G2C_EventNotification _Notify = new G2C_EventNotification();

            if (Sec > 0)
            {
                _Notify.EventName = "Thời gian nhận thưởng:";
                _Notify.ShortDetail = "TIME|" + Sec;

                _Notify.TotalInfo = new List<string>();

                _Notify.TotalInfo.Add("Gặp NPC Sứ Giả Hoạt Động");
            }

            if (InputPlayer.player.IsOnline())
            {
                InputPlayer.player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        public void NotifyALLTOBATTLE(string MSG)
        {
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                PlayerManager.ShowNotification(PlayerBattle.player, MSG);
            }
        }

        /// <summary>
        ///  Update thánh tích chiến đấu của người chơi trong vòng PK tự do
        /// </summary>
        /// <param name="InputPlayer"></param>
        /// <param name="Sec"></param>
        public void SendBattleNotify1VS1(LeaguePlayer InputPlayer, ROUNDSTATE _STATE, int Sec, ELIMINATION_INFO _INFO)
        {
            PlayChangeState(InputPlayer.player, 1);

            G2C_EventNotification _Notify = new G2C_EventNotification();

            if (_STATE == ROUNDSTATE.PREADING)
            {
                _Notify.EventName = "Tỉ thí sẽ bắt đầu sau:";
            }
            else if (_STATE == ROUNDSTATE.START)
            {
                _Notify.EventName = "Thời gian tỉ thí còn:";
            }
            else
            {
                _Notify.EventName = "Thời gian thi đấu đã kết thúc";
            }

            if (Sec > 0)
            {
                _Notify.ShortDetail = "TIME|" + Sec;
            }

            _Notify.TotalInfo = new List<string>();

            _Notify.TotalInfo.Add(KTGlobal.CreateStringByColor($"{_INFO.Player_1.player.RoleName}: {_INFO.Player_1.DamgeRecore}", ColorType.Yellow));

            _Notify.TotalInfo.Add(KTGlobal.CreateStringByColor("====VS====", ColorType.Importal));

            _Notify.TotalInfo.Add(KTGlobal.CreateStringByColor($"{_INFO.Player_2.player.RoleName}: {_INFO.Player_2.DamgeRecore}", ColorType.Yellow));

            if (InputPlayer.player.IsOnline())
            {
                InputPlayer.player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        /// <summary>
        ///  Update thánh tích chiến đấu của người chơi trong vòng PK tự do
        /// </summary>
        /// <param name="InputPlayer"></param>
        /// <param name="Sec"></param>
        public void SendBattleNotify(LeaguePlayer InputPlayer, int Sec)
        {
            PlayChangeState(InputPlayer.player, 1);

            G2C_EventNotification _Notify = new G2C_EventNotification();

            _Notify.EventName = "Vòng PK Hỗn Chiến";
            if (Sec > 0)
            {
                _Notify.ShortDetail = "TIME|" + Sec;
            }
            else
            {
                _Notify.ShortDetail = "Kết Thúc Hỗn Chiến";
            }

            _Notify.TotalInfo = new List<string>();

            _Notify.TotalInfo.Add("Tích Lũy : " + InputPlayer.nScore);

            _Notify.TotalInfo.Add("Bị Giết : " + InputPlayer.nDeathCount);

            _Notify.TotalInfo.Add("Hạng Hiện Tại :" + (GetRankInBxh(InputPlayer.player.RoleID) + 1));

            if (InputPlayer.player.IsOnline())
            {
                InputPlayer.player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        public void SendBettingNotify(LeaguePlayer InputPlayer, int Sec)
        {
            PlayChangeState(InputPlayer.player, 1);

            G2C_EventNotification _Notify = new G2C_EventNotification();

            _Notify.EventName = "Vòng đặt cược";
            if (Sec > 0)
            {
                _Notify.ShortDetail = "TIME|" + Sec;
            }
            else
            {
                _Notify.ShortDetail = "Kết thúc đặt cược";
            }

            if (InputPlayer.player.IsOnline())
            {
                InputPlayer.player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION, _Notify);
            }
        }

        #region MOVEPLAYER

        /// <summary>
        /// Move tất cả vào đấu trường PVP
        /// </summary>
        public void MoveAllPlayerToPvp()
        {
            // MOVE
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                {
                    //Đánh đấu post
                    PlayerBattle.LastPosition = PlayerBattle.player.CurrentPos;

                    int Random = new Random().Next(0, _Config.PVPRandomRevice.Count);

                    Postion _Position = _Config.PVPRandomRevice[Random];

                    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, PlayerBattle.player, _Position.X, _Position.Y, (int)PlayerBattle.player.CurrentDir);

                }
            }

            var _VerifyTele = VerifyGoIn();
        }

        public void MoveAllPlayerOutPvp()
        {
            // MOVE
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                {
                    //Set lại vị trí cuối cùng
                    PlayerBattle.LastPosition = PlayerBattle.player.CurrentPos;

                    // Gửi thông báo về thời gian đăng ký còn bao lâu cho bọn nó biết
                    PlayerManager.Relive(PlayerBattle.player, this.GetMapCode, _Config.GoIn.X, _Config.GoIn.Y, 100, 100, 100);
                }
            }
            // Call hàm verify tele để chắc chắn là người chơi đã được move ra
            var _VerifyTele = VerifyTelePortAsync();
        }

        public async System.Threading.Tasks.Task VerifyGoIn()
        {
            // LOOP 10 lần cho nó chắc
            for (int i = 0; i < 10; i++)
            {
                // Nếu trạng thái trận đấu là sẵn sàng cho solo 1 và nghỉ giữa trận thì chắc chắn rằng người chơi được chuyển ra khỏi khu vụ solo
                if ((BATTLESTATE == LeagueState.MELEE_ROUND_1 || BATTLESTATE == LeagueState.MELEE_ROUND_2) && (TimeUtil.NOW() - LastTick) < 120000)
                {
                    foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
                    {
                        LeaguePlayer PlayerBattle = entry.Value;

                        if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                        {
                            float Dist = KTGlobal.GetDistanceBetweenPoints(PlayerBattle.player.CurrentPos, PlayerBattle.LastPosition);

                            if (Dist < 1000)
                            {
                                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + PlayerBattle.player.RoleName + "] Toạch move vào đấu trường ==> Thực hiện move lại!");

                                int Random = new Random().Next(0, _Config.PVPRandomRevice.Count);

                                Postion _Position = _Config.PVPRandomRevice[Random];

                                GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, PlayerBattle.player, _Position.X, _Position.Y, (int)PlayerBattle.player.CurrentDir);
                            }
                        }
                    }
                }

                // Nửa giây tick 1 phát
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(500));

                i++;
            }
        }

        public async System.Threading.Tasks.Task VerifyTelePortAsync()
        {
            // LOOP 10 lần cho nó chắc
            for (int i = 0; i < 10; i++)
            {
                // Nếu trạng thái trận đấu là sẵn sàng cho solo 1 và nghỉ giữa trận thì chắc chắn rằng người chơi được chuyển ra khỏi khu vụ solo
                if ((BATTLESTATE == LeagueState.READY_ELIMINATION || BATTLESTATE == LeagueState.FREE_MELEE) && (TimeUtil.NOW() - LastTick) < 120000)
                {
                    foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
                    {
                        LeaguePlayer PlayerBattle = entry.Value;

                        if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                        {
                            float Dist = KTGlobal.GetDistanceBetweenPoints(PlayerBattle.player.CurrentPos, PlayerBattle.LastPosition);

                            if (Dist < 1000)
                            {
                                LogManager.WriteLog(LogTypes.LeagueBattle, "[" + PlayerBattle.player.RoleName + "] Toạch move khỏi đấu trường==> Thực hiện move lại!");
                                PlayerManager.Relive(PlayerBattle.player, this.GetMapCode, _Config.GoIn.X, _Config.GoIn.Y, 100, 100, 100);
                            }
                        }
                    }
                }

                // Nửa giây tick 1 phát
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(500));

                i++;
            }
        }

        public async System.Threading.Tasks.Task MoveDelayTask(Action task, int delay)
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(delay));

            task?.Invoke();
        }

        public void MoveAllPlayerQuitMap()
        {
            // MOVE
            foreach (KeyValuePair<int, LeaguePlayer> entry in PVPARENA)
            {
                LeaguePlayer PlayerBattle = entry.Value;

                //Xóa bỏ trạng thái cấm đánh
                PlayerBattle.player.ForbidUsingSkill = false;

                if (PlayerBattle.player.CurrentMapCode == GetMapCode)
                {
                    KT_TCPHandler.GetPlayerCopySceneInfoFromDB(PlayerBattle.player, out int copySceneID, out int preMapCode, out int prePosX, out int prePosY, out long copySceneCreateTicks);

                    GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), PlayerBattle.player, preMapCode, prePosX, prePosY, 0, false);
                }
            }
        }

        #endregion MOVEPLAYER

        public void SendKillStreak(KPlayer InputPlayer, int Count)
        {
            G2C_KillStreak _State = new G2C_KillStreak();

            _State.KillNumber = Count;

            if (InputPlayer.IsOnline())
            {
                InputPlayer.SendPacket<G2C_KillStreak>((int)TCPGameServerCmds.CMD_KT_KILLSTREAK, _State);
            }
        }

        public void NotifySocreForPlayer(LeaguePlayer PlayerBattle, bool ShowKillStreak)
        {
            long SEC = (LastTick + (_Config.TimeAct.MELEE_PVP_DULATION * 1000)) - TimeUtil.NOW();

            int FinalSec = (int)(SEC / 1000);

            SendBattleNotify(PlayerBattle, FinalSec);
        }

        /// <summary>
        /// Nếu là on hit taget
        /// </summary>
        /// <param name="Hit"></param>
        /// <param name="BeHit"></param>
        /// <param name="DamgeRecore"></param>
        public void OnHitTaget(KPlayer Hit, int DamgeRecore)
        {
            // Nếu là vòng tử chiến ghi lại số sát thương của thằng tấn công và thằng bị tấn công
            if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
            {
                try
                {
                    PVPARENA.TryGetValue(Hit.RoleID, out LeaguePlayer _HIT);

                    if (_HIT != null)
                    {
                        _HIT.DamgeRecore += DamgeRecore;

                        LogManager.WriteLog(LogTypes.LeagueBattle, "[" + _HIT.player.RoleName + "][" + BATTLESTATE.ToString() + "] Call Add Damge Value :" + _HIT.DamgeRecore);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.LeagueBattle, ex.ToString());
                }
            }
        }

        /// <summary>
        ///  Nếu mà đang thi đấu thằng này thoát ra
        /// </summary>
        /// <param name="Kill"></param>
        public void OnLogout(KPlayer LOGOUT)
        {
            if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
            {
                PVPARENA.TryGetValue(LOGOUT.RoleID, out LeaguePlayer _LOGOUT);

                if (_LOGOUT != null)
                {
                    int ROUNID = 0;

                    if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1)
                    {
                        ROUNID = 1;
                    }
                    else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_2)
                    {
                        ROUNID = 2;
                    }
                    else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_3)
                    {
                        ROUNID = 3;
                    }
                    else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
                    {
                        ROUNID = 4;
                    }

                    foreach (ELIMINATION_INFO _ARENAFIND in ELIMINATION_TOTAL)
                    {
                        if (_ARENAFIND.ROUNDID == ROUNID)
                        {
                            // Bỏ qua arena đã có kết quả (tránh ghi đè WinThisRound khi người thắng logout)
                            if (_ARENAFIND.WinThisRound != null)
                            {
                                LogManager.WriteLog(LogTypes.LeagueBattle, "[LOGOUT] Bỏ qua arena [" + _ARENAFIND.ARENAID + "] ROUND:" + ROUNID + " đã có người thắng: " + _ARENAFIND.WinThisRound.player.RoleName + " | Người logout: " + LOGOUT.RoleName);
                                continue;
                            }

                            if (_ARENAFIND.Player_1.player.RoleID == LOGOUT.RoleID || _ARENAFIND.Player_2.player.RoleID == LOGOUT.RoleID)
                            {
                                if (_ARENAFIND.Player_1.player.RoleID == LOGOUT.RoleID)
                                {
                                    if (_ARENAFIND.Player_1 != null)
                                    {
                                        _ARENAFIND.Player_1.BestElimateRank = ROUNID;
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_1.player.Camp = -1;
                                        _ARENAFIND.Player_1.player.PKMode = (int)PKMode.Peace;
                                    }

                                    // SET PALYER 2 CAMP VỀ HÒA BÌNH
                                    if (_ARENAFIND.Player_2 != null)
                                    {
                                        _ARENAFIND.WinThisRound = _ARENAFIND.Player_2;
                                        _ARENAFIND.Player_2.IsWinRound = true;

                                        Action _Action = new Action(() => GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _ARENAFIND.Player_2.player, _Config.GoIn.X, _Config.GoIn.Y, (int)_ARENAFIND.Player_2.player.CurrentDir));

                                        var MoveDelay = MoveDelayTask(_Action, 5000);

                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_2.player.Camp = -1;
                                        _ARENAFIND.Player_2.player.PKMode = (int)PKMode.Peace;
                                    }

                                    string NOTIFY = KTGlobal.CreateStringByColor("Trong trận tỉ thí giữa ", ColorType.Yellow) + KTGlobal.CreateStringByColor(_ARENAFIND.Player_1.player.RoleName, GameServer.Logic.ColorType.Importal) + " VS " + KTGlobal.CreateStringByColor(_ARENAFIND.Player_2.player.RoleName, GameServer.Logic.ColorType.Importal) + " người chơi " + KTGlobal.CreateStringByColor(_ARENAFIND.Player_2.player.RoleName, GameServer.Logic.ColorType.Importal) + " đã dành chiến thắng";

                                    NotifyALLTOBATTLE(NOTIFY);

                                    PlayerManager.ShowNotification(_ARENAFIND.Player_2.player, "Trong trận tỉ với " + _ARENAFIND.Player_1.player.RoleName + " bạn đã dành chiến thắng");

                                    PlayerManager.ShowNotification(_ARENAFIND.Player_1.player, "Trong trận tỉ với " + _ARENAFIND.Player_2.player.RoleName + " bạn đã thua cuộc");
                                }
                                else if (_ARENAFIND.Player_2.player.RoleID == LOGOUT.RoleID)
                                {
                                    // SET PALYER 1 CAMP VỀ HÒA BÌNH
                                    if (_ARENAFIND.Player_1 != null)
                                    {
                                        _ARENAFIND.WinThisRound = _ARENAFIND.Player_1;
                                        _ARENAFIND.Player_1.IsWinRound = true;

                                        Action _Action = new Action(() => GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _ARENAFIND.Player_1.player, _Config.GoIn.X, _Config.GoIn.Y, (int)_ARENAFIND.Player_1.player.CurrentDir));

                                        var MoveDelay = MoveDelayTask(_Action, 5000);
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_1.player.Camp = -1;
                                        _ARENAFIND.Player_1.player.PKMode = (int)PKMode.Peace;
                                    }

                                    // SET PALYER 2 CAMP VỀ HÒA BÌNH
                                    if (_ARENAFIND.Player_2 != null)
                                    {
                                        _ARENAFIND.Player_2.BestElimateRank = ROUNID;
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_2.player.Camp = -1;
                                        _ARENAFIND.Player_2.player.PKMode = (int)PKMode.Peace;
                                    }

                                    string NOTIFY = KTGlobal.CreateStringByColor("Trong trận tỉ thí giữa ", ColorType.Yellow) + KTGlobal.CreateStringByColor(_ARENAFIND.Player_1.player.RoleName, GameServer.Logic.ColorType.Importal) + " VS " + KTGlobal.CreateStringByColor(_ARENAFIND.Player_2.player.RoleName, GameServer.Logic.ColorType.Importal) + " người chơi " + KTGlobal.CreateStringByColor(_ARENAFIND.Player_1.player.RoleName, GameServer.Logic.ColorType.Importal) + " đã dành chiến thắng";

                                    NotifyALLTOBATTLE(NOTIFY);

                                    PlayerManager.ShowNotification(_ARENAFIND.Player_1.player, "Trong trận tỉ với " + _ARENAFIND.Player_2.player.RoleName + " bạn đã dành chiến thắng");

                                    PlayerManager.ShowNotification(_ARENAFIND.Player_2.player, "Trong trận tỉ với " + _ARENAFIND.Player_1.player.RoleName + " bạn đã thua cuộc");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event tính toán sự kiện A GIẾT B
        /// </summary>
        /// <param name="kPlayer1"></param>
        /// <param name="kPlayer2"></param>
        public void OnKillRole(KPlayer Kill, KPlayer Bekill)
        {
            if (BATTLESTATE == LeagueState.MELEE_ROUND_1 || BATTLESTATE == LeagueState.MELEE_ROUND_2)
            {
                PVPARENA.TryGetValue(Kill.RoleID, out LeaguePlayer _KILL);

                if (_KILL != null && _KILL.player.MapCode == this.GetMapCode)
                {
                    _KILL.KillStreak = _KILL.KillStreak + 1;

                    if (_KILL.MaxKillStreak < _KILL.KillStreak)
                    {
                        _KILL.MaxKillStreak = _KILL.KillStreak;
                    }

                    if (_KILL.KillStreak > 0 && _KILL.KillStreak % 3 == 0)
                    {
                        SendKillStreak(Kill, _KILL.KillStreak);
                    }

                    _KILL.nScore = _KILL.nScore + 1;

                    NotifySocreForPlayer(_KILL, true);
                }

                PVPARENA.TryGetValue(Bekill.RoleID, out LeaguePlayer _BEKILL);
                if (_BEKILL != null && _BEKILL.player.MapCode == this.GetMapCode)
                {
                    _BEKILL.KillStreak = 0;

                    _BEKILL.nDeathCount = _BEKILL.nDeathCount + 1;

                    NotifySocreForPlayer(_BEKILL, false);
                }
            }
            else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_2 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_3 || BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
            {
                PVPARENA.TryGetValue(Kill.RoleID, out LeaguePlayer _KILL);
                PVPARENA.TryGetValue(Bekill.RoleID, out LeaguePlayer _BEKILL);

                if (_KILL != null && _BEKILL != null)
                {
                    int ROUNID = 0;

                    if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_1)
                    {
                        ROUNID = 1;
                    }
                    else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_2)
                    {
                        ROUNID = 2;
                    }
                    else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_3)
                    {
                        ROUNID = 3;
                    }
                    else if (BATTLESTATE == LeagueState.ELIMINATION_ROUND_4)
                    {
                        ROUNID = 4;
                    }

                    string NOTIFY = KTGlobal.CreateStringByColor("Trong trận tỉ thí giữa ", ColorType.Yellow) + KTGlobal.CreateStringByColor(Kill.RoleName, GameServer.Logic.ColorType.Importal) + " VS " + KTGlobal.CreateStringByColor(Bekill.RoleName, GameServer.Logic.ColorType.Importal) + " người chơi " + KTGlobal.CreateStringByColor(Kill.RoleName, GameServer.Logic.ColorType.Importal) + " đã dành chiến thắng";

                    NotifyALLTOBATTLE(NOTIFY);


                    foreach (ELIMINATION_INFO _ARENAFIND in ELIMINATION_TOTAL)
                    {
                        if (_ARENAFIND.ROUNDID == ROUNID)
                        {
                            LogManager.WriteLog(LogTypes.LeagueBattle, "PVP [" + _ARENAFIND.ARENAID + "]ROUND :" + ROUNID + " | KILL ROLE REPORT FORM EVENT KILL :" + Kill.RoleName + "| BEKILL :" + Bekill.RoleName);

                            // Bỏ qua arena đã có kết quả (tránh kill trùng ghi đè WinThisRound)
                            if (_ARENAFIND.WinThisRound != null)
                                continue;

                            if (_ARENAFIND.Player_1.player != null && _ARENAFIND.Player_2.player != null)
                            {
                                if (_ARENAFIND.Player_1.player.RoleID == Kill.RoleID)
                                {
                                    lock (_ARENAFIND)
                                    {
                                        // SET PALYER 1 CAMP VỀ HÒA BÌNH
                                        _ARENAFIND.WinThisRound = _ARENAFIND.Player_1;
                                        _ARENAFIND.Player_1.IsWinRound = true;
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_1.player.Camp = -1;
                                        _ARENAFIND.Player_1.player.PKMode = (int)PKMode.Peace;
                                        // SET PALYER 2 CAMP VỀ HÒA BÌNH
                                        _ARENAFIND.Player_2.BestElimateRank = ROUNID;
                                        _ARENAFIND.Player_2.IsWinRound = false;
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_2.player.Camp = -1;
                                        _ARENAFIND.Player_2.player.PKMode = (int)PKMode.Peace;

                                        Action _Action = new Action(() => GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _ARENAFIND.Player_1.player, _Config.GoIn.X, _Config.GoIn.Y, (int)_ARENAFIND.Player_1.player.CurrentDir));
                                        var MoveDelay = MoveDelayTask(_Action, 5000);

                                        //PlayerManager.ShowNotification(_ARENAFIND.Player_1.player, "Trong trận tỉ với " + _ARENAFIND.Player_2.player.RoleName + " bạn đã dành chiến thắng");
                                        //
                                        //PlayerManager.ShowNotification(_ARENAFIND.Player_2.player, "Trong trận tỉ với " + _ARENAFIND.Player_1.player.RoleName + " bạn đã thua cuộc");

                                        _ARENAFIND._ROUNDSTATE = ROUNDSTATE.END;
                                    }
                                }
                                // SET THẰNG THỨ 2 THẮNG
                                else if (_ARENAFIND.Player_2.player.RoleID == Kill.RoleID)
                                {
                                    lock (_ARENAFIND)
                                    {
                                        _ARENAFIND.Player_1.BestElimateRank = ROUNID;
                                        _ARENAFIND.Player_1.IsWinRound = false;
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_1.player.Camp = -1;
                                        _ARENAFIND.Player_1.player.PKMode = (int)PKMode.Peace;

                                        _ARENAFIND.WinThisRound = _ARENAFIND.Player_2;
                                        _ARENAFIND.Player_2.IsWinRound = true;
                                        // set camp cho thằng này = -1 và hòa bình
                                        _ARENAFIND.Player_2.player.Camp = -1;
                                        _ARENAFIND.Player_2.player.PKMode = (int)PKMode.Peace;
                                        Action _Action = new Action(() => GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _ARENAFIND.Player_2.player, _Config.GoIn.X, _Config.GoIn.Y, (int)_ARENAFIND.Player_2.player.CurrentDir));
                                        var MoveDelay = MoveDelayTask(_Action, 5000);

                                        //PlayerManager.ShowNotification(_ARENAFIND.Player_2.player, "Trong trận tỉ với " + _ARENAFIND.Player_1.player.RoleName + " bạn đã dành chiến thắng");
                                        //
                                        //PlayerManager.ShowNotification(_ARENAFIND.Player_1.player, "Trong trận tỉ với " + _ARENAFIND.Player_2.player.RoleName + " bạn đã thua cuộc");

                                        _ARENAFIND._ROUNDSTATE = ROUNDSTATE.END;
                                    }
                                }
                            }
                            else
                            {
                                if (_ARENAFIND.Player_1.player != null)
                                {
                                    // set camp cho thằng này = -1 và hòa bình
                                    _ARENAFIND.Player_1.player.Camp = -1;
                                    _ARENAFIND.Player_1.player.PKMode = (int)PKMode.Peace;
                                    if (_ARENAFIND.Player_1.player.RoleID == Kill.RoleID)
                                    {
                                        // SET PALYER 1 CAMP VỀ HÒA BÌNH
                                        _ARENAFIND.WinThisRound = _ARENAFIND.Player_1;
                                        _ARENAFIND.Player_1.IsWinRound = true;
                                        PlayerManager.ShowNotification(_ARENAFIND.Player_1.player, "Trong trận tỉ với " + Bekill.RoleName + " bạn đã dành chiến thắng");
                                    }
                                    if (_ARENAFIND.Player_1.player.RoleID == Bekill.RoleID)
                                    {
                                        _ARENAFIND.Player_1.BestElimateRank = ROUNID;
                                        _ARENAFIND.Player_1.IsWinRound = false;

                                        PlayerManager.ShowNotification(_ARENAFIND.Player_1.player, "Trong trận tỉ với " + Kill.RoleName + " bạn đã thua cuộc");
                                    }    

                                    Action _Action = new Action(() => GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _ARENAFIND.Player_1.player, _Config.GoIn.X, _Config.GoIn.Y, (int)_ARENAFIND.Player_1.player.CurrentDir));
                                    var MoveDelay = MoveDelayTask(_Action, 5000);
                                }    
                                if (_ARENAFIND.Player_2.player != null)
                                {
                                    // set camp cho thằng này = -1 và hòa bình
                                    _ARENAFIND.Player_2.player.Camp = -1;
                                    _ARENAFIND.Player_2.player.PKMode = (int)PKMode.Peace;
                                    if (_ARENAFIND.Player_2.player.RoleID == Kill.RoleID)
                                    {
                                        _ARENAFIND.WinThisRound = _ARENAFIND.Player_2;
                                        _ARENAFIND.Player_2.IsWinRound = true;
                                        PlayerManager.ShowNotification(_ARENAFIND.Player_2.player, "Trong trận tỉ với " + Bekill.RoleName + " bạn đã dành chiến thắng");
                                    }
                                    if (_ARENAFIND.Player_2.player.RoleID == Bekill.RoleID)
                                    {
                                        _ARENAFIND.Player_2.BestElimateRank = ROUNID;
                                        _ARENAFIND.Player_2.IsWinRound = false;

                                        PlayerManager.ShowNotification(_ARENAFIND.Player_2.player, "Trong trận tỉ với " + Kill.RoleName + " bạn đã thua cuộc");
                                    }

                                    Action _Action = new Action(() => GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _ARENAFIND.Player_2.player, _Config.GoIn.X, _Config.GoIn.Y, (int)_ARENAFIND.Player_2.player.CurrentDir));
                                    var MoveDelay = MoveDelayTask(_Action, 5000);
                                }    
                            }

                            if (_ARENAFIND.IsCreateBox == false && _ARENAFIND.WinThisRound != null && _ARENAFIND.ROUNDID == 4)
                            {
                                /// đánh dấu là đã gửi r
                                _ARENAFIND.IsCreateBox = true;
                                //// gửi thưởng cho top 16
                                ///// gửi thưởng qua thư cho top 16
                                //List<LeaguePlayer> leaguePlayers16 = new List<LeaguePlayer>();
                                //leaguePlayers16 = PVPARENA.Values.Where(x => x.BestElimateRank == 1).ToList().Clone();
                                //if (leaguePlayers16.Count > 0 && leaguePlayers16 != null)
                                //{
                                //    foreach (LeaguePlayer player in leaguePlayers16)
                                //    {
                                //        if (!player.AreadyGetAward)
                                //        {
                                //            int Rank = 16;
                                //            /// ghi lại log
                                //            LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                                //
                                //            AwardLeague award = _Config.AwardLeagues.Where(x => x.FromTop <= Rank && x.ToTop >= Rank).FirstOrDefault();
                                //
                                //            if (award != null)
                                //            {
                                //                if (!string.IsNullOrEmpty(award.ListItems))
                                //                {
                                //                    List<GoodsData> goodsDatas = new List<GoodsData>();
                                //
                                //                    string[] listItems = award.ListItems.Split(';');
                                //                    foreach (string item in listItems)
                                //                    {
                                //                        string[] itemInfo = item.Split('_');
                                //                        GoodsData goodData = new GoodsData();
                                //                        goodData.GoodsID = Int32.Parse(itemInfo[0]);
                                //                        goodData.GCount = Int32.Parse(itemInfo[1]);
                                //                        goodData.Binding = Int32.Parse(itemInfo[2]);
                                //                        goodsDatas.Add(goodData);
                                //                    }
                                //                    /// gửi thư về db kể cả đang off
                                //                    string content = "Hệ thống gửi phần thưởng Giải đấu, bạn đạt top  " + Rank + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
                                //                    KTGlobal.SendMail(goodsDatas, player.player.RoleID, null, "Giải đấu", content, 0, 0);
                                //                    /// ghi lại log
                                //                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi đã nhận được thư thưởng top {Rank}, thư gồm {award.ListItems}");
                                //                    player.AreadyGetAward = true;
                                //                }
                                //            }
                                //        }
                                //    }
                                //}
                                //
                                ///// top 8
                                //List<LeaguePlayer> leaguePlayers8 = PVPARENA.Values.Where(x => x.BestElimateRank == 2).ToList().Clone();
                                //if (leaguePlayers8.Count > 0 && leaguePlayers8 != null)
                                //{
                                //    foreach (LeaguePlayer player in leaguePlayers8)
                                //    {
                                //        if (!player.AreadyGetAward)
                                //        {
                                //            int Rank = 8;
                                //            /// ghi lại log
                                //            LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                                //
                                //            AwardLeague award = _Config.AwardLeagues.Where(x => x.FromTop <= Rank && x.ToTop >= Rank).FirstOrDefault();
                                //
                                //            if (award != null)
                                //            {
                                //                if (!string.IsNullOrEmpty(award.ListItems))
                                //                {
                                //                    List<GoodsData> goodsDatas = new List<GoodsData>();
                                //
                                //                    string[] listItems = award.ListItems.Split(';');
                                //                    foreach (string item in listItems)
                                //                    {
                                //                        string[] itemInfo = item.Split('_');
                                //                        GoodsData goodData = new GoodsData();
                                //                        goodData.GoodsID = Int32.Parse(itemInfo[0]);
                                //                        goodData.GCount = Int32.Parse(itemInfo[1]);
                                //                        goodData.Binding = Int32.Parse(itemInfo[2]);
                                //                        goodsDatas.Add(goodData);
                                //                    }
                                //                    /// gửi thư về db kể cả đang off
                                //                    string content = "Hệ thống gửi phần thưởng Giải đấu, bạn đạt top  " + Rank + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
                                //                    KTGlobal.SendMail(goodsDatas, player.player.RoleID, null, "Giải đấu", content, 0, 0);
                                //                    /// ghi lại log
                                //                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi đã nhận được thư thưởng top {Rank}, thư gồm {award.ListItems}");
                                //                    player.AreadyGetAward = true;
                                //                }
                                //            }
                                //        }
                                //
                                //    }
                                //}
                                ///// top 4
                                //List<LeaguePlayer> leaguePlayers4 = PVPARENA.Values.Where(x => x.BestElimateRank == 3).ToList().Clone();
                                //if (leaguePlayers4.Count > 0 && leaguePlayers4 != null)
                                //{
                                //    foreach (LeaguePlayer player in leaguePlayers4)
                                //    {
                                //
                                //        if (!player.AreadyGetAward)
                                //        {
                                //            int Rank = 4;
                                //            /// ghi lại log
                                //            LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                                //
                                //            AwardLeague award = _Config.AwardLeagues.Where(x => x.FromTop <= Rank && x.ToTop >= Rank).FirstOrDefault();
                                //
                                //            if (award != null)
                                //            {
                                //                if (!string.IsNullOrEmpty(award.ListItems))
                                //                {
                                //                    List<GoodsData> goodsDatas = new List<GoodsData>();
                                //
                                //                    string[] listItems = award.ListItems.Split(';');
                                //                    foreach (string item in listItems)
                                //                    {
                                //                        string[] itemInfo = item.Split('_');
                                //                        GoodsData goodData = new GoodsData();
                                //                        goodData.GoodsID = Int32.Parse(itemInfo[0]);
                                //                        goodData.GCount = Int32.Parse(itemInfo[1]);
                                //                        goodData.Binding = Int32.Parse(itemInfo[2]);
                                //                        goodsDatas.Add(goodData);
                                //                    }
                                //                    /// gửi thư về db kể cả đang off
                                //                    string content = "Hệ thống gửi phần thưởng Giải đấu, bạn đạt top  " + Rank + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
                                //                    KTGlobal.SendMail(goodsDatas, player.player.RoleID, null, "Giải đấu", content, 0, 0);
                                //                    /// ghi lại log
                                //                    LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi đã nhận được thư thưởng top {Rank}, thư gồm {award.ListItems}");
                                //                    player.AreadyGetAward = true;
                                //                }
                                //            }
                                //        }
                                //
                                //    }
                                //}
                                //
                                ///// top 2
                                //List<LeaguePlayer> leaguePlayers2 = PVPARENA.Values.Where(x => x.BestElimateRank == 4).ToList().Clone();
                                //foreach (LeaguePlayer player in leaguePlayers2)
                                //{
                                //    if (!player.AreadyGetAward)
                                //    {
                                //        int Rank = 2;
                                //        /// ghi lại log
                                //        LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {player.player.RoleName},  RoleID= {player.player.RoleID} đạt top {Rank} Võ Lâm Minh Chủ lần này!");
                                //
                                //        AwardLeague award = _Config.AwardLeagues.Where(x => x.FromTop <= Rank && x.ToTop >= Rank).FirstOrDefault();
                                //
                                //        if (award != null)
                                //        {
                                //            if (!string.IsNullOrEmpty(award.ListItems))
                                //            {
                                //                List<GoodsData> goodsDatas = new List<GoodsData>();
                                //
                                //                string[] listItems = award.ListItems.Split(';');
                                //                foreach (string item in listItems)
                                //                {
                                //                    string[] itemInfo = item.Split('_');
                                //                    GoodsData goodData = new GoodsData();
                                //                    goodData.GoodsID = Int32.Parse(itemInfo[0]);
                                //                    goodData.GCount = Int32.Parse(itemInfo[1]);
                                //                    goodData.Binding = Int32.Parse(itemInfo[2]);
                                //                    goodsDatas.Add(goodData);
                                //                }
                                //                /// gửi thư về db kể cả đang off
                                //                string content = "Hệ thống gửi phần thưởng Giải đấu, bạn đạt top  " + Rank + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
                                //                KTGlobal.SendMail(goodsDatas, player.player.RoleID, null, "Giải đấu", content, 0, 0);
                                //                /// ghi lại log
                                //                LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi đã nhận được thư thưởng top {Rank}, thư gồm {award.ListItems}");
                                //                player.AreadyGetAward = true;
                                //            }
                                //        }
                                //    }  
                                //}
                                ///// top 1
                                //ELIMINATION_INFO _FINDQUANQUAN = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
                                //int RankQuanQuan = 1;
                                ///// ghi lại log
                                //LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {_FINDQUANQUAN.WinThisRound.player.RoleName},  RoleID= {_FINDQUANQUAN.WinThisRound.player.RoleID} đạt top {RankQuanQuan} Võ Lâm Minh Chủ lần này!");
                                //AwardLeague awardQuanQuan = _Config.AwardLeagues.Where(x => x.FromTop <= RankQuanQuan && x.ToTop >= RankQuanQuan).FirstOrDefault();
                                //
                                //if (awardQuanQuan != null && !_FINDQUANQUAN.WinThisRound.AreadyGetAward)
                                //{
                                //    if (!string.IsNullOrEmpty(awardQuanQuan.ListItems))
                                //    {
                                //        List<GoodsData> goodsDatas = new List<GoodsData>();
                                //
                                //        string[] listItems = awardQuanQuan.ListItems.Split(';');
                                //        foreach (string item in listItems)
                                //        {
                                //            string[] itemInfo = item.Split('_');
                                //            GoodsData goodData = new GoodsData();
                                //            goodData.GoodsID = Int32.Parse(itemInfo[0]);
                                //            goodData.GCount = Int32.Parse(itemInfo[1]);
                                //            goodData.Binding = Int32.Parse(itemInfo[2]);
                                //            goodsDatas.Add(goodData);
                                //        }
                                //        /// gửi thư về db kể cả đang off
                                //        string content = "Hệ thống gửi phần thưởng Giải đấu, bạn đạt top  " + RankQuanQuan + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
                                //        KTGlobal.SendMail(goodsDatas, _FINDQUANQUAN.WinThisRound.player.RoleID, null, "Giải đấu", content, 0, 0);
                                //        /// thêm danh hiệu cho top 1
                                //        _FINDQUANQUAN.WinThisRound.player.AddRoleTitle(117);
                                //        /// ghi lại log
                                //        LogManager.WriteLog(LogTypes.LeagueBattle, $"Người chơi {_FINDQUANQUAN.WinThisRound.player.RoleName} đã nhận được danh hiệu Chiến Thần, và nhận được thư phần thưởng top 1 gồm ({awardQuanQuan.ListItems})!");
                                //        _FINDQUANQUAN.WinThisRound.AreadyGetAward = true;
                                //    }
                                //}
                            }
                        }
                    }
                }
            }
        }

        public void GetAwardBetting(KPlayer player)
        {
            if (GameServer.KiemThe.GameEvents.KuaFuBalance.KuaFuBalanceManager.Instance.IsBalanceMap(player) || GameServer.KiemThe.GameEvents.KuaFuBalance.KuaFuBalanceManager.Instance.IsInBalance(player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Trong Liên Server Cân Bằng không thể thực hiện thao tác này!");
                return;
            }

            /// nếu chưa đến thời gian nhận thưởng thì bỏ qua
            if (BATTLESTATE != LeagueState.CHAMPION_AWARD)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Chưa đến thời gian nhận thưởng cược!");
                return;
            }    
            /// nếu ko có trong danh sách cá cược thì bỏ qua
            if (ListBets.TryGetValue(player.RoleID, out LeagueBet leagueBet))
            {
                /// nếu đã nhận thưởng rồi thì thôi
                if (!leagueBet.HasAward)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã nhận thưởng trước đó");
                    return;
                }
                /// nếu ko đoán đúng người thắng cuộc thì loại
                ELIMINATION_INFO _FINDQUANQUAN = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
                if (leagueBet.BettorID != _FINDQUANQUAN.WinThisRound.player.RoleID)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã đoán sai người giành quán quân giải lần này");
                    return;
                }

                /// tính tổng tiền thua và thắng
                List<LeagueBet> listLose = this.ListBets.Values.Where(x => x.BettorID != _FINDQUANQUAN.WinThisRound.player.RoleID).ToList();
                long totalMoneyLose = 0;
                if (listLose != null)
                {
                    foreach (LeagueBet leagueBetLose in listLose)
                    {
                        totalMoneyLose += leagueBetLose.MoneyBet;
                    }
                }
                List<LeagueBet> listWin = this.ListBets.Values.Where(x => x.BettorID == _FINDQUANQUAN.WinThisRound.player.RoleID).ToList();
                long totalMoneyWin = 0;
                if (listWin != null)
                {
                    foreach (LeagueBet leagueBetWin in listWin)
                    {
                        totalMoneyWin += leagueBetWin.MoneyBet;
                    }
                }
 
                /// tính theo công thức
                int money_refund = (int)Math.Round(leagueBet.MoneyBet / 100000.0, MidpointRounding.AwayFromZero);
                int number_item = 0;
                if (totalMoneyLose > 0 && totalMoneyWin > 0)
                {
                    double calc = (leagueBet.MoneyBet * (double)totalMoneyLose) / (double)totalMoneyWin / 15000.0;
                    number_item = (int)Math.Round(calc, MidpointRounding.AwayFromZero);
                    if (number_item < 0) number_item = 0;
                }
 
                /// dựng reward_str và gửi thư
                List<GoodsData> goodsDatas = new List<GoodsData>();
                if (money_refund > 0)
                {
                    goodsDatas.Add(new GoodsData() { GoodsID = _Config.BetRefundItemID, GCount = money_refund, Binding = 1 });
                }
                if (totalMoneyLose > 0 && number_item > 0)
                {
                    goodsDatas.Add(new GoodsData() { GoodsID = _Config.BetPrizeItemID, GCount = number_item, Binding = 1 });
                }
 
                if (goodsDatas.Count <= 0)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Không có phần thưởng để nhận");
                    return;
                }
 
                string content = "Phần thưởng cược Võ Lâm Minh Chủ";
                KTGlobal.SendMail(goodsDatas, player.RoleID, null, "Giải đấu", content, 0, 0);
 
                /// đánh dấu là đã nhận thưởng
                leagueBet.HasAward = false;
                /// ghi log
                string rewardStr = $"943_{money_refund}_1_0" + ((totalMoneyLose > 0 && number_item > 0) ? $"|25192_{number_item}_1_0" : "");
                LogManager.WriteLog(LogTypes.LeagueBattle, $"BetAward: {player.RoleName}({player.RoleID}) reward={rewardStr} bet={leagueBet.MoneyBet} lose={totalMoneyLose} win={totalMoneyWin}");
            }
            else
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn không có phần thưởng để nhận");
                return;
            }
        }

        private long SubmitRewardMailToQueue(int roleId, string roleName, int sourceServerId, List<GoodsData> goodsData, int boundToken, int boundMoney, string templateId, string templateArgs, string rewardType, int rank)
        {
            string rewardKey = BuildKuaFuRewardKey(sourceServerId, rewardType, roleId, rank);
            KuaFuRewardMailData reward = new KuaFuRewardMailData
            {
                RewardKey = rewardKey,
                EventType = "LeagueBattle-LSV",
                SourceServer = sourceServerId,
                RoleId = roleId,
                TemplateId = templateId,
                TemplateArgs = templateArgs,
                BoundMoney = boundMoney,
                BoundToken = boundToken,
                MailGoods = BuildMailGoodsString(goodsData),
                ItemLog = GetRewardGoodsLog(goodsData),
            };

            KuaFuRewardSubmitResult result = KuaFuRewardClient.getInstance().SubmitMailReward(reward);
            if (result == null || !result.Success)
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, string.Format(
                    "[VoLamMinhChu-KuaFu] Dua thuong vao queue that bai. RewardKey={0}, RoleID={1}, RoleName={2}, ServerGoc={3}, Detail={4}",
                    rewardKey,
                    roleId,
                    roleName,
                    sourceServerId,
                    result != null ? result.Message : "null"));
                return 0;
            }

            LogManager.WriteLog(LogTypes.Mail, string.Format("[VoLamMinhChu-KuaFu][QUEUE]-[{0}][{1}][server:{2}][reward:{3}][template:{4}][args:{5}] [items:{6}]", roleId, roleName, sourceServerId, rewardKey, reward.TemplateId, reward.TemplateArgs, reward.ItemLog));
            return result.RewardId;
        }

        private string BuildTemplateArgs(params object[] keyValues)
        {
            if (keyValues == null || keyValues.Length <= 1)
            {
                return string.Empty;
            }

            List<string> pairs = new List<string>();
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
            {
                pairs.Add(string.Format("{0}={1}", keyValues[i], keyValues[i + 1]));
            }

            return string.Join(";", pairs.ToArray());
        }

        private string BuildKuaFuRewardKey(int sourceServerId, string rewardType, int roleId, int rank)
        {
            long eventTicks = LastTick > 0 ? LastTick : TimeUtil.NOW();
            return string.Format("LeagueBattle-LSV:{0}:{1}:{2}:{3}:{4}:{5}", eventTicks, sourceServerId, FactionID, rewardType, roleId, rank);
        }

        private string BuildMailGoodsString(List<GoodsData> goodsData)
        {
            System.Text.StringBuilder mailGoodsBuilder = new System.Text.StringBuilder();
            if (goodsData == null)
            {
                return string.Empty;
            }

            foreach (GoodsData itemGD in goodsData)
            {
                if (itemGD == null)
                {
                    continue;
                }

                if (mailGoodsBuilder.Length > 0)
                {
                    mailGoodsBuilder.Append("|");
                }

                mailGoodsBuilder.Append(KTMailManager.GoodsToMailGoodsString(itemGD));
            }

            return mailGoodsBuilder.ToString();
        }

        private string GetRewardGoodsLog(List<GoodsData> goods)
        {
            if (goods == null || goods.Count <= 0)
            {
                return "none";
            }

            return string.Join(",", goods.Select(x => string.Format("{0}x{1}", x.GoodsID, x.GCount)).ToArray());
        }

        private List<GoodsData> ParseGoodsString(string goodsStr)
        {
            List<GoodsData> goodsDatas = new List<GoodsData>();
            if (string.IsNullOrEmpty(goodsStr)) return goodsDatas;

            string[] items = goodsStr.Split(';');
            foreach (string item in items)
            {
                string[] parts = item.Split('_');
                if (parts.Length >= 3)
                {
                    GoodsData gd = new GoodsData();
                    gd.GoodsID = Int32.Parse(parts[0]);
                    gd.GCount = Int32.Parse(parts[1]);
                    gd.Binding = Int32.Parse(parts[2]);
                    goodsDatas.Add(gd);
                }
            }
            return goodsDatas;
        }

        private int CalculateRank(int roleId, LeaguePlayer self)
        {
            ELIMINATION_INFO winnerInfo = ELIMINATION_TOTAL.Where(x => x.ROUNDID == 4).FirstOrDefault();
            if (winnerInfo != null && winnerInfo.WinThisRound != null && winnerInfo.WinThisRound == self)
            {
                return 1;
            }
            else if (self.BestElimateRank == 4 && self.IsWinRound == false)
            {
                return 2;
            }
            else if (self.BestElimateRank == 3)
            {
                return 4;
            }
            else if (self.BestElimateRank == 2)
            {
                return 8;
            }
            else if (self.BestElimateRank == 1)
            {
                return 16;
            }
            return -1;
        }

        public void AutoSendRewardMailsKuaFu()
        {
            if (!GameManager.IsKuaFuServer) return;
            if (isMailSentKuaFu) return;

            LogManager.WriteLog(LogTypes.LeagueBattle, "[VoLamMinhChu-KuaFu] Bắt đầu tự động quét phát thư quà cho toàn bộ người chơi lsv...");

            bool hasQueueFailure = false;
            foreach (var kvp in PVPARENA)
            {
                int roleId = kvp.Key;
                LeaguePlayer self = kvp.Value;
                if (self == null) continue;

                int sourceServerId = self.SourceServerId;
                if (sourceServerId <= 0)
                {
                    sourceServerId = self.player != null ? GetSourceServerId(self.player) : GameManager.LocalServerId;
                }
                string roleName = string.IsNullOrEmpty(self.RoleName) ? (self.player != null ? self.player.RoleName : "Đấu thủ") : self.RoleName;

                // 1. Tự động gửi quà Top
                if (!self.AreadyGetAward)
                {
                    int rank = CalculateRank(roleId, self);
                    if (rank != -1)
                    {
                        AwardLeague award = _Config.AwardLeagues.Where(x => x.FromTop <= rank && x.ToTop >= rank).FirstOrDefault();
                        if (award != null && !string.IsNullOrEmpty(award.ListItems))
                        {
                            List<GoodsData> goods = ParseGoodsString(award.ListItems);
                            if (goods.Count > 0)
                            {
                                long rewardId = SubmitRewardMailToQueue(
                                    roleId,
                                    roleName,
                                    sourceServerId,
                                    goods,
                                    0,
                                    0,
                                    KuaFuRewardTemplateIds.LeagueBattleTopReward,
                                    BuildTemplateArgs("rank", rank),
                                    "Top",
                                    rank);
                                hasQueueFailure = hasQueueFailure || rewardId <= 0;
                                if (rewardId > 0)
                                {
                                    self.AreadyGetAward = true;
                                    LogManager.WriteLog(LogTypes.LeagueBattle, $"[VoLamMinhChu-KuaFu] Đã đưa thưởng Top {rank} của {roleName} (Server gốc {sourceServerId}) vào queue. RewardID = {rewardId}");
                                }
                                else
                                {
                                    LogManager.WriteLog(LogTypes.LeagueBattle, $"[VoLamMinhChu-KuaFu] Đưa thưởng Top {rank} của {roleName} (Server gốc {sourceServerId}) vào queue thất bại!");
                                }
                            }
                        }
                    }
                }

                // 2. Tự động gửi quà Tham Gia
                if (!self.ReceivedJoinAward)
                {
                    if (!string.IsNullOrEmpty(_Config.ParticipationReward))
                    {
                        List<GoodsData> goods = ParseGoodsString(_Config.ParticipationReward);
                        if (goods.Count > 0)
                        {
                            long rewardId = SubmitRewardMailToQueue(
                                roleId,
                                roleName,
                                sourceServerId,
                                goods,
                                0,
                                0,
                                KuaFuRewardTemplateIds.LeagueBattleJoinReward,
                                string.Empty,
                                "Join",
                                0);
                            hasQueueFailure = hasQueueFailure || rewardId <= 0;
                            if (rewardId > 0)
                            {
                                self.ReceivedJoinAward = true;
                                LogManager.WriteLog(LogTypes.LeagueBattle, $"[VoLamMinhChu-KuaFu] Đã đưa thưởng Tham gia của {roleName} (Server gốc {sourceServerId}) vào queue. RewardID = {rewardId}");
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.LeagueBattle, $"[VoLamMinhChu-KuaFu] Đưa thưởng Tham gia của {roleName} (Server gốc {sourceServerId}) vào queue thất bại!");
                            }
                        }
                    }
                }
            }

            isMailSentKuaFu = !hasQueueFailure;
            if (hasQueueFailure)
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, "[VoLamMinhChu-KuaFu] Hoan thanh quet thuong LSV nhung con reward chua dua duoc vao queue.");
                return;
            }
            LogManager.WriteLog(LogTypes.LeagueBattle, "[VoLamMinhChu-KuaFu] Đã hoàn thành đưa thưởng LSV vào queue cho server gốc tự gửi mail.");
        }

        public int CheckState()
        {
            return (int)BATTLESTATE;
        }
    }
}
