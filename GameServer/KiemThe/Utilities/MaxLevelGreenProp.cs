using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using GameServer.Logic;
using Server.Tools;

namespace GameServer.KiemThe.Utilities
{
    public static class MaxLevelGreenProp
    {
        private static MaxLevelGPData _config;
        private const string ConfigPath = "Config/KT_Interactive/MaxLevelGreenProp.xml";
        public static void Init()
        {
            var configFilePath = Global.GameResPath(ConfigPath);
            if (!File.Exists(configFilePath))
            {
                return;
            }

            var xmlcontent = File.ReadAllText(configFilePath);
            XmlSerializer serializer = new XmlSerializer(typeof(MaxLevelGPData));
            using (TextReader reader = new StringReader(xmlcontent))
            {
                _config = (MaxLevelGPData)serializer.Deserialize(reader);
                SysConOut.WriteLine("Read Max Level GreenProp Config OK");
            }
        }

        public static byte GetMaxLevel(int itemid, string propname)
        {
            byte result = 0;
            var u = _config.items.Find(x=>x.ItemID == itemid && x.MagicName == propname);
            if (u != null)
            {
                result = u.MaxLevel;
            }
            return result;
        }
    }
    
    [XmlRoot("Config")]
    public class MaxLevelGPData{
        [XmlElement("Item")]
        public List<MaxLevelGPItem> items;
    }

    public class MaxLevelGPItem
    {
        [XmlAttribute("ItemID")] public int ItemID { get; set; }
        [XmlAttribute("MagicName")] public string MagicName { get; set; }
        [XmlAttribute("MaxValue")] public byte MaxLevel { get; set; }
    }
}