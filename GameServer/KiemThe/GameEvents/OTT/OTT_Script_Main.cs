using System;
using System.Collections.Concurrent;
using GameServer.KiemThe.GameEvents.Interface;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.GameEvents.OTT;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.KiemThe.LuaSystem.Entities.Math;
using MoonSharp.Interpreter;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.OTT
{
    /// <summary>
    /// Script chính thực thi Logic sự kiện Đoán hoa đăng
    /// </summary>
    public class OTT_Script_Main : GameMapEvent
    {
        #region Define

        private readonly Dictionary<int, Tuple<int, int, string>> BetRecords =
            new Dictionary<int, Tuple<int, int, string>>();

        private readonly ConcurrentDictionary<int, bool> RewardReceived = new ConcurrentDictionary<int, bool>();

        public enum OTT_STATE
        {
            NULL,
            PREPARE,
            SORTING,
            ENDED
        }

        public OTT_STATE currentState;
        private long startTick;

        #endregion

        #region Core GameMapEvent

        /// <summary>
        /// Script chính thực thi Logic sự kiện Đoán hoa đăng
        /// </summary>
        /// <param name="map"></param>
        /// <param name="activity"></param>
        public OTT_Script_Main(GameMap map, KTActivity activity) : base(map, activity)
        {
        }

        /// <summary>
        /// Sự kiện bắt đầu
        /// </summary>
        protected override void OnStart()
        {
            OTT_Result.Init();
            BetRecords.Clear();
            RewardReceived.Clear();
            startTick = DateTime.Now.Ticks;
            currentState = OTT_STATE.PREPARE;

            // Thông báo bắt đầu sự kiện
            KTGlobal.SendSystemEventNotification(
                $"Hoạt động Vui Game Dân Gian sẽ tiến hành ghép cặp thi đấu sau {OTT.Data.betDuration / 60} phút nữa, các vị bằng hữu hãy mau mau tới Vân Trung Trấn gặp Quan Ghi danh sự kiện Lăng Nhược Hư để đăng ký tham gia!");

            var players = GetPlayers();
            foreach (var player in players.Where(player => player.IsOnline()))
            {
                this.OpenEventBroadboard(player, 22);
            }
        }

        public override void OnPlayerEnter(KPlayer player)
        {
            if (currentState == OTT_STATE.PREPARE)
                OpenEventBroadboard(player, 22);
        }

        public override void OnPlayerLeave(KPlayer player, GameMap toMap)
        {
            CloseEventBroadboard(player, 22);
        }

        /// <summary>
        /// Sự kiện Tick
        /// </summary>
        protected override void OnTick()
        {
            var timeElapsed = new TimeSpan(DateTime.Now.Ticks - startTick);

            if (timeElapsed.TotalSeconds > OTT.Data.betDuration)
            {
                currentState = OTT_STATE.SORTING;
            }
            if (timeElapsed.TotalSeconds > OTT.Data.betDuration + 5)
            {
                OnRoundEnded();
                return;
            }

            if (currentState == OTT_STATE.PREPARE || currentState == OTT_STATE.SORTING) {
                this.UpdateEventDetailsToPlayers("Vui Game Dân Gian - Đăng ký", (long)(OTT.Data.betDuration * 1000 - timeElapsed.TotalMilliseconds),
                Array.Empty<string>());
            }
        }

        private void OnRoundEnded()
        {
            MatchMaker();
            currentState = OTT_STATE.ENDED;
            // Thông báo bắt đầu sự kiện
            KTGlobal.SendSystemEventNotification(
                $"Hoạt động Vui Game Dân Gian đã hoàn thành ghép cặp, các vị bằng hữu hãy nhanh chóng đến gặp Quan Ghi danh Sự kiện Lăng Nhược Hư để xem kết quả và nhận thưởng!");
            KTActivityManager.StopActivity(Activity.Data.ID);

            var players = GetPlayers();
            foreach (var player in players.Where(player => player.IsOnline()))
            {
                this.CloseEventBroadboard(player, 22);
            }
        }


        private void MatchMaker()
        {
            try
            {
                var listPlayers = new List<Tuple<int, int, int, string>>();
                foreach (var rid in BetRecords.Keys)
                {
                    listPlayers.Add(new Tuple<int, int, int, string>(rid, BetRecords[rid].Item1, BetRecords[rid].Item2,
                        BetRecords[rid].Item3));
                }

                var currentRound = new OTT_Result.OTTRound
                {
                    matches = new List<OTT_Result.OTTMatch>()
                };
                var currentHour = DateTime.Now.Hour;
                var currentMinute = DateTime.Now.Minute;
                currentRound.time = $"{currentHour:D2}:{currentMinute:D2}";

                StaticRandom.Shuffle(listPlayers);
                if (listPlayers.Count % 2 == 1)
                {
                    listPlayers.Add(new Tuple<int, int, int, string>(200000, 10, 100, "SơnLiễu"));
                }

                for (var i = 0; i + 1 < listPlayers.Count; i += 2)
                {
                    var p1 = listPlayers[i];
                    var p2 = listPlayers[i + 1];
                    var match = new OTT_Result.OTTMatch
                    {
                        player1 = new OTT_Result.OTTPlayer
                        {
                            rid = p1.Item1,
                            option = p1.Item2,
                            bet = p1.Item3,
                            rname = p1.Item4,
                            received = false
                        },
                        player2 = new OTT_Result.OTTPlayer
                        {
                            rid = p2.Item1,
                            option = p2.Item2,
                            bet = p2.Item3,
                            rname = p2.Item4,
                            received = false
                        }
                    };
                    if (match.player1.option == match.player2.option)
                    {
                        match.winner_id = -1;
                    }
                    else
                    {
                        switch (match.player1.option)
                        {
                            case 10:
                                match.winner_id = match.player2.option == 12 ? match.player1.rid : match.player2.rid;
                                break;
                            case 11:
                                match.winner_id = match.player2.option == 10 ? match.player1.rid : match.player2.rid;
                                break;
                            case 12:
                                match.winner_id = match.player2.option == 11 ? match.player1.rid : match.player2.rid;
                                break;
                        }
                    }

                    currentRound.matches.Add(match);
                }

                OTT_Result.SaveNewRound(currentRound);
                OTT_Result.Dump();
            }
            catch (Exception e)
            {
                LogManager.WriteException(e.ToString());
            }
            
        }


        /// <summary>
        /// Sự kiện kết thúc
        /// </summary>
        protected override void OnClose()
        {
        }

        #endregion


        #region Lua Bridge

        public bool AlreadyBet(KPlayer player)
        {
            return BetRecords.TryGetValue(player.RoleID, out _);
        }

        public bool Bet(KPlayer player, int racersId, int betValue)
        {
            if (AlreadyBet(player))
                return false;

            BetRecords[player.RoleID] = new Tuple<int, int, string>(racersId, betValue, player.RoleName);
            LogManager.WriteLog(LogTypes.PetRace,
                $"{player.RoleName}[{player.RoleID}] cược {racersId}, gói cược {betValue}");
            return true;
        }

        public Lua_Vector2 GetBetDescription(KPlayer player)
        {
            if (!BetRecords.TryGetValue(player.RoleID, out var record))
                return null;

            return new Lua_Vector2(record.Item1, record.Item2);
        }


        public bool SetRewardReceived(KPlayer player)
        {
            LogManager.WriteLog(LogTypes.PetRace, $"{player.RoleName}[{player.RoleID}] đã nhận thưởng.");
            return RewardReceived.TryAdd(player.RoleID, true);
        }

        public bool IsRewardReceived(KPlayer player)
        {
            return RewardReceived.TryGetValue(player.RoleID, out _);
        }

        #endregion
    }
}

namespace GameServer.KiemThe.LuaSystem.Logic
{
    [MoonSharpUserData]
    public static class KTLuaLib_OTT
    {
        public static int GetState()
        {
            var script = OTT_ActivityScript.activity_script;
            if (script == null)
                return -1;
            return (int)script.currentState;
        }

        public static bool Bet(Lua_Player player, int option, int bet_value)
        {
            var script = OTT_ActivityScript.activity_script;
            if (script == null)
                return false;
            script.Bet(player.RefObject, option, bet_value);
            return true;
        }

        public static bool AlreadyBet(Lua_Player player)
        {
            var script = OTT_ActivityScript.activity_script;
            if (script == null)
                return false;
            return script.AlreadyBet(player.RefObject);
        }

        public static Lua_Vector2 GetBetRecord(Lua_Player player)
        {
            var script = OTT_ActivityScript.activity_script;
            if (script == null)
                return null;

            return script.GetBetDescription(player.RefObject);
        }

        public static IDictionary<int, string> GetHistory(Lua_Player player)
        {
            var history = new Dictionary<int, string>();
            var round_index = 0;
            foreach (var round in OTT_Result.Data)
            {
                foreach (var match in round.matches)
                {
                    if (match.player1.rid == player.GetID() || match.player2.rid == player.GetID())
                    {
                        var reward_received =
                            player.RefObject.GetValueOfDailyRecore((int)RecoreDef.OTTReward + round_index) != -1;
                        var reward_status = reward_received ? " [Đã nhận thưởng]" : "";
                        history[round_index] = $"Vòng thi đấu {round.time}{reward_status}";
                        break;
                    }
                }

                round_index += 1;
            }

            if (history.Count == 0)
                return null;

            return history;
        }

        public static IDictionary<string, string> ShowMatch(Lua_Player player, int round_index)
        {
            var round = OTT_Result.Data[round_index];
            OTT_Result.OTTMatch currentMatch = null;
            var currentBet = -1;
            foreach (var match in round.matches)
            {
                if (match.player1.rid == player.GetID())
                {
                    currentMatch = match;
                    currentBet = match.player1.bet;
                    break;
                }
                if (match.player2.rid == player.GetID())
                {
                    currentMatch = match;
                    currentBet = match.player2.bet;
                    break;
                }
            }

            if (currentMatch == null)
            {
                PlayerManager.ShowMessageBox(player.RefObject, "Oẳn Tù Tì", "Đã có lỗi xảy ra, vui lòng thử lại sau");
                return null;
            }

            var result = new Dictionary<string, string>
            {
                ["player1"] = currentMatch.player1.rname,
                ["player1_option"] = currentMatch.player1.option.ToString(),
                ["player1_state"] = currentMatch.player1.rid == currentMatch.winner_id ? "win" : "lose",
                ["player2"] = currentMatch.player2.rname,
                ["player2_option"] = currentMatch.player2.option.ToString(),
                ["player2_state"] = currentMatch.player2.rid == currentMatch.winner_id ? "win" : "lose",
                ["match_result"] =  player.GetID() == currentMatch.winner_id ? "win" : "lose"
            };

            // var info =
            //     $"{currentMatch.player1.rname} [{currentMatch.player1.option}] vs. {currentMatch.player2.rname} [{currentMatch.player2.option}]";

            // PlayerManager.ShowMessageBox(player.RefObject, "Oẳn Tù Tì", info);

            if (player.RefObject.GetValueOfDailyRecore((int)RecoreDef.OTTReward + round_index) == -1)
            {
                var isWinner = player.GetID() == currentMatch.winner_id;
                var rewardID = -1;
                switch (currentBet)
                {
                    case 100:
                        rewardID = isWinner ? OTT.Data.reward100win : OTT.Data.reward100lose;
                        break;
                    case 1000:
                        rewardID = isWinner ? OTT.Data.reward1000win : OTT.Data.reward1000lose;
                        break;
                    case 1000000:
                        rewardID = isWinner ? OTT.Data.reward50vwin : OTT.Data.reward50vlose;
                        break;
                }

                if (KTLuaLib_Player.AddItemLua(player, rewardID, 1, 0, 1, 0, -1))
                {
                    player.RefObject.SetValueOfDailyRecore((int)RecoreDef.OTTReward + round_index, 1);
                    LogManager.WriteLog(LogTypes.PetRace, $"{player.RefObject.RoleName}[{player.RefObject.RoleID}] đã nhận thưởng {round_index}.");
                }
            }

            return result;
        }

        public static void ForceStart()
        {
            KTActivityManager.StartActivity(600);
        }

        public static List<string> GetTop10Rank()
        {
            return OTT_Result.GetTop10Ranking();
        }

        public static int GetWinCount1000(Lua_Player player)
        {
            return OTT_Result.GetWinCount1000(player.RefObject.RoleID);
        }
    }
}