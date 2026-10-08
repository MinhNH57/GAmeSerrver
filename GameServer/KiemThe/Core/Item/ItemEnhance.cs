using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Server;
using GameServer.Core.GameEvent;


using GameServer.Core.GameEvent.EventOjectImpl;


namespace GameServer.KiemThe.Core.Item
{
    /// <summary>
    /// Class cường hóa vật phẩm
    /// </summary>
    public static partial class ItemEnhance
    {
        public static int MIN_COMMON_EQUIP = 1;
        public static int MAX_COMMON_EQUIP = 11;

        public static double PEEL_RESTORE_RATE_12 = 1.0 * 3 / 100;
        public static double PEEL_RESTORE_RATE_14 = 1.0 * 5 / 100;
        public static double PEEL_RESTORE_RATE_17_18 = 1.0 * 5.5 / 100;
        public static double PEEL_RESTORE_RATE_19_20 = 1.0 * 7 / 100;
        public static double ENHANCE_COST_RATE = 1.0 * 10 / 100;

        public static int MAX_SPLITNUM = 8;

        public static double DUR_COST_PER_YEAR = (2 / 5) * 3600 * 6 * 365 / 20;
        public static double EQUIP_TOTAL_RATE = 4 * 1 + 1.5 * 4 + 1 * 5;
        public static double ALL_EQUIP_MIN_VALUE = 5000000;
        public static double ALL_EQUIP_MAX_VALUE = 500000000;
        public static double VALUEPERCEN_PER_YEAR = 0.3;

        #region CUONGHOA

        /// <summary>
        /// Tính toán cấp cường hóa tối đa của trang bị
        /// </summary>
        /// <param name="itemData"></param>
        /// <returns></returns>
        public static int CalcMaxEnhanceTimes(ItemData itemData)
        {
            int nMax;
            var nLevel = itemData.Level;

            if (nLevel <= 3)
            {
                nMax = 4;
            }
            else if (nLevel <= 6)
            {
                nMax = 8;
            }
            else if (nLevel <= 8)
            {
                nMax = 12;
            }
            else if (nLevel <= 9)
            {
                nMax = 14;
            }
            else
            {
                // Nếu là trang bị luyện hóa
                if (ItemRefine.IsRefinedEquip(itemData) || ItemManager.TrangBiBachNganHoangKim(itemData.ItemID) || itemData.IsArtifact)
                {
                    nMax = 20;
                }
                else
                {
                    nMax = 14;
                }
            }

            return nMax;
        }

        public static bool IsHuyenTinh(ItemData _Input)
        {
            if (_Input.Genre == 18 && _Input.DetailType == 1 && (_Input.ParticularType == 1 || _Input.ParticularType == 114))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hàm sử dụng để đổi ngũ hành hồn thạch từ vật phẩm sang
        /// </summary>
        /// <param name="player"></param>
        /// <param name="equip"></param>
        /// <returns></returns>
        public static bool ExchangeFS(KPlayer player, GoodsData equip)
        {
            // Nếu vật phẩm không tồn tại
            if (!ItemManager._TotalGameItem.TryGetValue(equip.GoodsID, out var itemData))
            {
                PlayerManager.ShowNotification(player, "Trang bị không tồn tại!");
                return false;
            }

            // Số lượng ngũ hành hồn thạch nhận lại
            long fsCount;

            // Loại trang bị
            var itemType = (KE_ITEM_EQUIP_DETAILTYPE)itemData.DetailType;
            // Nếu là phi phong
            if (itemType == KE_ITEM_EQUIP_DETAILTYPE.equip_mantle)
            {
                // Hạn dùng Phi Phong
                var maxDays = 30;
                // Nếu có hạn sử dụng
                if (!string.IsNullOrEmpty(equip.Endtime))
                {
                    // Thời gian hết hạn
                    var endTime = DateTime.Parse(equip.Endtime);

                    if (itemData.ItemID >= 13156 && itemData.ItemID <= 13203)
                    { 
                        PlayerManager.ShowMessageBox(player, "Thông báo", "Phi Phong từ sự kiện không thể tách thành Ngũ Hành Hồn Thạch!");
                        return false;
                    }

                    if (endTime > DateTime.Now)
                    {
                        var diff = endTime - DateTime.Now;
                        var diffSec = (int)diff.TotalSeconds;

                        // Toạch
                        if (diffSec == -1)
                        {
                            fsCount = 0;
                        }
                        // Nếu còn dưới 1 ngày thì toạch
                        else if (diffSec < 86400)
                        {
                            fsCount = 0;
                        }
                        else
                        {
                            // Số ngày còn lại
                            long daysLeft = diffSec / 86400;

                            // Số Ngũ Hành Hồn Thạch đổi được = số ngày còn lại
                            fsCount = daysLeft * itemData.MakeCost / 1000 / maxDays;
                        }
                    }
                    else
                    {
                        // Toạch
                        fsCount = 0;
                    }
                }
                // Nếu không có hạn sử dụng thì trả ra giá trị Max
                else
                {
                    fsCount = itemData.MakeCost / 1000;
                }
            }
            else
            {
                // Nếu không phải đồ chế
                if (string.IsNullOrEmpty(equip.Creator))
                {
                    PlayerManager.ShowNotification(player, "Chỉ có trang bị chế mới có thể tách thành Ngũ Hành Hồn Thạch!");
                    return false;
                }

                // Nếu đã khóa
                //if (equip.Binding == 1)
                //{
                //    PlayerManager.ShowNotification(player, "Chỉ trang bị chế không khóa mới có thể tách thành Ngũ Hành Hồn Thạch!");
                //    return false;
                //}

                // Nếu trang bị có cường hóa
                if (equip.Forge_level > 0)
                {
                    PlayerManager.ShowNotification(player, "Trang bị đã cường hóa không thể tách thành Ngũ Hành Hồn Thạch!");
                    return false;
                }

                // Nếu dưới cấp 7 thì không cho tách
                if (itemData.Level < 7)
                {
                    PlayerManager.ShowNotification(player, "Trang bị này không thể tách thành Ngũ Hành Hồn Thạch!");
                    return false;
                }

                // Số lượng ngũ hành hồn thạch nhận lại
                fsCount = itemData.MakeCost / 1000;
            }

            // Nếu không có ngũ hành hồn thạch có thể đổi
            if (fsCount <= 0)
            {
                PlayerManager.ShowNotification(player, "Trang bị này không thể tách thành Ngũ Hành Hồn Thạch!");
                return false;
            }

            // Vật phẩm Ngũ Hành Hồn Thạch
            var fsItemData = ItemManager.GetItemTemplate(506);

            // nếu là trang bị khóa
            if (equip.Binding == 1)
            {
                fsItemData.BindType = 1;
            }

            // Khoảng trống cần
            var spaceNeed = KTGlobal.GetTotalSpacesNeedToTakeItem(fsItemData.ItemID, (int)fsCount);

            // Nếu không đủ chỗ trống
            if (!KTGlobal.IsHaveSpace(spaceNeed, player))
            {
                PlayerManager.ShowNotification(player, "Túi đồ không đủ chỗ trống, không thể tách trang bị!");
                return false;
            }

            // Xóa trang bị
            if (!ItemManager.AbandonItem(equip, player, false,"Đổi ngũ hành hồn thạch"))
            {
                PlayerManager.ShowNotification(player, "Không thể xóa trang bị!");
                return false;
            }

            // Thêm vật phẩm
            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, fsItemData.ItemID, (int)fsCount, 0, "ExchangeFS", true, equip.Binding, false, Global.ConstGoodsEndTime))
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi nhận Ngũ Hành Hồn Thạch!");
                return false;
            }

            // Thao tác thành công
            return true;
        }

        /// <summary>
        /// Cường hóa ngũ hành ấn
        /// </summary>
        /// <param name="TotalFS"></param>
        /// <param name="ItemInput"></param>
        /// <param name="client"></param>
        /// <param name="SelectRounter"></param>
        /// <returns></returns>
        public static int DoEnhSignet(List<GoodsData> TotalFS, GoodsData ItemInput, KPlayer client, int SelectRounter)
        {
            // Nếu ko tìm thấy bản thể của ngũ hành ấn thì return toạch
            var _SignetInput = ItemManager.GetItemTemplate(ItemInput.GoodsID);

            // Dữ liệu không hợp lệ
            if (_SignetInput == null)
            {
                PlayerManager.ShowNotification(client, "Thông tin Ngũ Hành Ấn không tồn tại, hãy thử đóng khung và thao tác lại!");
                return -1;
            }

            // Nếu chọn sai ROUNTER thì cũng chim cút
            if (SelectRounter < 0 || SelectRounter > 1)
            {
                PlayerManager.ShowNotification(client, "Hãy chọn loại cường hóa!");
                return -200;
            }

            // Nếu vật phẩm đút vào ko phỉa ngũ hành ấn
            if (!ItemManager.KD_ISSIGNET(_SignetInput.DetailType))
            {
                PlayerManager.ShowNotification(client, "Hãy đặt vào Ngũ Hành Ấn!");
                return -100;
            }

            double nExp = 0;
            // Exp hiện tại
            var nLevel = 0;
            //Tổng số ngũ hành hồn thạch bỏ vào
            var TotalBF = 0;

            // lấy ra toàn bộ số ngũ hành hồn thạch
            foreach (var _BF in TotalFS)
            {
                // Nếu vật phẩm là ngũ hành hồn thạch
                if (_BF.GoodsID == 506)
                {
                    TotalBF += _BF.GCount;
                }
            }

            if (TotalBF == 0)
            {
                PlayerManager.ShowNotification(client, "Chưa đặt vào Ngũ Hành Hồn Thạch!");
                return -3;
            }

            var CountItemInBag = ItemManager.GetItemCountInBag(client, 506);

            // Nếu số lượng ngũ hành hồn thạch trong người ít hơn số bỏ vào thì có biến
            if (CountItemInBag < TotalBF)
            {
                PlayerManager.ShowNotification(client, "Không đủ Ngũ Hành Hồn Thạch!");
                return -5;
            }

            // Sói thêm Fix BUG NULL-Param1_2
            if (ItemInput.OtherParams == null)
            {
                PlayerManager.ShowNotification(client, "Thông tin Ngũ Hành Ấn bị lỗi, hãy liên hệ với hỗ trợ để được trợ giúp!");
                return -4;
            }

            // Nếu là cường hóa ngũ hành tương khắc
            if (SelectRounter == 0)
            {
                if (ItemInput.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var enhance))
                {
                    var enhancePram = enhance.Split('|');

                    nLevel = Int32.Parse(enhancePram[0]);
                    nExp = Int32.Parse(enhancePram[1]);
                }
            } // Nếu là nhược hóa ngũ hành ấn
            else
            {
                if (ItemInput.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var abate))
                {
                    var abatePram = abate.Split('|');

                    nLevel = Int32.Parse(abatePram[0]);
                    nExp = Int32.Parse(abatePram[1]);
                }
            }

            var MaxLevel = 1000;

            if (_SignetInput.Level == 2)
            {
                // lôi đình ấn
                MaxLevel = 1100 + ItemInput.Forge_level * 100;
            }

            // Return đã max level cường hóa
            if (nLevel >= MaxLevel)
            {
                PlayerManager.ShowNotification(client, "Ngũ Hành Ấn đã đạt cấp tối đa!");
                return -2;
            }

            double nMuti = 100;

            nExp = nExp + Math.Floor(TotalBF * 10 * nMuti / 100);



            var ExpFind = ItemManager._TotalSingNetExp.FirstOrDefault(x => x.Level == nLevel);

            var ExpFistFind = ExpFind.UpgardeExp;

            while (nLevel < MaxLevel && nExp >= ExpFistFind)
            {
                //Lấy ra lượng EXP cần để lên cấp
                ExpFistFind = ItemManager._TotalSingNetExp.FirstOrDefault(x => x.Level == nLevel).UpgardeExp;

                nExp = nExp - ExpFistFind;
                nLevel = nLevel + 1;
            }

            double nResCount = 0;

            if (nLevel >= MaxLevel && nExp > 0)
            {
                nResCount = Math.Floor(nExp / (10 * nMuti / 100));

                nExp = 0;
            }

            var ItemWillBeRemove = TotalBF - (int)nResCount;

            // Báo lỗi nếu không xóa được vật phẩm
            if (!ItemManager.RemoveItemFromBag(client, 506, ItemWillBeRemove,-1,"Cường hóa ấn"))
            {
                PlayerManager.ShowNotification(client, "Xóa vật phẩm thất bại!");
                return -20;
            }

            var COUNTAGINA = ItemManager.GetItemCountInBag(client, 506);

          
            // Khởi tạo 1 cái DICT UPDATE VÀO DB
            var TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, ItemInput.Id);

            // SET CÁC KIỂU VÀO DB
            if (SelectRounter == 0)
            {
                var OtherParams = new Dictionary<ItemPramenter, string>();

                if (ItemInput.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var OldValue))
                {
                    var NewValue = nLevel + "|" + nExp;

                    LogManager.WriteLog(LogTypes.NHHT, "[" + client.RoleID + "][" + client.RoleName + "]["+ ItemInput.Id + "]["+ ItemInput.GoodsID + "] cường hóa ngũ hành tương khắc gia tăng : " + NewValue + " | Tiêu tốn :" + ItemWillBeRemove);

                    OtherParams.Add(ItemPramenter.Pram_1, NewValue + "");
                    OtherParams.Add(ItemPramenter.Pram_2, OldValue + "");

                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);
                }
            }
            else
            {
                var OtherParams = new Dictionary<ItemPramenter, string>();
                if (ItemInput.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var OldValue))
                {
                    var NewValue = nLevel + "|" + nExp;

                    LogManager.WriteLog(LogTypes.NHHT, "[" + client.RoleID + "][" + client.RoleName + "][" + ItemInput.Id + "][" + ItemInput.GoodsID + "] nhược khóa ngũ hành tương khắc gia tăng : " + NewValue + " | Tiêu tốn :" + ItemWillBeRemove);


                    OtherParams.Add(ItemPramenter.Pram_1, OldValue + "");
                    OtherParams.Add(ItemPramenter.Pram_2, NewValue + "");

                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);
                }
            }

            // Thực hiện update vào DB

            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, ItemInput, client, "EnchanceSigNet"))
            {
                PlayerManager.ShowNotification(client, "Có lỗi khi cường hóa ngũ hành ấn");
            }

            // Trả về luyện hóa thành công
            return 1;
        }

        // Hàm caclulation nếu sói muốn đẩy dữ liệu về client đỡ phỉa load EXPFILES
        public static void Caclulation(int TotalBF, int nLevel, int nExp)
        {
            var nMuti = 100;

            nExp = nExp + TotalBF * 10 * nMuti / 100;

            var MaxLevel = ItemManager._TotalSingNetExp.Max(x => x.Level);

            var ExpFind = ItemManager._TotalSingNetExp.Where(x => x.Level == nLevel).FirstOrDefault();

            var ExpFistFind = ExpFind.UpgardeExp;

            while (nLevel < MaxLevel && nExp >= ExpFistFind)
            {
                ExpFistFind = ItemManager._TotalSingNetExp.Where(x => x.Level == nLevel).FirstOrDefault().UpgardeExp;
                nExp = nExp - ExpFistFind;
                nLevel = nLevel + 1;
            }
            var nResCount = 0;
            if (nLevel >= MaxLevel && nExp > 0)
            {
                nResCount = nExp / (10 * nMuti / 100);
            }

            //Console.WriteLine(nLevel);
            // Console.WriteLine(nExp);
            // Console.WriteLine(nLevel);

            // Số lượng  ngũ hành ấn thừa sẽ trả lại
            // Console.WriteLine(nResCount);
        }

        /// <summary>
        /// Thực hiện cường hóa
        /// </summary>
        /// <param name="TotalHuyenTinh"></param>
        /// <param name="Item"></param>
        /// <param name="_Player"></param>
        /// <param name="MoneyType"></param>
        public static int DoEnhItem(List<GoodsData> TotalHuyenTinh, GoodsData Item, KPlayer _Player, MoneyType Type)
        {
            // Check xem có đủ điều kiện cường hóa không
            if (!CheckEnhItem(Item, _Player))
            {
                return -1;
            }

            var _Caclution = GetEnhItem(TotalHuyenTinh, Item);

            if (_Caclution.nProb > 0)
            {
                // Nếu tỉ lệ quá thấp
                if (_Caclution.nTrueProb < 10)
                {
                    PlayerManager.ShowNotification(_Player, "Xác suất thành công quá thấp, không thể cường hóa");
                    return -1;
                }

                // Nếu tỉ lệ quá cao
                if (_Caclution.nTrueProb > 120)
                {
                    PlayerManager.ShowNotification(_Player, "Huyền Tinh đặt vào quá nhiều, xin đừng lãng phí");
                    return -1;
                }

                if (Item.GoodsID == 10030 && Item.Forge_level == 0 && _Caclution.nTrueProb < 90)
                {
                    PlayerManager.ShowNotification(_Player, "Cường hóa vật phẩm nhiệm vụ, vui lòng đặt thêm huyền tinh");
                    return -1;
                }
                
                // ma giáo
                if (_Caclution.nTrueProb < 20)
                {
                    _Caclution.nTrueProb -= 5;
                }
                else if (_Caclution.nTrueProb < 50)
                {
                    _Caclution.nTrueProb -= 3;
                }

                if (_Caclution.nTrueProb == 0) {
                    // pass
                } else if (_Caclution.nTrueProb <= 10) {
                    _Caclution.nTrueProb = 1;
                } else if (_Caclution.nTrueProb <= 20) {
                    _Caclution.nTrueProb = 5;
                } else if (_Caclution.nTrueProb <= 35) {
                    _Caclution.nTrueProb -= 10;
                } else if (_Caclution.nTrueProb <= 70) {
                    _Caclution.nTrueProb -= 7;
                } else if (_Caclution.nTrueProb <= 90) {
                    _Caclution.nTrueProb -= 5;
                }

                // nếu nằm trong danh sách cắt giảm thì giảm 1/2 tỉ lệ
                // if (KTGlobal.IsRateReduce(_Player.RoleID)  && _Caclution.nTrueProb < 98) 
                // {
                //     _Caclution.nTrueProb /= 2.0;
                // }

                // nếu là kiếm long ngũ thái gia thì tỉ lệ cường hóa luôn là 100%
                if (Item.GoodsID == 10030 && Item.Forge_level == 0)
                {
                    _Caclution.nTrueProb = 100;
                }

                var MoneyNeed = (int)_Caclution.nMoney;

                if (!KTGlobal.IsHaveMoney(_Player, MoneyNeed, Type))
                {
                    PlayerManager.ShowNotification(_Player, "Bạc mang trên người không đủ");
                    return -1;
                }

                // Nếu ok hết thực hiện cường hóa
                var _Submoney = KTGlobal.SubMoney(_Player, MoneyNeed, Type, "EnhItem");
                if (!_Submoney.IsOK)
                {
                    PlayerManager.ShowNotification(_Player, "Có lỗi khi trừ tiền");
                    return -1;
                }

                var LOCK = 0;

                if (Item.Binding == 1)
                {
                    LOCK = 1;
                }

                if (Type == MoneyType.BacKhoa)
                {
                    LOCK = 1;
                }

                var findlock = TotalHuyenTinh.Where(x => x.Binding == 1).FirstOrDefault();
                if (findlock != null)
                {
                    LOCK = 1;
                }
                // text lưu vào log huyền tinh
                var strlog = string.Empty;
                // Thực hiện xóa huyền tinh
                foreach (var _HuyenTinh in TotalHuyenTinh)
                {
                    // Thực hiện xóa tất cả huyền tinh
                    ItemManager.AbandonItem(_HuyenTinh, _Player, false, "Cường hóa vật phẩm");
                    strlog += $" ({_HuyenTinh.Id}/{_HuyenTinh.GoodsID}/{_HuyenTinh.ItemName})";
                }

                var Random = KTGlobal.GetRandomNumber(0, 99);
                
                LogManager.WriteLog(LogTypes.Enhance, "Người chơi [" + _Player.RoleID + "][" + _Player.RoleName + "][" + Item.GoodsID + "][" + Item.Id + "] mang " + ItemManager.GetNameItem(Item) + " cường hóa :[" + (Item.Forge_level + 1) + "] với tỉ lệ  :" + _Caclution.nProb);

                if (Random < _Caclution.nTrueProb)
                {
                    PlayerManager.ShowNotification(_Player, "Cường hóa thành công");

                    // Cường hóa thành công
                    ItemManager.SetEquipForgeLevel(Item, _Player, Item.Forge_level + 1, LOCK);

                    if (Item.Forge_level >= 9)
                    {
                        var ShowHang = "Người chơi " + KTGlobal.CreateStringByColor(_Player.RoleName, ColorType.Green) + " với " + KTGlobal.CreateStringByColor(_Caclution.nProb + "%", ColorType.VanillaIce) + " đem " + KTGlobal.GetItemDescInfoStringForChat(Item) + " cường hóa lên " + KTGlobal.CreateStringByColor("+" + Item.Forge_level + "", ColorType.VanillaIce) + " thành công!";
						KTGlobal.SendSystemEventNotification(ShowHang, new List<GoodsData>() { Item });
					}
                    LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) cường hóa thành công ({Item.Id}/{Item.GoodsID}/{Item.ItemName}) lên [{Item.Forge_level}] dùng" + strlog + $"tỷ lệ {_Caclution.nTrueProb}");

                    ItemManager._TotalGameItem.TryGetValue(Item.GoodsID, out var itemData);
                    
                    ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player, -1, Item.Id, Item.GoodsID, TaskTypes.Enhance);

                    // Nếu là vũ khí
                    if(ItemManager.KD_ISWEAPON(itemData.DetailType))
                    {

                        GlobalEventSource.getInstance().fireEvent(new EnhanceWeaponEventObject(_Player, Item.Forge_level));

                        if(Item.Forge_level >= 4)
                        {
                            //SideTaskManager.getInstance().ProcessTask(_Player, (int)SideTaskName.Enhance4);
                        }
                    }

                    if (ItemManager.KD_ISORNAMENT(itemData.DetailType) || ItemManager.KD_ISEQUIP_DEFEND(itemData.DetailType))
                    {
                        GlobalEventSource.getInstance().fireEvent(new EnhanceArmorEventObject(_Player, Item.Forge_level));
                    }
                    return 1;
                }
                else
                {
                    if (Item.Forge_level >= 8)
                    {
                        var ShowHang = "Thật đáng tiếc! " + KTGlobal.CreateStringByColor(_Player.RoleName, ColorType.Green) + " với " + KTGlobal.CreateStringByColor(_Caclution.nProb + "%", ColorType.VanillaIce) + " đã thất bại khi cường hóa " + KTGlobal.GetItemDescInfoStringForChat(Item) + " lên " + KTGlobal.CreateStringByColor("+" + (Item.Forge_level + 1) + "", ColorType.VanillaIce) + "!";
                        KTGlobal.SendSystemEventNotification(ShowHang, new List<GoodsData>() { Item });
                    }
                    PlayerManager.ShowNotification(_Player, "Cường hóa thất bại");
                    LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) cường hóa thất bại ({Item.Id}/{Item.GoodsID}/{Item.ItemName}) lên [{Item.Forge_level + 1}] dùng" + strlog + $"tỷ lệ {_Caclution.nTrueProb}");

                    return 0;
                }
            }
            else
            {
                PlayerManager.ShowNotification(_Player, "Tỉ lệ cường hóa quá thấp");
                return -1;
            }
        }

        public static CalcProb GetEnhItem(List<GoodsData> TotalHuyenTinh, GoodsData Item)
        {
            var _Prob = new CalcProb();

            _Prob.nMoney = -1;
            _Prob.nProb = 0;
            _Prob.nTrueProb = 0;

            long AllValue = 0;

            foreach (var FixItem in TotalHuyenTinh)
            {
                var _Find = ItemManager.GetItemTemplate(FixItem.GoodsID);
                if (_Find != null)
                {
                    if (IsHuyenTinh(_Find))
                    {
                        AllValue = AllValue + _Find.ItemValue;
                    }
                }
                else
                {
                    return _Prob;
                }
            }

            var _ItemSelect = ItemManager.GetItemTemplate(Item.GoodsID);

            if (!ItemManager.KD_ISEQUIP(_ItemSelect.Genre))
            {
                return _Prob;
            }

            var CurLevel = Item.Forge_level;

            var NextLevel = CurLevel + 1;

            long NeedValue = 0;

            var _FindValue = ItemManager._Calutaion.List_Enhance_Value.Where(x => x.EnhanceTimes == NextLevel).FirstOrDefault();

            if (_FindValue != null)
            {
                NeedValue = _FindValue.Value;
            }

            var nTypeRate = 100;

            var TypeRate = ItemManager._Calutaion.List_Equip_Type_Rate.Where(x => (int)x.EquipType == _ItemSelect.DetailType).FirstOrDefault();
            if (TypeRate != null)
            {
                nTypeRate = TypeRate.Value;
            }

            // Chia 100 cho cản thận
            nTypeRate = nTypeRate / 100;
            double nCostValue = nTypeRate * NeedValue;
            var Money = (long)(nCostValue * 0.1);

            nCostValue = nCostValue - Money;
            var nProb = Math.Floor(AllValue / nCostValue * 100);

            var nTrueProb = nProb;

            if (nProb > 100)
            {
                nProb = 100;
            }

            _Prob.nMoney = Money;
            _Prob.nProb = nProb;
            _Prob.nTrueProb = nTrueProb;

            return _Prob;
        }

        public static bool CheckEnhItem(GoodsData Input, KPlayer player)
        {
            // Nếu vật phẩm không tồn tại thì không cho cường hóa
            if (!ItemManager.IsExits(Input))
            {
                PlayerManager.ShowNotification(player, "Vật phẩm không tồn tại!");

                return false;
            }

            var _Data = ItemManager._TotalGameItem[Input.GoodsID];

            if (_Data.ListReqProp.Any(x => x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_ENH && x.ReqPropValue == 1))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Vật phẩm không thể cưởng hóa!");
                return false;
            }

            // Nếu không phải trang bị thì không cho cường hóa
            if (!ItemManager.KD_ISEQUIP(_Data.Genre))
            {
                PlayerManager.ShowNotification(player, "Vật phẩm không phải là trạng bị");

                return false;
            }

            if (_Data.DetailType < MIN_COMMON_EQUIP || _Data.DetailType > MAX_COMMON_EQUIP)
            {
                PlayerManager.ShowNotification(player, "Vật phẩm không đủ phẩm chất cường hóa");

                return false;
            }

            if (Input.Forge_level >= CalcMaxEnhanceTimes(_Data))
            {
                PlayerManager.ShowNotification(player, "Đã cường hóa tới cấp độ tối đa");
                return false;
            }

            return true;
        }

        #endregion CUONGHOA

        #region GHEPHUYENTINH

        /// <summary>
        /// Hàm ghép huyền tinh
        /// </summary>
        /// <param name="TotalHuyenTinh"></param>
        public static bool ComposeItemCrystal(List<GoodsData> TotalHuyenTinh, KPlayer _Player, MoneyType Type)
        {
            if (_Player == null)
            {
                PlayerManager.ShowNotification(_Player, "Lỗi thao tác!");
                return false;
            }
            if (TotalHuyenTinh == null)
            {
                PlayerManager.ShowNotification(_Player, "Bỏ vào quá ít huyền tinh");
                return false;
            }

            if (TotalHuyenTinh.Count <= 0)
            {
                PlayerManager.ShowNotification(_Player, "Bỏ vào quá ít huyền tinh");
                return false;
            }

            var _ComposeItem = ComposeItem(TotalHuyenTinh);

            if (_ComposeItem == null || _ComposeItem.nItemMaxLevel == null || _ComposeItem.nItemMinLevel == null)
            {
                PlayerManager.ShowNotification(_Player, "Thao tác bị lỗi, hãy thử lại!");
                return false;
            }

            if (_ComposeItem.nFee > 0)
            {
                var MoneyNeed = (int)_ComposeItem.nFee;

                if (!KTGlobal.IsHaveMoney(_Player, MoneyNeed, Type))
                {
                    PlayerManager.ShowNotification(_Player, "Bạc mang trên người không đủ");
                    return false;
                }

                // Nếu ok hết thực hiện cường hóa
                var _Submoney = KTGlobal.SubMoney(_Player, MoneyNeed, Type, "COMPOSECRYTAL");
                if (!_Submoney.IsOK)
                {
                    PlayerManager.ShowNotification(_Player, "Có lỗi khi trừ tiền");
                    return false;
                }

                var strlog = String.Empty;
                // Thực hiện xóa huyền tinh
                foreach (var _HuyenTinh in TotalHuyenTinh)
                {
                    // Thực hiện xóa tất cả huyền tinh
                    ItemManager.AbandonItem(_HuyenTinh, _Player, false, "Ghép huyền tinh");
                    strlog += $"({_HuyenTinh.Id}/{_HuyenTinh.GoodsID}/{_HuyenTinh.ItemName})"; 
                }

                var Random = KTGlobal.GetRandomNumber(1, 100);

                var MinRate = _ComposeItem.nMinLevelRate;
                var MaxRate = _ComposeItem.nMaxLevelRate;

                var Lock = 0;

                var Find = TotalHuyenTinh.Where(x => x.Binding == 1).FirstOrDefault();
                if (Find != null)
                {
                    Lock = 1;
                }

                if (Type == MoneyType.BacKhoa)
                {
                    Lock = 1;
                }

                // nếu nằm trong danh sách cắt giảm thì mặc định về HT cấp thấp
                // if (KTGlobal.IsRateReduce(_Player.RoleID) && MaxRate < 85) 
                // {
                //     MaxRate = 0;
                //     MinRate = 100;
                // } else if (MaxRate == 0) {
                if (MaxRate == 0) {
                    // pass
                } else if (MaxRate <= 10 && MaxRate > 0)
                {
                    MinRate = 99;
                    MaxRate = 1;
                } else if (MaxRate <= 20)
                {
                    MinRate = 95;
                    MaxRate = 5;
                } else if (MaxRate <= 35)
                {
                    MinRate += 10;
                    MaxRate -= 10;
                } else if (MaxRate <= 70)
                {
                    MinRate += 7;
                    MaxRate -= 7;
                } else if (MaxRate <= 90)
                {
                    MinRate += 5;
                    MaxRate -= 5;
                }

                // Nếu random ra mà nhỏ hơn min rate tức là vào huyền tinh cấp thấp
                if (Random <= MinRate)
                {
                    if (ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player, _ComposeItem.nItemMinLevel.ItemID, 1, 0, "COMPOSECRYTAL", false, Lock, false, Global.ConstGoodsEndTime))
                    {
                        PlayerManager.ShowNotification(_Player, "Hợp thành :" + _ComposeItem.nItemMinLevel.Name);

                        LogManager.WriteLog(LogTypes.Item, "[" + _Player.RoleID + "][" + _Player.RoleName + "][Ghép Huyền Tinh] : Hợp thành thành công HUYỀN TINH :" + _ComposeItem.nItemMinLevel.Name);
                        LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) ghép huyền tinh ra ({_ComposeItem.nItemMinLevel.ItemID}/{_ComposeItem.nItemMinLevel.Name}) từ" + strlog + $" tỷ lệ {MinRate}");
                        
                        ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player, -1, -1, 10000, TaskTypes.Other);
                    }
                    else
                    {
                       
                        PlayerManager.ShowNotification(_Player, "Có lỗi khi thêm huyền tinh vào túi đồ vui lòng liên hệ ADMIN để được giúp đỡ");

                        LogManager.WriteLog(LogTypes.Item, "[" + _Player.RoleID + "][" + _Player.RoleName + "][Ghép Huyền Tinh][BUG] : Hợp thành thành công HUYỀN TINH lỗi :" + _ComposeItem.nItemMinLevel.Name);
                    }
                } // Nếu mà random ra max rate tức là vào huyền tinh cấp cao
                else
                {
                    if (ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player, _ComposeItem.nItemMaxLevel.ItemID, 1, 0, "COMPOSECRYTAL", false, Lock, false, Global.ConstGoodsEndTime))
                    {
                        PlayerManager.ShowNotification(_Player, "Hợp thành :" + _ComposeItem.nItemMaxLevel.Name);

                        LogManager.WriteLog(LogTypes.Item, "[" + _Player.RoleID + "][" + _Player.RoleName + "][Ghép Huyền Tinh] : Hợp thành thành công HUYỀN TINH :" + _ComposeItem.nItemMaxLevel.Name);

                        //Hàm CreateItem hiện tại chưa trả về luôn ID của item trong DB nên đã log ra nguồn nhận được huyền tinh ngay bên trên
                        LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) ghép huyền tinh ra ({_ComposeItem.nItemMaxLevel.ItemID}/{_ComposeItem.nItemMaxLevel.Name}) từ " + strlog + $" tỷ lệ {MaxRate}");
                        
                        ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player, -1, -1, 10000, TaskTypes.Other);
                    }
                    else
                    {
                        
                        PlayerManager.ShowNotification(_Player, "Có lỗi khi thêm huyền tinh vào túi đồ vui lòng liên hệ ADMIN để được giúp đỡ");

                        LogManager.WriteLog(LogTypes.Item, "[" + _Player.RoleID + "][" + _Player.RoleName + "][Ghép Huyền Tinh][BUG] : Hợp thành thành công HUYỀN TINH lỗi :" + _ComposeItem.nItemMaxLevel.Name);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static ComposeItem ComposeItem(List<GoodsData> TotalHuyenTinh)
        {
            if (TotalHuyenTinh == null || TotalHuyenTinh.Count <= 0)
            {
                return null;
            }

            var _ComposeItem = new ComposeItem();
            _ComposeItem.nFee = 0;

            _ComposeItem.nMaxLevelRate = -1;
            _ComposeItem.nMinLevelRate = -1;

            var AllValue = 0;

            foreach (var FixItem in TotalHuyenTinh)
            {
                var _Find = ItemManager.GetItemTemplate(FixItem.GoodsID);
                if (_Find != null)
                {
                    if (IsHuyenTinh(_Find))
                    {
                        AllValue = AllValue + _Find.ItemValue;
                    }
                    else
                    {
                        return _ComposeItem;
                    }
                }
            }

            var MinLevel = 0;
            var MaxLevel = 0;

            var nMinLevelRate = 0;
            var nMaxLevelRate = 0;

            var _CristalItem = ItemManager.TotalItem.Where(x => x.QualityPrefix == 0 && x.Category == 0 && x.Genre == 18 && x.DetailType == 1 && x.ParticularType == 1).OrderByDescending(x => x.Level).ToList();

            foreach (var _Item in _CristalItem)
            {
                if (AllValue >= _Item.ItemValue)
                {
                    MinLevel = _Item.Level;
                    break;
                }
            }

            MaxLevel = MinLevel + 1;

            var MinLevelItem = _CristalItem.Where(x => x.Level == MinLevel).FirstOrDefault();

            var MaxLevelItem = _CristalItem.Where(x => x.Level == MaxLevel).FirstOrDefault();

            _ComposeItem.nItemMinLevel = MinLevelItem;
            _ComposeItem.nItemMaxLevel = MaxLevelItem;

            var MoneyNeed = AllValue / 10;

            if (MinLevel >= 12)
            {
                MinLevel = 11;
                MaxLevel = 12;
                MinLevelItem = _CristalItem.Where(x => x.Level == MinLevel).FirstOrDefault();
                MaxLevelItem = _CristalItem.Where(x => x.Level == MaxLevel).FirstOrDefault();
                _ComposeItem.nItemMinLevel = MinLevelItem;
                _ComposeItem.nItemMaxLevel = MaxLevelItem;
                nMinLevelRate = 0;
                nMaxLevelRate = 100;
            }
            else
            {
                nMinLevelRate = MaxLevelItem.ItemValue - AllValue;
                nMaxLevelRate = AllValue - MinLevelItem.ItemValue;

                // Quy đổi ra đơn vị % tương ứng
                var nMinLevelRateP = nMinLevelRate * 100f / (nMinLevelRate + nMaxLevelRate);
                var nMaxLevelRateP = nMaxLevelRate * 100f / (nMinLevelRate + nMaxLevelRate);
                if (nMinLevelRateP - (int)nMinLevelRateP >= 0.5f)
                {
                    nMinLevelRateP += 1f;
                }
                if (nMaxLevelRateP - (int)nMaxLevelRateP >= 0.5f)
                {
                    nMaxLevelRateP += 1f;
                }

                nMinLevelRate = (int)nMinLevelRateP;
                nMaxLevelRate = (int)nMaxLevelRateP;
            }

            _ComposeItem.nMinLevelRate = nMinLevelRate;
            _ComposeItem.nMaxLevelRate = nMaxLevelRate;
            _ComposeItem.nFee = MoneyNeed;

            return _ComposeItem;
        }

        #endregion GHEPHUYENTINH

        #region TACHHUYENTINH

        /// <summary>
        /// Tách Huyền Tinh từ trang bị tương ứng
        /// </summary>
        /// <param name="InputCrystal"></param>
        /// <param name="_Player"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool DoSplitCrystal(GoodsData InputCrystal, KPlayer _Player)
        {
            var _Input = ItemManager.GetItemTemplate(InputCrystal.GoodsID);

            if (_Input == null)
            {
                PlayerManager.ShowNotification(_Player, "Không tìm thấy vật phẩm");
                return false;
            }

            if (!IsHuyenTinh(_Input))
            {
                PlayerManager.ShowNotification(_Player, "Vật phẩm đặt vào không phải huyền tinh");
                return false;
            }

            //int MoneyNeed = _Input.ItemValue / 10;

            var TryFindInfo = TotalCrystalSplit(InputCrystal);

            if (TryFindInfo == null)
            {
                PlayerManager.ShowNotification(_Player, "Không thể tách!");
                return false;
            }

            if (!KTGlobal.IsHaveSpace(TryFindInfo.Count, _Player))
            {
                PlayerManager.ShowNotification(_Player, "Túi đồ không đủ chỗ!");
                return false;
            }

            //if (!KTGlobal.IsHaveMoney(_Player, MoneyNeed, Type))
            //{
            //    PlayerManager.ShowNotification(_Player, "Bạc trên người không đủ!");
            //    return false;
            //}

            //SubRep MoneySub = KTGlobal.SubMoney(_Player, MoneyNeed, Type);
            //if (!MoneySub.IsOK)
            //{
            //    PlayerManager.ShowNotification(_Player, "Bạc trên người không đủ!");
            //    return false;
            //}

            var isBound = InputCrystal.Binding;
            var strlog = String.Empty;

            // Xóa item source
            if (ItemManager.AbandonItem(InputCrystal, _Player, false, "Tách huyền tinh"))
            {
                // Thực hiện add item vào túi
                foreach (var _ItemOut in TryFindInfo)
                {
                    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player, _ItemOut.ItemID, 1, 0, "SPLITCRYTAL", false, isBound, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, true))
                    {
                        PlayerManager.ShowNotification(_Player, "Tách huyền tinh thành :" + _ItemOut.Name);

                        LogManager.WriteLog(LogTypes.Item, "[" + _Player.RoleID + "][" + _Player.RoleName + "][Tách huyền tinh] : Tách huyền tinh HUYỀN TINH :" + _ItemOut.Name);
                    }
                    strlog += $"({_ItemOut.ItemID}/{_ItemOut.Name})";
                }
                LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) tách huyền tinh ({InputCrystal.Id}/{InputCrystal.GoodsID}/{InputCrystal.ItemName}) thành " + strlog);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hàm tách huyền tinh cấp cao ra huyền tinh cấp thấp
        /// </summary>
        /// <param name="InputCrystal"></param>
        /// <param name="_Input"></param>
        /// <param name="isBound"></param>
        /// <returns></returns>
        public static List<ItemData> TotalCrystalSplit(GoodsData InputCrystal, ItemData _Input = null)
        {
            List<ItemData> _OutputCrytals = null;

            if (_Input == null)
            {
                _Input = ItemManager.GetItemTemplate(InputCrystal.GoodsID);
            }

            if (_Input == null)
            {
                return _OutputCrytals;
            }

            if (!IsHuyenTinh(_Input))
            {
                return _OutputCrytals;
            }

            // Nếu cấp mà nhỏ hơn 10 thì không cho tách
            if (_Input.Level < 10)
            {
                return _OutputCrytals;
            }

            double nValue = _Input.ItemValue * KTGlobal.KD_SPLITCRYTAL_VALUE_COST;

            _OutputCrytals = new List<ItemData>();

            // Lấy ra toàn bộ huyền tinh theo cấp độ giảm dần
            var _CristalItem = ItemManager.TotalItem.Where(x => x.QualityPrefix == 0 && x.Category == 0 && x.Genre == 18 && x.DetailType == 1 && x.ParticularType == 1).OrderByDescending(x => x.Level).ToList();

            var FindLevel1 = _CristalItem.Where(x => x.Level == 1).FirstOrDefault();

            // Tối đa tách ra 10 huyền tinh cấp thấp hơn
            for (var nCount = 1; nCount < MAX_SPLITNUM; nCount++)
            {
                foreach (var item in _CristalItem)
                {
                    if (nValue / item.ItemValue > 1)
                    {
                        var nNum = Math.Floor(nValue / item.ItemValue);

                        if (nNum > 1)
                        {
                            for (var i = 0; i < nNum; i++)
                            {
                                // Chỉ lấy các huyền tinh có level > 4 còn lại thì người chơi phải chịu lỗ VALUE coi như phí
                                if (item.Level > 4)
                                {
                                    _OutputCrytals.Add(item);
                                }
                            }

                            nValue = nValue % item.ItemValue;

                            break;
                        }
                    }
                }
                if ((nValue / FindLevel1.ItemValue < 1) || nValue == 0)
                {
                    break;
                }
            }

            var SUM = _OutputCrytals.Sum(x => x.ItemValue);
            return _OutputCrytals;
        }

        #endregion TACHHUYENTINH

        //VIẾT LẠI HÀM NÀY CHỈNH LẠI RATE

        #region TACHHUYENTINH_TUTRANGBI

        /// <summary>
        /// Tách Huyền Tinh khỏi trang bị
        /// </summary>
        /// <param name="InputItem"></param>
        /// <param name="_Player"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool DoSplitCrystalFromEquip(GoodsData InputItem, KPlayer _Player)
        {
            var _Input = ItemManager.GetItemTemplate(InputItem.GoodsID);

            if (_Input == null)
            {
                PlayerManager.ShowNotification(_Player, "Không tìm thấy vật phẩm");
                return false;
            }
            
            if (_Input.ListReqProp.Any(x => x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_ENH && x.ReqPropValue == 1))
            {
                PlayerManager.ShowMessageBox(_Player, "Thông báo", "Vật phẩm không thể tách huyền tinh!");
                return false;
            }

            if (ItemManager.KD_ISCHANNGUYEN(_Input.DetailType))
            {
                PlayerManager.ShowMessageBox(_Player, "Thông báo", "Vật phẩm không thể tách huyền tinh");
                return false;
            }    

            var Forge_level = InputItem.Forge_level;

            if (Forge_level < KTGlobal.KD_MIN_ENHLEVEL_TO_SPLIT)
            {
                PlayerManager.ShowNotification(_Player, "Chỉ trang bị cường hóa lên cấp " + KTGlobal.KD_MIN_ENHLEVEL_TO_SPLIT + " mới có thể tách huyền tinh!");
                return false;
            }

            //if (_Input.Level < KTGlobal.KD_MIN_ENHLEVEL_TO_SPLIT)
            //{
            //    PlayerManager.ShowNotification(_Player, "Chỉ trang bị cấp " + KTGlobal.KD_MIN_ENHLEVEL_TO_SPLIT + " trở lên mới có thể tách huyền tinh ra!");
            //    return false;
            //}

            // nếu là đồ có hạn sử dụng thì ko thể tách
            if (InputItem.Endtime != Global.ConstGoodsEndTime)
            {
                PlayerManager.ShowMessageBox(_Player, "Thông báo", "Vật phẩm không thể tách huyền tinh!");
                return false;
            }    

            var _FindValue = ItemManager._Calutaion.List_Enhance_Value.Where(x => x.EnhanceTimes <= Forge_level).ToList();

            if (_FindValue == null)
            {
                return false;
            }

            // lấy ra tổng tài phú của các huyền tinh
            double TotalValue = _FindValue.Sum(x => x.Value);

            float nTypeRate = 100;

            var TypeRate = ItemManager._Calutaion.List_Equip_Type_Rate.Where(x => (int)x.EquipType == _Input.DetailType).FirstOrDefault();

            if (TypeRate != null)
            {
                nTypeRate = TypeRate.Value;
            }

            // hardcode nếu là trang sức thì cho = 100
            if (TypeRate.EquipType == KE_ITEM_EQUIP_DETAILTYPE.equip_ring || TypeRate.EquipType == KE_ITEM_EQUIP_DETAILTYPE.equip_necklace 
                || TypeRate.EquipType == KE_ITEM_EQUIP_DETAILTYPE.equip_amulet || TypeRate.EquipType == KE_ITEM_EQUIP_DETAILTYPE.equip_pendant)
            {
                nTypeRate = 100;
            }    

            // Chia 100 cho cẩn thận
            nTypeRate = nTypeRate / 100;

            TotalValue = TotalValue * nTypeRate;

            var TryFindInfo = TotalCrystalSplitFromEquip(TotalValue * KTGlobal.KD_SPLIT_VALUE_COST);

            if (TryFindInfo == null)
            {
                PlayerManager.ShowNotification(_Player, "Không thể tách!");
                return false;
            }

            if (!KTGlobal.IsHaveSpace(TryFindInfo.Count, _Player))
            {
                PlayerManager.ShowNotification(_Player, "Túi đồ không đủ chỗ!");
                return false;
            }

            var nEnhLevel = InputItem.Forge_level;
            double nMoney = 0;
            if (nEnhLevel >= 12 && nEnhLevel <= 13)
            {
                nMoney = Math.Floor(TotalValue * ItemEnhance.PEEL_RESTORE_RATE_12);
            }
            else if (nEnhLevel >= 14 && nEnhLevel <= 16)
            {
                nMoney = Math.Floor(TotalValue * ItemEnhance.PEEL_RESTORE_RATE_14);
            }
            else if (nEnhLevel >= 17 && nEnhLevel <= 18)
            {
                nMoney = Math.Floor(TotalValue * ItemEnhance.PEEL_RESTORE_RATE_17_18);
            }
            else if (nEnhLevel >= 19 && nEnhLevel <= 20)
            {
                nMoney = Math.Floor(TotalValue * ItemEnhance.PEEL_RESTORE_RATE_19_20);
            }

            // Số Bạc Trả Về
            if (nMoney > 0)
            {
                PlayerManager.ShowNotification(_Player, KTGlobal.CreateStringByColor("Tách trang bị hoàn trả [" + nMoney + "] Bạc khóa", ColorType.Green));

                KTGlobal.AddMoney(_Player, (int)nMoney, MoneyType.BacKhoa, "TACHTRANGBI|" + nEnhLevel+"|" + InputItem.GoodsID);
            }
            ///// Số bạc khóa nhận được sau khi tách
            //int nMoneyAdd = (int) (_Input.ItemValue * KTGlobal.KD_SPLIT_VALUE_COST / 10);
            ///// Thêm bạc khóa cho người chơi
            //GameManager.ClientMgr.AddUserBoundMoney(_Player, nMoneyAdd, "BUY_SHOP_ITEM");

            // Xóa item source
            if (ItemManager.SetEquipForgeLevel(InputItem, _Player, 0, InputItem.Binding))
            {
                var strlog = String.Empty;
                // Thực hiện add item vào túi
                foreach (var _ItemOut in TryFindInfo)
                {
                    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Player, _ItemOut.ItemID, 1, 0, "SPLITCRYTALFROMEQU", false, InputItem.Binding, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, true))
                    {
                        PlayerManager.ShowNotification(_Player, "Tách huyền tinh thành :" + _ItemOut.Name);

                        LogManager.WriteLog(LogTypes.Item, "[" + _Player.RoleID + "][" + _Player.RoleName + "][Tách huyền tinh TỪ TRANG BỊ] : Tách huyền tinh HUYỀN TINH :" + _ItemOut.Name);
                    }
                    strlog += $"({_ItemOut.ItemID}/{_ItemOut.Name})";
                }
                LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) tách từ trang bị được " + strlog);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hàm tách huyền tinh cấp cao ra huyền tinh cấp thấp
        /// </summary>
        /// <param name="nValue"></param>
        /// <returns></returns>
        public static List<ItemData> TotalCrystalSplitFromEquip(double nValue)
        {
            List<ItemData> _OutputCrytals = null;

            _OutputCrytals = new List<ItemData>();

            // Lấy ra toàn bộ huyền tinh theo cấp độ giảm dần
            var _CristalItem = ItemManager.TotalItem.Where(x => x.QualityPrefix == 0 && x.Category == 0 && x.Genre == 18 && x.DetailType == 1 && x.ParticularType == 1).OrderByDescending(x => x.Level).ToList();

            var FindLevel1 = _CristalItem.Where(x => x.Level == 1).FirstOrDefault();

            // Tối đa tách ra 10 huyền tinh cấp thấp hơn
            for (var nCount = 1; nCount < MAX_SPLITNUM; nCount++)
            {
                foreach (var item in _CristalItem)
                {
                    if (nValue / item.ItemValue > 1)
                    {
                        var nNum = Math.Floor(nValue / item.ItemValue);

                        if (nNum > 1)
                        {
                            for (var i = 0; i < nNum; i++)
                            {
                                // // Chỉ lấy các huyền tinh có level > 4 còn lại thì người chơi phải chịu lỗ VALUE coi như phí
                                // if (item.Level > 4)
                                {
                                    _OutputCrytals.Add(item);
                                }
                            }

                            nValue = nValue % item.ItemValue;

                            break;
                        }
                    }
                }
                if ((nValue / FindLevel1.ItemValue < 1) || nValue == 0)
                {
                    break;
                }
            }

            var SUM = _OutputCrytals.Sum(x => (long)x.ItemValue);
            Console.WriteLine(SUM);
            return _OutputCrytals;
        }

        #endregion TACHHUYENTINH_TUTRANGBI

        #region cường hóa trang bị bạch ngân/ hoàng kim
        private static bool TryParseEnhanceEndTime(string raw, out DateTime endTime)
        {
            raw = raw?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                endTime = default;
                return false;
            }

            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss",
                "dd-MM-yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "M/d/yyyy H:mm:ss",
                "M/d/yyyy HH:mm:ss",
            };

            return DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out endTime)
                   || DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.AllowWhiteSpaces, out endTime)
                   || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out endTime);
        }

        public static string DoEnhArtifactItem(List<GoodsData> TotalHuyenTinh, GoodsData Item, KPlayer _Player, MoneyType Type, bool isGM = false)
        {
            // Check xem có đủ điều kiện cường hóa không
            var _Data = ItemManager._TotalGameItem[Item.GoodsID];
            if (!ItemManager.TrangBiBachNganHoangKim(Item.GoodsID) && !_Data.IsArtifact)
            {
                PlayerManager.ShowNotification(_Player, "Trang bị phải là trang bị bạch ngân/ hoàng kim");
                return "";
            }
            var propBNHK = "";
            if (Item.OtherParams != null && Item.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var oldParam3) && !string.IsNullOrEmpty(oldParam3))
            {
                var parts = oldParam3.Split('|');
                if (parts.Length > 2)
                {
                    propBNHK = "|" + string.Join("|", parts.Skip(2));
                }

                if (parts.Length >= 2 && int.TryParse(parts[0], out var oldEnhLevel) && oldEnhLevel >= 1 && TryParseEnhanceEndTime(parts[1], out var endTime))
                {
                    if (endTime > DateTime.Now)
                    {
                        PlayerManager.ShowNotification(_Player, "Trang bị đã nâng cấp trước đó rồi, không thể nâng cấp tiếp");
                        return "";
                    }
                }
            }
            if (!isGM)
            {
                // tính toán xem nguyên liệu đặt vào đủ chưa
                int moneyNeed = 2000000;
                int nhhtNeed = 2500;
                int partItem = 10;
                int gatherMakerPointNeed = 30000;
                int crystoneNeed = 2;
                if (_Data.QualityPrefix == 7)
                {
                    moneyNeed = 3000000;
                    nhhtNeed = 6500;
                    partItem = 20;
                    gatherMakerPointNeed = 50000;
                    crystoneNeed = 4;
                }
                // nếu ko đủ bạc
                if (KTGlobal.GetMoneyhave(_Player, MoneyType.Bac) < moneyNeed)
                {
                    PlayerManager.ShowNotification(_Player, "Không đủ bạc");
                    return "";
                }

                // nếu ko đủ tinh lực hoạt lực
                if (_Player.GetGatherPoint() < gatherMakerPointNeed)
                {
                    PlayerManager.ShowNotification(_Player, "Không đủ tinh lực");
                    return "";
                }
                if (_Player.GetMakePoint() < gatherMakerPointNeed)
                {
                    PlayerManager.ShowNotification(_Player, "Không đủ hoạt lực");
                    return "";
                }

                var listCrystone = new List<GoodsData>();
                var listNHHT = new List<GoodsData>();
                var listPartEquip = new List<GoodsData>();
                int countNHHT = 0;
                int countPartEquip = 0;
                foreach (var item in TotalHuyenTinh)
                {
                    // nếu là huyền tinh cấp 10
                    if (item.GoodsID == 192)
                    {
                        listCrystone.Add(item);
                    }
                    // nếu là nhht
                    else if (item.GoodsID == 506)
                    {
                        listNHHT.Add(item);
                        countNHHT += item.GCount;
                    }
                    // nếu là mảnh trang bị
                    else
                    {
                        var _DataPart = ItemManager._TotalGameItem[item.GoodsID];
                        if (_DataPart.Level != GetLevelItem(_Data))
                        {
                            PlayerManager.ShowNotification(_Player, "Hãy đặt vào mảnh trang bị tương ứng");
                            return "";
                        }
                        listPartEquip.Add(item);
                        countPartEquip += item.GCount;
                    }
                }
                if (listCrystone.Count < crystoneNeed || countNHHT < nhhtNeed || countPartEquip < partItem)
                {
                    PlayerManager.ShowNotification(_Player, "Chưa đủ nguyên liệu");
                    return "";
                }
                // trừ nguyên liệu bạc khóa và tinh hoạt lực
                KTGlobal.SubMoney(_Player, moneyNeed, MoneyType.Bac, "EQUIP_ARTIFACT");

                _Player.ChangeCurGatherPoint(-gatherMakerPointNeed);
                _Player.ChangeCurMakePoint(-gatherMakerPointNeed);

                int countNeed = 0;
                foreach (var item in listCrystone)
                {
                    if (countNeed >= crystoneNeed)
                        break;
                    ItemManager.RemoveItem(_Player, item.Id);
                    countNeed++;
                }
                countNeed = 0;

                foreach (var item in listNHHT)
                {
                    if (countNeed >= nhhtNeed)
                        break;
                    if (nhhtNeed - countNeed >= item.GCount)
                    {
                        countNeed += item.GCount;
                        ItemManager.RemoveItemByCount(_Player, item, item.GCount, "EQUIP_ARTIFACT");

                    }
                    else
                    {
                        ItemManager.RemoveItemByCount(_Player, item, nhhtNeed - countNeed, "EQUIP_ARTIFACT");
                        countNeed = nhhtNeed;
                    }
                }

                countNeed = 0;

                foreach (var item in listPartEquip)
                {
                    if (countNeed >= partItem)
                        break;
                    if (partItem - countNeed >= item.GCount)
                    {
                        countNeed += item.GCount;
                        ItemManager.RemoveItemByCount(_Player, item, item.GCount, "EQUIP_ARTIFACT");

                    }
                    else
                    {
                        ItemManager.RemoveItemByCount(_Player, item, partItem - countNeed, "EQUIP_ARTIFACT");
                        countNeed = partItem;
                    }
                }
            }

            // thực hiện nâng cấp
            int n_Level = 1;
            if (_Data.QualityPrefix == 7)
            {
                n_Level = 2;
            }
            // lưu vào db
            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, _Player.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, Item.Id);
            TotalUpdate.Add(UPDATEITEM.BINDING, Item.Binding);
            TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, Item.Forge_level);

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            if (Item.OtherParams != null)
            {
                if (Item.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var param1)
                && Item.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var param2))
                {

                    OtherParams.Add(ItemPramenter.Pram_1, param1);
                    OtherParams.Add(ItemPramenter.Pram_2, param2);
                }

                if (Item.OtherParams.TryGetValue(ItemPramenter.Hole, out var paramHole))
                {
                    OtherParams.Add(ItemPramenter.Hole, paramHole);
                }
            }
            var now = DateTime.Now;
            var endtime = now.AddDays(30);
            OtherParams.Add(ItemPramenter.Pram_3, n_Level + "|" + endtime.ToString("yyyy-MM-dd HH:mm:ss") + propBNHK);

            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);


            string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);

            string[] dbFields = null;

            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields, _Player.ServerId);

            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                LogManager.WriteLog(LogTypes.Item, "Nâng cấp cường hóa thất bại");

                return "";
            }
            else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
            {
                // Thực hiện update lại vật phẩm trong túi đồ
                KTGlobal.ModifyGoods(_Player, Item.Id, TotalUpdate);


                LogManager.WriteLog(LogTypes.Item, "Nâng cấp thành công!");
                var endHour = endtime.Hour * 10000 + endtime.Minute * 100 + endtime.Second;

                return n_Level + "|" + endHour + "|" + Item.Id + propBNHK;
            }

            return "";
        }

        public static int GetLevelItem(ItemData itemEquip)
        {
            int level = -1;
            //nếu là phòng cụ
            var phongcu = new List<int> { 3, 7, 8, 9, 10 };
            var trangsuc = new List<int> { 4, 5, 6, 11 };
            var vukhi = new List<int> { 1, 2 };
            if (phongcu.Contains(itemEquip.DetailType))
            {
                level = 1;
            }
            else if (trangsuc.Contains(itemEquip.DetailType))
            {
                level = 2;
            }
            else if (vukhi.Contains(itemEquip.DetailType))
            {
                level = 3;
            }
            return level;
        }

        public class ArtifactInfo
        {
            public int GoodID { get; set; }

            public string Param3 { get; set; }

            public List<int> Skills { get; set; }
        }
        #endregion

        #region nâng cấp trang bị bạch ngân/ hoàng kim
        /// <summary>
        /// Thực hiện nâng cấp trang bị bạch ngân hoàng kim
        /// </summary>
        /// <param name="TotalHuyenTinh"></param>
        /// <param name="Item"></param>
        /// <param name="_Player"></param>
        /// <returns></returns>
        public static string DoUpArtifactItem(List<GoodsData> TotalHuyenTinh, GoodsData Item, KPlayer _Player)
        {
            var param = "";

            // Check xem có đủ điều kiện cường hóa không
            var _Data = ItemManager._TotalGameItem[Item.GoodsID];
            if (!_Data.IsArtifact && !(Item.GoodsID >= 11244 && Item.GoodsID <= 11383) && !(Item.GoodsID >= 12752 && Item.GoodsID <= 12775) && !ItemManager.TrangBiBachNganHoangKim(Item.GoodsID))
            {
                PlayerManager.ShowNotification(_Player, "Trang bị phải là trang bị bạch ngân/ hoàng kim");
                return param;
            }


            int NHHT = 0;
            int NAT = 0;
            int TLDL = 0;
            int HTB = 0;
            int MKNTB = 0;
            foreach (var item in TotalHuyenTinh)
            {
                if (item.GoodsID == 506)
                {
                    NHHT += item.GCount;
                }
                else if (item.GoodsID == 928)
                {
                    NAT += item.GCount;
                }
                else if (item.GoodsID == 819)
                {
                    HTB += item.GCount;
                }
                else if (item.GoodsID >= 1886 && item.GoodsID <= 1894)
                {
                    TLDL += item.GCount;
                }
                else if (item.GoodsID >= 25299 && item.GoodsID <= 25301)
                {
                    MKNTB += item.GCount;
                }
            }

            var levelItem = GetItemLevel(_Data);

            var levelUp = 1;
            if (_Data.QualityPrefix == 7)
                levelUp = 2;

            var infoUp = ItemManager.ArtifactUpInfo.Where(x => x.LevelItem == levelItem && x.LevelUp == levelUp).FirstOrDefault();

            var NHHTInfo = infoUp.ItemInput.Where(x => x.ItemID == 506).FirstOrDefault();
            if (NHHT < NHHTInfo.ItemCount)
                return param;

            var NATInfo = infoUp.ItemInput.Where(x => x.ItemID == 928).FirstOrDefault();
            if (NAT < NATInfo.ItemCount)
                return param;

            var MKNTBInfo = infoUp.ItemInput.Where(x => x.ItemID >= 25299 && x.ItemID <= 25301).FirstOrDefault();
            if (MKNTB < MKNTBInfo.ItemCount)
            {
                return param;
            }

            if (levelItem > 0)
            {
                var TLDLTInfo = infoUp.ItemInput.Where(x => x.ItemID >= 1886 && x.ItemID <= 1894).FirstOrDefault();
                if (TLDL < TLDLTInfo.ItemCount)
                    return param;
            }
            else
            {
                var HTBTInfo = infoUp.ItemInput.Where(x => x.ItemID == 819).FirstOrDefault();
                if (HTB < HTBTInfo.ItemCount)
                    return param;
            }

            // trừ nguyên liệu
            foreach (var item in infoUp.ItemInput)
            {
                var listItem = TotalHuyenTinh.Where(x => x.GoodsID == item.ItemID).ToList();
                int count = item.ItemCount;
                while (count > 0)
                {
                    foreach (var ItemGD in listItem)
                    {
                        if (ItemGD.GCount < count)
                        {
                            count -= ItemGD.GCount;
                            ItemManager.RemoveItemByCount(_Player, ItemGD, ItemGD.GCount, "EQUIP_ARTIFACT");
                        }    
                        else
                        {
                            ItemManager.RemoveItemByCount(_Player, ItemGD, count, "EQUIP_ARTIFACT");
                            count = 0;
                        }    
                    }    
                }    
            }
            
            // nâng cấp
            
            if (Item.OtherParams != null)
            {
                if (Item.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                {
                    var arrParam = param3.Split('|');
                    if (arrParam.Length >= 3)
                    {
                        PlayerManager.ShowNotification(_Player, "Trang bị đã nâng cấp trước đó!");
                        return param;
                    }
                    foreach (var prop in infoUp.PropInfo)
                    {
                        param3 += "|" + prop.BasicPropType + ";" + prop.BasicPropValue;
                    }
                    param = param3;
                }    
                else
                {
                    param = "0|0";
                    foreach (var prop in infoUp.PropInfo)
                    {
                        param += "|" + prop.BasicPropType + ";" + prop.BasicPropValue;
                    }
                }    
            }
            else
            {
                param = "0|0";
                foreach (var prop in infoUp.PropInfo)
                {
                    param += "|" + prop.BasicPropType + ";" + prop.BasicPropValue;
                }
            }    
            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, _Player.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, Item.Id);
            TotalUpdate.Add(UPDATEITEM.BINDING, Item.Binding);
            TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, Item.Forge_level);

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            if (Item.OtherParams != null)
            {
                if (Item.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var param1)
                && Item.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var param2))
                {

                    OtherParams.Add(ItemPramenter.Pram_1, param1);
                    OtherParams.Add(ItemPramenter.Pram_2, param2);
                }

                if (Item.OtherParams.TryGetValue(ItemPramenter.Hole, out var paramHole))
                {
                    OtherParams.Add(ItemPramenter.Hole, paramHole);
                }

                OtherParams.Add(ItemPramenter.Pram_3, param);
            }
            else
            {
                OtherParams.Add(ItemPramenter.Pram_3, param);
            }    
            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);


            string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);

            string[] dbFields = null;

            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields, _Player.ServerId);

            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
                LogManager.WriteLog(LogTypes.Item, "Nâng cấp cường hóa thất bại");

                return "";
            }

            KTGlobal.SendSystemEventNotification($"{_Player.RoleName} dốc hết vốn liếng mang Tiểu Du Long Danh Vọng Lệnh và trang bị Bạch Ngân / Hoàng Kim đến gặp Đại Sư,nâng cấp thuộc tính anh hùng cho [{Item.ItemName}]. Thật đáng khâm phục!!");

            KTGlobal.ModifyGoods(_Player, Item.Id, TotalUpdate);

            return param;
        }

        /// <summary>
        /// Lấy thông tin trang bị sau khi nâng cấp
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="_Player"></param>
        /// <returns></returns>
        public static string GetInfoUpArtifactItem(GoodsData Item, KPlayer _Player)
        {
            var result = "";

            // Check xem có đủ điều kiện cường hóa không
            var _Data = ItemManager._TotalGameItem[Item.GoodsID];
            if (!_Data.IsArtifact && !(Item.GoodsID >= 11244 && Item.GoodsID <= 11383) && !(Item.GoodsID >= 12752 && Item.GoodsID <= 12775) && !ItemManager.TrangBiBachNganHoangKim(Item.GoodsID))
            {
                PlayerManager.ShowNotification(_Player, "Trang bị phải là trang bị bạch ngân/ hoàng kim");
                return "";
            }

            if (Item.OtherParams != null)
            {
                if(Item.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                {
                    var strParam3 = param3.Split('|');
                    if (strParam3.Length > 2)
                    {
                        PlayerManager.ShowNotification(_Player, "Trang bị đã nâng cấp trước đó");
                        return result;
                    }
                }
            }

            var _ItemData = ItemManager._TotalGameItem[Item.GoodsID];

            var levelUp = 1;
            if (_ItemData.QualityPrefix == 7)
                levelUp = 2;
            var levelItem = GetItemLevel(_ItemData);
            var infoUp = ItemManager.ArtifactUpInfo.Where(x => x.LevelItem == levelItem && x.LevelUp == levelUp).FirstOrDefault();
            
            if (infoUp != null)
            {
                result += $"{levelItem};{levelUp};";
                foreach (var input in infoUp.ItemInput)
                {
                    result += $"{input.ItemID}|{input.ItemCount}&";
                }
                result = result.TrimEnd('&');
                result += ";";
                foreach (var prop in infoUp.PropInfo)
                {
                    result += $"{prop.BasicPropType}|{prop.BasicPropValue}&";
                }
                result = result.TrimEnd('&');
            }
            return result;    
        }

       public static int GetItemLevel(ItemData Item)
        {
            int level = 0;
            switch (Item.DetailType)
            {
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_pendant:
                    level = 9;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_armor:
                    level = 3;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_belt:
                    level = 4;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_boots:
                    level = 5;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_cuff:
                    level = 8;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_necklace:
                    level = 6;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_ring:
                    level = 7;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_amulet:
                    level = 1;
                    break;
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_helm:
                    level = 2;
                    break;
            }
            return level;
        }

        #endregion
    }
}