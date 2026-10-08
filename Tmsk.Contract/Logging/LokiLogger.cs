using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Server.Tools;

namespace Server.Logging
{
    /// <summary>
    /// Implementation của IStructuredLogger sử dụng Serilog + Grafana Loki sink.
    /// Đóng gói toàn bộ logic tạo logger, quản lý lifecycle, và hook vào LogManager.
    /// 
    /// KHÔNG nên khởi tạo trực tiếp — hãy dùng LokiLoggerBuilder.
    /// </summary>
    public class LokiLogger : IStructuredLogger
    {
        private readonly ILogger _logger;
        private bool _disposed;

        /// <summary>
        /// Khởi tạo LokiLogger — chỉ nên gọi từ LokiLoggerBuilder.Build()
        /// </summary>
        internal LokiLogger(
            string lokiUrl,
            string applicationName,
            int serverId,
            Dictionary<string, string> labels,
            string[] propertiesAsLabels,
            bool enableSelfLog)
        {
            try
            {
                if (enableSelfLog)
                {
                    Serilog.Debugging.SelfLog.Enable(Console.Out);
                }

                // Chuyển Dictionary labels → LokiLabel[]
                var lokiLabels = labels
                    .Select(kv => new LokiLabel { Key = kv.Key, Value = kv.Value })
                    .ToArray();

                _logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.WithProperty("ServerID", serverId)
                    .Enrich.WithProperty("Application", applicationName)
                    .WriteTo.GrafanaLoki(
                        lokiUrl,
                        labels: lokiLabels,
                        propertiesAsLabels: propertiesAsLabels
                    )
                    .CreateLogger();

                _logger.Information(
                    "Serilog+Loki initialized — App={Application}, ServerID={ServerID}, LokiUrl={LokiUrl}",
                    applicationName, serverId, lokiUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LokiLogger] Failed to initialize: {ex.Message}");
                _logger = null;
            }
        }

        /// <inheritdoc />
        public bool IsEnabled => _logger != null;

        /// <inheritdoc />
        public void Information(string messageTemplate, params object[] propertyValues)
        {
            _logger?.Information(messageTemplate, propertyValues);
        }

        /// <inheritdoc />
        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            _logger?.Warning(messageTemplate, propertyValues);
        }

        /// <inheritdoc />
        public void Error(string messageTemplate, params object[] propertyValues)
        {
            _logger?.Error(messageTemplate, propertyValues);
        }

        /// <inheritdoc />
        public void Write(string logType, string messageTemplate, params object[] propertyValues)
        {
            _logger?.ForContext("LogType", logType)
                   .Information(messageTemplate, propertyValues);
        }

        /// <inheritdoc />
        public void WriteStructured(string logType, string message, params (string key, object value)[] fields)
        {
            if (_logger == null || fields == null) return;

            // Bắt đầu từ context có LogType
            var contextLogger = _logger.ForContext("LogType", logType);

            // Attach từng field vào log event như property riêng biệt (cấp cao nhất)
            foreach (var (key, value) in fields)
            {
                if (key != null)
                {
                    contextLogger = contextLogger.ForContext(key, value, destructureObjects: true);
                }
            }

            // In thẳng message làm MessageTemplate (encode {} để tránh lỗi format)
            // Nhờ ForContext ở trên, các field đã tự động được gắn chung vào root log event.
            if (string.IsNullOrEmpty(message)) message = logType;
            contextLogger.Information(message.Replace("{", "{{").Replace("}", "}}"));
        }

        /// <summary>
        /// Đăng ký callback vào LogManager để tự động chuyển tiếp log lên Loki.
        /// </summary>
        internal void HookLogManager()
        {
            LogManager.OnLogWritten += (logType, logMsg, pushToLoki) =>
            {
                if (!pushToLoki) return;
                _logger?.ForContext("LogType", logType.ToString())
                       .Information("[{LogCategory}] {LogMessage}", logType.ToString(), logMsg);
            };
        }

        /// <summary>
        /// Flush và đóng Serilog logger, giải phóng tài nguyên
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                (_logger as IDisposable)?.Dispose();
            }
            catch { }
        }
    }
}
