using System.Collections.Generic;
using System.Xml.Serialization;
using GameServer.KiemThe.GameEvents.BaoThach;
using System.Linq;

namespace GameServer.KiemThe.HotFix
{
    public static class HotFixVuKhiTanLang
    {
        private static ListHotFixData listHotFixData = null;
        
        private const string ConfigPath = "Config/KT_Interactive/HotFixVuKhiTanLang.xml";
        
        public static void Init()
        {
            listHotFixData = BaoThachManager.LoadConfig<ListHotFixData>(ConfigPath);
        }

        public static bool HasItem(int ItemID)
        {
            return listHotFixData.Arrays.Exists(x=>x.ItemID == ItemID);
        }
    }

    [XmlRoot("ArrayOfItemData")]
    public class ListHotFixData
    {
        [XmlElement("ItemData")]
        public List<ItemData> Arrays { get; set; }
    }

    public class ItemData
    {
        [XmlAttribute("ItemID")]
        public int ItemID { get; set; }
        [XmlAttribute("DetailType")]
        public int DetailType { get; set; }
    }
}