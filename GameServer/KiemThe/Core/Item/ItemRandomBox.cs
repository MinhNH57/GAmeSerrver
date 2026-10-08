using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Repute;
using GameServer.KiemThe.Core.Title;
using GameServer.KiemThe.LuaSystem.Logic;
using Newtonsoft.Json.Linq;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.KiemThe.Core.Item
{
    public static class ItemRandomBox
    {
        private static readonly string RandomBoxConfig = "Config/KT_Item/KT_ConfigRandomBox.xml";
        private static readonly string RandomBoxJSON = "Config/KT_Item/randomboxes.json";

        private static readonly List<string> Events = new List<string>()
        {
            "Config/KT_Item/Events/Archives/2023_06_Boxes.xml",
        };

        public static ConfigBox _ConfigBox = new ConfigBox();

        /// <summary>
        /// Loading all Drop
        /// </summary>
        public static void Setup()
        {
            var Files = Global.GameResPath(RandomBoxConfig);

            using (var stream = File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(ConfigBox));
                _ConfigBox = serializer.Deserialize(stream) as ConfigBox;
            }

            foreach (var file in Events.Select(Global.GameResPath))
            {
                if (!File.Exists(file))
                    continue;
                using (var stream = File.OpenRead(file))
                {
                    var serializer = new XmlSerializer(typeof(ConfigBox));
                    if (serializer.Deserialize(stream) is ConfigBox event_boxes)
                    {
                        _ConfigBox?.Boxs.AddRange(event_boxes.Boxs);
                    }
                }
            }

            var listJSON = Directory.GetFiles("Config/Config/KT_Item/Events/", "*Boxes.json",
                SearchOption.AllDirectories);
            foreach (var itemConfig in listJSON)
            {
                ReadJSONRandomBoxes(Global.GameResPath(itemConfig.Replace("Config/Config", "Config")));
            }

            listJSON = Directory.GetFiles("Config/Config/KT_Item/RandomBox/", "randomboxes_*.json",
                SearchOption.TopDirectoryOnly);
            foreach (var boxConfig in listJSON)
            {
                ReadJSONRandomBoxes(Global.GameResPath(boxConfig.Replace("Config/Config", "Config")));
            }

            ReadJSONRandomBoxes(Global.GameResPath(RandomBoxJSON));

            // foreach (var file in EventsJSON.Select(Global.GameResPath))
            // {
            //     ReadJSONRandomBoxes(file);
            // }
        }

        public static Dictionary<int, int> GetStaticBoxContent(int goodsID)
        {
            var goodsData = ItemManager.GetItemTemplate(goodsID);
            if (goodsData == null)
                return null;
            var boxID = goodsData.ItemValue;
            var box = _ConfigBox.Boxs.FirstOrDefault(x => x.IDBox == boxID);
            if (box == null)
                return null;
            var result = new Dictionary<int, int>();
            foreach (var item in box.Items)
            {
                if (item.Rate != -1)
                    continue;
                var outputItemID = -1;
                switch (item.Type)
                {
                    case 1:
                        result[1] = item.Number;
                        break;
                    case 2:
                        result[2] = item.Number;
                        break;
                    case 3:
                        result[4] = item.Number;
                        break;
                    case 4:
                        result[3] = item.Number;
                        break;
                    case 5:
                        result[5] = item.Number;
                        break;
                    case 6:
                        result[6] = item.Number;
                        break;
                    case 11:
                        result[5] = item.Number;
                        result[6] = item.Number;
                        break;
                    case 0:
                        result[item.ItemID] = item.Number;
                        break;
                }
            }

            return result;
        }

        private static ItemRandom JsonToItemRandom(int id, double rate, int upperBinding, JToken jObject)
        {
            var number = (jObject["Number"] ?? 1).Value<int>();
            var binding = (jObject["Binding"] ?? upperBinding).Value<int>();
            var expiryDate = (jObject["ExpiryDate"] ?? "").Value<string>();
            var time = (jObject["Time"] ?? 0).Value<int>();
            var shouldNotify = (jObject["Notify"] ?? false).Value<bool>();
            var isPreview = (jObject["IsPreview"] ?? false).Value<bool>();
            var sex = (jObject["Sex"] ?? -1).Value<int>();
            var forge = (jObject["Forge"] ?? 0).Value<int>();
            var series = (jObject["Series"] ?? -1).Value<int>();

            var itemRandom = new ItemRandom
            {
                ItemID = (id > 100) ? id : -1,
                Number = number,
                Type = (id > 100) ? 0 : id,
                Rate = (int)Math.Round(rate),
                Series = series,
                Lock = binding,
                ExpiryDate = expiryDate,
                TimeLimit = time,
                ShouldNotify = shouldNotify ? 1 : 0,
                Sex = sex,
                IsPreview = isPreview,
                Forge = forge
            };
            return itemRandom;
        }

        private static void ReadJSONRandomBoxes(string path)
        {
            var server_path = path.Replace(".json", $"_{GameManager.ServerLineID}.json");
            if (File.Exists(server_path))
            {
                SysConOut.WriteLine($"Read {path} for server {GameManager.ServerLineID}");
                path = server_path;
            }

            if (!File.Exists(path))
                return;

            var jsonStr = File.ReadAllText(path);
            var json = JArray.Parse(jsonStr);
            foreach (var box in json)
            {
                var randomBox = new RandomBox
                {
                    BoxName = box["BoxName"]?.ToString(),
                    IDBox = (box["IDBox"] ?? -1).Value<int>(),
                    LimitWeek = (box["LimitWeek"] ?? 0).Value<int>(),
                    LimitDay = (box["LimitDay"] ?? 0).Value<int>(),
                    LimitLifeTime = (box["LimitLifeTime"] ?? 0).Value<int>(),
                    LimitReachedReward = (box["LimitReachedReward"] ?? 0).Value<int>(),
                    LimitReachedRewardBinding = (box["LimitReachedRewardBinding"] ?? 1).Value<int>(),
                    Items = new List<ItemRandom>(),
                    TotalRate = 100000000,
                    // Milestones = new List<RandomBoxMilestone>(),
                    // DailyMilestones = new List<RandomBoxMilestone>()
                };

                // if (box[$"Milestones_{KTLuaLib_System.GetServerID()}"] != null)
                // {
                //     SysConOut.WriteLine($"Override milestone for randombox {randomBox.BoxName}, server {KTLuaLib_System.GetServerID()}");
                //     randomBox.Milestones = box[$"Milestones_{KTLuaLib_System.GetServerID()}"].ToObject<List<RandomBoxMilestone>>();
                // }
                // else if (box["Milestones"] != null)
                // {
                //     randomBox.Milestones = box["Milestones"].ToObject<List<RandomBoxMilestone>>();
                // }
                //
                //
                // if (box["DailyMilestones"] != null)
                // {
                //     randomBox.DailyMilestones = box["DailyMilestones"].ToObject<List<RandomBoxMilestone>>();
                // }
                //
                // // nếu không phải trong giai đoạn 2 thì bỏ các vật phẩm bùa chú ID 20778
                // if (!KTLuaLib_System.IsDateTimeInRange("2025-01-24 05:00:00", "2025-02-06 05:00:00"))
                //     randomBox.Milestones.RemoveAll(x => x.RewardID == 20778);

                if (box["Items"] == null)
                    continue;

                var boxBinding = (box["Binding"] ?? 1).Value<int>();

                foreach (var itemConfig in box["Items"])
                {
                    var rate = (itemConfig["Rate"] ?? 0).Value<double>();
                    rate = (Math.Abs(rate - (-1)) < 1e-3) ? -1 : (double)randomBox.TotalRate / 100 * rate;
                    var id = (itemConfig["ItemID"] ?? -1).Value<int>();

                    if (itemConfig["Details"] == null)
                    {
                        var itemRandom = JsonToItemRandom(id, (int)Math.Round(rate), boxBinding, itemConfig);
                        randomBox.Items.Add(itemRandom);
                        continue;
                    }

                    var itemBinding = (itemConfig["Binding"] ?? boxBinding).Value<int>();

                    foreach (var item in itemConfig["Details"])
                    {
                        var itemID = (item["ItemID"] ?? id).Value<int>();
                        var particularRate = (item["Rate"] ?? 100).Value<double>();
                        var finalRate = Math.Abs(particularRate - (-1)) < 1e-3
                            ? -1
                            : rate / 100 * particularRate;
                        // var finalRate = particularRate == -1 ? -1 : (int)(rate / 100 * particularRate);
                        var itemRandom = JsonToItemRandom(itemID, finalRate, itemBinding, item);
                        randomBox.Items.Add(itemRandom);
                    }
                }

                var rateSum = randomBox.Items.Sum(x => x.Rate != -1 ? x.Rate : 0);
                var isConstantBox = randomBox.Items.All(x => x.Rate == -1);
                if (!isConstantBox && rateSum != randomBox.TotalRate)
                {
                    SysConOut.WriteLine($"WARNING: Randombox {randomBox.BoxName} [{randomBox.IDBox}] is not correct.");
                }

                _ConfigBox?.Boxs.Add(randomBox);
            }
        }

        private static ConcurrentDictionary<string, int> RANDOMBOX_REWARD;

        public static void OpenBoxRandomInBatch(KPlayer player, int randomBoxId, int times)
        {
            KTGlobal.SendSystemChatToPlayer(player, "================Làm mới===================");
            RANDOMBOX_REWARD = new ConcurrentDictionary<string, int>();
            for (var i = 0; i < times; ++i)
            {
                OpenBoxRandom(player, randomBoxId, true);
            }

            var msg = RANDOMBOX_REWARD.Select(reward => $"{reward.Key}, {reward.Value} |").ToList();
            msg.Sort();
            msg.Insert(0, "Item, Số lượng |");

            KT_TCPHandler.SendOpenUI(player, "MainGame/UITextArea", Array.Empty<int>(),
                new List<string> { "InputField", string.Join("\r\n", msg) });
        }

        private static bool OpenRandomBox(KPlayer player, int itemID)
        {
            var itemDBId = ItemManager.GetOneItemInBag(player, itemID);
            if (itemDBId == -1)
                return false;

            var opened = OpenBoxRandom(player, itemDBId);

            if (opened)
            {
                var removed = ItemManager.RemoveItem(player, itemDBId);
                LogManager.WriteLog(LogTypes.RandomBox,
                    removed
                        ? $"{player.RoleName}[{player.RoleID}] hoàn thành mở RANDOMBOX, itemid {itemID}, dbid {itemDBId}"
                        : $"{player.RoleName}[{player.RoleID}] xóa lỗi RANDOMBOX, dbid {itemID}");

                opened = removed;
            }
            else
            {
                LogManager.WriteLog(LogTypes.RandomBox,
                    $"{player.RoleName}[{player.RoleID}] mở lỗi RANDOMBOX, itemid {itemID}, dbid {itemDBId}");
            }

            return opened;
        }

        public static void OpenAllRandomBox(KPlayer player, int itemID, int quantity)
        {
            var MS_INTERVAL_BETWEEN_OPENS = 500;

            if (KTGMCommandManager.IsGM(player))
            {
                MS_INTERVAL_BETWEEN_OPENS = 100;
            }

            var scheduleName = $"OPENRANDOMBOX_{player.RoleID}";
            ScheduleExecutor2.Instance.scheduleCancle(scheduleName);

            var counter = 0;
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask(scheduleName, () =>
            {
                var result = OpenRandomBox(player, itemID);
                if (result)
                    counter++;

                if (!result || counter == quantity)
                {
                    ScheduleExecutor2.Instance.scheduleCancle(scheduleName);
                }
            }), 0, MS_INTERVAL_BETWEEN_OPENS);
        }

        /// <summary>
        /// Mở rương vật phẩm
        /// </summary>
        /// <param name="player"></param>
        /// <param name="boxDbID"></param>
        /// <param name="dryRun"></param>
        /// <returns></returns>
        public static bool OpenBoxRandom(KPlayer player, int boxDbID, bool dryRun = false)
        {
            LogManager.WriteLog(LogTypes.RandomBox,
                $"{player.RoleName}[{player.RoleID}] bắt đầu mở RANDOMBOX, dbid {boxDbID}");
            if (dryRun && !KTGMCommandManager.IsGM(player))
            {
                return false;
            }

            if (GameManager.IsKuaFuServer)
            {
                PlayerManager.ShowNotification(player, "Vật phẩm không sử dụng được trên liên máy chủ!");
                return false;
            }

            GoodsData itemGD = null;
            ItemData itemData;

            if (dryRun)
            {
                itemData = ItemManager.GetItemTemplate(boxDbID);
            }
            else
            {
                // Vật phẩm tương ứng
                itemGD = Global.GetGoodsByDbID(player, boxDbID);
                // Nếu toác
                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                    return false;
                }

                // Thông tin vật phẩm
                itemData = ItemManager.GetItemTemplate(itemGD.GoodsID);
                // Nếu toác
                if (itemData == null)
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                    return false;
                }

                LogManager.WriteLog(LogTypes.RandomBox,
                    $"{player.RoleName}[{player.RoleID}] bắt đầu mở RANDOMBOX, goods id {itemGD.GoodsID}");
            }


            // ID loại hộp
            var boxID = itemData.ItemValue;

            return OpenRandomBoxByConfigID(player, boxID, itemGD?.GoodsID ?? -1, boxDbID, dryRun);
        }

        public static bool OpenRandomBoxByConfigID(KPlayer player, int boxID, int goodsID, int boxDbID,
            bool dryRun = false)
        {
            var findbox = _ConfigBox.Boxs.FirstOrDefault(x => x.IDBox == boxID);
            if (findbox != null)
            {
                findbox = findbox.DeepCopy();
                findbox.Items = findbox.Items.Where(x => x.Sex == -1 || x.Sex == player.RoleSex).ToList();

                LogManager.WriteLog(LogTypes.RandomBox,
                    $"{player.RoleName}[{player.RoleID}] bắt đầu mở RANDOMBOX, randombox id {boxID}");
                var TotalRate = findbox.TotalRate;

                var pool = findbox.Items.Where(x => x.Rate != -1).Sum(x => x.Rate);
                if (pool != findbox.TotalRate && TotalRate != -1)
                    SysConOut.WriteLine($"Open Box {findbox.BoxName}: {pool}/e{findbox.TotalRate}");

                // điều chỉnh lại vì totalOpenWeek và totalOpenInday tính từ 0 nên thừa 2 lần
                var LimitDay = findbox.LimitDay - 2;

                var LimitWeek = findbox.LimitWeek - 2;

                var LimitLifetime = findbox.LimitLifeTime - 2;

                if (dryRun)
                {
                    LimitDay = 999999;
                    LimitLifetime = 999999;
                    LimitWeek = 999999;
                }

                if (findbox.LimitWeek > 0)
                {
                    var TotalOpenInWeek = player.GetValueOfWeekRecore(boxID);

                    if (TotalOpenInWeek > LimitWeek)
                    {
                        PlayerManager.ShowNotification(player, "Tuần này bạn đã mở hết số lượt cho phép");
                        return false;
                    }
                }

                if (findbox.LimitDay > 0)
                {
                    var TotalOpenDay = player.GetValueOfDailyRecore(boxID);

                    if (TotalOpenDay > LimitDay)
                    {
                        PlayerManager.ShowNotification(player, "Hôm nay bạn đã mở hết số lượt cho phép");
                        return false;
                    }
                }

                if (findbox.LimitLifeTime > 0)
                {
                    var TotalOpen = player.GetValueOfForeverRecore(boxID);

                    if (TotalOpen > LimitLifetime)
                    {
                        PlayerManager.ShowNotification(player, "Bạn đã mở hết số lượt cho phép");
                        return false;
                    }
                }

                var selectedItems = new List<ItemRandom>();
                // ItemRandom _SelectItem = null;

                var totalSpaceNeeded = 0;
                var hasFixedItems = false;

                foreach (var item in findbox.Items.Where(item => item.Rate == -1))
                {
                    hasFixedItems = true;
                    selectedItems.Add(item);
                    if (item.Type == 0)
                    {
                        totalSpaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ItemID, item.Number);
                    }

                    if (item.Type == 8)
                        totalSpaceNeeded ++;
                }

                // if (TotalRate == -1)
                // {
                //     totalSpaceNeeded = 0;
                //     selectedItems.AddRange(findbox.Items);
                //     foreach (var item in selectedItems) 
                //         if (item.Type == 0)
                //         {
                //             var iData = ItemManager.GetItemTemplate(item.ItemID);
                //             totalSpaceNeeded += (item.Number - 1) / iData.Stack + 1;
                //         }
                // }
                // else
                var randomSpaceNeed = 4;
                {
                    var RandomValue = KTGlobal.GetRandomNumber(0, TotalRate);
                    var Add = 0;

                    foreach (var _Item in findbox.Items.Where(x => x.Rate != -1))
                    {
                        Add = Add + _Item.Rate;
                        if (Add >= RandomValue)
                        {
                            selectedItems.Add(_Item);
                            if (_Item.Type == 0)
                                randomSpaceNeed = Math.Max(randomSpaceNeed,
                                    KTGlobal.GetTotalSpacesNeedToTakeItem(_Item.ItemID, _Item.Number));
                            break;
                        }
                    }
                }

                totalSpaceNeeded += randomSpaceNeed;

                // Nếu ko đủ 1 ô để nhận thưởng thì thôi
                if (!KTGlobal.IsHaveSpace(totalSpaceNeeded, player))
                {
                    PlayerManager.ShowNotification(player, $"Vui lòng dọn dẹp kho đồ để tiếp tục mở vật phẩm.");
                    return false;
                }

                var AddOK = true;
                foreach (var _SelectItem in selectedItems)
                {
                    if (_SelectItem == null) continue;

                    switch (_SelectItem.Type)
                    {
                        case 1:
                            {
                                if (dryRun)
                                {
                                    var reward = $"Bạc Khóa [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(reward, out var value);
                                    RANDOMBOX_REWARD[reward] = value + 1;
                                    continue;
                                }

                                var AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number,
                                    MoneyType.BacKhoa, "RANDOMBOX_" + boxID);

                                if (AddMoneyKq.IsOK)
                                {
                                    LogManager.WriteLog(LogTypes.RandomBox,
                                        $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} bạc khóa.");
                                }

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 2:
                            {
                                if (dryRun)
                                {
                                    var reward = $"Bạc [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(reward, out var value);
                                    RANDOMBOX_REWARD[reward] = value + 1;
                                    continue;
                                }

                                var AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number, MoneyType.Bac,
                                    "RANDOMBOX_" + boxID);

                                if (AddMoneyKq.IsOK)
                                {
                                    LogManager.WriteLog(LogTypes.RandomBox,
                                        $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} bạc.");
                                }

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 3:
                            {
                                if (dryRun)
                                {
                                    var reward = $"KNB Khóa [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(reward, out var value);
                                    RANDOMBOX_REWARD[reward] = value + 1;
                                    continue;
                                }

                                var AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number,
                                    MoneyType.DongKhoa, "RANDOMBOX_" + boxID);

                                if (AddMoneyKq.IsOK)
                                {
                                    LogManager.WriteLog(LogTypes.RandomBox,
                                        $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} KNB khóa.");
                                }

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 4:
                            {
                                if (dryRun)
                                {
                                    var reward = $"KNB [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(reward, out var value);
                                    RANDOMBOX_REWARD[reward] = value + 1;
                                    continue;
                                }

                                var AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number, MoneyType.Dong,
                                    "RANDOMBOX_" + boxID);

                                if (AddMoneyKq.IsOK)
                                {
                                    LogManager.WriteLog(LogTypes.RandomBox,
                                        $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} KNB.");
                                }

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 5:
                            {
                                var expNumber = _SelectItem.Number;
                                if (boxID == 200344)
                                {
                                    // nếu là box cá vàng
                                    // + exp theo level của người chơi
                                    if (player.m_Level <= 100)
                                    {
                                        expNumber = 300000;
                                    }
                                    else if (player.m_Level <= 120)
                                    {
                                        expNumber = 250000;
                                    }
                                    else
                                    {
                                        expNumber = 200000;
                                    }
                                }

                                if (dryRun)
                                {
                                    var reward = $"EXP [{expNumber}]";
                                    RANDOMBOX_REWARD.TryGetValue(reward, out var value);
                                    RANDOMBOX_REWARD[reward] = value + 1;
                                    continue;
                                }

                                GameManager.ClientMgr.ProcessRoleExperience(player, expNumber, true, false, true,
                                    "RANDOMBOX_" + boxID);

                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {expNumber} EXP.");

                                break;
                            }
                        case 7:
                            {
                                if (dryRun)
                                {
                                    string nameRepute;
                                    // Cộng điểm danh vọng môn phái
                                    if (_SelectItem.ItemID == -1)
                                    {
                                        var _camp = player.m_cPlayerFaction.GetFactionId() + 300;
                                        KTGlobal.AddRepute(player, _camp, _SelectItem.Number);
                                        nameRepute = "môn phái " + ReputeManager.GetNameReputeClass(player, _camp);
                                    }
                                    else
                                    {
                                        KTGlobal.AddRepute(player, _SelectItem.ItemID, _SelectItem.Number);
                                        nameRepute = ReputeManager.GetNameReputeClass(player, _SelectItem.ItemID);
                                    }

                                    var reward = $"Repute [{_SelectItem.Number}] " + nameRepute;
                                    RANDOMBOX_REWARD.TryGetValue(reward, out var value);
                                    RANDOMBOX_REWARD[reward] = value + 1;
                                    continue;
                                }

                                if (player.m_cPlayerFaction.GetFactionId() == 0)
                                {
                                    PlayerManager.ShowNotification(player, "Bạn chưa gia nhập môn phái!");
                                    AddOK = false;
                                }

                                if (AddOK)
                                {
                                    string nameRepute;
                                    // Cộng điểm danh vọng môn phái
                                    if (_SelectItem.ItemID == -1)
                                    {
                                        var _camp = player.m_cPlayerFaction.GetFactionId() + 300;
                                        KTGlobal.AddRepute(player, _camp, _SelectItem.Number);
                                        nameRepute = "môn phái " + ReputeManager.GetNameReputeClass(player, _camp);
                                    }
                                    else
                                    {
                                        KTGlobal.AddRepute(player, _SelectItem.ItemID, _SelectItem.Number);
                                        nameRepute = ReputeManager.GetNameReputeClass(player, _SelectItem.ItemID);
                                    }

                                    PlayerManager.ShowNotification(player,
                                        $"Bạn nhận được {_SelectItem.Number} điểm danh vọng " + nameRepute);
                                    LogManager.WriteLog(LogTypes.RandomBox,
                                        $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} điểm danh vọng {nameRepute}.");

                                    var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                                    var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                                    var reward = KTGlobal.CreateStringByColor(
                                        $"{_SelectItem.Number} điểm danh vọng " + nameRepute, ColorType.PeachYellow);
                                    if ((_SelectItem.ShouldNotify & 1) == 1) // thế giới, chữ chạy ngang
                                        KTGlobal.SendSystemEventNotification(
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                                    if ((_SelectItem.ShouldNotify >> 1 & 1) != 0) // gia tộc
                                        KTGlobal.SendFamilyChat(Global._TCPManager.MySocketListener,
                                            Global._TCPManager.TcpOutPacketPool, player.FamilyID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                                    if ((_SelectItem.ShouldNotify >> 2 & 1) != 0) // bang hội
                                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener,
                                            Global._TCPManager.TcpOutPacketPool, player.GuildID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                                }

                                break;
                            }
                        case 0:
                            {
                                if (dryRun)
                                {
                                    var rewardName = ItemManager.GetItemTemplate(_SelectItem.ItemID).Name;
                                    rewardName = $"{rewardName} [{_SelectItem.Number}]";
                                    if (_SelectItem.TimeLimit > 0)
                                        rewardName = $"{rewardName} [{_SelectItem.TimeLimit}]";
                                    RANDOMBOX_REWARD.TryGetValue(rewardName, out var value);
                                    RANDOMBOX_REWARD[rewardName] = value + 1;
                                    continue;
                                }

                                var TimeFix = Global.ConstGoodsEndTime;

                                if (!string.IsNullOrEmpty(_SelectItem.ExpiryDate))
                                {
                                    var expiryDate = DateTime.Parse(_SelectItem.ExpiryDate, CultureInfo.InvariantCulture);
                                    if (_SelectItem.TimeLimit > 0)
                                    {
                                        var timeLimit = DateTime.Now.AddMinutes(_SelectItem.TimeLimit);
                                        TimeFix = expiryDate > timeLimit
                                            ? timeLimit.ToString("yyyy-MM-dd HH:mm:ss")
                                            : expiryDate.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    else
                                    {
                                        TimeFix = expiryDate.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                }
                                else if (_SelectItem.TimeLimit > 0)
                                {
                                    // var TMP = "1900-01-01 12:00:00";

                                    var Dt = DateTime.Now.AddMinutes(_SelectItem.TimeLimit);

                                    TimeFix = Dt.ToString("yyyy-MM-dd HH:mm:ss");
                                }

                                // Có khóa không
                                var bindState = _SelectItem.Lock;
                                // Add thêm đoạn này để mở khóa rương tranh đoạt lãnh thổ
                                // if (boxID == 2176 || boxID == 2177)
                                // {
                                //     if (itemGD == null)
                                //         bindState = 1;
                                //     else
                                //         bindState = _SelectItem.Lock == 1 ? 1 : itemGD.Binding;
                                // }

                                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, _SelectItem.ItemID,
                                        _SelectItem.Number, 0, "RANDOMBOX_" + boxID, true, bindState, false, TimeFix, "",
                                        _SelectItem.Series, "", _SelectItem.Forge, 1, true))
                                {
                                    PlayerManager.ShowNotification(player, "Có lỗi khi nhận vật phẩm chế tạo");

                                    AddOK = false;
                                }
                                else
                                {
                                    var itemTemplate = ItemManager.GetItemTemplate(_SelectItem.ItemID);
                                    var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                                    var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                                    var reward = KTGlobal.CreateStringByColor($"{_SelectItem.Number} {itemTemplate.Name}",
                                        ColorType.PeachYellow);
                                    if ((_SelectItem.ShouldNotify & 1) == 1) // thế giới, chữ chạy ngang
                                        KTGlobal.SendSystemEventNotification(
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                                    if ((_SelectItem.ShouldNotify >> 1 & 1) != 0) // gia tộc
                                        KTGlobal.SendFamilyChat(Global._TCPManager.MySocketListener,
                                            Global._TCPManager.TcpOutPacketPool, player.FamilyID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                                    if ((_SelectItem.ShouldNotify >> 2 & 1) != 0) // bang hội
                                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener,
                                            Global._TCPManager.TcpOutPacketPool, player.GuildID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                                    LogManager.WriteLog(LogTypes.RandomBox,
                                        $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {reward}.");
                                }

                                break;
                            }
                        case 6:
                            {
                                if (dryRun)
                                    continue;

                                // Nếu không có vật phẩm
                                if (player.GoodsDataList == null)
                                {
                                    PlayerManager.ShowNotification(player, "Bạn chưa trang bị mật tịch.");
                                    AddOK = false;
                                }

                                // Sách tương ứng
                                GoodsData book;
                                // Thông tin sách tương ứng
                                lock (player.GoodsDataList)
                                {
                                    book = player.GoodsDataList
                                        .FirstOrDefault(x => x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_BOOK);
                                }

                                // Nếu không có sách
                                if (book == null)
                                {
                                    PlayerManager.ShowNotification(player, "Bạn chưa trang bị mật tịch.");
                                    AddOK = false;
                                }

                                MonsterDeadHelper.BookProseccExp(player, _SelectItem.Number, false);
                                break;
                            }
                        case 8:
                            {
                                if (dryRun)
                                    continue;

                                var titleID = _SelectItem.Number;
                                var titleData = KTTitleManager.GetTitleData(titleID);
                                
                                if (!player.AddRoleTitle(titleID))
                                    continue;
                                PlayerManager.ShowNotification(player, $"Bạn đã nhận được danh hiệu {titleData.Text}");
                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được danh hiệu {titleData.Text}.");
                                break;
                            }
                        case 9:
                            {
                                if (dryRun)
                                    continue;

                                player.ChangeCurMakePoint(_SelectItem.Number);
                                PlayerManager.ShowNotification(player, $"Bạn đã nhận được {_SelectItem.Number} hoạt lực");
                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} hoạt lực.");
                                break;
                            }
                        case 10:
                            {
                                if (dryRun)
                                    continue;

                                player.ChangeCurGatherPoint(_SelectItem.Number);
                                PlayerManager.ShowNotification(player, $"Bạn đã nhận được {_SelectItem.Number} tinh lực");
                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} tinh lực.");
                                break;
                            }
                        case 11:
                            {
                                if (dryRun)
                                    continue;
                                player.ChangeCurMakePoint(_SelectItem.Number);
                                player.ChangeCurGatherPoint(_SelectItem.Number);
                                PlayerManager.ShowNotification(player, $"Bạn đã nhận được {_SelectItem.Number} tinh hoạt lực");
                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {_SelectItem.Number} tinh hoạt lực.");
                                break;
                            }
                        default:
                            if (dryRun)
                            {
                                var rewardName = "Không thưởng";
                                RANDOMBOX_REWARD.TryGetValue(rewardName, out var value);
                                RANDOMBOX_REWARD[rewardName] = value + 1;
                            }

                            if (!hasFixedItems)
                            {
                                var showBoxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                                PlayerManager.ShowNotification(player,
                                    $"Rất tiếc, bạn mở {showBoxName} không nhận được vật phẩm nào!");

                                if (findbox.IDBox == 200382 || findbox.IDBox == 200381)
                                {
                                    // rương đạo tặc sự kiện tháng 4
                                    // chạy thông báo hệ thống
                                    KTGlobal.SendSystemEventNotification(player.RoleSex == 0
                                        ? $"Đẹp trai, tài năng, tư duy chiến thuật đỉnh cao là những thứ mà {player.RoleName} không có. Hẹn gặp lại nhé con GÀ!"
                                        : $"Nếu khoảng cách giữa anh và {player.RoleName} là 1000 bước, thì anh xin phép được bước về nhà! Nhà bao việc!");
                                }
                                if (findbox.IDBox == 200385)
                                {
                                    // rương giftcode thần bí
                                    // chạy thông báo hệ thống
                                    KTGlobal.SendSystemEventNotification($"Người chơi {player.RoleName} mở Rương Giftcode Thần Bí không nhận được gì. Thật may mắn!");
                                }
                                if (findbox.IDBox == 200539)
                                {
                                    // rương giftcode thần bí
                                    // chạy thông báo hệ thống
                                    KTGlobal.SendSystemEventNotification($"Hoa Nhi xin gửi lời chúc đến đại hiệp {player.RoleName}, chúc đại hiệp hay ăn chóng lớn, sức khỏe dồi dào để tiếp tục chinh phục Kiếm Vương 1 trong năm Ất Tỵ - 2025.");
                                }
                            }

                            break;
                    }
                }

                if (dryRun)
                    return false;

                if (!AddOK)
                    return false;
                
                // đã mở thành công

                
                GlobalEventSource.getInstance().fireEvent(new OpenRandomBoxEventObject(player, goodsID));

                // var actualOpenTotal = player.GetValueOfForeverRecore(boxID) + 2;
                // var eventBoxIndex = ComposeEventManager.IsEventBox(goodsID);
                // if (eventBoxIndex == 1 || eventBoxIndex == 2)
                // {
                //     var roleParamName = RoleParamName.MonthlyBox1;
                //     if (eventBoxIndex == 2)
                //         roleParamName = RoleParamName.MonthlyBox2;
                //     actualOpenTotal = Global.GetRoleParamsInt32FromDB(player, roleParamName, 0) + 1;
                // }

                /*
                if (findbox.Milestones != null && findbox.Milestones.Any())
                {
                    var currentMilestones = findbox.Milestones.Where(x => x.Milestone == actualOpenTotal);
                    if (currentMilestones != null)
                    {
                        foreach (var currentMilestone in currentMilestones)
                        {
                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                                    currentMilestone.RewardID,
                                    currentMilestone.Number, 0, $"RANDOMBOX_{boxID}_MILESTONE_REWARD", true,
                                    currentMilestone.Binding, false, Global.ConstGoodsEndTime, "",
                                    -1, "", 0, 1, true))
                            {
                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID} {currentMilestone.Milestone} lượt, nhận phần thưởng lỗi.");
                                PlayerManager.ShowNotification(player, "Có lỗi khi nhận phần thưởng đạt mốc.");
                            }
                            else
                            {
                                var itemTemplate = ItemManager.GetItemTemplate(currentMilestone.RewardID);
                                var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                                var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                                var reward = KTGlobal.CreateStringByColor(
                                    $"{currentMilestone.Number} {itemTemplate.Name}",
                                    ColorType.PeachYellow);
                                KTGlobal.SendSystemEventNotification(
                                    $"Chúc mừng {playerName} mở {currentMilestone.Milestone} {boxName} nhận được {reward}!");
                                LogManager.WriteLog(LogTypes.RandomBox,
                                    $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID} {currentMilestone.Milestone} lượt, nhận được {reward}.");
                            }
                        }

                    }
                }

                var actualOpenToday = player.GetValueOfDailyRecore(boxID) + 2;
                if (findbox.DailyMilestones != null && findbox.DailyMilestones.Any())
                {
                    var currentMilestones = findbox.DailyMilestones.Where(x => x.Milestone == actualOpenToday);
                    foreach (var currentMilestone in currentMilestones)
                    {
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                                currentMilestone.RewardID,
                                currentMilestone.Number, 0, $"RANDOMBOX_{boxID}_MILESTONE_REWARD", true,
                                currentMilestone.Binding, false, Global.ConstGoodsEndTime, "",
                                -1, "", 0, 1, true))
                        {
                            LogManager.WriteLog(LogTypes.RandomBox,
                                $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID} {currentMilestone.Milestone} lượt, nhận phần thưởng lỗi.");
                            PlayerManager.ShowNotification(player, "Có lỗi khi nhận phần thưởng đạt mốc.");
                        }
                        else
                        {
                            var itemTemplate = ItemManager.GetItemTemplate(currentMilestone.RewardID);
                            var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                            var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                            var reward = KTGlobal.CreateStringByColor(
                                $"{currentMilestone.Number} {itemTemplate.Name}",
                                ColorType.PeachYellow);
                            KTGlobal.SendSystemEventNotification(
                                $"Chúc mừng {playerName} mở {currentMilestone.Milestone} {boxName} nhận được {reward}!");
                            LogManager.WriteLog(LogTypes.RandomBox,
                                $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID} {currentMilestone.Milestone} lượt, nhận được {reward}.");
                        }
                    }
                }
                */

                if (findbox.LimitReachedReward != 0)
                {
                    // có cấu hình phần thưởng khi mở full rương
                    var totalOpen = player.GetValueOfForeverRecore(boxID);
                    if (totalOpen > LimitLifetime - 1)
                    {
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                                findbox.LimitReachedReward,
                                1, 0, $"RANDOMBOX_{boxID}_REWARD", true, findbox.LimitReachedRewardBinding, false,
                                Global.ConstGoodsEndTime, "",
                                -1, "", 0, 1, true))
                        {
                            PlayerManager.ShowNotification(player, "Có lỗi khi nhận vật phẩm chế tạo");
                        }
                        else
                        {
                            var itemTemplate = ItemManager.GetItemTemplate(findbox.LimitReachedReward);
                            var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                            var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                            var reward = KTGlobal.CreateStringByColor($"{itemTemplate.Name}",
                                ColorType.PeachYellow);
                            KTGlobal.SendSystemEventNotification(
                                $"Chúc mừng {playerName} mở {boxName} nhận được {reward}!");
                            LogManager.WriteLog(LogTypes.RandomBox,
                                $"{player.RoleName}[{player.RoleID}] mở RANDOMBOX {boxDbID}|{goodsID}|{boxID}, nhận được {reward}.");
                        }
                    }
                }

                if (findbox.LimitWeek > 0)
                {
                    var TotalOpenInWeek = player.GetValueOfWeekRecore(boxID);

                    player.SetValueOfWeekRecore(boxID, TotalOpenInWeek + 1);
                    PlayerManager.ShowNotification(player,
                        $"Mở {KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.PeachYellow)} thành công. Trong tuần còn lại {KTGlobal.CreateStringByColor("" + (findbox.LimitWeek - 1 - (TotalOpenInWeek + 1)), ColorType.VanillaIce)} lượt mở.");
                }


                var TotalOpenToDay = player.GetValueOfDailyRecore(boxID);
                player.SetValueOfDailyRecore(boxID, TotalOpenToDay + 1);
                if (findbox.LimitDay > 0)
                {
                    PlayerManager.ShowNotification(player,
                        $"Mở {KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.PeachYellow)} thành công. Trong ngày còn lại {KTGlobal.CreateStringByColor("" + (findbox.LimitDay - 1 - (TotalOpenToDay + 1)), ColorType.VanillaIce)} lượt mở.");
                }

                var TotalOpenLifeTime = player.GetValueOfForeverRecore(boxID);
                player.SetValueOfForeverRecore(boxID, TotalOpenLifeTime + 1);

                // var eventBoxIndex = ComposeEventManager.IsEventBox(goodsID);
                // if (eventBoxIndex == 1 || eventBoxIndex == 2)
                // {
                //     var roleParamName = RoleParamName.MonthlyBox1;
                //     if (eventBoxIndex == 2)
                //         roleParamName = RoleParamName.MonthlyBox2;
                //     var currentParam = Global.GetRoleParamsInt32FromDB(player, roleParamName, 0);
                //     Global.SaveRoleParamsInt32ValueToDB(player, roleParamName, currentParam + 1, false);
                // }

                if (findbox.LimitLifeTime > 0)
                {
                    PlayerManager.ShowNotification(player,
                        $"Mở {KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.PeachYellow)} thành công. Còn lại {KTGlobal.CreateStringByColor("" + (findbox.LimitLifeTime - 1 - (TotalOpenLifeTime + 1)), ColorType.VanillaIce)} lượt mở.");
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}