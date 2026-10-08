using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Activity.SeashellCircle;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Repute;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents;
using GameServer.KiemThe.GameEvents.KuaFuBalance;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.Logic.Manager.Shop.SaleTime;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;

namespace GameServer.KiemThe.Logic.Manager.Shop
{
    public static class ShopManager
    {
        private static List<ShopTab> _TotalShopTab = new List<ShopTab>();
        private static List<ShopItem> _TotalShopItem = new List<ShopItem>();

        /// <summary>
        /// Kỳ Trân Các
        /// </summary>
        public static TokenShop TokenShop { get; } = new TokenShop
        {
            Token = new List<ShopTab>(),
            BoundToken = new List<ShopTab>(),
            StoreProducts = new List<TokenShopStoreProduct>()
        };

        private const string ShopItem_XML = "Config/KT_Shop/ShopItem.xml";
        private const string ShopTab_XML = "Config/KT_Shop/ShopTab.xml";
        private const string TokenShopFileDir = "Config/KT_Shop/TokenShop.xml";
        private const string ShopSaleTimes_XML = "Config/KT_Shop/ShopSaleTime.xml";
        private const int KuaFuGuildWarOfficeRankMinLevel = 5;
        private const int KuaFuGuildWarOfficeRankMaxLevel = 8;

        private static Dictionary<int, Tuple<DateTime, DateTime>> ItemSaleTimes;
        private static Dictionary<int, Tuple<DateTime, DateTime>> TabSaleTimes;
        private static readonly HashSet<int> KuaFuGuildWarOfficeRankShopIds = new HashSet<int> { 1469, 1470, 1471, 1472, 1473 };
        private static readonly Dictionary<int, int> KuaFuGuildWarOfficeRankTokenByLevel = new Dictionary<int, int>
        {
            { 5, 29011 },
            { 6, 29010 },
            { 7, 29009 },
            { 8, 29008 },
        };

        private const int ReloadInterval = 300000; // 5 phút reload 1 lần

        public static void Init()
        {
            Setup();
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("SHOPRELOAD", Setup), ReloadInterval,
                ReloadInterval);
        }

        public static void Setup(object sender = null, EventArgs e = null)
        {
            TokenShop.Token.Clear();
            TokenShop.BoundToken.Clear();
            TokenShop.StoreProducts.Clear();

            LoadSaleTimes(ShopSaleTimes_XML);
            LoadShopitem(ShopItem_XML);
            LoadShopTab(ShopTab_XML);

            LoadTokenShop();

            // TODO: chuyển toàn bộ config máy chủ mới sang 1 file
            // override máy chủ mới
            if (ServerConfig.Instance.ElapsedDaySinceServerOpen > 14)
            {
                // nếu server đã mở được 10 ngày thì không hiện các shop gói ưu đãi
                var SaleTokenShopIds = new List<int> { 1463, 1464, 1466 };
                TokenShop.Token.RemoveAll(x => SaleTokenShopIds.Contains(x.ShopID));
                TokenShop.BoundToken.RemoveAll(x => SaleTokenShopIds.Contains(x.ShopID));
            }
            
            if (ServerConfig.Instance.ElapsedDaySinceServerOpen > 14)
            {
                // nếu server đã mở được 14 ngày thì không hiện các shop giảm giá huyền tinh
                var SaleTokenShopIds = new List<int> { 1450, 1451, 1452, 1453, 1454 };
                TokenShop.Token.RemoveAll(x => SaleTokenShopIds.Contains(x.ShopID));
                TokenShop.BoundToken.RemoveAll(x => SaleTokenShopIds.Contains(x.ShopID));
            }
            else
            {
                // nếu server chưa mở được 14 ngày thì bật các shop ưu đãi, ẩn các shop thường
                var NormalTokenShopIds = new List<int> { 211, 212, 213, 214, 215 };
                TokenShop.Token.RemoveAll(x => NormalTokenShopIds.Contains(x.ShopID));
                TokenShop.BoundToken.RemoveAll(x => NormalTokenShopIds.Contains(x.ShopID));
            }

            ComposeEventManager.Instance.AddTokenItem();
            FishingManager.AddBaitItemToShop();
            KTSeashellCircleManager.AddTicketItemToShop();
        }

        public static void ForceAddShopItem(ShopItem item)
        {
            _TotalShopItem.RemoveAll(x => x.ID == item.ID);
            _TotalShopItem.Add(item);
        }

        private static void LoadSaleTimes(string path)
        {
            ItemSaleTimes = new Dictionary<int, Tuple<DateTime, DateTime>>();
            TabSaleTimes = new Dictionary<int, Tuple<DateTime, DateTime>>();

            var file = Global.GameResPath(path);
            ShopTime shopSaleTime;
            using (var stream = File.OpenRead(file))
            {
                var serializer = new XmlSerializer(typeof(ShopTime));
                shopSaleTime = serializer.Deserialize(stream) as ShopTime;
            }

            if (shopSaleTime == null)
                return;

            foreach (var item in shopSaleTime.SaleTime)
            {
                if (!string.IsNullOrEmpty(item.ZoneConfig))
                {
                    // có cấu hình zone, cần kiểm tra
                    var listZone = item.ZoneConfig.Split(
                            new[] { '!', ',' },
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse);
                    if (item.ZoneConfig.StartsWith("!"))
                    {
                        // cấu hình loại trừ
                        if (listZone.Contains(GameManager.ServerLineID))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!listZone.Contains(GameManager.ServerLineID))
                        {
                            continue;
                        }
                    }
                }

                if (item.Type == 0)
                {
                    ItemSaleTimes[item.ItemID] = new Tuple<DateTime, DateTime>(item.StartDate, item.EndDate);
                }
                else
                {
                    TabSaleTimes[item.ItemID] = new Tuple<DateTime, DateTime>(item.StartDate, item.EndDate);
                }
            }
        }

        private static void LoadShopitem(string FilesPath)
        {
            var Files = Global.GameResPath(FilesPath);
            using (var stream = File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<ShopItem>));
                _TotalShopItem = serializer.Deserialize(stream) as List<ShopItem>;
            }

            _TotalShopItem?.RemoveAll(x =>
            {
                if (!ItemSaleTimes.TryGetValue(x.ID, out var saleTime))
                    return false;

                var now = DateTime.Now;
                return now < saleTime.Item1 || now > saleTime.Item2;
            });
        }

        private static void LoadShopTab(string FilesPath)
        {
            var Files = Global.GameResPath(FilesPath);
            using (var stream = File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<ShopTab>));
                _TotalShopTab = serializer.Deserialize(stream) as List<ShopTab>;
            }

            _TotalShopTab?.RemoveAll(x =>
            {
                if (!TabSaleTimes.TryGetValue(x.ShopID, out var saleTime))
                    return false;

                var now = DateTime.Now;
                return now < saleTime.Item1 || now > saleTime.Item2;
            });
        }

        /// <summary>
        /// Tải dữ liệu Kỳ Trân Các
        /// </summary>
        private static void LoadTokenShop()
        {
            var files = Global.GameResPath(TokenShopFileDir);
            var content = File.ReadAllText(files);
            var xmlNode = XElement.Parse(content);

            // Tiệm Đồng
            foreach (var tokenShopNode in xmlNode.Element("Token").Elements("Shop"))
            {
                var shopID = int.Parse(tokenShopNode.Attribute("ID").Value);

                var shopTab = _TotalShopTab.FirstOrDefault(x => x.ShopID == shopID);

                if (shopTab == null)
                {
                    LogManager.WriteLog(LogTypes.Error, $"Không tìm thấy thông tin Shop tương ứng, ID={shopID}");
                }
                else
                {
                    shopTab.Items = GetItemShopFromList(shopTab.ShopItem);
                    if (shopTab.ForceBinded == 1)
                    {
                        foreach (var item in shopTab.Items)
                        {
                            item.Bind = 1;
                        }
                    }

                    TokenShop.Token.Add(shopTab);
                }
            }

            // Tiệm Đồng khóa
            foreach (var tokenShopNode in xmlNode.Element("BoundToken").Elements("Shop"))
            {
                var shopID = int.Parse(tokenShopNode.Attribute("ID").Value);

                var shopTab = _TotalShopTab.FirstOrDefault(x => x.ShopID == shopID);
                if (shopTab == null)
                {
                    LogManager.WriteLog(LogTypes.Error, $"Không tìm thấy thông tin Shop tương ứng, ID={shopID}");
                }
                else
                {
                    shopTab.Items = GetItemShopFromList(shopTab.ShopItem);
                    TokenShop.BoundToken.Add(shopTab);
                }
            }

            // Gói hàng bán trên Store
            foreach (var node in xmlNode.Element("StoreProduct").Elements("Product"))
            {
                var storeProduct = new TokenShopStoreProduct()
                {
                    ID =   node.Attribute("ID").Value,
                    Name = node.Attribute("Name").Value,
                    Hint = node.Attribute("Hint").Value,
                    Icon = node.Attribute("Icon").Value,
                    Recommend = bool.Parse(node.Attribute("Recommend").Value),
                    Price = int.Parse(node.Attribute("Price").Value),
                    Token = int.Parse(node.Attribute("Token").Value),
                    FirstBonus = int.Parse(node.Attribute("FirstBonus").Value),
                };
                ShopManager.TokenShop.StoreProducts.Add(storeProduct);
            }
        }

        public static TokenShopStoreProduct GetProductByID(string ID)
        {
            return TokenShop.StoreProducts.FirstOrDefault(x => x.ID == ID);
        }

        private static List<ShopItem> GetItemShopFromList(List<int> InputData, int filterSeriesID = -1,
            int filterSexID = -1)
        {
            var _Items = new List<ShopItem>();

            foreach (var ID in InputData)
            {
                var findItem = _TotalShopItem.FirstOrDefault(x => x.ID == ID);
                if (findItem != null)
                {
                    if (filterSeriesID == -1 && filterSexID == -1)
                    {
                        // nếu không có yêu cầu lọc giới tính + ngũ hành thì add luôn
                        _Items.Add(findItem);
                        continue;
                    }

                    var goodData = ItemManager.GetItemTemplate(findItem.ItemID);
                    if (!ItemManager.KD_ISEQUIP(goodData.Genre))
                    {
                        // nếu không phải trang bị thì không lọc, add luôn
                        _Items.Add(findItem);
                        continue;
                    }

                    var isValidSeries = filterSeriesID == -1 ||
                                        goodData.Series == filterSeriesID;

                    var isValidSex = false;
                    if (filterSexID == -1)
                    {
                        isValidSex = true;
                    }
                    else
                    {
                        var reqSex = goodData.ListReqProp.FirstOrDefault(x =>
                            x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_SEX);
                        if (reqSex == null)
                        {
                            isValidSex = true;
                        }
                        else
                        {
                            isValidSex = reqSex.ReqPropValue == filterSexID;
                        }
                    }


                    if (isValidSeries && isValidSex)
                        _Items.Add(findItem);
                }
            }

            return _Items;
        }

        public static ShopTab GetShopTable(int ShopID, KPlayer player)
        {
            var _Shop = _TotalShopTab.FirstOrDefault(x => x.ShopID == ShopID);

            if (_Shop == null)
                return null;

            var seriesFilter = -1;
            if (_Shop.SeriesFilter)
            {
                seriesFilter = (int)player.m_Series;
            }

            var sexFilter = -1;
            if (_Shop.SexFilter)
            {
                sexFilter = player.RoleSex;
            }

            _Shop.Items = GetItemShopFromList(_Shop.ShopItem, seriesFilter, sexFilter);
            _Shop.TotalSellItem = player.GetGoodAreadySellBefore();

            return _Shop;
        }


        public static int GetGuildMoneyNeed(int OfficeRank)
        {
            switch (OfficeRank)
            {
                case 1:
                    return 100000;
                case 2:
                    return 200000;
                case 3:
                    return 400000;
                case 4:
                    return 600000;
                case 5:
                    return 800000;
                default:
                    return -1;
            }
        }

        private static bool TryGetKuaFuGuildWarOfficeRankTokens(int shopId, ItemData itemTemplate, out List<int> tokenIds)
        {
            tokenIds = null;
            if (!KuaFuGuildWarOfficeRankShopIds.Contains(shopId) || itemTemplate == null)
            {
                return false;
            }

            if (itemTemplate.Level < KuaFuGuildWarOfficeRankMinLevel || itemTemplate.Level > KuaFuGuildWarOfficeRankMaxLevel)
            {
                return false;
            }

            tokenIds = KuaFuGuildWarOfficeRankTokenByLevel
                .Where(x => x.Key >= itemTemplate.Level)
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToList();

            return tokenIds.Count > 0;
        }

        private static int CountKuaFuGuildWarOfficeRankTokens(KPlayer player, List<int> tokenIds)
        {
            if (player == null || tokenIds == null)
            {
                return 0;
            }

            return tokenIds.Sum(tokenId => ItemManager.GetItemCountInBag(player, tokenId));
        }

        private static string GetItemName(int itemId)
        {
            var itemTemplate = ItemManager.GetItemTemplate(itemId);
            return itemTemplate != null ? itemTemplate.Name : $"ItemID {itemId}";
        }

        private static bool ValidateKuaFuGuildWarOfficeRankCost(KPlayer player, int shopId, ShopItem shopItem, ItemData itemTemplate, int buyNumber, int goodsHave, int goodsNeed)
        {
            if (!TryGetKuaFuGuildWarOfficeRankTokens(shopId, itemTemplate, out var tokenIds))
            {
                return true;
            }

            var tokenNeed = buyNumber;
            var tokenHave = CountKuaFuGuildWarOfficeRankTokens(player, tokenIds);
            if (tokenHave >= tokenNeed && goodsHave >= goodsNeed)
            {
                return true;
            }

            var requiredTokenId = KuaFuGuildWarOfficeRankTokenByLevel[itemTemplate.Level];
            var tokenName = GetItemName(requiredTokenId);
            var goodsName = GetItemName(shopItem.GoodsIndex);
            var tokenDesc = itemTemplate.Level < KuaFuGuildWarOfficeRankMaxLevel ? $"{tokenName} hoặc lệnh bài cấp cao hơn" : tokenName;

            PlayerManager.ShowMessageBox(player, "Thông báo",
                $"Không đủ nguyên liệu mua {itemTemplate.Name}.\nYêu cầu:\n- <color=#f9e2af>{tokenDesc}</color>: <color=#f38ba8>{tokenHave}/{tokenNeed}</color>\n- <color=#f9e2af>{goodsName}</color>: <color=#f38ba8>{goodsHave}/{goodsNeed}</color>");
            return false;
        }

        private static bool RemoveKuaFuGuildWarOfficeRankToken(KPlayer player, int shopId, ItemData itemTemplate, int buyNumber)
        {
            if (!TryGetKuaFuGuildWarOfficeRankTokens(shopId, itemTemplate, out var tokenIds))
            {
                return true;
            }

            var remaining = buyNumber;
            foreach (var tokenId in tokenIds)
            {
                var countInBag = ItemManager.GetItemCountInBag(player, tokenId);
                var removeCount = Math.Min(countInBag, remaining);
                if (removeCount <= 0)
                {
                    continue;
                }

                if (!ItemManager.RemoveItemFromBag(player, tokenId, removeCount, -1, "BUYKUAFUGUILDWAROFFICERANK"))
                {
                    return false;
                }

                remaining -= removeCount;
                if (remaining <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Xử lý sự kiện mua của người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ShopID"></param>
        /// <param name="ShopItemID"></param>
        /// <param name="Number"></param>
        /// <param name="couponID"></param>
        /// <returns></returns>
        public static SubRep BuyItem(KPlayer player, int ShopID, int ShopItemID, int Number, int couponID)
        {
            var _SubRep = new SubRep
            {
                IsOK = false,
                IsBuyBack = false,
                CountLess = 0
            };

            // Nếu đang ở LSV Cân Bằng, items mua sẽ vào túi riêng balance
            int shopSite = 0;
            if (GameManager.IsKuaFuServer && KuaFuBalanceManager.Instance.IsBalanceMap(player))
            {
                shopSite = (int)SiteItemType.kuafubalancebag;
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Shop buy routing to site=4. RoleID={0}, MapCode={1}",
                    player.RoleID, player.CurrentMapCode));
            }

            try
            {
                // TODO xử lý phiếu giảm giá Kỳ Trân Các

                var ShopFind = _TotalShopTab.FirstOrDefault(x => x.ShopID == ShopID);

                var NeedWriterLogs = false;

                if (ShopFind != null)
                {
                    if (!ShopFind.ShopItem.Contains(ShopItemID))
                    {
                        PlayerManager.ShowNotification(player, "Vật phẩm muốn mua không tồn tại");
                        return _SubRep;
                    }

                    if (ShopFind.LimitType == ShopLimitType.SalePerWeek)
                    {
                        var buyPerWeek =
                            player.GetValueOfWeekRecore((int)RecoreDef.ShopBuyCountBegin + ShopFind.ShopID);
                        if (buyPerWeek < 0)
                        {
                            buyPerWeek = 0;
                        }

                        var limitPerWeek = ShopFind.LimitValue;
                        if (buyPerWeek + Number > limitPerWeek)
                        {
                            PlayerManager.ShowMessageBox(player, "Thông báo",
                                $"Bằng hữu không thể mua vượt quá số lượng cho phép. Tuần này đã mua {buyPerWeek}/{limitPerWeek} vật phẩm trong tiệm này.");
                            return _SubRep;
                        }
                    }

                    var _item = _TotalShopItem.FirstOrDefault(x => x.ID == ShopItemID);

                    if (_item != null)
                    {
                        var Money = (MoneyType)ShopFind.MoneyType;

                        var FindTempate = ItemManager.GetItemTemplate(_item.ItemID);

                        if (FindTempate == null)
                        {
                            PlayerManager.ShowNotification(player, "Vật phẩm muốn mua không tồn tại");
                            return _SubRep;
                        }

                        var RATE = ShopFind.Discount;

                        var TotalMoneyNeed = 0;

                        var ItemDiscount = 0;

                        var ItUsingTicket = false;

                        //Nếu là shop đồng hoặc đông khóa
                        if (Money == MoneyType.Dong || Money == MoneyType.DongKhoa)
                        {
                            if (couponID != -1)
                            {
                                var FindGoldById = Global.GetGoodsByDbID(player, couponID);
                                if (FindGoldById != null)
                                {
                                    var TMP = ItemManager.GetItemTemplate(FindGoldById.GoodsID);
                                    if (TMP != null)
                                    {
                                        if (TMP.Genre == 18 && TMP.DetailType == 1 && TMP.ParticularType >= 1550 &&
                                            TMP.ParticularType <= 1554)
                                        {
                                            ItUsingTicket = true;
                                            ItemDiscount = TMP.ItemValue;
                                        }
                                    }
                                }
                            }

                            // Chỉ ghi logs với shop đồng hoặc đồng khóa
                            NeedWriterLogs = true;
                            TotalMoneyNeed = _item.GoodsPrice * Number;
                        }
                        else
                        {
                            NeedWriterLogs = ShopFind.EnableLog;
                            TotalMoneyNeed = FindTempate.Price * Number;
                        }

                        // Nếu là Shop giảm giá
                        if (RATE > 0)
                        {
                            var Discount = (RATE * TotalMoneyNeed) / 100;
                            TotalMoneyNeed = TotalMoneyNeed - Discount;
                        }

                        if (ItemDiscount > 0)
                        {
                            var Discount = (ItemDiscount * TotalMoneyNeed) / 100;

                            TotalMoneyNeed = TotalMoneyNeed - Discount;
                        }

                        var TotalBuyNumber = Number;

                        var TotalSpaceNeed = ItemManager.TotalSpaceNeed(FindTempate.ItemID, TotalBuyNumber * _item.Count);

                        if (FindTempate.IsPotionMedicine)
                        {
                            var inBag = ItemManager.GetPotionMedicineCountInBag(player);
                            var maxPotion = KuaFuBalanceManager.Instance.GetPotionMedicineCapacity(player);

                            if (inBag + TotalBuyNumber * _item.Count > maxPotion)
                            {
                                var medicineStatus =
                                    KTGlobal.CreateStringByColor($"{inBag}/{maxPotion}", ColorType.VanillaIce);
                                var medicineLeft = KTGlobal.CreateStringByColor($"{Math.Max(maxPotion - inBag, 0)}",
                                    ColorType.VanillaIce);
                                PlayerManager.ShowNotification(player,
                                    $"Số lượng bình thuốc đang mang theo: {medicineStatus}.\nBạn chỉ được mua thêm {medicineLeft} bình thuốc.");
                                return _SubRep;
                            }
                        }

                        // Check xem nếu không đủ chỗ trống để mua
                        if (!KTGlobal.IsHaveSpace(TotalSpaceNeed, player))
                        {
                            PlayerManager.ShowNotification(player,
                                "Túi đồ không đủ chỗ trống! Cần " + TotalSpaceNeed +
                                " vị trí trống để có thể mua vật phẩm này!");

                            return _SubRep;
                        }

                        var TempCount = 0;

                        if (_item.GoodsIndex == -1 && !KTGlobal.IsHaveMoney(player, TotalMoneyNeed, Money) &&
                            _item.GoodsIndex != -1)
                        {
                            PlayerManager.ShowNotification(player, "Tiền yêu cầu trên người không đủ!");
                            return _SubRep;
                        }

                        if (_item.ReputeDBID != -1)
                        {
                            var _Info = player.GetRepute().FirstOrDefault(x => x.DBID == _item.ReputeDBID);
                            if (_Info.Level < _item.ReputeLevel)
                            {
                                var reputeName = ReputeManager.GetReputeCampAndClass(_item.ReputeDBID);
                                PlayerManager.ShowNotification(player,
                                    $"Danh vọng không đủ.\nYêu cầu danh vọng {reputeName} cấp {_item.ReputeLevel}");
                                return _SubRep;
                            }
                        }

                        //TODO
                        if (_item.OfficialLevel != -1)
                        {
                            if (player.OfficeRank < _item.OfficialLevel)
                            {
                                PlayerManager.ShowNotification(player, "Danh vọng lãnh thổ không đủ không thể mua!");
                                return _SubRep;
                            }

                            //int GuildMoneyNeed = GetGuildMoneyNeed(_item.OfficialLevel);
                            var GuildMoneyNeed = _item.TongFund;

                            if (!KTGlobal.IsHaveMoney(player, GuildMoneyNeed, MoneyType.GuildMoney))
                            {
                                PlayerManager.ShowNotification(player,
                                    $"Tài sản cá nhân bang hội không đủ! Cần có {GuildMoneyNeed / 10000} vạn tài sản cá nhân!");
                                return _SubRep;
                            }
                        }

                        if (_item.Honor != -1)
                        {
                            var Rank = KTGlobal.GetRankHonor(player.GetTotalValue());
                            if (Rank < _item.Honor)
                            {
                                PlayerManager.ShowNotification(player, "Vinh dự tài phú không đủ");
                                return _SubRep;
                            }
                        }

                        //YÊU CẦU TIỀN CỐNG HIẾN BANG HỘI
                        if (_item.TongFund != -1)
                        {
                            var Request = _item.TongFund * Number;
                            if (player.RoleGuildMoney < Request)
                            {
                                PlayerManager.ShowNotification(player, "Tiền bang hội không đủ");
                            }
                        }

                        if (_item.GoodsIndex != -1)
                        {
                            var NumberRequest = _item.GoodsPrice * Number;
                            var goodsBinding = -1;
                            if (CauCaMuaThuManager.IsShopEvent(ShopID))
                            {
                                // shop event câu cá, yêu cầu vật phẩm giá có thuộc tính khóa giống với vật phẩm mua
                                goodsBinding = _item.Bind;
                            }

                            var CountInBag = ItemManager.GetItemCountInBag(player, _item.GoodsIndex, goodsBinding);
                            var crystalLevel = KTGlobal.IsCrystal(_item.GoodsIndex);
                            if (crystalLevel != -1)
                            {
                                CountInBag = KTGlobal.CountCrystalInBagByLevel(player, crystalLevel);
                            }

                            if (!ValidateKuaFuGuildWarOfficeRankCost(player, ShopID, _item, FindTempate, TotalBuyNumber, CountInBag, NumberRequest))
                            {
                                return _SubRep;
                            }

                            if (CountInBag < NumberRequest)
                            {
                                var alreadyHave = CountInBag;
                                var goodsName = ItemManager.GetItemTemplate(_item.GoodsIndex).Name;
                                var bindStr = goodsBinding == 1 ? "(khóa)" : "";
                                PlayerManager.ShowMessageBox(player, "Thông báo",
                                    $"Vật phẩm đạo cụ không đủ.\nHiện có <color=#f38ba8>{alreadyHave}/{NumberRequest}</color> {goodsName} {bindStr}");
                                return _SubRep;
                            }
                        }

                        var _ItemLimit = _item.LimitType;

                        if (_ItemLimit == LimitType.BuyCountPerDay)
                        {
                            var SendToDB = $"{player.RoleID}:{ShopItemID}";

                            var DataRead = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_QUERYLIMITGOODSUSEDNUM,
                                SendToDB, player.ServerId);

                            if (DataRead == null)
                            {
                                PlayerManager.ShowNotification(player, "Có lỗi khi kiểm tra nhật ký");
                                return _SubRep;
                            }

                            var Ret = int.Parse(DataRead[1]);

                            if (Ret == -1)
                            {
                                PlayerManager.ShowNotification(player, "Có lỗi khi kiểm tra nhật ký");
                                return _SubRep;
                            }

                            var dayID = TimeUtil.NowDateTime().DayOfYear;
                            if (dayID == int.Parse(DataRead[2]))
                            {
                                var TotalBuyInDay = int.Parse(DataRead[3]);

                                TempCount = TotalBuyInDay;

                                if (TotalBuyInDay + TotalBuyNumber > _item.LimitValue)
                                {
                                    PlayerManager.ShowNotification(player,
                                        $"Hôm nay bạn đã mua {TotalBuyInDay} vật phẩm. Còn được mua {Math.Max(_item.LimitValue - TotalBuyInDay, 0)} vật phẩm.");

                                    return _SubRep;
                                }
                            }
                        }

                        //TUẦN TODO
                        if (_ItemLimit == LimitType.BuyCountPerWeek)
                        {
                            var TotalBuyInWeek = player.GetValueOfWeekRecore(ShopItemID);

                            if (TotalBuyInWeek == -1)
                            {
                                TotalBuyInWeek = 0;
                            }

                            if (TotalBuyInWeek + TotalBuyNumber > _item.LimitValue)
                            {
                                PlayerManager.ShowNotification(player,
                                    $"Tuần này bạn đã mua {TotalBuyInWeek} vật phẩm. Còn được mua {Math.Max(_item.LimitValue - TotalBuyInWeek, 0)} vật phẩm.");

                                return _SubRep;
                            }
                        }

                        //THÁNG TODO
                        if (_ItemLimit == LimitType.BuycountPerMonth)
                        {
                            var TotalBuyInMonth = player.GetValueOfMonthRecore(ShopItemID);

                            if (TotalBuyInMonth == -1)
                            {
                                TotalBuyInMonth = 0;
                            }

                            if (TotalBuyInMonth + TotalBuyNumber > _item.LimitValue)
                            {
                                PlayerManager.ShowNotification(player,
                                    $"Tháng này bạn đã mua {TotalBuyInMonth} vật phẩm. Còn được mua {Math.Max(_item.LimitValue - TotalBuyInMonth, 0)} vật phẩm.");

                                return _SubRep;
                            }
                        }

                        //LIMIT CẢ ĐỜI
                        if (_ItemLimit == LimitType.LimitCount)
                        {
                            var TotalBuy = player.GetValueOfForeverRecore(ShopItemID);

                            if (TotalBuy == -1)
                            {
                                TotalBuy = 0;
                            }

                            if (TotalBuy + TotalBuyNumber > _item.LimitValue)
                            {
                                PlayerManager.ShowNotification(player,
                                    $"Bạn đã mua {TotalBuy} vật phẩm. Còn được mua {Math.Max(_item.LimitValue - TotalBuy, 0)} vật phẩm.");

                                return _SubRep;
                            }
                        }


                        if (_item.OfficialLevel != -1)
                        {
                            //int GuildMoneyNeed = GetGuildMoneyNeed(_item.OfficialLevel);
                            var GuildMoneyNeed = _item.TongFund;

                            if (!KTGlobal.SubMoney(player, GuildMoneyNeed, MoneyType.GuildMoney, "SHOPBUYITEM").IsOK)
                            {
                                PlayerManager.ShowNotification(player,
                                    $"Tài sản cá nhân bang hội không đủ! Cần có {GuildMoneyNeed / 10000} vạn tài sản cá nhân!");
                                //PlayerManager.ShowNotification(player, "Tài sản cá nhân bang hội không đủ!");

                                return _SubRep;
                            }
                        }

                        // Nếu vật phẩm yêu cầu vật phẩm thì thực hiện trừ vậ tphaamr
                        if (_item.GoodsIndex != -1)
                        {
                            var NumberRequest = _item.GoodsPrice * Number;
                            var goodsBinding = -1;

                            if (CauCaMuaThuManager.IsShopEvent(ShopID))
                            {
                                // shop event câu cá, yêu cầu vật phẩm giá có thuộc tính khóa giống với vật phẩm mua
                                goodsBinding = _item.Bind;
                            }

                            var crystalLevel = KTGlobal.IsCrystal(_item.GoodsIndex);
                            var subResult = false;
                            if (!RemoveKuaFuGuildWarOfficeRankToken(player, ShopID, FindTempate, TotalBuyNumber))
                            {
                                PlayerManager.ShowMessageBox(player, "Thông báo", "Lệnh bài quan ấn không đủ hoặc đã thay đổi, vui lòng kiểm tra lại túi đồ.");
                                return _SubRep;
                            }

                            if (crystalLevel >= 1)
                            {
                                subResult = KTGlobal.RemoveCrystalByLevel(player, crystalLevel, NumberRequest);
                            }
                            else
                            {
                                subResult = ItemManager.RemoveItemFromBag(player, _item.GoodsIndex, NumberRequest, goodsBinding,
                                    "BUYSHOPITEM");
                            }

                            if (!subResult)
                            {
                                var alreadyHave = ItemManager.GetItemCountInBag(player, _item.GoodsIndex, goodsBinding);
                                if (crystalLevel >= 1)
                                {
                                    alreadyHave = KTGlobal.CountCrystalInBagByLevel(player, crystalLevel);
                                }

                                var goodsName = ItemManager.GetItemTemplate(_item.GoodsIndex).Name;
                                var bindStr = goodsBinding == 1 ? "(khóa)" : "";
                                PlayerManager.ShowMessageBox(player, "Thông báo",
                                    $"Vật phẩm đạo cụ không đủ.\nHiện có <color=#f38ba8>{alreadyHave}/{NumberRequest}</color> {goodsName} {bindStr}");
                                return _SubRep;
                            }
                        }
                        else if (_item.TongFund != -1)
                        {
                            // nếu vật phẩm yêu cầu tiền bang hội thì thực hiện trừ tiền bang hội
                            var Request = _item.TongFund * Number;

                            _SubRep = KTGlobal.SubMoney(player, Request, MoneyType.GuildMoney, "SHOPBUYITEM");
                            // nếu trừ tiền thành công thực hiện ADD ITEM
                            if (!_SubRep.IsOK)
                            {
                                PlayerManager.ShowNotification(player, "Tích lũy cá nhân không thể sử dụng thêm");
                                return _SubRep;
                            }
                        }
                        else // Nếu vật phẩm yêu cầu tiền mặt thì sử dụng tiền mặt để mua
                        {
                            _SubRep = KTGlobal.SubMoney(player, TotalMoneyNeed, Money, "SHOPBUYITEM");
                            // nếu trừ tiền thành công thực hiện ADD ITEM
                            if (!_SubRep.IsOK)
                            {
                                PlayerManager.ShowNotification(player, "Bạc mang theo không đủ");
                                return _SubRep;
                            }

                            if (ItUsingTicket)
                            {
                                var FindGoldById = Global.GetGoodsByDbID(player, couponID);

                                ItemManager.RemoveItemByCount(player, FindGoldById, 1, "SHOPTICKETDISCOUNT");

                                LogManager.WriteLog(LogTypes.BuyNpc,
                                    "[" + player.RoleID + "][" + player.RoleName + "] Using Discount :" +
                                    FindGoldById.GoodsID);
                            }
                        }

                        var binding = 1;
                        if (Money == MoneyType.Dong)
                        {
                            binding = 0;
                            if (UnionShop.IsShopEvent(ShopID))
                            {
                                // shop sự kiện tháng 4/2024, bán bằng KNB nhận về vật phẩm khóa
                                binding = 1;
                            }
                            // tạm thời chưa mở do UI phía client chưa đáp ứng
                            /*
                                if (ShopFind.ForceBinded == 1)
                                {
                                    binding = 1;
                                }
                                */
                            if (_item.ID == 6533)
                            {
                                binding = 1;
                            }
                        }

                        var TimeUsing = Global.ConstGoodsEndTime;
                        if (_item.Expiry != -1)
                        {
                            var dt = DateTime.Now.AddMinutes(_item.Expiry);

                            // "1900-01-01 12:00:00";
                            TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }

                        // Tọa vật phẩm đã mua
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, _item.ItemID,
                                TotalBuyNumber * _item.Count, shopSite, "SHOPGETITEM", true, binding, false, TimeUsing, "", _item.Series, "",
                                0, 1, NeedWriterLogs))
                        {
                            PlayerManager.ShowNotification(player,
                                "Có lỗi khi thực hiện thêm vật phẩm vào túi đồ vui lòng liên hệ ADM để được giúp đỡ!");

                            LogManager.WriteLog(LogTypes.BuyNpc,
                                "Có lỗi khi thực hiện add vật phẩm :" + player.RoleID + "|" + _item.ItemID + " x " +
                                TotalBuyNumber);
                        }
                        else
                        {
                            if (NeedWriterLogs)
                            {
                                GameManager.logDBCmdMgr.WriterLogs(player.RoleID, player.strUserID, "SHOP", "BUYITEM",
                                    "", "", "ADD", player.ZoneID, _item.ItemID.ToString(), TotalBuyNumber.ToString(),
                                    ShopID.ToString(), "", player.ServerId);
                            }

                            if (Money == MoneyType.Dong)
                            {
                                LogManager.WriteLog(LogTypes.BuyNpc,
                                    "[SHOPKNB][" + player.RoleID + "][" + player.RoleName +
                                    "]Mua vật phẩm thành công :" + _item.ItemID + " x " + TotalBuyNumber +
                                    "==> tiêu tốn " + TotalMoneyNeed);
                                // nếu là thẻ đổi server thì bắn noti về lark
                                if (_item.ItemID == 25317)
                                {
                                    if (GameManager.IsLiveServer)
                                        Tools.LarkNotificationHelper.PushNormalNotification("[" + GameManager.ServerLineID + "] Player " + player.RoleName + " [" + player.RoleID + "] buy [Thẻ đăng ký đổi server] x number = " + TotalBuyNumber);
                                }    
                            }

                            //TODO : Ghi lại nhật ký tích nạp tích tiêu
                            PlayerManager.ShowNotification(player, "Mua vật phẩm thành công!");
                            // TODO: hiện popup hướng dẫn người chơi nếu mua thịt xào măng theo quest.
                            if (_item.ItemID == 128 && player.MainTaskID == 113)
                            {
                                // PlayerManager.ShowMessageBox(player, "Hướng dẫn", "Thức ăn có tác dụng hồi phục HP/nội lực theo thời gian. Thức ăn được mua tại Tửu lầu. Sau khi mua thức ăn có thể bật chế độ tự động ăn trong mục Cài đặt tự động");
                            }

                            if (UnionShop.IsShopEvent(ShopID))
                            {
                                UnionShop.UpdateTotalSell(player, _item.ItemID, TotalBuyNumber);
                            }
                        }

                        // nếu tiệm này có limit tổng số lượng bán thì ghi lại
                        if (ShopFind.LimitType == ShopLimitType.SalePerWeek)
                        {
                            var buyPerWeek =
                                player.GetValueOfWeekRecore((int)RecoreDef.ShopBuyCountBegin + ShopFind.ShopID);
                            if (buyPerWeek < 0)
                            {
                                buyPerWeek = 0;
                            }

                            buyPerWeek += TotalBuyNumber;
                            player.SetValueOfWeekRecore((int)RecoreDef.ShopBuyCountBegin + ShopFind.ShopID, buyPerWeek);
                        }

                        // Đánh dấu vào DB đã mua vật phẩm này
                        if (_ItemLimit == LimitType.BuyCountPerDay)
                        {
                            var dayID = TimeUtil.NowDateTime().DayOfYear;
                            var Count = TempCount + TotalBuyNumber;
                            var GooldID = ShopItemID;
                            var RoleID = player.RoleID;

                            var SendToDB = $"{RoleID}:{GooldID}:{dayID}:{Count}";

                            Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_UPDATELIMITGOODSUSEDNUM, SendToDB,
                                player.ServerId);
                        }

                        // Ghi lại tích lũy mua tuần
                        if (_ItemLimit == LimitType.BuyCountPerWeek)
                        {
                            var TotalBuyInWeek = player.GetValueOfWeekRecore(ShopItemID);

                            if (TotalBuyInWeek == -1)
                            {
                                TotalBuyInWeek = 0;
                            }

                            player.SetValueOfWeekRecore(ShopItemID, TotalBuyInWeek + TotalBuyNumber);
                        }

                        // Ghi lại tích lũy mua tuần
                        if (_ItemLimit == LimitType.BuycountPerMonth)
                        {
                            var TotalBuyInWeek = player.GetValueOfMonthRecore(ShopItemID);

                            if (TotalBuyInWeek == -1)
                            {
                                TotalBuyInWeek = 0;
                            }

                            player.SetValueOfMonthRecore(ShopItemID, TotalBuyInWeek + TotalBuyNumber);
                        }

                        if (_ItemLimit == LimitType.LimitCount)
                        {
                            var TotalBuy = player.GetValueOfForeverRecore(ShopItemID);

                            if (TotalBuy == -1)
                            {
                                TotalBuy = 0;
                            }

                            player.SetValueOfForeverRecore(ShopItemID, TotalBuy + TotalBuyNumber);
                        }

                        //LogManager.WriteLog(LogTypes.Shop, $"{player.RoleName} [ID: {player.RoleID}] đã mua {_item.ItemID} [ShopItemID: {_item.ID}], số lượng {TotalBuyNumber} tại ShopID {ShopID}");
                    }
                    else
                    {
                        PlayerManager.ShowNotification(player, "Vật phẩm cần tìm không tồn tại");
                    }
                }
                else
                {
                    if (ShopID == 9999)
                    {
                        var findOldItem = player.GetGoodAreadySellBefore().ToList()
                            .FirstOrDefault(x => x.Id == ShopItemID);
                        // nếu tìm thấy vật phẩm đã bán trước đó
                        if (findOldItem != null)
                        {
                            if (!KTGlobal.IsHaveSpace(1, player))
                            {
                                PlayerManager.ShowNotification(player,
                                    "Túi đồ không đủ chỗ trống! Cần " + 1 +
                                    " vị trí trống để có thể mua vật phẩm này!");

                                return _SubRep;
                            }

                            // Giá mua lại sẽ = giá đã bán ra
                            var price = ItemManager.GetItemTemplate(findOldItem.GoodsID).Price;

                            // Nếu đồ khóa thì mua lại sẽ cần bạc khóa, đồ không khóa mua lại sẽ cần bạc thường
                            var Type = findOldItem.Binding == 1 ? MoneyType.BacKhoa : MoneyType.Bac;

                            if (!KTGlobal.IsHaveMoney(player, price, Type))
                            {
                                PlayerManager.ShowNotification(player, "Tiền yêu cầu trên người không đủ!");
                            }
                            else
                            {
                                _SubRep = KTGlobal.SubMoney(player, price, Type, "BUYBACK");

                                _SubRep.IsBuyBack = true;
                                // nếu trừ tiền thành công thực hiện ADD ITEM
                                if (_SubRep.IsOK)
                                {
                                    var itemData = ItemManager.TotalItem.Where(x => x.ItemID == findOldItem.GoodsID).FirstOrDefault();
                                    //nếu là chân nguyên anh hùng
                                    if (itemData.Level == 2 && ItemManager.KD_ISCHANNGUYEN(itemData.DetailType))
                                    {
                                        var paramCN = "";
                                        if (findOldItem.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                                        {
                                            paramCN = param3;
                                        }

                                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                                                findOldItem.GoodsID, 1, shopSite, "BUYBACK", true, findOldItem.Binding, false,
                                                Global.ConstGoodsEndTime, findOldItem.Props, findOldItem.Series, "", 0, 1, false, paramCN))
                                        {
                                            PlayerManager.ShowNotification(player,
                                                "Có lỗi khi mua lại vui lòng liên hệ với ADMIN để được giúp đỡ!");

                                            LogManager.WriteLog(LogTypes.BuyNpc,
                                                "Có lỗi khi update lại số lượng :" + player.RoleID + "|" + findOldItem.Id +
                                                " x " + 1);
                                        }
                                        else
                                        {
                                            player.RemoveItemSell(findOldItem);
                                            //TODO : Ghi lại nhật ký tích nạp tích tiêu
                                            PlayerManager.ShowNotification(player, "Mua vật phẩm thành công!");
                                        }
                                    }
                                    else
                                    {
                                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                                                findOldItem.GoodsID, 1, shopSite, "BUYBACK", true, findOldItem.Binding, false,
                                                Global.ConstGoodsEndTime, findOldItem.Props, findOldItem.Series))
                                        {
                                            PlayerManager.ShowNotification(player,
                                                "Có lỗi khi mua lại vui lòng liên hệ với ADMIN để được giúp đỡ!");

                                            LogManager.WriteLog(LogTypes.BuyNpc,
                                                "Có lỗi khi update lại số lượng :" + player.RoleID + "|" + findOldItem.Id +
                                                " x " + 1);
                                        }
                                        else
                                        {
                                            player.RemoveItemSell(findOldItem);
                                            //TODO : Ghi lại nhật ký tích nạp tích tiêu
                                            PlayerManager.ShowNotification(player, "Mua vật phẩm thành công!");
                                        }
                                    }
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, "Trừ tiền không thành công!");
                                }
                            }
                        }
                        else
                        {
                            PlayerManager.ShowNotification(player, "Vật phẩm không còn tồn tại!");
                        }
                    }

                    // PlayerManager.ShowNotification(player, "Không tìm thấy Cửa hàng tương ứng");
                }
            }
            catch (Exception e)
            {
                LogManager.WriteLog(LogTypes.Exception,
                    $"Lỗi vật phẩm {ShopID}_{ShopItemID}_{Number}_{couponID}. {e.Message}\n{e.StackTrace}");
                PlayerManager.ShowNotification(player, "Có lỗi khi mua vật phẩm này");
                return _SubRep;
            }

            return _SubRep;
        }
    }
}
