using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.GameEvents.SpecialEvent
{
    /// <summary>
    /// Quản lý Timer sự kiện đặc biệt
    /// </summary>
    public partial class SpecialEventManager
    {
        #region Private fields
        /// <summary>
        /// Thời điểm vừa tái tạo điểm thu thập
        /// </summary>
        private readonly Dictionary<SpecialEvent.EventInfo.GrowPointInfo, long> lastGrowPointRespawnTicks = new Dictionary<SpecialEvent.EventInfo.GrowPointInfo, long>();

        private Dictionary<SpecialEvent.EventInfo, SpecialEvent.EventInfo.TimeStamp> eventList = new Dictionary<SpecialEvent.EventInfo, SpecialEvent.EventInfo.TimeStamp>();
        #endregion

        #region Timer Ticks
        /// <summary>
        /// Xử lý Logic Timer
        /// </summary>
        private void Timer_Tick()
        {
            /// Duyệt danh sách sự kiện
            foreach (SpecialEvent.EventInfo eventInfo in SpecialEvent.Events.Values)
            {

                /// Giờ hiện tại
                int nHour = DateTime.Now.Hour;
                /// Phút hiện tại
                int nMinute = DateTime.Now.Minute;
                /// Tạo đối tượng TimeStamp
                SpecialEvent.EventInfo.TimeStamp nowTime = new SpecialEvent.EventInfo.TimeStamp(nHour, nMinute);

                if (eventList.TryGetValue(eventInfo, out SpecialEvent.EventInfo.TimeStamp time))
                {
                    if (time == nowTime)
                    { 
                        break; 
                    }    
                }
                /// Nếu là sự kiện dạng thu thập nguyên liệu đổi quà
                if (eventInfo.Type == SpecialEvent.EventType.CollectGrowPoints)
                {
                    int numMap = 10;
                    List<SpecialEvent.EventInfo.GrowPointInfo> growPoints = new List<SpecialEvent.EventInfo.GrowPointInfo>();
                    while (growPoints.Count < numMap)
                    {
                        int rand = KTGlobal.GetRandomNumber(0, eventInfo.GrowPoints.Count - 1);
                        if (!growPoints.Contains(eventInfo.GrowPoints[rand]))
                        {
                            growPoints.Add(eventInfo.GrowPoints[rand]);
                        }    
                    }
                    
                    /// Duyệt danh sách điểm thu thập
                    foreach (SpecialEvent.EventInfo.GrowPointInfo growPointInfo in growPoints)
                    {
                        if (growPointInfo.Type == 1)
                        {
                            /// nếu chưa đến thời gian xuất hiện thì thôi
                            if (!growPointInfo.SpamnAt.Contains(nowTime))
                            {
                                continue;
                            }

                            /// Thực hiện tạo điểm thu thập
                            SpecialEvent_Logic.RespawnGrowPoint(growPointInfo);

                            ///// Đánh dấu đã tồn tại trong danh sách chưa
                            //bool isExisted = true;
                            ///// Nếu chưa tồn tại trong danh sách quản lý thời gian tái sinh
                            //if (!this.lastGrowPointRespawnTicks.TryGetValue(growPointInfo, out _))
                            //{
                            //    this.lastGrowPointRespawnTicks[growPointInfo] = 0;
                            //    /// Đánh dấu chưa tồn tại trong danh sách
                            //    isExisted = false;
                            //}
                            //if(!isExisted)
                            //{
                            //    
                            //    /// Đánh dấu thời gian tái sinh
                            //    this.lastGrowPointRespawnTicks[growPointInfo] = KTGlobal.GetCurrentTimeMilis();
                            //}
                        }
                        else
                        {
                            /// Nếu chưa đến giờ
                            if (nowTime < growPointInfo.FromHour || nowTime >= growPointInfo.ToHour)
                            {
                                continue;
                            }
                            /// Đánh dấu đã tồn tại trong danh sách chưa
                            bool isExisted = true;
                            /// Nếu chưa tồn tại trong danh sách quản lý thời gian tái sinh
                            if (!this.lastGrowPointRespawnTicks.TryGetValue(growPointInfo, out _))
                            {
                                this.lastGrowPointRespawnTicks[growPointInfo] = 0;
                                /// Đánh dấu chưa tồn tại trong danh sách
                                isExisted = false;
                            }

                            /// Nếu thời gian tái sinh là -1
                            if (growPointInfo.RespawnTicks == -1)
                            {
                                /// Nếu chưa tồn tại trong danh sách
                                if (!isExisted)
                                {
                                    /// Đánh dấu thời gian tái sinh
                                    this.lastGrowPointRespawnTicks[growPointInfo] = KTGlobal.GetCurrentTimeMilis();
                                    /// Thực hiện tạo điểm thu thập
                                    SpecialEvent_Logic.RespawnGrowPoint(growPointInfo);
                                }
                                /// Bỏ qua
                                return;
                            }


                            /// Nếu chưa đến thời gian tái sinh
                            if (KTGlobal.GetCurrentTimeMilis() - this.lastGrowPointRespawnTicks[growPointInfo] < growPointInfo.RespawnTicks)
                            {
                                continue;
                            }

                            /// Đánh dấu thời gian tái sinh
                            this.lastGrowPointRespawnTicks[growPointInfo] = KTGlobal.GetCurrentTimeMilis();
                            /// Thực hiện tạo điểm thu thập
                            SpecialEvent_Logic.RespawnGrowPoint(growPointInfo);
                        }
                    }

                    /// Đánh dấu là đã xong sự kiện
                    eventList[eventInfo] = nowTime;
                }
            }
        }
        #endregion
    }
}
