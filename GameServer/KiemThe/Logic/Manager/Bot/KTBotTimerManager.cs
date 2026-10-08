using GameServer.KiemThe.CopySceneEvents;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.LuaSystem;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static GameServer.Logic.KTBot;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý luồng thực thi Buff của nhân vật
    /// </summary>
    public class KTBotTimerManager
    {
        #region Singleton - Instance

        /// <summary>
        /// Đối tượng quản lý Timer của đối tượng
        /// </summary>
        public static KTBotTimerManager Instance { get; private set; }

        private readonly System.Diagnostics.Stopwatch reviveClock = System.Diagnostics.Stopwatch.StartNew();

        /// <summary>
        /// Private constructor
        /// </summary>
        private KTBotTimerManager() : base()
        {
            this.InitLocal();
        }

        #endregion Singleton - Instance

        #region Initialize

        /// <summary>
        /// Hàm này gọi đến khởi tạo đối tượng
        /// </summary>
        public static void Init()
        {
            KTBotTimerManager.Instance = new KTBotTimerManager();
        }

        #endregion Initialize

        private sealed class BotTimer
        {
            public KTBot Owner;
            public bool Started, CheckPlayersAround;
            public int Ticks, RandomMoveEvery;
            public bool PausedForNoPlayers;
        }
        private readonly Dictionary<KTBot, BotTimer> botTimers = new Dictionary<KTBot, BotTimer>();
        private BotWorkScheduler<BotTimer> scheduler;

        private void InitLocal()
        {
            scheduler = new BotWorkScheduler<BotTimer>(RunTick, Cleanup,
                (entry, reason) =>
                {
                    string operation = System.Threading.Volatile.Read(ref entry.Owner.QuestOperation);
                    entry.Owner.DisableQuestExecution(reason + "; Operation=" + operation);
                    BotRuntimeLog.Write("[BotCancellation] Bot=" + entry.Owner.RoleID + " Operation=" + operation + " Reason=" + reason);
                },
                entry => Describe(entry.Owner), BotRuntimeLog.Write,
                () => GameServer.Program.NeedExitServer);
        }

        private static string Describe(KTBot bot)
        {
            return string.Format("Bot={0} Name={1} Quest={2} State={3} Map={4} Pos={5} Operation={6}",
                bot.RoleID, bot.RoleName, bot.QuestController.QuestId,
                bot.QuestController.State, bot.CurrentMapCode, bot.CurrentPos,
                System.Threading.Volatile.Read(ref bot.QuestOperation));
        }

        public void Add(KTBot obj, bool checkPlayersAround = true)
        {
            if (obj == null || obj.QuestExecutionDisabled) return;
            lock (botTimers)
            {
                if (botTimers.ContainsKey(obj)) return;
                var entry = new BotTimer { Owner = obj, CheckPlayersAround = checkPlayersAround,
                    RandomMoveEvery = 5 + (int)((uint)obj.RoleID % 5) };
                if (!scheduler.Add(entry))
                {
                    obj.DisableQuestExecution("BOT scheduler capacity reached.");
                    BotRuntimeLog.Write("[BotCapacity] " + Describe(obj));
                    return;
                }
                botTimers.Add(obj, entry);
            }
        }

        // Removal is deferred: only the owner worker stops controllers or changes maps.
        public void Remove(KTBot obj)
        {
            if (obj == null) return;
            lock (botTimers)
                if (botTimers.TryGetValue(obj, out BotTimer entry)) scheduler.Remove(entry);
        }

        private void DisableBot(KTBot bot, string reason)
        {
            if (bot == null) return;
            bot.DisableQuestExecution(reason);
            BotRuntimeLog.Write("[BotDisabled] " + Describe(bot) + " Reason=" + reason);
            Remove(bot);
        }

        private void RunTick(BotTimer entry)
        {
            KTBot bot = entry.Owner;

            bot.CheckQuestCancellation("StartTick");

            // Dùng bộ quản lý client của Server, không dùng số người gần BOT.
            bool hasOnlinePlayers = GameManager.ClientMgr.GetClientCount() > 0;

            if (!hasOnlinePlayers)
            {
                if (!entry.PausedForNoPlayers)
                {
                    // Dừng đồng hồ timeout trước.
                    bot.QuestController.PauseForNoPlayers();

                    // Hủy đường di chuyển đang được thực thi.
                    KTOtherObjectStoryBoard.Instance.Remove(bot);

                    // Xóa trạng thái AI và mục tiêu/đường đi cũ.
                    bot.ResetAI();
                    bot.MainQuestActions.PrepareForRevive();
                    bot.SongJinController?.PauseForNoPlayers();

                    entry.PausedForNoPlayers = true;

                    BotRuntimeLog.Write(
                        $"[BotPaused] Bot={bot.RoleID}" +
                        $" Name={bot.RoleName}" +
                        " Reason=NoOnlinePlayers");
                }

                // Chỉ kết thúc lượt này. BOT vẫn ở trong scheduler.
                return;
            }

            if (entry.PausedForNoPlayers)
            {
                bot.QuestController.ResumeAfterPlayersOnline();
                entry.PausedForNoPlayers = false;

                BotRuntimeLog.Write(
                    $"[BotResumed] Bot={bot.RoleID}" +
                    $" Name={bot.RoleName}" +
                    " Reason=PlayersOnline");
            }

            // Luồng xử lý hiện tại.
            if (!entry.Started)
            {
                entry.Started = true;
                ProcessStart(bot);
            }

            bot.CheckQuestCancellation("ProcessTick");
            ProcessTick(bot);

            if (bot.QuestExecutionDisabled)
            {
                Remove(bot);
                return;
            }

            if (++entry.Ticks % entry.RandomMoveEvery == 0)
            {
                bot.CheckQuestCancellation("RandomMove");

                ProcessAIMoveTick(bot, entry.CheckPlayersAround);
            }

            if (entry.Ticks % 30 == 0)
            {
                BotRuntimeLog.Write("[BotHeartbeat] " + Describe(bot));
            }
        }

        private void Cleanup(BotTimer entry)
        {
            KTBot bot = entry.Owner;
            bot.SongJinController?.Cleanup();
            bot.QuestController.Stop();
            KTOtherObjectStoryBoard.Instance.Remove(bot);
            bot.RemoveAllLocalParams();
            bot.ResetAI();
            if (bot.QuestExecutionDisabled)
                GameServer.KiemThe.Logic.Manager.KTBotManager.Remove(bot);
            lock (botTimers) botTimers.Remove(bot);
        }

        #region Logic
        /// <summary>
        /// Đánh dấu có cần thiết phải xóa quái vật này ra khỏi Timer không
        /// <para>- Nếu chết</para>
        /// <para>- Nếu không có người chơi xung quanh</para>
        /// <param name="checkPlayersAround"></param>
        /// </summary>
        /// <returns></returns>
        private bool NeedToBeRemoved(KTBot bot, bool checkPlayersAround = false)
        {
            /// Nếu không có tham chiếu
            if (bot == null)
            {
                return true;
            }

            /// Nếu không tìm thấy bản đồ hiện tại
            if (!GameManager.MapMgr.DictMaps.TryGetValue(bot.CurrentMapCode, out GameMap map))
            {
                return true;
            }
            /// Nếu có phụ bản nhưng không tìm thấy thông tin
            if (bot.CurrentCopyMapID != -1 && !CopySceneEventManager.IsCopySceneExist(bot.CurrentCopyMapID, bot.CurrentMapCode))
            {
                return true;
            }

            /// Nếu kiểm tra người chơi xung quanh
            if (checkPlayersAround)
            {
                /// Nếu không có người chơi xung quanh
                if (bot.VisibleClientsNum <= 0)
                {
                    return true;
                }
            }

            /// Nếu thỏa mãn tất cả điều kiện
            return false;
        }

        /// <summary>
        /// Thực hiện hàm Start
        /// </summary>
        /// <param name="monster"></param>
        private void ProcessStart(KTBot bot)
        {
            /// Nếu vi phạm điều kiện, cần xóa khỏi Timer
            if (this.NeedToBeRemoved(bot))
            {
                this.Remove(bot);
                return;
            }

            /// Nếu Script có tồn tại trong hệ thống
            if (KTLuaScript.Instance.GetScriptByID(bot.ScriptID) != null)
            {
                /// ID bản đồ hiện tại
                GameManager.MapMgr.DictMaps.TryGetValue(bot.CurrentMapCode, out GameMap map);

                /// Thực hiện ScriptLua tương ứng
                KTLuaEnvironment.ExecuteBotScript_Start(map, bot, bot.ScriptID);
            }
        }

        /// <summary>
        /// Thực hiện hàm Tick
        /// </summary>
        /// <param name="monster"></param>
        private void ProcessTick(KTBot bot)
        {
            if (bot == null || bot.QuestExecutionDisabled)
                return;

            if (this.NeedToBeRemoved(bot))
            {
                this.DisableBot(bot, "Map or copy scene is unavailable.");
                return;
            }

            bot.SongJinController.Tick();
            if (bot.SongJinController.IsParticipating) return;

            if (bot.IsDead())
            {
                if (bot.IsInChallenge)
                {
                    KPlayer challengePlayer = GameManager.ClientMgr.FindClient(bot.ChallengePlayerID);
                    KT_TCPHandler.FinishBotChallenge(challengePlayer, bot);
                }

                long now = this.reviveClock.ElapsedMilliseconds;
                if(!bot.IsWaitingForRevive)
                {
                    bot.IsWaitingForRevive = true;
                    bot.ReviveAtMilliseconds = now + 20_000;
                    bot.ReviveRetryCount = 0;
                    bot.RecentDeathTimes.Enqueue(now);

                    while (bot.RecentDeathTimes.Count > 0 && now - bot.RecentDeathTimes.Peek() >= 600_000)
                    {
                        bot.RecentDeathTimes.Dequeue();
                    }
                    KTOtherObjectStoryBoard.Instance.Remove(bot);
                    // Cần bổ sung các hàm này, xem giải thích bên dưới.
                    bot.QuestController.PauseForRevive();
                    bot.MainQuestActions.PrepareForRevive();
                    BotRuntimeLog.Write(
                                    $"[BotDied] Bot={bot.RoleID}" +
                                    $" Quest={bot.QuestController.QuestId}" +
                                    $" Map={bot.CurrentMapCode}" +
                                    $" Pos={bot.CurrentPos}" +
                                    $" DeathsIn10Minutes={bot.RecentDeathTimes.Count}");

                    return;
                }
                // Chưa đến giờ: kết thúc tick, không giữ luồng.
                if (now < bot.ReviveAtMilliseconds)
                    return;

                // Đã hết thời gian chờ: thực hiện hồi sinh BOT. Hàm này sẽ
                // đồng bộ lại MapGrid và gửi trạng thái BOT mới tới client.
                TryReviveBot(bot, now);
                return;
            }

            // Tỷ thí có quyền ưu tiên cao hơn nhiệm vụ chính. Trong thời gian
            // này BOT chỉ đuổi/đánh đối thủ và không Tick QuestController.
            if (bot.IsInChallenge)
            {
                KPlayer challengePlayer = GameManager.ClientMgr.FindClient(bot.ChallengePlayerID);
                if (challengePlayer == null || challengePlayer.LogoutState ||
                    challengePlayer.IsDead() ||
                    challengePlayer.CurrentMapCode != bot.CurrentMapCode ||
                    challengePlayer.CurrentCopyMapID != bot.CurrentCopyMapID)
                {
                    KT_TCPHandler.FinishBotChallenge(challengePlayer, bot);
                    return;
                }

                bot.ChallengeTick(challengePlayer);
                return;
            }


            if (bot.CurrentActivity == BotActivity.Idle)
            {
                return;
            }

            // Chưa đến giờ: kết thúc lượt xử lý của BOT này.
            // Scheduler sẽ gọi lại ở lượt sau.
            if (DateTime.UtcNow < bot.NextQuestActionAtUtc)
            {
                return;
            }

            // Đến giờ mới khởi động nhiệm vụ và bắt đầu đếm timeout.
            if (!bot.QuestController.IsRunning &&
                bot.QuestController.State == BotQuestState.Idle &&
                bot.MainQuestActions.CompletedCount == 0)
            {
                bot.MainQuestActions.Start(bot, 100);

                Console.WriteLine(
                    $"[BotQuestStart] Bot={bot.RoleID} Name={bot.RoleName}");
            }

            // Xử lý nhiệm vụ ở các lượt tiếp theo.
            bot.QuestController.Tick();

            // Giữ nguyên xử lý lỗi trước khi chuyển nhiệm vụ.
            if (bot.QuestController.State == BotQuestState.Failed)
            {
                this.DisableBot(
                    bot,
                    bot.QuestController.LastError ?? "Quest failed.");

                return;
            }

            bot.MainQuestActions.ContinueChain(bot);
        }

        /// <summary>
        /// Thực hiện hàm AITick
        /// </summary>
        /// <param name="bot"></param>
        private void ProcessAIMoveTick(KTBot bot, bool checkPlayersAround = true)
        {
            if (bot == null || bot.QuestExecutionDisabled)
                return;
            if (bot.SongJinController.IsParticipating) return;
            /// Nếu vi phạm điều kiện, cần xóa khỏi Timer
            if (this.NeedToBeRemoved(bot, checkPlayersAround))
            {
                this.Remove(bot);
                return;
            }

            if (!bot.QuestController.IsRunning)
                bot.RandomMoveAround();
        }
        #endregion

        private void TryReviveBot(KTBot bot, long now)
        {
            if (bot == null ||
                bot.QuestExecutionDisabled ||
                !bot.IsWaitingForRevive ||
                now < bot.ReviveAtMilliseconds)
            {
                return;
            }

            try
            {
                if (bot.IsDead())
                {
                    // Hàm mới cần triển khai trong KTBotManager,
                    // KHÔNG phải hàm đã có sẵn.
                    KTBotManager.ReviveQuestBot(
                        bot,
                        bot.ReviveMapCode,
                        bot.RevivePosition);
                }

                if (bot.IsDead())
                {
                    throw new InvalidOperationException(
                        "BOT vẫn ở trạng thái chết sau khi hồi sinh.");
                }

                bot.MainQuestActions.PrepareForRevive();

                bot.IsWaitingForRevive = false;
                bot.ReviveRetryCount = 0;

                // Loại các lần chết đã quá 10 phút.
                while (bot.RecentDeathTimes.Count > 0 &&
                       now - bot.RecentDeathTimes.Peek() >= 600_000)
                {
                    bot.RecentDeathTimes.Dequeue();
                }

                if (bot.RecentDeathTimes.Count >= 3)
                {
                    bot.IsRestingAfterDeaths = true;

                    BotRuntimeLog.Write(
                        $"[BotResting] Bot={bot.RoleID}" +
                        " Reason=Chết ít nhất 3 lần trong 10 phút.");

                    // Giữ controller tạm dừng.
                    return;
                }

                bot.IsRestingAfterDeaths = false;
                bot.QuestController.ResumeAfterRevive();

                BotRuntimeLog.Write(
                    $"[BotRevived] Bot={bot.RoleID}" +
                    $" Quest={bot.QuestController.QuestId}" +
                    $" State={bot.QuestController.State}" +
                    $" Map={bot.CurrentMapCode}" +
                    $" Pos={bot.CurrentPos}");
            }
            catch (OperationCanceledException)
            {
                // Nhường cho bộ điều phối xử lý yêu cầu hủy.
                throw;
            }
            catch (Exception ex)
            {
                bot.ReviveRetryCount++;

                BotRuntimeLog.Write(
                    $"[BotReviveFailed] Bot={bot.RoleID}" +
                    $" Attempt={bot.ReviveRetryCount}" +
                    $" Error={ex.Message}");

                if (bot.ReviveRetryCount >= 3)
                {
                    this.DisableBot(
                        bot,
                        "Hồi sinh thất bại 3 lần: " + ex.Message);

                    return;
                }

                // Hẹn lần sau, không Sleep và không thử liên tục.
                bot.ReviveAtMilliseconds =
                    this.reviveClock.ElapsedMilliseconds + 5_000;
            }
        }
    }
}
