using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.Logic;
using Newtonsoft.Json;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.DaoPhong
{
    public static class DanphongManager
    {
        private const string ConfigPath = "Config/KT_Interactive/DanPhong.xml";
        private const int PackDivisor = 100;
        private static DanphongData rawData;

        public static void Init()
        {
            var configFilePath = Global.GameResPath(ConfigPath);
            if (!File.Exists(configFilePath))
            {
                return;
            }

            var xmlcontent = File.ReadAllText(configFilePath);
            XmlSerializer serializer = new XmlSerializer(typeof(DanphongData));
            using (TextReader reader = new StringReader(xmlcontent))
            {
                rawData = (DanphongData)serializer.Deserialize(reader);
                SysConOut.WriteLine("Read Danphong OK");
            }
        }

        public static int[] GetMaterials(int packId)
        {
            var packInfo = GetPackInfo(packId);
            return packInfo?.Input;
        }

        public static int[] GetNumbers(int packId)
        {
            var packInfo = GetPackInfo(packId);
            return packInfo?.NumberItem;
        }

        public static int[] GetOutputs(int packId)
        {
            var packInfo = GetPackInfo(packId);
            return packInfo?.OutputAmount;
        }

        public static int GetMaxCreatable(int packId)
        {
            var packInfo = GetPackInfo(packId);
            return packInfo?.Max ?? 0;
        }

        private static PackData GetPackInfo(int packId)
        {
            int mainId = GetMainId(packId);
            int index = GetPackIndex(packId);
            if (index == 0)
                return null;
            // Find the pack corresponding to mainId and index
            return rawData?.List.Find(x => x.ID == mainId)?.Packs[index - 1];
        }

        private static int GetMainId(int packId) => (packId / PackDivisor) * PackDivisor;

        private static int GetPackIndex(int packId) => packId % PackDivisor;

        public static void ApplyData(KPlayer client)
        {
            int packid = 0;
            for (int i = 0; i < client._danphongIndex.Count; i++)
            {
                packid = client._danphongIndex[i];
                var packInfo = GetPackInfo(packid);
                if (packInfo != null)
                {
                    var effects = DanphongManager.GetOutputs(packid);
                    for (int j = 0; j < effects.Length; j++)
                    {
                        // ex: băng đan có output "0,0,5,0,0"
                        client.AddCurResist((DAMAGE_TYPE)j, effects[j]* client._danphongNumber[i]); //  i = 2, tương ứng DAMAGE_TYPE.damage_colds
                    }
                }
                
            }
        }
    }

    [Serializable]
    [XmlRoot("DanPhong")]
    public class DanphongData
    {
        [XmlArray("list")]
        [XmlArrayItem("item")]
        public List<DanData> List { get; set; }
    }

    [Serializable]
    public class UIData
    {
        [XmlAttribute("description")]
        public string description;
        [XmlAttribute("pack_description")]
        public string pack_description;
    }

    [Serializable]
    public class DanData
    {
        [XmlAttribute("id")]
        public int ID { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("description")]
        public string Description { get; set; }
        [XmlElement("pack")]
        public List<PackData> Packs { get; set; }

        public ItemData _output;

        public ItemData Output
        {
            get
            {
                if (_output == null)
                {
                    var item = ItemManager.GetItemTemplate(ID);
                }

                return _output;
            }
        }
    }

    [Serializable]
    public class PackData
    {
        [XmlAttribute("id")]
        public int ID { get; set; }
        [XmlAttribute("name")]
        public string Name{ get; set; }
        [XmlAttribute("materials")]
        public string Materials{ get; set; }
        [XmlAttribute("number")]
        public string Number{ get; set; }
        [XmlAttribute("output")]
        public string Output{ get; set; }
        [XmlAttribute("max")]
        public int Max{ get; set; }

        private int[] _number;

        /// <summary>
        /// Get number material require
        /// </summary>
        public int[] NumberItem
        {
            get
            {
                if (_number == null)
                {
                    var list = Number.Split(','); // exam "2,3",
                    _number = new int[list.Length];
                    for (int i = 0; i < list.Length; i++)
                    {
                        _number[i] = int.Parse(list[i]);
                    }
                }

                return _number;
            }
        }

        private int[] _input;

        /// <summary>
        /// Get array id item material to create
        /// </summary>
        public int[] Input
        {
            get
            {
                if (_input == null)
                {
                    var str = Materials.Split(','); // exam "20217,20218",
                    _input = new int[str.Length];
                    for (int i = 0; i < str.Length; i++)
                    {
                        _input[i] = int.Parse(str[i]);
                    }
                }

                return _input;
            }
        }

        /// <summary>
        /// get value to upgrade
        /// </summary>
        public int[] OutputAmount
        {
            get
            {
                string[] list = Output.Split(','); // exam "0,0,0,5,0"
                int[] result = new int[list.Length];
                for (int i = 0; i < list.Length; i++)
                {
                    if (int.TryParse(list[i], out int number))
                        result[i] = number;
                    else
                        result[i] = 0;
                }

                return result;
            }
        }
    }
}