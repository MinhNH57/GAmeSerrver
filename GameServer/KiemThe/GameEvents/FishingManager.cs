using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents
{
    public static class FishingManager
    {
        [XmlRoot(ElementName = "FishingEvent")]
        public class FishingConfig
        {
            [XmlElement(ElementName = "Fish")]
            public List<FishInfo> Fishes;

            [XmlElement(ElementName = "Zone")]
            public List<ZoneInfo> FishingZones;

            [XmlElement(ElementName = "BaitID")]
            public BaitInfo Bait;

            [XmlElement(ElementName = "ShopID")]
            public int ShopID;

            [XmlElement(ElementName = "RewardID")]
            public int RewardID;

            [XmlElement(ElementName = "TimeKey")]
            public string TimeKey;
            
            [XmlElement(ElementName = "MantleRequire")]
            public List<Require> MantleRequire;
        }

        public class Require
        {
            [XmlAttribute(AttributeName = "ServerMin")]
            public int ServerMin;
            [XmlAttribute(AttributeName = "ServerMax")]
            public int ServerMax;
            [XmlAttribute(AttributeName = "Level")]
            public int Level;
        }

        public class BaitInfo
        {
            [XmlAttribute(AttributeName = "ID")]
            public int ID;
            [XmlAttribute(AttributeName = "PriceKNB")]
            public int PriceKNB;
            [XmlAttribute(AttributeName = "PriceKNBKhoa")]
            public int PriceKNBKhoa;
            [XmlAttribute(AttributeName = "LimitBuyCount")]
            public int LimitBuyCount;
        }

        public class FishInfo
        {
            [XmlAttribute(AttributeName = "ID")]
            public int ID;

            [XmlAttribute(AttributeName = "Name")]
            public string Name;

            [XmlAttribute(AttributeName = "RewardMoney")]
            public int RewardMoney;

            [XmlAttribute(AttributeName = "MakePoint")]
            public int MakePoint;
        }

        public class ZoneInfo
        {
            [XmlAttribute(AttributeName = "Type")]
            public string Type;

            [XmlAttribute(AttributeName = "Range")]
            public string Range;

            [XmlElement(ElementName = "Fish")]
            public List<FishRate> Rates;
        }

        public class FishRate
        {
            [XmlAttribute(AttributeName = "ID")]
            public int ID;

            [XmlAttribute(AttributeName = "Rate")]
            public int Rate;
        }

        private const string CONFIG_PATH = "Config/KT_Interactive/FishingEvent.xml";

        private static FishingConfig Config;
        private static Dictionary<int, FishInfo> FishesInfo;
        private static int RequireMantleToJoin = -1;

        public static void Init()
        {
            FishesInfo = new Dictionary<int, FishInfo>();
            var path = Global.GameResPath(CONFIG_PATH);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof(FishingConfig));
                Config = serializer.Deserialize(fs) as FishingConfig;
            }

            if (Config == null)
                return;

            // đọc cấp độ ff cần để tham gia câu cá
            foreach (var require in Config.MantleRequire)
            {
                if (GameManager.ServerId >= require.ServerMin && GameManager.ServerId <= require.ServerMax)
                {
                    RequireMantleToJoin = require.Level;
                    break;
                }
            }

            // nếu không đọc được từ config thì mặc định lấy config cuối cùng
            if (RequireMantleToJoin == -1)
            {
                RequireMantleToJoin = Config.MantleRequire.Last().Level;
            }

            foreach (var fish in Config.Fishes)
            {
                FishesInfo[fish.ID] = fish;
            }
        }

        public static void AddBaitItemToShop()
        {
            // nếu chưa có dữ liệu thì khởi tạo trước
            if (Config == null || Config.Bait.ID == 0)
            {
                Init();
            }

            if (Config == null)
                return;

            if (!EventTimeManager.IsEventEnabled(Config.TimeKey)) return;
            var newShopITem = new ShopItem
            {
                ID = 9992,
                ItemID = Config.Bait.ID,
                GoodsPrice = Config.Bait.PriceKNB
            };
            ShopManager.ForceAddShopItem(newShopITem);

            var tokenSale = ShopManager.TokenShop.Token.FirstOrDefault(x => x.ShopID == 203);
            tokenSale?.ShopItem.Remove(9992);
            tokenSale?.ShopItem.Insert(0, 9992);
            tokenSale?.Items.Insert(1, newShopITem);

            newShopITem = new ShopItem
            {
                ID = 9991,
                ItemID = Config.Bait.ID,
                GoodsPrice = Config.Bait.PriceKNBKhoa,
                LimitType = LimitType.BuyCountPerDay,
                LimitValue = Config.Bait.LimitBuyCount
            };
            ShopManager.ForceAddShopItem(newShopITem);

            var bindTokenSale = ShopManager.TokenShop.BoundToken.FirstOrDefault(x => x.ShopID == 207);
            bindTokenSale?.ShopItem.Remove(9991);
            bindTokenSale?.ShopItem.Insert(0, 9991);
            bindTokenSale?.Items.Insert(0, newShopITem);
        }

        public static string GetFishStatus(KPlayer client)
        {
            var baitCount = ItemManager.GetItemCountInBag(client, Config.Bait.ID);
            var fishingEventRecord = PrepareFish(client);

            return $"{baitCount};{fishingEventRecord}";
        }

        public static int GetRandomFish(ZoneInfo zone)
        {
            var output = StaticRandom.AccumulatedProbabilitySelection(
                zone.Rates.Select(w => w.ID).ToList(),
                zone.Rates.Select(w => w.Rate).ToList());

            return output;
        }

        public static string GetRandomFishesAndConsumeBait(KPlayer client, string zone, int multiplier)
        {
            var mantleLevel = Global.GetMantleLevel(client);
            if (mantleLevel < RequireMantleToJoin)
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", $"Bằng hữu cần trang bị phi phong cấp {KTLuaLib_System.GetMantleNameFromLevel(RequireMantleToJoin)} để tham gia hoạt động!");
                return "-1:0";
            }
            var baitCount = ItemManager.GetItemCountInBag(client, Config.Bait.ID);
            
            if (baitCount < multiplier)
            {
                PlayerManager.ShowNotification(client, $"Không đủ Thính để câu cá (Cần {multiplier}, có {baitCount})");
                return "-1:0";
            }

            if (!ItemManager.RemoveItemFromBag(client, Config.Bait.ID, multiplier))
            {
                PlayerManager.ShowNotification(client, "Lỗi khi tiêu hao Thính");
                return "-1:0";
            }

            var fishingEventRecord = PrepareFish(client);
            var fishCounts = new Dictionary<int, int>();
            var fishCaught = new Dictionary<int, int>();

            if (string.IsNullOrEmpty(fishingEventRecord))
            {
                fishingEventRecord = string.Join(";", Config.Fishes.Select(f => $"{f.ID}:0"));
            }

            foreach (var record in fishingEventRecord.Split(';'))
            {
                var parts = record.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var fishId) && int.TryParse(parts[1], out var count))
                {
                    fishCounts[fishId] = count;
                }
            }

            var zoneConfig = Config.FishingZones.FirstOrDefault(z => z.Type == zone);
            if (zoneConfig == null)
            {
                PlayerManager.ShowNotification(client, "Khu vực câu cá không tồn tại");
                return "-1:0";
            }

            for (var i = 0; i < multiplier; i++)
            {
                var fishId = GetRandomFish(zoneConfig);
                if (fishId > 0)
                {
                    fishCounts[fishId] = fishCounts.TryGetValue(fishId, out var count) ? count + 1 : 1;
                    fishCaught[fishId] = fishCaught.TryGetValue(fishId, out count) ? count + 1 : 1;
                }
                else
                {
                    PlayerManager.ShowNotification(client, "Không có cá nào trong khu vực này");
                    return "-1:0";
                }
            }

            fishingEventRecord = string.Join(";", fishCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

            WriteFish(client, fishingEventRecord);

            GlobalEventSource.getInstance().fireEvent(new JoinFishingEventObject(client, multiplier));

            string fishCaughtStr = string.Join(";", fishCaught.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

            foreach (var fish in fishCaught)
            {
                LogManager.WriteLog(LogTypes.FishingEvent,
                    $"[FISHING_EVENT] {client.RoleName} caught: {GetFishName(fish.Key)}({fish.Key}) x{fish.Value}");
            }

            return fishCaughtStr;
        }

        public static string ExchangeFish(KPlayer client, int fishId, int multiplier)
        {
            var fishingEventRecord = PrepareFish(client);
            if (string.IsNullOrEmpty(fishingEventRecord))
            {
                return "-1";
            }

            var fishCounts = new Dictionary<int, int>();
            foreach (var record in fishingEventRecord.Split(';'))
            {
                var parts = record.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var id) &&
                    int.TryParse(parts[1], out var count))
                {
                    fishCounts[id] = count;
                }
            }

            if (!fishCounts.TryGetValue(fishId, out var currentCount) || currentCount < multiplier)
            {
                PlayerManager.ShowNotification(client, $"Không đủ cá (Cần {multiplier}, có {currentCount})");
                return "-1";
            }

            if (!FishesInfo.TryGetValue(fishId, out var fish))
            {
                LogManager.WriteLog(LogTypes.FishingEvent,
                    $"[FISHING_EVENT] {client.RoleName} exchange result: Fish: {fishId} not found");
                PlayerManager.ShowNotification(client, "Cá không tồn tại");
                return "-1";
            }

            int pointCost = fish.MakePoint * multiplier;

            if (client.GetMakePoint() < pointCost)
            {
                PlayerManager.ShowNotification(client, $"Không đủ hoạt lực (Cần {pointCost}, có {client.GetMakePoint()})");
                return "-1";
            }

            if (client.GetGatherPoint() < pointCost)
            {
                PlayerManager.ShowNotification(client, $"Không đủ tinh lực (Cần {pointCost}, có {client.GetGatherPoint()})");
                return "-1";
            }

            if (!KTGlobal.IsHaveSpace(1, client))
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Cần có ít nhất 1 ô trống trong túi đồ để thực hiện đổi vật phẩm");
                return "-1";
            }

            var rewardAmount = fish.RewardMoney * multiplier;

            ItemManager.CreateSimpleItem(client, Config.RewardID, rewardAmount, 1, 0, 1);

            fishCounts[fishId] = currentCount - multiplier;
            client.ChangeCurMakePoint(-pointCost);
            client.ChangeCurGatherPoint(-pointCost);

            fishingEventRecord = string.Join(";", fishCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

            WriteFish(client, fishingEventRecord);
            
            LogManager.WriteLog(LogTypes.FishingEvent,
                $"[FISHING_EVENT] {client.RoleName} exchange result: Fish: {fish.Name}({fish.ID}) x{multiplier} Receive: {Config.RewardID} x{rewardAmount}");
            return $"{fishId}:{fishCounts[fishId]}";
        }

        private static string PrepareFish(KPlayer client)
        {
            var fishingEventRecord = Global.GetRoleParamsStringWithDB(client, RoleParamName.FishingEventRecord);
            if (string.IsNullOrEmpty(fishingEventRecord))
            {
                LogManager.WriteLog(LogTypes.FishingEvent,
                    $"[FISHING_EVENT] {client.RoleName} first time open fishing event");
                fishingEventRecord = string.Join(";", Config.Fishes.Select(f => $"{f.ID}:0"));
                WriteFish(client, fishingEventRecord);
            }

            var decode64 = DataHelper.DecodeBase64(fishingEventRecord);
            return decode64;
        }

        private static void WriteFish(KPlayer client, string fishingEventRecord)
        {
            var encode64 = DataHelper.EncodeBase64(fishingEventRecord);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.FishingEventRecord, encode64, true);
        }

        private static string GetFishName(int fishId)
        {
            if (FishesInfo.TryGetValue(fishId, out var fish))
            {
                return fish.Name;
            }

            return "Unknown";
        }
    }
}