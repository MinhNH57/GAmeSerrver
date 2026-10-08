using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.Tools;
using GameServer.KiemThe.GameDbController;

namespace GameServer.KiemThe.GameEvents.GiftExchange
{
    public static class GiftExchangePlayerManager
    {
        [XmlRoot("Item")]
        public class ItemData
        {
            [XmlAttribute("ID")] public int ID;
            [XmlAttribute("Count")] public int Count;
        }

        [XmlRoot("Milestone")]
        public class Milestone
        {
            [XmlAttribute("Count")] public int Count;
            [XmlElement("Item")] public List<ItemData> Items;
        }

        [XmlRoot("GiftExchangePlayer")]
        public class GiftExchangeData
        {
            [XmlElement("Milestone")] public List<Milestone> Milestones;
        }

        private const string CONFIG_PATH = "Config/KT_Interactive/GiftExchangePlayer.xml";
        private static GiftExchangeData Config;

        public static void Init()
        {
            var path = Global.GameResPath(CONFIG_PATH);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(GiftExchangeData));
                Config = xmlSerializer.Deserialize(fs) as GiftExchangeData;
            }

            // đọc log event từ file json 10'/lần
            // json được generate từ script python
            ScheduleExecutor2.Instance.scheduleCancle("TopGiftRanking");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopGiftRanking", ReadTopGiftRanking), 1000,
                60000);
        }


        private static string TopGiftSentXML;
        private static string TopGiftReceivedXML;
        private static void ReadTopGiftRanking()
        {
            TopGiftSentXML = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_TEAMMATE_GIFT_SENT, true);
            TopGiftReceivedXML = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_TEAMMATE_GIFT_RECEIVE, true);
        }

        private static List<int> GetGiftExchangeStatus(KPlayer player)
        {
            var sent = Global.GetRoleParamsInt32FromDB(player, RoleParamName.GiftExchange_Sent);
            var received = Global.GetRoleParamsInt32FromDB(player, RoleParamName.GiftExchange_Received);
            var sentAccumulate = Global.GetRoleParamsInt32FromDB(player, RoleParamName.GiftExchange_Sent_Milestone);
            return new List<int> { sent, received, sentAccumulate };
        }

        public static TCPOutPacket ResponseGiftExchangePlayer_Status(KPlayer player, TCPGameServerCmds cmdId,
            TCPOutPacketPool pool)
        {
            var statuses = GetGiftExchangeStatus(player);

            var data = DataHelper.ObjectToBytes(statuses);
            var outPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, (int)cmdId);
            return outPacket;
        }

        public static TCPOutPacket ResponseGiftExchange_ReceiveReward(KPlayer player, TCPGameServerCmds nID,
            TCPOutPacketPool pool)
        {
            var items = new List<ItemData>();
            var statuses = GetGiftExchangeStatus(player);
            var sent = statuses[0];
            var sentAccumulate = statuses[2];

            foreach (var milestone in Config.Milestones)
            {
                if (sent < milestone.Count)
                    continue;
                if (sentAccumulate >= milestone.Count)
                    continue;
                
                items.AddRange(milestone.Items);
            }
            
            var spaceNeeded = 0;
            foreach (var item in items)
            {
                spaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ID, item.Count);
            }
            
            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo",
                    $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm. Vui lòng dọn dẹp túi đồ và thử lại.");
                return null;
            }

            var hasReward = false;
            foreach (var item in items)
            {
                ItemManager.CreateSimpleItem(player, item.ID, item.Count, 0, 1);
                hasReward = true;
            }

            if (hasReward)
            {
                Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.GiftExchange_Sent_Milestone, sent, true);
            }
            else
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bằng hữu không có quà tích luỹ có thể nhận! Hãy tặng quà cho đồng đội để tích luỹ thêm điểm nhân duyên!");
            }
            
            return ResponseGiftExchangePlayer_Status(player, nID, pool);
        }

        public static void ResponseGiftExchange_TopRanking(KPlayer player, bool topSent)
            {
                if (topSent)
                    KT_TCPHandler.SendOpenUI(player, "EventRanking", Array.Empty<int>(), new List<string>{"Bảng xếp hạng Nhân Duyên", TopGiftSentXML});
                else
                    KT_TCPHandler.SendOpenUI(player, "EventRanking", Array.Empty<int>(), new List<string>{"Bảng xếp hạng Hấp Dẫn", TopGiftReceivedXML});
            }
    }
}