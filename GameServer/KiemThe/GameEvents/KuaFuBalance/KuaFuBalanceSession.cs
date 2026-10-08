using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    internal sealed class KuaFuBalanceSession
    {
        public int RoleId { get; set; }

        public int MantleLevel { get; set; }

        public int OriginalFactionId { get; set; }

        public int OriginalRouteId { get; set; }

        public int OriginalLevel { get; set; }

        public bool HasTemporaryLevel { get; set; }

        public bool NeedsDefaultPointReset { get; set; }

        public bool HasTemporarySkillPointReset { get; set; }

        public bool HasTemporaryBonusSkillPointOverride { get; set; }

        public int OriginalBonusSkillPoints { get; set; }

        public bool HasTemporaryPotentialReset { get; set; }

        public bool HasTemporaryBonusPotentialOverride { get; set; }

        public int OriginalBonusPotentialPoints { get; set; }

        public int OriginalStrength { get; set; }

        public int OriginalDexterity { get; set; }

        public int OriginalVitality { get; set; }

        public int OriginalEnergy { get; set; }

        public int TempFactionId { get; set; }

        public int TempRouteId { get; set; }

        public int TempSeriesId { get; set; }

        public int BuffSkillId { get; set; }

        public List<int> OriginalEquipDbIds { get; private set; } = new List<int>();

        public Dictionary<int, int> OriginalEquipPositions { get; private set; } = new Dictionary<int, int>();

        public HashSet<int> TempItemDbIds { get; private set; } = new HashSet<int>();

        /// <summary>
        /// Số KuafuMoney (bạc khóa LSV) của player khi vào balance.
        /// Dùng để khôi phục khi rời, tránh carry over sang LSV thường.
        /// </summary>
        public int OriginalKuafuMoney { get; set; }

        public bool HasBlockedTemporaryProfileRestore { get; set; }

        public Dictionary<int, KuaFuBalanceSkillSnapshot> OriginalSkillSnapshots { get; private set; } = new Dictionary<int, KuaFuBalanceSkillSnapshot>();

        public Dictionary<int, KuaFuBalanceSkillSnapshot> TempBookSkillSnapshots { get; private set; } = new Dictionary<int, KuaFuBalanceSkillSnapshot>();

        /// <summary>
        /// Đánh dấu đã chụp trạng thái gốc của nhân vật hay chưa.
        /// </summary>
        public bool IsOriginalStateCaptured { get; set; }
    }

    internal sealed class KuaFuBalanceSkillSnapshot
    {
        public int SkillId { get; set; }

        public bool HadSkill { get; set; }

        public int AddedLevel { get; set; }

        public int Exp { get; set; }
    }
}
