using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý luồng thực thi Buff của nhân vật
    /// </summary>
    public class KTPlayerTimerManager
    {
        #region Singleton - Instance

        /// <summary>
        /// Đối tượng quản lý Timer của đối tượng
        /// </summary>
        public static KTPlayerTimerManager Instance { get; private set; }

        /// <summary>
        /// Private constructor
        /// </summary>
        private KTPlayerTimerManager()
        {
        }

        #endregion Singleton - Instance

        #region Initialize

        /// <summary>
        /// Hàm này gọi đến khởi tạo đối tượng
        /// </summary>
        public static void Init()
        {
            KTPlayerTimerManager.Instance = new KTPlayerTimerManager();
            KTPlayerTimerManager.Instance.StartTimer();
        }

        #endregion Initialize

        #region Define

        /// <summary>
        /// Thông tin yêu cầu
        /// </summary>
        private class QueueItem
        {
            /// <summary>
            /// Loại yêu cầu
            /// <para>1: Thêm, 0: Xóa</para>
            /// </summary>
            public int Type { get; set; }

            /// <summary>
            /// Người chơi tương ứng
            /// </summary>
            public KPlayer Player { get; set; }
        }

        #endregion

        #region Core

        /// <summary>
        /// Worker
        /// </summary>
        private BackgroundWorker[] worker;

        /// <summary>
        /// Worker cập nhật vị trí
        /// </summary>
        private BackgroundWorker[] updateGridWorker;

        /// <summary>
        /// Bắt đầu chạy Timer
        /// </summary>
        private void StartTimer()
        {
            this.storyboardBuckets = new System.Collections.Concurrent.ConcurrentDictionary<int, KPlayer>[ServerConfig.Instance.MaxPlayerStoryBoardThread];
            for (var i = 0; i < this.storyboardBuckets.Length; i++)
            {
                this.storyboardBuckets[i] = new System.Collections.Concurrent.ConcurrentDictionary<int, KPlayer>();
            }
            this.updateGridBuckets = new System.Collections.Concurrent.ConcurrentDictionary<int, KPlayer>[ServerConfig.Instance.MaxUpdateGridThread];
            for (var i = 0; i < this.updateGridBuckets.Length; i++)
            {
                this.updateGridBuckets[i] = new System.Collections.Concurrent.ConcurrentDictionary<int, KPlayer>();
            }
            // Khởi tạo Worker Logic
            {
                // Thời gian báo cáo trạng thái lần cuối
                var lastReportStateTick = new long[ServerConfig.Instance.MaxPlayerStoryBoardThread];
                // Thời gian thực thi công việc lần cuối
                var lastWorkerBusyTick = new long[ServerConfig.Instance.MaxPlayerStoryBoardThread];
                // Thời gian log timeout lần cuối
                var lastTimeoutLogTick = new long[ServerConfig.Instance.MaxPlayerStoryBoardThread];
                // Khởi tạo worker
                this.worker = new BackgroundWorker[ServerConfig.Instance.MaxPlayerStoryBoardThread];
                for (var i = 0; i < this.worker.Length; i++)
                {
                    this.worker[i] = new BackgroundWorker();
                    this.worker[i].DoWork += this.DoBackgroundWork;
                    this.worker[i].RunWorkerCompleted += this.Worker_Completed;
                    // Thời gian báo cáo trạng thái lần cuối
                    lastReportStateTick[i] = KTGlobal.GetCurrentTimeMilis();
                    // Thời gian thực thi công việc lần cuối
                    lastWorkerBusyTick[i] = 0;
                    // Thời gian log timeout lần cuối
                    lastTimeoutLogTick[i] = 0;
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
                        LogManager.WriteLog(LogTypes.TimerReport, $"Terminate => Player Timer {worker.Length}");
                        // Ngừng luồng
                        timer.Stop();
                        return;
                    }

                    Diagnostic.PlayerTimerCounter = this.worker.Length;

                    for (var i = 0; i < this.worker.Length; i++)
                    {
                        if (KTGlobal.GetCurrentTimeMilis() - lastReportStateTick[i] >= 10000)
                        {
                            LogManager.WriteLog(LogTypes.TimerReport, "Tick alive => PlayerTimer");
                            lastReportStateTick[i] = KTGlobal.GetCurrentTimeMilis();
                        }

                        if (this.worker[i].IsBusy)
                        {
                            // Quá thời gian thì Cancel
                            if (KTGlobal.GetCurrentTimeMilis() - lastWorkerBusyTick[i] >= 2000)
                            {
                                // Cập nhật thời gian thực thi công việc lần cuối
                                lastWorkerBusyTick[i] = KTGlobal.GetCurrentTimeMilis();
                                if (KTGlobal.GetCurrentTimeMilis() - lastTimeoutLogTick[i] >= 10000)
                                {
                                    lastTimeoutLogTick[i] = KTGlobal.GetCurrentTimeMilis();
                                    LogManager.WriteLog(LogTypes.TimerReport, "Timeout => PlayerTimer");
                                }
                            }
                        }
                        else
                        {
                            // Cập nhật thời gian thực thi công việc lần cuối
                            lastWorkerBusyTick[i] = KTGlobal.GetCurrentTimeMilis();
                            // Thực thi công việc
                            if (!this.worker[i].IsBusy)
                            {
                                this.worker[i].RunWorkerAsync(i);
                            }
                        }
                    }
                };
                timer.Start();
            }

            // Khởi tạo Worker cập nhật vị trí
            {
                // Thời gian báo cáo trạng thái lần cuối
                var lastReportStateTick = new long[ServerConfig.Instance.MaxUpdateGridThread];
                // Thời gian thực thi công việc lần cuối
                var lastWorkerBusyTick = new long[ServerConfig.Instance.MaxUpdateGridThread];
                // Thời gian log timeout lần cuối
                var lastTimeoutLogTick = new long[ServerConfig.Instance.MaxUpdateGridThread];
                // Khởi tạo worker
                this.updateGridWorker = new BackgroundWorker[ServerConfig.Instance.MaxUpdateGridThread];
                for (var i = 0; i < this.updateGridWorker.Length; i++)
                {
                    this.updateGridWorker[i] = new BackgroundWorker();
                    this.updateGridWorker[i].DoWork += this.DoUpdateGridBackgroundWork;
                    this.updateGridWorker[i].RunWorkerCompleted += this.Worker_Completed;
                    // Thời gian báo cáo trạng thái lần cuối
                    lastReportStateTick[i] = KTGlobal.GetCurrentTimeMilis();
                    // Thời gian thực thi công việc lần cuối
                    lastWorkerBusyTick[i] = 0;
                    // Thời gian log timeout lần cuối
                    lastTimeoutLogTick[i] = 0;
                }

                // Tạo Timer riêng
                var timer = new System.Timers.Timer();
                timer.AutoReset = true;
                timer.Interval = 200;
                timer.Elapsed += (o, e) =>
                {
                    // Nếu có lệnh ngắt Server
                    if (Program.NeedExitServer)
                    {
                        LogManager.WriteLog(LogTypes.TimerReport,
                            $"Terminate => PlayerUpdateGridTimer {updateGridWorker.Length}");
                        // Ngừng luồng
                        timer.Stop();
                        return;
                    }

                    Diagnostic.PlayerGridTimerCounter = updateGridWorker.Length;

                    for (var i = 0; i < this.updateGridWorker.Length; i++)
                    {
                        if (KTGlobal.GetCurrentTimeMilis() - lastReportStateTick[i] >= 10000)
                        {
                            LogManager.WriteLog(LogTypes.TimerReport, "Tick alive => PlayerUpdateGridTimer");
                            lastReportStateTick[i] = KTGlobal.GetCurrentTimeMilis();
                        }

                        if (this.updateGridWorker[i].IsBusy)
                        {
                            // Quá thời gian thì Cancel
                            if (KTGlobal.GetCurrentTimeMilis() - lastWorkerBusyTick[i] >= 2000)
                            {
                                // Cập nhật thời gian thực thi công việc lần cuối
                                lastWorkerBusyTick[i] = KTGlobal.GetCurrentTimeMilis();
                                if (KTGlobal.GetCurrentTimeMilis() - lastTimeoutLogTick[i] >= 10000)
                                {
                                    lastTimeoutLogTick[i] = KTGlobal.GetCurrentTimeMilis();
                                    LogManager.WriteLog(LogTypes.TimerReport, "Timeout => PlayerUpdateGridTimer");
                                }
                            }
                        }
                        else
                        {
                            // Cập nhật thời gian thực thi công việc lần cuối
                            lastWorkerBusyTick[i] = KTGlobal.GetCurrentTimeMilis();
                            // Thực thi công việc
                            if (!this.updateGridWorker[i].IsBusy)
                            {
                                this.updateGridWorker[i].RunWorkerAsync(i);
                            }
                        }
                    }
                };
                timer.Start();
            }
        }

        /// <summary>
        /// Sự kiện khi Background Worker hoàn tất công việc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogManager.WriteLog(LogTypes.Exception, e.Error.ToString());
            }
        }

        /// <summary>
        /// Thực hiện công việc
        /// </summary>
        private void DoBackgroundWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // ID luồng
                var workerID = (int)e.Argument;

                this.ProcessWaitingQueue();

                if (workerID >= 0 && workerID < this.storyboardBuckets.Length)
                {
                    var bucket = this.storyboardBuckets[workerID];
                    foreach (var kv in bucket)
                    {
                        var player = kv.Value;
                        if (player == null) continue;
                        if (player.ClosingClientStep > 0) continue;
                        try
                        {
                            player.Tick();
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }

        /// <summary>
        /// Thực hiện công việc cập nhật lưới
        /// </summary>
        private void DoUpdateGridBackgroundWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // ID luồng
                var workerID = (int)e.Argument;

                this.ProcessWaitingQueue();

                if (workerID >= 0 && workerID < this.updateGridBuckets.Length)
                {
                    var bucket = this.updateGridBuckets[workerID];
                    foreach (var kv in bucket)
                    {
                        var player = kv.Value;
                        if (player == null) continue;
                        if (player.ClosingClientStep > 0) continue;
                        try
                        {
                            ClientManager.DoSpriteMapGridMove(player);
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                        }
 
                        //Thread.Sleep(2);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }

        #endregion

        private void ProcessWaitingQueue()
        {
            while (!this.waitingQueue.IsEmpty)
            {
                if (this.waitingQueue.TryDequeue(out var item))
                {
                    var player = item.Player;
                    if (player == null) continue;
                    var sbIdx = player.StaticID % ServerConfig.Instance.MaxPlayerStoryBoardThread;
                    var ugIdx = player.StaticID % ServerConfig.Instance.MaxUpdateGridThread;
                    if (item.Type == 1)
                    {
                        this.players[player.RoleID] = player;
                        if (sbIdx >= 0 && sbIdx < this.storyboardBuckets.Length)
                        {
                            this.storyboardBuckets[sbIdx][player.RoleID] = player;
                        }
                        if (ugIdx >= 0 && ugIdx < this.updateGridBuckets.Length)
                        {
                            this.updateGridBuckets[ugIdx][player.RoleID] = player;
                        }
                    }
                    else if (item.Type == 0)
                    {
                        this.players.TryRemove(player.RoleID, out _);
                        if (sbIdx >= 0 && sbIdx < this.storyboardBuckets.Length)
                        {
                            this.storyboardBuckets[sbIdx].TryRemove(player.RoleID, out _);
                        }
                        if (ugIdx >= 0 && ugIdx < this.updateGridBuckets.Length)
                        {
                            this.updateGridBuckets[ugIdx].TryRemove(player.RoleID, out _);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Queue chứa danh sách người chơi
        /// </summary>
        private readonly ConcurrentQueue<QueueItem> waitingQueue = new ConcurrentQueue<QueueItem>();

        /// <summary>
        /// Danh sách người chơi
        /// </summary>
        private readonly ConcurrentDictionary<int, KPlayer> players = new ConcurrentDictionary<int, KPlayer>();
        private System.Collections.Concurrent.ConcurrentDictionary<int, KPlayer>[] storyboardBuckets;
        private System.Collections.Concurrent.ConcurrentDictionary<int, KPlayer>[] updateGridBuckets;

        /// <summary>
        /// Thêm người chơi vào danh sách
        /// </summary>
        /// <param name="player"></param>
        public void AddPlayer(KPlayer player)
        {
            this.waitingQueue.Enqueue(new QueueItem()
            {
                Type = 1,
                Player = player,
            });
        }

        /// <summary>
        /// Xóa người chơi khỏi danh sách
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(KPlayer player)
        {
            this.waitingQueue.Enqueue(new QueueItem()
            {
                Type = 0,
                Player = player,
            });
        }
    }
}
