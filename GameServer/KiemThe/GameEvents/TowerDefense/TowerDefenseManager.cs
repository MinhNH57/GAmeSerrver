using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using Direction = GameServer.KiemThe.Entities.Direction;

namespace GameServer.KiemThe.GameEvents.TowerDefense
{
    public static class TowerDefenseManager
    {
        private const int TowerMapCode = 52;
        private const int TowerTemplateID = 30000;
        private const int TowerPosX = 6000;
        private const int TowerPosY = 5650;
        private const int DefenderMonsterID = 2038;
        private const int MonstersPerWave = 3;
        private const int SpawnOffset = 120;
        private const int TowerAttackDamage = 100;
        private const int TickInterval = 1000;

        private static readonly object StateLock = new object();
        private static readonly TowerDefenseState State = new TowerDefenseState
        {
            TowerNPCID = TowerTemplateID,
            MapCode = TowerMapCode,
            CopyMapID = -1,
            PosX = TowerPosX,
            PosY = TowerPosY,
            MaxHP = 100000,
            CurrentHP = 100000,
        };

        private static Timer timer;
        private static bool isTickRunning;

        /// <summary>
        /// Khởi động hệ thống phòng thủ trụ. Hàm này an toàn khi bị gọi lặp lại.
        /// </summary>
        public static void Init()
        {
            lock (StateLock)
            {
                if (timer != null)
                {
                    return;
                }

                timer = new Timer(TickInterval) { AutoReset = true };
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }

            LogManager.WriteLog(LogTypes.Info,
                $"[TowerDefense] Khởi động: TowerTemplate={TowerTemplateID}, Map={TowerMapCode}, " +
                $"Pos=({TowerPosX},{TowerPosY}), DefenseRadius={State.DefenseRadius}, " +
                $"DangerRadius={State.DangerRadius}");
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (StateLock)
            {
                if (isTickRunning)
                {
                    return;
                }
                isTickRunning = true;
            }

            try
            {
                if (Program.NeedExitServer)
                {
                    timer?.Stop();
                    return;
                }
                Tick();
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, $"[TowerDefense] Tick lỗi: {ex}");
            }
            finally
            {
                lock (StateLock)
                {
                    isTickRunning = false;
                }
            }
        }

        /// <summary>
        /// Quét người chơi và xử lý hai lớp phòng thủ.
        /// </summary>
        private static void Tick()
        {
            if (!State.IsActive)
            {
                return;
            }

            List<KPlayer> mapPlayers =
                GameManager.ClientMgr.GetMapClients(State.MapCode, State.CopyMapID)
                ?? new List<KPlayer>();
            List<KPlayer> activePlayers = mapPlayers
                .Where(player => player != null && player.IsOnline() && !player.IsDead())
                .ToList();

            HashSet<int> defenseNow = new HashSet<int>();
            HashSet<int> dangerNow = new HashSet<int>();
            Point towerPosition = new Point(State.PosX, State.PosY);

            foreach (KPlayer player in activePlayers)
            {
                double distance = KTGlobal.GetDistanceBetweenPoints(towerPosition, player.CurrentPos);
                if (distance <= State.DefenseRadius)
                {
                    defenseNow.Add(player.RoleID);
                }
                if (distance <= State.DangerRadius)
                {
                    dangerNow.Add(player.RoleID);
                }
            }

            LogZoneTransitions(State.PlayersInDefenseZone, defenseNow, "vùng phòng thủ");
            LogZoneTransitions(State.PlayersInDangerZone, dangerNow, "vùng nguy hiểm");

            lock (StateLock)
            {
                ReplaceSet(State.PlayersInDefenseZone, defenseNow);
                ReplaceSet(State.PlayersInDangerZone, dangerNow);
            }

            KPlayer defenseTarget =
                activePlayers.FirstOrDefault(player => defenseNow.Contains(player.RoleID));
            if (defenseTarget != null)
            {
                TrySpawnWave(defenseTarget);
            }

            KPlayer dangerTarget =
                activePlayers.FirstOrDefault(player => dangerNow.Contains(player.RoleID));
            if (dangerTarget != null)
            {
                TryTowerAttack(dangerTarget);
            }
        }

        private static void LogZoneTransitions(
            HashSet<int> previous,
            HashSet<int> current,
            string zoneName)
        {
            int[] entered;
            int[] exited;
            lock (StateLock)
            {
                entered = current.Except(previous).ToArray();
                exited = previous.Except(current).ToArray();
            }

            foreach (int roleID in entered)
            {
                LogManager.WriteLog(LogTypes.Info,
                    $"[TowerDefense] Người chơi RoleID={roleID} đi vào {zoneName} của trụ.");
            }
            foreach (int roleID in exited)
            {
                LogManager.WriteLog(LogTypes.Info,
                    $"[TowerDefense] Người chơi RoleID={roleID} rời {zoneName} của trụ.");
            }
        }

        private static void ReplaceSet(HashSet<int> destination, HashSet<int> source)
        {
            destination.Clear();
            foreach (int value in source)
            {
                destination.Add(value);
            }
        }

        private static void TrySpawnWave(KPlayer target)
        {
            long now = KTGlobal.GetCurrentTimeMilis();
            int spawnCount;
            lock (StateLock)
            {
                if (now - State.LastMonsterSpawnTick < State.MonsterSpawnCooldown)
                {
                    return;
                }

                int availableSlots = State.MaxAliveMonsters
                    - State.SpawnedMonsterRoleIDs.Count
                    - State.PendingMonsterSpawns;
                spawnCount = Math.Min(MonstersPerWave, Math.Max(0, availableSlots));
                if (spawnCount == 0)
                {
                    return;
                }

                State.LastMonsterSpawnTick = now;
                State.PendingMonsterSpawns += spawnCount;
            }

            GetSpawnPositionTowards(target, out int spawnX, out int spawnY);
            for (int i = 0; i < spawnCount; i++)
            {
                if (!SpawnMonster(DefenderMonsterID, spawnX, spawnY, target.RoleID))
                {
                    lock (StateLock)
                    {
                        State.PendingMonsterSpawns--;
                    }
                }
            }
        }

        private static void GetSpawnPositionTowards(
            KPlayer target,
            out int spawnX,
            out int spawnY)
        {
            double dx = target.CurrentPos.X - State.PosX;
            double dy = target.CurrentPos.Y - State.PosY;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1)
            {
                spawnX = State.PosX;
                spawnY = State.PosY;
                return;
            }

            spawnX = State.PosX + (int)(dx / length * SpawnOffset);
            spawnY = State.PosY + (int)(dy / length * SpawnOffset);
        }

        private static void TryTowerAttack(KPlayer target)
        {
            long now = KTGlobal.GetCurrentTimeMilis();
            lock (StateLock)
            {
                if (now - State.LastAttackTick < State.AttackCooldown)
                {
                    return;
                }
                State.LastAttackTick = now;
            }

            int oldLife = target.m_CurrentLife;
            target.m_CurrentLife = Math.Max(0, target.m_CurrentLife - TowerAttackDamage);
            KT_TCPHandler.NotifySelfLifeChanged(target, target.m_CurrentLife);
            KT_TCPHandler.NotifyObjectLifeChangedToAllPlayers(target, target.m_CurrentLife);

            LogManager.WriteLog(LogTypes.Info,
                $"[TowerDefense] Trụ tấn công RoleID={target.RoleID}, " +
                $"Damage={oldLife - target.m_CurrentLife}, HP={oldLife}->{target.m_CurrentLife}");
        }

        public static bool SpawnMonster(
            int monsterID,
            int posX,
            int posY,
            int targetRoleID)
        {
            LogManager.WriteLog(LogTypes.Info,
                $"[TowerDefense] Yêu cầu sinh quái: MonsterID={monsterID}, " +
                $"Map={TowerMapCode}, Pos=({posX},{posY}), TargetRoleID={targetRoleID}");

            Monster spawnedMonster = null;
            bool accepted = GameManager.MonsterZoneMgr.AddDynamicMonsters(
                TowerMapCode,
                monsterID,
                -1,
                1,
                posX,
                posY,
                "Quái Thủ Trụ",
                "Bảo vệ Trụ Phòng Thủ",
                -1,
                -1,
                Direction.DOWN,
                KE_SERIES_TYPE.series_none,
                MonsterAIType.Normal,
                -1,
                -1,
                -1,
                $"TowerDefense:{targetRoleID}",
                monster =>
                {
                    spawnedMonster = monster;
                    lock (StateLock)
                    {
                        State.PendingMonsterSpawns =
                            Math.Max(0, State.PendingMonsterSpawns - 1);
                        State.SpawnedMonsterRoleIDs.Add(monster.RoleID);
                    }

                    KPlayer target = GameManager.ClientMgr.FindClient(targetRoleID);
                    if (target != null && target.IsOnline() && !target.IsDead())
                    {
                        // Gây 0 sát thương để kích hoạt AI đuổi đúng người chơi.
                        monster.TakeDamage(target, 0);
                    }

                    LogManager.WriteLog(LogTypes.Info,
                        $"[TowerDefense] Sinh quái thành công: RoleID={monster.RoleID}, " +
                        $"MonsterID={monsterID}, Pos=({monster.CurrentPos.X},{monster.CurrentPos.Y})");
                },
                -1,
                null,
                killer =>
                {
                    if (spawnedMonster == null)
                    {
                        return;
                    }
                    lock (StateLock)
                    {
                        State.SpawnedMonsterRoleIDs.Remove(spawnedMonster.RoleID);
                    }
                    LogManager.WriteLog(LogTypes.Info,
                        $"[TowerDefense] Quái bảo vệ đã chết: RoleID={spawnedMonster.RoleID}");
                });

            if (!accepted)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"[TowerDefense] Không thể xếp yêu cầu sinh quái. " +
                    $"Map={TowerMapCode}, MonsterID={monsterID}");
            }
            return accepted;
        }
    }
}
