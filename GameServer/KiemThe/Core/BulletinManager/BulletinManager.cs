using GameServer.Core.Executor;
using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Server.Tools;

namespace GameServer.KiemThe.Core.BulletinManager
{
    /// <summary>
    /// Lớp quản lý thông báo của toàn máy chủ
    /// </summary>
    public static class BulletinManager
    {
        private static int DayID;

        private const string Bulletin_Path = "Config/KT_Chat/BulletinMsg.xml";

        private static List<BulletinMsg> _Total = new List<BulletinMsg>();

        public static void Setup()
        {
            DayID = DateTime.Now.DayOfYear;

            _Total.Clear();
            Reset();

            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("NOTIFYCHECK", Processing), 5 * 1000, 2000);
        }

        public static void Reset()
        {
            try
            {
                _Total.Clear();
                var Files = Global.GameResPath(Bulletin_Path);
                using (var stream = System.IO.File.OpenRead(Files))
                {
                    var serializer = new XmlSerializer(typeof(List<BulletinMsg>));
                    _Total = serializer.Deserialize(stream) as List<BulletinMsg>;
                }

                _Total?.RemoveAll(msg =>
                {
                    var serverIds = new List<int>();
                    if (!string.IsNullOrEmpty(msg.ServerID))
                    {
                        serverIds = msg.ServerID.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                            .Select(int.Parse).ToList();
                    }

                    return serverIds.Count > 0 && !serverIds.Contains(GameManager.ServerLineID);
                });
            }
            catch
            {
                //ignore
            }
           
            // thông báo hoạt động tiên đồng ngọc nữ
            // AddTDNNBulletin();
        }

        /// <summary>
        /// tạo thông báo người thắng cuộc cho hoạt động báo danh tiên đồng ngọc nữ
        /// </summary>
        private static void AddTDNNBulletin()
        {
            var system_message = KTGlobal.GetTDNNSystemMessage();
            
            // nếu không có thông báo thì không tạo
            if (string.IsNullOrEmpty(system_message))
                return;
            
            // tạo bulletin
            var tdnnBulleting = new BulletinMsg
            {
                Messenger = system_message,
                ID = 999999,
                DayOfWeek = new List<int>{1, 2, 3, 4, 5, 6, 7},
                NotifyTimes = new List<NotifyTime>()
            };
            for (var h = 9; h < 24; h += 2)
            {
                tdnnBulleting.NotifyTimes.Add(new NotifyTime
                {
                    Hours = h,
                    Minute = 7,
                    IsPushNotify = false
                });
            }
            _Total.Add(tdnnBulleting);
        }

        private static void Processing(object sender, EventArgs e)
        {
            try
            {
                // Thực hiện reset lại thông báo khi qua ngày
                if (DayID != DateTime.Now.DayOfYear)
                {
                    DayID = DateTime.Now.DayOfYear;
                    Reset();
                }

                foreach (var _Msg in _Total)
                {
                    var DayOfWeek = _Msg.DayOfWeek;
                    var DayOfMonth = _Msg.DayOfMonth;

                    var Today = TimeUtil.GetWeekDay1To7(DateTime.Now);
                    var TodayOfMonth = DateTime.Now.Day;
                    
                    if (!DayOfWeek.Contains(Today) && !DayOfMonth.Contains(TodayOfMonth)) 
                        continue;
                    
                    var Now = DateTime.Now;
                    var NowHours = Now.Hour;
                    var NowMins = Now.Minute;

                    foreach (var Time in _Msg.NotifyTimes.Where(Time => Time.Hours == NowHours && Time.Minute == NowMins && Time.IsPushNotify == false))
                    {
                        Time.IsPushNotify = true;
                        KTGlobal.SendSystemEventNotification(_Msg.Messenger);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }
    }
}