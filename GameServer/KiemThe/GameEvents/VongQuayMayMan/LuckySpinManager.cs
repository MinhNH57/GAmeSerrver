using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.SeashellCircle;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.GameEvents.DailyCheckin.Data;
using GameServer.KiemThe.GameEvents.DoThach;
using GameServer.KiemThe.GameEvents.DoThach.Data;
using GameServer.KiemThe.GameEvents.VongQuayMayMan.Data;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using GameServer.Tools;
using KF.Client;
using KF.Contract.Data;
using ProtoBuf;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine.Playables;
using static GameServer.KiemThe.CopySceneEvents.YouLongGe.YouLong.AwardInfo;
using Ranking = GameServer.KiemThe.GameEvents.VongQuayMayMan.Data.Ranking;

namespace GameServer.KiemThe.GameEvents.VongQuayMayMan
{
    namespace Data
    {
        [XmlRoot("VongQuayMayMan")]
        public class LuckySpinConfig
        {
            [XmlAttribute(AttributeName = "TimeKey")] public string timeKey;
            [XmlAttribute(AttributeName = "PointPerSpin")] public int pointPerSpin;
            [XmlAttribute(AttributeName = "TotalSpaceNeed")] public int totalSpaceNeed;
            [XmlAttribute(AttributeName = "MinSpinDuration")] public int minSpinDuration;
            [XmlAttribute(AttributeName = "MaxSpinDuration")] public int maxSpinDuration;
            [XmlAttribute(AttributeName = "MinSpinRounds")] public int minSpinRounds;
            [XmlAttribute(AttributeName = "MaxSpinRounds")] public int maxSpinRounds;

            [XmlArray("Fees")]
            [XmlArrayItem("Fee")]
            public List<Fee> fees;

            [XmlArray("Rates")]
            [XmlArrayItem("Rate")]
            public List<ItemChance> itemChances;

            [XmlArray("Accumulate")]
            [XmlArrayItem("Milestone")]
            public List<Milestone> milestones;


            [XmlArray("TopReward")]
            [XmlArrayItem("Ranking")]
            public List<Ranking> rankings;
        }

        public class Fee
        {
            [XmlAttribute(AttributeName = "id")]
            public int id;

            [XmlAttribute(AttributeName = "amount")]
            public int amount;

            [XmlAttribute(AttributeName = "spinTimes")]
            public int spinTimes;
        }

        public struct ItemChance
        {
            [XmlAttribute(AttributeName = "index")]
            public int index;

            [XmlAttribute(AttributeName = "item")]
            public int itemId;

            [XmlAttribute(AttributeName = "type")]
            public string type;

            [XmlAttribute(AttributeName = "amount")]
            public int amount;

            [XmlAttribute(AttributeName = "weight")]
            public int weight;

            [XmlAttribute(AttributeName = "notify")]
            public bool notify;

            [XmlAttribute(AttributeName = "binding")]
            public int binding;

            [XmlIgnore]
            public bool bindingSpecified;

        }

        public class Milestone
        {

            [XmlAttribute(AttributeName = "index")]
            public int index;

            [XmlAttribute(AttributeName = "point")]
            public int point;

            [XmlElement("Reward")]
            public List<RewardItem> rewards;
        }

        public class Ranking
        {
            [XmlAttribute(AttributeName = "from")]
            public int from;

            [XmlAttribute(AttributeName = "to")]
            public int to;

            [XmlAttribute(AttributeName = "minPoint")]
            public long minPoint;

            [XmlElement("Reward")]
            public List<RewardItem> rewards;
        }

        public class RewardItem
        {
            [XmlAttribute(AttributeName = "item")]
            public int itemId;

            [XmlAttribute(AttributeName = "type")]
            public string type;

            [XmlAttribute(AttributeName = "amount")]
            public int amount;

            [XmlAttribute(AttributeName = "binding")]
            public int binding;

            [XmlIgnore]
            public bool bindingSpecified;
        }

        public class NotificationInfo
        {
            public int id;
            public string playerName;
            public string itemName;
            public int awardKnb;

            public NotificationInfo(int id, string playerName, string itemName, int awardKnb)
            {
                this.id = id;
                this.playerName = playerName;
                this.itemName = itemName;
                this.awardKnb = awardKnb;
            }
        }
    }

    class LuckySpinManager
    {
        public const string ConfigPath = "Config/KT_Interactive/VongQuayMayMan.xml";

        private static LuckySpinConfig Config;

        private static Dictionary<int, KPlayer> NotificationSubscribers;
        private static Queue<NotificationInfo> NotificationPool = new Queue<NotificationInfo>();
        private static bool isRankRewardPaid;

        public static void Init()
        {
            var path = Global.GameResPath(ConfigPath);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(LuckySpinConfig));
                Config = xmlSerializer.Deserialize(fs) as LuckySpinConfig;
            }
            NotificationSubscribers = new Dictionary<int, KPlayer>();

            // nếu đã hết thời gian sự kiện thì đánh dấu đã trả thưởng xếp hạng rồi
            if (!InEventTime())
            {
                isRankRewardPaid = true;
                LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Ngoai thoi gian su kien");
            }

            // request đọc lịch sử jackpot từ center 10'/lần nếu kết nối được thì ngừng request
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("JackpotDump", LoadServiceData), 10000,
                    10000);



        }

        private static string TopAccumulatePointXML;

        private static long jarAmount = 3000;
        public static void SetJarAmount(long newAmount)
        {
            System.Threading.Interlocked.Exchange(ref jarAmount, newAmount);
        }
        public static void LoadTopRanking(List<TopRoleInfo> rankingList)
        {
            //parse ra xml
            if (rankingList == null || rankingList.Count == 0)
            {
                return;
            }
            XElement rankingXml = new XElement("Ranks");

            foreach (var rank in rankingList)
            {
                rankingXml.Add(new XElement("Rank",
                    new XAttribute("Top", rank.Rank),
                    new XAttribute("PlayerId", rank.Rid),
                    new XAttribute("PlayerName", rank.Rname),
                    new XAttribute("ServerId", rank.ZoneId),
                    new XAttribute("Point", rank.Pvalue)
                ));
            }
            TopAccumulatePointXML = rankingXml.ToString();
            //kết thúc sự kiện
            if (!InEventTime())
            {
                OnEndEvent(rankingList);
            }
        }


        private static void OnEndEvent(List<TopRoleInfo> rankingList)
        {
            ScheduleExecutor2.Instance.scheduleCancle("JackpotDump");
            int totalUsers = rankingList != null ? rankingList.Count : 0;
            //Nếu đã trả thưởng xếp hạng rồi thì thôi
            if (isRankRewardPaid) return;

            isRankRewardPaid = true;
            LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Tiến hành trả thưởng xếp hạng cho {totalUsers} users.");
            //Kết thúc sự kiện gửi mail thưởng top ranking
            var data = XElement.Parse(TopAccumulatePointXML);
            foreach (TopRoleInfo info in rankingList)
            {
                int top = info.Rank;
                int playerId = info.Rid;
                string playerName = info.Rname;
                long point = info.Pvalue;
                int serverId = info.ZoneId;


                Ranking ranking = GetRanking(top, point);
                if (ranking != null)
                {
                    var goodsData = new List<GoodsData>();
                    int boundToken = 0;
                    int boundMoney = 0;
                    foreach (var reward in ranking.rewards)
                    {
                        if (reward.type == "money")
                        {
                            if (reward.itemId == (int)MoneyType.DongKhoa)
                            {
                                boundToken += reward.amount;
                            }
                            else if (reward.itemId == (int)MoneyType.BacKhoa)
                            {
                                boundMoney += reward.amount;
                            }
                        }
                        else if (reward.type == "item")
                        {
                            var goodData = new GoodsData
                            {
                                GoodsID = reward.itemId,
                                Binding = GetBinding(reward.binding, reward.bindingSpecified),
                                GCount = reward.amount
                            };
                            goodsData.Add(goodData);
                        }

                    }

                    // gửi thư về db kể cả đang off
                    if (GameManager.ServerId == serverId)
                    {
                        var title = "Thưởng xếp hạng sự kiện Tân Niên Bí Bảo";
                        var content = $"Chúc mừng {playerName} đã đạt Top {top} sự kiện Tân Niên Bí Bảo. Vui lòng nhận thưởng đính kèm trong thư.";
                        int sendMailResult = KTGlobal.SendMail(goodsData, playerId, playerName, title, content, boundToken, boundMoney);
                        if (sendMailResult > 0)
                        {
                            LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Đã trả thưởng về mail người chơi {playerName}:{playerId} đã đạt Top {top}");
                        }
                        else
                            LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Lỗi trả thưởng về mail người chơi {playerName}:{playerId} đã đạt Top {top}");
                    }


                    //Log thông tin thưởng để trả trưởng bằng tay test trong giai đoạn demo 
                    string rewardStr = string.Join(",", ranking.rewards.Select(x => $"{x.type}:{x.itemId}:{x.amount}"));
                    LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Yêu cầu trả thưởng về mail cho người chơi {playerName}:{playerId} được {info.Pvalue} điểm đã đạt Top {top}, danh sách phần thưởng <{rewardStr}>");
                }
                else
                    LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Người chơi {playerName}:{playerId} được {info.Pvalue} đạt Top {top} chưa đủ điều kiện để nhận thưởng");
            }

            int clearedRows = JackpotClient.getInstance().ClearLuckySpinServerPointData();
            LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Yeu cau Center xoa toan bo LuckySpinPoint sau khi phat thuong server {GameManager.ServerId}, Rows={clearedRows}");
            long resetJarAmount = JackpotClient.getInstance().ResetJarAmount();
            LogManager.WriteLog(LogTypes.LuckySpin, $"LuckySpin: Yeu cau Center reset jackpot sau khi ket thuc su kien server {GameManager.ServerId}, JarAfter={resetJarAmount}");
        }

        private static void LoadServiceData()
        {
            try
            {
                //Load hết 20 thông báo jackpot gần nhất

                if (JackpotClient.getInstance().isConnected)
                {
                    // ngừng request
                    ScheduleExecutor2.Instance.scheduleCancle("JackpotDump");

                    // lấy danh sách notification

                    var listNotifications = JackpotClient.getInstance().GetJackpotHistory();
                    foreach (var r in listNotifications)
                    {
                        jarAmount = r.JarAfter;
                        if (r.notify)
                        {
                            NotificationInfo notification = new NotificationInfo(r.ID, r.PlayerName, r.Message, (int)r.AwardKnb);
                            AddNotification(notification);
                        }

                    }

                    // Lấy danh sách top bxh

                    //get top ranking sau khi sự kiện kết thúc
                    List<TopRoleInfo> topRoles = JackpotClient.getInstance().GetTopRankings();
                    LoadTopRanking(topRoles);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, ex.ToString());
            }

        }

        private static Ranking GetRanking(int top, long point)
        {
            foreach (Ranking ranking in Config.rankings)
            {
                if (top <= ranking.to && point >= ranking.minPoint)
                {
                    return ranking;
                }
            }
            return null;
        }

        public static void InitClientData(KPlayer client)
        {
            int nDate = TimeUtil.NowDateTime().DayOfYear;
            var now = TimeUtil.NowDateTime();


            EventTimeManager.Event evt = EventTimeManager.GetEvent(Config.timeKey);
            //Clear data sự kiện cũ
            if (evt != null && client.RoleLuckySpinData.lastUpdateTime < evt.StartDate)
            {
                client.ResetLuckySpinData();
            }
        }

        // Client đăng kí nhận thông báo
        public static void SubcribeNotification(KPlayer player)
        {
            lock (NotificationSubscribers)
            {
                if (!NotificationSubscribers.ContainsKey(player.RoleID))
                {
                    NotificationSubscribers.Add(player.RoleID, player);
                }
            }

        }

        // Client hủy đăng kí nhận thông báo
        public static void UnsubcribeNotification(int roleID)
        {
            lock (NotificationSubscribers)
            {
                if (NotificationSubscribers.ContainsKey(roleID))
                {
                    NotificationSubscribers.Remove(roleID);
                }
            }

        }

        // Xoá những client đã đăng xuất khỏi danh sách nhận thông báo
        private static void RemoveLoggedOutSubscribers()
        {
            lock (NotificationSubscribers)
            {
                var loggedOutSubscribers = NotificationSubscribers.Where(x => x.Value.LogoutState).Select(x => x.Key).ToList();
                foreach (var roleID in loggedOutSubscribers)
                {
                    NotificationSubscribers.Remove(roleID);
                }
            }

        }

        // Thêm thông báo mới vào pool
        public static void AddNotification(NotificationInfo result)
        {

            lock (NotificationPool)
            {
                foreach (var notification in NotificationPool)
                {
                    if (notification.id == result.id)
                        return;
                }

                if (NotificationPool.Count >= 20)
                    NotificationPool.Dequeue();

                NotificationPool.Enqueue(result);
            }

        }


        // Gửi thông báo mới tới các client đã đăng kí
        public static void PushNewNotification(long jarAfter, int id, string playerName, string itemName, int awardKnb, int notify)
        {
            NotificationInfo notification = new NotificationInfo(id, playerName, itemName, awardKnb);
            jarAmount = jarAfter;
            RemoveLoggedOutSubscribers();
            lock (NotificationSubscribers)
            {
                string notificationCmd = "";
                if (notify == 1)
                {
                    AddNotification(notification);
                    notificationCmd = $"{id}-{playerName}-{itemName}-{awardKnb}";
                }

                foreach (var subscriber in NotificationSubscribers)
                {
                    string cmd = $"{jarAfter}:{notificationCmd}";
                    GameManager.ClientMgr.SendToClient(subscriber.Value, cmd, (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_NOTIFICATION);
                }
            }

        }

        // Gửi toàn bộ thông báo trong pool tới người chơi
        public static void PushAllNotifications(KPlayer player)
        {
            RemoveLoggedOutSubscribers();
            lock (NotificationPool)
            {
                string notificationCmd = string.Join(":", NotificationPool.Select(x => $"{x.id}-{x.playerName}-{x.itemName}-{x.awardKnb}"));
                notificationCmd = $"{jarAmount}:{notificationCmd}";
                GameManager.ClientMgr.SendToClient(player, notificationCmd, (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_NOTIFICATION);

            }
        }

        //Lấy dữ liệu trạng thái nhận thưởng tích lũy(0: chưa đủ điều kiện, 1: chưa nhận, 2: đã nhận)
        private static Dictionary<int, int> GetAccummulateStatus(KPlayer player)
        {
            Dictionary<int, int> status = new Dictionary<int, int>();
            foreach (var milestone in Config.milestones)
            {
                bool claimed = player.GetMilestoneClaimedStatus_LuckySpin(milestone.index);
                bool claimable = player.GetAccumulatedPoints_LuckySpin() >= milestone.point;
                int statusValue = claimed ? 2 : (claimable ? 1 : 0);
                status.Add(milestone.index, statusValue);
            }
            return status;
        }

        private static string GetAccumulateStatusString(KPlayer player)
        {
            return string.Join("|", GetAccummulateStatus(player).Select(x => $"{x.Key},{x.Value}"));
        }

        public static bool InEventTime()
        {
            EventTimeManager.Event evt = EventTimeManager.GetEvent(Config.timeKey);
            if (evt != null)
            {
                var now = TimeUtil.NowDateTime();
                return now >= evt.StartDate && now <= evt.EndDate;
            }
            return false;
        }


        // Lấy vật phẩm ngẫu nhiên dựa trên tỉ lệ
        private static Data.ItemChance GetRandomItem()
        {

            float random = (float)Global.GetRandomNumber(0, 1000);

            int allChances = 0;
            foreach (var itemChance in Config.itemChances)
            {
                allChances += itemChance.weight;
            }

            float chancePart = 1000f / allChances;
            float checkedChances = 0f;

            foreach (var itemChance in Config.itemChances)
            {
                checkedChances += chancePart * itemChance.weight;
                if (random < checkedChances)
                {
                    return itemChance;
                }
            }
            return default;
        }

        private static Data.ItemChance GetJackpotItem(string type)
        {
            foreach (var itemChance in Config.itemChances)
            {
                if (itemChance.type == type)
                {
                    return itemChance;
                }
            }
            return default;
        }

        private static int GetBinding(int binding, bool bindingSpecified)
        {
            if (!bindingSpecified)
            {
                return 1;
            }

            return binding == 0 ? 0 : 1;
        }

        // trả thông tin điểm tích lũy, trạng thái nhận thưởng tích lũy về cho client
        public static TCPProcessCmdResults ProcessLuckySpinInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                int openLuckySpin = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (openLuckySpin == 1) // nếu mở popup thì đăng kí nhận thông báo
                {
                    SubcribeNotification(client);
                    PushAllNotifications(client);
                }
                else //nếu đóng popup thì hủy đăng kí nhận thông báo
                {
                    UnsubcribeNotification(client.RoleID);
                }

                if (!InEventTime())
                {
                    if (openLuckySpin == 1)
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện này đã kết thúc, bằng hữu hãy quay lại sau!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                strcmd = $"{client.GetAccumulatedPoints_LuckySpin()}:{GetAccumulateStatusString(client)}";

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        // Xử lí lệnh quay luckyspin
        public static TCPProcessCmdResults ProcessSpinCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                int spinTimes = Convert.ToInt32(fields[1]);

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
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "-1", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                Data.Fee fee = Config.fees.FirstOrDefault(f => f.spinTimes == spinTimes);
                if (fee == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find fee roll Lucky Spin corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (!KTGlobal.IsHaveMoney(client, fee.amount, (MoneyType)fee.id))
                {
                    LogManager.WriteLog(LogTypes.LuckySpin, string.Format("Not enough money corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "money", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (!KTGlobal.IsHaveSpace(Config.totalSpaceNeed, client))
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Hành trang đầy, cần làm trống tối thiểu {Config.totalSpaceNeed} ô để tiến hành quay");
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "-1", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (!JackpotClient.getInstance().isConnected)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Kết nối với Center Server bị gián đoạn. Máy chủ có thể sẽ bảo trì trong ít phút.");
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "-1", nID);
                    LogManager.WriteLog(LogTypes.Error, string.Format("Lucky Spin: Center server connection error CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                KTGlobal.SubMoney(client, fee.amount, (MoneyType)fee.id, "LUCKY_SPIN");

                int finalPostion = 0;
                List<Data.ItemChance> rewards = new List<Data.ItemChance>();
                for (int i = 0; i < spinTimes; i++)
                {
                    Data.ItemChance item = GetRandomItem();
                    if (client.NextSpinJackpot > 0) // Nếu được GM buff nổ jackpot 
                    {
                        item = GetJackpotItem(client.NextSpinJackpot == 2 ? "jackpot_big" : "jackpot_small");
                        client.NextSpinJackpot = 0;
                    }
                    if (item.itemId != 0) // k roll được vật phẩm trả ra default
                    {
                        if (item.type == "item")
                        {
                            ItemManager.CreateItem(pool, client, item.itemId, item.amount, 0, "LUCKY_SPIN",
                            true, GetBinding(item.binding, item.bindingSpecified), false, Global.ConstGoodsEndTime, HitItem: 0);
                        }
                        else if (item.type == "money")
                        {
                            KTGlobal.AddMoney(client, item.amount, (MoneyType)item.itemId, "LUCKY_SPIN", false);
                        }

                        rewards.Add(item);
                        finalPostion = item.index - 1;

                        ////gửi sự kiện quay lên center để check hũ
                        int knbPerSpin = Config.fees[0].amount;
                        string rewardName = KTGlobal.GetItemName(item.type == "money" ? item.itemId + 1 : item.itemId);
                        bool isBigWin = item.type == "jackpot_big";
                        bool isSmallWin = item.type == "jackpot_small";
                        var result = JackpotClient.getInstance().TryHitKnbJackpot(client.RoleID, client.RoleName, knbPerSpin, rewardName, isBigWin, isSmallWin, item.notify);
                        if (result != null)
                        {
                            string jackpotCmd = $"{(isBigWin ? 1 : 0)}:{(isSmallWin ? 1 : 0)}:{result.AwardKnb}:{result.JarAfter}:{result.Message}";
                            GameManager.ClientMgr.SendToClient(client, jackpotCmd, (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_JACKPOT_HIT);

                            if (result.AwardKnb > 0)
                            {
                                KTGlobal.AddMoney(client, (int)result.AwardKnb, MoneyType.Dong, "LUCKY_SPIN_JACKPOT", false);
                            }

                            //Gửi thông báo về lark
                            if (isBigWin || isSmallWin || item.itemId == 2017)
                            {
                                GoodsData goods = new GoodsData
                                {
                                    GoodsID = item.itemId,
                                    Binding = GetBinding(item.binding, item.bindingSpecified),
                                    GCount = item.amount
                                };
                                string percentText = $"chiếm {(result.AwardKnb * 100 / result.JarBefore).ToString("F1")}% hũ";
                                string moreText = result.AwardKnb > 0 ? $"bên trong chứa {result.AwardKnb} KNB {percentText}" : "";
                                string larkNoti = $"Người chơi {client.RoleName}({client.RoleID}) vừa quay trúng {KTGlobal.GetItemName(goods)} {moreText}";
                                LarkNotificationHelper.PushNormalNotification(larkNoti);
                            }

                            LogManager.WriteLog(LogTypes.LuckySpin, $"Lucky Spin: Người chơi {client.RoleName} quay vòng quay may mắn nhận được {item.type}<{item.itemId}>.");

                        }
                        else
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("JackpotTry: result null for playerId={0}, bet={1}, isBig={2}, isSmall={3}", client.RoleID, knbPerSpin, isBigWin, isSmallWin));
                        }
                    }
                }

                string rewardsStr = string.Join("|", rewards.Select(x => x.index));

                float duration = KTGlobal.GetRandomNumber(Config.minSpinDuration, Config.maxSpinDuration);
                // Số vòng quay
                int totalRounds = KTGlobal.GetRandomNumber(Config.minSpinRounds, Config.maxSpinRounds);
                int cellsPerRound = Config.itemChances.Count;

                int lastSpinPosition = client.GetLastSpinPosition_LuckySpin();
                int accumulatePoints = client.GetAccumulatedPoints_LuckySpin();
                int totalCells = finalPostion + totalRounds * cellsPerRound - lastSpinPosition;
                strcmd = $"{rewardsStr}:{lastSpinPosition}:{totalCells}:{duration}";

                client.SetLastSpinPosition_LuckySpin(finalPostion);
                client.SetAccumulatedPoints_LuckySpin(accumulatePoints + spinTimes * Config.pointPerSpin);

                GlobalEventSource.getInstance().fireEvent(new SpinLuckyEventObject(client, spinTimes));

                string updateCmdData = $"{client.GetAccumulatedPoints_LuckySpin()}:{GetAccumulateStatusString(client)}";
                GameManager.ClientMgr.SendToClient(client, updateCmdData, (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_STATUS);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        // Xử lí lệnh nhận thưởng tích lũy
        public static TCPProcessCmdResults ProcessLuckySpinClaimAccumulateCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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
                int milestoneIndex = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!JackpotClient.getInstance().isConnected)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Kết nối với Center Server bị gián đoạn. Máy chủ có thể sẽ bảo trì trong ít phút.");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (!InEventTime())
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện này đã kết thúc, bằng hữu hãy quay lại sau!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                Milestone milestone = Config.milestones.FirstOrDefault(x => x.index == milestoneIndex);
                if (milestone == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find lucky spin accumulate milestone index: {0} corresponding ID, CMD={1}, Client={2}, RoleID={3}", milestoneIndex, (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int myAcummulatePoint = client.GetAccumulatedPoints_LuckySpin();
                if (milestone.point > myAcummulatePoint)
                {
                    //blocked request UI on client
                    LogManager.WriteLog(LogTypes.Error, string.Format("Player not enought lucky spin accumulate but try to request receive reward to server corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                if (client.GetMilestoneClaimedStatus_LuckySpin(milestoneIndex))
                {
                    //blocked request UI on client
                    LogManager.WriteLog(LogTypes.Error, string.Format("Player already claimed lucky spin accumulate milestone index: {0} corresponding ID, CMD={1}, Client={2}, RoleID={3}", milestoneIndex, (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int totalSpackNeed = milestone.rewards.Where(x => x.type == "item").Sum(x => ItemManager.TotalSpaceNeed(x.itemId, x.amount));

                if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Túi đồ không đủ chỗ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                foreach (Data.RewardItem reward in milestone.rewards)
                {
                    if (reward.type == "item")
                    {
                        ItemManager.CreateItem(pool, client, reward.itemId, reward.amount, 0, "LUCKY_SPIN_ACCUMULATE",
                            true, GetBinding(reward.binding, reward.bindingSpecified), false, Global.ConstGoodsEndTime);
                    }
                    else if (reward.type == "money")
                    {
                        KTGlobal.AddMoney(client, reward.amount, (MoneyType)reward.itemId, "LUCKY_SPIN_ACCUMULATE");
                    }
                }
                client.SetMilestoneClaimedStatus_LuckySpin(milestoneIndex, true);


                strcmd = $"{client.GetAccumulatedPoints_LuckySpin()}:{GetAccumulateStatusString(client)}";

                GameManager.ClientMgr.SendToClient(client, strcmd, (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_STATUS);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        // Xử lí lệnh lấy bảng xếp hạng tích lũy
        public static TCPProcessCmdResults ProcessLuckySpinRankingCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
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

                if (!JackpotClient.getInstance().isConnected)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Kết nối với Center Server bị gián đoạn. Máy chủ có thể sẽ bảo trì trong ít phút.");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (!InEventTime())
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Sự kiện này đã kết thúc, bằng hữu hãy quay lại sau!");
                    return TCPProcessCmdResults.RESULT_OK;
                }


                GameManager.ClientMgr.SendToClient(client, TopAccumulatePointXML, (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_RANKING);
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
