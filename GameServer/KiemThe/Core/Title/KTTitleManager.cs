using GameServer.KiemThe.Entities.Player;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace GameServer.KiemThe.Core.Title
{
	/// <summary>
	/// Quản lý danh hiệu của Game
	/// </summary>
	public static class KTTitleManager
	{
		/// <summary>
		/// Danh sách danh hiệu
		/// </summary>
		private static Dictionary<int, KTitleXML> Titles = new Dictionary<int, KTitleXML>();

		/// <summary>
		/// Khởi tạo dữ liệu
		/// </summary>
		public static void Init()
		{
			// /// Đối tượng XML tương ứng đọc từ File
			// XElement xmlNode = Global.GetGameResXml("Config/KT_Title/Title.xml");
			// /// Duyệt danh sách
			// foreach (XElement node in xmlNode.Elements("Title"))
			// {
			// 	KTitleXML titleData = KTitleXML.Parse(node);
			// 	KTTitleManager.Titles[titleData.ID] = titleData;
			// }

			Titles = new Dictionary<int, KTitleXML>();

			var titleJSON = Global.GameResPath("Config/KT_Title/title.json");
			if (!File.Exists(titleJSON)) 
				return;
			var jsonStr = File.ReadAllText(titleJSON);
			var titles = JsonConvert.DeserializeObject<List<KTitleXML>>(jsonStr);
			foreach (var title in titles)
			{
				if (title.MagicProps != null)
					foreach (var magicProp in title.MagicProps)
					{
						while (magicProp.Value.Count < 3)
						{
							magicProp.Value.Add(0);
						}
						magicProp.Value0 = magicProp.Value[0];
						magicProp.Value1 = magicProp.Value[1];
						magicProp.Value2 = magicProp.Value[2];
						magicProp.Value.Clear();
					}
				Titles[title.ID] = title;
			}
		}

		/// <summary>
		/// Trả về thông tin danh hiệu tương ứng
		/// </summary>
		/// <param name="titleID"></param>
		/// <returns></returns>
		public static KTitleXML GetTitleData(int titleID)
		{
			if (KTTitleManager.Titles.TryGetValue(titleID, out KTitleXML titleData))
			{
				return titleData;
			}
			return null;
		}

		/// <summary>
		/// Kiểm tra danh hiệu tương ứng có tồn tại không
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static bool IsTitleExist(int id)
		{
			return KTTitleManager.Titles.ContainsKey(id);
		}
	}
}
