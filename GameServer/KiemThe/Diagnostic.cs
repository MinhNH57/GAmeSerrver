using System.Collections.Generic;
using System.Web.UI;
using GameServer.Logic;

namespace GameServer.KiemThe
{
    public class Diagnostic
    {
        public static int PlayerTimerCounter;
        public static int PlayerGridTimerCounter;
        public static int NormalMonsterTimerCounter;
        public static int NormalBossTimerCounter;
        public static int SpecialMonsterTimerCounter;
        public static int SpecialBossTimerCounter;
        public static int DynamicNPCTimerCounter;
        public static int BulletTimerCounter;
        public static int MonsterStoryboardTimerCounter;
        public static int PlayerStoryboardTimerCounter;
        public static Dictionary<string, int> TimerManager = new Dictionary<string, int>();
        public static int SpecialEventTimerCounter;
        public static int TeamBattleTimerCounter;
        public static int BulletExplodeTimerCounter;

        public static string GetReport()
        {
            var str = "";
            if (GameManager.ServerMonitor.GetCpuAndMem(out var cpuLoad, out var mem))
            {
                str = $"CPU[{(int)cpuLoad}], RAM[{mem}].";
            }
            str =  $"{str} Timers: " +
                   $"Player [{PlayerTimerCounter}], PlayerGrid [{PlayerGridTimerCounter}], " +
                   $"NormalMonster[{NormalMonsterTimerCounter}], NormalBoss[{NormalBossTimerCounter}], " +
                   $"SpecialMonster[{SpecialMonsterTimerCounter}], SpecialBoss[{SpecialBossTimerCounter}], " +
                   $"DynamicNPC[{DynamicNPCTimerCounter}], " +
                   $"Bullet[{BulletTimerCounter}], BulletExplode[{BulletExplodeTimerCounter}], " +
                   $"PlayerStb[{PlayerStoryboardTimerCounter}], MonsterStb[{MonsterStoryboardTimerCounter}]";
            foreach (var timer in TimerManager)
            {
                str += $", {timer.Key}[{timer.Value}]";
            }

            return str;
        }
    }
}