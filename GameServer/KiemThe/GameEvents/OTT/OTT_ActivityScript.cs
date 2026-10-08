using GameServer.KiemThe.GameEvents.Model;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.GameEvents.OTT
{
    /// <summary>
    /// Script hoạt động Đoán hoa đăng
    /// </summary>
    public class OTT_ActivityScript : IActivityScript
    {
        #region Define
        /// <summary>
        /// Hoạt động tương ứng
        /// </summary>
        public KTActivity Activity { get; set; }

        /// <summary>
        /// Danh sách Script đang thực thi
        /// </summary>
        public static OTT_Script_Main activity_script = null;
        #endregion

        #region Public methods
        /// <summary>
        /// Trả về Script ở bản đồ tương ứng
        /// </summary>
        /// <param name="mapID"></param>
        /// <returns></returns>
        public OTT_Script_Main GetScript(int mapID)
        {
            return activity_script;
        }
        #endregion

        #region Core ActivityScript
        /// <summary>
        /// Bắt đầu Đoán hoa đăng
        /// </summary>
        public void Begin()
        {
            // Nếu không có thông tin hoạt động
            if (this.Activity == null)
            {
                return;
            }

            // ID sự kiện
            var activityID = this.Activity.Data.ID;
            // Nếu là thông báo trước khi bắt đầu 10 phút
            if (activityID == 600)
            {
                // Làm rỗng Script
                activity_script = null;
                
                var map = GameManager.MapMgr.GetGameMap(OTT.Data.mapCode);
                // Nếu có bản đồ tương ứng
                if (map != null)
                {
                    // Khởi tạo Script tương ứng
                    activity_script = new OTT_Script_Main(map, this.Activity);
                    activity_script.RemoveAllObjectsOnDispose = false;
                    // Bắt đầu thực thi
                    activity_script.Begin();
                }
            }
        }

        /// <summary>
        /// Kết thúc Đoán hoa đăng
        /// </summary>
        public void Close()
        {
            activity_script.Dispose();
            activity_script = null;
        }
        #endregion
    }
}
