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
        private void SaveTemporaryProfile(KPlayer player, KuaFuBalanceSession session)
        {
            try
            {
                KuaFuBalanceProfile profile = BuildTemporaryProfile(player, session);
                string profileData = profile != null ? JsonConvert.SerializeObject(profile) : "";
                Global.SaveRoleParamsStringWithNullToDB(player, RoleParamName.KuaFuBalanceProfile, profileData, true);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Save temporary profile failed. RoleID={0}, Error={1}", player.RoleID, ex));
            }
        }

        private KuaFuBalanceProfile BuildTemporaryProfile(KPlayer player, KuaFuBalanceSession session)
        {
            bool hasTemporaryPotential = HasTemporaryPotentialProfile(player, session);
            bool hasTemporarySkillReset = HasTemporarySkillPointProfile(player, session);

            KuaFuBalanceProfile profile = new KuaFuBalanceProfile()
            {
                FactionId = session.TempFactionId,
                RouteId = session.TempRouteId,
                SeriesId = session.TempSeriesId,
                HasTemporaryLevel = session.HasTemporaryLevel,
                TemporaryLevel = session.HasTemporaryLevel ? player.m_Level : 0,
                HasTemporaryPotential = hasTemporaryPotential,
                Strength = hasTemporaryPotential ? player.GetStrength() : 0,
                Dexterity = hasTemporaryPotential ? player.GetDexterity() : 0,
                Vitality = hasTemporaryPotential ? player.GetVitality() : 0,
                Energy = hasTemporaryPotential ? player.GetEnergy() : 0,
                HasTemporarySkillReset = hasTemporarySkillReset,
            };

            profile.Items = GetTemporaryProfileItems(player, session);
            profile.Skills = GetTemporaryProfileSkills(player, session);
            profile.HasTemporaryBookSkills = profile.Skills.Any(IsProfileBookSkill);

            if (!profile.HasTemporaryBuild())
            {
                return null;
            }

            return profile;
        }

        private List<KuaFuBalanceProfileItem> GetTemporaryProfileItems(KPlayer player, KuaFuBalanceSession session)
        {
            List<KuaFuBalanceProfileItem> profileItems = new List<KuaFuBalanceProfileItem>();
            if (player.GoodsDataList == null || session.TempItemDbIds.Count <= 0)
            {
                return profileItems;
            }

            List<GoodsData> temporaryItems;
            lock (player.GoodsDataList)
            {
                temporaryItems = player.GoodsDataList
                    .Where(x => x != null &&
                        x.GCount > 0 &&
                        session.TempItemDbIds.Contains(x.Id) &&
                        !session.OriginalEquipDbIds.Contains(x.Id))
                    .OrderBy(x => x.Using < 0 ? int.MaxValue : x.Using)
                    .ThenBy(x => x.Id)
                    .ToList();
            }

            foreach (GoodsData item in temporaryItems)
            {
                profileItems.Add(new KuaFuBalanceProfileItem()
                {
                    GoodsId = item.GoodsID,
                    Count = Math.Max(1, item.GCount),
                    Series = item.Series,
                    Binding = item.Binding,
                    EnhanceLevel = item.Forge_level,
                    Using = item.Using,
                    EndTime = string.IsNullOrEmpty(item.Endtime) ? Global.ConstGoodsEndTime : item.Endtime,
                    Props = item.Props ?? "",
                    OtherParams = ToProfileOtherParams(item.OtherParams),
                });
            }

            return profileItems;
        }

        private List<KuaFuBalanceProfileSkill> GetTemporaryProfileSkills(KPlayer player, KuaFuBalanceSession session)
        {
            if (player == null || player.Skills == null)
            {
                return new List<KuaFuBalanceProfileSkill>();
            }

            bool includeSkillPointSkills = HasTemporarySkillPointProfile(player, session);

            return player.Skills.ListSkills
                .Where(x => x != null &&
                    x.AddedLevel > 0 &&
                    (includeSkillPointSkills || IsProfileBookSkill(x.SkillID)))
                .Select(x => new KuaFuBalanceProfileSkill()
                {
                    SkillId = x.SkillID,
                    AddedLevel = x.AddedLevel,
                    Exp = x.Exp,
                })
                .ToList();
        }

        private bool HasTemporaryPotentialProfile(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.HasTemporaryPotentialReset)
            {
                return true;
            }

            return HasTemporaryProfileContext(session) && HasAssignedPotential(player);
        }

        private static bool HasAssignedPotential(KPlayer player)
        {
            return player != null &&
                (player.GetStrength() > 0 ||
                    player.GetDexterity() > 0 ||
                    player.GetVitality() > 0 ||
                    player.GetEnergy() > 0);
        }

        private bool HasTemporarySkillPointProfile(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.HasTemporarySkillPointReset)
            {
                return true;
            }

            return HasTemporaryProfileContext(session) && HasAssignedRouteSkillPoints(player);
        }

        private static bool HasTemporaryProfileContext(KuaFuBalanceSession session)
        {
            return session != null &&
                (session.TempFactionId > 0 ||
                    session.TempRouteId > 0 ||
                    session.HasTemporaryLevel ||
                    session.TempItemDbIds.Count > 0);
        }

        private bool HasAssignedRouteSkillPoints(KPlayer player)
        {
            if (player == null || player.Skills == null)
            {
                return false;
            }

            int factionId = player.m_cPlayerFaction.GetFactionId();
            if (factionId <= 0)
            {
                return false;
            }

            HashSet<int> factionSkills = new HashSet<int>(KFaction.GetFactionSkills(factionId));
            return player.Skills.ListSkills.Any(x =>
                x != null &&
                x.CanAddPoint &&
                x.AddedLevel > 0 &&
                factionSkills.Contains(x.SkillID));
        }

        private bool RestoreTemporaryProfile(KPlayer player, KuaFuBalanceSession session, out bool hasSavedProfile)
        {
            hasSavedProfile = false;
            KuaFuBalanceProfile profile = LoadTemporaryProfile(player);
            if (profile == null || !profile.HasTemporaryBuild())
            {
                return false;
            }

            hasSavedProfile = true;
            RemoveOrphanedConfiguredTemporaryBagItems(player, session);
            if (!CanRestoreProfileItems(player, profile.Items))
            {
                session.HasBlockedTemporaryProfileRestore = true;
                return false;
            }

            session.HasBlockedTemporaryProfileRestore = false;
            bool restored = false;

            if (profile.HasTemporaryLevel && profile.TemporaryLevel > 0)
            {
                session.HasTemporaryLevel = true;
                if (player.m_Level != profile.TemporaryLevel)
                {
                    lock (player.PropPointMutex)
                    {
                        player.UnAssignPotential();
                    }
                    GameManager.ClientMgr.SetRoleLevel(player, profile.TemporaryLevel);
                    EnsureCurrentLifeManaValid(player);
                }

                ApplyBaseOnlySkillPoints(player, session);
                restored = true;
            }

            if (profile.FactionId > 0 && profile.RouteId > 0)
            {
                restored = ChangeTemporaryFaction(player, session, profile.FactionId, profile.RouteId) || restored;
                session.TempSeriesId = profile.SeriesId;
            }

            if (profile.HasTemporaryPotential)
            {
                RestoreProfilePotential(player, session, profile);
                restored = true;
            }

            if (profile.HasTemporarySkillReset)
            {
                RestoreProfileSkills(player, session, profile.Skills);
                restored = true;
            }
            else if (profile.HasTemporaryBookSkills)
            {
                RestoreProfileBookSkills(player, profile.Skills);
                restored = true;
            }

            if (profile.Items != null && profile.Items.Count > 0)
            {
                restored = RestoreProfileItems(player, session, profile.Items) || restored;
            }

            if (restored)
            {
                PlayerManager.ShowNotification(player, "Đã khôi phục build cân bằng đã lưu.");
            }

            return restored;
        }

        private void ResetDefaultTemporaryLevel(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.HasTemporaryLevel)
            {
                return;
            }

            session.HasTemporaryLevel = true;
            if (player.m_Level != DefaultFirstEntryLevel)
            {
                lock (player.PropPointMutex)
                {
                    player.UnAssignPotential();
                }
                GameManager.ClientMgr.SetRoleLevel(player, DefaultFirstEntryLevel);
                EnsureCurrentLifeManaValid(player);
            }
        }

        private void ResetDefaultTemporaryPoints(KPlayer player, KuaFuBalanceSession session)
        {
            lock (player.PropPointMutex)
            {
                if (!session.HasTemporaryPotentialReset)
                {
                    if (!session.IsOriginalStateCaptured)
                    {
                        session.OriginalStrength = player.GetStrength();
                        session.OriginalDexterity = player.GetDexterity();
                        session.OriginalVitality = player.GetVitality();
                        session.OriginalEnergy = player.GetEnergy();
                    }
                    session.HasTemporaryPotentialReset = true;
                }

                ApplyBaseOnlyPotentialPoints(player, session);
                player.UnAssignPotential();
            }

            ApplyBaseOnlySkillPoints(player, session);
            session.HasTemporarySkillPointReset = true;
            session.NeedsDefaultPointReset = false;

            if (player.Skills != null)
            {
                SnapshotOriginalSkillBuild(player, session);
                player.Skills.ResetAllSkillsLevel();
                RestoreNewbieAttackSkills(player);
                KT_TCPHandler.SendRenewSkillList(player);
            }

            KT_TCPHandler.NotifyAttribute(player);
            PlayerManager.ShowNotification(player, "Đã làm mới điểm tiềm năng và kỹ năng cân bằng.");
        }

        private KuaFuBalanceProfile LoadTemporaryProfile(KPlayer player)
        {
            try
            {
                string profileData = Global.GetRoleParamsStringWithNullFromDB(player, RoleParamName.KuaFuBalanceProfile);
                if (string.IsNullOrEmpty(profileData))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<KuaFuBalanceProfile>(profileData);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Load temporary profile failed. RoleID={0}, Error={1}", player.RoleID, ex));
                return null;
            }
        }

        private void RestoreProfilePotential(KPlayer player, KuaFuBalanceSession session, KuaFuBalanceProfile profile)
        {
            lock (player.PropPointMutex)
            {
                if (!session.HasTemporaryPotentialReset)
                {
                    if (!session.IsOriginalStateCaptured)
                    {
                        session.OriginalStrength = player.GetStrength();
                        session.OriginalDexterity = player.GetDexterity();
                        session.OriginalVitality = player.GetVitality();
                        session.OriginalEnergy = player.GetEnergy();
                    }
                    session.HasTemporaryPotentialReset = true;
                }

                ApplyBaseOnlyPotentialPoints(player, session);
                player.UnAssignPotential();
                if (!player.AssignPotential(profile.Strength, profile.Dexterity, profile.Vitality, profile.Energy))
                {
                    LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Restore profile potential failed. RoleID={0}, Str={1}, Dex={2}, Vit={3}, Ene={4}", player.RoleID, profile.Strength, profile.Dexterity, profile.Vitality, profile.Energy));
                }
            }

            KT_TCPHandler.NotifyAttribute(player);
        }

        private void RestoreProfileSkills(KPlayer player, KuaFuBalanceSession session, List<KuaFuBalanceProfileSkill> skills)
        {
            if (player == null || player.Skills == null)
            {
                return;
            }

            SnapshotOriginalSkillBuild(player, session);
            ApplyBaseOnlySkillPoints(player, session);
            session.HasTemporarySkillPointReset = true;
            player.Skills.ResetAllSkillsLevel();

            if (skills == null || skills.Count <= 0)
            {
                RestoreNewbieAttackSkills(player);
                return;
            }

            List<KeyValuePair<int, int>> skillLevels = BuildRestorableProfileSkillLevels(player, skills);

            if (skillLevels.Count > 0)
            {
                player.Skills.AddSkillsLevel(skillLevels);
            }

            RestoreProfileBookSkills(player, skills);
            RestoreNewbieAttackSkills(player);
        }

        private List<KeyValuePair<int, int>> BuildRestorableProfileSkillLevels(KPlayer player, List<KuaFuBalanceProfileSkill> skills)
        {
            List<KeyValuePair<int, int>> skillLevels = new List<KeyValuePair<int, int>>();
            if (player == null || player.Skills == null || skills == null || skills.Count <= 0)
            {
                return skillLevels;
            }

            int factionId = player.m_cPlayerFaction.GetFactionId();
            int routeId = player.m_cPlayerFaction.GetRouteId();
            HashSet<int> routeSkills = new HashSet<int>(KFaction.GetRouteSkills(factionId, routeId));
            int remainingSkillPoints = Math.Max(0, player.GetBaseSkillPoint());

            foreach (KuaFuBalanceProfileSkill skill in skills.Where(x => x != null && x.SkillId > 0 && x.AddedLevel > 0 && !IsProfileBookSkill(x)))
            {
                if (remainingSkillPoints <= 0)
                {
                    break;
                }

                if (!routeSkills.Contains(skill.SkillId))
                {
                    continue;
                }

                SkillLevelRef skillRef = player.Skills.GetSkillLevelRef(skill.SkillId);
                if (skillRef == null || !skillRef.CanAddPoint)
                {
                    continue;
                }

                int maxLevelByConfig = skillRef.Data.MaxSkillLevel > 0 ? skillRef.Data.MaxSkillLevel : skill.AddedLevel;
                int maxLevelByRoleLevel = player.m_Level - skillRef.Data.RequireLevel + 1;
                int maxLevel = Math.Max(0, Math.Min(maxLevelByConfig, maxLevelByRoleLevel));
                int restoreLevel = Math.Min(skill.AddedLevel, Math.Min(maxLevel, remainingSkillPoints));
                if (restoreLevel <= 0)
                {
                    continue;
                }

                skillLevels.Add(new KeyValuePair<int, int>(skill.SkillId, restoreLevel));
                remainingSkillPoints -= restoreLevel;
            }

            return skillLevels;
        }

        private void RestoreProfileBookSkills(KPlayer player, List<KuaFuBalanceProfileSkill> skills)
        {
            if (player == null || player.Skills == null || skills == null || skills.Count <= 0)
            {
                return;
            }

            foreach (KuaFuBalanceProfileSkill skill in skills.Where(IsProfileBookSkill))
            {
                player.Skills.RestoreSkillLevelAndExp(skill.SkillId, skill.AddedLevel, skill.Exp);
            }
        }

        private static HashSet<int> _bookSkillIds = null;
        private static readonly object _bookSkillLock = new object();

        private static HashSet<int> GetGlobalBookSkillIds()
        {
            if (_bookSkillIds != null)
            {
                return _bookSkillIds;
            }

            lock (_bookSkillLock)
            {
                if (_bookSkillIds != null)
                {
                    return _bookSkillIds;
                }

                _bookSkillIds = new HashSet<int>();
                if (ItemManager._TotalGameItem != null)
                {
                    foreach (var item in ItemManager._TotalGameItem.Values)
                    {
                        if (item != null && item.BookProperty != null)
                        {
                            if (item.BookProperty.SkillID1 > 0) _bookSkillIds.Add(item.BookProperty.SkillID1);
                            if (item.BookProperty.SkillID2 > 0) _bookSkillIds.Add(item.BookProperty.SkillID2);
                            if (item.BookProperty.SkillID3 > 0) _bookSkillIds.Add(item.BookProperty.SkillID3);
                            if (item.BookProperty.SkillID4 > 0) _bookSkillIds.Add(item.BookProperty.SkillID4);
                        }
                    }
                }
                return _bookSkillIds;
            }
        }

        private static bool IsProfileBookSkill(KuaFuBalanceProfileSkill skill)
        {
            return skill != null && IsProfileBookSkill(skill.SkillId);
        }

        private static bool IsProfileBookSkill(int skillId)
        {
            SkillDataEx skillData = KSkill.GetSkillData(skillId);
            if (skillData == null)
            {
                return false;
            }
            return skillData.IsExpSkill || GetGlobalBookSkillIds().Contains(skillId);
        }

        private bool RestoreProfileItems(KPlayer player, KuaFuBalanceSession session, List<KuaFuBalanceProfileItem> profileItems)
        {
            List<GoodsData> restoredItems = new List<GoodsData>();
            List<KuaFuBalanceProfileItem> orderedProfileItems = profileItems
                .Where(x => x != null)
                .OrderBy(x => ShouldRestoreProfileItemEquipped(x) ? 0 : 1)
                .ThenBy(x => x.Using < 0 ? int.MaxValue : x.Using)
                .ThenBy(x => x.GoodsId)
                .ToList();

            foreach (KuaFuBalanceProfileItem profileItem in orderedProfileItems)
            {
                GoodsData created = CreateProfileItem(player, session, profileItem);
                if (created == null)
                {
                    RemoveCreatedItemsOnFailure(player, restoredItems);
                    session.TempItemDbIds.Clear();
                    return false;
                }

                restoredItems.Add(created);

                if (!ShouldRestoreProfileItemEquipped(profileItem))
                {
                    continue;
                }

                GoodsData latestItem = Global.GetGoodsByDbID(player, created.Id);
                if (latestItem == null || latestItem.GCount <= 0)
                {
                    continue;
                }

                if (!EquipProfileItem(player, latestItem, profileItem.Using))
                {
                    LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Restore profile equip failed. RoleID={0}, ItemDBID={1}, ItemID={2}, Using={3}", player.RoleID, latestItem.Id, latestItem.GoodsID, profileItem.Using));
                }
            }

            return restoredItems.Count > 0;
        }

        private bool CanRestoreProfileItems(KPlayer player, List<KuaFuBalanceProfileItem> profileItems)
        {
            if (profileItems == null || profileItems.Count <= 0)
            {
                return true;
            }

            List<KuaFuBalanceProfileItem> validItems = profileItems
                .Where(x => x != null && x.GoodsId > 0 && x.Count > 0)
                .ToList();

            int equippedItemCount = validItems.Count(ShouldRestoreProfileItemEquipped);
            int bagItemCount = validItems.Count - equippedItemCount;
            int requiredSlots = Math.Max(bagItemCount, equippedItemCount > 0 ? 1 : 0);
            int freeSlots = CountFreeBagSlots(player, (int)SiteItemType.kuafubalancebag);

            if (freeSlots >= requiredSlots)
            {
                return true;
            }

            PlayerManager.ShowNotification(player, string.Format("Túi liên server cần trống ít nhất {0} ô để khôi phục build cân bằng.", requiredSlots));
            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Restore profile items blocked by full bag. RoleID={0}, FreeSlots={1}, RequiredSlots={2}, Items={3}, EquippedItems={4}", player.RoleID, freeSlots, requiredSlots, validItems.Count, equippedItemCount));
            return false;
        }

        private void RemoveOrphanedConfiguredTemporaryBagItems(KPlayer player, KuaFuBalanceSession session)
        {
            if (player.GoodsDataList == null)
            {
                return;
            }

            List<GoodsData> orphanedItems;
            lock (player.GoodsDataList)
            {
                orphanedItems = player.GoodsDataList
                    .Where(x => x != null &&
                        x.GCount > 0 &&
                        x.Using < 0 &&
                        x.Site == (int)SiteItemType.kuafubalancebag &&
                        !session.TempItemDbIds.Contains(x.Id))
                    .ToList();
            }

            if (orphanedItems.Count <= 0)
            {
                return;
            }

            foreach (GoodsData item in orphanedItems)
            {
                RemoveTemporaryItem(player, item);
            }

            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Removed orphaned configured temporary bag items before profile restore. RoleID={0}, Count={1}", player.RoleID, orphanedItems.Count));
        }

        private bool ShouldRestoreProfileItemEquipped(KuaFuBalanceProfileItem profileItem)
        {
            if (profileItem == null || profileItem.Using < 0)
            {
                return false;
            }

            ItemData itemData = ItemManager.GetItemTemplate(profileItem.GoodsId);
            return itemData != null && ItemManager.KD_ISEQUIP(itemData.Genre);
        }

        private GoodsData CreateProfileItem(KPlayer player, KuaFuBalanceSession session, KuaFuBalanceProfileItem profileItem)
        {
            if (profileItem == null || profileItem.GoodsId <= 0 || profileItem.Count <= 0)
            {
                return null;
            }

            ItemData itemData = ItemManager.GetItemTemplate(profileItem.GoodsId);
            if (itemData == null)
            {
                return null;
            }

            SnapshotTemporaryBookSkills(player, session, itemData);

            HashSet<int> beforeIds = GetGoodsDbIds(player);
            bool ok = ItemManager.CreateItem(
                Global._TCPManager.TcpOutPacketPool,
                player,
                profileItem.GoodsId,
                profileItem.Count,
                (int)SiteItemType.kuafubalancebag,
                SourceName,
                false,
                profileItem.Binding,
                false,
                string.IsNullOrEmpty(profileItem.EndTime) ? Global.ConstGoodsEndTime : profileItem.EndTime,
                profileItem.Props ?? "",
                profileItem.Series,
                "",
                profileItem.EnhanceLevel,
                0,
                true);

            if (!ok)
            {
                return null;
            }

            GoodsData created = FindCreatedGoods(player, beforeIds, profileItem.GoodsId);
            if (created == null)
            {
                return null;
            }

            session.TempItemDbIds.Add(created.Id);
            RestoreProfileItemOtherParams(player, created, profileItem.OtherParams);
            return created;
        }

        private bool EquipProfileItem(KPlayer player, GoodsData item, int usingPosition)
        {
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            if (itemData == null || !ItemManager.KD_ISEQUIP(itemData.Genre))
            {
                return false;
            }

            return player.GetPlayEquipBody().EquipLoadMain(item, usingPosition);
        }

        private void RestoreProfileItemOtherParams(KPlayer player, GoodsData item, Dictionary<int, string> profileOtherParams)
        {
            Dictionary<ItemPramenter, string> otherParams = ToItemOtherParams(profileOtherParams);
            if (otherParams.Count <= 0)
            {
                return;
            }

            Dictionary<UPDATEITEM, object> totalUpdate = new Dictionary<UPDATEITEM, object>();
            totalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
            totalUpdate.Add(UPDATEITEM.ITEMDBID, item.Id);
            totalUpdate.Add(UPDATEITEM.OTHER_PRAM, otherParams);
            ItemManager.UpdateItemPrammenter(totalUpdate, item, player, "KuaFuBalance_RestoreProfileItem", true);
        }

        private static Dictionary<int, string> ToProfileOtherParams(Dictionary<ItemPramenter, string> otherParams)
        {
            Dictionary<int, string> profileOtherParams = new Dictionary<int, string>();
            if (otherParams == null || otherParams.Count <= 0)
            {
                return profileOtherParams;
            }

            foreach (KeyValuePair<ItemPramenter, string> pair in otherParams)
            {
                profileOtherParams[(int)pair.Key] = pair.Value ?? "";
            }

            return profileOtherParams;
        }

        private static Dictionary<ItemPramenter, string> ToItemOtherParams(Dictionary<int, string> profileOtherParams)
        {
            Dictionary<ItemPramenter, string> otherParams = new Dictionary<ItemPramenter, string>();
            if (profileOtherParams == null || profileOtherParams.Count <= 0)
            {
                return otherParams;
            }

            foreach (KeyValuePair<int, string> pair in profileOtherParams)
            {
                if (Enum.IsDefined(typeof(ItemPramenter), pair.Key))
                {
                    otherParams[(ItemPramenter)pair.Key] = pair.Value ?? "";
                }
            }

            return otherParams;
        }

        internal void CaptureOriginalState(KPlayer player, KuaFuBalanceSession session)
        {
            if (GameManager.IsKuaFuServer)
            {
                return;
            }

            if (player == null || session == null || session.IsOriginalStateCaptured)
            {
                return;
            }

            // Chụp tiềm năng gốc
            session.OriginalStrength = player.GetStrength();
            session.OriginalDexterity = player.GetDexterity();
            session.OriginalVitality = player.GetVitality();
            session.OriginalEnergy = player.GetEnergy();
            session.OriginalBonusPotentialPoints = player.GetBonusRemainPotentialPoints();

            // Chụp kỹ năng gốc
            if (player.Skills != null)
            {
                session.OriginalBonusSkillPoints = player.GetBonusSkillPoint();
                session.OriginalSkillSnapshots.Clear();
                foreach (SkillLevelRef skill in player.Skills.ListSkills)
                {
                    if (skill == null || skill.SkillID <= 0)
                    {
                        continue;
                    }

                    session.OriginalSkillSnapshots[skill.SkillID] = new KuaFuBalanceSkillSnapshot()
                    {
                        SkillId = skill.SkillID,
                        HadSkill = true,
                        AddedLevel = Math.Max(0, skill.AddedLevel),
                        Exp = Math.Max(0, skill.Exp),
                    };
                }
            }

            session.IsOriginalStateCaptured = true;
        }

        internal void SaveOriginalSnapshot(KPlayer player, KuaFuBalanceSession session)
        {
            if (GameManager.IsKuaFuServer)
            {
                return;
            }

            if (player == null || session == null)
            {
                return;
            }

            try
            {
                KuaFuBalanceOriginalSnapshot snapshot = new KuaFuBalanceOriginalSnapshot()
                {
                    OriginalFactionId = session.OriginalFactionId,
                    OriginalRouteId = session.OriginalRouteId,
                    OriginalLevel = session.OriginalLevel,
                    OriginalKuafuMoney = session.OriginalKuafuMoney,
                    OriginalBonusSkillPoints = session.OriginalBonusSkillPoints,
                    OriginalBonusPotentialPoints = session.OriginalBonusPotentialPoints,
                    OriginalStrength = session.OriginalStrength,
                    OriginalDexterity = session.OriginalDexterity,
                    OriginalVitality = session.OriginalVitality,
                    OriginalEnergy = session.OriginalEnergy,
                };

                if (session.OriginalEquipDbIds != null)
                {
                    snapshot.OriginalEquipDbIds.AddRange(session.OriginalEquipDbIds);
                }

                if (session.OriginalEquipPositions != null)
                {
                    foreach (var kvp in session.OriginalEquipPositions)
                    {
                        snapshot.OriginalEquipPositions[kvp.Key] = kvp.Value;
                    }
                }

                if (session.OriginalSkillSnapshots != null)
                {
                    foreach (var kvp in session.OriginalSkillSnapshots)
                    {
                        snapshot.OriginalSkillSnapshots[kvp.Key] = kvp.Value;
                    }
                }

                string snapshotData = snapshot.SerializeToCompactString();
                Global.SaveRoleParamsStringWithNullToDB(player, RoleParamName.KuaFuBalanceOriginalSnapshot, snapshotData, true);
                
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Saved original snapshot to DB (Compact). RoleID={0}, Level={1}, Faction={2}, Equips={3}, Len={4}",
                    player.RoleID, snapshot.OriginalLevel, snapshot.OriginalFactionId, snapshot.OriginalEquipDbIds.Count, snapshotData.Length));

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Save original snapshot failed. RoleID={0}, Error={1}", player.RoleID, ex));
            }
        }


        internal KuaFuBalanceOriginalSnapshot LoadOriginalSnapshot(KPlayer player)
        {
            if (player == null)
            {
                return null;
            }

            try
            {
                string snapshotData = Global.GetRoleParamsStringWithNullFromDB(player, RoleParamName.KuaFuBalanceOriginalSnapshot);
                if (string.IsNullOrEmpty(snapshotData))
                {
                    return null;
                }

                return KuaFuBalanceOriginalSnapshot.DeserializeFromCompactString(snapshotData);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Load original snapshot failed. RoleID={0}, Error={1}", player.RoleID, ex));
                return null;
            }
        }

        internal void ForceRestoreOriginalState(KPlayer player, KuaFuBalanceOriginalSnapshot snapshot)
        {
            if (player == null || snapshot == null)
            {
                return;
            }

            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Force restoring original state from DB snapshot. RoleID={0}, Level={1}, Faction={2}",
                player.RoleID, snapshot.OriginalLevel, snapshot.OriginalFactionId));

            KuaFuBalanceSession session = new KuaFuBalanceSession()
            {
                RoleId = player.RoleID,
                OriginalFactionId = snapshot.OriginalFactionId,
                OriginalRouteId = snapshot.OriginalRouteId,
                OriginalLevel = snapshot.OriginalLevel,
                OriginalKuafuMoney = snapshot.OriginalKuafuMoney,
                OriginalBonusSkillPoints = snapshot.OriginalBonusSkillPoints,
                OriginalBonusPotentialPoints = snapshot.OriginalBonusPotentialPoints,
                OriginalStrength = snapshot.OriginalStrength,
                OriginalDexterity = snapshot.OriginalDexterity,
                OriginalVitality = snapshot.OriginalVitality,
                OriginalEnergy = snapshot.OriginalEnergy,
            };

            if (snapshot.OriginalEquipDbIds != null)
            {
                session.OriginalEquipDbIds.AddRange(snapshot.OriginalEquipDbIds);
            }

            if (snapshot.OriginalEquipPositions != null)
            {
                foreach (var kvp in snapshot.OriginalEquipPositions)
                {
                    session.OriginalEquipPositions[kvp.Key] = kvp.Value;
                }
            }

            if (snapshot.OriginalSkillSnapshots != null)
            {
                foreach (var kvp in snapshot.OriginalSkillSnapshots)
                {
                    session.OriginalSkillSnapshots[kvp.Key] = kvp.Value;
                }
            }

            try
            {
                PerformFullRestore(player, session);

                if (!GameManager.IsKuaFuServer)
                {
                    //Global.SaveRoleParamsStringWithNullToDB(player, RoleParamName.KuaFuBalanceOriginalSnapshot, "", true);
                }

                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Force restored original state successfully. RoleID={0}", player.RoleID));
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] ForceRestoreOriginalState failed. RoleID={0}, Error={1}", player.RoleID, ex));
            }
        }

        public void PrepareOriginalSnapshotBeforeTransfer(KPlayer player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));
                session.OriginalKuafuMoney = player.KuafuMoney;
                CaptureOriginalState(player, session);
                SaveOriginalSnapshot(player, session);
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] PrepareOriginalSnapshotBeforeTransfer successfully. RoleID={0}", player.RoleID));
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] PrepareOriginalSnapshotBeforeTransfer failed. RoleID={0}, Error={1}", player.RoleID, ex));
            }
        }

    }
}
