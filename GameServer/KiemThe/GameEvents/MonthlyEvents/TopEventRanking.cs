using System;
using System.Collections.Generic;
using System.IO;
using GameServer.Core.Executor;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using Newtonsoft.Json;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.MonthlyEvents
{
    public static class TopEventRanking
    {

        private const string DATA_PATH = "PyScripts/TopEvents_2024Oct.json";

        [Serializable]
        public class TopEventData
        {
            public List<string> tops;
            public Dictionary<int, int> scores;
            public List<string> seashells;
            public string group_scores;
        }

        private static TopEventData EventRecord;

        public static void Init()
        {
            // đọc log event từ file json 10'/lần
            // json được generate từ script python
            ScheduleExecutor2.Instance.scheduleCancle("TopEventRanking");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopEventRanking", ReadEventData), 1000, 60000);
        }

        public static void ReadEventData()
        {
            if (!File.Exists(DATA_PATH))
                return;

            var configStr = File.ReadAllText(DATA_PATH);
            EventRecord = JsonConvert.DeserializeObject<TopEventData>(configStr);
        }

        public static void ShowTopRanking(KPlayer player)
        {
            try
            {
                var data = new List<string>();
                for (var i = 0; i < 10; ++i)
                {
                    data.Add($"top_{i + 1}");
                    data.Add(i < EventRecord.tops.Count ? EventRecord.tops[i] : "");
                }
                data.Add("group_scores");
                data.Add(EventRecord.group_scores);

                KT_TCPHandler.SendOpenUI(player, "CustomUI/TopEvents", Array.Empty<int>(), data);
            }
            catch
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bảng xếp hạng đang cập nhật!");
            }
        }

        public static void ShowTopSeashells(KPlayer player)
        {
            try
            {
                var data = new List<string>();
                for (var i = 0; i < 10; ++i)
                {
                    data.Add($"top_{i + 1}");
                    data.Add(i < EventRecord.seashells.Count ? EventRecord.seashells[i] : "");
                }

                KT_TCPHandler.SendOpenUI(player, "CustomUI/TopSeashells", Array.Empty<int>(), data);
            }
            catch
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bảng xếp hạng đang cập nhật!");
            }
        }

        // public static string GetTopSeashellsString()
        // {
        //     return EventRecord.seashells;
        // }
    }
}

namespace GameServer.KiemThe.LuaSystem.Logic
{
    public partial class KTLuaLib_Event
    {
        public static void ShopTopEventRanking(Lua_Player player)
        {
            TopEventRanking.ShowTopRanking(player.RefObject);
        }

        // public static string TopSeashells()
        // {
        //     return TopEventRanking.GetTopSeashellsString();
        // }
    }
}