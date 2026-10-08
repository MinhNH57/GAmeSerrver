using GameServer.KiemThe.LuaSystem.Entities;
using System.Collections.Generic;
using GameServer.KiemThe.GameEvents.MonthlyEvents;

namespace GameServer.KiemThe.LuaSystem.Logic
{
    /// <summary>
    /// Cung cấp thư viện dùng cho Lua, liên quan đến sự kiện, hoạt động
    /// </summary>
    public static partial class KTLuaLib_Event
    {
        public static IDictionary<string, string> GetEventShopSellInfo(Lua_Player player)
        {
            return UnionShop.GetStats(player.RefObject);
        }

        public static IDictionary<int, long> GetTop3DamageDealerToBoss(string bossTag)
        {
            return Event_2024Apr.GetTop3DamageDealerToBoss(bossTag);
        }

        public static int GetTop1DamageDealerToBoss(string bossTag) 
        {
            return Event_2024Apr.GetTop1DamageDealerToBoss(bossTag);
        }

        public static void SaveBossCaller(string bossTag, Lua_Player player)
        {
            Event_2024Apr.SaveBossCaller(bossTag, player.RefObject.RoleID);
            Event_2024Apr.AddNameLookup(player.RefObject.RoleID, player.RefObject.RoleName);
        }

        public static int GetBossCaller(string bossTag)
        {
            return Event_2024Apr.GetBossCaller(bossTag);
        }

        public static string GetNameFromRid(int rid)
        {
            return Event_2024Apr.GetNameFromRid(rid);
        }

        public static void AddBoss(Lua_Monster monster)
        {
            Event_2024Apr.AddBoss(monster.RefObject);
        }

        public static void RemoveDeadBoss(Lua_Monster monster)
        {
            Event_2024Apr.RemoveDeadBoss(monster.RefObject);
        }
    }
}