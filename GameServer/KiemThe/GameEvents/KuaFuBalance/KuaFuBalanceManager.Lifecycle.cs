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
        /// <summary>
        /// Kiểm tra người chơi đã chọn phái/nhánh tạm trong LSV cân bằng chưa (dựa vào session).
        /// </summary>
        public bool HasSelectedTemporaryFaction(KPlayer player)
        {
            if (player == null)
            {
                return false;
            }

            return sessions.TryGetValue(player.RoleID, out KuaFuBalanceSession session)
                && session.TempFactionId > 0
                && session.TempRouteId > 0;
        }

        public bool SelectTemporaryFaction(KPlayer player, int factionId, int routeId)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            if (factionId <= 0 || routeId <= 0)
            {
                PlayerManager.ShowNotification(player, "Lựa chọn phái/nhánh tạm không hợp lệ.");
                return false;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));

            if (!PrepareCurrentEquipment(player, session))
            {
                return false;
            }

            if (!ChangeTemporaryFaction(player, session, factionId, routeId))
            {
                RestoreOriginalEquipment(player, session);
                return false;
            }

            if (session.NeedsDefaultPointReset)
            {
                ResetDefaultTemporaryPoints(player, session);
            }

            PlayerManager.ShowNotification(player, "Đã chọn phái/nhánh tạm.");
            return true;
        }

        public bool ResetTemporaryPotential(KPlayer player)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            if (player.m_cPlayerFaction.GetFactionId() <= 0 || player.m_cPlayerFaction.GetRouteId() <= 0)
            {
                PlayerManager.ShowNotification(player, "Bạn cần chọn phái/nhánh tạm trước khi tẩy điểm tiềm năng.");
                return false;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));
            ResetTemporaryPotential(player, session);
            return true;
        }

        public bool ResetTemporarySkillPoints(KPlayer player)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            if (player.m_cPlayerFaction.GetFactionId() <= 0 || player.m_cPlayerFaction.GetRouteId() <= 0)
            {
                PlayerManager.ShowNotification(player, "Bạn cần chọn phái/nhánh tạm trước khi tẩy điểm kỹ năng.");
                return false;
            }

            if (player.Skills == null)
            {
                PlayerManager.ShowNotification(player, "Dữ liệu kỹ năng không hợp lệ.");
                return false;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));
            SnapshotOriginalSkillBuild(player, session);
            ApplyBaseOnlySkillPoints(player, session);
            session.HasTemporarySkillPointReset = true;
            player.Skills.ResetAllSkillsLevel();
            RestoreNewbieAttackSkills(player);
            PlayerManager.ShowNotification(player, "Đã tẩy điểm kỹ năng tạm thời, hãy cộng lại theo phái/nhánh đã chọn.");
            return true;
        }

        public bool ReissueTemporaryEquipment(KPlayer player)
        {
            if (player == null)
            {
                return false;
            }

            return ReceiveTemporaryEquipment(player);
        }

        public bool SetTemporaryLevel(KPlayer player, int level)
        {
            if (!CanUseBalance(player))
            {
                return false;
            }

            if (level <= 0)
            {
                PlayerManager.ShowNotification(player, "Cấp tạm không hợp lệ.");
                return false;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));
            if (!PrepareCurrentEquipment(player, session))
            {
                return false;
            }

            session.HasTemporaryLevel = true;
            if (player.m_Level != level)
            {
                lock (player.PropPointMutex)
                {
                    player.UnAssignPotential();
                }
                GameManager.ClientMgr.SetRoleLevel(player, level);
            }

            ApplyBaseOnlySkillPoints(player, session);
            EnsureCurrentLifeManaValid(player);
            PlayerManager.ShowNotification(player, string.Format("Đã nhận cấp {0} tạm thời.", level));
            return true;
        }

        public void LeaveBalance(KPlayer player, bool switchBackServer)
        {
            if (player == null)
            {
                return;
            }

            if (!sessions.TryRemove(player.RoleID, out KuaFuBalanceSession session))
            {
                if (switchBackServer)
                {
                    KuaFuManager.getInstance().GotoLastMap(player);
                }

                KuaFuBalanceOriginalSnapshot snapshot = LoadOriginalSnapshot(player);
                if (snapshot != null && snapshot.OriginalLevel > 0)
                {
                    ForceRestoreOriginalState(player, snapshot);
                }

                return;
            }

            if (!session.HasBlockedTemporaryProfileRestore)
            {
                SaveTemporaryProfile(player, session);
            }

            // Xóa items mua trong balance và khôi phục toàn bộ trạng thái gốc
            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] LeaveBalance start. RoleID={0}, MapCode={1}, TempItems={2}, KuafuMoney={3}, OriginalKuafuMoney={4}",
                player.RoleID, player.CurrentMapCode, session.TempItemDbIds.Count, player.KuafuMoney, session.OriginalKuafuMoney));
            PerformFullRestore(player, session);

            if (switchBackServer)
            {
                if (!IsEnabled)
                {
                    Global.SaveRoleParamsStringWithNullToDB(player, RoleParamName.KuaFuBalanceOriginalSnapshot, "", true);
                }
                KuaFuManager.getInstance().GotoLastMap(player);
            }
        }

        public void OnEnterMap(KPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (!IsBalanceMap(player))
            {
                bool restored = false;

                // 1. Ưu tiên khôi phục từ session trên RAM của Server chính nếu còn
                if (sessions.TryRemove(player.RoleID, out KuaFuBalanceSession localSession))
                {
                    LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Restore from local session RAM. RoleID={0}", player.RoleID));
                    PerformFullRestore(player, localSession);
                    restored = true;
                }
                // 2. Nếu RAM bị mất, luôn luôn kiểm tra DB snapshot để khôi phục (đề phòng văng game/login thông thường)
                else
                {
                    KuaFuBalanceOriginalSnapshot snapshot = LoadOriginalSnapshot(player);
                    if (snapshot != null && snapshot.OriginalLevel > 0)
                    {
                        LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Restore from DB snapshot. RoleID={0}", player.RoleID));
                        ForceRestoreOriginalState(player, snapshot);
                        restored = true;
                    }
                }

                // Nếu đã khôi phục thành công (từ RAM hoặc DB), ta dọn dẹp DB snapshot và reset cờ switch server
                if (restored)
                {
                    Global.SaveRoleParamsStringWithNullToDB(player, RoleParamName.KuaFuBalanceOriginalSnapshot, "", true);
                    player.isSwitchServer = false;
                }

                ForceCleanUpAllTemporaryItems(player);
                return;
            }

            KuaFuBalanceSession session = sessions.GetOrAdd(player.RoleID, _ => CreateSession(player));

            KuaFuBalanceOriginalSnapshot dbSnapshot = LoadOriginalSnapshot(player);
            if (dbSnapshot != null)
            {
                session.OriginalFactionId = dbSnapshot.OriginalFactionId;
                session.OriginalRouteId = dbSnapshot.OriginalRouteId;
                session.OriginalLevel = dbSnapshot.OriginalLevel;
                session.OriginalKuafuMoney = dbSnapshot.OriginalKuafuMoney;
                session.OriginalBonusSkillPoints = dbSnapshot.OriginalBonusSkillPoints;
                session.OriginalBonusPotentialPoints = dbSnapshot.OriginalBonusPotentialPoints;
                session.OriginalStrength = dbSnapshot.OriginalStrength;
                session.OriginalDexterity = dbSnapshot.OriginalDexterity;
                session.OriginalVitality = dbSnapshot.OriginalVitality;
                session.OriginalEnergy = dbSnapshot.OriginalEnergy;
                session.OriginalEquipDbIds.Clear();
                session.OriginalEquipDbIds.AddRange(dbSnapshot.OriginalEquipDbIds);
                session.OriginalEquipPositions.Clear();
                foreach (var kvp in dbSnapshot.OriginalEquipPositions)
                {
                    session.OriginalEquipPositions[kvp.Key] = kvp.Value;
                }
                session.OriginalSkillSnapshots.Clear();
                foreach (var kvp in dbSnapshot.OriginalSkillSnapshots)
                {
                    session.OriginalSkillSnapshots[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                session.OriginalKuafuMoney = player.KuafuMoney;
                CaptureOriginalState(player, session);
                SaveOriginalSnapshot(player, session);
            }
            LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] OnEnterMap. RoleID={0}, MapCode={1}, ExistingTempItems={2}, KuafuMoney={3}",
                player.RoleID, player.CurrentMapCode, session.TempItemDbIds.Count, player.KuafuMoney));
            if (!PrepareCurrentEquipment(player, session))
            {
                return;
            }
            SaveOriginalSnapshot(player, session);

            DismountPlayerInBalance(player);

            if (session.TempItemDbIds.Count <= 0)
            {
                bool restoredProfile = RestoreTemporaryProfile(player, session, out bool hasSavedProfile);
                if (!restoredProfile && !hasSavedProfile)
                {
                    ResetDefaultTemporaryLevel(player, session);
                }

                if (!restoredProfile && !session.HasTemporaryPotentialReset && !session.HasTemporarySkillPointReset)
                {
                    session.NeedsDefaultPointReset = true;
                }
            }

            if (session.TempItemDbIds.Count > 0)
            {
                ApplyWingBuff(player, session);
            }
        }

        public void OnLogout(KPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (IsBalanceMap(player))
            {
                LeaveBalance(player, false);
            }
        }

        private bool CanUseBalance(KPlayer player)
        {
            if (player == null)
            {
                return false;
            }

            if (ServerConfig.Instance == null || !ServerConfig.Instance.IsKuaFuBalance)
            {
                PlayerManager.ShowNotification(player, "Liên server cân bằng chưa được mở.");
                return false;
            }

            if (!GameManager.IsKuaFuServer || !IsBalanceMap(player))
            {
                PlayerManager.ShowNotification(player, "Chỉ có thể dùng tính năng này trong Hoàng Thành LSV Cân Bằng.");
                return false;
            }

            int mantleLevel = GetSessionMantleLevel(player);
            if (mantleLevel < config.Join.MinMantleLevel)
            {
                PlayerManager.ShowNotification(player, string.Format("Cần mặc Phi Phong cấp {0} trở lên để tham gia.", config.Join.MinMantleLevel));
                return false;
            }

            return true;
        }

        private KuaFuBalanceSession CreateSession(KPlayer player)
        {
            return new KuaFuBalanceSession()
            {
                RoleId = player.RoleID,
                MantleLevel = Global.GetMantleLevel(player),
                OriginalFactionId = player.m_cPlayerFaction.GetFactionId(),
                OriginalRouteId = player.m_cPlayerFaction.GetRouteId(),
                OriginalLevel = player.m_Level,
                OriginalKuafuMoney = player.KuafuMoney,
            };
        }

        /// <summary>
        /// Thực hiện khôi phục đầy đủ toàn bộ trạng thái gốc (Bạc LSV, đồ tạm, buff, phái, cấp, kỹ năng, tiềm năng).
        /// </summary>
        private void PerformFullRestore(KPlayer player, KuaFuBalanceSession session)
        {
            if (player == null || session == null)
            {
                return;
            }

            CleanUpBalancePurchasedItems(player, session);
            RestoreKuafuMoney(player, session);
            ClearAllBalanceBuffs(player, session);
            RemoveEquippedTemporaryItems(player, session, true);
            ClearTemporaryPotential(player, session);
            RestoreTemporaryBookSkills(player, session);
            RestoreOriginalFaction(player, session);
            RestoreOriginalLevel(player, session);
            RestoreOriginalSkillPoints(player, session);
            RestoreOriginalPotential(player, session);
            RestoreOriginalEquipment(player, session);
            EnsureCurrentLifeManaValid(player);
        }

        private int GetSessionMantleLevel(KPlayer player)
        {
            if (sessions.TryGetValue(player.RoleID, out KuaFuBalanceSession session) && session.MantleLevel > 0)
            {
                return session.MantleLevel;
            }

            return Global.GetMantleLevel(player);
        }

    }
}
