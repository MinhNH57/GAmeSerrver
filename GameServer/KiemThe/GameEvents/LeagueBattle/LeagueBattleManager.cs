using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.KuaFuBalance;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GameServer.KiemThe.GameEvents.LeagueBattle
{
    /// <summary>
    /// Quản lý thi đấu môn phái
    /// </summary>
    public class LeagueBattleManager
    {
        public bool IsActive = false;

        /// <summary>
        /// Toàn bộ các trận thi đấu môn phái đang diễn ra
        /// </summary>
        public static ConcurrentDictionary<int, LeagueBattle> TotalBattle = new ConcurrentDictionary<int, LeagueBattle>();

        /// <summary>
        /// Config thi đấu môn phái
        /// </summary>
        public static string Battle_Config = "Config/KT_Battle/League_Battle.xml";

        private const string LocalBattleConfig = "Config/KT_Battle/League_Battle.xml";

        private const string KuaFuBalanceBattleConfig = "Config/KT_Battle/League_Battle_KuaFu.xml";

        /// <summary>
        /// Hàm này gọi trước khi người chơi chuyển sang bản đồ khác
        /// </summary>
        /// <param name="player"></param>
        public static void OnPlayerLeave(KPlayer client, GameMap map)
        {
            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);

            if (FindBattle != null)
            {
                int MAPCODE = FindBattle.GetMapCode;
                if (map.MapCode != MAPCODE)
                {
                    //XÓA STATE KHI RỜI KHỎI
                    PlayChangeState(client, 0);
                    client.PKMode = (int)PKMode.Peace;
                    client.ForbidUsingSkill = false;
                    client.Camp = -1;
                    client.StopAllActiveFights();
                }
            }
        }

        public static void PlayChangeState(KPlayer Player, int State)
        {
            G2C_EventState _State = new G2C_EventState();

            _State.EventID = 21;
            _State.State = State;
            if (Player.IsOnline())
            {
                Player.SendPacket<G2C_EventState>((int)TCPGameServerCmds.CMD_KT_EVENT_STATE, _State);
            }
            else
            {
                //Console.WriteLine("OFFLINE");
            }
        }

        /// <summary>
        /// Xử lý sự kiện hồi sinh khi đang trong bản đồ thi đấu môn phái
        /// </summary>
        /// <param name="client"></param>
        public static void Revice(KPlayer client)
        {
            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                FindBattle.Revice(client);
            }
        }

        /// <summary>
        /// Trả về client bảng xếp hạng khi người chơi ấn vào nút bảng xếp hạng
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static LEAGUE_PVP_RANKING_INFO GetRanking(KPlayer client)
        {
            LEAGUE_PVP_RANKING_INFO _Ranking = new LEAGUE_PVP_RANKING_INFO();

            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                _Ranking = FindBattle.RankingBuilder(client);
            }

            return _Ranking;
        }

        public static void OpenBetting(KPlayer player, string bettorName)
        {
            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                FindBattle.DoBetting(player, bettorName);
            }
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đang trong thi đấu môn phái ko
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsInLeagueBattle(KPlayer player)
        {
            if (player.MapCode == 2222)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsKuaFuBalanceMap(int mapCode)
        {
            if (!GameManager.IsKuaFuServer ||
                !Battle_Config.EndsWith("League_Battle_KuaFu.xml", StringComparison.OrdinalIgnoreCase) ||
                _BattleDef == null ||
                !ServerConfig.Instance.IsKuaFuBalance ||
                _BattleDef.MapFactionList == null)
            {
                return false;
            }

            foreach (LeagueMap map in _BattleDef.MapFactionList)
            {
                if (map != null && map.MapId == mapCode)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Lưu lại config
        /// </summary>
        public static LeagueDef _BattleDef = new LeagueDef();

        /// <summary>
        ///  Event xử lý sự kiện tham gia thi đấu môn phái khi click vào NPC
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int LeagueBattleJoin(KPlayer player)
        {
            if (player.m_cPlayerFaction.GetFactionId() == 0)
            {
                return -9;
            }
            else
            {
                int FactionID = 0;

                TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
                if (FindBattle != null)
                {
                    return FindBattle.Register(player);
                }
                else
                {
                    return -1;
                }
            }
        }

        public static int StateLeagueBattle()
        {
            int FactionID = 0;
            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                return FindBattle.CheckState();
            }    
            else
                return 0;
        }

        public static void LeagueLogout(KPlayer player)
        {
            if (player.m_cPlayerFaction.GetFactionId() == 0)
            {
                return;
            }
            else
            {
                int FactionID = 0;

                TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
                if (FindBattle != null)
                {
                    FindBattle.OnLogout(player);
                }
                else
                {
                    return;
                }
            }
        }

        public static void NpcClick(GameMap map, NPC npc, KPlayer client)
        {
            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                FindBattle.NpcQuanQuan(map, npc, client);
            }
        }

        //TODO
        public static void DamgeRecore(GameObject HIT, GameObject BEHIT, int Dagme)
        {
            if (HIT is KPlayer && BEHIT is KPlayer)
            {
                int FactionID = 0;

                TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);

                ///Xử lý sự kiện khi nhân vật chết
                if (FindBattle != null)
                {
                    FindBattle.OnHitTaget(HIT as KPlayer, Dagme);
                }
            }
        }

        /// <summary>
        /// Gọi hàm xử lý chết ở FACTNIONBATTE
        /// </summary>
        /// <param name="Kill"></param>
        /// <param name="BeKill"></param>
        public static void OnDie(GameObject Kill, GameObject BeKill)
        {
            if (Kill is KPlayer && BeKill is KPlayer)
            {
                lock(BeKill)
                {
                    int FactionID = 0;

                    TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);

                    ///Xử lý sự kiện khi nhân vật chết
                    if (FindBattle != null)
                    {
                        FindBattle.OnKillRole(Kill as KPlayer, BeKill as KPlayer);
                    }
                }
            }
        }

        /// <summary>
        ///  Sự kiện shutdown thi đấu môn phái khi thi đấu môn phái
        /// </summary>
        public static void ShutDown()
        {
            foreach (LeagueBattle _Battle in TotalBattle.Values)
            {
                _Battle.showdown();
            }
        }

        #region GMCTR

        /// <summary>
        /// Lệnh start chiến trường = lệnh GM
        /// </summary>
        public static void ForceStartBattle()
        {
            foreach (LeagueBattle _League in TotalBattle.Values)
            {
                _League.ForceStartBattle();
            }
        }

        /// <summary>
        /// Lệnh tắt chiến trường = lệnh GM
        /// </summary>
        public static void ForceEndBattle()
        {
            foreach (LeagueBattle _League in TotalBattle.Values)
            {
                _League.ForceEndBattle();
            }
        }

        #endregion GMCTR

        /// <summary>
        /// Gọi sự kiện stảt chiến trường khi GS chạy
        /// </summary>
        public static void BattleStatup()
        {
            LoadConfig();
            Start();
        }

        public static void ReloadConfig()
        {
            LoadConfig();

            foreach (LeagueBattle battle in TotalBattle.Values)
            {
                if (battle != null)
                {
                    battle._Config = _BattleDef;
                }
            }
        }

        private static void LoadConfig()
        {
            Battle_Config = GetBattleConfigPath();
            string Files = Global.GameResPath(Battle_Config);

            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(LeagueDef));
                _BattleDef = serializer.Deserialize(stream) as LeagueDef;
            }
        }

        private static string GetBattleConfigPath()
        {
            if (GameManager.IsKuaFuServer && KuaFuBalanceManager.Instance.IsEnabled)
            {
                return KuaFuBalanceBattleConfig;
            }

            return LocalBattleConfig;
        }

        /// <summary>
        /// Start toàn bộ các bản đồ có sự kiện thi đấu môn phái
        /// </summary>
        public static void Start()
        {
            List<LeagueMap> MapFactionList = _BattleDef.MapFactionList;

            foreach (LeagueMap Map in MapFactionList)
            {
                LeagueBattle _Battle = new LeagueBattle(Map.FactionID, _BattleDef);

                _Battle.startup();

                TotalBattle.TryAdd(Map.FactionID, _Battle);
            }
        }

        public static void GetAwardBetting(KPlayer player)
        {
            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                FindBattle.GetAwardBetting(player);
            }
            else
            {
                return;
            }
        }

        public static void GetAwardParticipation(KPlayer player)
        {
            int FactionID = 0;

            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                FindBattle.GetAwardParticipation(player);
            }
            else
            {
                return;
            }
        }
 
        public static void GetAwardTop(KPlayer player)
        {
            int FactionID = 0;
 
            TotalBattle.TryGetValue(FactionID, out LeagueBattle FindBattle);
            if (FindBattle != null)
            {
                FindBattle.GetAwardTop(player);
            }
            else
            {
                return;
            }
        }
    }
}
