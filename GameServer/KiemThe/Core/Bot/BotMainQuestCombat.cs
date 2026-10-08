using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows;
using GameServer.KiemThe;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using Quest = GameServer.KiemThe.Core.Task.Task;

namespace GameServer.Logic
{
    public sealed partial class BotMainQuestActions
    {
        private Monster combatTarget;
        private int combatMap = -1;
        private readonly ConcurrentQueue<Monster> kills = new ConcurrentQueue<Monster>();
        private readonly HashSet<Monster> creditedKills = new HashSet<Monster>();
        private readonly System.Diagnostics.Stopwatch combatClock = System.Diagnostics.Stopwatch.StartNew();
        private long nextMove;
        private long nextAttack;
        private int questHomeMap;

        // Called only by the server's real OnKillObject event, never by polling IsDead().
        public void RecordKill(Monster monster)
        {
            this.kills.Enqueue(monster);
        }

        private List<MapTeleport> FindRoute(KTBot bot, Func<int, bool> destination)
        {
            var pending = new Queue<Tuple<int, List<MapTeleport>>>();
            var visited = new HashSet<int> { bot.CurrentMapCode };
            pending.Enqueue(Tuple.Create(bot.CurrentMapCode, new List<MapTeleport>()));
            while (pending.Count > 0)
            {
                bot.CheckQuestCancellation("FindPortalRoute");
                var entry = pending.Dequeue();
                if (destination(entry.Item1)) return entry.Item2;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(entry.Item1, out GameMap map)) continue;
                foreach (var gate in map.MapTeleportDict.Values.OrderBy(g => g.Code))
                {
                    if (gate.Camp > 0 && gate.Camp != bot.Camp) continue;
                    if (gate.ToMapID <= 0 || !GameManager.MapMgr.DictMaps.ContainsKey(gate.ToMapID) || !visited.Add(gate.ToMapID)) continue;
                    var path = new List<MapTeleport>(entry.Item2) { gate };
                    pending.Enqueue(Tuple.Create(gate.ToMapID, path));
                }
            }
            throw new InvalidOperationException("No loaded portal route to quest destination.");
        }

        private BotQuestStepResult Travel(KTBot bot, int destination)
        {
            if (bot.CurrentMapCode == destination) return BotQuestStepResult.Completed;
            var route = this.FindRoute(bot, map => map == destination);
            var gate = route[0];
            if ((bot.CurrentPos - new Point(gate.X, gate.Y)).Length > Math.Max(20, gate.Radius))
            {
                this.MoveCombat(bot, new Point(gate.X, gate.Y));
                return BotQuestStepResult.Running;
            }
            KTBotManager.MoveQuestBotThroughPortal(bot, gate);
            this.nextMove = 0;
            this.Report(bot, "Portal -> map " + gate.ToMapID);
            return BotQuestStepResult.Running;
        }

        private BotQuestStepResult MoveToMonsters(KTBot bot, Quest quest)
        {
            if (this.combatMap < 0)
            {
                // Positive map IDs are literal. Negative/special IDs are resolved by
                // reachable maps containing the configured monster template, BOT-only.
                var route = this.FindRoute(bot, map =>
                    (quest.TargetMapCode <= 0 || map == quest.TargetMapCode) &&
                    GameManager.MonsterMgr.GetObjectsByMap(map, -1).Any(m => m.MonsterInfo.ExtensionID == quest.TargetNPC));
                this.combatMap = route.Count == 0 ? bot.CurrentMapCode : route.Last().ToMapID;
            }
            return this.Travel(bot, this.combatMap);
        }

        private Point lastNavigationPosition;
        private long lastNavigationProgress;
        private bool navigationObserved;
        private int navigationMap = -1;
        private readonly Queue<Point> remainingPath = new Queue<Point>();
        private readonly Random pathRetryRandom = new Random(Guid.NewGuid().GetHashCode());
        private Point cachedDestination, cachedAnchor;
        private int cachedMap = -1;
        private int pathFailures;
        private long pathRetryAt;

        private void ClearNavigation()
        {
            this.remainingPath.Clear();
            this.cachedMap = -1;
            this.pathFailures = 0;
            this.pathRetryAt = 0;
            this.nextMove = 0;
            this.navigationObserved = false;
        }

        private void RetryPath(KTBot bot, Point target, string reason)
        {
            this.remainingPath.Clear();
            // One initial attempt followed by at most three retries.
            if (++this.pathFailures > 3)
                throw new InvalidOperationException("Pathfinding failed after 3 retries: " + reason +
                    "; map=" + bot.CurrentMapCode + " from=" + bot.CurrentPos + " to=" + target);
            int delay = this.pathRetryRandom.Next(1000, 3001);
            this.pathRetryAt = this.combatClock.ElapsedMilliseconds + delay;
            this.Report(bot, "PathRetry " + this.pathFailures + "/3 in " + delay + " ms: " + reason);
        }

        private void MoveCombat(KTBot bot, Point target, bool chasingMonster = false)
        {
            bot.CheckQuestCancellation("MoveCombat");
            long now = this.combatClock.ElapsedMilliseconds;
            if (bot.IsDead()) return;
            if (bot.GetCurrentRunSpeed() <= 0)
                throw new InvalidOperationException("BOT movement speed is zero.");
            if (this.navigationMap != bot.CurrentMapCode) this.ClearNavigation();
            if (!this.navigationObserved || (bot.CurrentPos - this.lastNavigationPosition).Length >= 5)
            {
                this.navigationObserved = true;
                this.navigationMap = bot.CurrentMapCode;
                this.lastNavigationPosition = bot.CurrentPos;
                this.lastNavigationProgress = now;
            }
            var storyboard = KTOtherObjectStoryBoard.Instance;
            // Small target movements do not cancel a route that is already being followed.
            if (chasingMonster && this.cachedMap == bot.CurrentMapCode &&
                storyboard.HasStoryBoard(bot) && (target - this.cachedDestination).Length <= 60)
                target = this.cachedDestination;
            if (this.cachedMap != bot.CurrentMapCode || this.cachedDestination != target)
            {
                this.remainingPath.Clear();
                this.cachedMap = bot.CurrentMapCode;
                this.cachedDestination = target;
                if (storyboard.HasStoryBoard(bot)) storyboard.Remove(bot);
            }
            if (storyboard.HasStoryBoard(bot))
            {
                if (now - this.lastNavigationProgress < 15000) return;
                storyboard.Remove(bot);
                throw new InvalidOperationException("BOT has a path but has not moved for 15 seconds at " + bot.CurrentPos);
            }
            if (now < this.nextMove || now < this.pathRetryAt) return;
            if (!bot.IsCanPositiveMove())
                throw new InvalidOperationException("BOT movement is blocked by its current state.");
            GameMap map = GameManager.MapMgr.DictMaps[bot.CurrentMapCode];
            if (!map.CanMove(bot.CurrentPos))
                throw new InvalidOperationException("BOT is standing in an obstructed cell: " + bot.CurrentPos);
            if (!map.CanMove(target))
            {
                this.RetryPath(bot, target, "Navigation destination is obstructed");
                return;
            }
            // A displaced/interrupted BOT must not connect to a cached route across walls.
            if ((bot.CurrentPos - this.cachedAnchor).Length > 2 || this.remainingPath.Any(p => !map.CanMove(p)))
                this.remainingPath.Clear();

            bool reused = this.remainingPath.Count > 0;
            if (!reused)
            {
                Point start = new Point((int)bot.CurrentPos.X / map.MapGridWidth, (int)bot.CurrentPos.Y / map.MapGridHeight);
                Point end = new Point((int)target.X / map.MapGridWidth, (int)target.Y / map.MapGridHeight);
                var obstruction = map.MyNodeGrid.GetFixedObstruction();
                int searchLimit;
                switch (this.pathFailures)
                {
                    case 0: searchLimit = 30000; break;
                    case 1: searchLimit = 60000; break;
                    case 2: searchLimit = 120000; break;
                    default: searchLimit = obstruction.Length; break;
                }
                searchLimit = Math.Min(searchLimit, obstruction.Length);
                var finder = new HSGameEngine.Tools.AStar.PathFinderFast(obstruction)
                {
                    Formula = HSGameEngine.Tools.AStar.HeuristicFormula.Manhattan,
                    Diagonals = false, HeuristicEstimate = 1, ReopenCloseNodes = true,
                    SearchLimit = searchLimit, EnablePunish = false,
                    MaxNum = Math.Max(map.MapGridWidth, map.MapGridHeight),
                    CancellationToken = bot.QuestCancellation, TimeLimitMilliseconds = 300
                };
                bot.CheckQuestCancellation("FindPath");
                List<HSGameEngine.Tools.AStar.PathFinderNode> nodes;
                try { nodes = finder.FindPath(start, end); }
                catch (TimeoutException ex) { this.RetryPath(bot, target, ex.Message); return; }
                if (finder.TimedOut || nodes == null || nodes.Count == 0)
                {
                    this.RetryPath(bot, target, finder.TimedOut ? $"Pathfinding exceeded 300 ms; searchLimit={searchLimit}" : $"No path within {searchLimit} nodes");
                    return;
                }
                bot.QuestCancellation.ThrowIfCancellationRequested();
                nodes.Reverse();
                // Include the start-cell center so the fallback route also keeps safe grid transitions.
                var rawPath = nodes.Select(node => new Point(
                    node.X * map.MapGridWidth + map.MapGridWidth / 2,
                    node.Y * map.MapGridHeight + map.MapGridHeight / 2)).ToList();
                if (rawPath.Count == 0 || rawPath.Last() != target) rawPath.Add(target);
                bot.CheckQuestCancellation("SimplifyPath");
                var smoothPath = BotPathSimplifier.Simplify(map.MyNodeGrid.GetFixedObstruction(),
                    map.MapGridWidth, map.MapGridHeight, bot.CurrentPos, rawPath, bot.QuestCancellation);
                foreach (var point in smoothPath) this.remainingPath.Enqueue(point);
                this.Report(bot, "Path simplified: " + rawPath.Count + " -> " + smoothPath.Count + " points");
            }
            // The storyboard owns the entire route; packet windowing does not stop movement.
            var segment = this.remainingPath.ToList();
            var points = new List<Point> { bot.CurrentPos };
            points.AddRange(segment);
            string path = string.Join("|", points.Select(p => ((int)p.X) + "_" + ((int)p.Y)));
            bot.CheckQuestCancellation("Storyboard.Add");
            storyboard.Add(bot, path);
            bot.CheckQuestCancellation("Storyboard.Add completed");
            if (!storyboard.HasStoryBoard(bot))
                throw new InvalidOperationException("BOT movement scheduler rejected route to " + target);
            foreach (var point in segment) this.remainingPath.Dequeue();
            this.cachedAnchor = segment.Last();
            this.pathFailures = 0;
            this.pathRetryAt = 0;
            this.nextMove = this.combatClock.ElapsedMilliseconds + 500;
            this.lastNavigationProgress = this.combatClock.ElapsedMilliseconds;
            this.lastNavigationPosition = bot.CurrentPos;
            this.Report(bot, "Moving: map=" + bot.CurrentMapCode + " destination=" + target +
                " routeNodes=" + segment.Count + " continuous=True");
        }

        private BotQuestStepResult FightQuest(KTBot bot, Quest quest)
        {
            while (this.kills.TryDequeue(out Monster dead))
            {
                bot.CheckQuestCancellation("CreditKills");
                if (dead.MonsterInfo.ExtensionID == quest.TargetNPC && dead.CurrentMapCode == this.combatMap &&
                    dead.CurrentCopyMapID == bot.CurrentCopyMapID && this.creditedKills.Add(dead))
                {
                    this.objectiveValue = Math.Min(quest.TargetNum, this.objectiveValue + 1);
                    this.Report(bot, "Kill progress " + this.objectiveValue + "/" + quest.TargetNum);
                }
            }
            if (this.objectiveValue >= quest.TargetNum)
            {
                KTOtherObjectStoryBoard.Instance.Remove(bot);
                this.combatTarget = null;
                return BotQuestStepResult.Completed;
            }
            if (bot.CurrentMapCode != this.combatMap) return this.Travel(bot, this.combatMap);
            if (this.combatTarget == null || this.combatTarget.IsDead() ||
                this.combatTarget.CurrentMapCode != bot.CurrentMapCode || !KTLogic.IsOpposite(bot, this.combatTarget))
            {
                this.combatTarget = GameManager.MonsterMgr.GetObjectsByMap(bot.CurrentMapCode, bot.CurrentCopyMapID)
                    .Where(m => m.MonsterInfo.ExtensionID == quest.TargetNPC && !m.IsDead() && KTLogic.IsOpposite(bot, m))
                    .OrderBy(m => (m.CurrentPos - bot.CurrentPos).LengthSquared).FirstOrDefault();
                if (this.combatTarget != null) this.creditedKills.Remove(this.combatTarget);
                this.nextMove = 0;
            }
            if (this.combatTarget == null) return BotQuestStepResult.Running;
            int category = 11;
            if (ItemManager._TotalGameItem.TryGetValue(bot.WeaponID, out ItemData weapon)) category = weapon.Category;
            SkillDataEx skill = null;
            int skillId = 0;
            for (int id = 1; id <= 20; id++)
            {
                var candidate = KSkill.GetSkillData(id);
                if (candidate != null && candidate.IsBasicWeaponSkill && candidate.WeaponLimit == category)
                { skill = candidate; skillId = id; break; }
            }
            if (skill == null) throw new InvalidOperationException("No basic weapon skill for category " + category);
            if ((this.combatTarget.CurrentPos - bot.CurrentPos).Length > Math.Max(40, skill.AttackRadius * 0.8))
            {
                this.MoveCombat(bot, this.combatTarget.CurrentPos, true);
                return BotQuestStepResult.Running;
            }
            KTOtherObjectStoryBoard.Instance.Remove(bot);
            if (this.combatClock.ElapsedMilliseconds >= this.nextAttack)
            {
                bot.CheckQuestCancellation("UseSkill");
                bot.UseSkill(skillId, 1, this.combatTarget);
                bot.CheckQuestCancellation("UseSkill completed");
                this.nextAttack = this.combatClock.ElapsedMilliseconds + 1000;
            }
            return BotQuestStepResult.Running;
        }

        public void PrepareForRevive()
        {
            // Bỏ mục tiêu cũ.
            this.combatTarget = null;

            // Xóa đường và trạng thái tìm đường cũ.
            this.ClearNavigation();

            // Cho phép kiểm tra lại thời điểm đánh sau hồi sinh.
            this.nextAttack = 0;

            // Xóa trạng thái tiếp cận NPC.
            this.movingToNpc = -1;
            this.movementTimer.Reset();
        }
    }
}
