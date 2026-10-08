using ProtoBuf;
using Server.Tools;
using System;
using System.Collections.Generic;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        public EventResetTimestamps RoleEventResetTimestamps { get; set; }

        [ProtoContract]
        public class EventResetTimestamps
        {
            [ProtoMember(1)]
            public Dictionary<string, long> LastResetTicks { get; set; } = new Dictionary<string, long>();
        }

        /// <summary>
        /// Load from base64 string (called during login in KTLogic.cs)
        /// </summary>
        public void LoadEventResetTimestamps(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                RoleEventResetTimestamps = new EventResetTimestamps();
                return;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(data);
                RoleEventResetTimestamps = DataHelper.BytesToObject<EventResetTimestamps>(bytes, 0, bytes.Length);
                if (RoleEventResetTimestamps == null)
                    RoleEventResetTimestamps = new EventResetTimestamps();
                if (RoleEventResetTimestamps.LastResetTicks == null)
                    RoleEventResetTimestamps.LastResetTicks = new Dictionary<string, long>();
            }
            catch
            {
                RoleEventResetTimestamps = new EventResetTimestamps();
            }
        }

        public DateTime GetLastResetTime(string eventKey)
        {
            if (RoleEventResetTimestamps?.LastResetTicks != null &&
                RoleEventResetTimestamps.LastResetTicks.TryGetValue(eventKey, out long ticks))
            {
                return new DateTime(ticks);
            }
            return DateTime.MinValue;
        }

        public void SetLastResetTime(string eventKey, DateTime time)
        {
            if (RoleEventResetTimestamps == null)
                RoleEventResetTimestamps = new EventResetTimestamps();
            if (RoleEventResetTimestamps.LastResetTicks == null)
                RoleEventResetTimestamps.LastResetTicks = new Dictionary<string, long>();
            RoleEventResetTimestamps.LastResetTicks[eventKey] = time.Ticks;
        }

        public void SaveEventResetTimestamps()
        {
            if (RoleEventResetTimestamps == null) return;
            byte[] bytes = DataHelper.ObjectToBytes(RoleEventResetTimestamps);
            string encoded = Convert.ToBase64String(bytes);
            Global.SaveRoleParamsStringToDB(this, RoleParamName.EventResetTimestamps, encoded, true);
        }
    }
}
