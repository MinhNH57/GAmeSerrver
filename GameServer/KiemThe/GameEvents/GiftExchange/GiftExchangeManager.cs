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

namespace GameServer.KiemThe.GameEvents.GiftExchange
{
    public static class GiftExchangeManager
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

        [XmlRoot("Exchange")]
        public class ExchangeData
        {
            [XmlAttribute("GiftID")] public int GiftID;
            [XmlAttribute("RoleParam")] public string RoleParam;
            [XmlElement("Milestone")] public List<Milestone> Milestones;
        }

        [XmlRoot("GiftExchange")]
        public class GiftExchangeData
        {
            [XmlElement("Exchange")] public List<ExchangeData> Exchange;

            [XmlElement("TimeKey")] public string TimeKey;
        }

        private const string CONFIG_PATH = "Config/KT_Interactive/GiftExchange.xml";
        internal static GiftExchangeData Config;

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
                // Config.Packages = new Dictionary<int, CheckInPackage>();
                // foreach (var package in Config.CheckInPackages)
                // {
                //     Config.Packages.Add(package.ID, package);
                // }
            }
        }

        private static void UpdateGiftExchangeStatus(KPlayer player, Dictionary<int, List<int>> data)
        {
            foreach (var exchangeData in Config.Exchange)
            {
                if (!data.TryGetValue(exchangeData.GiftID, out var status))
                    return;

                var status_Str = $"{status[0]}_{status[1]}";
                Global.SaveRoleParamsStringToDB(player, exchangeData.RoleParam, status_Str, true);
            }
        }

        private static Dictionary<int, List<int>> GetGiftExchangeStatus(KPlayer player)
        {
            var result = new Dictionary<int, List<int>>();
            foreach (var exchangeData in Config.Exchange)
            {
                var statusStr = Global.GetRoleParamByName(player, exchangeData.RoleParam);
                if (string.IsNullOrEmpty(statusStr))
                {
                    result[exchangeData.GiftID] = new List<int> { 0, -1 };
                    continue;
                }

                var fields = statusStr.Split('_');
                if (fields.Length != 2)
                {
                    result[exchangeData.GiftID] = new List<int> { 0, -1 };
                    continue;
                }

                var gived = int.Parse(fields[0]);
                var received = int.Parse(fields[1]);
                result[exchangeData.GiftID] = new List<int> { gived, received };
            }

            return result;
        }

        public static TCPOutPacket ResponseGiftExchange_Status(KPlayer player, TCPGameServerCmds cmdId,
            TCPOutPacketPool pool)
        {
            var statuses = GetGiftExchangeStatus(player);

            var data = DataHelper.ObjectToBytes(statuses);
            var outPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, (int)cmdId);
            return outPacket;
        }

        public static TCPOutPacket ResponseGiftExchange_SendGift(KPlayer player, int giftId, int count,
            TCPGameServerCmds nID, TCPOutPacketPool pool)
        {
            var giftData = ItemManager.GetItemTemplate(giftId);
            var inBag = ItemManager.GetItemCountInBag(player, giftId);
            if (inBag < count)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", $"Bằng hữu không có đủ {giftData.Name} cần thiết!");
                return null;
            }

            if (!ItemManager.RemoveItemFromBag(player, giftId, count))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng thử lại sau!");
                return null;
            }

            var statuses = GetGiftExchangeStatus(player);
            statuses[giftId][0] += count;

            UpdateGiftExchangeStatus(player, statuses);

            var data = DataHelper.ObjectToBytes(statuses);
            var outPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, (int)nID);
            return outPacket;
        }

        public static TCPOutPacket ResponseGiftExchange_ReceiveReward(KPlayer player, TCPGameServerCmds nID,
            TCPOutPacketPool pool)
        {
            var items = new List<ItemData>();
            var statuses = GetGiftExchangeStatus(player);
            foreach (var exchangeData in Config.Exchange)
            {
                if (!statuses.TryGetValue(exchangeData.GiftID, out var status))
                    continue;
                var sended = status[0];
                var received = status[1];
                var milestoneCounter = 0;
                foreach (var milestone in exchangeData.Milestones)
                {
                    if (sended >= milestone.Count && received < milestoneCounter)
                    {
                        items.AddRange(milestone.Items);
                    }
                    
                    milestoneCounter++;
                }
            }
            
            var spaceNeeded = 0;
            foreach (var item in items)
            {
                spaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ID, item.Count);
            }
            
            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Check-In",
                    $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm. Vui lòng dọn dẹp túi đồ và thử lại.");
                return null;
            }
            
            foreach (var item in items)
            {
                ItemManager.CreateSimpleItem(player, item.ID, item.Count, 0, 1);
            }
            
            foreach (var exchangeData in Config.Exchange)
            {
                if (!statuses.TryGetValue(exchangeData.GiftID, out var status))
                    continue;
                var sended = status[0];
                var received = status[1];
                var milestoneCounter = 0;
                foreach (var milestone in exchangeData.Milestones)
                {
                    if (sended >= milestone.Count && received < milestoneCounter)
                    {
                        received = milestoneCounter;
                    }

                    milestoneCounter++;
                }

                statuses[exchangeData.GiftID][1] = received;
            }
            
            UpdateGiftExchangeStatus(player, statuses);
            var data = DataHelper.ObjectToBytes(statuses);
            var outPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, (int)nID);
            return outPacket;
        }
    }
}