using GameServer.Core.Executor;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameServer.KiemThe.LuaSystem.Logic;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý sự kiện trong Game
    /// </summary>
    public static class KTActivityManager
    {
        #region Thiết lập

        /// <summary>
        /// Danh sách sự kiện
        /// </summary>
        private static readonly ConcurrentDictionary<int, KTActivity> activities =
            new ConcurrentDictionary<int, KTActivity>();

        /// <summary>
        /// Tổng số sự kiện
        /// </summary>
        public static int TotalActivities
        {
            get { return KTActivityManager.activities.Count; }
        }

        #endregion

        #region Khởi tạo

        public static void LoadActivitiesFromFile(string filename)
        {
            var xmlNode = Global.GetGameResXml(filename);
            foreach (var node in xmlNode.Elements("Activity"))
            {
                var activity = ActivityXML.Parse(node);
                if (activity.Type == ActivityType.ConstructedDateTime)
                {
                    activity.Type = ActivityType.Everyday;
                    var data = activity.Time.Split('_');
                    var startTime = TimeSpan.Parse(data[0]);
                    var endTime = TimeSpan.Parse(data[1]);
                    var interval = int.Parse(data[2]);
                    var timeList = new List<TimeSpan>();
                    while (startTime < endTime)
                    {
                        timeList.Add(startTime);
                        startTime += TimeSpan.FromMinutes(interval);
                    }

                    var timeStr = string.Join("_", timeList.Select(x => $"{x.Hours};{x.Minutes}"));
                    activity.Time = timeStr;
                }

                if (activities.TryGetValue(activity.ID, out var act))
                {
                    act.Data = activity;
                }
                else
                {
                    activities[activity.ID] = new KTActivity()
                    {
                        Data = activity,
                        IsStarted = false,
                    };
                }
            }
        }

        public static void ReloadXML()
        {
            XElement xmlNode;
            if (GameManager.IsKuaFuServer)
            {
                if (GameManager.ServerLineID == 9998)
                {
                    Console.WriteLine("load ActivityKuafu Low");
                    LoadActivitiesFromFile("Config/KT_Activity/ActivityKuafuLow.xml");
                }
                else
                {
                    Console.WriteLine("load ActivityKuafu");
                    LoadActivitiesFromFile("Config/KT_Activity/ActivityKuafu.xml");
                }
            }
            else 
            {
                Console.WriteLine("load Activity");
                LoadActivitiesFromFile("Config/KT_Activity/Activity.xml");
            }
            
            if (!GameManager.IsKuaFuServer && !ServerConfig.Instance.KuaFuServerEnable)
            {
                // nếu không phải là máy chủ liên server (GameManager.IsKuaFuServer)
                // vả máy chủ hiện tại không mở liên server (ServerConfig.Instance.)
                // thì đọc và override các hoạt động từ file
                LoadActivitiesFromFile("Config/KT_Activity/Activity_KuaFuDisable.xml");
                
            } 

            // tự động mở Tần Lăng nếu chưa mở
            // dùng khi bảo trì server đột xuất
            if (activities.TryGetValue(400, out var emperorTombBegin))
            {
                if (!emperorTombBegin.IsStarted)
                {
                    if (!GameManager.IsKuaFuServer)
                    {
                        var now = DateTime.Now;
                        if (now.Hour > 7 || (now.Hour == 7 && now.Minute > 0))
                        {
                            var endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
                            if (ServerConfig.Instance.KuaFuServerEnable)
                            {
                                endOfDay = new DateTime(now.Year, now.Month, now.Day, 16, 59, 59);
                            }

                            now = now.Add(new TimeSpan(0, 5, 0));

                            var duration = endOfDay - now;
                            emperorTombBegin.Data.Time = $"{now.Hour};{now.Minute}";
                            emperorTombBegin.Data.DurationTicks = (long)duration.TotalMilliseconds;

                            SysConOut.WriteLine(
                                $"Mở lại Tần Lăng vào {now.Hour}:{now.Minute}, thời gian {duration.TotalMilliseconds}ms");
                        }
                    }
                }
            }

            if (activities.TryGetValue(403, out var emperorTombRegister))
            {
                if (!emperorTombRegister.IsStarted)
                {
                    var now = DateTime.Now;
                    if (now.Hour > 7 || (now.Hour == 7 && now.Minute > 0))
                    {
                        var endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
                        now = now.Add(new TimeSpan(0, 5, 0));

                        var duration = endOfDay - now;
                        emperorTombRegister.Data.Time = $"{now.Hour};{now.Minute}";
                        emperorTombRegister.Data.DurationTicks = (long)duration.TotalMilliseconds;
                    }
                }
            }

            // ở server event thì không bật các hoạt động này
            if (GameManager.ServerLineID == 99)
            {
                activities.Clear();
            }
        }

        /// <summary>
        /// Tải danh sách sự kiện trong hệ thống
        /// </summary>
        public static void Init()
        {
            ReloadXML();

            // Thực thi kiểm tra sự kiện liên tục
            ScheduleExecutor2.Instance.scheduleExecute(
                new NormalScheduleTask("KTActivityManager", (o, e) => { KTActivityManager.ProcessCheckActivities(); }),
                0, 10000);
        }

        /// <summary>
        /// Thực thi kiểm tra và bắt đầu các sự kiện khi đến thời gian
        /// </summary>
        private static void ProcessCheckActivities()
        {
            // Duyệt danh sách sự kiện
            foreach (var activity in KTActivityManager.activities.Values)
            {
                // Nếu đã đến thời gian bắt đầu
                if (!activity.IsStarted && KTActivityManager.IsActivityOnTime(activity))
                {
                    var nowTime = DateTime.Now;
                    LogManager.WriteLog(LogTypes.Error,
                        String.Format("Activity: {0}; Time: {1}h{2}p", activity.Data.Name, nowTime.Hour,
                            nowTime.Minute));
                    KTActivityManager.StartActivity(activity.Data.ID);
                }
            }
        }

        /// <summary>
        /// Kiểm tra sự kiện đã đến thời gian chưa
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private static bool IsActivityOnTime(KTActivity activity)
        {
            // Nếu sự kiện đã bắt đầu rồi
            if (activity.IsStarted)
            {
                return false;
            }

            // Nếu sự kiện không mở
            if (activity.Data.Type == ActivityType.None)
            {
                return false;
            }

            var now = DateTime.Now;

            if (now < activity.Data.StartDate)
                return false;

            if (now >= activity.Data.EndDate)
                return false;

            try
            {
                // Duyệt danh sách thời gian
                foreach (var timeString in activity.Data.Time.Split('_'))
                {
                    // Thiết lập thời gian
                    var timeSettings = timeString.Split(';');
                    // Loại sự kiện
                    switch (activity.Data.Type)
                    {
                        // Nếu là sự kiện 1 lần duy nhất sau khi khởi động máy chủ
                        case ActivityType.OnceOnly:
                        {
                            // Nếu thiết lập thời gian không rỗng
                            if (timeSettings.Length != 0)
                            {
                                continue;
                            }

                            return true;
                        }
                        // Nếu là sự kiện mỗi ngày
                        case ActivityType.Everyday:
                        {
                            // Nếu thiết lập thời gian không thỏa mãn
                            if (timeSettings.Length != 2)
                            {
                                continue;
                            }

                            // Giờ
                            var nHour = int.Parse(timeSettings[0]);
                            // Phút
                            var nMinute = int.Parse(timeSettings[1]);

                            // Giờ hiện tại
                            var currentHour = DateTime.Now.Hour;
                            // Phút hiện tại
                            var currentMinute = DateTime.Now.Minute;

                            // Trả về kết quả
                            if (nHour == currentHour && nMinute == currentMinute)
                            {
                                return true;
                            }

                            break;
                        }
                        // Nếu là sự kiện mỗi tuần
                        case ActivityType.EveryWeek:
                        {
                            // Nếu thiết lập thời gian không thỏa mãn
                            if (timeSettings.Length != 3)
                            {
                                continue;
                            }

                            // Thứ tự ngày trong tuần
                            var nWeekDay = int.Parse(timeSettings[0]);
                            // Giờ
                            var nHour = int.Parse(timeSettings[1]);
                            // Phút
                            var nMinute = int.Parse(timeSettings[2]);

                            // Thứ trong tuần của ngày hiện tại
                            var currentWeekDay = (int)DateTime.Now.DayOfWeek;
                            // Giờ hiện tại
                            var currentHour = DateTime.Now.Hour;
                            // Phút hiện tại
                            var currentMinute = DateTime.Now.Minute;

                            // Trả về kết quả
                            if (currentWeekDay == nWeekDay && nHour == currentHour && nMinute == currentMinute)
                            {
                                return true;
                            }

                            break;
                        }
                        case ActivityType.EveryMonthDay:
                        {
                            // Nếu thiết lập thời gian không thỏa mãn
                            if (timeSettings.Length != 3)
                            {
                                continue;
                            }

                            // Ngày trong tháng
                            var nDay = int.Parse(timeSettings[0]);
                            // Giờ
                            var nHour = int.Parse(timeSettings[1]);
                            // Phút
                            var nMinute = int.Parse(timeSettings[2]);

                            // Ngày hiện tại
                            var currentDay = DateTime.Now.Day;
                            // Giờ hiện tại
                            var currentHour = DateTime.Now.Hour;
                            // Phút hiện tại
                            var currentMinute = DateTime.Now.Minute;

                            // Trả về kết quả
                            if (currentDay == nDay && nHour == currentHour && nMinute == currentMinute)
                            {
                                return true;
                            }

                            break;
                        }
                        case ActivityType.EveryMonthWeekDay:
                        {
                            // Nếu thiết lập thời gian không thỏa mãn
                            if (timeSettings.Length != 4)
                            {
                                continue;
                            }

                            // Tuần trong tháng
                            var nWeek = int.Parse(timeSettings[0]);
                            // Thứ tự ngày trong tuần
                            var nWeekDay = int.Parse(timeSettings[1]);
                            // Giờ
                            var nHour = int.Parse(timeSettings[2]);
                            // Phút
                            var nMinute = int.Parse(timeSettings[3]);

                            // Tuần hiện tại
                            var currentWeek = KTGlobal.GetCurrentWeekOfMonth();
                            // Ngày hiện tại
                            var currentWeekDay = (int)DateTime.Now.DayOfWeek;
                            // Giờ hiện tại
                            var currentHour = DateTime.Now.Hour;
                            // Phút hiện tại
                            var currentMinute = DateTime.Now.Minute;

                            // Trả về kết quả
                            if (currentWeek == nWeek && currentWeekDay == nWeekDay && nHour == currentHour &&
                                nMinute == currentMinute)
                            {
                                return true;
                            }

                            break;
                        }
                        case ActivityType.FixedDateTime:
                        {
                            // Nếu thiết lập thời gian không thỏa mãn
                            if (timeSettings.Length != 5)
                            {
                                continue;
                            }

                            // Ngày
                            var nDate = int.Parse(timeSettings[0]);
                            // Tháng
                            var nMonth = int.Parse(timeSettings[1]);
                            // Năm
                            var nYear = int.Parse(timeSettings[2]);
                            // Giờ
                            var nHour = int.Parse(timeSettings[3]);
                            // Phút
                            var nMinute = int.Parse(timeSettings[4]);

                            // Ngày hiện tại
                            var currentDate = DateTime.Now.DayOfYear;
                            // Tháng hiện tại
                            var currentMonth = DateTime.Now.Month;
                            // Năm hiện tại
                            var currentYear = DateTime.Now.Year;
                            // Giờ hiện tại
                            var currentHour = DateTime.Now.Hour;
                            // Phút hiện tại
                            var currentMinute = DateTime.Now.Minute;

                            // Trả về kết quả
                            if (currentYear == nYear && currentMonth == nMonth && currentDate == nDate &&
                                nHour == currentHour && nMinute == currentMinute)
                            {
                                return true;
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            // Nếu không thỏa mãn
            return false;
        }

        /// <summary>
        /// Buộc bắt đầu một sự kiện nào đó mà không cần đợi đến thời gian thiết lập
        /// </summary>
        /// <param name="id"></param>
        public static void StartActivity(int id)
        {
            if (KTActivityManager.activities.TryGetValue(id, out var activity))
            {
                GameMapEventsManager.StartGameMapEvent(activity);
            }
        }

        /// <summary>
        /// Buộc kết thúc một sự kiện nào đó mà không cần đợi đến thười gian thiết lập
        /// </summary>
        /// <param name="id"></param>
        public static void StopActivity(int id)
        {
            if (KTActivityManager.activities.TryGetValue(id, out var activity))
            {
                activity.IsStarted = false;

                GameMapEventsManager.StopGameMapEvent(activity);
            }
        }

        public static KTActivity GetActivityConfig(int id)
        {
            if (KTActivityManager.activities.TryGetValue(id, out var activity))
            {
                return activity;
            }

            return null;
        }

        #endregion
    }
}