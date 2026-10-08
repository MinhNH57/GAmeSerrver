using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Activity.EveryDayActivity
{
    /// <summary>
    /// Khởi tạo ban đầu để đọc từ xml file
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "DayActivityEvent")]
    public class ActivityInfo
    {
        [ProtoMember(1)]
        [XmlElement(ElementName = "Mission")]
        public ActivityEvent _ActivityEvent;

        [ProtoMember(2)]
        [XmlElement(ElementName = "DayActivityCollect")]
        public DayActivityCollect _DayActivityCollect;

        [ProtoMember(3)]
        [XmlElement(ElementName = "MonthActivityCollect")]
        public MonthActivityCollect _MonthActivityCollect;
    }
    /// <summary>
    /// Thông tin người chơi cần đền bù
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "Acitvity")]
    public class AllPlayerInfoBugActivity
    {
        [ProtoMember(1)]
        [XmlElement(ElementName = "Bug")]
        public List<PlayerInfoBugActivity> _InfoBugPlayer;
    }
    /// <summary>
    /// Thông tin người chơi cần đền bù
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "Bug")]
    public class PlayerInfoBugActivity
    {
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "ID")]
        public int RoleID;
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "Name")]
        public string RoleName;
        [ProtoMember(3)]
        [XmlAttribute(AttributeName = "MaxScoreMonth")]
        public int IndexScoreMonthAdd;
        [ProtoMember(4)]
        [XmlAttribute(AttributeName = "GiftCollect")]
        public int stepGiftCollect;
    }
    #region Quản lý nhiệm vụ
    /// <summary>
    /// Danh sách nhiem vu
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "DayActivity")]
    public class DayActivity
    {
        /// <summary>
        /// Ten nhiem vu 
        /// </summary>
        //[ProtoMember(1)]
        //[XmlAttribute(AttributeName = "Name")]
        //public string Name { get; set; }
        /// <summary>
        /// ID nhiem vu 
        /// </summary>
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }
        /// <summary>
        /// loai nhiem vu
        /// </summary>
        [ProtoMember(3)]
        [XmlAttribute(AttributeName = "Type")]
        public int Type { get; set; }
        /// <summary>
        /// so luong item de hoan thanh nhiem vu
        /// </summary>
        [ProtoMember(4)]
        [XmlAttribute(AttributeName = "NumberItem")]
        public int NumberItem { get; set; }
        /// <summary>
        /// diem duoc cong 
        /// </summary>
        [ProtoMember(5)]
        [XmlAttribute(AttributeName = "Score")]
        public int Score { get; set; }
        /// <summary>
        /// co duoc lap lai khong
        /// </summary>
        [ProtoMember(6)]
        [XmlAttribute(AttributeName = "Repeat")]
        public int Repeat { get; set; }
        /// <summary>
        /// co duoc lap lai khong
        /// </summary>
        [ProtoMember(7)]
        [XmlAttribute(AttributeName = "IndexRepeat")]
        public int IndexRepeat { get; set; }
        /// <summary>
        /// da hoan thanh chua  
        /// </summary>
        [ProtoMember(8)]
        [XmlIgnore]
        public bool isDone { get; set; }
        /// <summary>
        /// lam duoc bao nhieu lan roi 
        /// </summary>
        [ProtoMember(9)]
        [XmlIgnore]
        public int indexComplete { get; set; }
        /// <summary>
        /// so luon item da su dung de lam nhiem vu
        /// </summary>
        [ProtoMember(10)]
        [XmlIgnore]
        public int numberItemUsed { get; set; }
    }
    /// <summary>
    /// Thông tin của ng chơi về điểm năng động (gửi về client)
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "Mission")]
    public class ActivityEvent
    {
        /// <summary>
        /// ID nhiem vu 
        /// </summary>
        [ProtoMember(1)]
        [XmlElement(ElementName = "DayActivity")]
        public List<DayActivity> dayActivities { get; set; }

        /// <summary>
        /// Điểm ngày
        /// </summary>
        [ProtoMember(2)]
        [XmlIgnore]
        public int ScoreDay { get; set; }
        /// <summary>
        /// Đã nhận mốc nào của ngày rồi
        /// </summary>
        [ProtoMember(3)]
        [XmlIgnore]
        public Dictionary<int, bool> DayActivityAwardStep { get; set; }
        /// <summary>
        /// Điểm tháng
        /// </summary>
        [ProtoMember(5)]
        [XmlIgnore]
        public int ScoreMonth { get; set; }

        [ProtoMember(6)]
        [XmlIgnore]
        public DayActivityCollect DayActivityAward { get; set; }
        /// <summary>
        /// Đã nhận mốc nào của tháng rồi
        /// </summary>
        [ProtoMember(7)]
        [XmlIgnore]
        public Dictionary<int, bool> MonthActivityAwardStep { get; set;}

        [ProtoMember(8)]
        [XmlIgnore]
        public MonthActivityCollect MonthActivityAward { get; set; }

        /// <summary>
        /// Max điểm ngày có thể hiện trên thanh ở client
        /// </summary>
        [ProtoMember(10)]
        [XmlAttribute(AttributeName = "MaxScoreDay")]
        public int MaxScoreDayActivity { get; set; }
        /// <summary>
        /// Max điểm tháng có thể hiện trên thanh ở client
        /// </summary>
        [ProtoMember(11)]
        [XmlAttribute(AttributeName = "MaxScoreMonth")]
        public int MaxScoreMonthActivity { get; set; }
        /// <summary>
        /// Đã mua Vip hay chưa < 0 là chưa = 1 là rồi
        /// </summary>
        [ProtoMember(12)]
        [XmlIgnore]
        public int VipActivity { get; set; }
        /// <summary>
        /// Giá tiền mua VIP ĐNĐ
        /// </summary>
        [ProtoMember(13)]
        [XmlAttribute(AttributeName = "PriceVip")]
        public int PriceVip { get; set; }

        [ProtoMember(14)]
        [XmlAttribute(AttributeName = "TypeMoney")]
        public int TypeMoney { get; set; }

        /// <summary>
        /// Điểm thêm cho người chơi vip mỗi ngày
        /// </summary>
        [ProtoMember(15)]
        [XmlAttribute(AttributeName = "BonusScore")]
        public int BonusScore { get; set; }
        /// <summary>
        /// Check xem đã nhận thưởng hằng ngày VIP chưa
        /// </summary>
        [ProtoMember(16)]
        [XmlIgnore]
        public int VipCollectScore { get; set; }
    }
    #endregion

    #region Quản lý vật phẩm
    /// <summary>
    /// Vật phẩm sẽ nhận được
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "AwardItem")]
    public class AwardItem
    {
        /// <summary>
        /// ID vật phẩm
        /// </summary>
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "ItemID")]
        public int ItemID { get; set; }

        /// <summary>
        /// Số lượng
        /// </summary>
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "Number")]
        public int Number { get; set; }

        /// <summary>
        /// RATE sẽ random ra
        /// </summary>
        [ProtoMember(3)]
        [XmlAttribute(AttributeName = "Rate")]
        public int Rate { get; set; }

    }
    /// <summary>
    /// Thông tin quà ĐNĐ
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "Item")]
    public class DayActivityAwardItem
    {
        /// <summary>
        /// Mốc nào
        /// </summary>
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "StepID")]
        public int StepID { get; set; }
        /// <summary>
        /// mốc điểm 
        /// </summary>
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "Score")]
        public int Score { get; set; }
        /// <summary>
        /// Danh sách vật phẩm sẽ ROLL
        /// </summary>
        [ProtoMember(3)]
        [XmlElement(ElementName = "RollAwardItem")]
        public List<AwardItem> RollAwardItem { get; set; }
        /// <summary>
        /// xem đây có phải mốc thưởng vip hay không
        /// </summary>
        [ProtoMember(4)]
        [XmlAttribute(AttributeName = "Vip")]
        public int Vip { get; set; }
    }
    /// <summary>
    /// Thông tin quà ĐNĐ ngày
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "DayActivityCollect")]
    public class DayActivityCollect
    {
        /// <summary>
        /// Danh sách vật phẩm
        /// </summary>
        [ProtoMember(1)]
        [XmlElement(ElementName = "Item")]
        public List<DayActivityAwardItem> Item { get; set; }

    }
    /// <summary>
    /// Thông tin quà ĐNĐ tháng
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "MonthActivityCollect")]
    public class MonthActivityCollect
    {
        /// <summary>
        /// Danh sách vật phẩm
        /// </summary>
        [ProtoMember(1)]
        [XmlElement(ElementName = "Item")]
        public List<DayActivityAwardItem> Item { get; set; }

    }
    #endregion
}
