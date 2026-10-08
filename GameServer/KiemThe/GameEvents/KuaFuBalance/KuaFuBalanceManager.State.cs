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
        private void RestoreOriginalEquipment(KPlayer player, KuaFuBalanceSession session)
        {
            KPlayerEquipBody equipBody = player.GetPlayEquipBody();
            if (equipBody == null)
            {
                return;
            }

            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from RestoreOriginalEquipment() in KuaFuBalanceManager.State.cs");
            equipBody.ClearAllEffecEquipBody();

            foreach (int dbId in session.OriginalEquipDbIds)
            {
                GoodsData item = Global.GetGoodsByDbID(player, dbId);
                if (item == null || item.GCount <= 0)
                {
                    continue;
                }

                ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
                if (itemData == null || !ItemManager.KD_ISEQUIP(itemData.Genre))
                {
                    continue;
                }

                int originalUsing = session.OriginalEquipPositions.TryGetValue(dbId, out int savedUsing)
                    ? savedUsing
                    : ItemManager.g_anEquipPos[itemData.DetailType];

                item.Using = originalUsing;
                SendEquipmentStateChanged(player, item, originalUsing, ModGoodsTypes.EquipLoad);
                ItemManager.NotifyGoodsInfo(player, item);

                if (originalUsing >= (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_HEAD)
                {
                    lock (equipBody.PreventiveEquipBody)
                    {
                        equipBody.PreventiveEquipBody[(KE_EQUIP_POSITION_SUB)originalUsing] = new KItem(item);
                    }
                }
                else
                {
                    lock (equipBody.EquipBody)
                    {
                        equipBody.EquipBody[(KE_EQUIP_POSITION)originalUsing] = new KItem(item);
                    }
                }
            }

            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from RestoreOriginalEquipment() in KuaFuBalanceManager.State.cs");
            equipBody.AttackAllEquipBody();
        }

        private void DismountPlayerInBalance(KPlayer player)
        {
            if (player == null || !player.IsRiding)
            {
                return;
            }

            player.IsRiding = false;
            player.LastTickToggleHorseState = KTGlobal.GetCurrentTimeMilis();
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.HorseToggleOn, 0, true);
            KT_TCPHandler.SendHorseStateChanged(player);
        }

        private void RestoreOriginalLevel(KPlayer player, KuaFuBalanceSession session)
        {
            if (!session.HasTemporaryLevel || session.OriginalLevel <= 0 || player.m_Level == session.OriginalLevel)
            {
                return;
            }

            GameManager.ClientMgr.SetRoleLevel(player, session.OriginalLevel);
        }

        private void ApplyBaseOnlySkillPoints(KPlayer player, KuaFuBalanceSession session)
        {
            if (!session.HasTemporaryBonusSkillPointOverride)
            {
                if (!session.IsOriginalStateCaptured)
                {
                    session.OriginalBonusSkillPoints = player.GetBonusSkillPoint();
                }
                session.HasTemporaryBonusSkillPointOverride = true;
            }

            if (player.GetBonusSkillPoint() != 0)
            {
                player.SetTemporaryBonusSkillPoint(0, false);
            }
        }

        private void ApplyBaseOnlyPotentialPoints(KPlayer player, KuaFuBalanceSession session)
        {
            if (!session.HasTemporaryBonusPotentialOverride)
            {
                if (!session.IsOriginalStateCaptured)
                {
                    session.OriginalBonusPotentialPoints = player.GetBonusRemainPotentialPoints();
                }
                session.HasTemporaryBonusPotentialOverride = true;
            }

            if (player.GetBonusRemainPotentialPoints() != 0)
            {
                player.SetTemporaryBonusRemainPotentialPoints(0);
            }
        }

        private void RestoreOriginalBonusPotentialPoints(KPlayer player, KuaFuBalanceSession session)
        {
            if (!session.HasTemporaryBonusPotentialOverride)
            {
                return;
            }

            player.SetTemporaryBonusRemainPotentialPoints(session.OriginalBonusPotentialPoints);
            session.HasTemporaryBonusPotentialOverride = false;
        }

        private void RestoreOriginalSkillPoints(KPlayer player, KuaFuBalanceSession session)
        {
            if (session.HasTemporaryBonusSkillPointOverride)
            {
                player.SetTemporaryBonusSkillPoint(session.OriginalBonusSkillPoints);
                session.HasTemporaryBonusSkillPointOverride = false;
            }

            RestoreOriginalSkillBuild(player, session);
            session.HasTemporarySkillPointReset = false;
        }

        private void SnapshotOriginalSkillBuild(KPlayer player, KuaFuBalanceSession session)
        {
            if (player == null || player.Skills == null || session == null || session.OriginalSkillSnapshots.Count > 0 || session.IsOriginalStateCaptured)
            {
                return;
            }

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

        private void RestoreOriginalSkillBuild(KPlayer player, KuaFuBalanceSession session)
        {
            if (player == null || player.Skills == null || session == null || session.OriginalSkillSnapshots.Count <= 0)
            {
                return;
            }

            List<int> currentSkillIds = player.Skills.ListSkills.Select(x => x.SkillID).ToList();

            player.Skills.ResetAllSkillsLevel();

            foreach (KuaFuBalanceSkillSnapshot snapshot in session.OriginalSkillSnapshots.Values)
            {
                if (snapshot == null || !snapshot.HadSkill || snapshot.SkillId <= 0)
                {
                    continue;
                }

                player.Skills.RestoreSkillLevelAndExp(snapshot.SkillId, snapshot.AddedLevel, snapshot.Exp);
            }

            if (currentSkillIds != null)
            {
                foreach (int skillId in currentSkillIds)
                {
                    if (!session.OriginalSkillSnapshots.ContainsKey(skillId))
                    {
                        LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Remove temporary LSV skill. RoleID={0}, SkillID={1}", player.RoleID, skillId));
                        player.Skills.RemoveSkill(skillId);
                    }
                }
            }

            KT_TCPHandler.SendRenewSkillList(player);
            session.OriginalSkillSnapshots.Clear();
        }

        private void RestoreNewbieAttackSkills(KPlayer player)
        {
            if (player == null || player.Skills == null)
            {
                return;
            }

            List<KeyValuePair<int, int>> restoreLevels = new List<KeyValuePair<int, int>>();
            foreach (int skillId in KTLogic.NewbieAttackSkillIds)
            {
                SkillLevelRef skillRef = player.Skills.GetSkillLevelRef(skillId);
                if (skillRef == null)
                {
                    player.Skills.RestoreSkillLevelAndExp(skillId, 1, 0);
                    continue;
                }

                if (skillRef.AddedLevel <= 0)
                {
                    restoreLevels.Add(new KeyValuePair<int, int>(skillId, 1));
                }
            }

            if (restoreLevels.Count > 0)
            {
                player.Skills.AddSkillsLevel(restoreLevels);
            }
        }

        private void ResetTemporaryPotential(KPlayer player, KuaFuBalanceSession session)
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

            KT_TCPHandler.NotifyAttribute(player);
            PlayerManager.ShowNotification(player, "Đã tẩy điểm tiềm năng tạm thời, hãy cộng lại theo phái/nhánh đã chọn.");
        }

        private void ClearTemporaryPotential(KPlayer player, KuaFuBalanceSession session)
        {
            if (!session.HasTemporaryPotentialReset)
            {
                return;
            }

            lock (player.PropPointMutex)
            {
                player.UnAssignPotential();
            }
        }

        private void RestoreOriginalPotential(KPlayer player, KuaFuBalanceSession session)
        {
            RestoreOriginalBonusPotentialPoints(player, session);

            // Luôn khôi phục điểm tiềm năng gốc của người chơi khi rời đấu trường cân bằng,
            // không kiểm tra cờ HasTemporaryPotentialReset vì việc hạ cấp tạm thời khi vào map
            // đã tự động gọi UnAssignPotential trong core game để reset điểm tiềm năng.

            bool restored = true;
            lock (player.PropPointMutex)
            {
                player.UnAssignPotential(); // Thu hồi toàn bộ điểm tự động phân phối khi lên lại cấp độ gốc

                int totalOriginalPotential = session.OriginalStrength + session.OriginalDexterity + session.OriginalVitality + session.OriginalEnergy;
                if (totalOriginalPotential > 0)
                {
                    restored = player.AssignPotential(session.OriginalStrength, session.OriginalDexterity, session.OriginalVitality, session.OriginalEnergy);
                }

                if (restored)
                {
                    session.HasTemporaryPotentialReset = false;
                }
            }

            if (!restored)
            {
                LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Restore original potential failed. RoleID={0}, Str={1}, Dex={2}, Vit={3}, Ene={4}", player.RoleID, session.OriginalStrength, session.OriginalDexterity, session.OriginalVitality, session.OriginalEnergy));
            }

            KT_TCPHandler.NotifyAttribute(player);
        }

        private void EnsureCurrentLifeManaValid(KPlayer player)
        {
            if (player.m_CurrentLifeMax > 0 && player.m_CurrentLife <= 0)
            {
                player.m_CurrentLife = player.m_CurrentLifeMax;
            }
            else if (player.m_CurrentLife > player.m_CurrentLifeMax)
            {
                player.m_CurrentLife = player.m_CurrentLifeMax;
            }

            if (player.m_CurrentManaMax > 0 && player.m_CurrentMana < 0)
            {
                player.m_CurrentMana = player.m_CurrentManaMax;
            }
            else if (player.m_CurrentMana > player.m_CurrentManaMax)
            {
                player.m_CurrentMana = player.m_CurrentManaMax;
            }
        }

        private GoodsData GetEquippedTemporaryBook(KPlayer player, KuaFuBalanceSession session)
        {
            if (player.GoodsDataList == null || session == null)
            {
                return null;
            }

            lock (player.GoodsDataList)
            {
                return player.GoodsDataList
                    .FirstOrDefault(x => x != null &&
                        x.GCount > 0 &&
                        x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_BOOK &&
                        session.TempItemDbIds.Contains(x.Id) &&
                        !session.OriginalEquipDbIds.Contains(x.Id));
            }
        }

        private void SnapshotTemporaryBookSkills(KPlayer player, KuaFuBalanceSession session, ItemData itemData)
        {
            if (player == null || session == null || itemData == null || itemData.BookProperty == null)
            {
                return;
            }

            foreach (int skillId in GetBookSkillIds(itemData))
            {
                if (session.TempBookSkillSnapshots.ContainsKey(skillId))
                {
                    continue;
                }

                SkillLevelRef skillRef = player.Skills.GetSkillLevelRef(skillId);
                session.TempBookSkillSnapshots[skillId] = new KuaFuBalanceSkillSnapshot()
                {
                    SkillId = skillId,
                    HadSkill = skillRef != null,
                    AddedLevel = skillRef != null ? skillRef.AddedLevel : 0,
                    Exp = skillRef != null ? skillRef.Exp : 0,
                };
            }
        }

        private void RestoreTemporaryBookSkills(KPlayer player, KuaFuBalanceSession session)
        {
            if (player == null || session == null || session.TempBookSkillSnapshots.Count <= 0)
            {
                return;
            }

            if (!IsEnabled)
            {
                session.TempBookSkillSnapshots.Clear();
                return;
            }

            foreach (KuaFuBalanceSkillSnapshot snapshot in session.TempBookSkillSnapshots.Values)
            {
                if (snapshot.HadSkill)
                {
                    player.Skills.RestoreSkillLevelAndExp(snapshot.SkillId, snapshot.AddedLevel, snapshot.Exp);
                }
                else if (player.Skills.HasSkill(snapshot.SkillId))
                {
                    SkillDataEx skillData = KSkill.GetSkillData(snapshot.SkillId);
                    if (skillData != null && skillData.IsExpSkill)
                    {
                        continue;
                    }
                    player.Skills.RemoveSkill(snapshot.SkillId);
                }
            }

            session.TempBookSkillSnapshots.Clear();
        }

        private static IEnumerable<int> GetBookSkillIds(ItemData itemData)
        {
            if (itemData == null || itemData.BookProperty == null)
            {
                yield break;
            }

            int[] skillIds = new int[]
            {
                itemData.BookProperty.SkillID1,
                itemData.BookProperty.SkillID2,
                itemData.BookProperty.SkillID3,
                itemData.BookProperty.SkillID4,
            };

            foreach (int skillId in skillIds)
            {
                if (skillId > 0 && KSkill.GetSkillData(skillId) != null)
                {
                    yield return skillId;
                }
            }
        }

    }
}
