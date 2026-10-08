using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.Core;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.Logic;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.FlowerTower
{
    public class FlowerTowerConfig
    {
        public string TimeKey;
        public string ComposeTaskID;
        public List<int> ComposeTaskIDs;
        public List<TowerTaskData> Tasks;
        public List<FloorData> Floors;
        public List<SpeedReward> SpeedRewards;
    }

    public class FlowerTowerManager : BaseGameEvent<FlowerTowerConfig>
    {
        public enum FloorStatus
        {
            Lock = -1,
            Unlock_NotDone = 0,
            Unlock_Done_NotReceiveReward = 1,
            Unlock_Done_ReceiveReward = 2,
        }

        private const string FlowerTowerDir = "Config/KT_Interactive/FlowerTower.xml";
        public static readonly FlowerTowerManager Instance = new FlowerTowerManager();

        public override string EventKey => Config?.TimeKey ?? "FlowerTower";
        protected override string ConfigFilePath => FlowerTowerDir;

        protected override FlowerTowerConfig ParseConfig(string xmlContent)
        {
            var xml = XElement.Parse(xmlContent);
            var composeTaskID = xml.Attribute("ComposeTaskID").Value;

            return new FlowerTowerConfig
            {
                TimeKey = xml.Attribute("TimeKey").Value,
                ComposeTaskID = composeTaskID,
                ComposeTaskIDs = composeTaskID.Split(',').Select(int.Parse).ToList(),
                Tasks = ParseAllTasks(xml),
                Floors = ParseAllFloors(xml),
                SpeedRewards = ParseSpeedRewards(xml),
            };
        }

        protected override void RegisterEventListeners()
        {
            GlobalEventSource eventSource = GlobalEventSource.getInstance();
            eventSource.registerListener((int)EventTypes.Join_DuLongChienThu, this);
            eventSource.registerListener((int)EventTypes.ChucPhuc, this);
            eventSource.registerListener((int)EventTypes.OpenRandomBox, this);
            eventSource.registerListener((int)EventTypes.EquipItem, this);
            eventSource.registerListener((int)EventTypes.MonsterDead, this);
            eventSource.registerListener((int)EventTypes.CompletedTask, this);
            eventSource.registerListener((int)EventTypes.Kill_BachHoDuong, this);
            eventSource.registerListener((int)EventTypes.Join_BachHoDuong, this);
            eventSource.registerListener((int)EventTypes.Receive_ThiDauMonPhai, this);
            eventSource.registerListener((int)EventTypes.Receive_TongKimRanking, this);
            eventSource.registerListener((int)EventTypes.Kill_TongKim, this);
            eventSource.registerListener((int)EventTypes.MonsterBloodChanged, this);
            eventSource.registerListener((int)EventTypes.Enhance_ChanNguyen, this);
            eventSource.registerListener((int)EventTypes.RemoveItem, this);
            eventSource.registerListener((int)EventTypes.Join_TieuDaoCoc, this);
            eventSource.registerListener((int)EventTypes.Join_PetRace, this);
            eventSource.registerListener((int)EventTypes.Join_TuLuyenBiCanh, this);
            eventSource.registerListener((int)EventTypes.Join_Fishing, this);
            eventSource.registerListener((int)EventTypes.Compose_Event_Item, this);
            eventSource.registerListener((int)EventTypes.Recharge, this);
            eventSource.registerListener((int)EventTypes.Spin_Lucky, this);
        }

        public override void processEvent(EventObject eventObject)
        {
            EventTypes eventType = (EventTypes)eventObject.getEventType();
            var matchingTasks = Config.Tasks.Where(t => t.EventType == eventType).ToList();

            foreach (var task in matchingTasks)
            {
                ProcessTaskProgress(eventObject, task);
            }
        }

        private static List<TowerTaskData> ParseAllTasks(XElement xml)
        {
            var list = new List<TowerTaskData>();
            foreach (var floorEl in xml.Elements("Floor"))
            {
                int floorId = (int)floorEl.Attribute("id");
                foreach (var taskEl in floorEl.Elements("Task"))
                {
                    list.Add(new TowerTaskData
                    {
                        Id = (int)taskEl.Attribute("id"),
                        Description = (string)taskEl.Attribute("Description"),
                        Goal = (int)taskEl.Attribute("Goal"),
                        Cost = (int)taskEl.Attribute("Cost"),
                        FloorId = floorId,
                        EventType = (EventTypes)(int)taskEl.Attribute("EventType"),
                        Param = (string)taskEl.Attribute("Param") ?? "-1",
                    });
                }
            }
            return list;
        }

        private static List<FloorData> ParseAllFloors(XElement xml)
        {
            var list = new List<FloorData>();
            foreach (var floorEl in xml.Elements("Floor"))
            {
                list.Add(new FloorData
                {
                    Id = (int)floorEl.Attribute("id"),
                    RewardID = (int)floorEl.Attribute("RewardID"),
                });
            }
            return list;
        }

        private static List<SpeedReward> ParseSpeedRewards(XElement xml)
        {
            var list = new List<SpeedReward>();
            foreach (var speedXml in xml.Element("TopSpeed").Elements("Floor"))
            {
                list.Add(new SpeedReward
                {
                    FloorID = KTGlobal.GetXMLAttribute<int>(speedXml, "id"),
                    RewardCount = KTGlobal.GetXMLAttribute<int>(speedXml, "reward_count"),
                    RewardID = KTGlobal.GetXMLAttribute<int>(speedXml, "reward"),
                    CongratMsg = KTGlobal.GetXMLAttribute<string>(speedXml, "message"),
                    Limit = KTGlobal.GetXMLAttribute<int>(speedXml, "count"),
                });
            }
            return list;
        }

        public static List<SpeedReward> SpeedRewardList => Instance.Config?.SpeedRewards;

        private void ProcessTaskProgress(EventObject eventObject, TowerTaskData task)
        {
            EventTypes eventType = (EventTypes)eventObject.getEventType();
            KPlayer player = null;
            int updateValue = 1;

            switch (eventType)
            {
                case EventTypes.Join_TuLuyenBiCanh:
                    if (eventObject is BiCanhEventObject biCanhEvent) player = biCanhEvent.player;
                    break;
                case EventTypes.Join_DuLongChienThu:
                    if (eventObject is DuLongChienThuEventObject duLongChienThuEvent) player = duLongChienThuEvent.player;
                    break;
                case EventTypes.ChucPhuc:
                    if (eventObject is ChucPhucEventObject chucPhucEvent) player = chucPhucEvent.player;
                    break;
                case EventTypes.Compose_Event_Item:
                    if (eventObject is EventComposeEventObject composeEvent)
                    {
                        if (composeEvent.itemID == ComposeEventManager.Instance.GetEventComposeItemID())
                        {
                            player = composeEvent.player;
                            updateValue = composeEvent.multiple;
                        }
                    }
                    break;
                case EventTypes.EquipItem:
                    if (eventObject is EquipItemEventObject equipItemEvent)
                    {
                        player = equipItemEvent.player;
                        KItem item = equipItemEvent.item;
                        if (task.Param.StartsWith("Mantle_"))
                        {
                            int requiredLevel = int.Parse(task.Param.Substring(7));
                            if (item.TypeItem != KE_ITEM_EQUIP_DETAILTYPE.equip_mantle || item._ItemData.Level < requiredLevel) return;
                        }
                        else if (int.TryParse(task.Param, out int targetItemID))
                        {
                            if (item._ItemData.ItemID != targetItemID) return;
                        }
                    }
                    break;
                case EventTypes.MonsterDead:
                    if (eventObject is MonsterDeadEventObject monsterDeadEvent)
                    {
                        Monster monster = monsterDeadEvent.getMonster();
                        player = monsterDeadEvent.getAttacker();
                        if (task.Param == "MainBoss")
                        {
                            if (!monster.IsWorldBoss()) return;
                        }
                        else if (int.TryParse(task.Param, out int monsterID))
                        {
                            if (monster.MonsterInfo.ExtensionID != monsterID) return;
                        }

                        if (player != null && player.Teammates?.Count > 0)
                        {
                            foreach (KPlayer member in player.Teammates) UpdateTaskProgress(member, task.Id, updateValue);
                            return;
                        }
                    }
                    break;
                case EventTypes.CompletedTask:
                    if (eventObject is CompletedTaskEventObject completedTaskEvent)
                    {
                        player = completedTaskEvent.player;
                        if (int.TryParse(task.Param, out int targetTaskClass))
                        {
                            if (completedTaskEvent.taskClass != (TaskClasses)targetTaskClass) return;
                        }
                        else
                        {
                            return;
                        }
                    }
                    break;
                case EventTypes.Join_BachHoDuong:
                    if (eventObject is BachHoDuongJoinEventObject joinEvent) player = joinEvent.player;
                    break;
                case EventTypes.Kill_BachHoDuong:
                    if (eventObject is BachHoDuongKillEventObject killEvent)
                    {
                        player = killEvent.player;
                        if (!(killEvent.victim is KPlayer)) return;
                    }
                    break;
                case EventTypes.Receive_ThiDauMonPhai:
                    if (eventObject is ThiDauMonPhaiReceiveRewardEventObject receiveEvent) player = receiveEvent.player;
                    break;
                case EventTypes.Receive_TongKimRanking:
                    if (eventObject is TongKimReceiveRankingEventObject rankEvent)
                    {
                        player = rankEvent.player;
                        if (int.TryParse(task.Param, out int maxRank))
                        {
                            if (rankEvent.rank < 1 || rankEvent.rank > maxRank) return;
                        }
                    }
                    break;
                case EventTypes.Kill_TongKim:
                    if (eventObject is TongKimKillEventObject tkKillEvent) player = tkKillEvent.player;
                    break;
                case EventTypes.MonsterBloodChanged:
                    if (eventObject is MonsterBloodChangeEventObject bloodEvent)
                    {
                        player = bloodEvent.attacker;
                        if (int.TryParse(task.Param, out int targetMonsterID))
                        {
                            if (bloodEvent.victim.MonsterInfo.ExtensionID == targetMonsterID) updateValue = bloodEvent.oldHP - bloodEvent.newHP;
                            else return;
                        }
                        else
                        {
                            return;
                        }
                    }
                    break;
                case EventTypes.Enhance_ChanNguyen:
                    if (eventObject is ChanNguyenEnhanceEventObject enhanceEvent)
                    {
                        player = enhanceEvent.player;
                        updateValue = enhanceEvent.exp;
                    }
                    break;
                case EventTypes.RemoveItem:
                    if (eventObject is RemoveItemEventObject removeEvent)
                    {
                        player = removeEvent.player;
                        if (int.TryParse(task.Param, out int targetID))
                        {
                            if (removeEvent.itemId != targetID) return;
                        }
                        else
                        {
                            return;
                        }
                    }
                    break;
                case EventTypes.Join_TieuDaoCoc:
                    if (eventObject is TieuDaoCocEventObject tdcEvent) player = tdcEvent.player;
                    break;
                case EventTypes.Join_PetRace:
                    if (eventObject is ThamGiaDuaThuEventObject petRaceEvent)
                    {
                        player = petRaceEvent.player;
                        if (int.TryParse(task.Param, out int requiredBet))
                        {
                            if (petRaceEvent.betPack != requiredBet) return;
                        }
                    }
                    break;
                case EventTypes.Spin_Lucky:
                    if (eventObject is SpinLuckyEventObject spinLuckyEvent)
                    {
                        player = spinLuckyEvent.player;
                        updateValue = spinLuckyEvent.spinAmount;
                    }
                    break;
            }

            if (player != null) UpdateTaskProgress(player, task.Id, updateValue);
        }

        public string GiveTowerReward(KPlayer player, int floorId)
        {
            if (!IsFloorClearByPlayer(player, floorId))
            {
                PlayerManager.ShowNotification(player, "Chưa đủ điều kiện nhận thưởng");
                return "0";
            }

            if (!KTGlobal.IsHaveSpace(1, player))
            {
                PlayerManager.ShowNotification(player, "Hành trang không đủ chỗ trống");
                return "0";
            }

            var floorData = GetFloorById(floorId);
            if (floorData != null)
            {
                if (player.GetStatusOfFloorTower(floorId) == (int)FloorStatus.Unlock_Done_NotReceiveReward)
                {
                    ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, floorData.RewardID, 1, 0, "FLOWER_TOWER", true, 1, false, Global.ConstGoodsEndTime);
                    UnlockNextFloor(player, floorId);
                    player.SetStatusOfFloorTower(floorId, (int)FloorStatus.Unlock_Done_ReceiveReward);

                    LogManager.WriteLog(LogTypes.FlowerTower, $"[FLOWERTOWER]{player.RoleName} [{player.RoleID}] nhận quà tầng {floorId}");

                    var speedConfig = Config.SpeedRewards.FirstOrDefault(x => x.FloorID == floorId);
                    if (speedConfig != null)
                    {
                        KTGlobal.SendSystemEventNotification(string.Format(speedConfig.CongratMsg, player.RoleName));
                        var totalRewardSended = GameManager.GameConfigMgr.GetGameConfigItemInt($"flowertower_{GameManager.ServerId}_speed{floorId}", 0);
                        if (totalRewardSended < speedConfig.Limit)
                        {
                            ItemManager.CreateSimpleItem(player, speedConfig.RewardID, speedConfig.RewardCount, 0, 1);
                            GameManager.GameConfigMgr.UpdateGameConfigItem($"flowertower_{GameManager.ServerId}_speed{floorId}", (totalRewardSended + 1).ToString());
                        }
                    }
                    return "1";
                }
            }
            return "0";
        }

        public FloorData GetFloorById(int floorId) => Config.Floors.FirstOrDefault(f => f.Id == floorId);

        public string SkipTowerTask(KPlayer player, int taskId)
        {
            var task = GetTaskById(taskId);
            if (task == null) return "0";

            var skipPrice = task.Cost;
            if (KTGMCommandManager.IsGM(player) && skipPrice <= -1) skipPrice = 99;
            if (skipPrice <= -1)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Nhiệm vụ không thể bỏ qua được!");
                return "0";
            }

            if (skipPrice <= KTGlobal.GetMoneyhave(player, MoneyType.Dong))
            {
                KTGlobal.SubMoney(player, skipPrice, MoneyType.Dong, "FLOWERTOWER");
                SetTaskProgress(player, taskId, task.Goal);
                return "1";
            }

            PlayerManager.ShowNotification(player, "Không đủ KNB");
            return "0";
        }

        public int[] GetTowerStatus(KPlayer player)
        {
            int[] towerStatus = new int[Config.Floors.Count];
            for (int i = 0; i < Config.Floors.Count; i++)
            {
                var floor = Config.Floors[i];
                if (floor.Id <= 0)
                {
                    towerStatus[i] = (int)FloorStatus.Lock;
                    continue;
                }
                var floorStatus = player.GetStatusOfFloorTower(floor.Id);
                if (i == 0 && floorStatus == (int)FloorStatus.Lock)
                {
                    floorStatus = (int)FloorStatus.Unlock_NotDone;
                    player.SetStatusOfFloorTower(floor.Id, floorStatus);
                }
                if (floorStatus == (int)FloorStatus.Unlock_NotDone && IsFloorClearByPlayer(player, floor.Id))
                {
                    floorStatus = (int)FloorStatus.Unlock_Done_NotReceiveReward;
                    player.SetStatusOfFloorTower(floor.Id, floorStatus);
                }
                towerStatus[i] = floorStatus;
            }
            return towerStatus;
        }

        public void UpdateTaskProgress(KPlayer player, int taskId, int updateValue)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;

            InitialFloorData(player, task.FloorId);
            if (!CanUpdateTaskId(player, taskId)) return;

            LogManager.WriteLog(LogTypes.FlowerTower, $"{player.RoleName}[{player.RoleID}] update {taskId} with value {updateValue}");
            player.SetValueOfTaskProgress(task.Id, GetTaskProgress(player, taskId) + updateValue);
        }

        public void SetTaskProgress(KPlayer player, int taskId, int setValue)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;

            InitialFloorData(player, task.FloorId);
            if (!CanUpdateTaskId(player, taskId)) return;

            LogManager.WriteLog(LogTypes.FlowerTower, $"{player.RoleName}[{player.RoleID}] set {taskId} with value {setValue}");
            player.SetValueOfTaskProgress(task.Id, setValue);
        }

        public void InitialFloorData(KPlayer player, int floorID)
        {
            var floorStatus = player.GetStatusOfFloorTower(floorID);
            if (floorID == 1 && floorStatus == (int)FloorStatus.Lock)
            {
                player.SetStatusOfFloorTower(floorID, (int)FloorStatus.Unlock_NotDone);
            }
            if (player.GetStatusOfFloorTower(floorID) == (int)FloorStatus.Unlock_NotDone && IsFloorClearByPlayer(player, floorID))
            {
                player.SetStatusOfFloorTower(floorID, (int)FloorStatus.Unlock_Done_NotReceiveReward);
            }
        }

        public int GetTaskProgress(KPlayer player, int taskId)
        {
            int progress = player.GetValueOfTaskProgress(taskId);
            if (progress < 0)
            {
                progress = 0;
                player.SetValueOfTaskProgress(taskId, progress);
            }
            return progress;
        }

        public Dictionary<int, int> GetAllTaskProgress(KPlayer player)
        {
            var taskProgress = new Dictionary<int, int>();
            foreach (var task in Config.Tasks) taskProgress.Add(task.Id, GetTaskProgress(player, task.Id));
            return taskProgress;
        }

        public void UpdateKNBTask(KPlayer player)
        {
            if (!IsActive()) return;
            var evt = EventTimeManager.GetEvent(EventKey);
            var totalCharge = KTGlobal.GetPlayerTotalSumRechargeByTime(player, evt.StartDate_str, evt.EndDate_str);
            foreach (var task in Config.Tasks.Where(t => t.EventType == EventTypes.Recharge)) SetTaskProgress(player, task.Id, (int)totalCharge);
        }

        public void UpdateComposeTask(KPlayer player)
        {
            var totalCompose = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MonthlyBox1);
            foreach (var task in Config.Tasks.Where(t => t.EventType == EventTypes.Compose_Event_Item))
            {
                if (Config.ComposeTaskIDs.Contains(task.Id)) SetTaskProgress(player, task.Id, totalCompose);
            }
        }

        public void SetTaskProgressByRoleID(int roleID, int taskID, int progress)
        {
            KPlayer player = GameManager.ClientMgr.FindClient(roleID);
            if (player != null && Config.Tasks.Any(t => t.Id == taskID)) SetTaskProgress(player, taskID, progress);
        }

        private bool IsFloorClearByPlayer(KPlayer player, int floorID) => GetTasksByFloorID(floorID).All(t => GetTaskProgress(player, t.Id) >= t.Goal);

        private List<TowerTaskData> GetTasksByFloorID(int floorId) => Config.Tasks.Where(t => t.FloorId == floorId).ToList();
        private TowerTaskData GetTaskById(int taskId) => Config.Tasks.FirstOrDefault(t => t.Id == taskId);

        private bool CanUpdateTaskId(KPlayer player, int taskId)
        {
            if (!IsActive()) return false;
            try
            {
                var taskData = GetTaskById(taskId);
                if ((FloorStatus)player.GetStatusOfFloorTower(taskData.FloorId) == FloorStatus.Unlock_NotDone)
                    return GetTaskProgress(player, taskId) < taskData.Goal;
                return false;
            }
            catch { return false; }
        }

        private void UnlockNextFloor(KPlayer player, int floorID)
        {
            var currentFloor = GetFloorById(floorID);
            int currentIndex = Config.Floors.IndexOf(currentFloor);
            if (currentIndex < Config.Floors.Count - 1)
            {
                player.SetStatusOfFloorTower(Config.Floors[currentIndex + 1].Id, (int)FloorStatus.Unlock_NotDone);
                UpdateKNBTask(player);
            }
        }
    }

    public class TowerTaskData
    {
        public int Id;
        public string Description;
        public int Goal;
        public int Cost;
        public int FloorId;
        public EventTypes EventType;
        public string Param;
    }

    public class FloorData
    {
        public int Id;
        public int RewardID;
    }

    public class SpeedReward
    {
        public int FloorID;
        public int Limit;
        public int RewardID;
        public int RewardCount;
        public string CongratMsg;
    }
}

namespace GameServer.KiemThe.LuaSystem.Logic
{
    public static partial class KTLuaLib_Event
    {
        public static void UpdateTaskProgress(Lua_Player player, int taskId, int value) => FlowerTowerManager.Instance.UpdateTaskProgress(player.RefObject, taskId, value);
        public static void SetTaskProgress(Lua_Player player, int taskId, int value) => FlowerTowerManager.Instance.SetTaskProgress(player.RefObject, taskId, value);
    }
}