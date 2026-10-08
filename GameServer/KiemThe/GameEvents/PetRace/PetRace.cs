using System;
using GameServer.Logic;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace GameServer.KiemThe.GameEvents.PetRace
{
    /// <summary>
    /// Sự kiện đoán hoa đăng
    /// </summary>
    public static class PetRace
    {
        [Serializable]
        public class PetTrack
        {
            public List<int> start;
            public List<int> finish;
        }

        [Serializable]
        public class PetRaceJSON
        {
            public List<int> racers;
            public List<PetTrack> tracks;
            public long betDuration;
            public long rewardDuration;
            public long forceEndDuration;
            public int mapCode;
            public int forceWinner;
        }

        public static PetRaceJSON Data;

        #region Public methods
        /// <summary>
        /// Khởi tạo dữ liệu Cuộc đua kỳ thú
        /// </summary>
        public static void Init()
        {
            var configPath = Global.GameResPath("Config/KT_GameEvents/AmazingRace.json");
            if (!File.Exists(configPath))
                return;

            var configStr = File.ReadAllText(configPath);
            Data = JsonConvert.DeserializeObject<PetRaceJSON>(configStr);
        }
        #endregion
    }
}
