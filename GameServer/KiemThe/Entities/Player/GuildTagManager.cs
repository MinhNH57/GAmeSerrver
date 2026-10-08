using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using GameServer.Logic;
using Newtonsoft.Json;

namespace GameServer.KiemThe.Entities.Player
{
    public static class GuildTagManager
    {
        [Serializable]
        private class GuildTagInfo
        {
            public int ID;
            public string Name;
            public int TagID;
        }

        private static readonly ConcurrentDictionary<int, GuildTagInfo> guildTagDict =
            new ConcurrentDictionary<int, GuildTagInfo>();

        public static void Setup()
        {
            guildTagDict.Clear();
            var filePath = Global.GameResPath("Config/KT_Setting/GuildTag.json");
            if (!File.Exists(filePath))
                return;
            var jsonString = File.ReadAllText(filePath);
            var json = JsonConvert.DeserializeObject<List<GuildTagInfo>>(jsonString);
            foreach (var tagInfo in json)
            {
                guildTagDict[tagInfo.ID] = tagInfo;
            }
        }

        public static string GetGuildTagName(int guildId)
        {
            if (guildTagDict.TryGetValue(guildId, out var tagInfo))
            {
                return tagInfo.Name;
            }

            return "";
        }
    }
}