using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.GameEvents.Interface;
using GameServer.KiemThe.GameEvents.Model;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Tools;
using System.Collections.Generic;

namespace GameServer.KiemThe.GameEvents.BaiHuTang
{
    /// <summary>
    /// Script hoạt động Bạch Hổ Đường
    /// </summary>
    public class BaiHuTang_ActivityScript : IActivityScript
    {
        #region Private fields
        /// <summary>
        /// Danh sách Script đang thực thi
        /// </summary>
        public readonly List<GameMapEvent> scripts = new List<GameMapEvent>();
		#endregion

		#region Properties
		/// <summary>
		/// Hoạt động tương ứng
		/// </summary>
		public KTActivity Activity { get; set; }
		#endregion

		#region Private methods
		/// <summary>
		/// Kiểm tra hoạt động tương ứng có tồn tại trong danh sách không
		/// </summary>
		/// <param name="activityID"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		private bool IsActivityExist(int activityID, BaiHuTang.RoundInfo list)
        {
            return list.Activities.ContainsKey(activityID);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Kiểm tra người chơi đã tham gia Bạch Hổ Đường hôm nay chưa
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool BaiHuTang_HasCompletedToday(KPlayer player)
		{
            int ret = -1;
            if (GameManager.IsKuaFuServer)
            {
                ret = player.GetValueOfDailyRecore((int)AcitvityRecore.BaiHuTangKuafu);
            }
            else
            {
                ret = player.GetValueOfDailyRecore((int)AcitvityRecore.BaiHuTang);
            }
            return ret != -1;
		}

        /// <summary>
        /// Thiết lập đánh dấu đã tham gia Bạch Hổ Đường ngày hôm nay
        /// </summary>
        /// <param name="player"></param>
        public static void BaiHuTang_SetEnteredToday(KPlayer player)
		{
            if (GameManager.IsKuaFuServer)
            {
                player.SetValueOfDailyRecore((int)AcitvityRecore.BaiHuTangKuafu, 1);
            }
            else
            {
                player.SetValueOfDailyRecore((int)AcitvityRecore.BaiHuTang, 1);
            }
            GlobalEventSource.getInstance().fireEvent(new BachHoDuongJoinEventObject(player,GameManager.IsKuaFuServer));
        }
		#endregion

		#region Core ActivityScript
		/// <summary>
		/// Hàm này được gọi khi sự kiện được khởi tạo
		/// </summary>
		/// <param name="activity"></param>
		public void Begin()
        {
            /// Nếu không có thông tin hoạt động
            if (this.Activity == null)
			{
                return;
			}

            /// ID sự kiện
            int activityID = this.Activity.Data.ID;
            /// Nếu là báo danh Bạch Hổ Đường
            if (this.IsActivityExist(activityID, BaiHuTang.Prepare))
            {
                /// Thông báo bắt đầu báo danh Bạch Hổ Đường
                KTGlobal.SendSystemEventNotification("Sự kiện Bạch Hổ Đường đã bắt đầu chuẩn bị, người chơi có thể thông qua Môn Đồ Bạch Hổ Đường ở Đại Điện để tiến vào tranh đoạt báu vật bên trong Bạch Hổ Đường.");
                /// Thiết lập mở báo danh Bạch Hổ Đường
                BaiHuTang.BeginRegistered = true;
                /// Thiết lập thông tin tầng tương ứng
                BaiHuTang.CurrentStage = 1;

                /// Duyệt danh sách bản đồ tương ứng
                foreach (int mapID in BaiHuTang.Prepare.Activities[activityID].Maps)
                {
                    /// Bản đồ tương ứng
                    GameMap map = GameManager.MapMgr.GetGameMap(mapID);
                    /// Nếu có bản đồ tương ứng
                    if (map != null)
                    {
                        /// Khởi tạo Script tương ứng
                        BaiHuTang_Script_Prepare script = new BaiHuTang_Script_Prepare(map, this.Activity);
                        /// Đánh dấu không tự xóa các đối tượng khi bị hủy
                        script.RemoveAllObjectsOnDispose = false;
                        /// Bắt đầu thực thi
                        script.Begin();
                        /// Thêm Script vào danh sách
                        this.scripts.Add(script);
                    }
                }
            }
            /// Nếu là Bạch Hổ Đường 1
            else if (this.IsActivityExist(activityID, BaiHuTang.Round1))
            {
                /// Thông báo bắt đầu Bạch Hổ Đường
                KTGlobal.SendSystemEventNotification("Sự kiện Bạch Hổ Đường đã bắt đầu. Ai sẽ là người đoạt được báu vật bên trong Bạch Hổ Đường và trở thành cao thủ võ lâm đây?");
                /// Thiết lập đóng báo danh Bạch Hổ Đường
                BaiHuTang.BeginRegistered = false;
                /// Thiết lập thông tin tầng tương ứng
                BaiHuTang.CurrentStage = 2;
                for (int i= 0; i< BaiHuTang.Round1.Activities[activityID].Maps.Count; i++)
                {
                    GameMap map = GameManager.MapMgr.GetGameMap(BaiHuTang.Round1.Activities[activityID].Maps[i]);
                    /// Nếu có bản đồ tương ứng
                    if (map != null)
                    {
                        /// Khởi tạo Script tương ứng
                        BaiHuTang_Script_Round1 script = new BaiHuTang_Script_Round1(map, this.Activity)
                        {
                            Level = BaiHuTang.Round1.Activities[activityID].ActivityLevel,
                        };

                        /// Thêm Script vào danh sách
                        this.scripts.Add(script);

                        ///// Bắt đầu thực thi
                        //script.Begin();
                        
                    }
                }   
                foreach(BaiHuTang_Script_Round1 script in this.scripts)
                {
                    script.Begin();
                    // log lại xem hoạt động có được bắt đầu ko?
                    LogManager.WriteLog(LogTypes.Error, string.Format("Baihutanground1: MapActivity: {0}", script.Map.MapCode));
                }
                ///// Duyệt danh sách bản đồ tương ứng
                //foreach (int mapID in BaiHuTang.Round1.Activities[activityID].Maps)
                //{
                //    /// Bản đồ tương ứng
                //    GameMap map = GameManager.MapMgr.GetGameMap(mapID);
                //    /// Nếu có bản đồ tương ứng
                //    if (map != null)
                //    {
                //        /// Khởi tạo Script tương ứng
                //        BaiHuTang_Script_Round1 script = new BaiHuTang_Script_Round1(map, this.Activity)
                //        {
                //            Level = BaiHuTang.Round1.Activities[activityID].ActivityLevel,
                //        };
                //        /// Bắt đầu thực thi
                //        script.Begin();
                //        /// Thêm Script vào danh sách
                //        this.scripts.Add(script);
                //    }
                //}
            }
            /// Nếu là Bạch Hổ Đường 2
            else if (this.IsActivityExist(activityID, BaiHuTang.Round2))
            {
                /// Thiết lập đóng báo danh Bạch Hổ Đường
                BaiHuTang.BeginRegistered = false;
                /// Thiết lập thông tin tầng tương ứng
                BaiHuTang.CurrentStage = 3;
                for (int i = 0; i < BaiHuTang.Round2.Activities[activityID].Maps.Count; i++)
                {
                    GameMap map = GameManager.MapMgr.GetGameMap(BaiHuTang.Round2.Activities[activityID].Maps[i]);
                    if (map != null)
                    {
                        /// Khởi tạo Script tương ứng
                        BaiHuTang_Script_Round2 script = new BaiHuTang_Script_Round2(map, this.Activity)
                        {
                            Level = BaiHuTang.Round2.Activities[activityID].ActivityLevel,
                        };
                        /// Bắt đầu thực thi
                        //script.Begin();
                        /// Thêm Script vào danh sách
                        this.scripts.Add(script);
                    }
                }

                foreach (BaiHuTang_Script_Round2 script in this.scripts)
                {
                    script.Begin();
                    LogManager.WriteLog(LogTypes.Error, string.Format("Baihutanground2: MapActivity: {0}", script.Map.MapCode));
                }
                ///// Duyệt danh sách bản đồ tương ứng
                //foreach (int mapID in BaiHuTang.Round2.Activities[activityID].Maps)
                //{
                //    /// Bản đồ tương ứng
                //    GameMap map = GameManager.MapMgr.GetGameMap(mapID);
                //    /// Nếu có bản đồ tương ứng
                //    if (map != null)
                //    {
                //        /// Khởi tạo Script tương ứng
                //        BaiHuTang_Script_Round2 script = new BaiHuTang_Script_Round2(map, this.Activity)
                //        {
                //            Level = BaiHuTang.Round2.Activities[activityID].ActivityLevel,
                //        };
                //        /// Bắt đầu thực thi
                //        script.Begin();
                //        /// Thêm Script vào danh sách
                //        this.scripts.Add(script);
                //    }
                //}
            }
            /// Nếu là Bạch Hổ Đường 3
            else if (this.IsActivityExist(activityID, BaiHuTang.Round3))
            {
                /// Thiết lập đóng báo danh Bạch Hổ Đường
                BaiHuTang.BeginRegistered = false;
                /// Thiết lập thông tin tầng tương ứng
                BaiHuTang.CurrentStage = 4;

                /// Duyệt danh sách bản đồ tương ứng
                for (int i = 0; i < BaiHuTang.Round3.Activities[activityID].Maps.Count; i++)
                {
                    GameMap map = GameManager.MapMgr.GetGameMap(BaiHuTang.Round3.Activities[activityID].Maps[i]);
                    if (map != null)
                    {
                        /// Khởi tạo Script tương ứng
                        BaiHuTang_Script_Round3 script = new BaiHuTang_Script_Round3(map, this.Activity)
                        {
                            Level = BaiHuTang.Round3.Activities[activityID].ActivityLevel,
                        };
                        /// Bắt đầu thực thi
                        //script.Begin();
                        /// Thêm Script vào danh sách
                        this.scripts.Add(script);
                    }
                }

                foreach (BaiHuTang_Script_Round3 script in this.scripts)
                {
                    script.Begin();
                    LogManager.WriteLog(LogTypes.Error, string.Format("Baihutanground3: MapActivity: {0}", script.Map.MapCode));
                }
            }
            /// Nếu là kết thúc Bạch Hổ Đường
            else if (this.IsActivityExist(activityID, BaiHuTang.End))
            {
                /// Thiết lập đóng báo danh Bạch Hổ Đường
                BaiHuTang.BeginRegistered = false;
                /// Thiết lập thông tin tầng tương ứng
                BaiHuTang.CurrentStage = -1;
                /// Đóng sự kiện
                this.Close();
            }
        }

        /// <summary>
        /// Đóng hoạt động tương ứng
        /// </summary>
        public void Close()
		{
            /// Duyệt danh sách Script và thực hiện đóng lại
            foreach (GameMapEvent script in this.scripts)
			{
                /// Hủy Script
                script.Dispose();
			}
            /// Giải phóng bộ nhớ
            this.scripts.Clear();
		}
        #endregion
    }
}
