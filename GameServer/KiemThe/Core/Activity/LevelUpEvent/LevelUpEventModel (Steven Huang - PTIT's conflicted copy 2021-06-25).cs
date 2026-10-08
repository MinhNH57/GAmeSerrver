using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Activity.LevelUpEvent
{
	[XmlRoot(ElementName = "LevelUpItem")]
	public class LevelUpItem
	{
		[XmlAttribute(AttributeName = "ID")]
		public int ID { get; set; }

		[XmlAttribute(AttributeName = "ToLevel")]
		public int ToLevel { get; set; }

		[XmlAttribute(AttributeName = "LevelUpGift")]
		public string LevelUpGift { get; set; }
	}

	[XmlRoot(ElementName = "LevelUpGiftConfig")]
	public class LevelUpGiftConfig
	{

		[XmlElement(ElementName = "LevelUpItem")]
		public List<LevelUpItem> LevelUpItem { get; set; }

		/// <summary>
		/// Danh sách trạng thái
		/// </summary>
		public List<int> BitFlags { get; set; }
	}
}
