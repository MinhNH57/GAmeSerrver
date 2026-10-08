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
        private bool IsConfiguredTemporaryItem(int itemId)
        {
            return configuredEquipItemIds.Contains(itemId);
        }

        private bool IsTemporaryEquipment(KPlayer player, GoodsData item)
        {
            if (player == null || item == null || item.GCount <= 0)
            {
                return false;
            }

            if (!sessions.TryGetValue(player.RoleID, out KuaFuBalanceSession session))
            {
                return false;
            }

            return session.TempItemDbIds.Contains(item.Id) && !session.OriginalEquipDbIds.Contains(item.Id);
        }

        private List<GoodsData> FindCreatedGoodsList(KPlayer player, HashSet<int> beforeIds, int itemId)
        {
            if (player.GoodsDataList == null)
            {
                return new List<GoodsData>();
            }

            lock (player.GoodsDataList)
            {
                return player.GoodsDataList
                    .Where(x => x != null && x.GoodsID == itemId && x.GCount > 0 && !beforeIds.Contains(x.Id))
                    .OrderByDescending(x => x.Id)
                    .ToList();
            }
        }

        private int CountFreeBagSlots(KPlayer player, int site)
        {
            int maxSlots = (site == (int)SiteItemType.kuafubalancebag) ? 50 : player.BagNum;
            if (player.GoodsDataList == null)
            {
                return maxSlots;
            }

            lock (player.GoodsDataList)
            {
                int used = player.GoodsDataList.Count(x => x != null && x.GCount > 0 && x.Site == site && x.BagIndex >= 0);
                return Math.Max(maxSlots - used, 0);
            }
        }

        public int GetPotionMedicineCapacity(KPlayer player)
        {
            if (player == null)
            {
                return 0;
            }

            if (IsInBalance(player) || IsBalanceMap(player))
            {
                return 50;
            }

            return player.BagNum + player.extBagBonus;
        }

        private int CountActiveItems(KuaFuBalanceEquipSet equipSet, int roleSex)
        {
            if (equipSet == null)
            {
                return 0;
            }

            return equipSet.Items.Count(x => x.Sex < 0 || x.Sex == roleSex);
        }

        private KuaFuBalanceEquipSet GetEquipSet(int seriesId, int factionId, int routeId)
        {
            return config.GetEquipSet(seriesId, factionId, routeId);
        }

        private int GetCurrentSeries(KPlayer player)
        {
            int factionId = player.m_cPlayerFaction.GetFactionId();
            if (factionId <= 0)
            {
                return 0;
            }

            return (int)KFaction.GetSeries(factionId);
        }

        private bool ChangeTemporaryFaction(KPlayer player, KuaFuBalanceSession session, int factionId, int routeId)
        {
            if (!KFaction.IsFactionExist(factionId) || !KFaction.IsRouteExist(factionId, routeId))
            {
                PlayerManager.ShowNotification(player, "Lựa chọn phái/nhánh tạm không hợp lệ.");
                return false;
            }

            SnapshotOriginalSkillBuild(player, session);

            int currentFactionId = player.m_cPlayerFaction.GetFactionId();
            if (currentFactionId != factionId)
            {
                int changeFactionResult = PlayerManager.JoinFaction(player, factionId);
                if (changeFactionResult != 1)
                {
                    return false;
                }
            }

            int changeRouteResult = PlayerManager.ChangeRoute(player, routeId);
            if (changeRouteResult != 1)
            {
                RestoreOriginalFaction(player, session);
                return false;
            }

            session.TempFactionId = factionId;
            session.TempRouteId = routeId;
            return true;
        }

        private void RestoreOriginalFaction(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.OriginalFactionId <= 0)
            {
                return;
            }

            if (player.m_cPlayerFaction.GetFactionId() != session.OriginalFactionId)
            {
                PlayerManager.JoinFaction(player, session.OriginalFactionId);
            }

            if (player.m_cPlayerFaction.GetRouteId() != session.OriginalRouteId)
            {
                PlayerManager.ChangeRoute(player, session.OriginalRouteId);
            }
        }

        private void ApplyWingBuff(KPlayer player, KuaFuBalanceSession session)
        {
            RemoveDefaultKuaFuSupportBuffs(player);
            RemoveWingBuff(player, session);

            int buffLevel = config.GetWingBuffLevel(session.MantleLevel);
            if (buffLevel <= 0 || config.WingBuffSkillId <= 0)
            {
                return;
            }

            player.Buffs.AddBuff(config.WingBuffSkillId, buffLevel, config.WingBuffDurationMinutes * 60 * 1000, 1, false);
            session.BuffSkillId = config.WingBuffSkillId;
        }

        private void RemoveDefaultKuaFuSupportBuffs(KPlayer player)
        {
            if (ServerConfig.Instance.IDBuffSupportLSV > 0 && player.Buffs.HasBuff(ServerConfig.Instance.IDBuffSupportLSV))
            {
                player.Buffs.RemoveBuff(ServerConfig.Instance.IDBuffSupportLSV);
            }

            if (player.Buffs.HasBuff(2067))
            {
                player.Buffs.RemoveBuff(2067);
            }

            if (player.Buffs.HasBuff(2066))
            {
                player.Buffs.RemoveBuff(2066);
            }
        }

        private void RemoveWingBuff(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.BuffSkillId > 0 && player.Buffs.HasBuff(session.BuffSkillId))
            {
                player.Buffs.RemoveBuff(session.BuffSkillId);
            }

            if (config.WingBuffSkillId > 0 && player.Buffs.HasBuff(config.WingBuffSkillId))
            {
                player.Buffs.RemoveBuff(config.WingBuffSkillId);
            }

            session.BuffSkillId = 0;
        }

        /// <summary>
        /// Khôi phục KuafuMoney về giá trị trước khi vào balance.
        /// Trừ đi phần chênh lệch nếu player nhận thêm tiền trong balance.
        /// </summary>
        private void RestoreKuafuMoney(KPlayer player, KuaFuBalanceSession session)
        {
            if (player == null || session == null)
            {
                return;
            }

            int delta = player.KuafuMoney - session.OriginalKuafuMoney;
            if (delta == 0)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance,
                    string.Format("[KuaFuBalance] RestoreKuafuMoney: no change. RoleID={0}, Current={1}, Original={2}",
                    player.RoleID, player.KuafuMoney, session.OriginalKuafuMoney));
                return;
            }

            if (delta > 0)
            {
                var result = KTGlobal.SubMoney(player, delta, MoneyType.KuafuMoney, "KuaFuBalance_LeaveBalance");
                LogManager.WriteLog(LogTypes.KuaFuBalance,
                    string.Format("[KuaFuBalance] RestoreKuafuMoney: subtracted {0}. RoleID={1}, Current={2}, Original={3}, Result={4}",
                    delta, player.RoleID, session.OriginalKuafuMoney, player.KuafuMoney, result.IsOK));
            }
            else
            {
                int amountToAdd = -delta;
                var result = KTGlobal.AddMoney(player, amountToAdd, MoneyType.KuafuMoney, "KuaFuBalance_LeaveBalance");
                LogManager.WriteLog(LogTypes.KuaFuBalance,
                    string.Format("[KuaFuBalance] RestoreKuafuMoney: added {0}. RoleID={1}, Current={2}, Original={3}, Result={4}",
                    amountToAdd, player.RoleID, session.OriginalKuafuMoney, player.KuafuMoney, result.IsOK));
            }
        }

        /// <summary>
        /// Xóa toàn bộ buff và vòng sáng khi rời LSV cân bằng,
        /// đảm bảo không buff nào carry over sang server chính.
        /// </summary>
        private void ClearAllBalanceBuffs(KPlayer player, KuaFuBalanceSession session)
        {
            // Xóa toàn bộ buff (doLogic=true: detach thuộc tính, notify=true: lưu DB + gửi client)
            player.Buffs.RemoveAllBuffs(true, true);
            player.Buffs.RemoveAllAruas(true, true);

            session.BuffSkillId = 0;

            LogManager.WriteLog(LogTypes.KuaFuBalance,
                string.Format("[KuaFuBalance] Cleared all buffs on leave balance. RoleID={0}", player.RoleID));
        }
    }
}
