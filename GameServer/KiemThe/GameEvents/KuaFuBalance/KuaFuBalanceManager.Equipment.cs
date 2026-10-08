using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.GuildWarManager;
using GameServer.KiemThe.GameEvents.LeagueBattle;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using Newtonsoft.Json;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    public sealed partial class KuaFuBalanceManager
    {
        public bool ReceiveTemporaryEquipment(KPlayer player)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            int seriesId = GetCurrentSeries(player);
            int factionId = player.m_cPlayerFaction.GetFactionId();
            int routeId = player.m_cPlayerFaction.GetRouteId();
            if (seriesId <= 0)
            {
                PlayerManager.ShowNotification(player, "Bạn cần chọn phái/nhánh tạm trước khi nhận set đồ.");
                return false;
            }

            KuaFuBalanceEquipSet equipSet = GetEquipSet(seriesId, factionId, routeId);
            if (equipSet == null || equipSet.Items.Count <= 0)
            {
                PlayerManager.ShowNotification(player, "Chưa có bộ trang bị cân bằng cho phái/nhánh này.");
                return false;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));
            HashSet<int> oldTempItemDbIds = new HashSet<int>(session.TempItemDbIds);

            session.TempSeriesId = seriesId;

            if (!PrepareCurrentEquipment(player, session))
            {
                return false;
            }

            List<GoodsData> createdItems = CreateTemporaryItems(player, equipSet);
            int expectedItemCount = CountActiveItems(equipSet, player.RoleSex);
            if (createdItems == null || createdItems.Count != expectedItemCount)
            {
                RollbackReceiveEquipmentFailure(player, session, oldTempItemDbIds);
                PlayerManager.ShowNotification(player, "Cấp trang bị cân bằng thất bại, vui lòng thử lại.");
                return false;
            }

            if (!config.KeepPreviousTemporaryEquipment)
            {
                session.TempItemDbIds.Clear();
            }

            HashSet<int> newTempItemDbIds = new HashSet<int>();
            foreach (GoodsData item in createdItems)
            {
                session.TempItemDbIds.Add(item.Id);
                newTempItemDbIds.Add(item.Id);
            }

            if (config.AutoEquipTemporaryEquipment && !EquipTemporaryItems(player, createdItems))
            {
                RemoveTemporaryItemsByDbIds(player, newTempItemDbIds);
                session.TempItemDbIds.Clear();
                RollbackReceiveEquipmentFailure(player, session, oldTempItemDbIds);
                PlayerManager.ShowNotification(player, "Mặc trang bị cân bằng thất bại, vui lòng kiểm tra lại set cấu hình.");
                return false;
            }

            if (!config.KeepPreviousTemporaryEquipment)
            {
                RemoveTemporaryItemsByDbIds(player, oldTempItemDbIds);
            }

            session.HasBlockedTemporaryProfileRestore = false;
            ApplyWingBuff(player, session);

            string notifyMessage = config.AutoEquipTemporaryEquipment ? "Đã cấp và mặc trang bị cân bằng." : "Đã cấp trang bị cân bằng.";
            PlayerManager.ShowNotification(player, notifyMessage);
            return true;
        }

        public bool DeleteAllTemporaryEquipment(KPlayer player)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            if (!sessions.TryGetValue(player.RoleID, out KuaFuBalanceSession session) || session.TempItemDbIds.Count <= 0)
            {
                PlayerManager.ShowNotification(player, "Không có trang bị tạm để xóa.");
                return false;
            }

            HashSet<int> targetItemDbIds = new HashSet<int>(session.TempItemDbIds);
            HashSet<int> bagItemDbIds = GetExistingBagGoodsDbIds(player, targetItemDbIds);
            if (bagItemDbIds.Count <= 0)
            {
                PlayerManager.ShowNotification(player, "Không có trang bị tạm trong túi để xóa.");
                return false;
            }

            RemoveTemporaryItemsByDbIds(player, bagItemDbIds);

            HashSet<int> remainingItemDbIds = GetExistingGoodsDbIds(player, targetItemDbIds);
            session.TempItemDbIds.Clear();
            foreach (int itemDbId in remainingItemDbIds)
            {
                session.TempItemDbIds.Add(itemDbId);
            }

            int removedCount = bagItemDbIds.Count - GetExistingGoodsDbIds(player, bagItemDbIds).Count;
            if (removedCount <= 0)
            {
                PlayerManager.ShowNotification(player, "Xóa trang bị tạm thất bại, vui lòng thử lại.");
                return false;
            }

            PlayerManager.ShowNotification(player, string.Format("Đã xóa {0} trang bị tạm.", removedCount));
            return true;
        }

        public bool AddTemporarySessionItem(KPlayer player, int itemId, int number, int series, int binding, int enhanceLevel = 0, int expireMinutes = -1)
        {
            if (!CanUseBalance(player) || itemId <= 0 || number <= 0)
            {
                return false;
            }

            int spaceNeed = ItemManager.TotalSpaceNeed(itemId, number);
            if (!KTGlobal.IsHaveSpace(spaceNeed, player))
            {
                PlayerManager.ShowNotification(player, "Túi đồ không đủ chỗ");
                return false;
            }

            ItemData itemData = ItemManager.GetItemTemplate(itemId);
            if (itemData == null)
            {
                PlayerManager.ShowNotification(player, "Vật phẩm tạm không tồn tại.");
                return false;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));
            SnapshotTemporaryBookSkills(player, session, itemData);

            string endTime = expireMinutes >= 0 ? DateTime.Now.AddMinutes(expireMinutes).ToString("yyyy-MM-dd HH:mm:ss") : Global.ConstGoodsEndTime;
            HashSet<int> beforeIds = GetGoodsDbIds(player);

            bool ok = ItemManager.CreateItem(
                Global._TCPManager.TcpOutPacketPool,
                player,
                itemId,
                number,
                (int)SiteItemType.kuafubalancebag,
                SourceName,
                false,
                binding,
                false,
                endTime,
                "",
                series,
                "",
                enhanceLevel,
                0,
                true);

            if (!ok)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi add vật phẩm");
                return false;
            }

            List<GoodsData> createdItems = FindCreatedGoodsList(player, beforeIds, itemId);
            if (createdItems.Count <= 0)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Cannot register temporary session item. RoleID={0}, ItemID={1}, Count={2}", player.RoleID, itemId, number));
                PlayerManager.ShowNotification(player, "Cấp vật phẩm tạm thất bại, vui lòng thử lại.");
                return false;
            }

            foreach (GoodsData item in createdItems)
            {
                session.TempItemDbIds.Add(item.Id);
            }

            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Created temp item. RoleID={0}, ItemID={1}, Count={2}, Site=4, DbIds={3}",
                player.RoleID, itemId, number, string.Join(",", createdItems.Select(x => x.Id.ToString()).ToArray())));
            session.HasBlockedTemporaryProfileRestore = false;
            return true;
        }

        public bool SetTemporaryBookLevelAndExp(KPlayer player, int level, int exp)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            if (!sessions.TryGetValue(player.RoleID, out KuaFuBalanceSession session))
            {
                PlayerManager.ShowNotification(player, "Bạn chưa có phiên cân bằng tạm.");
                return false;
            }

            GoodsData temporaryBook = GetEquippedTemporaryBook(player, session);
            if (temporaryBook == null)
            {
                PlayerManager.ShowNotification(player, "Cần mặc mật tịch tạm trước khi nhận Max EXP.");
                return false;
            }

            ItemData bookData = ItemManager.GetItemTemplate(temporaryBook.GoodsID);
            if (bookData == null || bookData.BookProperty == null)
            {
                PlayerManager.ShowNotification(player, "Mật tịch tạm không hợp lệ.");
                return false;
            }

            SnapshotTemporaryBookSkills(player, session, bookData);

            level = Math.Min(100, level);
            Dictionary<UPDATEITEM, object> totalUpdate = new Dictionary<UPDATEITEM, object>();
            totalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
            totalUpdate.Add(UPDATEITEM.ITEMDBID, temporaryBook.Id);

            Dictionary<ItemPramenter, string> otherParams = new Dictionary<ItemPramenter, string>();
            otherParams.Add(ItemPramenter.Pram_1, level + "");
            otherParams.Add(ItemPramenter.Pram_2, exp + "");
            otherParams.Add(ItemPramenter.Pram_3, "0");
            totalUpdate.Add(UPDATEITEM.OTHER_PRAM, otherParams);

            int expAdded = ItemManager._TotalExpBook.Where(x => x.Level <= level && x.BookLevel == bookData.Level).Sum(x => x.Exp);
            if (!ItemManager.UpdateItemPrammenter(totalUpdate, temporaryBook, player, "KuaFuBalance_SetBookLevelAndExp", true))
            {
                PlayerManager.ShowNotification(player, "Cập nhật mật tịch tạm thất bại.");
                return false;
            }

            foreach (int skillId in GetBookSkillIds(bookData))
            {
                player.Skills.AddSkillExp(skillId, expAdded);
            }

            return true;
        }

        private void RollbackReceiveEquipmentFailure(KPlayer player, KuaFuBalanceSession session, HashSet<int> oldTempItemDbIds)
        {
            if (oldTempItemDbIds.Count > 0)
            {
                session.TempItemDbIds.Clear();
                foreach (int itemDbId in oldTempItemDbIds)
                {
                    session.TempItemDbIds.Add(itemDbId);
                }

                return;
            }

            RestoreOriginalEquipment(player, session);
        }

        private bool PrepareCurrentEquipment(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.OriginalEquipDbIds.Count > 0)
            {
                ApplyServerLimitEquipPositions(player, session);
                return true;
            }

            List<GoodsData> equippedItems = GetEquippedNonBalanceItems(player, session);
            if (equippedItems.Count <= 0)
            {
                return true;
            }

            foreach (GoodsData item in equippedItems)
            {
                int originalUsing = item.Using;
                session.OriginalEquipDbIds.Add(item.Id);
                session.OriginalEquipPositions[item.Id] = originalUsing;
            }

            ApplyServerLimitEquipPositions(player, session);
            return true;
        }

        private void ApplyServerLimitEquipPositions(KPlayer player, KuaFuBalanceSession session)
        {
            HashSet<int> limitedPositions = GetServerLimitedEquipPositions();
            if (limitedPositions.Count <= 0)
            {
                return;
            }

            KPlayerEquipBody equipBody = player.GetPlayEquipBody();
            if (equipBody == null)
            {
                return;
            }

            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from ApplyServerLimitEquipPositions() in KuaFuBalanceManager.Equipment.cs");
            equipBody.ClearAllEffecEquipBody();

            HideOriginalEquipmentFromClient(player, session, limitedPositions);

            lock (equipBody.EquipBody)
            {
                foreach (KE_EQUIP_POSITION position in equipBody.EquipBody
                    .Where(x => limitedPositions.Contains((int)x.Key) && !session.TempItemDbIds.Contains(x.Value._GoodDatas.Id))
                    .Select(x => x.Key)
                    .ToList())
                {
                    equipBody.EquipBody.Remove(position);
                }
            }

            lock (equipBody.PreventiveEquipBody)
            {
                foreach (KE_EQUIP_POSITION_SUB position in equipBody.PreventiveEquipBody
                    .Where(x => limitedPositions.Contains((int)x.Key) && !session.TempItemDbIds.Contains(x.Value._GoodDatas.Id))
                    .Select(x => x.Key)
                    .ToList())
                {
                    equipBody.PreventiveEquipBody.Remove(position);
                }
            }

            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from ApplyServerLimitEquipPositions() in KuaFuBalanceManager.Equipment.cs");
            equipBody.AttackAllEquipBody();
        }

        private void HideOriginalEquipmentFromClient(KPlayer player, KuaFuBalanceSession session, HashSet<int> limitedPositions)
        {
            if (player.GoodsDataList == null || session.OriginalEquipDbIds.Count <= 0)
            {
                return;
            }

            List<GoodsData> items;
            lock (player.GoodsDataList)
            {
                items = player.GoodsDataList
                    .Where(x => x != null &&
                        x.GCount > 0 &&
                        session.OriginalEquipDbIds.Contains(x.Id) &&
                        x.Using >= 0 &&
                        limitedPositions.Contains(x.Using) &&
                        !session.TempItemDbIds.Contains(x.Id))
                    .ToList();
            }

            foreach (GoodsData item in items)
            {
                item.Using = -1;
                SendEquipmentStateChanged(player, item, -1, ModGoodsTypes.EquipUnload);
                ItemManager.NotifyGoodsInfo(player, item);
            }
        }

        private static void SendEquipmentStateChanged(KPlayer player, GoodsData item, int usingPosition, ModGoodsTypes modType)
        {
            SCModGoods scData = new SCModGoods()
            {
                BagIndex = item.BagIndex,
                Count = item.GCount,
                IsUsing = usingPosition,
                ModType = (int)modType,
                ID = item.Id,
                NewHint = 0,
                Site = item.Site,
                State = 0,
            };

            player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
        }

        private static HashSet<int> _cachedLimitedEquipPositions;

        private static HashSet<int> GetServerLimitedEquipPositions()
        {
            if (_cachedLimitedEquipPositions != null)
            {
                return _cachedLimitedEquipPositions;
            }

            if (ServerConfig.Instance == null || string.IsNullOrEmpty(ServerConfig.Instance.LimitEquipPos))
            {
                return _cachedLimitedEquipPositions = new HashSet<int>();
            }

            return _cachedLimitedEquipPositions = new HashSet<int>(
                ServerConfig.Instance.LimitEquipPos
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out int position) ? position : -1)
                    .Where(x => x >= 0));
        }

        private bool UnloadOriginalEquipment(KPlayer player, GoodsData item)
        {
            if (item.Using >= (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_HEAD)
            {
                return player.GetPlayEquipBody().EquipUnloadSub(item);
            }

            return player.GetPlayEquipBody().EquipUnloadMain(item, item.Using);
        }

        private List<GoodsData> GetEquippedNonBalanceItems(KPlayer player, KuaFuBalanceSession session)
        {
            HashSet<int> limitedPositions = GetServerLimitedEquipPositions();
            if (limitedPositions.Count > 0 && player.GoodsDataList != null)
            {
                lock (player.GoodsDataList)
                {
                    return player.GoodsDataList
                        .Where(x => x != null &&
                            x.GCount > 0 &&
                            x.Using >= 0 &&
                            limitedPositions.Contains(x.Using) &&
                            !session.TempItemDbIds.Contains(x.Id))
                        .ToList();
                }
            }

            KPlayerEquipBody equipBody = player.GetPlayEquipBody();
            if (equipBody == null)
            {
                return new List<GoodsData>();
            }

            List<GoodsData> equippedItems = new List<GoodsData>();
            lock (equipBody.EquipBody)
            {
                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in equipBody.EquipBody)
                {
                    if (!IsManagedOriginalEquipPosition(entry.Key))
                    {
                        continue;
                    }

                    GoodsData item = entry.Value?._GoodDatas;
                    if (item == null || item.GCount <= 0 || session.TempItemDbIds.Contains(item.Id))
                    {
                        continue;
                    }

                    equippedItems.Add(item);
                }
            }

            lock (equipBody.PreventiveEquipBody)
            {
                foreach (KeyValuePair<KE_EQUIP_POSITION_SUB, KItem> entry in equipBody.PreventiveEquipBody)
                {
                    GoodsData item = entry.Value?._GoodDatas;
                    if (item == null || item.GCount <= 0 || session.TempItemDbIds.Contains(item.Id))
                    {
                        continue;
                    }

                    equippedItems.Add(item);
                }
            }

            return equippedItems;
        }

        private static bool IsManagedOriginalEquipPosition(KE_EQUIP_POSITION position)
        {
            if (position >= KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_1 &&
                position <= KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_3)
            {
                return false;
            }

            if (position >= KE_EQUIP_POSITION.emEQUIPPOS_PET &&
                position <= KE_EQUIP_POSITION.emEQUIPPOS_PET_SIGNET)
            {
                return false;
            }

            return position >= KE_EQUIP_POSITION.emEQUIPPOS_HEAD &&
                position < KE_EQUIP_POSITION.emEQUIPPOS_NUM;
        }

        private List<GoodsData> CreateTemporaryItems(KPlayer player, KuaFuBalanceEquipSet equipSet)
        {
            List<KuaFuBalanceItemConfig> activeItems = equipSet.Items
                .Where(x => x.Sex < 0 || x.Sex == player.RoleSex)
                .ToList();

            if (activeItems.Count <= 0)
            {
                PlayerManager.ShowNotification(player, "Chưa có trang bị cân bằng phù hợp giới tính nhân vật.");
                return null;
            }

            int freeKuaFuBagSlots = CountFreeBagSlots(player, (int)SiteItemType.kuafubalancebag);
            if (freeKuaFuBagSlots < activeItems.Count)
            {
                PlayerManager.ShowNotification(player, string.Format("Túi liên server cân bằng cần trống ít nhất {0} ô.", activeItems.Count));
                return null;
            }

            List<GoodsData> createdItems = new List<GoodsData>();

            foreach (KuaFuBalanceItemConfig itemConfig in activeItems)
            {
                HashSet<int> beforeIds = GetGoodsDbIds(player);
                string endTime = GetTemporaryItemEndTime(itemConfig.ExpireMinutes);

                bool ok = ItemManager.CreateItem(
                    Global._TCPManager.TcpOutPacketPool,
                    player,
                    itemConfig.ItemId,
                    itemConfig.Count,
                    (int)SiteItemType.kuafubalancebag,
                    SourceName,
                    false,
                    itemConfig.Binding,
                    false,
                    endTime,
                    "",
                    itemConfig.Series,
                    "",
                    itemConfig.EnhanceLevel,
                    1,
                    true);

                if (!ok)
                {
                    RemoveCreatedItemsOnFailure(player, createdItems);
                    return null;
                }

                GoodsData created = FindCreatedGoods(player, beforeIds, itemConfig.ItemId);
                if (created == null)
                {
                    RemoveCreatedItemsOnFailure(player, createdItems);
                    return null;
                }

                createdItems.Add(created);
            }

            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Created {0} equip set temp items (site=4). RoleID={1}, SeriesSet={2}",
                createdItems.Count, player.RoleID, equipSet.SeriesId));
            return createdItems;
        }

        private static string GetTemporaryItemEndTime(int expireMinutes)
        {
            if (expireMinutes < 0)
            {
                return Global.ConstGoodsEndTime;
            }

            return expireMinutes > 0
                ? DateTime.Now.AddMinutes(expireMinutes).ToString("yyyy-MM-dd HH:mm:ss")
                : Global.ConstGoodsEndTime;
        }

        private void RemoveCreatedItemsOnFailure(KPlayer player, List<GoodsData> createdItems)
        {
            foreach (GoodsData item in createdItems)
            {
                GoodsData latestItem = Global.GetGoodsByDbID(player, item.Id);
                if (latestItem != null && latestItem.GCount > 0)
                {
                    RemoveTemporaryItem(player, latestItem);
                }
            }
        }

        private bool EquipTemporaryItems(KPlayer player, List<GoodsData> createdItems)
        {
            foreach (GoodsData item in createdItems)
            {
                GoodsData latestItem = Global.GetGoodsByDbID(player, item.Id);
                if (latestItem == null || latestItem.GCount <= 0)
                {
                    return false;
                }

                ItemData itemData = ItemManager.GetItemTemplate(latestItem.GoodsID);
                if (itemData == null || !ItemManager.KD_ISEQUIP(itemData.Genre))
                {
                    continue;
                }

                int equipPosition = ItemManager.g_anEquipPos[itemData.DetailType];
                if (!player.GetPlayEquipBody().EquipLoadMain(latestItem, equipPosition))
                {
                    return false;
                }
            }

            return true;
        }

        private HashSet<int> GetGoodsDbIds(KPlayer player)
        {
            if (player.GoodsDataList == null)
            {
                return new HashSet<int>();
            }

            lock (player.GoodsDataList)
            {
                return new HashSet<int>(player.GoodsDataList.Where(x => x != null).Select(x => x.Id));
            }
        }

        private HashSet<int> GetExistingBagGoodsDbIds(KPlayer player, HashSet<int> dbIds)
        {
            if (dbIds == null || dbIds.Count <= 0 || player.GoodsDataList == null)
            {
                return new HashSet<int>();
            }

            lock (player.GoodsDataList)
            {
                return new HashSet<int>(player.GoodsDataList
                    .Where(x => x != null && x.GCount > 0 && x.Using < 0 && dbIds.Contains(x.Id))
                    .Select(x => x.Id));
            }
        }

        private HashSet<int> GetExistingGoodsDbIds(KPlayer player, HashSet<int> dbIds)
        {
            if (dbIds == null || dbIds.Count <= 0 || player.GoodsDataList == null)
            {
                return new HashSet<int>();
            }

            lock (player.GoodsDataList)
            {
                return new HashSet<int>(player.GoodsDataList
                    .Where(x => x != null && x.GCount > 0 && dbIds.Contains(x.Id))
                    .Select(x => x.Id));
            }
        }

        private GoodsData FindCreatedGoods(KPlayer player, HashSet<int> beforeIds, int itemId)
        {
            if (player.GoodsDataList == null)
            {
                return null;
            }

            lock (player.GoodsDataList)
            {
                return player.GoodsDataList
                    .Where(x => x != null && x.GoodsID == itemId && x.GCount > 0 && !beforeIds.Contains(x.Id))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();
            }
        }

        private void RemoveTemporaryItemsByDbIds(KPlayer player, HashSet<int> dbIds)
        {
            if (dbIds == null || dbIds.Count <= 0 || player.GoodsDataList == null)
            {
                return;
            }

            List<GoodsData> items;
            lock (player.GoodsDataList)
            {
                items = player.GoodsDataList
                    .Where(x => x != null && x.GCount > 0 && dbIds.Contains(x.Id))
                    .ToList();
            }

            foreach (GoodsData item in items)
            {
                RemoveTemporaryItem(player, item);
            }
        }

        private void RemoveEquippedTemporaryItems(KPlayer player, KuaFuBalanceSession session, bool includeConfiguredItems)
        {
            if (player.GoodsDataList == null)
            {
                return;
            }

            List<GoodsData> items;
            lock (player.GoodsDataList)
            {
                items = player.GoodsDataList
                    .Where(x => x != null && x.GCount > 0 &&
                        !session.OriginalEquipDbIds.Contains(x.Id) &&
                        (session.TempItemDbIds.Contains(x.Id) || (includeConfiguredItems && x.Site == (int)SiteItemType.kuafubalancebag)))
                    .ToList();
            }

            foreach (GoodsData item in items)
            {
                RemoveTemporaryItem(player, item);
            }
        }

        private void RemoveTemporaryItem(KPlayer player, GoodsData item)
        {
            GoodsData latestItem = Global.GetGoodsByDbID(player, item.Id);
            if (latestItem == null || latestItem.GCount <= 0)
            {
                return;
            }

            if (latestItem.Using >= 0)
            {
                if (!UnloadOriginalEquipment(player, latestItem))
                {
                    LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Cannot unload temporary item before remove. RoleID={0}, ItemDBID={1}, ItemID={2}", player.RoleID, latestItem.Id, latestItem.GoodsID));
                    return;
                }

                latestItem = Global.GetGoodsByDbID(player, item.Id);
                if (latestItem == null || latestItem.GCount <= 0)
                {
                    return;
                }
            }

            ItemManager.AbandonItem(latestItem, player, false, SourceName);
        }

        /// <summary>
        /// Quét và xóa cưỡng bức toàn bộ trang bị tạm thời khi người chơi ở ngoài Balance Map (phòng ngừa rò rỉ trang bị).
        /// </summary>
        public void ForceCleanUpAllTemporaryItems(KPlayer player)
        {
            if (player == null || player.GoodsDataList == null)
            {
                return;
            }

            List<GoodsData> itemsToCleanup;
            lock (player.GoodsDataList)
            {
                itemsToCleanup = player.GoodsDataList
                    .Where(x => x != null && x.GCount > 0 &&
                        x.Site == (int)SiteItemType.kuafubalancebag)
                    .ToList();
            }

            if (itemsToCleanup.Count <= 0)
            {
                return;
            }

            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] ForceCleanUpAllTemporaryItems: found {0} leaked items. RoleID={1}", itemsToCleanup.Count, player.RoleID));

            int removedCount = 0;
            foreach (GoodsData item in itemsToCleanup)
            {
                GoodsData latestItem = Global.GetGoodsByDbID(player, item.Id);
                if (latestItem == null || latestItem.GCount <= 0)
                {
                    continue;
                }

                if (latestItem.Using >= 0)
                {
                    if (!UnloadOriginalEquipment(player, latestItem))
                    {
                        LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] ForceCleanUpAllTemporaryItems: Cannot unload leaked item. RoleID={0}, ItemDBID={1}", player.RoleID, latestItem.Id));
                        continue;
                    }

                    latestItem = Global.GetGoodsByDbID(player, latestItem.Id);
                    if (latestItem == null || latestItem.GCount <= 0)
                    {
                        continue;
                    }
                }

                ItemManager.AbandonItem(latestItem, player, false, "[KuaFuBalanceForceClean]");
                removedCount++;
            }

            if (removedCount > 0)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] ForceCleanUpAllTemporaryItems: successfully removed {0} leaked items. RoleID={1}", removedCount, player.RoleID));
            }
        }

    }
}
