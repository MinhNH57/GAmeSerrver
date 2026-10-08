using System.Collections.Generic;
using System;
using GameServer.KiemThe.Logic.Manager.Shop;
using System.Linq;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        private List<ComposeRecord> composeRecord = new List<ComposeRecord>();

        public void AddComposeRecord(int composeId, int multiply)
        {
            composeRecord.Add(new ComposeRecord(composeId, DateTime.Now, multiply));
            // bỏ save vào db vì đang lỗi
            //SaveRecord();
        }

        public int GetComposeRecordCount(int composeId, LimitType type)
        {
            ComposeRecord[] records;
            switch (type)
            {
                case LimitType.LimitCount:
                    records = composeRecord.FindAll(x=>x.composeId == composeId).ToArray();
                    break;
                case LimitType.BuyCountPerDay:
                    records = composeRecord.FindAll(x => x.composeId == composeId && Global.GetDayOfTime(x.time) == Global.GetDayOfTime(DateTime.Now)).ToArray();
                    break;
                case LimitType.BuyCountPerWeek:
                    records = composeRecord.FindAll(x => x.composeId == composeId && Global.GetWeekOfTime(x.time) == Global.GetWeekOfTime(DateTime.Now)).ToArray();
                    break;
                case LimitType.BuycountPerMonth:
                    records = composeRecord.FindAll(x => x.composeId == composeId && Global.GetMonthOfTime(x.time) == Global.GetMonthOfTime(DateTime.Now)).ToArray();
                    break;
                default:
                    return -1;
            }

            int count = 0;
            foreach (var record in records)
            {
                count += record.multiple;
            }
            return count;
        }

        private void SaveRecord()
        {
            string recordString = null;
            foreach (ComposeRecord record in composeRecord)
            {
                recordString += record.ToString() + ";";
            }
            recordString.TrimEnd(';');
            Global.SaveRoleParamsStringToDB(this, RoleParamName.Compose_Record, recordString, true);
        }

        public void LoadComposeRecord(string data)
        {
            if (data == null)
            {
                composeRecord = new List<ComposeRecord>();
                return;
            }
            var split = data.Split(';');
            foreach (var e in split)
            {
                composeRecord.Add(new ComposeRecord(e));
            }
        }

        private class ComposeRecord
        {
            public int composeId;
            public DateTime time;
            public int multiple;

            public ComposeRecord(int composeId, DateTime time, int multiple)
            {
                this.composeId = composeId;
                this.time = time;
                this.multiple = multiple;
            }
            
            public ComposeRecord(int composeId, string time, int multiple)
            {
                this.composeId = composeId;
                this.multiple = multiple;
                this.time = DateTime.ParseExact(time, TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            }

            public ComposeRecord(string data)
            {
                string[] split = data.Split(':');
                composeId = int.Parse(split[0]);
                multiple = int.Parse(split.Length > 2 ? split[2] : "1");
                time = DateTime.ParseExact(split[1], TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
            
            
            public override string ToString()
            {
                return $"{composeId}:{time.ToString(TimeFormat)}:{multiple}";
            }

            private const string TimeFormat = "yyyy MM dd HH mm ss";
        }
    }
}