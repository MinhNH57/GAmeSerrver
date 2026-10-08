using System;
using GameServer.Logic;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace GameServer.KiemThe.GameEvents.OTT
{
    /// <summary>
    /// Sự kiện đoán hoa đăng
    /// </summary>
    public static class OTT
    {

        [Serializable]
        public class OTTJSON
        {
            public long betDuration;
            public int mapCode;
            public int reward100win;
            public int reward100lose;
            public int reward1000win;
            public int reward1000lose;
            public int reward50vwin;
            public int reward50vlose;
        }

        public static OTTJSON Data;

        #region Public methods
        /// <summary>
        /// Khởi tạo dữ liệu Cuộc đua kỳ thú
        /// </summary>
        public static void Init()
        {
            var configPath = Global.GameResPath("Config/KT_GameEvents/OTT.json");
            if (!File.Exists(configPath))
                return;

            var configStr = File.ReadAllText(configPath);
            Data = JsonConvert.DeserializeObject<OTTJSON>(configStr);
        }
        #endregion
    }
}
