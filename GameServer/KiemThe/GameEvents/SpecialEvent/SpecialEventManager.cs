using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameServer.Core.Executor;
using Newtonsoft.Json;

namespace GameServer.KiemThe.GameEvents.SpecialEvent
{
    /// <summary>
    /// Quản lý sự kiện đặc biệt
    /// </summary>
    public partial class SpecialEventManager
    {
        #region Singleton - Instance
        /// <summary>
        /// Luồng quản lý Võ lâm liên đấu
        /// </summary>
        public static SpecialEventManager Instance { get; private set; }

        /// <summary>
        /// Luồng quản lý Võ lâm liên đấu
        /// </summary>
        private SpecialEventManager()
        {
            this.StartTimer();
        }
        #endregion

        #region Init
        /// <summary>
        /// Khởi tạo
        /// </summary>
        public static void Init()
        {
            SpecialEventManager.Instance = new SpecialEventManager();

            ReadRoseRanking();
            ScheduleExecutor2.Instance.scheduleCancle("RoseAnalysis");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("RoseAnalysis", ReadRoseRanking), 0, 60000);
        }
        #endregion
        
        #region hoa hồng truyền tình
        
        [Serializable]
        internal class RoseRanking_Data
        {
            public int rid;
            public string rname;
            public int rose;
        }

        private static List<RoseRanking_Data> roseRanking;

        public static void ReadRoseRanking()
        {
            try
            {
                roseRanking = new List<RoseRanking_Data>();
                var path = "hoahong.json";
                if (!File.Exists(path))
                {
                    return;
                }

                var jsonStr = File.ReadAllText(path);
                roseRanking = JsonConvert.DeserializeObject<List<RoseRanking_Data>>(jsonStr);
            }
            catch
            {
                roseRanking = new List<RoseRanking_Data>();
            }
        }

        public static IDictionary<string, int> GetRoseRanking()
        {
            var r = new Dictionary<string, int>();
            foreach (var player in roseRanking)
            {
                r[player.rname] = player.rose;
            }

            if (!r.Any())
                return null;

            return r;
        }
        #endregion
    }
}
