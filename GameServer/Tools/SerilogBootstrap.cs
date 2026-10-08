using System;
using System.Configuration;
using Server.Logging;

namespace GameServer.Tools
{
    /// <summary>
    /// Thin wrapper khởi tạo LokiLogger cho GameServer.
    /// Toàn bộ cấu hình (URL, labels, environment...) được đọc từ app.config.
    /// Deploy server khác chỉ cần sửa config, không cần build lại.
    /// </summary>
    public static class SerilogBootstrap
    {
        private static IStructuredLogger _logger;

        /// <summary>
        /// Logger instance, null nếu chưa Initialize hoặc Initialize thất bại
        /// </summary>
        public static IStructuredLogger Logger => _logger;

        /// <summary>
        /// Khởi tạo Serilog + Loki sink — đọc cấu hình từ app.config
        /// </summary>
        /// <param name="serverId">ID của game server (lấy từ GameManager.ServerId)</param>
        public static void Initialize(int serverId)
        {
            try
            {
                _logger = new LokiLoggerBuilder()
                    .ReadFromAppConfig()    // Đọc LokiUrl, LokiAppName, LokiEnvironment, LokiLabels từ config
                    .WithServerId(serverId)
                    .WithLabel("ServerID", serverId.ToString())
                    .HookIntoLogManager()   // Tự động đăng ký delegate vào LogManager
                    .EnableSelfLog()        // Bật SelfLog để debug khi cần
                    .Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SerilogBootstrap] Failed to initialize: {ex.Message}");
                _logger = null;
            }
        }

        /// <summary>
        /// Flush và đóng Serilog khi server shutdown
        /// </summary>
        public static void CloseAndFlush()
        {
            try
            {
                _logger?.Dispose();
            }
            catch { }
        }
    }
}
