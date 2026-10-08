using System;
using System.Collections.Concurrent;
using GameServer.KiemThe.GameEvents.Interface;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GameServer.Core.Executor;
using GameServer.KiemThe.GameEvents;
using GameServer.KiemThe.GameEvents.PetRace;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.KiemThe.LuaSystem.Entities.Math;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Activity.LevelUpEvent;
using GameServer.KiemThe.Core.Repute;
using MoonSharp.Interpreter;

namespace GameServer.KiemThe.GameEvents.PetRace
{

    /// <summary>
    /// Script chính thực thi Logic sự kiện Đoán hoa đăng
    /// </summary>
    public class PetRace_Script_Main : GameMapEvent
    {
        #region Define

        private class RacerInfo
        {
            public Monster racer;
            public Point destination;
        }

        /// <summary>
        /// Đối tượng NPC di chuyển
        /// </summary>
        private readonly List<RacerInfo> Racers = new List<RacerInfo>();

        private readonly List<RacerInfo> FinishedRacer = new List<RacerInfo>();

        private readonly Dictionary<int, Tuple<int, int>> BetRecords = new Dictionary<int, Tuple<int, int>>();
        private readonly ConcurrentDictionary<int, bool> RewardReceived = new ConcurrentDictionary<int, bool>();

        public enum RaceState
        {
            NULL,
            BETTING,
            RACING,
            ENDED
        }

        public RaceState currentRaceState;
        private long startTick;
        private long finishedTick;
        private long winTick;

        private const string UpdateRacerSpeedSchedulerName = "PetRaceSpeedSchedule";

        #endregion

        #region Core GameMapEvent

        /// <summary>
        /// Script chính thực thi Logic sự kiện Đoán hoa đăng
        /// </summary>
        /// <param name="map"></param>
        /// <param name="activity"></param>
        public PetRace_Script_Main(GameMap map, KTActivity activity) : base(map, activity)
        {
        }

        /// <summary>
        /// Sự kiện bắt đầu
        /// </summary>
        protected override void OnStart()
        {
            BetRecords.Clear();
            RewardReceived.Clear();
            // Xóa NPC cũ
            startTick = 0;
            finishedTick = 0;
            winTick = 0;
            this.RemoveRacers();
            CreateRacer();
            currentRaceState = RaceState.BETTING;
            startTick = DateTime.Now.Ticks;
        }

        private int GetPreWinner()
        {
            // nếu có config sẵn thì trả về id luôn
            if (PetRace.Data.forceWinner != -1)
                return PetRace.Data.forceWinner;
            
            try
            {
                var betValueOfRacers = new Dictionary<int, int>();
                foreach (var racerInfo in Racers)
                {
                    betValueOfRacers[racerInfo.racer.MonsterInfo.ExtensionID] = 0;
                }

                foreach (var record in BetRecords.Select(x => x.Value))
                {
                    if (!betValueOfRacers.TryGetValue(record.Item1, out _))
                        betValueOfRacers[record.Item1] = 0;

                    var betValue = 0;
                    switch (record.Item2)
                    {
                        case 3:
                            betValue = 100;
                            break;
                        case 4:
                            betValue = 1000;
                            break;
                    }

                    betValueOfRacers[record.Item1] += betValue;
                }

                var minBet = betValueOfRacers.Select(x => x.Value).Min();
                var listWinners = betValueOfRacers.Where(x => x.Value == minBet).ToList();
                var winner = listWinners[StaticRandom.GetRandom(listWinners.Count())];
                var counter = 0;
                foreach (var racer in Racers)
                {
                    if (racer.racer.MonsterInfo.ExtensionID == winner.Key)
                        return counter;
                    counter += 1;
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private void StartRace()
        {
            PetRace.Data.forceWinner = GetPreWinner();
            var counter = 0;
            foreach (var racer in Racers)
            {
                racer.racer.MoveTo(racer.destination, true);
                if (counter == PetRace.Data.forceWinner)
                {
                    SysConOut.WriteLine($"Force {racer.racer.RoleName} win!!!!");
                    racer.racer.ChangeRunSpeed(-2, 0, 0);
                }
                else
                {
                    racer.racer.ChangeRunSpeed(-5, 0, 0);
                }

                counter += 1;
            }

            // cập nhật tốc độ racer 1s/lần
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask(UpdateRacerSpeedSchedulerName, () =>
            {
                var other_counter = 0;
                foreach (var racer in Racers)
                {
                    var speedBefore = racer.racer.GetCurrentRunSpeed();
                    var speedAfter = StaticRandom.GetRandom(2, 16);
                    if (other_counter == PetRace.Data.forceWinner)
                        speedAfter = StaticRandom.GetRandom(4, 16);
                    var speedChange = speedAfter - speedBefore;
                    racer.racer.ChangeRunSpeed(speedChange, 0, 0);
                    if (speedBefore <= 0)
                        racer.racer.MoveTo(racer.destination, true);

                    other_counter += 1;
                }
            }), 0, 1000);
        }


        public override void OnPlayerEnter(KPlayer player)
        {
            if (currentRaceState != RaceState.NULL)
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
            switch (currentRaceState)
            {
                case RaceState.NULL:
                    return;
                case RaceState.BETTING:
                {
                    var timeElapsed = new TimeSpan(DateTime.Now.Ticks - startTick);
                    if (timeElapsed.TotalSeconds > PetRace.Data.betDuration - 15)
                    {
                        // mở bảng thông báo event
                        SendRaceBeginEventState();
                        UpdatePreRaceStatus((int)(PetRace.Data.betDuration * 1000 - timeElapsed.TotalMilliseconds));
                    }

                    if (timeElapsed.TotalSeconds > PetRace.Data.betDuration)
                    {
                        currentRaceState = RaceState.RACING;
                        StartRace();
                        Ticks = 100;
                    }

                    break;
                }
                case RaceState.RACING:
                {
                    var raceStopped = UpdateRaceStatus();
                    if (raceStopped)
                    {
                        OnRaceFinished();
                    }

                    break;
                }
                case RaceState.ENDED:
                {
                    Ticks = 1000;
                    if (finishedTick == 0)
                        return;
                    var timeElapsed = new TimeSpan(DateTime.Now.Ticks - finishedTick);
                    UpdateAfterRaceStatus((int)(PetRace.Data.rewardDuration * 1000 - timeElapsed.TotalMilliseconds));

                    if (timeElapsed.TotalSeconds > PetRace.Data.rewardDuration)
                    {
                        SendRaceFinishedEventState();
                        finishedTick = 0;
                        KTActivityManager.StopActivity(Activity.Data.ID);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnRaceFinished()
        {
            UpdateRaceStatus();
            currentRaceState = RaceState.ENDED;
            finishedTick = DateTime.Now.Ticks;
            ScheduleExecutor2.Instance.scheduleCancle(UpdateRacerSpeedSchedulerName);
            KTGlobal.SendSystemEventNotification(
                $"Cuộc đua Thú cưng đã kết thúc! Hãy nhanh chóng đến gặp Quan Ghi danh Đua thú để nhận thưởng bình chọn. Thời gian nhận thưởng còn {PetRace.Data.rewardDuration / 60} phút!");
            var racerStr = string.Join(", ", FinishedRacer.Select(x => $"{x.racer.RoleName}[{x.racer.MonsterInfo.ExtensionID}]"));
            LogManager.WriteLog(LogTypes.PetRace, $"Đua thú kết thúc. Kết quả: {racerStr}");
        }

        /// <summary>
        /// Sự kiện kết thúc
        /// </summary>
        protected override void OnClose()
        {
            // Xóa NPC
            this.RemoveRacers();
            ScheduleExecutor2.Instance.scheduleCancle(UpdateRacerSpeedSchedulerName);
        }


        private void SendRaceBeginEventState()
        {
            var players = GetPlayers();
            foreach (var player in players.Where(player => player.IsOnline()))
            {
                this.OpenEventBroadboard(player, 22);
            }
        }

        private void SendRaceFinishedEventState()
        {
            var players = GetPlayers();
            foreach (var player in players.Where(player => player.IsOnline()))
            {
                this.CloseEventBroadboard(player, 22);
            }
        }

        private void UpdatePreRaceStatus(int milisecondRemain)
        {
            if (milisecondRemain < 0)
            {
                milisecondRemain = 1;
            }

            this.UpdateEventDetailsToPlayers("Cuộc đua thú cưng - Chuẩn bị", milisecondRemain,
                Array.Empty<string>());
        }

        private void UpdateAfterRaceStatus(int milisecondRemain)
        {
            var info = FinishedRacer.Select((racerInfo, i) => $"{racerInfo.racer.RoleName}: Hạng {i + 1}").ToList();
            foreach (var racerInfo in Racers)
            {
                if (FinishedRacer.Any(x => x.racer.RoleID == racerInfo.racer.RoleID))
                    continue;
                info.Add($"{racerInfo.racer.RoleName}: Không hoàn thành");
            }
            this.UpdateEventDetailsToPlayers("Cuộc đua thú cưng - Nhận thưởng", milisecondRemain,
                info.ToArray());
        }

        private bool UpdateRaceStatus()
        {
            var raceStopped = true;
            // var _Notify = new G2C_EventNotification
            // {
            //     EventName = "Cuộc đua thú cưng - Đang diễn ra",
            //     TotalInfo = new List<string>()
            // };

            var info = FinishedRacer.Select((racerInfo, i) => $"{racerInfo.racer.RoleName}: Hạng {i + 1}").ToList();

            foreach (var racerInfo in Racers)
            {
                if (FinishedRacer.Any(x => x.racer.RoleID == racerInfo.racer.RoleID))
                    continue;

                var distance = KTGlobal.GetDistanceBetweenPoints(racerInfo.racer.CurrentPos, racerInfo.destination);
                info.Add($"{racerInfo.racer.RoleName}: {distance:F3}");
                if (distance < 5)
                {
                    FinishedRacer.Add(racerInfo);
                }
                else
                {
                    raceStopped = false;
                }
            }

            if (winTick == 0 && FinishedRacer.Count > 0)
                winTick = DateTime.Now.Ticks;

            if (winTick > 0)
            {
                var winElapsed = new TimeSpan(DateTime.Now.Ticks - winTick);
                if (winElapsed.TotalSeconds > PetRace.Data.forceEndDuration)
                    raceStopped = true;
            }

            if (raceStopped)
            {
                UpdateEventDetailsToPlayers("Cuộc đua thú cưng", "Đã kết thúc", info.ToArray());
            }
            else
            {
                UpdateEventDetailsToPlayers("Cuộc đua thú cưng", "Đang diễn ra", info.ToArray());
            }


            return raceStopped;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Tạo các con thú tại theo vị trí tương ứng trong config và lưu lại
        /// </summary>
        private void CreateRacer()
        {
            // Xóa NPC cũ
            this.RemoveRacers();

            var randomRacers = PetRace.Data.racers.OrderBy(a => StaticRandom.GetRandom(1000000)).ToList();
            var index = 0;
            var racerStr = "";
            foreach (var track in PetRace.Data.tracks)
            {
                var racer = randomRacers[index];
                racerStr = $"{racerStr} | {racer}";
                GameManager.MonsterZoneMgr.AddDynamicMonsters(1, racer, -1,
                    1, track.start[0], track.start[1], default, default, 1, 1,
                    Entities.Direction.DOWN_RIGHT, Entities.KE_SERIES_TYPE.series_none,
                    Entities.MonsterAIType.DynamicNPC, -1, -1,
                    -1, "PetRace", (npc) =>
                    {
                        this.Racers.Add(new RacerInfo
                        {
                            racer = npc,
                            destination = new Point(track.finish[0], track.finish[1])
                        });
                    });
                index += 1;
            }

            LogManager.WriteLog(LogTypes.PetRace, $"Chuẩn bị đua thú: {racerStr}");

            // Nếu sự kiện đã kết thúc
            if (this.isDisposed)
            {
                // Xóa NPC
                this.RemoveRacers();
            }
        }


        /// <summary>
        /// Xóa NPC
        /// </summary>
        private void RemoveRacers()
        {
            // Nếu tồn tại NPC đang di chuyển
            if (this.Racers == null)
                return;
            foreach (var movingNPC in Racers)
            {
                this.RemoveMonster(movingNPC.racer);
            }

            // Hủy
            this.Racers.Clear();
            this.FinishedRacer.Clear();
        }

        #endregion

        #region Lua Bridge

        public Dictionary<int, string> GetRacers()
        {
            if (currentRaceState == RaceState.NULL)
            {
                return null;
            }

            var racers = new Dictionary<int, string>();

            foreach (var racerInfo in Racers)
            {
                racers[racerInfo.racer.MonsterInfo.ExtensionID] = racerInfo.racer.RoleName;
            }

            return racers;
        }

        public bool AlreadyBet(KPlayer player)
        {
            return BetRecords.TryGetValue(player.RoleID, out _);
        }


        public bool Bet(KPlayer player, int racersId, int betValue)
        {
            if (AlreadyBet(player))
                return false;

            BetPack pack = BetPackData.packs[betValue - 1];
            int VALUE = KTGlobal.GetMoneyhave(player, (MoneyType)pack.moneyType);
            if (VALUE < pack.number)
            {
                KT_TCPHandler.SendOpenUI(player, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], new List<string> { "Message", "Bẳng hữu không đem theo đủ tiền!" });
                return false;
            }
            //Bình chọn thành công
            KTGlobal.SubMoney(player, pack.number, (MoneyType)pack.moneyType, $"PETRACE_BET_{betValue}");
            BetRecords[player.RoleID] = new Tuple<int, int>(racersId, betValue);
            LogManager.WriteLog(LogTypes.PetRace, $"{player.RoleName}[{player.RoleID}] cược {racersId}, gói cược {betValue}");
            return true;
        }

        public Lua_Vector2 GetBetDescription(KPlayer player)
        {
            if (!BetRecords.TryGetValue(player.RoleID, out var record))
                return null;
            var racerInfo = Racers.FirstOrDefault(x => x.racer.MonsterInfo.ExtensionID == record.Item1);
            if (racerInfo == null)
                return null;
            return new Lua_Vector2(record.Item1, record.Item2);
        }

        public bool IsWinner(KPlayer player)
        {
            if (!BetRecords.TryGetValue(player.RoleID, out var record))
                return false;

            if (FinishedRacer == null)
                return false;

            if (FinishedRacer.Count == 0)
                return false;

            return FinishedRacer[0].racer.MonsterInfo.ExtensionID == record.Item1;
        }


        public int GetRanking(KPlayer player)
        {
            if (!BetRecords.TryGetValue(player.RoleID, out var record))
                return -1;

            if (FinishedRacer == null|| FinishedRacer.Count == 0)
                return -1;
            
            for(int i = 0; i< FinishedRacer.Count;i++)
            {
                if (FinishedRacer[i].racer.MonsterInfo.ExtensionID == record.Item1)
                    return i + 1;
            }
            return -1;

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
    public static partial class KTLuaLib_System
    {
        public static int PetRace_GetRaceStatus()
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return -1;
            return (int)script.currentRaceState;
        }

        public static void PetRace_Join(Lua_Player player)
        {
            int status = 0;
            KPlayer client = player.RefObject;
            var script = PetRace_ActivityScript.activity_script;
            
            if (script == null)
            {
                KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], new List<string> { "Message", "Hiện tại không có cuộc đua nào đang diễn ra!" });
                return;
            }
            PetRace_Script_Main.RaceState state = script.currentRaceState;
            if(script.AlreadyBet(client))
            {
                var betRecord = PetRace_GetBetRecord(player);
                
                //Nhận phần thưởng
                if (state == PetRace_Script_Main.RaceState.ENDED)
                {
                    bool isReceived = script.IsRewardReceived(client);
                    int ranking = script.GetRanking(client);
                    int petId = (int)betRecord.GetX();
                    int packId = (int)betRecord.GetY();
                    string petName = script.GetRacers().Where(x => x.Key == petId).FirstOrDefault().Value;
                    string cmdStr = $"0:{petId}:{petName}:{packId}:{ranking}:{isReceived}";
                    GameManager.ClientMgr.SendToClient(client, cmdStr, (int)TCPGameServerCmds.CMD_KT_PET_RACE);
                }
                //Hiện popup thông tin bình chọn
                else
                {
                    int petId = (int)betRecord.GetX();
                    string petName = script.GetRacers().Where(x => x.Key == petId).FirstOrDefault().Value;
                    string cmdStr = $"1:{petName}:{betRecord.GetY()}";
                    GameManager.ClientMgr.SendToClient(client, cmdStr, (int)TCPGameServerCmds.CMD_KT_PET_RACE);
                }
                

            }
            else
            {
                if(state == PetRace_Script_Main.RaceState.BETTING)
                {
                    var racers = string.Join(":",
                                        script.GetRacers().Select(x => $"{x.Key}_{x.Value}"));
                    var racersStringEncoded = DataHelper.EncodeBase64(racers);
                    string cmdStr = $"2:{racersStringEncoded}";
                    GameManager.ClientMgr.SendToClient(client, cmdStr, (int)TCPGameServerCmds.CMD_KT_PET_RACE);
                }
                else
                {
                    KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], new List<string> { "Message", "Cuộc đua đang diễn ra không thể bình chọn!" });
                }
                
            }
            
        }

        public static void PetRace_AccumulateUI_Open(Lua_Player luaPlayer)
        {
            KPlayer player = luaPlayer.RefObject;
            List<BetPack> packs = BetPackData.packs;
            for(int i = 0;i < packs.Count;i++)
            {
                BetPack pack = packs[i];
                int totalWin = player.GetValueOfForeverRecore(pack.winRecord);
                if (totalWin < 0) totalWin = 0;
                pack.totalWin = totalWin;
            }

            var packsString = string.Join(":",
                                        packs.Select(x => $"{x.id}_{x.totalWin}_{x.luckyChest5}_{x.luckyChest10}_{x.luckyChest15}"));
            var packsStringEncoded = DataHelper.EncodeBase64(packsString);
            GameManager.ClientMgr.SendToClient(player, packsStringEncoded, (int)TCPGameServerCmds.CMD_KT_PET_RACE_OPEN_ACCUMULATE_UI);
        }


        public static IDictionary<int, string> PetRace_GetRacers()
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return null;
            return script.GetRacers();
        }

        public static bool PetRace_CheckBet(Lua_Player player)
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return false;
            return script.AlreadyBet(player.RefObject);
        }

        public static bool PetRace_Bet(Lua_Player player, int racersID, int bet_value)
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return false;
            return script.Bet(player.RefObject, racersID, bet_value);
        }

        public static Lua_Vector2 PetRace_GetBetRecord(Lua_Player player)
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return null;
            return script.GetBetDescription(player.RefObject);
        }

        public static bool PetRace_IsWinner(Lua_Player player)
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return false;
            return script.IsWinner(player.RefObject);
        }

        public static bool PetRace_SetRewardReceived(Lua_Player player)
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return false;
            return script.SetRewardReceived(player.RefObject);
        }

        public static bool PetRace_IsRewardReceived(Lua_Player player)
        {
            var script = PetRace_ActivityScript.activity_script;
            if (script == null)
                return false;
            return script.IsRewardReceived(player.RefObject);
        }

        public static void ForceStartRace(Lua_Player player)
        {
            if (!player.IsGM())
                return;
            KTActivityManager.StartActivity(500);
        }
    }
}