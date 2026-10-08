using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    public sealed partial class KuaFuBalanceManager
    {
        /// <summary>
        /// Xóa bỏ items mua trong LSV cân bằng (thuốc, đồ ăn...) khi rời balance.
        /// Player được cấp tiền miễn phí trong balance nên items không được giữ lại.
        /// Chỉ lọc items site=kuafubalancebag, không ảnh hưởng items LSV thường.
        /// </summary>
        private void CleanUpBalancePurchasedItems(KPlayer player, KuaFuBalanceSession session)
        {
            if (player.GoodsDataList == null)
            {
                return;
            }

            // Tìm items trong kuafubalancebag không thuộc temp items
            List<GoodsData> itemsToCleanup;
            int totalBagItems = 0;
            int balanceBagItems = 0;
            lock (player.GoodsDataList)
            {
                totalBagItems = player.GoodsDataList.Count(x => x != null && x.GCount > 0);
                balanceBagItems = player.GoodsDataList.Count(x => x != null && x.GCount > 0 && x.Site == (int)SiteItemType.kuafubalancebag);
                itemsToCleanup = player.GoodsDataList
                    .Where(x => x != null &&
                        x.GCount > 0 &&
                        x.Site == (int)SiteItemType.kuafubalancebag &&
                        !session.TempItemDbIds.Contains(x.Id))
                    .ToList();
            }

            if (itemsToCleanup.Count <= 0)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance,
                    string.Format("[KuaFuBalance] CleanUp: nothing to remove. RoleID={0}, TotalBagItems={1}, BalanceBagItems={2}",
                    player.RoleID, totalBagItems, balanceBagItems));
                return;
            }

            LogManager.WriteLog(LogTypes.KuaFuBalance,
                string.Format("[KuaFuBalance] CleanUp: found {0} items to remove. RoleID={1}, TotalBagItems={2}, BalanceBagItems={3}, TempItems={4}",
                itemsToCleanup.Count, player.RoleID, totalBagItems, balanceBagItems, session.TempItemDbIds.Count));

            int removedCount = 0;
            foreach (GoodsData item in itemsToCleanup)
            {
                GoodsData latestItem = Global.GetGoodsByDbID(player, item.Id);
                if (latestItem == null || latestItem.GCount <= 0)
                {
                    continue;
                }

                // Tháo trang bị nếu đang mặc
                if (latestItem.Using >= 0)
                {
                    if (!UnloadOriginalEquipment(player, latestItem))
                    {
                        LogManager.WriteLog(LogTypes.KuaFuBalance,
                            string.Format("[KuaFuBalance] Cannot unload balance-purchased item before remove. RoleID={0}, ItemDBID={1}",
                            player.RoleID, latestItem.Id));
                        continue;
                    }

                    latestItem = Global.GetGoodsByDbID(player, item.Id);
                    if (latestItem == null || latestItem.GCount <= 0)
                    {
                        continue;
                    }
                }

                ItemManager.AbandonItem(latestItem, player, false, "[KuaFuBalance]");
                removedCount++;
            }

            if (removedCount > 0)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance,
                    string.Format("[KuaFuBalance] Removed {0} balance-purchased items on leave. RoleID={1}",
                    removedCount, player.RoleID));
            }
        }
    }
}
