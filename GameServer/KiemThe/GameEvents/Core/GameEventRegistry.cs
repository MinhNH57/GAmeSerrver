using System;
using System.Collections.Generic;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.Core
{
    /// <summary>
    /// Registry quản lý vòng đời tập trung của tất cả Game Event.
    /// Register event trước, sau đó gọi InitAll() / ReloadAll().
    /// </summary>
    public static class GameEventRegistry
    {
        private static readonly Dictionary<string, IGameEvent> _events
            = new Dictionary<string, IGameEvent>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Đăng ký event. Ghi đè nếu key đã tồn tại.</summary>
        public static void Register(IGameEvent gameEvent)
        {
            string key = gameEvent.EventKey;
            if (string.IsNullOrEmpty(key))
            {
                SysConOut.WriteLine($"[GameEventRegistry] Không thể đăng ký {gameEvent.GetType().Name}: EventKey bị null hoặc rỗng.");
                return;
            }

            _events[key] = gameEvent;
        }

        /// <summary>Lấy event theo key. Trả về null nếu không tìm thấy.</summary>
        public static IGameEvent Get(string key)
        {
            _events.TryGetValue(key, out var evt);
            return evt;
        }

        public static T Get<T>(string key) where T : class, IGameEvent
            => Get(key) as T;

        public static void InitAll()
        {
            foreach (var evt in _events.Values)
            {
                try
                {
                    evt.Init();
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"[GameEventRegistry] Lỗi Init '{evt.EventKey}': {ex.Message}");
                }
            }
        }

        /// <summary>Gọi Reload() cho tất cả event đã đăng ký.</summary>
        public static void ReloadAll()
        {
            foreach (var evt in _events.Values)
            {
                try
                {
                    evt.Reload();
                    SysConOut.WriteLine($"[GameEventRegistry] Reloaded '{evt.EventKey}'.");
                }
                catch (Exception ex)
                {
                    SysConOut.WriteLine($"[GameEventRegistry] Lỗi Reload '{evt.EventKey}': {ex.Message}");
                }
            }
        }

        public static void ReloadEvent(string key)
        {
            var evt = Get(key);
            if (evt == null)
            {
                SysConOut.WriteLine($"[GameEventRegistry] Không tìm thấy event '{key}' để reload.");
                return;
            }

            try
            {
                evt.Reload();
                SysConOut.WriteLine($"[GameEventRegistry] Reloaded '{evt.EventKey}'.");
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine($"[GameEventRegistry] Lỗi Reload '{evt.EventKey}': {ex.Message}");
            }
        }

        /// <summary>In trạng thái tất cả event ra console.</summary>
        public static void PrintStatus()
        {
            SysConOut.WriteLine($"[GameEventRegistry] Total: {_events.Count}");
            foreach (var kvp in _events)
            {
                var config = EventTimeManager.GetEventConfig(kvp.Value.EventKey);
                string timeStr = config != null ? $"{config.StartDate:dd/MM/yyyy HH:mm} - {config.EndDate:dd/MM/yyyy HH:mm}" : "Chưa cấu hình thời gian";
                SysConOut.WriteLine($"  - {kvp.Key}: IsActive={kvp.Value.IsActive()} | Time: {timeStr}");
            }
        }

        public static void PrintEventStatus(string key)
        {
            var evt = Get(key);
            if (evt == null)
            {
                SysConOut.WriteLine($"[GameEventRegistry] Không tìm thấy event '{key}'.");
                return;
            }
            var config = EventTimeManager.GetEventConfig(evt.EventKey);
            string timeStr = config != null ? $"{config.StartDate:dd/MM/yyyy HH:mm:ss} - {config.EndDate:dd/MM/yyyy HH:mm:ss}" : "Chưa cấu hình thời gian";
            SysConOut.WriteLine($"[GameEventRegistry] Status '{key}': IsActive={evt.IsActive()}");
            SysConOut.WriteLine($"[GameEventRegistry] Time: {timeStr}");
        }
    }
}
