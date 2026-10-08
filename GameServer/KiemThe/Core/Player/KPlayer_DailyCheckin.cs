using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using GameServer.KiemThe;
using GameServer.KiemThe.GameEvents.DailyCheckin;
using Newtonsoft.Json;
using ProtoBuf;
using Server.Tools;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        public RoleDailyCheckin RoleDailyCheckinData { get; set; }
        
        [ProtoContract]
        public class RoleDailyCheckin
        {
            [ProtoMember(1)]
            public int[] days;
            [ProtoMember(2)]
            public int[] steps;
            [ProtoMember(3)]
            public string lastCheckinTime;
            public int step;

            public void SetLastCheckinTime()
            {
                lastCheckinTime = KTGlobal.GetDateNow().ToString("yyyy-MM-dd HH:mm:ss");
            }

            public DateTime GetLastCheckinTime()
            {
                if (string.IsNullOrEmpty(lastCheckinTime))
                {
                    Reset();
                }
                return DateTime.ParseExact(lastCheckinTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            public void Reset()
            {
                var now = KTGlobal.GetDateNow();
                var dayAmount = DateTime.DaysInMonth(now.Year, now.Month);
                days = new int[dayAmount]; // ngày đếm từ 0
                steps = new int[5];
                lastCheckinTime = KTGlobal.GetDateNow().ToString("yyyy-MM-dd HH:mm:ss");
                step = 0;
            }

            public void CalculatorStep()
            {
                step = days.Count(x=>x==1);
            }
        }

        public DateTime GetLastDailyCheckin()
        {
            return RoleDailyCheckinData.GetLastCheckinTime();
        }
        
        public void ResetDailyCheckin()
        {
            RoleDailyCheckinData.Reset();
            SaveRoleDailyCheckinData();
        }

        public bool GetActiveDailyCheckin(int day)
        {
            return RoleDailyCheckinData.days[day-1] == 1;
        }

        public void DailyCheckin(int day)
        {
            RoleDailyCheckinData.days[day-1] = 1;
            RoleDailyCheckinData.lastCheckinTime = KTGlobal.GetDateNow().ToString("yyyy-MM-dd HH:mm:ss");
            RoleDailyCheckinData.CalculatorStep();
            SaveRoleDailyCheckinData();
        }

        public string GetDailyCheckinDataString()
        {
            if (RoleDailyCheckinData == null || RoleDailyCheckinData.days == null)
            {
                RoleDailyCheckinData = new RoleDailyCheckin();
                ResetDailyCheckin();
            }
            var xml = new XDocument(
                new XElement("DailyCheckin",
                    new XElement("ToDay", KTGlobal.GetDateNow().Day),
                    new XElement("CheckinDays", RoleDailyCheckinData.days.ToArrayString()),
                    new XElement("RewardCounter", RoleDailyCheckinData.steps.ToArrayString()),
                    new XElement("Counter", RoleDailyCheckinData.step),
                    new XElement("SecondToReset", (int)GetTimeToReset())
                )
            );

            double GetTimeToReset()
            {
                var now = KTGlobal.GetDateNow();
                var nextDay = now.Date.AddDays(1);
                return (nextDay - now).TotalSeconds + 1;
            }
            return xml.ToString();
        }

        public int GetCheckinDayCount()
        {
            return RoleDailyCheckinData.step;
        }

        private void SaveRoleDailyCheckinData()
        {
            byte[] DataByteArray = DataHelper.ObjectToBytes(this.RoleDailyCheckinData);
            string InfoEncoding = Convert.ToBase64String(DataByteArray);
            Global.SaveRoleParamsStringToDB(this, RoleParamName.DailyCheckin, InfoEncoding, true);
        }

        public void LoadRoleDailyCheckinData(string data)
        {
            if (data == null)
            {
                RoleDailyCheckinData = new RoleDailyCheckin();
                ResetDailyCheckin();
                return;
            }
            byte[] DataByteArray = Convert.FromBase64String(data);
            RoleDailyCheckinData = DataHelper.BytesToObject<RoleDailyCheckin>(DataByteArray, 0, DataByteArray.Length);
            RoleDailyCheckinData.CalculatorStep();
        }

        public void SetCheckinReward(int index, string reward)
        {
            RoleDailyCheckinData.steps[index] = 1;
            SaveRoleDailyCheckinData();
        }

        public bool GetDailyCheckinReward(int index)
        {
            return RoleDailyCheckinData.steps[index] == 1;
        }

        public int GetMaxDayInMonth()
        {
            return RoleDailyCheckinData.days.Length;
        }
    }
}

public static class Helper
{
    public static string ToArrayString<T>(this T[] arrays) where T : IComparable
    {
        return  string.Join(",", arrays);
    }
}