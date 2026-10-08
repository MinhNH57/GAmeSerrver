namespace GameServer.KiemThe.GameEvents.Core
{
    /// <summary>
    /// Contract cho tất cả Game Event. Implement để đăng ký vào <see cref="GameEventRegistry"/>.
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>Phải khớp với TimeKey trong EventsTimeConfig.xml.</summary>
        string EventKey { get; }

        /// <summary>Load config XML và đăng ký event listener. Gọi khi server khởi động.</summary>
        void Init();

        /// <summary>Reload config mà không cần restart server.</summary>
        void Reload();

        /// <summary>Kiểm tra event có đang trong thời gian hoạt động không.</summary>
        bool IsActive();
    }
}
