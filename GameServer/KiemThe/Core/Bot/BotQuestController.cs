using System;
using System.Diagnostics;

namespace GameServer.Logic
{
    public enum BotQuestState
    {
        Idle,
        SelectQuest,
        MoveToNpc,
        AcceptQuest,
        MoveToTarget,
        ExecuteQuest,
        ReturnToNpc,
        SubmitQuest,
        Completed,
        Failed
    }
    public enum BotQuestStepResult
    {
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// Implement using server quest rules. Each call must be short and non-blocking.
    /// Return Completed only after the server confirms the current step.
    /// Repeated Running calls must not duplicate movement, acceptance or rewards.
    /// </summary>
    public interface IBotQuestActions
    {
        BotQuestStepResult ExecuteStep(KTBot bot, int questId, BotQuestState state);

        // Cancel actions started by this adapter; do not abandon the server quest.
        void Cancel(KTBot bot, int questId);
    }

    /// <summary>
    /// Runtime scaffold only: no quest selection, movement, combat, rewards or persistence.
    /// Call Start/Stop/Tick from the BOT scheduler, not concurrently from admin threads.
    /// </summary>
    public sealed class BotQuestController
    {
        private readonly KTBot owner;
        private readonly Stopwatch stepTimer = new Stopwatch();
        private IBotQuestActions actions;
        private TimeSpan stepTimeout;
        public bool IsPausedForRevive { get; private set; }
        public bool IsPausedForActivity { get; private set; }
        public BotQuestState State { get; private set; } = BotQuestState.Idle;
        public int? QuestId { get; private set; }
        public string LastError { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPausedForNoPlayers { get; private set; }
        public BotQuestController(KTBot owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Start(int questId, IBotQuestActions actions, TimeSpan stepTimeout)
        {
            if (questId <= 0) throw new ArgumentOutOfRangeException(nameof(questId));
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            if (stepTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(stepTimeout));
            if (this.IsRunning) throw new InvalidOperationException("Stop the current quest controller first.");

            this.QuestId = questId;
            this.actions = actions;
            this.stepTimeout = stepTimeout;
            this.LastError = null;
            this.IsRunning = true;
            this.IsPausedForRevive = false;
            this.IsPausedForActivity = false;
            this.TransitionTo(BotQuestState.SelectQuest);
        }

        public void Stop()
        {
            this.IsRunning = false;
            this.stepTimer.Reset();
            this.CancelActions();
            this.QuestId = null;
            this.State = BotQuestState.Idle;
            this.IsPausedForRevive = false;
            this.IsPausedForActivity = false;
        }

        public void ResumeAfterRevive()
        {
            if (!this.IsPausedForRevive ||
                this.owner.IsDead() ||
                this.owner.QuestExecutionDisabled)
            {
                return;
            }

            // Có thể đã hồi sinh xa NPC, nên cần đi lại đến NPC.
            if (this.State == BotQuestState.AcceptQuest)
            {
                this.State = BotQuestState.MoveToNpc;
            }
            else if (this.State == BotQuestState.SubmitQuest)
            {
                this.State = BotQuestState.ReturnToNpc;
            }

            this.IsPausedForRevive = false;

            if (this.IsRunning && !this.IsPausedForActivity && !this.IsPausedForNoPlayers)
                this.stepTimer.Start();
        }

        public void Tick()
        {
            if (!this.IsRunning || this.IsPausedForRevive || this.IsPausedForActivity || this.IsPausedForNoPlayers)
                return;
            // Pause while dead; revival is owned by the server, not this scaffold.
            if (this.owner.IsDead())
            {
                this.stepTimer.Stop();
                return;
            }
            this.stepTimer.Start();
            if (this.stepTimer.Elapsed >= this.stepTimeout)
            {
                this.Fail("Timed out in " + this.State);
                return;
            }
            try
            {
                this.owner.CheckQuestCancellation(this.State.ToString());
                var result = this.actions.ExecuteStep(this.owner, this.QuestId.Value, this.State);
                this.owner.QuestCancellation.ThrowIfCancellationRequested();
                if (result == BotQuestStepResult.Running) return;
                if (result != BotQuestStepResult.Completed)
                {
                    this.Fail("Action failed in " + this.State);
                    return;
                }
                switch (this.State)
                {
                    case BotQuestState.SelectQuest: this.TransitionTo(BotQuestState.MoveToNpc); break;
                    case BotQuestState.MoveToNpc: this.TransitionTo(BotQuestState.AcceptQuest); break;
                    case BotQuestState.AcceptQuest: this.TransitionTo(BotQuestState.MoveToTarget); break;
                    case BotQuestState.MoveToTarget: this.TransitionTo(BotQuestState.ExecuteQuest); break;
                    case BotQuestState.ExecuteQuest: this.TransitionTo(BotQuestState.ReturnToNpc); break;
                    case BotQuestState.ReturnToNpc: this.TransitionTo(BotQuestState.SubmitQuest); break;
                    case BotQuestState.SubmitQuest:
                        this.State = BotQuestState.Completed;
                        this.IsRunning = false;
                        this.stepTimer.Reset();
                        this.actions = null;
                        break;
                    default: this.Fail("Unexpected state " + this.State); break;
                }
            }
            catch (Exception ex)
            {
                this.Fail(ex.Message);
            }
        }

        private void TransitionTo(BotQuestState state)
        {
            this.State = state;
            this.stepTimer.Reset();

            if (!this.IsPausedForRevive &&
                !this.IsPausedForActivity &&
                !this.IsPausedForNoPlayers &&
                !this.owner.IsDead())
            {
                this.stepTimer.Start();
            }
        }

        public void PauseForActivity()
        {
            this.IsPausedForActivity = true;
            this.stepTimer.Stop();
        }

        public void ResumeAfterActivity()
        {
            if (!this.IsPausedForActivity) return;
            this.IsPausedForActivity = false;
            if (this.State == BotQuestState.AcceptQuest) this.State = BotQuestState.MoveToNpc;
            if (this.State == BotQuestState.SubmitQuest) this.State = BotQuestState.ReturnToNpc;
            if (this.IsRunning &&
                !this.IsPausedForRevive &&
                !this.IsPausedForNoPlayers &&
                !this.owner.IsDead()) this.stepTimer.Start();
        }

        public void PauseForRevive()
        {
            if (!this.IsRunning || this.IsPausedForRevive)
                return;

            this.IsPausedForRevive = true;
            this.stepTimer.Stop();
        }

        private void Fail(string error)
        {
            this.LastError = error;
            this.State = BotQuestState.Failed;
            this.IsRunning = false;
            this.stepTimer.Reset();
            this.CancelActions();
        }

        private void CancelActions()
        {
            var previous = this.actions;
            this.actions = null;
            if (previous == null || !this.QuestId.HasValue) return;
            try { previous.Cancel(this.owner, this.QuestId.Value); }
            catch (Exception ex) { this.LastError = (this.LastError ?? "") + " Cancel: " + ex.Message; }
        }

        public void PauseForNoPlayers()
        {
            if (this.IsPausedForNoPlayers)
                return;

            this.IsPausedForNoPlayers = true;
            this.stepTimer.Stop();
        }

        public void ResumeAfterPlayersOnline()
        {
            if (!this.IsPausedForNoPlayers)
                return;

            this.IsPausedForNoPlayers = false;

            // Đường đi cũ đã bị hủy khi nghỉ, cần tiếp cận NPC lại.
            if (this.State == BotQuestState.AcceptQuest)
                this.State = BotQuestState.MoveToNpc;
            else if (this.State == BotQuestState.SubmitQuest)
                this.State = BotQuestState.ReturnToNpc;

            if (this.IsRunning &&
                !this.IsPausedForRevive &&
                !this.IsPausedForActivity &&
                !this.owner.IsDead())
            {
                this.stepTimer.Start();
            }
        }
    }
}