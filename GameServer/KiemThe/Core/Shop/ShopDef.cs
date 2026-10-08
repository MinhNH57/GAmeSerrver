using GameServer.Core.Executor;
using GameServer.KiemThe.Entities;
using ProtoBuf;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Logic.Manager.Shop
{
    public class SubRep
    {
        public bool IsBuyBack { get; set; }
        public bool IsOK { get; set; }
        public long CountLess { get; set; }
    }




    public enum LimitType
    {
        [XmlEnum("None")]
        NoLimit = -1,

        [XmlEnum("BuyCountPerDay")]
        BuyCountPerDay,

        [XmlEnum("BuyCountPerWeek")]
        BuyCountPerWeek,

        [XmlEnum("BuyCount")]
        LimitCount,

        [XmlEnum("BuyCountPerMonth")]
        BuycountPerMonth,
    }

    namespace SaleTime
    {
        [XmlRoot(ElementName ="Item")]
        public class ItemSaleTime 
        {
            [XmlAttribute(AttributeName = "ID")]
            public int ItemID {get; set;}

            [XmlAttribute(AttributeName = "StartDate", DataType = "dateTime")]
            public DateTime StartDate {get; set;}

            [XmlAttribute(AttributeName = "EndDate", DataType = "dateTime")]
            public DateTime EndDate {get; set;}

            [XmlAttribute(AttributeName = "Type")]
            public int Type {get; set;}
            [XmlAttribute(AttributeName = "ZoneConfig")]
            public string ZoneConfig { get; set; }
        }
        /// <summary>
        /// Shop Timer
        /// Quản lý việc hẹn giờ bày bán vật phẩm
        /// </summary>
        [XmlRoot(ElementName = "ShopSaleTime")]
        public class ShopTime 
        {
            [XmlElement(ElementName = "SaleTime")]
            public List<ItemSaleTime> SaleTime;
        }
    }

    public enum ShopLimitType {
        [XmlEnum("")]
        None,
        [XmlEnum("SalePerWeek")]
        SalePerWeek
    }

    /// <summary>
    /// SHOP TABLE
    /// </summary>
    /// 
    [ProtoContract]
    [XmlRoot(ElementName = "ShopTab")]
    public class ShopTab
    {
        /// <summary>
        /// Tên SHOP
        /// </summary>
        /// 

        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "ShopName")]
        public string ShopName { get; set; }

        /// <summary>
        /// ID SHOP
        /// </summary>
        /// 
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "ShopID")]
        public int ShopID { get; set; }

        /// <summary>
        /// DANH SÁCH VẬT PHẨM
        /// </summary>
        /// 
       
        [XmlElement(ElementName = "ShopItem")]
        public List<int> ShopItem { get; set; }


        [ProtoMember(3)]
        [XmlIgnore]
        public List<ShopItem> Items { get; set; }
        /// <summary>
        /// KIỂU TIỀN | 0 Bạc khóa | 1  Bạc | 2  Đồng | 3  Đồng khóa
        /// </summary>
        /// 
        [ProtoMember(4)]
        [XmlAttribute(AttributeName = "MoneyType")]
        public int MoneyType { get; set; }

        /// <summary>
        /// Có giảm giá không | Nếu cứ lớn hơn 0 thì lấy giá mặc định X vào là ra giá giảm
        /// </summary>
        /// 
        [ProtoMember(5)]
        [XmlAttribute(AttributeName = "Discount")]
        public int Discount { get; set; }

        /// <summary>
        /// Thời gian bắt đầu bán cái này chỉ máy chủ quan tâm
        /// </summary>
        /// 
        [ProtoMember(6)]
        [XmlAttribute(AttributeName = "TimeSaleStart")]
        public string TimeSaleStart { get; set; }

        /// <summary>
        /// Client có thể đọc và hiện luôn trên TOP của CỬA HÀNG
        /// </summary>
        /// 
        [ProtoMember(7)]
        [XmlAttribute(AttributeName = "TimeSaleEnd")]
        public string TimeSaleEnd { get; set; }

        [ProtoMember(8)]
        [XmlIgnore]
        public List<GoodsData> TotalSellItem { get; set; }

        [ProtoMember(tag: 9)] 
        [XmlAttribute(AttributeName = "ForceBinded")]
        public int ForceBinded;

        [ProtoMember(tag: 10)]
        [XmlAttribute(AttributeName = "EnableLog")]
        public bool EnableLog;

        [ProtoMember(tag: 11)]
        [XmlAttribute(AttributeName = "SeriesFilter")]
        public bool SeriesFilter;

        [ProtoMember(tag: 12)]
        [XmlAttribute(AttributeName = "SexFilter")]
        public bool SexFilter;

        [ProtoMember(tag:13)]
        [XmlAttribute(AttributeName = "LimitType")]
        public ShopLimitType LimitType;

        [ProtoMember(tag:14)]
        [XmlAttribute(AttributeName ="LimitValue")]
        public int LimitValue;
    }

    [ProtoContract]
    [XmlRoot(ElementName = "ShopItem")]
    public class ShopItem
    {
        /// <summary>
        /// ID thứ tự vật phẩm trên shop
        /// </summary>
        ///
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }

        /// <summary>
        /// ID template
        /// </summary>
        ///
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "ItemID")]
        public int ItemID { get; set; }

        /// <summary>
        /// Ngũ hành đồ
        /// </summary>
        ///
        [ProtoMember(3)]
        [XmlAttribute(AttributeName = "Series")]
        public int Series { get; set; } = 0;

        /// <summary>
        /// Khóa hay không khóa
        /// </summary>
        ///
        [ProtoMember(4)]
        [XmlAttribute(AttributeName = "Bind")]
        public int Bind { get; set; } = 1;

        /// <summary>
        /// Yêu cầu danh vọng hay không
        /// Nếu là -1 thì không yêu càu
        /// </summary>
        ///

        [ProtoMember(5)]
        [XmlAttribute(AttributeName = "ReputeDBID")]
        public int ReputeDBID { get; set; } = -1;

        /// <summary>
        /// Cấp độ của loại danh vọng tương ứng
        /// </summary>
        /// 
        [ProtoMember(6)]
        [XmlAttribute(AttributeName = "ReputeLevel")]
        public int ReputeLevel { get; set; } = -1;

        /// <summary>
        /// Cấp danh vọng bang hội | Trí sự v....
        /// </summary>
        ///

        [ProtoMember(7)]
        [XmlAttribute(AttributeName = "OfficialLevel")]
        public int OfficialLevel { get; set; } = -1;

        /// <summary>
        /// Yêu cầu bậc danh hiệu khi mua phi phong
        /// </summary>
        ///

        [ProtoMember(8)]
        [XmlAttribute(AttributeName = "Honor")]
        public int Honor { get; set; } = -1;

        /// <summary>
        /// Thời hạn sử dụng của vật phẩm sau khi mua
        /// </summary>
        ///

        [ProtoMember(9)]
        [XmlAttribute(AttributeName = "Expiry")]
        public int Expiry { get; set; } = -1;

        /// <summary>
        /// Điểm cống hiện bang hội khi mua
        /// </summary>
        ///

        [ProtoMember(10)]
        [XmlAttribute(AttributeName = "TongFund")]
        public int TongFund { get; set; } = -1;

        /// <summary>
        /// Yêu cầu vật phẩm nào đó
        /// </summary>
        ///
        [ProtoMember(11)]
        [XmlAttribute(AttributeName = "GoodsIndex")]
        public int GoodsIndex { get; set; } = -1;

        /// <summary>
        /// Số lượng vật phẩm yêu cầu
        /// </summary>
        ///
        [ProtoMember(12)]
        [XmlAttribute(AttributeName = "GoodsPrice")]
        public int GoodsPrice { get; set; } = -1;

        /// <summary>
        /// Giới hạn mua | -1  Không giới hạn | 0 : Giới hạn số lượng mua /1 ngày | 1 : Giới hạn số lượng mua trên tuần | 2 : Giới hạn mua cả đời
        /// </summary>
        /// 
        [ProtoMember(13)]
        [XmlAttribute(AttributeName = "LimitType")]
        public LimitType LimitType { get; set; } = LimitType.NoLimit;

        [ProtoMember(14)]
        [XmlAttribute(AttributeName = "LimitValue")]
        public int LimitValue { get; set; } = -1;

        [ProtoMember(15)]
        [XmlAttribute(AttributeName = "Count")]
        public int Count {get; set;} = 1;
    }
    /// <summary>
    /// Kỳ Trân Các
    /// </summary>
    [ProtoContract]
    public class TokenShop
    {
        /// <summary>
        /// Danh sách Shop trong tiệm Đồng
        /// </summary>
        [ProtoMember(1)]
        public List<ShopTab> Token { get; set; }

        /// <summary>
        /// Danh sách Shop trong tiệm Đồng khóa
        /// </summary>
        [ProtoMember(2)]
        public List<ShopTab> BoundToken { get; set; }

        /// <summary>
        /// Danh sách gói hàng bán trên Store
        /// </summary>
        [ProtoMember(3)]
        public List<TokenShopStoreProduct> StoreProducts { get; set; }
    }

    /// <summary>
    /// Thông tin gói hàng bán trên Service Store (App store, Play store, ...)
    /// </summary>
    [ProtoContract]
    public class TokenShopStoreProduct
	{
        /// <summary>
        /// ID gói hàng
        /// </summary>
        [ProtoMember(1)]
        public string ID { get; set; }

        /// <summary>
        /// Tên gói hàng
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; }

        /// <summary>
        /// Hint gói hàng
        /// </summary>
        [ProtoMember(3)]
        public string Hint { get; set; }

        /// <summary>
        /// Icon gói hàng
        /// </summary>
        [ProtoMember(4)]
        public string Icon { get; set; }

        /// <summary>
        /// Khuyến nghị mua không
        /// </summary>
        [ProtoMember(5)]
        public bool Recommend { get; set; }

        /// <summary>
        /// Giá mua
        /// </summary>
        [ProtoMember(6)]
        public int Price { get; set; }


        
        public int Token { get; set; }

        public int FirstBonus { get; set; }
    }
}