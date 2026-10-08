using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Newtonsoft.Json;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GameServer.KiemThe.GameEvents.RechargeEvent
{
    namespace Data
    {
        [XmlRoot(ElementName = "DailyRecharge")]
        public class DailyRechargeEventConfig
        {
            [XmlElement(ElementName = "RechargeMilestone")] public List<RechargeMilestone> milestones;
            [XmlAttribute(AttributeName = "ActiveDay")]
            public int ActiveDay;
            [XmlAttribute(AttributeName = "StartDate")]
            public string startDate;
            [XmlIgnore]
            public Dictionary<string, RechargeMilestone> milestonesDict = new Dictionary<string, RechargeMilestone>();
            [XmlIgnore]
            public DateTime StartDate => DateTime.Parse(startDate, CultureInfo.InvariantCulture);
        }

        [XmlRoot(ElementName = "RechargeMilestone")]
        public class RechargeMilestone
        {
            [XmlAttribute(AttributeName = "ID")] public string ID;

            [XmlAttribute(AttributeName = "KNB")]
            public int KNB;
            [XmlElement(ElementName = "Item")] public List<RewardItem> items;

        }
        
        [XmlRoot(ElementName = "Item")]
        public class RewardItem
        {
            [XmlAttribute(AttributeName = "ID")] public int ID;

            [XmlAttribute(AttributeName = "Count")]
            public int Count;
        }

    }
    public class DailyRechargeEventManager
    {
        
        public enum RewardStatus
        {
            NOT_CLAIMABLE,
            NOT_CLAIM,
            CLAIMED
        }


        private const string CONFIG_PATH = "Config/KT_Interactive/DailyRecharge.xml";
        private static Data.DailyRechargeEventConfig Config;

        public static void Init()
        {
            var path = Global.GameResPath(CONFIG_PATH);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(Data.DailyRechargeEventConfig));
                Config = xmlSerializer.Deserialize(fs) as Data.DailyRechargeEventConfig;
                foreach(var milestone in Config.milestones)
                {
                    Config.milestonesDict.Add(milestone.ID, milestone);
                }
                
                // clear
                Config.milestones.Clear();
            }
        }

        private static int GetMilestoneStatus(KPlayer client, string dayKey, string milestoneId)
        {
            String valueString = Global.GetRoleParamByName(client, RoleParamName.DailyRechargeEvent);
            if (!String.IsNullOrEmpty(valueString))
            {
                byte[] Base64Decode = Convert.FromBase64String(valueString);
                Dictionary<string, Dictionary<string, int>> rewardStatus = DataHelper.BytesToObject<Dictionary<string, Dictionary<string, int>>>(Base64Decode, 0, Base64Decode.Length);
                if (rewardStatus.TryGetValue(dayKey, out var dayRewards))
                {
                    if (dayRewards.TryGetValue(milestoneId, out var status))
                        return status;
                }

            }
            return GetMilestoneStatus_Old(client, dayKey, milestoneId);

        }

        private static void SetMilestoneStatus(KPlayer client, string dayKey, string milestoneId, int status)
        {
            Dictionary<string, Dictionary<string, int>> rewardStatus;
            String valueString = Global.GetRoleParamByName(client, RoleParamName.DailyRechargeEvent);
            if (String.IsNullOrEmpty(valueString))
                rewardStatus = new Dictionary<string, Dictionary<string, int>>();
            else
            {
                byte[] Base64Decode = Convert.FromBase64String(valueString);
                rewardStatus = DataHelper.BytesToObject<Dictionary<string, Dictionary<string, int>>>(Base64Decode, 0, Base64Decode.Length);
            }

            if (!rewardStatus.ContainsKey(dayKey))
            {
                rewardStatus[dayKey] = new Dictionary<string, int>();
            }
            rewardStatus[dayKey][milestoneId] = status;

            byte[] data = DataHelper.ObjectToBytes(rewardStatus);
            string stringData = Convert.ToBase64String(data);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyRechargeEvent, stringData, false);
        }

        private static int GetMilestoneStatus_Old(KPlayer client, string dayKey, string milestoneId)
        {
            var saveKey = $"DailyRecharge_{dayKey}_{milestoneId}";
            int status = Global.GetRoleParamsInt32FromDB(client, saveKey);
            SetMilestoneStatus(client, dayKey, milestoneId, status);//Chuyển data cũ sang data mới
            return status;
        }


        private static void RefreshRechargeData(KPlayer client)
        {
            var today = DateTime.Now.Date;
            for (var i = 0; i < Config.ActiveDay; i++)
            {

                var startDay = today.AddDays(-i);
                if (startDay.Date < Config.StartDate.Date) // Trước ngày bắt đầu sự kiện
                    break;
                var endDay = startDay.AddDays(1).AddSeconds(-1);
                if (startDay < Config.StartDate)
                    startDay = Config.StartDate;
                if (endDay > DateTime.Now)
                    endDay = DateTime.Now;

                var dayKey = startDay.ToString("yyyy_M_dd");
                foreach (var milestoneKPV in Config.milestonesDict)
                {
                    var status = GetMilestoneStatus(client, dayKey, milestoneKPV.Key);
                    if (status == (int)RewardStatus.NOT_CLAIMABLE)
                    {
                        var milestone = milestoneKPV.Value;
                        var rechargeAmount = KTGlobal.GetPlayerTotalSumRechargeByTime(client, startDay.ToString("yyyy-MM-dd HH:mm:ss"), endDay.ToString("yyyy-MM-dd HH:mm:ss"));
                        var rewardStatus = rechargeAmount >= milestone.KNB ? RewardStatus.NOT_CLAIM : RewardStatus.NOT_CLAIMABLE;
                        SetMilestoneStatus(client, dayKey, milestoneKPV.Key, (int)rewardStatus);
                    }

                }

            }
        }

        public static Dictionary<string, int[]> GetRewardStatus(KPlayer client)
        {
            var eventStatus = new Dictionary<string, int[]>();
            var today = DateTime.Now.Date;
            for (var i = 0; i < Config.ActiveDay; i++)
            {

                var startDay = today.AddDays(-i);
                if (startDay.Date < Config.StartDate.Date) // Trước ngày bắt đầu sự kiện
                    break;
                if (startDay < Config.StartDate)
                    startDay = Config.StartDate;

                var dayKey = startDay.ToString("yyyy_M_dd");
                var values = new List<int>(); // danh sách status các mốc nạp 
                foreach (var milestoneKPV in Config.milestonesDict)
                {
                    var status = GetMilestoneStatus(client, dayKey, milestoneKPV.Key);
                    values.Add(status);

                }
                eventStatus.Add(dayKey, values.ToArray());

            }
            return eventStatus;
        }



        public static TCPProcessCmdResults ProcessDailyRechargeEventInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                var fields = cmdData.Split(':');

                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var roleID = Convert.ToInt32(fields[0]);

                var strcmd = "";

                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                RefreshRechargeData(client);

                var clientData = GetRewardStatus(client);
                var _cmdData = DataHelper.ObjectToBytes<Dictionary<string, int[]>>(clientData);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, _cmdData, nID);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ProcessReceiveDailyRechargeReward(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                var fields = cmdData.Split(':');

                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var roleID = Convert.ToInt32(fields[0]);
                var milestoneId = fields[1];
                var day = fields[2];


                var strcmd = "";

                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var rewardStatus = (RewardStatus)GetMilestoneStatus(client,day, milestoneId);
                if(rewardStatus == RewardStatus.NOT_CLAIM)
                {
                    var milestone = Config.milestonesDict[milestoneId];
                    var totalSpackNeed = milestone.items.Sum(x => ItemManager.TotalSpaceNeed(x.ID, x.Count));
                    if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
                    {
                        PlayerManager.ShowNotification(client, "Túi đồ không đủ chỗ");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    //can get this pack
                    foreach (var rewardItem in milestone.items)
                    {
                        ItemManager.CreateItem(pool, client, rewardItem.ID, rewardItem.Count, 0, "NAP_DAU_MOI_NGAY",
                            true, 1, false, Global.ConstGoodsEndTime);
                        //PlayerManager.ShowNotification(client, $"Chúc mừng bạn nhận được vật phẩm {rewardItem.ID}");
                    }
                    SetMilestoneStatus(client, day, milestoneId, (int)RewardStatus.CLAIMED);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "1", nID);
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
