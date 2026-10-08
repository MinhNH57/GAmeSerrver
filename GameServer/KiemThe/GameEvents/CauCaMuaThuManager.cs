using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using GameServer.Core.Executor;
using GameServer.KiemThe.GameEvents;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.KiemThe.LuaSystem.Entities.Math;
using GameServer.Logic;
using MoonSharp.Interpreter;
using Newtonsoft.Json;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents
{
    public static class CauCaMuaThuManager
    {
        [Serializable]
        internal class GiamDinhCa_Data
        {
            public List<int> yesterday;
            public List<int> today;
            public string yesterday_msg;
            public string today_msg;
        }

        private static GiamDinhCa_Data gdcData;

        [Serializable]
        internal class CauCaMuaThu_Data
        {
            public List<List<int>> fishes_weight;
        }

        private static Dictionary<int, Tuple<int, int>> FishesWeight = new Dictionary<int, Tuple<int, int>>();

        private const string CauCaConfig = "Config/KT_GameEvents/CauCaMuaThu.json";
        
        public static void Setup()
        {
            FishesWeight.Clear();
            var configPath = Global.GameResPath(CauCaConfig);
            if (!File.Exists(configPath))
            {
                return;
            }
            
            var jsonStr = File.ReadAllText(configPath);
            var rawData = JsonConvert.DeserializeObject<CauCaMuaThu_Data>(jsonStr);
            foreach (var rate in rawData.fishes_weight)
            {
                FishesWeight[rate[0]] = new Tuple<int, int>(rate[1], rate[2]);
            }
            
            // đọc log giám định cá từ file json 10'/lần
            ScheduleExecutor2.Instance.scheduleCancle("GiamDinhCa");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("GiamDinhCa", ReadGDCData), 0, 60000);
        }

        private static void ReadGDCData()
        {
            try
            {
                var path = "giamdinhca.json";
                if (!File.Exists(path))
                {
                    gdcData = null;
                    return;
                }

                var jsonStr = File.ReadAllText(path);
                gdcData = JsonConvert.DeserializeObject<GiamDinhCa_Data>(jsonStr);
            }
            catch
            {
                gdcData = null;
            }
        }

        public static string GetYesterdayMsg()
        {
            return gdcData == null ? "Không có dữ liệu!" : gdcData.yesterday_msg;
        }
        
        public static string GetTodayMsg()
        {
            return gdcData == null ? "Không có dữ liệu!" : gdcData.today_msg;
        }

        public static bool IsWinner(int roleID)
        {
            if (gdcData == null)
                return false;

            return gdcData.yesterday.Contains(roleID);
        }

        public static bool IsShopEvent(int shopID)
        {
            return shopID == 1457 || shopID == 1458;
        }
        
        public static void GiamDinhCa(KPlayer player, int fishID) {
            if (!FishesWeight.TryGetValue(fishID, out var weightRange))
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi giám định cá, vui lòng liên hệ Game Master.");
                return;
            }

            var fishWeight = StaticRandom.GetRandom(weightRange.Item1 * 1000, weightRange.Item2 * 1000);
            
            GameManager.logDBCmdMgr.WriteLogDBForPlayer(player, 
                "GIAMDINHCA", "", "", "", "",
                fishWeight.ToString(), "", "", "");
            
            KT_TCPHandler.SendOpenUI(player, "Messages/UIFishExamine", Array.Empty<int>(), new List<string>()
            {
                "fishWeight", $"Câu được cá lớn nhất nặng <color=#f38ba8>{(fishWeight / 1000000f):F3}</color> kg!"
            });
        }
        
        public static void GiamDinhCa(KPlayer player, List<int> fishIDs)
        {
            var fishWeight = 0;
            foreach (var fishID in fishIDs)
            {
                if (!FishesWeight.TryGetValue(fishID, out var weightRange))
                {
                    PlayerManager.ShowNotification(player, "Có lỗi khi giám định cá, vui lòng liên hệ Game Master.");
                    return;
                }

                fishWeight = Math.Max(fishWeight, StaticRandom.GetRandom(weightRange.Item1 * 1000, weightRange.Item2 * 1000));
            }
            
            GameManager.logDBCmdMgr.WriteLogDBForPlayer(player, 
                "GIAMDINHCA", "", "", "", "",
                fishWeight.ToString(), "", "", "");
            
            KT_TCPHandler.SendOpenUI(player, "Messages/UIFishExamine", Array.Empty<int>(), new List<string>()
            {
                "fishWeight", $"Câu được cá lớn nhất nặng <color=#f38ba8>{(fishWeight / 1000000f):F3}</color> kg!"
            });
        }
    }
}

namespace GameServer.KiemThe.LuaSystem.Logic
{
    public static partial class KTLuaLib_System
    {
        public static string GetGDCYesterday()
        {
            return CauCaMuaThuManager.GetYesterdayMsg();
        }
        
        public static string GetGDCToday()
        {
            return CauCaMuaThuManager.GetTodayMsg();
        }

        public static bool IsGDCYesterDayWinner(Lua_Player player)
        {
            return CauCaMuaThuManager.IsWinner(player.GetID());
        }

        public static void GiamDinhCa(Lua_Player player, IDictionary<int, int> fishes)
        {
            CauCaMuaThuManager.GiamDinhCa(player.RefObject, fishes.Select(x => x.Key).ToList());
        }
    }
}