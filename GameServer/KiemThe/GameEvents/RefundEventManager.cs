using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace GameServer.KiemThe.GameEvents
{
    namespace RefundEvent.Data

    {
        [XmlRoot(ElementName = "RefundEvent")]
        public class RefundEventConfig
        {
            [XmlElement(ElementName = "RefundPack")] public List<RefundPack> refundPacks;
            [XmlAttribute(AttributeName = "TimeKey")] public string timeKey;
            [XmlAttribute(AttributeName = "MaxPurchaseDays")] public int maxPurchaseDays;
        }

        [XmlRoot(ElementName = "RechargeMilestone")]
        public class RefundPack
        {
            [XmlAttribute(AttributeName = "id")] public int id;

            [XmlAttribute(AttributeName = "knb")] public int knb;

            [XmlElement(ElementName = "Milestone")] public List<RefundMilestone> milestones;
        }

        [XmlRoot(ElementName = "Reward")]
        public class RefundMilestone
        {
            [XmlAttribute(AttributeName = "day")] public int day;
            [XmlAttribute(AttributeName = "type")] public string type;
            [XmlAttribute(AttributeName = "rewardId")] public int rewardId;
            [XmlAttribute(AttributeName = "amount")] public int amount;
        }
    }

    public class RefundEventManager
    {
        private const string CONFIG_PATH_OLD = "Config/KT_Interactive/RefundEvent_OldServer.xml";
        private const string CONFIG_PATH_NEW = "Config/KT_Interactive/RefundEvent_NewServer.xml";
        private static RefundEvent.Data.RefundEventConfig Config;


        public static void Init()
        {
            var configPath = CONFIG_PATH_OLD;
            if (EventTimeManager.IsNewServer())
            {
                configPath = CONFIG_PATH_NEW;
            }

            var path = Global.GameResPath(configPath);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(RefundEvent.Data.RefundEventConfig));
                Config = xmlSerializer.Deserialize(fs) as RefundEvent.Data.RefundEventConfig;
            }
        }

        public static void InitClientData(KPlayer client)
        {
            int nDate = TimeUtil.NowDateTime().DayOfYear;
            var now = TimeUtil.NowDateTime();


            EventTimeManager.Event evt = EventTimeManager.GetEvent(Config.timeKey);
            //Clear data sự kiện cũ
            if (evt != null && client.RoleRefundEventData.lastUpdateTime < evt.StartDate)
            {
                client.ResetRefundData();
            }
        }

        public static bool InEventTime() => EventTimeManager.IsEventEnabled(Config.timeKey);

        public static bool CanPurchase()
        {
            if (!InEventTime()) return false;
            var now = TimeUtil.NowDateTime().Day;
            
            EventTimeManager.Event evt = EventTimeManager.GetEvent(Config.timeKey);
            if (evt != null)
            {
                var startDay =  evt.StartDate.Day;
                if(now - startDay < Config.maxPurchaseDays)
                return true;
            }
            return false;
        }

        public static Dictionary<int,Tuple<int,int>> GetEventStatus(KPlayer player)
        {
            Dictionary<int, Tuple<int, int>> status = new Dictionary<int, Tuple<int, int>>();
            foreach (var pack in Config.refundPacks)
            {
                int packId = pack.id;
                int claimedMilestone = player.GetClaimedMilestone(packId);
                int daysElapsed = player.GetDaysElapedSincePackagePurchase(packId);
                status.Add(packId, new Tuple<int, int>(claimedMilestone, daysElapsed));
            }
            return status;
        }

        public static TCPProcessCmdResults ProcessRefundEventInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!InEventTime())
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện này đã kết thúc, bằng hữu hãy quay lại sau!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                byte[] _cmdData = DataHelper.ObjectToBytes(GetEventStatus(client));
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, _cmdData, nID);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ProcessClaimRefundEventReward(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int packID = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if(!InEventTime())
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện này đã kết thúc, bằng hữu hãy quay lại sau!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                int claimedMilestone = client.GetClaimedMilestone(packID);
                int daysElapsed = client.GetDaysElapedSincePackagePurchase(packID);

                var packConfig = Config.refundPacks.Where(x => x.id == packID).FirstOrDefault();
                if(packConfig == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Refund pack config not found, CMD={0}, Client={1}, RoleID={2}, PackID={3}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID, packID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                //Get Reward
                int totalSpackNeed = packConfig.milestones.Where(mil => mil.type == "item" && mil.day <= daysElapsed && mil.day > claimedMilestone).
                        Sum(mil => ItemManager.TotalSpaceNeed(mil.rewardId, mil.amount));
                if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
                {
                    PlayerManager.ShowNotification(client, "Túi đồ không đủ chỗ");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                foreach (var milestone in packConfig.milestones)
                {
                    if (milestone.day <= daysElapsed && milestone.day > claimedMilestone)
                    {
                        
                        if (milestone.type == "item")
                        {
                            ItemManager.CreateItem(pool, client, milestone.rewardId, milestone.amount, 0, "SU_KIEN_HOAN_TRA",
                                true, 1, false, Global.ConstGoodsEndTime);
                        }
                        else if (milestone.type == "money")
                        {
                            KTGlobal.AddMoney(client, milestone.amount, (MoneyType)milestone.rewardId, "SU_KIEN_HOAN_TRA");
                        }

                        claimedMilestone = milestone.day;
                    }
                    
                }
                client.SetClaimRefundMilestoneData(packID, claimedMilestone);

                byte[] _cmdData = DataHelper.ObjectToBytes(GetEventStatus(client));
                GameManager.ClientMgr.SendToClient(client, _cmdData, (int)TCPGameServerCmds.CMD_KT_UPDATE_REFUND_EVENT);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }


        public static TCPProcessCmdResults ProcessPurchaseRefundEventPack(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int packID = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!InEventTime())
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện này đã kết thúc, bằng hữu hãy quay lại sau!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var packConfig = Config.refundPacks.Where(x => x.id == packID).FirstOrDefault();
                if (packConfig == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Refund pack config not found, CMD={0}, Client={1}, RoleID={2}, PackID={3}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID, packID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if(!CanPurchase())
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có thể mua các gói quà trong 7 ngày đầu tiên kể từ khi máy chủ ra mắt!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (!KTGlobal.IsHaveMoney(client, packConfig.knb, MoneyType.Dong))
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Không đủ KNB!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                KTGlobal.SubMoney(client, packConfig.knb, MoneyType.Dong, "Mua gói sự kiện hoàn trả");
                client.SetPurchaseRefundPackData(packID);

                //Log
                LogManager.WriteLog(LogTypes.RefundEvent, $"{client.RoleName} buy refund package {packConfig.knb} KNB, RoleID={roleID}, AccountName= {client.strUserID}, ZoneId={client.ZoneID}");
                GameManager.logDBCmdMgr.WriterLogs(roleID, client.strUserID, "REFUND EVENT", "BUY PACK", "", "", "", client.ZoneID, packConfig.knb.ToString(), "","","", client.ServerId);

                byte[] _cmdData = DataHelper.ObjectToBytes(GetEventStatus(client));
                GameManager.ClientMgr.SendToClient(client, _cmdData, (int)TCPGameServerCmds.CMD_KT_UPDATE_REFUND_EVENT);
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
