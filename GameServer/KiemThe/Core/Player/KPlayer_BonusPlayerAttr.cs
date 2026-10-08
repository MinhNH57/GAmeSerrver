using GameServer.KiemThe.Core.Item;
using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        private static string PathString = "Config/KT_Setting/BonusPlayerAttr.xml";

        public static List<Attribute> _ListAttributes = new List<Attribute>();

        public static void LoadConfigBonusPlayerAttr()
        {
            string Files = Global.GameResPath(PathString);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<Attribute>));

                _ListAttributes = serializer.Deserialize(stream) as List<Attribute>;
            }
            GetItemTTKEV();
            GetItemVLMTEV();
        }

        public Dictionary<int, Attribute> GetHistoryItemUsed(KPlayer player)
        {
            var result = new Dictionary<int, Attribute>();

            foreach (var attribute in _ListAttributes)
            {
                result.Add(attribute.ItemID, attribute);
            }    

            return result;
        }

        public class Attribute
        {
            [XmlAttribute(AttributeName = "ItemID")]
            public int ItemID { get; set; }
            [XmlAttribute(AttributeName = "Name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "RecordID")]
            public int RecordID { get; set; }
        }

        private static void GetItemTTKEV()
        {
            var result = new Dictionary<int, Attribute>();

            foreach (var itemData in ItemManager.TotalItem.Where(x => x.ScriptID == 200096).ToList())
            {
                ExtPram extParam = itemData.ListExtPram.Where(x => x.Index == 1).FirstOrDefault();
                /// Nếu không tìm thấy
                if (extParam == null)
                {
                    continue ;
                }
                var attribute = new Attribute
                {
                    ItemID = itemData.ItemID,
                    Name = itemData.Name,
                    RecordID = extParam.Pram
                };
                if (!_ListAttributes.Contains(attribute))
                    _ListAttributes.Add(attribute);
                result.Add(itemData.ItemID, attribute);
            }

            return ;
        }

        private static void GetItemVLMTEV()
        {

            foreach (var itemData in ItemManager.TotalItem.Where(x => x.ScriptID == 200097).ToList())
            {
                ExtPram extParam = itemData.ListExtPram.Where(x => x.Index == 1).FirstOrDefault();
                /// Nếu không tìm thấy
                if (extParam == null)
                {
                    continue;
                }
                var attribute = new Attribute
                {
                    ItemID = itemData.ItemID,
                    Name = itemData.Name,
                    RecordID = extParam.Pram
                };
                if (!_ListAttributes.Contains(attribute))
                    _ListAttributes.Add(attribute);
            }
            return;
        }

    }
}
