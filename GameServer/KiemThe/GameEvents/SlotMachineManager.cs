using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.GameEvents.Core;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.SlotMachine
{
    [XmlRoot("SlotMachineRewards")]
    public class SlotMachineConfig
    {
        [XmlAttribute("TimeKey")] public string TimeKey;
        [XmlAttribute("ExchangeTicketTimeKey")] public string ExchangeTicketTimeKey;
        [XmlAttribute("TicketId")] public int TicketId;
        [XmlAttribute("RewardId")] public int RewardId;
        [XmlAttribute("TicketNeededForX1")] public int TicketNeededForX1;
        [XmlAttribute("TicketNeededForX10")] public int TicketNeededForX10;
        [XmlAttribute("TriggerSysNoti")] public int TriggerSysNoti;

        [XmlElement("Points")] public List<PointsGroup> PointsGroups;
        [XmlElement("NumberLimit")] public List<NumberLimitConfig> NumberLimits;

        [XmlIgnore]
        public List<SlotMachineCondition> Conditions =>
            PointsGroups?.SelectMany(g => g.Conditions.Select(c => new SlotMachineCondition
            {
                Pattern = c.Pattern,
                Points = g.Points,
                Reward = c.Reward,
                ItemReward = c.ItemReward,
            })).ToList() ?? new List<SlotMachineCondition>();
    }

    public class PointsGroup
    {
        [XmlAttribute("value")] public int Points;
        [XmlElement("Condition")] public List<ConditionEntry> Conditions;
    }

    public class ConditionEntry
    {
        [XmlAttribute("Pattern")] public string Pattern;
        [XmlAttribute("Reward")] public int Reward;
        [XmlAttribute("ItemReward")] public int ItemReward = -1;
    }

    public class SlotMachineCondition
    {
        public string Pattern;
        public int Points;
        public int Reward;
        public int ItemReward;
    }

    public class NumberLimitConfig
    {
        [XmlAttribute("StartDate")] public string StartDateStr;
        [XmlAttribute("EndDate")] public string EndDateStr;
        [XmlAttribute("MaxNumber")] public int MaxNumber;

        [XmlIgnore] public DateTime StartDate => DateTime.Parse(StartDateStr);
        [XmlIgnore] public DateTime EndDate => DateTime.Parse(EndDateStr);
    }

    public class SlotMachineManager : BaseGameEvent<SlotMachineConfig>
    {
        private const string SlotMachineDir = "Config/KT_Interactive/SlotMachine.xml";
        public static readonly SlotMachineManager Instance = new SlotMachineManager();

        public string DailyGlobalRecord { get; private set; } = "";
        private ConcurrentDictionary<int, int> _playerLatestNumber;
        private ConcurrentDictionary<int, string> _playerHistory;
        private string _topSlotMaxXml;
        private string _topSlotPointXml;

        public override string EventKey => Config?.TimeKey ?? "SlotMachine";
        protected override string ConfigFilePath => SlotMachineDir;

        public override void Init()
        {
            base.Init();
            EnsureRuntimeState();
        }

        private void EnsureRuntimeState()
        {
            if (_playerLatestNumber == null) _playerLatestNumber = new ConcurrentDictionary<int, int>();
            if (_playerHistory == null) _playerHistory = new ConcurrentDictionary<int, string>();

            ScheduleExecutor2.Instance.scheduleCancle("TopSlotRanking");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopSlotRanking", ReadSlotRanking), 1000, 60000);
        }

        private void ReadSlotRanking()
        {
            _topSlotMaxXml = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_SLOT_MAX, true);
            _topSlotPointXml = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_SLOT_POINT, true);
        }

        public Tuple<int[], string> GetSlotMachineStatus(KPlayer player)
        {
            _playerLatestNumber.TryGetValue(player.RoleID, out var currentNumber);
            var maxNumber = Global.GetRoleParamsInt32FromDB(player, RoleParamName.SlotMachine_MaxNumber, 0);
            var points = Global.GetRoleParamsInt32FromDB(player, RoleParamName.SlotMachine_Point, 0);
            var freeSpinUsed = player.GetValueOfDailyRecore((int)RecoreDef.SlotMachine_FreeSpinUsed);
            _playerHistory.TryGetValue(player.RoleID, out var playerRecord);
            return new Tuple<int[], string>(new int[] { currentNumber, maxNumber, points, freeSpinUsed }, playerRecord);
        }

        public string GetRankingData(string isNumberRanking)
        {
            var xmlStr = isNumberRanking == "1" ? _topSlotMaxXml : _topSlotPointXml;
            try
            {
                var xmlDoc = XElement.Parse(xmlStr);
                var data = new List<string>();
                foreach (var roleXML in xmlDoc.Elements("Role"))
                {
                    data.Add($"{KTGlobal.GetXMLAttribute<string>(roleXML, "rid")}_{KTGlobal.GetXMLAttribute<string>(roleXML, "rname")}_{KTGlobal.GetXMLAttribute<int>(roleXML, "zone")}_{KTGlobal.GetXMLAttribute<int>(roleXML, "score")}");
                }
                return string.Join(":", data);
            }
            catch { return ""; }
        }

        public int[] SpinSlotMachine(KPlayer player, int numberToGen)
        {
            if (!KTGlobal.IsHaveSpace(1, player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Vui lòng để trống ít nhất 1 ô trống trong túi đồ!");
                return new int[1];
            }

            bool isFreeSpinUsed = player.GetValueOfDailyRecore((int)RecoreDef.SlotMachine_FreeSpinUsed) != -1;
            if (!isFreeSpinUsed && numberToGen == 1 && Global.GetMantleLevel(player) >= 5) player.SetValueOfDailyRecore((int)RecoreDef.SlotMachine_FreeSpinUsed, 1);
            else
            {
                var ticketsNeeded = numberToGen == 10 ? Config.TicketNeededForX10 : Config.TicketNeededForX1;
                if (ItemManager.GetItemCountInBag(player, Config.TicketId) < ticketsNeeded) return new int[1];
                ItemManager.RemoveItemFromBag(player, Config.TicketId, ticketsNeeded);
            }

            var genNumber = GenerateSlotMachineNumber(numberToGen);
            if (KTGMCommandManager.IsGM(player))
            {
                int nextNumber = player.GetValueOfDailyRecore((int)RecoreDef.SlotMachine_GMSetNextNumber);
                if (nextNumber != -1)
                {
                    for (int i = 0; i < genNumber.Length; i++) genNumber[i] = nextNumber;
                    player.SetValueOfDailyRecore((int)RecoreDef.SlotMachine_GMSetNextNumber, -1);
                }
            }

            int maxNumber = genNumber.Max();
            _playerLatestNumber.TryAdd(player.RoleID, genNumber.Last());

            if (maxNumber > Global.GetRoleParamsInt32FromDB(player, RoleParamName.SlotMachine_MaxNumber, 0))
            {
                Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.SlotMachine_MaxNumber, maxNumber, false);
            }

            GiveReward(genNumber, player);
            GlobalEventSource.getInstance().fireEvent(new SpinSlotMachineEventObject(player, numberToGen));
            return genNumber;
        }

        public void ReceiveFreeTicket(KPlayer player)
        {
            var evt = EventTimeManager.GetEvent(Config.ExchangeTicketTimeKey);
            if (evt == null || !EventTimeManager.IsEventEnabled(Config.ExchangeTicketTimeKey))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", evt == null ? "Không có thời gian hoạt động." : $"Hoạt động diễn ra từ {evt.StartDate:HH:mm dd/MM} đến {evt.EndDate:HH:mm dd/MM}.");
                return;
            }

            var totalXuCharged = (int)KTGlobal.GetPlayerTotalSumRechargeByTime(player, evt.StartDate_str, evt.EndDate_str);
            var ticketToReceive = totalXuCharged / 100 - Global.GetRoleParamsInt32FromDB(player, RoleParamName.SlotMachine_FreeTicket, 0);

            if (ticketToReceive <= 0)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Mỗi 100 KNB được chuyển vào nhân vật sẽ nhận được 1 vé miễn phí.");
                return;
            }

            if (!KTGlobal.IsHaveSpace(1, player)) return;

            if (ItemManager.CreateSimpleItem(player, Config.TicketId, ticketToReceive, 0, 1, 0))
            {
                Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.SlotMachine_FreeTicket, totalXuCharged / 100, true);
                PlayerManager.ShowMessageBox(player, "Thông báo", $"Bạn đã nhận được {ticketToReceive} Vé Ngũ phúc.");
            }
        }

        private void GiveReward(int[] numbers, KPlayer player)
        {
            int totalReward = 0;
            var totalPoints = Global.GetRoleParamsInt32FromDB(player, RoleParamName.SlotMachine_Point, 0);

            foreach (var number in numbers)
            {
                int sumReward = 0;
                int sumPoints = 0;
                foreach (var condition in Config.Conditions)
                {
                    if (MatchPattern(number, condition.Pattern))
                    {
                        sumReward += condition.Reward;
                        sumPoints += condition.Points;
                        if (condition.ItemReward != -1) ItemManager.CreateSimpleItem(player, condition.ItemReward, 1, 0, 1);
                    }
                }
                AddRecordAndShowNotification(player, number, sumPoints, sumReward);
                totalReward += sumReward;
                totalPoints += sumPoints;

                LogManager.WriteLog(LogTypes.SlotMachine, $"[SLOTMACHINE] {player.RoleName} [{player.RoleID}] spun number: {number:D5}");
                LogManager.WriteLog(LogTypes.SlotMachine, $"[SLOTMACHINE] {player.RoleName} [{player.RoleID}] got {number:D5} received {sumPoints} points and {sumReward} reward.");
                LogManager.WriteLog(LogTypes.SlotMachine, $"[SLOTMACHINE] {player.RoleName} [{player.RoleID}] total points: {totalPoints}");
            }

            if (totalReward > 0) ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, Config.RewardId, totalReward, 0, "SLOT_MACHINE", true, 1, false, Global.ConstGoodsEndTime);
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.SlotMachine_Point, Math.Max(0, totalPoints), false);
        }

        private static bool MatchPattern(int number, string pattern)
        {
            string fmt = number.ToString("D5");
            if (fmt.Length != pattern.Length) return false;
            for (int i = 0; i < pattern.Length; i++) if (pattern[i] != '*' && pattern[i] != fmt[i]) return false;
            return true;
        }

        private void AddRecordAndShowNotification(KPlayer player, int number, int points, int reward)
        {
            _playerHistory.TryGetValue(player.RoleID, out var playerRecord);
            var records = (playerRecord ?? "").Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            records.Add($"{number}_{points}_{reward}");
            if (records.Count > 10) records.RemoveAt(0);
            playerRecord = string.Join(":", records) + ":";
            _playerHistory.AddOrUpdate(player.RoleID, playerRecord, (k, v) => playerRecord);

            if (points >= Config.TriggerSysNoti)
            {
                var globalRecords = DailyGlobalRecord.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                globalRecords.Add($"{player.RoleID}_{player.RoleName}_{number}_{points}_{reward}");
                if (globalRecords.Count > 20) globalRecords.RemoveAt(0);
                DailyGlobalRecord = string.Join(":", globalRecords) + ":";
                KTGlobal.SendSystemEventNotification($"Chúc mừng {player.RoleName} quay trúng {number:D5} nhận {points} điểm và {reward} phiếu thưởng");
            }
        }

        private int[] GenerateSlotMachineNumber(int numberToGen)
        {
            var gen = new int[numberToGen];
            var random = new Random();
            var limit = Config.NumberLimits.FirstOrDefault(l => DateTime.Now >= l.StartDate && DateTime.Now <= l.EndDate);
            int max = limit?.MaxNumber ?? 99999;
            for (int i = 0; i < numberToGen; i++) gen[i] = random.Next(1, max + 1);
            return gen;
        }
    }
}
