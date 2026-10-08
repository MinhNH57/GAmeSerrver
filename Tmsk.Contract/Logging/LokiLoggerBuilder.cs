using System;
using System.Collections.Generic;
using System.Configuration;
using Server.Tools;

namespace Server.Logging
{
    /// <summary>
    /// Builder Pattern để cấu hình LokiLogger một cách linh hoạt.
    /// Hỗ trợ đọc config từ app.config hoặc truyền thủ công qua code.
    /// 
    /// Cách dùng:
    /// <code>
    /// var logger = new LokiLoggerBuilder()
    ///     .ReadFromAppConfig()
    ///     .WithServerId(serverId)
    ///     .HookIntoLogManager()
    ///     .Build();
    /// </code>
    /// </summary>
    public class LokiLoggerBuilder
    {
        private string _lokiUrl;
        private string _applicationName;
        private int _serverId;
        private readonly Dictionary<string, string> _labels = new Dictionary<string, string>();
        private readonly List<string> _propertiesAsLabels = new List<string> { "LogType" };
        private bool _hookLogManager;
        private bool _enableSelfLog;

        /// <summary>
        /// Đọc toàn bộ cấu hình từ app.config (ConfigurationManager.AppSettings).
        /// Các key được hỗ trợ:
        ///   - LokiUrl: URL endpoint của Loki (bắt buộc)
        ///   - LokiAppName: tên ứng dụng → label "app"
        ///   - LokiEnvironment: môi trường → label "environment"
        ///   - cluster label should be supplied by caller from runtime ServerLine if needed
        /// </summary>
        public LokiLoggerBuilder ReadFromAppConfig()
        {
            // Đọc URL
            _lokiUrl = ConfigurationManager.AppSettings["LokiUrl"];

            // Đọc tên app
            var appName = ConfigurationManager.AppSettings["LokiAppName"];
            if (!string.IsNullOrEmpty(appName))
            {
                _applicationName = appName;
                _labels["app"] = appName;
            }

            // Đọc environment
            var env = ConfigurationManager.AppSettings["LokiEnvironment"];
            if (!string.IsNullOrEmpty(env))
            {
                _labels["environment"] = env;
            }

            return this;
        }

        /// <summary>
        /// Override URL của Loki endpoint (ưu tiên cao hơn config)
        /// </summary>
        public LokiLoggerBuilder WithLokiUrl(string url)
        {
            _lokiUrl = url;
            return this;
        }

        /// <summary>
        /// Đặt tên ứng dụng (enriched property "Application")
        /// </summary>
        public LokiLoggerBuilder WithApplication(string appName)
        {
            _applicationName = appName;
            return this;
        }

        /// <summary>
        /// Đặt Server ID (enriched property "ServerID" + label "server_id")
        /// </summary>
        public LokiLoggerBuilder WithServerId(int serverId)
        {
            _serverId = serverId;
            return this;
        }

        /// <summary>
        /// Thêm 1 label cho Loki
        /// </summary>
        public LokiLoggerBuilder WithLabel(string key, string value)
        {
            _labels[key] = value;
            return this;
        }

        /// <summary>
        /// Thêm nhiều labels cùng lúc cho Loki
        /// </summary>
        public LokiLoggerBuilder WithLabels(Dictionary<string, string> labels)
        {
            if (labels != null)
            {
                foreach (var kv in labels)
                {
                    _labels[kv.Key] = kv.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Chỉ định properties nào của log event sẽ thành label trên Loki.
        /// Mặc định: "LogType"
        /// </summary>
        public LokiLoggerBuilder WithPropertiesAsLabels(params string[] props)
        {
            _propertiesAsLabels.Clear();
            _propertiesAsLabels.AddRange(props);
            return this;
        }

        /// <summary>
        /// Tự động đăng ký delegate callback vào LogManager.OnLogWritten.
        /// Mọi log từ LogManager.WriteLog sẽ tự động đẩy lên Loki.
        /// </summary>
        public LokiLoggerBuilder HookIntoLogManager()
        {
            _hookLogManager = true;
            return this;
        }

        /// <summary>
        /// Bật Serilog SelfLog để debug khi Loki không nhận được log
        /// </summary>
        public LokiLoggerBuilder EnableSelfLog()
        {
            _enableSelfLog = true;
            return this;
        }

        /// <summary>
        /// Tạo LokiLogger instance từ cấu hình đã thiết lập.
        /// </summary>
        /// <returns>IStructuredLogger instance, sẵn sàng ghi log</returns>
        /// <exception cref="InvalidOperationException">Nếu LokiUrl chưa được cấu hình</exception>
        public IStructuredLogger Build()
        {
            if (string.IsNullOrEmpty(_lokiUrl))
            {
                throw new InvalidOperationException(
                    "[LokiLoggerBuilder] LokiUrl chưa được cấu hình. " +
                    "Hãy gọi .ReadFromAppConfig() hoặc .WithLokiUrl(\"...\") trước .Build().");
            }

            // Luôn thêm server_id vào labels
            _labels["server_id"] = _serverId.ToString();

            var logger = new LokiLogger(
                _lokiUrl,
                _applicationName ?? "GameServer",
                _serverId,
                _labels,
                _propertiesAsLabels.ToArray(),
                _enableSelfLog
            );

            // Hook vào LogManager nếu được yêu cầu
            if (_hookLogManager)
            {
                logger.HookLogManager();
            }

            return logger;
        }
    }
}
