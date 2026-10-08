using System.Xml.Linq;
using static GameServer.KiemThe.Utilities.KTMath;

namespace GameServer.KiemThe.Entities
{
    public class GiftCode
    {
        public int codeID { get; set; }

        public int status { get; set; }

        public string name { get; set; }

        public string msg { get; set; }

        public string item { get; set; }

        public int token { get; set; }

        public int boundMoney { get; set; }

        public int boundToken { get; set; }

        public int gatherPoint { get; set; }

        public int makePoint { get; set; }

        public string roleID { get; set; }

        public static GiftCode Parse(XElement xmlNode)
        {
            return new GiftCode()
            {
                codeID = int.Parse(xmlNode.Attribute("codeID").Value),
                status = int.Parse(xmlNode.Attribute("status").Value),
                name = xmlNode.Attribute("name").Value,
                msg = xmlNode.Attribute("msg").Value,
                item = xmlNode.Attribute("item").Value,
                token = int.Parse(xmlNode.Attribute("token").Value),
                boundMoney = int.Parse(xmlNode.Attribute("boundMoney").Value),
                boundToken = int.Parse(xmlNode.Attribute("boundToken").Value),
                gatherPoint = int.Parse(xmlNode.Attribute("gatherPoint").Value),
                makePoint = int.Parse(xmlNode.Attribute("makePoint").Value),
                roleID = xmlNode.Attribute("roleID").Value,
            };
        }
    }
}