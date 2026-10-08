
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.Logic;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.Logic.Manager.Pet
{
    /// <summary>
    /// Hàm quản lý tặng quà cho pet
    /// </summary>
    public static class PetGiftManager
    {    /// <summary>
         /// Tổng giá trị lĩnh ngộ của pet theo từ level dùng để tính % giá trị của quà
         /// </summary>
        public static readonly Dictionary<int, int> PetValueLinhNgo = new Dictionary<int, int>();
        /// <summary>
        /// Danh sách quà có thể tặng
        /// </summary>
        public static readonly Dictionary<int, List<GiftValue>> ItemValueGift = new Dictionary<int, List<GiftValue>>();

        /// <summary>
        /// kiểm tra xem có phải quà có thể tặng không
        /// </summary>
        public static bool IsGift(int ID)
        {
            if (ItemValueGift.ContainsKey(ID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tiến hành tặng quà
        /// </summary>
        public static int DoSendGift(List<GoodsData> TotalGift, GoodsData goodsData, KPlayer _Player)
        {

            int linhNgo = 0;
            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd))
                {
                    var data = strcmd.Split('|');
                    linhNgo = Int32.Parse(data[2]);
                }
            }
            catch 
            {
                return -2;
            }
            // Tính toán điểm LN tăng
            int _Caclution = GetValueGift(TotalGift, goodsData);
            if(_Caclution > 0)
            {
                if(_Caclution + linhNgo > 100)
                {
                    PlayerManager.ShowNotification(_Player, "Quà tặng đặt vào quá nhiều, xin đừng lãng phí!");
                    return -1;
                }

                foreach (GoodsData _gift in TotalGift)
                {
                    // Thực hiện xóa tất cả quà
                    ItemManager.AbandonItem(_gift, _Player, false, $"Tặng quà cho pet :{goodsData.Id}");
                }

                PetManager.UpdateLinhNgoPet(_Player, goodsData.Id, _Caclution);
                PlayerManager.ShowNotification(_Player, "Tặng quà cho bạn đồng hành thành công!");

                return 1;

            }
            else
            {
                PlayerManager.ShowNotification(_Player, "Quà tặng giá trị quá thấp hãy thêm phần quà!");
                return -1;
            }
        }
        public static int GetNumberSkill(GoodsData Item)
        {
            if (Item.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill))
            {
                string[] fields = levelSkill.Split('|');
                return fields.Length;
            }
            return 0;
        }

        /// <summary>
        /// Tính toán điểm lĩnh ngộ sẽ tăng khi tặng
        /// </summary>
        private static int GetValueGift(List<GoodsData> TotalGift, GoodsData Item)
        {
            // giá trị của quà
            int totalvalue = 0;
           
            foreach(GoodsData Gift in TotalGift)
            {
                if (!IsGift(Gift.GoodsID))
                    return -1;

                var giftConfig = ItemValueGift;
                if (PetManager.IsYenTieuLau(Item.GoodsID)) 
                {
                    giftConfig = PetManager.YenTieuLauOverride.ItemValueGift;
                }
                var gift = giftConfig[Gift.GoodsID].FirstOrDefault(x => x.numberSkill == GetNumberSkill(Item));
                if (gift == null)
                    return -1;
                totalvalue += gift.Value;
            }

            // Tổng giá trị
            float valueNeed = 0;

            foreach(var dt in PetValueLinhNgo.Keys.ToList())
            {
                if(Item.Forge_level < dt)
                {
                    valueNeed = PetValueLinhNgo[dt];
                    break;
                }
            }

            if(valueNeed > 0)
            {
                float current = (float)totalvalue / valueNeed;
                totalvalue = (int)(current * 100);
            }

            return totalvalue;
        }
    }
}
