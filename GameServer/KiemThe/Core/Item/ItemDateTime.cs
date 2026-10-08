using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Item
{
    public static partial class ItemDateTime
    {
        public class ItemDate
        {
            [XmlAttribute(AttributeName = "ItemID")]
            public int ItemID { get; set; }

            [XmlAttribute(AttributeName = "Name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "DateTime")]
            public string Date { get; set; }

            [XmlAttribute(AttributeName = "ServerID")]
            public string ServerID { get; set; }

            [XmlAttribute(AttributeName = "RoleID")]
            public string RoleID { get; set; }
        }

        public static List<ItemDate> ListItemDates = new List<ItemDate>();

        public static Dictionary<int, List<int>> ListItemAndRoleID = new Dictionary<int, List<int>>();

        public static void LoadConfig()
        {
            ListItemDates.Clear();
            ListItemAndRoleID.Clear();
            string PathString = "Config/KT_Item/item_date.xml";
            string Files = Global.GameResPath(PathString);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<ItemDate>));

                ListItemDates = serializer.Deserialize(stream) as List<ItemDate>;
            }
        }
    }
}
