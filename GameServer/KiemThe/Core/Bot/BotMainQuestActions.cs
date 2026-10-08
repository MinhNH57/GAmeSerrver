using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using GameServer.KiemThe;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Logic;
using Quest = GameServer.KiemThe.Core.Task.Task;

namespace GameServer.Logic
{
    /// <summary>
    /// BOT-only, in-memory execution of main-quest NPC objectives.
    /// Does not impersonate KPlayer or write player task/reward records.
    /// Unsupported objectives stop visibly instead of being auto-completed.
    /// </summary>
    public sealed partial class BotMainQuestActions : IBotQuestActions
    {
        private const string BotTaskConfigPath = "Config/KT_Task/BotKillTasks.xml";
        private static readonly object botTaskConfigLock = new object();
        private static Dictionary<int, Quest> botTasks;

        private readonly HashSet<int> completed = new HashSet<int>();
        private readonly Stopwatch movementTimer = new Stopwatch();
        private int acceptedId;
        private int objectiveValue;
        private int movingToNpc = -1;
        private bool stopped;
        private BotQuestState lastState = BotQuestState.Idle;
        public string Status { get; private set; } = "Not started";
        public int CompletedCount => this.completed.Count;
        public int ObjectiveValue => this.objectiveValue;
        // Reward claims are deliberately pending until a BOT inventory/profile is implemented.
        public IReadOnlyCollection<int> PendingRewardQuestIds => this.completed.ToArray();

        public void Start(KTBot bot, int firstQuestId)
        {
            if (bot.QuestExecutionDisabled)
                return;
            if (bot.QuestController.IsRunning)
                throw new InvalidOperationException("BOT already has an active quest controller.");
            this.stopped = false;

            Quest quest = GetBotTask(firstQuestId);
            if (quest == null)
            {
                this.stopped = true;
                this.Report(bot, "Không tìm thấy nhiệm vụ đầu tiên " + firstQuestId + ".");
                return;

            }

            if (quest.TaskClass != (int)TaskClasses.MainTask)
            {
                this.stopped = true;
                this.Report(bot, "Nhiệm vụ " + firstQuestId + " không phải nhiệm vụ chính tuyến.");
                return;
            }

            bot.QuestController.Start(quest.ID, this, TimeSpan.FromMinutes(10));
        }

        public void ContinueChain(KTBot bot)
        {
            if (bot.QuestExecutionDisabled)
                return;
            if (this.stopped)
                return;

            var controller = bot.QuestController;

            if (controller.State == BotQuestState.Failed)
            {
                this.stopped = true;
                this.Report(bot, "Stopped: " + controller.LastError);
                return;
            }

            if (controller.State != BotQuestState.Completed)
                return;

            int completedQuestId = controller.QuestId ?? 0;
            Quest completedQuest = GetBotTask(completedQuestId);
            if (completedQuest == null)
            {
                this.stopped = true;
                this.Report(bot, "Không tìm thấy nhiệm vụ vừa hoàn thành " + completedQuestId + ".");
                return;
            }

            int nextQuestId = completedQuest.NextTask;
            if (nextQuestId <= 0)
            {
                this.stopped = true;
                this.Report(bot, "Đã hoàn thành chuỗi nhiệm vụ chính tuyến tại nhiệm vụ " + completedQuestId + ".");
                return;
            }

            Quest nextQuest = GetBotTask(nextQuestId);
            if (nextQuest == null || nextQuest.TaskClass != (int)TaskClasses.MainTask)
            {
                this.stopped = true;
                this.Report(bot, "Không tìm thấy nhiệm vụ chính tuyến tiếp theo " + nextQuestId + ".");
                return;
            }

            controller.Start(nextQuest.ID, this, TimeSpan.FromMinutes(10));
        }

        public BotQuestStepResult ExecuteStep(KTBot bot, int questId, BotQuestState state)
        {
            bot.CheckQuestCancellation(state.ToString());
            Quest quest = GetBotTask(questId);
            if (quest == null || quest.TaskClass != (int)TaskClasses.MainTask)
                throw new InvalidOperationException("Missing kill quest " + questId);
            if (state != this.lastState)
            {
                this.lastState = state;
                this.Report(bot, questId + " " + quest.Title + " / " + state);
            }
            switch (state)
            {
                case BotQuestState.SelectQuest:
                    this.ClearNavigation();
                    this.objectiveValue = 0;
                    this.combatMap = -1;
                    this.combatTarget = null;
                    this.creditedKills.Clear();
                    while (this.kills.TryDequeue(out _)) { }
                    this.questHomeMap = bot.CurrentMapCode;
                    ValidateEligibility(bot, quest, this.completed);
                    if (quest.TargetType != (int)TaskTypes.Talk && quest.TargetType != (int)TaskTypes.AnswerQuest && !IsBotKillTask(quest))
                        throw new NotSupportedException("Quest " + questId + " requires " + (TaskTypes)quest.TargetType + "; BOT action adapter is not implemented.");
                    return BotQuestStepResult.Completed;
                case BotQuestState.MoveToNpc:
                    return this.MoveToNpcAcrossMaps(bot, quest.SourceNPC, -1, this.questHomeMap);
                case BotQuestState.AcceptQuest:
                    RequireNearNpc(bot, quest.SourceNPC);
                    if (this.acceptedId != questId)
                    {
                        ValidateEligibility(bot, quest, this.completed);
                        this.acceptedId = questId;
                        this.objectiveValue = 0;
                    }
                    return BotQuestStepResult.Completed;
                case BotQuestState.MoveToTarget:
                    if (IsBotKillTask(quest)) return this.MoveToMonsters(bot, quest);
                    return this.MoveToNpcAcrossMaps(bot, quest.TargetNPC, quest.TargetMapCode, this.combatMap);
                case BotQuestState.ExecuteQuest:
                    if (this.acceptedId != questId) throw new InvalidOperationException("Quest was not accepted.");
                    if (IsBotKillTask(quest)) return this.FightQuest(bot, quest);
                    RequireNearNpc(bot, quest.TargetNPC);
                    if (quest.TargetType == (int)TaskTypes.AnswerQuest)
                        FindCorrectAnswer(quest.QuestionTable);
                    // Same objective threshold as TaskManager.IsQuestComplete for Talk/AnswerQuest.
                    // Increment only after reaching the configured NPC and validating the answer.
                    this.objectiveValue = 1;
                    return IsBotTaskComplete(quest, this.objectiveValue)
                        ? BotQuestStepResult.Completed : BotQuestStepResult.Running;
                case BotQuestState.ReturnToNpc:
                    return this.MoveToNpcAcrossMaps(bot, quest.DestNPC, -1, this.combatMap);
                case BotQuestState.SubmitQuest:
                    RequireNearNpc(bot, quest.DestNPC);
                    if (this.acceptedId != questId || !IsBotTaskComplete(quest, this.objectiveValue))
                        throw new InvalidOperationException("Quest objective is incomplete.");
                    if (this.completed.Add(questId))
                        this.Report(bot, "Objective completed: " + questId + "; reward pending BOT inventory integration.");
                    this.acceptedId = 0;
                    return BotQuestStepResult.Completed;
                default:
                    throw new InvalidOperationException("Unexpected quest state " + state);
            }
        }

        /// <summary>
        /// Nhiệm vụ của BOT có vùng ID riêng và không được nạp vào TaskManager của
        /// người chơi. Nhờ vậy ID 100 trong BotKillTasks không đè ID 100 của
        /// SystemTasks.xml.
        /// </summary>
        private static Quest GetBotTask(int taskId)
        {
            EnsureBotTasksLoaded();
            botTasks.TryGetValue(taskId, out Quest quest);
            return quest;
        }

        private static void EnsureBotTasksLoaded()
        {
            if (botTasks != null)
                return;

            lock (botTaskConfigLock)
            {
                if (botTasks != null)
                    return;

                string filePath = Global.GameResPath(BotTaskConfigPath);
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Không tìm thấy cấu hình nhiệm vụ BOT.", filePath);

                Config config;
                using (FileStream stream = File.OpenRead(filePath))
                {
                    var serializer = new XmlSerializer(typeof(Config));
                    config = serializer.Deserialize(stream) as Config;
                }

                if (config?.Tasks?.Task == null)
                    throw new InvalidDataException("BotKillTasks.xml không có danh sách <Tasks> hợp lệ.");

                List<Quest> configuredTasks = config.Tasks.Task;
                Quest invalidTask = configuredTasks.FirstOrDefault(x =>
                    x.TaskClass != (int)TaskClasses.MainTask || !IsBotKillTask(x));
                if (invalidTask != null)
                    throw new InvalidDataException("BotKillTasks.xml chứa nhiệm vụ không phải đánh quái: " + invalidTask.ID);

                botTasks = configuredTasks.ToDictionary(x => x.ID, x => x);
                Console.WriteLine("[BotMainQuest] Loaded " + botTasks.Count + " kill tasks from " + filePath);
            }
        }

        private static bool IsBotTaskComplete(Quest quest, int taskValue)
        {
            return quest != null &&
                   IsBotKillTask(quest) &&
                   taskValue >= quest.TargetNum;
        }

        private static bool IsBotKillTask(Quest quest)
        {
            return quest != null &&
                   (quest.TargetType == (int)TaskTypes.KillMonster ||
                    quest.TargetType == (int)TaskTypes.MonsterSomething);
        }

        internal static void ValidateEligibility(KTBot bot, Quest quest, HashSet<int> completed)
        {
            if (completed.Contains(quest.ID)) throw new InvalidOperationException("Quest already completed.");
            if (bot.m_Level < quest.MinLevel || bot.m_Level > quest.MaxLevel)
                throw new InvalidOperationException("BOT level does not satisfy quest conditions.");
            if (quest.SexCondition != 0 && quest.SexCondition != bot.RoleSex)
                throw new InvalidOperationException("BOT sex does not satisfy quest conditions.");
            if (quest.OccupCondition != 0 && quest.OccupCondition != bot.FactionID)
                throw new InvalidOperationException("BOT faction does not satisfy quest conditions.");
        }

        internal static int FindCorrectAnswer(string questionTable)
        {
            string[] parts = (questionTable ?? "").Split('|');
            if (parts.Length < 2) throw new InvalidOperationException("Invalid QuestionTable.");
            string[] answers = parts[1].Split('\n');
            int result = Array.FindIndex(answers, answer => answer.Contains("#"));
            if (result < 0) throw new InvalidOperationException("QuestionTable has no configured correct answer.");
            return result;
        }

        private static NPC ResolveNpc(KTBot bot, int npcId)
        {
            // Player Talk/AnswerQuest processing matches NPC template ID, not TargetMapCode.
            NPC npc = NPCGeneralManager.FindNPCBYRES(bot.CurrentMapCode, bot.CurrentCopyMapID, npcId);
            if (npc == null)
                throw new InvalidOperationException("NPC " + npcId + " is not on map " + bot.CurrentMapCode + "; cross-map BOT routing is not implemented.");
            return npc;
        }

        /// <summary>
        /// Đi đến NPC qua portal khi NPC không nằm ở map hiện tại.
        /// Các mã map âm trong nhiệm vụ là mã động, vì vậy ưu tiên map vừa
        /// thực hiện mục tiêu rồi mới tìm một map có NPC và có tuyến portal.
        /// </summary>
        private BotQuestStepResult MoveToNpcAcrossMaps(KTBot bot, int npcId, int configuredMap, int preferredMap)
        {
            if (NPCGeneralManager.FindNPCBYRES(bot.CurrentMapCode, bot.CurrentCopyMapID, npcId) != null)
                return this.MoveToNpc(bot, npcId);

            if (configuredMap > 0 && GameManager.MapMgr.DictMaps.ContainsKey(configuredMap))
                return this.Travel(bot, configuredMap);

            if (preferredMap > 0 && GameManager.MapMgr.DictMaps.ContainsKey(preferredMap) &&
                NPCGeneralManager.FindNPCBYRES(preferredMap, bot.CurrentCopyMapID, npcId) != null)
                return this.Travel(bot, preferredMap);

            var route = this.FindRoute(bot, map =>
                NPCGeneralManager.FindNPCBYRES(map, bot.CurrentCopyMapID, npcId) != null);
            if (route.Count == 0)
                Console.WriteLine($"NPC " + npcId + " is not available on a reachable map.");

            return this.Travel(bot, route.Last().ToMapID);
        }

        private static void RequireNearNpc(KTBot bot, int npcId)
        {
            NPC npc = ResolveNpc(bot, npcId);
            if ((npc.CurrentPos - bot.CurrentPos).Length > 100)
                throw new InvalidOperationException("BOT is not near NPC " + npcId);
        }

        private BotQuestStepResult MoveToNpc(KTBot bot, int npcId)
        {
            NPC npc = ResolveNpc(bot, npcId);
            if ((npc.CurrentPos - bot.CurrentPos).Length <= 100)
            {
                KTOtherObjectStoryBoard.Instance.Remove(bot);
                this.movingToNpc = -1;
                return BotQuestStepResult.Completed;
            }
            // MoveCombat keeps an active route and owns retry timing; no five-second gate.
            {
                // Target a walkable approach cell; NPC's own cell may be blocked.
                Point target = npc.CurrentPos;
                bool found = false;
                for (int radius = 40; radius <= 80 && !found; radius += 20)
                    for (int direction = 0; direction < 8 && !found; direction++)
                    {
                        double angle = direction * Math.PI / 4;
                        Point candidate = new Point((int)(target.X + radius * Math.Cos(angle)), (int)(target.Y + radius * Math.Sin(angle)));
                        if (!Global.InObs(ObjectTypes.OT_MONSTER, bot.CurrentMapCode, (int)candidate.X, (int)candidate.Y, 1) && KTGlobal.HasPath(bot.CurrentMapCode, bot.CurrentPos, candidate))
                        {
                            // Follow a bounded segment of the actual A* route to the NPC.
                            // MoveCombat checks route acceptance and actual movement progress.
                            // Path search has a finite node budget.
                            this.MoveCombat(bot, candidate);
                            found = true;
                        }
                    }
                if (!found) throw new InvalidOperationException("No walkable approach to NPC " + npcId);
                this.movingToNpc = npc.NPCID;
                this.movementTimer.Restart();
            }
            return BotQuestStepResult.Running;
        }

        public void Cancel(KTBot bot, int questId)
        {
            this.ClearNavigation();
            this.stopped = true;
            this.Report(bot, "Stopped: " + (bot.QuestController.LastError ?? "cancelled"));
            this.movementTimer.Reset();
            this.movingToNpc = -1;
            KTOtherObjectStoryBoard.Instance.Remove(bot);
        }

        private void Report(KTBot bot, string text)
        {
            this.Status = text;
            string message = string.Format("[BotMainQuest] {0} ({1}): {2}; map={3}, pos=({4:0},{5:0})",
                bot.RoleName, bot.RoleID, text, bot.CurrentMapCode, bot.CurrentPos.X, bot.CurrentPos.Y);
            BotRuntimeLog.Write(message);
        }

    }
}
