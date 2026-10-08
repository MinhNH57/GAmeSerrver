using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.Core;
using GameServer.KiemThe.Utilities;
using GameServer.Core.Executor;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using ProtoBuf;
using Server.Protocol;
using Server.TCP;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.RechargeEvent
{
    [ProtoContract]
    [XmlRoot(ElementName = "RechargeEvent")]
    public class RechargeEventConfig
    {
        [XmlElement(ElementName = "TimeKey")]
        public string timeKey = "RechargeEvent";

        [XmlElement(ElementName = "RechargeMilestone")]
        public List<RechargeMilestone> milestones;

        [XmlIgnore]
        public Dictionary<int, Package> Packages { get; set; }

        [XmlIgnore]
        public List<int> claimedPackage { get; set; } = new List<int>();

        [XmlIgnore]
        public Dictionary<string, int> status { get; set; }
    }

    [ProtoContract]
    [XmlRoot(ElementName = "RechargeMilestone")]
    public class RechargeMilestone
    {
        [XmlAttribute(AttributeName = "Name")]
        public string ID;

        [XmlAttribute(AttributeName = "Price")]
        public int price;

        [XmlElement(ElementName = "Package")]
        public List<Package> packages;
    }

    [ProtoContract]
    [XmlRoot(ElementName = "Package")]
    public class Package
    {
        [XmlAttribute(AttributeName = "ID")]
        public int ID;

        [XmlAttribute(AttributeName = "Name")]
        public string name;

        [XmlIgnore]
        public RechargeEventManager.RewardStatus rewardStatus;

        [XmlElement(ElementName = "Item")]
        public List<RewardItem> Items;
    }

    [ProtoContract]
    [XmlRoot(ElementName = "Item")]
    public class RewardItem
    {
        [XmlAttribute(AttributeName = "ID")]
        public int ID;

        [XmlAttribute(AttributeName = "Count")]
        public int Count;
    }

    /// <summary>
    /// Sự kiện nạp thẻ liên tiếp
    /// </summary>
    public class RechargeEventManager : BaseGameEvent<RechargeEventConfig>
    {
        public enum RewardStatus
        {
            NOT_CLAIMABLE,
            NOT_CLAIM,
            CLAIMED
        }

        public static RechargeEventManager Instance { get; } = new RechargeEventManager();

        private const string CONFIG_PATH_OLD = "Config/KT_Interactive/RechargeEvent_OldServer.xml";
        private const string CONFIG_PATH_OPEN = "Config/KT_Interactive/RechargeEvent_OpenServer.xml";

        public override string EventKey => Config?.timeKey ?? "RechargeEvent";

        protected override string ConfigFilePath => EventTimeManager.IsNewServer() ? CONFIG_PATH_OPEN : CONFIG_PATH_OLD;

        protected override RechargeEventConfig ParseConfig(string xmlContent)
        {
            using (var reader = new StringReader(xmlContent))
            {
                var serializer = new XmlSerializer(typeof(RechargeEventConfig));
                var config = (RechargeEventConfig)serializer.Deserialize(reader);

                config.Packages = new Dictionary<int, Package>();
                foreach (var milestone in config.milestones)
                {
                    foreach (var package in milestone.packages)
                    {
                        config.Packages[package.ID] = package;
                    }
                }
                return config;
            }
        }

        public void InitClientData(KPlayer client)
        {
            if (Config == null || !IsActive()) return;

            int nDate = TimeUtil.NowDateTime().DayOfYear;
            var now = TimeUtil.NowDateTime();
            var evt = EventTimeManager.GetEvent(EventKey);

            if (client._DailyRechargeRecord.lastUpdate < evt.StartDate)
            {
                client.ClearRechargeAccumulateEventData();
            }

            DateTime eventDay = evt.StartDate;
            DateTime lastDay = eventDay.AddDays(1).Date;
            int dayId = eventDay.DayOfYear;

            while (dayId != nDate)
            {
                long rechargeAmount = KTGlobal.GetPlayerTotalSumRechargeByTime(client, eventDay.ToString("yyyy-MM-dd HH:mm:ss"), lastDay.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));
                client.SetRechargeValue(dayId, rechargeAmount);
                eventDay = eventDay.AddDays(1).Date;
                lastDay = lastDay.AddDays(1);
                dayId = eventDay.DayOfYear;
            }

            long todayRechargeAmount = KTGlobal.GetPlayerTotalSumRechargeByTime(client, eventDay.Date.ToString("yyyy-MM-dd HH:mm:ss"), now.ToString("yyyy-MM-dd HH:mm:ss"));
            client.SetRechargeValue(nDate, todayRechargeAmount);
        }

        public void RefreshNewRerchargeData(KPlayer client)
        {
            if (Config == null || !IsActive())
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện nạp thẻ liên tiếp lần này đã kết thúc, bằng hữu hãy quay lại sau!");
                return;
            }

            int nDate = TimeUtil.NowDateTime().DayOfYear;
            var now = TimeUtil.NowDateTime();
            var evt = EventTimeManager.GetEvent(EventKey);

            DateTime eventDay = client._DailyRechargeRecord.lastUpdate.Date > evt.StartDate ? client._DailyRechargeRecord.lastUpdate.Date : evt.StartDate;
            DateTime lastDay = eventDay.AddDays(1).Date;
            int dayId = eventDay.DayOfYear;

            while (dayId != nDate)
            {
                long rechargeAmount = KTGlobal.GetPlayerTotalSumRechargeByTime(client, eventDay.ToString("yyyy-MM-dd HH:mm:ss"), lastDay.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));
                client.SetRechargeValue(dayId, rechargeAmount);
                eventDay = eventDay.AddDays(1).Date;
                lastDay = lastDay.AddDays(1);
                dayId = eventDay.DayOfYear;
            }

            DateTime startToday = eventDay.Date > evt.StartDate ? eventDay.Date : evt.StartDate;
            long todayRechargeAmount = KTGlobal.GetPlayerTotalSumRechargeByTime(client, startToday.ToString("yyyy-MM-dd HH:mm:ss"), now.ToString("yyyy-MM-dd HH:mm:ss"));
            client.SetRechargeValue(nDate, todayRechargeAmount);
        }

        public Dictionary<string, int> GetEventStatus(KPlayer client)
        {
            var status = new Dictionary<string, int>();
            if (Config == null) return status;

            foreach (var milestone in Config.milestones)
            {
                int accumulate = client._DailyRechargeRecord.EventRecording.Values.Count(v => v >= milestone.price);
                accumulate -= client.GetAccumulatePreRound(milestone.ID);
                status[milestone.ID] = accumulate;
            }
            return status;
        }

        public TCPProcessCmdResults ProcessRechargeEventInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {
                string cmdData = Encoding.UTF8.GetString(data, 0, count);
                int roleID = Convert.ToInt32(cmdData);

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID) return TCPProcessCmdResults.RESULT_FAILED;

                RefreshNewRerchargeData(client);

                var clientData = new Tuple<Dictionary<string, int>, List<int>>(GetEventStatus(client), client._DailyRechargeRecord.claimedPackage);
                byte[] _cmdData = DataHelper.ObjectToBytes(clientData);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, _cmdData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public TCPProcessCmdResults ProcessReceiveRechargeAccumulateReward(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {
                string cmdData = Encoding.UTF8.GetString(data, 0, count);
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2) return TCPProcessCmdResults.RESULT_FAILED;

                int roleID = Convert.ToInt32(fields[0]);
                int packID = Convert.ToInt32(fields[1]);

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID) return TCPProcessCmdResults.RESULT_FAILED;

                Dictionary<string, int> status = GetEventStatus(client);

                foreach (var milestone in Config.milestones)
                {
                    int milestoneAccum = status[milestone.ID];
                    foreach (var pack in milestone.packages)
                    {
                        if (packID != pack.ID) continue;
                        if (milestone.packages.IndexOf(pack) < milestoneAccum && !client._DailyRechargeRecord.claimedPackage.Contains(pack.ID))
                        {
                            int totalSpackNeed = pack.Items.Sum(x => ItemManager.TotalSpaceNeed(x.ID, x.Count));
                            if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
                            {
                                PlayerManager.ShowNotification(client, "Túi đồ không đủ chỗ");
                                return TCPProcessCmdResults.RESULT_OK;
                            }

                            foreach (var rewardItem in pack.Items)
                            {
                                ItemManager.CreateItem(pool, client, rewardItem.ID, rewardItem.Count, 0, "TICH_LUY_NAP_THE", true, 1, false, Global.ConstGoodsEndTime);
                            }

                            client._DailyRechargeRecord.claimedPackage.Add(pack.ID);
                            if (client.IsTakenAllDaysOfMilestone(milestone))
                            {
                                int totalAccumulate = status[milestone.ID] + client.GetAccumulatePreRound(milestone.ID);
                                client.SetAccumulatePreRound(milestone.ID, totalAccumulate);
                                client.ResetMilestoneRechargeAccumulate(milestone.packages.Select(x => x.ID).ToList());
                            }

                            Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyRechargeRecord, client.DailyRechargeRecordString, true);
                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "1", nID);
                        }
                    }
                }
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }
    }
}
