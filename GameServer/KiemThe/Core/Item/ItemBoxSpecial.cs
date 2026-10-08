using GameServer.KiemThe.Core.Repute;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.FactionBattle;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.Logic;
using KF.Contract.Interface;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace GameServer.KiemThe.Core.Item
{
    public class ItemBoxSpecial
    {
        public static string SpecialBoxConfig = "Config/KT_Item/KT_ConfigSpecialBox.xml";

        public static ConfigBoxSpecial _ConfigBox = new ConfigBoxSpecial();

        public static void Setup()
        {
            string Files = Global.GameResPath(SpecialBoxConfig);

            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(ConfigBoxSpecial));
                _ConfigBox = serializer.Deserialize(stream) as ConfigBoxSpecial;
            }
        }

        public static ConcurrentDictionary<string, int> RANDOMBOX_REWARD;
        /// <summary>
        /// Lưu lại dữ liệu thời gian bắt đầu sử dụng
        /// </summary>
        private static bool SaveInfoTimeOpen(KPlayer player, int itemID, int TimeOpen)
        {
            if (player.GoodsDataList == null)
                return false;
            GoodsData BoxSpecialItem = null;
            lock (player.GoodsDataList)
            {
                BoxSpecialItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
            }
            if (BoxSpecialItem == null)
                return false;

            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
            TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, BoxSpecialItem.Id);

            int Step = GetDataBoxSpecial(player, itemID, DataGet.Step);
            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            OtherParams.Add(ItemPramenter.Pram_1, TimeOpen + "");
            OtherParams.Add(ItemPramenter.Pram_2, Step + "");

            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, BoxSpecialItem, player, "BOXSPECIAL", true))
            {
                string cmd = $"Người chơi: {player.RoleName} lưu mở box special {BoxSpecialItem.Id} thất bại";
                LogManager.WriteLog(LogTypes.Error, cmd);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Lưu lại dữ liệu khi sử dụng mở ở mốc mấy rồi, mở đc mấy lần rồi
        /// </summary>
        private static bool SaveInfoReward(KPlayer player, int itemID , int Step)
        {
            if (player.GoodsDataList == null)
                return false;
            GoodsData BoxSpecialItem = null;
            lock (player.GoodsDataList)
            {
                BoxSpecialItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
            }
            if (BoxSpecialItem == null)
                return false;

            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
            TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, BoxSpecialItem.Id);

            int Day = GetDataBoxSpecial(player, itemID, DataGet.TimeOpen);
            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            OtherParams.Add(ItemPramenter.Pram_1, Day + "");
            OtherParams.Add(ItemPramenter.Pram_2, Step + "");

            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, BoxSpecialItem, player, "BOXSPECIAL", true))
            {
                string cmd = $"Người chơi: {player.RoleName} lưu mở box special {BoxSpecialItem.Id} thất bại";
                LogManager.WriteLog(LogTypes.Error, cmd);
                return false;
            }
            return true;
        }
        public static bool IsHaveDataBoxSpecial(KPlayer player, int DBIdBox)
        {
            if (player.GoodsDataList == null)
                return false;
            GoodsData RandoxBoxItem = null;
            lock (player.GoodsDataList)
            {
                RandoxBoxItem = player.GoodsDataList.Where(x => x.Id == DBIdBox).FirstOrDefault();
            }
            int dayOpen = 0;
            int step = 0;
            if (RandoxBoxItem.OtherParams == null)
                return false;
            if (RandoxBoxItem.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string DayOpen) &&
                RandoxBoxItem.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string Step) )
            {
                try
                {
                    dayOpen = Int32.Parse(DayOpen);
                    step = Int32.Parse(Step);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        /// Lưu lại dữ liệu của đồ lúc tạo cho người chơi để tính phần thưởng 
        /// </summary>
        private static bool CreateDataInfoSpecialBox(KPlayer player, int itemID)
        {
            if (player.GoodsDataList == null)
                return false;
            GoodsData BoxSpecialItem = null;
            lock (player.GoodsDataList)
            {
                BoxSpecialItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
            }
            if (BoxSpecialItem == null)
                return false;
            int Day = DateTime.Now.DayOfYear;
            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
            TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, BoxSpecialItem.Id);

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            OtherParams.Add(ItemPramenter.Pram_1, Day + "");
            OtherParams.Add(ItemPramenter.Pram_2, 0 + "");

            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, BoxSpecialItem, player, "BOXSPECIAL", true))
            {
                string cmd = $"Người chơi: {player.RoleName} lưu thời gian tạo box special {BoxSpecialItem.Id} thất bại";
                LogManager.WriteLog(LogTypes.Error, cmd);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lấy ra dữ liệu của đồ 
        /// </summary>
        public static int GetDataBoxSpecial(KPlayer player, int itemID, DataGet data)
        {
            int _Data = -1; 
            if(player.GoodsDataList != null)
            {
                GoodsData BoxSpecialItem = null;
                lock (player.GoodsDataList)
                {
                    BoxSpecialItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
                }
                try
                {
                    if (BoxSpecialItem.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string _DateTime) &&
                        BoxSpecialItem.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string _Step))
                    {
                        // kiểm tra xem là cần lấy dữ liệu gì thời gian hay là check nhận đến bước mấy rồi
                        switch (data)
                        {
                            case DataGet.TimeOpen:
                                _Data = Int32.Parse(_DateTime);
                                break;
                            case DataGet.Step:
                                _Data = Int32.Parse(_Step);
                                break;
                        }

                    }
                }
                catch
                {
                    _Data = -1;
                }
            }
            return _Data;
        }
        public static int GetMaxStepCanCollect(KPlayer player, int ItemID)
        {
            int stepMax = 0;
            var Item = Global.GetGoodsByDbID(player, ItemID);
            ItemData _Template = ItemManager.GetItemTemplate(Item.GoodsID);
            SpecialBox box = _ConfigBox.BoxSpecials.FirstOrDefault(x => x.IDBox == _Template.ItemValue);
            stepMax = box.TotalTime;
            return stepMax;
        }
        /// <summary>
        /// Lấy ra đang đến mốc mấy để tính thưởng
        /// </summary>
        private static int StepAward(KPlayer player, GoodsData Item)
        {
            int step = -1;
            var _TimeOpen = GetDataBoxSpecial(player, Item.Id, DataGet.TimeOpen);
            ItemData _Template = ItemManager.GetItemTemplate(Item.GoodsID);

            if (_TimeOpen == -1)
                return -1;

            int currentTime = DateTime.Now.DayOfYear - _TimeOpen;

            SpecialBox box = _ConfigBox.BoxSpecials.FirstOrDefault(x => x.IDBox == _Template.ItemValue);

            // không tìm thấy thì thôi luôn
            if (box == null)
                return -1;

            int stepData = GetDataBoxSpecial(player, Item.Id, DataGet.Step);
            foreach (var item in box.Boxs)
            {
                if (currentTime >= 0 && currentTime >= item.TimeUsed && stepData < item.IDTime)
                {
                    step = item.IDTime;
                    break;
                }
            }
            if (step < 0)
                step = stepData;
            //Không còn mốc nào nữa thì xóa vật phẩm đi
            if (stepData == box.TotalTime)
                return -10;

            return step;
        }

        /// <summary>
        /// Thêm ngày sở hữu để test logic
        /// </summary>
        public static void AddMoreDayOpenItem(KPlayer player, int itemID)
        {
            if(!IsHaveDataBoxSpecial(player, itemID))
            {
                CreateDataInfoSpecialBox(player, itemID);
            }
            int timeOpen = GetDataBoxSpecial(player, itemID, DataGet.TimeOpen);
            SaveInfoTimeOpen(player, itemID, timeOpen - 1);
        }

        public static void CheckRemoveItemBoxSpecial(KPlayer player, int boxDbID)
        {
            int step = GetDataBoxSpecial(player, boxDbID, DataGet.Step);
            int timeCreate = GetDataBoxSpecial(player, boxDbID, DataGet.TimeOpen);
            var Item = Global.GetGoodsByDbID(player, boxDbID);
            //Thực hiện khóa vật phẩm
            if(Item.Binding == 0)
            {
                Global.SetBindGoodsData(player, Item, "BOXSPECIAL", 1);
            }
            ItemData _Template = ItemManager.GetItemTemplate(Item.GoodsID);
            SpecialBox box = _ConfigBox.BoxSpecials.FirstOrDefault(x => x.IDBox == _Template.ItemValue);
            // không tìm thấy thì thôi luôn
            if (box == null)
                return;

            int currentTime = DateTime.Now.DayOfYear - timeCreate;
            int maxDayUsed = 0;
            foreach (var item in box.Boxs)
            {
                if (maxDayUsed < item.TimeUsed)
                {
                    maxDayUsed = item.TimeUsed;
                }

            }

            if (step == box.TotalTime)
            {
                ItemManager.RemoveItem(player, boxDbID);
            }
        }

        public static bool OpenBoxSpecial(KPlayer player, int boxDbID, bool dryRun = false)
        {
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
            ItemData itemData = null;
            if (dryRun)
            {
                itemData = ItemManager.GetItemTemplate(boxDbID);
            }
            else
            {
                itemGD = Global.GetGoodsByDbID(player, boxDbID);

                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                    return false;
                }

                /// Thông tin vật phẩm
                itemData = ItemManager.GetItemTemplate(itemGD.GoodsID);
                /// Nếu toác
                if (itemData == null)
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                    return false;
                }
            }
            /// ID loại hộp
            int boxID = itemData.ItemValue;

            var findbox = _ConfigBox.BoxSpecials.FirstOrDefault(x => x.IDBox == boxID);

            if (findbox == null)
            {
                PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                return false;
            }

            /// điều chỉnh lại vì totalOpenWeek và totalOpenInday tính từ 0 nên thừa 2 lần
            int LimitDay = findbox.LimitDay - 2;

            int LimitWeek = findbox.LimitWeek - 2;

            var LimitLifetime = findbox.LimitLifeTime - 2;


            if (dryRun)
            {
                LimitDay = 999999;
                LimitLifetime = 999999;
                LimitWeek = 999999;
            }

            if (!IsHaveDataBoxSpecial(player, boxDbID) && findbox.LimitLifeTime > 0)
            {
                var TotalOpen = player.GetValueOfForeverRecore(itemGD.GoodsID);

                if (TotalOpen > LimitLifetime)
                {
                    PlayerManager.ShowNotification(player, $"Bạn chỉ có thể sử dụng {findbox.LimitLifeTime} vật phẩm này.");
                    return false;
                }
            }

            if (findbox.LimitWeek > 0)
            {
                int TotalOpenInWeek = player.GetValueOfWeekRecore(itemGD.GoodsID);

                if (TotalOpenInWeek > LimitWeek)
                {
                    PlayerManager.ShowNotification(player, "Tuần này bạn đã mở hết số lượt cho phép");
                    return false;
                }
            }

            if (findbox.LimitDay > 0)
            {
                int TotalOpenDay = player.GetValueOfDailyRecore(itemGD.GoodsID);

                if (TotalOpenDay > LimitDay)
                {
                    PlayerManager.ShowNotification(player, "Hôm nay bạn đã mở vật phẩm này hết số lượt cho phép");
                    return false;
                }
            }


            //Kiểm tra xem có dữ liện box chưa
            if (!IsHaveDataBoxSpecial(player, boxDbID))
            {
                CreateDataInfoSpecialBox(player, boxDbID);
            }

            int step = StepAward(player, itemGD);
            if (step > 0)
            {
                var reward = findbox.Boxs.FirstOrDefault(x => x.IDTime == step);
                if(reward == null) 
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                    return false;
                }

                int stepDb = GetDataBoxSpecial(player, boxDbID, DataGet.Step);
                // nhận rồi thì thôi
                if (stepDb >= step)
                {
                    PlayerManager.ShowNotification(player, "Bạn đã nhận hết phần thưởng của mốc thời gian này rồi");
                    return false;
                }

                List<ItemRandom> selectedItems = new List<ItemRandom>();

                int TotalRate = reward.TotalRate;

                var totalSpaceNeeded = 0;
                foreach (var item in reward.Items.Where(item => item.Rate == -1))
                {
                    selectedItems.Add(item);
                    if (item.Type == 0)
                    {
                        totalSpaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ItemID, item.Number);
                    }
                }
                var randomSpaceNeed = 4;
                var RandomValue = KTGlobal.GetRandomNumber(0, TotalRate);
                var Add = 0;

                foreach (ItemRandom _Item in reward.Items.Where(x => x.Rate != -1))
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

                totalSpaceNeeded += randomSpaceNeed;

                if (!KTGlobal.IsHaveSpace(totalSpaceNeeded, player))
                {
                    PlayerManager.ShowNotification(player, $"Cần ít nhất {totalSpaceNeeded} ô trống để mở");
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
                                    var rewardstr = $"Bạc Khóa [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(rewardstr, out var value);
                                    RANDOMBOX_REWARD[rewardstr] = value + 1;
                                    continue;
                                }
                                var AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number,
                                    Entities.MoneyType.BacKhoa, "RANDOMBOXSPECIAL_" + boxID);

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 2:
                            {
                                if (dryRun)
                                {
                                    var rewardstr = $"Bạc [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(rewardstr, out var value);
                                    RANDOMBOX_REWARD[rewardstr] = value + 1;
                                    continue;
                                }
                                SubRep AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number, Entities.MoneyType.Bac,
                                    "RANDOMBOXSPECIAL_" + boxID);

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 3:
                            {
                                if (dryRun)
                                {
                                    var rewardstr = $"KNB Khóa [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(rewardstr, out var value);
                                    RANDOMBOX_REWARD[rewardstr] = value + 1;
                                    continue;
                                }
                                SubRep AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number,
                                    Entities.MoneyType.DongKhoa, "RANDOMBOXSPECIAL_" + boxID);

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 4:
                            {
                                if (dryRun)
                                {
                                    var rewardstr = $"KNB [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(rewardstr, out var value);
                                    RANDOMBOX_REWARD[rewardstr] = value + 1;
                                    continue;
                                }
                                SubRep AddMoneyKq = KTGlobal.AddMoney(player, _SelectItem.Number, Entities.MoneyType.Dong,
                                    "RANDOMBOXSPECIAL_" + boxID);

                                AddOK = AddOK && AddMoneyKq.IsOK;
                                break;
                            }
                        case 5:
                            {
                                if (dryRun)
                                {
                                    var rewardstr = $"EXP [{_SelectItem.Number}]";
                                    RANDOMBOX_REWARD.TryGetValue(rewardstr, out var value);
                                    RANDOMBOX_REWARD[rewardstr] = value + 1;
                                    continue;
                                }

                                var expToGet = _SelectItem.Number;

                                // hard fix cho rương ưu đãi điểm năng động VIP (logic phức tạp khó thể hiện trong xml)
                                if (boxID ==  3 || boxID == 4)
                                {
                                    switch (expToGet)
                                    {
                                        case 1 when (player.m_Level >= 50 && player.m_Level < 52):
                                            GameManager.ClientMgr.SetRoleLevel(player, 52);
                                            PlayerManager.ShowNotification(player, "Chúc mừng bạn đã thăng cấp lên 52");
                                            LogManager.WriteLog(LogTypes.ActivityPlayer, $"Player: ({player.RoleID}/{player.RoleName}) tăng lên cấp 52 rương ĐNĐ Vip");
                                            player.m_Experience = 0;
                                            break;
                                        case 1 when player.m_Level < 50 :
                                            GameManager.ClientMgr.SetRoleLevel(player, 50);
                                            PlayerManager.ShowNotification(player, "Chúc mừng bạn đã thăng cấp lên 50");
                                            LogManager.WriteLog(LogTypes.ActivityPlayer, $"Player: ({player.RoleID}/{player.RoleName}) tăng lên cấp 50 rương ĐNĐ Vip");
                                            player.m_Experience = 0;
                                            break;
                                        case 2 when (player.m_Level >= 60 && player.m_Level < 62):
                                            GameManager.ClientMgr.SetRoleLevel(player, 62);
                                            PlayerManager.ShowNotification(player, "Chúc mừng bạn đã thăng cấp lên 62");
                                            LogManager.WriteLog(LogTypes.ActivityPlayer, $"Player: ({player.RoleID}/{player.RoleName}) tăng lên cấp 62 rương ĐNĐ Vip");
                                            player.m_Experience = 0;
                                            break;
                                        case 2 when player.m_Level < 60:
                                            GameManager.ClientMgr.SetRoleLevel(player, 60);
                                            PlayerManager.ShowNotification(player, "Chúc mừng bạn đã thăng cấp lên 60");
                                            LogManager.WriteLog(LogTypes.ActivityPlayer, $"Player: ({player.RoleID}/{player.RoleName}) tăng lên cấp 60 rương ĐNĐ Vip");
                                            player.m_Experience = 0;
                                            break;
                                        default:
                                            PlayerManager.ShowNotification(player, "Bạn đã vượt cấp của mốc thưởng!");
                                            LogManager.WriteLog(LogTypes.ActivityPlayer, $"Player: ({player.RoleID}/{player.RoleName}), level {player.m_Level} vượt quá level mốc thưởng {expToGet + 1} rương ĐNĐ Vip");
                                            break;
                                    }

                                    expToGet = 0;
                                }

                                GameManager.ClientMgr.ProcessRoleExperience(player, expToGet, true, false, true,
                                    "RANDOMBOXSPECIAL_" + boxID);
                                break;
                            }
                        case 7:
                            {
                                if (dryRun)
                                {
                                    var nameRepute = "";
                                    // Cộng điểm danh vọng môn phái
                                    if (_SelectItem.ItemID == -1)
                                    {
                                        int _camp = player.m_cPlayerFaction.GetFactionId() + 300;
                                        KTGlobal.AddRepute(player, _camp, _SelectItem.Number);
                                        nameRepute = "môn phái " + ReputeManager.GetNameReputeClass(player, _camp);
                                    }
                                    else
                                    {
                                        KTGlobal.AddRepute(player, _SelectItem.ItemID, _SelectItem.Number);
                                        nameRepute = ReputeManager.GetNameReputeClass(player, _SelectItem.ItemID);
                                    }
                                    var rewardstr = $"Repute [{_SelectItem.Number}] " + nameRepute;
                                    RANDOMBOX_REWARD.TryGetValue(rewardstr, out var value);
                                    RANDOMBOX_REWARD[rewardstr] = value + 1;
                                    continue;
                                }
                                if (player.m_cPlayerFaction.GetFactionId() == 0)
                                {
                                    PlayerManager.ShowNotification(player, "Bạn chưa gia nhập môn phái!");
                                    AddOK = false;
                                }
                                if (AddOK)
                                {
                                    var nameRepute = "";
                                    // Cộng điểm danh vọng môn phái
                                    if (_SelectItem.ItemID == -1)
                                    {
                                        int _camp = player.m_cPlayerFaction.GetFactionId() + 300;
                                        KTGlobal.AddRepute(player, _camp, _SelectItem.Number);
                                        nameRepute = "môn phái " + ReputeManager.GetNameReputeClass(player, _camp);
                                    }
                                    else
                                    {
                                        KTGlobal.AddRepute(player, _SelectItem.ItemID, _SelectItem.Number);
                                        nameRepute = ReputeManager.GetNameReputeClass(player, _SelectItem.ItemID);
                                    }
                                    PlayerManager.ShowNotification(player, $"Bạn nhận được {_SelectItem.Number} điểm danh vọng " + nameRepute);
                                    var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                                    var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                                    var rewardstr = KTGlobal.CreateStringByColor($"{_SelectItem.Number} điểm danh vọng " + nameRepute, ColorType.PeachYellow);
                                    if ((_SelectItem.ShouldNotify & 1) == 1) // thế giới, chữ chạy ngang
                                        KTGlobal.SendSystemEventNotification($"Chúc mừng {playerName} mở {boxName} nhận được {rewardstr}!");
                                    if ((_SelectItem.ShouldNotify >> 1 & 1) != 0) // gia tộc
                                        KTGlobal.SendFamilyChat(Global._TCPManager.MySocketListener,
                                        Global._TCPManager.TcpOutPacketPool, player.FamilyID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {rewardstr}!");
                                    if ((_SelectItem.ShouldNotify >> 2 & 1) != 0) // bang hội
                                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener,
                                        Global._TCPManager.TcpOutPacketPool, player.GuildID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {rewardstr}!");
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

                                    DateTime Dt = DateTime.Now.AddMinutes(_SelectItem.TimeLimit);

                                    TimeFix = Dt.ToString("yyyy-MM-dd HH:mm:ss");
                                }

                                /// Có khóa không
                                int bindState = _SelectItem.Lock;
                                /// Add thêm đoạn này để mở khóa rương tranh đoạt lãnh thổ
                                if (boxID == 2176 || boxID == 2177)
                                {
                                    if (itemGD == null)
                                        bindState = 1;
                                    else
                                        bindState = _SelectItem.Lock == 1 ? 1 : itemGD.Binding;
                                }

                                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, _SelectItem.ItemID,
                                        _SelectItem.Number, 0, "RANDOMBOXSPECIAL_" + boxID, true, bindState, false, TimeFix, "",
                                        _SelectItem.Series, "", 0, 1, true))
                                {
                                    PlayerManager.ShowNotification(player, "Có lỗi khi nhận vật phẩm chế tạo");

                                    AddOK = false;
                                }
                                else
                                {
                                    var itemTemplate = ItemManager.GetItemTemplate(_SelectItem.ItemID);
                                    var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                                    var boxName = KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.VanillaIce);
                                    var rewardstr = KTGlobal.CreateStringByColor($"{_SelectItem.Number} {itemTemplate.Name}",
                                        ColorType.PeachYellow);
                                    if ((_SelectItem.ShouldNotify & 1) == 1) // thế giới, chữ chạy ngang
                                        KTGlobal.SendSystemEventNotification($"Chúc mừng {playerName} mở {boxName} nhận được {rewardstr}!");
                                    if ((_SelectItem.ShouldNotify >> 1 & 1) != 0) // gia tộc
                                        KTGlobal.SendFamilyChat(Global._TCPManager.MySocketListener,
                                            Global._TCPManager.TcpOutPacketPool, player.FamilyID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {rewardstr}!");
                                    if ((_SelectItem.ShouldNotify >> 2 & 1) != 0) // bang hội
                                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener,
                                            Global._TCPManager.TcpOutPacketPool, player.GuildID,
                                            $"Chúc mừng {playerName} mở {boxName} nhận được {rewardstr}!");

                                }

                                break;
                            }
                        case 6:
                            {
                                if (dryRun)
                                    continue;

                                /// Nếu không có vật phẩm
                                if (player.GoodsDataList == null)
                                {
                                    PlayerManager.ShowNotification(player, "Bạn chưa trang bị mật tịch.");
                                    AddOK = false;
                                }

                                /// Sách tương ứng
                                GoodsData book = null;
                                /// Thông tin sách tương ứng
                                lock (player.GoodsDataList)
                                {
                                    book = player.GoodsDataList
                                        .FirstOrDefault(x => x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_BOOK);
                                }

                                /// Nếu không có sách
                                if (book == null)
                                {
                                    PlayerManager.ShowNotification(player, "Bạn chưa trang bị mật tịch.");
                                    AddOK = false;
                                }

                                MonsterDeadHelper.BookProseccExp(player, _SelectItem.Number, false);
                                break;
                            }
                    }
                }

                if (dryRun)
                    return false;

                if (AddOK)
                {
                    SaveInfoReward(player, boxDbID, step);
                    if (findbox.LimitWeek > 0)
                    {
                        int TotalOpenInWeek = player.GetValueOfWeekRecore(itemGD.GoodsID);

                        player.SetValueOfWeekRecore(itemGD.GoodsID, TotalOpenInWeek + 1);
                        PlayerManager.ShowNotification(player,
                            $"Mở {KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.PeachYellow)} thành công. Trong tuần còn lại {KTGlobal.CreateStringByColor("" + (findbox.LimitWeek - 1 - (TotalOpenInWeek + 1)), ColorType.VanillaIce)} lượt mở.");
                    }

                    if (findbox.LimitDay > 0)
                    {
                        int TotalOpenDay = player.GetValueOfDailyRecore(itemGD.GoodsID);

                        player.SetValueOfDailyRecore(itemGD.GoodsID, TotalOpenDay + 1);
                        PlayerManager.ShowNotification(player,
                            $"Mở {KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.PeachYellow)} thành công. Trong ngày còn lại {KTGlobal.CreateStringByColor("" + (findbox.LimitDay - 1 - (TotalOpenDay + 1)), ColorType.VanillaIce)} lượt mở.");
                    }

                    //kiểm tra nếu không may trong lúc đang sở hữu 1 cái rương mở chưa xong thì mua hộp khác để mở tiếp
                    if (findbox.LimitLifeTime > 0 && step == 1)
                    {
                        var TotalOpen = player.GetValueOfForeverRecore(itemGD.GoodsID);

                        player.SetValueOfForeverRecore(itemGD.GoodsID, TotalOpen + 1);
                        PlayerManager.ShowNotification(player,
                            $"Sử dụng {KTGlobal.CreateStringByColor(findbox.BoxName, ColorType.PeachYellow)} thành công. Còn lại {KTGlobal.CreateStringByColor("" + (findbox.LimitLifeTime - 1 - (TotalOpen + 1)), ColorType.VanillaIce)} lượt sử dụng.");

                    }
                }

                return AddOK;

            }
            else 
            { 
                if (step == -1)
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm không khả dụng!");
                    return false;
                }

                if(step == -10)
                {
                    PlayerManager.ShowNotification(player, "Vật phẩm đã hết hạn sử dụng!");
                    ItemManager.RemoveItem(player, boxDbID);
                    return false;
                }
            }

            return false;
        }
    }

    public enum DataGet
    {
        TimeOpen,
        Step,
        NumberOpen
    }
}
