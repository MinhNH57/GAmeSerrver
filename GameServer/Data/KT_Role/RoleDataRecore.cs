using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Task;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Server.Data
{
    /// <summary>
    ///  Các sự kiện đánh dấu theo ngày
    /// </summary>
    ///
    [ProtoContract]
    public class DailyDataRecore
    {
        /// <summary>
        /// ID của ngày tính theo DAYOFYEAR
        /// </summary>
        ///
        [ProtoMember(1)]
        public int DayID { get; set; }

        /// <summary>
        /// Danh sách các sự kiện cần đánh dấu
        /// Ví dụ : VẬT PHẨM A ĐÃ ĂN MẤY LẦN
        /// </summary>
        ///
        [ProtoMember(2)]
        public Dictionary<int, int> EventRecoding { get; set; } = new Dictionary<int, int>();
        ///// <summary>
        ///// Danh sách các sự kiện hoat dong hang ngay cần đánh dấu
        ///// Ví dụ : nhiem vu dang nhap da lam xong chua
        ///// </summary>
        /////
        //[ProtoMember(3)]
        //public Dictionary<int, DayActivity> EventActivityRecoding { get; set; } = new Dictionary<int, DayActivity>();
        ///// <summary>
        ///// diem hoat dong ngay
        ///// </summary>
        //[ProtoMember(4)]
        //public int ScoreDay { get; set; }
        ///// <summary>
        ///// nhan den moc may roi
        ///// </summary>
        //[ProtoMember(5)]
        //public int DayActivityAwardStep { get; set; }
        ///// <summary>
        ///// danh sach qua da nhan
        ///// </summary>
        //[ProtoMember(6)]
        //public string DayActivityAwardGoodsID { get; set; }
        //public DailyDataRecore()
        //{
        //    EventRecoding = new Dictionary<int, int>();
        //    EventActivityRecoding = new Dictionary<int, DayActivity>();
        //    ScoreDay = 0;
        //    DayActivityAwardStep = 0;
        //    DayActivityAwardGoodsID = "";
        //}
        //public DailyDataRecore(int ID)
        //{
        //    this.DayID = ID;
        //    EventRecoding = new Dictionary<int, int>();
        //    EventActivityRecoding = new Dictionary<int, DayActivity>();
        //    ScoreDay = 0;
        //    DayActivityAwardStep = 0;
        //    DayActivityAwardGoodsID = "";
        //}

    }

    /// <summary>
    /// Các sự kiện đánh dấu theo tuần
    /// </summary>
    ///
    [ProtoContract]
    public class WeekDataRecore
    {
        /// <summary>
        /// ID của ngày tính theo DAYOFYEAR
        /// </summary>
        ///
        [ProtoMember(1)]
        public int WeekID { get; set; }

        /// <summary>
        /// Danh sách các sự kiện cần đánh dấu
        /// Ví dụ : VẬT PHẨM A ĐÃ ĂN MẤY LẦN
        /// </summary>
        ///
        [ProtoMember(2)]
        public Dictionary<int, int> EventRecoding { get; set; } = new Dictionary<int, int>();
    }

    [ProtoContract]
    public class MonthDataRecore
    {
        /// <summary>
        /// ID của tháng
        /// </summary>
        [ProtoMember(1)]
        public int MonthID { get; set; }

        /// <summary>
        /// Danh sách các sự kiện cần đánh dấu
        /// </summary>
        [ProtoMember(2)]
        public Dictionary<int, int> EventRecoding { get; set; } = new Dictionary<int, int>();
        /// <summary>
        /// Điểm hoạt động hằng tháng
        /// </summary>
        ///
        [ProtoMember(3)]
        public int ScoreMonth { get; set; }
        /// <summary>
        /// Lưu xem những mốc nào đã được nhận tháng
        /// </summary>
        [ProtoMember(4)]
        public Dictionary<int, bool> MonthActivityAwardStep = new Dictionary<int, bool>();
        /// <summary>
        /// danh sách quà đã nhận
        /// </summary>
        [ProtoMember(5)]
        public string MonthActivityAwardGoodsID { get; set; }
    }
    /// <summary>
    /// Các sự kiện đánh dấu vĩnh viễn 
    /// </summary>
    ///
    [ProtoContract]
    public class ForeverRecore
    {
        ///
        [ProtoMember(1)]
        public Dictionary<int, int> EventRecoding { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Dữ liệu nhiệm vụ phụ tuyến của người chơi
        /// </summary>
        [ProtoMember(2)]
        public ConcurrentDictionary<int,SideTaskData> sideTasks { get; set; } = new ConcurrentDictionary<int, SideTaskData> ();
    }

    [ProtoContract]
    public class SideTaskRecord
    {
        /// <summary>
        /// Dữ liệu nhiệm vụ phụ tuyến của người chơi
        /// </summary>
        [ProtoMember(1)]
        public ConcurrentDictionary<int,SideTaskData> sideTasks { get; set; } = new ConcurrentDictionary<int, SideTaskData> ();
    }

    [ProtoContract]
    public class DailyRechargeRecord
    {
        [ProtoMember(1)]
        public Dictionary<int, long> EventRecording { get; set; } = new Dictionary<int, long>();
        [ProtoMember(2)]
        public List<int> claimedPackage { get; set; } = new List<int> ();
        [ProtoMember(3)]
        public Dictionary<string,int> accumulatePreRound = new Dictionary<string, int> ();
        [ProtoMember(4)]
        public DateTime lastUpdate;
    }

    [ProtoContract]
    public class QuestEventRecord
    {
        [ProtoMember(1)]
        public Dictionary<int, int> EventRecording { get; set; } = new Dictionary<int, int>();

    }

    /// <summary>
    /// Các sự kiện trạng thái thiên tầng tháp
    /// </summary>
    ///
    [ProtoContract]
    public class TowerStatusData
    {
        [ProtoMember(1)]
        public Dictionary<int, int> tasksProgress = new Dictionary<int, int>();
        [ProtoMember(2)]
        public Dictionary<int, int> floorsStatus = new Dictionary<int, int>();
    }

    /// <summary>
    /// Các sự kiện hoạt động đánh dấu hằng ngày
    /// </summary>
    ///
    [ProtoContract]
    public class DailyDataRecoreActivity
    {
        /// <summary>
        /// ID của ngày tính theo DAYOFYEAR
        /// </summary>
        [ProtoMember(1)]
        public int DayID { get; set; }

        /// <summary>
        /// Danh sách các  hoạt động hằng ngày cần đánh dấu
        /// Ví dụ : nhiem vu dang nhap da lam xong chua
        /// </summary>
        [ProtoMember(2)]
        public Dictionary<int, DayActivity> EventActivityRecoding { get; set; } = new Dictionary<int, DayActivity>();
        /// <summary>
        /// Diểm hoạt động ngày
        /// </summary>
        [ProtoMember(3)]
        public int ScoreDay { get; set; }
        /// <summary>
        /// Lưu xem những mốc nào đã được nhận ngày
        /// </summary>
        [ProtoMember(4)]
        public Dictionary<int, bool> DayActivityAwardStep { get; set; }
        /// <summary>
        /// danh sách quà đã nhận
        /// </summary>
        [ProtoMember(5)]
        public string DayActivityAwardGoodsID { get; set; }

        public DailyDataRecoreActivity()
        {
            DayID = 0;
            EventActivityRecoding = new Dictionary<int, DayActivity>();
            ScoreDay = 0;
            DayActivityAwardStep = new Dictionary<int, bool>();
            DayActivityAwardGoodsID = "";
        }
    }
}