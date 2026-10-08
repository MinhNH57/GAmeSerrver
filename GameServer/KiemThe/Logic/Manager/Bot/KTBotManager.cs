using GameServer.KiemThe.Core;
using GameServer.KiemThe.Entities;
using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace GameServer.KiemThe.Logic.Manager
{
    /// <summary>
    /// Đối tượng quản lý khu vực động
    /// </summary>
    public static partial class KTBotManager
    {
        /// <summary>
        /// Đối tượng sử dụng khóa LOCK
        /// </summary>
        private static readonly object Mutex = new object();

        /// <summary>
        /// Danh sách BOT
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, KTBot> bots = new System.Collections.Concurrent.ConcurrentDictionary<int, KTBot>();

        private static int startupSpawnStarted;
        /// Timer tạo bot định kỳ.
        private static System.Threading.Timer botSpawnTimer;

        /// Chỉ cho phép khởi động lịch tạo bot một lần.
        private static int botSpawnScheduleStarted;

        /// Random dùng chung, tránh tạo nhiều Random có seed giống nhau.
        private static readonly Random botSpawnRandom = new Random();

        /// Lần lượt phân phối bot qua các tân thủ thôn.
        private static int nextVillageIndex;
        private static int botSpawnBusy;

        private static readonly Random botRandom = new Random();
        private static readonly object botRandomLock = new object();

        // Run once after maps, items and BOT timers have been initialized.
        public static void SpawnStartupBots()
        {
            if (System.Threading.Interlocked.Exchange(ref startupSpawnStarted, 1) != 0)
                return;
            try
            {
                string path = Global.GameResPath("Config/KT_Setting/StartupBots.xml");
                if (!File.Exists(path))
                {
                    Console.WriteLine("[StartupBots] Disabled: missing configuration: " + path);
                    return;
                }
                XElement config = XElement.Load(path);
                if (config.Name != "StartupBots")
                    throw new InvalidDataException("Expected StartupBots root element.");
                if (!((bool?)config.Attribute("Enabled") ?? false))
                {
                    Console.WriteLine("[StartupBots] Disabled.");
                    return;
                }
                int count = (int?)config.Attribute("Count") ?? 50;
                Console.WriteLine("Số lượng BOT cần tạo: " + count);
                bool mainQuestsEnabled = (bool?)config.Attribute("MainQuestsEnabled") ?? false;
                int firstMainQuestId = (int?)config.Attribute("FirstMainQuestId") ?? 100;
                if (mainQuestsEnabled && firstMainQuestId <= 0)
                    throw new InvalidDataException("FirstMainQuestId must be positive.");
                int radius = (int?)config.Attribute("SpawnRadius") ?? 300;
                if (count < 0 || count > 500 || radius < 0 || radius > 2000)
                    throw new InvalidDataException("Count must be 0..500; SpawnRadius must be 0..2000.");
                var villages = KTGlobal.NewbieVillages.Where(v =>
                    GameManager.MapMgr.DictMaps.ContainsKey(v.ID) &&
                    GameManager.MapGridMgr.GetMapGrid(v.ID) != null).ToList();
                if (villages.Count == 0)
                    throw new InvalidDataException("No configured newbie village has a loaded map/grid.");
                var random = new Random();
                int created = 0;
                for (int i = 0; i < count; i++)
                {
                    var village = villages[i % villages.Count];
                    var map = GameManager.MapMgr.DictMaps[village.ID];
                    bool placed = false;
                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        int x = village.Position.X + random.Next(-radius, radius + 1);
                        int y = village.Position.Y + random.Next(-radius, radius + 1);
                        if (x < 0 || y < 0 || Global.InObs(ObjectTypes.OT_MONSTER, village.ID, x, y, 1))
                            continue;
                        AddBotByGm(village.ID, x, y, 0, new System.Windows.Point(
                            x / map.MapGridWidth, y / map.MapGridHeight), mainQuestsEnabled, firstMainQuestId);
                        created++;
                        placed = true;
                        break;
                    }
                    if (!placed)
                        Console.WriteLine("[StartupBots] No walkable spawn found on map " + village.ID);
                }
                Console.WriteLine("[StartupBots] Created {0}/{1} BOTs across {2} newbie village(s).", created, count, villages.Count);
            }
            catch (Exception ex)
            {
                global::Server.Tools.LogManager.WriteException("[StartupBots] " + ex);
                Console.WriteLine("[StartupBots] Startup population stopped: " + ex.Message);
            }
        }
        /// <summary>
        /// Tìm BOT có ID tương ứng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static KTBot FindBot(int id)
        {
            lock (KTBotManager.bots)
            {
                if (KTBotManager.bots.TryGetValue(id, out KTBot bot))
                {
                    return bot;
                }
                return null;
            }
        }

        // Lịch trình tạo bot định kỳ, chạy trong luồng riêng.
        public static void StartTimedBotSpawning()
        {
            // OnEnterGame có thể được gọi nhiều lần.
            // Chỉ người đăng nhập đầu tiên mới khởi động timer.
            if (System.Threading.Interlocked.Exchange(
                    ref botSpawnScheduleStarted, 1) != 0)
            {
                return;
            }

            try
            {
                string path = Global.GameResPath(
                    "Config/KT_Setting/StartupBots.xml"
                );

                if (!File.Exists(path))
                {
                    Console.WriteLine(
                        "[TimedBots] Không tìm thấy config: " + path
                    );
                    return;
                }

                XElement config = XElement.Load(path);

                bool enabled =
                    (bool?)config.Attribute("Enabled") ?? false;

                if (!enabled)
                {
                    Console.WriteLine("[TimedBots] Disabled.");
                    return;
                }

                int initialCount =
                    (int?)config.Attribute("InitialCount") ?? 0;

                int maxCount =
                    (int?)config.Attribute("MaxCount") ?? 50;

                int intervalMinutes =
                    (int?)config.Attribute("SpawnIntervalMinutes") ?? 30;

                int radius =
                    (int?)config.Attribute("SpawnRadius") ?? 300;

                bool mainQuestsEnabled =
                    (bool?)config.Attribute("MainQuestsEnabled") ?? false;

                int firstMainQuestId =
                    (int?)config.Attribute("FirstMainQuestId") ?? 100;

                if (initialCount < 0 || initialCount > maxCount)
                {
                    throw new InvalidDataException(
                        "InitialCount không hợp lệ."
                    );
                }

                if (maxCount <= 0 || maxCount > 10000)
                {
                    throw new InvalidDataException(
                        "MaxCount phải nằm trong khoảng 1..10000."
                    );
                }

                if (intervalMinutes <= 0)
                {
                    throw new InvalidDataException(
                        "SpawnIntervalMinutes phải lớn hơn 0."
                    );
                }

                // Tạo số bot ban đầu nếu được cấu hình.
                for (int i = 0; i < initialCount; i++)
                {
                    if (!TrySpawnOneBot(
                            radius,
                            maxCount,
                            mainQuestsEnabled,
                            firstMainQuestId))
                    {
                        break;
                    }
                }

                TimeSpan interval =
                    TimeSpan.FromMinutes(intervalMinutes);

                // dueTime = interval nên bot đầu tiên được tạo sau 30 phút.
                botSpawnTimer = new System.Threading.Timer(
                    state =>
                    {
                        try
                        {
                            TrySpawnOneBot(
                                radius,
                                maxCount,
                                mainQuestsEnabled,
                                firstMainQuestId
                            );
                        }
                        catch (Exception ex)
                        {
                            global::Server.Tools.LogManager.WriteException(
                                "[TimedBots] " + ex
                            );
                        }
                    },
                    null,
                    interval,
                    interval
                );

                Console.WriteLine(
                    "[TimedBots] Đã bắt đầu: {0} phút tạo 1 bot, tối đa {1} bot.",
                    intervalMinutes,
                    maxCount
                );
            }
            catch (Exception ex)
            {
                // Cho phép đăng nhập sau thử khởi động timer lại.
                System.Threading.Interlocked.Exchange(
                    ref botSpawnScheduleStarted, 0
                );

                global::Server.Tools.LogManager.WriteException(
                    "[TimedBots] " + ex
                );
            }
        }

        private static bool TrySpawnOneBot(int radius, int maxCount, bool mainQuestsEnabled, int firstMainQuestId)
        {
            if (GameServer.Program.NeedExitServer ||
                System.Threading.Interlocked.CompareExchange(ref botSpawnBusy, 1, 0) != 0) return false;
            try { return TrySpawnOneBotCore(radius, maxCount, mainQuestsEnabled, firstMainQuestId); }
            finally { System.Threading.Volatile.Write(ref botSpawnBusy, 0); }
        }

        private static bool TrySpawnOneBotCore(
            int radius,
            int maxCount,
            bool mainQuestsEnabled,
            int firstMainQuestId)
        {
            {
                // Count includes quarantined bots until cleanup succeeds.
                if (bots.Count >= maxCount)
                {
                    Console.WriteLine(
                        "[TimedBots] Đã đạt giới hạn {0} bot.",
                        maxCount
                    );

                    return false;
                }
            }

            var villages = KTGlobal.NewbieVillages
                .Where(v =>
                    GameManager.MapMgr.DictMaps.ContainsKey(v.ID) &&
                    GameManager.MapGridMgr.GetMapGrid(v.ID) != null)
                .ToList();

            if (villages.Count == 0)
            {
                Console.WriteLine(
                    "[TimedBots] Không có tân thủ thôn hợp lệ."
                );

                return false;
            }

            // Mỗi lần tạo sẽ chuyển sang thôn tiếp theo.
            var village =
                villages[nextVillageIndex % villages.Count];

            nextVillageIndex++;

            var map = GameManager.MapMgr.DictMaps[village.ID];

            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x;
                int y;

                lock (botSpawnRandom)
                {
                    x = village.Position.X +
                        botSpawnRandom.Next(-radius, radius + 1);

                    y = village.Position.Y +
                        botSpawnRandom.Next(-radius, radius + 1);
                }

                if (x < 0 || y < 0)
                {
                    continue;
                }

                if (Global.InObs(
                    ObjectTypes.OT_MONSTER,
                    village.ID,
                    x,
                    y,
                    1))
                {
                    continue;
                }

                AddBotByGm(
                    village.ID,
                    x,
                    y,
                    0,
                    new System.Windows.Point(
                        x / map.MapGridWidth,
                        y / map.MapGridHeight
                    ),
                    mainQuestsEnabled,
                    firstMainQuestId
                );

                Console.WriteLine(
                    "[TimedBots] Đã tạo 1 bot tại map {0}, tọa độ ({1},{2}).",
                    village.ID,
                    x,
                    y
                );

                return true;
            }

            Console.WriteLine(
                "[TimedBots] Không tìm được vị trí hợp lệ tại map {0}.",
                village.ID
            );

            return false;
        }

        public static void ReviveQuestBot(
            KTBot bot,
            int mapCode,
            System.Windows.Point position)
        {
            if (bot == null)
                throw new ArgumentNullException(nameof(bot));

            bot.CheckQuestCancellation("ReviveQuestBot");

            using (new BotMutationLease(bot))
            {
                // BOT phải còn thuộc hệ thống quản lý.
                if (!bots.TryGetValue(bot.RoleID, out KTBot registeredBot) ||
                    !ReferenceEquals(registeredBot, bot))
                {
                    throw new InvalidOperationException(
                        "BOT không còn trong danh sách quản lý.");
                }

                if (bot.QuestExecutionDisabled)
                    throw new InvalidOperationException("BOT đã bị vô hiệu hóa.");

                // Tránh hồi sinh lặp lại một BOT đã sống.
                if (!bot.IsDead())
                    return;

                if (bot.CurrentCopyMapID != -1)
                {
                    throw new NotSupportedException(
                        "Chưa hỗ trợ hồi sinh BOT trong phụ bản.");
                }

                // Kiểm tra đích trước khi thay đổi BOT.
                if (!GameManager.MapMgr.DictMaps.TryGetValue(
                        mapCode, out GameMap destination))
                {
                    throw new InvalidOperationException(
                        "Map hồi sinh chưa được tải: " + mapCode);
                }

                if (double.IsNaN(position.X) ||
                    double.IsNaN(position.Y) ||
                    double.IsInfinity(position.X) ||
                    double.IsInfinity(position.Y))
                {
                    throw new InvalidOperationException(
                        "Tọa độ hồi sinh không hợp lệ.");
                }

                // Đồng nhất tọa độ server và tọa độ gửi client.
                var revivePosition = new System.Windows.Point(
                    Math.Floor(position.X),
                    Math.Floor(position.Y));

                if (revivePosition.X < 0 ||
                    revivePosition.Y < 0 ||
                    revivePosition.X >= destination.MapWidth ||
                    revivePosition.Y >= destination.MapHeight ||
                    !destination.CanMove(revivePosition))
                {
                    throw new InvalidOperationException(
                        "Điểm hồi sinh nằm ngoài map hoặc bị chặn: " +
                        revivePosition);
                }

                var sourceGrid =
                    GameManager.MapGridMgr.GetMapGrid(bot.CurrentMapCode);

                var destinationGrid =
                    GameManager.MapGridMgr.GetMapGrid(mapCode);

                if (sourceGrid == null || destinationGrid == null)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy grid nguồn hoặc grid hồi sinh.");
                }

                // Dừng các hành động cũ, nhưng không hủy nhiệm vụ/token.
                KTOtherObjectStoryBoard.Instance.Remove(bot);
                bot.ResetAI();

                // Chính sách ở đây: bỏ hiệu ứng cũ sau khi chết.
                bot.Buffs.RemoveAllBuffs();
                bot.Buffs.RemoveAllAruas();
                bot.Buffs.RemoveAllAvoidBuffs();
                bot.RemoveAllAutoSkills();

                if (bot.m_CurrentLifeMax <= 0)
                {
                    throw new InvalidOperationException(
                        "Máu tối đa của BOT không hợp lệ.");
                }

                // Lưu trạng thái để khôi phục nếu chuyển map thất bại.
                int oldMap = bot.CurrentMapCode;
                var oldPosition = bot.CurrentPos;
                var oldGridPosition = bot.CurrentGrid;
                var oldToPosition = bot.ToPos;
                var oldStartPosition = bot.StartPos;
                int oldLife = bot.m_CurrentLife;
                var oldDoing = bot.m_eDoing;

                bool destinationInsertAttempted = false;

                try
                {
                    // Không gọi KTBotManager.Remove(bot):
                    // hàm đó xóa đăng ký BOT và trả lại ID.
                    sourceGrid.RemoveObject(bot);

                    // Thông báo xóa ở vị trí cũ trước khi đổi tọa độ.
                    try
                    {
                        NotifyNearClientsToRemoveSelf(bot);
                    }
                    catch (Exception notifyError)
                    {
                        BotRuntimeLog.Write(
                            $"[BotReviveNotifyFailed] Bot={bot.RoleID}" +
                            $" Phase=RemoveOld Error={notifyError.Message}");
                    }

                    bot.CheckQuestCancellation("ReviveTransfer");

                    bot.SetCurrentMapCode(mapCode);
                    bot.CurrentPos = revivePosition;
                    bot.CurrentGrid = new System.Windows.Point(
                        (int)revivePosition.X / destination.MapGridWidth,
                        (int)revivePosition.Y / destination.MapGridHeight);

                    bot.ToPos = revivePosition;
                    bot.StartPos = revivePosition;

                    // Giữ trạng thái chết trong lúc đưa vào grid mới.
                    destinationInsertAttempted = true;

                    if (!AddToMap(bot))
                    {
                        throw new InvalidOperationException(
                            "Không thể thêm BOT vào map hồi sinh.");
                    }

                    bot.CheckQuestCancellation("ReviveCommit");

                    // IsDead() kiểm tra cả HP lẫn động tác.
                    bot.m_CurrentLife = bot.m_CurrentLifeMax;
                    bot.m_eDoing =
                        GameServer.KiemThe.Entities.KE_NPC_DOING.do_stand;
                }
                catch (Exception reviveError)
                {
                    try
                    {
                        if (destinationInsertAttempted)
                            destinationGrid.RemoveObject(bot);

                        bot.SetCurrentMapCode(oldMap);
                        bot.CurrentPos = oldPosition;
                        bot.CurrentGrid = oldGridPosition;
                        bot.ToPos = oldToPosition;
                        bot.StartPos = oldStartPosition;
                        bot.m_CurrentLife = oldLife;
                        bot.m_eDoing = oldDoing;

                        // Dọn đăng ký grid cũ còn sót trước khi thêm lại.
                        sourceGrid.RemoveObject(bot);

                        if (!AddToMap(bot))
                        {
                            throw new InvalidOperationException(
                                "Không thể khôi phục BOT về grid cũ.");
                        }
                    }
                    catch (Exception rollbackError)
                    {
                        bot.DisableQuestExecution(
                            "Hồi sinh và khôi phục vị trí đều thất bại.");

                        throw new AggregateException(
                            "BOT cần được dọn vì hồi sinh thất bại.",
                            reviveError,
                            rollbackError);
                    }

                    // Chuyển lỗi cho TryReviveBot() hẹn thử lại.
                    throw;
                }

                // BOT đã sống và có mặt trong grid mới.
                // Lỗi gửi thông báo không được làm hồi sinh lại lần nữa.
                try
                {
                    NotifyNearClientsToAddSelf(bot);
                }
                catch (Exception notifyError)
                {
                    BotRuntimeLog.Write(
                        $"[BotReviveNotifyFailed] Bot={bot.RoleID}" +
                        $" Phase=AddNew Error={notifyError.Message}");
                }

                BotRuntimeLog.Write(
                    $"[BotReviveWorldCompleted] Bot={bot.RoleID}" +
                    $" Map={bot.CurrentMapCode}" +
                    $" Pos={bot.CurrentPos}" +
                    $" HP={bot.m_CurrentLife}");
            }
        }

        public static int RandomDelaySeconds(int min, int max)
        {
            lock(botRandomLock)
            {
                return botRandom.Next(min, max+1);
            }
        }
    }
}
