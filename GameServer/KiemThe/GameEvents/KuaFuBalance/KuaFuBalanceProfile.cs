using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    internal sealed class KuaFuBalanceProfile
    {
        public int FactionId { get; set; }

        public int RouteId { get; set; }

        public int SeriesId { get; set; }

        public bool HasTemporaryLevel { get; set; }

        public int TemporaryLevel { get; set; }

        public bool HasTemporaryPotential { get; set; }

        public int Strength { get; set; }

        public int Dexterity { get; set; }

        public int Vitality { get; set; }

        public int Energy { get; set; }

        public bool HasTemporarySkillReset { get; set; }

        public bool HasTemporaryBookSkills { get; set; }

        public List<KuaFuBalanceProfileSkill> Skills { get; set; } = new List<KuaFuBalanceProfileSkill>();

        public List<KuaFuBalanceProfileItem> Items { get; set; } = new List<KuaFuBalanceProfileItem>();

        public bool HasTemporaryBuild()
        {
            return (FactionId > 0 && RouteId > 0) ||
                HasTemporaryLevel ||
                HasTemporaryPotential ||
                HasTemporarySkillReset ||
                HasTemporaryBookSkills ||
                (Items != null && Items.Count > 0);
        }
    }

    internal sealed class KuaFuBalanceProfileItem
    {
        public int GoodsId { get; set; }

        public int Count { get; set; }

        public int Series { get; set; }

        public int Binding { get; set; }

        public int EnhanceLevel { get; set; }

        public int Using { get; set; }

        public string EndTime { get; set; }

        public string Props { get; set; }

        public Dictionary<int, string> OtherParams { get; set; } = new Dictionary<int, string>();
    }

    internal sealed class KuaFuBalanceProfileSkill
    {
        public int SkillId { get; set; }

        public int AddedLevel { get; set; }

        public int Exp { get; set; }
    }
}
