using GameServer.KiemThe.Core;
using System.Collections.Generic;

namespace GameServer.KiemThe.GameEvents.TowerDefense
{
    /// <summary>
    /// Trạng thái vận hành của một trụ phòng thủ.
    /// </summary>
    internal class TowerDefenseState
    {
        /// <summary>
        /// ID NPC đại diện cho trụ.
        /// </summary>
        public int TowerNPCID { get; set; }

        /// <summary>
        /// ID bản đồ đặt trụ.
        /// </summary>
        public int MapCode { get; set; }

        /// <summary>
        /// ID bản sao bản đồ; map thường dùng -1.
        /// </summary>
        public int CopyMapID { get; set; } = -1;

        /// <summary>
        /// Tọa độ trung tâm của trụ.
        /// </summary>
        public int PosX { get; set; }

        public int PosY { get; set; }

        /// <summary>
        /// Bán kính phòng thủ: đi vào vùng này trụ bắt đầu sinh quái.
        /// </summary>
        public double DefenseRadius { get; set; } = 900;

        /// <summary>
        /// Bán kính nguy hiểm: trụ vừa sinh quái vừa tấn công.
        /// </summary>
        public double DangerRadius { get; set; } = 450;

        /// <summary>
        /// Máu của trụ.
        /// </summary>
        public int CurrentHP { get; set; }

        public int MaxHP { get; set; }

        /// <summary>
        /// Trụ còn hoạt động hay không.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Thời điểm sinh đợt quái gần nhất, tính bằng milliseconds.
        /// </summary>
        public long LastMonsterSpawnTick { get; set; }

        /// <summary>
        /// Khoảng nghỉ giữa hai đợt quái.
        /// </summary>
        public long MonsterSpawnCooldown { get; set; } = 10000;

        /// <summary>
        /// Thời điểm trụ tấn công gần nhất.
        /// </summary>
        public long LastAttackTick { get; set; }

        /// <summary>
        /// Khoảng nghỉ giữa hai lần trụ tấn công.
        /// </summary>
        public long AttackCooldown { get; set; } = 2000;

        /// <summary>
        /// Giới hạn quái do trụ tạo ra đang còn sống.
        /// </summary>
        public int MaxAliveMonsters { get; set; } = 10;

        /// <summary>
        /// Số yêu cầu sinh quái đã xếp hàng nhưng callback tạo quái chưa chạy.
        /// </summary>
        public int PendingMonsterSpawns { get; set; }

        /// <summary>
        /// RoleID của những quái do trụ sinh ra.
        /// </summary>
        public HashSet<int> SpawnedMonsterRoleIDs { get; }
            = new HashSet<int>();

        /// <summary>
        /// RoleID của người chơi đang nằm trong vùng phòng thủ.
        /// </summary>
        public HashSet<int> PlayersInDefenseZone { get; }
            = new HashSet<int>();

        /// <summary>
        /// RoleID của người chơi đang nằm trong vùng nguy hiểm.
        /// </summary>
        public HashSet<int> PlayersInDangerZone { get; }
            = new HashSet<int>();
    }
}
