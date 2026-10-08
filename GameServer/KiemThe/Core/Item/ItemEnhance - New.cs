using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Server;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;

namespace GameServer.KiemThe.Core.Item
{
    /// <summary>
    /// Class cường hóa vật phẩm
    /// </summary>
    public static partial class ItemEnhance
    {
        private static AdvanceEnhanceCondition _advanceEnhanceCondition;

        public static void Init()
        {
            var file = Global.GameResPath("Config/KT_Interactive/AdvanceEnhance.xml");
            var serializer = new XmlSerializer(typeof(AdvanceEnhanceCondition));
            using (var stream = File.OpenRead(file))
            {
                _advanceEnhanceCondition = serializer.Deserialize(stream) as AdvanceEnhanceCondition;
            }
        } 

        /// <summary>
        /// Thực hiện cường hóa
        /// </summary>
        /// <param name="TotalHuyenTinh"></param>
        /// <param name="Item"></param>
        /// <param name="_Player"></param>
        /// <param name="MoneyType"></param>
        public static int DoAdvanceEnhItem(List<GoodsData> TotalHuyenTinh, GoodsData Item, KPlayer _Player, MoneyType Type)
        {
            // Check xem có đủ điều kiện cường hóa không
            if (!CheckEnhItem(Item, _Player))
            {
                return -1;
            }

            var _Caclution = GetAdvanceEnhItem(TotalHuyenTinh, Item);

            // Nếu tỉ lệ quá thấp
            if (_Caclution.nTrueProb < 100)
            {
                PlayerManager.ShowNotification(_Player, "Tỷ lệ cường hóa cần đạt 100%");
                return -1;
            }

            int ticketID = GetEnhanceTicketID(Item);

            if (ticketID == -1)
            {
                PlayerManager.ShowNotification(_Player, "Sai vé");
                return -1;
            }

            int ticketNeeded = GetEnhanceTicketNeeded(Item);
            int ticketHave = GetEnhanceTicketHave(Item, _Player); 

            if (ticketHave < ticketNeeded)
            {
                PlayerManager.ShowNotification(_Player, "Không đủ vé cần thiết");
                return -1;
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

            //ItemManager.RemoveItemByCount(_Player, Ticket, ticketNeeded, "Cường hóa vật phẩm");

            int boundCount = ItemManager.GetItemCountInBag(_Player, ticketID, 1);
            int unboundCount = ItemManager.GetItemCountInBag(_Player, ticketID, 0);

            if (boundCount > 0)
            {
                int boundToRemove = boundCount > ticketNeeded ? ticketNeeded : boundCount;
                ItemManager.RemoveItemFromBag(_Player, ticketID, boundToRemove, 1, "Cường hóa vật phẩm");
                
                int remainingNeeded = ticketNeeded - boundToRemove;
                if (remainingNeeded > 0)
                {
                    if (unboundCount < remainingNeeded)
                    {
                        PlayerManager.ShowNotification(_Player, "Không đủ vé cường hóa");
                        return -1;
                    }
                    ItemManager.RemoveItemFromBag(_Player, ticketID, remainingNeeded, 0, "Cường hóa vật phẩm");
                }
            }
            else
            {
                if (unboundCount < ticketNeeded)
                {
                    PlayerManager.ShowNotification(_Player, "Không đủ vé cường hóa");
                    return -1;
                }
                ItemManager.RemoveItemFromBag(_Player, ticketID, ticketNeeded, 0, "Cường hóa vật phẩm");
            }

            LogManager.WriteLog(LogTypes.Enhance, "Người chơi [" + _Player.RoleID + "][" + _Player.RoleName + "][" + Item.GoodsID + "][" + Item.Id + "] mang " + ItemManager.GetNameItem(Item) + " cường hóa cao cấp :[" + (Item.Forge_level + 1) + "] với tỉ lệ  :" + _Caclution.nProb);

            PlayerManager.ShowNotification(_Player, "Cường hóa thành công");

            // Cường hóa thành công
            ItemManager.SetEquipForgeLevel(Item, _Player, Item.Forge_level + 1, LOCK);

            var ShowHang = "Người chơi " + KTGlobal.CreateStringByColor(_Player.RoleName, ColorType.Green) + " với " + KTGlobal.CreateStringByColor(_Caclution.nProb + "%", ColorType.VanillaIce) + " đem " + KTGlobal.GetItemDescInfoStringForChat(Item) + " cường hóa lên " + KTGlobal.CreateStringByColor("+" + Item.Forge_level + "", ColorType.VanillaIce) + " thành công!";
            KTGlobal.SendSystemEventNotification(ShowHang, new List<GoodsData>() { Item });

            LogManager.WriteLog(LogTypes.Crystals, $"({_Player.RoleID}/{_Player.RoleName}) cường hóa cao cấp thành công ({Item.Id}/{Item.GoodsID}/{Item.ItemName}) lên [{Item.Forge_level}] dùng" + strlog);

            ItemManager._TotalGameItem.TryGetValue(Item.GoodsID, out var itemData);

            ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player, -1, Item.Id, Item.GoodsID, TaskTypes.Enhance);

            // Nếu là vũ khí
            if (ItemManager.KD_ISWEAPON(itemData.DetailType))
            {
                if (Item.Forge_level >= 4)
                {
                    //SideTaskManager.getInstance().ProcessTask(_Player, (int)SideTaskName.Enhance4);
                }
            }
            if (!ItemManager.KD_ISORNAMENT(itemData.DetailType) && !ItemManager.KD_ISWEAPON(itemData.DetailType))
            {
                if (Item.Forge_level >= 2)
                {
                    //SideTaskManager.getInstance().ProcessTask(_Player, (int)SideTaskName.Enhance2);
                }
            }
            return 1;
        }

        private static int GetEnhanceTicketID(GoodsData Item)
        {
            ItemManager._TotalGameItem.TryGetValue(Item.GoodsID, out var itemData);
            if (itemData == null)
                return -1;

            switch (itemData.DetailType)
            {
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon:
                    return _advanceEnhanceCondition.EnhanceTickets.WeaponEnhanceTicket.ID;
                    
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_armor:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_boots:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_belt:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_helm:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_cuff:
                    return _advanceEnhanceCondition.EnhanceTickets.ArmorEnhanceTicket.ID;
                    
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_ring:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_necklace:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_amulet:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_pendant:
                    return _advanceEnhanceCondition.EnhanceTickets.AccessoryEnhanceTicket.ID;
            }

            return -1;
        }

        private static int GetEnhanceTicketNeeded(GoodsData Item)
        {
            ItemManager._TotalGameItem.TryGetValue(Item.GoodsID, out var itemData);

            var nextLevel = Item.Forge_level + 1;

            switch (itemData.DetailType)
            {
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon:
                    {
                        return _advanceEnhanceCondition.Weapons.EnhancementLevels.Levels.FirstOrDefault(x => x.Level == nextLevel)?.EnhanceTicket ?? -1; 
                    }
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_armor:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_boots:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_belt:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_helm:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_cuff:
                    {
                        return _advanceEnhanceCondition.Equipment.EnhancementLevels.Levels.FirstOrDefault(x => x.Level == nextLevel)?.EnhanceTicket ?? -1;
                    }
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_ring:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_necklace:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_amulet:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_pendant:
                    {
                        return _advanceEnhanceCondition.Equipment.EnhancementLevels.Levels.FirstOrDefault(x => x.Level == nextLevel)?.EnhanceTicket ?? -1;
                    }
            }

            return -1;
        }

        private static int GetEnhanceTicketHave(GoodsData Item, KPlayer player)
        {
            int ticketId = -1;

            ItemManager._TotalGameItem.TryGetValue(Item.GoodsID, out var itemData);
            if (itemData == null)
                return -1;

            switch (itemData.DetailType)
            {
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon:
                    ticketId = _advanceEnhanceCondition.EnhanceTickets.WeaponEnhanceTicket.ID;
                    break;
                    
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_armor:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_boots:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_belt:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_helm:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_cuff:
                    ticketId = _advanceEnhanceCondition.EnhanceTickets.ArmorEnhanceTicket.ID;
                    break;
                    
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_ring:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_necklace:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_amulet:
                case (int)KE_ITEM_EQUIP_DETAILTYPE.equip_pendant:
                    ticketId = _advanceEnhanceCondition.EnhanceTickets.AccessoryEnhanceTicket.ID;
                    break;
            }

            return ItemManager.GetItemCountInBag(player, ticketId);
        }

        public static CalcProb GetAdvanceEnhItem(List<GoodsData> TotalHuyenTinh, GoodsData Item)
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

            if (CurLevel < 16)
            {
                return _Prob;
            }

            var NextLevel = CurLevel + 1;

            long NeedValue = 0;
            var Money = 0;

            if (ItemManager.KD_ISWEAPON(_ItemSelect.DetailType))
            {
                var levelReq = _advanceEnhanceCondition.Weapons.EnhancementLevels.Levels.FirstOrDefault(x => x.Level == NextLevel);
                if (levelReq != null)
                {
                    NeedValue = levelReq.CrystalValue;
                    Money = levelReq.SilverCost * 10000;
                }
            }
            else
            {
                var levelReq = _advanceEnhanceCondition.Equipment.EnhancementLevels.Levels.FirstOrDefault(x => x.Level == NextLevel);
                if (levelReq != null)
                {
                    NeedValue = levelReq.CrystalValue;
                    Money = levelReq.SilverCost * 10000;
                }
            }

            var nProb = Math.Floor((double)AllValue / NeedValue * 100);

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


        public class AdvanceEnhanceCondition
        {
            [XmlElement("Equipment")]
            public ItemCategory Equipment { get; set; }

            [XmlElement("Weapons")]
            public ItemCategory Weapons { get; set; }

            [XmlElement("EnhanceTickets")]
            public EnhanceTickets EnhanceTickets { get; set; }
        }

        public class ItemCategory
        {
            [XmlElement("EnhancementLevels")]
            public EnhancementLevels EnhancementLevels { get; set; }
        }

        public class EnhancementLevels
        {
            [XmlElement("Level")]
            public List<LevelRequirement> Levels { get; set; }
        }

        public class LevelRequirement
        {
            [XmlAttribute("Value")]
            public int Level { get; set; }

            [XmlElement("EnhanceTicket")]
            public int EnhanceTicket { get; set; }

            [XmlElement("CrystalValue")]
            public int CrystalValue { get; set; }

            [XmlElement("SilverCost")]
            public int SilverCost { get; set; }
            }

        public class EnhanceTickets
        {
            [XmlElement("WeaponEnhanceTicket")]
            public EnhanceTicket WeaponEnhanceTicket { get; set; }

            [XmlElement("ArmorEnhanceTicket")]
            public EnhanceTicket ArmorEnhanceTicket { get; set; }

            [XmlElement("AccessoryEnhanceTicket")]
            public EnhanceTicket AccessoryEnhanceTicket { get; set; }
        }

        public class EnhanceTicket
        {
            [XmlAttribute("ID")]
            public int ID { get; set; }
        }

        
    }
}