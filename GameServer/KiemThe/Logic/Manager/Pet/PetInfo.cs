using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Logic.Manager.Pet
{

    [XmlRoot(ElementName = "PetInfo")]
    public class PetInfo
    {
        /// <summary>
        /// id của pet 
        /// </summary>
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }

        /// <summary>
        /// Extension id của quái
        /// </summary>
        [XmlAttribute(AttributeName = "IDMonster")]
        public int IDMonster { get; set; }

        /// <summary>
        /// loại pet (có phải vip không)
        /// </summary>
        [XmlAttribute(AttributeName = "Type")]
        public int TypePet { get; set; }
        /// <summary>
        /// tỷ lệ bắt thành công
        /// </summary>
        [XmlAttribute(AttributeName = "BaseRate")]
        public int BaseRate { get; set; }
        
        [XmlAttribute(AttributeName = "CardRate")]
        public string CardRateStr { get; set; }

        /// <summary>
        /// Tỷ lệ chân nguyên
        /// </summary>
        [XmlAttribute(AttributeName = "CNRate")]
        public int CNRate { get; set; }

        public List<int> CardRate;
    }

    [XmlRoot(ElementName = "Gift_Value")]
    public class Gift_Value
    {
        [XmlAttribute(AttributeName = "ID")]
        public int IDItem { get; set; }

        [XmlAttribute(AttributeName = "Value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "TM_Value")]
    public class TM_Value
    {
        [XmlAttribute(AttributeName = "Level")]
        public int Level { get; set; }

        [XmlAttribute(AttributeName = "Value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "LinhNgo_Value")]
    public class Pet_LinhNgo_Value
    {
        [XmlAttribute(AttributeName = "LevelPet")]
        public int LevelPet { get; set; }

        [XmlAttribute(AttributeName = "Value")]
        public int Value { get; set; }
    }
    
    [XmlRoot(ElementName = "Pet")]
    public class YenTieuLauInfo
    {
        [XmlElement(ElementName = "YTLID")]
        public int YenTieuLau_ID;
        [XmlElement(ElementName = "FixedSkills")]
        public List<int> FixedSkills;
        [XmlElement(ElementName = "Gift_Value")]
        public List<Gift_Value> GiftValues { get; set; }

        [XmlElement(ElementName = "TinhHoa")]
        public List<TinhHoa> TinhHoa { get; set; }

        [XmlElement(ElementName = "PetPoint")]
        public List<Pet_Point> Pet_Point { get; set; }
        
        [XmlIgnore]
        public List<TinhHoaValue> TinhHoaValue;
        [XmlIgnore]
        public Dictionary<int, List<GiftValue>> ItemValueGift;
    }

    [XmlRoot(ElementName = "Pet")]
    public class PetInfos
    {
        [XmlElement(ElementName = "PetInfo")]
        public List<PetInfo> Pets { get;set; }

        [XmlElement(ElementName = "LinhNgo_Value")]
        public List<Pet_LinhNgo_Value> LinhNgo_Values { get; set; }

        [XmlElement(ElementName = "Gift_Value")]
        public List<Gift_Value> GiftValues { get; set; }

        [XmlElement(ElementName = "TM_Value")]
        public List<TM_Value> TMValues { get; set; }

        [XmlElement(ElementName = "PetSkill")]
        public List<PetSkill> PetSkills { get; set; }

        [XmlElement(ElementName = "PetAttr")]
        public List<PetAttrData> PetAttrData { get; set; }

        [XmlElement(ElementName = "NumberSkill")]
        public List<NumberSkill> NumberSkill { get; set; }


        [XmlElement(ElementName = "TinhHoa")]
        public List<TinhHoa> TinhHoa { get; set; }

        [XmlElement(ElementName = "Value_Skill")]
        public List<Value_Skill> Value_Skill { get; set; }

        [XmlElement(ElementName = "PetPoint")]
        public List<Pet_Point> Pet_Point { get; set; }

    }

    /// <summary>
    /// kỹ năng và tỷ lệ random ra được
    /// </summary>
    [XmlRoot(ElementName = "PetSkill")]
    public class PetSkill
    {
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }

        [XmlAttribute(AttributeName = "Rate")]
        public int Rate { get; set; }
    }

    /// <summary>
    /// chỉ số chia (thân, sức, ngoại, nội) theo tỷ lệ ra được
    /// </summary>
    [XmlRoot(ElementName = "PetAttr")]
    public class PetAttrData
    {
        [XmlAttribute(AttributeName = "Str")]
        public int Str { get; set; }
        [XmlAttribute(AttributeName = "Dex")]
        public int Dex { get; set; }
        [XmlAttribute(AttributeName = "Vit")]
        public int Vit { get; set; }
        [XmlAttribute(AttributeName = "Eng")]
        public int Eng { get; set; }
    }

    [XmlRoot(ElementName = "NumberSkill")]
    public class NumberSkill
    {
        [XmlAttribute(AttributeName = "TypePet")]
        public int TypePet { get; set; }

        [XmlAttribute(AttributeName = "CardLevel")]
        public int CardLevel { get; set; }

        [XmlAttribute(AttributeName = "SkillRate")]
        public string SkillRate { get; set; }

        [XmlAttribute(AttributeName = "IndexSkill")]
        public string IndexSkill { get; set; }
    }

    [XmlRoot(ElementName = "TinhHoa")]
    public class TinhHoa
    {
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }

        [XmlAttribute(AttributeName = "DiemLinhNgo")]
        public int DiemLinhNgo { get; set; }

        [XmlAttribute(AttributeName = "Kind")]
        public int Kind { get; set; }

        
    }

    [XmlRoot(ElementName = "Value_Skill")]
    public class Value_Skill
    {
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }

        [XmlAttribute(AttributeName = "Level")]
        public int Level { get; set; }

        [XmlAttribute(AttributeName = "ValueSkill")]
        public int ValueSkill { get; set; }
    }

    [XmlRoot(ElementName = "PetPoint")]
    public class Pet_Point
    {
        [XmlAttribute(AttributeName = "Star")]
        public int Star { get; set; }

        [XmlAttribute(AttributeName = "Point")]
        public int Point { get; set; }
    }

    public class NumberSkillRate
    {
        public int numberSkill;

        public int Rate;
    }
    public class GiftValue
    {
        public int Value { get; set; }

        public int numberSkill { get; set; }
    }

    public class TMValue
    {
        public int Level { get; set; }
        public int Value { get; set; }
    }

    public class TinhHoaValue
    {
        public int ID { get; set; }
        public int Value { get; set; }
        public int Kind { get; set; }
    }
}
