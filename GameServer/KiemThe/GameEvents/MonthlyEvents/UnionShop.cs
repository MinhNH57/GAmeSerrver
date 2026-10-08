using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using Newtonsoft.Json;
using Server.Tools;
using UnityEngine.Networking.Types;
using static GameServer.KiemThe.CopySceneEvents.YouLongGe.YouLong.AwardInfo;
using static GameServer.KiemThe.GameEvents.MonthlyEvents.UnionShop;

namespace GameServer.KiemThe.GameEvents.MonthlyEvents
{
    public static class UnionShop
    {
        [Serializable]
        public class EventShopItem
        {
            public int ShopItemID;
            public int ItemID;
            public List<int> Milestones;
            public List<int> MilestonesRefund;
        }

        [Serializable]
        public class UnionShopData
        {
            public List<int> servers;
            public List<int> shopIDs;
            public DateTime startDate;
            public DateTime endDate;
            public string key;
            public List<EventShopItem> config;
        }

        [Serializable]
        public class UnionShopConfig
        {

            public List<UnionShopData> data;
        }

        private static string timeKey = "UuDaiDoanKet";
        private static List<EventShopItem> ShopData;
        private static List<int> UnionShopIDs;
        // public static DateTime ShopStartDate;
        // public static DateTime ShopEndDate;
        private static string SavedKey;

        //private const string CONFIG_PATH = "Config/KT_GameEvents/UuDaiDoanKet.json";
        private const string UnionShopDataDir = "Config/KT_Interactive/UnionShop.xml";
        public static XElement UnionShopXMLData;
        private static string RawData;

        public static void Init()
        {
            LoadUnionShopData();

            //var absoluteConfigPath = Global.GameResPath(CONFIG_PATH);
            //if (!File.Exists(absoluteConfigPath))
            //    return;

            //var configStr = File.ReadAllText(absoluteConfigPath);
            //var config = JsonConvert.DeserializeObject<List<UnionShopData>>(configStr);

            //foreach (var unionShopConfig in config)
            //{
            //    if (unionShopConfig.servers.Contains(KTLuaLib_System.GetServerID()))
            //    {
            //        ShopData = unionShopConfig.config;
            //        UnionShopIDs = unionShopConfig.shopIDs;
            //        ShopStartDate = unionShopConfig.startDate;
            //        ShopEndDate = unionShopConfig.endDate;
            //        SavedKey = unionShopConfig.key;
            //        break;
            //    }
            //}

            // nếu không có cấu hình shop dành cho server thì dùng cấu hình default (server = -1)
            //if (ShopData == null) 
            //{
            //    foreach (var unionShopConfig in config)
            //        {
            //            if (unionShopConfig.servers.Contains(-1))
            //            {
            //                ShopData = unionShopConfig.config;
            //                UnionShopIDs = unionShopConfig.shopIDs;
            //                ShopStartDate = unionShopConfig.startDate;
            //                ShopEndDate = unionShopConfig.endDate;
            //                SavedKey = unionShopConfig.key;
            //                break;
            //            }
            //        }
            //}
        }

        public static List<int> ParseIntList(string input)
        {
            try
            {
                string[] parts = input.Split(',');
                List<int> result = new List<int>();

                for (int i = 0; i < parts.Length; i++)
                {
                    result.Add(int.Parse(parts[i]));
                }

                return result;
            }
            catch
            {
                return new List<int> { 0, 0, 0 };
            }
        }


        public static bool IsShopEvent(int shopID)
        {
            if (UnionShopIDs == null)
                return false;
            return UnionShopIDs.Contains(shopID);
        }
        
        
        public static void UpdateTotalSell(KPlayer player, int itemID, int added)
        {
            var key = $"{GameManager.ServerId}_{SavedKey}_{itemID}";
            var current = GameManager.GameConfigMgr.GetGameConfigItemInt(key, 0);
            current += added;
            GameManager.GameConfigMgr.UpdateGameConfigItem(key, current.ToString());
            
            LogManager.WriteLog(LogTypes.UnionShop, $"[UNIONSHOP] {player.RoleName} [{player.RoleID}] đã mua {added} x {itemID}");
        }

        public static int GetTotalSell(int itemID)
        {
            var key = $"{GameManager.ServerId}_{SavedKey}_{itemID}";
            return GameManager.GameConfigMgr.GetGameConfigItemInt(key, 0);
        }

        public static Dictionary<string, string> GetStats(KPlayer player)
        {
            var result = new Dictionary<string, string>();
            foreach (var it in ShopData)
            {
                var totalSell = GetTotalSell(it.ItemID);
                result[$"value_{it.ItemID}"] = totalSell.ToString();

                result[$"milestone_1_{it.ItemID}"] = (totalSell >= it.Milestones[0]).ToString();
                result[$"milestone_2_{it.ItemID}"] = (totalSell >= it.Milestones[1]).ToString();
                result[$"milestone_3_{it.ItemID}"] = (totalSell >= it.Milestones[2]).ToString();
            }

            foreach (var it in ShopData)
            {
                var playerBuyCount = player.GetValueOfForeverRecore(it.ShopItemID);
                if (playerBuyCount < 0)
                    playerBuyCount = 0;
                result[$"buy_count_{it.ItemID}"] = playerBuyCount.ToString();
            }

            return result;
        }

        public static string GetUnionStatsList(KPlayer player)
        {
            var result = "";
            
            foreach (var it in ShopData)
            {
                var totalSell = GetTotalSell(it.ItemID);
                //var buyLimit = 
                var playerBuyCount = player.GetValueOfForeverRecore(it.ShopItemID);
                if (playerBuyCount < 0)
                    playerBuyCount = 0;

                var str = $"{it.ItemID},{playerBuyCount},{totalSell}";
                    //new Tuple<int, int, int>(it.ItemID, playerBuyCount, totalSell); 
                
                result += str + ';';
            }

            return result.TrimEnd(';');
        }
        public static void RefundKNBToPlayer(KPlayer player)
        {
            int refundAmount = 0;

            foreach (var it in ShopData)
            {
                var totalSell = GetTotalSell(it.ItemID);
                var playerBuyCount = player.GetValueOfForeverRecore(it.ShopItemID);
                if (playerBuyCount < 0)
                    playerBuyCount = 0;

                if (totalSell >= it.Milestones[2])
                {
                    refundAmount += it.MilestonesRefund[2] * playerBuyCount;
                }
                else if (totalSell >= it.Milestones[1])
                {
                    refundAmount += it.MilestonesRefund[1] * playerBuyCount;
                }
                else if (totalSell >= it.Milestones[0])
                {
                    refundAmount += it.MilestonesRefund[0] * playerBuyCount;
                }
            }

            // int moneyRefunded = player.GetValueOfForeverRecore((int)RecoreDef.UnionShopRefundedRecord);
            int moneyRefunded = Global.GetRoleParamsInt32FromDB(player, RoleParamName.UnionShopRefunded, 0);

            if (moneyRefunded < 0)
                moneyRefunded = 0;
            if (moneyRefunded > refundAmount)
                moneyRefunded = refundAmount;

            int moneyToRefund = refundAmount - moneyRefunded;

            if (moneyToRefund <= 0)
            {
                PlayerManager.ShowNotification(player, "Đã nhận hoàn trả");

                LogManager.WriteLog(LogTypes.UnionShop, $"[UNIONSHOP] Hoàn trả thất bại. Đã nhận hoàn trả. {player.RoleID} [{player.RoleName}]");
                return;
            };

            if (GameManager.ClientMgr.AddToken(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, player, moneyToRefund, "UNION REFUND"))
            {
                moneyRefunded += moneyToRefund;
                // player.SetValueOfForeverRecore((int)RecoreDef.UnionShopRefundedRecord, moneyRefunded);
                Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.UnionShopRefunded, moneyRefunded, true);
                PlayerManager.ShowNotification(player, $"Hoàn trả {moneyToRefund} KNB");

                LogManager.WriteLog(LogTypes.UnionShop, $"[UNIONSHOP] Hoàn trả thành công. Hoàn trả {moneyToRefund} KNB. {player.RoleID} [{player.RoleName}]");
            }
            else
            {
                PlayerManager.ShowNotification(player, "Hoàn trả thất bại");

                LogManager.WriteLog(LogTypes.UnionShop, $"[UNIONSHOP] Hoàn trả thất bại. Không thêm được KNB. {player.RoleID} [{player.RoleName}]");
            }
        }

        public static bool IsUnionShopAvailable()
        {
            var current = DateTime.Now;
            return EventTimeManager.IsEventEnabled(timeKey);
        }

        public static void LoadUnionShopData()
        {
            var files = Global.GameResPath(UnionShopDataDir);
            var content = File.ReadAllText(files);

            // if (UnionShopXMLData == null)
            UnionShopXMLData = XElement.Parse(content);

            foreach (var serverConfig in UnionShopXMLData.Elements("Server"))
            {
                var servers = serverConfig.Attribute("servers").Value.Split(',').Select(int.Parse).ToList();
                if (servers.Contains(KTLuaLib_System.GetServerID()))
                {
                    ParseUnionShopData(serverConfig);
                }
            }

            RawData = content;
        }

        public static void ParseUnionShopData(XElement xmlNode)
        {
            var shopIDs = new List<int> { KTGlobal.GetXMLAttribute<int>(xmlNode, "shopID") };
            timeKey = xmlNode.Attribute("TimeKey").Value;
            var key = xmlNode.Attribute("key").Value;

            var config = xmlNode.Elements("ConfigData").Select(configElement => new EventShopItem
            {
                ShopItemID = int.Parse(configElement.Attribute("ShopItemID").Value),
                ItemID = int.Parse(configElement.Attribute("ItemID").Value),
                Milestones = configElement.Attribute("Milestones").Value.Split(',').Select(int.Parse).ToList(),
                MilestonesRefund = configElement.Attribute("MilestonesRefund").Value.Split(',').Select(int.Parse).ToList()
            }).ToList();

            ShopData = config;
            UnionShopIDs = shopIDs;
            SavedKey = key;
        }
        
        public static DateTime GetShopStartDate()
        {
            EventTimeManager.Event evt = EventTimeManager.GetEvent(timeKey);
            return evt.StartDate;
        }
        
        public static DateTime GetShopEndDate()
        {
            EventTimeManager.Event evt = EventTimeManager.GetEvent(timeKey);
            return evt.EndDate;
        }

    }
}

namespace GameServer.KiemThe.LuaSystem.Logic
{
    public partial class KTLuaLib_Event
    {
        public static bool IsUnionShopAvailable()
        {
            return UnionShop.IsUnionShopAvailable();
        }
    }
}