using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameServer.Core.GameEvent;
using GameServer.Logic;

namespace GameServer.KiemThe.GameEvents.Core
{
    /// <summary>
    /// Base class cho tất cả Game Event. Xử lý: load XML, deserialize config, kiểm tra thời gian.
    /// Mặc định dùng <see cref="XmlSerializer"/> — chỉ cần gắn [XmlAttribute]/[XmlElement] lên POCO config.
    /// Override <see cref="ParseConfig"/> nếu XML có cấu trúc phức tạp cần parse thủ công.
    /// </summary>
    public abstract class BaseGameEvent<TConfig> : IGameEvent, IEventListener
        where TConfig : class, new()
    {
        /// <summary>Config đã parse từ XML.</summary>
        protected TConfig Config { get; private set; }

        protected XElement XmlData { get; private set; }

        /// <summary>Khớp với TimeKey trong EventsTimeConfig.xml.</summary>
        public abstract string EventKey { get; }

        /// <summary>Đường dẫn tương đối đến file XML, ví dụ "Config/KT_Interactive/MyEvent.xml".</summary>
        protected abstract string ConfigFilePath { get; }

        /// <summary>Đăng ký IEventListener với GlobalEventSource. Được gọi tự động trong Init().</summary>
        protected virtual void RegisterEventListeners() { }

        public virtual void processEvent(EventObject eventObject) { }

        /// <summary>
        /// Parse XML content thành TConfig.
        /// Mặc định dùng XmlSerializer. Override nếu cần logic tùy chỉnh.
        /// </summary>
        protected virtual TConfig ParseConfig(string xmlContent)
        {
            var serializer = new XmlSerializer(typeof(TConfig));
            using (var reader = XmlReader.Create(new StringReader(xmlContent)))
            {
                return (TConfig)serializer.Deserialize(reader);
            }
        }

        public virtual void Init()
        {
            var content = File.ReadAllText(Global.GameResPath(ConfigFilePath));
            XmlData = XElement.Parse(content);
            Config  = ParseConfig(content);
            RegisterEventListeners();
        }

        public virtual void Reload() => Init();

        public virtual bool IsActive() => EventTimeManager.IsEventEnabled(EventKey);
    }
}
