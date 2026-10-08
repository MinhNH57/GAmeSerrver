using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace GameServer.KiemThe.GameEvents.ThienPhuc
{
    /// <summary>
    /// Đại diện cho toàn bộ file KTThienPhuc.xml.
    /// </summary>
    [XmlRoot("ThienPhuc")]
    public class ThienPhucFileConfig
    {
        [XmlElement("Config")]
        public ThienPhucConfig Config { get; set; }

        [XmlArray("Rewards")]
        [XmlArrayItem("Reward")]
        public List<ThienPhucReward> Rewards { get; set; }
            = new List<ThienPhucReward>();
    }

    public class ThienPhucConfig
    {
        [XmlAttribute("Activate")]
        public bool Activate { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlIgnore]
        public DateTime StartTime { get; set; }

        [XmlAttribute("StartTime")]
        public string StartTimeText
        {
            get => StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            set => StartTime = DateTime.ParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );
        }

        [XmlIgnore]
        public DateTime EndTime { get; set; }

        [XmlAttribute("EndTime")]
        public string EndTimeText
        {
            get => EndTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            set => EndTime = DateTime.ParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );
        }

        [XmlAttribute("MinLevel")]
        public int MinLevel { get; set; }

        [XmlAttribute("MinNumber")]
        public int MinNumber { get; set; }

        [XmlAttribute("MaxNumber")]
        public int MaxNumber { get; set; }

        [XmlAttribute("DailyLimit")]
        public int DailyLimit { get; set; }

        [XmlAttribute("RequiredItemID")]
        public int RequiredItemID { get; set; }

        [XmlAttribute("RequiredItemCount")]
        public int RequiredItemCount { get; set; }

        [XmlIgnore]
        public List<ThienPhucReward> Rewards { get; set; }
            = new List<ThienPhucReward>();
    }

    public class ThienPhucReward
    {
        [XmlAttribute("ItemID")]
        public int ItemID { get; set; }

        [XmlAttribute("Quantity")]
        public int Quantity { get; set; }

        [XmlAttribute("Binding")]
        public int Binding { get; set; }
    }
}
