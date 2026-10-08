using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.OTT
{
    /// <summary>
    /// Sự kiện đoán hoa đăng
    /// </summary>
    public static class OTT_Result
    {
        [Serializable]
        public class OTTPlayer
        {
            public int rid;
            public int option;
            public int bet;
            public bool received;
            public string rname;
        }

        [Serializable]
        public class OTTMatch
        {
            public OTTPlayer player1;
            public OTTPlayer player2;
            public int winner_id;
        }

        [Serializable]
        public class OTTRound
        {
            public string time;
            public List<OTTMatch> matches;
        }

        public static List<OTTRound> Data;

        private static string workingTimestamp = "";

        private static Dictionary<int, Tuple<string, int>> Ranking;

        private static Dictionary<int, Tuple<string, int>> Ranking_1000;

        #region Public methods

        /// <summary>
        /// Khởi tạo dữ liệu Cuộc đua kỳ thú
        /// </summary>
        public static void Init()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            if (timestamp == workingTimestamp)
                return;

            Ranking = new Dictionary<int, Tuple<string, int>>();
            Ranking_1000 = new Dictionary<int, Tuple<string, int>>();
            workingTimestamp = timestamp;

            var configPath = $"OTT_record_{timestamp}.json";
            if (!File.Exists(configPath))
            {
                Data = new List<OTTRound>();
            }
            else
            {
                var configStr = File.ReadAllText(configPath);
                Data = JsonConvert.DeserializeObject<List<OTTRound>>(configStr) ?? new List<OTTRound>();
            }

            var listJSON = Directory.GetFiles(".", "OTT_record_*.json", SearchOption.TopDirectoryOnly);
            foreach (var json in listJSON)
            {
                SysConOut.WriteLine(json);
                var str = File.ReadAllText(json);
                var tempData = JsonConvert.DeserializeObject<List<OTTRound>>(str) ?? new List<OTTRound>();
                foreach (var round in tempData)
                {
                    UpdateRoundToRank(round);
                }
            }
        }

        private static void AddPoint(Dictionary<int, Tuple<string, int>> scoreboard, OTTPlayer player, bool is_winner)
        {
            var POINT_DICT = new Dictionary<int, Dictionary<bool, int>>
            {
                {
                    1000000, new Dictionary<bool, int>
                    {
                        { true, 1 },
                        { false, 0 }
                    }
                },
                {
                    100, new Dictionary<bool, int>
                    {
                        { true, 2 },
                        { false, 1 }
                    }
                },
                {
                    1000, new Dictionary<bool, int>
                    {
                        { true, 6 },
                        { false, 3 }
                    }
                }
            };

            var point = 0;

            try
            {
                point = POINT_DICT[player.bet][is_winner];
            }
            catch
            {
                // ignore
            }
            
            if (scoreboard.TryGetValue(player.rid, out var previous))
            {
                scoreboard[player.rid] = new Tuple<string, int>(player.rname, previous.Item2 + point);
            }
            else
            {
                scoreboard[player.rid] = new Tuple<string, int>(player.rname, point);
            }
        }

        private static void UpdateRoundToRank(OTTRound round)
        {
            foreach (var match in round.matches)
            {
                OTTPlayer winner = null;
                OTTPlayer loser = null;
                if (match.player1.rid == match.winner_id)
                {
                    winner = match.player1;
                    loser = match.player2;
                }
                else if (match.player2.rid == match.winner_id)
                {
                    winner = match.player2;
                    loser = match.player1;
                }

                if (winner == null)
                {
                    // hòa thì coi là thua
                    AddPoint(Ranking, match.player1, false);
                    AddPoint(Ranking, match.player2, false);
                    continue;
                }

                AddPoint(Ranking, winner, true);
                AddPoint(Ranking, loser, false);

                if (winner.bet == 1000)
                {
                    AddPoint(Ranking_1000, winner, true);
                }
            }
        }

        public static void SaveNewRound(OTTRound round)
        {
            Data.Add(round);
            UpdateRoundToRank(round);
        }

        public static List<string> GetTop10Ranking()
        {
            Ranking.Remove(200000);
            var list = Ranking.ToList();
            list.Sort((a, b) => b.Value.Item2.CompareTo(a.Value.Item2));

            var r = list.GetRange(0, Math.Min(10, list.Count)).Select(x => $"{x.Value.Item1}: {x.Value.Item2}")
                .ToList();
            if (r.Count == 0)
                return null;
            return r;
        }

        public static int GetWinCount1000(int rid)
        {
            if (Ranking_1000.TryGetValue(rid, out var record))
                return record.Item2 / 6;

            return 0;
        }

        public static void Dump()
        {
            var configPath = $"OTT_record_{workingTimestamp}.json";
            var str = JsonConvert.SerializeObject(Data);
            File.WriteAllText(configPath, str);
        }

        #endregion
    }
}