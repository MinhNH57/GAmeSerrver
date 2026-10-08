using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using ProtoBuf;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using UnityEngine.Experimental.Rendering;
using static GameServer.KiemThe.CopySceneEvents.YouLongGe.YouLong.AwardInfo;

namespace GameServer.KiemThe.Core.Task
{
    /// <summary>
    /// Class quản lý nhiệm vụ phụ tuyến
    /// </summary>
    public class SideTaskManager : IEventListener
    {
        public static string KTSideTask = "Config/KT_Task/SideTasks.xml";
        public static string KTSideTaskS29 = "Config/KT_Interactive/SideTaskS29.xml";

        private static SideTaskManager instance = new SideTaskManager();

        public SideTasks sideTasks = new SideTasks();

        private Dictionary<int, SideTask> _TotalTaskData = new Dictionary<int, SideTask>();

        public static SideTaskManager getInstance()
        {
            return instance;
        }

        public void Setup()
        {
            if (GameManager.ServerId < 29)
                return;

            SetupEvent();
            LoadConfig();
        }

        private void SetupEvent()
        {
            GlobalEventSource eventSource = GlobalEventSource.getInstance();
            eventSource.registerListener((int)EventTypes.MonsterDead, getInstance());
            eventSource.registerListener((int)EventTypes.PlayerLevelup, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TuLuyenBiCanh, getInstance());
            eventSource.registerListener((int)EventTypes.Enhance_Weapon, getInstance());
            eventSource.registerListener((int)EventTypes.Enhance_Armor, getInstance());
            eventSource.registerListener((int)EventTypes.CompletedTask, getInstance());
            eventSource.registerListener((int)EventTypes.Join_ThiDauMonPhai, getInstance());
            eventSource.registerListener((int)EventTypes.Kill_BachHoDuong, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TieuDaoCoc, getInstance());
            eventSource.registerListener((int)EventTypes.CheckIn_TongKim, getInstance());
            eventSource.registerListener((int)EventTypes.Kill_TongKim, getInstance());
            eventSource.registerListener((int)EventTypes.Join_DoanHoaDang, getInstance());
            eventSource.registerListener((int)EventTypes.Join_GiaToc, getInstance());
            eventSource.registerListener((int)EventTypes.Crafting, getInstance());
            eventSource.registerListener((int)EventTypes.Capture_Pet, getInstance());
            eventSource.registerListener((int)EventTypes.UseItem, getInstance());
            eventSource.registerListener((int)EventTypes.EquipItem, getInstance());
        }

        private void LoadConfig()
        {
            int currentServer = GameManager.ServerLineID;

            string FileMisson = Global.GameResPath(currentServer >= 1 ? KTSideTaskS29 : KTSideTask);

            using (var stream = System.IO.File.OpenRead(FileMisson))
            {
                var serializer = new XmlSerializer(typeof(SideTasks));
                sideTasks = serializer.Deserialize(stream) as SideTasks;
            }

            _TotalTaskData = sideTasks.sideTasks.ToDictionary(x => x.ID, x => x);
        }

        void IEventListener.processEvent(EventObject eventObject)
        {
            switch ((EventTypes)eventObject.getEventType())
            {
                case EventTypes.MonsterDead:
                    MonsterDeadEventObject monsterEvent = eventObject as MonsterDeadEventObject;

                    var attacker = monsterEvent.getAttacker();
                    var monster = monsterEvent.getMonster();

                    if (attacker.Teammates != null && attacker.Teammates.Count > 0)
                    {
                        foreach (var teammate in attacker.Teammates)
                        {
                            ProcessTask(teammate, EventTypes.MonsterDead, 1, 0, monster.MonsterInfo.ExtensionID);
                        }
                    }
                    else
                    {
                        ProcessTask(monsterEvent.getAttacker(), EventTypes.MonsterDead, 1, 0, monster.MonsterInfo.ExtensionID);
                    }
                    break;
                case EventTypes.PlayerLevelup:
                    PlayerLevelupEventObject playerLevelupEvent = eventObject as PlayerLevelupEventObject;
                    ProcessTask(playerLevelupEvent.Player, EventTypes.PlayerLevelup, 0, playerLevelupEvent.Player.m_Level);
                    break;
                case EventTypes.Join_TuLuyenBiCanh:
                    BiCanhEventObject biCanhEvent = eventObject as BiCanhEventObject;
                    ProcessTask(biCanhEvent.player, EventTypes.Join_TuLuyenBiCanh, 1);
                    break;
                case EventTypes.Enhance_Weapon:
                    EnhanceWeaponEventObject enhanceWeaponEvent = eventObject as EnhanceWeaponEventObject;
                    ProcessTask(enhanceWeaponEvent.player, EventTypes.Enhance_Weapon, 0, enhanceWeaponEvent.enhanceLevel);
                    break;
                case EventTypes.Enhance_Armor:
                    EnhanceArmorEventObject enhanceArmorEvent = eventObject as EnhanceArmorEventObject;
                    ProcessTask(enhanceArmorEvent.player, EventTypes.Enhance_Armor, 0, enhanceArmorEvent.enhanceLevel);
                    break;
                case EventTypes.CompletedTask:
                    CompletedTaskEventObject completedTaskEvent = eventObject as CompletedTaskEventObject;
                    ProcessTask(completedTaskEvent.player, EventTypes.CompletedTask, 1, 0, (int)completedTaskEvent.taskClass);
                    break;
                case EventTypes.Join_ThiDauMonPhai:
                    ThiDauMonPhaiJoinEventObject thiDauMonPhaiJoinEvent = eventObject as ThiDauMonPhaiJoinEventObject;
                    ProcessTask(thiDauMonPhaiJoinEvent.player, EventTypes.Join_ThiDauMonPhai, 1);
                    break;
                case EventTypes.Kill_BachHoDuong:
                    BachHoDuongKillEventObject killBachHoDuongEvent = eventObject as BachHoDuongKillEventObject;
                    ProcessTask(killBachHoDuongEvent.player, EventTypes.Kill_BachHoDuong, 1, 0, killBachHoDuongEvent.victim.RoleID);
                    break;
                case EventTypes.Join_TieuDaoCoc:
                    TieuDaoCocEventObject tieuDaoCocEvent = eventObject as TieuDaoCocEventObject;
                    ProcessTask(tieuDaoCocEvent.player, EventTypes.Join_TieuDaoCoc, 1);
                    break;
                case EventTypes.CheckIn_TongKim:
                    TongKimCheckInEventObject checkInTongKimEvent = eventObject as TongKimCheckInEventObject;
                    ProcessTask(checkInTongKimEvent.player, EventTypes.CheckIn_TongKim, 1);
                    break;
                case EventTypes.Kill_TongKim:
                    TongKimKillEventObject tongKimKillEvent = eventObject as TongKimKillEventObject;
                    if (tongKimKillEvent.victim is KPlayer)
                    {
                        ProcessTask(tongKimKillEvent.player, EventTypes.Kill_TongKim, 1);
                    }
                    break;
                case EventTypes.Join_DoanHoaDang:
                    DoanHoaDangEventObject doanHoaDangEvent = eventObject as DoanHoaDangEventObject;
                    ProcessTask(doanHoaDangEvent.player, EventTypes.Join_DoanHoaDang, 1);
                    break;
                case EventTypes.Join_GiaToc:
                    GiaNhapGiaTocEvntObj giaTocEvent = eventObject as GiaNhapGiaTocEvntObj;
                    ProcessTask(giaTocEvent.player, EventTypes.Join_GiaToc, 1);
                    break;
                case EventTypes.Crafting:
                    CraftingEventObject craftingEvent = eventObject as CraftingEventObject;
                    ProcessTask(craftingEvent.player, EventTypes.Crafting, 1, 0, craftingEvent.itemID);
                    break;
                case EventTypes.Capture_Pet:
                    CapturePetEventObject capturePetEvent = eventObject as CapturePetEventObject;
                    ProcessTask(capturePetEvent.player, EventTypes.Capture_Pet, 1, 0, capturePetEvent.petID);
                    break;
                case EventTypes.UseItem:
                    UseItemEventObject useItemEvent = eventObject as UseItemEventObject;
                    ProcessTask(useItemEvent.player, EventTypes.UseItem, 1, 0, useItemEvent.itemGD.GoodsID);
                    break;
                case EventTypes.EquipItem:
                    EquipItemEventObject equipItemEvent = eventObject as EquipItemEventObject;
                    KItem ItemFind = equipItemEvent.item;
                    ProcessTask(equipItemEvent.player, EventTypes.EquipItem, 1, 0, (int)ItemFind.TypeItem);
                    break;
            }
        }

        #region Task
        /// <summary>
        /// Tạo mới nhiệm vụ phụ tuyến cho player
        /// </summary>
        public void InitTasksForPlayer(KPlayer client)
        {
            if (GameManager.ServerId < 29)
                return;

            var sortedTasks = sideTasks.sideTasks.OrderBy(t => t.ID).ToList();

            if (client._SideTaskRecord.sideTasks == null || client._SideTaskRecord.sideTasks.Count == 0)
            {
                client._SideTaskRecord.sideTasks = new ConcurrentDictionary<int, SideTaskData>();
                foreach (var task in sortedTasks)
                {
                    var sideTaskData = new SideTaskData()
                    {
                        ID = task.ID,
                        isDone = false,
                        CanCollectAward = false,
                        NumberUsed = 0,
                        NumberRepeat = 0
                    };
                    client._SideTaskRecord.sideTasks.TryAdd(task.ID, sideTaskData);
                }
            }
            else
            {
                var validTaskIds = sortedTasks.Select(t => t.ID).ToHashSet();

                var obsoleteTaskIds = client._SideTaskRecord.sideTasks.Keys
                    .Where(id => !validTaskIds.Contains(id))
                    .OrderBy(id => id)
                    .ToList();

                foreach (var id in obsoleteTaskIds)
                {
                    client._SideTaskRecord.sideTasks.TryRemove(id, out _);
                }

                foreach (var task in sortedTasks)
                {
                    if (!client._SideTaskRecord.sideTasks.ContainsKey(task.ID))
                    {
                        var sideTaskData = new SideTaskData()
                        {
                            ID = task.ID,
                            isDone = false,
                            CanCollectAward = false,
                            NumberUsed = 0,
                            NumberRepeat = 0
                        };
                        client._SideTaskRecord.sideTasks.TryAdd(task.ID, sideTaskData);
                    }
                }
            }

            try
            {
                Global.SaveRoleParamsStringToDB(client, RoleParamName.SideTaskRecord, client.SideTaskString, true);
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// Lấy ra thông tin dữ liệu nhiệm vụ đã làm
        /// </summary>
        private static SideTaskData GetSideTaskData(KPlayer client, int ID)
        {
            if (client._SideTaskRecord.sideTasks.ContainsKey(ID))
            {
                return client._SideTaskRecord.sideTasks[ID];
            }

            return null;
        }

        private void ProcessTask(KPlayer client, EventTypes eventType, int numberItemUsed = 0, int numberItemSet = 0, int param = -1)
        {
            var matchingTasks = sideTasks.sideTasks.Where(x => x.EventType == (int)eventType);

            var filteredTasks = matchingTasks
                .Where(task =>
                {
                    if (task.Param == "-1")
                        return true;

                    var taskParams = task.Param.Split(':');
                    return taskParams.Any(p => Int32.Parse(p) == param);
                })
                .ToList();

            foreach (var task in filteredTasks)
            {
                var taskData = GetSideTaskData(client, task.ID);
                if (taskData == null || taskData.isDone || taskData.CanCollectAward)
                    continue;

                switch (task.Type)
                {
                    case 0:
                        taskData.CanCollectAward = true;
                        if (task.IndexRepeat == 0)
                        {
                            taskData.isDone = true;
                        }
                        else
                        {
                            taskData.NumberRepeat++;
                            if (taskData.NumberRepeat >= task.IndexRepeat)
                            {
                                taskData.isDone = true;
                            }
                        }
                        break;

                    case 1:
                        taskData.NumberUsed += numberItemUsed;
                        if (numberItemSet > taskData.NumberUsed)
                        {
                            taskData.NumberUsed = numberItemSet;
                        }
                        if (taskData.NumberUsed >= task.NumberItem)
                        {
                            taskData.CanCollectAward = true;
                            if (task.IndexRepeat == 0)
                            {
                                taskData.isDone = true;
                            }
                            else
                            {
                                taskData.NumberRepeat++;
                                if (taskData.NumberRepeat >= task.IndexRepeat)
                                {
                                    taskData.isDone = true;
                                }
                            }
                        }
                        break;
                }

                if (taskData.isDone)
                {
                    KT_TCPHandler.SendOpenUI(client, "Messages/UISideQuest", Array.Empty<int>());
                }
            }

            try
            {
                Global.SaveRoleParamsStringToDB(client, RoleParamName.SideTaskRecord, client.SideTaskString, false);
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        #endregion

        #region GM

        public void AddValueMission(KPlayer client, int ID, int value)
        {
            var taskData = GetSideTaskData(client, ID);
            taskData.NumberUsed += value;
        }

        public void CompleteMission(KPlayer client, int ID)
        {
            var taskData = GetSideTaskData(client, ID);
            taskData.CanCollectAward = true;
            taskData.isDone = true;

            Global.SaveRoleParamsStringToDB(client, RoleParamName.SideTaskRecord, client.SideTaskString, false);

            PlayerManager.ShowNotification(client, $"Hoàn thành nhiệm vụ {ID} cho {client.RoleName} {client.RoleID}");
        }

        public void CompleteAllMission(KPlayer client)
        {
            foreach (var ID in client._SideTaskRecord.sideTasks.Keys)
            {
                CompleteMission(client, ID);
            }
        }
        public void ResetMission(KPlayer client, int ID)
        {
            var taskData = GetSideTaskData(client, ID);
            taskData.CanCollectAward = false;
            taskData.isDone = false;
            taskData.NumberUsed = 0;
            taskData.NumberRepeat = 0;
        }

        public void ResetAllMission(KPlayer client)
        {
            client._SideTaskRecord.sideTasks.Clear();
            InitTasksForPlayer(client);
        }
        #endregion

        #region Award

        /// <summary>
        /// Thực hiện thêm phần thưởng cho người chơi
        /// </summary>
        private bool AddQuestReward(KPlayer client, int ID)
        {
            var task = sideTasks.sideTasks.FirstOrDefault(x => x.ID == ID);
            if (task == null)
                return false;

            int totalSpaceNeeded = 4;
            var items = new List<AwardInfo>();

            foreach (var item in task.Award)
            {
                if (item.IDItem > 0)
                {
                    if (IsEquipment(item.IDItem, out ItemData itemData))
                    {
                        foreach (ReqProp res in itemData.ListReqProp)
                        {
                            if (res.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_SEX
                                && res.ReqPropValue != client.RoleSex)
                            {
                                return false;
                            }
                        }

                        if (itemData.Series != (int)client.m_Series)
                        {
                            return false;
                        }
                    }

                    if (item.IDItem == 1 || item.IDItem == 2 || item.IDItem == 3 || item.IDItem == 4)
                    {
                        continue;
                    }

                    totalSpaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.IDItem, item.NumberItem);
                    items.Add(item);
                }
            }

            if (items.Count != 0 && !KTGlobal.IsHaveSpace(totalSpaceNeeded, client))
            {
                PlayerManager.ShowNotification(client, $"Cần ít nhất {totalSpaceNeeded} ô trống để mở");
                return false;
            }

            foreach (var item in task.Award)
            {
                if (item.IDItem == -1)
                {
                    GameManager.ClientMgr.ProcessRoleExperience(client, item.NumberItem, true, false, true, "SIDETASK_" + task.ID);
                    continue;
                }

                else if (item.IDItem == -2)
                {
                    int oldPrestige = client.Prestige;
                    client.Prestige += item.NumberItem;
                    LogManager.WriteLog(LogTypes.UyDanh,
                        String.Format("PlayerID: {0},tên: {1} nhận được {2} uy danh ở nhiệm vụ phụ tuyến, uy danh tăng từ {3} lên {4}",
                        client.RoleID, client.RoleName, item.NumberItem, oldPrestige, client.Prestige));
                    continue;
                }

                if (item.IDItem == -3)
                {
                    client.ChangeCurGatherPoint(item.NumberItem);
                    continue;
                }

                if (item.IDItem == -4)
                {
                    client.ChangeCurMakePoint(item.NumberItem);
                    continue;
                }

                if (item.IDItem == -5)
                {
                    client.SetAllLifeSkillLevel(item.NumberItem);
                    continue;
                }

                if (item.IDItem == 1 || item.IDItem == 2 || item.IDItem == 3 || item.IDItem == 4)
                {
                    KTGlobal.AddMoney(client, item.NumberItem, (MoneyType)(item.IDItem - 1), "SIDETASK_" + task.ID);
                    continue;
                }

                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client,
                    item.IDItem, item.NumberItem, 0, "SIDETASK_" + task.ID, false, 1, false,
                    Global.ConstGoodsEndTime, "", -1, "", 0, 1, false))
                {
                    PlayerManager.ShowNotification(client, "Có lỗi khi nhận vật phẩm chế tạo");
                    return false;
                }
            }

            return true;
        }

        private bool IsEquipment(int goodID, out ItemData itemData)
        {
            itemData = ItemManager.GetItemTemplate(goodID);
            if (itemData == null)
                return false;

            return ItemManager.KD_ISEQUIP(itemData.Genre);
        }

        #endregion

        #region TCP Network
        /// <summary>
        /// Class xử lý việc hiện nhiệm vụ phụ tuyến
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessSideTaskInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lam nhieu vu loi , CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            try
            {
                string[] fields = cmdData.Split(':');

                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var sideTaskDatas = new AllSideTaskData();

                var sortedTasks = client._SideTaskRecord.sideTasks
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => kvp.Value)
                    .ToList();

                sideTaskDatas.sideTaskDatas.AddRange(sortedTasks);

                client.SendPacket<AllSideTaskData>(nID, sideTaskDatas);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        /// Class xử lý việc nhận thưởng nhiệm vụ
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessSpriteGetSideTaskAwardGiftCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int CurentStep = Convert.ToInt32(fields[1]);

                SideTaskData sideTaskdata = GetSideTaskData(client, CurentStep);

                if (!sideTaskdata.CanCollectAward)
                {
                    PlayerManager.ShowNotification(client, "Bạn chưa hoàn thành nhiệm vụ!");

                    strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, -1, CurentStep, "");
                    LogManager.WriteLog(LogTypes.SideTask, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút, mốc thưởng {2}",
                                         client.RoleID, client.RoleName, CurentStep));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }
                else
                {

                    if (!SideTaskManager.getInstance().AddQuestReward(client, CurentStep))
                    {
                        strcmd = string.Format("{0}:{1}:{2}:{3}", roleID, -1, CurentStep, "");
                        LogManager.WriteLog(LogTypes.SideTask, String.Format("PlayerID: {0}, tên: {1} lỗi nhận mốc thưởng {2}", client.RoleID, client.RoleName, CurentStep));

                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else
                    {
                        sideTaskdata.CanCollectAward = false;
                        strcmd = string.Format("{0}:{1}:{2}", roleID, 1, CurentStep);
                        LogManager.WriteLog(LogTypes.SideTask, String.Format("PlayerID: {0}, tên: {1} nhận mốc thưởng {2}", client.RoleID, client.RoleName, CurentStep));

                        //Lưu lại vào DB
                        try
                        {
                            Global.SaveRoleParamsStringToDB(client, RoleParamName.SideTaskRecord, client.SideTaskString, true);
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteException(ex.ToString());
                        }
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }


        #endregion
    }
    [ProtoContract]
    public class AllSideTaskData
    {
        [ProtoMember(1)]
        public List<SideTaskData> sideTaskDatas = new List<SideTaskData>();
    }

    [ProtoContract]
    public class SideTaskData
    {
        [ProtoMember(1)]
        public int ID { get; set; }
        [ProtoMember(2)]
        public bool isDone { get; set; } = false;
        [ProtoMember(3)]
        public bool CanCollectAward { get; set; } = false;
        [ProtoMember(4)]
        public int NumberUsed { get; set; }
        [ProtoMember(5)]
        public int NumberRepeat { get; set; }
    }
}
