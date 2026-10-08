using System.Xml.Linq;
using static GameServer.KiemThe.Utilities.KTMath;

namespace GameServer.KiemThe.Entities
{
    public class RandomName
    {
        public string name { get; set; }

        public static RandomName Parse(XElement xmlNode)
        {
            return new RandomName()
            {
                name = xmlNode.Attribute("name").Value,
            };
        }
    }
}