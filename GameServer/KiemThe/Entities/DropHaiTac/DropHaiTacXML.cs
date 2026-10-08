using System.Xml.Linq;
using static GameServer.KiemThe.Utilities.KTMath;

namespace GameServer.KiemThe.Entities
{
    public class DropHaiTac
    {
        public int MonsterLevel { get; set; }

        public int Money { get; set; }

        public int BindMoney { get; set; }

        public string ItemID { get; set; }

        public string ItemHTID { get; set; }

        public int Rate { get; set; }

        public string HTID { get; set; }

        public static DropHaiTac Parse(XElement xmlNode)
        {
            return new DropHaiTac()
            {
                MonsterLevel = int.Parse(xmlNode.Attribute("MonsterLevel").Value),
                Money = int.Parse(xmlNode.Attribute("Money").Value),
                ItemID = xmlNode.Attribute("ItemID").Value,
                ItemHTID = xmlNode.Attribute("ItemHTID").Value,
                BindMoney = int.Parse(xmlNode.Attribute("BindMoney").Value),
                Rate = int.Parse(xmlNode.Attribute("Rate").Value),
                HTID = xmlNode.Attribute("HTID").Value,
            };
        }
    }
}