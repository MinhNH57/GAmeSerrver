using System;
using System.Data;
using Maticsoft.DBUtility;
using MySQLDriverCS;
using MySql.Data.MySqlClient;
using System.IO;
using Server.Tools;
using Tmsk.Tools;
using Tmsk.Contract;
using KF.Contract.Data;
using KF.Contract.Interface;
using KF.Contract;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KF.Remoting
{
    /// <summary>
    /// jackpot KNB
    /// </summary>
    public partial class JackpotService : MarshalByRefObject, IJackpotService
    {
        private readonly GameTypes JackpotGameType = GameTypes.Jackpot;
        private static readonly object DbMutex = new object();
        private static readonly Random Rng = new Random();

        // In-memory cache for the jar to avoid frequent DB reads.
        // Protected by DbMutex.
        private static long _jarCache = 0L;
        private static bool _jarCached = false;
        private static DateTime _lastRefreshTime = DateTime.MinValue;

        private static ConcurrentDictionary<int, JackpotResult> _jarResults = new ConcurrentDictionary<int, JackpotResult>();
        private static int _nextResultId = -1;
        private static int NextResultId() { return Interlocked.Increment(ref _nextResultId); }
        private class JackpotRequest
        {
            public int ServerId;
            public int PlayerId;
            public string playerName;
            public long BetKnb;
            public string ItemName;
            public bool IsBigWin;
            public bool IsSmallWin;
            public bool notify;
            public TaskCompletionSource<JackpotResult> Tcs;
        }
        private static readonly ConcurrentQueue<JackpotRequest> _queue = new ConcurrentQueue<JackpotRequest>();
        private static readonly AutoResetEvent _queueEvent = new AutoResetEvent(false);
        private static Thread _worker;
        private static bool _workerInit = false;
        private static int _queueCount = 0;
        private static int _queueMax = 1000;
        private const int RequestTimeoutMs = 10000;
        private static readonly JackpotService _instance = new JackpotService();
        private static readonly ConcurrentQueue<JackpotResult> _history = new ConcurrentQueue<JackpotResult>();
        private const int HistoryMax = 20;

        private static List<TopRoleInfo> _rankingCache = new List<TopRoleInfo>();
        private static System.Threading.Timer _rankingTimer;
        private const string LuckySpinPointParamName = "LuckySpinPoint";
        private const long DefaultJackpotJarAmount = 3000;

        static JackpotService()
        {
            if (!_workerInit)
            {
                _worker = new Thread(WorkerLoop);
                _worker.IsBackground = true;
                _worker.Start();
                _workerInit = true;

                // Start ranking timer: delay 10s, repeat every 10 mins
                _rankingTimer = new System.Threading.Timer(RankingTimerCallback, null, 10000, 10 * 60 * 1000);
            }
        }

        private static void RankingTimerCallback(object state)
        {
            try
            {
                if (!CheckDatabaseConnectionAndNotify())
                {
                    return;
                }
                LogManager.WriteLog(LogTypes.JackPot, "RankingTimerCallback: Bắt đầu truy vấn xếp hạng...");
                _rankingCache = _instance.GetTopRolesByPvalue(LuckySpinPointParamName);
                if (_rankingCache != null)
                {
                    LogManager.WriteLog(LogTypes.JackPot, string.Format("RankingTimerCallback: Nhận được {0} nhân vật. Đang gửi thông báo tới GameServer...", _rankingCache.Count));
                    var evItem = new AsyncDataItem(KuaFuEventTypes.TopRoleRankingUpdated, _rankingCache);
                    _instance.Broadcast2GsAgent(evItem);
                }
                else
                {
                    LogManager.WriteLog(LogTypes.JackPot, "RankingTimerCallback: Kết quả truy vấn rỗng.");
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "RankingTimerCallback thất bại: " + ex.ToString());
            }
        }

        private static bool CheckDatabaseConnectionAndNotify()
        {
            try
            {
                using (var connection = new MySqlConnection(DbHelperMySQL.connectionString))
                {
                    connection.Open();
                    using (var cmdNames = new MySqlCommand("SET NAMES 'utf8'", connection))
                    {
                        cmdNames.ExecuteNonQuery();
                    }
                    using (var cmd = new MySqlCommand("SELECT 1", connection))
                    {
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Kiểm tra DB chính (MySql.Data) thất bại: " + ex.Message + ". Đang thử phương án dự phòng...");

                try
                {
                    // Fallback to DbHelperMySQL2 (DriverCS)
                    DbHelperMySQL2.ExecuteReader("SELECT 1");
                    return true;
                }
                catch (Exception ex2)
                {
                    LogManager.WriteLog(LogTypes.JackPot, "Kiểm tra DB dự phòng (DriverCS) thất bại: " + ex2.ToString());
                    var evItem = new AsyncDataItem(KuaFuEventTypes.DatabaseConnectionError, null);
                    _instance.Broadcast2GsAgent(evItem);
                    return false;
                }
            }
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                _queueEvent.WaitOne();
                while (_queue.TryDequeue(out var req))
                {
                    Interlocked.Decrement(ref _queueCount);
                    try
                    {
                        LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.WorkerLoop: Đã lấy yêu cầu serverId={0}, playerId={1}, betKnb={2}, queueCount={3}", req.ServerId, req.PlayerId, req.BetKnb, _queueCount));
                        var res = _instance.ProcessJackpot(req.ServerId, req.PlayerId, req.playerName, req.BetKnb, req.ItemName, req.IsBigWin, req.IsSmallWin, req.notify);
                        req.Tcs.TrySetResult(res);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.JackPot, "JackpotService.WorkerLoop ngoại lệ: " + ex.ToString());
                        req.Tcs.TrySetResult(new JackpotResult
                        {
                            ID = NextResultId(),
                            PlayerID = req.PlayerId,
                            PlayerName = req.playerName,
                            IsBigWin = false,
                            IsSmallWin = false,
                            AwardKnb = 0,
                            JarBefore = 0,
                            JarAfter = 0,
                            Message = "Lỗi nội bộ"
                        });
                    }
                }
            }
        }
        private JackpotResult ProcessJackpot(int serverId, int playerId, string playerName, long betKnb, string itemName, bool isBigWin, bool isSmallWin, bool notify)
        {
            var newID = NextResultId();
            if (betKnb <= 0)
            {
                return new JackpotResult { ID = newID, PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = 0, JarAfter = 0, Time = DateTime.Now, Message = "Cược không hợp lệ", notify = notify };
            }
            try
            {
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.ProcessJackpot: Bắt đầu serverId={0}, playerId={1}, betKnb={2}, isBigWin={3}, isSmallWin={4}", serverId, playerId, betKnb, isBigWin, isSmallWin));
                long jarBefore = InstanceGetJarAmount();
                if (jarBefore < 0)
                {
                    LogManager.WriteLog(LogTypes.JackPot, "TryHitKnbJackpot: không thể đọc số lượng trong hũ");
                    return new JackpotResult { ID = newID, PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = 0, JarAfter = 0, Time = DateTime.Now, Message = "Lỗi DB", notify = notify };
                }
                lock (DbMutex)
                {
                    long jarBeforeLocked = _jarCache;
                    long jarAfter = jarBeforeLocked + +(long)Math.Floor(betKnb * 0.1);
                    bool isWin = isBigWin || isSmallWin;
                    long award = 0;
                    if (isBigWin)
                    {
                        double pct = 0.30 + Rng.NextDouble() * 0.20;
                        award = (long)Math.Floor(pct * jarAfter);
                        if (award < 0) award = 0;
                        if (!SaveJarAmount(DefaultJackpotJarAmount))
                        {
                            LogManager.WriteLog(LogTypes.JackPot, "TryHitKnbJackpot: không thể đặt lại hũ sau khi trúng lớn");
                            return new JackpotResult { ID = newID, PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = jarBeforeLocked, JarAfter = _jarCache, Time = DateTime.Now, Message = "Lỗi DB", notify = notify };
                        }
                        jarAfter = DefaultJackpotJarAmount;
                    }
                    else if (isSmallWin)
                    {
                        double pct = 0.05 + Rng.NextDouble() * 0.05;
                        award = (long)Math.Floor(pct * jarAfter);
                        if (award < 0) award = 0;
                        long newJar = jarAfter - award;
                        if (newJar < 0) newJar = 0;
                        if (!SaveJarAmount(newJar))
                        {
                            LogManager.WriteLog(LogTypes.JackPot, string.Format("TryHitKnbJackpot: không thể lưu số lượng hũ {0}", newJar));
                            return new JackpotResult { ID = newID, PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = jarBeforeLocked, JarAfter = jarBeforeLocked, Time = DateTime.Now, Message = "Lỗi DB", notify = notify };
                        }
                        jarAfter = newJar;
                    }
                    else
                    {
                        if (!SaveJarAmount(jarAfter))
                        {
                            LogManager.WriteLog(LogTypes.JackPot, string.Format("TryHitKnbJackpot: không thể lưu số lượng hũ {0}", jarAfter));
                            return new JackpotResult { ID = newID, PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = jarBeforeLocked, JarAfter = jarBeforeLocked, Time = DateTime.Now, Message = "Lỗi DB", notify = notify };
                        }
                    }
                    jarBefore = jarBeforeLocked;
                    try
                    {
                        LogManager.WriteLog(LogTypes.JackPot, string.Format("Ghi log KNB: serverid={0}, playerid={1}, bet={2}, win={3}, award={4}, jar_before={5}, jar_after={6}",
                            serverId, playerId, betKnb, isWin ? 1 : 0, award, jarBefore, jarAfter));
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.JackPot, "Ghi log KNB thất bại: " + ex.ToString());
                    }
                    //if (isWin)
                    {
                        try
                        {
                            var evItem = new AsyncDataItem(KuaFuEventTypes.JackpotWin, newID, serverId, playerId, playerName, award, isBigWin ? 1 : isSmallWin ? 2 : 0, jarBefore, jarAfter, itemName, notify ? 1 : 0);
                            Broadcast2GsAgent(evItem);
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteLog(LogTypes.JackPot, "Gửi thông báo trúng thưởng thất bại: " + ex.ToString());
                        }
                    }
                    var res = new JackpotResult
                    {
                        ID = newID,
                        PlayerID = playerId,
                        PlayerName = playerName,
                        IsBigWin = isBigWin,
                        IsSmallWin = isSmallWin,
                        AwardKnb = award,
                        JarBefore = jarBefore,
                        JarAfter = jarAfter,
                        Time = DateTime.Now,
                        Message = itemName,
                        notify = notify,
                    };
                    _jarResults[playerId] = res;
                    if (isBigWin || isSmallWin || !string.IsNullOrEmpty(itemName))
                    {
                        _history.Enqueue(res);
                        while (_history.Count > HistoryMax) _history.TryDequeue(out _);
                    }
                    LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.ProcessJackpot: Kết thúc playerId={0}, isBigWin={1}, isSmallWin={2}, award={3}, jarBefore={4}, jarAfter={5}", playerId, isBigWin, isSmallWin, award, jarBefore, jarAfter));
                    return res;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "JackpotService.ProcessJackpot ngoại lệ: " + ex.ToString());
                return new JackpotResult { ID = newID, PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = 0, JarAfter = 0, Time = DateTime.Now, Message = "Lỗi nội bộ", notify = notify };
            }
        }
        private static long InstanceGetJarAmount()
        {
            try
            {
                if (!_jarCached || (DateTime.Now - _lastRefreshTime).TotalMinutes >= 10)
                {
                    if (!DbRefreshJar()) return _jarCache > DefaultJackpotJarAmount ? _jarCache : DefaultJackpotJarAmount;
                }
                return _jarCache;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Lấy số lượng hũ thất bại: " + ex.ToString());
                return -1;
            }
        }
        private static bool DbRefreshJar()
        {
            try
            {
                lock (DbMutex)
                {
                    object jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                    if (jarObj == null)
                    {
                        EnsureJarStorageInitialized();
                        jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                    }
                    _jarCache = jarObj != null ? Convert.ToInt64(jarObj) : DefaultJackpotJarAmount;
                    _jarCached = true;
                    _lastRefreshTime = DateTime.Now;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Làm mới hũ từ DB thất bại: " + ex.ToString());
                if (EnsureJarStorageInitialized())
                {
                    lock (DbMutex)
                    {
                        if (_jarCache < DefaultJackpotJarAmount)
                        {
                            _jarCache = DefaultJackpotJarAmount;
                        }
                        _jarCached = true;
                    }
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Try to hit the KNB jackpot.
        /// Refresh cache from DB (sync). Returns true on success.
        /// </summary>
        public bool RefreshJarFromDb()
        {
            try
            {
                lock (DbMutex)
                {
                    object jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                    _jarCache = jarObj != null ? Convert.ToInt64(jarObj) : 0L;
                    _jarCached = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Làm mới hũ từ DB thất bại: " + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Get current jar amount from cache if present, otherwise read DB once and cache it. Returns -1 on error.
        /// </summary>
        /// <returns>jar amount (long), -1 if error</returns>
        public long GetJarAmount()
        {
            try
            {
                if (!_jarCached)
                {
                    if (!RefreshJarFromDb()) return -1;
                }

                return _jarCache;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Lấy số lượng hũ thất bại: " + ex.ToString());
                return -1;
            }
        }

        /// <summary>
        /// Save the jar amount to DB (upsert) and update cache. Returns true on success.
        /// </summary>
        public bool SaveJarAmount(long amount)
        {
            try
            {
                if (amount < 0) amount = 0;
                lock (DbMutex)
                {
                    LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.SaveJarAmount: Cập nhật số lượng hũ thành {0}", amount));
                    string upsertSql = string.Format("INSERT INTO `t_knb_jar`(`id`,`value`) VALUES(1,{0}) ON DUPLICATE KEY UPDATE `value`={0}", amount);
                    DbHelperMySQL.ExecuteSql(upsertSql);
                    _jarCache = amount;
                    _jarCached = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Lưu hũ thất bại: " + ex.ToString());
                if (EnsureJarStorageInitialized())
                {
                    try
                    {
                        string upsertSql = string.Format("INSERT INTO `t_knb_jar`(`id`,`value`) VALUES(1,{0}) ON DUPLICATE KEY UPDATE `value`={0}", amount < 0 ? 0 : amount);
                        DbHelperMySQL.ExecuteSql(upsertSql);
                        lock (DbMutex)
                        {
                            _jarCache = amount < 0 ? 0 : amount;
                            _jarCached = true;
                        }
                        return true;
                    }
                    catch (Exception ex2)
                    {
                        LogManager.WriteLog(LogTypes.JackPot, "Thử lại lưu hũ thất bại: " + ex2.ToString());
                    }
                }

                // Broadcast DB Error
                try
                {
                    LogManager.WriteLog(LogTypes.JackPot, "SaveJarAmount: Gửi thông báo lỗi kết nối DB do thất bại.");
                    var evItem = new AsyncDataItem(KuaFuEventTypes.DatabaseConnectionError, null);
                    Broadcast2GsAgent(evItem);
                }
                catch (Exception) { }

                return false;
            }
        }

        private long AdjustJarAmountInternal(int serverId, long delta)
        {
            try
            {
                lock (DbMutex)
                {
                    if (!_jarCached)
                    {
                        object jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                        if (jarObj == null)
                        {
                            EnsureJarStorageInitialized();
                            jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                        }
                        _jarCache = jarObj != null ? Convert.ToInt64(jarObj) : 0L;
                        _jarCached = true;
                        _lastRefreshTime = DateTime.Now;
                    }

                    long jarBefore = _jarCache;
                    long jarAfter;

                    if (delta >= 0)
                    {
                        jarAfter = jarBefore > long.MaxValue - delta ? long.MaxValue : jarBefore + delta;
                    }
                    else
                    {
                        long sub = -delta;
                        jarAfter = jarBefore < sub ? 0 : jarBefore - sub;
                    }

                    LogManager.WriteLog(LogTypes.JackPot, string.Format("AdjustJarAmountInternal: serverId={0}, jarBefore={1}, delta={2}, jarAfter={3}", serverId, jarBefore, delta, jarAfter));
                    try
                    {
                        string upsertSql = string.Format("INSERT INTO `t_knb_jar`(`id`,`value`) VALUES(1,{0}) ON DUPLICATE KEY UPDATE `value`={0}", jarAfter);
                        DbHelperMySQL.ExecuteSql(upsertSql);
                        _jarCache = jarAfter;
                        _jarCached = true;
                        _lastRefreshTime = DateTime.Now;
                        try
                        {
                            var evItem = new AsyncDataItem(KuaFuEventTypes.JackpotJarAdjusted, serverId, jarBefore, jarAfter, delta);
                            Broadcast2GsAgent(evItem);
                        }
                        catch (Exception) { }
                        return _jarCache;
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.JackPot, "AdjustJarAmountInternal: Lưu hũ thất bại: " + ex.ToString());
                        if (EnsureJarStorageInitialized())
                        {
                            try
                            {
                                string upsertSql = string.Format("INSERT INTO `t_knb_jar`(`id`,`value`) VALUES(1,{0}) ON DUPLICATE KEY UPDATE `value`={0}", jarAfter);
                                DbHelperMySQL.ExecuteSql(upsertSql);
                                _jarCache = jarAfter;
                                _jarCached = true;
                                _lastRefreshTime = DateTime.Now;
                                try
                                {
                                    var evItem = new AsyncDataItem(KuaFuEventTypes.JackpotJarAdjusted, serverId, jarBefore, jarAfter, delta);
                                    Broadcast2GsAgent(evItem);
                                }
                                catch (Exception) { }
                                return _jarCache;
                            }
                            catch (Exception ex2)
                            {
                                LogManager.WriteLog(LogTypes.JackPot, "AdjustJarAmountInternal: Thử lại lưu hũ thất bại: " + ex2.ToString());
                            }
                        }

                        try
                        {
                            var evItem = new AsyncDataItem(KuaFuEventTypes.DatabaseConnectionError, null);
                            Broadcast2GsAgent(evItem);
                        }
                        catch (Exception) { }

                        return -1;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "AdjustJarAmountInternal ngoại lệ: " + ex.ToString());
                return -1;
            }
        }

        public long AdjustJarAmount(int serverId, long delta)
        {
            if (delta == 0) return GetJarAmount();
            return AdjustJarAmountInternal(serverId, delta);
        }

        public long ResetJarAmount(int serverId)
        {
            try
            {
                long amount = DefaultJackpotJarAmount;
                lock (DbMutex)
                {
                    if (!_jarCached)
                    {
                        object jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                        if (jarObj == null)
                        {
                            EnsureJarStorageInitialized();
                            jarObj = DbHelperMySQL.GetSingle("SELECT `value` FROM `t_knb_jar` WHERE `id` = 1");
                        }
                        _jarCache = jarObj != null ? Convert.ToInt64(jarObj) : 0L;
                        _jarCached = true;
                    }

                    long jarBefore = _jarCache;
                    string upsertSql = string.Format("INSERT INTO `t_knb_jar`(`id`,`value`) VALUES(1,{0}) ON DUPLICATE KEY UPDATE `value`={0}", amount);
                    DbHelperMySQL.ExecuteSql(upsertSql);
                    _jarCache = amount;
                    _jarCached = true;
                    _lastRefreshTime = DateTime.Now;

                    try
                    {
                        var evItem = new AsyncDataItem(KuaFuEventTypes.JackpotJarAdjusted, serverId, jarBefore, amount, amount - jarBefore);
                        Broadcast2GsAgent(evItem);
                    }
                    catch (Exception) { }

                    LogManager.WriteLog(LogTypes.JackPot, string.Format("ResetJarAmount: serverId={0}, jarBefore={1}, jarAfter={2}", serverId, jarBefore, amount));
                    return _jarCache;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "ResetJarAmount failed: " + ex.ToString());
                try
                {
                    var evItem = new AsyncDataItem(KuaFuEventTypes.DatabaseConnectionError, null);
                    Broadcast2GsAgent(evItem);
                }
                catch (Exception) { }
                return -1;
            }
        }

        /// <summary>
        /// Try to hit the KNB jackpot.
        /// - serverId: id of the GameServer (used for logging)
        /// - playerId: player identifier
        /// - betKnb: the KNB amount player put into the jar this try
        /// </summary>
        public JackpotResult TryHitKnbJackpot(int serverId, int playerId, string playerName, long betKnb, string itemName, bool isBigWin, bool isSmallWin, bool notify)
        {
            if (betKnb <= 0)
            {
                return new JackpotResult { ID = NextResultId(), PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, Message = "Cược không hợp lệ" };
            }
            var tcs = new TaskCompletionSource<JackpotResult>();
            var req = new JackpotRequest
            {
                ServerId = serverId,
                playerName = playerName,
                PlayerId = playerId,
                BetKnb = betKnb,
                ItemName = itemName,
                IsBigWin = isBigWin,
                IsSmallWin = isSmallWin,
                notify = notify,
                Tcs = tcs
            };
            int current = Interlocked.Increment(ref _queueCount);
            LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.TryHitKnbJackpot: Thêm vào hàng đợi serverId={0}, playerId={1}, betKnb={2}, queueCount={3}", serverId, playerId, betKnb, current));
            if (current > _queueMax)
            {
                Interlocked.Decrement(ref _queueCount);
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.TryHitKnbJackpot: Hàng đợi đầy cho playerId={0}, trả về Busy", playerId));
                return new JackpotResult { ID = NextResultId(), PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = 0, JarAfter = 0, Time = DateTime.Now, Message = "Busy", notify = notify };
            }
            _queue.Enqueue(req);
            _queueEvent.Set();
            if (tcs.Task.Wait(RequestTimeoutMs))
            {
                var res = tcs.Task.Result;
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.TryHitKnbJackpot: Hoàn tất cho playerId={0}, isBigWin={1}, isSmallWin={2}, award={3}", playerId, res != null ? res.IsBigWin.ToString() : "null", res != null ? res.IsSmallWin.ToString() : "null", res != null ? res.AwardKnb.ToString() : "null"));
                return res;
            }
            else
            {
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotService.TryHitKnbJackpot: Quá thời gian cho playerId={0}", playerId));
                return new JackpotResult { ID = NextResultId(), PlayerID = playerId, PlayerName = playerName, IsBigWin = false, IsSmallWin = false, AwardKnb = 0, JarBefore = 0, JarAfter = 0, Time = DateTime.Now, Message = "Timeout", notify = notify };
            }
        }
        public Dictionary<int, JackpotResult> GetJackpotResults()
        {
            return _jarResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        public List<JackpotResult> GetJackpotHistory()
        {
            return _history.ToList();
        }

        public int InitializeClient(IKuaFuClient callback, KuaFuClientContext clientInfo)
        {
            try
            {
                if (clientInfo.ServerId != 0)
                {
                    bool bFirstInit = false;
                    int ret = ClientAgentManager.Instance().InitializeClient(callback, clientInfo, out bFirstInit);
                    if (ret > 0)
                    {
                        if (clientInfo.MapClientCountDict != null && clientInfo.MapClientCountDict.Count > 0)
                        {
                            KuaFuServerManager.UpdateKuaFuLineData(clientInfo.ServerId, clientInfo.MapClientCountDict);
                            ClientAgentManager.Instance().SetMainlinePayload(clientInfo.ServerId, clientInfo.MapClientCountDict.Values.ToList().Sum());
                        }
                    }
                    return ret;
                }
                else
                {
                    LogManager.WriteLog(LogTypes.JackPot, string.Format("InitializeClient: Sai GameType, từ chối kết nối. ServerId:{0}, GameType:{1}", clientInfo.ServerId, clientInfo.GameType));
                    return StdErrorCode.Error_Invalid_GameType;
                }
            }
            catch (System.Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, string.Format("InitializeClient: Trùng ServerID, từ chối kết nối. ServerId:{0}, ClientId:{1}, info:{2}, Error:{3}", clientInfo.ServerId, clientInfo.ClientId, clientInfo.Token, ex.ToString()));
                return StdErrorCode.Error_Server_Internal_Error;
            }
        }

        public AsyncDataItem[] GetClientCacheItems(int serverId)
        {
            return ClientAgentManager.Instance().PickAsyncEvent(serverId, JackpotGameType);
        }

        private void Broadcast2GsAgent(AsyncDataItem item)
        {
            ClientAgentManager.Instance().BroadCastAsyncEvent(JackpotGameType, item);
        }

        private static bool EnsureJarStorageInitialized()
        {
            try
            {
                DbHelperMySQL.ExecuteSql("CREATE TABLE IF NOT EXISTS `t_knb_jar`(`id` INT NOT NULL PRIMARY KEY, `value` BIGINT NOT NULL)");
                DbHelperMySQL.ExecuteSql(string.Format("INSERT IGNORE INTO `t_knb_jar`(`id`,`value`) VALUES(1,{0})", DefaultJackpotJarAmount));
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "Khởi tạo lưu trữ hũ thất bại: " + ex.ToString());
                return false;
            }
        }

        public List<TopRoleInfo> GetTopRolesByPvalue(string pname)
        {
            var result = new List<TopRoleInfo>();
            try
            {
                LogManager.WriteLog(LogTypes.JackPot, string.Format("GetTopRolesByPvalue: Bắt đầu pname={0}, ConnStrEmpty={1}", pname, string.IsNullOrEmpty(DbHelperMySQL.connectionString)));
                // Use IFNULL to handle potential NULLs from LEFT JOIN or missing values
                string sql = string.Format("SELECT t1.rid, IFNULL(t2.rname,'') AS rname, IFNULL(t2.createZoneId,0) AS original_zoneid, IFNULL(t1.pvalue,'0') AS pvalue, IFNULL(t2.zoneid,0) AS zoneid " +
                                           "FROM t_roleparams_2 t1 " +
                                           "LEFT JOIN t_roles t2 ON t1.rid = t2.rid " +
                                           "WHERE t1.pname='{0}' " +
                                           "ORDER BY CAST(IFNULL(t1.pvalue,'0') AS UNSIGNED) DESC LIMIT 20", pname);

                try
                {
                    LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue: Bắt đầu ExecuteReader (MySql.Data)");
                    using (MySqlDataReader reader = DbHelperMySQL.ExecuteReader(sql))
                    {
                        LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue: Đã mở ExecuteReader (MySql.Data)");
                        int rank = 1;
                        int rows = 0;
                        while (reader != null && reader.Read())
                        {
                            rows++;
                            try
                            {
                                int rid;
                                int zoneId;
                                int originalZoneId;

                                // Read columns explicitly to debug if needed
                                object oRid = reader["rid"];
                                if (!TryGetInt(oRid, out rid))
                                {
                                    continue;
                                }

                                object oOriginal = reader["original_zoneid"];
                                bool gotOriginal = TryGetInt(oOriginal, out originalZoneId);

                                object oZone = reader["zoneid"];
                                bool gotZone = TryGetInt(oZone, out zoneId);

                                zoneId = gotZone ? zoneId : (gotOriginal ? originalZoneId : 0);

                                object oRname = reader["rname"];
                                string rname = (oRname == null || Convert.IsDBNull(oRname)) ? "" : oRname.ToString();

                                var info = new TopRoleInfo
                                {
                                    Rid = rid,
                                    Rname = rname,
                                    ZoneId = zoneId,
                                    Rank = rank++
                                };
                                object oPvalue = reader["pvalue"];
                                long pval;
                                info.Pvalue = TryGetLong(oPvalue, out pval) ? pval : 0L;

                                result.Add(info);
                            }
                            catch (Exception innerEx)
                            {
                                LogManager.WriteLog(LogTypes.JackPot, string.Format("GetTopRolesByPvalue lỗi đọc dòng {0}: {1}", rows, innerEx.ToString()));
                            }
                        }
                        LogManager.WriteLog(LogTypes.JackPot, string.Format("GetTopRolesByPvalue: Số dòng={0}", rows));
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue lỗi chính: " + ex.ToString());
                    LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue chính thất bại: " + ex.ToString());
                    LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue: Chuyển sang DriverCS");
                    var fallback = ReadTopRolesWithDriverCS(sql);
                    if (fallback != null && fallback.Count > 0)
                    {
                        result = fallback;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue thất bại: " + ex.ToString());
            }
            return result;
        }

        public int ClearLuckySpinServerPointData(int serverId)
        {
            if (serverId <= 0)
            {
                return 0;
            }

            try
            {
                string deleteSql = string.Format(
                    "DELETE t1 FROM t_roleparams_2 t1 " +
                    "LEFT JOIN t_roles t2 ON t1.rid = t2.rid " +
                    "WHERE t1.pname='{0}' " +
                    "AND (IFNULL(t2.createZoneId,0)={1} OR IFNULL(t2.zoneid,0)={1})",
                    LuckySpinPointParamName, serverId);
                int rows = DbHelperMySQL.ExecuteSql(deleteSql);
                LogManager.WriteLog(LogTypes.JackPot, $"ClearLuckySpinServerPointData server {serverId} rows: {rows}");
                return rows;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, $"ClearLuckySpinServerPointData server {serverId} failed : " + ex.ToString());
                return -1;
            }
        }

        private static bool TryGetInt(object value, out int result)
        {
            result = 0;
            if (value == null || value is DBNull)
            {
                return false;
            }

            if (value is int)
            {
                result = (int)value;
                return true;
            }

            return int.TryParse(value.ToString(), out result);
        }

        private static bool TryGetLong(object value, out long result)
        {
            result = 0L;
            if (value == null || value is DBNull)
            {
                return false;
            }

            if (value is long)
            {
                result = (long)value;
                return true;
            }

            return long.TryParse(value.ToString(), out result);
        }

        private static List<TopRoleInfo> ReadTopRolesWithDriverCS(string sql)
        {
            var result = new List<TopRoleInfo>();
            MySQLDataReader reader = null;
            try
            {
                EnsureNativeMySQLDllPresent();
                LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue: Bắt đầu ExecuteReader (DriverCS)");
                reader = DbHelperMySQL2.ExecuteReader(sql, false);
                LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue: Đã mở ExecuteReader (DriverCS)");
                int rank = 1;
                int rows = 0;
                while (reader != null && reader.Read())
                {
                    rows++;
                    int rid;
                    int zoneId;
                    int originalZoneId;
                    if (!TryGetInt(reader["rid"], out rid))
                    {
                        continue;
                    }
                    bool gotOriginal = TryGetInt(reader["original_zoneid"], out originalZoneId);
                    bool gotZone = TryGetInt(reader["zoneid"], out zoneId);
                    zoneId = gotZone ? zoneId : (gotOriginal ? originalZoneId : 0);

                    var info = new TopRoleInfo
                    {
                        Rid = rid,
                        Rname = reader["rname"] == null || reader["rname"] is DBNull ? "" : reader["rname"].ToString(),
                        ZoneId = zoneId,
                        Rank = rank++
                    };
                    long pval;
                    info.Pvalue = TryGetLong(reader["pvalue"], out pval) ? pval : 0L;

                    result.Add(info);
                }
                LogManager.WriteLog(LogTypes.JackPot, string.Format("GetTopRolesByPvalue DriverCS: Số dòng={0}", rows));
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue DriverCS ngoại lệ: " + ex.ToString());
                LogManager.WriteLog(LogTypes.JackPot, "GetTopRolesByPvalue DriverCS thất bại: " + ex.ToString());
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return result;
        }

        private static void EnsureNativeMySQLDllPresent()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dllName = "libmySQL.dll";
                string target = Path.Combine(baseDir, dllName);
                LogManager.WriteLog(LogTypes.JackPot, string.Format("EnsureNativeMySQLDllPresent: Thư mục gốc={0}, Tồn tại đích={1}", baseDir, File.Exists(target)));
                if (!File.Exists(target))
                {
                    string source = Path.Combine(@"e:\KV1_ADNX\kv1-game-server\log-tools", dllName);
                    if (File.Exists(source))
                    {
                        File.Copy(source, target, true);
                        LogManager.WriteLog(LogTypes.JackPot, "EnsureNativeMySQLDllPresent: Đã sao chép libmySQL.dll");
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.JackPot, "EnsureNativeMySQLDllPresent: Không tìm thấy nguồn libmySQL.dll");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.JackPot, "EnsureNativeMySQLDllPresent thất bại: " + ex.ToString());
            }
        }

        public List<TopRoleInfo> GetTopRankings()
        {
            return _rankingCache;
        }

    }
}
