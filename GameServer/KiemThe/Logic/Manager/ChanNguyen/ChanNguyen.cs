using System.Xml.Serialization;

namespace GameServer.KiemThe.Logic.Manager.ChanNguyen
{
    public class ChanNguyen
    {
        public class PropUp
        {
            /// <summary>
            /// tên tư chất
            /// </summary>
            public string NameProp { get; set; }

            /// <summary>
            /// title
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// số sao ban đầu
            /// </summary>
            public float StarOld { get; set; }

            /// <summary>
            /// số sao có thể tăng
            /// </summary>
            public float StarUp { get; set; }
        }
    }

    public class EXPCN
    {
        [XmlAttribute(AttributeName = "CNLevel")]
        public int CNLevel { get; set; }

        [XmlAttribute(AttributeName = "Level")]
        public int Level { get; set; }

        [XmlAttribute(AttributeName = "Exp")] 
        public int Exp { get; set; }
    }

    public class ChanNguyenPropUp
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Level")]
        public int Level { get; set; }

        [XmlAttribute(AttributeName = "Value")] 
        public int Value { get; set; }

        [XmlAttribute(AttributeName = "Money")]
        public int Money { get; set; }
    }

    public class PropInfo
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Point1")]
        public float Point1 { get; set; }

        [XmlAttribute(AttributeName = "Point2")]
        public float Point2 { get; set; }

        [XmlAttribute(AttributeName = "Title")]
        public string Title { get; set; }

        [XmlAttribute(AttributeName = "Rate")]
        public int Rate { get; set; }
    }

    public class RandomCN
    {
        [XmlAttribute(AttributeName = "IDCN")]
        public int IDCN { get; set; }

        [XmlAttribute(AttributeName = "RateApear")]
        public int RateApear { get; set; }

        [XmlAttribute(AttributeName = "RateProps1")]
        public string RateProps1 { get; set; }

        [XmlAttribute(AttributeName = "RateProps2")]
        public string RateProps2 { get; set; }

        [XmlAttribute(AttributeName = "RateProps3")]
        public string RateProps3 { get; set; }

        [XmlAttribute(AttributeName = "RateProps4")]
        public string RateProps4 { get; set; }
    }

    public class AnhHungInfo
    {
        [XmlAttribute(AttributeName = "BasicPropType")]
        public string BasicPropType { get; set; }

        [XmlAttribute(AttributeName = "BasicPropPA1Min")]
        public int BasicPropPA1Min { get; set; }

        [XmlAttribute(AttributeName = "BasicPropPA1Max")]
        public int BasicPropPA1Max { get; set; }

        [XmlAttribute(AttributeName = "BasicPropPA2Min")]
        public int BasicPropPA2Min { get; set; }

        [XmlAttribute(AttributeName = "BasicPropPA2Max")]
        public int BasicPropPA2Max { get; set; }

        [XmlAttribute(AttributeName = "BasicPropPA3Min")]
        public int BasicPropPA3Min { get; set; }

        [XmlAttribute(AttributeName = "BasicPropPA3Max")]
        public int BasicPropPA3Max { get; set; }

        [XmlAttribute(AttributeName = "Rate")]
        public int Rate { get; set; }
    }
}
