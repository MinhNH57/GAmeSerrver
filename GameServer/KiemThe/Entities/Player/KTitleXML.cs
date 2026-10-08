using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Entities.Player
{
	/// <summary>
	/// Định nghĩa danh hiệu của người chơi
	/// </summary>
	public class KTitleXML
	{
		/// <summary>
		/// ID danh hiệu
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// Tên danh hiệu
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Mô tả danh hiệu
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Thời gian tồn tại (giờ)
		/// </summary>
		public int Duration { get; set; }

		

		[Serializable]
		public class MagicInfo
		{
			public string Name { get; set; }
			
			public List<int> Value { get; set; }

			public int Value0 { get; set; }

			public int Value1 { get; set; }

			public int Value2 { get; set; }

			public static MagicInfo Parse(XElement xmlNode)
			{
				return new MagicInfo()
				{
					Name = xmlNode.Attribute("Name").Value,
					Value0 = int.Parse(xmlNode.Attribute("Value0").Value),
					Value1 = int.Parse(xmlNode.Attribute("Value1").Value),
					Value2 = int.Parse(xmlNode.Attribute("Value2").Value),
				};
			}

		}

		public List<MagicInfo> MagicProps { get; set; }

		/// <summary>
		/// Chuyển đối tượng từ XMLNode
		/// </summary>
		/// <param name="xmlNode"></param>
		/// <returns></returns>
		public static KTitleXML Parse(XElement xmlNode)
		{
			KTitleXML title =  new KTitleXML()
			{
				ID = int.Parse(xmlNode.Attribute("ID").Value),
				Text = xmlNode.Attribute("Text").Value,
				Description = xmlNode.Attribute("Description").Value,
				Duration = int.Parse(xmlNode.Attribute("Duration").Value),
				MagicProps = new List<MagicInfo>(),
			};

			foreach (XElement magic in xmlNode.Elements("Magic"))
            {
				title.MagicProps.Add(MagicInfo.Parse(magic));
            }

			return title;
		}
	}

	
}
