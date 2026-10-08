using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Activity.ChargePromotion
{
    [XmlRoot(ElementName = "RechargeAward")]
    public class ChargePromotionData
    {
        [XmlElement(ElementName = "DayRecharge")]
        public PromotionData ByPackagePromotion;

        [XmlElement(ElementName = "AccumulateRecharge")]
        public PromotionData AccumulatePromotion;
    }

    public class PromotionData
    {
        [XmlAttribute(AttributeName = "TimeKey")] 
        public string TimeKey;
        
        [XmlElement(ElementName = "RechargeAward")]
        public List<AwardData> Awards;

        [XmlIgnore]
        public Dictionary<int, AwardData> RewardDict;
        
        [XmlAttribute(AttributeName = "BonusCount")]
        public int BonusCount;
    }

    [XmlRoot(ElementName = "RechargeAward")]
    public class AwardData
    {
        [XmlAttribute(AttributeName = "ID")] public int ID;
        [XmlAttribute(AttributeName = "KNB")] public int KNB;
        [XmlAttribute(AttributeName = "DailyKey")] public int DailyKeyRecord;
        [XmlAttribute(AttributeName = "ItemBox")] public int RewardItemID;
        [XmlAttribute(AttributeName = "GoodsID")] public string GoodsID;
        [XmlAttribute(AttributeName = "BonusItem")] public int BonusItem;
        [XmlAttribute(AttributeName = "BonusItemContent")] public string BonusItemContent;
    }
}