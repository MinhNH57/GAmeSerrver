using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.DailyCheckin.Data;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.Tools;
using GameServer.KiemThe.Core.BattlePass;

namespace GameServer.KiemThe.GameEvents.DailyCheckin
{
    namespace Data
    {
        [XmlRoot("Config")]
        public class Config
        {
            [XmlElement("Reward")]
            public List<Reward> DailyCheckins { get; set; }
            [XmlElement("Price")]
            public Price Price { get; set; }
            [XmlElement("Step")]
            public List<Step> Step { get; set; }

            public Reward[] GetRewards(int day)
            {
                return DailyCheckins.FindAll(x=>x.Counter == day).ToArray();
            }

            public int GetIndexStep(int day)
            {
                var item = Step.Find(x => x.Counter == day);
                if(item == null) return -1;
                return item.Index;
            }
        }

        public class Price
        {
            [XmlAttribute("Money")]
            public string Currency { get; set; }
        }

        public class Step
        {
            [XmlAttribute("Index")]
            public int Index { get; set; }
            [XmlAttribute("Counter")]
            public int Counter { get; set; }
        }

        public class Reward
        {
            [XmlAttribute("Counter")]
            public int Counter { get; set; }
            [XmlAttribute("Info")]
            public string Info { get; set; }
            [XmlAttribute("Type")]
            public string Type { get; set; }
            [XmlAttribute("ItemID")]
            public int ItemID { get; set; }
            [XmlAttribute("Number")]
            public int Number { get; set; }
            [XmlAttribute("Repeat")]
            public string Repeat { get; set; }
        }

    }

    public class DailyCheckinManager
    {
        const string ConfigPath = "Config/KT_Interactive/DailyCheckin.xml";
        static Data.Config Config { get; set; }

        public static void Init()
        {
            Config = LoadConfig<Config>(ConfigPath);
            Console.WriteLine("Daily Checkin Loaded");
        }
        
        private static T LoadConfig<T>(string path)
        {
            var configFilePath = Global.GameResPath(path);
            if (!File.Exists(configFilePath))
            {
                return default(T);
            }

            var xmlcontent = File.ReadAllText(configFilePath);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(xmlcontent))
            {
                return (T)serializer.Deserialize(reader);
            }
            return default(T);
        }

        public static int Checkin(KPlayer client, int day, string from = "DailyCheckin")
        {
            CompareMonth(client);
            var now = KTGlobal.GetDateNow();
            if (day <= 0)
            {
                day = now.Day;
            }
            
            if (day > client.GetMaxDayInMonth()  || day <= 0)
            {
                PlayerManager.ShowMessageBox(client, "Lỗi", $"Không thể điểm danh vào ngày này!");
                SendClientCheckinUpdate(client);
                return 1;
            }

            if (day > now.Day)
            {
                PlayerManager.ShowMessageBox(client, "Lỗi", $"Không thể điểm danh vào ngày này!");
                SendClientCheckinUpdate(client);
                return 1;
            }

            var active = client.GetActiveDailyCheckin(day);
            if (active) // đã checkin rồi, báo lỗi
            {
                PlayerManager.ShowMessageBox(client, "Lỗi", "Bạn đã điểm danh rồi");
                SendClientCheckinUpdate(client);
                return 1;
            }
            var rewards = Config.GetRewards(1).ToList();
            int totalSpackNeed = rewards.Sum(x => ItemManager.TotalSpaceNeed(x.ItemID, x.Number));
            
            if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", $"Cần sắp xếp tối thiểu {totalSpackNeed} ô trống trong túi để nhận thưởng!");
                return 1;
            }

            if (day < now.Day) // điêm danh bù
            {
                var pin = Config.Price.Currency.Split(':');
                var moneytype = int.Parse(pin[0]);
                var amount = int.Parse(pin[1]);
                PlayerManager.ShowMessageBox(client,"Thông báo", $"Bạn có muốn sử dụng <color=yellow>{amount}KNB</color> để điểm danh bù không?", () =>
                {
                    if (KTGlobal.IsHaveMoney(client, amount, (MoneyType)moneytype))
                    {
                        KTGlobal.SubMoney(client, amount, (MoneyType)moneytype, from);
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Lỗi", $"Bạn không đủ <color=yellow>{amount}KNB</color> để điểm danh bù!");
                        return;
                    }
                    GoCheckin(client, day, from, now);
                }, true);
            }
            else
            {
                GoCheckin(client, day, from, now);
            }

            return 0;
        }

        private static void GoCheckin(KPlayer client, int day, string from, DateTime now)
        {
            client.DailyCheckin(day);
            LogManager.WriteLog(LogTypes.DailyCheckin, $"[{client.ServerId}][{client.RoleID}][{from}] Điểm danh hàng ngày: {now.Day}");
            ReceiveReward(client, Config.GetRewards(1), from, $"Hằng ngày ({day})");
            SendClientCheckinUpdate(client);
            LogManager.WriteLog(LogTypes.DailyCheckin, $"[{client.ServerId}][{client.RoleID}][{from}] Nhận phần thưởng điểm danh hàng ngày {now.Day}");
            BattlePassManager.ProcessLogOnTask(client);
        }

        public static int GetRewardMethi(KPlayer client, int day, string from = "DailyCheckin")
        {
            CompareMonth(client);

            int index = Config.GetIndexStep(day);
            if (index == -1)
            {
                PlayerManager.ShowMessageBox(client, "Lỗi", "Không có mốc nhận thưởng này!");
                SendClientCheckinUpdate(client);
                return 1;
            }
            
            int current = client.GetCheckinDayCount();
            if (current < day)
            {
                PlayerManager.ShowMessageBox(client, "Lỗi", "Chưa đủ ngày nhận thưởng");
                SendClientCheckinUpdate(client);
                return 1;
            }
            
            if (client.GetDailyCheckinReward(index))
            {
                PlayerManager.ShowMessageBox(client, "Lỗi", "Đã nhận thưởng ở mốc này rồi!");
                SendClientCheckinUpdate(client);
                return 1;
            }

            var rewards = Config.GetRewards(day).ToList();
            int totalSpackNeed = rewards.Sum(x => ItemManager.TotalSpaceNeed(x.ItemID, x.Number));
            if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", $"Cần sắp xếp tối thiểu {totalSpackNeed} ô trống trong túi để nhận thưởng!");
                return 1;
            }
            ReceiveReward(client, Config.GetRewards(day), from, $"Tích luỹ ({day} ngày)");
            client.SetCheckinReward(Config.GetIndexStep(day), from);
            //client.SendPacket((int)TCPGameServerCmds.CMD_KT_DAILYCHECKIN_REWARD, "0");
            SendClientCheckinUpdate(client);
            LogManager.WriteLog(LogTypes.DailyCheckin, $"[{client.ServerId}][{client.RoleID}][{from}] Nhận thưởng tích luỹ: {day}");
            return 0;
        }

        private static void CompareMonth(KPlayer client)
        {
            var lastCheckin = client.GetLastDailyCheckin();
            var now = KTGlobal.GetDateNow();
            if (!GetMonth(lastCheckin).Equals(GetMonth(now)))
            {
                client.ResetDailyCheckin();
                SendClientCheckinUpdate(client);
            }
        }

        private static void ReceiveReward(KPlayer client, Reward[] rewards, string from, string type)
        {
            foreach (var reward in rewards)
            {
                switch (reward.Type)
                {
                    case "Item":
                    {
                        if (!ItemManager.CreateItem(TCPOutPacketPool.getInstance(), client, reward.ItemID, reward.Number,
                                0, from,
                                true, 1,
                                false, Global.ConstGoodsEndTime))
                        {
                            LogManager.WriteLog(LogTypes.DailyCheckin, $"[{client.ServerId}][{client.RoleID}][{from}] Nhận thưởng {type} lỗi: {reward.ItemID}:{reward.Number}");
                        }
                        break;
                    }
                    case "Money":
                    {
                        KTGlobal.AddMoney(client, reward.ItemID, (MoneyType)reward.Number, from);
                        break;
                    }
                }
            }
        }

        public static void SendClientCheckinUpdate(KPlayer client)
        {
            CompareMonth(client);
            client.SendPacket((int)TCPGameServerCmds.CMD_KT_DAILYCHECKIN_UPDATE, client.GetDailyCheckinDataString());
        }
        
        private static DateTime GetMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }
    }
}