using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý quái đặc biệt không bị xóa khi mất người
    /// </summary>
    public partial class KTMonsterTimerManager
    {
        #region Define

        /// <summary>
        /// Danh sách Timer đang thực thi cho loại quái đặc biệt
        /// </summary>
        private readonly ConcurrentDictionary<int, MonsterTimer> specialMonsters =
            new ConcurrentDictionary<int, MonsterTimer>();

        /// <summary>
        /// Danh sách quái cần thêm vào
        /// </summary>
        private readonly ConcurrentQueue<QueueItem> waitingQueueSpecial = new ConcurrentQueue<QueueItem>();

        #endregion

        #region Core

        /// <summary>
        /// Worker cho quái đặc biệt
        /// </summary>
        private BackgroundWorker[] specialWorker;

        /// <summary>
        /// Khởi tạo luồng quản lý quái đặc biệt
        /// </summary>
        private void StartSpecialMonsterTimer()
        {
            // Thời gian báo cáo trạng thái lần cuối
            var lastReportStateTick = new long[ServerConfig.Instance.MaxMonsterTimer];
            // Thời gian thực thi công việc lần cuối
            var lastWorkerBusyTick = new long[ServerConfig.Instance.MaxMonsterTimer];

            // Khởi tạo Worker
            this.specialWorker = new BackgroundWorker[ServerConfig.Instance.MaxMonsterTimer];
            for (var i = 0; i < this.specialWorker.Length; i++)
            {
                this.specialWorker[i] = new BackgroundWorker();
                this.specialWorker[i].DoWork += this.DoSpecialMonsterBackgroundWork;
                this.specialWorker[i].RunWorkerCompleted += this.Worker_Completed;
                lastReportStateTick[i] = KTGlobal.GetCurrentTimeMilis();
                lastWorkerBusyTick[i] = 0;
            }

            // Tạo Timer riêng
            var timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Interval = 500;
            timer.Elapsed += (o, e) =>
            {
                // Nếu có lệnh ngắt Server
                if (Program.NeedExitServer)
                {
                    LogManager.WriteLog(LogTypes.TimerReport,
                        $"Terminate => KTMonsterTimer_Special {specialWorker.Length}");
                    // Ngừng luồng
                    timer.Stop();
                    return;
                }

                Diagnostic.SpecialMonsterTimerCounter = specialWorker.Length;

                for (var i = 0; i < this.specialWorker.Length; i++)
                {
                    if (KTGlobal.GetCurrentTimeMilis() - lastReportStateTick[i] >= 5000)
                    {
                        LogManager.WriteLog(LogTypes.TimerReport,
                            "Tick alive => KTMonsterTimer_Special, total monster timers = " +
                            this.specialMonsters.Count);
                        lastReportStateTick[i] = KTGlobal.GetCurrentTimeMilis();
                    }

                    if (this.specialWorker[i].IsBusy)
                    {
                        // Quá thời gian thì Cancel
                        if (KTGlobal.GetCurrentTimeMilis() - lastWorkerBusyTick[i] >= 2000)
                        {
                            // Cập nhật thời gian thực thi công việc lần cuối
                            lastWorkerBusyTick[i] = KTGlobal.GetCurrentTimeMilis();
                            LogManager.WriteLog(LogTypes.TimerReport,
                                string.Format("Timeout => KTMonsterTimer_Special, total monster timers = " +
                                              this.specialMonsters.Count));
                        }
                    }
                    else
                    {
                        // Cập nhật thời gian thực thi công việc lần cuối
                        lastWorkerBusyTick[i] = KTGlobal.GetCurrentTimeMilis();
                        // Thực thi công việc
                        if (!this.specialWorker[i].IsBusy)
                        {
                            this.specialWorker[i].RunWorkerAsync(i);
                        }
                    }
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Thực hiện công việc
        /// </summary>
        private void DoSpecialMonsterBackgroundWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // ID luồng
                var workerID = (int)e.Argument;

                // Duyệt danh sách chờ
                while (!this.waitingQueueSpecial.IsEmpty)
                {
                    if (this.waitingQueueSpecial.TryDequeue(out var item))
                    {
                        // Nếu đối tượng không tồn tại
                        if (item.Data == null)
                        {
                            continue;
                        }

                        if (item.Type == 1)
                        {
                            var newTimer = (MonsterTimer)item.Data;
                            // Nếu Timer cũ tồn tại
                            if (this.specialMonsters.TryGetValue(newTimer.Owner.RoleID, out var monsterTimer))
                            {
                                // Thực hiện Reset đối tượng
                                monsterTimer.Owner.Reset();
                                // Xóa Timer cũ
                                this.specialMonsters.TryRemove(newTimer.Owner.RoleID, out _);
                                // FIX LEAK: Remove from MonsterManager
                                if (monsterTimer.Owner.MonsterZoneNode == null)
                                {
                                    GameManager.MonsterMgr.RemoveMonster(monsterTimer.Owner);
                                }
                            }

                            // Thêm vào danh sách
                            this.specialMonsters[newTimer.Owner.RoleID] = newTimer;
                        }
                        else if (item.Type == 0)
                        {
                            var monster = (Monster)item.Data;
                            // Nếu Timer cũ tồn tại
                            if (this.specialMonsters.TryGetValue(monster.RoleID, out var monsterTimer))
                            {
                                // Thực hiện Reset đối tượng
                                monsterTimer.Owner.Reset();
                                // Xóa Timer cũ
                                this.specialMonsters.TryRemove(monster.RoleID, out _);
                            }
                        }
                    }
                }

                // Danh sách cần xóa
                List<int> toRemoveMonsterTimers = null;
                // Duyệt danh sách
                var keys = this.specialMonsters.Keys.ToList();
                foreach (var key in keys)
                {
                    // Nếu không tồn tại
                    if (!this.specialMonsters.TryGetValue(key, out var monsterTimer))
                    {
                        if (toRemoveMonsterTimers == null)
                        {
                            toRemoveMonsterTimers = new List<int>();
                        }

                        toRemoveMonsterTimers.Add(key);
                        continue;
                    }

                    // Nếu Null
                    if (monsterTimer == null || monsterTimer.Owner == null || monsterTimer.Owner.IsDead())
                    {
                        if (toRemoveMonsterTimers == null)
                        {
                            toRemoveMonsterTimers = new List<int>();
                        }

                        toRemoveMonsterTimers.Add(key);
                        continue;
                    }

                    // Nếu không phải công việc của Worker này
                    if (monsterTimer.Owner.RoleID % ServerConfig.Instance.MaxMonsterTimer != workerID)
                    {
                        continue;
                    }

                    // Nếu chưa Start
                    if (!monsterTimer.IsStarted)
                    {
                        this.ExecuteAction(monsterTimer.Start, null, false);
                        // Đánh dấu đã thực hiện Start
                        monsterTimer.IsStarted = true;
                    }

                    // Nếu đã đến thời điểm Tick
                    if (monsterTimer.IsTickTime && monsterTimer.HasCompletedLastTick)
                    {
                        // Thời điểm Tick hiện tại
                        var currentTick = KTGlobal.GetCurrentTimeMilis();
                        monsterTimer.LastTick = currentTick;

                        // Đánh dấu chưa hoàn thành Tick lần trước
                        monsterTimer.HasCompletedLastTick = false;
                        // Thực thi sự kiện
                        this.ExecuteAction(monsterTimer.Tick, null, false);
                    }
                }


                // Nếu có đánh dấu phải xóa luồng quái
                if (this.forceClearAllMonsterTimers)
                {
                    this.specialMonsters.Clear();
                }
                else
                {
                    // Duyệt danh sách cần xóa
                    if (toRemoveMonsterTimers != null)
                    {
                        foreach (var key in toRemoveMonsterTimers)
                        {
                            if (this.specialMonsters.TryRemove(key, out var removedTimer))
                            {
                                // FIX LEAK: Remove from MonsterManager
                                if (removedTimer != null && removedTimer.Owner != null)
                                {
                                    // Only remove if NOT managed by a Zone
                                    if (removedTimer.Owner.MonsterZoneNode == null)
                                    {
                                        GameManager.MonsterMgr.RemoveMonster(removedTimer.Owner);
                                    }
                                }
                            }
                        }

                        toRemoveMonsterTimers.Clear();
                        toRemoveMonsterTimers = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }

        #endregion
    }
}