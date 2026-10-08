using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using Newtonsoft.Json;

namespace GameServer.KiemThe.GameEvents.MonthlyEvents
{
    public static class Event_2024Apr
    {
        public static int BossIDFree = 9838;
        public static int BossIDVip = 9839;

        public static int NPCIDProvoke = 20199;
        public static int NPCIDReward = 20200;

        public static ConcurrentDictionary<string, List<Tuple<int, long>>> DamageRecord;
        public static ConcurrentDictionary<string, int> BossCaller;
        public static ConcurrentDictionary<int, string> NameDict;

        public static List<Monster> Bosses;

        [Serializable]
        public class DaoTacData
        {
            public Dictionary<int, List<List<int>>> position;
            public int boss_per_map;
            public string message_prepare;
            public string message_appear;
            public string message_escape; 
        }
        
        private static DaoTacData daoTacData;

#region boss đạo tặc
        public static void NotifyPrepare() 
        {
            KTGlobal.SendSystemEventNotification(daoTacData.message_prepare);
        }

        public static void StartBossesWave()
        {
            KTGlobal.SendSystemEventNotification(daoTacData.message_appear);

            foreach (var mapCode in daoTacData.position.Keys) 
            {
                var positionInMap = daoTacData.position[mapCode];
                StaticRandom.Shuffle(positionInMap);
                for (var i = 0; i < daoTacData.boss_per_map; ++i) 
                {
                    var posX = positionInMap[i][0];
                    var posY = positionInMap[i][1];

                    var tag = $"DaoTacThangTu_{StaticRandom.GetRandom(100000000)}";
                    NPCGeneralManager.AddNewNPC(mapCode, -1, 20199, posX, posY, "Đạo tặc Sinh nhật", "[Khiêu chiến]", Entities.Direction.DOWN, 232, tag, (npc) => {
                    npc.MinimapName = "Đạo tặc Sinh nhật";
                    npc.VisibleOnMinimap = true;
                });
                }
            }
        }

        public static void EndBossesWave() 
        {
            KTGlobal.SendSystemEventNotification(daoTacData.message_escape);
            NPCGeneralManager.RemoveNPCsByScriptID(232);
            NPCGeneralManager.RemoveNPCsByScriptID(233);
            RemoveAllBoss();
        }
        public static void AddBoss(Monster boss) 
        {
            Bosses.Add(boss);
        }

        public static void RemoveDeadBoss(Monster boss)
        {
            Bosses.Remove(boss);
        }

        public static void RemoveAllBoss()
        {
            foreach (var boss in Bosses) 
            {
                // Nếu bản đồ không tồn tại
                if (!GameManager.MapMgr.DictMaps.TryGetValue(boss.CurrentMapCode, out var _))
                {
                    continue;
                }
                boss.MonsterZoneNode?.DestroyMonster(boss);
            }
        }

        public static void AddNameLookup(int rid, string rname)
        {
            NameDict[rid] = rname;
        }

        public static string GetNameFromRid(int rid) 
        {
            if (NameDict.TryGetValue(rid, out var rname)) {
                return rname;
            }
            return "";
        }

        public static bool IsEventBoss(int monsterID)
        {
            return monsterID == BossIDFree || monsterID == BossIDVip;
        }

        public static void SaveBossCaller(string monsterTag, int rid)
        {
            BossCaller[monsterTag] = rid;
        }

        public static int GetBossCaller(string monsterTag) 
        {
            monsterTag = monsterTag.Replace("_DONE", "");
            if (BossCaller.TryGetValue(monsterTag, out var rid)) 
            {
                return rid;
            }

            return -1;
        }

        public static void SaveDamageRecord(string monsterTag,
            ConcurrentDictionary<int, Monster.DamgeGroup> damageRecord)
        {
            monsterTag = monsterTag.Replace("_DONE", "");
            var record = damageRecord.Values.Select(x => new Tuple<int, long>(x.ID, x.TotalDamage)).ToList();
            DamageRecord[monsterTag] = record;

            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var configPath = $"BossDaoTac_record_{timestamp}.json";
            var str = JsonConvert.SerializeObject(DamageRecord);
            File.WriteAllText(configPath, str);
        }

        public static Dictionary<int, long> GetTop3DamageDealerToBoss(string bossTag)
        {
            bossTag = bossTag.Replace("_DONE", "");
            if (DamageRecord.TryGetValue(bossTag, out var damageRecords))
            {
                damageRecords.Sort((x, y) => {
                    return y.Item2.CompareTo(x.Item2);
                });
                var r = new Dictionary<int, long>();
                for (var i = 0; i < Math.Min(3, damageRecords.Count); ++i) {
                    r[damageRecords[i].Item1] = damageRecords[i].Item2;
                }

                return r;
            }
            else
            {
                return null;
            }
        }

        public static int GetTop1DamageDealerToBoss(string bossTag) 
        {
            bossTag = bossTag.Replace("_DONE", "");
            if (DamageRecord.TryGetValue(bossTag, out var damageRecords))
            {
                damageRecords.Sort((x, y) => {
                    return y.Item2.CompareTo(x.Item2);
                });
                return damageRecords[0].Item1;
            }
            else
            {
                return -1;
            }
        }
#endregion

        public static void Init()
        {
            DamageRecord = new ConcurrentDictionary<string, List<Tuple<int, long>>>();
            BossCaller = new ConcurrentDictionary<string, int>();
            NameDict = new ConcurrentDictionary<int, string>();
            Bosses = new List<Monster>();
            
            var configPath = Global.GameResPath("Config/KT_GameEvents/daotac.json");
            if (!File.Exists(configPath))
                return;

            var configStr = File.ReadAllText(configPath);
            daoTacData = JsonConvert.DeserializeObject<DaoTacData>(configStr);
        }

    }
}