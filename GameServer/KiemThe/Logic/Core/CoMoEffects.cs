using System;
using System.Linq;
using GameServer.KiemThe.Entities;
using GameServer.Logic;

namespace GameServer.KiemThe.Logic
{
    /// <summary>Co Mo mechanics not expressible by the legacy flat-stat symbols.
    /// Percent endpoints are configured in SkillData Param1/Param2, not hard-coded here.
    /// Read current learned levels on demand, so respec/route changes never stack bonuses.</summary>
    public static class CoMoEffects
    {
        public static int Interpolate(int first, int last, int level, int maxLevel)
        {
            if (level <= 0) return 0;
            if (maxLevel <= 1) return first;
            level = Math.Min(level, maxLevel);
            return first + (last - first) * (level - 1) / (maxLevel - 1);
        }

        public static int PassivePercent(GameObject actor, int id)
        {
            var player = actor as KPlayer;
            if (player == null || player.m_cPlayerFaction.GetFactionId() != 13) return 0;
            var skill = player.Skills.GetSkillLevelRef(id);
            if (skill == null || skill.Level <= 0 || skill.AddedLevel <= 0 ||
                skill.Data.Type != 3 || player.m_cPlayerFaction.GetRouteId() != skill.Data.SubID) return 0;
            return Interpolate(skill.Data.Params[0], skill.Data.Params[1], skill.Level, skill.Data.MaxSkillLevel);
        }

        public static int Scale(int value, int percent) => value + (int)((long)value * percent / 100);

        public static bool IsBelowPercent(int life, int maximumLife, int threshold)
            => life > 0 && maximumLife > 0 && (long)life * 100 < (long)maximumLife * threshold;

        public static int CriticalDamageBonus(GameObject actor, SkillLevelRef skill)
            => PassivePercent(actor, skill.Data.IsPhysical ? 16007 : 16018);

        // The legacy reducer skips secondary damage (e.g. poison ticks). These two
        // Co Mo protections explicitly cover incoming attacks, including those ticks.
        public static int SecondaryDamageReduction(GameObject actor)
        {
            int reduction = 0;
            var player = actor as KPlayer;
            if (player != null && player.m_cPlayerFaction.GetFactionId() == 13 &&
                player.m_cPlayerFaction.GetRouteId() == 2)
            {
                var passive = player.Skills.GetSkillLevelRef(16026);
                if (passive != null && passive.AddedLevel > 0)
                    reduction += passive.Properties.Get<KMagicAttrib>((int)MAGIC_ATTRIB.magic_reduce_received_damage_p)?.nValue[0] ?? 0;
            }
            var shield = actor.Buffs.GetBuff(16506);
            if (shield != null)
                reduction += shield.Skill.Properties.Get<KMagicAttrib>((int)MAGIC_ATTRIB.magic_reduce_received_damage_p)?.nValue[0] ?? 0;
            return Math.Max(0, Math.Min(99, reduction));
        }

        public static bool HasHarmfulState(GameObject target)
        {
            for (int state = (int)KE_STATE.emSTATE_HURT; state <= (int)KE_STATE.emSTATE_STUN; state++)
                if (target.HaveState((KE_STATE)state)) return true;
            for (int state = (int)KE_STATE.emSTATE_FIXED; state <= (int)KE_STATE.emSTATE_FLOAT; state++)
                if (target.HaveState((KE_STATE)state)) return true;
            return target.Buffs.ListBuffs.Any(buff => buff.CurseOwner != null);
        }

        public static bool IsForcedCritical(SkillLevelRef skill, GameObject target)
            => skill.SkillID == 16011 && skill.Data.Params[4] == 100 && HasHarmfulState(target);

        public static void ApplyHitDebuff(GameObject attacker, GameObject target, SkillLevelRef skill)
        {
            if (skill.SkillID != 16017 || target.IsDead() || skill.Data.Params[4] <= 0 ||
                KTGlobal.GetRandomNumber(1, 100) > skill.Data.Params[3]) return;
            var data = KSkill.GetSkillData(skill.Data.Params[4]);
            if (data == null) return;
            var buffSkill = new SkillLevelRef { Data = data, AddedLevel = skill.Level };
            var duration = buffSkill.Properties.Get<KMagicAttrib>(
                (int)MAGIC_ATTRIB.magic_skill_statetime);
            if (duration == null || duration.nValue[0] <= 0) return;
            var buff = new BuffDataEx
            {
                Skill = buffSkill, StackCount = 1, SaveToDB = false,
                Duration = (long)duration.nValue[0] * 1000 / 18,
                StartTick = KTGlobal.GetCurrentTimeMilis(), CurseOwner = attacker,
            };
            if (KTGlobal.GetRandomNumber(1, 100) <= target.m_CurrentReturnSkillPercent)
            {
                buff.CurseOwner = target;
                attacker.Buffs.AddBuff(buff);
            }
            else target.Buffs.AddBuff(buff);
        }
    }
}
