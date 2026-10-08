using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Item
{
    [ProtoContract]
    [XmlRoot(ElementName = "PropInfo")]
    public class PropInfo
    {
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "BasicPropType")]
        public string BasicPropType { get; set; }

        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "BasicPropValue")]
        public int BasicPropValue { get; set; }
    }

    [ProtoContract]
    [XmlRoot(ElementName = "ItemInput")]
    public class ItemInput
    {
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "ItemID")]
        public int ItemID { get; set; }

        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "ItemCount")]
        public int ItemCount { get; set; }
    }

    [ProtoContract]
    [XmlRoot(ElementName = "ArtifactUpInfo")]
    public class ArtifactUpInfo
    {
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "LevelItem")]
        public int LevelItem { get; set; }

        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "LevelUp")]
        public int LevelUp { get; set; }

        [ProtoMember(3)]
        [XmlElement(ElementName = "PropInfo")]
        public List<PropInfo> PropInfo { get; set; }

        [ProtoMember(4)]
        [XmlElement(ElementName = "ItemInput")]
        public List<ItemInput> ItemInput { get; set; }
    }

    

}
